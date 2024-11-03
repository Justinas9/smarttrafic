// Models/DetectionBatch.cs
using System;

namespace CustomIdentity.Models
{
    public class DetectionBatch
    {
        public int BatchID { get; set; }
        public string? VideoID { get; set; } // Nullable string
        public string? ObjectType { get; set; } // Nullable string
        public float? Probability { get; set; } // Nullable float
        public DateTime? DetectionTime { get; set; } // Nullable DateTime
        public int? ObjectCount { get; set; } // Nullable int
        public int? SessionID { get; set; } // Nullable int
        public float? LocationX { get; set; } // Nullable float
        public float? LocationY { get; set; } // Nullable float
    }
}
