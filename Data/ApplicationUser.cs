using Microsoft.AspNetCore.Identity;

namespace TextCommunicator.Data;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    // PhoneNumber already exists in IdentityUser
    public string? ProfileImagePath { get; set; }

}
