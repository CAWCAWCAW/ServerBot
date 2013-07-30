using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;

namespace Bot
{
    public class Pl
    {
        public int Index;
        public int scount = 0;
        public int kcount = 0;
        public string ctype = "";
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public string PlayerName { get { return Main.player[Index].name; } }


        public Pl(int index)
        {
            Index = index;
        }
    }
}
