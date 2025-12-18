using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MapleHomework.Rendering
{
    public static class ImageHelper
    {
        private static readonly ConcurrentDictionary<string, Bitmap> _bitmapCache = new ConcurrentDictionary<string, Bitmap>();
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// URL에서 이미지를 다운로드하여 System.Drawing.Bitmap으로 반환합니다. (캐싱 지원)
        /// </summary>
        public static async Task<Bitmap?> LoadBitmapFromUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            if (_bitmapCache.TryGetValue(url, out var actachedBitmap))
            {
                return actachedBitmap;
            }

            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
                using (var ms = new MemoryStream(bytes))
                {
                    // Copy to a new bitmap to avoid stream issues
                    var bitmap = new Bitmap(ms);
                    // Clone to detach from stream
                    var clonedBitmap = new Bitmap(bitmap);

                    _bitmapCache[url] = clonedBitmap;
                    return clonedBitmap;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load image from URL: {url}. Error: {ex.Message}");
                return null;
            }
        }
    }
}
