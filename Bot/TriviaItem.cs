using System;

namespace Bot
{
	public struct TriviaItem
	{
		public TriviaItem(string q, string a)
		{
			Question = q;
			Answer = a;
		}
		public string Question;
		public string Answer;
	}
}