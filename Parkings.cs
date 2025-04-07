using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkAccess
{
    public class Parking
    {
        public string Nom { get; set; }
        public string Mail { get; set; }
        public string Ceff { get; set; }
        public string Ip { get; set; }
        public Parking(string nom, string mail, string ceff, string ip)
        {
            Nom = nom;
            Mail = mail;
            Ceff = ceff;
            Ip = ip;
        }
    }
}
