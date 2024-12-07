using Microsoft.EntityFrameworkCore;

namespace CustomIdentity.Models
{
    [Keyless]
    public class ObjectDetectionCount
    {
        public string ObjectID { get; set; }
        public int ObjectCount { get; set; }
    }
}
