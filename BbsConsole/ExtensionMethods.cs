using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yoteichi.Bbs
{
    static class ExtensionMethods
    {
        /// <summary>
        /// Unix 時代の始まり
        /// </summary>
        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// 必要ならば UTC に変換した後、Unix 時間に変換する。
        /// </summary>
        static public int ToUnixTime(this DateTime self)
        {
            if (self.Kind != DateTimeKind.Utc)
                self = self.ToUniversalTime();

            return (int) self.Subtract(UnixEpoch).TotalSeconds;
        }

        /// <summary>
        /// int に変換する
        /// </summary>
        static public int ToInt(this string self)
        {
            return int.Parse(self);
        }

        /// <summary>
        /// タグを削除する
        /// </summary>
        static public string UnescapeHtml(this string self)
        {
            return WebText.UnescapeHtml(self);
        }
    }
}
