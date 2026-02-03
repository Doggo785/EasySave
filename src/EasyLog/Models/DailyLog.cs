using System;
using System.Collections.Generic;
using System.Text;

namespace EasyLog.Models
{
    public class DailyLog
    {
        public DateTime TimeStamp { get; set; }
        public string JobName { get; set; }
        public string SourceFile { get; set; }
        public string TargetFile { get; set; }
        public long FileSize { get; set; }
        public double TransferTimeMs { get; set; }
    }
}
