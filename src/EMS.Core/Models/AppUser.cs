using Microsoft.AspNetCore.Identity;

namespace EMS.Core.Models;

public class AppUser : IdentityUser
{
    public string FullName { get; set; }
    public string Department { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
}
