using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Link_statuses
{
    public class BotUser
    {
        public string Status { get; set; } = "idle";
        public List<string> Links { get; set; } = new List<string>();
        public bool ReceiveBroadcast { get; set; } = true;
    }
}
