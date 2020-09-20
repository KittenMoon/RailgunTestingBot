using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgunTestingBot
{
    public class GuildInfo
    {
        public ulong guildid { get; set; }

        public List<RoleSelector> rs { get; set; }

        public List<ulong> botonlychan { get; set; }

        public GuildInfo()
        {
            guildid = 0;
            rs = new List<RoleSelector>();
            botonlychan = new List<ulong>();
        }

        public GuildInfo(ulong g)
        {
            guildid = g;
            rs = new List<RoleSelector>();
            botonlychan = new List<ulong>();
        }
    }
}
