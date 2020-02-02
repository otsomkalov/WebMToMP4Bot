using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WebMToMP4TelegramBot.Services.Interfaces;

namespace WebMToMP4TelegramBot
{
    public class UpdateFunctions
    {
        private readonly IMessageService _messageService;

        public UpdateFunctions(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [FunctionName(nameof(ProcessUpdateAsync))]
        public async Task<IActionResult> ProcessUpdateAsync(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "update")]
            string updateString,
            ILogger logger)
        {
            try
            {
                var update = JsonConvert.DeserializeObject<Update>(updateString);

                switch (update.Type)
                {
                    case UpdateType.Unknown:
                        break;

                    case UpdateType.Message:
                        await _messageService.HandleAsync(update.Message);

                        break;

                    case UpdateType.InlineQuery:
                        break;

                    case UpdateType.ChosenInlineResult:
                        break;

                    case UpdateType.CallbackQuery:
                        break;

                    case UpdateType.EditedMessage:
                        break;

                    case UpdateType.ChannelPost:
                        break;

                    case UpdateType.EditedChannelPost:
                        break;

                    case UpdateType.ShippingQuery:
                        break;

                    case UpdateType.PreCheckoutQuery:
                        break;

                    case UpdateType.Poll:
                        break;

                    case UpdateType.PollAnswer:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception exception)
            {
                logger.LogError("{Exception}", exception);
            }

            return new OkResult();
        }
    }
}
