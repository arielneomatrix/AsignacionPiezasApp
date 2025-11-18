using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace AsignacionPiezasApp.Services
{


    public static class Database
    {
        public static string BaseDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AsignacionPiezasApp");
        public static string DbPath => Path.Combine(BaseDir, "asignacion.db");
        public static string FotosDir => Path.Combine(BaseDir, "Fotos");
        // NUEVO: carpeta base para informes (en Documentos, no en AppData)
        public static string ReportsRoot => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "AsignacionPiezas",
            "Informes"
        );

        public static string ConnectionString => $"Data Source={DbPath};Cache=Shared";

        public static void Initialize()
        {
            Directory.CreateDirectory(BaseDir);
            Directory.CreateDirectory(FotosDir);
            Directory.CreateDirectory(ReportsRoot); // NUEVO


            bool firstTime = !File.Exists(DbPath);
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                PRAGMA journal_mode=WAL;
                CREATE TABLE IF NOT EXISTS Usuarios(
                    Id TEXT PRIMARY KEY,
                    Nombre TEXT NOT NULL,
                    Username TEXT NULL
                );
                CREATE TABLE IF NOT EXISTS Estatus(
                    Id TEXT PRIMARY KEY,
                    Nombre TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Piezas(
                    Codigo TEXT PRIMARY KEY,
                    Descripcion TEXT,
                    UsuarioId TEXT NULL,
                    EstatusId TEXT NULL,
                    FotoPath TEXT NULL,
                    FechaRegistro TEXT NOT NULL,
                    FOREIGN KEY(UsuarioId) REFERENCES Usuarios(Id),
                    FOREIGN KEY(EstatusId) REFERENCES Estatus(Id)
                );";
                cmd.ExecuteNonQuery();
            }

            // ---- Migración: asegura columna Username e índice único parcial ----
            EnsureColumn(conn, "Usuarios", "Username", "TEXT");
            using (var idx = conn.CreateCommand())
            {
                idx.CommandText = @"CREATE UNIQUE INDEX IF NOT EXISTS UX_Usuarios_Username 
                                    ON Usuarios(Username) 
                                    WHERE Username IS NOT NULL AND Username <> '';";
                idx.ExecuteNonQuery();
            }

            if (firstTime)
            {
                using var tx = conn.BeginTransaction();
                void Seed(string table, string id, string nombre, string? username = null)
                {
                    using var c = conn.CreateCommand();
                    c.CommandText = $"INSERT INTO {table}(Id,Nombre,Username) VALUES ($id,$n,$u)";
                    c.Parameters.AddWithValue("$id", id);
                    c.Parameters.AddWithValue("$n", nombre);
                    c.Parameters.AddWithValue("$u", (object?)username ?? DBNull.Value);
                    c.ExecuteNonQuery();
                }
                Seed("Usuarios", Guid.NewGuid().ToString(), "Modelador A", "modelador.a");
                Seed("Usuarios", Guid.NewGuid().ToString(), "Texturizador B", "texturizador.b");
                tx.Commit();
            }
        }

        private static void EnsureColumn(SqliteConnection conn, string table, string column, string type)
        {
            using var check = conn.CreateCommand();
            check.CommandText = $"PRAGMA table_info({table});";
            using var rd = check.ExecuteReader();
            bool exists = false;
            while (rd.Read()) if (string.Equals(rd.GetString(1), column, StringComparison.OrdinalIgnoreCase)) { exists = true; break; }

            if (!exists)
            {
                using var alter = conn.CreateCommand();
                alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type};";
                alter.ExecuteNonQuery();
            }
        }

        public static SqliteConnection Open() => new SqliteConnection(ConnectionString);
    }
}
