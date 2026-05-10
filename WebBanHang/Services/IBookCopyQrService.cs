namespace WebBanHang.Services
{
    public interface IBookCopyQrService
    {
        /// <summary>Gán CopyCode/QrPayload theo Id, sinh PNG, cập nhật đường dẫn.</summary>
        Task FinalizeNewBookCopyAsync(int bookCopyId, CancellationToken cancellationToken = default);

        Task RegenerateBookCopyQrAsync(int bookCopyId, CancellationToken cancellationToken = default);

        void TryDeleteQrFile(string? webRelativePath);
    }
}
