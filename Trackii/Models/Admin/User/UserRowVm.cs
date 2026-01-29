namespace Trackii.Models.Admin.User
{
    public class UserRowVm
    {
        public uint Id { get; set; }
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
        public bool Active { get; set; }
    }
}
