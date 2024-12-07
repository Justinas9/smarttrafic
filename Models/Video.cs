namespace CustomIdentity.Models
{
    public class Video
    {
        public int ID { get; set; }
        public string Path { get; set; }
        public string? Extension { get; set; }
        public long SizeInBytes { get; set; }
        public int? DurationInSeconds { get; set; }
        public int? CameraID { get; set; } // Nullable foreign key
    }
}
