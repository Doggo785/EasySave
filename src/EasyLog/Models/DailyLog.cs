using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Core.Models
{
    public class DailyLog
    {
        public DateTime TimeStamp { get; set; }
        public string ClientId { get; set; } = Environment.UserName;
        public string JobName { get; set; }
        public string SourceFile { get; set; }
        public string TargetFile { get; set; }
        public long FileSize { get; set; }
        public double TransferTimeMs { get; set; }
    }
}
