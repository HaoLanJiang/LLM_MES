// *Author：jxx
// *Contact：283591387@qq.com
// *代码由框架生成,任何更改都可能导致被代码生成器覆盖
export default function(){
    const table = {
        key: 'Id',
        footer: "Foots",
        cnName: 'APS设备表',
        name: 'aps_machine',
        url: "/aps_machine/",
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
    const columns = [{field:'Id',title:'Id',type:'string',width:110,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'MachineCode',title:'设备编码',type:'string',width:110,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'MachineName',title:'设备名称',type:'string',width:120,require:true,align:'left'},
                       {field:'CapacityMinutesPerDay',title:'每日产能分钟',type:'int',width:110,align:'left'},
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