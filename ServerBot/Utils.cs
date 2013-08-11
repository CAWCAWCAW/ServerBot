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
    public class Utils
    {
        #region Database
        public static void SetUpDB()
        {
            if (TShock.Config.StorageType.ToLower() == "sqlite")
            {
                BotMain.db = new SqliteConnection(string.Format("uri=file://{0},Version=3", Path.Combine(TShock.SavePath, "ServerBot/ServerBot.sqlite")));
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
        public static void CheckChat(string text, TSPlayer pl)
        {
            #region Swearblocker
            if (BotMain.bcfg.EnableSwearBlocker)
            {
                foreach (Pl p in BotMain.players)
                {
                    if (p.PlayerName == pl.Name)
                    {
                        if (!text.StartsWith("^"))
                        {
                            var parts = text.Split();
                            foreach (string s in parts)
                            {
                                if (BotMain.Swearwords.Contains(s.ToLower()))
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
            #endregion

            #region YoloSwagDeath
            if (BotMain.bcfg.EnableYoloSwagBlock)
            {
                if (!text.StartsWith("/") || !text.StartsWith("^"))
                {
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
                }
            }
            #endregion
        }
        #endregion

        #region GreetMsg
        public static void GreetMsg(TSPlayer pl, int msg)
        {
            string greet = "";
            if (msg == 0)
            { greet = string.Format("[Bot] {0}: Hi {1}, welcome to {2}!", BotMain.bcfg.OnjoinBot, pl.Name, TShock.Config.ServerNickname); }
            if (msg == 1)
            { greet = string.Format("[Bot] {0}: Welcome to the land of amaaaaziiiiinnnng, {1}", BotMain.bcfg.OnjoinBot, pl.Name); }
            if (msg == 2)
            { greet = string.Format("[Bot] {0}: Hallo there, {1}", BotMain.bcfg.OnjoinBot, pl.Name); }
            if (msg == 3)
            { greet = string.Format("[Bot] {0}: Well hello there, {1}, I'm {0}, and this is {2}!", BotMain.bcfg.OnjoinBot, pl.Name, BotMain.servername); }
            if (msg== 4)
            { greet = string.Format("[Bot] {0}: Hi there {1}! I hope you enjoy your stay", BotMain.bcfg.OnjoinBot, pl.Name); }
            pl.SendMessage(greet, BotMain.CommandBot.r, BotMain.CommandBot.g, BotMain.CommandBot.b);
        }
        #endregion
        
        #region LogToConsole
        public static void LogToConsole(ConsoleColor clr, string message)
        {
        	Console.ForegroundColor = clr;
        	Console.WriteLine(message);
        	Console.ResetColor();
        	Log.Info(message);
        }
        public static void LogToConsole(ConsoleColor clr, string message, object[] objs)
        {
        	Console.ForegroundColor = clr;
        	Console.WriteLine(message, objs);
        	Console.ResetColor();
        	Log.Info(string.Format(message, objs));
        }
        #endregion
        
        #region RegisterBuiltinCommands
        public static void RegisterBuiltinCommands()
        {
        	BotMain.Handler.RegisterCommand("help", BuiltinBotCommands.BotHelp);
        	BotMain.Handler.RegisterCommand("kill", BuiltinBotCommands.BotKill);
        	BotMain.Handler.RegisterCommand("hi", BuiltinBotCommands.BotGreet);
        	BotMain.Handler.RegisterCommand("good", BuiltinBotCommands.BotResponseGood);
        	BotMain.Handler.RegisterCommand("bad", BuiltinBotCommands.BotResponseBad);
        	BotMain.Handler.RegisterCommand("hug", BuiltinBotCommands.BotHug);
        	BotMain.Handler.RegisterCommand("ban", BuiltinBotCommands.BotBan);
        	BotMain.Handler.RegisterCommand("kick", BuiltinBotCommands.BotKick);
        	BotMain.Handler.RegisterCommand("mute", BuiltinBotCommands.BotMute);
        	BotMain.Handler.RegisterCommand("unmute", BuiltinBotCommands.BotUnmute);
        	BotMain.Handler.RegisterCommand("butcher", BuiltinBotCommands.BotButcher);
        	//BotMain.Handler.RegisterCommand(new List<string>(){"How are you?", "how are you?", "how are you"}, BuiltinBotCommands.BotHowAreYou);
        	BotMain.Handler.RegisterCommand("insult", BuiltinBotCommands.BotInsult);
        	//BotMain.Handler.RegisterCommand(new List<string>(){"g", "google"}, BuiltinBotCommands.BotWebsite);
        	BotMain.Handler.RegisterCommand("starttrivia", BuiltinBotCommands.BotTriviaStart);
        	BotMain.Handler.RegisterCommand("answer", BuiltinBotCommands.BotTriviaAnswer);
            BotMain.Handler.RegisterCommand("badwords", BuiltinBotCommands.BotBadWords);
            BotMain.Handler.RegisterCommand("reload", BuiltinBotCommands.BotReloadCfg);
        }
        #endregion

        #region GetSwears
        public static void GetSwears()
        {
            string swears = "";
            using (var reader = BotMain.db.QueryReader("SELECT * FROM BotSwear"))
            {
                while (reader.Read())
                {
                    try
                    {
                        swears = reader.Get<string>("SwearBlock");

                        string[] words = swears.Split(',');
                        foreach (string s in words)
                        {
                            BotMain.Swearwords.Add(s.ToLower());
                        }
                    }
                    catch { }
                }
            }
        }
        #endregion
    }
}
