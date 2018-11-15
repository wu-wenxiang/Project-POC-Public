using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using UFileClient.Common;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Drawing;

namespace ImageScan.GdiPlusLib
{

    public static class Gdip
    {
        static Gdip()
        {
 
        }
        private static IntPtr gdipToken = IntPtr.Zero;                // 用于初始化GDI+
        /// <summary>
        /// 通过文件名后缀获取图像文件个是的GUID
        /// 获取成功返回true，GUID通过clsid返
        /// 获取失败返回false，clsid维持为Guid.Empty
        /// </summary>
        /// <param name="fileFormat">文件格式</param>
        /// <param name="clsid">clsid值</param>
        /// <returns>操作是否成功</returns>
        private static bool GetCodecClsid(ref string fileFormat, out Guid clsid)
        {
            String strGuid = null;

            if (fileFormat.Equals("jpg") || fileFormat.Equals("jpeg"))  // 获取对应的encoder
            {
                strGuid = "557cf401-1a04-11d3-9a73-0000f81ef32e";      // 保存为JPEG文件的标志
            }
            else if (fileFormat.Equals("tiff") || fileFormat.Equals("tif"))
            {
                strGuid = "557cf405-1a04-11d3-9a73-0000f81ef32e";      // 保存为TIFF文件
            }
            else if (fileFormat.Equals("bmp"))
            {
                strGuid = "557cf400-1a04-11d3-9a73-0000f81ef32e";      // 保存为bmp文件
            }
            else if (fileFormat.Equals("png"))
            {
                strGuid = "557cf406-1a04-11d3-9a73-0000f81ef32e";      // 保存为png文件
            }
            else if (fileFormat.Equals("gif"))
            {
                strGuid = "557cf402-1a04-11d3-9a73-0000f81ef32e";      // 保存为gif文件
            }



            if (strGuid != null)
            {
                clsid = new Guid(strGuid);
                return true;
            }
            else
            {
                clsid = Guid.Empty;
                return false;
            }
        }

        /// <summary>
        /// 初始化GDI+
        /// </summary>
        public static void InitGDIPlus()
        {
            if (gdipToken == IntPtr.Zero)   // 初始化GDI+
            {
                StartupInput input = StartupInput.GetDefaultStartupInput();
                StartupOutput output;
                int status = GdiplusStartup(out   gdipToken, ref   input, out   output);
            }
        }
        /// <summary>
        /// 清除GDI+
        /// </summary>
        public static void ClearGDIPlus()
        {
            if (gdipToken != IntPtr.Zero)       // 释放GDI+
            {
                GdiplusShutdown(gdipToken);
                gdipToken = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 将文件按照指定的格式保存到文件夹中
        /// </summary>
        /// <param name="filePtr">文件句柄</param>
        /// <param name="fileFormat">文件类型，后缀名</param>
        /// <returns>文件全路径名</returns>
        public static string SaveSingleImageToFile(IntPtr filePtr, ref String fileFormat, bool isGroup4, int resolution)
        {

            String fileHeader = Config.GetImageDir();   // 文件路径
            Guid clsid;                                          // 保存图像类型的GUID

            if (!GetCodecClsid(ref fileFormat, out clsid))
            {
                MessageBox.Show("Unknown picture format for extension " + Path.GetExtension(fileFormat),
                                "Image Codec");
                return null;
            }

            IntPtr dibhand = (IntPtr)filePtr;
            IntPtr bmpptr = GlobalLock(dibhand);
            IntPtr pixptr = GetPixelInfo(bmpptr);

            IntPtr imgData = IntPtr.Zero;
            int st = GdipCreateBitmapFromGdiDib(bmpptr, pixptr, ref imgData);
            if ((st != 0) || (imgData == IntPtr.Zero))
                return null;

            String fileName = fileHeader + FileNameUtility.GetOnlyOneFileName(fileFormat);
            st = GdipBitmapSetResolution(imgData, resolution, resolution);
            st = GdipSaveImageToFile(imgData, fileName, ref clsid, IntPtr.Zero);    // 将图像保存为JPEG文件

            GdipDisposeImage(imgData);  // 
            GlobalFree(dibhand);    // 释放从扫描源产生的图像内存块
            dibhand = IntPtr.Zero;

            if (isGroup4)
            {
                System.Drawing.Bitmap newImage = new Bitmap(fileName);
                //newImage.SetResolution(resolution, resolution);
                EncoderParameters encParams = new EncoderParameters();
                Encoder encCompress;
                encCompress = new Encoder(Encoder.Compression.Guid);
                EncoderParameter encParamCompress = new EncoderParameter(encCompress, (long)GetBestTIFFCompression(newImage.PixelFormat));
                encParams.Param[0] = encParamCompress;
                string tmpFileName = fileName;
                fileName = fileHeader + FileNameUtility.GetOnlyOneFileName(fileFormat);
                newImage.Save(fileName, GetCodecInfo(ImageFormat.Tiff), encParams);
                newImage.Dispose();
                File.Delete(tmpFileName);
                GC.Collect();
            }
            else
            {
                System.Drawing.Bitmap newImage = new Bitmap(fileName);
                //newImage.SetResolution(resolution, resolution);
                string tmpFileName = fileName;
                fileName = fileHeader + FileNameUtility.GetOnlyOneFileName(fileFormat);
                newImage.Save(fileName);
                newImage.Dispose();
                File.Delete(tmpFileName);
                GC.Collect();
            }
            return fileName;
        }

        private static EncoderValue GetBestTIFFCompression(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Format1bppIndexed: // Black & White
                    return EncoderValue.CompressionCCITT4;

                default: // Color
                    return EncoderValue.CompressionNone;
            }
        }

        public static ImageCodecInfo GetCodecInfo(ImageFormat format)
        {

            Guid clsid;

            clsid = format.Guid;
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (clsid.Equals(codec.FormatID)) return codec;
            }
            throw new Exception("Error Getting Codec Information");
        }
        /// <summary>
        /// 获取图像的像素位置信息
        /// </summary>
        /// <param name="bmpptr">图像元素指针</param>
        /// <returns>图像元素位置信息指针</returns>
        private static IntPtr GetPixelInfo(IntPtr bmpptr)
        {
            BitmapInfoHeader bmi = new BitmapInfoHeader();
            Marshal.PtrToStructure(bmpptr, bmi);

            if (bmi.biSizeImage == 0)
                bmi.biSizeImage = ((((bmi.biWidth * bmi.biBitCount) + 31) & ~31) >> 3) * bmi.biHeight;

            int p = bmi.biClrUsed;
            if ((p == 0) && (bmi.biBitCount <= 8))
                p = 1 << bmi.biBitCount;
            p = (p * 4) + bmi.biSize + (int)bmpptr;
            return (IntPtr)p;
        }

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GlobalLock(IntPtr handle);
        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GlobalFree(IntPtr handle);

        [DllImport("gdiplus.dll", ExactSpelling = true)]
        internal static extern int GdipCreateBitmapFromGdiDib(IntPtr bminfo, IntPtr pixdat, ref IntPtr image);

        [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern int GdipSaveImageToFile(IntPtr image, string filename, [In] ref Guid clsid, IntPtr encparams);

        [DllImport("gdiplus.dll", ExactSpelling = true)]
        internal static extern int GdipDisposeImage(IntPtr image);

        [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern int GdiplusStartup(out   IntPtr token, ref   StartupInput input, out   StartupOutput output);

        [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern int GdiplusShutdown(IntPtr token);

        [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern int GdipBitmapSetResolution(IntPtr image, float xResolution, float yResolution);

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct StartupInput
    {
        public int GdiplusVersion;
        public IntPtr DebugEventCallback;
        public bool SuppressBackgroundThread;
        public bool SuppressExternalCodecs;

        public static StartupInput GetDefaultStartupInput()
        {
            StartupInput result = new StartupInput();
            result.GdiplusVersion = 1;
            result.SuppressBackgroundThread = false;
            result.SuppressExternalCodecs = false;
            return result;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct StartupOutput
    {
        public IntPtr Hook;
        public IntPtr Unhook;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class BitmapInfoHeader
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
    }

}
