using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Xabe.FFmpeg;
using File = System.IO.File;
using Message = Telegram.Bot.Types.Message;

namespace Bot.Services
{
    public interface IMessageService
    {
        void HandleAsync(Message message);
    }
    
    public class MessageService : IMessageService
    {
        private readonly ITelegramBotClient _bot;
        private readonly ILogger _logger;
        private static readonly Regex WebmRegex = new Regex("https?[^ ]*.webm");

        public MessageService(ITelegramBotClient bot, ILogger<MessageService> logger)
        {
            _bot = bot;
            _logger = logger;
        }

        public async void HandleAsync(Message message)
        {
            try
            {
                if (message.From?.IsBot == true)
                {
                    return;
                }

                if (message.Text?.StartsWith("/start") == true)
                {
                    await _bot.SendTextMessageAsync(
                        new ChatId(message.Chat.Id),
                        "Send me a video or link to WebM or add bot to group.");
                }
                else
                {
                    await ProcessMessageAsync(message);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, string.Empty);
            }
        }
        
        private async Task ProcessMessageAsync(Message message)
        {
            if (message?.Document?.FileName?.EndsWith(".webm", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                if (message.Caption?.Contains("!nsfw", StringComparison.InvariantCultureIgnoreCase) != true)
                {
                    await HandleDocumentAsync(message);
                }
            }

            if (!string.IsNullOrEmpty(message?.Text))
            {
                if (message.Text.Contains("!nsfw", StringComparison.InvariantCultureIgnoreCase) != true)
                {
                    var matches = WebmRegex.Matches(message.Text);

                    foreach (Match match in matches)
                    {
                        await HandleLinkAsync(message, match.Value);
                    }   
                }
            }

            if (!string.IsNullOrEmpty(message?.Caption))
            {
                if (message.Caption.Contains("!nsfw", StringComparison.InvariantCultureIgnoreCase) != true)
                {
                    var matches = WebmRegex.Matches(message.Caption);

                    foreach (Match match in matches)
                    {
                        await HandleLinkAsync(message, match.Value);
                    }
                }
            }
        }

        private async Task HandleLinkAsync(Message receivedMessage, string link)
        {
            var inputFileName = Path.ChangeExtension(Path.GetTempFileName(), ".webm");

            var sentMessage = await _bot.SendTextMessageAsync(
                new ChatId(receivedMessage.Chat.Id),
                $"{link}\nDownloading file 📥",
                replyToMessageId: receivedMessage.MessageId,
                disableNotification: true);

            using var webClient = new WebClient();

            try
            {
                await webClient.DownloadFileTaskAsync(link, inputFileName);

                await ProcessFileAsync(receivedMessage, sentMessage, inputFileName, link);
            }
            catch (WebException webException)
            {
                if (webException.Response is HttpWebResponse response)
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.Unauthorized:

                            await _bot.EditMessageTextAsync(
                                new ChatId(sentMessage.Chat.Id),
                                sentMessage.MessageId,
                                $"{link}\nI am not authorized to download video from this source 🚫");

                            return;

                        case HttpStatusCode.NotFound:

                            await _bot.EditMessageTextAsync(
                                new ChatId(sentMessage.Chat.Id),
                                sentMessage.MessageId,
                                $"{link}\nVideo not found ⚠️");

                            return;

                        case HttpStatusCode.InternalServerError:

                            await _bot.EditMessageTextAsync(
                                new ChatId(sentMessage.Chat.Id),
                                sentMessage.MessageId,
                                $"{link}\nServer error 🛑");

                            return;
                    }
                }
            }
        }

        private async Task HandleDocumentAsync(Message receivedMessage)
        {
            var inputFileName = Path.ChangeExtension(Path.GetTempFileName(), ".webm");

            var sentMessage = await _bot.SendTextMessageAsync(
                new ChatId(receivedMessage.Chat.Id), 
                $"{receivedMessage.Document.FileName}\nDownloading file 📥",
                replyToMessageId: receivedMessage.MessageId,
                disableNotification: true);

            await using (var fileStream = File.Create(inputFileName))
            {
                await _bot.GetInfoAndDownloadFileAsync(receivedMessage.Document.FileId, fileStream);
            }

            await ProcessFileAsync(receivedMessage, sentMessage, inputFileName, receivedMessage.Document.FileName);
        }

        private async Task ProcessFileAsync(Message receivedMessage, Message sentMessage, string inputFilePath,
            string link)
        {
            await _bot.EditMessageTextAsync(
                new ChatId(sentMessage.Chat.Id),
                sentMessage.MessageId,
                $"{link}\nConversion in progress 🚀");

            var mediaInfo = await FFmpeg.GetMediaInfo(inputFilePath);

            var videoStream = mediaInfo.VideoStreams.FirstOrDefault();

            if (videoStream == null)
            {
                await _bot.EditMessageTextAsync(
                    new ChatId(sentMessage.Chat.Id),
                    sentMessage.MessageId,
                    $"{link}\nVideo doesn't have video stream in it");
                
                return;
            }
            
            var width = videoStream.Width % 2 == 0 ? videoStream.Width : videoStream.Width - 1;
            var height = videoStream.Height % 2 == 0 ? videoStream.Height : videoStream.Height - 1;

            videoStream = videoStream
                .SetCodec(VideoCodec.h264)
                .SetSize(width, height);

            var audioStream = mediaInfo.AudioStreams.FirstOrDefault()?.SetCodec(AudioCodec.aac);

            var outputFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".mp4");
            
            try
            {
                await FFmpeg.Conversions.New()
                    .AddStream<IStream>(videoStream, audioStream)
                    .SetOutput(outputFilePath)
                    .Start();
            }
            catch (Exception)
            {
                await _bot.EditMessageTextAsync(
                    new ChatId(sentMessage.Chat.Id),
                    sentMessage.MessageId,
                    $"{link}\nError during file conversion");
                
                CleanupFiles(inputFilePath);

                throw;
            }

            await _bot.EditMessageTextAsync(
                new ChatId(sentMessage.Chat.Id),
                sentMessage.MessageId,
                $"{link}\nGenerating thumbnail 🖼️");

            var thumbnailFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".jpg");
            var thumbnailConversion = await FFmpeg.Conversions.FromSnippet.Snapshot(inputFilePath, thumbnailFilePath, TimeSpan.Zero);

            try
            {
                await thumbnailConversion.Start();
            }
            catch (Exception)
            {
                await _bot.EditMessageTextAsync(
                    new ChatId(sentMessage.Chat.Id),
                    sentMessage.MessageId,
                    $"{link}\nError during file conversion");
                
                CleanupFiles(inputFilePath, outputFilePath);

                throw;
            }

            await _bot.EditMessageTextAsync(
                new ChatId(sentMessage.Chat.Id),
                sentMessage.MessageId, 
                $"{link}\nUploading file to Telegram 📤");

            try
            {
                await using var outputStream = File.OpenRead(outputFilePath);
                await using var thumbnailStream = File.OpenRead(thumbnailFilePath);

                await _bot.DeleteMessageAsync(
                    new ChatId(sentMessage.Chat.Id),
                    sentMessage.MessageId
                );

                await _bot.SendVideoAsync(
                    new ChatId(sentMessage.Chat.Id),
                    new InputMedia(outputStream, outputFilePath),
                    replyToMessageId: receivedMessage.MessageId,
                    thumb: new InputMedia(thumbnailStream, thumbnailFilePath),
                    caption: link,
                    disableNotification: true);
            }
            catch (Exception)
            {
                await _bot.EditMessageTextAsync(
                    new ChatId(sentMessage.Chat.Id),
                    sentMessage.MessageId,
                    $"{link}\nError during file upload");
                    
                CleanupFiles(inputFilePath, outputFilePath, thumbnailFilePath);

                throw;
            }

            CleanupFiles(inputFilePath, outputFilePath, thumbnailFilePath);
        }

        private static void CleanupFiles(string inputFilePath = null, string outputFile = null, string thumbnail = null)
        {
            if (inputFilePath != null)
            {
                File.Delete(inputFilePath);
            }
            
            if (outputFile != null)
            {
                File.Delete(outputFile);
            }
            
            if (thumbnail != null)
            {
                File.Delete(thumbnail);
            }
        }
    }
}