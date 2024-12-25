using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Mime;
using System.Threading;

class Program
{
    public class Game
    {
        public string GameName { get; set; }
        public string DownloadLink { get; set; }
    }

    static async Task Main(string[] args)
    {
        // Путь к файлу конфигурации JSON
        string filePath = "games.json";
        
        // Чтение и десериализация JSON в список объектов Game
        List<Game> games = LoadGames(filePath);

        // Если игры найдены
        if (games != null && games.Count > 0)
        {
            Console.WriteLine("Выберите игру:");

            // Отображение списка игр
            for (int i = 0; i < games.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {games[i].GameName}");
            }

            // Ввод пользователем номера игры
            int choice = int.Parse(Console.ReadLine()) - 1;

            if (choice >= 0 && choice < games.Count)
            {
                Console.WriteLine($"Вы выбрали: {games[choice].GameName}");
                Console.WriteLine($"Ссылка для скачивания: {games[choice].DownloadLink}");

                // Скачать игру
                await DownloadGame(games[choice].DownloadLink, games[choice].GameName);
            }
            else
            {
                Console.WriteLine("Неверный выбор.");
            }
        }
        else
        {
            Console.WriteLine("Не удалось загрузить игры.");
        }
    }

    // Метод для загрузки игр из JSON файла
    static List<Game> LoadGames(string filePath)
    {
        try
        {
            // Чтение содержимого JSON файла
            string json = File.ReadAllText(filePath);
            
            // Десериализация JSON в список объектов Game
            return JsonConvert.DeserializeObject<List<Game>>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке файла: {ex.Message}");
            return null;
        }
    }

    // Метод для скачивания игры с отображением процента загрузки, скорости и размера
    static async Task DownloadGame(string url, string gameName)
    {
        using (var client = new HttpClient())
        using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
        using (var stream = await response.Content.ReadAsStreamAsync())
        {
            // Получаем тип контента из заголовков HTTP
            string contentType = response.Content.Headers.ContentType?.ToString();
            string fileExtension = GetFileExtensionFromContentType(contentType);
            
            // Если тип файла не определен, используем расширение .bin
            if (string.IsNullOrEmpty(fileExtension))
                fileExtension = ".bin";

            // Путь для сохранения файла
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), $"{gameName}{fileExtension}");

            // Получение общего размера файла
            long totalBytes = response.Content.Headers.ContentLength ?? 0;
            long totalBytesRead = 0;
            int bytesRead;
            DateTime startTime = DateTime.Now;

            // Таймер для обновления консоли не чаще, чем раз в секунду
            var timer = new Timer(_ => 
            {
                Console.Clear();
                if (totalBytes > 0)
                {
                    int currentPercentage = (int)(totalBytesRead * 100 / totalBytes);
                    double downloadSpeed = totalBytesRead / (DateTime.Now - startTime).TotalSeconds / 1024;  // KB/sec
                    double timeRemaining = (totalBytes - totalBytesRead) / (downloadSpeed * 1024);  // sec
                    Console.WriteLine($"Загружается {gameName}...");
                    Console.WriteLine($"Загружено: {currentPercentage}% ({totalBytesRead / 1024 / 1024} MB из {totalBytes / 1024 / 1024} MB)");
                    Console.WriteLine($"Скорость: {downloadSpeed:F2} KB/s");
                    Console.WriteLine($"Осталось времени: {timeRemaining:F2} сек");
                }
            }, null, 0, 1000);

            // Создание потока для записи
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] buffer = new byte[65536];  // Увеличен до 64 KB

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                }

                // Остановить таймер после завершения загрузки
                timer.Dispose();

                Console.WriteLine($"\nФайл {gameName} успешно загружен!");
            }
        }
    }

    // Метод для получения расширения файла по типу контента
    static string GetFileExtensionFromContentType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return null;

        // Пример маппинга контента в расширение файла
        if (contentType.Contains("application/zip"))
            return ".zip";
        if (contentType.Contains("application/pdf"))
            return ".pdf";
        if (contentType.Contains("image/jpeg"))
            return ".jpg";
        if (contentType.Contains("image/png"))
            return ".png";
        if (contentType.Contains("application/x-msdownload"))
            return ".exe";
        if (contentType.Contains("text/plain"))
            return ".txt";
        // Если неизвестный тип, возвращаем бинарное расширение
        return ".bin";
    }
}
