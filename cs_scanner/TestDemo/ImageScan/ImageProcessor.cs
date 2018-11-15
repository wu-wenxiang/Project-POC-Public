using System;
using System.Runtime.InteropServices;

public static class ImageProcessor
{
    #region
    [DllImport("ImageProcessor.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr LoadImageFile(string filePath);

    [DllImport("ImageProcessor.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr LoadImagePtr(IntPtr pImage);

    [DllImport("ImageProcessor.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern bool IsBlank(IntPtr pImage);

    [DllImport("ImageProcessor.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr ReviseTwist(IntPtr pImage);

    [DllImport("ImageProcessor.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr RemoveBlackEdge(IntPtr pImage);

    [DllImport("ImageProcessor.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern bool SaveImage(IntPtr pImage, string filePath, bool isBW, bool isGroup4);

    [DllImport("ImageProcessor.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern void ReleaseImage(IntPtr pImage);

    #endregion

    [DllImport("ImageProcessor.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
    public static extern bool RemoveBlackEdge(string srcImgName, string dstImgName, int threshold);

    [DllImport("ImageProcessor.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
    public static extern bool ReviseTwist(string srcImgName, string dstImgName);

    [DllImport("ImageProcessor.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
    public static extern bool Rotate(string srcImgName, string dstImgName, int angle, bool originSize);

}