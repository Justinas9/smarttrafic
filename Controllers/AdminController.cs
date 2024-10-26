using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using CustomIdentity.Models;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> ManageRoles()
    {
        var users = _userManager.Users.ToList();
        var roles = _roleManager.Roles.ToList();

        // Create the view model
        var model = new ManageRolesViewModel(users, roles);

        // Populate UserRoles dictionary with each user's roles
        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            model.UserRoles[user.Id] = userRoles.ToList();
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateUserRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        var roles = await _userManager.GetRolesAsync(user);

        if (user != null)
        {
            await _userManager.RemoveFromRolesAsync(user, roles);
            await _userManager.AddToRoleAsync(user, role);
        }

        return RedirectToAction("ManageRoles");
    }
}
