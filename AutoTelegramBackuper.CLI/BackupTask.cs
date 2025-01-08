using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace AutoTelegramBackuper.CLI
{
    public class BackupTask
    {
        public string SourceFolder { get; }
        public string TelegramChannelId { get; }
        public string Description { get; }
        public int IntervalHours { get; }
        public string ProgramName { get; }

        private Timer _timer;
        private string _cacheDirectory;
        private string _cacheCompressedDirectory;

        public BackupTask(string sourceFolder, string telegramChannelId, string description, int intervalHours, string programName, string cacheDirectory)
        {
            SourceFolder = sourceFolder;
            TelegramChannelId = telegramChannelId;
            Description = description;
            IntervalHours = intervalHours;
            ProgramName = programName;
            _cacheDirectory = Path.Combine(cacheDirectory, programName, "uncompressed");
            _cacheCompressedDirectory = Path.Combine(cacheDirectory, programName, "compressed");

            _timer = new Timer(ExecuteBackup, null, TimeSpan.Zero, TimeSpan.FromHours(intervalHours));
        }

        private void ExecuteBackup(object state)
        {
            try
            {
                if (!Directory.Exists(SourceFolder))
                {
                    Console.WriteLine($"Папка источника {SourceFolder} не найдена для программы {ProgramName}.");
                    return;
                }

                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                }
                if (!Directory.Exists(_cacheCompressedDirectory))
                {
                    Directory.CreateDirectory(_cacheCompressedDirectory);
                }

                foreach (var filePath in Directory.GetFiles(SourceFolder))
                {
                    string destinationPath = Path.Combine(_cacheDirectory, Path.GetFileName(filePath));
                    File.Copy(filePath, destinationPath, true);
                }

                Console.WriteLine($"Файлы успешно скопированы для программы {ProgramName}.");
                CompressFiles(_cacheDirectory, _cacheCompressedDirectory, 44);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выполнении задачи для программы {ProgramName}: {ex.Message}");
            }
        }

        public async void CompressFiles(string sourceFolder, string compressedFolder, int partSizeInMB)
        {
            if (!Directory.Exists(compressedFolder))
            {
                Directory.CreateDirectory(compressedFolder);
            }

            string name = $"{ProgramName}_{Guid.NewGuid().ToString().Replace("-","").Substring(0,5)}";
            string archiveBaseName = Path.Combine(compressedFolder, $"{name}.zip");

            string partSize = (partSizeInMB * 1024).ToString(); 

            string arguments = $"a -tzip \"{archiveBaseName}\" \"{sourceFolder}\\*\" -v{partSize}k";

            ProcessStartInfo pro = new ProcessStartInfo
            {
                FileName = "7z.exe", 
                Arguments = arguments,
                WindowStyle = ProcessWindowStyle.Hidden 
            };

            using (Process process = Process.Start(pro))
            {
                process.WaitForExit(); 
            }

            List<string> pathFFiles = new List<string>();
            foreach (string filePath in Directory.GetFiles(_cacheCompressedDirectory))
            {
                pathFFiles.Add(filePath);
            }
            UploadFiles uploadFiles = new UploadFiles
            {
                FilePaths = pathFFiles,
                ChannelId = TelegramChannelId,
                Description = Description,
                Title = ProgramName
            };
            await Program.telegramService.SendFilesAsync(uploadFiles);

            foreach (string filePath in Directory.GetFiles(sourceFolder))
            {
                File.Delete(filePath);
            }
        }
    }
}