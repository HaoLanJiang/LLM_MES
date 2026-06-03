/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *如果数据库字段发生变化，请在代码生器重新生成此Model
 */
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VOL.Entity.SystemModels;

namespace VOL.Entity.DomainModels
{
    [Entity(TableCnName = "排产班次表",TableName = "Aps_Shift")]
    public partial class Aps_Shift:BaseEntity
    {
        /// <summary>
       ///主键
       /// </summary>
       [Key]
       [Display(Name ="主键")]
       [Column(TypeName="bigint")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public long id { get; set; }

       /// <summary>
       ///班次编码
       /// </summary>
       [Display(Name ="班次编码")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string shift_code { get; set; }

       /// <summary>
       ///班次名称
       /// </summary>
       [Display(Name ="班次名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string shift_name { get; set; }

       /// <summary>
       ///班次类型：1白班，2夜班
       /// </summary>
       [Display(Name ="班次类型：1白班，2夜班")]
       [Column(TypeName="sbyte")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public sbyte shift_type { get; set; }

       /// <summary>
       ///开始时间
       /// </summary>
       [Display(Name ="开始时间")]
       [Column(TypeName="time")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public TimeSpan start_time { get; set; }

       /// <summary>
       ///结束时间
       /// </summary>
       [Display(Name ="结束时间")]
       [Column(TypeName="time")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public TimeSpan end_time { get; set; }

       /// <summary>
       ///是否跨天：0否，1是
       /// </summary>
       [Display(Name ="是否跨天：0否，1是")]
       [Column(TypeName="sbyte")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public sbyte cross_day_flag { get; set; }

       /// <summary>
       ///工作分钟数
       /// </summary>
       [Display(Name ="工作分钟数")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int work_minutes { get; set; }

       /// <summary>
       ///是否启用：0否，1是
       /// </summary>
       [Display(Name ="是否启用：0否，1是")]
       [Column(TypeName="sbyte")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public sbyte enable_flag { get; set; }

       /// <summary>
       ///备注
       /// </summary>
       [Display(Name ="备注")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       public string remark { get; set; }

       /// <summary>
       ///创建时间
       /// </summary>
       [Display(Name ="创建时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime create_time { get; set; }

       /// <summary>
       ///更新时间
       /// </summary>
       [Display(Name ="更新时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime update_time { get; set; }

       
    }
}