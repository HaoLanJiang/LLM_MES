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
    [Entity(TableCnName = "排产订单表",TableName = "Aps_Work_Order")]
    public partial class Aps_Work_Order:BaseEntity
    {
        /// <summary>
       ///
       /// </summary>
       [Key]
       [Display(Name ="Id")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Required(AllowEmptyStrings=false)]
       public Guid Id { get; set; }

       /// <summary>
       ///工单号
       /// </summary>
       [Display(Name ="工单号")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string WorkOrderNo { get; set; }

       /// <summary>
       ///客户
       /// </summary>
       [Display(Name ="客户")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Required(AllowEmptyStrings=false)]
       public string CustomerName { get; set; }

       /// <summary>
       ///客户优先级：1最高，5最低
       /// </summary>
       [Display(Name ="客户优先级：1最高，5最低")]
       [Column(TypeName="int")]
       public int? CustomerPriority { get; set; }

       /// <summary>
       ///产品编码
       /// </summary>
       [Display(Name ="产品编码")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       public string ProductCode { get; set; }

       /// <summary>
       ///产品名称
       /// </summary>
       [Display(Name ="产品名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       public string ProductName { get; set; }

       /// <summary>
       ///订单数量
       /// </summary>
       [Display(Name ="订单数量")]
       [DisplayFormat(DataFormatString="18,2")]
       [Column(TypeName="decimal")]
       [Required(AllowEmptyStrings=false)]
       public decimal OrderQty { get; set; }

       /// <summary>
       ///最早开始时间
       /// </summary>
       [Display(Name ="最早开始时间")]
       [Column(TypeName="datetime")]
       [Required(AllowEmptyStrings=false)]
       public DateTime EarliestStartTime { get; set; }

       /// <summary>
       ///最晚交付日期
       /// </summary>
       [Display(Name ="最晚交付日期")]
       [Column(TypeName="datetime")]
       [Required(AllowEmptyStrings=false)]
       public DateTime LatestDeliveryDate { get; set; }

       /// <summary>
       ///预计加工总分钟数
       /// </summary>
       [Display(Name ="预计加工总分钟数")]
       [Column(TypeName="int")]
       [Required(AllowEmptyStrings=false)]
       public int ProcessMinutes { get; set; }

       /// <summary>
       ///指定/适用设备
       /// </summary>
       [Display(Name ="指定/适用设备")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       public string RequiredMachine { get; set; }

       /// <summary>
       ///换型分组
       /// </summary>
       [Display(Name ="换型分组")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       public string ChangeoverGroup { get; set; }

       /// <summary>
       ///排产状态
       /// </summary>
       [Display(Name ="排产状态")]
       [MaxLength(20)]
       [Column(TypeName="nvarchar(20)")]
       public string ScheduleStatus { get; set; }

       /// <summary>
       ///备注
       /// </summary>
       [Display(Name ="备注")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       public string Remark { get; set; }

       /// <summary>
       ///创建人
       /// </summary>
       [Display(Name ="创建人")]
       [Column(TypeName="int")]
       public int? CreateID { get; set; }

       /// <summary>
       ///创建人名称
       /// </summary>
       [Display(Name ="创建人名称")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       public string Creator { get; set; }

       /// <summary>
       ///创建时间
       /// </summary>
       [Display(Name ="创建时间")]
       [Column(TypeName="datetime")]
       public DateTime? CreateDate { get; set; }

       /// <summary>
       ///更新人
       /// </summary>
       [Display(Name ="更新人")]
       [Column(TypeName="int")]
       public int? ModifyID { get; set; }

       /// <summary>
       ///更新人名称
       /// </summary>
       [Display(Name ="更新人名称")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       public string Modifier { get; set; }

       /// <summary>
       ///更新时间
       /// </summary>
       [Display(Name ="更新时间")]
       [Column(TypeName="datetime")]
       public DateTime? ModifyDate { get; set; }

       
    }
}