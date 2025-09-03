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
        private readonly static string _folder;
        private readonly static string _pathToUsers;
        private readonly static string _pathToUsersStates;
        private readonly static string _pathToUrls;

        private static string? jsonUsers;
        private static string? jsonUserStates;
        private static string? jsonUsersUrls;

        public static Dictionary<long, string> _users;
        public static Dictionary<long, string> _userStates;
        public static List<string>? _usersUrls;
        public static Dictionary<long, Dictionary<string, int>> _usersData;

        static Handlers()
        {

            _folder = Path.Combine(AppContext.BaseDirectory, "UsersData");

            _pathToUrls = Path.Combine(_folder, "urls.json");
            _pathToUsersStates = Path.Combine(_folder, "userStates.json");
            _pathToUsers = Path.Combine(_folder, "users.json");

            Directory.CreateDirectory(_folder);

            var files = Directory.GetFiles(_folder);
            var requiredFiles = new List<string> { _pathToUrls, _pathToUsersStates, _pathToUsers };
            foreach (var file in requiredFiles)
            {
                if (!files.Contains(file))
                {
                    File.WriteAllText(file, "[]");
                }
            }

            jsonUsers = File.ReadAllText(_pathToUsers);
            jsonUserStates = File.ReadAllText(_pathToUsersStates);
            jsonUsersUrls = File.ReadAllText(_pathToUrls);

            _usersUrls = JsonSerializer.Deserialize<List<string>>(jsonUsersUrls) ?? new List<string>();
            _userStates = JsonSerializer.Deserialize<Dictionary<long, string>>(jsonUserStates) ?? new Dictionary<long, string>();
            _users = JsonSerializer.Deserialize<Dictionary<long, string>>(jsonUsers) ?? new Dictionary<long, string>();
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
        private static async Task AddLinkToTracking(ITelegramBotClient bot, Update update)
        {
            jsonUsersUrls = File.ReadAllText(_pathToUrls);
            _usersUrls = JsonSerializer.Deserialize<List<string>>(jsonUsersUrls);
            if (_usersUrls != null && !_usersUrls.Contains(TrimLink(update.Message.Text)))
            {
                _usersUrls.Add(TrimLink(update.Message.Text));
                var newJson = JsonSerializer.Serialize(_usersUrls);
                File.WriteAllText(_pathToUrls, newJson);
                await bot.SendMessage(update.Message.Chat.Id, "Link added to trackings");
            }
            else { await bot.SendMessage(update.Message.Chat.Id, "Link is already tracked"); }

        }
        private static async Task DeleteLinkFromTracking(ITelegramBotClient bot, Update update)
        {
            if (!string.IsNullOrEmpty(update.Message.Text) && _usersUrls.Contains(TrimLink(update.Message.Text)))
            {
                _usersUrls.Remove(TrimLink(update.Message.Text));
                var newJson = JsonSerializer.Serialize(_usersUrls);
                File.WriteAllText(_pathToUrls, newJson);
                await bot.SendMessage(update.Message.Chat.Id, "Link removed from trackings");
            }
            else { await bot.SendMessage(update.Message.Chat.Id, "Invalid link, try again"); }
        }

        public static async Task MessageHandle(ITelegramBotClient bot, Update update)
        {
            if (update.Message.Text == "/start")
            {
                if (!_users.ContainsKey(update.Message.Chat.Id))
                    _users.Add(update.Message.Chat.Id, update.Message.Chat.Username);
                await bot.SendMessage(update.Message.Chat.Id, "Welcome to Link Status Bot!\nUse /show to see tracked links\nUse /add to add new link\nUse /delete to delete link from trackings\nUse /clear to delete all links from trackings");
            }
            else if (update.Message.Text == "/show")
            {
                if (_usersUrls != null && _usersUrls.Count > 0)
                    await bot.SendMessage(update.Message.Chat.Id, $"Currently tracked links:\n{string.Join("\n", _usersUrls)}");

                else
                    await bot.SendMessage(update.Message.Chat.Id, "No links are currently being tracked.");

            }
            else if (update.Message.Text == "/add")
            {
                _userStates[update.Message.Chat.Id] = "add_link";
                await bot.SendMessage(update.Message.Chat.Id, "Send link to track");
                return;

            }
            else if (update.Message.Text == "/delete")
            {
                if (_usersUrls == null || _usersUrls.Count <= 0)
                {
                    await bot.SendMessage(update.Message.Chat.Id, "No links are currently being tracked.");
                    return;
                }

                _userStates[update.Message.Chat.Id] = "delete_link";
                await bot.SendMessage(update.Message.Chat.Id, $"Currently tracked links:\n{string.Join("\n", _usersUrls)}");
                await bot.SendMessage(update.Message.Chat.Id, "Choose link to delete");
                return;
            }
            else if (update.Message.Text == "/clear")
            {
                if (_usersUrls == null || _usersUrls.Count <= 0)
                {
                    await bot.SendMessage(update.Message.Chat.Id, "No links are currently being tracked.");
                    return;
                }
                _usersUrls.Clear();
                var newJson = JsonSerializer.Serialize(_usersUrls);
                File.WriteAllText(_pathToUrls, newJson);
                await bot.SendMessage(update.Message.Chat.Id, "All links removed");
            }


            if (_userStates.ContainsKey(update.Message.Chat.Id) && _userStates[update.Message.Chat.Id] == "add_link")
            {
                if (IsValidUrl(update.Message.Text))
                {
                    await AddLinkToTracking(bot, update);
                    _userStates.Remove(update.Message.Chat.Id);
                }
                else { await bot.SendMessage(update.Message.Chat.Id, "Invalid link, try again:"); }
            }
            else if (_userStates.ContainsKey(update.Message.Chat.Id) && _userStates[update.Message.Chat.Id] == "delete_link")
            {
                if (IsValidUrl(update.Message.Text))
                {
                    await DeleteLinkFromTracking(bot, update);
                    _userStates.Remove(update.Message.Chat.Id);
                }
                else { await bot.SendMessage(update.Message.Chat.Id, "Invalid link, try again:"); }
            }
        }

        public static async Task SendMessage(ITelegramBotClient bot, Dictionary<string, int> responses)
        {
            StringBuilder message = new StringBuilder("Currently tracked links statuses: \n");
            foreach (var response in responses)
            {
                if (response.Value == 0)
                    message.AppendLine($"Link {response.Key} is unreachable ❌");
                else
                    message.AppendLine($"Link {response.Key} is reachable ✅");
            }
            foreach (var id in _users)
            {
                await bot.SendMessage(id.Key, message.ToString());
            }
        }

    }
}