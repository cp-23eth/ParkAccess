using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ParkAccess
{
    public class EventData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; }
        public string ParkingMail { get; set; }
        public string ParkingIp { get; set; } = "";
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
    }
}
