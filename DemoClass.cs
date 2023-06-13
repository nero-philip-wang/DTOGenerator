using codeGenerate;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace DemoClass
{
    /// <summary>
    /// 人员
    /// </summary>
    public class Person : Exception
    {
        /// <summary>
        /// 名称
        /// </summary>
        [In("PersonInDTO")]
        [Out("PersonListDTO")]
        [MaxLength(60)]
        public string Name { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        [In("PersonUpdateDTO")]
        [Out("PersonListDTO|int")]
        public int? Age { get; set; }
    }
}