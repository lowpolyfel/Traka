using System.ComponentModel.DataAnnotations;

namespace Trackii.Models;

public class RegisterVm
{
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = "";

    [Required]
    [StringLength(100)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required]
    [Compare(nameof(Password))]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}
