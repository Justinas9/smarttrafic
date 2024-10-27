using CustomIdentity.Models;
using CustomIdentity.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CustomIdentity.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> signInManager;
        private readonly UserManager<AppUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager; // Add RoleManager

        public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager; // Initialize RoleManager
        }

        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["IsLoginRegister"] = true;
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                //login
                var result = await signInManager.PasswordSignInAsync(model.Username!, model.Password!, model.RememberMe, false);

                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }

                ModelState.AddModelError("", "Invalid login attempt");
            }
            return View(model);
        }


        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["IsLoginRegister"] = true;
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        public IActionResult SaveCoordinates(double Latitude, double Longitude)
        {
            // Process or store coordinates
            // e.g., Save to database, log, or perform other actions

            return RedirectToAction("Map"); // Redirect or return a view
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                AppUser user = new()
                {
                    Name = model.Name,
                    UserName = model.Email,
                    Email = model.Email,
                    Address = model.Address
                };

                var result = await userManager.CreateAsync(user, model.Password!);

                if (result.Succeeded)
                {
                    // Ensure the User role exists
                    if (!await roleManager.RoleExistsAsync("User"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("User"));
                    }

                    // Assign the default "User" role
                    await userManager.AddToRoleAsync(user, "User");

                    // Sign in the user
                    await signInManager.SignInAsync(user, false);

                    return RedirectToLocal(returnUrl);
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        public async Task<IActionResult> Profile()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                Name = user.Name,
                Email = user.Email,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }
        // Profile POST method
        [HttpPost]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                user.Name = model.Name;
                user.Email = model.Email;
                user.Address = model.Address;
                user.PhoneNumber = model.PhoneNumber;

                var result = await userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToAction("Profile");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }
        public async Task<IActionResult> DeleteProfile()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var result = await userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await signInManager.SignOutAsync(); // Sign out after deletion
                return RedirectToAction("Index", "Home"); // Redirect to home page after deletion
            }

            return RedirectToAction("Profile");
        }
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            // Check if returnUrl is not empty and is a local URL
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl); // Redirect to the provided return URL
            }
            // Redirect to the Index action of the Home controller
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }

}
