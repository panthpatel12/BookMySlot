using BookMySlot.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookMySlot.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // Ensure database schema is created
        await context.Database.MigrateAsync();

        // 1. Roles
        string[] roles = { "Admin", "Provider", "Customer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Configurable default password for development
        var defaultPassword = configuration["SeedData:DefaultPassword"] ?? "BookMySlot@2026";

        // 2. Administrators
        var adminEmails = new[] { "admin@bookmyslot.com", "admin@bookmyslot.local" };
        foreach (var adminEmail in adminEmails)
        {
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Administrator"
                };
                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }

        // 3. Services
        if (!await context.Services.AnyAsync())
        {
            var services = new List<Service>
            {
                new()
                {
                    Name = "General Consultation",
                    Description = "Comprehensive one-on-one session to discuss your requirements, project roadmap, and professional objectives.",
                    DurationMinutes = 30,
                    Price = 500.00m,
                    IsActive = true
                },
                new()
                {
                    Name = "Technical Architecture Review",
                    Description = "Deep-dive architectural review of software systems, database schemas, and microservice scalability strategies.",
                    DurationMinutes = 60,
                    Price = 1500.00m,
                    IsActive = true
                },
                new()
                {
                    Name = "Career & Resume Counselling",
                    Description = "Personalized career strategy, portfolio critique, tech mock interviews, and high-impact resume refinement.",
                    DurationMinutes = 45,
                    Price = 800.00m,
                    IsActive = true
                },
                new()
                {
                    Name = "Code & System Debugging",
                    Description = "Live pair-programming consultation to identify elusive bugs, optimize SQL queries, and resolve performance bottlenecks.",
                    DurationMinutes = 60,
                    Price = 1200.00m,
                    IsActive = true
                }
            };

            await context.Services.AddRangeAsync(services);
            await context.SaveChangesAsync();
        }

        var dbServices = await context.Services.ToListAsync();
        var sConsultation = dbServices.First(s => s.Name == "General Consultation");
        var sArchitecture = dbServices.First(s => s.Name == "Technical Architecture Review");
        var sCareer = dbServices.First(s => s.Name == "Career & Resume Counselling");
        var sDebugging = dbServices.First(s => s.Name == "Code & System Debugging");

        // 4. Providers
        var providerData = new[]
        {
            (
                Email: "provider@bookmyslot.com",
                Name: "Dr. Rajesh Sharma",
                Bio: "Senior Consultant with 15+ years experience in career counselling, educational strategy, and tech leadership.",
                Services: new[] { sConsultation, sCareer }
            ),
            (
                Email: "provider.rajesh@bookmyslot.local",
                Name: "Dr. Rajesh Sharma",
                Bio: "Senior Consultant with 15+ years experience in career counselling, educational strategy, and tech leadership.",
                Services: new[] { sConsultation, sCareer }
            ),
            (
                Email: "provider.priya@bookmyslot.local",
                Name: "Priya Patel",
                Bio: "Principal Cloud & Enterprise Architect specializing in distributed systems, clean architecture, and cloud migrations.",
                Services: new[] { sConsultation, sArchitecture, sDebugging }
            ),
            (
                Email: "provider.amit@bookmyslot.local",
                Name: "Amit Verma",
                Bio: "Senior Staff Engineer with extensive background in backend optimization, database scaling, and live diagnostics.",
                Services: new[] { sArchitecture, sDebugging }
            )
        };

        var providerProfiles = new List<ProviderProfile>();

        foreach (var p in providerData)
        {
            var user = await userManager.FindByEmailAsync(p.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = p.Email,
                    Email = p.Email,
                    EmailConfirmed = true,
                    FullName = p.Name
                };
                var createResult = await userManager.CreateAsync(user, defaultPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Provider");
                }
            }

            var profile = await context.ProviderProfiles
                .Include(prof => prof.ProviderServices)
                .FirstOrDefaultAsync(prof => prof.ApplicationUserId == user.Id);

            if (profile == null)
            {
                profile = new ProviderProfile
                {
                    ApplicationUserId = user.Id,
                    Bio = p.Bio,
                    IsActive = true
                };
                context.ProviderProfiles.Add(profile);
                await context.SaveChangesAsync();

                foreach (var s in p.Services)
                {
                    context.ProviderServices.Add(new ProviderService
                    {
                        ProviderProfileId = profile.Id,
                        ServiceId = s.Id
                    });
                }
                await context.SaveChangesAsync();
            }

            providerProfiles.Add(profile);
        }

        // 5. Customers
        var customerData = new[]
        {
            ("customer@bookmyslot.com", "Arun Kumar"),
            ("customer.arun@bookmyslot.local", "Arun Kumar"),
            ("customer.neha@bookmyslot.local", "Neha Gupta"),
            ("customer.vikram@bookmyslot.local", "Vikram Singh"),
            ("customer.pooja@bookmyslot.local", "Pooja Desai"),
            ("customer.rohit@bookmyslot.local", "Rohit Mehta")
        };

        var customers = new List<ApplicationUser>();
        foreach (var c in customerData)
        {
            var user = await userManager.FindByEmailAsync(c.Item1);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = c.Item1,
                    Email = c.Item1,
                    EmailConfirmed = true,
                    FullName = c.Item2
                };
                var result = await userManager.CreateAsync(user, defaultPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Customer");
                }
            }
            customers.Add(user);
        }

        // 6. Slots and Sample Appointments (Idempotent seed relative to today)
        if (!await context.AppointmentSlots.AnyAsync())
        {
            var now = DateTime.UtcNow;
            var today = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

            var createdSlots = new List<AppointmentSlot>();

            // Generate slots for each provider over the next 7 days
            for (int dayOffset = 1; dayOffset <= 5; dayOffset++)
            {
                var slotDate = today.AddDays(dayOffset);

                // Skip weekends (Saturday = 6, Sunday = 0)
                if (slotDate.DayOfWeek == DayOfWeek.Saturday || slotDate.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                foreach (var profile in providerProfiles)
                {
                    // Fetch assigned services for this provider
                    var assignedServices = await context.ProviderServices
                        .Where(ps => ps.ProviderProfileId == profile.Id)
                        .Select(ps => ps.Service)
                        .ToListAsync();

                    if (!assignedServices.Any()) continue;

                    // Generate morning and afternoon slots
                    // Morning: 10:00 AM UTC (approx 3:30 PM IST)
                    var s1 = assignedServices[0];
                    var start1 = slotDate.AddHours(4).AddMinutes(30); // 10:00 AM IST is 04:30 UTC
                    var end1 = start1.AddMinutes(s1.DurationMinutes);

                    var slot1 = new AppointmentSlot
                    {
                        ProviderProfileId = profile.Id,
                        ServiceId = s1.Id,
                        StartUtc = start1,
                        EndUtc = end1,
                        IsActive = true
                    };
                    createdSlots.Add(slot1);

                    // Afternoon: 02:00 PM IST is 08:30 UTC
                    var s2 = assignedServices.Count > 1 ? assignedServices[1] : assignedServices[0];
                    var start2 = slotDate.AddHours(8).AddMinutes(30);
                    var end2 = start2.AddMinutes(s2.DurationMinutes);

                    var slot2 = new AppointmentSlot
                    {
                        ProviderProfileId = profile.Id,
                        ServiceId = s2.Id,
                        StartUtc = start2,
                        EndUtc = end2,
                        IsActive = true
                    };
                    createdSlots.Add(slot2);

                    // Late Afternoon: 04:00 PM IST is 10:30 UTC
                    var s3 = assignedServices.Last();
                    var start3 = slotDate.AddHours(10).AddMinutes(30);
                    var end3 = start3.AddMinutes(s3.DurationMinutes);

                    var slot3 = new AppointmentSlot
                    {
                        ProviderProfileId = profile.Id,
                        ServiceId = s3.Id,
                        StartUtc = start3,
                        EndUtc = end3,
                        IsActive = true
                    };
                    createdSlots.Add(slot3);
                }
            }

            // Also create a few past slots (yesterday & 2 days ago) to demonstrate Completed and NoShow appointments
            var pastDate1 = today.AddDays(-2).AddHours(4).AddMinutes(30);
            var pastDate2 = today.AddDays(-1).AddHours(8).AddMinutes(30);

            var pastSlot1 = new AppointmentSlot
            {
                ProviderProfileId = providerProfiles[0].Id,
                ServiceId = sConsultation.Id,
                StartUtc = pastDate1,
                EndUtc = pastDate1.AddMinutes(sConsultation.DurationMinutes),
                IsActive = true
            };
            var pastSlot2 = new AppointmentSlot
            {
                ProviderProfileId = providerProfiles[1].Id,
                ServiceId = sArchitecture.Id,
                StartUtc = pastDate2,
                EndUtc = pastDate2.AddMinutes(sArchitecture.DurationMinutes),
                IsActive = true
            };
            var pastSlot3 = new AppointmentSlot
            {
                ProviderProfileId = providerProfiles[2].Id,
                ServiceId = sDebugging.Id,
                StartUtc = pastDate2.AddHours(2),
                EndUtc = pastDate2.AddHours(2).AddMinutes(sDebugging.DurationMinutes),
                IsActive = true
            };

            createdSlots.Add(pastSlot1);
            createdSlots.Add(pastSlot2);
            createdSlots.Add(pastSlot3);

            await context.AppointmentSlots.AddRangeAsync(createdSlots);
            await context.SaveChangesAsync();

            // 7. Seed Sample Appointments across statuses
            // Past Completed appointment
            var appCompleted = new Appointment
            {
                CustomerId = customers[0].Id,
                AppointmentSlotId = pastSlot1.Id,
                Status = AppointmentStatus.Completed,
                Notes = "Discussed initial cloud strategy requirements.",
                BookedAtUtc = pastDate1.AddDays(-2)
            };
            context.Appointments.Add(appCompleted);
            await context.SaveChangesAsync();

            // Historical reservation remains for completed appointments
            context.SlotReservations.Add(new SlotReservation
            {
                AppointmentSlotId = pastSlot1.Id,
                AppointmentId = appCompleted.Id,
                ReservedAtUtc = pastDate1.AddDays(-2)
            });

            // Past NoShow appointment
            var appNoShow = new Appointment
            {
                CustomerId = customers[1].Id,
                AppointmentSlotId = pastSlot2.Id,
                Status = AppointmentStatus.NoShow,
                Notes = "Client did not attend scheduled call.",
                BookedAtUtc = pastDate2.AddDays(-1)
            };
            context.Appointments.Add(appNoShow);
            await context.SaveChangesAsync();

            context.SlotReservations.Add(new SlotReservation
            {
                AppointmentSlotId = pastSlot2.Id,
                AppointmentId = appNoShow.Id,
                ReservedAtUtc = pastDate2.AddDays(-1)
            });

            // Future Booked appointment #1 (Arun with Rajesh)
            var futureSlot1 = createdSlots[0];
            var appBooked1 = new Appointment
            {
                CustomerId = customers[0].Id,
                AppointmentSlotId = futureSlot1.Id,
                Status = AppointmentStatus.Booked,
                Notes = "Preparing for upcoming system design interview.",
                BookedAtUtc = now
            };
            context.Appointments.Add(appBooked1);
            await context.SaveChangesAsync();

            context.SlotReservations.Add(new SlotReservation
            {
                AppointmentSlotId = futureSlot1.Id,
                AppointmentId = appBooked1.Id,
                ReservedAtUtc = now
            });

            // Future Booked appointment #2 (Neha with Priya)
            var futureSlot2 = createdSlots[4];
            var appBooked2 = new Appointment
            {
                CustomerId = customers[1].Id,
                AppointmentSlotId = futureSlot2.Id,
                Status = AppointmentStatus.Booked,
                Notes = "Reviewing PostgreSQL schema and index strategy.",
                BookedAtUtc = now
            };
            context.Appointments.Add(appBooked2);
            await context.SaveChangesAsync();

            context.SlotReservations.Add(new SlotReservation
            {
                AppointmentSlotId = futureSlot2.Id,
                AppointmentId = appBooked2.Id,
                ReservedAtUtc = now
            });

            // Past Cancelled appointment (reservation was released)
            var appCancelled = new Appointment
            {
                CustomerId = customers[2].Id,
                AppointmentSlotId = pastSlot3.Id,
                Status = AppointmentStatus.Cancelled,
                Notes = "Rescheduled to next week due to personal travel.",
                BookedAtUtc = pastDate2.AddDays(-3),
                CancelledAtUtc = pastDate2.AddDays(-1)
            };
            context.Appointments.Add(appCancelled);
            // No SlotReservation for cancelled appointments!

            await context.SaveChangesAsync();
            logger.LogInformation("Database seeded successfully with initial roles, admin, providers, customers, slots, and appointments.");
        }
    }
}
