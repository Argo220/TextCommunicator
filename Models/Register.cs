using System.ComponentModel.DataAnnotations;

namespace TextCommunicator.Models;

public class RegisterModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = default!;
}