using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RailgunTestingBot
{
    class Program
    {
        public static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private bool first = true;
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        private string folder = @"D:\One Drive\OneDrive - CenterLynx Corporation\Programs Storage\File Storage";
        private string folder2 = @"C:\Users\srgri\OneDrive - CenterLynx Corporation\Programs Storage\File Storage";

        private BotSavedInfo botssaves;

        NotifyIcon n;

        private List<Role> rolesd;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;
            _client.MessageReceived += MessageReceived;
            _client.ReactionAdded += ReactTimeAdd;
            _client.ReactionRemoved += ReactTimeDelete;

            //_client.MessageUpdated += MessageUpdate;


            // Remember to keep token private or to read it from an 
            // external source! In this case, we are reading the token 
            // from an environment variable. If you do not know how to set-up
            // environment variables, you may find more information on the 
            // Internet or by using other methods such as reading from 
            // a configuration.
            
            if (first)
            {
                var handle = GetConsoleWindow();
                //ShowWindow(handle, SW_HIDE);

                n = new NotifyIcon();
                n.Icon = Resources.weather;
                n.Text = "Railgun Running (Click for Console)";
                n.Visible = true;

                //await Application.Run();

                botssaves = Load();

                first = false;
            }

            await _client.LoginAsync(TokenType.Bot, botssaves.auth);
            await _client.StartAsync();

            _client.GuildAvailable += TestGUI;

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private void Save()
        {
            string thisFolder = "";
            FileInfo testfile;

            String s = JsonConvert.SerializeObject(botssaves);
            StreamWriter sw = null;

            try
            {
                if (sw != null)
                    sw.Close();

                DirectoryInfo dinfo = null;
                try
                {
                    dinfo = new DirectoryInfo(folder);
                    testfile = dinfo.GetFiles("BotInfo_Railgun.txt").ToList()[0];
                    thisFolder = folder;
                }
                catch
                {
                    dinfo = new DirectoryInfo(folder2);
                    testfile = dinfo.GetFiles("BotInfo_Railgun.txt").ToList()[0];
                    thisFolder = folder2;
                }

                using (sw = new StreamWriter(thisFolder + "\\BotInfo_Railgun.txt"))
                {
                    sw.WriteLine(s);
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (sw != null)
                    sw.Close();
            }
        }

        private BotSavedInfo Load()
        {
            DirectoryInfo dinfo = null;
            FileInfo file = null;
            try
            {
                dinfo = new DirectoryInfo(folder);
                file = dinfo.GetFiles("BotInfo_Railgun.txt").ToList()[0];
            }
            catch
            {
                dinfo = new DirectoryInfo(folder2);
                file = dinfo.GetFiles("BotInfo_Railgun.txt").ToList()[0];
            }
            
            StreamReader fileInfo = file.OpenText();
            string fileInfoString = fileInfo.ReadToEnd();

            var resp = fileInfoString;

            BotSavedInfo response = null;
            response = JsonConvert.DeserializeObject<BotSavedInfo>(resp);

            fileInfo.Close();

            if (response == null)
                response = new BotSavedInfo();

            return response;
        }

        private async Task TestGUI(SocketGuild guil)
        {
            GuildInfo bsu = null;

            if(botssaves.GGs.Where(d => d.guildid == guil.Id).ToList().Count == 0)
            {
                bsu = new GuildInfo(guil.Id);
                botssaves.GGs.Add(bsu);
            }
            else
            {
                bsu = botssaves.GGs.Where(d => d.guildid == guil.Id).ToList()[0];
            }

            for (int i = 0; i < bsu.rs.Count; i++)
            {
                RoleSelector rsm = bsu.rs[i];
                var iso = guil.Channels.Where(c => c.Id == rsm.messagechannel).ToList();

                if (iso.Count > 0)
                {
                    SocketTextChannel itso = iso[0] as SocketTextChannel;

                    ulong idd = rsm.storemessageid;

                    if (idd == 0)
                        continue;

                    RestUserMessage sm = await itso.GetMessageAsync(idd) as RestUserMessage;

                    await LoadTheList(sm, rsm);
                }
            }
            
            Save();
        }

        private async Task LoadTheList(RestUserMessage sm, RoleSelector rsm)
        {
            await sm.ModifyAsync(msg => msg.Embed = MakeEmbed(rsm).Build());
            await CheckReactions(sm, rsm);
        }

        private async Task CheckReactions(RestUserMessage sm, RoleSelector rsm, bool dupchecker = false)
        {
            for (int i = 0; i < sm.Reactions.Count; i++)
            {
                var rd = sm.Reactions.ElementAt(i);

                var t = await sm.GetReactionUsersAsync(rd.Key, 100).FlattenAsync();

                if (!rd.Value.IsMe)
                {
                    foreach (var bb in t)
                    {
                        await sm.RemoveReactionAsync(rd.Key, bb);
                    }
                }
                else if (!dupchecker)
                {
                    var p = rsm.roles.Where(s => s.EmoteCheckCompare(rd.Key.Name)).ToList();
                    if (p.Count == 0)
                    {
                        foreach (var bb in t)
                        {
                            await sm.RemoveReactionAsync(rd.Key, bb);
                        }
                    }
                }
            }

            if (!dupchecker)
                await GetEmotes(sm, rsm);
        }

        private async Task ReactTimeAdd(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction message)
        {
            await RoleChanges(message, true);
        }

        private async Task ReactTimeDelete(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction message)
        {
            await RoleChanges(message, false);
        }
       

        private async Task RoleChanges(SocketReaction message, bool add)
        {
            var user = message.User.Value as SocketGuildUser;
            IGuild gg = (user as IGuildUser).Guild;
            IChannel chai = message.Channel;

            GuildInfo bsu = GetBSUFromID(gg.Id);
            RoleSelector rsMain = GetRoleSelector(bsu, chai.Id);

            ulong messageSelectorID = rsMain.storemessageid;

            if (message.MessageId != messageSelectorID)
                return;
            
            if (add)
            {
                if (message.User.Value.IsBot)
                    return;

                RestUserMessage sm = await message.Channel.GetMessageAsync(messageSelectorID) as RestUserMessage;
                await CheckReactions(sm, rsMain, true);

                Role r = FindRole(message, rsMain);

                if (r != null)
                {
                    var role = gg.GetRole(r.roleID);

                    if (role == null)
                        role = gg.Roles.FirstOrDefault(x => x.Name == r.ValueName);

                    if (role == null)
                        role = await gg.CreateRoleAsync(r.ValueName, null, null, false, null);
                    
                    if (!user.Roles.Contains(role))
                        await user.AddRoleAsync(role);
                }
            }
            else
            {
                if (message.User.Value.IsBot)
                {
                    if (lastdead == null || !lastdead.EmoteCheckCompare(message.Emote.Name))
                        return;

                    var role = gg.GetRole(lastdead.roleID);

                    if (role == null)
                        role = gg.Roles.FirstOrDefault(x => x.Name == lastdead.ValueName);
                    
                    if (role == null)
                        return;
                    
                    for (int i = 0; i < gg.Roles.Count; i++)
                    {
                        var arbor = gg.Roles.ElementAt(i);

                        if(arbor == role)
                            await arbor.DeleteAsync();
                    }

                    lastdead = null;
                }

                Role r = FindRole(message, rsMain);
                
                if (r != null)
                {
                    var role = gg.GetRole(r.roleID);

                    if (role == null)
                        role = gg.Roles.FirstOrDefault(x => x.Name == r.ValueName);

                    if (role == null)
                        role = await gg.CreateRoleAsync(r.ValueName, null, null, false, null);

                    if (user.Roles.Contains(role))
                        await user.RemoveRoleAsync(role);
                }
            }
        }

        private Role FindRole(SocketReaction message, RoleSelector rsm)
        {
            Role r = null;
            for (int i = 0; i < rsm.roles.Count; i++)
            {
                if (rsm.roles[i].NameFromEmojiString(message.Emote.Name) != "")
                {
                    r = rsm.roles[i];
                    break;
                }
            }
            return r;
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private ulong setupID = 0;

        private async Task MessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot)
                return;

            await CheckMessageTask(message);
        }

        Role lastdead = null;

        private async Task CheckMessageTask(SocketMessage message)
        {
            SocketGuildUser sg = await (message.Channel as IChannel).GetUserAsync(message.Author.Id) as SocketGuildUser;

            IChannel chani = message.Channel;
            IGuild guil = sg.Guild;

            GuildInfo thisGuildInfo = GetBSUFromID(guil.Id);
            RoleSelector rsMain = GetRoleSelector(thisGuildInfo, chani.Id);

            ulong messageSelectorID = rsMain.storemessageid;

            ulong botOnlyID = thisGuildInfo.botonlychan.FirstOrDefault(x => x == chani.Id);

            string messages = message.Content;

            if (botOnlyID != 0 && messages.ToLower() != "delete bot only")
            {
                if (message.Attachments.Count > 0)
                {
                    for (int i = 0; i < message.Attachments.Count; i++)
                    {
                        await message.Channel.SendMessageAsync(message.Attachments.ElementAt(i).Url);
                    }
                }
                else
                    await message.Channel.SendMessageAsync(messages);

                await message.DeleteAsync();
            }

            if (!CheckVaildRoles(rsMain, message.Author))
            {
                if (guil.OwnerId != message.Author.Id)
                    return;
            }
            
            if (messages.ToLower() == "setup")
            {
                await message.DeleteAsync();

                await DeleteOld(chani, rsMain);

                EmbedBuilder builder = MakeEmbed(rsMain);
                
                RestUserMessage t = await message.Channel.SendMessageAsync("", false, builder.Build());
                
                await GetEmotes(t, rsMain);

                rsMain.storemessageid = t.Id;
                
                Save();
            }
            else if (messages.ToLower() == "make bot only")
            {
                await message.DeleteAsync();
                
                if (botOnlyID != 0)
                    return;

                thisGuildInfo.botonlychan.Add(chani.Id);

                Save();
            }
            else if (messages.ToLower() == "delete bot only")
            {
                await message.DeleteAsync();

                if (botOnlyID == 0)
                    return;

                thisGuildInfo.botonlychan.Remove(botOnlyID);

                Save();
            }
            else if (messages.ToLower() == "clearset")
            {
                await message.DeleteAsync();

                await DeleteOld(chani, rsMain);
                thisGuildInfo.rs.Remove(rsMain);
                    
                Save();
            }
            else if (messages.Length > 11 && messages.ToLower().Substring(0, 11) == "deleteemote")
            {
                await message.DeleteAsync();
                string em = messages.Substring(12).Trim();

                Role deleter = null;
                for (int i = 0; i < rsMain.roles.Count; i++)
                {
                    if (rsMain.roles[i].NameFromEmojiString(em) != "")
                    {
                        deleter = rsMain.roles[i];
                        break;
                    }
                }

                rsMain.roles.Remove(deleter);

                Save();

                RestUserMessage sm = await message.Channel.GetMessageAsync(messageSelectorID) as RestUserMessage;

                await CheckReactions(sm, rsMain);
                await sm.ModifyAsync(msg => msg.Embed = MakeEmbed(rsMain).Build());
            }
            else if (messages.Length > 10 && messages.ToLower().Substring(0, 10) == "deleterole")
            {
                await message.DeleteAsync();
                string em = messages.Substring(11).Trim();

                Role deleter = null;
                for (int i = 0; i < rsMain.roles.Count; i++)
                {
                    if (rsMain.roles[i].NameFromEmojiString(em) != "")
                    {
                        deleter = rsMain.roles[i];
                        break;
                    }
                }

                lastdead = deleter;

                rsMain.roles.Remove(deleter);

                Save();

                RestUserMessage sm = await message.Channel.GetMessageAsync(messageSelectorID) as RestUserMessage;

                await CheckReactions(sm, rsMain);
                await sm.ModifyAsync(msg => msg.Embed = MakeEmbed(rsMain).Build());
            }
            else if (messages.Length > 8 && messages.ToLower().Substring(0, 8) == "editrole")
            {
                await message.DeleteAsync();
                string em = messages.Substring(9).Trim();

                IRole role = null;
                if (em.IndexOf("<") >= 0)
                {
                    string drrule = em.Substring(3);
                    drrule = drrule.Substring(0, drrule.IndexOf(">"));

                    role = guil.GetRole(Convert.ToUInt64(drrule));

                    if (role == null)
                        return;

                    if (rsMain.roles.Where(x => x.roleID == Convert.ToUInt64(drrule)).ToList().Count == 0)
                        return;

                    Role eee = rsMain.roles.Where(x => x.roleID == Convert.ToUInt64(drrule)).ToList()[0];

                    string[] ss2 = em.Split(' ');

                    ss2[0] = "";
                    string newName = string.Join(" ", ss2).Trim();

                    if (newName == "")
                        return;

                    eee.ValueName = newName;
                }

                Save();

                RestUserMessage sm = await message.Channel.GetMessageAsync(messageSelectorID) as RestUserMessage;

                await CheckReactions(sm, rsMain);
                await sm.ModifyAsync(msg => msg.Embed = MakeEmbed(rsMain).Build());
            }
            else if (messages.Length > 8 && messages.ToLower().Substring(0, 8) == "addemote")
            {
                await message.DeleteAsync();
                string m2 = messages.Substring(9);

                string[] ss = m2.Split(' ');

                string em = ss[0];
                ss[0] = "";
                string rulename = string.Join(" ", ss).Trim();

                if (rulename.Length <= 0)
                    return;

                if (em.Length >= 4)
                {
                    Emote e = null;
                    if (Emote.TryParse(em, out var emote))
                    {
                        e = emote;
                    }

                    if (e == null)
                        return;
                    else if (guil.Emotes.Where(x => x.Id == e.Id).ToList().Count == 0)
                        return;
                }

                IRole role = null;
                if (rulename.IndexOf("<") >= 0)
                {
                    string drrule = rulename.Substring(3);
                    drrule = drrule.Substring(0, drrule.IndexOf(">"));

                    role = guil.GetRole(Convert.ToUInt64(drrule));

                    string[] ss2 = rulename.Split(' ');
                    
                    ss2[0] = "";
                    string newName = string.Join(" ", ss2).Trim();

                    rulename = newName == "" ? role.Name : newName;
                }
                
                Role rboi = new Role(rulename, em, em.Length == 2);

                if (role == null)
                    role = guil.Roles.FirstOrDefault(x => x.Name == rulename);

                if (role == null)
                    role = await guil.CreateRoleAsync(rulename, null, null, false, null);

                rboi.roleID = role.Id;
                rboi.RoleRealName = role.Name;

                rsMain.roles.Add(rboi);

                Save();

                RestUserMessage sm = await message.Channel.GetMessageAsync(messageSelectorID) as RestUserMessage;

                await sm.ModifyAsync(msg => msg.Embed = MakeEmbed(rsMain).Build());
                await GetEmotes(sm, rsMain);
            }
            else if (messages.Length > 8 && messages.ToLower().Substring(0, 8) == "addvalid")
            {
                await message.DeleteAsync();
                string rle = messages.Substring(9).Trim();

                if (rle.IndexOf("<") == -1 || rle.IndexOf(">") == -1)
                    return;

                rle = rle.Substring(3);
                rle = rle.Substring(0, rle.IndexOf(">"));

                IRole irsf = CheckIfRole(rle, guil);

                if (irsf == null)
                    return;

                rsMain.ValidRolesIDs.Add(irsf.Id);

                Save();
            }
            else if (messages.Length > 11 && messages.ToLower().Substring(0, 11) == "removevalid")
            {
                await message.DeleteAsync();
                string rle = messages.Substring(12).Trim();

                if (rle.IndexOf("<") == -1 || rle.IndexOf(">") == -1)
                    return;

                rle = rle.Substring(3);
                rle = rle.Substring(0, rle.IndexOf(">"));

                IRole irsf = CheckIfRole(rle, guil);

                if (irsf == null)
                    return;

                rsMain.ValidRolesIDs.Add(irsf.Id);

                Save();
            }
            else if (messages.Length > 9 && messages.ToLower().Substring(0, 9) == "edittitle")
            {
                await message.DeleteAsync();
                string rle = messages.Substring(10).Trim();

                if (rle == "")
                    return;

                rsMain.title = rle;

                Save();

                RestUserMessage sm = await message.Channel.GetMessageAsync(messageSelectorID) as RestUserMessage;

                await sm.ModifyAsync(msg => msg.Embed = MakeEmbed(rsMain).Build());
            }
            else if (messages.Length > 8 && messages.ToLower().Substring(0, 8) == "editdesc")
            {
                await message.DeleteAsync();
                string rle = messages.Substring(9).Trim();

                if (rle == "")
                    return;

                rsMain.desc = rle;

                Save();

                RestUserMessage sm = await message.Channel.GetMessageAsync(messageSelectorID) as RestUserMessage;

                await sm.ModifyAsync(msg => msg.Embed = MakeEmbed(rsMain).Build());
            }
        }

        private RoleSelector GetRoleSelector(GuildInfo thisGuildInfo, ulong id)
        {
            if (thisGuildInfo.rs.Where(d => d.messagechannel == id).ToList().Count > 0)
            {
                return thisGuildInfo.rs.Where(d => d.messagechannel == id).ToList()[0];
            }
            else
            {
                RoleSelector rsnew = new RoleSelector(0, id);
                thisGuildInfo.rs.Add(rsnew);
                return rsnew;
            }
        }

        private IRole CheckIfRole(string rle, IGuild guil)
        {
            for (int i = 0; i < guil.Roles.Count; i++)
            {
                IRole r = guil.Roles.ElementAt(i);

                if (r.Id == Convert.ToUInt64(rle))
                    return r;
            }
            return null;
        }

        private bool CheckVaildRoles(RoleSelector rsm, SocketUser author)
        {
            SocketGuildUser scgo = author as SocketGuildUser;

            for (int i = 0; i < rsm.ValidRolesIDs.Count; i++)
            {
                ulong br = rsm.ValidRolesIDs[i];

                if (scgo.Roles.Where(d => d.Id == br).ToList().Count > 0)
                    return true;
            }

            return false;
        }

        private GuildInfo GetBSUFromID(ulong id)
        {
            if (botssaves.GGs.Where(d => d.guildid == id).ToList().Count > 0)
            {
                 return botssaves.GGs.Where(d => d.guildid == id).ToList()[0];
            }
            else
            {
                GuildInfo bnew = new GuildInfo(id);
                botssaves.GGs.Add(bnew);

                return bnew;
            }
        }

        private async Task DeleteOld(IChannel chano, RoleSelector rsm)
        {
            RestUserMessage sm = null;

            SocketTextChannel itso = chano as SocketTextChannel;

            ulong idd = rsm.storemessageid;

            if (idd > 0)
            {
                sm = await itso.GetMessageAsync(idd) as RestUserMessage;

                if (sm != null)
                {
                    await sm.DeleteAsync();
                }
            }
        }
        private EmbedBuilder MakeEmbed(RoleSelector rsm)
        {
            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle(rsm.title);

            if (rsm.roles.Count == 0)
                builder.WithDescription("Add Some Roles for Users to Pick From!");
            else
                builder.WithDescription(rsm.desc);

            for (int i = 0; i < rsm.roles.Count; i++)
            {
                builder.AddField(rsm.roles[i].ValueName + " - @" + rsm.roles[i].RoleRealName, rsm.roles[i].Emote, false);
            }

            builder.WithColor(Color.Green);

            return builder;
        }

        private async Task GetEmotes(RestUserMessage t, RoleSelector rsm)
        {
            int inc = -1;
            for (int i = 0; i < rsm.roles.Count; i++)
            {
                inc++;

                if (rsm.roles[inc].DefaultEmoji)
                {
                    Emoji e = new Emoji(rsm.roles[inc].Emote);

                    if (t.Reactions.Where(f => f.Key.Name == e.Name).ToList().Count == 0)
                        await t.AddReactionAsync(e);
                }
                else
                {
                    if (Emote.TryParse(rsm.roles[inc].Emote, out var emote))
                    {
                        if (t.Reactions.Where(f => f.Key.Name == emote.Name).ToList().Count == 0)
                            await t.AddReactionAsync(emote);
                    }
                }
            }
        }
    }
}
