using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WallpaperHighlight
{
    [Serializable]
    public struct HighlightBox
    {
        public HighlightBox(string name, int x, int y, int w, int h, string screen, Color textColor, Color highlightColor, Color borderColor)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            Name = name;
            TextColor = textColor;
            HighlightColor = highlightColor;
            BorderColor = borderColor;
            Screen = screen;

            TextBrush = new System.Windows.Media.SolidColorBrush(ColorUtils.ToMediaColor(textColor));
            HighlightBrush = new System.Windows.Media.SolidColorBrush(ColorUtils.ToMediaColor(highlightColor));
            BorderBrush = new System.Windows.Media.SolidColorBrush(ColorUtils.ToMediaColor(borderColor));
        }
        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Color TextColor { get; set; }
        public Color HighlightColor { get; set; }
        public Color BorderColor { get; set; }
        public string Screen { get; set; }

        public System.Windows.Media.Brush TextBrush { get; set; }
        public System.Windows.Media.Brush HighlightBrush { get; set; }
        public System.Windows.Media.Brush BorderBrush { get; set; }
    }
}
