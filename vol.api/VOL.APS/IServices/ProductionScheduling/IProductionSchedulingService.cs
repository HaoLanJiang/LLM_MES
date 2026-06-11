using VOL.Core.Utilities;
using VOL.DTO.Aps_ProductionScheduling;

namespace VOL.APS.IServices
{
    public interface IProductionSchedulingService
    {
        /// <summary>
        /// 执行生产排产，并将结果写入排产结果表。
        /// </summary>
        /// <param name="input">排产参数，包含排序模式、规则顺序、开始日期以及指定工单范围。</param>
        /// <returns>返回排产执行结果及统计信息。</returns>
        WebResponseContent RunProductionScheduling(RunProductionSchedulingInputDto input);
    }
}
