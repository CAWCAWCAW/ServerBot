using System;
using System.Collections.Generic;
using TShockAPI;

namespace ServerBot
{
	/// <summary>
	/// Wraps functions that are called when their appropriate command is invoked.
	/// </summary>
	public delegate void BotCommandDelegate(BotCommandArgs args);
	
	/// <summary>
	/// Arguments required for functions wrapped by the BotCommandDelegate.
	/// Provides all necessary info to the command function.
	/// </summary>
	/// <param name="command">The name of the command. 
	/// The word to be used after the command character.
	/// </param>
	/// <param name="parms">The parameters passed to the command by the player. 
	/// This includes all words past the command name.
	/// </param>
	/// <param name="ply">The player that called the command.</param>
	public class BotCommandArgs : EventArgs
	{
		public string Command;
		public List<string> Parameters;
		public TSPlayer Player;
		public bBot Bot;
		
		public BotCommandArgs(string command, List<string> parms, bBot bot, TSPlayer ply)
		{
			Command = command;
			Parameters = parms;
			Player = ply;
			Bot = bot;
		}
	}
	
	public class BotCommand
	{
		public List<string> Names = new List<string>();
		public BotCommandDelegate Delegate;
		
		public BotCommand(string name, BotCommandDelegate com)
		{
			Names.Add(name);
			Delegate = com;
		}
		
		public BotCommand(List<string> names, BotCommandDelegate com)
		{
			Names = names;
			Delegate = com;
		}
	}
}
