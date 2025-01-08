using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTelegramBackuper.CLI
{
    public class UploadFiles
    {
        public List<string> FilePaths { get; set; }
        public string ChannelId { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
    }
}
