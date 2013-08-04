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
            try
            {
                if (File.Exists(BotMain.BotSave))
                {
                    BotMain.bcfg = BotConfig.Read(BotMain.BotSave);
                }
                else
                {
                    BotMain.bcfg.Write(BotMain.BotSave);
                }
            }
            catch (Exception z)
            {
                Log.Error("Error in BotConfig.json");
                Log.Info(z.ToString());
            }
        }
        #endregion

        #region ChatCommands
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
                        if (text.ToLower().Contains("yolo") || text.ToLower().Contains("YOLO") || text.ToLower().Contains("Y.O.L.O") || text.ToLower().Contains("Yolo"))
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
                    if (text.StartsWith("Swag") || text.Contains("Swag") || text.StartsWith("SWAG") || text.Contains("SWAG") || text.StartsWith("S.W.A.G") || text.Contains("S.W.A.G") || text.StartsWith("swag") || text.Contains("swag") || text.StartsWith("Swa g") || text.Contains("Swa g") || text.StartsWith("swa g") || text.Contains("swa g"))
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
                                    TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: help", pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                                    Commands.HandleCommand(pl, "/help");
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine(string.Format("{0} used {1} to execute: {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                    Console.ResetColor();
                                    Log.Info(string.Format("{0} used {1} to execute {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                }
                                if (words[1] == "help-" && words[2] == "register")
                                {
                                    TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: help- register", pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                                    TSPlayer.All.SendMessage(string.Format("{0}: To register, use /register <password>", BotMain.bcfg.CommandBot), b.msgcol);
                                    TSPlayer.All.SendMessage(string.Format("<password> can be anything, and you define it personally."), b.msgcol);
                                    TSPlayer.All.SendMessage(string.Format("Always remember to keep your password secure!"), b.msgcol);
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine(string.Format("{0} used {1} to execute: {2} {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], words[2]));
                                    Console.ResetColor();
                                    Log.Info(string.Format("{0} used {1} to execute: {2} {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], words[2]));
                                }
                                if (words[1] == "help-" && words[2] == "item")
                                {
                                    TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: help- item", pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                                    TSPlayer.All.SendMessage(string.Format("{0}: To spawn any item, use the command /item", BotMain.bcfg.CommandBot), b.msgcol);
                                    TSPlayer.All.SendMessage(string.Format("Items that are made of multiple words MUST be wrapped in quotes"), b.msgcol);
                                    TSPlayer.All.SendMessage(string.Format("Eg: /item \"hallowed repeater\""), b.msgcol);
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine(string.Format("{0} used {1} to execute: {2} {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], words[2]));
                                    Console.ResetColor();
                                    Log.Info(string.Format("{0} used {1} to execute: {2} {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], words[2]));
                                }
                                #endregion

                                #region Bot.KillCmd
                                if (words[1] == "kill")
                                {
                                    TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: kill {4}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                                    if (pl.Group.HasPermission("kill"))
                                    {
                                        var player = TShock.Utils.FindPlayer(words[2]);
                                        var plr = player[0];
                                        plr.DamagePlayer(999999);
                                        TSPlayer.All.SendMessage(string.Format("{0}: {1}: I just killed {2}!", BotMain.bcfg.CommandBot, pl.Name, plr.Name), b.msgcol);
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                        Console.ResetColor();
                                        Log.Info(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                    }
                                    else
                                    {
                                        pl.SendMessage(string.Format("{0}: Sorry {1}, but you don't have permission to use kill", BotMain.bcfg.CommandBot, pl.Name), b.msgcol);
                                        Log.Info(string.Format("{0} failed to use {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                                    }
                                }
                                #endregion

                                #region Bot.Greeting
                                if ((words[1] == "Hi" || words[1] == "hi" || words[1] == "hello"))
                                {
                                    TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: {4}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[1]), pl.Group.R, pl.Group.G, pl.Group.B);
                                    TSPlayer.All.SendMessage(string.Format("{0}: Hello {1}, how are you?", BotMain.bcfg.CommandBot, pl.Name), b.msgcol);
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine(string.Format("{0} used {1} to execute: {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                    Console.ResetColor();
                                    Log.Info(string.Format("{0} used {1} to execute {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                }
                                #endregion

                                #region Bot.Responses
                                if ((words[1] == "good") || (words[1] == "Good"))
                                {
                                    Random z = new Random();
                                    int q = z.Next(0, 5);
                                    {
                                        TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: {4}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[1]), pl.Group.R, pl.Group.G, pl.Group.B);
                                        if (q == 0)
                                        { TSPlayer.All.SendMessage(string.Format("{0}: That's great. I'll tell you how I am, if you ask me ;)", BotMain.bcfg.CommandBot), b.msgcol); }
                                        if (q == 1)
                                        { TSPlayer.All.SendMessage(string.Format("{0}: Hah, nice. What's the bet I'm better though? >:D", BotMain.bcfg.CommandBot), b.msgcol); }
                                        if (q == 2)
                                        { TSPlayer.All.SendMessage(string.Format("{0}: Nice to hear", BotMain.bcfg.CommandBot), b.msgcol); }
                                        if (q == 3)
                                        { TSPlayer.All.SendMessage(string.Format("{0}: ...'kay", BotMain.bcfg.CommandBot), b.msgcol); }
                                        if (q == 4)
                                        { TSPlayer.All.SendMessage(string.Format("{0}: Good, you say? Did you bring me a present then?", BotMain.bcfg.CommandBot), b.msgcol); }
                                        if (q == 5)
                                        { TSPlayer.All.SendMessage(string.Format("{0}: I'm always happiest with good friends... And lots of alcohol. Want to join me?", BotMain.bcfg.CommandBot), b.msgcol); }
                                        Log.Info(string.Format("{0} used {1} to execute: {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                    }
                                }
                                else if ((words[1] == "bad") || (words[1] == "Bad"))
                                {
                                    Random h = new Random();
                                    int cf = h.Next(0, 4);
                                    {
                                        TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: quoteID {4}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[1]), pl.Group.R, pl.Group.G, pl.Group.B);
                                        if (cf == 0)
                                        { TSPlayer.All.SendMessage(string.Format("{1}: Well {0}... Always remember, the new day is a great big fish.", pl.Name, BotMain.bcfg.CommandBot), b.msgcol); }
                                        if (cf == 1)
                                        { TSPlayer.All.SendMessage(string.Format("{1}: Poor {0}... It could be worse though. You could have crabs.", pl.Name, BotMain.bcfg.CommandBot), b.msgcol); }
                                        if (cf == 2)
                                        {
                                            TSPlayer.All.SendMessage(string.Format("{1}: There there, {0}", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
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
                                            TSPlayer.All.SendMessage(string.Format("*{1} hugs {0}", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
                                        }
                                        if (cf == 3)
                                        { TSPlayer.All.SendMessage(string.Format("{1}: {0}, What you need is a good sleep... And a monkey", pl.Name, BotMain.bcfg.CommandBot), b.msgcol); }
                                        if (cf == 4)
                                        { TSPlayer.All.SendMessage(string.Format("{1}: Feeling down eh? What you need is a cat.", pl.Name, BotMain.bcfg.CommandBot), b.msgcol); }
                                        Log.Info(string.Format("{0} used {1} to execute: {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                    }
                                }
                                #endregion

                                #region Bot.Hug (heal)
                                if (words[1] == "hug")
                                {
                                    TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: hug {4}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                                    var player = TShock.Utils.FindPlayer(words[2]);
                                    var plr = player[0];
                                    Item heart = TShock.Utils.GetItemById(58);
                                    Item star = TShock.Utils.GetItemById(184);
                                    for (int i = 0; i < 20; i++)
                                        plr.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
                                    for (int i = 0; i < 10; i++)
                                        plr.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
                                    TSPlayer.All.SendMessage(string.Format("*{1} hugs {0}", plr.Name, BotMain.bcfg.CommandBot), b.msgcol);
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                    Console.ResetColor();
                                    Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                }
                                #endregion

                                #region Bot.SetAFK
                                if (words[1] == "afk")
                                {
                                    TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: afk", pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar), pl.Group.R, pl.Group.G, pl.Group.B);
                                    Commands.HandleCommand(pl, "/afk");
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine(string.Format("{0} used {1} to execute: {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                    Console.ResetColor();
                                    Log.Info(string.Format("{0} used {1} to execute {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
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
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                        Console.ResetColor();
                                        Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                    }
                                    else
                                    {
                                        pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use ban.", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
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
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                        Console.ResetColor();
                                        Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                    }
                                    else
                                    {
                                        pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use kick.", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
                                        Log.Info(string.Format("{0} failed to use {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                                    }
                                }
                                if (words[1] == "mute")
                                {
                                    if (pl.Group.HasPermission("mute"))
                                    {
                                        TSPlayer.All.SendMessage(string.Format("{0}: {1}: mute {2}", pl.Name, BotMain.bcfg.CommandChar, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                                        var player = TShock.Utils.FindPlayer(words[2]);
                                        var plr = player[0];
                                        plr.mute = true;
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                        Console.ResetColor();
                                        Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                    }
                                    else
                                    {
                                        pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use mute.", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
                                        Log.Info(string.Format("{0} failed to used {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                                    }
                                }
                                if (words[1] == "unmute")
                                {
                                    if (pl.Group.HasPermission("mute"))
                                    {
                                        TSPlayer.All.SendMessage(string.Format("{0}: {1}: unmute {2}", pl.Name, BotMain.bcfg.CommandBot, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                                        var player = TShock.Utils.FindPlayer(words[2]);
                                        var plr = player[0];
                                        plr.mute = false;
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                        Console.ResetColor();
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
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                        Console.ResetColor();
                                        Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                    }
                                    else
                                    {
                                        pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use super kick.", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
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
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.WriteLine(string.Format("{0} used {1} to execute: {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                        Console.ResetColor();
                                        Log.Info(string.Format("{0} used {1} to execute {2} on {3}", pl.Name, BotMain.bcfg.CommandBot, words[1], plr.Name));
                                    }
                                    else
                                    {
                                        pl.SendMessage(string.Format("{1}: Sorry {0}, but you don't have permission to use super ban.", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
                                        Log.Info(string.Format("{0} failed to use {1} on {2} because of lack of permissions.", pl.Name, words[1], words[2]));
                                    }
                                }
                                #endregion

                                #region Bot.CommandsList
                                if (words[1] == "commands" || words[1] == "cmds")
                                {
                                    TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: {4}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[1]), pl.Group.R, pl.Group.G, pl.Group.B);
                                    pl.SendMessage(string.Format("{1}: {0}; My executable commands are:", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
                                    pl.SendMessage(string.Format("help, help- register, help- item, kill, hi, good, How are you?, butcher"), b.msgcol);
                                    pl.SendMessage(string.Format("hug, afk, ban, kick, mute, unmute, insult, cmds, google, quote"), b.msgcol);
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine(string.Format("{0} used {1} to execute: {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                    Console.ResetColor();
                                    Log.Info(string.Format("{0} used {1}'s command sender to check {1}'s commands.", pl.Name, BotMain.bcfg.CommandBot));
                                }
                                #endregion

                                #region Bot.Butcher
                                if (words[1] == "butcher")
                                {
                                    TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: butcher", pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar), pl.Group.R, pl.Group.G, pl.Group.B);
                                    if (!pl.Group.HasPermission("butcher"))
                                    {
                                        Random r = new Random();
                                        int p = r.Next(1, 100);
                                        if (p <= BotMain.bcfg.ButcherCmdPct)
                                        {
                                            Commands.HandleCommand(BotMain.CommandExec, "/butcher");
                                            TSPlayer.All.SendMessage(string.Format("{1}: {0}: I butchered all hostile NPCs!", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
                                            Console.ForegroundColor = ConsoleColor.Cyan;
                                            Console.WriteLine(string.Format("{0} used {1} to execute: {2}.", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                            Console.ResetColor();
                                            Log.Info(string.Format("{0} used {1} to execute {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                        }
                                        else
                                        {
                                            TSPlayer.All.SendMessage(string.Format("{1}: Sorry {0}, you rolled a {2}. You need to roll less than {3} to butcher", pl.Name, BotMain.bcfg.CommandBot, p, BotMain.bcfg.ButcherCmdPct), b.msgcol);
                                        }
                                    }
                                    else
                                    {
                                        Commands.HandleCommand(pl, "/butcher");
                                        TSPlayer.All.SendMessage(string.Format("{1}: {0}: I butchered all hostile NPCs!", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.WriteLine(string.Format("{0} used {1} to execute: {2}.", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                        Console.ResetColor();
                                        Log.Info(string.Format("{0} used {1} to execute {2}", pl.Name, BotMain.bcfg.CommandBot, words[1]));
                                    }
                                }
                                #endregion

                                #region Bot.How Are you?
                                if (words[1] == "How" || words[1] == "how" && words[2] == "are".ToLower() && words[3].ToString() == "you?".ToLower())
                                {
                                    TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: How are you?", pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar), pl.Group.R, pl.Group.G, pl.Group.B);
                                    Random r = new Random();
                                    int p = r.Next(1, 10);
                                    if (p == 1)
                                    {
                                        TSPlayer.All.SendMessage(string.Format("{1}: {0}, I am feelings quite well today, thank you!", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
                                    }
                                    else if (p > 3 && p < 6)
                                    {
                                        TSPlayer.All.SendMessage(string.Format("{1}: {0}, I'm feeling a bit down. Might go get drunk later.", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
                                    }
                                    else if (p == 7 || p == 6)
                                    {
                                        TSPlayer.All.SendMessage(string.Format("{1}: {0}, Better than you. 'cos I'm AWESOME!", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
                                    }
                                    else if (p > 8 && p != 10)
                                    {
                                        TSPlayer.All.SendMessage(string.Format("{1}: {0}, I'm seeing unicorns and gnomes. How do you think I am?", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
                                    }
                                    else if (p == 10)
                                    {
                                        TSPlayer.All.SendMessage(string.Format("{1}: {0}, I just won the lottery. Stop being so poor in front of me.", pl.Name, BotMain.bcfg.CommandBot), b.msgcol);
                                    }
                                }
                                #endregion

                                #region Bot.PlayerInsult
                                if (words[1] == "insult")
                                {
                                    var ply = TShock.Utils.FindPlayer(words[2]);
                                    var plr = ply[0].Name;
                                    TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: insult {4}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
                                    Random r = new Random();
                                    int p = r.Next(1, 10);

                                    if (p == 1)
                                    { TSPlayer.All.SendMessage(string.Format("{1}: Yo, {0}, I bet your mother is a nice lady!", plr, BotMain.bcfg.CommandBot), b.msgcol); }
                                    if (p == 2)
                                    { TSPlayer.All.SendMessage(string.Format("{1}: I bet {0}'s mother was a hamster, and their father smelled of elderberries.", plr, BotMain.bcfg.CommandBot), b.msgcol); }
                                    if (p == 3)
                                    { TSPlayer.All.SendMessage(string.Format("{1}: I bet {0} uses the term swag liberally.", plr, BotMain.bcfg.CommandBot), b.msgcol); }
                                    if (p == 4)
                                    { TSPlayer.All.SendMessage(string.Format("{1}: {0} is such a... twig!", plr), b.msgcol); }
                                    if (p == 5)
                                    { TSPlayer.All.SendMessage(string.Format("{0}: ...But I'm a nice bot!... Sometimes", BotMain.bcfg.CommandBot), b.msgcol); }
                                    if (p == 6)
                                    { TSPlayer.All.SendMessage(string.Format("{1}: {0} is such a! a... erm... thing!", plr, BotMain.bcfg.CommandBot), b.msgcol); }
                                    if (p == 7)
                                    { TSPlayer.All.SendMessage(string.Format("{1}: {0}, I'm so awesome you should feel insulted already.", plr, BotMain.bcfg.CommandBot), b.msgcol); }
                                    if (p == 9)
                                    { TSPlayer.All.SendMessage(string.Format("{1}: {0}... You remind me of someone named {2}.", plr, BotMain.bcfg.CommandBot, BotMain.bcfg.GenericInsultName), b.msgcol); }
                                    if (p == 10)
                                    { TSPlayer.All.SendMessage(string.Format("{1}: Don't tell me what to do, {0}!", pl.Name), b.msgcol); }
                                }
                                #endregion

                                #region Bot.Website
                                if (words[1] == "g" || words[1] == "google")
                                {
                                    int count = 0;
                                    TSPlayer.All.SendMessage(string.Format("{0}{1}{2}: {3}: {4} {5}", pl.Group.Prefix, pl.Name, pl.Group.Suffix, BotMain.bcfg.CommandChar, words[1], words[2]), pl.Group.R, pl.Group.G, pl.Group.B);
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
                                                TSPlayer.All.SendMessage(string.Format("{0}: Website for {1}: {2}", BotMain.bcfg.CommandBot, words[2], website), b.msgcol);
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
                                                TSPlayer.All.SendMessage(string.Format("{0}: Website for {1}: {2}", BotMain.bcfg.CommandBot, words[2], website), b.msgcol);
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
                                                TSPlayer.All.SendMessage(string.Format("{0}: Website for {1}: {2}", BotMain.bcfg.CommandBot, words[2], website), b.msgcol);
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
                                                TSPlayer.All.SendMessage(string.Format("{0}: Website for {1}: {2}", BotMain.bcfg.CommandBot, words[2], website), b.msgcol);
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        count++;
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
    }
}
