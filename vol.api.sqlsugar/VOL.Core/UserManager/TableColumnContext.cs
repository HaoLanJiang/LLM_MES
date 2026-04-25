using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VOL.Core.DbSqlSugar;
using VOL.Entity.DomainModels;

namespace VOL.Core.UserManager
{
    public static class TableColumnContext
    {
        private static object _colObject = new object();
        private static List<TableColumnField> _columnData = null;

        public static List<TableColumnField> Data
        {
            get
            {
                if (_columnData == null)
                {
                    lock (_colObject)
                    {
                        if (_columnData == null)
                        {
                            _columnData = DbManger.SqlSugarClient.Queryable<Sys_TableColumn>().Select(s => new TableColumnField
                            {
                                ColumnCnName = s.ColumnCnName,
                                ColumnName = s.ColumnName,
                                ColumnType = s.ColumnType,
                                IsDisplay = s.IsDisplay ?? 1,
                                TableName = s.TableName

                            }).ToList();
                        }
                    }
                }
                return _columnData;
            }
        }
        private static object _tableObject = new object();
        private static List<TableInfo> _tableData = null;
        public static void Reload()
        {
            _tableData = null;
            _columnData = null;
        }
        public static List<TableInfo> TableInfo
        {
            get
            {
                if (_tableData != null)
                {
                    return _tableData;
                }
                lock (_tableObject)
                {
                    if (_tableData == null)
                    {
                        _tableData = DbManger.SqlSugarClient.Queryable<Sys_TableInfo>().Select(s => new TableInfo()
                        {
                            ColumnCNName = s.ColumnCNName,
                            TableTrueName = s.TableTrueName,
                            TableName = s.TableName,
                            DetailCnName = s.DetailCnName,
                            SortName = s.SortName,
                            DetailName = s.DetailName,
                            Table_Id = s.Table_Id,
                        }).ToList();
                    }
                }
                return _tableData;
            }
        }

        /// <summary>
        /// 获取表隐藏的字段
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static List<string> GetTableHideFields(string table)
        {
            return Data.Where(x => x.TableName == table && x.IsDisplay == 0).Select(s => s.ColumnName).ToList();
        }

    }

    public class TableColumnField
    {
        /// <summary>
        ///
        /// <summary>
        [Display(Name = "")]
        [Editable(true)]
        public string ColumnName { get; set; }

        /// <summary>
        ///
        /// <summary>
        [Display(Name = "")]
        [Editable(true)]
        public string ColumnCnName { get; set; }

        /// <summary>
        ///
        /// <summary>
        [Display(Name = "")]
        [Editable(true)]
        public string ColumnType { get; set; }

        /// <summary>
        ///
        /// <summary>
        [Display(Name = "")]
        [Editable(true)]
        public string TableName { get; set; }

        /// <summary>
        ///
        /// <summary>
        [Display(Name = "")]
        [Column(TypeName = "int")]
        [Editable(true)]
        public int? IsDisplay { get; set; }
    }

    public class TableInfo
    {
        [Column(TypeName = "int")]
        public int Table_Id { get; set; }

        [Editable(true)]
        public string TableName { get; set; }
        public string SortName { get; set; }
        [Editable(true)]
        public string TableTrueName { get; set; }
        [Editable(true)]
        public string ColumnCNName { get; set; }

        /// <summary>
        /// 与主表关联的字段
        /// </summary>
        [Editable(true)]
        public string ForeignField { get; set; }

        /// <summary>
        /// 显示所有查询条件
        /// </summary>
        [Editable(true)]
        public int? FixedSearch { get; set; }



        /// <summary>
        /// 启用日志审计
        /// </summary>
        [Editable(true)]
        public int? ActionLog { get; set; }

        public string DetailCnName { get; set; }

        public string DetailName { get; set; }

    }
}
