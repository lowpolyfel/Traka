using System.Collections.Generic;

namespace Trackii.Models.Admin.User
{
    public class UserListVm
    {
        public List<UserRowVm> Rows { get; set; } = new();

        public string? Search { get; set; }
        public bool ShowInactive { get; set; }

        public int Page { get; set; }
        public int TotalPages { get; set; }
    }
}
