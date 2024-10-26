using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using CustomIdentity.Models;

public class ManageRolesViewModel
{
    public List<AppUser> Users { get; set; }
    public List<IdentityRole> Roles { get; set; }
    public Dictionary<string, List<string>> UserRoles { get; set; }

    public ManageRolesViewModel(IEnumerable<AppUser> users, IEnumerable<IdentityRole> roles)
    {
        Users = new List<AppUser>(users);
        Roles = new List<IdentityRole>(roles);
        UserRoles = new Dictionary<string, List<string>>();
    }
}