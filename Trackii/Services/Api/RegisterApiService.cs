using Microsoft.AspNetCore.Identity;
using MySql.Data.MySqlClient;
using Trackii.Models.Api;

namespace Trackii.Services.Api;

public class RegisterApiService
{
    private readonly string _cs;

    public RegisterApiService(IConfiguration cfg)
    {
        _cs = cfg.GetConnectionString("TrackiiDb")!;
    }

    public RegisterResponseDto Register(RegisterRequestDto dto)
    {
        using var cn = new MySqlConnection(_cs);
        cn.Open();

        using var tx = cn.BeginTransaction();

        try
        {
            // 1) Validar token provisioning
            var tokenCmd = cn.CreateCommand();
            tokenCmd.Transaction = tx;
            tokenCmd.CommandText = "SELECT 1 FROM tokens WHERE code=@code";
            tokenCmd.Parameters.AddWithValue("@code", dto.Token);

            if (tokenCmd.ExecuteScalar() == null)
                throw new Exception("Token inválido");

            // 2) Resolver role Piso
            var roleCmd = cn.CreateCommand();
            roleCmd.Transaction = tx;
            roleCmd.CommandText = "SELECT id FROM role WHERE name='Piso' AND active=1";
            var roleIdObj = roleCmd.ExecuteScalar();
            if (roleIdObj == null)
                throw new Exception("No existe el rol Piso activo");

            var roleId = Convert.ToUInt32(roleIdObj);

            // 3) Username automático (Variante B)
            var username = $"PISO-{dto.DeviceUid}";

            // 4) Evitar doble registro
            var existsUserCmd = cn.CreateCommand();
            existsUserCmd.Transaction = tx;
            existsUserCmd.CommandText = "SELECT 1 FROM user WHERE username=@u LIMIT 1";
            existsUserCmd.Parameters.AddWithValue("@u", username);

            if (existsUserCmd.ExecuteScalar() != null)
                throw new Exception("Este dispositivo ya está registrado");

            // 5) Crear usuario
            var hasher = new PasswordHasher<string>();
            var hash = hasher.HashPassword(username, dto.Password);

            var userCmd = cn.CreateCommand();
            userCmd.Transaction = tx;
            userCmd.CommandText = """
                INSERT INTO user (username, password, role_id, active)
                VALUES (@u, @p, @r, 1)
            """;
            userCmd.Parameters.AddWithValue("@u", username);
            userCmd.Parameters.AddWithValue("@p", hash);
            userCmd.Parameters.AddWithValue("@r", roleId);
            userCmd.ExecuteNonQuery();

            var userId = (uint)userCmd.LastInsertedId;

            // 6) Crear / upsert device SIN localidad
            var deviceCmd = cn.CreateCommand();
            deviceCmd.Transaction = tx;
            deviceCmd.CommandText = """
                INSERT INTO devices (device_uid, name, location_id, active)
                VALUES (@uid, @name, NULL, 1)
                ON DUPLICATE KEY UPDATE
                    name = VALUES(name),
                    active = 1
            """;
            deviceCmd.Parameters.AddWithValue("@uid", dto.DeviceUid);
            deviceCmd.Parameters.AddWithValue("@name", (object?)dto.DeviceName ?? DBNull.Value);
            deviceCmd.ExecuteNonQuery();

            uint deviceId;
            var lastId = (uint)deviceCmd.LastInsertedId;

            if (lastId != 0)
            {
                deviceId = lastId;
            }
            else
            {
                var getDevId = cn.CreateCommand();
                getDevId.Transaction = tx;
                getDevId.CommandText = "SELECT id FROM devices WHERE device_uid=@uid";
                getDevId.Parameters.AddWithValue("@uid", dto.DeviceUid);

                var devIdObj = getDevId.ExecuteScalar();
                if (devIdObj == null)
                    throw new Exception("No se pudo resolver el DeviceId");

                deviceId = Convert.ToUInt32(devIdObj);
            }

            tx.Commit();

            return new RegisterResponseDto
            {
                UserId = userId,
                DeviceId = deviceId,
                Username = username,
                Jwt = "" // Login se hace después
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
