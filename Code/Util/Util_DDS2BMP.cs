using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace L2_login
{
    public static partial class Util
    {
#pragma warning disable CS0414
        private static object GraphicCard = null;
        private static object GraphicSettings = null;
#pragma warning restore CS0414
        private static Panel GraphicPanel = new Panel();

        public static Bitmap Dds2BMP(byte[] Bytes)
        {
            try
            {
                if (Bytes.Length < 4)
                    return null;

                var magic = System.BitConverter.ToUInt32(Bytes, 0);
                if (magic == 0x20534444)
                {
                    return LoadDdsDirect(Bytes);
                }

                using (var ms = new MemoryStream(Bytes))
                {
                    return new Bitmap(ms);
                }
            }
            catch
            {
                return null;
            }
        }

        private static Bitmap LoadDdsDirect(byte[] data)
        {
            if (data.Length < 148)
                return null;

            uint header = System.BitConverter.ToUInt32(data, 0);
            if (header != 0x20534444)
                return null;

            int height = System.BitConverter.ToInt32(data, 12);
            int width = System.BitConverter.ToInt32(data, 16);
            uint format = System.BitConverter.ToUInt32(data, 84);

            uint pitchOrLinearSize = System.BitConverter.ToUInt32(data, 20);
            uint mipMapCount = System.BitConverter.ToUInt32(data, 28);
            uint caps = System.BitConverter.ToUInt32(data, 36);

            if ((format == 0) || (format == 0x31545844) || (format == 0x33545844) || (format == 0x35545844) || (format == 0x36315644))
            {
                return DecodeDdsCompressed(data, width, height, format);
            }

            int pitch = (pitchOrLinearSize > 0) ? (int)pitchOrLinearSize : width * 4;
            if (pitch == 0) pitch = width * 4;

            int dx = 128;
            int pixelFormat = (int)System.BitConverter.ToUInt32(data, dx);
            int RGBBitCount = (int)System.BitConverter.ToUInt32(data, dx + 4);
            uint RBitMask = System.BitConverter.ToUInt32(data, dx + 8);
            uint GBitMask = System.BitConverter.ToUInt32(data, dx + 12);
            uint BBitMask = System.BitConverter.ToUInt32(data, dx + 16);
            uint ABitMask = System.BitConverter.ToUInt32(data, dx + 20);

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);

            int srcOffset = 148;
            unsafe
            {
                byte* dst = (byte*)bmpData.Scan0;
                int dstStride = bmpData.Stride;

                for (int y = 0; y < height; y++)
                {
                    byte* row = dst + (y * dstStride);
                    for (int x = 0; x < width; x++)
                    {
                        int srcIdx = srcOffset + (y * pitch) + (x * 4);
                        if (srcIdx + 3 < data.Length)
                        {
                            int bIdx = srcIdx;
                            row[x * 4 + 0] = (byte)((data[bIdx + 0]));
                            row[x * 4 + 1] = (byte)((data[bIdx + 1]));
                            row[x * 4 + 2] = (byte)((data[bIdx + 2]));
                            row[x * 4 + 3] = (byte)((data[bIdx + 3]));
                        }
                    }
                }
            }
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        private static Bitmap DecodeDdsCompressed(byte[] data, int width, int height, uint format)
        {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);

            int srcOffset = 148;
            int blockSize = (format == 0x31545844) ? 8 : 16;
            int blocksX = (width + 3) / 4;
            int blocksY = (height + 3) / 4;

            unsafe
            {
                byte* dst = (byte*)bmpData.Scan0;
                int dstStride = bmpData.Stride;

                for (int by = 0; by < blocksY; by++)
                {
                    for (int bx = 0; bx < blocksX; bx++)
                    {
                        int srcIdx = srcOffset + (by * blocksX + bx) * blockSize;
                        if (srcIdx + blockSize > data.Length)
                            continue;

                        for (int py = 0; py < 4; py++)
                        {
                            int y = by * 4 + py;
                            if (y >= height)
                                continue;
                            for (int px = 0; px < 4; px++)
                            {
                                int x = bx * 4 + px;
                                if (x >= width)
                                    continue;

                                byte* pixel = dst + (y * dstStride) + (x * 4);
                                pixel[0] = 128;
                                pixel[1] = 128;
                                pixel[2] = 128;
                                pixel[3] = 255;
                            }
                        }
                    }
                }
            }
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        private class Device { }
        private class PresentParameters { }
    }
}