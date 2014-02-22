using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Yoteichi.Bbs
{
    /// <summary>
    /// ２ちゃんねると同様のプロトコルでアクセスできる掲示板サービスのための板クラス。
    /// </summary>
    abstract class BaseNichanBoard : IBoard
    {
        public Uri TopUri { get; private set; }
        public List<ThreadProxy> ThreadList { get; set; }

        protected string m_ItaName;
        protected string m_Host;
        protected string m_Name;

        /// <summary>
        /// ホスト部が満たすべき正規表現パターンを表す文字列。具象クラスで定義される「パラメータ」
        /// </summary>
        abstract protected string HostPattern { get; }
        /// <summary>
        /// ブラウザ用板トップのエンコーディングを表す文字列。具象クラスで定義される「パラメータ」
        /// </summary>
        abstract protected string HtmlEncoding { get; }
        /// <summary>
        /// 具象クラスに対応するスレッドクラスのインスタンスを作成する
        /// </summary>
        abstract public IThread CreateThread(int threadId, string title, int latestResNumber);

        /// <summary>
        /// 掲示板のタイトル
        /// </summary>
        public string Name
        {
            get
            {
                if (m_Name == null)
                {
                    m_Name = GetBoardTitle();
                }
                return m_Name;
            }
        }

        /// <summary>
        /// 板トップのページからタイトルを得る
        /// </summary>
        private string GetBoardTitle()
        {
            var http = new HttpClient();

            http.DefaultRequestHeaders.Range = new RangeHeaderValue(0, 1024);
            byte[] bytes = http.GetByteArrayAsync(TopUri).Result;
            var html = Encoding.GetEncoding(HtmlEncoding).GetString(bytes);
            var match = Regex.Match(html, "<title>(.*)</title>", RegexOptions.IgnoreCase);
            if (!match.Success)
                return m_ItaName;

            var title = match.Groups[1].Value;
            title = title.Trim();
            return WebText.UnescapeHtml(title);
        }

        /// <summary>
        /// スレッドを表示するURIからスレッドIDを得る
        /// </summary>
        /// <param name="uri">パスが /test/read.cgi で始まるURI</param>
        /// <returns>スレッドIDが特定できなければ null</returns>
        public static int? GetThreadId(Uri uri)
        {
            var match = Regex.Match(uri.AbsolutePath, @"^/test/read\.cgi/[A-Za-z0-9]+/(\d+)($|/)");
            if (match.Success)
            {
                return match.Groups[1].Value.ToInt();
            }
            else
            {
                return null;
            }
        }

        // コンストラクタ
        public BaseNichanBoard(Uri uri)
        {
            var match = Regex.Match(uri.Host, HostPattern);
            if (!match.Success)
                throw new ArgumentException();
            m_Host = uri.Host;

            if (Regex.IsMatch(uri.AbsolutePath, @"^/test/read.cgi/"))
            {
                match = Regex.Match(uri.AbsolutePath, @"^/test/read\.cgi/([^/]+)");
            }
            else
            {
                match = Regex.Match(uri.AbsolutePath, @"^/([^/]+)");
            }
            if (!match.Success)
                throw new ArgumentException();
            m_ItaName = match.Groups[1].Value;

            TopUri = new Uri(String.Format("http://{0}/{1}/", m_Host, m_ItaName));
        }

        public void Reload()
        {
            if (ThreadList == null)
            {
                ThreadList = new List<ThreadProxy>();

                string ThreadListUri = string.Format("http://{0}/{1}/subject.txt", m_Host, m_ItaName);
                string buf = HttpGet(ThreadListUri, "Shift_JIS");
                string[] lines = buf.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // string pattern = String.Format("{0}.dat<>", threadNumber);
                foreach (var line in lines)
                {
                    var match = Regex.Match(line, @"^(\d+)\.dat<>(.+?)\((\d+)\)$");
                    Debug.Assert(match.Success);
                    var threadNumber = int.Parse(match.Groups[1].Value);
                    string title = match.Groups[2].Value;
                    int latestResNumber = int.Parse(match.Groups[3].Value);
                    //ThreadList.Add(CreateThread(threadNumber, title, latestResNumber));
                    ThreadList.Add(new ThreadProxy(threadNumber, title, latestResNumber, this));
                }
            }
            else
            {
                // 実装せよ
            }
        }

        private byte[] HttpGet(string uri)
        {
            Debug.WriteLine("HttpGet(\"{0}\")", uri.ToString(), "");
            var http = new HttpClient();
            var responseMessage = http.GetAsync(uri).Result;
            foreach (var header in responseMessage.Headers)
            {
                Debug.WriteLine("{0}: {1}", header.Key, header.Value.First());
            }
            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                throw new ApplicationException();
            }
            var content = responseMessage.Content.ReadAsByteArrayAsync().Result;
            Debug.WriteLine("HttpGet got {0} bytes", content.Length);
            return content;
        }

        private string HttpGet(string uri, string encoding)
        {
            byte[] buf = HttpGet(uri);

            return Encoding.GetEncoding(encoding).GetString(buf);
        }

        public void Post(Message res, int threadNumber)
        {
            throw new ApplicationException("この種類の板に対する投稿は未実装です。");
        }
    }
}
