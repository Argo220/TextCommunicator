using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TextCommunicator.Models;

public class ProfileEditViewModel
{
    public string UserId { get; set; } = default!;

    [Display(Name = "Imię")]
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [Display(Name = "Nazwisko")]
    [MaxLength(100)]
    public string? LastName { get; set; }

    [Display(Name = "Numer telefonu")]
    [Phone]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Zdjęcie")]
    public IFormFile? Photo { get; set; }

    public string? CurrentPhotoPath { get; set; }
    public string? Email { get; set; }
}
