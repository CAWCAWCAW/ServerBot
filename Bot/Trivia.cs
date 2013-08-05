using TShockAPI;
using System;
using System.IO;
using System.Collections.Generic;

namespace Bot
{
	/// <summary>
	/// Handles the trivia game for a bot.
	/// </summary>
	public class Trivia
	{
		private TriviaConfig Config;
		private Bot Master;
		public bool Enabled = false;
		public bool OngoingGame = false;
		public TriviaItem CurrentQuestion;
		public List<TriviaItem> UnaskedQuestions = new List<TriviaItem>();
		
		public Trivia(Bot master) 
		{ 
			Master = master;
			LoadConfig(BotMain.TriviaSave);
			if (Enabled)
				UnaskedQuestions.AddRange(Config.TriviaItems);
		}
		
		public void LoadConfig(string path)
		{
			LoadConfig(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
		}
		
		public void LoadConfig(Stream stream)
		{
			try
			{
				Config = TriviaConfig.Read(stream);
				Enabled = true;
			}
			catch
			{
				TShockAPI.Log.ConsoleError(string.Format("Trivia config for the bot named {0} has failed to load. Trivia will be disabled until a proper config is given.", Master.Name));
			}
		}
	}
}
