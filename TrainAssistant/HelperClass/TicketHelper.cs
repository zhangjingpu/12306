using JasonLong.Helper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrainAssistant.Models;
using System.Runtime.InteropServices;

namespace JasonLong.Helper
{
    public class TicketHelper
    {
        HttpHelper httpHelper = new HttpHelper();
        DataSecurity dsecurity = new DataSecurity();
        public const string accountFile = "Account";//登录用户信息
        StringBuilder codeBuilder = new StringBuilder(8, 8);

        [DllImport("wininet")]
        private static extern bool InternetGetConnectedState(out int connectionDescription, int reservedValue);

        /// <summary>
        /// 检查是否连接Internet网络
        /// </summary>
        /// <returns></returns>
        public bool CheckInternetConnectedState()
        {
            int connDescript = 0;
            return InternetGetConnectedState(out connDescript, 0);
        }

        /// <summary>
        /// 读取用户信息
        /// </summary>
        /// <param name="fileName"></param>
        public List<Users> ReadUser(string fileName)
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
        /// 读取搜索条件
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<List<Query>> ReadQuerys(string fileName)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                List<Query> lstQuerys = new List<Query>();
                if (File.Exists("Query.txt"))
                {
                    using (StreamReader reader = new StreamReader("Query.txt", Encoding.UTF8))
                    {
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        string strQuery = reader.ReadToEnd();
                        if (strQuery.Count() > 0)
                        {
                            JObject jsonQuery = JObject.Parse(strQuery);
                            var jsonQueryData = (from q in jsonQuery["querys"]
                                                 select new { user = q["user"], fromName = q["fromStationName"], fromCode = q["formStationCode"], toName = q["toStationName"], toCode = q["toStationCode"], date = q["trainDate"] }).ToList();
                            if (jsonQueryData.Count > 0)
                            {
                                foreach (var q in jsonQueryData)
                                {
                                    lstQuerys.Add(new Query() { User = q.user.ToString(), FromName = q.fromName.ToString(), FromCode = q.fromCode.ToString(), ToName = q.toName.ToString(), ToCode = q.toCode.ToString(), Date = q.date.ToString() });
                                }
                            }
                        }
                    }
                }
                return lstQuerys;
            });
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        public void SaveFile(string fileName, string data)
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
        /// 获取登录验证码图片并识别
        /// </summary>
        public Task<Dictionary<BitmapImage, string>> GetLoginCodeAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                byte[] msbuffer = new byte[4096];
                var url = ConfigurationManager.AppSettings["LoginValidateCodeImageUrl"].ToString() + "&rand=sjrand";
                var data = httpHelper.GetResponseData(url);
                int count = 0;
                string strLoginCode = "";
                BitmapImage loginCodeImg = new BitmapImage();//登录验证码图片
                if (data != null)
                {
                    do
                    {
                        codeBuilder.Length = 0;
                        if (BasicOCR.GetCodeFromBuffer(1, data, data.Length, codeBuilder))
                        {
                            strLoginCode = codeBuilder.ToString();
                        }
                        count++;
                    } while (strLoginCode.Length != 4 && count < 10);
                    using (MemoryStream ms = new MemoryStream(data, false))
                    {
                        loginCodeImg.BeginInit();
                        loginCodeImg.StreamSource = ms;
                        loginCodeImg.CacheOption = BitmapCacheOption.OnLoad;
                        loginCodeImg.EndInit();
                        loginCodeImg.Freeze();
                    }
                }
                Dictionary<BitmapImage, string> dicLoginCode = new Dictionary<BitmapImage, string>()
                {
                    {loginCodeImg,strLoginCode}
                };
                return dicLoginCode;
            });
        }

        /// <summary>
        /// 判断登录验证码是否输入正确
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public Task<string> ValidateLoginCode(string code)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                string url = ConfigurationManager.AppSettings["LoginCodeValidateUrl"].ToString(), result = "";
                Dictionary<string, string> param = new Dictionary<string, string>();
                param.Add("randCode", code);
                param.Add("rand", "sjrand");
                string valiresult = httpHelper.GetResponseByPOST(url, param);
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
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="code"></param>
        /// <param name="isRemeberMe"></param>
        /// <param name="isAutoLogin"></param>
        /// <returns></returns>
        public Task<string> Login(string userName, string password, string code, bool isRemeberMe, bool isAutoLogin)
        {
            return Task.Factory.StartNew(() =>
            {
                string result = "", url = ConfigurationManager.AppSettings["LoginUrl"].ToString();
                Dictionary<string, string> param = new Dictionary<string, string>();
                param.Add("loginUserDTO.user_name", userName);//用户名
                param.Add("userDTO.password", password);//密码
                param.Add("randCode", code);//验证码
                Thread.Sleep(100);
                result = httpHelper.GetResponseByPOST(url, param);
                if (result != "")
                {
                    JObject json = JObject.Parse(result);
                    string errormsg = json["messages"].ToString();
                    if (errormsg != "[]")
                    {
                        result = errormsg.Replace("[", "").Replace("]", "").Trim();
                    }
                    else
                    {
                        string isloginsucess = json["data"]["loginCheck"].ToString();
                        if (isloginsucess == "Y")
                        {
                            url = ConfigurationManager.AppSettings["GetLoginUserNameUrl"].ToString();
                            result = httpHelper.GetResponseChartByGET(url);
                            if (isRemeberMe)
                            {
                                List<Users> users = ReadUser(accountFile);
                                var u = (from p in users
                                         where p.Name == userName
                                         select p).FirstOrDefault<Users>();
                                if (u == null)
                                {
                                    users.Add(new Users() { Name = userName, Password = password, IsAutoLogin = isAutoLogin });
                                }
                                else
                                {
                                    u.IsAutoLogin = isAutoLogin;
                                    u.Password = password;
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
                                SaveFile(accountFile, obj.ToString());
                            }
                            try
                            {
                                var name = Regex.Match(result, @"var\s+sessionInit\s*=\s*'(?<name>[^']+)';", RegexOptions.Singleline, TimeSpan.FromSeconds(10));
                                if (name.Success)
                                {
                                    result = httpHelper.UnicodeToGBK(name.Groups["name"].Value) + "登录成功";
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
        public Task<string> Logout()
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                var url = ConfigurationManager.AppSettings["LogoutUrl"].ToString();
                string result = httpHelper.GetResponseChartByGET(url);
                if (!string.IsNullOrEmpty(result))
                {
                    result = "已成功注销";
                }
                return result;
            });
        }

        /// <summary>
        /// 获取城市
        /// </summary>
        /// <returns></returns>
        public Task<List<Stations>> GetCitys(string keyword)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                StationName staionName = new StationName();
                List<Stations> citys = staionName.GetStationNmaes();
                citys = (from s in citys
                         where s.PinYin.Contains(keyword) || s.SZiMu.Contains(keyword) || s.ZHName.Contains(keyword)
                         select s).Take(10).ToList<Stations>();

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
        public Task<List<Tickets>> GetSearchTrain(string date, string fromstation, string tostation, string purposecode, string time, string tickTypes, bool isCanBuy)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                List<Tickets> ticks = new List<Tickets>();
                string url = ConfigurationManager.AppSettings["QueryTicketUrl"] + "?leftTicketDTO.train_date=" + date + "&leftTicketDTO.from_station=" + fromstation + "&leftTicketDTO.to_station=" + tostation + "&purpose_codes=" + purposecode;
                string result = httpHelper.GetResponseChartByGET(url);
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
                        //时间
                        int startTime = Convert.ToInt32(time.Substring(0, 5).Replace(":",""));
                        int endTime = Convert.ToInt32(time.Substring(time.LastIndexOf('-') + 1).Replace(":", ""));
                        foreach (var t in list)
                        {
                            string tickCode = t.stationtraincode.ToString();
                            tickCode = tickCode.Substring(0, 1);
                            int tickTime = Convert.ToInt32(t.starttime.ToString().Replace(":", ""));
                            bool IsCanBuytick=t.canWebBuy.ToString() == "Y" ? true : false;
                            if (tickTime >= startTime && tickTime <= endTime)
                            {
                                if (GetTickType(tickTypes, tickCode))
                                {
                                    if (isCanBuy && IsCanBuytick)
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
                                            IsCanBuy = IsCanBuytick,
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
                                    else if(!isCanBuy)
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
                                            IsCanBuy = IsCanBuytick,
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
                        }
                    }
                }
                return ticks;
            });
        }

        /// <summary>
        /// 帅选车次类型
        /// </summary>
        /// <param name="selTypes">选择的车次类型</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool GetTickType(string selTypes, string type)
        {
            if (selTypes == "QB")
            {
                return true;
            }
            var strArrTypes = selTypes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in strArrTypes)
            {
                if (t == type)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 保存联系人
        /// </summary>
        /// <param name="passengerName">乘客名</param>
        /// <returns></returns>
        public Task<bool> SaveContacts(string passengerName)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                bool isSuccess = false;
                var url = ConfigurationManager.AppSettings["ContactUrl"].ToString();
                Dictionary<string, string> param = new Dictionary<string, string>();
                param.Add("pageIndex", "1");
                param.Add("pageSize", "9999");
                string result = httpHelper.GetResponseByPOST(url, param);
                if (result != "")
                {
                    JObject json = JObject.Parse(result);
                    string contactFile = "Contact";
                    SaveFile(contactFile, json.ToString());
                    isSuccess = true;
                }
                return isSuccess;
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
        public Task<string> GetTickPrice(string fromstationno, string _seatTypes, string tostationno, string traindate, string trainno)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                string result = "", url = ConfigurationManager.AppSettings["TickPriceUrl"] + "?train_no=" + trainno + "&from_station_no=" + fromstationno + "&to_station_no=" + tostationno + "&seat_types=" + _seatTypes + "&train_date=" + traindate;
                result = httpHelper.GetResponseChartByGET(url);
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
        /// 读取乘客
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public List<Contacts> ReadContacts(string fileName)
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
                string submitOrderRequestResult = httpHelper.GetResponseByPOST(orderRequestUrl, orderRequesParams);
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
                result = httpHelper.GetResponseByPOST(submitOrderTokenUrl, submitOrderTokenParams);
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
        /// 获取提交订单验证码并识别
        /// </summary>
        /// <returns></returns>
        public Task<Dictionary<BitmapImage, string>> GetSubmitOrderCode()
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                var url = ConfigurationManager.AppSettings["OrderSubmitCodeUrl"] + "&rand=randp";
                var data = httpHelper.GetResponseData(url);
                BitmapImage orderCodeImg = new BitmapImage();
                string strOrderCode = "";
                int count = 0;
                if (data != null)
                {
                    do
                    {
                        codeBuilder.Length = 0;
                        if (BasicOCR.GetCodeFromBuffer(1, data, data.Length, codeBuilder))
                        {
                            strOrderCode = codeBuilder.ToString();
                        }
                        count++;

                    } while (strOrderCode.Length != 4 && count < 10);
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        orderCodeImg.BeginInit();
                        orderCodeImg.StreamSource = ms;
                        orderCodeImg.CacheOption = BitmapCacheOption.OnLoad;
                        orderCodeImg.EndInit();
                        orderCodeImg.Freeze();
                    }
                }
                Dictionary<BitmapImage, string> dicOrderCode = new Dictionary<BitmapImage, string>()
                {
                    {orderCodeImg,strOrderCode}
                };
                return dicOrderCode;
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
                string result = httpHelper.GetResponseByPOST(orderCodeUrl, orderCodeParams);
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
                string checkOrderInfoResult = httpHelper.GetResponseByPOST(checkOrderInfoUrl, checkOrderParams);
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
                queueCountResult = httpHelper.GetResponseByPOST(queueCountUrl, dicQueueParams);
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
                confirmOrderResult = httpHelper.GetResponseByPOST(confirmOrderUrl, dicConfirmOrderParams);
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
                string responResult = httpHelper.GetResponseChartByGET(waitTimeUrl);
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
