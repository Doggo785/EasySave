using System;

namespace EasyLog.Models
{
    public class StateLog
    {
        public string JobName { get; set; }
        public DateTime LastActionTimestamp { get; set; }
        public string State {  get; set; }
        public int TotalFilesToCopy { get; set; }
        public long TotalFilesSize { get; set; }
        public int NbFilesLeftToDo { get; set; }
        public long RemainingFilesSize { get; set; }
        public int Progression { get; set; }
        public string CurrentSourceFilePath { get; set; }
        public string CurrentDestinationFilePath { get; set; }
    }
}
