using System;
using System.Text.RegularExpressions;

namespace Yoteichi.Bbs
{
    class BintanBoard : BaseNichanBoard
    {
        static readonly string _HostPattern = @"^katsu.ula.cc$";

        protected override string HostPattern
        {
            get { return _HostPattern; }
        }

        protected override string HtmlEncoding
        {
            get { return "UTF-8"; }
        }

        public override IThread CreateThread(int threadId, string title, int latestResNumber)
        {
            return new BintanThread(m_Host, m_ItaName, threadId, title, latestResNumber, this);
        }

        public BintanBoard(Uri uri)
            : base(uri)
        { }

        public static bool IsBoardUri(Uri uri)
        {
            var match = Regex.Match(uri.Host, _HostPattern);
            if (!match.Success)
                return false;
#warning バグあり
            match = Regex.Match(uri.AbsolutePath, @"^/([^/]+)");
            if (!match.Success)
                return false;
            return true;
        }
    }
}
