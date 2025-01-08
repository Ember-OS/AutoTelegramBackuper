using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Telegram.Bot;
using Telegram.Bot.Types;

namespace AutoTelegramBackuper.CLI
{
    public class TelegramService
    {
        private readonly TelegramBotClient _botClient;
        public List<UploadFiles> Files;
        private readonly Task _processingTask;
        private readonly CancellationTokenSource _cancellationTokenSource;
        public TelegramService(string token)
        {
            Files = new List<UploadFiles>();
            _cancellationTokenSource = new CancellationTokenSource();

            _botClient = new TelegramBotClient(token);
            _processingTask = Task.Run(ProcessFilesAsync);
        }

        private async Task ProcessFilesAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                UploadFiles fileToSend = null;

                lock (Files)
                {
                    if (Files.Any())
                    {
                        fileToSend = Files.Last();
                        Console.WriteLine($"На отправку ушел {fileToSend.Title}");
                        Files.RemoveAt(Files.Count - 1);
                    }
                }

                if (fileToSend != null)
                {
                    try
                    {
                        await SendFilesAsync(fileToSend);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при отправке файла: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Ожидание 30 секунд началом новой отправки");
                    await Task.Delay(TimeSpan.FromSeconds(30), _cancellationTokenSource.Token);
                }
            }
        }

        public async Task SendFilesAsync(UploadFiles uploadFiles)
        {
            if (uploadFiles.FilePaths == null || uploadFiles.FilePaths.Count == 0)
                throw new ArgumentException("No files to upload.");

            bool firstFile = true;

            foreach (var filePath in uploadFiles.FilePaths)
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}");
                    continue;
                }

                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var inputFile = new InputFileStream(fileStream, Path.GetFileName(filePath));

                    bool sentSuccessfully = false;
                    while (!sentSuccessfully)
                    {
                        try
                        {
                            if (firstFile)
                            {
                                var currentTime = DateTime.Now.ToString("HH:mm dd-MM-yyyy");
                                var caption = $"***{uploadFiles.Title}***\n{uploadFiles.Description}\n___{currentTime}___";
                                await _botClient.SendDocument(uploadFiles.ChannelId, inputFile, caption: caption, parseMode:Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                Console.WriteLine($"Отправлен: {filePath}");
                                firstFile = false;
                            }
                            else
                            {
                                await _botClient.SendDocument(uploadFiles.ChannelId, inputFile);
                                Console.WriteLine($"Отправлен: {filePath}");
                            }

                            sentSuccessfully = true;
                            File.Delete(filePath); 
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка отправки файла {filePath}: {ex.Message}");
                            await Task.Delay(5000);
                        }
                    }
                }
            }
        }

    }
}
