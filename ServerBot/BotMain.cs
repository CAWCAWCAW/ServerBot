using System.Collections.Generic;
using System.ComponentModel;
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
    [APIVersion(1, 12)]
    public class BotMain : TerrariaPlugin
    {
        public static BotConfig bcfg { get; set; }
        public WebRequest request;
        internal static string BotSave { get { return Path.Combine(TShock.SavePath, "ServerBot/BotConfig.json"); } }
        internal static string TriviaSave { get { return Path.Combine(TShock.SavePath, "ServerBot/TriviaConfig.json"); } }
        public static List<Bot> bots = new List<Bot>();
        public static List<Pl> players = new List<Pl>();
        public static DateTime lastmsgupdate = DateTime.Now;
        public static DateTime lastswearupdate = DateTime.Now;
        public static Random rid = new Random();
        public static string IP { get; set; }
        public static int plycount = 0;
        public static string servername { get; set; }
        public static BotCommandHandler Handler;
        public static Bot CommandBot;
        public static List<string> Swearwords = new List<string>();

        public static IDbConnection db;

        #region Name, Version, Author, Description, Hooks
        public override string Name
        {
            get { return "ServerBot"; }
        }

        public override Version Version
        {
            get { return new Version(1, 0, 15); }
        }

        public override string Author
        {
            get { return "WhiteX"; }
        }

        public override string Description
        {
            get { return "Terraria server bot(s)."; }
        }

        public override void Initialize()
        {
            GameHooks.Initialize += OnInitialize;
            ServerHooks.Join += OnJoin;
            GameHooks.Update += OnUpdate;
            ServerHooks.Chat += OnChat;
            ServerHooks.Leave += OnLeave;
            NetHooks.GreetPlayer += OnGreet;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GameHooks.Initialize -= OnInitialize;
                GameHooks.Update -= OnUpdate;
                ServerHooks.Chat -= OnChat;
                ServerHooks.Join -= OnJoin;
                ServerHooks.Leave -= OnLeave;
                NetHooks.GreetPlayer -= OnGreet;
            }
            base.Dispose(disposing);
        }

        public BotMain(Main game)
            : base(game)
        {
            
        }
        #endregion

        #region OnInitialize
        public void OnInitialize()
        {
            Commands.ChatCommands.Add(new Command(new List<string>() { "bot.bot", "bot.*" }, BComs.BotMethod, "/bot"));
            Commands.ChatCommands.Add(new Command(new List<string>() { "bot.reload", "bot.*" }, BComs.ReloadCfg, "/botrld"));
            Commands.ChatCommands.Add(new Command(new List<string>() { "bot.badwords", "bot.*" }, BComs.BadWords, "/badwords", "/badword", "/bd"));
            Commands.ChatCommands.Add(new Command(new List<string>() { "bot.kickplayer", "bot.*" }, BComs.KickPlayers, "/kickplayers", "/kickplayer", "/kp"));

            Utils.SetUpConfig();
            Utils.SetUpDB();
            Handler = new BotCommandHandler();
            Utils.RegisterBuiltinCommands();
            
            CommandBot = new Bot(1, bcfg.CommandBot);

            if (TShock.Config.UseServerName)
            {
                servername = TShock.Config.ServerName;
            }
            else
            {
                servername = TShock.Config.ServerNickname;
            }

            Utils.GetSwears();

        }
        #endregion

        #region Onjoin
        public void OnJoin(int who, HandledEventArgs e)
        {
            lock (players)
                players.Add(new Pl(who));

            #region Bot.AutoJoining
            if (bcfg.EnableAutoJoin)
            {
                if (plycount == 0)
                {
                    lock (bots)
                    {
                        Random r = new Random();
                        int z = r.Next(2, 256);
                        {
                            bots.Add(new Bot(z, bcfg.OnjoinBot));
                            foreach (Bot b in bots)
                            {
                                if (b.Name == bcfg.OnjoinBot)
                                {
                                	b.r = bcfg.OnjoinBotColourR;
                                	b.g = bcfg.OnjoinBotColourG;
                                	b.b = bcfg.OnjoinBotColourB;
                                }
                            }
                            plycount++;
                            return;
                        }
                    }
                }
            }
            #endregion

            #region Bot.AutoKick, Bot.AutoBan
            var ply = players[who];
            ply.kcount = 0;
            using (var reader = db.QueryReader("SELECT * FROM BotKick WHERE KickNames = @0", ply.PlayerName))
            {
                if (reader.Read() && ply.kcount < bcfg.KickCountB4Ban)
                {
                    db.Query("UPDATE BotKick SET KickIP = @0 WHERE KickNames = @1", ply.TSPlayer.IP, ply.PlayerName);
                    TShock.Utils.ForceKick(ply.TSPlayer, bcfg.OnjoinBot + " kick", false, false);
                    ply.kcount++;
                }

                else if (reader.Read() && ply.kcount > bcfg.KickCountB4Ban)
                {
                    db.Query("UPDATE BotKick SET KickIP = @0 WHERE KickNames = @1", ply.TSPlayer.IP, ply.PlayerName);
                    TShock.Utils.Ban(ply.TSPlayer, bcfg.OnjoinBot + " ban", true, null);
                    ply.kcount++;
                }

                if (ply.TSPlayer.IP == IP && ply.kcount < bcfg.KickCountB4Ban)
                {
                    db.Query("UPDATE BotKick SET KickIP = @0 WHERE KickNames = @1", ply.TSPlayer.IP, ply.PlayerName);
                    TShock.Utils.ForceKick(ply.TSPlayer, bcfg.OnjoinBot.ToString() + " kick", false, false);
                    ply.kcount++;
                }

                else if (ply.TSPlayer.IP == IP && ply.kcount > bcfg.KickCountB4Ban)
                {
                    db.Query("UPDATE BotKick SET KickIP = @0 WHERE KickNames = @1", ply.TSPlayer.IP, ply.PlayerName);
                    TShock.Utils.Ban(ply.TSPlayer, bcfg.OnjoinBot.ToString() + " ban", true, null);
                }
            }
            #endregion
        }
        #endregion

        #region OnGreet
        public void OnGreet(int who, HandledEventArgs e)
        {
            if (bcfg.BotJoinMessage)
            {
                var ply = TShock.Players[who];

                Random r = new Random();
                var rand = r.Next(0,4);

                Utils.GreetMsg(ply, rand);
            }
        }
        #endregion

        #region OnLeave
        public void OnLeave(int who)
        {
            lock (players)
            {
                players.RemoveAll(plr => plr.Index == who);
            }
        }
        #endregion

        #region Bot.OnChat
        public void OnChat(messageBuffer msg, int who, string text, HandledEventArgs e)
        {
            TSPlayer pl = TShock.Players[msg.whoAmI];
            
            if (pl == null)
            {
                return;
            }
            
            if (text == "/")
            {
                if (bcfg.EnableSnark)
                {
                    pl.SendWarningMessage("Correct. That is a command starter. Try writing an actual command now.");

                    e.Handled = true;
                    return;
                }
                else
                {
                    return;
                }
            }
            
            if (BotCommandHandler.CheckForBotCommand(text))
            {
            	Handler.HandleCommand(text, pl);
            }

            Utils.CheckChat(text, pl);
        }
        #endregion

        #region OnUpdate
        public void OnUpdate()
        {
            DateTime now = DateTime.Now;
            lock (bots)
            {
                int botmessagers = 0;
                if (bots.Count > 0)
                {
                    foreach (Bot b in bots)
                    {
                        if (b.Msgtime != 0)
                        {
                            if ((now - lastmsgupdate).TotalMinutes >= b.Msgtime)
                            {
                                if (b.Type == "asay")
                                {
                                    botmessagers++;
                                    if (b.Message.StartsWith("/"))
                                    {
                                        Commands.HandleCommand(TShockAPI.TSPlayer.Server, b.Message);
                                    }
                                    TSPlayer.All.SendMessage(b.Message, b.r, b.g, b.b);

                                    lastmsgupdate = DateTime.Now;
                                    return;
                                }
                            }
                        }
                        else
                        {
                            if ((now - lastmsgupdate).TotalMinutes >= 1)
                            {
                                if (b.Type == "asay")
                                {
                                    botmessagers++;
                                    TSPlayer.All.SendMessage(b.Message, b.r, b.g, b.b);
                                    lastmsgupdate = DateTime.Now;
                                }
                            }
                        }
                    }
                }
            }
            lock (players)
            {
                if (players.Count > 0)
                {
                    foreach (Pl p in players)
                    {
                        int swearers = 0;
                        if (p.ctype == "swear")
                        {
                            swearers++;
                            if ((now - lastswearupdate).TotalSeconds >= 30)
                            {
                                int count = p.scount;
                                p.scount--;
                                p.TSPlayer.SendInfoMessage(string.Format("Your swear count has dropped from {0} to {1}", count, p.scount));
                                lastswearupdate = DateTime.Now;
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}