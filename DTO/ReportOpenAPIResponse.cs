using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTools.DTO
{
    public class ReportOpenAPIResponse
    {
        /// <summary>
        /// 报表名称
        /// </summary>
        public string ReportName { get; set; }

        /// <summary>
        /// 业务数据
        /// </summary>
        public ResponseDataRows DataSource { get; set; }

        /// <summary>
        /// 栏目元数据
        /// </summary>
        public ResponseDataRows ColumnSource { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int Pages { get; set; }

        /// <summary>
        /// 当前页
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// 每页显示记录数
        ///     取值范围：>=0，当值为零时表示不分页
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// TaskSessionID
        /// </summary>
        public string TaskSessionID { get; set; }

        /// <summary>
        /// 报表方案ID
        /// </summary>
        public string SolutionID { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 错误编码
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// 本次消耗时间
        /// </summary>
        public int ElapsedTime { get; set; }
    }
    public class ResponseDataRows 
    {
        public List<Object> Rows { get; set; }
    }
}
