using NoteSystem;
using Npgsql;
using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace Kurs
{
    public static class AuthService
    {
        public static string GetMd5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static bool Login(string username, string password)
        {
            string hash = GetMd5Hash(password);
            string query = "SELECT id, role FROM users WHERE username=@u AND password_md5=@p";
            var parameters = new[]
            {
                new NpgsqlParameter("u", username),
                new NpgsqlParameter("p", hash)
            };
            DataTable result = DbHelper.ExecuteQuery(query, parameters);
            bool success = result.Rows.Count == 1;

            if (success)
            {
                int userId = Convert.ToInt32(result.Rows[0]["id"]);
                string role = result.Rows[0]["role"].ToString();
                // обновим время последнего входа
                string updateQuery = "UPDATE users SET last_login_at = @now WHERE id = @id";
                var updateParams = new[]
                {
                    new NpgsqlParameter("now", DateTime.Now),
                    new NpgsqlParameter("id", userId)
                };
                DbHelper.ExecuteNonQuery(updateQuery, updateParams);
                // сохраняем в глобальных переменных в Program
                Program.SetCurrentUser(userId, username, role);
            }

            SecurityLogger.Log(success ? "LOGIN_SUCCESS" : "LOGIN_FAIL", username,
                               "127.0.0.1", success ? "Успешный вход" : "Неверный пароль или логин", success ? "INFO" : "WARNING");
            return success;
        }

        public static bool Register(string username, string password, string role = "user")
        {
            if (role != "admin" && role != "user" && role != "readonly")
                role = "user";

            string hash = GetMd5Hash(password);
            try
            {
                string query = "INSERT INTO users (username, password_md5, role, created_at) VALUES (@u, @p, @r, @now)";
                var parameters = new[]
                {
                    new NpgsqlParameter("u", username),
                    new NpgsqlParameter("p", hash),
                    new NpgsqlParameter("r", role),
                    new NpgsqlParameter("now", DateTime.Now)
                };
                DbHelper.ExecuteNonQuery(query, parameters);
                SecurityLogger.Log("REGISTER", username, "127.0.0.1", $"Новая регистрация с ролью {role}", "INFO");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetUserRole(int userId)
        {
            string query = "SELECT role FROM users WHERE id = @id";
            var parameters = new[] { new NpgsqlParameter("id", userId) };
            DataTable dt = DbHelper.ExecuteQuery(query, parameters);
            if (dt.Rows.Count > 0)
                return dt.Rows[0]["role"].ToString();
            return "user";
        }

        public static bool UserExists(string username)
        {
            string query = "SELECT id FROM users WHERE username=@u";
            var parameters = new[] { new NpgsqlParameter("u", username) };
            var result = DbHelper.ExecuteQuery(query, parameters);
            return result.Rows.Count > 0;
        }
    }
}