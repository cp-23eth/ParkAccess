using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ParkAccess
{
    public class Parking
    {
        [JsonPropertyName("nom")]
        public string Nom { get; set; }

        [JsonPropertyName("mail")]
        public string Mail { get; set; }

        [JsonPropertyName("ceff")]
        public string Ceff { get; set; }

        [JsonPropertyName("ip")]
        public string Ip { get; set; }

        public Parking() { }

        public Parking(string nom, string mail, string ceff, string ip)
        {
            Nom = nom;
            Mail = mail;
            Ceff = ceff;
            Ip = ip;
        }

        public override string ToString()
        {
            return $"Nom: {Nom}, Mail: {Mail}, Ceff: {Ceff}, Ip: {Ip}";
        }
    }
}
