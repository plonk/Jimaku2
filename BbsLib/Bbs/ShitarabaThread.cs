using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Yoteichi.Bbs
{
    class ShitarabaThread : IThread
    {
        ShitarabaBoard m_Board;
        public int LatestMessageNumber { get; set; }
        public string Title { get; set; }
        public int Id { get { return int.Parse(m_ThreadNumber); } }
        string m_ThreadNumber;

        public IBoard Board { get { return m_Board; } }

        Uri RawModeCgiUri
        {
            get
            {
                return m_Board.GetDatUri(m_ThreadNumber);
            }
        }

        public Uri Uri { get; set; }

        public ShitarabaThread(Uri uri, string threadTitle, string numberOfReses, ShitarabaBoard board)
        {
            Uri = uri;
            var match = Regex.Match(uri.AbsolutePath, @"^/bbs/read\.cgi/([A-Za-z]+)/(\d+)/(\d+)");
            if (!match.Success)
                throw new FormatException("bad uri");
            m_Board = board;
            m_ThreadNumber = match.Groups[3].Value;
            Title = threadTitle;
            LatestMessageNumber = int.Parse(numberOfReses);

            Reload();
        }

        public List<Message> MessageList { get; set; }

        public void Reload()
        {
            if (MessageList == null)
            {
                var resList = new List<Message>();

                var http = new HttpClient();

                var bytes = http.GetByteArrayAsync(RawModeCgiUri).Result;
                var eucJp = Encoding.GetEncoding("EUC-JP");
                string dat = eucJp.GetString(bytes);
                var lines = dat.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    resList.Add(CreateRes(line));
                }
                MessageList = resList;
            }
            else
            {
                var http = new HttpClient();
                var bytes = http.GetByteArrayAsync(RawModeCgiUri.ToString() + (LatestMessageNumber + 1) + "-").Result;
                var eucJp = Encoding.GetEncoding("EUC-JP");
                string dat = eucJp.GetString(bytes);
                var lines = dat.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                Debug.WriteLine("Reload: {0} new messages", lines.Length);
                foreach (var line in lines)
                {
                    MessageList.Add(CreateRes(line));
                }
                LatestMessageNumber += lines.Length;
            }
        }

        private enum FieldIndex
        {
            ResNo = 0,
            Name,
            Mail,
            Date,
            Body,
            ThreadName,
            Id,
        };

        private static Message CreateRes(string datLine)
        {
            string[] fields;
            fields = datLine.Split(new string[] { "<>" }, StringSplitOptions.None);

            int number = int.Parse(fields[(int) FieldIndex.ResNo]);
            string name = fields[(int) FieldIndex.Name];
            string mail = fields[(int) FieldIndex.Mail];
            string id = fields[(int) FieldIndex.Id];
            string date = fields[(int) FieldIndex.Date];
            string body = fields[(int) FieldIndex.Body];

            return new Message(number, name, mail, id, date, body);
        }

        public bool IsStopped
        {
            get
            {
                int maximumResNumber = int.Parse(m_Board.Settings["BBS_THREAD_STOP"]);
                return (maximumResNumber == LatestMessageNumber);
            }
        }
    }
}
