using Npgsql;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Sockets;

namespace Kurs
{
    public static class Stats
    {
        public static (double cpuUsage, double ramUsage, long ramTotal, long ramAvailable, double hddUsage, long hddTotal, long hddFree) GetCurrentStats()
        {
            double cpu = GetCpuUsage();
            long totalRam = GetTotalRamMb();
            long availableRam = GetAvailableRamMb();
            double ramUsage = totalRam > 0 ? Math.Round((double)(totalRam - availableRam) / totalRam * 100, 2) : 0;
            (long total, long free) = GetHddTotalFreeGb();
            long hddTotalGb = total;
            long hddFreeGb = free;
            double hddUsage = total > 0 ? Math.Round((double)(total - free) / total * 100, 2) : 0;

            return (cpu, ramUsage, totalRam, availableRam, hddUsage, hddTotalGb, hddFreeGb);
        }

        public static void SaveLocalStatsToDb()
        {
            var (cpu, ramUsage, ramTotal, ramAvailable, hddUsage, hddTotal, hddFree) = GetCurrentStats();
            string hostname = Environment.MachineName;
            string ip = GetLocalIPAddress();
            string query = @"
                INSERT INTO system_stats (device_name, device_ip, cpu_usage, ram_usage, ram_total, ram_available, hdd_usage, hdd_total, hdd_free, collected_at)
                VALUES (@name, @ip, @cpu, @ramUsage, @ramTotal, @ramAvailable, @hddUsage, @hddTotal, @hddFree, @now)";
            var parameters = new[]
            {
                new NpgsqlParameter("name", hostname),
                new NpgsqlParameter("ip", ip),
                new NpgsqlParameter("cpu", cpu),
                new NpgsqlParameter("ramUsage", ramUsage),
                new NpgsqlParameter("ramTotal", ramTotal),
                new NpgsqlParameter("ramAvailable", ramAvailable),
                new NpgsqlParameter("hddUsage", hddUsage),
                new NpgsqlParameter("hddTotal", hddTotal),
                new NpgsqlParameter("hddFree", hddFree),
                new NpgsqlParameter("now", DateTime.Now)
            };
            DbHelper.ExecuteNonQuery(query, parameters);
        }

        public static void ShowLocalStats()
        {
            var (cpu, ramUsage, ramTotal, ramAvailable, hddUsage, hddTotal, hddFree) = GetCurrentStats();
            Console.WriteLine($"📊 Статистика {Environment.MachineName} ({GetLocalIPAddress()}):");
            Console.WriteLine($"  CPU: {cpu}%");
            Console.WriteLine($"  RAM: {ramUsage}% ({ramAvailable} MB / {ramTotal} MB)");
            Console.WriteLine($"  HDD: {hddUsage}% ({hddFree} GB / {hddTotal} GB)");
        }

        // Для удалённой статистики пока заглушка
        public static void ShowRemoteStats(string host, string user, string pass)
        {
            Console.WriteLine($"Удалённая статистика для {host} пока не реализована.");
            // Здесь можно добавить реальную реализацию через WMI или SSH
        }

        private static double GetCpuUsage()
        {
            var counter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            counter.NextValue();
            System.Threading.Thread.Sleep(500);
            return Math.Round(counter.NextValue(), 2);
        }

        private static long GetTotalRamMb()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    return long.Parse(obj["TotalVisibleMemorySize"].ToString()) / 1024;
                }
            }
            return 0;
        }

        private static long GetAvailableRamMb()
        {
            var counter = new PerformanceCounter("Memory", "Available MBytes");
            return (long)counter.NextValue();
        }

        private static (long totalGb, long freeGb) GetHddTotalFreeGb()
        {
            DriveInfo drive = new DriveInfo("C");
            long total = drive.TotalSize / (1024 * 1024 * 1024);
            long free = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
            return (total, free);
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            return "127.0.0.1";
        }
    }
}