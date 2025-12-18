using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapleHomework.Rendering
{
    /// <summary>
    /// GDI+ Bitmap을 WPF ImageSource로 변환하는 유틸리티
    /// </summary>
    public static class WpfBitmapConverter
    {
        /// <summary>
        /// System.Drawing.Bitmap을 WPF BitmapSource로 변환
        /// </summary>
        public static BitmapSource? ToBitmapSource(Bitmap? bitmap)
        {
            if (bitmap == null)
                return null;

            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        /// <summary>
        /// System.Drawing.Bitmap을 WPF ImageSource로 변환
        /// </summary>
        public static ImageSource? ToImageSource(Bitmap? bitmap)
        {
            return ToBitmapSource(bitmap);
        }
    }
}
