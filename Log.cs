using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Link_statuses
{
    public class Log
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Link { get; set; }
        public int Status { get; set; }
    }
}
