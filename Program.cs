

using System.Text.Json;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Link_statuses
{
    public class Program
    {
        static Host telegramBot;
        static readonly HttpClient client = new HttpClient();
        static System.Timers.Timer? timer;
        static async Task Main()
        {
            //"7446920723:AAHF1ZpctjnHIBPIdAvr46DTZEO9OIGHCqU"
            //Console.WriteLine("TelegramBot token: ");  Потом включи
            //string BotToken = Console.ReadLine();

            telegramBot = new Host("7446920723:AAHF1ZpctjnHIBPIdAvr46DTZEO9OIGHCqU");
            telegramBot.OnMessage = Handlers.MessageHandle;
            telegramBot.Start();


            timer = new System.Timers.Timer(4000);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;

            Console.ReadLine();

            timer.Stop();
            timer.Dispose();
            client.Dispose();
        }



        public static async Task<int> Status(string link)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(link);
                return ((int)response.StatusCode);
            }
            catch
            {
                return 0;
            }
        }
        static void OnTimedEvent(object? source, ElapsedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (Handlers._usersUrls != null && Handlers._usersUrls.Count != 0)
                {
                   var responeses = new Dictionary<string, int>();
                    foreach (var link in Handlers._usersUrls)
                    {
                        var response = await Status("http://" + link);
                        responeses.Add(link, response);
                    }
                    await Handlers.SendMessage(telegramBot.bot, responeses);
                }
                else { Console.WriteLine("No links are being tracked"); }
            });
        }
    }
}
