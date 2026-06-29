using Microsoft.AspNetCore.Identity;
using EMS.Core.Models;

namespace EMS.Web.Services;

public class RoleSeederService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<RoleSeederService> _logger;

    public RoleSeederService(
        RoleManager<IdentityRole> roleManager,
        UserManager<AppUser> userManager,
        ILogger<RoleSeederService> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task InitializeRolesAsync()
    {
        var roles = new[] { "Admin", "Operator", "Viewer" };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
                _logger.LogInformation("Created role: {Role}", role);
            }
        }

        var adminEmail = "admin@energymonitoring.local";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                Department = "IT",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation("Default admin user created successfully");
            }
            else
            {
                _logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
