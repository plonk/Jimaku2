using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jimaku2.Properties;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Jimaku2
{
    class JimakuRenderer
    {
        int m_MarginLeft = 30;
        int m_MarginTop = 30;

        public string Text
        {
            get;
            set;
        }

        public Size DrawArea
        {
            get;
            set;
        }

        public JimakuRenderer(Size drawArea)
        {
            DrawArea = drawArea;
            Text = "";
        }

        public Font Font { get { return Settings.Default.JimakuFont; } }

        public void Draw(Graphics g)
        {
            Debug.WriteLine("DrawJimaku: message = {0}", Text, "");

            // GraphicsPathオブジェクトの作成
            GraphicsPath graphicsPath = new GraphicsPath();

            var logicalLines = Text.Split(new char[] { '\n' });
            List<string> displayLines;
            displayLines = ToDisplayLines(g, logicalLines);

            Debug.WriteLine("Font.Unit={2}, font.Size={0}, font.SizeInPoints={1}", Font.Size, Font.SizeInPoints, Font.Unit);

            int y = m_MarginTop;
            foreach (var line in displayLines)
            {
                // GraphicsPathに文字列を追加する
                graphicsPath.AddString(line,
                    Font.FontFamily,                        // font family
                    (int) Font.Style,                       // style
                    Font.Size,                              // size
                    new Point(m_MarginLeft, y),             // origin
                    StringFormat.GenericDefault);
                y += (int) Math.Round(Font.Size);
            }

            // 文字列の縁を描画する
            var thickPen = new Pen(Color.Navy);
            thickPen.Width = Font.Size / 7;
            thickPen.LineJoin = LineJoin.Round;
            thickPen.EndCap = LineCap.Round;
            thickPen.StartCap = LineCap.Round;
            g.DrawPath(thickPen, graphicsPath);
            thickPen.Dispose();

            // 文字列の中を塗りつぶす
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var brush = new SolidBrush(Settings.Default.JimakuColor);
            g.FillPath(brush, graphicsPath);
            brush.Dispose();
        }

        /// <summary>
        /// 論理行を折り返して表示行にする。
        /// </summary>
        /// <returns>表示行を表す string の List</returns>
        private List<string> ToDisplayLines(Graphics g, string[] logicalLines)
        {
            var displayLines = new List<string>();
            string currentLine;
            for (var i = 0; i < logicalLines.Length; i++)
            {
                currentLine = logicalLines[i];

            repeat:
                string nextLine = "";
                while (IsTooWide(g, currentLine)) // MarginLeft が使えない・・・
                {
                    var lastChar = new string(currentLine.Last(), 1);
                    currentLine = currentLine.Substring(0, currentLine.Length - 1);
                    nextLine = lastChar + nextLine;
                }
                displayLines.Add(currentLine);
                if (nextLine != "")
                {
                    currentLine = nextLine;
                    goto repeat;
                }
            }
            return displayLines;
        }

        bool IsTooWide(Graphics g, string line)
        {
            int displayWidthInPixel = this.DrawArea.Width;
            GraphicsUnit defaultUnit = g.PageUnit;
            g.PageUnit = GraphicsUnit.Point;

            var sizeInPoints = g.MeasureString(line, Font);
            var requiredWidthInPoints = sizeInPoints.Width;
            float displayWidthInPoints = displayWidthInPixel / g.DpiX * 72;

            return (requiredWidthInPoints > displayWidthInPoints);
        }
    }
}
