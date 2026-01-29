using MySql.Data.MySqlClient;
using System.Data;
using Trackii.Models.Admin.Route;

namespace Trackii.Services.Admin;

public class RouteService
{
    private readonly string _conn;

    public RouteService(IConfiguration cfg)
    {
        _conn = cfg.GetConnectionString("TrackiiDb")!;
    }

    // Lookups
    public List<(uint Id, string Name)> GetActiveSubfamilies()
    {
        var list = new List<(uint, string)>();
        using var cn = new MySqlConnection(_conn);
        cn.Open();

        using var cmd = new MySqlCommand(@"
            SELECT sf.id, CONCAT(a.name,' / ', f.name,' / ', sf.name) AS name
            FROM subfamily sf
            JOIN family f ON f.id = sf.id_family
            JOIN area a ON a.id = f.id_area
            WHERE sf.active = 1 AND f.active = 1 AND a.active = 1
            ORDER BY a.name, f.name, sf.name
        ", cn);

        using var rd = cmd.ExecuteReader();
        while (rd.Read())
            list.Add((rd.GetUInt32("id"), rd.GetString("name")));

        return list;
    }

    public List<(uint Id, string Name)> GetActiveLocations()
    {
        var list = new List<(uint, string)>();
        using var cn = new MySqlConnection(_conn);
        cn.Open();

        using var cmd = new MySqlCommand(@"
            SELECT id, name
            FROM location
            WHERE active = 1
            ORDER BY name
        ", cn);

        using var rd = cmd.ExecuteReader();
        while (rd.Read())
            list.Add((rd.GetUInt32("id"), rd.GetString("name")));

        return list;
    }

    // Listado paginado
    public RouteListVm GetPaged(uint? subfamilyId, string? search, bool showInactive, int page, int pageSize)
    {
        var vm = new RouteListVm
        {
            SubfamilyId = subfamilyId,
            Search = search,
            ShowInactive = showInactive,
            Page = page < 1 ? 1 : page,
            PageSize = pageSize < 1 ? 10 : pageSize
        };

        using var cn = new MySqlConnection(_conn);
        cn.Open();

        var where = " WHERE 1=1 ";
        if (!showInactive) where += " AND r.active = 1 ";
        if (subfamilyId.HasValue) where += " AND r.subfamily_id = @sf ";
        if (!string.IsNullOrWhiteSpace(search)) where += " AND (r.name LIKE @q OR r.version LIKE @q) ";

        // Count
        using (var countCmd = new MySqlCommand($@"
            SELECT COUNT(*)
            FROM route r
            {where}
        ", cn))
        {
            if (subfamilyId.HasValue) countCmd.Parameters.AddWithValue("@sf", subfamilyId.Value);
            if (!string.IsNullOrWhiteSpace(search)) countCmd.Parameters.AddWithValue("@q", "%" + search.Trim() + "%");

            vm.Total = Convert.ToInt32(countCmd.ExecuteScalar());
        }

        vm.TotalPages = (int)Math.Ceiling(vm.Total / (double)vm.PageSize);
        if (vm.TotalPages <= 0) vm.TotalPages = 1;
        if (vm.Page > vm.TotalPages) vm.Page = vm.TotalPages;

        var offset = (vm.Page - 1) * vm.PageSize;

        using var cmd = new MySqlCommand($@"
            SELECT
                r.id,
                r.name,
                r.version,
                r.active,
                CONCAT(a.name,' / ', f.name,' / ', sf.name) AS subfamily,
                COALESCE(GROUP_CONCAT(l.name ORDER BY rs.step_number SEPARATOR ' -> '), '') AS steps
            FROM route r
            JOIN subfamily sf ON sf.id = r.subfamily_id
            JOIN family f ON f.id = sf.id_family
            JOIN area a ON a.id = f.id_area
            LEFT JOIN route_step rs ON rs.route_id = r.id
            LEFT JOIN location l ON l.id = rs.location_id
            {where}
            GROUP BY r.id, r.name, r.version, r.active, subfamily
            ORDER BY r.id DESC
            LIMIT @ps OFFSET @off
        ", cn);

        if (subfamilyId.HasValue) cmd.Parameters.AddWithValue("@sf", subfamilyId.Value);
        if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@q", "%" + search.Trim() + "%");
        cmd.Parameters.AddWithValue("@ps", vm.PageSize);
        cmd.Parameters.AddWithValue("@off", offset);

        using var rd = cmd.ExecuteReader();
        while (rd.Read())
        {
            vm.Items.Add(new RouteRowVm
            {
                Id = rd.GetUInt32("id"),
                Name = rd.GetString("name"),
                Version = rd.GetString("version"),
                Active = rd.GetBoolean("active"),
                Subfamily = rd.GetString("subfamily"),
                StepsSummary = rd.IsDBNull("steps") ? "" : rd.GetString("steps")
            });
        }

        return vm;
    }

    public RouteEditVm GetForCreate()
    {
        // Version se calculará al guardar (según SubfamilyId).
        return new RouteEditVm();
    }

    public RouteEditVm GetForEdit(uint id)
    {
        using var cn = new MySqlConnection(_conn);
        cn.Open();

        var vm = new RouteEditVm { Id = id };

        using (var cmd = new MySqlCommand(@"
            SELECT id, subfamily_id, name, version
            FROM route
            WHERE id = @id
        ", cn))
        {
            cmd.Parameters.AddWithValue("@id", id);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read())
                throw new InvalidOperationException("Ruta no encontrada.");

            vm.SubfamilyId = rd.GetUInt32("subfamily_id");
            vm.Name = rd.GetString("name");
            vm.Version = rd.GetString("version");
        }

        using (var stepsCmd = new MySqlCommand(@"
            SELECT rs.step_number, rs.location_id, l.name AS location_name
            FROM route_step rs
            JOIN location l ON l.id = rs.location_id
            WHERE rs.route_id = @id
            ORDER BY rs.step_number
        ", cn))
        {
            stepsCmd.Parameters.AddWithValue("@id", id);
            using var rd2 = stepsCmd.ExecuteReader();
            while (rd2.Read())
            {
                vm.Steps.Add(new RouteStepVm
                {
                    StepNumber = (int)rd2.GetUInt32("step_number"),
                    LocationId = rd2.GetUInt32("location_id"),
                    LocationName = rd2.GetString("location_name")
                });
            }
        }

        return vm;
    }

    // Activa una ruta histórica (y desactiva la actual activa) con validación de WIP
    public void Activate(uint id)
    {
        using var cn = new MySqlConnection(_conn);
        cn.Open();
        using var tx = cn.BeginTransaction();

        uint subfamilyId;
        bool targetActive;

        using (var cmd = new MySqlCommand(@"SELECT subfamily_id, active FROM route WHERE id=@id", cn, tx))
        {
            cmd.Parameters.AddWithValue("@id", id);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) throw new InvalidOperationException("Ruta no encontrada.");
            subfamilyId = rd.GetUInt32("subfamily_id");
            targetActive = rd.GetBoolean("active");
        }

        if (targetActive)
        {
            tx.Commit();
            return;
        }

        // Ruta activa actual
        uint? currentActiveId = null;
        using (var cmd = new MySqlCommand(@"
            SELECT id
            FROM route
            WHERE subfamily_id=@sf AND active=1
            LIMIT 1
        ", cn, tx))
        {
            cmd.Parameters.AddWithValue("@sf", subfamilyId);
            var obj = cmd.ExecuteScalar();
            if (obj != null) currentActiveId = Convert.ToUInt32(obj);
        }

        if (currentActiveId.HasValue)
        {
            var wipCount = CountWipInRoute(cn, tx, currentActiveId.Value);
            if (wipCount > 0)
                throw new InvalidOperationException("No se puede cambiar la ruta activa: hay WIP en proceso en la ruta activa actual.");
        }

        // Desactiva todas y activa objetivo
        using (var off = new MySqlCommand(@"UPDATE route SET active=0 WHERE subfamily_id=@sf", cn, tx))
        {
            off.Parameters.AddWithValue("@sf", subfamilyId);
            off.ExecuteNonQuery();
        }
        using (var on = new MySqlCommand(@"UPDATE route SET active=1 WHERE id=@id", cn, tx))
        {
            on.Parameters.AddWithValue("@id", id);
            on.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public void Save(RouteEditVm vm)
    {
        if (vm.SubfamilyId == 0)
            throw new InvalidOperationException("Selecciona una Subfamily.");

        vm.Name = (vm.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(vm.Name))
            throw new InvalidOperationException("El nombre es requerido.");

        // Normaliza steps: sin duplicados, orden por StepNumber (y re-secuencia)
        var steps = vm.Steps
            .Where(s => s.LocationId != 0)
            .OrderBy(s => s.StepNumber)
            .ToList();

        // Si no mandaron StepNumber (por algún motivo), lo reasignamos por orden actual:
        if (steps.Count > 0 && steps.All(s => s.StepNumber == 0))
        {
            for (int i = 0; i < steps.Count; i++)
                steps[i].StepNumber = i + 1;
        }

        // Deduplicar por LocationId conservando el primer orden
        var seen = new HashSet<uint>();
        var finalSteps = new List<RouteStepVm>();
        foreach (var s in steps)
        {
            if (seen.Add(s.LocationId))
                finalSteps.Add(s);
        }
        steps = finalSteps;

        if (steps.Count < 1)
            throw new InvalidOperationException("La ruta debe tener al menos 1 paso (Location).");

        using var cn = new MySqlConnection(_conn);
        cn.Open();
        using var tx = cn.BeginTransaction();

        // Reglas: no puedes crear ruta para subfamily inactiva o cuyo family/area esté inactiva
        using (var chk = new MySqlCommand(@"
            SELECT COUNT(*)
            FROM subfamily sf
            JOIN family f ON f.id = sf.id_family
            JOIN area a ON a.id = f.id_area
            WHERE sf.id=@sf AND sf.active=1 AND f.active=1 AND a.active=1
        ", cn, tx))
        {
            chk.Parameters.AddWithValue("@sf", vm.SubfamilyId);
            var ok = Convert.ToInt32(chk.ExecuteScalar());
            if (ok == 0)
                throw new InvalidOperationException("No puedes asignar ruta: la Subfamily (o su Family/Area) está inactiva.");
        }

        if (vm.Id == 0)
        {
            // Antes de crear una nueva ruta activa, valida que se pueda desactivar la actual (si existe)
            uint? currentActiveId = null;
            using (var cmd = new MySqlCommand(@"
                SELECT id
                FROM route
                WHERE subfamily_id=@sf AND active=1
                LIMIT 1
            ", cn, tx))
            {
                cmd.Parameters.AddWithValue("@sf", vm.SubfamilyId);
                var obj = cmd.ExecuteScalar();
                if (obj != null) currentActiveId = Convert.ToUInt32(obj);
            }

            if (currentActiveId.HasValue)
            {
                var wipCount = CountWipInRoute(cn, tx, currentActiveId.Value);
                if (wipCount > 0)
                    throw new InvalidOperationException("No se puede crear una nueva ruta activa: hay WIP en proceso en la ruta activa actual.");
            }

            // Generar version: MAX(version numeric) + 100
            var nextVersion = GetNextVersion(cn, tx, vm.SubfamilyId);
            vm.Version = nextVersion.ToString();

            // Desactivar todas las rutas de esa subfamily (histórico) y esta será la activa
            using (var off = new MySqlCommand(@"UPDATE route SET active=0 WHERE subfamily_id=@sf", cn, tx))
            {
                off.Parameters.AddWithValue("@sf", vm.SubfamilyId);
                off.ExecuteNonQuery();
            }

            using (var ins = new MySqlCommand(@"
                INSERT INTO route (subfamily_id, name, version, active)
                VALUES (@sf, @n, @v, 1)
            ", cn, tx))
            {
                ins.Parameters.AddWithValue("@sf", vm.SubfamilyId);
                ins.Parameters.AddWithValue("@n", vm.Name);
                ins.Parameters.AddWithValue("@v", vm.Version);
                ins.ExecuteNonQuery();
                vm.Id = (uint)ins.LastInsertedId;
            }

            InsertSteps(cn, tx, vm.Id, steps);
        }
        else
        {
            // Si la ruta está activa y tiene WIP, no permitimos modificar pasos (ni rearmar flujo)
            var isActive = false;
            using (var st = new MySqlCommand(@"SELECT active FROM route WHERE id=@id", cn, tx))
            {
                st.Parameters.AddWithValue("@id", vm.Id);
                isActive = Convert.ToBoolean(st.ExecuteScalar());
            }

            if (isActive)
            {
                var wipCount = CountWipInRoute(cn, tx, vm.Id);
                if (wipCount > 0)
                    throw new InvalidOperationException("No se pueden cambiar pasos: hay WIP en proceso en esta ruta activa.");
            }

            using (var upd = new MySqlCommand(@"
                UPDATE route
                SET name=@n
                WHERE id=@id
            ", cn, tx))
            {
                upd.Parameters.AddWithValue("@n", vm.Name);
                upd.Parameters.AddWithValue("@id", vm.Id);
                upd.ExecuteNonQuery();
            }

            using (var del = new MySqlCommand(@"DELETE FROM route_step WHERE route_id=@id", cn, tx))
            {
                del.Parameters.AddWithValue("@id", vm.Id);
                del.ExecuteNonQuery();
            }

            InsertSteps(cn, tx, vm.Id, steps);
        }

        tx.Commit();
    }

    private static void InsertSteps(MySqlConnection cn, MySqlTransaction tx, uint routeId, List<RouteStepVm> steps)
    {
        // Re-secuencia estricta 1..N
        for (int i = 0; i < steps.Count; i++)
            steps[i].StepNumber = i + 1;

        for (int i = 0; i < steps.Count; i++)
        {
            using var ins = new MySqlCommand(@"
                INSERT INTO route_step (route_id, step_number, location_id)
                VALUES (@r, @sn, @loc)
            ", cn, tx);

            ins.Parameters.AddWithValue("@r", routeId);
            ins.Parameters.AddWithValue("@sn", steps[i].StepNumber);
            ins.Parameters.AddWithValue("@loc", steps[i].LocationId);
            ins.ExecuteNonQuery();
        }
    }

    private static int CountWipInRoute(MySqlConnection cn, MySqlTransaction tx, uint routeId)
    {
        using var cmd = new MySqlCommand(@"
            SELECT COUNT(*)
            FROM wip_item wi
            JOIN route_step rs ON rs.id = wi.current_step_id
            WHERE rs.route_id = @rid
              AND wi.status IN ('ACTIVE','HOLD')
        ", cn, tx);

        cmd.Parameters.AddWithValue("@rid", routeId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int GetNextVersion(MySqlConnection cn, MySqlTransaction tx, uint subfamilyId)
    {
        // version es varchar(20), guardamos "100","200"... pero calculamos numérico
        using var cmd = new MySqlCommand(@"
            SELECT COALESCE(MAX(CAST(version AS UNSIGNED)), 0)
            FROM route
            WHERE subfamily_id=@sf
        ", cn, tx);

        cmd.Parameters.AddWithValue("@sf", subfamilyId);
        var max = Convert.ToInt32(cmd.ExecuteScalar());
        return max + 100;
    }
}
