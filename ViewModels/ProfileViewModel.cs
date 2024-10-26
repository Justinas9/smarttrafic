using System.ComponentModel.DataAnnotations;

namespace CustomIdentity.ViewModels
{
    public class ProfileViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Address { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }
    }
}
