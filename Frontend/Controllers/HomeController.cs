﻿using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Frontend.Models;
using Azure.Storage.Blobs;
using Shared.AzureServices;
using Shared.Kafka.SchemaRegistry;
using Shared.Kafka;
using Azure.Security.KeyVault.Keys.Cryptography;
using Frontend.Infrastructure;
using Azure.Identity;
using Confluent.Kafka;
using global::Avro.Specific;
namespace Frontend.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public IActionResult Index()
        {
            return View();
        }
        [Authorize(Policy = AuthorizationPolicies.AssignmentToAzureUploadRoleRequired)]
        public async Task<IActionResult> Blob(IFormFile postedFile)
        {
            // create a blob on behalf of the user
            if (postedFile == null || postedFile.Length == 0)
                return Content("file not selected");
            string location = Request.Form["location"];
            string key = "";
            if (location == "Stuttgart")
                key = "1111";
            else if (location == "Hamburg")
                key = "2222";
            else if (location == "Frankfurt")
                key = "3333";
            string blobContainerName = location.ToLower();
            var filePath = Path.GetTempFileName();
            try
            {
                using (var stream = System.IO.File.Create(filePath))
                {
                    // The formFile is the method parameter which type is IFormFile
                    // Saves the files to the local file system using a file name generated by the app.
                    await postedFile.CopyToAsync(stream);
                }
            }
            catch (Exception e)
            {

                Console.WriteLine("Unable to upload file: " + e.Message);
            }
            string fileName = Path.GetFileName(postedFile.FileName);
            string blobName = fileName;
            string storageAccountName = System.Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_NAME");
            Uri blobContainerUri = new Uri("https://" + storageAccountName + ".blob.core.windows.net/" + blobContainerName);
            string keyName = System.Environment.GetEnvironmentVariable("KEY_NAME");
            string tokenRequestContext = "https://" + System.Environment.GetEnvironmentVariable("EVENTHUB_NAMESPACE") + ".servicebus.windows.net/.default";
            string bootstrapServer = System.Environment.GetEnvironmentVariable("EVENTHUB_NAMESPACE") + ".servicebus.windows.net:9093";
            string topic = "datauploaded";
            string SchemaRegistryUrl = "https://" + System.Environment.GetEnvironmentVariable("EVENTHUB_NAMESPACE") + ".servicebus.windows.net";
            string endpoint = System.Environment.GetEnvironmentVariable("EVENTHUB_NAMESPACE") + ".servicebus.windows.net";
            string schemaGroup = System.Environment.GetEnvironmentVariable("SCHEMA_GROUP");
            DefaultAzureCredential credentialToken = new DefaultAzureCredential();
            BlobContainerClient blobContainer = new BlobContainerClient(blobContainerUri, credentialToken);
            CryptographyClient cryptoClient = new CryptographyClient(new Uri(keyName), credentialToken);
            KeyResolver keyResolver = new KeyResolver(credentialToken);
            AzureHandler.Instance.InitEncryption(cryptoClient, keyResolver);
            Dictionary<string, string> metadata = new Dictionary<string, string>();
            await AzureHandler.Instance.UploadFileAsyncToBlob(blobContainer, blobContainerName, filePath, blobName, metadata);
            if (AzureHandler.Instance.EventInfos != null)
            {
                KafkaHandler.Instance.InitAAD(credentialToken, tokenRequestContext);
                KafkaHandler.Instance.InitProducer(bootstrapServer);
                KafkaHandler.Instance.InitSchemaRegistry(endpoint, schemaGroup, true);
                IProducer<string, ISpecificRecord> producer = KafkaHandler.Instance.BuildProducerWithAADToken();
                var msg = new Message<string, ISpecificRecord>
                {
                    Key = key,
                    Value = new UploadedEventInfo
                    {
                        uri = AzureHandler.Instance.EventInfos.Blob.Uri,
                    }
                };
                Console.WriteLine();
                Console.WriteLine("Uploaded to uri " + AzureHandler.Instance.EventInfos.Blob.Uri);
                await KafkaHandler.Instance.SendMessage(producer, topic, msg);
            }
            return View();
        }
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
