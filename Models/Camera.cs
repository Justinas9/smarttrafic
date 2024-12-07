namespace CustomIdentity.Models
{
    public class Camera
    {
        public int ID { get; set; }
        public int IntersectionID { get; set; } // Foreign key
        public string Description { get; set; }
    }
}
