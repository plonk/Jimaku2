using System;
using System.Text.RegularExpressions;

namespace Yoteichi.Bbs
{
    class WaiwaiBoard : BaseNichanBoard
    {
        // ホスト名は *.kakiko.com あるいは yy*.60.kg である
        static readonly string _HostPattern = @"^(.+\.kakiko\.com|yy\d+\.60\.kg)$";

        protected override string HostPattern
        {
            get { return _HostPattern; }
        }

        protected override string HtmlEncoding
        {
            get { return "Shift_JIS"; }
        }

        public override IThread CreateThread(int threadId, string title, int latestResNumber)
        {
            return new WaiwaiThread(m_Host, m_ItaName, threadId, title, latestResNumber, this);
        }

        public WaiwaiBoard(Uri uri)
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
