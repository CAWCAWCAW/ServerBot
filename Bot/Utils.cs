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

namespace Bot
{
    public class Utils
    {
        #region Database
        public static void SetUpDB()
        {
            if (TShock.Config.StorageType.ToLower() == "sqlite")
            {
                BotMain.db = new SqliteConnection(string.Format("uri=file://{0},Version=3", Path.Combine(TShock.SavePath, "Bot.sqlite")));
            }
            else if (TShock.Config.StorageType.ToLower() == "mysql")
            {
                try
                {
                    var host = TShock.Config.MySqlHost.Split(':');
                    BotMain.db = new MySqlConnection();
                    BotMain.db.ConnectionString =
                        String.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
                                      host[0],
                                      host.Length == 1 ? "3306" : host[1],
                                      TShock.Config.MySqlDbName,
                                      TShock.Config.MySqlUsername,
                                      TShock.Config.MySqlPassword
                    );
                }
                catch (MySqlException ex)
                {
                    Log.Error(ex.ToString());
                    throw new Exception("MySql not setup correctly");
                }
            }
            else
            {
                throw new Exception("Invalid storage type");
            }


            var table = new SqlTable("BotKick",
                                     new SqlColumn("KickNames", MySqlDbType.Text),
                                     new SqlColumn("KickIP", MySqlDbType.Text)
                );
            var SQLcreator = new SqlTableCreator(BotMain.db,
                                                 BotMain.db.GetSqlType() == SqlType.Sqlite
                                                 ? (IQueryBuilder)new SqliteQueryCreator()
                                                 : new MysqlQueryCreator());

            SQLcreator.EnsureExists(table);

            var table2 = new SqlTable("BotSwear",
                new SqlColumn("SwearBlock", MySqlDbType.Text)
                );
            SQLcreator.EnsureExists(table2);
        }
        #endregion

        #region SetUpConfig
        public static void SetUpConfig()
        {
        	BotMain.bcfg = new BotConfig();
            try
            {
            	if (Directory.Exists(Path.Combine(TShock.SavePath, "ServerBot")))
	            	{
	                if (File.Exists(BotMain.BotSave))
	                {
	                    BotMain.bcfg = BotConfig.Read(BotMain.BotSave);
	                }
	                else
	                {
	                    BotMain.bcfg.Write(BotMain.BotSave);
	                }
	                if (!File.Exists(BotMain.TriviaSave))
                    {
                    	new TriviaConfig().Write(BotMain.TriviaSave);
                    }
            	}
            	else
            	{
            		Directory.CreateDirectory(Path.Combine(TShock.SavePath, "ServerBot"));
            		BotMain.bcfg.Write(BotMain.BotSave);
            		new TriviaConfig().Write(BotMain.TriviaSave);
            	}
            }
            catch (Exception z)
            {
                Log.Error("Error in BotConfig.json");
                Log.Info(z.ToString());
            }
        }
        #endregion

        #region CheckChat
        public static void CheckChat(string text, TSPlayer pl, HandledEventArgs e)
        {
            #region Swearblocker
            if (BotMain.bcfg.EnableSwearBlocker)
            {
                if (!e.Handled)
                    foreach (Pl p in BotMain.players)
                    {
                        if (p.PlayerName == pl.Name)
                        {
                            if (!text.StartsWith("/"))
                            {
                                e.Handled = true;
                                var parts = text.Split();
                                foreach (string s in parts)
                                {
                                    using (var reader = BotMain.db.QueryReader("SELECT * FROM BotSwear WHERE SwearBlock = @0", s))
                                    {
                                        if (reader.Read())
                                        {
                                            if (BotMain.bcfg.SwearBlockAction == "kick")
                                            {
                                                p.scount++;
                                                p.TSPlayer.SendWarningMessage(string.Format("Your swear warning count has risen! It is now: {0}", p.scount));
                                                if (p.scount >= BotMain.bcfg.SwearBlockChances)
                                                {
                                                    TShock.Utils.ForceKick(pl, "Swearing", false, false);
                                                    p.scount = 0;
                                                }
                                            }
                                            else if (BotMain.bcfg.SwearBlockAction == "mute")
                                            {
                                                p.scount++;
                                                p.TSPlayer.SendWarningMessage(string.Format("Your swear warning count has risen! It is now: {0}/{1}", p.scount, BotMain.bcfg.SwearBlockChances));
                                                if (p.scount >= BotMain.bcfg.SwearBlockChances)
                                                {
                                                    pl.mute = true;
                                                    pl.SendWarningMessage("You have been muted for swearing!");
                                                    p.scount = 0;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
            }
            #endregion

            #region YoloSwagDeath
            if (!text.StartsWith("/"))
            {
                if (BotMain.bcfg.EnableYoloSwagBlock)
                {
                    if (!e.Handled)
                        if (text.ToLower().Contains("yolo") || text.ToLower().Contains("y.o.l.o"))
                        {
                            var plr = TShock.Utils.FindPlayer(pl.Name)[0];
                            if (BotMain.bcfg.FailNoobAction == "kill")
                            {
                                plr.DamagePlayer(999999);
                                TSPlayer.All.SendMessage(string.Format("{0} lived more than once. Liar.", plr.Name), Color.CadetBlue);
                            }
                            else if (BotMain.bcfg.FailNoobAction == "kick")
                            {
                                TShock.Utils.ForceKick(plr, BotMain.bcfg.FailNoobKickReason, false, false);
                            }
                            else if (BotMain.bcfg.FailNoobAction == "mute")
                            {
                                plr.mute = true;
                                plr.SendWarningMessage("You've been muted for yoloing.");
                            }
                        }
                    if (text.ToLower().Contains("swag") || text.ToLower().Contains("s.w.a.g"))
                    {
                        var player = TShock.Utils.FindPlayer(pl.Name);
                        var plr = player[0];
                        if (BotMain.bcfg.FailNoobAction == "kill")
                        {
                            plr.DamagePlayer(999999);
                            TSPlayer.All.SendMessage(string.Format("Swag doesn't stop your players from DYING, {0}", plr.Name), Color.CadetBlue);
                        }
                        else if (BotMain.bcfg.FailNoobAction == "kick")
                        {
                            TShock.Utils.ForceKick(plr, BotMain.bcfg.FailNoobKickReason, false, false);
                        }
                        else if (BotMain.bcfg.FailNoobAction == "mute")
                        {
                            plr.mute = true;
                            plr.SendWarningMessage("You have been muted for swagging.");
                        }
                    }
            #endregion

                    foreach (Bot b in BotMain.bots)
                    {
                        if (b.Name == BotMain.bcfg.CommandBot)
                        {
                            if (text.StartsWith(BotMain.bcfg.CommandChar))
                            {
                            	string[] words = text.Split();

                                #region Bot.HelpCmds
                                if (words[1] == "help")
                                {
                                	b.Say("{0}{1}{2}: {3}: help", new object[]{pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[2]});
                                    Commands.HandleCommand(pl, "/help");
                                    LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2}", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1]});
                                    Log.Info(string.Format("{0} used {1} to execute {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                }
                                if (words[1] == "help-" && words[2] == "register")
                                {
                                	b.Say("{0}{1}{2}: {3}: help- register", new object[]{pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[2]});
                                	b.Say("{0}: To register, use /register <password>", new object[]{BotMain.bcfg.CommandBot});
                                	b.Say("<password> can be anything, and you define it personally.");
                                	b.Say("Always remember to keep your password secure!");
                                	LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2} {3}", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1], words[2]});
                                    Log.Info(string.Format("{0} used {1} to execute: {2} {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], words[2]));
                                }
                                if (words[1] == "help-" && words[2] == "item")
                                {
                                	b.Say("{0}{1}{2}: {3}: help- item", new object[]{pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[2]});
                                	b.Say("{0}: To spawn any item, use the command /item", new object[]{BotMain.bcfg.CommandBot});
                                	b.Say("Items that are made of multiple words MUST be wrapped in quotes");
                                	b.Say("Eg: /item \"hallowed repeater\"");
                                	LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2} {3}", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1], words[2]});
                                    Log.Info(string.Format("{0} used {1} to execute: {2} {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], words[2]));
                                }
                                #endregion

                                #region Bot.KillCmd
                                if (words[1] == "kill")
                                {
                                	b.Say("{0}{1}{2}: {3}: kill {4}", new object[]{pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[2]});
                                    if (pl.Group.HasPermission("kill"))
                                    {
                                        var player = TShock.Utils.FindPlayer(words[2]);
                                        var plr = player[0];
                                        plr.DamagePlayer(999999);
                                        b.Say("{0}: {1}: I just killed {2}!", new object[]{BotMain.bcfg.CommandBot, pl.Name, plr.Name});
                                        LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2} on {3}", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name});
                                        Log.Info(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                    }
                                    else
                                    {
                                        pl.SendMessage(string.Format("{0}: Sorry {1}, but you don't have permission to use kill", BotMain.bcfg.CommandBot, pl.Name), b.r, b.g, b.b);
                                        Log.Info(string.Format("{0} failed to use {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                                    }
                                }
                                #endregion

                                #region Bot.Greeting
                                if ((words[1] == "Hi" || words[1] == "hi" || words[1] == "hello"))
                                {
                                	b.Say("{0}{1}{2}: {3}: {4}", new object[]{pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[1]});
                                	b.Say("{0}: Hello {1}, how are you?", new object[]{BotMain.bcfg.CommandBot, pl.Name});
                                	LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2}", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1]});
                                    Log.Info(string.Format("{0} used {1} to execute {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                }
                                #endregion

                                #region Bot.Responses
                                if ((words[1] == "good") || (words[1] == "Good"))
                                {
                                    Random z = new Random();
                                    int q = z.Next(0, 5);
                                    {
                                    	b.Say("{0}{1}{2}: {3}: {4}", new object[]{pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[1]});
                                        if (q == 0)
                                        { b.Say("{0}: That's great. I'll tell you how I am, if you ask me ;)", new object[]{BotMain.bcfg.CommandBot}); }
                                        if (q == 1)
                                        { b.Say("{0}: Hah, nice. What's the bet I'm better though? >:D", new object[]{BotMain.bcfg.CommandBot}); }
                                        if (q == 2)
                                        { b.Say("{0}: Nice to hear", new object[]{BotMain.bcfg.CommandBot}); }
                                        if (q == 3)
                                        { b.Say("{0}: ...'kay", new object[]{BotMain.bcfg.CommandBot}); }
                                        if (q == 4)
                                        { b.Say("{0}: Good, you say? Did you bring me a present then?", new object[]{BotMain.bcfg.CommandBot}); }
                                        if (q == 5)
                                        { b.Say("{0}: I'm always happiest with good friends... And lots of alcohol. Want to join me?", new object[]{BotMain.bcfg.CommandBot}); }
                                        Log.Info(string.Format("{0} used {1} to execute: {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                    }
                                }
                                else if ((words[1] == "bad") || (words[1] == "Bad"))
                                {
                                    Random h = new Random();
                                    int cf = h.Next(0, 4);
                                    {
                                    	b.Say("{0}{1}{2}: {3}: quoteID {4}", new object[]{pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[1]});
                                        if (cf == 0)
                                        { b.Say("{1}: Well {0}... Always remember, the new day is a great big fish.", new object[]{pl.Name, BotMain.bcfg.CommandBot}); }
                                        if (cf == 1)
                                        { b.Say("{1}: Poor {0}... It could be worse though. You could have crabs.", new object[]{ pl.Name, BotMain.bcfg.CommandBot}); }
                                        if (cf == 2)
                                        {
                                        	b.Say("{1}: There there, {0}", new object[]{pl.Name, BotMain.bcfg.CommandBot});
                                            Item heart = TShock.Utils.GetItemById(58);
                                            Item star = TShock.Utils.GetItemById(184);
                                            for (int i = 0; i < 20; i++)
                                            {
                                                pl.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
                                            }
                                            for (int i = 0; i < 10; i++)
                                            {
                                                pl.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
                                            }
                                            b.Say("*{1} hugs {0}", new object[]{pl.Name, BotMain.bcfg.CommandBot});
                                        }
                                        if (cf == 3)
                                        { b.Say("{1}: {0}, What you need is a good sleep... And a monkey", new object[]{pl.Name, BotMain.bcfg.CommandBot}); }
                                        if (cf == 4)
                                        { b.Say("{1}: Feeling down eh? What you need is a cat.", new object[]{pl.Name, BotMain.bcfg.CommandBot}); }
                                        Log.Info(string.Format("{0} used {1} to execute: {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                    }
                                }
                                #endregion

                                #region Bot.Hug (heal)
                                if (words[1] == "hug")
                                {
                                	b.Say("{0}{1}{2}: {3}: hug {4}", new object[]{pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[2]});
                                    var player = TShock.Utils.FindPlayer(words[2]);
                                    var plr = player[0];
                                    Item heart = TShock.Utils.GetItemById(58);
                                    Item star = TShock.Utils.GetItemById(184);
                                    for (int i = 0; i < 20; i++)
                                        plr.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
                                    for (int i = 0; i < 10; i++)
                                        plr.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
                                    b.Say("*{1} hugs {0}", new object[]{plr.Name, BotMain.bcfg.CommandBot});
                                    LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2} on {3}", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name});
                                    Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                }
                                #endregion

                                #region Bot.Ban, Bot.Kick, Bot.Mute
                                if (words[1] == "ban")
                                {
                                    if (pl.Group.HasPermission("ban"))
                                    {
                                        var player = TShock.Utils.FindPlayer(words[2]);
                                        var plr = player[0];
                                        TShock.Utils.Ban(plr, BotMain.bcfg.CommandBot + " ban", false, null);
                                        LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2} on {3}", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name});
                                        Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                    }
                                    else
                                    {
                                        pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use ban.", pl.Name, BotMain.bcfg.CommandBot), b.r, b.g, b.b);
                                        Log.Info(string.Format("{0} failed to use {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                                    }
                                }
                                if (words[1] == "kick")
                                {
                                    if (pl.Group.HasPermission("kick"))
                                    {
                                        var player = TShock.Utils.FindPlayer(words[2]);
                                        var plr = player[0];
                                        TShock.Utils.Kick(plr, BotMain.bcfg.CommandBot + " forcekick", false, false, null, false);
                                        LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2} on {3}", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name});
                                        Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                    }
                                    else
                                    {
                                        pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use kick.", pl.Name, BotMain.bcfg.CommandBot), b.r, b.g, b.b);
                                        Log.Info(string.Format("{0} failed to use {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                                    }
                                }
                                if (words[1] == "mute")
                                {
                                    if (pl.Group.HasPermission("mute"))
                                    {
                                    	b.Say("{0}: {1}: mute {2}", new object[]{pl.Name, BotMain.bcfg.CommandChar, words[2]});
                                        var player = TShock.Utils.FindPlayer(words[2]);
                                        var plr = player[0];
                                        plr.mute = true;
                                        LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2} on {3}", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name});
                                        Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                    }
                                    else
                                    {
                                        pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use mute.", pl.Name, BotMain.bcfg.CommandBot), b.r, b.g, b.b);
                                        Log.Info(string.Format("{0} failed to used {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                                    }
                                }
                                if (words[1] == "unmute")
                                {
                                    if (pl.Group.HasPermission("mute"))
                                    {
                                    	b.Say("{0}: {1}: unmute {2}", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[2]});
                                        var player = TShock.Utils.FindPlayer(words[2]);
                                        var plr = player[0];
                                        plr.mute = false;
                                        LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2} on {3}", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name});
                                        Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                    }
                                }
                                #endregion

                                #region Bot.Super.Ban, Bot.Super.Kick
                                if (words[1] == "skick")
                                {
                                    if (pl.Group.HasPermission("skick"))
                                    {
                                        var player = TShock.Utils.FindPlayer(words[2]);
                                        var plr = player[0];
                                        TShock.Utils.Kick(plr, BotMain.bcfg.CommandBot + " forcekick", true, false, null, false);
                                        LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2} on {3}", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name});
                                        Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                    }
                                    else
                                    {
                                        pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use super kick.", pl.Name, BotMain.bcfg.CommandBot), b.r, b.g, b.b);
                                        Log.Info(string.Format("{0} failed to use {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                                    }
                                }
                                if (words[1] == "sban")
                                {
                                    if (pl.Group.HasPermission("sban"))
                                    {
                                        var player = TShock.Utils.FindPlayer(words[2]);
                                        var plr = player[0];
                                        TShock.Utils.Ban(plr, BotMain.bcfg.CommandBot + " forceban", true, null);
                                        LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2} on {3}", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name});
                                        Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                    }
                                    else
                                    {
                                        pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use super ban.", pl.Name, BotMain.bcfg.CommandBot), b.r, b.g, b.b);
                                        Log.Info(string.Format("{0} failed to use {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                                    }
                                }
                                #endregion

                                #region Bot.CommandsList
                                if (words[1] == "commands" || words[1] == "cmds")
                                {
                                	b.Say("{0}{1}{2}: {3}: {4}", new object[]{pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[1]});
                                    pl.SendMessage(string.Format("{1}: {0}; My executable commands are:", pl.Name, BotMain.bcfg.CommandBot), b.r, b.g, b.b);
                                    pl.SendMessage(string.Format("help, help- register, help- item, kill, hi, good, How are you?, butcher"), b.r, b.g, b.b);
                                    pl.SendMessage(string.Format("hug, afk, ban, kick, mute, unmute, insult, cmds, google, quote"), b.r, b.g, b.b);
                                    LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2}.", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1]});
                                    Log.Info(string.Format("{0} used {1}'s command sender to check {1}'s commands.", pl.Name, BotMain.bcfg.CommandBot));
                                }
                                #endregion

                                #region Bot.Butcher
                                if (words[1] == "butcher")
                                {
                                	b.Say("{0}{1}{2}: {3}: butcher", new object[]{pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar});
                                    if (!pl.Group.HasPermission("butcher"))
                                    {
                                        Random r = new Random();
                                        int p = r.Next(1, 100);
                                        if (p <= BotMain.bcfg.ButcherCmdPct)
                                        {
                                            Commands.HandleCommand(TShockAPI.TSPlayer.Server, "/butcher");
                                            b.Say("{1}: {0}: I butchered all hostile NPCs!", new object[]{pl.Name, BotMain.bcfg.CommandBot});
                                           	LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2}.", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1]});
                                            Log.Info(string.Format("{0} used {1} to execute {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                        }
                                        else
                                        {
                                        	b.Say("{1}: Sorry {0}, you rolled a {2}. You need to roll less than {3} to butcher", new object[]{pl.Name, BotMain.bcfg.CommandBot, p, BotMain.bcfg.ButcherCmdPct});
                                        }
                                    }
                                    else
                                    {
                                        Commands.HandleCommand(pl, "/butcher");
                                        b.Say("{1}: {0}: I butchered all hostile NPCs!", new object[]{pl.Name, BotMain.bcfg.CommandBot});
                                        LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: {2}.", new object[]{pl.Name, BotMain.bcfg.CommandBot, words[1]});
                                        Log.Info(string.Format("{0} used {1} to execute {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                    }
                                }
                                #endregion

                                #region Bot.How Are you?
                                if (words[1] == "How" || words[1] == "how" && words[2] == "are".ToLower() && words[3].ToString() == "you?".ToLower())
                                {
                                	b.Say("{0}{1}{2}: {3}: How are you?", new object[]{pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar});
                                    Random r = new Random();
                                    int p = r.Next(1, 10);
                                    if (p == 1)
                                    {
                                    	b.Say("{1}: {0}, I am feelings quite well today, thank you!", new object[]{pl.Name, BotMain.bcfg.CommandBot});
                                    }
                                    else if (p > 3 && p < 6)
                                    {
                                    	b.Say("{1}: {0}, I'm feeling a bit down. Might go get drunk later.", new object[]{pl.Name, BotMain.bcfg.CommandBot});
                                    }
                                    else if (p == 7 || p == 6)
                                    {
                                    	b.Say("{1}: {0}, Better than you. 'cos I'm AWESOME!", new object[]{pl.Name, BotMain.bcfg.CommandBot});
                                    }
                                    else if (p > 8 && p != 10)
                                    {
                                    	b.Say("{1}: {0}, I'm seeing unicorns and gnomes. How do you think I am?", new object[]{pl.Name, BotMain.bcfg.CommandBot});
                                    }
                                    else if (p == 10)
                                    {
                                    	b.Say("{1}: {0}, I just won the lottery. Stop being so poor in front of me.", new object[]{pl.Name, BotMain.bcfg.CommandBot});
                                    }
                                }
                                #endregion

                                #region Bot.PlayerInsult
                                if (words[1] == "insult")
                                {
                                    var ply = TShock.Utils.FindPlayer(words[2]);
                                    var plr = ply[0].Name;
                                    b.Say("{0}{1}{2}: {3}: insult {4}", new object[]{pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[2]});
                                    Random r = new Random();
                                    int p = r.Next(1, 10);

                                    if (p == 1)
                                    { b.Say("{1}: Yo, {0}, I bet your mother is a nice lady!", new object[]{plr, BotMain.bcfg.CommandBot}); }
                                    if (p == 2)
                                    { b.Say("{1}: I bet {0}'s mother was a hamster, and their father smelled of elderberries.", new object[]{plr, BotMain.bcfg.CommandBot}); }
                                    if (p == 3)
                                    { b.Say("{1}: I bet {0} uses the term swag liberally.", new object[]{plr, BotMain.bcfg.CommandBot}); }
                                    if (p == 4)
                                    { b.Say("{1}: {0} is such a... twig!", new object[]{plr, BotMain.bcfg.CommandBot}); }
                                    if (p == 5)
                                    { b.Say("{0}: ...But I'm a nice bot!... Sometimes", new object[]{BotMain.bcfg.CommandBot}); }
                                    if (p == 6)
                                    { b.Say("{1}: {0} is such a! a... erm... thing!", new object[]{plr, BotMain.bcfg.CommandBot}); }
                                    if (p == 7)
                                    { b.Say("{1}: {0}, I'm so awesome you should feel insulted already.", new object[]{plr, BotMain.bcfg.CommandBot}); }
                                    if (p == 9)
                                    { b.Say("{1}: {0}... You remind me of someone named {2}.", new object[]{plr, BotMain.bcfg.CommandBot, BotMain.bcfg.GenericInsultName}); }
                                    if (p == 10)
                                    { b.Say("{1}: Don't tell me what to do, {0}!", new object[]{plr, BotMain.bcfg.CommandBot}); }
                                }
                                #endregion

                                #region Bot.Website
                                if (words[1] == "g" || words[1] == "google")
                                {
                                    int count = 0;
                                    b.Say("{0}{1}{2}: {3}: {4} {5}", new object[]{pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[1], words[2]});
                                    try
                                    {
                                        string website = "http://www." + words[2] + ".com";
                                        WebClient client = new WebClient();
                                        string value = client.DownloadString(website);
                                        string[] test = value.Split('/');
                                        foreach (string s in test)
                                        {
                                            if (s.Contains("<Title>".ToLower()))
                                            {
                                            	b.Say("{0}: Website for {1}: {2}", new object[]{BotMain.bcfg.CommandBot, words[2], website});
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        count++;
                                        //Console.WriteLine(".com failed");
                                        //Console.WriteLine(x.ToString());
                                    }
                                    try
                                    {
                                        string website = "http://www." + words[2] + ".org";
                                        WebClient client = new WebClient();
                                        string value = client.DownloadString(website);
                                        string[] test = value.Split('/');
                                        foreach (string s in test)
                                        {
                                            if (s.Contains("<Title>".ToLower()))
                                            {
                                                b.Say("{0}: Website for {1}: {2}", new object[]{BotMain.bcfg.CommandBot, words[2], website});
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        count++;
                                        //Console.WriteLine(".org failed");
                                        //Console.WriteLine(x.ToString());
                                    }
                                    try
                                    {
                                        string website = "http://www." + words[2] + ".net";
                                        WebClient client = new WebClient();
                                        string value = client.DownloadString(website);
                                        string[] test = value.Split('/');
                                        foreach (string s in test)
                                        {
                                            if (s.Contains("<Title>".ToLower()))
                                            {
                                                b.Say("{0}: Website for {1}: {2}", new object[]{BotMain.bcfg.CommandBot, words[2], website});
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        count++;
                                        //Console.WriteLine(".net failed");
                                        //Console.WriteLine(x.ToString());
                                    }
                                    try
                                    {
                                        string website = "http://www." + words[2] + ".co";
                                        WebClient client = new WebClient();
                                        string value = client.DownloadString(website);
                                        string[] test = value.Split('/');
                                        foreach (string s in test)
                                        {
                                            if (s.Contains("<Title>".ToLower()))
                                            {
                                                b.Say("{0}: Website for {1}: {2}", new object[]{BotMain.bcfg.CommandBot, words[2], website});
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        count++;
                                        if (count == 4)
                                        {
                                        	b.Say("{0}: Sorry {1}, I cannot find a website for {2}", new object[]{b.Name, pl.Name, words[2]});
                                        }
                                    }
                                    return;
                                }
                                #endregion
                                
                                #region TriviaStarting
                                if (words[1] == "starttrivia")
                                {
                                	if (words.Length > 2)
                                	{
	                                	int numq;
	                                	if (!int.TryParse(words[2], out numq))
	                                	{
	                                		pl.SendMessage(string.Format("Bot {0}: You didn't provide a valid number of questions for the game.", b.Name), b.r, b.g, b.b);
	                                		return;
	                                	}
	                                	b.trivia.StartGame(numq);
	                                	return;
                                	}
                                	else
                                	{
                                		pl.SendMessage(string.Format("Bot {0}: Proper format of \"^ starttrivia\": ^ starttrivia <number of questions to ask>", b.Name), b.r, b.g, b.b);
                                		return;
                                	}
                                }
                                #endregion
                                
                                #region TriviaAnswering
                                if (words[1] == "answer")
                                {
                                	if (b.trivia.OngoingGame)
                                	{
                                		b.trivia.CheckAnswer(string.Join(" ", words, 2, words.Length-2), pl.Name);
                                	}
                                }
                                #endregion
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region GreetMsg
        public static void GreetMsg(TSPlayer pl, int msg)
        {
            string greet = "";
            if (msg == 0)
            { greet = string.Format("{0}: Hi {1}, welcome to {2}!", BotMain.bcfg.OnjoinBot, pl.Name, TShock.Config.ServerNickname); }
            if (msg == 1)
            { greet = string.Format("{0}: Welcome to the land of amaaaaziiiiinnnng, {1}", BotMain.bcfg.OnjoinBot, pl.Name); }
            if (msg == 2)
            { greet = string.Format("{0}: Hallo there, {1}", BotMain.bcfg.OnjoinBot, pl.Name); }
            if (msg == 3)
            { greet = string.Format("{0}: Well hello there, {1}, I'm {0}, and this is {2}!", BotMain.bcfg.OnjoinBot, pl.Name, BotMain.servername); }
            if (msg== 4)
            { greet = string.Format("{0}: Hi there {1}! I hope you enjoy your stay", BotMain.bcfg.OnjoinBot, pl.Name); }
            pl.SendMessage(greet, BotMain.RBC);
        }
        #endregion
        
        #region LogToConsole
        public static void LogToConsole(ConsoleColor clr, string message)
        {
        	Console.ForegroundColor = clr;
        	Console.WriteLine(message);
        	Console.ResetColor();
        }
        public static void LogToConsole(ConsoleColor clr, string message, object[] objs)
        {
        	Console.ForegroundColor = clr;
        	Console.WriteLine(message, objs);
        	Console.ResetColor();
        }
        #endregion
    }
}
