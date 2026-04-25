using Npgsql;
using System.Data;

namespace Kurs
{
    public static class DbHelper
    {
        private static string connectionString = "Server=localhost;Port=5432;User ID=postgres;Password=3455;Database=NoteSystem5;";

        public static void InitializeDatabase()
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                // Создание таблиц, если их нет
                string createUsers = @"
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                username VARCHAR(100) NOT NULL UNIQUE,
                password_md5 VARCHAR(32) NOT NULL,
                role VARCHAR(20) NOT NULL DEFAULT 'user',
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                last_login_at TIMESTAMP
            );";
                string createNotes = @"
            CREATE TABLE IF NOT EXISTS notes (
                id SERIAL PRIMARY KEY,
                content TEXT NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP,
                user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                deleted_at TIMESTAMP
            );";
                string createSecurityLogs = @"
            CREATE TABLE IF NOT EXISTS security_logs (
                id SERIAL PRIMARY KEY,
                event_type VARCHAR(50) NOT NULL,
                username VARCHAR(100) NOT NULL,
                user_ip VARCHAR(45),
                description TEXT,
                severity VARCHAR(20) DEFAULT 'INFO',
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );";
                string createSystemStats = @"
            CREATE TABLE IF NOT EXISTS system_stats (
                id SERIAL PRIMARY KEY,
                device_name VARCHAR(255) NOT NULL,
                device_ip VARCHAR(45),
                cpu_usage DOUBLE PRECISION,
                ram_usage DOUBLE PRECISION,
                ram_total BIGINT,
                ram_available BIGINT,
                hdd_usage DOUBLE PRECISION,
                hdd_total BIGINT,
                hdd_free BIGINT,
                collected_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );";
                string createIndexes = @"
            CREATE INDEX IF NOT EXISTS idx_notes_user_id ON notes(user_id);
            CREATE INDEX IF NOT EXISTS idx_security_logs_created_at ON security_logs(created_at);
        ";

                using (var cmd = new NpgsqlCommand(createUsers, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new NpgsqlCommand(createNotes, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new NpgsqlCommand(createSecurityLogs, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new NpgsqlCommand(createSystemStats, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new NpgsqlCommand(createIndexes, conn)) cmd.ExecuteNonQuery();
            }
        }
        public static DataTable ExecuteQuery(string query, NpgsqlParameter[] parameters = null)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);
                    using (var adapter = new NpgsqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public static int ExecuteNonQuery(string query, NpgsqlParameter[] parameters = null)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
        }
    }
}