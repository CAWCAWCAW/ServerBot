using System;

namespace ServerBot
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