using System;

namespace CustomIdentity.Models
{
    public class ObjectDetection
    {
        public int ID { get; set; }
        public string ObjectID { get; set; }
        public decimal Probability { get; set; } // Format: decimal(2,1)
        public DateTime DetectionTime { get; set; }
        public int ObjectCount { get; set; }
        public int RequestID { get; set; }
        public short BatchNumber { get; set; } // Format: SMALLINT
    }
}
