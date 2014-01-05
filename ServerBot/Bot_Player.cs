using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;

namespace ServerBot
{
    public class bPlayer
    {
        public int Index;
        public int swear_Count = 0;
        public int kick_Count = 0;
        public string ctype = ""; //?
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public string PlayerName { get { return Main.player[Index].name; } }

        public bPlayer(int index)
        {
            Index = index;
        }
    }
}
