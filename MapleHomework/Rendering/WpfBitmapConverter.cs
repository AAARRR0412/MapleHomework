using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapleHomework.Rendering
{
    /// <summary>
    /// System.Drawing.Bitmap을 WPF ImageSource로 변환하는 유틸리티
    /// </summary>
    public static class WpfBitmapConverter
    {
        /// <summary>
        /// Bitmap을 BitmapSource로 변환
        /// </summary>
        public static BitmapSource? ToBitmapSource(Bitmap? bitmap)
        {
            if (bitmap == null) return null;

            try
            {
                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Bitmap을 ImageSource로 변환
        /// </summary>
        public static ImageSource? ToImageSource(Bitmap? bitmap)
        {
            return ToBitmapSource(bitmap);
        }
    }
}
