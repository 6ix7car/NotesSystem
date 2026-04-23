using System;

namespace Kurs
{
    public static class UpdateService
    {
        public static bool CheckForUpdates()
        {
            Console.WriteLine("Проверка обновлений...");
            return false;
        }

        public static void ApplyUpdate()
        {
            Console.WriteLine("Применение обновления...");
            SecurityLogger.Log("UPDATE", "system", "127.0.0.1", "Обновление установлено", "INFO");
        }
    }
}