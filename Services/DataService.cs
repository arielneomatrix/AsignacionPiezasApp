using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using AsignacionPiezasApp.Models;

namespace AsignacionPiezasApp.Services
{
    /// <summary>
    /// Capa de datos sobre SQLite. Expone eventos para refrescar UI.
    /// Integra: Usuarios (username), Estatus y Piezas.
    /// </summary>
    /// 
   

    public sealed class DataService
    {
        public static DataService Instance { get; } = new();

        public event Action? UsuariosChanged;
        public event Action? EstatusChanged;
        public event Action? PiezasChanged;

        private DataService() { }

        // ============================
        //          USUARIOS
        // ============================

        public IEnumerable<Usuario> GetUsuarios()
        {
            using var conn = Database.Open(); conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Nombre, Username FROM Usuarios ORDER BY Nombre";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                yield return new Usuario
                {
                    Id = Guid.Parse(rd.GetString(0)),
                    Nombre = rd.GetString(1),
                    NombreUsuario = rd.IsDBNull(2) ? "" : rd.GetString(2)
                };
            }
        }

        // Compatibilidad con código viejo (sin username)
        public void AddUsuario(string nombre)
        {
            AddUsuario(nombre, "", out _);
        }

        public bool AddUsuario(string nombre, string? username, out string? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(nombre))
            {
                error = "El nombre es obligatorio.";
                return false;
            }

            try
            {
                using var conn = Database.Open(); conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Usuarios(Id,Nombre,Username) VALUES ($id,$n,$u)";
                cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
                cmd.Parameters.AddWithValue("$n", nombre.Trim());
                cmd.Parameters.AddWithValue("$u", string.IsNullOrWhiteSpace(username) ? DBNull.Value : username.Trim());
                cmd.ExecuteNonQuery();
                UsuariosChanged?.Invoke();
                return true;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // constraint (índice único username)
            {
                error = "El nombre de usuario ya existe. Usa otro.";
                return false;
            }
        }

        public bool UpdateUsuario(Guid id, string nombre, string? username, out string? error)
        {
            error = null;
            if (id == Guid.Empty) { error = "Selecciona un usuario."; return false; }
            if (string.IsNullOrWhiteSpace(nombre)) { error = "El nombre es obligatorio."; return false; }

            try
            {
                using var conn = Database.Open(); conn.Open();
                using var cmd = conn
                    .CreateCommand();
                cmd.CommandText = "UPDATE Usuarios SET Nombre=$n, Username=$u WHERE Id=$id";
                cmd.Parameters.AddWithValue("$id", id.ToString());
                cmd.Parameters.AddWithValue("$n", nombre.Trim());
                cmd.Parameters.AddWithValue("$u", string.IsNullOrWhiteSpace(username) ? DBNull.Value : username.Trim());
                if (cmd.ExecuteNonQuery() == 0) { error = "Usuario no encontrado."; return false; }
                UsuariosChanged?.Invoke();
                return true;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                error = "El nombre de usuario ya existe. Usa otro.";
                return false;
            }
        }

        public bool DeleteUsuario(Guid id, out string? error)
        {
            error = null;
            if (id == Guid.Empty) { error = "Selecciona un usuario."; return false; }

            using var conn = Database.Open(); conn.Open();

            // Evitar borrar si está asignado a piezas
            using (var chk = conn.CreateCommand())
            {
                chk.CommandText = "SELECT COUNT(1) FROM Piezas WHERE UsuarioId=$id";
                chk.Parameters.AddWithValue("$id", id.ToString());
                var count = (long)chk.ExecuteScalar()!;
                if (count > 0)
                {
                    error = "No se puede eliminar: el usuario está asignado a piezas.";
                    return false;
                }
            }

            using (var del = conn.CreateCommand())
            {
                del.CommandText = "DELETE FROM Usuarios WHERE Id=$id";
                del.Parameters.AddWithValue("$id", id.ToString());
                if (del.ExecuteNonQuery() == 0) { error = "Usuario no encontrado."; return false; }
            }

            UsuariosChanged?.Invoke();
            return true;
        }

        // ============================
        //           ESTATUS
        // ============================

        public IEnumerable<EstatusPieza> GetEstatus()
        {
            using var conn = Database.Open(); conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Nombre FROM Estatus ORDER BY Nombre";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                yield return new EstatusPieza { Id = Guid.Parse(rd.GetString(0)), Nombre = rd.GetString(1) };
        }

        public void AddEstatus(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return;
            using var conn = Database.Open(); conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Estatus(Id,Nombre) VALUES ($id,$n)";
            cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("$n", nombre.Trim());
            cmd.ExecuteNonQuery();
            EstatusChanged?.Invoke();
        }

        // ============================
        //            PIEZAS
        // ============================

        public IEnumerable<AsignacionPieza> GetPiezas(string? codigoLike = null, Guid? usuarioId = null, Guid? estatusId = null)
        {
            using var conn = Database.Open(); conn.Open();
            using var cmd = conn.CreateCommand();
            var where = new List<string>();
            if (!string.IsNullOrWhiteSpace(codigoLike)) { where.Add("Codigo LIKE $c"); cmd.Parameters.AddWithValue("$c", "%" + codigoLike + "%"); }
            if (usuarioId is not null) { where.Add("UsuarioId = $u"); cmd.Parameters.AddWithValue("$u", usuarioId.ToString()); }
            if (estatusId is not null) { where.Add("EstatusId = $e"); cmd.Parameters.AddWithValue("$e", estatusId.ToString()); }
            var sql = "SELECT Codigo, Descripcion, UsuarioId, EstatusId, FotoPath, FechaRegistro FROM Piezas";
            if (where.Count > 0) sql += " WHERE " + string.Join(" AND ", where);
            sql += " ORDER BY datetime(FechaRegistro) DESC";
            cmd.CommandText = sql;

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                yield return new AsignacionPieza
                {
                    Codigo = rd.GetString(0),
                    Descripcion = rd.IsDBNull(1) ? "" : rd.GetString(1),
                    UsuarioId = rd.IsDBNull(2) ? null : Guid.Parse(rd.GetString(2)),
                    EstatusId = rd.IsDBNull(3) ? null : Guid.Parse(rd.GetString(3)),
                    FotoPath = rd.IsDBNull(4) ? null : rd.GetString(4),
                    FechaRegistro = DateTime.Parse(rd.GetString(5))
                };
            }
        }

        public AsignacionPieza? FindByCodigo(string codigo)
        {
            using var conn = Database.Open(); conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Codigo, Descripcion, UsuarioId, EstatusId, FotoPath, FechaRegistro FROM Piezas WHERE Codigo=$c";
            cmd.Parameters.AddWithValue("$c", codigo);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return null;

            return new AsignacionPieza
            {
                Codigo = rd.GetString(0),
                Descripcion = rd.IsDBNull(1) ? "" : rd.GetString(1),
                UsuarioId = rd.IsDBNull(2) ? null : Guid.Parse(rd.GetString(2)),
                EstatusId = rd.IsDBNull(3) ? null : Guid.Parse(rd.GetString(3)),
                FotoPath = rd.IsDBNull(4) ? null : rd.GetString(4),
                FechaRegistro = DateTime.Parse(rd.GetString(5))
            };
        }

        public void AddOrUpdatePieza(AsignacionPieza p)
        {
            using var conn = Database.Open(); conn.Open();
            bool exists = FindByCodigo(p.Codigo) is not null;

            using var cmd = conn.CreateCommand();
            if (!exists)
            {
                cmd.CommandText = @"INSERT INTO Piezas(Codigo,Descripcion,UsuarioId,EstatusId,FotoPath,FechaRegistro)
                    VALUES($c,$d,$u,$e,$f,$fe)";
                cmd.Parameters.AddWithValue("$c", p.Codigo);
                cmd.Parameters.AddWithValue("$d", p.Descripcion ?? "");
                cmd.Parameters.AddWithValue("$u", (object?)p.UsuarioId?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$e", (object?)p.EstatusId?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$f", (object?)p.FotoPath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$fe", DateTime.Now.ToString("s"));
            }
            else
            {
                cmd.CommandText = @"UPDATE Piezas SET Descripcion=$d, UsuarioId=$u, EstatusId=$e, FotoPath=$f
                    WHERE Codigo=$c";
                cmd.Parameters.AddWithValue("$c", p.Codigo);
                cmd.Parameters.AddWithValue("$d", p.Descripcion ?? "");
                cmd.Parameters.AddWithValue("$u", (object?)p.UsuarioId?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$e", (object?)p.EstatusId?.ToString() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$f", (object?)p.FotoPath ?? DBNull.Value);
            }
            cmd.ExecuteNonQuery();
            PiezasChanged?.Invoke();
        }

        public string? GetUsuarioNombre(Guid? id) => GetUsuarios().FirstOrDefault(u => u.Id == id)?.Nombre;
        public string? GetEstatusNombre(Guid? id) => GetEstatus().FirstOrDefault(e => e.Id == id)?.Nombre;
    }
}

