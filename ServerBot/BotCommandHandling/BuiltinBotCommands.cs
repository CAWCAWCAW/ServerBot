using System;
using TShockAPI;
using TShockAPI.DB;
using Terraria;
using System.Collections.Generic;

namespace ServerBot
{
	/// <summary>
	/// Bot commands that come with the bot
	/// </summary>
	public class BuiltinBotCommands
	{
		#region BotHelp
		public static void BotHelp(BotCommandArgs args)
		{
            if (args.Parameters.Count == 1)
            {
                if (args.Parameters[0] == "register")
                {
                    args.Bot.Say("To register, use /register <password>");
                    args.Bot.Say("<password> can be anything, and you define it personally.");
                    args.Bot.Say("Always remember to keep your password secure!");
                    bTools.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: help register", args.Player.Name, bTools.Bot.Name);
                }
                else if (args.Parameters[0] == "item")
                {
                    args.Bot.Say("To spawn items, use the command /item");
                    args.Bot.Say("Items that are made of multiple words MUST be wrapped in quotes");
                    args.Bot.Say("Eg: /item \"hallowed repeater\"");
                    bTools.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: help item", args.Player.Name, bTools.Bot.Name);
                }
            }
            else
            {
                args.Bot.Say("Whoops! Try using ^ help item or ^ help register");
            }
		}
		#endregion
		
		#region BotKill
		public static void BotKill(BotCommandArgs args)
		{
			if (args.Parameters.Count > 0)
			{
				var targets = TShock.Utils.FindPlayer(args.Parameters[0]);
                if (targets.Count < 1)
                {
                    TShock.Utils.SendMultipleMatchError(args.Player, targets);
                    return;
                }
				TSPlayer target = targets[0];
				
				if (args.Player.Group.HasPermission("kill"))
				{
					target.DamagePlayer(target.TPlayer.statLifeMax * target.TPlayer.statDefense);
                    target.TPlayer.dead = true;
                    target.Dead = true;
					args.Bot.Say("{1} just had me kill {2}!", args.Player.Name, target.Name);
					bTools.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: kill on {2}", args.Player.Name, bTools.Bot.Name, target.Name);
				}
				else
				{
					args.Bot.Private(args.Player, "Sorry, but you don't have the permission to use kill.");
					bTools.LogToConsole(ConsoleColor.Cyan, "{0} failed to use kill on {2} because of lack of permissions.", args.Player.Name, target.Name);
				}
			}
		}
		#endregion
		
		#region BotGreet
		public static void BotGreet(BotCommandArgs args)
		{
            if (args.Parameters.Count > 0)
            {
                args.Bot.Say("Hello {1}, how are you?",  args.Player.Name );
                bTools.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: hi", args.Player.Name, args.Bot.Name);
            }
		}
		#endregion
		
		#region BotResponseGood
		public static void BotResponseGood(BotCommandArgs args)
		{
			//insert random good responses here
			
			bTools.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: good", args.Player.Name, args.Bot.Name);			
		}
		#endregion
		
		#region BotResponseBad
        public static void BotResponseBad(BotCommandArgs args)
        {
            //insert random bad responses here

            args.Bot.Say("There there, {0}.", args.Player.Name);
            Item heart = TShock.Utils.GetItemById(58);
            Item star = TShock.Utils.GetItemById(184);
            for (int i = 0; i < 20; i++)
            {
                args.Player.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
            }
            for (int i = 0; i < 10; i++)
            {
                args.Player.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
            }
            args.Bot.Me("hugs {0}", args.Player.Name, bTools.Bot.Name);
            bTools.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: bad", args.Player.Name, args.Bot.Name);
        }
		#endregion
		
		#region BotHug
		public static void BotHug(BotCommandArgs args)
		{
			Item heart = TShock.Utils.GetItemById(58);
            Item star = TShock.Utils.GetItemById(184);
	        for (int i = 0; i < 20; i++)
	        {
	            args.Player.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
	        }
	        for (int i = 0; i < 10; i++)
	        {
	            args.Player.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
	        }
	        args.Bot.Say("*{1} hugs {0}", args.Player.Name, args.Bot.Name);	
		}
		#endregion
		
		#region BotBan
		public static void BotBan(BotCommandArgs args)
		{
            var targets = TShock.Utils.FindPlayer(args.Parameters[0]);
            if (targets.Count < 1)
            {
                TShock.Utils.SendMultipleMatchError(args.Player, targets);
                return;
            }
            TSPlayer target = targets[0];
			
			if (args.Player.Group.HasPermission("ban"))
			{
				TShock.Utils.Ban(target, bTools.Bot.Name + " ban", false, null);
				bTools.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: ban on {2}", args.Player.Name, args.Bot.Name, target.Name);
			}
			else
			{
				args.Bot.Private(args.Player, "Sorry, but you don't have permission to use ban.");
				bTools.LogToConsole(ConsoleColor.Cyan, "{0} failed to use \"ban\" on {1} because of a lack of permission.", args.Player.Name, target.Name);
			}
		}
		#endregion
		
		#region BotKick
		public static void BotKick(BotCommandArgs args)
		{
            var targets = TShock.Utils.FindPlayer(args.Parameters[0]);
            if (targets.Count < 1)
            {
                TShock.Utils.SendMultipleMatchError(args.Player, targets);
                return;
            }
            TSPlayer plr = targets[0];

			if (args.Player.Group.HasPermission("kick"))
			{
                TShock.Utils.Kick(plr, args.Bot.Name + " forcekick", false, false, null, false);
                bTools.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: kick on {2}", args.Player.Name, args.Bot.Name, plr.Name);
			}
			else
            {
				args.Bot.Private(args.Player, "Sorry, but you don't have permission to use kick.");
				bTools.LogToConsole(ConsoleColor.Cyan, "{0} failed to use kick on {1} because of a lack of permission.", args.Player.Name, plr.Name);
            }
		}
		#endregion
		
		#region BotMute
		public static void BotMute(BotCommandArgs args)
		{
            var targets = TShock.Utils.FindPlayer(args.Parameters[0]);
            if (targets.Count < 1)
            {
                TShock.Utils.SendMultipleMatchError(args.Player, targets);
                return;
            }
            TSPlayer plr = targets[0];

			if (args.Player.Group.HasPermission("mute"))
			{
				plr.mute = true;
				bTools.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: mute on {2}", args.Player.Name, args.Bot.Name, plr.Name);
			}
			else
		    {
				bTools.LogToConsole(ConsoleColor.Cyan, "{0} failed to use mute on {1} because of a lack of permission.", 
                    args.Player.Name, plr.Name);
               	args.Bot.Private(args.Player, "Sorry, but you don't have permission to use mute.");
		    }
		}
		#endregion
		
		#region BotUnmute
		public static void BotUnmute(BotCommandArgs args)
		{
            var targets = TShock.Utils.FindPlayer(args.Parameters[0]);
            if (targets.Count < 1)
            {
                TShock.Utils.SendMultipleMatchError(args.Player, targets);
                return;
            }
            TSPlayer plr = targets[0];
			
			if (args.Player.Group.HasPermission("mute"))
			{
				plr.mute = false;	
				bTools.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: unmute on {2}", args.Player.Name, args.Bot.Name, plr.Name);
			}
			else
			{
				bTools.LogToConsole(ConsoleColor.Cyan, "{0} failed to use unmute on {1} because of a lack of permission.",
                    args.Player.Name, plr.Name);
               	args.Bot.Private(args.Player, "Sorry, but you don't have permission to use unmute.");
			}
		}
		#endregion
		
		#region BotButcher
		public static void BotButcher(BotCommandArgs args)
		{
			if (args.Player.Group.HasPermission("butcher"))
			{
				Commands.HandleCommand(args.Player, "/butcher");
				args.Bot.Say("I butchered all hostile NPCs!");
				bTools.LogToConsole(ConsoleColor.Cyan,"{0} used {1} to execute: butcher", args.Player.Name, args.Bot.Name);
			}
			else
			{
				Random r = new Random();
                int p = r.Next(1, 100);
                if (p <= bTools.bot_Config.command_Success_Percent)
                {
                    Commands.HandleCommand(TShockAPI.TSPlayer.Server, "/butcher");
                   	args.Bot.Say("I butchered all hostile NPCs!");
					bTools.LogToConsole(ConsoleColor.Cyan,"{0} used {1} to execute: butcher", args.Player.Name, args.Bot.Name);
                }
                else
                {
                	args.Bot.Say("Sorry {0}, you rolled a {1}. You need to roll less than {2} to butcher", 
                        args.Player.Name, p, bTools.bot_Config.command_Success_Percent);
                }
			}
		}
		#endregion
		
		#region BotHowAreYou
		public static void BotHowAreYou(BotCommandArgs args)
		{
            //Insert random howareyou phrase here
            bTools.LogToConsole(ConsoleColor.Cyan,"{0} used {1} to execute: howareyou", args.Player.Name, args.Bot.Name);
		}
		#endregion
		
		#region BotInsult
		public static void BotInsult(BotCommandArgs args)
		{
            var targets = TShock.Utils.FindPlayer(args.Parameters[0]);
            if (targets.Count < 1)
            {
                TShock.Utils.SendMultipleMatchError(args.Player, targets); //Needs to point to a list<string> not list<TSPlayer>
                return;
            }

            string plr = targets[0].Name;

            //Randomly select insult here
            
            bTools.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: insult on {2}", args.Player.Name, args.Bot.Name, plr);
        }		
		#endregion
		
		#region BotTriviaStart
		public static void BotTriviaStart(BotCommandArgs args)
		{
			if (args.Parameters.Count > 0)
			{
		    	int numq;
		    	if (!int.TryParse(args.Parameters[0], out numq))
		    	{
		    		args.Bot.Say("You didn't provide a valid number of questions for the game.");
		    		return;
		    	}
		    	args.Bot.Trivia.StartGame(numq);
		    	return;
			}
			else
			{
				args.Bot.Say("Proper format of \"^ starttrivia\": ^ starttrivia <number of questions to ask>");
				return;
			}
		}
		#endregion
		
		#region BotTriviaAnswer
		public static void BotTriviaAnswer(BotCommandArgs args)
		{
			if (args.Bot.Trivia.OngoingGame)
			{
				args.Bot.Trivia.CheckAnswer(string.Join(" ", args.Parameters), args.Player.Name);
			}
		}
		#endregion

        //Make all badwords .ToLower(), meaning that people can't avoid detection by changing case
        #region BotBadwords
        public static void BotBadWords(BotCommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                if (args.Parameters[0] == "add")
                {
                    using (var reader = bTools.db.QueryReader("SELECT * FROM BotSwear WHERE SwearBlock = @0", args.Parameters[1]))
                    {
                        if (!reader.Read())
                        {
                            bTools.db.Query("INSERT INTO BotSwear (SwearBlock) VALUES (@0)", args.Parameters[1]);
                            args.Player.SendMessage(string.Format("Added {0} into the banned word list.", args.Parameters[1]), Color.CadetBlue);
                            bTools.Swearwords.Add(args.Parameters[1].ToLower());
                        }
                        else
                        {
                            args.Player.SendWarningMessage(string.Format("{0} already exists in the swear list.", args.Parameters[1]));
                        }
                    }
                }
                else if (args.Parameters[0] == "del")
                {
                    using (var reader = bTools.db.QueryReader("SELECT * FROM BotSwear WHERE SwearBlock = @0", args.Parameters[1]))
                    {
                        if (reader.Read())
                        {
                            bTools.db.Query("DELETE FROM BotSwear WHERE SwearBlock = @0", args.Parameters[1]);
                            args.Player.SendMessage(string.Format("Deleted {0} from the banned word list.", args.Parameters[1]), Color.CadetBlue);
                            bTools.Swearwords.Remove(args.Parameters[1].ToLower());
                        }
                        else
                        {
                            args.Player.SendWarningMessage(string.Format("{0} does not exist in the swear list.", args.Parameters[1]));
                        }
                    }
                }
            }
            else
            {
                args.Bot.Say("You didn't have a valid number of parameters; Use ^ badwords [add/del] \"word\"");
                return;
            }
        }
        #endregion    //Make all badwords .ToLower(), meaning that people can't avoid detection by changing case

        #region BotReloadCfg
        public static void BotReloadCfg(BotCommandArgs args)
        {
            bTools.SetUpConfig();

            bTools.Bot.Trivia.LoadConfig(bTools.trivia_Save_Path);

            args.Player.SendWarningMessage("Reloaded Bot config");
        }
        #endregion

        //Make all players .ToLower(), meaning that people can't avoid detection by changing case
        #region BotPlayerManagement
        public static void KickPlayers(BotCommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendWarningMessage("You didn't have a valid number of parameters; Use ^ player [add/del] \"playername\"");
            }
            else
            {
                if (args.Parameters[0] == "add")
                {
                    using (var reader = bTools.db.QueryReader("SELECT * FROM BotKick WHERE KickNames = @0", args.Parameters[1]))
                    {
                        if (!reader.Read())
                        {
                            bTools.db.Query("INSERT INTO BotKick (KickNames) VALUES (@0)", args.Parameters[1]);
                            args.Player.SendMessage(string.Format("Added {0} to the playermanager list.", args.Parameters[1]), Color.CadetBlue);
                        }
                        else
                        {
                            args.Player.SendWarningMessage(string.Format("{0} already exists in the playermanager list!", args.Parameters[1]));
                        }
                    }
                }
                else if (args.Parameters[0] == "del")
                {
                    using (var reader = bTools.db.QueryReader("SELECT * FROM BotKick WHERE KickNames = @0", args.Parameters[1]))
                    {
                        if (reader.Read())
                        {
                            bTools.db.Query("DELETE FROM BotKick WHERE KickNames = @0", args.Parameters[1]);
                            args.Player.SendMessage(string.Format("Deleted {0} from the playermanager list!", args.Parameters[1]), Color.CadetBlue);
                        }
                        else
                        {
                            args.Player.SendWarningMessage(string.Format("{0} does not exist on the playermanager list!", args.Parameters[1]));
                        }
                    }
                }
            }
        }
        #endregion
    }	
}
