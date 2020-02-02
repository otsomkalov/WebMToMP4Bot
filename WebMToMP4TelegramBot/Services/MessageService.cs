using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FFmpeg.NET;
using Telegram.Bot;
using Telegram.Bot.Types;
using WebMToMP4TelegramBot.Services.Interfaces;
using File = System.IO.File;

namespace WebMToMP4TelegramBot.Services
{
    public class MessageService : IMessageService
    {
        private readonly ITelegramBotClient _bot;
        private readonly Engine _ffmpeg;
        private readonly WebClient _webClient;

        public MessageService(Engine ffmpeg, ITelegramBotClient bot, WebClient webClient)
        {
            _ffmpeg = ffmpeg;
            _bot = bot;
            _webClient = webClient;
        }

        public async Task HandleAsync(Message message)
        {
            Message sentMessage;
            var inputFileName = $"{Path.GetTempPath()}{Guid.NewGuid()}.webm";

            if (message.Document != null)
            {
                if (!message.Document.FileName.Contains(".webm", StringComparison.InvariantCultureIgnoreCase)) return;

                sentMessage = await _bot.SendTextMessageAsync(
                    new ChatId(message.Chat.Id),
                    "Downloading file...",
                    replyToMessageId: message.MessageId);

                await using var fileStream = File.Create(inputFileName);
                await _bot.GetInfoAndDownloadFileAsync(message.Document.FileId, fileStream);
            }
            else
            {
                if (string.IsNullOrEmpty(message.Text)) return;
                if (!message.Text.Contains(".webm", StringComparison.InvariantCultureIgnoreCase)) return;
                if (!Uri.TryCreate(message.Text, UriKind.RelativeOrAbsolute, out var uri)) return;

                sentMessage = await _bot.SendTextMessageAsync(
                    new ChatId(message.Chat.Id),
                    "Downloading file...",
                    replyToMessageId: message.MessageId);

                await _webClient.DownloadFileTaskAsync(uri, inputFileName);
            }

            sentMessage = await _bot.EditMessageTextAsync(
                new ChatId(sentMessage.Chat.Id),
                sentMessage.MessageId,
                "Conversion in progress...");

            var inputFile = new MediaFile(inputFileName);

            var outputFile = await _ffmpeg.ConvertAsync(inputFile,
                new MediaFile($"{Path.GetTempPath()}{Guid.NewGuid().ToString()}.mp4"));

            sentMessage = await _bot.EditMessageTextAsync(
                new ChatId(sentMessage.Chat.Id),
                sentMessage.MessageId,
                "Generating thumbnail...");

            var thumbnail = await _ffmpeg.GetThumbnailAsync(
                outputFile,
                new MediaFile($"{Path.GetTempPath()}{Guid.NewGuid()}.jpg"),
                new ConversionOptions {Seek = TimeSpan.Zero});

            await _bot.EditMessageTextAsync(
                new ChatId(sentMessage.Chat.Id),
                sentMessage.MessageId,
                "Uploading file to Telegram...");

            await using (var videoStream = File.OpenRead(outputFile.FileInfo.FullName))
            {
                await using var imageStream = File.OpenRead(thumbnail.FileInfo.FullName);

                await _bot.SendVideoAsync(
                    new ChatId(sentMessage.Chat.Id),
                    new InputMedia(videoStream, outputFile.FileInfo.Name),
                    replyToMessageId: message.MessageId,
                    thumb: new InputMedia(imageStream, thumbnail.FileInfo.Name));
            }

            await _bot.DeleteMessageAsync(
                new ChatId(sentMessage.Chat.Id),
                sentMessage.MessageId);

            File.Delete(inputFile.FileInfo.FullName);
            File.Delete(outputFile.FileInfo.FullName);
            File.Delete(thumbnail.FileInfo.FullName);
        }
    }
}
