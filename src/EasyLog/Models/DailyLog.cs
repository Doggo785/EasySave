using System;

namespace EasyLog.Models
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
        public int EncryptionTimeMs { get; set; }

        public DailyLog() { }

        public DailyLog(string jobName, string source, string target, long size, double time)
        {
            TimeStamp = DateTime.Now;
            JobName = jobName;
            SourceFile = source;
            TargetFile = target;
            FileSize = size;
            TransferTimeMs = time;
            EncryptionTimeMs = 0;
        }
    }
}
