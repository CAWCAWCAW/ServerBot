using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;

namespace ServerBot
{
    public class bBot
    {
        public string Type = "";
        public string Message = "";
        public int Msgtime = 0;
        public string Name;
        public byte[] rgb = new byte[3];
        public Color color { get { return new Color(rgb[0], rgb[1], rgb[2]); } }
        public Trivia Trivia;

        public bBot(string name)
        {
            Name = name;
            Trivia = new Trivia(this);
        }

        /// <summary>
        /// Announce to all players as if the bot is performing the /me command
        /// </summary>
        /// <param name="msg">What the bot should say.</param>
        public void Me(string msg)
        {
            TSPlayer.All.SendMessage(string.Format("*{0}: {1}", Name, msg), 205, 133, 63);
        }

        /// <summary>
        /// Announce to all players as if the bot is performing the /me command. Supports string formatting
        /// </summary>
        /// <param name="msg">What the bot should say. Allows formatting marks</param>
        /// <param name="objs">Objects to be formatted into the string</param>
        public void Me(string msg, params object[] objs)
        {
            TSPlayer.All.SendMessage(string.Format("*{0}: {1}", Name, string.Format(msg, objs)), 205, 133, 63);
        }

        /// <summary>
        /// Announce to all players as if the bot is saying something.
        /// </summary>
        /// <param name="msg">What the bot should say.</param>
        public void Say(string msg)
        {
        	TSPlayer.All.SendMessage(string.Format("[Bot] {0}: {1}", Name, msg), color);
        }
        
        /// <summary>
        /// Announce to all players as if the bot is saying something, supports string formatting.
        /// </summary>
        /// <param name="msg">What the bot should say. Allows for formatting marks.</param>
        /// <param name="objs">Objects to be formatted into the string.</param>
        public void Say(string msg, params object[] args)
        {
        	TSPlayer.All.SendMessage(string.Format("[Bot] {0}: {1}", Name, string.Format(msg, args)), color);
        }
        
        /// <summary>
        /// Announce to one player as if the bot is saying something.
        /// </summary>
        /// <param name="player">Player to send message to.</param>
        /// <param name="msg">What the bot should say.</param>
        public void Private(TSPlayer player, string msg)
        {
            player.SendMessage(string.Format("<From {0}> {1}", Name, msg), Color.MediumPurple);
        }
        
        /// <summary>
        /// Announce to one player as if the bot is saying something, supports string formatting.
        /// Now uses the TShock /whisper format and colouration.
        /// </summary>
        /// <param name="player">Player to send message to.</param>
        /// <param name="msg">What the bot should say. Allows for formatting marks.</param>
        /// <param name="objs">Objects to be formatted into the string.</param>
        public void Private(TSPlayer player, string msg, params object[] args)
        {
        	player.SendMessage(string.Format("<From {0}> {1}", Name, string.Format(msg, args)), Color.MediumPurple);
        }

        public void doSetUp()
        {
            rgb = bTools.bot_Config.bot_MessageRGB;

        }
    }
}