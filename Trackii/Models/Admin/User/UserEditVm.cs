using System.ComponentModel.DataAnnotations;

namespace Trackii.Models.Admin.User
{
    public class UserEditVm
    {
        [Required]
        public uint Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public uint RoleId { get; set; }

        public bool Active { get; set; }

        // Opcional. Si viene vacío, no se cambia.
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }
    }
}
