using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ServerBot
{
    public class bConfig
    {
        public bool enableAutoJoin = true;
        public string bot_Name = "Botname";
        public byte[] bot_MessageRGB = new byte[3] { 255, 255, 255 };
        public string command_Char = "^";
        public string bot_Join_Message = "";
        public int kicks_Before_Ban = 3;
        public bool Snark = true;
        public int command_Success_Percent = 10;
        public string generic_Insult_Names = "Tony Abbott";
        public bool swear_Block = true;
        public string[] swear_Block_Action = new string[3];
        public int swear_Block_Chances = 5;
        public bool yolo_Swag_Block = true;
        public string[] yolo_Swag_Action = new string[3];
        public string yolo_Swag_KickBan_Reason = "Failnoob";


        public static bConfig Read(string path)
        {
            if (!File.Exists(path))
                return new bConfig();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(fs);
            }
        }

        public static bConfig Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                var cf = JsonConvert.DeserializeObject<bConfig>(sr.ReadToEnd());
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

        public static Action<bConfig> ConfigRead;
    }
}
