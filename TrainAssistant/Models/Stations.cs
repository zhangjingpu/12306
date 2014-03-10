using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainAssistant.Models
{
    /// <summary>
    /// 站名实体
    /// </summary>
    public class Stations
    {
        /// <summary>
        /// 编号
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// 拼音
        /// </summary>
        public string PinYin { get; set; }

        /// <summary>
        /// 首字母
        /// </summary>
        public string SZiMu { get; set; }

        /// <summary>
        /// 代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 中文名称
        /// </summary>
        public string ZHName { get; set; }
    }
}
