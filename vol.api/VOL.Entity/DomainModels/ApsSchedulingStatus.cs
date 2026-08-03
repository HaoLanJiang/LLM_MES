using System;
using System.ComponentModel;
using System.Reflection;

namespace VOL.Entity.DomainModels
{
    /// <summary>
    /// 排产时间片的可用状态，与 Aps_Schedule_Time.status 的数据库值保持一致。
    /// </summary>
    public enum ApsScheduleTimeStatus : sbyte
    {
        [Description("可用")]
        Available = 1,

        [Description("停机")]
        Shutdown = 2,

        [Description("维护")]
        Maintenance = 3,

        [Description("休息")]
        Rest = 4
    }

    /// <summary>
    /// 二值启用标志，与数据库中的 0/1 值保持一致。
    /// </summary>
    public enum ApsEnabledStatus : sbyte
    {
        [Description("禁用")]
        Disabled = 0,

        [Description("启用")]
        Enabled = 1
    }

    /// <summary>
    /// 排产结果是否延期，与 Aps_Schedule_Result.IsDelay 的数据库值保持一致。
    /// </summary>
    public enum ApsDelayStatus : sbyte
    {
        [Description("未延期")]
        OnSchedule = 0,

        [Description("已延期")]
        Delayed = 1
    }

    /// <summary>
    /// 班次是否跨日，与 Aps_Shift.cross_day_flag 的数据库值保持一致。
    /// </summary>
    public enum ApsCrossDayStatus : sbyte
    {
        [Description("不跨日")]
        SameDay = 0,

        [Description("跨日")]
        CrossDay = 1
    }

    /// <summary>
    /// 班次类型，与 Aps_Shift.shift_type 当前服务校验的数据库值保持一致。
    /// </summary>
    public enum ApsShiftType : sbyte
    {
        [Description("白班")]
        DayShift = 0,

        [Description("夜班")]
        NightShift = 1
    }

    /// <summary>
    /// 工单及排产结果的业务状态。
    /// </summary>
    public enum ApsScheduleStatus
    {
        [Description("待排产")]
        Pending,

        [Description("排产中")]
        Scheduling,

        [Description("已排产")]
        Scheduled,

        [Description("已延期")]
        Delayed,

        [Description("已完成")]
        Completed,

        [Description("清厂")]
        Changeover,

        [Description("生产中")]
        InProduction,

        [Description("开工中")]
        WorkStarting,

        [Description("已开工")]
        Started,

        [Description("加工中")]
        Processing,

        [Description("执行中")]
        Executing,

        [Description("在制")]
        WorkInProgress
    }

    public static class EnumDescriptionExtensions
    {
        /// <summary>
        /// 获取枚举成员的 Description 特性值；未配置 Description 时返回枚举成员名称。
        /// </summary>
        public static string GetDescription<TEnum>(this TEnum value)
            where TEnum : struct, Enum
        {
            FieldInfo field = typeof(TEnum).GetField(value.ToString());
            return field?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? value.ToString();
        }
    }

    public static class ApsScheduleStatusExtensions
    {
        /// <summary>
        /// 获取用于持久化和 API 交互的状态文本，保持现有数据库契约不变。
        /// </summary>
        public static string ToStorageValue(this ApsScheduleStatus status)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }

            return status.GetDescription();
        }
    }
}
