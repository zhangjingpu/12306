using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainAssistant.Models
{
    /// <summary>
    /// 用户
    /// </summary>
    public class Users
    {
        /// <summary>
        /// 登录名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 自动登录
        /// </summary>
        public bool IsAutoLogin { get; set; }
    }
}
