// *Author：jxx
// *Contact：283591387@qq.com
// *代码由框架生成,任何更改都可能导致被代码生成器覆盖
export default function(){
    const table = {
        key: 'Id',
        footer: "Foots",
        cnName: '排产结果表',
        name: 'aps_schedule_result',
        url: "/aps_schedule_result/",
        sortName: "CreateDate"
    };
    const tableName = table.name;
    const tableCNName = table.cnName;
    const newTabEdit = false;
    const key = table.key;
    const editFormFields = {};
    const editFormOptions = [];
    const searchFormFields = {};
    const searchFormOptions = [];
    const columns = [{field:'Id',title:'主键GUID',type:'string',width:110,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'WorkOrderId',title:'工单ID',type:'string',width:110,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'WorkOrderNo',title:'工单号',type:'string',width:110,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'MachineCode',title:'设备编码',type:'string',width:110,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'MachineName',title:'设备名称',type:'string',width:120,align:'left'},
                       {field:'PlanStartTime',title:'计划开始时间',type:'datetime',width:150,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'PlanEndTime',title:'计划结束时间',type:'datetime',width:150,require:true,align:'left'},
                       {field:'PlanMinutes',title:'计划生产分钟数',type:'int',width:110,require:true,align:'left'},
                       {field:'OrderQty',title:'订单数量',type:'decimal',width:110,require:true,align:'left'},
                       {field:'CustomerName',title:'客户',type:'string',width:120,align:'left'},
                       {field:'CustomerPriority',title:'客户优先级',type:'int',width:110,align:'left'},
                       {field:'EarliestStartTime',title:'工单最早开始时间',type:'datetime',width:150,align:'left'},
                       {field:'LatestDeliveryDate',title:'工单最晚交付日期',type:'datetime',width:150,align:'left'},
                       {field:'IsDelay',title:'是否延期：0否，1是',type:'sbyte',width:110,hidden:true,readonly:true,align:'left'},
                       {field:'DelayMinutes',title:'延期分钟数',type:'int',width:110,align:'left'},
                       {field:'ScheduleStatus',title:'排产状态',type:'string',width:110,align:'left'},
                       {field:'Remark',title:'备注',type:'string',width:220,align:'left'},
                       {field:'CreateID',title:'创建人',type:'int',width:100,hidden:true,align:'left'},
                       {field:'Creator',title:'创建人名称',type:'string',width:100,align:'left'},
                       {field:'CreateDate',title:'创建时间',type:'datetime',width:150,align:'left'},
                       {field:'ModifyID',title:'更新人',type:'int',width:100,hidden:true,align:'left'},
                       {field:'Modifier',title:'更新人名称',type:'string',width:100,align:'left'},
                       {field:'ModifyDate',title:'更新时间',type:'datetime',width:150,align:'left'}];
    const detail ={columns:[]};
    const details = [];

    return {
        table,
        key,
        tableName,
        tableCNName,
        newTabEdit,
        editFormFields,
        editFormOptions,
        searchFormFields,
        searchFormOptions,
        columns,
        detail,
        details
    };
}