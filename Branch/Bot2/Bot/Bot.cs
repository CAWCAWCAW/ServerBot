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
        public Color msgcol;  //message colour
        public string Name;
        public byte r;
        public byte g;
        public byte b;

        public Bot(int index, string name)
        {
            Index = index;
            Name = name;
        }
    }
}
