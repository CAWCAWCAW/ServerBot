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

    /* TODO
     * Add another config file for storing output messages, so I don't have to create them all
     * Change a bunch of stuff relating to CheckChat(), SwearBlocker and other message blockers
     * Fix the webrequest function
     * Make sure all commands fire correctly
     * Fix stuff I commented out for being broken
     * Finalize the one-bot system, rather than many bots. Extra bots can be added as hard-coded bots, rather than in a list
     * More that I have forgotten at the moment.
     */

    [ApiVersion(1, 14)]
    public class ServerBots : TerrariaPlugin
    {
        #region ServerBot
        public override string Name { get { return "ServerBot"; } }
        public override Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }
        public override string Author { get { return "WhiteX, Ijwu"; } }
        public override string Description { get { return "Terraria server bot(s)."; } }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
            ServerApi.Hooks.ServerChat.Register(this, OnChat);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
            ServerApi.Hooks.GamePostInitialize.Register(this, bTools.autoJoin);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, bTools.autoJoin);
            }
            base.Dispose(disposing);
        }

        public ServerBots(Main game)
            : base(game)
        {
            Order = 1;
        }
        #endregion

        #region OnInitialize
        public void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command(new List<string>() { "bot.bot", "bot.*" }, bCommands.BotMethod, "/bot"));
            Commands.ChatCommands.Add(new Command(new List<string>() { "bot.reload", "bot.*" }, bCommands.ReloadCfg, "/botrld"));
            Commands.ChatCommands.Add(new Command(new List<string>() { "bot.badwords", "bot.*" }, bCommands.BadWords, "/badwords", "/badword", "/bd"));
            Commands.ChatCommands.Add(new Command(new List<string>() { "bot.kickplayer", "bot.*" }, bCommands.KickPlayers, "/kickplayers", "/kickplayer", "/kp"));

            bTools.SetUpConfig();
            bTools.SetUpDB();
            bTools.Handler = new BotCommandHandler();
            bTools.RegisterBuiltinCommands();

            //bTools.servername = TShock.Config.ServerName;

            bTools.GetSwears();
        }
        #endregion

        #region Onjoin
        public void OnJoin(JoinEventArgs args)
        {

        }
        #endregion

        #region OnGreet
        public void OnGreet(GreetPlayerEventArgs args)
        {
            bTools.players.Add(new bPlayer(args.Who));

            #region Bot.AutoJoining
            /*
            if (bTools.bot_Config.EnableAutoJoin)
            {
                if (plycount == 0)
                {
                    lock (bots)
                    {
                        Random r = new Random();
                        int z = r.Next(2, 256);
                        {
                            bots.Add(new bBot(z, bot_Config.OnjoinBot));
                            foreach (bBot b in bots)
                            {
                                if (b.Name == bot_Config.OnjoinBot)
                                {
                                    b.r = bot_Config.OnjoinBotColourR;
                                    b.g = bot_Config.OnjoinBotColourG;
                                    b.b = bot_Config.OnjoinBotColourB;
                                }
                            }
                            plycount++;
                            return;
                        }
                    }
                }
            }
            */
            #endregion

            #region Bot.AutoKick, Bot.AutoBan
            /*
            var ply = players[args.Who];
            ply.kick_Count = 0;
            using (var reader = db.QueryReader("SELECT * FROM BotKick WHERE KickNames = @0", ply.PlayerName))
            {
                if (reader.Read() && ply.kick_Count < bot_Config.KickCountB4Ban)
                {
                    db.Query("UPDATE BotKick SET KickIP = @0 WHERE KickNames = @1", ply.TSPlayer.IP, ply.PlayerName);
                    TShock.Utils.ForceKick(ply.TSPlayer, bot_Config.OnjoinBot + " kick", false, false);
                    ply.kick_Count++;
                }

                else if (reader.Read() && ply.kick_Count > bot_Config.KickCountB4Ban)
                {
                    db.Query("UPDATE BotKick SET KickIP = @0 WHERE KickNames = @1", ply.TSPlayer.IP, ply.PlayerName);
                    TShock.Utils.Ban(ply.TSPlayer, bot_Config.OnjoinBot + " ban", true, null);
                    ply.kick_Count++;
                }

                if (ply.TSPlayer.IP == IP && ply.kick_Count < bot_Config.KickCountB4Ban)
                {
                    db.Query("UPDATE BotKick SET KickIP = @0 WHERE KickNames = @1", ply.TSPlayer.IP, ply.PlayerName);
                    TShock.Utils.ForceKick(ply.TSPlayer, bot_Config.OnjoinBot.ToString() + " kick", false, false);
                    ply.kick_Count++;
                }

                else if (ply.TSPlayer.IP == IP && ply.kick_Count > bot_Config.KickCountB4Ban)
                {
                    db.Query("UPDATE BotKick SET KickIP = @0 WHERE KickNames = @1", ply.TSPlayer.IP, ply.PlayerName);
                    TShock.Utils.Ban(ply.TSPlayer, bot_Config.OnjoinBot.ToString() + " ban", true, null);
                }
            }
            */
            #endregion

            if (bTools.bot_Config.bot_Join_Message != "")
            {
                var player = TShock.Players[args.Who];

                Random r = new Random();
                var rand = r.Next(0, bTools.bot_Config.bot_Join_Message.Split('|').Length - 1);

                bTools.GreetMsg(player, bTools.bot_Config.bot_Join_Message.Split('|')[rand]);
            }
        }
        #endregion

        #region OnLeave
        public void OnLeave(LeaveEventArgs args)
        {
            bTools.players.RemoveAll(plr => plr.Index == args.Who);
        }
        #endregion

        #region OnChat
        public void OnChat(ServerChatEventArgs args)
        {
            TSPlayer player = TShock.Players[args.Who];

            if (player == null)
            {
                return;
            }

            if (args.Text == "/")
            {
                if (bTools.bot_Config.Snark)
                {
                    player.SendWarningMessage("Correct. That is a command starter. Try writing an actual command now.");
                }
                args.Handled = true;
                return;
            }

            bTools.CheckChat(args);
        }
        #endregion
    }
}