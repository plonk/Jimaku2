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
    static class Service
    {
        static public bool IsBoard(Uri uri)
        {
            Server s = GetServer(uri);
            return s.IsBoard(uri);
        }
        static public bool IsThread(Uri uri)
        {
            Server s = GetServer(uri);
            return s.IsThread(uri);
        }
        static public Board GetBoard(Uri uri)
        {
            Server s = GetServer(uri);
            return s.GetBoard(uri);
        }
        static public Thread GetThread(Uri uri)
        {
            Server s = GetServer(uri);
            return s.GetThread(uri);
        }
        private static ShitarabaServer m_ShitarabaServer;
        static Server GetServer(Uri uri)
        {
            return m_ShitarabaServer;
        }

        // 静的コンストラクタ
        static Service()
        {
            m_ShitarabaServer = new ShitarabaServer();
        }
    }

    class Board
    {
        public Uri Uri
        {
            get { return _server.GetBoardUri(this); }
        }
        public string LocalRules
        {
            get { return _server.GetLocalRules(this); }
        }
        public List<Thread> Threads;
        public bool IsLoaded { get; private set; }
        public void Load()
        {
            if (IsLoaded == true)
                return;

            Threads = _server.GetThreads(this);

            IsLoaded = true;
        }
        public void Update()
        {
        }
        public string Title
        {
            get { return _server.GetBoardTitle(this); }
        }
        public void CreateNewThread(string title, Message msg)
        {
            _server.CreateNewThread(this, title, msg);
        }

        // event ThreadDeleted;
        // event NewThread;

        public Server _server;
        public object _data;
    }

    class Thread
    {
        public Uri Uri;
        public string Title { get; private set; }
        public bool IsLoaded { get; private set; }
        public void Load()
        {
            IsLoaded = true;
            Messages = _server.GetMessages(this);
        }
        public void Update()
        {
        }
        public List<Message> Messages { get; private set; }
        public int Number { get; private set; }
        public DateTime CreationTime;
        public double ActivityIndex;
        public void PostNewMessage(Message msg)
        {
            _server.PostNewMessage(this, msg);
        }
        public Thread(int threadNumber, string title, Server server, object data)
        {
            Number = threadNumber;
            Title = title;

            _server = server;
            _data = data;
        }

        // event MessageDeleted;
        // event NewMessage;

        public Server _server;
        public object _data;
    }

    struct Message
    {

        public Message(int number, string name, string mail, string id, string date, string body) 
            : this()
        {
            Number = number;
            Name = name;
            Mail = mail;
            Id = id;
            Date = date;
            Body = body;
        }

        public int Number { get; set; }
        public string Name { get; set; }
        public string Mail { get; set; }
        public string Id { get; set; }
        public string Date { get; set; }
        public string Body { get; set; }
    }

    abstract class Server
    {
        abstract public bool IsBoard(Uri url);
        abstract public bool IsThread(Uri url);
        abstract public Board GetBoard(Uri url);
        abstract public Thread GetThread(Uri url);
        abstract public string GetLocalRules(Board b);
        abstract public string GetBoardTitle(Board b);
        abstract public List<Message> GetMessages(Thread t);
        abstract public List<Thread> GetThreads(Board b);
        abstract public List<Message> GetNewMessages(Thread t);
        abstract public Uri GetBoardUri(Board b);
        abstract public Uri GetThreadUri(Thread t);
        abstract public void CreateNewThread(Board b, string title, Message msg);
        abstract public void PostNewMessage(Thread b, Message msg);
    }

    class ShitarabaServer : Server
    {
        class ThreadData
        {
            public int LatestMessageNumber;
            public Uri Uri { get; set; }
        }

        class BoardData
        {
            public string Category { get; set; }
            public string BoardNumber { get; set; }
            public Uri uri { get; set; }
            public Dictionary<string, string> Settings;
        }

        override public bool IsBoard(Uri uri)
        {
            // http://jbbs.shitaraba.net/game/48538/
            // http://jbbs.shitaraba.net/bbs/read.cgi/game/48538/

            if (uri.Host.Equals("jbbs.shitaraba.net", StringComparison.InvariantCultureIgnoreCase) &&
                (Regex.Match(uri.AbsolutePath, @"^/[A-Za-z]+/\d+/?$").Success) ||
                Regex.Match(uri.AbsolutePath, @"^/bbs/read\.cgi/[A-Za-z]+/\d+($|/)").Success)
                return true;
            else
                return false;
        }

        override public bool IsThread(Uri uri)
        {
            // http://jbbs.shitaraba.net/bbs/read.cgi/game/48538/1370999611/
            // http://jbbs.shitaraba.net/bbs/read.cgi/game/48538/1370999611/l50

            if (!uri.Host.Equals("jbbs.shitaraba.net", StringComparison.InvariantCultureIgnoreCase))
                return false;

            if (!Regex.Match(uri.AbsolutePath, @"^/bbs/read\.cgi/[A-Za-z]+/\d+/\d+($|/)").Success)
                return false;

            return true;
        }

        override public Board GetBoard(Uri uri)
        {
            Board board = m_BoardPool.Find((Board b) =>
                {
                    return b.Uri == uri;
                });
            if (board == null)
                board = CreateBoard(uri);

            return board;
        }

        private Board CreateBoard(Uri uri)
        {
            Debug.Assert(uri.Host.Equals("jbbs.shitaraba.net", StringComparison.InvariantCultureIgnoreCase));

            Match match;
            match = Regex.Match(uri.AbsolutePath, @"^/([A-Za-z]+)/(\d+)/?$");
            if (!match.Success)
                match = Regex.Match(uri.AbsolutePath, @"^/bbs/read\.cgi/([A-Za-z]+)/(\d+)($|/)");
            Debug.Assert(match.Success);

            Board b = new Board();

            b._server = this;
            var data = new BoardData();

            data.Category = match.Groups[1].Value;
            data.BoardNumber = match.Groups[2].Value;
            b._data = data;

            return b;
        }

        override public Thread GetThread(Uri uri)
        {
            Debug.Assert(uri.Host.Equals("jbbs.shitaraba.net", StringComparison.InvariantCultureIgnoreCase));

            var match = Regex.Match(uri.AbsolutePath, @"^/bbs/read\.cgi/([A-Za-z]+)/(\d+)/(\d+)($|/)");
            Debug.Assert(match.Success);

            Board board = GetBoard(uri);
            board.Load();

            int threadNumber = match.Groups[3].Value.ToInt();

            foreach (var t in board.Threads)
            {
                if (t.Number == threadNumber)
                    return t;
            }
            return null;
        }

        override public string GetLocalRules(Board b)
        {
            return "unimplemented";
        }
        override public string GetBoardTitle(Board b)
        {
            var data = b._data as BoardData;
            if (data.Settings == null)
                data.Settings = GetSettings(data.Category, data.BoardNumber.ToInt());
            return data.Settings["BBS_TITLE"];
        }
        override public List<Message> GetMessages(Thread t)
        {
            var messageList = new List<Message>();

            var http = new HttpClient();
            ThreadData threadData = t._data as ThreadData;
            Board b = GetBoard(threadData.Uri);
            BoardData boardData = b._data as BoardData;
            var rawModeCgiUri = String.Format("http://jbbs.shitaraba.net/bbs/rawmode.cgi/{0}/{1}/{2}/",
                boardData.Category, boardData.BoardNumber, t.Number);
            var bytes = http.GetByteArrayAsync(rawModeCgiUri).Result;
            var eucJp = Encoding.GetEncoding("EUC-JP");
            string dat = eucJp.GetString(bytes);
            var lines = dat.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                messageList.Add(CreateMessage(line));
            }
            return messageList;
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

        private static Message CreateMessage(string datLine)
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
        override public List<Thread> GetThreads(Board b)
        {
            var threadList = new List<Thread>();
            var http = new HttpClient();
            BoardData boardData = b._data as BoardData;
            string subjectTxtUri = String.Format("http://jbbs.shitaraba.net/{0}/{1}/subject.txt",
                boardData.Category, boardData.BoardNumber);
            var responseMessage =  http.GetAsync(subjectTxtUri).Result;
            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new ApplicationException("スレッド一覧が取得できません。");
            }
            var bytes = responseMessage.Content.ReadAsByteArrayAsync().Result;

            var eucJp = Encoding.GetEncoding("EUC-JP");
            string body = eucJp.GetString(bytes);

            List<string> threadNumbers = new List<string>();
            var lines = body.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                // "1370999611.cgi,19(2)"
                var match = Regex.Match(line, @"^(\d+)\.cgi,([^(]+)\((\d+)\)$");
                Debug.Assert(match.Success);
                var threadNumber = match.Groups[1].Value;
                var threadTitle = match.Groups[2].Value;
                var numberOfReses = match.Groups[3].Value;
                if (!threadNumbers.Contains(threadNumber))
                {
                    var threadUri = String.Format("http://jbbs.shitaraba.net/bbs/read.cgi/{0}/{1}/{2}",
                        boardData.Category, boardData.BoardNumber, threadNumber);
                    ThreadData threadData = new ThreadData();
                    threadData.Uri = new Uri(threadUri);
                    var newThread = new Thread(threadNumber.ToInt(), threadTitle, this, threadData);
                    threadList.Add(newThread);
                    threadNumbers.Add(threadNumber);
                }
            }
            return threadList;
        }
        override public List<Message> GetNewMessages(Thread t)
        {
            return new List<Message>();
        }
        override public Uri GetBoardUri(Board b)
        {
            BoardData data = b._data as BoardData;
            return data.uri;
        }
        override public Uri GetThreadUri(Thread t)
        {
            ThreadData data = t._data as ThreadData;
            return data.Uri;
        }
        override public void CreateNewThread(Board b, string title, Message msg)
        {
            return;
        }
        override public void PostNewMessage(Thread b, Message msg)
        {
            return;
        }

        private Dictionary<string, string> GetSettings(string m_Category, int m_BoardNumber)
        {
            var http = new HttpClient();
            byte[] bytes = http.GetByteArrayAsync(
                String.Format("http://jbbs.shitaraba.net/bbs/api/setting.cgi/{0}/{1}/",
                m_Category, m_BoardNumber)).Result;
            var eucJp = Encoding.GetEncoding("EUC-JP");
            string text = eucJp.GetString(bytes);
            return BoardSettings.ParseSettingTxt(text);
        }

        List<Board> m_BoardPool;

        public ShitarabaServer()
        {
            m_BoardPool = new List<Board>();
        }
    }
    //class NiChannelServer : Server
    //class BintanServer : NiChannelServer
    //class WaiwaiKakikoServer : NiChannelServer
}
