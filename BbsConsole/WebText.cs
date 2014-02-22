using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Yoteichi.Bbs
{
    static class WebText
    {
        /// <summary>
        /// encoding で表現できない文字を &#XXXX; 形式の文字実体参照にエンコードする
        /// </summary>
        /// <param name="encoding">EUC-JP, Shift_JIS, ASCII など</param>
        static string HtmlEncodeForeignCharacters(string input, string encoding)
        {
            int initialCapacity = input.Length * 2;
            Encoding domesticEncoding = Encoding.GetEncoding(encoding,
                new EncoderExceptionFallback(), new DecoderExceptionFallback());
            var output = new StringBuilder(initialCapacity);
            foreach (char c in input)
            {
                string s = new String(c, 1);
                try
                {
                    byte[] bytes = domesticEncoding.GetBytes(s);
                }
                catch (EncoderFallbackException)
                {
                    var entity = "&#" + ((int) c).ToString() + ";";
                    output.Append(entity);
                    goto Skip;
                }
                output.Append(c);
            Skip: ;
            }
            Debug.WriteLineIf(output.Capacity > initialCapacity,
                String.Format(
                "HtmlEncodeForeignCharacters: Buffer reallocation({0} -> {1})",
                initialCapacity, output.Capacity));
            return output.ToString();
        }

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
            tmp = tmp.Replace("<hr>", "\n--------------------------\n");
            tmp = Regex.Replace(tmp, @"<!--.*-->", "");
            tmp = WebText.StripTags(tmp);
            tmp = WebText.DecodeEntities(tmp);
            return tmp;
        }
    }
}
