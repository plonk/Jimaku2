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
    class ShitarabaBoard : IBoard
    {
        public static int? GetThreadId(Uri uri)
        {
            Debug.Assert(IsBoardUri(uri));

            var match = Regex.Match(uri.AbsolutePath, @"^/bbs/read\.cgi/[A-Za-z]+/\d+/(\d+)($|/)");
            if (match.Success)
            {
                return match.Groups[1].Value.ToInt();
            }
            else
            {
                return null;
            }
        }

        public IThread CreateThread(int id, string title, int latestMessageNumber)
        {
            Uri threadUri = new Uri(String.Format("http://jbbs.shitaraba.net/bbs/read.cgi/{0}/{1}/{2}", m_Category, m_BoardNumber, id));
            return new ShitarabaThread(threadUri, title, latestMessageNumber.ToString(), this);
        }

        public static bool IsBoardUri(Uri uri)
        {
            // http://jbbs.shitaraba.net/game/48538/
            // http://jbbs.shitaraba.net/bbs/read.cgi/game/48538/1370999611/
            // http://jbbs.shitaraba.net/bbs/read.cgi/game/48538/1370999611/l50

            if (!uri.Host.Equals("jbbs.shitaraba.net", StringComparison.InvariantCultureIgnoreCase))
                return false;

            if (Regex.IsMatch(uri.AbsolutePath, @"^/[A-Za-z]+/\d+/?$"))
                return true;

            if (Regex.IsMatch(uri.AbsolutePath, @"^/bbs/read\.cgi/[A-Za-z]+/\d+/\d+($|/)"))
                return true;

            return false;
        }

        public string Name
        {
            get
            {
                return Settings["BBS_TITLE"];
            }
        }

        private string m_Category;
        private string m_BoardNumber;

        // コンストラクタ
        public ShitarabaBoard(Uri uri)
        {
            var match = Regex.Match(uri.AbsolutePath, @"^/([A-Za-z]+)/(\d+)/?$");

            if (match.Success)
            {
                m_Category = match.Groups[1].Value;
                m_BoardNumber = match.Groups[2].Value;
                return;
            }

            match = Regex.Match(uri.AbsolutePath, @"^/bbs/read\.cgi/([A-Za-z]+)/(\d+)/\d+($|/)");
            if (match.Success)
            {
                m_Category = match.Groups[1].Value;
                m_BoardNumber = match.Groups[2].Value;
                return;
            }
            throw new FormatException("bad Uri");
        }

        public Uri TopUri
        {
            get
            {
                // return new Uri(Settings["TOP"]);
                return new Uri(String.Format("http://jbbs.shitaraba.net/{0}/{1}/", m_Category, m_BoardNumber));
            }
        }

        Uri SubjectTxtUri
        {
            get
            {
                var uriString = String.Format("http://jbbs.shitaraba.net/{0}/{1}/subject.txt", m_Category, m_BoardNumber);
                return new Uri(uriString);
            }
        }

        public List<ThreadProxy> ThreadList { get; set; }

        /// <summary>
        /// subject.txt から ThreadList を生成する
        /// </summary>
        public void Reload()
        {
            var threadList = new List<ThreadProxy>();
            var http = new HttpClient();

            // スレッド一覧 subjet.txt をダウンロードする
            var responseMessage = http.GetAsync(SubjectTxtUri).Result;
            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new ApplicationException("スレッド一覧が取得できません。");
            }
            var bytes = responseMessage.Content.ReadAsByteArrayAsync().Result;

            // EUC から Unicode に変換
            var eucJp = Encoding.GetEncoding("EUC-JP");
            string body = eucJp.GetString(bytes);

            //重複するエントリーがあるので threadNumbers にすでに処理したスレッドの番号を入れて管理する
            List<string> threadNumbers = new List<string>();
            var lines = body.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                // "スレッドID.cgi,スレタイ(投稿数)"
                var match = Regex.Match(line, @"^(\d+)\.cgi,(.+)\((\d+)\)$");
                Debug.Assert(match.Success);
                var threadNumber = match.Groups[1].Value;
                var threadTitle = match.Groups[2].Value;
                var numberOfReses = match.Groups[3].Value;
                if (!threadNumbers.Contains(threadNumber))
                {
                    var threadUri = String.Format("http://jbbs.shitaraba.net/bbs/read.cgi/{0}/{1}/{2}",
                        m_Category, m_BoardNumber, threadNumber);
                    threadList.Add(new ThreadProxy(int.Parse(threadNumber), threadTitle, int.Parse(numberOfReses), this));
                    threadNumbers.Add(threadNumber);
                }
            }
            ThreadList = threadList;
        }

        public Uri GetDatUri(string threadNumber)
        {
            var uri = String.Format("http://jbbs.shitaraba.net/bbs/rawmode.cgi/{0}/{1}/{2}/",
                m_Category, m_BoardNumber, threadNumber);
            return new Uri(uri);
        }

        private Dictionary<string, string> m_Settings;
        public Dictionary<string, string> Settings
        {
            get
            {
                if (m_Settings == null)
                {
                    var http = new HttpClient();
                    byte[] bytes = http.GetByteArrayAsync(
                        String.Format("http://jbbs.shitaraba.net/bbs/api/setting.cgi/{0}/{1}/",
                        m_Category, m_BoardNumber)).Result;
                    var eucJp = Encoding.GetEncoding("EUC-JP");
                    string text = eucJp.GetString(bytes);
                    m_Settings = BoardSettings.ParseSettingTxt(text);
                }
                return m_Settings;
            }
        }

        void CreateNewThread(Message res, string threadTitle)
        {
        }

        /// <summary>
        /// メッセージを投稿する。
        /// </summary>
        /// <param name="msg">投稿するメッセージ</param>
        /// <param name="threadNumber">スレッド番号</param>
        public void Post(Message msg, int threadNumber)
        {
            var formData = new FormData(new Uri("http://jbbs.shitaraba.net/bbs/write.cgi/"));

            // write.cgiのパラメータ：
            // DIR=[板ジャンル]&BBS=[板番号]&TIME=[投稿時間]&NAME=[名前]
            // &MAIL=[メール]&MESSAGE=[本文]&KEY=[スレッド番号]&submit=書き込む

            formData.Fields["DIR"] = m_Category;
            formData.Fields["BBS"] = m_BoardNumber;
            formData.Fields["TIME"] = DateTime.Now.ToUnixTime().ToString();
            formData.Fields["NAME"] = msg.Name;
            formData.Fields["MAIL"] = msg.Mail;
            formData.Fields["MESSAGE"] = msg.Body;
            formData.Fields["KEY"] = threadNumber.ToString();
            formData.Fields["submit"] = "書き込む";

            var response = formData.Submit();

            // X-JBBS-Error ヘッダーがあった場合は、エラーが起こった。
            if (response.Headers.Contains("X-JBBS-Error"))
            {
                string errorCode = "(不明)";
                foreach (var header in response.Headers)
                {
                    if (header.Key == "X-JBBS-Error")
                        errorCode = header.Value.First();
                }
                var message = String.Format("投稿に失敗しました。X-JBBS-Error: {0}", errorCode);
                throw new ApplicationException(message);
            }
            if (!response.IsSuccessStatusCode)
                throw new ApplicationException("投稿の送信に失敗しました。");
        }
    }
}
