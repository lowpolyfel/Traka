using MySql.Data.MySqlClient;

namespace Trackii.Services.Api;

public class DeviceApiService
{
    private readonly string _conn;

    public DeviceApiService(IConfiguration cfg)
    {
        _conn = cfg.GetConnectionString("TrackiiDb")
            ?? throw new Exception("Connection string TrackiiDb no configurada");
    }

    public (uint DeviceId, uint LocationId, string LocationName) Bind(string deviceUid, uint locationId)
    {
        using var cn = new MySqlConnection(_conn);
        cn.Open();

        // Validar location
        using (var lc = new MySqlCommand("SELECT id, name FROM location WHERE id=@id AND active=1 LIMIT 1", cn))
        {
            lc.Parameters.AddWithValue("@id", locationId);
            using var rd = lc.ExecuteReader();
            if (!rd.Read()) throw new Exception("Location inválida o inactiva");
        }

        // Upsert device por device_uid
        using (var up = new MySqlCommand(@"
            INSERT INTO devices (device_uid, location_id, name, active)
            VALUES (@uid, @loc, NULL, 1)
            ON DUPLICATE KEY UPDATE location_id=@loc, active=1;", cn))
        {
            up.Parameters.AddWithValue("@uid", deviceUid);
            up.Parameters.AddWithValue("@loc", locationId);
            up.ExecuteNonQuery();
        }

        uint deviceId;
        using (var q = new MySqlCommand("SELECT id FROM devices WHERE device_uid=@uid LIMIT 1", cn))
        {
            q.Parameters.AddWithValue("@uid", deviceUid);
            deviceId = Convert.ToUInt32(q.ExecuteScalar());
        }

        string locName;
        using (var q2 = new MySqlCommand("SELECT name FROM location WHERE id=@id LIMIT 1", cn))
        {
            q2.Parameters.AddWithValue("@id", locationId);
            locName = Convert.ToString(q2.ExecuteScalar()) ?? "";
        }

        return (deviceId, locationId, locName);
    }

    public (uint LocationId, string LocationName) GetDeviceLocation(uint deviceId)
    {
        using var cn = new MySqlConnection(_conn);
        cn.Open();

        using var cmd = new MySqlCommand(@"
            SELECT d.location_id, l.name
            FROM devices d
            JOIN location l ON l.id = d.location_id
            WHERE d.id=@id AND d.active=1
            LIMIT 1", cn);

        cmd.Parameters.AddWithValue("@id", deviceId);
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) throw new Exception("Device inválido o inactivo");

        return (Convert.ToUInt32(rd.GetUInt32("location_id")), rd.GetString("name"));
    }
}
