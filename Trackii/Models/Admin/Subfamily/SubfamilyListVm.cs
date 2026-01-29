namespace Trackii.Models.Admin.Subfamily
{
    public class SubfamilyListVm
    {
        public string? Search { get; set; }
        public bool ShowInactive { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }

        public List<Row> Rows { get; set; } = new();

        public class Row
        {
            public uint Id { get; set; }

            public string Name { get; set; } = string.Empty;

            // 🔹 ESTAS DOS PROPIEDADES FALTABAN
            public string AreaName { get; set; } = string.Empty;
            public string FamilyName { get; set; } = string.Empty;

            public bool Active { get; set; }
        }
    }
}
