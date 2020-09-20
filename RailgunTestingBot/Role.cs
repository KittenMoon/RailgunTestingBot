using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgunTestingBot
{
    public class Role
    {
        public string ValueName { get; set; }
        public string Emote { get; set; }
        public bool DefaultEmoji { get; set; }

        public string RoleRealName { get; set; }

        public ulong roleID { get; set; }
      
        public Role()
        {
            ValueName = "";
            Emote = "";
            DefaultEmoji = true;
            RoleRealName = "";
        }

        public Role(string v, string e)
        {
            ValueName = v;
            RoleRealName = v;
            Emote = e;
            DefaultEmoji = true;
        }

        public Role(string v, string e, bool def)
        {
            ValueName = v;
            RoleRealName = v;
            Emote = e;
            DefaultEmoji = def;
        }

        public bool EmoteCheckCompare(string e)
        {
            string en = Emote;
            if (!DefaultEmoji)
            {
                if (en == e)
                    return e == en;

                if (en.IndexOf(":") == -1)
                    DefaultEmoji = true;
                else
                {
                    string etemp = Emote.Substring(2);

                    en = etemp.Substring(0, etemp.IndexOf(":"));
                }
            }

            return e == en;
        }

    public string NameFromEmojiString(string e)
        {
            string en = Emote;
            if (!DefaultEmoji)
            {
                if (en == e)
                    return ValueName;

                if (en.IndexOf(":") == -1)
                    DefaultEmoji = true;
                else
                {
                    string etemp = Emote.Substring(2);

                    en = etemp.Substring(0, etemp.IndexOf(":"));
                }
            }

            if (e == en)
                return ValueName;
            else
                return "";
        }
    }
}
