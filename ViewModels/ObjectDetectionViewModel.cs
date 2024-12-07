namespace CustomIdentity.ViewModels
{
    public class ObjectDetectionViewModel
    {
        public int ID { get; set; }
        public string ObjectID { get; set; }
        public decimal Probability { get; set; }
        public DateTime DetectionTime { get; set; }
        public int ObjectCount { get; set; }
        public short BatchNumber { get; set; }
    }
}