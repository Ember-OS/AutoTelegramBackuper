using System.ComponentModel.DataAnnotations;
using System.Text;

using Telegram.Bot;

namespace AutoTelegramBackuper.CLI
{
    public class Program
    {
        public static TelegramService telegramService;
        static  void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var token = "0YLRiyDQtNGD0LzQsNC7INGN0YLQviDRgtC+0LrQtdC9Pw==";
            token = File.ReadAllText("token.txt");
           
            telegramService = new TelegramService(token);
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string backupConfigFolder = Path.Combine(appDirectory, "BackupCfg");
            string cacheDirectory = Path.Combine(appDirectory, "cache");

            if (!Directory.Exists(backupConfigFolder))
            {
                Directory.CreateDirectory(backupConfigFolder);
            }
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            if (!Directory.Exists(backupConfigFolder))
            {
                Console.WriteLine("Папка BackupCfg не найдена.");
                return;
            }

            List<BackupTask> backupTasks = new List<BackupTask>();

            foreach (var configFile in Directory.GetFiles(backupConfigFolder, "*.cfg"))
            {
                try
                {
                    string[] lines = File.ReadAllLines(configFile);

                    if (lines.Length < 5)
                    {
                        Console.WriteLine($"Файл {Path.GetFileName(configFile)} имеет неверный формат.");
                        continue;
                    }

                    string sourceFolder = lines[0];
                    string telegramChannelId = lines[1];
                    string description = lines[2];
                    if (!int.TryParse(lines[3], out int intervalHours) || intervalHours <= 0)
                    {
                        Console.WriteLine($"Неверный интервал в файле {Path.GetFileName(configFile)}.");
                        continue;
                    }
                    string programName = lines[4];

                    BackupTask task = new BackupTask(sourceFolder, telegramChannelId, description, intervalHours, programName, cacheDirectory);
                    backupTasks.Add(task);

                    Console.WriteLine($"Загружена задача из {Path.GetFileName(configFile)}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при чтении {Path.GetFileName(configFile)}: {ex.Message}");
                }
            }

            Console.WriteLine("Все задачи загружены. Программа запущена.");
            Console.ReadLine();
        }
    }
}
