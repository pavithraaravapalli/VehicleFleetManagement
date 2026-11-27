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

            // Assign normal User role (Drivers are registered and assigned by Fleet Administrator)
            var targetRole = DbSeeder.RoleUser;

            if (!await _roleManager.RoleExistsAsync(targetRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(targetRole));
            }

            await _userManager.AddToRoleAsync(newUser, targetRole);

            _logger.LogInformation("New user registered: {Email} with role {Role}", newUser.Email, targetRole);
            TempData["Success"] = $"Account created successfully! Welcome to FleetFlow Pro, {newUser.FullName}.";

            // Sign user in automatically
            await _signInManager.SignInAsync(newUser, isPersistent: false);

            return await RedirectUserByRoleAsync(newUser, returnUrl);
        }

        // ==========================================
        // GET: /Account/ForgotPassword
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            ViewBag.Title = "Forgot Password - FleetFlow Pro";
            return View();
        }

        // ==========================================
        // POST: /Account/ForgotPassword
        // ==========================================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            ViewBag.Title = "Forgot Password - FleetFlow Pro";
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ViewBag.ResetSent = true;
                return View();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account", new { token, email = model.Email }, Request.Scheme);

            ViewBag.ResetLink = resetLink;
            ViewBag.ResetSent = true;
            return View();
        }

        // ==========================================
        // GET: /Account/ResetPassword
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? token, string? email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(Login));
            }

            ViewBag.Title = "Reset Password - FleetFlow Pro";
            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        // ==========================================
        // POST: /Account/ResetPassword
        // ==========================================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            ViewBag.Title = "Reset Password - FleetFlow Pro";
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Success"] = "Your password has been reset successfully. You may now sign in.";
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["Success"] = "Your password has been reset successfully. You may now sign in.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
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
