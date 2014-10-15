using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Net.Security;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Net.Cache;

namespace JasonLong.Helper
{
    /// <summary>
    /// Http连接操作帮助类
    /// </summary>
    public class HttpHelper
    {

        public CookieContainer cookieContainer = new CookieContainer();

        /// <summary>
        /// 获取返回的数据
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public byte[] GetResponseData(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.KeepAlive = true;
            request.Method = "GET";
            request.ServicePoint.Expect100Continue = false;
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
            request.AllowAutoRedirect = true;
            request.CookieContainer = cookieContainer;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:32.0) Gecko/20100101 Firefox/32.0";
            request.Timeout = 30000;
            ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) =>
            {
                return true;
            };
            Encoding encoding = Encoding.GetEncoding("UTF-8");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] bytes = new byte[1024];
                while (true)
                {
                    int size = stream.Read(bytes, 0, 1024);
                    if (size == 0) break;
                    ms.Write(bytes, 0, size);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 获取验证码图片
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public BitmapImage GetResponseImage(string url)
        {
            Uri uri = new Uri(url);
            BitmapImage bmp = new BitmapImage();
            byte[] buffer = new byte[4096];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.KeepAlive = true;
            request.Method = "GET";
            request.ServicePoint.Expect100Continue = false;
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
            request.AllowAutoRedirect = true;
            request.CookieContainer = cookieContainer;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:32.0) Gecko/20100101 Firefox/32.0";
            request.Timeout = 30000;
            ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) =>
            {
                return true;
            };
            Encoding encoding = Encoding.GetEncoding("UTF-8");
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                using (MemoryStream ms = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, buffer.Length);
                        ms.Write(buffer, 0, count);
                    } while (count != 0);
                    bmp.BeginInit();
                    bmp.StreamSource = ms;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                }
            }
            catch (Exception)
            {

                throw;
            }
            return bmp;
        }

        /// <summary>
        /// 获取字符
        /// </summary>
        /// <returns></returns>
        public string GetResponseChartByGET(string url)
        {
            Uri uri = new Uri(url);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.KeepAlive = true;
            request.Method = "GET";
            request.ServicePoint.Expect100Continue = false;
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
            request.AllowAutoRedirect = true;
            request.CookieContainer = cookieContainer;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:32.0) Gecko/20100101 Firefox/32.0";
            request.Timeout = 30000;
            ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) =>
            {
                return true;
            };
            Encoding encoding = Encoding.GetEncoding("UTF-8");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.Cookies.Count > 0)
            {
                cookieContainer.Add(response.Cookies);
            }
            Stream stream = response.GetResponseStream();
            if (response.ContentEncoding.ToLower().Contains("gzip"))//是否压缩
            {
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }
            StreamReader reader = new StreamReader(stream, encoding);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// POST
        /// </summary>
        /// <param name="url"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public string GetResponseByPOST(string url, Dictionary<string, string> param)
        {
            Uri uri = new Uri(url);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.KeepAlive = true;
            request.Method = "POST";
            request.ServicePoint.Expect100Continue = false;
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
            request.AllowAutoRedirect = true;
            request.CookieContainer = cookieContainer;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:32.0) Gecko/20100101 Firefox/32.0";
            request.Timeout = 30000;
            ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) =>
            {
                return true;
            };
            Encoding encoding = Encoding.GetEncoding("UTF-8");
            if (param != null || param.Count > 0)
            {
                StringBuilder str = new StringBuilder();
                int i = 0;
                foreach (var p in param)
                {
                    if (i == 0)
                    {
                        str.AppendFormat("{0}={1}", p.Key, p.Value);
                    }
                    else
                    {
                        str.AppendFormat("&{0}={1}", p.Key, p.Value);
                    }
                    i++;
                }
                byte[] buffer_data = Encoding.UTF8.GetBytes(str.ToString());
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(buffer_data, 0, buffer_data.Length);
                }
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.Cookies.Count > 0)
            {
                cookieContainer.Add(response.Cookies);
            }
            Stream responseStream = response.GetResponseStream();
            if (response.ContentEncoding.ToLower().Contains("gzip"))//是否压缩
            {
                responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
            }
            StreamReader reader = new StreamReader(responseStream, encoding);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Unicode转中文
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string UnicodeToGBK(string str)
        {
            MatchCollection match = Regex.Matches(str,@"\\u([\\w{4}])");
            string text=str.Replace(@"\u","");
            char[] charStr = new char[match.Count];
            for (int i = 0; i < charStr.Length; i++)
            {
                charStr[i] = (char)Convert.ToInt32(text.Substring(i*4,4),16);
            }
            return new string(charStr);
        }

    }
}