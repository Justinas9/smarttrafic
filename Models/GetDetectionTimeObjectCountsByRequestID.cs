using Microsoft.EntityFrameworkCore;

namespace CustomIdentity.Models
{
    [Keyless]
    public class GetDetectionTimeObjectCountsByRequestID
    {
        public DateTime DetectionTime { get; set; }
        public string ObjectID { get; set; }
        public int ObjectCount { get; set; }

    }
}
