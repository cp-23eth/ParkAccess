using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ParkAccess.ViewModels
{
    public class EventData
    {
        public string Name { get; set; }
        public string ParkingMail { get; set; }

        [JsonPropertyName("start")]
        public DateTime StartDateTime { get; set; }

        [JsonPropertyName("end")]
        public DateTime EndDateTime { get; set; }
    }
}
