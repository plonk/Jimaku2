using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yoteichi.Bbs
{
    /// <summary>
    /// レスを表すクラス
    /// </summary>
    public class Message
    {
        /// <param name="number">レス番号</param>
        public Message(int number, string name, string mail, string id, string date, string body)
        {
            Number = number;
            Name = name;
            Mail = mail;
            Id = id;
            Date = date;
            Body = body;
        }

        /// <summary>
        /// レス番号
        /// </summary>
        public int Number { get; private set; }
        public string Name { get; private set; }
        public string Mail { get; private set; }
        public string Id { get; private set; }
        public string Date { get; private set; }
        public string Body { get; private set; }
    }
}
