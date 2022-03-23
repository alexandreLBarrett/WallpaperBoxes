using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace WallpaperHighlight
{
    

    public class WallpaperWrapper
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SystemParametersInfo(int uiAction, int uiParam, StringBuilder pvParam, int fWinIni);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int GetLastError();

        private const string HIGHLIGHT_BOX_DATA = "boxes.json";
        private string wallpaperGridFilename = "";

        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;
        private const int SPI_GETDESKWALLPAPER = 0x73;
        private const int WALLPAPER_PATH_LENGTH = 300;
        public bool GridActive { get; set; } = false;

        public IEnumerable<Screen> Screens { get; set; } = new List<Screen>();

        public string WallpaperPath { get; private set; }
        public string WallpaperPathOriginal { get; private set; }
        public string WallpaperPathOriginalBmp { get; private set; }
        public List<HighlightBox> HighlightBoxes { get; set; } = null;

        private string currentWallpaperPath;

        private void GenerateGrid()
        {
            int height = (int)SystemParameters.VirtualScreenHeight;
            int width = (int)SystemParameters.VirtualScreenWidth;

            string filename = "";
            foreach (Screen s in Screens)
            {
                filename += $"{s.Name}:{s.OffsetX},{s.OffsetY},{s.Width},{s.Height}";

                if (s != Screens.Last())
                    filename += '|';
            }

            string filenameHash = Directory.GetCurrentDirectory() + "\\" + HashString(filename) + ".png";

            if (!File.Exists(filenameHash))
            {
                // If doesnt exist generate the map
                Bitmap bmpOut = new Bitmap(width, height);

                using (Graphics g = Graphics.FromImage(bmpOut))
                {
                    Pen borderPen = new Pen(Color.Gray);
                    Brush backgroundBrush = new SolidBrush(Color.DarkBlue);
                    Brush idxBrush = new SolidBrush(Color.White);

                    int offset = 0;
                    foreach (Screen screen in Screens)
                    {
                        int gridWidth = (int)Math.Ceiling(screen.Width / 75f);
                        int gridHeight = (int)Math.Ceiling(screen.Height / 100f);

                        g.FillRectangle(backgroundBrush, offset, screen.OffsetY, screen.Width, screen.Height);

                        for (int x = 0; x <= gridWidth; x++)
                        {
                            for (int y = 0; y <= gridHeight; y++)
                            {
                                int xCoord = offset + x * 75;
                                int yCoord = screen.OffsetY + y * 100;

                                g.DrawRectangle(borderPen, xCoord, yCoord, 75, 100);

                                Font font1 = new Font("Arial", 15);

                                g.DrawString($"{x},{y}", font1, idxBrush, xCoord, yCoord);
                            }
                        }

                        offset += screen.Width;
                    }
                }

                bmpOut.Save(filenameHash);
            }

            wallpaperGridFilename = filenameHash;
        }

        public static string HashString(string str)
        {
            using (SHA256 mySHA256 = SHA256.Create())
            {
                MemoryStream memStream = new MemoryStream();
                StreamWriter s = new StreamWriter(memStream);
                s.Write(str);
                s.Flush();
                memStream.Position = 0;
                byte[] hash = mySHA256.ComputeHash(memStream);

                string stringHash = "";

                foreach (byte b in hash)
                {
                    stringHash += b.ToString("X2");
                }

                return stringHash;
            }
        }

        public void SetGrid()
        {
            if (!GridActive && wallpaperGridFilename != "" && File.Exists(wallpaperGridFilename))
            {
                SetWallpaper(wallpaperGridFilename, false);
                GridActive = true;
            }
        }

        public void UnSetGrid()
        {
            if (GridActive)
            {
                SetWallpaper(WallpaperPath, true);
                GridActive = false;
            } 
        }

        public WallpaperWrapper() 
        {
            currentWallpaperPath = GetCurrentWallpaperPath();
            HighlightBoxes = LoadHighlightBoxes();
            Screens = LoadScreenInfo();

            SaveWallpaperToLocal();
            GenerateGrid();
        }

        private IEnumerable<Screen> LoadScreenInfo()
        {
            List<Screen> screens = new List<Screen>();

            NativeMethods.Monitorenumproc monitorenumproc = NativeMethods.IterateScreenSizes;

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, monitorenumproc, ObjectHandleExtensions.ToIntPtr(screens));
            NativeMethods.DISPLAY_DEVICE dev = new NativeMethods.DISPLAY_DEVICE(0);
            int devNum = 0;
            while (NativeMethods.EnumDisplayDevices(IntPtr.Zero, devNum, ref dev, 0))
            {
                NativeMethods.EnumDisplayDevices(dev.DeviceName, 0, ref dev, 0);

                if (dev.DeviceString.Length != 0)
                {
                    // The name is valid
                    screens[devNum].Name = dev.DeviceString;
                }
                devNum++;
            }

            return screens.OrderBy(x => x.OffsetX);
        }

        public List<HighlightBox> LoadHighlightBoxes()
        {
            // Check if file exists first
            if (!File.Exists(HIGHLIGHT_BOX_DATA))
                return new List<HighlightBox>();

            string jsonData = File.ReadAllText(HIGHLIGHT_BOX_DATA);

            return JsonConvert.DeserializeObject<List<HighlightBox>>(jsonData);
        }

        public void SaveHighlightBoxes()
        {
            string jsonBoxes = JsonConvert.SerializeObject(HighlightBoxes);

            File.WriteAllText(HIGHLIGHT_BOX_DATA, jsonBoxes);
        }

        public void AddHightlightBox(HighlightBox box)
        {
            HighlightBoxes.Add(box);
        }

        public void ConvertToBitmap()
        {
            using (Image img = Image.FromFile(WallpaperPathOriginal))
            {
                int height = (int)SystemParameters.VirtualScreenHeight;
                int width = (int)SystemParameters.VirtualScreenWidth;

                using (Bitmap bmp = new Bitmap(width, height))
                {
                    var offset = 0;
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        foreach (var screen in Screens)
                        {
                            var destRect = new Rectangle(offset, screen.OffsetY, screen.Width, screen.Height);
                            g.CompositingMode = CompositingMode.SourceCopy;
                            g.CompositingQuality = CompositingQuality.HighQuality;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                            using (var wrapMode = new ImageAttributes())
                            {
                                wrapMode.SetWrapMode(WrapMode.Tile);
                                g.DrawImage(img, destRect, 0, 0, screen.Width, screen.Height, GraphicsUnit.Pixel, wrapMode);
                            }

                            offset += screen.Width;
                        }
                    }

                    bmp.Save(WallpaperPathOriginalBmp);
                }
            }
        }


        public void PaintBoxes(Bitmap img)
        {
            using (Graphics g = Graphics.FromImage(img))
            {
                foreach (HighlightBox hb in HighlightBoxes)
                {
                    Screen s = Screens.First(x => x.Name == hb.Screen);
                    int offset = 0;
                    foreach (Screen screen in Screens) {
                        if (s == screen) break;
                        offset += screen.Width;
                    }

                    Pen borderPen = new Pen(hb.BorderColor, 5);
                    Pen borderHighlightPen = new Pen(hb.HighlightColor, 7);
                    Brush textBrush = new SolidBrush(hb.TextColor);
                    Brush labelBrush = new SolidBrush(hb.BorderColor);

                    Color backgroundCOlor = Color.FromArgb(60, hb.BorderColor.R, hb.BorderColor.G, hb.BorderColor.B);
                    Brush backgroundBrush = new SolidBrush(backgroundCOlor);

                    // Draw surrounding box
                    g.DrawRectangle(borderHighlightPen,
                        offset + hb.X * 75,
                        s.OffsetY + hb.Y * 100,
                        hb.Width * 75,
                        hb.Height * 100
                    );
                    g.DrawRectangle(borderPen,
                        offset + hb.X * 75,
                        s.OffsetY + hb.Y * 100,
                        hb.Width * 75,
                        hb.Height * 100
                    );

                    g.FillRectangle(backgroundBrush,
                        offset + hb.X * 75,
                        s.OffsetY + hb.Y * 100,
                        hb.Width * 75,
                        hb.Height * 100
                    );

                    // -- Label settings --

                    Font font1 = new Font("Arial", 15);

                    // Setup text size
                    var catNameSize = g.MeasureString(hb.Name, font1);

                    // Draw border
                    g.DrawRectangle(borderHighlightPen,
                        offset + hb.X * 75,
                        s.OffsetY + hb.Y * 100 - catNameSize.Height,
                        catNameSize.Width,
                        catNameSize.Height
                    );
                    g.DrawRectangle(borderPen,
                        offset + hb.X * 75,
                        s.OffsetY + hb.Y * 100 - catNameSize.Height,
                        catNameSize.Width,
                        catNameSize.Height
                    );

                    // Draw filling
                    g.FillRectangle(labelBrush,
                        offset + hb.X * 75,
                        s.OffsetY + hb.Y * 100 - catNameSize.Height,
                        catNameSize.Width,
                        catNameSize.Height
                    );

                    // Draw string in label
                    g.DrawString(hb.Name, font1, textBrush,
                        offset + hb.X * 75,
                        s.OffsetY + hb.Y * 100 - catNameSize.Height
                    );
                }
            }
        }

        public void SaveWallpaperToLocal()
        {
            string filename = Directory.GetCurrentDirectory() + "\\" + currentWallpaperPath.Split('\\').Last();
            WallpaperPath = filename;
            WallpaperPathOriginal = EditFilename(filename, "_original");

            var lastDot = filename.LastIndexOf(".");
            WallpaperPathOriginalBmp = filename.Substring(0, lastDot) + ".bmp";

            // Check if file is in this directory
            if (!File.Exists(WallpaperPathOriginal))
            {
                // If not take existing and copy to here
                File.Copy(currentWallpaperPath, WallpaperPathOriginal);
            }

            if (!File.Exists(WallpaperPathOriginalBmp))
            {
                ConvertToBitmap();
            }
        }

        public string GetCurrentWallpaperPath()
        {
            StringBuilder s = new StringBuilder(WALLPAPER_PATH_LENGTH);
            if (!SystemParametersInfo(SPI_GETDESKWALLPAPER, WALLPAPER_PATH_LENGTH, s, 0))
            {
                Console.Error.WriteLine("Error while retrieving wallpaper " + GetLastError());
                return null;
            }
            return s.ToString();
        }

        public bool SetWallpaper(string imgPath, bool perm)
        {
            StringBuilder sb = new StringBuilder(imgPath);
            if (!SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, sb, perm ? SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE : 0))
            {
                Console.Error.WriteLine("Error while settting wallpaper " + GetLastError());
                return false;
            }
            return true;
        }
        private string EditFilename(string filename, string extension) => filename.Insert(filename.Length - 4, extension);
    }
}
 