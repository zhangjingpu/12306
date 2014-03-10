using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainAssistant.Models
{
    /// <summary>
    /// 车票
    /// </summary>
    public class Tickets
    {
        /// <summary>
        /// 车次编号
        /// </summary>
        public string TrainNo { get; set; }

        /// <summary>
        /// 车次
        /// </summary>
        public string TrainName { get; set; }

        /// <summary>
        /// 始发站代码
        /// </summary>
        public string StartStationCode { get; set; }

        /// <summary>
        /// 始发站名称
        /// </summary>
        public string StartStationName { get; set; }

        /// <summary>
        /// 终点站代码
        /// </summary>
        public string EndStationCode { get; set; }

        /// <summary>
        /// 终点站名称
        /// </summary>
        public string EndStationName { get; set; }

        /// <summary>
        /// 出发站代码
        /// </summary>
        public string FromStationCode { get; set; }

        /// <summary>
        /// 出发站名称
        /// </summary>
        public string FromStationName { get; set; }

        /// <summary>
        /// 目的地代码
        /// </summary>
        public string ToStationCode { get; set; }

        /// <summary>
        /// 目的地名称
        /// </summary>
        public string ToStationName { get; set; }

        /// <summary>
        /// 出发时间
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// 到达时间
        /// </summary>
        public string ArriveTime { get; set; }

        /// <summary>
        /// 出发
        /// </summary>
        public string From{ get; set; }

        /// <summary>
        /// 到达
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// 天数
        /// </summary>
        public string DayDifference { get; set; }

        /// <summary>
        /// 历时
        /// </summary>
        public string LiShi { get; set; }

        /// <summary>
        /// 历时天
        /// </summary>
        public string LiShiDay { get; set; }

        /// <summary>
        /// 列车类型名称
        /// </summary>
        public string TrainClassName { get; set; }

        /// <summary>
        /// 是否可以预订
        /// </summary>
        public bool IsCanBuy { get; set; }

        /// <summary>
        /// 历时值
        /// </summary>
        public string LiShiValue { get; set; }

        public string YPInfo { get; set; }

        public string ControlTrainDay { get; set; }

        /// <summary>
        /// 发车日期
        /// </summary>
        public string StartTrainDate{ get; set; }

        /// <summary>
        /// 席别特征
        /// </summary>
        public string SeatFeature { get; set; }

        public string YPEx { get; set; }

        public string TrainSeatFeature { get; set; }

        /// <summary>
        /// 席别
        /// </summary>
        public string SeatTypes { get; set; }

        public string LocationCode { get; set; }

        /// <summary>
        /// 出发站编号
        /// </summary>
        public string FromStationNo { get; set; }

        /// <summary>
        /// 目的地编号
        /// </summary>
        public string ToStationNo { get; set; }

        /// <summary>
        /// 日期差
        /// </summary>
        public string ControlDay { get; set; }

        /// <summary>
        /// 预订起售时间
        /// </summary>
        public string SaleTime { get; set; }

        public string IsSupportCard { get; set; }

        public string GGNum { get; set; }

        /// <summary>
        /// 高级软卧
        /// </summary>
        public string GRNum { get; set; }

        /// <summary>
        /// 其他
        /// </summary>
        public string QTNum { get; set; }

        /// <summary>
        /// 软卧
        /// </summary>
        public string RWNum { get; set; }

        /// <summary>
        /// 软座
        /// </summary>
        public string RZNum { get; set; }

        /// <summary>
        /// 特等座
        /// </summary>
        public string TZNum { get; set; }

        /// <summary>
        /// 无座
        /// </summary>
        public string WZNum { get; set; }

        public string YBNum { get; set; }

        /// <summary>
        /// 硬卧
        /// </summary>
        public string YWNum { get; set; }

        /// <summary>
        /// 硬座
        /// </summary>
        public string YZNum { get; set; }

        /// <summary>
        /// 二等座
        /// </summary>
        public string ZENum { get; set; }

        /// <summary>
        /// 一等座
        /// </summary>
        public string ZYNum { get; set; }

        /// <summary>
        /// 商务座
        /// </summary>
        public string SWZNum { get; set; }

        public string SecretStr { get; set; }
    }
}
