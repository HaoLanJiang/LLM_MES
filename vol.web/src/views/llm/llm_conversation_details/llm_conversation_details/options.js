// *Author：jxx
// *Contact：283591387@qq.com
// *代码由框架生成,任何更改都可能导致被代码生成器覆盖
export default function(){
    const table = {
        key: 'Id',
        footer: "Foots",
        cnName: '大模型会话详情',
        name: 'llm_conversation_details',
        url: "/llm_conversation_details/",
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
    const columns = [{field:'CreateID',title:'创建人',type:'int',width:120,hidden:true,align:'left'},
                       {field:'Creator',title:'创建人名称',type:'string',width:120,align:'left'},
                       {field:'CreateDate',title:'创建时间',type:'datetime',width:120,align:'left'},
                       {field:'ModifyID',title:'更新人',type:'int',width:120,hidden:true,align:'left'},
                       {field:'Modifier',title:'更新人名称',type:'string',width:120,align:'left'},
                       {field:'ModifyDate',title:'更新时间',type:'datetime',width:120,align:'left'},
                       {field:'Id',title:'主键',type:'string',width:120,hidden:true,readonly:true,require:true,align:'left'},
                       {field:'llm_conversatio_id',title:'llm_conversatio_id',type:'string',width:120,align:'left'}];
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