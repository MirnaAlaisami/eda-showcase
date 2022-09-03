using System;
using System.Threading.Tasks;
using Shared.AzureServices;
using Shared.Kafka;
using Confluent.Kafka;
using Azure.Identity;
using Azure.Storage.Blobs;
using Shared.Kafka.SchemaRegistry;
using Azure.Security.KeyVault.Keys.Cryptography;
namespace Backend
{
    class Downloader
    {
        static readonly string bootstrapServer = System.Environment.GetEnvironmentVariable("EVENTHUB_NAMESPACE") + ".servicebus.windows.net:9093";
        static readonly string endpoint = System.Environment.GetEnvironmentVariable("EVENTHUB_NAMESPACE") + ".servicebus.windows.net";
        static readonly string schemaGroup = System.Environment.GetEnvironmentVariable("SCHEMA_GROUP");
        static readonly string tokenRequestContext = "https://" + System.Environment.GetEnvironmentVariable("EVENTHUB_NAMESPACE") + ".servicebus.windows.net/.default";
        static readonly string topic = "datauploaded";
        static readonly string keyName = System.Environment.GetEnvironmentVariable("KEY_NAME");
        static readonly DefaultAzureCredential credentialToken = new DefaultAzureCredential();
        CryptographyClient cryptoClient = new CryptographyClient(new Uri(keyName), credentialToken);
        KeyResolver keyResolver = new KeyResolver(credentialToken);
        static string downloaderLocation = "";
        public async Task Subscribe(string myLocation)
        {
            downloaderLocation = myLocation;
            Console.WriteLine("Starting Consumer");
            KafkaHandler.Instance.InitAAD(credentialToken, tokenRequestContext);
            KafkaHandler.Instance.InitConsumer(bootstrapServer, Group: myLocation.ToLower());
            KafkaHandler.Instance.InitSchemaRegistry(endpoint, schemaGroup, true);
            AzureHandler.Instance.InitEncryption(cryptoClient, keyResolver);
            KafkaHandler.Instance.MessageReceived += Instance_MessageReceived;
            KafkaHandler.Instance.StartConsumer(topic);
        }
        private void Instance_MessageReceived(MessageReceivedEventArgs e)
        {
            Console.WriteLine("Message Received");
            if (e != null)
            {
                var msg = new Message<string, UploadedEventInfo>
                {
                    Key = e.Key,
                    Value = (UploadedEventInfo)e.Message
                };
                MachineLocation machineLocation = getLocation(msg.Key);
                string location = "";
                if (machineLocation.Stuttgart)
                    location = "Stuttgart";
                if (machineLocation.Hamburg)
                    location = "Hamburg";
                if (machineLocation.Frankfurt)
                    location = "Frankfurt";
                string blobUri = msg.Value.uri;
                Console.WriteLine("Received Message that blob" + blobUri + " has been uploaded in " + location);
                if (downloaderLocation == "Stuttgart")
                {
                    Console.WriteLine("I'm the central location, so I have to download all the data!");
                    BlobClient blobClient = new BlobClient(new Uri(blobUri), credentialToken);
                    AzureHandler.Instance.DownloadFileAsyncFromBlob(blobClient, "Stuttgart/");
                }
                else
                {
                    if (location == downloaderLocation)
                    {
                        Console.WriteLine("This machine belongs to me, so I'll download the data!");
                        BlobClient blobClient = new BlobClient(new Uri(blobUri), credentialToken);
                        AzureHandler.Instance.DownloadFileAsyncFromBlob(blobClient, location + "/");
                    }
                    else
                    {
                        Console.WriteLine("This machine doesn't belong to me, so I'll ignore the data!");
                    }
                }
                Console.WriteLine();
            }
        }
        private MachineLocation getLocation(string machineNr)
        {
            MachineLocation machineLocation = new MachineLocation();
            switch (machineNr)
            {
                case "1111":
                    machineLocation.Stuttgart = true;
                    break;
                case "2222":
                    machineLocation.Hamburg = true;
                    break;
                case "3333":
                    machineLocation.Frankfurt = true;
                    break;
                default:
                    break;
            }
            return machineLocation;
        }
    }
}