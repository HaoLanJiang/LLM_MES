/*
*所有关于Aps_Schedule_Result类的业务代码接口应在此处编写
*/
using VOL.Core.BaseProvider;
using VOL.Entity.DomainModels;
using VOL.Core.Utilities;
using System.Linq.Expressions;
using VOL.DTO.Aps_Schedule_Result;
namespace VOL.APS.IServices
{
    public partial interface IAps_Schedule_ResultService
    {
        PageGridData<ApsScheduleResultPageOutputDto> GetScheduleResultPageList(ApsScheduleResultPageQueryInputDto input);
    }
 }
