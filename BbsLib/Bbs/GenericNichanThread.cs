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
using Yoteichi.Bbs;

namespace Yoteichi.Bbs
{
    class GenericNichanThread : BaseNichanThread
    {
        // コンストラクタ
        public GenericNichanThread(string host, string itaName, int threadNumber, string title, int latestResNumber, GenericNichanBoard board)
            : base(host, itaName, threadNumber, title, latestResNumber, board)
        {
        }
    }
}
