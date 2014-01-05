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
using TShockAPI.DB;
using TShockAPI;
using Terraria;
using TerrariaApi;
using TerrariaApi.Server;

namespace ServerBot
{
    public class bTools
    {
        public static bConfig bot_Config { get; set; }
        public static CmdConfig com_Config { get; set; }
        public WebRequest request;
        internal static string save_Path { get { return Path.Combine(TShock.SavePath, "ServerBot/BotConfig.json"); } }
        internal static string trivia_Save_Path { get { return Path.Combine(TShock.SavePath, "ServerBot/TriviaConfig.json"); } }
        internal static string com_Save_Path { get { return Path.Combine(TShock.SavePath, "ServerBot/BotCommands.json"); } }
        public static bBot Bot { get; set; }
        public static List<bPlayer> players = new List<bPlayer>();
        public static DateTime lastmsgupdate = DateTime.Now;
        public static DateTime lastswearupdate = DateTime.Now;
        public static Random rid = new Random();
        public static string IP { get; set; }
        public static int plycount = 0;
        public static string servername { get; set; }
        public static BotCommandHandler Handler;
        public static bBot CommandBot;
        public static List<string> Swearwords = new List<string>();
        public static IDbConnection db;

        public static void autoJoin(EventArgs args)
        {
            if (bot_Config.enableAutoJoin)
            {
                Bot = new bBot(bot_Config.bot_Name);
                Bot.doSetUp();
            }
        }

        #region Database
        public static void SetUpDB()
        {
            if (TShock.Config.StorageType.ToLower() == "sqlite")
            {
                bTools.db = new SqliteConnection(string.Format("uri=file://{0},Version=3", Path.Combine(TShock.SavePath, "ServerBot/ServerBot.sqlite")));
            }
            else if (TShock.Config.StorageType.ToLower() == "mysql")
            {
                try
                {
                    var host = TShock.Config.MySqlHost.Split(':');
                    bTools.db = new MySqlConnection();
                    bTools.db.ConnectionString =
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
            var SQLcreator = new SqlTableCreator(bTools.db,
                                                 bTools.db.GetSqlType() == SqlType.Sqlite
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
            bTools.bot_Config = new bConfig();
            bTools.com_Config = new CmdConfig();

            try
            {
                if (Directory.Exists(Path.Combine(TShock.SavePath, "ServerBot")))
                {
                    if (File.Exists(bTools.save_Path))
                    {
                        bTools.bot_Config = bConfig.Read(bTools.save_Path);
                    }
                    else
                    {
                        bTools.bot_Config.Write(bTools.save_Path);
                    }
                    if (!File.Exists(bTools.trivia_Save_Path))
                    {
                        new TriviaConfig().Write(bTools.trivia_Save_Path);
                    }

                    if (File.Exists(bTools.com_Save_Path))
                        bTools.com_Config = CmdConfig.Read(bTools.com_Save_Path);
                    else
                        bTools.com_Config.Write(bTools.com_Save_Path);
                }
                else
                {
                    Directory.CreateDirectory(Path.Combine(TShock.SavePath, "ServerBot"));
                    bTools.bot_Config.Write(bTools.save_Path);
                    bTools.com_Config.Write(bTools.com_Save_Path);
                    new TriviaConfig().Write(bTools.trivia_Save_Path);
                }
            }
            catch (Exception x)
            {
                Log.Error("Error in BotConfig.json");
                Log.Info(x.ToString());
            }
        }
        #endregion

        public static cBotCommand getcBotCommand(string text)
        {
            for (int i = 0; i < bTools.com_Config.BotActions.Count; i++)
            {
                foreach (cBotCommand c in bTools.com_Config.BotActions[i].botcommands)
                {
                    if (c.CommandName == text)
                        return c;
                }
            }
            return null;
        }

        #region CheckChat
        public static void CheckChat(ServerChatEventArgs args)
        {
            var player = TShock.Players[args.Who];
            var bPlayer = players[args.Who];

            if (args.Handled)
                return;

            if (BotCommandHandler.CheckForBotCommand(args.Text))
            {
                bTools.Handler.HandleCommand(args.Text, player);
                args.Handled = true;
            }

            #region ConfigCommands
            try
            {
                var command = getcBotCommand(args.Text);
                if (command != null)
                {
                    if (command.ReturnMessage.Length > 0)
                        if (!command.noisyCommand)
                            player.SendMessage(command.ReturnMessage, bTools.Bot.color);
                        else
                            TSPlayer.All.SendMessage(command.ReturnMessage, bTools.Bot.color);

                    if (command.CommandActions.Count > 0)
                        for (int i = 0; i < command.CommandActions.Count; i++)
                            Commands.HandleCommand(player, command.CommandActions[i]);
                    args.Handled = true;
                }
            }
            catch { }
            #endregion

            #region Swearblocker
            if (bTools.bot_Config.swear_Block)
            {
                if (!BotCommandHandler.CheckForBotCommand(args.Text) && !args.Text.StartsWith("/"))
                {
                    var command = getcBotCommand(args.Text);
                    if (command == null)
                    {
                        foreach (string word in args.Text.Split(' '))
                        {
                            if (Swearwords.Contains(word))
                            {
                                checkOffences(bPlayer);
                            }
                        }
                    }
                }

            }
            #endregion

            #region YoloSwagDeath
            /*  (bTools.bot_Config.EnableYoloSwagBlock)
            {
                if (!text.StartsWith("/") || !text.StartsWith("^"))
                {
                    if (text.ToLower().Contains("yolo") || text.ToLower().Contains("y.o.l.o"))
                    {
                        var plr = TShock.Utils.FindPlayer(pl.Name)[0];
                        if (bTools.bot_Config.FailNoobAction == "kill")
                        {
                            plr.DamagePlayer(999999);
                            TSPlayer.All.SendMessage(string.Format("{0} lived more than once. Liar.", plr.Name), Color.CadetBlue);
                        }
                        else if (bTools.bot_Config.FailNoobAction == "kick")
                        {
                            TShock.Utils.ForceKick(plr, bTools.bot_Config.FailNoobKickReason, false, false);
                        }
                        else if (bTools.bot_Config.FailNoobAction == "mute")
                        {
                            plr.mute = true;
                            plr.SendWarningMessage("You've been muted for yoloing.");
                        }
                    }
                    if (text.ToLower().Contains("swag") || text.ToLower().Contains("s.w.a.g"))
                    {
                        var player = TShock.Utils.FindPlayer(pl.Name);
                        var plr = player[0];
                        if (bTools.bot_Config.FailNoobAction == "kill")
                        {
                            plr.DamagePlayer(999999);
                            TSPlayer.All.SendMessage(string.Format("Swag doesn't stop your players from DYING, {0}", plr.Name), Color.CadetBlue);
                        }
                        else if (bTools.bot_Config.FailNoobAction == "kick")
                        {
                            TShock.Utils.ForceKick(plr, bTools.bot_Config.FailNoobKickReason, false, false);
                        }
                        else if (bTools.bot_Config.FailNoobAction == "mute")
                        {
                            plr.mute = true;
                            plr.SendWarningMessage("You have been muted for swagging.");
                        }
                    }
                }
            }
             */
            #endregion
        }
        #endregion

        #region checkOffences
        public static void checkOffences(bPlayer player)
        {
            player.swear_Count++;
            var tsPlayer = TShock.Players[player.Index];
            
            if (player.swear_Count == bot_Config.swear_Block_Chances)
            {
                switch (bot_Config.swear_Block_Action)
                {
                    case "kick":
                        {
                            TShock.Utils.ForceKick(tsPlayer, string.Format("Swear count exceeds {0}",
                                bot_Config.swear_Block_Chances), false, true);
                            break;
                        }
                    case "mute":
                        {
                            tsPlayer.mute = true;
                            tsPlayer.SendWarningMessage("You have been muted for: Swear count exceeds {0}",
                                bot_Config.swear_Block_Chances);
                            
                            break;
                        }
                    case "ban":
                        {
                            TShock.Utils.Ban(tsPlayer, string.Format("Swear count exceeds {0}",
                                bot_Config.swear_Block_Chances), true, Bot.Name);
                            break;
                        }
                }
            }
        }
        #endregion

        #region GreetMsg
        public static void GreetMsg(TSPlayer player, string msg)
        {
            player.SendMessage(msg, Bot.color);
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
        public static void LogToConsole(ConsoleColor clr, string message, params object[] objs)
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
            bTools.Handler.RegisterCommand("help", BuiltinBotCommands.BotHelp);
            bTools.Handler.RegisterCommand("kill", BuiltinBotCommands.BotKill);
            bTools.Handler.RegisterCommand("hi", BuiltinBotCommands.BotGreet);
            bTools.Handler.RegisterCommand("good", BuiltinBotCommands.BotResponseGood);
            bTools.Handler.RegisterCommand("bad", BuiltinBotCommands.BotResponseBad);
            bTools.Handler.RegisterCommand("hug", BuiltinBotCommands.BotHug);
            bTools.Handler.RegisterCommand("ban", BuiltinBotCommands.BotBan);
            bTools.Handler.RegisterCommand("kick", BuiltinBotCommands.BotKick);
            bTools.Handler.RegisterCommand("mute", BuiltinBotCommands.BotMute);
            bTools.Handler.RegisterCommand("unmute", BuiltinBotCommands.BotUnmute);
            bTools.Handler.RegisterCommand("butcher", BuiltinBotCommands.BotButcher);
            //BotMain.Handler.RegisterCommand(new List<string>(){"How are you?", "how are you?", "how are you"}, BuiltinBotCommands.BotHowAreYou);
            bTools.Handler.RegisterCommand("insult", BuiltinBotCommands.BotInsult);
            //BotMain.Handler.RegisterCommand(new List<string>(){"g", "google"}, BuiltinBotCommands.BotWebsite);
            bTools.Handler.RegisterCommand("starttrivia", BuiltinBotCommands.BotTriviaStart);
            bTools.Handler.RegisterCommand("answer", BuiltinBotCommands.BotTriviaAnswer);
            bTools.Handler.RegisterCommand("badwords", BuiltinBotCommands.BotBadWords);
            bTools.Handler.RegisterCommand("reload", BuiltinBotCommands.BotReloadCfg);
        }
        #endregion

        #region GetSwears
        public static void GetSwears()
        {
            using (var reader = bTools.db.QueryReader("SELECT * FROM BotSwear"))
            {
                while (reader.Read())
                {
                    try
                    {
                        foreach (string swear in reader.Get<string>("SwearBlock").Split(','))
                        {
                            bTools.Swearwords.Add(swear.ToLower());
                        }
                    }
                    catch { }
                }
            }
        }
        #endregion
    }
}
