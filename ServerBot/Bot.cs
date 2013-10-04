using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;

namespace ServerBot
{
    public class Bot
    {
        public int Index;
        public string Type = "";
        public string Message = "";
        public int Msgtime = 0;
        public string Name;
        public byte r = 255;
        public byte g = 255;
        public byte b = 255;
        public Trivia Trivia;

        public Bot(int index, string name)
        {
            Index = index;
            Name = name;
            Trivia = new Trivia(this);
        }
        
        /// <summary>
        /// Announce to all players as if the bot is saying something.
        /// </summary>
        /// <param name="msg">What the bot should say.</param>
        public void Say(string msg)
        {
        	TSPlayer.All.SendMessage(string.Format("[Bot] {0}: {1}", Name, msg), r, g, b);
        }
        
        /// <summary>
        /// Announce to all players as if the bot is saying something, supports string formatting.
        /// </summary>
        /// <param name="msg">What the bot should say. Allows for formatting marks.</param>
        /// <param name="objs">Objects to be formatted into the string.</param>
        public void Say(string msg, object[] objs)
        {
        	TSPlayer.All.SendMessage(string.Format("[Bot] {0}: {1}", Name, string.Format(msg, objs)), r, g, b);
        }
        
        /// <summary>
        /// Announce to one player as if the bot is saying something.
        /// </summary>
        /// <param name="player">Player to send message to.</param>
        /// <param name="msg">What the bot should say.</param>
        public void Private(TSPlayer player, string msg)
        {
        	player.SendMessage(string.Format("[Bot] {0}: {1}", Name, msg), r, g, b);
        }
        
        /// <summary>
        /// Announce to one player as if the bot is saying something, supports string formatting.
        /// </summary>
        /// <param name="player">Player to send message to.</param>
        /// <param name="msg">What the bot should say. Allows for formatting marks.</param>
        /// <param name="objs">Objects to be formatted into the string.</param>
        public void Private(TSPlayer player, string msg, object[] objs)
        {
        	player.SendMessage(string.Format("[Bot] {0}: {1}", Name, string.Format(msg, objs)), r, g, b);
        }
    }
}