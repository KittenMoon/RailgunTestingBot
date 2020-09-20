using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgunTestingBot
{
    public class BotSavedInfo
    {
        public List<GuildInfo> GGs { get; set; }
        public string auth { get; set; }

        public BotSavedInfo()
        {
            
            GGs = new List<GuildInfo>();
        }
    }
}
