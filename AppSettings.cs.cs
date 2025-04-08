using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkAccess
{
    public class ApiSettings
    {
        public string Key { get; set; }
        public string BaseUrl { get; set; }
    }
    class AppSettings
    {
        public ApiSettings Api { get; set; }
    }
}
