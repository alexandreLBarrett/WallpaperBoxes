using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using System.Windows.Interop;
using static WallpaperHighlight.NativeMethods;

namespace WallpaperHighlight
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WallpaperWrapper wppw;
        public MainWindow()
        {
            InitializeComponent();
            wppw = new WallpaperWrapper();
            BoxesList.ItemsSource = wppw.HighlightBoxes;
            ScreenBox.ItemsSource = wppw.Screens;

            this.Dispatcher.ShutdownStarted += UnloadWindow;
        }

        public void SetWallpaper(object sender, RoutedEventArgs e)
        {
            // Note: icons are 75 x 100 at 1080p
            Bitmap img = new Bitmap(wppw.WallpaperPathOriginalBmp);

            wppw.PaintBoxes(img);

            wppw.SaveHighlightBoxes();

            // Save modified image
            img.Save(wppw.WallpaperPath);

            // Close image file
            img.Dispose();

            // Update wallpaper
            if (!wppw.SetWallpaper(wppw.WallpaperPath, true))
            {
                MessageBox.Show("ERROR: Could not set the used wallpaper");
            }
        }

        private void AddOrUpdateBox(object sender, RoutedEventArgs e)
        {
            if (BoxesList.SelectedItem != null) // MODIFY
            {
                HighlightBox toMod = (HighlightBox)BoxesList.SelectedItem;
                wppw.HighlightBoxes.Remove(toMod);
                toMod.Name = CatNameTxt.Text.Trim();
                toMod.X = int.Parse(BoxX.Text);
                toMod.Y = int.Parse(BoxY.Text);
                toMod.Width = int.Parse(BoxWidth.Text);
                toMod.Height = int.Parse(BoxHeight.Text);
                toMod.Screen = ((Screen)ScreenBox.SelectedItem).Name;
                toMod.TextColor = ColorUtils.FromMediaColor(TextColorPicker.SelectedColor.Value);
                toMod.TextBrush = new System.Windows.Media.SolidColorBrush(TextColorPicker.SelectedColor.Value);
                toMod.HighlightColor = ColorUtils.FromMediaColor(HighlightColorPicker.SelectedColor.Value);
                toMod.HighlightBrush = new System.Windows.Media.SolidColorBrush(HighlightColorPicker.SelectedColor.Value);
                toMod.BorderColor = ColorUtils.FromMediaColor(BorderColorPicker.SelectedColor.Value);
                toMod.BorderBrush = new System.Windows.Media.SolidColorBrush(BorderColorPicker.SelectedColor.Value);
                wppw.HighlightBoxes.Add(toMod);
            }
            else // ADD
            {
                var newBox = new HighlightBox(
                       CatNameTxt.Text.Trim(),
                       int.Parse(BoxX.Text),
                       int.Parse(BoxY.Text),
                       int.Parse(BoxWidth.Text),
                       int.Parse(BoxHeight.Text),
                       ((Screen)ScreenBox.SelectedItem).Name,
                       ColorUtils.FromMediaColor(TextColorPicker.SelectedColor.Value),
                       ColorUtils.FromMediaColor(HighlightColorPicker.SelectedColor.Value),
                       ColorUtils.FromMediaColor(BorderColorPicker.SelectedColor.Value)
                   );

                if (!wppw.HighlightBoxes.Exists(x => x.Name == newBox.Name))
                    wppw.AddHightlightBox(newBox);
            }
            BoxesList.Items.Refresh();
        }

        private void UndoWallpaper(object sender, RoutedEventArgs e)
        {
            File.Copy(wppw.WallpaperPathOriginal, wppw.WallpaperPath, true);

            if (!wppw.SetWallpaper(wppw.WallpaperPath, true))
            {
                MessageBox.Show("ERROR: Could not set the wallpaper");
            }
        }

        private void ResetHighlights(object sender, RoutedEventArgs e)
        {
            wppw.HighlightBoxes.Clear();
            wppw.SaveHighlightBoxes();
            BoxesList.Items.Refresh();
        }

        private void BoxesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (BoxesList.Items.Count != 0)
            {
                if (BoxesList.SelectedItem != null)
                {
                    HighlightBox toMod = (HighlightBox)BoxesList.SelectedItem;

                    CatNameTxt.Text = toMod.Name;
                    BoxX.Text = toMod.X.ToString();
                    BoxY.Text = toMod.Y.ToString();
                    BoxWidth.Text = toMod.Width.ToString();
                    BoxHeight.Text = toMod.Height.ToString();
                    ScreenBox.SelectedItem = wppw.Screens.First(x => x.Name == toMod.Screen);
                    TextColorPicker.SelectedColor = ColorUtils.ToMediaColor(toMod.TextColor);
                    HighlightColorPicker.SelectedColor = ColorUtils.ToMediaColor(toMod.HighlightColor);
                    BorderColorPicker.SelectedColor = ColorUtils.ToMediaColor(toMod.BorderColor);
                    AddButton.Content = "Modify highlight";
                } 
                else
                {
                    AddButton.Content = "Add Highlight";
                }
            }
        }

        private void DeleteHighlight(object sender, RoutedEventArgs e)
        {
            if (BoxesList.SelectedItem != null)
            {
                HighlightBox toMod = (HighlightBox)BoxesList.SelectedItem;
                wppw.HighlightBoxes.Remove(toMod);
                BoxesList.Items.Refresh();
            }
        }

        private void ToggleGrid(object sender, RoutedEventArgs e)
        {
            if (wppw.GridActive)
                wppw.UnSetGrid();
            else
                wppw.SetGrid();
        }

        private void UnloadWindow(object sender, EventArgs e)
        {
            wppw.UnSetGrid();
        }
    }
}
