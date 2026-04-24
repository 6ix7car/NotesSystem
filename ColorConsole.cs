using System;

namespace NoteSystem
{
    public static class ColorConsole
    {
        // Сообщение об ошибке (красный)
        public static void WriteError(string message)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(message);
            Console.ForegroundColor = oldColor;
        }

        public static void WriteLineError(string message)
        {
            WriteError(message);
            Console.WriteLine();
        }

        // Успешное сообщение (зелёный)
        public static void WriteSuccess(string message)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(message);
            Console.ForegroundColor = oldColor;
        }

        public static void WriteLineSuccess(string message)
        {
            WriteSuccess(message);
            Console.WriteLine();
        }

        // Предупреждение (жёлтый)
        public static void WriteWarning(string message)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(message);
            Console.ForegroundColor = oldColor;
        }

        public static void WriteLineWarning(string message)
        {
            WriteWarning(message);
            Console.WriteLine();
        }
    }
}