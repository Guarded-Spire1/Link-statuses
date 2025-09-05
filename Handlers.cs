using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly static string _pathToLogs = Path.Combine(_folder, "logs.txt");
        public static readonly Dictionary<long, BotUser> Users = new Dictionary<long, BotUser>();
        public static readonly Dictionary<long, List<Log>> Logs = new Dictionary<long, List<Log>>();
        static Handlers()
        {
            Directory.CreateDirectory(_folder);

            if (!File.Exists(_pathToUsersData))
                File.WriteAllText(_pathToUsersData, "{}");

            if (!File.Exists(_pathToLogs))
                File.WriteAllText(_pathToLogs, "{}");

            string jsonUsers = File.ReadAllText(_pathToUsersData);
            string jsonLogs = File.ReadAllText(_pathToLogs);

            Logs = JsonSerializer.Deserialize<Dictionary<long, List<Log>>>(jsonLogs)!;
            Users = JsonSerializer.Deserialize<Dictionary<long, BotUser>>(jsonUsers)!;

        }
        public static void SaveUsersData()
        {
            string jsonUsers = JsonSerializer.Serialize(Users);
            File.WriteAllText(_pathToUsersData, jsonUsers);
        }
        public static void SaveUsersLogs()
        {
            string jsonLogs = JsonSerializer.Serialize(Logs);
            File.WriteAllText(_pathToLogs, jsonLogs);
        }
        //public static string TrimLink(string link)
        //{
        //    var parsedLink = new Uri(link);
        //    return parsedLink.Host;
        //}
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

            if (!Logs.ContainsKey(userId))
                Logs[userId] = new List<Log>();
            var userLogs = Logs[userId];


            if (command == "/start")
            {
                if (!Users.ContainsKey(userId))
                {
                    Users[userId] = new BotUser();
                    SaveUsersData();
                    await bot.SendMessage(userId,
                        "You are registered now!\n\n" +
                        "This bot helps you track the status of your favorite links (websites).\n" +
                        "Click /manual to learn more"
                    );
                    return;
                }

                await bot.SendMessage(userId,
                        "This bot helps you track the status of your favorite links (websites).\n" +
                        "Click /manual to learn more"
                     );
            }
            else if (command == "/manual")
            {
                await bot.SendMessage(userId,
                    "Commands:\n" +
                    "/add <link> - Add a link to tracking\n" +
                    "/delete <link> - Remove a link from tracking\n" +
                    "/show - Show all tracked links\n" +
                    "/clear - Remove all tracked links\n" +
                    "/logs - Show logs of link status checks\n" +
                    "/deleteLog <index> - Delete a specific log entry by its index\n" +
                    "/clearLogs - Remove all log entries\n" +
                    "/subscribe - Subscribe to periodic status updates about your links\n" +
                    "/unsubscribe - Unsubscribe from periodic status updates about your links\n\n" +
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
                    userLogs.Add(new Log { Link = link, Status = response, Timestamp = DateTimeOffset.UtcNow });
                }
                SaveUsersLogs();
                await bot.SendMessage(userId, message.ToString());
            }
            else if (command == "/logs")
            {
                if (userLogs.Count == 0)
                {
                    await bot.SendMessage(userId, "No logs available.");
                    return;
                }
                StringBuilder message = new StringBuilder("Logs:\n");
                foreach (var log in userLogs)
                    message.AppendLine($"{log.Timestamp}: Link {log.Link} returned status {log.Status}");

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

                if (Users[userId].Links.Contains(userMessage))
                {
                    await bot.SendMessage(userId, "This link is already being tracked.");
                    return;
                }

                Users[userId].Links.Add(userMessage);
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

                if (!Users[userId].Links.Contains(userMessage))
                {
                    await bot.SendMessage(userId, "This link is not being tracked.");
                    return;
                }

                if (Users[userId].Links.Count == 0)
                {
                    await bot.SendMessage(userId, "No links are currently being tracked.");
                    return;
                }

                Users[userId].Links.Remove(userMessage);
                SaveUsersData();
                await bot.SendMessage(userId, "Link removed from tracking.");
            }
            else if (command == "/deleteLog")
            {
                if (userLogs.Count == 0)
                {
                    await bot.SendMessage(userId, "No logs available.");
                    return;
                }

                if (int.TryParse(userMessage, out int logIndex))
                {
                    if (logIndex < 1 || logIndex > userLogs.Count)
                    {
                        await bot.SendMessage(userId, "Invalid log index.");
                        return;
                    }
                    userLogs.RemoveAt(logIndex - 1);
                    SaveUsersLogs();
                    await bot.SendMessage(userId, "Log entry removed.");
                }
                else
                {
                    await bot.SendMessage(userId, "Please provide a valid log index to delete.");
                }
            }
            else if (command == "/clearLogs")
            {
                if (userLogs.Count == 0)
                {
                    await bot.SendMessage(userId, "No logs available.");
                    return;
                }
                userLogs.Clear();
                SaveUsersLogs();
                await bot.SendMessage(userId, "All log entries have been removed.");
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
                await bot.SendMessage(userId, "Unknown command. Please use /manual to see available commands.");
            }
        }
        public static async Task SendMessage(ITelegramBotClient bot, Dictionary<long, Dictionary<string, int>> responses)
        {
            bool hasIssues = responses.Values.Any(resp => resp.Values.Any(status => status == 0));
            if (!hasIssues) return;

            StringBuilder message = new StringBuilder("Something went wrong: \n");
            foreach (var response in responses.Values)
            {
                foreach (var linkStatus in response)
                {
                    if (linkStatus.Value == 0)
                        message.AppendLine($"Link {linkStatus.Key} is unreachable ❌");
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