using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainAssistant.Models
{
    /// <summary>
    /// 联系人
    /// </summary>
    public class Contacts
    {
        /// <summary>
        /// 地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 出生年月
        /// </summary>
        public string BornDate {get;set;}

        /// <summary>
        /// 序号
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 国家代码
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 是否是用户自己
        /// </summary>
        public string IsUserSelf { get; set; }

        /// <summary>
        /// 手机号码
        /// </summary>
        public string Mobile { get; set; }

        /// <summary>
        /// 乘客标志
        /// </summary>
        public string PassengerFlag { get; set; }

        /// <summary>
        /// 乘客身份证（二代）
        /// </summary>
        public string PassengerIdNo { get; set; }

        /// <summary>
        /// 乘客身份证类型代码（二代）
        /// </summary>
        public string PassengerIdTypeCode { get; set; }

        /// <summary>
        /// 乘客身份证类型名称（二代）
        /// </summary>
        public string PassengerIdTypeName { get; set; }

        /// <summary>
        /// 乘客名称
        /// </summary>
        public string PassengerName { get; set; }

        /// <summary>
        /// 乘客类型
        /// </summary>
        public string PassengerType { get; set; }

        /// <summary>
        /// 乘客类型名称
        /// </summary>
        public string PassengerTypeName { get; set; }

        /// <summary>
        /// 电话号码
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// 邮编
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string RecordCount { get; set; }

        /// <summary>
        /// 性别代码
        /// </summary>
        public string SexCode { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        public string SexName { get; set; }

    }
}
