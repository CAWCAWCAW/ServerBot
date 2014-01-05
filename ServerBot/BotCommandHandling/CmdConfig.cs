using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ServerBot
{
    public class cBotCommand
    {
        /// <summary>
        /// Name of the chat-command to send to the bot
        /// </summary>
        public string CommandName;

        /// <summary>
        /// The message the bot will send to the player, or to the server
        /// </summary>
        public string ReturnMessage;

        /// <summary>
        /// Which commands from TShock.ChatCommands should be used when the bot command is used
        /// </summary>
        public List<string> CommandActions;

        /// <summary>
        /// Whether the bot should broadcast to all players, or just the player who executed the command
        /// </summary>
        public bool noisyCommand;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cn">CommandName</param>
        /// <param name="rm">ReturnMessage</param>
        /// <param name="ca">CommandActions</param>
        /// <param name="noisy">NoisyCommand</param>
        public cBotCommand(string cn, string rm, List<string> ca, bool noisy)
        {
            CommandName = cn;
            ReturnMessage = rm;
            CommandActions = ca;
            noisyCommand = noisy;
        }
    }

    public class BotCommandSet
    {
        public List<cBotCommand> botcommands;
        public BotCommandSet(List<cBotCommand> botcommands)
        {
            this.botcommands = botcommands;
        }
    }


    public class CmdConfig
    {

        public List<BotCommandSet> BotActions;

        public static CmdConfig Read(string path)
        {
            if (!File.Exists(path))
                return new CmdConfig();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(fs);
            }
        }

        public static CmdConfig Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                var cf = JsonConvert.DeserializeObject<CmdConfig>(sr.ReadToEnd());
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

            BotActions = new List<BotCommandSet>();
            List<cBotCommand> commandSet = new List<cBotCommand>();
            commandSet.Add(new cBotCommand("help", "", new List<string>() { "/help" }, false));
            BotActions.Add(new BotCommandSet(commandSet));



            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }

        public static Action<CmdConfig> ConfigRead;
    }
}
