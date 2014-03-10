using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
using JasonLong.Helper;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace TrainAssistant
{
    /// <summary>
    /// CodeOCRDemo.xaml 的交互逻辑
    /// </summary>
    public partial class CodeOCRDemo : Window
    {
        public CodeOCRDemo()
        {
            InitializeComponent();
        }

        HttpHelper hhelper = new HttpHelper();
        byte[] msbuffer = new byte[4096];
        StringBuilder codeBuilder = new StringBuilder(8, 8);
        string login_code = string.Empty;//登录验证码文字
        BitmapImage login_codeImg = new BitmapImage();//登录验证码图片

        private byte[] GetImage(Stream stream)
        {
            byte[] result = null;
            int count = 0;
            int offset = 0;
            do
            {
                count = stream.Read(msbuffer, offset, msbuffer.Length - offset);
                if (count > 0)
                {
                    offset += count;
                }
            } while (count > 0);
            if (offset > 0)
            {
                result = new byte[offset];
                Array.Copy(msbuffer, result, offset);
            }
            return result;
        }

        private void GetLoginCodeAsync()
        {
            Task.Run(() =>
            {
                Thread.Sleep(500);
                var url = ConfigurationManager.AppSettings["LoginValidateCodeImageUrl"].ToString() + "&rand=sjrand";
                var data = hhelper.GetResponseData(url);
                byte[] buffer;
                //using (MemoryStream ms = new MemoryStream(data, false))
                //{
                    
                //}

                int count = 0;
                do
                {
                    //buffer = GetImage(ms);
                    if (data != null)
                    {
                        codeBuilder.Length = 0;
                        if (BasicOCR.GetCodeFromBuffer(1, data, data.Length, codeBuilder))
                        {
                            login_code = codeBuilder.ToString();
                        }
                        count++;
                    }
                } while (login_code.Length != 4 && count < 4);
                var aa = login_code;

            });
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.FileName = "请选择图片";
            open.Filter = "图片格式(*.png,*jpg,*.bmp)|*.png;*.jpg;*.bmp";
            if ((bool)open.ShowDialog())
            {
                txtCodeImg.Text = open.FileName;
                imgCode.Source = new BitmapImage(new Uri(open.FileName, UriKind.RelativeOrAbsolute));
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            GetLoginCodeAsync();

           
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            byte[] buffter = TrainAssistant.Properties.Resources.data;
            if (!BasicOCR.LoadLibFromBuffer(buffter, buffter.Length, "123"))
            {
                MessageBox.Show("API初始化失败！");
            }

            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("1", "1");
            dic.Add("2", "2");
            cmbBind.ItemsSource = dic;
            cmbBind.DisplayMemberPath = "Key";
            cmbBind.SelectedValuePath = "Value";
        }


    }
}
