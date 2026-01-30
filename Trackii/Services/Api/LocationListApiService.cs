using MySql.Data.MySqlClient;

namespace Trackii.Services.Api;

public class LocationListApiService
{
    private readonly string _cs;

    public LocationListApiService(IConfiguration cfg)
    {
        _cs = cfg.GetConnectionString("TrackiiDb")!;
    }

    public List<LocationDto> GetAll()
    {
        var list = new List<LocationDto>();

        using var cn = new MySqlConnection(_cs);
        cn.Open();

        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, active
            FROM location
            ORDER BY name
        """;

        using var rd = cmd.ExecuteReader();
        while (rd.Read())
        {
            list.Add(new LocationDto
            {
                Id = rd.GetUInt32("id"),
                Name = rd.GetString("name"),
                Active = rd.GetBoolean("active")
            });
        }

        return list;
    }

    public class LocationDto
    {
        public uint Id { get; set; }
        public string Name { get; set; } = "";
        public bool Active { get; set; }
    }
}
