using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logwire.Agent.Workstation.Win.Models
{
    class LockModel
    {
        public string event_type = "LOCK";
        public string machine_name { get; set; }
        public string username { get; set; }
        public string time { get; set; }
    }
}
