using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VehicleFleetManagement.Data;
using VehicleFleetManagement.Models;
using VehicleFleetManagement.ViewModels;

namespace VehicleFleetManagement.Controllers
{
    /// <summary>
    /// Manages user authentication, registration, external Google OAuth sign-in,
    /// logout workflows, and role-based redirect routing.
    /// Uses ASP.NET Core Identity with secure password hashing and cookie sessions.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // ==========================================
        // GET: /Account/Login
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    return await RedirectUserByRoleAsync(currentUser, returnUrl);
                }
            }

            ViewBag.Title = "Sign In - FleetFlow Pro";
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.IsGoogleConfigured = !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientId"]) &&
                                        !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientSecret"]);

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // ==========================================
        // POST: /Account/Login
        // ==========================================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewBag.Title = "Sign In - FleetFlow Pro";
            ViewBag.IsGoogleConfigured = !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientId"]) &&
                                        !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientSecret"]);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in successfully.", model.Email);
                TempData["Success"] = $"Welcome back, {user.FullName}!";

                // Session telemetry
                var roles = await _userManager.GetRolesAsync(user);
                HttpContext.Session.SetString("UserEmail", user.Email ?? string.Empty);
                HttpContext.Session.SetString("UserRole", roles.FirstOrDefault() ?? "User");
                HttpContext.Session.SetString("LoginTime", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

                return await RedirectUserByRoleAsync(user, model.ReturnUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account {Email} is locked out.", model.Email);
                ModelState.AddModelError(string.Empty, "This account has been temporarily locked out due to multiple failed attempts.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // ==========================================
        // GET: /Account/Register
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Title = "Create Account - FleetFlow Pro";
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.IsGoogleConfigured = !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientId"]) &&
                                        !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientSecret"]);

            return View(new RegisterViewModel());
        }

        // ==========================================
        // POST: /Account/Register
        // ==========================================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewBag.Title = "Create Account - FleetFlow Pro";
            ViewBag.IsGoogleConfigured = !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientId"]) &&
                                        !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientSecret"]);

            // Rule: Admin role cannot be self-selected during public registration
            if (model.Role.Equals(DbSeeder.RoleAdmin, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Role", "Administrator accounts cannot be self-registered. Please contact system admin.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if email already registered
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            // If Driver role chosen, validate license uniqueness
            if (model.Role.Equals(DbSeeder.RoleDriver, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(model.LicenseNumber))
                {
                    ModelState.AddModelError("LicenseNumber", "Driving license number is required for driver registration.");
                    return View(model);
                }

                bool licenseExists = await _context.Drivers.AnyAsync(d => d.LicenseNumber == model.LicenseNumber.Trim());
                if (licenseExists)
                {
                    ModelState.AddModelError("LicenseNumber", "This license number is already registered in the system.");
                    return View(model);
                }
            }

            var newUser = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(newUser, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Assign chosen role (Driver or User)
            var targetRole = model.Role.Equals(DbSeeder.RoleDriver, StringComparison.OrdinalIgnoreCase) 
                ? DbSeeder.RoleDriver 
                : DbSeeder.RoleUser;

            if (!await _roleManager.RoleExistsAsync(targetRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(targetRole));
            }

            await _userManager.AddToRoleAsync(newUser, targetRole);

            // If Driver, automatically provision Driver profile record
            if (targetRole == DbSeeder.RoleDriver)
            {
                var newDriver = new Driver
                {
                    UserId = newUser.Id,
                    FullName = model.FullName.Trim(),
                    Email = model.Email.Trim(),
                    PhoneNumber = model.PhoneNumber.Trim(),
                    LicenseNumber = model.LicenseNumber!.Trim(),
                    LicenseExpiryDate = DateTime.UtcNow.AddYears(3),
                    Address = string.IsNullOrWhiteSpace(model.Address) ? "Fleet Operations Center" : model.Address.Trim(),
                    JoiningDate = DateTime.UtcNow,
                    AvailabilityStatus = DriverStatus.Available,
                    Rating = 5.0m,
                    TotalReviews = 0
                };
                _context.Drivers.Add(newDriver);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("New user registered: {Email} with role {Role}", newUser.Email, targetRole);
            TempData["Success"] = $"Account created successfully! Welcome to FleetFlow Pro, {newUser.FullName}.";

            // Sign user in automatically
            await _signInManager.SignInAsync(newUser, isPersistent: false);

            return await RedirectUserByRoleAsync(newUser, returnUrl);
        }

        // ==========================================
        // POST: /Account/ExternalLogin
        // ==========================================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var isConfigured = !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientId"]) &&
                               !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientSecret"]);

            if (!isConfigured && provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Google Authentication is not yet configured. Please set 'Authentication:Google:ClientId' and 'Authentication:Google:ClientSecret' via User Secrets: dotnet user-secrets set \"Authentication:Google:ClientId\" \"YOUR_ID\".";
                return RedirectToAction("Login", new { returnUrl });
            }

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        // ==========================================
        // GET: /Account/ExternalLoginCallback
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                _logger.LogError("External provider authentication error: {Error}", remoteError);
                TempData["Error"] = $"External login provider error: {remoteError}";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["Error"] = "Could not retrieve login information from external provider.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            // Attempt to sign in if this external login is already linked
            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (signInResult.Succeeded)
            {
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                _logger.LogInformation("User {Email} logged in with {Provider}.", user?.Email, info.LoginProvider);
                TempData["Success"] = $"Signed in with {info.ProviderDisplayName ?? info.LoginProvider}!";
                return await RedirectUserByRoleAsync(user!, returnUrl);
            }

            // If account doesn't exist under this login, check if a user with matching email exists
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Email address not received from external provider.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // Link external login to existing local user account
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(existingUser, isPersistent: false);
                    TempData["Success"] = $"Your {info.ProviderDisplayName ?? info.LoginProvider} account has been linked successfully!";
                    return await RedirectUserByRoleAsync(existingUser, returnUrl);
                }
            }

            // Auto-provision new User role account from Google Claims
            var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email.Split('@')[0];
            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = name,
                CreatedAt = DateTime.UtcNow
            };

            var identityResult = await _userManager.CreateAsync(newUser);
            if (identityResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, DbSeeder.RoleUser);
                await _userManager.AddLoginAsync(newUser, info);

                await _signInManager.SignInAsync(newUser, isPersistent: false);
                TempData["Success"] = $"Welcome to FleetFlow Pro, {newUser.FullName}! Your Google account is now connected.";
                return await RedirectUserByRoleAsync(newUser, returnUrl);
            }

            TempData["Error"] = "Failed to create an account from external credentials.";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        // ==========================================
        // POST: /Account/Logout
        // ==========================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userEmail = User.Identity?.Name;
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();

            _logger.LogInformation("User {Email} logged out.", userEmail);
            TempData["Info"] = "You have been logged out safely.";

            return RedirectToAction("Login", "Account");
        }

        // ==========================================
        // GET: /Account/AccessDenied
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            ViewBag.Title = "Access Denied";
            return View();
        }

        // ==========================================
        // GET: /Account/Profile
        // Normal User / Staff Profile View
        // ==========================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User profile not found.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            // If Driver, redirect to dedicated Driver Profile
            if (roles.Contains(DbSeeder.RoleDriver))
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == user.Id);
                if (driver != null)
                {
                    return RedirectToAction("Profile", "Drivers", new { id = driver.DriverId });
                }
            }

            var model = new UserProfileViewModel
            {
                UserId = user.Id,
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? (user.UserName ?? "User") : user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureUrl = user.ProfilePictureUrl,
                RoleName = roles.FirstOrDefault() ?? "User",
                CreatedAt = user.CreatedAt,
                ReviewsCount = await _context.DriverReviews.CountAsync(r => r.ReviewerUserId == user.Id)
            };

            ViewBag.Title = "My Profile - FleetFlow Pro";
            return View(model);
        }

        // ==========================================
        // POST: /Account/Profile
        // Updates full name, phone number, and optional avatar
        // ==========================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User profile not found.");
            }

            if (!ModelState.IsValid)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.UserId = user.Id;
                model.Email = user.Email ?? string.Empty;
                model.ProfilePictureUrl = user.ProfilePictureUrl;
                model.RoleName = roles.FirstOrDefault() ?? "User";
                model.CreatedAt = user.CreatedAt;
                model.ReviewsCount = await _context.DriverReviews.CountAsync(r => r.ReviewerUserId == user.Id);
                return View(model);
            }

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = !string.IsNullOrWhiteSpace(model.PhoneNumber) ? model.PhoneNumber.Trim() : null;

            // Handle Photo Upload
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("ProfilePicture", "Only .jpg, .jpeg, .png, and .webp images are allowed.");
                    var roles = await _userManager.GetRolesAsync(user);
                    model.RoleName = roles.FirstOrDefault() ?? "User";
                    model.CreatedAt = user.CreatedAt;
                    model.ReviewsCount = await _context.DriverReviews.CountAsync(r => r.ReviewerUserId == user.Id);
                    return View(model);
                }

                if (model.ProfilePicture.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("ProfilePicture", "Profile picture must be under 2MB in file size.");
                    var roles = await _userManager.GetRolesAsync(user);
                    model.RoleName = roles.FirstOrDefault() ?? "User";
                    model.CreatedAt = user.CreatedAt;
                    model.ReviewsCount = await _context.DriverReviews.CountAsync(r => r.ReviewerUserId == user.Id);
                    return View(model);
                }

                var uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                var uniqueFileName = $"user_{user.Id}_{DateTime.UtcNow.Ticks}{ext}";
                var filePath = Path.Combine(uploadsDir, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(fileStream);
                }

                user.ProfilePictureUrl = $"/uploads/profiles/{uniqueFileName}";
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
            {
                TempData["Success"] = "Your profile information has been successfully updated.";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ==========================================
        // POST: /Account/RemoveProfilePicture
        // ==========================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProfilePicture()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User profile not found.");
            }

            if (!string.IsNullOrEmpty(user.ProfilePictureUrl) && user.ProfilePictureUrl.StartsWith("/uploads/profiles/"))
            {
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePictureUrl.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    try { System.IO.File.Delete(fullPath); } catch { }
                }
            }

            user.ProfilePictureUrl = null;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Profile picture removed successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // ==========================================
        // POST: /Account/ChangePassword
        // ==========================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User profile not found.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please ensure all password requirements are met.";
                return RedirectToAction(nameof(Profile));
            }

            var changeResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (changeResult.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Your password has been changed successfully.";
                return RedirectToAction(nameof(Profile));
            }

            var errorMsg = string.Join("; ", changeResult.Errors.Select(e => e.Description));
            TempData["Error"] = $"Password change failed: {errorMsg}";
            return RedirectToAction(nameof(Profile));
        }

        /// <summary>
        /// Centralized role-based routing logic ensuring users land on their authorized workspace.
        /// </summary>
        private async Task<IActionResult> RedirectUserByRoleAsync(ApplicationUser user, string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/")
            {
                return Redirect(returnUrl);
            }

            if (await _userManager.IsInRoleAsync(user, DbSeeder.RoleAdmin))
            {
                return RedirectToAction("Admin", "Dashboard");
            }

            if (await _userManager.IsInRoleAsync(user, DbSeeder.RoleDriver))
            {
                return RedirectToAction("Driver", "Dashboard");
            }

            return RedirectToAction("User", "Dashboard");
        }
    }
}
