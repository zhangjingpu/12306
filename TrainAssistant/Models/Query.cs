using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainAssistant.Models
{
    public class Query
    {
        /// <summary>
        /// 用户
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// 出发地
        /// </summary>
        public string FromName { get; set; }
        /// <summary>
        /// 出发地代码
        /// </summary>
        public string FromCode { get; set; }
        /// <summary>
        /// 目的地
        /// </summary>
        public string ToName { get; set; }
        /// <summary>
        /// 目的地代码
        /// </summary>
        public string ToCode { get; set; }
        /// <summary>
        /// 出发日期
        /// </summary>
        public string Date { get; set; }
    }
}
