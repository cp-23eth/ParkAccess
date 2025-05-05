using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkAccess.ViewModels
{
    public class History
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }

        public History(DateTime date, string description)
        {
            Date = date;
            Description = description;
        }
    }
}
