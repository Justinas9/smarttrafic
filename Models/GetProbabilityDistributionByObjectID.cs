using Microsoft.EntityFrameworkCore;

namespace CustomIdentity.Models
{
    [Keyless]
    public class GetProbabilityDistributionByObjectID
    {
        public string ObjectID { get; set; }
        public decimal AvgProbability { get; set; }
    }
}
