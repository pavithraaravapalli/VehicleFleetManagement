using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.Data
{
    /// <summary>
    /// Seeds essential Identity roles, primary system accounts (Admin, Driver, User),
    /// and ensures every registered fleet driver has an active login credential.
    /// Does NOT insert demo vehicles, trips, or fake business logs.
    /// </summary>
    public static class DbSeeder
    {
        public const string RoleAdmin = "Admin";
        public const string RoleDriver = "Driver";
        public const string RoleUser = "User";

        public static async Task SeedRolesAndUsersAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

            // 1. Ensure Standard Roles exist
            string[] roles = { RoleAdmin, RoleDriver, RoleUser };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Created Identity Role: {Role}", role);
                }
            }

            // 2. Ensure Primary Administrator Account exists
            await EnsureUserAccountAsync(userManager, logger, "admin@fleet.com", "Fleet Administrator", "Admin@123", RoleAdmin);

            // 3. Ensure Standard Corporate Staff User Account exists
            await EnsureUserAccountAsync(userManager, logger, "user@fleet.com", "Corporate Staff Member", "User@123", RoleUser);

            // 4. Ensure Standard Fleet Driver Account exists
            var standardDriverUser = await EnsureUserAccountAsync(userManager, logger, "driver@fleet.com", "Fleet Driver", "Driver@123", RoleDriver);

            // 5. Auto-provision & link login accounts for all registered Drivers
            var drivers = await dbContext.Drivers.ToListAsync();
            foreach (var driver in drivers)
            {
                if (string.IsNullOrWhiteSpace(driver.Email)) continue;

                var existingUser = await userManager.FindByEmailAsync(driver.Email);
                if (existingUser == null)
                {
                    existingUser = new ApplicationUser
                    {
                        UserName = driver.Email,
                        Email = driver.Email,
                        FullName = driver.FullName,
                        PhoneNumber = driver.PhoneNumber,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createResult = await userManager.CreateAsync(existingUser, "Driver@123");
                    if (createResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(existingUser, RoleDriver);
                        logger.LogInformation("Provisioned Driver login account for {Email}", driver.Email);
                    }
                    else
                    {
                        logger.LogError("Failed to provision user for driver {Email}: {Errors}", 
                            driver.Email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    if (!await userManager.IsInRoleAsync(existingUser, RoleDriver))
                    {
                        await userManager.AddToRoleAsync(existingUser, RoleDriver);
                    }
                }

                // Link driver foreign key to user
                if (existingUser != null && driver.UserId != existingUser.Id)
                {
                    driver.UserId = existingUser.Id;
                }
            }

            // If standard driver account exists but no driver was linked, link to first driver if available
            if (standardDriverUser != null && drivers.Any() && !drivers.Any(d => d.UserId == standardDriverUser.Id))
            {
                var unlinked = drivers.FirstOrDefault(d => d.UserId == null);
                if (unlinked != null)
                {
                    unlinked.UserId = standardDriverUser.Id;
                }
            }

            await dbContext.SaveChangesAsync();
        }

        private static async Task<ApplicationUser?> EnsureUserAccountAsync(
            UserManager<ApplicationUser> userManager,
            ILogger logger,
            string email,
            string fullName,
            string password,
            string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                    logger.LogInformation("Created {Role} account: {Email}", role, email);
                }
                else
                {
                    logger.LogError("Failed to create {Role} account {Email}: {Errors}",
                        role, email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    return null;
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(user, role))
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }

            return user;
        }
    }
}
