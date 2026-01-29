using System.ComponentModel.DataAnnotations;

namespace Trackii.Models.Admin.User
{
    public class UserCreateVm
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public uint RoleId { get; set; }

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
