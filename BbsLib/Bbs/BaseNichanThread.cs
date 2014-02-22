using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Yoteichi.Bbs
{
    class BaseNichanThread : IThread
    {
        protected int m_LatestResNumber;
        protected int m_ThreadNumber;
        protected string m_ItaName;
        protected string m_Host;
        protected BaseNichanBoard m_Board;

        int m_DatLastSize;
        DateTimeOffset? m_DatLastModified;
        List<Message> m_ResList;

        public IBoard Board { get { return m_Board; } }

        public string Title { get; set; }

        public int LatestMessageNumber
        {
            get { return m_LatestResNumber; }
        }

        public int Id
        {
            get
            {
                return m_ThreadNumber;
            }
        }
        public Uri Uri
        {
            get
            {
                return new Uri(String.Format("http://{0}/{1}/", m_Host, m_ItaName));
            }
        }

        // コンストラクタ
        public BaseNichanThread(string host, string itaName, int threadNumber, string title, int latestResNumber, BaseNichanBoard board)
        {
            m_Host = host;

            m_ItaName = itaName;
            m_ThreadNumber = threadNumber;

            m_LatestResNumber = latestResNumber;
            Debug.Print("WaiwaiThread ctor: m_LatestResNumber={0}", m_LatestResNumber);

            // Title プロパティを設定
            Title = title;

            m_Board = board;
            GetAllPosts();
        }

        private string DatUri
        {
            get
            {
                return String.Format("http://{0}/{1}/dat/{2}.dat",
                        m_Host, m_ItaName, m_ThreadNumber);
            }
        }

        private void GetAllPosts()
        {
            // 初回取得
            Debug.Assert(m_DatLastModified == null);
            Debug.Assert(m_DatLastSize == 0);
            Debug.Assert(m_ResList == null);
            ReloadAllPosts();
        }

        private void ReloadAllPosts()
        {
            var http = new HttpClient();
            var responseMessage = http.GetAsync(DatUri).Result;
            m_DatLastModified = responseMessage.Content.Headers.LastModified;
            var contentInBytes = responseMessage.Content.ReadAsByteArrayAsync().Result;
            m_DatLastSize = contentInBytes.Length;
            string content = Encoding.GetEncoding("Shift_JIS").GetString(contentInBytes);
            m_ResList = ParseDat(content);
            m_LatestResNumber = m_ResList.Count;
        }

        public List<Message> MessageList
        {
            get
            {
                return m_ResList;
            }
        }

        private List<Message> ParseDat(string buf, int resNumber = 1)
        {
            string[] lines = buf.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<Message> posts = new List<Message>();
            foreach (var line in lines)
            {
                posts.Add(CreateResFromString(line, resNumber));
                resNumber++;
            }
            return posts;
        }


        public void Reload()
        {
            if (m_ResList == null)
            {
                GetAllPosts();
            }
            else
            {
                Debug.Print("requesting to {2} GET {0} If-Modified-Since {1}...", DatUri, m_DatLastModified, m_Host);

                var http = new HttpClient();
                http.DefaultRequestHeaders.IfModifiedSince = m_DatLastModified;
                http.DefaultRequestHeaders.Range = new RangeHeaderValue(m_DatLastSize - 1, null);
                HttpResponseMessage responseMessage;
                try
                {
                    responseMessage = http.GetAsync(DatUri).Result;
                }
                catch
                {
                    Debug.Print("Update: Error: Server did not respond");
                    return;
                }

                Debug.Print("server {0} responded {1}", m_Host, responseMessage.StatusCode);
                switch (responseMessage.StatusCode)
                {
                case HttpStatusCode.PartialContent:
                    m_DatLastModified = responseMessage.Content.Headers.LastModified;
                    var contentInBytes = responseMessage.Content.ReadAsByteArrayAsync().Result;
                    Debug.WriteLine("{0} bytes received", contentInBytes.Length);
                    if (contentInBytes[0] != '\n')
                    {
                        goto Reload;
                    }
                    m_DatLastSize += contentInBytes.Length - 1;
                    string content = Encoding.GetEncoding("Shift_JIS").GetString(contentInBytes);
                    Debug.Assert(content[0] == '\n');
                    content = content.TrimStart(new char[] { '\n' });
                    var newPosts = ParseDat(content, m_LatestResNumber + 1);
                    m_ResList.AddRange(newPosts);
                    Debug.Assert(m_ResList.Count == m_LatestResNumber + newPosts.Count);
                    m_LatestResNumber = m_ResList.Count;
                    break;
                case HttpStatusCode.NotModified:
                    break;
                case HttpStatusCode.RequestedRangeNotSatisfiable:
                Reload:
                    Debug.WriteLine("あぼ～んを検出しました。読み直します");
                    ReloadAllPosts();
                    break;
                default:
                    Debug.WriteLine("なんかおかしいです。");
                    break;
                }
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


        //[名前]<>[メール]<>[日付] [ID] [BE-ID]<>[本文]<>[スレッドタイトル]
        //（[日付] [ID] [BE-ID]に関してはいずれかの項目が欠けている場合があります。）
        enum FieldIndex
        {
            Name = 0,
            Mail,
            DateId,
            Body,
            ThreadName,
        };

        private static Message CreateResFromString(string datLine, int resNumber)
        {
            Debug.Assert(resNumber > 0);

            string[] fields;
            fields = datLine.Split(new string[] { "<>" }, StringSplitOptions.None);

            string name = fields[(int) FieldIndex.Name];
            string mail = fields[(int) FieldIndex.Mail];

            string dateId = fields[(int) FieldIndex.DateId];
            var pair = dateId.Split(new string[] { " ID:" }, StringSplitOptions.None);
            string date = pair.First();
            string id = pair.Last();

            string body = fields[(int) FieldIndex.Body];
            return new Message(resNumber, name, mail, id, date, body);
        }

        public bool IsStopped
        {
            get
            {
                return false;
            }
        }
    }
}
