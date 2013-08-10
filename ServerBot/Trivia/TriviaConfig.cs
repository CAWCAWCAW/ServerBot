using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ServerBot
{
    public class TriviaConfig
    {
    	public List<TriviaItem> TriviaItems = new List<TriviaItem>();


        public static TriviaConfig Read(string path)
        {
            if (!File.Exists(path))
                return new TriviaConfig();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(fs);
            }
        }

        public static TriviaConfig Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                var cf = JsonConvert.DeserializeObject<TriviaConfig>(sr.ReadToEnd());
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
    }
}
