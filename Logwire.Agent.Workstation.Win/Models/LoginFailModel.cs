using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logwire.Agent.Workstation.Win.Models
{
    class LoginFailModel
    {
        public string event_type = "LOGIN_FAIL";
        public string username { get; set; }
        public string machine_name { get; set; }
        public string time { get; set; }
    }
}
