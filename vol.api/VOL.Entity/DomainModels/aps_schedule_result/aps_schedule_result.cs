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
    [Entity(TableCnName = "排产结果表",TableName = "aps_schedule_result")]
    public partial class aps_schedule_result:BaseEntity
    {
        /// <summary>
       ///主键GUID
       /// </summary>
       [Key]
       [Display(Name ="主键GUID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid Id { get; set; }

       /// <summary>
       ///工单ID
       /// </summary>
       [Display(Name ="工单ID")]
       [MaxLength(36)]
       [Column(TypeName="uniqueidentifier")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public Guid WorkOrderId { get; set; }

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
       ///设备编码
       /// </summary>
       [Display(Name ="设备编码")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public string MachineCode { get; set; }

       /// <summary>
       ///设备名称
       /// </summary>
       [Display(Name ="设备名称")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string MachineName { get; set; }

       /// <summary>
       ///计划开始时间
       /// </summary>
       [Display(Name ="计划开始时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime PlanStartTime { get; set; }

       /// <summary>
       ///计划结束时间
       /// </summary>
       [Display(Name ="计划结束时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public DateTime PlanEndTime { get; set; }

       /// <summary>
       ///计划生产分钟数
       /// </summary>
       [Display(Name ="计划生产分钟数")]
       [Column(TypeName="int")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public int PlanMinutes { get; set; }

       /// <summary>
       ///订单数量
       /// </summary>
       [Display(Name ="订单数量")]
       [DisplayFormat(DataFormatString="18,2")]
       [Column(TypeName="decimal")]
       [Editable(true)]
       [Required(AllowEmptyStrings=false)]
       public decimal OrderQty { get; set; }

       /// <summary>
       ///客户
       /// </summary>
       [Display(Name ="客户")]
       [MaxLength(100)]
       [Column(TypeName="nvarchar(100)")]
       [Editable(true)]
       public string CustomerName { get; set; }

       /// <summary>
       ///客户优先级
       /// </summary>
       [Display(Name ="客户优先级")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? CustomerPriority { get; set; }

       /// <summary>
       ///工单最早开始时间
       /// </summary>
       [Display(Name ="工单最早开始时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? EarliestStartTime { get; set; }

       /// <summary>
       ///工单最晚交付日期
       /// </summary>
       [Display(Name ="工单最晚交付日期")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? LatestDeliveryDate { get; set; }

       /// <summary>
       ///是否延期：0否，1是
       /// </summary>
       [Display(Name ="是否延期：0否，1是")]
       [Column(TypeName="sbyte")]
       [Editable(true)]
       public sbyte? IsDelay { get; set; }

       /// <summary>
       ///延期分钟数
       /// </summary>
       [Display(Name ="延期分钟数")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? DelayMinutes { get; set; }

       /// <summary>
       ///排产状态
       /// </summary>
       [Display(Name ="排产状态")]
       [MaxLength(20)]
       [Column(TypeName="nvarchar(20)")]
       [Editable(true)]
       public string ScheduleStatus { get; set; }

       /// <summary>
       ///备注
       /// </summary>
       [Display(Name ="备注")]
       [MaxLength(500)]
       [Column(TypeName="nvarchar(500)")]
       [Editable(true)]
       public string Remark { get; set; }

       /// <summary>
       ///创建人
       /// </summary>
       [Display(Name ="创建人")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? CreateID { get; set; }

       /// <summary>
       ///创建人名称
       /// </summary>
       [Display(Name ="创建人名称")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string Creator { get; set; }

       /// <summary>
       ///创建时间
       /// </summary>
       [Display(Name ="创建时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? CreateDate { get; set; }

       /// <summary>
       ///更新人
       /// </summary>
       [Display(Name ="更新人")]
       [Column(TypeName="int")]
       [Editable(true)]
       public int? ModifyID { get; set; }

       /// <summary>
       ///更新人名称
       /// </summary>
       [Display(Name ="更新人名称")]
       [MaxLength(50)]
       [Column(TypeName="nvarchar(50)")]
       [Editable(true)]
       public string Modifier { get; set; }

       /// <summary>
       ///更新时间
       /// </summary>
       [Display(Name ="更新时间")]
       [Column(TypeName="datetime")]
       [Editable(true)]
       public DateTime? ModifyDate { get; set; }

       
    }
}