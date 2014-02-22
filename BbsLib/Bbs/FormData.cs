using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Yoteichi.Bbs
{
    /// <summary>
    /// フォーム送信クラス
    /// </summary>
    class FormData
    {
        // 送信先CGIプログラムのURI。
        public Uri Action { get; private set; }
        // Unicode 以外にも対応させるのは難しい？
        public Encoding Encoding { get; set; }

        public FormData(Uri action)
        {
            Action = action;
        }

        public Dictionary<string, string> Fields = new Dictionary<string, string>();

        private HttpContent Compile()
        {
            return new FormUrlEncodedContent(Fields);
        }

        /// <summary>
        /// フォームを Action で指定されたアドレスに POST メソッドを使って送信します。
        /// </summary>
        public HttpResponseMessage Submit()
        {
            HttpContent httpContent = Compile();

            var http = new HttpClient();

            Debug.WriteLine("{0} に以下の内容をPOSTします。\n{1}", Action, httpContent.ReadAsStringAsync().Result);
            Debug.Write("送信開始...");
            var response = http.PostAsync(Action, httpContent).Result;
            Debug.WriteLine("送信しました。");


            Debug.WriteLine("StatusCode={0}", response.StatusCode);
            Debug.WriteLine("Response Headers ----------");
            DebugPrintHeaders(response.Headers);
            Debug.WriteLine("Content Headers ----------");
            DebugPrintHeaders(response.Content.Headers);
            Debug.WriteLine("Content={0}", response.Content.ReadAsStringAsync().Result);

            return response;
        }

        [Conditional("DEBUG")]
        void DebugPrintHeaders(HttpHeaders headers)
        {
            foreach (var h in headers)
            {
                Debug.WriteLine("{0}: ", h.Key);

                int itemsPrintedSoFar = 0;
                foreach (var v in h.Value)
                {
                    if (itemsPrintedSoFar != 0)
                        Debug.Write(",");
                    Debug.Write("{0}", v);
                    itemsPrintedSoFar++;
                }
            }
        }
    }
}
