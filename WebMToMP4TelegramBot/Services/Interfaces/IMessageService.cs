using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace WebMToMP4TelegramBot.Services.Interfaces
{
    public interface IMessageService
    {
        Task HandleAsync(Message message);
    }
}
