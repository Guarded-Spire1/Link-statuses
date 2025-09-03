using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Link_statuses
{
    public class Host
    {

        public Func<ITelegramBotClient, Update, Task>? OnMessage;
        public ITelegramBotClient bot { get; private set; }
        public Host(string token)
        {
            bot = new TelegramBotClient(token);
            bot.SetMyCommands(new[]
            {
                new BotCommand{Command="/start", Description="Start bot"},
                new BotCommand{Command="/show", Description="Show tracked links"},
                new BotCommand{Command="/add", Description="Adding new link"},
                new BotCommand{Command="/delete", Description="Deleting link from trackings"},
                new BotCommand{Command="/clear", Description="Deleting all links from tracking"}
            });
        }

        public void Start()
        {
            bot.StartReceiving(UpdateHandle, ErrorHandler);
            Console.WriteLine("Bot is working");
        }

        private async Task ErrorHandler(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            Console.WriteLine($"Error occuried: {exception.Message}");
            await Task.CompletedTask;
        }

        private async Task UpdateHandle(ITelegramBotClient client, Update update, CancellationToken token)
        {
            Console.WriteLine(update.Message?.Text);
            OnMessage?.Invoke(client, update);
            await Task.CompletedTask;
        }


    }
}
