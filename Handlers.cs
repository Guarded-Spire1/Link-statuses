using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Link_statuses
{
    static public class Handlers
    {
        private readonly static string _folder = Path.Combine(AppContext.BaseDirectory, "Data");
        private readonly static string _pathToUsersData = Path.Combine(_folder, "usersData");
        private 
        public static readonly Dictionary<long, BotUser> Users = new Dictionary<long, BotUser>();
        static Handlers()
        {
            Directory.CreateDirectory(_folder);

            if (!File.Exists(_pathToUsersData))
                File.WriteAllText(_pathToUsersData, "{}");

            string json = File.ReadAllText(_pathToUsersData);
            Users = JsonSerializer.Deserialize<Dictionary<long, BotUser>>(json) ?? new Dictionary<long, BotUser>();
        }
        public static void SaveUsersData()
        {
            string json = JsonSerializer.Serialize(Users);
            File.WriteAllText(_pathToUsersData, json);
        }
        public static string TrimLink(string link)
        {
            var parsedLink = new Uri(link);
            return parsedLink.Host;

        }
        private static bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
        public static async Task MessageHandle(ITelegramBotClient bot, Update update)
        {
            if (update.Message == null || string.IsNullOrWhiteSpace(update.Message.Text))
            {
                Console.WriteLine("Something went wrong");
                return;
            }

            string command = !string.IsNullOrWhiteSpace(update.Message?.Text)
                             ? update.Message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                             ?? ""
                             : "";
            long userId = update.Message.Chat.Id;
            string userMessage = string.Join(" ", update.Message.Text
                                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                    .Skip(1));

            if (command == "/start")
            {
                if (!Users.ContainsKey(userId))
                {
                    Users[userId] = new BotUser();
                    SaveUsersData();
                    await bot.SendMessage(userId,
                        "You are registered now!\n\n" +
                        "This bot helps you track the status of your favorite links (websites).\n" +
                        "Commands:\n" +
                        "/add <link> - Add a link to tracking\n" +
                        "/delete <link> - Remove a link from tracking\n" +
                        "/show - Show all tracked links\n" +
                        "/clear - Remove all tracked links\n\n" +
                        "The bot will periodically check your links and notify you if any become unreachable."
                    );
                    return;
                }

                await bot.SendMessage(userId,
                         "Commands:\n" +
                         "/add <link> - Add a link to tracking\n" +
                         "/delete <link> - Remove a link from tracking\n" +
                         "/show - Show all tracked links\n" +
                         "/clear - Remove all tracked links\n\n" +
                         "The bot will periodically check your links and notify you if any become unreachable."
                     );
            }
            else if (command == "/show")
            {
                StringBuilder message = new StringBuilder("Currently tracked links:\n");
                if (Users[userId].Links.Count == 0)
                {
                    await bot.SendMessage(userId, "No links are currently being tracked.");
                    return;
                }

                foreach (var link in Users[userId].Links)
                {
                    var response = await Program.Status(link);
                    if (response == 0)
                        message.AppendLine($"Link {link} is unreachable ❌");
                    else 
                        message.AppendLine($"Link {link} is reachable ✅\n");

                }
                await bot.SendMessage(userId, message.ToString());
            }
            else if (command == "/add")
            {
                if (string.IsNullOrWhiteSpace(userMessage))
                {
                    await bot.SendMessage(userId, "To add link you need to write like this '/add example.com'");
                    return;
                }
                if (!IsValidUrl(userMessage))
                {
                    await bot.SendMessage(userId, "Invalid link, try again:");
                    return;
                }

                if (Users[userId].Links.Contains(TrimLink(userMessage)))
                {
                    await bot.SendMessage(userId, "This link is already being tracked.");
                    return;
                }

                Users[userId].Links.Add(TrimLink(userMessage));
                SaveUsersData();
                await bot.SendMessage(userId, "Link added to tracking.");

            }
            else if (command == "/delete")
            {
                if (string.IsNullOrWhiteSpace(userMessage))
                {
                    await bot.SendMessage(userId, "To delete link you need to write like this '/delete example.com'");
                    return;
                }

                if (!IsValidUrl(userMessage))
                {
                    await bot.SendMessage(userId, "Invalid link, try again:");
                    return;
                }

                if (!Users[userId].Links.Contains(TrimLink(userMessage)))
                {
                    await bot.SendMessage(userId, "This link is not being tracked.");
                    return;
                }

                if (Users[userId].Links.Count == 0)
                {
                    await bot.SendMessage(userId, "No links are currently being tracked.");
                    return;
                }

                Users[userId].Links.Remove(TrimLink(userMessage));
                SaveUsersData();
                await bot.SendMessage(userId, "Link removed from tracking.");
            }
            else if (command == "/clear")
            {
                if (Users[userId].Links.Count == 0)
                {
                    await bot.SendMessage(userId, "No links are currently being tracked.");
                    return;
                }
                Users[userId].Links.Clear();
                SaveUsersData();
                await bot.SendMessage(userId, "All links have been removed from tracking.");
            }
            else if (command == "/subscribe")
            {
                Users[userId].ReceiveBroadcast = true;
                SaveUsersData();
                await bot.SendMessage(userId, "You have subscribed to broadcast messages.");
            }
            else if (command == "/unsubscribe")
            {
                Users[userId].ReceiveBroadcast = false;
                SaveUsersData();
                await bot.SendMessage(userId, "You have unsubscribed from broadcast messages.");
            }
            else
            {
                await bot.SendMessage(userId, "Unknown command. Please use /start to see available commands.");
            }
        }
        public static async Task SendMessage(ITelegramBotClient bot, Dictionary<long, Dictionary<string, int>> responses)
        {
            StringBuilder message = new StringBuilder("Currently tracked links statuses: \n");
            foreach (var response in responses.Values)
            {
                foreach (var linkStatus in response)
                {
                    if (linkStatus.Value == 0)
                        message.AppendLine($"Link {linkStatus.Key} is unreachable ❌");
                    else
                        message.AppendLine($"Link {linkStatus.Key} is reachable ✅");
                }
            }
            message.AppendLine("\nThis message is sent automatically every hour. If you wish to stop receiving, click /unsubscribe button");
            foreach (var user in responses.Keys)
            {
                await bot.SendMessage(user, message.ToString());
            }
        }

    }
}