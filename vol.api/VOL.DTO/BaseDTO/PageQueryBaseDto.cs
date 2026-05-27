namespace VOL.DTO.BaseDTO
{
    /// <summary>
    /// 分页查询通用入参
    /// </summary>
    public class PageQueryBaseDto
    {
        /// <summary>
        /// 页码，从 1 开始
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int Rows { get; set; } = 30;

        /// <summary>
        /// 排序字段
        /// </summary>
        public string? Sort { get; set; }

        /// <summary>
        /// 排序方向，asc/desc
        /// </summary>
        public string? Order { get; set; }
    }
}
