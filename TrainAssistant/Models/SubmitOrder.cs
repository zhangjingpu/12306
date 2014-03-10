using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrainAssistant.Models;

namespace TrainAssistant.Models
{

    /// <summary>
    /// 提交订单
    /// </summary>
    public class SubmitOrder
    {
        /// <summary>
        /// 席别
        /// </summary>
        public Dictionary<string,string> SeatType { get; set; }

        /// <summary>
        /// 乘客名
        /// </summary>
        public string PassengerName { get; set; }

        /// <summary>
        /// 乘客手机号
        /// </summary>
        public string PassengerMobile { get; set; }

        /// <summary>
        /// 票种
        /// </summary>
        public Dictionary<string,string> TickType { get; set; }

        /// <summary>
        /// 证件类型
        /// </summary>
        public Dictionary<string, string> IDType { get; set; }

        /// <summary>
        /// 证件号码
        /// </summary>
        public string PassengerId { get; set; }

    }
}
