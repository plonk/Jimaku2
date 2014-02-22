using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yoteichi.Bbs;

namespace BbsLib.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void YoteichiShitaraba()
        {
            IBoard b = BoardGetter.GetBoard("http://jbbs.shitaraba.net/bbs/read.cgi/game/48538/1370999611/l50");
            Assert.AreEqual("Yoteichi.Bbs.ShitarabaBoard", b.GetType().ToString());
            Assert.AreEqual("http://jbbs.shitaraba.net/game/48538/", b.TopUri.ToString());
            Assert.IsNull(b.ThreadList);

            IBoard b2 = BoardGetter.GetBoard("http://jbbs.shitaraba.net/bbs/read.cgi/game/48538/1370999611/");
            Assert.AreEqual(b, b2);
        }

        [TestMethod]
        public void HonsureWaiwai()
        {
            IBoard b = BoardGetter.GetBoard("http://yy25.60.kg/test/read.cgi/peercastjikkyou/1372081742/");
            Assert.AreEqual("Yoteichi.Bbs.WaiwaiBoard", b.GetType().ToString());
            Assert.AreEqual("http://yy25.60.kg/peercastjikkyou/", b.TopUri.ToString());
            Assert.IsNull(b.ThreadList);
        }

        [TestMethod]
        public void YoteichiBintan()
        {
            IBoard b = BoardGetter.GetBoard("http://katsu.ula.cc/yoteichi/");
            Assert.AreEqual("Yoteichi.Bbs.BintanBoard", b.GetType().ToString());
            Assert.AreEqual("http://katsu.ula.cc/yoteichi/", b.TopUri.ToString());
            Assert.IsNull(b.ThreadList);
        }
    }
}
