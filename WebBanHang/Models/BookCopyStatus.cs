namespace WebBanHang.Models
{
    /// <summary>Trạng thái vật lý của bản sao. Trạng thái mượn do Borrow quản lý.</summary>
    public enum BookCopyStatus
    {
        Active = 0,
        Lost = 1,
        Disposed = 2
    }
}
