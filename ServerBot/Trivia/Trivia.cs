using TShockAPI;
using System;
using System.IO;
using System.Collections.Generic;

namespace ServerBot
{
	/// <summary>
	/// Handles the trivia game for a bot.
	/// </summary>
	public class Trivia
	{
		private TriviaConfig Config;
		private int NumQuestions;
		private int NumQuestionsAsked;
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
		
		public void StartGame(int NumQ)
		{
			OngoingGame = true;
			NumQuestions = NumQ;
			NumQuestionsAsked = 0;
			Master.Say("A Trivia game is about to start. I'll ask you questions and you use the \"^ answer\" bot command to answer me!");
			Master.Say("If you answer the question correctly then you'll be rewarded with a prize!");
			AskQuestion();
		}
		
		public void AskQuestion()
		{
			NumQuestionsAsked += 1;
			int choice = new Random().Next(UnaskedQuestions.Count);
			CurrentQuestion = UnaskedQuestions[choice];
			
			Master.Say("Question Number {0}:", new object[]{NumQuestionsAsked.ToString()});
			Master.Say(CurrentQuestion.Question);
			
			UnaskedQuestions.Remove(CurrentQuestion);
		}
		
		public void CheckAnswer(string ans, string player)
		{
			if (CurrentQuestion.Answer.ToLower() == ans.ToLower())
			{
				Master.Say("Congrats, {0}. You got the correct answer!", new object[]{player});
				//TODO: Insert Prize code here.
				if (NumQuestionsAsked < NumQuestions)
				{
					AskQuestion();
				}
				else
				{
					Master.Say("The Trivia game has ended. Thank you all for participating.");
					OngoingGame = false;
					UnaskedQuestions.Clear();
					UnaskedQuestions.AddRange(Config.TriviaItems);
				}
			}
		}
	}
}
