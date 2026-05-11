// *Author：jxx
// *Contact：283591387@qq.com
// *代码由框架生成,任何更改都可能导致被代码生成器覆盖
export default function(){
    const table = {
        key: 'Id',
        footer: "Foots",
        cnName: '排产订单表',
        name: 'aps_work_order',
        url: "/aps_work_order/",
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
    const columns = [{field:'Id',title:'Id',type:'string',width:120,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'WorkOrderNo',title:'工单号',type:'string',width:120,require:true,align:'left'},
                       {field:'CustomerName',title:'客户',type:'string',width:120,require:true,align:'left'},
                       {field:'CustomerPriority',title:'客户优先级：1最高，5最低',type:'int',width:120,align:'left'},
                       {field:'ProductCode',title:'产品编码',type:'string',width:120,align:'left'},
                       {field:'ProductName',title:'产品名称',type:'string',width:120,align:'left'},
                       {field:'OrderQty',title:'订单数量',type:'decimal',width:120,require:true,align:'left'},
                       {field:'EarliestStartTime',title:'最早开始时间',type:'datetime',width:120,require:true,align:'left'},
                       {field:'LatestDeliveryDate',title:'最晚交付日期',type:'datetime',width:120,require:true,align:'left'},
                       {field:'ProcessMinutes',title:'预计加工总分钟数',type:'int',width:120,require:true,align:'left'},
                       {field:'RequiredMachine',title:'指定/适用设备',type:'string',width:120,align:'left'},
                       {field:'ChangeoverGroup',title:'换型分组',type:'string',width:120,align:'left'},
                       {field:'ScheduleStatus',title:'排产状态',type:'string',width:120,align:'left'},
                       {field:'Remark',title:'备注',type:'string',width:120,align:'left'},
                       {field:'CreateID',title:'创建人',type:'int',width:120,hidden:true,align:'left'},
                       {field:'Creator',title:'创建人名称',type:'string',width:120,align:'left'},
                       {field:'CreateDate',title:'创建时间',type:'datetime',width:120,align:'left'},
                       {field:'ModifyID',title:'更新人',type:'int',width:120,hidden:true,align:'left'},
                       {field:'Modifier',title:'更新人名称',type:'string',width:120,align:'left'},
                       {field:'ModifyDate',title:'更新时间',type:'datetime',width:120,align:'left'}];
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