using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.Data
{
    /// <summary>
    /// Seeds default Identity roles, initial administrative/driver accounts,
    /// and baseline fleet records for development and university testing.
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
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

            logger.LogInformation("Checking database migrations and seeding initial fleet data...");

            // 1. Seed Roles
            string[] roles = { RoleAdmin, RoleDriver, RoleUser };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Created Identity Role: {Role}", role);
                }
            }

            // 2. Seed Default Admin
            string adminEmail = "admin@fleet.com";
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Fleet Administrator",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, RoleAdmin);
                    logger.LogInformation("Created Default Admin account: {Email}", adminEmail);
                }
                else
                {
                    logger.LogError("Failed to create Admin: {Errors}", string.Join(", ", result.Errors));
                }
            }

            // 3. Seed Demo Driver User & Linked Driver Profile
            string driverEmail = "driver@fleet.com";
            var existingDriverUser = await userManager.FindByEmailAsync(driverEmail);
            ApplicationUser? driverUser = existingDriverUser;

            if (driverUser == null)
            {
                driverUser = new ApplicationUser
                {
                    UserName = driverEmail,
                    Email = driverEmail,
                    FullName = "Robert Miller",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(driverUser, "Driver@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(driverUser, RoleDriver);
                    logger.LogInformation("Created Demo Driver user account: {Email}", driverEmail);
                }
            }

            if (driverUser != null)
            {
                var existingDriverRecord = await context.Drivers.FirstOrDefaultAsync(d => d.UserId == driverUser.Id);
                if (existingDriverRecord == null)
                {
                    var driverProfile = new Driver
                    {
                        UserId = driverUser.Id,
                        FullName = "Robert Miller",
                        Email = driverEmail,
                        PhoneNumber = "+1-555-0199",
                        LicenseNumber = "DL-98765-NY",
                        LicenseExpiryDate = DateTime.UtcNow.AddYears(3),
                        Address = "742 Evergreen Terrace, Springfield",
                        JoiningDate = DateTime.UtcNow.AddMonths(-12),
                        AvailabilityStatus = DriverStatus.Available
                    };
                    context.Drivers.Add(driverProfile);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Created Driver profile linked to UserId: {UserId}", driverUser.Id);
                }
            }

            // 4. Seed Permitted Standard User
            string regularEmail = "user@fleet.com";
            var existingStandardUser = await userManager.FindByEmailAsync(regularEmail);
            if (existingStandardUser == null)
            {
                var standardUser = new ApplicationUser
                {
                    UserName = regularEmail,
                    Email = regularEmail,
                    FullName = "Staff Fleet Viewer",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(standardUser, "User@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(standardUser, RoleUser);
                    logger.LogInformation("Created Standard User account: {Email}", regularEmail);
                }
            }

            // 5. Seed Baseline Vehicles if empty
            if (!await context.Vehicles.AnyAsync())
            {
                var vehicles = new[]
                {
                    new Vehicle
                    {
                        RegistrationNumber = "FL-01-AB-1001",
                        VehicleName = "Toyota Hilux 4x4",
                        Model = "Hilux 2.8L Turbo",
                        VehicleType = "Truck",
                        FuelType = "Diesel",
                        ManufacturingYear = 2022,
                        SeatingCapacity = 5,
                        CurrentOdometer = 24500.00m,
                        RegistrationDate = DateTime.UtcNow.AddYears(-2),
                        InsuranceExpiryDate = DateTime.UtcNow.AddMonths(10),
                        Status = VehicleStatus.Available,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Vehicle
                    {
                        RegistrationNumber = "FL-02-CD-2002",
                        VehicleName = "Ford Transit High Roof",
                        Model = "Transit 350 Extended",
                        VehicleType = "Van",
                        FuelType = "Diesel",
                        ManufacturingYear = 2023,
                        SeatingCapacity = 12,
                        CurrentOdometer = 18200.00m,
                        RegistrationDate = DateTime.UtcNow.AddYears(-1),
                        InsuranceExpiryDate = DateTime.UtcNow.AddMonths(6),
                        Status = VehicleStatus.Available,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Vehicle
                    {
                        RegistrationNumber = "FL-03-EF-3003",
                        VehicleName = "Honda Accord Hybrid",
                        Model = "Accord EX-L",
                        VehicleType = "Sedan",
                        FuelType = "Hybrid",
                        ManufacturingYear = 2024,
                        SeatingCapacity = 5,
                        CurrentOdometer = 9500.00m,
                        RegistrationDate = DateTime.UtcNow.AddMonths(-6),
                        InsuranceExpiryDate = DateTime.UtcNow.AddYears(1),
                        Status = VehicleStatus.Available,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Vehicles.AddRange(vehicles);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded 3 baseline operational vehicles.");
            }

            // 6. Seed Sample Maintenance if empty
            if (!await context.Maintenances.AnyAsync())
            {
                var hilux = await context.Vehicles.FirstOrDefaultAsync(v => v.RegistrationNumber == "FL-01-AB-1001");
                var transit = await context.Vehicles.FirstOrDefaultAsync(v => v.RegistrationNumber == "FL-02-CD-2002");
                var accord = await context.Vehicles.FirstOrDefaultAsync(v => v.RegistrationNumber == "FL-03-EF-3003");

                if (hilux != null && transit != null && accord != null)
                {
                    var maintenances = new[]
                    {
                        new Maintenance
                        {
                            VehicleId = hilux.VehicleId,
                            ServiceType = "Scheduled Service",
                            Description = "Standard 20,000 km engine oil, fuel filter and lubrication service.",
                            Cost = 245.50m,
                            ServiceDate = DateTime.UtcNow.AddMonths(-1),
                            NextServiceDate = DateTime.UtcNow.AddMonths(5),
                            Status = MaintenanceStatus.Completed,
                            ServiceCenter = "Toyota Certified Service Center"
                        },
                        new Maintenance
                        {
                            VehicleId = transit.VehicleId,
                            ServiceType = "Brake Inspection",
                            Description = "Front brake pad wear check, disc skimming, and brake fluid flushing.",
                            Cost = 310.00m,
                            ServiceDate = DateTime.UtcNow.AddDays(4), // Due within 7 days -> triggers active reminder!
                            NextServiceDate = DateTime.UtcNow.AddMonths(6),
                            Status = MaintenanceStatus.Scheduled,
                            ServiceCenter = "Apex Fleet Mechanics Ltd."
                        },
                        new Maintenance
                        {
                            VehicleId = accord.VehicleId,
                            ServiceType = "Tire Replacement",
                            Description = "Rotate 4 tires and perform laser wheel balancing & alignment.",
                            Cost = 180.00m,
                            ServiceDate = DateTime.UtcNow.AddDays(-3), // Past due -> triggers overdue notice!
                            NextServiceDate = DateTime.UtcNow.AddMonths(4),
                            Status = MaintenanceStatus.Scheduled,
                            ServiceCenter = "Speedy Wheels Care"
                        }
                    };

                    context.Maintenances.AddRange(maintenances);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded 3 baseline maintenance records (including upcoming & overdue alerts).");
                }
            }

            // 6b. Self-healing maintenance schedule refresh for upcoming & overdue alerts
            var todayUtc = DateTime.UtcNow.Date;
            var thresholdUtc = todayUtc.AddDays(7);
            bool hasUpcomingAlert = await context.Maintenances.AnyAsync(m => 
                (m.Status != MaintenanceStatus.Completed && ((m.ServiceDate >= todayUtc && m.ServiceDate <= thresholdUtc) || (m.NextServiceDate >= todayUtc && m.NextServiceDate <= thresholdUtc)))
                || (m.Status == MaintenanceStatus.Completed && m.NextServiceDate >= todayUtc && m.NextServiceDate <= thresholdUtc));

            if (!hasUpcomingAlert)
            {
                var schedMaint = await context.Maintenances.FirstOrDefaultAsync(m => m.Status == MaintenanceStatus.Scheduled);
                if (schedMaint != null)
                {
                    schedMaint.ServiceDate = todayUtc.AddDays(4);
                    schedMaint.NextServiceDate = todayUtc.AddMonths(4);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Refreshed maintenance service date to +4 days to ensure active upcoming 7-day alert.");
                }
            }

            // 7. Seed Sample Fuel Logs if empty
            if (!await context.FuelLogs.AnyAsync())
            {
                var hilux = await context.Vehicles.FirstOrDefaultAsync(v => v.RegistrationNumber == "FL-01-AB-1001");
                var transit = await context.Vehicles.FirstOrDefaultAsync(v => v.RegistrationNumber == "FL-02-CD-2002");

                if (hilux != null && transit != null)
                {
                    var fuelLogs = new[]
                    {
                        new FuelLog
                        {
                            VehicleId = hilux.VehicleId,
                            FuelQuantity = 52.50m,
                            PricePerLitre = 1.62m,
                            FuelDate = DateTime.UtcNow.AddDays(-20),
                            FuelStation = "Shell Expressway Gateway",
                            OdometerReading = 23750.00m
                        },
                        new FuelLog
                        {
                            VehicleId = hilux.VehicleId,
                            FuelQuantity = 54.00m,
                            PricePerLitre = 1.65m,
                            FuelDate = DateTime.UtcNow.AddDays(-4),
                            FuelStation = "TotalEnergies Logistics Hub",
                            OdometerReading = 24500.00m // 750 km / 54 L = ~13.89 km/L mileage!
                        },
                        new FuelLog
                        {
                            VehicleId = transit.VehicleId,
                            FuelQuantity = 68.00m,
                            PricePerLitre = 1.68m,
                            FuelDate = DateTime.UtcNow.AddDays(-25),
                            FuelStation = "BP Fleet Fuel Center",
                            OdometerReading = 17450.00m
                        },
                        new FuelLog
                        {
                            VehicleId = transit.VehicleId,
                            FuelQuantity = 71.50m,
                            PricePerLitre = 1.70m,
                            FuelDate = DateTime.UtcNow.AddDays(-6),
                            FuelStation = "Chevron Commercial Hub",
                            OdometerReading = 18200.00m // 750 km / 71.5 L = ~10.49 km/L mileage!
                        }
                    };

                    context.FuelLogs.AddRange(fuelLogs);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded 4 baseline fuel purchase logs with dynamic mileage continuity.");
                }
            }

            // 7b. Self-healing check for any fuel logs with zero TotalCost
            var zeroCostLogs = await context.FuelLogs.Where(f => f.TotalCost == 0 && f.FuelQuantity > 0).ToListAsync();
            if (zeroCostLogs.Any())
            {
                foreach (var z in zeroCostLogs)
                {
                    z.TotalCost = Math.Round(z.FuelQuantity * z.PricePerLitre, 2);
                }
                await context.SaveChangesAsync();
                logger.LogInformation("Recalculated {Count} fuel logs with computed TotalCost.", zeroCostLogs.Count);
            }

            // 8. Seed Sample Trips if empty
            if (!await context.Trips.AnyAsync())
            {
                var hilux = await context.Vehicles.FirstOrDefaultAsync(v => v.RegistrationNumber == "FL-01-AB-1001");
                var transit = await context.Vehicles.FirstOrDefaultAsync(v => v.RegistrationNumber == "FL-02-CD-2002");
                var driver = await context.Drivers.FirstOrDefaultAsync();

                if (hilux != null && transit != null && driver != null)
                {
                    var sampleTrips = new[]
                    {
                        new Trip
                        {
                            VehicleId = hilux.VehicleId,
                            DriverId = driver.DriverId,
                            Source = "New York Fleet Terminal",
                            Destination = "Boston Distribution Depot",
                            StartDate = DateTime.UtcNow.AddDays(-10),
                            ExpectedEndDate = DateTime.UtcNow.AddDays(-9),
                            ActualEndDate = DateTime.UtcNow.AddDays(-9),
                            StartOdometer = 23800.00m,
                            EndOdometer = 24150.00m,
                            Distance = 350.00m,
                            Purpose = "Scheduled inter-city logistics dispatch",
                            TripStatus = TripStatusEnum.Completed,
                            CreatedAt = DateTime.UtcNow.AddDays(-12)
                        },
                        new Trip
                        {
                            VehicleId = transit.VehicleId,
                            DriverId = driver.DriverId,
                            Source = "Manhattan Logistics Hub",
                            Destination = "Philadelphia Cargo Center",
                            StartDate = DateTime.UtcNow.AddDays(1),
                            ExpectedEndDate = DateTime.UtcNow.AddDays(1).AddHours(6),
                            StartOdometer = 18200.00m,
                            Distance = 0m,
                            Purpose = "Scheduled passenger transit service",
                            TripStatus = TripStatusEnum.Scheduled,
                            CreatedAt = DateTime.UtcNow.AddDays(-1)
                        }
                    };

                    context.Trips.AddRange(sampleTrips);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded 2 baseline operational trips (Completed and Scheduled).");
                }
            }

            // 9. Seed Sample Driver Review if empty
            if (!await context.DriverReviews.AnyAsync())
            {
                var completedTrip = await context.Trips.Include(t => t.Driver).FirstOrDefaultAsync(t => t.TripStatus == TripStatusEnum.Completed);
                var standardUserAccount = await userManager.FindByEmailAsync("user@fleet.com");

                if (completedTrip != null && completedTrip.Driver != null && standardUserAccount != null)
                {
                    var sampleReview = new DriverReview
                    {
                        DriverId = completedTrip.DriverId,
                        TripId = completedTrip.TripId,
                        ReviewerUserId = standardUserAccount.Id,
                        Rating = 5,
                        Comment = "Exceptional driving professionalism! Punctual departure, smooth handling through adverse weather, and pristine vehicle handoff.",
                        ReviewDate = DateTime.UtcNow.AddDays(-8),
                        IsModerated = false
                    };

                    context.DriverReviews.Add(sampleReview);
                    completedTrip.Driver.Rating = 5.0m;
                    completedTrip.Driver.TotalReviews = 1;
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded baseline driver review and updated driver rating.");
                }
            }

            // 10. Seed Sample Vehicle Defect Report if empty
            if (!await context.VehicleIssueReports.AnyAsync())
            {
                var hilux = await context.Vehicles.FirstOrDefaultAsync(v => v.RegistrationNumber == "FL-01-AB-1001");
                var driver = await context.Drivers.FirstOrDefaultAsync();

                if (hilux != null && driver != null)
                {
                    var sampleIssue = new VehicleIssueReport
                    {
                        VehicleId = hilux.VehicleId,
                        DriverId = driver.DriverId,
                        Title = "Slight steering vibration at highway speed (90+ km/h)",
                        Description = "Noticeable slight wheel vibration when driving above 90 km/h on highway stretches. Recommended front wheel alignment and tyre balance check during next inspection.",
                        Category = IssueCategory.Tyres,
                        Severity = IssueSeverity.Medium,
                        Status = IssueStatus.Open,
                        ReportDate = DateTime.UtcNow.AddDays(-1)
                    };

                    context.VehicleIssueReports.Add(sampleIssue);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded baseline vehicle fault report ticket.");
                }
            }

            // 11. Seed Baseline Notifications if empty
            if (!await context.Notifications.AnyAsync())
            {
                var adminUserAccount = await userManager.FindByEmailAsync("admin@fleet.com");
                var driverUserAccount = await userManager.FindByEmailAsync("driver@fleet.com");

                if (adminUserAccount != null)
                {
                    context.Notifications.Add(new Notification
                    {
                        UserId = adminUserAccount.Id,
                        Title = "Vehicle Defect Reported",
                        Message = "Driver Robert Miller reported tyre vibration on Toyota Hilux (FL-01-AB-1001).",
                        Type = NotificationType.VehicleIssueReported,
                        ActionUrl = "/Issues",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    });
                }

                if (driverUserAccount != null)
                {
                    context.Notifications.Add(new Notification
                    {
                        UserId = driverUserAccount.Id,
                        Title = "Welcome to FleetFlow Pro",
                        Message = "Your commercial driver portal is active. You can manage your availability, review trips, and report vehicle issues.",
                        Type = NotificationType.General,
                        ActionUrl = "/Dashboard/Driver",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    });
                }

                await context.SaveChangesAsync();
                logger.LogInformation("Seeded baseline notifications.");
            }
        }
    }
}
