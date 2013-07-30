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
    [APIVersion(1, 12)]
    public class BotMain : TerrariaPlugin
    {
        public BotConfig bcfg { get; set; }
        public WebRequest request;
        internal static string BotSave { get { return Path.Combine(TShock.SavePath, "BotConfig.json"); } }
        public static List<Bot> bots = new List<Bot>();
        public static List<Pl> players = new List<Pl>();
        public static DateTime lastmsgupdate = DateTime.Now;
        public static DateTime lastswearupdate = DateTime.Now;
        public static Random rid = new Random();
        public static string IP { get; set; }
        public static TSServerPlayer CommandExec = new TSServerPlayer();
        public static Color RBC;
        public static int plycount = 0;

        private static IDbConnection db;

        #region Name, Version, Author, Description, Hooks
        public override string Name
        {
            get { return "Bot"; }
        }

        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
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

        public BotMain(Main game) : base(game)
        {
            bcfg = new BotConfig();
            Order = -1;
        }
        #endregion

        #region OnInitialize
        public void OnInitialize()
        {
            Commands.ChatCommands.Add(new Command(new List<string>() { "bot.bot", "bot.*" }, BotMethod, "/bot"));
            Commands.ChatCommands.Add(new Command(new List<string>() { "bot.reload", "bot.*" }, ReloadCfg, "/botrld"));
            Commands.ChatCommands.Add(new Command(new List<string>() { "bot.badwords", "bot.*" }, BadWords, "/badwords", "/badword", "/bd"));
            Commands.ChatCommands.Add(new Command(new List<string>() { "bot.kickplayer", "bot.*" }, KickPlayers, "/kickplayers", "/kickplayer", "/kp"));

            SetUpConfig();
            SetUpDB();
        }
        #endregion

        #region Database
        public void SetUpDB()
        {
            if (TShock.Config.StorageType.ToLower() == "sqlite")
            {
                db = new SqliteConnection(string.Format("uri=file://{0},Version=3", Path.Combine(TShock.SavePath, "Bot.sqlite")));
            }
            else if (TShock.Config.StorageType.ToLower() == "mysql")
            {
                try
                {
                    var host = TShock.Config.MySqlHost.Split(':');
                    db = new MySqlConnection();
                    db.ConnectionString =
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
            var SQLcreator = new SqlTableCreator(db,
                                                 db.GetSqlType() == SqlType.Sqlite
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
        public void SetUpConfig()
        {
            try
            {
                if (File.Exists(BotSave))
                {
                    bcfg = BotConfig.Read(BotSave);
                }
                else
                {
                    bcfg.Write(BotSave);
                }
            }
            catch (Exception z)
            {
                Log.Error("Error in BotConfig.json");
                Log.Info(z.ToString());
            }
        }
        #endregion

        #region Bot.ReloadCfg
        public void ReloadCfg(CommandArgs z)
        {
            SetUpConfig();
            z.Player.SendWarningMessage("Reloaded Bot config");
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
                        int z = r.Next(1, 256);
                        {
                            bots.Add(new Bot(z, bcfg.OnjoinBot));
                            foreach (Bot b in bots)
                            {
                                if (b.Name == bcfg.OnjoinBot)
                                {
                                    b.msgcol = new Color(bcfg.OnjoinBotColourB, bcfg.OnjoinBotColourG, bcfg.OnjoinBotColourR);
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
            QueryResult reader = db.QueryReader("SELECT * FROM BotKick WHERE KickNames = @0", ply.PlayerName);
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
            #endregion
        }
        #endregion

        #region OnGreet
        public void OnGreet(int who, HandledEventArgs e)
        {
            if (bcfg.BotJoinMessage)
            {
                var ply = TShock.Players[who];

                Random f = new Random();
                int p = f.Next(0, 5);
                if (p == 0)
                { ply.SendMessage(string.Format("{0}: Hi {1}, welcome to {2}!", bcfg.OnjoinBot.ToString(), ply.Name, TShock.Config.ServerNickname), RBC); }
                if (p == 1)
                { ply.SendMessage(string.Format("{0}: Welcome to the land of amaaaaziiiiinnnng, {1}", bcfg.OnjoinBot.ToString(), ply.Name), RBC); }
                if (p == 2)
                { ply.SendMessage(string.Format("{0}: Hallo there, {1}", bcfg.OnjoinBot.ToString(), ply.Name), RBC); }
                if (p == 3)
                { ply.SendMessage(string.Format("{0}: Well hello there, {1}, I'm {0}, and this is {2}!", bcfg.OnjoinBot.ToString(), ply.Name, TShock.Config.ServerNickname), RBC); }
                if (p == 4)
                { ply.SendMessage(string.Format("{0}: Hi there {1}! I hope you enjoy your stay", bcfg.OnjoinBot.ToString(), ply.Name), RBC); }
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

        #region Bot.OnChatCmds
        public void OnChat(messageBuffer msg, int who, string text, HandledEventArgs e)
        {
            TSPlayer pl = TShock.Players[msg.whoAmI];

            if (pl == null)
            {
                e.Handled = true;
                return;
            }
            if (e.Handled)
                return;

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

            #region Swearblocker
            if (bcfg.EnableSwearBlocker)
            {
                if (!e.Handled)
                {
                    foreach (Pl p in players)
                    {
                        if (p.PlayerName == pl.Name)
                        {
                            var parts = text.Split();
                            foreach (string s in parts)
                            {
                                QueryResult reader = db.QueryReader("SELECT * FROM BotSwear WHERE SwearBlock = @0", s);
                                if (reader.Read())
                                {
                                    if (bcfg.SwearBlockAction == "kick")
                                    {
                                        p.scount++;
                                        p.TSPlayer.SendWarningMessage(string.Format("Your swear warning count has risen! It is now: {0}", p.scount));
                                        if (p.scount >= bcfg.SwearBlockChances)
                                        {
                                            TShock.Utils.ForceKick(pl, "Swearing", false, false);
                                            p.scount = 0;
                                        }
                                        e.Handled = true;
                                    }
                                    else if (bcfg.SwearBlockAction == "mute")
                                    {
                                        p.scount++;
                                        p.TSPlayer.SendWarningMessage(string.Format("Your swear warning count has risen! It is now: {0}/{1}", p.scount, bcfg.SwearBlockChances));
                                        if (p.scount >= bcfg.SwearBlockChances)
                                        {
                                            pl.mute = true;
                                            pl.SendWarningMessage("You have been muted for swearing!");
                                            p.scount = 0;
                                        }
                                        e.Handled = true;
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
                if (bcfg.EnableYoloSwagBlock)
                {
                    if (text.StartsWith("yolo") || text.Contains("yolo") || text.StartsWith("YOLO") || text.Contains("YOLO") || text.StartsWith("Y.O.L.O") || text.Contains("Y.O.L.O") || text.StartsWith("Yolo") || text.Contains("Yolo"))
                    {
                        e.Handled = true;
                        var plr = TShock.Utils.FindPlayer(pl.Name)[0];
                        if (bcfg.FailNoobAction == "kill")
                        {
                            plr.DamagePlayer(999999);
                            TSPlayer.All.SendMessage(string.Format("{0} lived more than once. Liar.", plr.Name), Color.CadetBlue);
                        }
                        else if (bcfg.FailNoobAction == "kick")
                        {
                            TShock.Utils.ForceKick(plr, bcfg.FailNoobKickReason, false, false);
                        }
                        else if (bcfg.FailNoobAction == "mute")
                        {
                            plr.mute = true;
                            plr.SendWarningMessage("You've been muted for yoloing.");
                        }
                    }
                    if (text.StartsWith("Swag") || text.Contains("Swag") || text.StartsWith("SWAG") || text.Contains("SWAG") || text.StartsWith("S.W.A.G") || text.Contains("S.W.A.G") || text.StartsWith("swag") || text.Contains("swag") || text.StartsWith("Swa g") || text.Contains("Swa g") || text.StartsWith("swa g") || text.Contains("swa g"))
                    {
                        e.Handled = true;
                        var player = TShock.Utils.FindPlayer(pl.Name);
                        var plr = player[0];
                        if (bcfg.FailNoobAction == "kill")
                        {
                            plr.DamagePlayer(999999);
                            TSPlayer.All.SendMessage(string.Format("Swag doesn't stop your players from DYING, {0}", plr.Name), Color.CadetBlue);
                        }
                        else if (bcfg.FailNoobAction == "kick")
                        {
                            TShock.Utils.ForceKick(plr, bcfg.FailNoobKickReason, false, false);
                        }
                        else if (bcfg.FailNoobAction == "mute")
                        {
                            plr.mute = true;
                            plr.SendWarningMessage("You have been muted for swagging.");
                        }
                    }
                }
            }
            #endregion

            foreach (Bot b in bots)
            {
                if (b.Name == bcfg.CommandBot)
                {
                    if (text.StartsWith(bcfg.CommandChar))
                    {
                        string[] words = text.Split();
                        e.Handled = true;

                        #region Bot.HelpCmds
                        if (words[1] == "help")
                        {
                            TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: help", pl.Group.Prefix, pl.Name, pl.Group.Suffix, bcfg.CommandChar, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                            Commands.HandleCommand(pl, "/help");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(string.Format("{0} used {1} to execute: {2}", pl.Name, bcfg.CommandBot, words[1]));
                            Console.ResetColor();
                            Log.Info(string.Format("{0} used {1} to execute {2}", pl.Name, bcfg.CommandBot, words[1]));
                        }
                        if (words[1] == "help-" && words[2] == "register")
                        {
                            TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: help- register", pl.Group.Prefix, pl.Name, pl.Group.Suffix, bcfg.CommandChar, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                            TSPlayer.All.SendMessage(string.Format("{0}: To register, use /register <password>", bcfg.CommandBot), b.msgcol);
                            TSPlayer.All.SendMessage(string.Format("<password> can be anything, and you define it personally."), b.msgcol);
                            TSPlayer.All.SendMessage(string.Format("Always remember to keep your password secure!"), b.msgcol);
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(string.Format("{0} used {1} to execute: {2} {3}", pl.Name, bcfg.CommandBot, words[1], words[2]));
                            Console.ResetColor();
                            Log.Info(string.Format("{0} used {1} to execute: {2} {3}", pl.Name, bcfg.CommandBot, words[1], words[2]));
                        }
                        if (words[1] == "help-" && words[2] == "item")
                        {
                            TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: help- item", pl.Group.Prefix, pl.Name, pl.Group.Suffix, bcfg.CommandChar, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                            TSPlayer.All.SendMessage(string.Format("{0}: To spawn any item, use the command /item", bcfg.CommandBot), b.msgcol);
                            TSPlayer.All.SendMessage(string.Format("Items that are made of multiple words MUST be wrapped in quotes"), b.msgcol);
                            TSPlayer.All.SendMessage(string.Format("Eg: /item \"hallowed repeater\""), b.msgcol);
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(string.Format("{0} used {1} to execute: {2} {3}", pl.Name, bcfg.CommandBot, words[1], words[2]));
                            Console.ResetColor();
                            Log.Info(string.Format("{0} used {1} to execute: {2} {3}", pl.Name, bcfg.CommandBot, words[1], words[2]));
                        }
                        #endregion

                        #region Bot.KillCmd
                        if (words[1] == "kill")
                        {
                            TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: kill {4}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, bcfg.CommandChar, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                            if (pl.Group.HasPermission("kill"))
                            {
                                var player = TShock.Utils.FindPlayer(words[2]);
                                var plr = player[0];
                                plr.DamagePlayer(999999);
                                TSPlayer.All.SendMessage(string.Format("{0}: {1}: I just killed {2}!", bcfg.CommandBot, pl.Name, plr.Name), b.msgcol);
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                                Console.ResetColor();
                                Log.Info(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                            }
                            else
                            {
                                pl.SendMessage(string.Format("{0}: Sorry {1}, but you don't have permission to use kill", bcfg.CommandBot, pl.Name), b.msgcol);
                                Log.Info(string.Format("{0} failed to use {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                            }
                        }
                        #endregion

                        #region Bot.Greeting
                        if ((words[1] == "Hi" || words[1] == "hi" || words[1] == "hello"))
                        {
                            TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: {4}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, bcfg.CommandChar, words[1]), pl.Group.R, pl.Group.G, pl.Group.B);
                            TSPlayer.All.SendMessage(string.Format("{0}: Hello {1}, how are you?", bcfg.CommandBot, pl.Name), b.msgcol);
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(string.Format("{0} used {1} to execute: {2}", pl.Name, bcfg.CommandBot, words[1]));
                            Console.ResetColor();
                            Log.Info(string.Format("{0} used {1} to execute {2}", pl.Name, bcfg.CommandBot, words[1]));
                        }
                        #endregion

                        #region Bot.Responses
                        if ((words[1] == "good") || (words[1] == "Good"))
                        {
                            Random z = new Random();
                            int q = z.Next(0, 5);
                            {
                                TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: {4}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, bcfg.CommandChar, words[1]), pl.Group.R, pl.Group.G, pl.Group.B);
                                if (q == 0)
                                { TSPlayer.All.SendMessage(string.Format("{0}: That's great. I'll tell you how I am, if you ask me ;)", bcfg.CommandBot), b.msgcol); }
                                if (q == 1)
                                { TSPlayer.All.SendMessage(string.Format("{0}: Hah, nice. What's the bet I'm better though? >:D", bcfg.CommandBot), b.msgcol); }
                                if (q == 2)
                                { TSPlayer.All.SendMessage(string.Format("{0}: Nice to hear", bcfg.CommandBot), b.msgcol); }
                                if (q == 3)
                                { TSPlayer.All.SendMessage(string.Format("{0}: ...'kay", bcfg.CommandBot), b.msgcol); }
                                if (q == 4)
                                { TSPlayer.All.SendMessage(string.Format("{0}: Good, you say? Did you bring me a present then?", bcfg.CommandBot), b.msgcol); }
                                if (q == 5)
                                { TSPlayer.All.SendMessage(string.Format("{0}: I'm always happiest with good friends... And lots of alcohol. Want to join me?", bcfg.CommandBot), b.msgcol); }
                                Log.Info(string.Format("{0} used {1} to execute: {2}", pl.Name, bcfg.CommandBot, words[1]));
                            }
                        }
                        else if ((words[1] == "bad") || (words[1] == "Bad"))
                        {
                            Random h = new Random();
                            int cf = h.Next(0, 4);
                            {
                                TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: quoteID {4}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, bcfg.CommandChar, words[1]), pl.Group.R, pl.Group.G, pl.Group.B);
                                if (cf == 0)
                                { TSPlayer.All.SendMessage(string.Format("{1}: Well {0}... Always remember, the new day is a great big fish.", pl.Name, bcfg.CommandBot), b.msgcol); }
                                if (cf == 1)
                                { TSPlayer.All.SendMessage(string.Format("{1}: Poor {0}... It could be worse though. You could have crabs.", pl.Name, bcfg.CommandBot), b.msgcol); }
                                if (cf == 2)
                                {
                                    TSPlayer.All.SendMessage(string.Format("{1}: There there, {0}", pl.Name, bcfg.CommandBot), b.msgcol);
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
                                    TSPlayer.All.SendMessage(string.Format("*{1} hugs {0}", pl.Name, bcfg.CommandBot), b.msgcol);
                                }
                                if (cf == 3)
                                { TSPlayer.All.SendMessage(string.Format("{1}: {0}, What you need is a good sleep... And a monkey", pl.Name, bcfg.CommandBot), b.msgcol); }
                                if (cf == 4)
                                { TSPlayer.All.SendMessage(string.Format("{1}: Feeling down eh? What you need is a cat.", pl.Name, bcfg.CommandBot), b.msgcol); }
                                Log.Info(string.Format("{0} used {1} to execute: {2}", pl.Name, bcfg.CommandBot, words[1]));
                            }
                        }
                        #endregion

                        #region Bot.Hug (heal)
                        if (words[1] == "hug")
                        {
                            TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: hug {4}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, bcfg.CommandChar, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                            var player = TShock.Utils.FindPlayer(words[2]);
                            var plr = player[0];
                            Item heart = TShock.Utils.GetItemById(58);
                            Item star = TShock.Utils.GetItemById(184);
                            for (int i = 0; i < 20; i++)
                                plr.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
                            for (int i = 0; i < 10; i++)
                                plr.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
                            TSPlayer.All.SendMessage(string.Format("*{1} hugs {0}", plr.Name, bcfg.CommandBot), b.msgcol);
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                            Console.ResetColor();
                            Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                        }
                        #endregion

                        #region Bot.SetAFK
                        if (words[1] == "afk")
                        {
                            TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: afk", pl.Group.Prefix, pl.Name, pl.Group.Suffix, bcfg.CommandChar), pl.Group.R, pl.Group.G, pl.Group.B);
                            Commands.HandleCommand(pl, "/afk");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(string.Format("{0} used {1} to execute: {2}", pl.Name, bcfg.CommandBot, words[1]));
                            Console.ResetColor();
                            Log.Info(string.Format("{0} used {1} to execute {2}", pl.Name, bcfg.CommandBot, words[1]));
                        }
                        #endregion

                        #region Bot.Ban, Bot.Kick, Bot.Mute
                        if (words[1] == "ban")
                        {
                            if (pl.Group.HasPermission("ban"))
                            {
                                var player = TShock.Utils.FindPlayer(words[2]);
                                var plr = player[0];
                                TShock.Utils.Ban(plr, bcfg.CommandBot + " ban", false, null);
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                                Console.ResetColor();
                                Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                            }
                            else
                            {
                                pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use ban.", pl.Name, bcfg.CommandBot), b.msgcol);
                                Log.Info(string.Format("{0} failed to use {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                            }
                        }
                        if (words[1] == "kick")
                        {
                            if (pl.Group.HasPermission("kick"))
                            {
                                var player = TShock.Utils.FindPlayer(words[2]);
                                var plr = player[0];
                                TShock.Utils.Kick(plr, bcfg.CommandBot + " forcekick", false, false, null, false);
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                                Console.ResetColor();
                                Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                            }
                            else
                            {
                                pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use kick.", pl.Name, bcfg.CommandBot), b.msgcol);
                                Log.Info(string.Format("{0} failed to use {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                            }
                        }
                        if (words[1] == "mute")
                        {
                            if (pl.Group.HasPermission("mute"))
                            {
                                TSPlayer.All.SendMessage(string.Format("{0}: {1}: mute {2}", pl.Name, bcfg.CommandChar, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                                var player = TShock.Utils.FindPlayer(words[2]);
                                var plr = player[0];
                                plr.mute = true;
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                                Console.ResetColor();
                                Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                            }
                            else
                            {
                                pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use mute.", pl.Name, bcfg.CommandBot), b.msgcol);
                                Log.Info(string.Format("{0} failed to used {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                            }
                        }
                        if (words[1] == "unmute")
                        {
                            if (pl.Group.HasPermission("mute"))
                            {
                                TSPlayer.All.SendMessage(string.Format("{0}: {1}: unmute {2}", pl.Name, bcfg.CommandBot, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                                var player = TShock.Utils.FindPlayer(words[2]);
                                var plr = player[0];
                                plr.mute = false;
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                                Console.ResetColor();
                                Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
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
                                TShock.Utils.Kick(plr, bcfg.CommandBot + " forcekick", true, false, null, false);
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                                Console.ResetColor();
                                Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                            }
                            else
                            {
                                pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use super kick.", pl.Name, bcfg.CommandBot), b.msgcol);
                                Log.Info(string.Format("{0} failed to use {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                            }
                        }
                        if (words[1] == "sban")
                        {
                            if (pl.Group.HasPermission("sban"))
                            {
                                var player = TShock.Utils.FindPlayer(words[2]);
                                var plr = player[0];
                                TShock.Utils.Ban(plr, bcfg.CommandBot + " forceban", true, null);
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                                Console.ResetColor();
                                Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, bcfg.CommandBot, words[1], plr.Name));
                            }
                            else
                            {
                                pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use super ban.", pl.Name, bcfg.CommandBot), b.msgcol);
                                Log.Info(string.Format("{0} failed to use {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                            }
                        }
                        #endregion

                        #region Bot.CommandsList
                        if (words[1] == "commands" || words[1] == "cmds")
                        {
                            TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: {4}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, bcfg.CommandChar, words[1]), pl.Group.R, pl.Group.G, pl.Group.B);
                            pl.SendMessage(string.Format("{1}: {0}; My executable commands are:", pl.Name, bcfg.CommandBot), b.msgcol);
                            pl.SendMessage(string.Format("help, help- register, help- item, kill, hi, good, How are you?, butcher"), b.msgcol);
                            pl.SendMessage(string.Format("hug, afk, ban, kick, mute, unmute, insult, cmds, google, quote"), b.msgcol);
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(string.Format("{0} used {1} to execute: {2}", pl.Name, bcfg.CommandBot, words[1]));
                            Console.ResetColor();
                            Log.Info(string.Format("{0} used {1}'s command sender to check {1}'s commands.", pl.Name, bcfg.CommandBot));
                        }
                        #endregion

                        #region Bot.Butcher
                        if (words[1] == "butcher")
                        {
                            TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: butcher", pl.Group.Prefix, pl.Name, pl.Group.Suffix, bcfg.CommandChar), pl.Group.R, pl.Group.G, pl.Group.B);
                            if (!pl.Group.HasPermission("butcher"))
                            {
                                Random r = new Random();
                                int p = r.Next(1, 100);
                                if (p <= bcfg.ButcherCmdPct)
                                {
                                    Commands.HandleCommand(CommandExec, "/butcher");
                                    TSPlayer.All.SendMessage(string.Format("{1}: {0}: I butchered all hostile NPCs!", pl.Name, bcfg.CommandBot), b.msgcol);
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine(string.Format("{0} used {1} to execute: {2}.", pl.Name, bcfg.CommandBot, words[1]));
                                    Console.ResetColor();
                                    Log.Info(string.Format("{0} used {1} to execute {2}", pl.Name, bcfg.CommandBot, words[1]));
                                }
                                else
                                {
                                    TSPlayer.All.SendMessage(string.Format("{1}: Sorry {0}, you rolled a {2}. You need to roll less than {3} to butcher", pl.Name, bcfg.CommandBot, p, bcfg.ButcherCmdPct), b.msgcol);
                                }
                            }
                            else
                            {
                                Commands.HandleCommand(pl, "/butcher");
                                TSPlayer.All.SendMessage(string.Format("{1}: {0}: I butchered all hostile NPCs!", pl.Name, bcfg.CommandBot), b.msgcol);
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine(string.Format("{0} used {1} to execute: {2}.", pl.Name, bcfg.CommandBot, words[1]));
                                Console.ResetColor();
                                Log.Info(string.Format("{0} used {1} to execute {2}", pl.Name, bcfg.CommandBot, words[1]));
                            }
                        }
                        #endregion

                        #region Bot.How Are you?
                        if (words[1] == "How" || words[1] == "how" && words[2] == "are".ToLower() && words[3].ToString() == "you?".ToLower())
                        {
                            TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: How are you?", pl.Group.Prefix, pl.Name, pl.Group.Suffix, bcfg.CommandChar), pl.Group.R, pl.Group.G, pl.Group.B);
                            Random r = new Random();
                            int p = r.Next(1, 10);
                            if (p == 1)
                            {
                                TSPlayer.All.SendMessage(string.Format("{1}: {0}, I am feelings quite well today, thank you!", pl.Name, bcfg.CommandBot), b.msgcol);
                            }
                            else if (p > 3 && p < 6)
                            {
                                TSPlayer.All.SendMessage(string.Format("{1}: {0}, I'm feeling a bit down. Might go get drunk later.", pl.Name, bcfg.CommandBot), b.msgcol);
                            }
                            else if (p == 7 || p == 6)
                            {
                                TSPlayer.All.SendMessage(string.Format("{1}: {0}, Better than you. 'cos I'm AWESOME!", pl.Name, bcfg.CommandBot), b.msgcol);
                            }
                            else if (p > 8 && p != 10)
                            {
                                TSPlayer.All.SendMessage(string.Format("{1}: {0}, I'm seeing unicorns and gnomes. How do you think I am?", pl.Name, bcfg.CommandBot), b.msgcol);
                            }
                            else if (p == 10)
                            {
                                TSPlayer.All.SendMessage(string.Format("{1}: {0}, I just won the lottery. Stop being so poor in front of me.", pl.Name, bcfg.CommandBot), b.msgcol);
                            }
                        }
                        #endregion

                        #region Bot.PlayerInsult
                        if (words[1] == "insult")
                        {
                            var ply = TShock.Utils.FindPlayer(words[2]);
                            var plr = ply[0].Name;
                            TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: insult {4}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, bcfg.CommandChar, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                            Random r = new Random();
                            int p = r.Next(1, 10);

                            if (p == 1)
                            { TSPlayer.All.SendMessage(string.Format("{1}: Yo, {0}, I bet your mother is a nice lady!", plr, bcfg.CommandBot), b.msgcol); }
                            if (p == 2)
                            { TSPlayer.All.SendMessage(string.Format("{1}: I bet {0}'s mother was a hamster, and their father smelled of elderberries.", plr, bcfg.CommandBot), b.msgcol); }
                            if (p == 3)
                            { TSPlayer.All.SendMessage(string.Format("{1}: I bet {0} uses the term swag liberally.", plr, bcfg.CommandBot), b.msgcol); }
                            if (p == 4)
                            { TSPlayer.All.SendMessage(string.Format("{1}: {0} is such a... twig!", plr), b.msgcol); }
                            if (p == 5)
                            { TSPlayer.All.SendMessage(string.Format("{0}: ...But I'm a nice bot!... Sometimes", bcfg.CommandBot), b.msgcol); }
                            if (p == 6)
                            { TSPlayer.All.SendMessage(string.Format("{1}: {0} is such a! a... erm... thing!", plr, bcfg.CommandBot), b.msgcol); }
                            if (p == 7)
                            { TSPlayer.All.SendMessage(string.Format("{1}: {0}, I'm so awesome you should feel insulted already.", plr, bcfg.CommandBot), b.msgcol); }
                            if (p == 9)
                            { TSPlayer.All.SendMessage(string.Format("{1}: {0}... You remind me of someone named {2}.", plr, bcfg.CommandBot, bcfg.GenericInsultName), b.msgcol); }
                            if (p == 10)
                            { TSPlayer.All.SendMessage(string.Format("{1}: Don't tell me what to do, {0}!", pl.Name), b.msgcol); }
                        }
                        #endregion

                        #region Bot.Website
                        if (words[1] == "g" || words[1] == "google")
                        {
                            int count = 0;
                            TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: {4} {5}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, bcfg.CommandChar, words[1], words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
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
                                        TSPlayer.All.SendMessage(string.Format("{0}: Website for {1}: {2}", bcfg.CommandBot, words[2], website), b.msgcol);
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
                                        TSPlayer.All.SendMessage(string.Format("{0}: Website for {1}: {2}", bcfg.CommandBot, words[2], website), b.msgcol);
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
                                        TSPlayer.All.SendMessage(string.Format("{0}: Website for {1}: {2}", bcfg.CommandBot, words[2], website), b.msgcol);
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
                                        TSPlayer.All.SendMessage(string.Format("{0}: Website for {1}: {2}", bcfg.CommandBot, words[2], website), b.msgcol);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                count++;
                                //Console.WriteLine(".co failed");
                                //Console.WriteLine(x.ToString());
                                if (count == 4)
                                {
                                    TSPlayer.All.SendMessage(string.Format("{0}: Sorry {1}, I cannot find a website for {2}", b.Name, pl.Name, words[2]), b.msgcol);
                                }
                            }
                            return;
                        }
                        #endregion
                    }
                }
            }
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
                        if (b.msgtime != 0)
                        {
                            if ((now - lastmsgupdate).TotalMinutes >= b.msgtime)
                            {
                                if (b.type == "asay")
                                {
                                    botmessagers++;
                                    if (b.message.StartsWith("/"))
                                    {
                                        Commands.HandleCommand(CommandExec, b.message);
                                    }
                                    TSPlayer.All.SendMessage(b.message, b.msgcol);

                                    lastmsgupdate = DateTime.Now;
                                    return;
                                }
                            }
                        }
                        else
                        {
                            if ((now - lastmsgupdate).TotalMinutes >= 1)
                            {
                                if (b.type == "asay")
                                {
                                    botmessagers++;
                                    TSPlayer.All.SendMessage(b.message, b.msgcol);
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

        #region BotMethod
        public void BotMethod(CommandArgs z)
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
                id = rid.Next(1, 256);
                foreach (Bot b in bots)
                {
                    if (id == b.Index)
                    {
                        id = rid.Next(1, 256);
                    }
                }
                bots.Add(new Bot(id, name));
                TSPlayer.All.SendInfoMessage(string.Format("Bot '{0}' was summoned by {1}", name, plyname));
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("Bot '{0}' was summoned by {1}", name, plyname));
                Console.ResetColor();
                Log.Warn(string.Format("{0} made a bot named '{1}' join", plyname, name));
            }
            else if (z.Parameters[0] == "leave")
            {
                TSPlayer.All.SendWarningMessage(string.Format("{0} made bot {1} leave.", z.Player.Name, name));
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("{0} made bot '{1}' leave.", z.Player.Name, name));
                Console.ResetColor();
                foreach (Bot b in bots)
                {
                    if (b.Name == z.Parameters[1])
                    {
                        bots.RemoveAll(b1 => b1.Name == z.Parameters[1]);
                    }
                }
            }

           
            #endregion

            #region Bot.Colour + Bot.Talk
            else if (z.Parameters[0] == "colour" || z.Parameters[0] == "color")
            {
                foreach (Bot b in bots)
                {
                    if (b.Name == z.Parameters[1].ToString())
                    {
                        b.r = Convert.ToByte(z.Parameters[2]);
                        b.g = Convert.ToByte(z.Parameters[3]);
                        b.b = Convert.ToByte(z.Parameters[4]);
                        z.Player.SendSuccessMessage(string.Format("Set bot {3}'s colo(u)r to {0}, {1}, {2}", b.r, b.g, b.b, b.Name));
                    }
                    else
                    {
                        z.Player.SendWarningMessage(string.Format("Bot name '{0}' does not exist", z.Parameters[1]));
                    }
                }
            }
            else if (z.Parameters[0] == "say")
            {
                string text = z.Parameters[2];
                foreach (Bot b in bots)
                {
                    if (b.Name == z.Parameters[1].ToString())
                    {
                        string message = string.Format("{0}: {1}", b.Name, text);

                        b.msgcol = new Color(b.r, b.g, b.b);
                        TSPlayer.All.SendMessage(message, b.msgcol);
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
                foreach (Bot b in bots)
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
                foreach (Bot b in bots)
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
                        bots.RemoveAll(b1 => b1.Name.ToLower() == z.Parameters[1].ToLower());
                    }
                    else
                    {
                        z.Player.SendWarningMessage("Could not find bot '" + z.Parameters[1] + "'");
                    }
                }
            }

            else if (z.Parameters[0] == "killall")
            {
                bots.Clear();
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
                foreach (Bot b in bots)
                {
                    if (b.Name == z.Parameters[1])
                    {
                        b.msgtime = Convert.ToInt32(z.Parameters[3]);
                        b.message = string.Format("{0}: {1}", b.Name, z.Parameters[2]);
                        z.Player.SendSuccessMessage(string.Format("Bot '{0}' will now broadcast your message every {1} minute(s).", b.Name, b.msgtime));
                        b.type = "asay";
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(string.Format("{0} set bot '{1}' to broadcast a message every {2} minute(s)", z.Player.Name, b.Name, b.msgtime));
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(string.Format("Message: {0}", b.message));
                        Console.ResetColor();
                    }
                }
            }
            else if (z.Parameters[0] == "mclear")
            {
                foreach (Bot b in bots)
                {
                    if (b.Name == z.Parameters[1])
                    {
                        b.msgtime = -1;
                        b.message = string.Empty;
                        b.type = "";
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
        public void BadWords(CommandArgs z)
        {
            if (z.Parameters.Count < 2)
            {
                z.Player.SendWarningMessage("Invalid syntax. Try //badwords [add/del] word");
            }
            else if (z.Parameters[0] == "add")
            {
                QueryResult reader = db.QueryReader("SELECT * FROM BotSwear WHERE SwearBlock = @0", z.Parameters[1]);
                if (!reader.Read())
                {
                    db.Query("INSERT INTO BotTable (SwearBlock) VALUES (@0)", z.Parameters[1]);
                    z.Player.SendMessage(string.Format("Added word {0} into the banned word list.", z.Parameters[1]), Color.CadetBlue);
                }
                else
                {
                    z.Player.SendWarningMessage(string.Format("Word {0} already exists in the swear list.", z.Parameters[1]));
                }
            }
            else if (z.Parameters[0] == "del")
            {
                QueryResult reader = db.QueryReader("SELECT * FROM BotSwear WHERE SwearBlock = @0", z.Parameters[1]);
                if (reader.Read())
                {
                    db.Query("DELETE FROM BotTable WHERE SwearBlock = @0", z.Parameters[1]);
                    z.Player.SendMessage(string.Format("Delete word {0} from the banned word list.", z.Parameters[1]), Color.CadetBlue);
                }
                else
                {
                    z.Player.SendWarningMessage(string.Format("Word {0} does not exist in the swear list.", z.Parameters[1]));
                }
            }
        }
        #endregion

        #region Bot.KickPlayers
        public void KickPlayers(CommandArgs z)
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
                    QueryResult reader = db.QueryReader("SELECT * FROM BotKick WHERE KickNames = @0", z.Parameters[1]);
                    if (!reader.Read())
                    {
                        db.Query("INSERT INTO BotTable (KickNames) VALUES (@0)", z.Parameters[1]);
                        z.Player.SendMessage(string.Format("Added player {0} to the joinkick player list.", z.Parameters[1]), Color.CadetBlue);
                    }
                    else
                    {
                        z.Player.SendWarningMessage(string.Format("Player {0} already exists in the joinkick player list!", z.Parameters[1]));
                    }
                }
                else if (z.Parameters[0] == "del")
                {
                    QueryResult reader = db.QueryReader("SELECT * FROM BotKick WHERE KickNames = @0", z.Parameters[1]);
                    if (reader.Read())
                    {
                        db.Query("DELETE FROM BotTable WHERE KickNames = @0", z.Parameters[1]);
                        z.Player.SendMessage(string.Format("Delete player {0} from the joinkick player list!", z.Parameters[1]), Color.CadetBlue);
                    }
                    else
                    {
                        z.Player.SendWarningMessage(string.Format("Player {0} does not exist on the joinkick player list!", z.Parameters[1]));
                    }
                }
            }
        }
        #endregion
    }
}