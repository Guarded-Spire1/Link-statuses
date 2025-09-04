using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Link_statuses
{
    public class Logs
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Link { get; set; }
        public int Status { get; set; }
    }
}
