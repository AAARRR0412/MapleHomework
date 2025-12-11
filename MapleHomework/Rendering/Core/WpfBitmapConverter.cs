using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapleHomework.Rendering.Core
{
    /// <summary>
    /// GDI+ Bitmap을 WPF ImageSource로 변환하는 유틸리티
    /// </summary>
    public static class WpfBitmapConverter
    {
        /// <summary>
        /// System.Drawing.Bitmap을 WPF BitmapSource로 변환
        /// </summary>
        /// <param name="bitmap">변환할 GDI+ 비트맵</param>
        /// <returns>WPF BitmapSource</returns>
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
        /// <param name="bitmap">변환할 GDI+ 비트맵</param>
        /// <returns>WPF ImageSource</returns>
        public static ImageSource? ToImageSource(Bitmap? bitmap)
        {
            return ToBitmapSource(bitmap);
        }

        /// <summary>
        /// System.Drawing.Image를 WPF ImageSource로 변환
        /// </summary>
        /// <param name="image">변환할 GDI+ 이미지</param>
        /// <returns>WPF ImageSource</returns>
        public static ImageSource? ToImageSource(Image? image)
        {
            if (image == null)
                return null;

            if (image is Bitmap bitmap)
            {
                return ToBitmapSource(bitmap);
            }

            // Image를 Bitmap으로 변환 후 처리
            using (var bmp = new Bitmap(image))
            {
                return ToBitmapSource(bmp);
            }
        }

        /// <summary>
        /// 바이트 배열을 WPF BitmapSource로 변환
        /// </summary>
        /// <param name="imageData">PNG/JPEG 등의 이미지 바이트 배열</param>
        /// <returns>WPF BitmapSource</returns>
        public static BitmapSource? FromBytes(byte[]? imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            using (var memory = new MemoryStream(imageData))
            {
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
        /// URL에서 이미지 로드 (비동기 지원)
        /// </summary>
        /// <param name="url">이미지 URL</param>
        /// <returns>WPF BitmapImage</returns>
        public static BitmapImage? FromUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(url, UriKind.Absolute);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
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
        /// WPF BitmapSource를 System.Drawing.Bitmap으로 변환
        /// </summary>
        /// <param name="source">WPF BitmapSource</param>
        /// <returns>GDI+ Bitmap</returns>
        public static Bitmap? ToBitmap(BitmapSource? source)
        {
            if (source == null)
                return null;

            using (var memory = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(memory);

                memory.Position = 0;
                return new Bitmap(memory);
            }
        }

        /// <summary>
        /// BitmapSource를 PNG 바이트 배열로 변환
        /// </summary>
        /// <param name="source">WPF BitmapSource</param>
        /// <returns>PNG 바이트 배열</returns>
        public static byte[]? ToBytes(BitmapSource? source)
        {
            if (source == null)
                return null;

            using (var memory = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(memory);

                return memory.ToArray();
            }
        }
    }
}

