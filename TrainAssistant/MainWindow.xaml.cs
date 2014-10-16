using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using JasonLong.Helper;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using TrainAssistant.Models;
using System.Web;
using System.Windows.Controls.Primitives;

namespace TrainAssistant
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        HttpHelper hhelper = new HttpHelper();
        DataSecurity dsecurity = new DataSecurity();
        StationName sname = new StationName();

        StringBuilder codeBuilder = new StringBuilder(8, 8);
        byte[] msbuffer = new byte[4096];

        string login_code = string.Empty;//登录验证码文字
        string order_code = string.Empty;//订单验证码
        private string file = "Account";//登录用户信息
        string seatTypes = "";//席别

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 获取登录验证码图片并识别
        /// </summary>
        private Task<BitmapImage> GetLoginCodeAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                var url = ConfigurationManager.AppSettings["LoginValidateCodeImageUrl"].ToString() + "&rand=sjrand";
                var data = hhelper.GetResponseData(url);
                int count = 0;
                BitmapImage login_codeImg = new BitmapImage();//登录验证码图片
                if (data != null)
                {
                    do
                    {
                        codeBuilder.Length = 0;
                        if (BasicOCR.GetCodeFromBuffer(1, data, data.Length, codeBuilder))
                        {
                            login_code = codeBuilder.ToString();
                        }
                        count++;
                    } while (login_code.Length != 4 && count < 10);
                    using (MemoryStream ms = new MemoryStream(data, false))
                    {
                        login_codeImg.BeginInit();
                        login_codeImg.StreamSource = ms;
                        login_codeImg.CacheOption = BitmapCacheOption.OnLoad;
                        login_codeImg.EndInit();
                        login_codeImg.Freeze();
                    }
                }
                return login_codeImg;
            });
        }

        /// <summary>
        /// 判断登录验证码是否输入正确
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private Task<string> ValidateLoginCode(string code)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                string url = ConfigurationManager.AppSettings["LoginCodeValidateUrl"].ToString(), result = "";
                Dictionary<string, string> param = new Dictionary<string, string>();
                param.Add("randCode", code);
                param.Add("rand", "sjrand");
                string valiresult = hhelper.GetResponseByPOST(url, param);
                if (valiresult != "")
                {
                    JObject json = JObject.Parse(valiresult);
                    result = json["data"].ToString();
                    if (result == "N")
                    {
                        result = "验证码错误";
                    }
                    else
                    {
                        result = "";
                    }
                }
                return result;
            });
        }

        /// <summary>
        /// 登录
        /// </summary>
        private Task<string> Login(string userName, string password, string code, bool isRemeberMe, bool isAutoLogin)
        {
            return Task.Factory.StartNew(() =>
            {
                string result = "", url = ConfigurationManager.AppSettings["LoginUrl"].ToString();
                Dictionary<string, string> param = new Dictionary<string, string>();
                param.Add("loginUserDTO.user_name", userName);//用户名
                param.Add("userDTO.password", password);//密码
                param.Add("randCode", code);//验证码
                Thread.Sleep(100);
                result = hhelper.GetResponseByPOST(url, param);
                if (result != "")
                {
                    JObject json = JObject.Parse(result);
                    string errormsg = json["messages"].ToString();
                    if (errormsg != "[]")
                    {
                        var msg = Regex.Match(errormsg, "[\r\n\"(?<msg>[^\"]+)\"\r\n]", RegexOptions.Singleline, TimeSpan.FromSeconds(10));
                        if (msg.Success)
                        {
                            result = msg.Groups["msg"].Value;
                        }
                    }
                    else
                    {
                        string isloginsucess = json["data"]["loginCheck"].ToString();
                        if (isloginsucess == "Y")
                        {
                            url = ConfigurationManager.AppSettings["GetLoginUserNameUrl"].ToString();
                            result = hhelper.GetResponseChartByGET(url);
                            if (isRemeberMe)
                            {
                                List<Users> users = ReadUser(file);
                                var u = (from p in users
                                         where p.Name == userName
                                         select p).FirstOrDefault<Users>();
                                if (u == null)
                                {
                                    users.Add(new Users() { Name = userName, Password = password, IsAutoLogin = isAutoLogin });
                                }

                                JObject obj = new JObject(
                                    new JProperty("users", new JArray(
                                        from j in users
                                        select new JObject(
                                            new JProperty("name", j.Name),
                                            new JProperty("password", dsecurity.Encrypto(j.Password)),
                                            new JProperty("isAutoLogin", j.IsAutoLogin)
                                            )
                                        ))
                                    );
                                SaveFile(file, obj.ToString());
                            }
                            try
                            {
                                var name = Regex.Match(result, @"var\s+sessionInit\s*=\s*'(?<name>[^']+)';", RegexOptions.Singleline, TimeSpan.FromSeconds(10));
                                if (name.Success)
                                {
                                    result = hhelper.UnicodeToGBK(name.Groups["name"].Value) + "登录成功";
                                }
                                else
                                {
                                    result = "未获取到用户名";
                                }
                            }
                            catch (Exception)
                            {
                                result = "获取用户名超时";
                            }
                        }
                    }
                }
                return result;
            });
        }

        /// <summary>
        /// 注销登录
        /// </summary>
        /// <returns></returns>
        private Task<string> Logout()
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                var url = ConfigurationManager.AppSettings["LogoutUrl"].ToString();
                string result = hhelper.GetResponseChartByGET(url);
                if (result != "")
                {
                    try
                    {
                        var login = Regex.Match(result, @"var\s+clicktitle\s*=\s*'(?<login>[^']+)';", RegexOptions.Singleline, TimeSpan.FromSeconds(10));
                        if (login.Success)
                        {
                            result = login.Groups["login"].Value.ToString();
                        }
                    }
                    catch (Exception)
                    {
                        result = "错误";
                    }
                }
                return result;
            });
        }

        /// <summary>
        /// 读取用户信息
        /// </summary>
        /// <param name="fileName"></param>
        private List<Users> ReadUser(string fileName)
        {
            List<Users> list = new List<Users>();
            if (File.Exists(fileName + ".txt"))
            {
                using (StreamReader reader = new StreamReader(fileName + ".txt", Encoding.UTF8))
                {
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    string result = reader.ReadToEnd();
                    if (result.Count() > 0)
                    {
                        JObject json = JObject.Parse(result);
                        var u = (from j in json["users"]
                                 select new { name = j["name"], password = j["password"], isAutoLogin = j["isAutoLogin"] }).ToList();
                        if (u.Count() > 0)
                        {
                            foreach (var p in u)
                            {
                                list.Add(new Users() { Name = p.name.ToString(), Password = dsecurity.Decrypto(p.password.ToString()), IsAutoLogin = (bool)p.isAutoLogin });
                            }
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        private void SaveFile(string fileName, string data)
        {
            if (!File.Exists(fileName + ".txt"))
            {
                File.CreateText(fileName + ".txt").Close();
            }
            using (StreamWriter writer = new StreamWriter(fileName + ".txt", false, Encoding.UTF8))
            {
                writer.BaseStream.Seek(0, SeekOrigin.Begin);
                writer.Write(data);
            }
        }

        /// <summary>
        /// 获取城市
        /// </summary>
        /// <returns></returns>
        private Task<List<Stations>> GetCitys(string keyword)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                List<Stations> citys = sname.GetStationNmaes();
                citys = (from s in citys
                         where s.PinYin.Contains(keyword) || s.SZiMu.Contains(keyword) || s.ZHName.Contains(keyword)
                         select s).ToList<Stations>();

                return citys;
            });
        }

        /// <summary>
        /// 查询车票
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="fromstation">出发站</param>
        /// <param name="tostation">目的地</param>
        /// <param name="purposecode">是否为普通票</param>
        /// <returns></returns>
        private Task<List<Tickets>> GetSearchTrain(string date, string fromstation, string tostation, string purposecode)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                List<Tickets> ticks = new List<Tickets>();
                string url = ConfigurationManager.AppSettings["QueryTicketUrl"] + "?leftTicketDTO.train_date=" + date + "&leftTicketDTO.from_station=" + fromstation + "&leftTicketDTO.to_station=" + tostation + "&purpose_codes=" + purposecode;
                string result = hhelper.GetResponseChartByGET(url);
                if (result != "" && result != "-1")
                {
                    JObject json = JObject.Parse(result);
                    var ss = (from t in json["data"].Children()
                              select t).ToList();
                    var list = (from t in json["data"]
                                select new
                                {
                                    trainno = t["queryLeftNewDTO"]["train_no"],//车次编号
                                    stationtraincode = t["queryLeftNewDTO"]["station_train_code"],//车次
                                    startstationtelecode = t["queryLeftNewDTO"]["start_station_telecode"],//始发站代码
                                    startstationname = t["queryLeftNewDTO"]["start_station_name"],//始发站
                                    endstationtelecode = t["queryLeftNewDTO"]["end_station_telecode"],//终点站代码
                                    endstationname = t["queryLeftNewDTO"]["end_station_name"],//终点站
                                    fromstationtelecode = t["queryLeftNewDTO"]["from_station_telecode"],//出发地代码
                                    fromstationname = t["queryLeftNewDTO"]["from_station_name"],//出发地
                                    tostationtelecode = t["queryLeftNewDTO"]["to_station_telecode"],//目的地代码
                                    tostationname = t["queryLeftNewDTO"]["to_station_name"],//目的地
                                    starttime = t["queryLeftNewDTO"]["start_time"],//出发时间
                                    arrivetime = t["queryLeftNewDTO"]["arrive_time"],//到达时间
                                    daydifference = t["queryLeftNewDTO"]["day_difference"],//天数
                                    trainclassname = t["queryLeftNewDTO"]["train_class_name"],//列车类型名称
                                    lishi = t["queryLeftNewDTO"]["lishi"],//历时
                                    canWebBuy = t["queryLeftNewDTO"]["canWebBuy"],//是否可以预订
                                    lishiValue = t["queryLeftNewDTO"]["lishiValue"],
                                    ypinfo = t["queryLeftNewDTO"]["yp_info"],
                                    controltrainday = t["queryLeftNewDTO"]["control_train_day"],
                                    starttraindate = t["queryLeftNewDTO"]["start_train_date"],//发车日期
                                    seatfeature = t["queryLeftNewDTO"]["seat_feature"],
                                    ypex = t["queryLeftNewDTO"]["yp_ex"],
                                    trainseatfeature = t["queryLeftNewDTO"]["train_seat_feature"],
                                    seattypes = t["queryLeftNewDTO"]["seat_types"],//席别
                                    locationcode = t["queryLeftNewDTO"]["location_code"],
                                    fromstationno = t["queryLeftNewDTO"]["from_station_no"],//出发地编号
                                    tostationno = t["queryLeftNewDTO"]["to_station_no"],//目的地编号
                                    controlday = t["queryLeftNewDTO"]["control_day"],//日期差
                                    saletime = t["queryLeftNewDTO"]["sale_time"],//预订起售时间
                                    issupportcard = t["queryLeftNewDTO"]["is_support_card"],
                                    ggnum = t["queryLeftNewDTO"]["gg_num"],
                                    grnum = t["queryLeftNewDTO"]["gr_num"],//高级软卧
                                    qtnum = t["queryLeftNewDTO"]["qt_num"],//其他
                                    rwnum = t["queryLeftNewDTO"]["rw_num"],//软卧
                                    rznum = t["queryLeftNewDTO"]["rz_num"],//软座
                                    tznum = t["queryLeftNewDTO"]["tz_num"],//特等座
                                    wznum = t["queryLeftNewDTO"]["wz_num"],//无座
                                    ybnum = t["queryLeftNewDTO"]["yb_num"],
                                    ywnum = t["queryLeftNewDTO"]["yw_num"],//硬卧
                                    yznum = t["queryLeftNewDTO"]["yz_num"],//硬座
                                    zenum = t["queryLeftNewDTO"]["ze_num"],//二等座
                                    zynum = t["queryLeftNewDTO"]["zy_num"],//一等座
                                    swznum = t["queryLeftNewDTO"]["swz_num"],//商务座
                                    secretStr = t["secretStr"]//预订凭证
                                }).ToList();

                    if (list.Count > 0)
                    {
                        foreach (var t in list)
                        {
                            ticks.Add(new Tickets()
                            {
                                TrainNo = t.trainno.ToString(),
                                TrainName = t.stationtraincode.ToString(),
                                StartStationCode = t.startstationtelecode.ToString(),
                                StartStationName = t.startstationname.ToString(),
                                EndStationCode = t.endstationtelecode.ToString(),
                                EndStationName = t.endstationname.ToString(),
                                FromStationCode = t.fromstationtelecode.ToString(),
                                FromStationName = t.fromstationname.ToString(),
                                ToStationCode = t.tostationtelecode.ToString(),
                                ToStationName = t.tostationname.ToString(),
                                StartTime = t.starttime.ToString(),
                                ArriveTime = t.arrivetime.ToString(),
                                From = t.fromstationname.ToString() + "（" + t.starttime.ToString() + "）",
                                To = t.tostationname.ToString() + "（" + t.arrivetime.ToString() + "）",
                                DayDifference = t.daydifference.ToString(),
                                TrainClassName = t.trainclassname.ToString(),
                                LiShi = t.lishi.ToString(),
                                LiShiDay = t.daydifference.ToString() == "0" ? t.lishi.ToString() + "（当日到达）" :
                                        t.daydifference.ToString() == "1" ? t.lishi.ToString() + "（次日到达）" :
                                        t.daydifference.ToString() == "2" ? t.lishi.ToString() + "（两天内到达）" :
                                        t.daydifference.ToString() == "3" ? t.lishi.ToString() + "（三天内到达）" :
                                        t.daydifference.ToString() == "4" ? t.lishi.ToString() + "（四天内到达）" :
                                        t.daydifference.ToString() == "5" ? t.lishi.ToString() + "（五天内到达）" :
                                        t.daydifference.ToString() == "6" ? t.lishi.ToString() + "（六天内到达）" :
                                        t.daydifference.ToString() == "7" ? t.lishi.ToString() + "（七天内到达）" :
                                        t.lishi.ToString(),
                                IsCanBuy = t.canWebBuy.ToString() == "Y" ? true : false,
                                LiShiValue = t.lishiValue.ToString(),
                                YPInfo = t.ypinfo.ToString(),
                                ControlTrainDay = t.controltrainday.ToString(),
                                StartTrainDate = t.starttraindate.ToString().Substring(0, 4) + "-" + t.starttraindate.ToString().Substring(4, 2) + "-" + t.starttraindate.ToString().Substring(6, 2),
                                SeatFeature = t.seatfeature.ToString(),
                                YPEx = t.ypex.ToString(),
                                TrainSeatFeature = t.trainseatfeature.ToString(),
                                SeatTypes = t.seattypes.ToString(),
                                LocationCode = t.locationcode.ToString(),
                                FromStationNo = t.fromstationno.ToString(),
                                ToStationNo = t.tostationno.ToString(),
                                ControlDay = t.controlday.ToString(),
                                SaleTime = t.saletime.ToString().Substring(0, 2) + ":" + t.saletime.ToString().Substring(2, 2),
                                IsSupportCard = t.issupportcard.ToString(),
                                GGNum = t.ggnum.ToString(),
                                GRNum = t.grnum.ToString(),
                                QTNum = t.qtnum.ToString(),
                                RWNum = t.rwnum.ToString(),
                                RZNum = t.rznum.ToString(),
                                TZNum = t.tznum.ToString(),
                                WZNum = t.wznum.ToString(),
                                YBNum = t.ybnum.ToString(),
                                YWNum = t.ywnum.ToString(),
                                YZNum = t.yznum.ToString(),
                                ZENum = t.zenum.ToString(),
                                ZYNum = t.zynum.ToString(),
                                SWZNum = t.swznum.ToString(),
                                SecretStr = t.secretStr.ToString()
                            });
                        }
                    }
                }
                return ticks;
            });
        }

        /// <summary>
        /// 保存联系人
        /// </summary>
        /// <param name="passengerName">乘客名</param>
        /// <returns></returns>
        private Task<bool> SaveContacts(string passengerName)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                bool isSuccess = false;
                var url = ConfigurationManager.AppSettings["ContactUrl"].ToString();
                Dictionary<string, string> param = new Dictionary<string, string>();
                param.Add("pageIndex", "1");
                param.Add("pageSize", "9999");
                string result = hhelper.GetResponseByPOST(url, param);
                if (result != "")
                {
                    JObject json = JObject.Parse(result);
                    string file = "Contact";
                    SaveFile(file, json.ToString());
                    isSuccess = true;
                }
                return isSuccess;
            });
        }

        /// <summary>
        /// 读取乘客
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private List<Contacts> ReadContacts(string fileName)
        {
            List<Contacts> contacts = new List<Contacts>();
            if (File.Exists(fileName + ".txt"))
            {
                using (StreamReader read = new StreamReader(fileName + ".txt", Encoding.UTF8))
                {
                    read.BaseStream.Seek(0, SeekOrigin.Begin);
                    string result = read.ReadToEnd();
                    if (result != "")
                    {
                        JObject json = JObject.Parse(result);
                        var p = (from c in json["data"]["datas"]
                                 select new
                                 {
                                     passengerName = c["passenger_name"],
                                     sex = c["sex_name"],
                                     passengerIdTypeName = c["passenger_id_type_name"],
                                     passengerIdNo = c["passenger_id_no"],
                                     moile = c["mobile_no"],
                                     passengerTypeName = c["passenger_type_name"],
                                     address = c["address"],
                                     code = c["code"],
                                     countryCode = c["country_code"],
                                     email = c["email"],
                                     firstLetter = c["first_letter"],
                                     isUserSelf = c["isUserSelf"],
                                     passengerFlag = c["passenger_flag"],
                                     passengerIdTypeCode = c["passenger_id_type_code"],
                                     passengerType = c["passenger_type"],
                                     phone = c["phone_no"],
                                     postalCode = c["postalcode"],
                                     recordCount = c["recordCount"],
                                     sexCode = c["sex_code"],
                                     bornDate = c["born_date"]
                                 }).ToList();
                        if (p.Count() > 0)
                        {
                            foreach (var c in p)
                            {
                                contacts.Add(new Contacts()
                                {
                                    PassengerName = c.passengerName.ToString(),
                                    SexName = c.sex.ToString(),
                                    PassengerIdTypeName = c.passengerIdTypeName.ToString(),
                                    PassengerIdNo = c.passengerIdNo.ToString(),
                                    Mobile = c.moile.ToString(),
                                    PassengerTypeName = c.passengerTypeName.ToString(),
                                    Address = c.address.ToString(),
                                    Code = c.code.ToString(),
                                    CountryCode = c.countryCode.ToString(),
                                    Email = c.email.ToString(),
                                    UserName = c.firstLetter.ToString(),
                                    IsUserSelf = c.isUserSelf.ToString(),
                                    PassengerFlag = c.passengerFlag.ToString(),
                                    PassengerIdTypeCode = c.passengerIdTypeCode.ToString(),
                                    PassengerType = c.passengerType.ToString(),
                                    Phone = c.phone.ToString(),
                                    PostalCode = c.postalCode.ToString(),
                                    RecordCount = c.recordCount.ToString(),
                                    SexCode = c.sexCode.ToString(),
                                    BornDate = c.bornDate.ToString()
                                });
                            }
                        }
                    }
                }
            }
            return contacts;
        }

        /// <summary>
        /// 获取提交订单验证码并识别
        /// </summary>
        /// <returns></returns>
        private Task<BitmapImage> GetSubmitOrderCode()
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                var url = ConfigurationManager.AppSettings["OrderSubmitCodeUrl"] + "&rand=randp";
                var data = hhelper.GetResponseData(url);
                BitmapImage bmp = new BitmapImage();
                int count = 0;
                if (data != null)
                {
                    do
                    {
                        codeBuilder.Length = 0;
                        if (BasicOCR.GetCodeFromBuffer(1, data, data.Length, codeBuilder))
                        {
                            order_code = codeBuilder.ToString();
                        }
                        count++;

                    } while (order_code.Length != 4 && count < 10);
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        bmp.BeginInit();
                        bmp.StreamSource = ms;
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bmp.Freeze();
                    }
                }
                return bmp;
            });
        }

        /// <summary>
        /// 获取车票价格
        /// </summary>
        /// <param name="fromstationno">出发站编号</param>
        /// <param name="seatTypes">席别</param>
        /// <param name="tostationno">目的地编号</param>
        /// <param name="traindate">发车日期</param>
        /// <param name="trainno">列车编号</param>
        /// <returns></returns>
        private Task<string> GetTickPrice(string fromstationno, string _seatTypes, string tostationno, string traindate, string trainno)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                string result = "", url = ConfigurationManager.AppSettings["TickPriceUrl"] + "?train_no=" + trainno + "&from_station_no=" + fromstationno + "&to_station_no=" + tostationno + "&seat_types=" + _seatTypes + "&train_date=" + traindate;
                result = hhelper.GetResponseChartByGET(url);
                if (result != "-1")
                {
                    JObject json = JObject.Parse(result);
                    var val = from j in json["data"]
                              select new
                              {
                                  other = j["OT"].ToString() == "[]" ? "" : j["OT"].ToString(),//其他
                                  noSeat = j["WZ"].ToString(),//无座
                                  seat1 = j["M"].ToString(),//一等座
                                  busindessSeat = j["A9"].ToString(),//商务座
                                  seat2 = j["O"].ToString(),//一等座
                                  trainNo = j["train_no"].ToString(),//列车编号
                                  tdzSeat = j["P"].ToString(),//特等座
                                  rwSeat = j["A4"].ToString(),//软卧
                                  ywSeat = j["A3"].ToString(),//硬卧
                                  yzSeat = j["A1"].ToString(),//硬座
                                  rzSeat = j["A2"].ToString(),//软座
                                  gjrwSeat = j["A6"].ToString()//高级软卧
                              };
                }
                return result;
            });
        }

        /// <summary>
        /// 绑定联系人
        /// </summary>
        /// <returns></returns>
        private async Task BindContact(bool isSearch)
        {
            progressRingAnima.IsActive = true;
            if (isSearch)
            {
                await SaveContacts(txtContactName.Text.Trim());
            }
            List<Contacts> contacts = ReadContacts("Contact");
            if (!isSearch)
            {
                contacts = (from c in contacts
                            where c.PassengerName.Contains(txtContactName.Text.Trim())
                            select c).ToList<Contacts>();
            }
            gridContact.ItemsSource = contacts;
            lblTicketCount.Content = "共" + contacts.Count() + "个联系人";
            progressRingAnima.IsActive = false;
        }

        /// <summary>
        /// 根据车次获取席别
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private Task<Dictionary<string, string>> GetTickType(string type)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                Dictionary<string, string> tickType = new Dictionary<string, string>();
                if (File.Exists("SeatType.txt"))
                {
                    using (StreamReader read = new StreamReader("SeatType.txt", Encoding.UTF8))
                    {
                        string result = read.ReadToEnd();
                        if (result != "")
                        {
                            JObject json = JObject.Parse(result);
                            var t = (from j in json[type]
                                     select new { value = j["value"].ToString(), id = j["id"].ToString() }).ToList();
                            tickType = t.ToDictionary(c => c.id, c => c.value);
                        }
                    }
                }
                return tickType;
            });
        }

        /// <summary>
        /// 刷新验证码
        /// </summary>
        /// <returns></returns>
        private async Task GetValidateCodeImage()
        {
            progressRingAnima.IsActive = true;
            lblStatusMsg.Content = "获取验证码中...";
            imgCode.Source = await GetLoginCodeAsync();
            txtValidateCode.Text = login_code;
            lblStatusMsg.Content = "获取验证码完成";
            progressRingAnima.IsActive = false;
        }

        /// <summary>
        /// 是否显示登录界面
        /// </summary>
        /// <param name="isShow"></param>
        public async void IsShowLoginPopup(bool isShow)
        {
            if (isShow)
            {
                btnLogout.Visibility = Visibility.Hidden;
                gridOpacity.Visibility = Visibility.Visible;
                loginPopup.Visibility = Visibility.Visible;
                lblErrorMsg.Content = "";

                await GetValidateCodeImage();

                List<Users> users = ReadUser(file);
                if (users.Count > 0)
                {
                    txtUserName.ItemsSource = users;
                    txtUserName.DisplayMemberPath = "Name";
                    txtUserName.SelectedValuePath = "Name";
                    txtUserName.SelectedIndex = 0;
                }
            }
            else
            {
                btnLogout.Visibility = Visibility.Visible;
                gridOpacity.Visibility = Visibility.Hidden;
                loginPopup.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// 获取列车类型
        /// </summary>
        /// <returns></returns>
        private string GetCheckedTickType()
        {
            string result = "";
            CheckBox chkTickType;
            foreach (Control type in gridTicketType.Children)
            {
                if (type is CheckBox)
                {
                    chkTickType = type as CheckBox;
                    if (chkTickType.IsChecked == true)
                    {
                        result += chkTickType.Tag + "#";
                    }
                }
            }
            return result;
        }

        //关闭登录层
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            //IsShowLoginPopup(false);
        }

        //登录按钮
        private void btnLoginPopup_Click(object sender, RoutedEventArgs e)
        {
            if (lblLoginName.Text != "登录")
            {
                IsShowLoginPopup(false);
            }
            else
            {
                IsShowLoginPopup(true);
            }
        }

        //窗口加载
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {

            txtDate.DisplayDateStart = DateTime.Now;
            txtDate.DisplayDateEnd = DateTime.Now.AddDays(19);
            txtDate.Text = txtDate.DisplayDateEnd.Value.ToString("yyyy-MM-dd");
            chkDayBefore.Content = DateTime.Parse(txtDate.Text).AddDays(-1).ToString("yyyy-MM-dd");
            chkTwoDayBefore.Content = DateTime.Parse(txtDate.Text).AddDays(-2).ToString("yyyy-MM-dd");

            IsShowLoginPopup(true);

            byte[] buffter = TrainAssistant.Properties.Resources.data;
            if (!BasicOCR.LoadLibFromBuffer(buffter, buffter.Length, "123"))
            {
                MessageBox.Show("API初始化失败！");
            }

        }

        //更换验证码
        private async void imgCode_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            await GetValidateCodeImage();
        }

        //注册新用户
        private void linkJoin_Click(object sender, RoutedEventArgs e)
        {
            var reg_url = ConfigurationManager.AppSettings["UserRegisterUrl"].ToString();
            Process.Start(reg_url);
        }

        //忘记密码
        private void linkForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            var forgot_url = ConfigurationManager.AppSettings["ForgotPasswordUrl"].ToString();
            Process.Start(forgot_url);
        }

        //自动登录选项
        private void chkAutoLogin_Click(object sender, RoutedEventArgs e)
        {
            if (chkAutoLogin.IsChecked == true)
            {
                chkRemeberMe.IsChecked = true;
            }
        }

        //验证验证码是否正确
        private async void txtValidateCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtValidateCode.Text.Length > 5 || txtValidateCode.Text.Length < 4)
            {
                lblStatusMsg.Content = "验证码长度为4位";
            }
            else
            {
                lblStatusMsg.Content = await ValidateLoginCode(txtValidateCode.Text.Trim());
            }
        }

        //登录
        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (txtUserName.Text.Trim() == "")
            {
                lblErrorMsg.Content = "用户名不能为空";
                return;
            }
            if (txtPassword.Password.Trim() == "")
            {
                lblErrorMsg.Content = "密码不能为空";
                return;
            }
            if (txtValidateCode.Text.Trim() == "")
            {
                lblErrorMsg.Content = "验证码不能为空";
                return;
            }
            if (Convert.ToString(lblStatusMsg.Content).Contains("验证码错误"))
            {
                return;
            }
            progressRingAnima.IsActive = true;
            lblStatusMsg.Content = "正在登录...";
            btnLogin.IsEnabled = false;
            try
            {
                lblErrorMsg.Content = await Login(txtUserName.Text.Trim(), txtPassword.Password.Trim(), txtValidateCode.Text.Trim(), (bool)chkRemeberMe.IsChecked, (bool)chkAutoLogin.IsChecked);
            }
            catch (Exception)
            {
                lblErrorMsg.Content = "程序异常";
                lblStatusMsg.Content = "登录";
                btnLogin.IsEnabled = true;
                progressRingAnima.IsActive = false;
            }
            string loginName = lblErrorMsg.Content.ToString();
            if (loginName.IndexOf("登录成功") > 0)
            {
                IsShowLoginPopup(false);
                lblLoginName.Text = "欢迎，" + loginName.Substring(0, loginName.IndexOf("登录成功"));
                lblStatusMsg.Content = loginName.Substring(loginName.IndexOf("登录成功"));
            }
            else
            {
                await GetValidateCodeImage();
            }
            btnLogin.IsEnabled = true;
            progressRingAnima.IsActive = false;
        }

        //更改用户登录
        private void txtUserName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<Users> users = ReadUser(file);
            if (users.Count > 0 && txtUserName.SelectedIndex > -1)
            {
                var model = (from m in users
                             where m.Name == txtUserName.SelectedValue.ToString()
                             select m).FirstOrDefault<Users>();
                if (txtUserName.SelectedValue.ToString() == model.Name)
                {
                    txtPassword.Password = model.Password;
                    chkRemeberMe.IsChecked = true;
                    if (model.IsAutoLogin)
                    {
                        chkAutoLogin.IsChecked = true;
                    }
                    else
                    {
                        chkAutoLogin.IsChecked = false;
                    }
                }
            }
            else
            {
                txtPassword.Password = "";
                chkRemeberMe.IsChecked = false;
                chkAutoLogin.IsChecked = false;
            }
        }

        //退出登录
        private async void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await Logout();
                if (result == "登录")
                {
                    lblLoginName.Text = "登录";
                    IsShowLoginPopup(true);
                }
            }
            catch (Exception)
            {
                lblStatusMsg.Content = "程序异常";
            }
        }

        //出发站
        private async void txtStartCity_KeyUp(object sender, KeyEventArgs e)
        {
            if (txtStartCity.Text.Trim() != "")
            {
                var list = await GetCitys(txtStartCity.Text.ToLower());
                if (list.Count > 0)
                {
                    txtStartCity.ItemsSource = list;
                    txtStartCity.DisplayMemberPath = "ZHName";
                    txtStartCity.SelectedValuePath = "Code";
                    txtStartCity.IsDropDownOpen = true;
                }
            }
        }

        //到达站
        private async void txtEndCity_KeyUp(object sender, KeyEventArgs e)
        {
            if (txtEndCity.Text.Trim() != "")
            {
                var list = await GetCitys(txtEndCity.Text.ToLower());
                if (list.Count > 0)
                {
                    txtEndCity.ItemsSource = list;
                    txtEndCity.DisplayMemberPath = "ZHName";
                    txtEndCity.SelectedValuePath = "Code";
                    txtEndCity.IsDropDownOpen = true;
                }
            }
        }

        //展开日期
        private void txtDate_MouseUp(object sender, MouseButtonEventArgs e)
        {
            txtDate.IsDropDownOpen = true;
        }

        //选择全部车类
        private void chkAll_Click(object sender, RoutedEventArgs e)
        {
            if (chkAll.IsChecked == true)
            {
                chkEMU.IsChecked = chkK.IsChecked = chkZ.IsChecked = chkT.IsChecked = chkOther.IsChecked = chkHSR.IsChecked = true;
            }
            else
            {
                chkEMU.IsChecked = chkK.IsChecked = chkZ.IsChecked = chkT.IsChecked = chkOther.IsChecked = chkHSR.IsChecked = false;
            }
        }

        //选择高铁
        private void chkHSR_Click(object sender, RoutedEventArgs e)
        {
            if (chkHSR.IsChecked == true && chkEMU.IsChecked == true && chkK.IsChecked == true && chkZ.IsChecked == true && chkT.IsChecked == true && chkOther.IsChecked == true)
            {
                chkAll.IsChecked = true;
            }
            else
            {
                chkAll.IsChecked = false;
            }
        }

        //选择动车
        private void chkEMU_Click(object sender, RoutedEventArgs e)
        {
            if (chkEMU.IsChecked == true && chkK.IsChecked == true && chkZ.IsChecked == true && chkT.IsChecked == true && chkOther.IsChecked == true)
            {
                chkAll.IsChecked = true;
            }
            else
            {
                chkAll.IsChecked = false;
            }
        }

        //选择最快
        private void chkZ_Click(object sender, RoutedEventArgs e)
        {
            if (chkZ.IsChecked == true && chkEMU.IsChecked == true && chkK.IsChecked == true && chkT.IsChecked == true && chkOther.IsChecked == true)
            {
                chkAll.IsChecked = true;
            }
            else
            {
                chkAll.IsChecked = false;
            }
        }

        //选择特快
        private void chkT_Click(object sender, RoutedEventArgs e)
        {
            if (chkT.IsChecked == true && chkEMU.IsChecked == true && chkK.IsChecked == true && chkZ.IsChecked == true && chkOther.IsChecked == true)
            {
                chkAll.IsChecked = true;
            }
            else
            {
                chkAll.IsChecked = false;
            }
        }

        //选择K字头
        private void chkK_Click(object sender, RoutedEventArgs e)
        {
            if (chkK.IsChecked == true && chkEMU.IsChecked == true && chkZ.IsChecked == true && chkT.IsChecked == true && chkOther.IsChecked == true)
            {
                chkAll.IsChecked = true;
            }
            else
            {
                chkAll.IsChecked = false;
            }
        }

        //选择其他
        private void chkOther_Click(object sender, RoutedEventArgs e)
        {
            if (chkOther.IsChecked == true && chkEMU.IsChecked == true && chkK.IsChecked == true && chkZ.IsChecked == true && chkT.IsChecked == true == true)
            {
                chkAll.IsChecked = true;
            }
            else
            {
                chkAll.IsChecked = false;
            }
        }

        //切换地址
        private void btnChangeAddress_Click(object sender, RoutedEventArgs e)
        {
            string startStation = txtStartCity.Text.ToString();
            string endStation = txtEndCity.Text.ToString();

            txtStartCity.Text = endStation;
            txtEndCity.Text = startStation;
        }

        //自动搜索
        private void chkAutoSearch_Click(object sender, RoutedEventArgs e)
        {
            if (chkAutoSearch.IsChecked == true)
            {

            }
        }

        //搜索
        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (txtStartCity.Text.Trim() == "")
            {
                lblStatusMsg.Content = "出发地不能为空";
                return;
            }
            if (txtEndCity.Text.Trim() == "")
            {
                lblStatusMsg.Content = "目的地不能为空";
                return;
            }
            if (txtDate.Text.Trim() == "")
            {
                lblStatusMsg.Content = "出发日期不能为空";
                return;
            }
            string isNormal = rdoNormal.IsChecked == true ? rdoNormal.Tag.ToString() : rdoStudent.Tag.ToString();
            progressRingAnima.IsActive = true;
            List<Tickets> ticketModel = await GetSearchTrain(txtDate.Text.Replace('/', '-'), txtStartCity.SelectedValue.ToString(), txtEndCity.SelectedValue.ToString(), isNormal);
            gridTrainList.ItemsSource = ticketModel;
            lblTicketCount.Content = txtStartCity.Text + "-->" + txtEndCity.Text + "（共" + ticketModel.Count() + "趟列车）";
            progressRingAnima.IsActive = false;
        }

        //预订
        private async void Reservate_Click(object sender, RoutedEventArgs e)
        {
            Tickets tickets = gridTrainList.SelectedItem as Tickets;
            seatTypes = "";
            for (int o = 4; o < gridTrainList.Columns.Count - 1; o++)
            {
                string str = (gridTrainList.Columns[o].GetCellContent(gridTrainList.SelectedItem) as TextBlock).Text;
                if (!str.Contains("--") && !str.Contains("无"))
                {
                    seatTypes += gridTrainList.Columns[o].Header.ToString() + "@";
                }
            }
            lblTicket.Content = tickets.FromStationName + "→" + tickets.ToStationName + "(" + tickets.TrainName + ")";
            lblTicket.Tag = tickets.StartTrainDate + "," + tickets.TrainNo + "," + tickets.TrainName + "," + tickets.FromStationCode + "," + tickets.ToStationCode + "," + tickets.YPInfo + "," + tickets.LocationCode;
            string purposeCode = rdoNormal.IsChecked == true ? rdoNormal.Tag.ToString() : rdoStudent.Tag.ToString();
            Dictionary<bool, string> dicSubmitOrderReq = await SubmitOrderRequest(tickets, purposeCode);
            if (!dicSubmitOrderReq.Keys.First())
            {
                MessageBox.Show(dicSubmitOrderReq.Values.First(), "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            progressRingAnima.IsActive = true;
            gridOpacity.Visibility = Visibility.Visible;
            orderPopup.Visibility = Visibility.Visible;
            List<Contacts> contacts = ReadContacts("Contact");
            int row = contacts.Count;//(int)Math.Ceiling((double)contacts.Count / 3);
            while (row-- > 0)
            {
                gContacts.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength()
                });
            }
            if (contacts.Count > 0)
            {
                gContacts.Children.Clear();
                for (int i = 0; i < contacts.Count; i++)
                {
                    CheckBox chkContact = new CheckBox()
                    {
                        Content = contacts[i].PassengerName,
                        Name = "chk" + contacts[i].Code,
                        Height = 15,
                        Tag = contacts[i].PassengerTypeName + "#" + contacts[i].PassengerName + "#" + contacts[i].PassengerIdTypeName + "#" + contacts[i].PassengerIdNo + "#" + contacts[i].Mobile
                    };
                    chkContact.Click += chkContact_Click;
                    gContacts.Children.Add(chkContact);
                    chkContact.SetValue(Grid.RowProperty, i);
                    chkContact.SetValue(Grid.ColumnProperty, 0);
                }
            }
            lblSecretStr.Content = await GetSubmitOrderToken();
            imgOrderCode.Source = await GetSubmitOrderCode();
            txtOrderCode.Text = order_code;
            progressRingAnima.IsActive = false;
        }

        //选择乘客
        async void chkContact_Click(object sender, RoutedEventArgs e)
        {
            string val = "";
            CheckBox chk;
            foreach (var c in gContacts.Children)
            {
                if (c is CheckBox)
                {
                    chk = c as CheckBox;
                    if (chk.IsChecked == true)
                    {
                        val += chk.Tag + ",";
                    }
                }
            }
            string train = lblTicket.Content.ToString().Substring(lblTicket.Content.ToString().IndexOf("(") + 1, 1);
            if (train == "G")
            {
                train = "D";
            }
            Dictionary<string, string> seatType = await GetTickType(train);//席别
            IDictionary<string, string> seat_Type = new Dictionary<string, string>();
            var type = seatTypes.Split('@');
            for (int i = 0; i < type.Count(); i++)
            {
                if (type[i] != "")
                {
                    var _seatType = (from s in seatType
                                     where s.Value == type[i]
                                     select s).ToDictionary(t => t.Key, t => t.Value);
                    foreach (var t in _seatType)
                    {
                        if (!seat_Type.Contains(t))
                        {
                            seat_Type.Add(t);
                        }
                    }
                }
            }

            Dictionary<string, string> tickType = new Dictionary<string, string>();//票种
            tickType.Add("成人票", "1");
            tickType.Add("儿童票", "2");
            tickType.Add("学生票", "3");
            tickType.Add("残军票", "4");
            Dictionary<string, string> idType = new Dictionary<string, string>();//证件类型
            idType.Add("二代身份证", "1");
            idType.Add("一代身份证", "2");
            idType.Add("港澳通行证", "C");
            idType.Add("台湾通行证", "G");
            idType.Add("护照", "B");

            List<SubmitOrder> subOrder = new List<SubmitOrder>();
            if (val != "")
            {
                var selPassenger = val.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);//选中的乘客
                if (selPassenger.Count() > 5)
                {
                    MessageBox.Show("乘客数不能超过5个", "消息", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CheckBox chkItem = e.Source as CheckBox;
                    chkItem.IsChecked = false;
                    return;
                }
                for (int j = 0; j < selPassenger.Count(); j++)
                {
                    if (selPassenger[j] != "")
                    {
                        var item = selPassenger[j].Split('#');
                        SubmitOrder subOrderModel = new SubmitOrder();
                        subOrderModel.SeatType = seat_Type.ToDictionary(m => m.Key, m => m.Value);
                        subOrderModel.SelSeatType = subOrderModel.SeatType.Keys.Last();
                        subOrderModel.TickType = (from t in tickType
                                                  where t.Key.Contains(item[0])
                                                  select t).ToDictionary(m => m.Key, m => m.Value);
                        subOrderModel.SelTickType = subOrderModel.TickType.Values.First();
                        subOrderModel.PassengerName = item[1];
                        subOrderModel.IDType = (from d in idType
                                                where d.Key.Contains(item[2])
                                                select d).ToDictionary(m => m.Key, m => m.Value);
                        subOrderModel.SelIDType = subOrderModel.IDType.Values.First();
                        subOrderModel.PassengerId = item[3];
                        subOrderModel.PassengerMobile = item[4];
                        subOrder.Add(subOrderModel);
                    }
                }
            }

            gridPassenger.ItemsSource = subOrder;
        }

        //常用联系人
        private async void tabContact_MouseUp(object sender, MouseButtonEventArgs e)
        {
            await BindContact(true);
        }

        //车票查询/预订
        private void tabQuery_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }

        //查询联系人
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await BindContact(false);
        }

        //关闭提交订单层
        private void btnClosePopup_Click(object sender, RoutedEventArgs e)
        {
            gridOpacity.Visibility = Visibility.Hidden;
            orderPopup.Visibility = Visibility.Hidden;
            gridPassenger.ItemsSource = null;
        }

        //更换订单验证码
        private async void imgOrderCode_MouseUp(object sender, MouseButtonEventArgs e)
        {
            progressRingAnima.IsActive = true;
            imgOrderCode.Source = await GetSubmitOrderCode();
            txtOrderCode.Text = order_code;
            progressRingAnima.IsActive = false;
        }

        //提交
        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (gridPassenger.Items.Count < 1)
            {
                lblStatusMsg.Content = "未选择乘客";
                return;
            }
            if (txtOrderCode.Text.Trim() == "")
            {
                lblStatusMsg.Content = "验证码不能为空";
                return;
            }
            if (txtOrderCode.Text.Trim().Length < 4 || txtOrderCode.Text.Trim().Length > 4)
            {
                lblStatusMsg.Content = "验证码长度为4位";
                return;
            }
            var lstPassengers = gridPassenger.ItemsSource as List<SubmitOrder>;
            progressRingAnima.IsActive = true;
            List<string> lstQueues = new List<string>();
            var arrQueues = lblTicket.Tag.ToString().Split(',');
            for (int i = 0; i < arrQueues.Length; i++)
            {
                lstQueues.Add(arrQueues[i]);
            }
            var arrToken = lblSecretStr.Content.ToString().Split(',');
            string orderResult = await SubmitOrder(lstPassengers, txtOrderCode.Text, arrToken[0], lstQueues, arrToken[1]);
            lblStatusMsg.Content = orderResult;
            MessageBox.Show(orderResult, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            progressRingAnima.IsActive = false;
        }

        /// <summary>
        /// 预订
        /// </summary>
        /// <param name="ticket"></param>
        /// <param name="purposeCode"></param>
        /// <returns></returns>
        public Task<Dictionary<bool, string>> SubmitOrderRequest(Tickets ticket, string purposeCode)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                string orderRequestUrl = ConfigurationManager.AppSettings["SubmitOrderRequestUrl"].ToString();
                Dictionary<string, string> orderRequesParams = new Dictionary<string, string>()
                {
                    {"secretStr",ticket.SecretStr},
                    {"train_date",ticket.StartTrainDate},
                    {"back_train_date",DateTime.Now.ToString("yyyy-MM-dd")},
                    {"tour_flag","dc"},
                    {"purpose_codes",purposeCode},
                    {"query_from_station_name",ticket.StartStationName},
                    {"query_to_station_name",ticket.ToStationName},
                    {"undefined",""}
                };
                string submitOrderRequestResult = hhelper.GetResponseByPOST(orderRequestUrl, orderRequesParams);
                Dictionary<bool, string> dicResult = new Dictionary<bool, string>();
                if (!string.IsNullOrEmpty(submitOrderRequestResult))
                {
                    JObject json = JObject.Parse(submitOrderRequestResult);
                    dicResult.Add((bool)json["status"], json["messages"].ToString().Replace("[\"", "").Replace("\"]", ""));
                }
                return dicResult;
            });
        }

        /// <summary>
        /// 提交订单
        /// </summary>
        /// <returns></returns>
        private async Task<string> SubmitOrder(List<SubmitOrder> lstPassengers, string submitOrderCode, string token, List<string> lstQueues, string keyChange)
        {
            string oldPassengers = "", passengerTickets = "";
            int p = 0;
            foreach (var passenger in lstPassengers)
            {
                //证件类型
                DataGridCell selIDTypeCell = await GetDataGridCell(gridPassenger, p, 4);
                var idTypeValue = selIDTypeCell.Content as ComboBox;
                //席别
                DataGridCell selSeatTypeCell = await GetDataGridCell(gridPassenger, p, 0);
                var seateTypeValue = selSeatTypeCell.Content as ComboBox;
                //票种
                DataGridCell selTickTypeCell = await GetDataGridCell(gridPassenger, p, 3);
                var tickTypeValue = selTickTypeCell.Content as ComboBox;

                oldPassengers += passenger.PassengerName + "," + idTypeValue.SelectedValue + "," + passenger.PassengerId + "," + tickTypeValue.SelectedValue + "_";
                passengerTickets += seateTypeValue.SelectedValue + ",0," + tickTypeValue.SelectedValue + "," + passenger.PassengerName + "," + idTypeValue.SelectedValue + "," + passenger.PassengerId + "," + passenger.PassengerMobile + ",N_";
                p++;
            }
            passengerTickets = HttpUtility.UrlEncode(passengerTickets.TrimEnd('_'));
            oldPassengers = HttpUtility.UrlEncode(oldPassengers);

            //检验验证码
            bool checkOrderCode = await CheckOrderCode(submitOrderCode, token);
            string resultMsg = "";
            if (!checkOrderCode)
            {
                return "验证码不正确";
            }
            //检查订单信息
            Dictionary<bool, string> checkOrderInfo = await CheckOrderInfo(passengerTickets, oldPassengers, submitOrderCode, token);
            if (!checkOrderInfo.Keys.First())
            {
                return checkOrderInfo.Values.First();
            }
            //获取排队
            OrderQueue orderQueue = await GetQueueCount(lstQueues, token, checkOrderInfo.Values.First());
            if (orderQueue.OP_2)
            {
                return "排队人数（" + orderQueue.countT + "）超过余票数。";
            }
            //确认订单
            Dictionary<bool, string> confirmOrder = await ConfirmOrderQueue(passengerTickets, oldPassengers, submitOrderCode, token, keyChange, lstQueues);
            if (!confirmOrder.Keys.First())
            {
                return confirmOrder.Values.First();
            }
            //O038850 401 M060350 030 P071950 008 -8
            //O038850 401 M060350 030 P071950 008 -30
            //O038850 401 M060350 030 P071950 008 -401

            //等待出票
            while (true)
            {
                QueryOrderWaitTime queryOrderWaitTime = await QueryOrderWaitTime("", "dc", "", token);
                if (queryOrderWaitTime.WaitTime <= 0)
                {
                    if (!string.IsNullOrEmpty(queryOrderWaitTime.OrderId))
                    {
                       resultMsg="出票成功！订单号：【" + queryOrderWaitTime.OrderId + "】";
                    }
                    break;
                }
                else
                {
                    lblStatusMsg.Content = queryOrderWaitTime.Count > 0 ? "前面还有【" + queryOrderWaitTime.WaitCount + "】订单等待处理，大约需等待【" + queryOrderWaitTime.WaitTime + "】秒" : "正在处理订单，大约需要" + queryOrderWaitTime.WaitTime + "秒";
                }
            }
            return resultMsg;
        }

        /// <summary>
        /// 获取提交订单凭证
        /// </summary>
        /// <returns></returns>
        public Task<string> GetSubmitOrderToken()
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                string submitOrderTokenUrl = ConfigurationManager.AppSettings["OrderTokenUrl"].ToString(), result = "";
                Dictionary<string, string> submitOrderTokenParams = new Dictionary<string, string>(){
                    {"_json_att",""}
                };
                result = hhelper.GetResponseByPOST(submitOrderTokenUrl, submitOrderTokenParams);
                string strResult = "";
                var strToken = Regex.Match(result, @"var\s+globalRepeatSubmitToken\s*=\s*'(?<token>[^']+)';", RegexOptions.Singleline, TimeSpan.FromSeconds(10));
                if (strToken.Success)
                {
                    strResult = strToken.Groups["token"].Value;
                }
                var keyIsChang = Regex.Match(result, @"'key_check_isChange':'(?<key>[^']+)'", RegexOptions.Singleline, TimeSpan.FromSeconds(10));
                if (keyIsChang.Success)
                {
                    strResult += "," + keyIsChang.Groups["key"].Value;
                }
                return strResult;
            });
        }

        /// <summary>
        /// 检验订单验证
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public Task<bool> CheckOrderCode(string code, string token)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                Dictionary<string, string> orderCodeParams = new Dictionary<string, string>(){
                    {"randCode",code},
                    {"rand","randp"},
                    {"_json_att",""},
                    {"REPEAT_SUBMIT_TOKEN",token}
                };
                string orderCodeUrl = ConfigurationManager.AppSettings["OrderVlidateCodeUrl"].ToString();
                string result = hhelper.GetResponseByPOST(orderCodeUrl, orderCodeParams);
                bool reqResult = true;
                if (!string.IsNullOrEmpty(result))
                {
                    JObject json = JObject.Parse(result);
                    result = json["data"].ToString();
                    if (result == "N")
                    {
                        reqResult = false;
                    }
                }
                return reqResult;
            });
        }

        /// <summary>
        /// 检查订单信息
        /// </summary>
        /// <param name="lstPassengers"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public Task<Dictionary<bool, string>> CheckOrderInfo(string passengerTickets, string oldPassengers, string code, string token)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                Dictionary<string, string> checkOrderParams = new Dictionary<string, string>();
                checkOrderParams.Add("cancel_flag", "2");
                checkOrderParams.Add("bed_level_order_num", "000000000000000000000000000000");
                checkOrderParams.Add("passengerTicketStr", passengerTickets);
                checkOrderParams.Add("oldPassengerStr", oldPassengers);
                checkOrderParams.Add("tour_flag", "dc");
                checkOrderParams.Add("randCode", code);
                checkOrderParams.Add("_json_att", "");
                checkOrderParams.Add("REPEAT_SUBMIT_TOKEN", token);
                string checkOrderInfoUrl = ConfigurationManager.AppSettings["CheckOrderInfoUrl"].ToString();
                string checkOrderInfoResult = hhelper.GetResponseByPOST(checkOrderInfoUrl, checkOrderParams);
                bool result = checkOrderInfoResult.Contains("\"submitStatus\":true");
                string firstSeat = HttpUtility.UrlDecode(passengerTickets);
                string resMsg = firstSeat.Substring(0, firstSeat.IndexOf(','));
                if (!result)
                {
                    JObject json = JObject.Parse(checkOrderInfoResult);
                    resMsg = json["data"]["errMsg"].ToString();
                }
                Dictionary<bool, string> dicResult = new Dictionary<bool, string>(){
                    {result,resMsg}
                };
                return dicResult;
            });
        }

        /// <summary>
        /// 排队
        /// </summary>
        /// <param name="strTicks"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<OrderQueue> GetQueueCount(List<string> strTicks, string token, string seate)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                OrderQueue orderQueue = new OrderQueue();
                string queueCountUrl = ConfigurationManager.AppSettings["TickQueueCountUrl"].ToString(), queueCountResult = "";
                string trainDate = Convert.ToDateTime(strTicks[0]).ToString("ddd MMM dd yyyy HH:mm:ss 'GMT'zzz", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
                trainDate = HttpUtility.UrlEncode(trainDate.Replace("08:00", "0800"));
                Dictionary<string, string> dicQueueParams = new Dictionary<string, string>(){
                    {"train_date",trainDate},
                    {"train_no",strTicks[1]},
                    {"stationTrainCode",strTicks[2]},
                    {"seatType",seate},
                    {"fromStationTelecode",strTicks[3]},
                    {"toStationTelecode",strTicks[4]},
                    {"leftTicket",strTicks[5]},
                    {"purpose_codes","00"},
                    {"_json_att",""},
                    {"REPEAT_SUBMIT_TOKEN",token}
                };
                queueCountResult = hhelper.GetResponseByPOST(queueCountUrl, dicQueueParams);
                if (!string.IsNullOrEmpty(queueCountResult))
                {
                    JObject json = JObject.Parse(queueCountResult);
                    var jsonData = json["data"];
                    orderQueue.Count = jsonData["count"].ToString();
                    orderQueue.countT = jsonData["countT"].ToString();
                    orderQueue.OP_1 = jsonData["op_1"].ToString();
                    orderQueue.OP_2 = Convert.ToBoolean(jsonData["op_2"].ToString());
                    orderQueue.Ticket = jsonData["ticket"].ToString();
                }
                return orderQueue;
            });
        }

        /// <summary>
        /// 确认订单
        /// </summary>
        /// <param name="passengerTickets"></param>
        /// <param name="oldPassengers"></param>
        /// <param name="code"></param>
        /// <param name="token"></param>
        /// <param name="key"></param>
        /// <param name="lstQueues"></param>
        /// <returns></returns>
        public Task<Dictionary<bool, string>> ConfirmOrderQueue(string passengerTickets, string oldPassengers, string code, string token, string key, List<string> lstQueues)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                string confirmOrderUrl = ConfigurationManager.AppSettings["ConfirmSubmitOrderUrl"].ToString(), confirmOrderResult = "";
                Dictionary<string, string> dicConfirmOrderParams = new Dictionary<string, string>(){
                    {"passengerTicketStr",passengerTickets},
                    {"oldPassengerStr",oldPassengers},
                    {"randCode",code},
                    {"purpose_codes","00"},
                    {"key_check_isChange",key},
                    {"leftTicketStr",lstQueues[5]},
                    {"train_location",lstQueues[6]},
                    {"_json_att",""},
                    {"REPEAT_SUBMIT_TOKEN",token}
                };
                confirmOrderResult = hhelper.GetResponseByPOST(confirmOrderUrl, dicConfirmOrderParams);
                bool result = confirmOrderResult.Contains("\"submitStatus\":true");
                string msg = "";
                if (!result)
                {
                    JObject json = JObject.Parse(confirmOrderResult);
                    msg = json["data"]["errMsg"].ToString();
                }
                Dictionary<bool, string> dicResult = new Dictionary<bool, string>()
                {
                    {result,msg}
                };
                return dicResult;
            });
        }

        /// <summary>
        /// 生成订单等待时间
        /// </summary>
        /// <param name="random"></param>
        /// <param name="tourFlag"></param>
        /// <param name="jsonAtt"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<QueryOrderWaitTime> QueryOrderWaitTime(string random, string tourFlag, string jsonAtt, string token)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                QueryOrderWaitTime queryOrderWaitTime = new QueryOrderWaitTime();
                string waitTimeUrl = ConfigurationManager.AppSettings["QueryOrderWaitTimeUrl"].ToString() + "?random={0}&tourFlag={1}&_json_att={2}&REPEAT_SUBMIT_TOKEN={3}";
                waitTimeUrl = string.Format(waitTimeUrl, random, tourFlag, jsonAtt, token);
                string responResult = hhelper.GetResponseChartByGET(waitTimeUrl);
                if (responResult != "-1" || !string.IsNullOrEmpty(responResult))
                {
                    JObject json = JObject.Parse(responResult);
                    var jsonData = json["data"];
                    queryOrderWaitTime.Count = Convert.ToInt32(jsonData["count"].ToString());
                    queryOrderWaitTime.OrderId = jsonData["orderId"].ToString();
                    queryOrderWaitTime.RequestId = jsonData["requestId"].ToString();
                    queryOrderWaitTime.Status = Convert.ToBoolean(jsonData["queryOrderWaitTimeStatus"].ToString());
                    queryOrderWaitTime.TourFlag = jsonData["tourFlag"].ToString();
                    queryOrderWaitTime.WaitCount = Convert.ToInt32(jsonData["waitCount"].ToString());
                    queryOrderWaitTime.WaitTime = Convert.ToInt32(jsonData["waitTime"].ToString());
                }
                return queryOrderWaitTime;
            });
        }

        /// <summary>
        /// 获取DataGrid行
        /// </summary>
        /// <param name="dataGrid"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public Task<DataGridRow> GetDataGridRow(DataGrid dataGrid, int index)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(index);
                if (row == null)
                {
                    dataGrid.UpdateLayout();
                    dataGrid.ScrollIntoView(dataGrid.Items[index]);
                    row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(index);
                }
                return row;
            });
        }

        /// <summary>
        /// 获取当前节点的子节点
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <returns></returns>
        public T GetObjectChildren<T>(Visual parent) where T : Visual
        {
            T children = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                children = v as T;
                if (children == null)
                {
                    children = GetObjectChildren<T>(v);
                }
                if (children != null)
                {
                    break;
                }
            }
            return children;
        }

        /// <summary>
        /// 获取DataGrid列
        /// </summary>
        /// <param name="dataGrid"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public async Task<DataGridCell> GetDataGridCell(DataGrid dataGrid, int row, int column)
        {
            DataGridRow rowContainer = await GetDataGridRow(dataGrid, row);
            if (rowContainer != null)
            {
                DataGridCellsPresenter presenter = GetObjectChildren<DataGridCellsPresenter>(rowContainer);

                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                if (cell == null)
                {
                    dataGrid.ScrollIntoView(rowContainer, dataGrid.Columns[column]);
                    cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                }
                return cell;
            }
            return null;
        }
    }
}
