using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yoteichi.Bbs
{
    class BintanThread : BaseNichanThread
    {
        public BintanThread(string host, string itaName, int threadNumber, string title, int latestResNumber, BintanBoard board)
            : base(host, itaName, threadNumber, title, latestResNumber, board)
        {
        }
    }
}
