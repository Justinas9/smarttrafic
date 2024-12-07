using System;

namespace CustomIdentity.Models
{
    public class VideoRequest
    {
        public int ID { get; set; }
        public Guid UserID { get; set; }
        public int VideoID { get; set; }
        public DateTime StartTime { get; set; }
        public int? IntersectionID { get; set; } // Nullable foreign key
        public DateTime? EndTime { get; set; }  // Nullable datetime
    }
}
