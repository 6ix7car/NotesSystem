using System;
using System.Linq;

namespace Kurs
{
    class Program
    {
        private static int currentUserId = -1;
        private static string currentUsername = "";
        private static string currentUserRole = "";

        public static void SetCurrentUser(int id, string name, string role)
        {
            currentUserId = id;
            currentUsername = name;
            currentUserRole = role;
        }

        static void Main(string[] args)
        {
            Console.Title = "Notes System";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════╗
║                 СИСТЕМА ЗАМЕТОК v1.0                         ║
║                 Введите --help для списка команд             ║
╚══════════════════════════════════════════════════════════════╝
");
            Console.ResetColor();
            TestDatabaseConnection();

            // Проверка обновлений при старте
            // if (UpdateService.CheckForUpdates()) UpdateService.ApplyUpdate();

            while (true)
            {
                if (currentUserId == -1)
                {
                    Console.Write("\n> ");
                    string input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(input)) continue;
                    if (input == "exit") break;
                    ProcessCommand(input);
                }
                else
                {
                    Console.Write($"\n{currentUsername}@{currentUserRole}> ");
                    string input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(input)) continue;
                    if (input == "exit") break;
                    ProcessCommand(input);
                }
            }
        }

        static void ProcessCommand(string input)
        {
            string[] parts = input.Split(' ');
            string cmd = parts[0].ToLower();

            // Команды, доступные даже без авторизации
            switch (cmd)
            {
                case "--help":
                    ShowHelp();
                    return;
                case "--login":
                    if (parts.Length >= 3)
                        AuthService.Login(parts[1], parts[2]);
                    else
                        Console.WriteLine("Использование: --login <username> <password>");
                    return;
                case "--register":
                    if (parts.Length >= 3)
                    {
                        string role = parts.Length >= 4 ? parts[3].ToLower() : "user";
                        AuthService.Register(parts[1], parts[2], role);
                    }
                    else
                        Console.WriteLine("Использование: --register <username> <password> [role]");
                    return;
                case "--checkupdate":
                    UpdateService.CheckForUpdates();
                    return;
                case "--update":
                    UpdateService.ApplyUpdate();
                    return;
                case "exit":
                    Environment.Exit(0);
                    return;
            }

            // Если не авторизован, остальные команды не доступны
            if (currentUserId == -1)
            {
                Console.WriteLine("Необходимо войти. Используйте --login <user> <pass>");
                return;
            }

            // Команды, требующие авторизации
            switch (cmd)
            {
                case "--logout":
                    currentUserId = -1;
                    currentUsername = "";
                    currentUserRole = "";
                    Console.WriteLine("Вы вышли из системы.");
                    break;

                case "--myrole":
                    Console.WriteLine($"Ваша роль: {currentUserRole}");
                    break;

                case "--addnewnote":
                    if (currentUserRole == "readonly")
                    {
                        Console.WriteLine("Недостаточно прав. Роль readonly не может создавать заметки.");
                        break;
                    }
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Использование: --addNewNote \"текст заметки\"");
                        break;
                    }
                    string noteText = string.Join(" ", parts.Skip(1)).Trim('"');
                    NoteService.AddNote(currentUserId, noteText);
                    Console.WriteLine("Заметка добавлена.");
                    break;

                case "--listnotes":
                    NoteService.ShowNotes(currentUserId);
                    break;

                case "--getnote":
                    if (parts.Length < 2 || !int.TryParse(parts[1], out int getNoteId))
                    {
                        Console.WriteLine("Использование: --getNote <id>");
                        break;
                    }
                    string content = NoteService.GetNoteContent(getNoteId, currentUserId);
                    if (content == null)
                        Console.WriteLine($"Заметка с id {getNoteId} не найдена.");
                    else
                        Console.WriteLine($"Заметка {getNoteId}: {content}");
                    break;

                case "--editnote":
                    if (currentUserRole == "readonly")
                    {
                        Console.WriteLine("Недостаточно прав. Роль readonly не может редактировать заметки.");
                        break;
                    }
                    if (parts.Length < 3 || !int.TryParse(parts[1], out int editId))
                    {
                        Console.WriteLine("Использование: --editNote <id> \"новый текст\"");
                        break;
                    }
                    string newText = string.Join(" ", parts.Skip(2)).Trim('"');
                    if (NoteService.UpdateNote(editId, currentUserId, newText))
                        Console.WriteLine("Заметка обновлена.");
                    else
                        Console.WriteLine($"Не удалось обновить заметку {editId}.");
                    break;

                case "--deletenote":
                    if (currentUserRole == "readonly")
                    {
                        Console.WriteLine("Недостаточно прав. Роль readonly не может удалять заметки.");
                        break;
                    }
                    if (parts.Length < 2 || !int.TryParse(parts[1], out int delId))
                    {
                        Console.WriteLine("Использование: --deleteNote <id>");
                        break;
                    }
                    if (NoteService.DeleteNote(delId, currentUserId))
                        Console.WriteLine("Заметка удалена.");
                    else
                        Console.WriteLine($"Не удалось удалить заметку {delId}.");
                    break;

                case "--restorenote":
                    if (currentUserRole == "readonly")
                    {
                        Console.WriteLine("Недостаточно прав. Роль readonly не может восстанавливать заметки.");
                        break;
                    }
                    if (parts.Length < 2 || !int.TryParse(parts[1], out int restId))
                    {
                        Console.WriteLine("Использование: --restoreNote <id>");
                        break;
                    }
                    if (NoteService.RestoreNote(restId, currentUserId))
                        Console.WriteLine("Заметка восстановлена.");
                    else
                        Console.WriteLine($"Не удалось восстановить заметку {restId}.");
                    break;

                case "--systemstats":
                    if (currentUserRole != "admin")
                    {
                        Console.WriteLine("Доступ запрещён. Только для администратора.");
                        break;
                    }
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Использование: --systemStats local|remote <host> <user> <pass>");
                        break;
                    }
                    if (parts[1].ToLower() == "local")
                    {
                        Stats.ShowLocalStats();
                        Stats.SaveLocalStatsToDb();
                        Console.WriteLine("Статистика сохранена в БД.");
                    }
                    else if (parts[1].ToLower() == "remote" && parts.Length >= 5)
                    {
                        Stats.ShowRemoteStats(parts[2], parts[3], parts[4]);
                    }
                    else
                        Console.WriteLine("Неверный формат. Используйте --help.");
                    break;

                case "--securitylogs":
                    if (currentUserRole != "admin")
                    {
                        Console.WriteLine("Доступ запрещён. Только для администратора.");
                        break;
                    }
                    if (parts.Length >= 2 && parts[1].ToLower() == "list")
                        ShowSecurityLogs();
                    else
                        Console.WriteLine("Использование: --securityLogs list");
                    break;

                default:
                    Console.WriteLine("Неизвестная команда. Введите --help.");
                    break;
            }
        }

        static void ShowSecurityLogs()
        {
            var dt = DbHelper.ExecuteQuery("SELECT * FROM security_logs ORDER BY created_at DESC LIMIT 20");
            if (dt.Rows.Count == 0)
            {
                Console.WriteLine("Логи безопасности пусты.");
                return;
            }
            Console.WriteLine("\nПоследние события безопасности:");
            foreach (System.Data.DataRow row in dt.Rows)
            {
                Console.WriteLine($"{row["created_at"]} | {row["event_type"]} | {row["username"]} | {row["user_ip"]} | {row["description"]} | {row["severity"]}");
            }
        }

       

        static void TestDatabaseConnection()
        {
            try
            {
                var dt = DbHelper.ExecuteQuery("SELECT NOW() as current_time, version() as pg_version");
                Console.WriteLine("Подключение к БД успешно!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подключения: {ex.Message}");
            }
        }
        static void ShowHelp()
        {
            bool isAuth = currentUserId != -1;
            if (!isAuth)
            {
                Console.WriteLine(@"
Доступные команды (не авторизован):
  --login <username> <password>   - Вход в систему
  --register <user> <pass> [role] - Регистрация (роль: admin/user/readonly)
  --checkUpdate                   - Проверить обновления
  --update                        - Выполнить обновление
  --help                          - Эта справка
  exit                            - Выход
");
                return;
            }

            // Авторизован
            Console.WriteLine("=== ДОСТУПНЫЕ КОМАНДЫ ===");
            Console.WriteLine("  --logout                 - Выйти из системы");
            Console.WriteLine("  --myrole                 - Показать мою роль");
            Console.WriteLine("  --listNotes              - Список заметок");
            Console.WriteLine("  --getNote <id>           - Показать заметку по ID");

            if (currentUserRole != "readonly")
            {
                Console.WriteLine("  --addNewNote \"текст\"    - Создать заметку");
                Console.WriteLine("  --editNote <id> \"текст\" - Редактировать заметку");
                Console.WriteLine("  --deleteNote <id>       - Удалить заметку");
                Console.WriteLine("  --restoreNote <id>      - Восстановить заметку");
            }

            if (currentUserRole == "admin")
            {
                Console.WriteLine("  --systemStats local     - Статистика системы (CPU/RAM/HDD)");
                Console.WriteLine("  --systemStats remote host user pass - Удалённая статистика");
                Console.WriteLine("  --securityLogs list     - Логи безопасности");
            }

            Console.WriteLine("  --checkUpdate           - Проверить обновления");
            Console.WriteLine("  --update                - Выполнить обновление");
            Console.WriteLine("  --help                  - Справка");
            Console.WriteLine("  exit                    - Выход");
        }
    }
}