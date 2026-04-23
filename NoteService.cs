using System;
using System.Data;
using Npgsql;

namespace Kurs
{
    public static class NoteService
    {
        public static void AddNote(int userId, string text)
        {
            string query = "INSERT INTO notes (user_id, content, created_at) VALUES (@uid, @txt, @now)";
            var parameters = new[]
            {
                new NpgsqlParameter("uid", userId),
                new NpgsqlParameter("txt", text),
                new NpgsqlParameter("now", DateTime.Now)
            };
            DbHelper.ExecuteNonQuery(query, parameters);
            SecurityLogger.Log("ADD_NOTE", GetUsernameById(userId), "127.0.0.1", $"Заметка: {text}", "INFO");
        }

        public static void ShowNotes(int userId)
        {
            string query = "SELECT id, content, created_at FROM notes WHERE user_id = @uid AND is_deleted = false ORDER BY created_at DESC";
            var parameters = new[] { new NpgsqlParameter("uid", userId) };
            DataTable dt = DbHelper.ExecuteQuery(query, parameters);
            Console.WriteLine("Ваши заметки:");
            foreach (DataRow row in dt.Rows)
            {
                Console.WriteLine($"[{row["created_at"]}] {row["content"]} (id={row["id"]})");
            }
        }

        public static string GetNoteContent(int noteId, int userId)
        {
            string query = "SELECT content FROM notes WHERE id = @id AND user_id = @uid AND is_deleted = false";
            var parameters = new[]
            {
                new NpgsqlParameter("id", noteId),
                new NpgsqlParameter("uid", userId)
            };
            DataTable dt = DbHelper.ExecuteQuery(query, parameters);
            if (dt.Rows.Count > 0)
                return dt.Rows[0]["content"].ToString();
            return null;
        }

        public static bool UpdateNote(int noteId, int userId, string newText)
        {
            string query = "UPDATE notes SET content = @txt, updated_at = @now WHERE id = @id AND user_id = @uid AND is_deleted = false";
            var parameters = new[]
            {
                new NpgsqlParameter("txt", newText),
                new NpgsqlParameter("now", DateTime.Now),
                new NpgsqlParameter("id", noteId),
                new NpgsqlParameter("uid", userId)
            };
            int rows = DbHelper.ExecuteNonQuery(query, parameters);
            if (rows > 0)
            {
                SecurityLogger.Log("EDIT_NOTE", GetUsernameById(userId), "127.0.0.1", $"Заметка {noteId} изменена", "INFO");
                return true;
            }
            return false;
        }

        public static bool DeleteNote(int noteId, int userId)
        {
            string query = "UPDATE notes SET is_deleted = true, deleted_at = @now WHERE id = @id AND user_id = @uid AND is_deleted = false";
            var parameters = new[]
            {
                new NpgsqlParameter("now", DateTime.Now),
                new NpgsqlParameter("id", noteId),
                new NpgsqlParameter("uid", userId)
            };
            int rows = DbHelper.ExecuteNonQuery(query, parameters);
            if (rows > 0)
            {
                SecurityLogger.Log("DELETE_NOTE", GetUsernameById(userId), "127.0.0.1", $"Заметка {noteId} удалена", "WARNING");
                return true;
            }
            return false;
        }

        public static bool RestoreNote(int noteId, int userId)
        {
            string query = "UPDATE notes SET is_deleted = false, deleted_at = NULL, updated_at = @now WHERE id = @id AND user_id = @uid AND is_deleted = true";
            var parameters = new[]
            {
                new NpgsqlParameter("now", DateTime.Now),
                new NpgsqlParameter("id", noteId),
                new NpgsqlParameter("uid", userId)
            };
            int rows = DbHelper.ExecuteNonQuery(query, parameters);
            if (rows > 0)
            {
                SecurityLogger.Log("RESTORE_NOTE", GetUsernameById(userId), "127.0.0.1", $"Заметка {noteId} восстановлена", "INFO");
                return true;
            }
            return false;
        }

        private static string GetUsernameById(int userId)
        {
            var dt = DbHelper.ExecuteQuery("SELECT username FROM users WHERE id=@id", new[] { new NpgsqlParameter("id", userId) });
            if (dt.Rows.Count > 0)
                return dt.Rows[0]["username"].ToString();
            return "unknown";
        }
    }
}