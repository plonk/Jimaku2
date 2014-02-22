using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yoteichi.Bbs
{
    static class BoardSettings
    {
        public static Dictionary<string, string> ParseSettingTxt(string text)
        {
            var dictionary = new Dictionary<string, string>();

            text = text.Replace("\r\n", "\n");
            var lines = text.Split(new char [] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var pair = line.Split(new char [] { '=' }, 2);
                Debug.Assert(pair.Length == 2);
                dictionary.Add(pair.First(), pair.Last());
            }
            return dictionary;
        }
    }
}
