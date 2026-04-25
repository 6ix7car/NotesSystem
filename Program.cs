using NoteSystem;
using System;
using System.Collections.Generic;
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
            DbHelper.InitializeDatabase();


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
        private static readonly Dictionary<string, string> ShortToLongCmd = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
        // --- заметки ---
        { "-a", "--addnewnote" },
        { "-add", "--addnewnote" },
        { "-l", "--listnotes" },
        { "-list", "--listnotes" },
        { "-g", "--getnote" },
        { "-e", "--editnote" },
        { "-edit", "--editnote" },
        { "-d", "--deletenote" },
        { "-del", "--deletenote" },
        { "-rm", "--deletenote" },
        { "-r", "--restorenote" },
        { "-restore", "--restorenote" },

        // --- системная статистика и логи ---
        { "-stats", "--systemstats" },
        { "-logs", "--securitylogs" },

        // --- аутентификация ---
        { "-login", "--login" },
        { "-reg", "--register" },
        { "-role", "--myrole" },
        { "-out", "--logout" },

        // --- обновления ---
        { "-up", "--update" },
        { "-check", "--checkupdate" },

        // --- справка и выход ---
        { "-h", "--help" },
        { "?", "--help" },
        { "/?", "--help" }
        };
        static void ProcessCommand(string input)
        {
            string[] parts = input.Split(' ');
            string rawCmd = parts[0].ToLower();

            // маппинг коротких команд -> длинные
            if (ShortToLongCmd.TryGetValue(rawCmd, out string mappedCmd))
                rawCmd = mappedCmd;

            string cmd = rawCmd;   // теперь cmd – это уже нормализованная команда



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
                ColorConsole.WriteLineWarning("Необходимо войти. Используйте --login <user> <pass>");
                return;
            }

            // Команды, требующие авторизации
            switch (cmd)
            {
                case "--logout":
                    currentUserId = -1;
                    currentUsername = "";
                    currentUserRole = "";
                    ColorConsole.WriteLineSuccess("Вы вышли из системы.");
                    break;

                case "--myrole":
                    Console.WriteLine($"Ваша роль: {currentUserRole}");
                    break;

                case "--addnewnote":
                    if (currentUserRole == "readonly")
                    {
                        ColorConsole.WriteLineWarning("Недостаточно прав. Роль readonly не может создавать заметки.");
                        break;
                    }
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Использование: --addNewNote \"текст заметки\"");
                        break;
                    }
                    string noteText = string.Join(" ", parts.Skip(1)).Trim('"');
                    NoteService.AddNote(currentUserId, noteText);
                    ColorConsole.WriteLineSuccess("Заметка добавлена.");
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
                        ColorConsole.WriteLineWarning($"Заметка с id {getNoteId} не найдена.");
                    else
                        Console.WriteLine($"Заметка {getNoteId}: {content}");
                    break;

                case "--editnote":
                    if (currentUserRole == "readonly")
                    {
                        ColorConsole.WriteLineWarning("Недостаточно прав. Роль readonly не может редактировать заметки.");
                        break;
                    }
                    if (parts.Length < 3 || !int.TryParse(parts[1], out int editId))
                    {
                        Console.WriteLine("Использование: --editNote <id> \"новый текст\"");
                        break;
                    }
                    string newText = string.Join(" ", parts.Skip(2)).Trim('"');
                    if (NoteService.UpdateNote(editId, currentUserId, newText))
                        ColorConsole.WriteLineSuccess("Заметка обновлена.");
                    else
                        ColorConsole.WriteLineError($"Не удалось обновить заметку {editId}.");
                    break;

                case "--deletenote":
                    if (currentUserRole == "readonly")
                    {
                        ColorConsole.WriteLineWarning("Недостаточно прав. Роль readonly не может удалять заметки.");
                        break;
                    }
                    if (parts.Length < 2 || !int.TryParse(parts[1], out int delId))
                    {
                        Console.WriteLine("Использование: --deleteNote <id>");
                        break;
                    }
                    ColorConsole.WriteLineWarning($"Вы уверены, что хотите удалить заметку {delId}? (y/n): ");
                    string confirm = Console.ReadLine();
                    if (confirm != null && confirm.ToLower() == "y")
                    {
                        if (NoteService.DeleteNote(delId, currentUserId))
                            ColorConsole.WriteLineSuccess("Заметка удалена.");
                        else
                            ColorConsole.WriteLineWarning($"Не удалось удалить заметку {delId}.");
                    }
                    else
                        ColorConsole.WriteLineSuccess("Удаление отменено.");
                    break;

                case "--restorenote":
                    if (currentUserRole == "readonly")
                    {
                        ColorConsole.WriteLineWarning("Недостаточно прав. Роль readonly не может восстанавливать заметки.");
                        break;
                    }
                    if (parts.Length < 2 || !int.TryParse(parts[1], out int restId))
                    {
                        Console.WriteLine("Использование: --restoreNote <id>");
                        break;
                    }
                    if (NoteService.RestoreNote(restId, currentUserId))
                        ColorConsole.WriteLineSuccess("Заметка восстановлена.");
                    else
                        ColorConsole.WriteLineWarning($"Не удалось восстановить заметку {restId}.");
                    break;

                case "--systemstats":
                    if (currentUserRole != "admin")
                    {
                        ColorConsole.WriteLineError("Доступ запрещён. Только для администратора.");
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
                        ColorConsole.WriteLineSuccess("Статистика сохранена в БД.");
                    }
                    else if (parts[1].ToLower() == "remote" && parts.Length >= 5)
                    {
                        Stats.ShowRemoteStats(parts[2], parts[3], parts[4]);
                    }
                    else
                        ColorConsole.WriteLineError("Неверный формат. Используйте --help.");
                    break;

                case "--securitylogs":
                    if (currentUserRole != "admin")
                    {
                        ColorConsole.WriteLineError("Доступ запрещён. Только для администратора.");
                        break;
                    }
                    if (parts.Length >= 2 && parts[1].ToLower() == "list")
                        ShowSecurityLogs();
                    else
                        Console.WriteLine("Использование: --securityLogs list");
                    break;

                default:
                    ColorConsole.WriteLineWarning("Неизвестная команда. Введите --help.");
                    break;
            }
        }

        static void ShowSecurityLogs()
        {
            var dt = DbHelper.ExecuteQuery("SELECT * FROM security_logs ORDER BY created_at DESC LIMIT 20");
            if (dt.Rows.Count == 0)
            {
                ColorConsole.WriteLineWarning("Логи безопасности пусты.");
                return;
            }
            Console.WriteLine("\nПоследние события безопасности:");
            foreach (System.Data.DataRow row in dt.Rows)
            {
                Console.WriteLine($"{row["created_at"]} | {row["event_type"]} | {row["username"]} | {row["user_ip"]} | {row["description"]} | {row["severity"]}");
            }
        }


        static void AnimatedWait(int seconds)
        {
            for (int i = seconds; i > 0; i--)
            {
                Console.Write($"\rПовторная попытка через {i} секунд...   ");
                System.Threading.Thread.Sleep(1000);
            }
            Console.Write("\r" + new string(' ', 40) + "\r"); // Очищаем строку
        }
        static void TestDatabaseConnection()
        {
            int attempts = 0;
            const int maxAttempts = 10;
            const int retryDelaySeconds = 5;

            while (attempts < maxAttempts)
            {
                try
                {
                    var dt = DbHelper.ExecuteQuery("SELECT NOW() as current_time, version() as pg_version");
                    if (dt.Rows.Count > 0)
                    {
                        if (attempts > 0)
                        {
                            Console.WriteLine(); // Переход на новую строку после успешного подключения
                        }
                        ColorConsole.WriteLineSuccess($"Подключение к БД успешно! (попытка {attempts + 1})");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    attempts++;

                    if (attempts == 1)
                    {
                        ColorConsole.WriteLineError($"Ошибка подключения: {ex.Message}");
                    }

                    if (attempts < maxAttempts)
                    {
                        // Анимированный обратный отсчёт
                        AnimatedWait(retryDelaySeconds);
                        Console.Write($"Попытка {attempts + 1} подключения...");
                    }
                }
            }

            Console.WriteLine(); // Переход на новую строку
            ColorConsole.WriteLineError("Не удалось подключиться к базе данных после нескольких попыток.");
            ColorConsole.WriteLineWarning("Программа будет работать в ограниченном режиме (некоторые функции могут быть недоступны).");
        }
        static void ShowHelp()
        {
            bool isAuth = currentUserId != -1;
            if (!isAuth)
            {
                Console.WriteLine(@"
                Доступные команды (не авторизован):
                  --login (-login) <username> <password>     - Вход в систему
                  --register (-reg) <user> <pass> [role]     - Регистрация (роль: admin/user/readonly)
                  --checkUpdate (-check)                     - Проверить обновления
                  --update (-up)                             - Выполнить обновление
                  --help (-h, ?, /?)                         - Эта справка
                  exit                                       - Выход
                ");
                return;
            }

            // Авторизован
            Console.WriteLine("=== ДОСТУПНЫЕ КОМАНДЫ ===");
            Console.WriteLine("  --logout (-out)                    - Выйти из системы");
            Console.WriteLine("  --myrole (-role)                   - Показать мою роль");
            Console.WriteLine("  --listNotes (-l, -list)            - Список заметок");
            Console.WriteLine("  --getNote (-g) <id>                - Показать заметку по ID");

            if (currentUserRole != "readonly")
            {
                Console.WriteLine("  --addNewNote (-a, -add) \"текст\"    - Создать заметку");
                Console.WriteLine("  --editNote (-e, -edit) <id> \"текст\" - Редактировать заметку");
                Console.WriteLine("  --deleteNote (-d, -del, -rm) <id>     - Удалить заметку");
                Console.WriteLine("  --restoreNote (-r, -restore) <id>     - Восстановить заметку");
            }

            if (currentUserRole == "admin")
            {
                Console.WriteLine("  --systemStats (-stats) local       - Статистика системы (CPU/RAM/HDD)");
                Console.WriteLine("  --systemStats remote host user pass - Удалённая статистика");
                Console.WriteLine("  --securityLogs (-logs) list        - Логи безопасности");
            }

            Console.WriteLine("  --checkUpdate (-check)             - Проверить обновления");
            Console.WriteLine("  --update (-up)                     - Выполнить обновление");
            Console.WriteLine("  --help (-h, ?, /?)                 - Справка");
            Console.WriteLine("  exit                               - Выход");
        }
    }
}