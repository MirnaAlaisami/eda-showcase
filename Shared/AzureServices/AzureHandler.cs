using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Storage;
using System.Collections.Generic;
using System.Text;
namespace Shared.AzureServices
{
    public delegate void BlobDownloadStartedEventHandler(BlobDownloadEventInfos e);
    public delegate void BlobDownloadProgressChangedEventHandler(BlobDownloadProgressArgs e);
    public delegate void BlobDownloadFinishedEventHandler(BlobDownloadEventInfos e);
    public delegate void BlobDownloadErrorEventHandler(BlobDownloadEventErrorInfos e);
    public delegate void BlobUploadStartedEventHandler(BlobUploadEventInfos e);
    public delegate void BlobUploadProgressChangedEventHandler(BlobUploadProgressArgs e);
    public delegate void BlobUploadFinishedEventHandler(BlobUploadEventInfos e);
    public class BlobUploadEventInfos : EventArgs
    {
        public BlobUploadEventInfos(Shared.AzureServices.Mapper.UploadedBlobInfo blob, string target)
        {
            Blob = blob;
            TargetFilePath = target;
        }
        public Shared.AzureServices.Mapper.UploadedBlobInfo Blob { get; private set; }
        public string TargetFilePath { get; private set; }
    }
    public class BlobDownloadEventInfos : EventArgs
    {
        public BlobDownloadEventInfos(Shared.AzureServices.Mapper.DownloadedBlobInfo blob, string target)
        {
            Blob = blob;
            TargetFilePath = target;
        }
        public Shared.AzureServices.Mapper.DownloadedBlobInfo Blob { get; private set; }
        public string TargetFilePath { get; private set; }
    }
    public class BlobDownloadEventErrorInfos : BlobDownloadEventInfos
    {
        public BlobDownloadEventErrorInfos(Shared.AzureServices.Mapper.DownloadedBlobInfo blob, string target, string error) : base(blob, target)
        {
            ErrorMessage = error;
        }
        public string ErrorMessage { get; private set; }
    }
    public class BlobDownloadProgressArgs : System.ComponentModel.ProgressChangedEventArgs
    {
        public BlobDownloadProgressArgs(long bytesReceived, long totalBytesToReceive, long elapsed) : base(0, null)
        {
            BytesReceived = bytesReceived;
            TotalBytesToReceive = totalBytesToReceive;
            BytesLeft = TotalBytesToReceive - BytesReceived;
            if (elapsed != 0)
            {
                BytesPerMillisecond = BytesReceived / elapsed;
                if (BytesPerMillisecond != 0) TotalMillisecondsLeft = BytesLeft / BytesPerMillisecond;
            }
        }
        public long BytesReceived { get; private set; }
        public long TotalBytesToReceive { get; private set; }
        public long BytesLeft { get; private set; }
        public long BytesPerMillisecond { get; private set; }
        public long TotalMillisecondsLeft { get; private set; }
    }
    public class BlobUploadProgressArgs : System.ComponentModel.ProgressChangedEventArgs
    {
        public BlobUploadProgressArgs(long bytesSend, long totalBytesToSend, long elapsed, string target) : base(0, null)
        {
            uri = target;
            BytesSend = bytesSend;
            TotalBytesToSend = totalBytesToSend;
            BytesLeft = TotalBytesToSend - BytesSend;
            if (elapsed != 0)
            {
                BytesPerMillisecond = BytesSend / elapsed;
                if (BytesPerMillisecond != 0) TotalMillisecondsLeft = BytesLeft / BytesPerMillisecond;
            }
        }
        public string uri { get; private set; }
        public long BytesSend { get; private set; }
        public long TotalBytesToSend { get; private set; }
        public long BytesLeft { get; private set; }
        public long BytesPerMillisecond { get; private set; }
        public long TotalMillisecondsLeft { get; private set; }
    }
    public static class Base64Extensions
    {
        public static string ToBase64(this string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }
        public static string FromBase64(this string input)
        {
            var bytes = Convert.FromBase64String(input);
            return Encoding.UTF8.GetString(bytes);
        }
        public static bool IsBase64String(this string base64String)
        {
            if (string.IsNullOrEmpty(base64String) || base64String.Length % 4 != 0
               || base64String.Contains(" ") || base64String.Contains("\t") || base64String.Contains("\r") || base64String.Contains("\n"))
                return false;
            try
            {
                Convert.FromBase64String(base64String);
                return true;
            }
            catch (Exception exception)
            {
            }
            return false;
        }
    }
    public class AzureHandler
    {
        public event BlobDownloadStartedEventHandler BlobDownloadStarted;
        public event BlobDownloadProgressChangedEventHandler BlobDownloadProgressChanged;
        public event BlobDownloadFinishedEventHandler BlobDownloadFinished;
        public event BlobDownloadErrorEventHandler BlobDownloadError;
        public event BlobUploadStartedEventHandler BlobUploadStarted;
        public event BlobUploadProgressChangedEventHandler BlobUploadProgressChanged;
        public event BlobUploadFinishedEventHandler BlobUploadFinished;
        public BlobUploadEventInfos EventInfos;
        private static AzureHandler _inst; /* Instanz des Handlers */
        private Stopwatch Counter;
        private ClientSideEncryptionOptions encryptOptions = null;
        public static AzureHandler Instance
        {
            get
            {
                if (_inst == null)
                {
                    _inst = new AzureHandler();
                }
                return _inst;
            }
        }
        public AzureHandler()
        {
            Counter = new Stopwatch();
        }
        public void InitEncryption(CryptographyClient cryptoClient, KeyResolver keyResolver)
        {
            encryptOptions = GetEncryptionOptions(cryptoClient, keyResolver);
        }
        public void ResetCounter()
        {
            Counter.Reset();
            Counter.Start();
        }
        public async Task UploadFileAsyncToBlob(BlobContainerClient blobContainer, string container, string filePath, string target, Dictionary<string, string> metadata = null)
        {
            try
            {
                blobContainer.CreateIfNotExists(PublicAccessType.None);
                using (FileStream SourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    BlobClient blobClient = blobContainer.GetBlobClient(target);
                    if (encryptOptions != null)
                    {
                        blobClient = blobClient.WithClientSideEncryptionOptions(encryptOptions);
                    }
                    if (BlobUploadStarted != null) BlobUploadStarted(new BlobUploadEventInfos(blob: new Shared.AzureServices.Mapper.UploadedBlobInfo(uploader: System.Environment.UserName, filesize: SourceStream.Length, account: blobClient.AccountName, container: blobClient.BlobContainerName, file: blobClient.Name, uri: System.Web.HttpUtility.UrlDecode(blobClient.Uri.ToString()), timestamp: DateTime.Now), target: target));
                    BlobUploadOptions uploadOptions = new BlobUploadOptions();
                    uploadOptions.TransferOptions = new Azure.Storage.StorageTransferOptions
                    {
                        MaximumTransferSize = 4 * 1024 * 1024,
                        InitialTransferSize = 4 * 1024 * 1024
                    };
                    long totalReadBytesCount = SourceStream.Length;
                    uploadOptions.ProgressHandler = new Progress<long>(percent =>
                    {
                        BlobUploadProgressArgs args = new BlobUploadProgressArgs(percent, SourceStream.Length, Counter.ElapsedMilliseconds, target);
                        if (BlobUploadProgressChanged != null) BlobUploadProgressChanged(args);
                    });
                    ResetCounter();
                    await blobClient.UploadAsync(SourceStream, options: uploadOptions);
                    BlobProperties properties = await blobClient.GetPropertiesAsync();
                    if (properties.Metadata != null)
                    {
                        if (metadata == null)
                        {
                            metadata = (Dictionary<string, string>)properties.Metadata;
                        }
                        else
                        {
                            foreach (string key in properties.Metadata.Keys)
                            {
                                if (key.ToLower() != "encryptiondata") metadata.Add(key, Base64Extensions.ToBase64(properties.Metadata[key]));
                                else metadata.Add(key, properties.Metadata[key]);
                            }
                        }
                    }
                    else
                    {
                        if (metadata == null)
                        {
                            metadata = new Dictionary<string, string>();
                        }
                    }
                    if (!metadata.ContainsKey("uploader")) metadata.Add("uploader", Base64Extensions.ToBase64(System.Environment.UserName));
                    if (!metadata.ContainsKey("filesize")) metadata.Add("filesize", Base64Extensions.ToBase64(string.Format("{0}", SourceStream.Length)));
                    if (!metadata.ContainsKey("account")) metadata.Add("account", Base64Extensions.ToBase64(blobClient.AccountName));
                    if (!metadata.ContainsKey("container")) metadata.Add("container", Base64Extensions.ToBase64(blobClient.BlobContainerName));
                    if (!metadata.ContainsKey("file")) metadata.Add("file", Base64Extensions.ToBase64(blobClient.Name));
                    if (!metadata.ContainsKey("timestamp")) metadata.Add("timestamp", Base64Extensions.ToBase64(string.Format("{0}", DateTime.Now)));
                    long sourceLength = SourceStream.Length;
                    SourceStream.Close();
                    blobClient.SetMetadata(metadata);
                    EventInfos = new BlobUploadEventInfos(blob: new Shared.AzureServices.Mapper.UploadedBlobInfo(uploader: System.Environment.UserName, filesize: sourceLength, account: blobClient.AccountName, container: blobClient.BlobContainerName, file: blobClient.Name, uri: System.Web.HttpUtility.UrlDecode(blobClient.Uri.ToString()), timestamp: DateTime.Now), target: target);
                }
            }
            catch (Azure.RequestFailedException e)
            {
                Console.WriteLine("HTTP error code {0}: {1}",
                                    e.Status, e.ErrorCode);
                Console.WriteLine(e.Message);
            }
        }
        public Dictionary<string, string> GetMetadataOfBlob(BlobClient blobClient)
        {
            try
            {
                if (encryptOptions != null)
                {
                    blobClient = blobClient.WithClientSideEncryptionOptions(encryptOptions);
                }
                var task = Task.Run(async () => await blobClient.GetPropertiesAsync());
                task.Wait();
                BlobProperties properties = task.Result;
                if (properties.Metadata != null)
                {
                    return (Dictionary<string, string>)properties.Metadata;
                }
                return null;
            }
            catch (Azure.RequestFailedException e)
            {
                Console.WriteLine("HTTP error code {0}: {1}",
                                    e.Status, e.ErrorCode);
                Console.WriteLine(e.Message);
                return null;
            }
        }
        public async Task DownloadFileAsyncFromBlob(BlobClient blobClient, string targetFilePath, int sleepMs = 100)
        {
            string container = blobClient.BlobContainerName;
            if (blobClient.Exists() == false)
            {
                if (BlobDownloadError != null) BlobDownloadError(new BlobDownloadEventErrorInfos(blob: new Shared.AzureServices.Mapper.DownloadedBlobInfo(downloader: System.Environment.UserName, filesize: 0, account: "", container: container, file: "", uri: "", timestamp: DateTime.Now), target: targetFilePath, error: "Der Blob ist nicht verfügbar"));
                return;
            }
            var splittedPath = targetFilePath.Split(new char[] { '\\', '/' });
            var folder = "";
            for (int i = 0; i < splittedPath.Length - 1; i++)
            {
                folder += splittedPath[i] + "/";
            }
            if (Directory.Exists(folder) == false) Directory.CreateDirectory(folder);
            string fileName = blobClient.Name;
            string localFilePath = Path.Combine(targetFilePath, fileName);
            var file = File.Open(localFilePath, FileMode.CreateNew, FileAccess.ReadWrite);
            var blobContentLength = blobClient.GetProperties().Value.ContentLength;
            long filesize = blobContentLength;
            if (encryptOptions != null)
            {
                blobClient = blobClient.WithClientSideEncryptionOptions(encryptOptions);
                try
                {
                    if (BlobDownloadStarted != null) BlobDownloadStarted(new BlobDownloadEventInfos(blob: new Shared.AzureServices.Mapper.DownloadedBlobInfo(downloader: System.Environment.UserName, filesize: filesize, account: blobClient.AccountName, container: blobClient.BlobContainerName, file: blobClient.Name, uri: System.Web.HttpUtility.UrlDecode(blobClient.Uri.ToString()), timestamp: DateTime.Now), target: targetFilePath));
                    ResetCounter();
                    Task downloadTask = blobClient.DownloadToAsync(file);
                    await Task.Run(() =>
                    {
                        //Read how many bytes on stream.
                        while (!downloadTask.IsCompleted)
                        {
                            System.Threading.Thread.Sleep(sleepMs);
                            if (BlobDownloadProgressChanged != null) BlobDownloadProgressChanged(new BlobDownloadProgressArgs(file.Length, blobContentLength, Counter.ElapsedMilliseconds));
                        }
                        // clean up
                        file?.Close();
                        file?.Dispose();
                        var metDat = GetMetadataOfBlob(blobClient);
                        if (BlobDownloadFinished != null)
                        {
                            BlobDownloadFinished(new BlobDownloadEventInfos(blob: new Shared.AzureServices.Mapper.DownloadedBlobInfo(downloader: System.Environment.UserName, filesize: filesize, account: blobClient.AccountName, container: blobClient.BlobContainerName, file: blobClient.Name, uri: System.Web.HttpUtility.UrlDecode(blobClient.Uri.ToString()), timestamp: DateTime.Now), target: targetFilePath));
                            Console.WriteLine("Data downloaded successfully!");
                        }
                    });
                }
                catch (Exception ex)
                {
                    if (BlobDownloadError != null) BlobDownloadError(new BlobDownloadEventErrorInfos(blob: new Shared.AzureServices.Mapper.DownloadedBlobInfo(downloader: System.Environment.UserName, filesize: filesize, account: blobClient.AccountName, container: blobClient.BlobContainerName, file: blobClient.Name, uri: System.Web.HttpUtility.UrlDecode(blobClient.Uri.ToString()), timestamp: DateTime.Now), target: targetFilePath, error: string.Format("Fehler beim Download des Blob: {0}", ex.Message)));
                }
            }
        }
        private ClientSideEncryptionOptions GetEncryptionOptions(CryptographyClient cryptoClient, KeyResolver keyResolver, string KeyWrapAlgorithm = "RSA-OAEP")
        {
            ClientSideEncryptionOptions encryptionOptions = new ClientSideEncryptionOptions(ClientSideEncryptionVersion.V1_0)
            {
                KeyEncryptionKey = cryptoClient,
                KeyResolver = keyResolver,
                KeyWrapAlgorithm = KeyWrapAlgorithm
            };
            return encryptionOptions;
        }
    }
}
