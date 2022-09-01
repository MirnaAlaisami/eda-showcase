using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Shared.AzureServices.Mapper
{
    public class BlobInfo
    {
        public BlobInfo()
        {
            Account = "";
            Container = "";
            File = "";
            Uri = "";
        }
        public BlobInfo(string account, string container, string file, string uri, DateTime timestamp)
        {
            Account = account;
            Container = container;
            File = file;
            Uri = uri;
            TimeStamp = timestamp;
        }
        public string Account;
        public string Container;
        public string File;
        public string Uri;
        public DateTime TimeStamp;
    }
    public class UploadedBlobInfo : BlobInfo
    {
        public UploadedBlobInfo(string uploader, long filesize, string account, string container, string file, string uri, DateTime timestamp) : base(account, container, file, uri, timestamp)
        {
            Uploader = uploader;
            FileSize = filesize;
        }
        public string Uploader;

        public long FileSize;
    }
    public class DownloadedBlobInfo : BlobInfo
    {
        public DownloadedBlobInfo(string downloader, long filesize, string account, string container, string file, string uri, DateTime timestamp) : base(account, container, file, uri, timestamp)
        {
            Downloader = downloader;

            FileSize = filesize;
        }
        public string Downloader;

        public long FileSize;

    }
}
