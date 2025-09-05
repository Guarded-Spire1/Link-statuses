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
            Console.WriteLine("TelegramBot token: ");
            string BotToken = Console.ReadLine();


            telegramBot = new Host(BotToken);
            telegramBot.OnMessage = Handlers.MessageHandle;
            telegramBot.Start();


            timer = new System.Timers.Timer(60000); //time interval in milliseconds (60000 ms = 1 minute)
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;

            Console.ReadLine();

            timer.Stop();
            timer.Dispose();
            client.Dispose();
        }



        public static async Task<int> Status(string link) // return status code or 0 if link is invalid or request failed
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(link);
                if(response.IsSuccessStatusCode)
                {
                    return ((int)response.StatusCode);
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
        }
        static void OnTimedEvent(object? source, ElapsedEventArgs e) // every minute check links of all users who want to receive broadcast
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
                            Handlers.Logs[subscriber].Add(new Log { Link = link, Status = status, Timestamp = DateTimeOffset.Now });
                        }
                    }
                }
                Handlers.SaveUsersLogs();
                await Handlers.SendMessage(telegramBot.bot, responses);
            });
        }
    }
}
