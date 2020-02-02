using System.Net;
using FFmpeg.NET;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using WebMToMP4TelegramBot;
using WebMToMP4TelegramBot.Services;
using WebMToMP4TelegramBot.Services.Interfaces;

[assembly: WebJobsStartup(typeof(Startup))]

namespace WebMToMP4TelegramBot
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            builder.Services
                .AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient(config["Token"]))
                .AddSingleton(provider => new Engine("D:\\home\\site\\wwwroot\\ffmpeg.exe"))
                .AddSingleton(provider => new WebClient())
                .AddSingleton<IMessageService, MessageService>();
        }
    }
}
