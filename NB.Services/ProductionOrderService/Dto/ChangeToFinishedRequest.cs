namespace NB.Service.ProductionOrderService.Dto
{
    /// <summary>
    /// Request cho việc phê duyệt đơn sản xuất sang trạng thái Finished
    /// </summary>
    public class ChangeToFinishedRequest
    {
        /// <summary>
        /// Ghi chú của Manager khi phê duyệt (tùy chọn)
        /// </summary>
        public string? Note { get; set; }
    }
}
