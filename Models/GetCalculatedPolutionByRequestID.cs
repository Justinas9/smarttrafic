using Microsoft.EntityFrameworkCore;

namespace CustomIdentity.Models
{
    [Keyless]
    public class GetCalculatedPolutionByRequestID
    {
        public string ObjectType { get; set; }
        public int ObjectCount { get; set; } 
        public decimal? CO { get; set; }          
        public decimal? NOX { get; set; }        
        public decimal? PM { get; set; }          
        public decimal? VOC { get; set; }         
    }
}

