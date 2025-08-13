using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace DrJaw.Utils
{
    public static class ImageHelper
    {
        /// <summary>
        /// Преобразует BitmapImage (WPF) в System.Drawing.Image (для сохранения в БД)
        /// </summary>
        public static Image? BitmapImageToDrawingImage(BitmapImage? bitmapImage)
        {
            if (bitmapImage == null) return null;

            using MemoryStream ms = new();
            BitmapEncoder encoder = new PngBitmapEncoder(); // без потерь
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            encoder.Save(ms);
            ms.Position = 0;

            return Image.FromStream(ms);
        }

        /// <summary>
        /// Преобразует System.Drawing.Image в JPEG-байты с сжатием (по умолчанию 80% качества)
        /// Используется для уменьшения размера фотографий перед сохранением в БД
        /// </summary>
        public static byte[] ImageToBytes(Image image, long jpegQuality = 80L)
        {
            using MemoryStream ms = new();

            var jpegEncoder = ImageCodecInfo.GetImageEncoders()
                .First(c => c.FormatID == ImageFormat.Jpeg.Guid);

            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, jpegQuality);

            image.Save(ms, jpegEncoder, encoderParams);
            return ms.ToArray();
        }

        /// <summary>
        /// Преобразует массив байтов обратно в BitmapImage (например, из БД)
        /// </summary>
        public static BitmapImage? BytesToBitmapImage(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            using MemoryStream ms = new(bytes);
            BitmapImage bmp = new();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze(); // для многопоточности
            return bmp;
        }
    }
}
