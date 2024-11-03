using System;

namespace CustomIdentity.Models
{
    public class VideoRequest
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public float CoordinatesX { get; set; }
        public float CoordinatesY { get; set; }
        public DateTime StartTime { get; set; }
        public string VideoFile { get; set; }
    }
}
