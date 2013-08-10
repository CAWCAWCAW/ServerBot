using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Linq;
using System.Text;
using System.Data;
using System.Net;
using System.IO;
using System;
using MySql.Data.MySqlClient;
using Mono.Data.Sqlite;
using MySql.Web;
using TShockAPI.DB;
using TShockAPI;
using Terraria;
using Hooks;

namespace ServerBot
{
    public class BComs
    {
        #region BotMethod
        public static void BotMethod(CommandArgs z)
        {
            string name = "";
            int id = 0;

            #region Invalid Syntax
            if (z.Parameters.Count < 1)
            {
                z.Player.SendWarningMessage("Invalid syntax.");
                z.Player.SendWarningMessage("Valid options: //bot join <name>, //bot list");
                z.Player.SendWarningMessage("//bot leave <name || ID>, //bot kill <name || ID>, //bot killall");
                z.Player.SendWarningMessage("//bot say <botname> <message, //bot asay <botname> <message> <interval>, //bot mclear <botname>");
            }
            #endregion

            #region Bot.Join, Bot.Leave
            else if (z.Parameters[0] == "join")
            {
                string plyname = z.Player.Name;
                name = z.Parameters[1].ToString();
                id = BotMain.rid.Next(1, 256);
                foreach (Bot b in BotMain.bots)
                {
                    if (id == b.Index)
                    {
                        id = BotMain.rid.Next(1, 256);
                    }
                }
                BotMain.bots.Add(new Bot(id, name));
                TSPlayer.All.SendInfoMessage(string.Format("Bot '{0}' was summoned by {1}", name, plyname));
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("Bot '{0}' was summoned by {1}", name, plyname));
                Console.ResetColor();
                Log.Warn(string.Format("{0} made bot '{1}' join", plyname, name));
            }
            else if (z.Parameters[0] == "leave")
            {
                TSPlayer.All.SendWarningMessage(string.Format("{0} made bot {1} leave.", z.Player.Name, name));
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("{0} made bot '{1}' leave.", z.Player.Name, name));
                Console.ResetColor();
                foreach (Bot b in BotMain.bots)
                {
                    if (b.Name == z.Parameters[1])
                    {
                        BotMain.bots.RemoveAll(b1 => b1.Name == z.Parameters[1]);
                    }
                }
            }


            #endregion

            #region Bot.Colour + Bot.Talk
            else if (z.Parameters[0] == "colour" || z.Parameters[0] == "color")
            {
                foreach (Bot b in BotMain.bots)
                {
                    if (b.Name == z.Parameters[1].ToString())
                    {
                        b.r = Convert.ToByte(z.Parameters[2]);
                        b.g = Convert.ToByte(z.Parameters[3]);
                        b.b = Convert.ToByte(z.Parameters[4]);
                        z.Player.SendSuccessMessage(string.Format("Set bot {3}'s chat colo(u)r to {0}, {1}, {2}", b.r, b.g, b.b, b.Name));
                    }
                    else
                    {
                        z.Player.SendWarningMessage(string.Format("Bot '{0}' does not exist", z.Parameters[1]));
                    }
                }
            }
            else if (z.Parameters[0] == "say")
            {
                string text = z.Parameters[2];
                foreach (Bot b in BotMain.bots)
                {
                    if (b.Name == z.Parameters[1].ToString())
                    {
                        string message = string.Format("{0}: {1}", b.Name, text);

                        TSPlayer.All.SendMessage(message, b.r, b.g, b.b);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(string.Format("{0}: {1}", b.Name, text));
                        if (text.StartsWith("/"))
                        {
                            Commands.HandleCommand(z.Player, text);
                            Console.ResetColor();
                            return;
                        }
                        Console.ResetColor();
                    }
                }
            }
            #endregion

            #region Bot.List
            else if (z.Parameters[0] == "list")
            {
                List<string> botList = new List<string>();
                string botString = "";
                foreach (Bot b in BotMain.bots)
                {
                    botList.Add(b.Name);
                }
                foreach (string s in botList)
                {
                    if (botString.Length == 0)
                    {
                        botString += s;
                    }
                    else if (botString.Length > 0)
                    {
                        botString += ", " + s;
                    }
                }
                if (botString.Length == 0)
                {
                    z.Player.SendInfoMessage("There are currently no bots online.");
                }
                else
                {
                    z.Player.SendInfoMessage("Current bots: " + botString);
                }
            }
            #endregion

            #region Bot.Kill
            else if (z.Parameters[0] == "kill")
            {
                foreach (Bot b in BotMain.bots)
                {
                    if (b.Name == z.Parameters[1])
                    {
                        TSServerPlayer.All.SendWarningMessage("BANG!");
                        TSServerPlayer.All.SendSuccessMessage(string.Format("{0} killed bot {1}", z.Player.Name, b.Name));
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("BANG!");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(string.Format("{0} killed bot {1}", z.Player.Name, b.Name));
                        Console.ResetColor();
                        BotMain.bots.RemoveAll(b1 => b1.Name.ToLower() == z.Parameters[1].ToLower());
                    }
                    else
                    {
                        z.Player.SendWarningMessage("Could not find bot '" + z.Parameters[1] + "'");
                    }
                }
            }

            else if (z.Parameters[0] == "killall")
            {
                BotMain.bots.Clear();
                TSPlayer.All.SendWarningMessage("Bot genocide.");
                TSPlayer.All.SendWarningMessage("They all died.");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Bot genocide. There we no survivors.");
                Console.ResetColor();
            }
            #endregion

            #region Bot.AutoSay
            else if (z.Parameters[0] == "asay")
            {
                foreach (Bot b in BotMain.bots)
                {
                    if (b.Name == z.Parameters[1])
                    {
                        b.Msgtime = Convert.ToInt32(z.Parameters[3]);
                        b.Message = string.Format("{0}: {1}", b.Name, z.Parameters[2]);
                        z.Player.SendSuccessMessage(string.Format("Bot '{0}' will now broadcast your message every {1} minute(s).", b.Name, b.Msgtime));
                        b.Type = "asay";
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(string.Format("{0} set bot '{1}' to broadcast a message every {2} minute(s)", z.Player.Name, b.Name, b.Msgtime));
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(string.Format("Message: {0}", b.Message));
                        Console.ResetColor();
                    }
                }        
            }
            else if (z.Parameters[0] == "mclear")
            {
                foreach (Bot b in BotMain.bots)
                {
                    if (b.Name == z.Parameters[1])
                    {
                        b.Msgtime = -1;
                        b.Message = string.Empty;
                        b.Type = "";
                        z.Player.SendSuccessMessage(string.Format("Removed bot {0}'s timed message.", b.Name));
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(string.Format("{0} removed bot {1}'s broadcast message ", z.Player.Name, b.Name));
                        Console.ResetColor();
                    }
                }
            }
            #endregion
        }
        #endregion

        #region Bot.Badwords
        public static void BadWords(CommandArgs z)
        {
            if (z.Parameters.Count < 2)
            {
                z.Player.SendWarningMessage("Invalid syntax. Try //badwords [add/del] word");
            }
            else if (z.Parameters[0] == "add")
            {
                using (var reader = BotMain.db.QueryReader("SELECT * FROM BotSwear WHERE SwearBlock = @0", z.Parameters[1]))
                {
                    if (!reader.Read())
                    {
                        BotMain.db.Query("INSERT INTO BotSwear (SwearBlock) VALUES (@0)", z.Parameters[1]);
                        z.Player.SendMessage(string.Format("Added {0} into the banned word list.", z.Parameters[1]), Color.CadetBlue);
                    }
                    else
                    {
                        z.Player.SendWarningMessage(string.Format("{0} already exists in the swear list.", z.Parameters[1]));
                    }
                }
            }
            else if (z.Parameters[0] == "del")
            {
                using (var reader = BotMain.db.QueryReader("SELECT * FROM BotSwear WHERE SwearBlock = @0", z.Parameters[1]))
                {
                    if (reader.Read())
                    {
                        BotMain.db.Query("DELETE FROM BotSwear WHERE SwearBlock = @0", z.Parameters[1]);
                        z.Player.SendMessage(string.Format("Deleted {0} from the banned word list.", z.Parameters[1]), Color.CadetBlue);
                    }
                    else
                    {
                        z.Player.SendWarningMessage(string.Format("{0} does not exist in the swear list.", z.Parameters[1]));
                    }
                }
            }
        }
        #endregion

        #region Bot.ReloadCfg
        public static void ReloadCfg(CommandArgs z)
        {
            Utils.SetUpConfig();
            foreach (Bot b in BotMain.bots)
            {
            	b.Trivia.LoadConfig(BotMain.TriviaSave);
            }
            z.Player.SendWarningMessage("Reloaded Bot config");
        }
        #endregion

        #region Bot.KickPlayers
        public static void KickPlayers(CommandArgs z)
        {
            if (z.Parameters.Count < 2)
            {
                z.Player.SendWarningMessage("Invalid syntax. Try //kickplayers [add/del] \"player\"");
            }
            else
            {
                if (z.Parameters[0] == "add")
                {
                    var ply = TShock.Users.GetUserByName(z.Parameters[1]);
                    using (var reader = BotMain.db.QueryReader("SELECT * FROM BotKick WHERE KickNames = @0", z.Parameters[1]))
                    {
                        if (!reader.Read())
                        {
                            BotMain.db.Query("INSERT INTO BotKick (KickNames) VALUES (@0)", z.Parameters[1]);
                            z.Player.SendMessage(string.Format("Added {0} to the joinkick player list.", z.Parameters[1]), Color.CadetBlue);
                        }
                        else
                        {
                            z.Player.SendWarningMessage(string.Format("{0} already exists in the joinkick player list!", z.Parameters[1]));
                        }
                    }
                }
                else if (z.Parameters[0] == "del")
                {
                    using (var reader = BotMain.db.QueryReader("SELECT * FROM BotKick WHERE KickNames = @0", z.Parameters[1]))
                    {
                        if (reader.Read())
                        {
                            BotMain.db.Query("DELETE FROM BotKick WHERE KickNames = @0", z.Parameters[1]);
                            z.Player.SendMessage(string.Format("Deleted {0} from the joinkick player list!", z.Parameters[1]), Color.CadetBlue);
                        }
                        else
                        {
                            z.Player.SendWarningMessage(string.Format("{0} does not exist on the joinkick player list!", z.Parameters[1]));
                        }
                    }
                }
            }
        }
        #endregion
    }
}
