using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgunTestingBot
{
    public class RoleSelector
    {
        public ulong storemessageid { get; set; }

        public List<Role> roles { get; set; }
        public List<ulong> ValidRolesIDs { get; set; }

        public ulong messagechannel { get; set; }

        public string title { get; set; } = "Server Roles";
        public string desc { get; set; } = "Select roles for you to have!";

        public RoleSelector()
        {
            storemessageid = 0;
            messagechannel = 0;
            roles = new List<Role>();
            ValidRolesIDs = new List<ulong>();
        }

        public RoleSelector(ulong smi)
        {
            storemessageid = smi;
            messagechannel = 0;
            roles = new List<Role>();
            ValidRolesIDs = new List<ulong>();
        }

        public RoleSelector(ulong smi, ulong mc)
        {
            storemessageid = smi;
            messagechannel = mc;
            roles = new List<Role>();
            ValidRolesIDs = new List<ulong>();
        }
    }
}
