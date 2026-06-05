namespace VOL.DTO.Aps_Work_Order
{
    /// <summary>
    /// 生成工单测试数据入参
    /// </summary>
    public class CreateApsWorkOrderTestDataInputDto
    {
        /// <summary>
        /// 生成数量，默认10条，最大200条
        /// </summary>
        public int Count { get; set; } = 10;
    }
}
