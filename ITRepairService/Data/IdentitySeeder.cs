using System;
using System.Threading.Tasks;
using ITRepairService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ITRepairService.Data;

public static class IdentitySeeder
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = configuration["SeedAdmin:Email"];
        var adminPassword = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
        {
            await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
        }
    }

    public static async Task SeedTestUsersAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Test User 1: Regular user in FI department (non-SM/DM, non-DX)
        // Expected roles: User only
        var regularFIUser = await userManager.FindByNameAsync("test_user_fi");
        if (regularFIUser is null)
        {
            regularFIUser = new ApplicationUser
            {
                UserName = "test_user_fi",
                Email = "test_user_fi@example.com",
                FullName = "Test User FI",
                Department = "FI",
                Title = "",
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(regularFIUser, "Test@1234");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(regularFIUser, AppRoles.User);
            }
        }

        // Test User 2: SM (Section Manager) in FI department
        // Expected roles: User, Approve (SM/DM but not DX)
        var smFIUser = await userManager.FindByNameAsync("test_sm_fi");
        if (smFIUser is null)
        {
            smFIUser = new ApplicationUser
            {
                UserName = "test_sm_fi",
                Email = "test_sm_fi@example.com",
                FullName = "Test SM FI",
                Department = "FI",
                Title = "SM",
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(smFIUser, "Test@1234");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(smFIUser, AppRoles.User);
                await userManager.AddToRoleAsync(smFIUser, AppRoles.Approve);
            }
        }

        // Test User 3: Regular user in DX department (non-SM/DM)
        // Expected roles: User, ITSupport, Approve
        var regularDXUser = await userManager.FindByNameAsync("test_user_dx");
        if (regularDXUser is null)
        {
            regularDXUser = new ApplicationUser
            {
                UserName = "test_user_dx",
                Email = "test_user_dx@example.com",
                FullName = "Test User DX",
                Department = "DX",
                Title = "",
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(regularDXUser, "Test@1234");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(regularDXUser, AppRoles.User);
                await userManager.AddToRoleAsync(regularDXUser, AppRoles.ITSupport);
                await userManager.AddToRoleAsync(regularDXUser, AppRoles.Approve);
            }
        }

        // Test User 4: SM (Section Manager) in DX department
        // Expected roles: User, Approve, ITSupport, Admin
        var smDXUser = await userManager.FindByNameAsync("test_sm_dx");
        if (smDXUser is null)
        {
            smDXUser = new ApplicationUser
            {
                UserName = "test_sm_dx",
                Email = "test_sm_dx@example.com",
                FullName = "Test SM DX",
                Department = "DX",
                Title = "SM",
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(smDXUser, "Test@1234");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(smDXUser, AppRoles.User);
                await userManager.AddToRoleAsync(smDXUser, AppRoles.Approve);
                await userManager.AddToRoleAsync(smDXUser, AppRoles.ITSupport);
                await userManager.AddToRoleAsync(smDXUser, AppRoles.Admin);
            }
        }

        // Test User 5: Regular user in HR department (non-SM/DM, non-DX)
        // Expected roles: User only
        var regularHRUser = await userManager.FindByNameAsync("test_user_hr");
        if (regularHRUser is null)
        {
            regularHRUser = new ApplicationUser
            {
                UserName = "test_user_hr",
                Email = "test_user_hr@example.com",
                FullName = "Test User HR",
                Department = "HR",
                Title = "",
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(regularHRUser, "Test@1234");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(regularHRUser, AppRoles.User);
            }
        }

        // Test User 6: SM/DM in HR department with Approve role
        // Expected roles: User, Approve
        var smHRUser = await userManager.FindByNameAsync("test_sm_hr");
        if (smHRUser is null)
        {
            smHRUser = new ApplicationUser
            {
                UserName = "test_sm_hr",
                Email = "test_sm_hr@example.com",
                FullName = "Test SM HR",
                Department = "HR",
                Title = "SM",
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(smHRUser, "Test@1234");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(smHRUser, AppRoles.User);
                await userManager.AddToRoleAsync(smHRUser, AppRoles.Approve);
            }
        }
    }
}
