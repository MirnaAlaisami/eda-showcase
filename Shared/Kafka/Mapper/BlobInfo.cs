using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared.kafka.Mapper
{
    public class DBObject_BlobInfo
    {
        public string Account { get; set; } = "";
        public string Container { get; set; } = "";
        public string File { get; set; } = "";
        public string Uri { get; set; } = "";
        public string ConsumerKeywords { get; set; } = "";
        public DateTime TimeStamp { get; set; }
        public string Downloader { get; set; } = "";
        public string Uploader { get; set; } = "";
        public long Duration { get; set; } = 0;
        public long FileSize { get; set; } = 0;
    }
}
