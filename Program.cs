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


            timer = new System.Timers.Timer(3000);
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
                HttpResponseMessage response = await client.GetAsync("http://" + link);
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

                var subscribers = new List<long>();
                foreach (var user in Handlers.Users)
                {
                    if (user.Value.ReceiveBroadcast)
                    {
                        subscribers.Add(user.Key);
                    }
                }

                if (subscribers.Count == 0) return;
                var responses = new Dictionary<long, Dictionary<string, int>>();
                foreach (var subscriber in subscribers)
                {
                    if (Handlers.Users.TryGetValue(subscriber, out var user))
                    {
                        foreach (var link in user.Links)
                        {
                            int status = await Status(link);
                            if (!responses.ContainsKey(subscriber))
                            {
                                responses[subscriber] = new Dictionary<string, int>();
                            }
                            responses[subscriber][link] = status;
                            Handlers.Logs.Add(new Log { Link = link, Status = status, Timestamp = DateTimeOffset.Now });
                        }
                    }
                }
                Handlers.SaveLogs();
                await Handlers.SendMessage(telegramBot.bot, responses);
            });
        }
    }
}
