using System;
using TShockAPI;
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
			if (args.Parameters.Count > 1)
			{
				if (args.Parameters[0] == "register")
				{
	            	args.Bot.Say("To register, use /register <password>");
	            	args.Bot.Say("<password> can be anything, and you define it personally.");
	            	args.Bot.Say("Always remember to keep your password secure!");
	            	Utils.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: help register", new object[]{args.Player.Name, BotMain.CommandBot.Name});
				}
				else if (args.Parameters[0] == "item")
				{
		        	args.Bot.Say("To spawn any item, use the command /item");
		        	args.Bot.Say("Items that are made of multiple words MUST be wrapped in quotes");
		        	args.Bot.Say("Eg: /item \"hallowed repeater\"");
		        	Utils.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: help item", new object[]{args.Player.Name, BotMain.CommandBot.Name});
				}
			}
		}
		#endregion
		
		#region BotKill
		public static void BotKill(BotCommandArgs args)
		{
			if (args.Parameters.Count > 0)
			{
				List<TSPlayer> targets = TShock.Utils.FindPlayer(args.Parameters[0]);
				if (targets.Count < 1)
					return;
				TSPlayer target = targets[0];
				
				if (args.Player.Group.HasPermission("kill"))
				{
					target.DamagePlayer(99999);
					args.Bot.Say("{1} just had me kill {2}!", new object[]{args.Player.Name, target.Name});
					Utils.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: kill on {2}", new object[]{args.Player.Name, BotMain.CommandBot.Name, target.Name});
				}
				else
				{
					args.Bot.Private(args.Player, "Sorry, but you don't have the permission to use kill.");
					Utils.LogToConsole(ConsoleColor.Cyan, "{0} failed to use kill on {2} because of lack of permissions.", new object[]{args.Player.Name, target.Name});
				}
			}
		}
		#endregion
		
		#region BotGreet
		public static void BotGreet(BotCommandArgs args)
		{
			args.Bot.Say("Hello {1}, how are you?", new object[]{args.Player.Name});
			Utils.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: hi", new object[]{args.Player.Name, args.Bot.Name});
		}
		#endregion
		
		#region BotResponseGood
		public static void BotResponseGood(BotCommandArgs args)
		{
			Random response = new Random();
			switch (response.Next(0, 5))
			{
				case 0:
					args.Bot.Say("That's great. I'll tell you how I am, if you ask me. ;)");
					break;
				case 1:
					args.Bot.Say("Hah, nice. What's the bet I'm better though? >:D");
					break;
				case 2:
					args.Bot.Say("Nice to hear");
					break;
				case 3:
					args.Bot.Say("Nice to hear");
					break;
				case 4:
					args.Bot.Say("Good, you say? Did you bring me a present then?");
					break;
				case 5:
					args.Bot.Say("I'm always happiest with good friends... And lots of alcohol. Want to join me?");
					break;
			}
			Utils.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: good", new object[]{args.Player.Name, args.Bot.Name});			
		}
		#endregion
		
		#region BotResponseBad
		public static void BotResponseBad(BotCommandArgs args)
		{
			
			Random response = new Random();
			switch (response.Next(0, 4))
			{
				case 0:
					args.Bot.Say("Well {0}... Always remember, the new day is a great big fish.", new object[]{args.Player.Name});
					break;
				case 1:
					args.Bot.Say("Poor {0}... It could be worse though. You could have crabs.", new object[]{args.Player.Name});
					break;
				case 2:
					args.Bot.Say("There there, {0}.", new object[]{args.Player.Name});
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
	                args.Bot.Say("*{1} hugs {0}", new object[]{args.Player.Name, BotMain.CommandBot.Name});
					break;
				case 3:
					args.Bot.Say("{0}, What you need is a good sleep... And a monkey", new object[]{args.Player.Name});
					break;
				case 4:
					args.Bot.Say("Feeling down eh? What you need is a cat.");
					break;
			}
			Utils.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: bad", new object[]{args.Player.Name, args.Bot.Name});
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
	        args.Bot.Say("*{1} hugs {0}", new object[]{args.Player.Name, args.Bot.Name});	
		}
		#endregion
		
		#region BotBan
		public static void BotBan(BotCommandArgs args)
		{
			List<TSPlayer> targets = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (targets.Count < 1)
				return;
			TSPlayer target = targets[0];
			
			if (args.Player.Group.HasPermission("ban"))
			{
				TShock.Utils.Ban(target, BotMain.CommandBot.Name + " ban", false, null);
				Utils.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: ban on {2}", new object[]{args.Player.Name, args.Bot.Name, target.Name});
			}
			else
			{
				args.Bot.Private(args.Player, "Sorry, but you don't have permission to use ban.");
				Utils.LogToConsole(ConsoleColor.Cyan, "{0} failed to use \"ban\" on {1} because of a lack of permission.", new object[]{args.Player.Name, target.Name});
			}
		}
		#endregion
		
		#region BotKick
		public static void BotKick(BotCommandArgs args)
		{
			List<TSPlayer> player = TShock.Utils.FindPlayer(args.Parameters[0]);
            TSPlayer plr = player[0];
			if (args.Player.Group.HasPermission("kick"))
			{
                TShock.Utils.Kick(plr, args.Bot.Name + " forcekick", false, false, null, false);
                Utils.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: kick on {2}", new object[]{args.Player.Name, args.Bot.Name, plr.Name});
			}
			else
            {
				args.Bot.Private(args.Player, "Sorry, but you don't have permission to use kick.");
				Utils.LogToConsole(ConsoleColor.Cyan, "{0} failed to use kick on {1} because of a lack of permission.", new object[]{args.Player.Name, plr.Name});
            }
		}
		#endregion
		
		#region BotMute
		public static void BotMute(BotCommandArgs args)
		{
			List<TSPlayer> player = TShock.Utils.FindPlayer(args.Parameters[0]);
			TSPlayer plr = player[0];
			if (args.Player.Group.HasPermission("mute"))
			{
				plr.mute = true;
				Utils.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: mute on {2}", new object[]{args.Player.Name, args.Bot.Name, plr.Name});
			}
			else
		    {
				Utils.LogToConsole(ConsoleColor.Cyan, "{0} failed to use mute on {1} because of a lack of permission.", new object[]{args.Player.Name, plr.Name});
               	args.Bot.Private(args.Player, "Sorry, but you don't have permission to use mute.");
		    }
		}
		#endregion
		
		#region BotUnmute
		public static void BotUnmute(BotCommandArgs args)
		{
			List<TSPlayer> player = TShock.Utils.FindPlayer(args.Parameters[0]);
			TSPlayer plr = player[0];
			
			if (args.Player.Group.HasPermission("mute"))
			{
				plr.mute = false;	
				Utils.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: unmute on {2}", new object[]{args.Player.Name, args.Bot.Name, plr.Name});
			}
			else
			{
				Utils.LogToConsole(ConsoleColor.Cyan, "{0} failed to use unmute on {1} because of a lack of permission.", new object[]{args.Player.Name, plr.Name});
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
				Utils.LogToConsole(ConsoleColor.Cyan,"{0} used {1} to execute: butcher", new object[]{args.Player.Name, args.Bot.Name});
			}
			else
			{
				Random r = new Random();
                int p = r.Next(1, 100);
                if (p <= BotMain.bcfg.ButcherCmdPct)
                {
                    Commands.HandleCommand(TShockAPI.TSPlayer.Server, "/butcher");
                   	args.Bot.Say("I butchered all hostile NPCs!");
					Utils.LogToConsole(ConsoleColor.Cyan,"{0} used {1} to execute: butcher", new object[]{args.Player.Name, args.Bot.Name});
                }
                else
                {
                	args.Bot.Say("Sorry {0}, you rolled a {1}. You need to roll less than {2} to butcher", new object[]{args.Player.Name, p, BotMain.bcfg.ButcherCmdPct});
                }
			}
		}
		#endregion
		
		#region BotHowAreYou
		public static void BotHowAreYou(BotCommandArgs args)
		{
            Random r = new Random();
            int p = r.Next(1, 10);
            if (p == 1)
            {
            	args.Bot.Say("{0}, I am feelings quite well today, thank you!", new object[]{args.Player.Name});
            }
            else if (p > 3 && p < 6)
            {
            	args.Bot.Say("{0}, I'm feeling a bit down. Might go get drunk later.", new object[]{args.Player.Name});
            }
            else if (p == 7 || p == 6)
            {
            	args.Bot.Say("{0}, Better than you. 'cos I'm AWESOME!", new object[]{args.Player.Name});
            }
            else if (p > 8 && p != 10)
            {
            	args.Bot.Say("{0}, I'm seeing unicorns and gnomes. How do you think I am?", new object[]{args.Player.Name});
            }
            else if (p == 10)
            {
            	args.Bot.Say("{0}, I just won the lottery. Stop being so poor in front of me.", new object[]{args.Player.Name});
            }
            Utils.LogToConsole(ConsoleColor.Cyan,"{0} used {1} to execute: howareyou", new object[]{args.Player.Name, args.Bot.Name});
		}
		#endregion
		
		#region BotInsult
		public static void BotInsult(BotCommandArgs args)
		{
			List<TSPlayer> ply = TShock.Utils.FindPlayer(args.Parameters[0]);
            string plr = ply[0].Name;
            Random r = new Random();
            int p = r.Next(1, 10);

            if (p == 1)
            { args.Bot.Say("Yo, {0}, I bet your mother is a nice lady!", new object[]{plr}); }
            if (p == 2)
            { args.Bot.Say("I bet {0}'s mother was a hamster, and their father smelled of elderberries.", new object[]{plr}); }
            if (p == 3)
            { args.Bot.Say("I bet {0} uses the term swag liberally.", new object[]{plr}); }
            if (p == 4)
            { args.Bot.Say("{0} is such a... twig!", new object[]{plr}); }
            if (p == 5)
            { args.Bot.Say("...But I'm a nice bot!... Sometimes"); }
            if (p == 6)
            { args.Bot.Say("{0} is such a! a... erm... thing!", new object[]{plr}); }
            if (p == 7)
            { args.Bot.Say("{0}, I'm so awesome you should feel insulted already.", new object[]{plr}); }
            if (p == 9)
            { args.Bot.Say("{0}... You remind me of someone named {1}.", new object[]{plr, BotMain.bcfg.GenericInsultName}); }
            if (p == 10)
            { args.Bot.Say("Don't tell me what to do, {0}!", new object[]{args.Player.Name}); }
            Utils.LogToConsole(ConsoleColor.Cyan, "{0} used {1} to execute: insult on {2}", new object[]{args.Player.Name, args.Bot.Name, plr});
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
				args.Bot.Trivia.CheckAnswer(string.Join(" ", args.Parameters.ToArray()), args.Player.Name);
			}
		}
		#endregion
	}	
}
