using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jimaku2
{
    static class WebText
    {
        static Dictionary<string, string> m_EntityTable;

        public static string StripTags(string str)
        {
            str = Regex.Replace(str, @"<[^>]+>", "");
            return str;
        }

        static WebText()
        {
            m_EntityTable = new Dictionary<string, string>
                {
                    { "lt",   "<"  },
                    { "gt",   ">"  },
                    { "amp",  "&"  },
                    { "quot", "\"" },
                    { "nbsp", " "  },
	            };
        }

        private static string EntityMatchToCharacter(Match match)
        {
            string name = match.Groups[1].Value; // &(...);
            if (name.StartsWith("#"))
            {
                string digits = name.Substring(1, name.Length - 1);
                Int32 codepoint = Int32.Parse(digits);
                string returnValue = "";
                returnValue += char.ConvertFromUtf32(codepoint);
                return returnValue;
            }
            else
            {
                if (m_EntityTable.ContainsKey(name))
                {
                    return m_EntityTable[name];
                }
                else
                {
                    return "〓";
                }
            }
        }

        public static string DecodeEntities(string str)
        {
            str = Regex.Replace(str, @"&(\w+|#\d+);", EntityMatchToCharacter);
            return str;
        }
        
        /// <summary>
        /// DATファイルの本文フィールドのデコードを想定している。
        /// BRタグは改行に変換。その他のタグは削除して、HTMLエンティティはデコードする。
        /// </summary>
        public static string UnescapeHtml(string input)
        {
            string tmp = input;
            tmp = tmp.Replace("<br>", "\n");
            tmp = WebText.StripTags(tmp);
            tmp = WebText.DecodeEntities(tmp);
            return tmp;
        }
    }
}
