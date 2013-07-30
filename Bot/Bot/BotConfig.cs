using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Bot
{
    public class BotConfig
    {
        public bool EnableAutoJoin = true;
        public string Comment1 = "The name of your default bot that joins when the first player joins, and its rgb colours";
        public string OnjoinBot = "Botname";
        public byte OnjoinBotColourR = 255;
        public byte OnjoinBotColourG = 255;
        public byte OnjoinBotColourB = 255;
        public string Comment2 = "The character or word used to execute bot commands. EG: ^ kill white";
        public string CommandChar = "^";
        public string Comment3 = "The bot that will execute the commands in the command char. Can be the same as onjoin bot's name.";
        public string CommandBot = "Botname";
        public string Comment4 = "Whether or not to let your Bot say things to players when they join.";
        public bool BotJoinMessage = true;
        public string Comment5 = "Number of times to autokick on join before ban on join";
        public int KickCountB4Ban = 3;
        public string Comment6 = "Enable snarky response for using \"/\" without any other text";
        public bool EnableSnark = true;
        public string Comment7 = "The percentage chance of players without /butcher being able to use the bot to butcher.";
        public int ButcherCmdPct = 10;
        public string Comment8 = "Generic insult name; The name of someone you want to compare a player to, as an insult.";
        public string GenericInsultName = "Tony Abbott";
        public string Comment9 = "Swear blocker + action. Action can be kick, mute";
        public bool EnableSwearBlocker = true;
        public string SwearBlockAction = "mute";
        public string Comment10 = "Number of chances a player gets to stop swearing before being acted upon.";
        public int SwearBlockChances = 5;
        public string Comment11 = "YOLO/Swag blocker. Optional mute/kick/kill";
        public bool EnableYoloSwagBlock = true;
        public string FailNoobAction = "kick";
        public string FailNoobKickReason = "Failnoob";


        public static BotConfig Read(string path)
        {
            if (!File.Exists(path))
                return new BotConfig();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(fs);
            }
        }

        public static BotConfig Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                var cf = JsonConvert.DeserializeObject<BotConfig>(sr.ReadToEnd());
                if (ConfigRead != null)
                    ConfigRead(cf);
                return cf;
            }
        }

        public void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs);
            }
        }

        public void Write(Stream stream)
        {
            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }

        public static Action<BotConfig> ConfigRead;
    }
}
