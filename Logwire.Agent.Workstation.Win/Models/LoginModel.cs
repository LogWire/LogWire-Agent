using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logwire.Agent.Workstation.Win.Models
{
    class LoginModel
    {
        public string event_type = "LOGIN";
        public string machine_name { get; set; }
        public string username { get; set; }
        public string time { get; set; }
        public string ip { get; set; }
    }
}
