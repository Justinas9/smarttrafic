namespace CustomIdentity.Models
{
    public class Object
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public decimal MinThreshold { get; set; } // Format: decimal(2,1)
        public decimal? CO { get; set; }          // Format: decimal(6,3)
        public decimal? NOX { get; set; }         // Format: decimal(6,3)
        public decimal? PM { get; set; }          // Format: decimal(6,3)
        public decimal? VOC { get; set; }         // Format: decimal(6,3)
    }
}
