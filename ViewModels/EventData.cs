using System;

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
