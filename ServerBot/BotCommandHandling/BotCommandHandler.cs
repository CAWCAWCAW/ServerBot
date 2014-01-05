using System;
using System.Collections.Generic;
using TShockAPI;

namespace ServerBot
{
	/// <summary>
	/// Holds and invokes all commands. Allows for easy registration of new commands.
	/// </summary>
	public class BotCommandHandler
	{
		public List<BotCommand> Commands = new List<BotCommand>();
		
		/// <summary>
		/// Takes in a command message and handles it appropriately
		/// </summary>
		/// <param name="message">Chat message to be handled.</param>
		/// <param name="ply">Player which sent the chat message.</param>
		public void HandleCommand(string message, TSPlayer ply)
		{
			string[] split = message.Split(' ');
			List<string> parms = new List<string>();
			
			parms.AddRange(split);
			string name = parms[1];
			
			parms.RemoveRange(0,2);
			BotCommandArgs args = new BotCommandArgs(name, parms, bTools.Bot, ply);
			foreach (BotCommand com in Commands)
			{
				if (com.Names.Contains(name))
				{
					com.Delegate(args);
				}
			}
		}
		
		public void RegisterCommand(string name, BotCommandDelegate command)
		{
			Commands.Add(new BotCommand(name, command));
		}
		
		public void RegisterCommand(List<string> names, BotCommandDelegate command)
		{
			Commands.Add(new BotCommand(names, command));
		}
		
		public static bool CheckForBotCommand(string msg)
		{
			if (msg.StartsWith(bTools.bot_Config.command_Char))
				return true;
			return false;
		}
	}
}
