using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO.Compression;

namespace JasonLong.Helper
{
    /// <summary>
    /// 验证码识别
    /// </summary>
    public static class BasicOCR
    {
        [DllImport("Sunday.dll", CharSet = CharSet.Ansi)]
        public static extern bool GetCodeFromBuffer(Int32 LibFileIndex, Byte[] FileBuffer, Int32 ImgBufLen, StringBuilder Code);

        [DllImport("Sunday.dll", CharSet = CharSet.Ansi)]
        public static extern bool LoadLibFromBuffer(Byte[] FileBuffer, Int32 BufLen, String Password);
    }
}

