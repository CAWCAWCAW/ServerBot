using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Hooks;
using TShockAPI;

namespace Bot
{
    public class Bot
    {
        public int Index;
        public string type = "";
        public string message = "";
        public int msgtime = 0;
        public string Name;
        public byte r;
        public byte g;
        public byte b;
        public Trivia trivia;

        public Bot(int index, string name)
        {
            Index = index;
            Name = name;
            trivia = new Trivia(this);
        }
        
        public void Say(string msg)
        {
        	TSPlayer.All.SendMessage(string.Format("Bot {0}: {1}", Name, msg), r, g, b);
        }
        
        public void Say(string msg, object[] objs)
        {
        	TSPlayer.All.SendMessage(string.Format(msg, objs), r, g, b);
        }
    }
}