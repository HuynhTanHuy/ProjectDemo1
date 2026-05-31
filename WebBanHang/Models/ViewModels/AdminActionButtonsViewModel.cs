namespace WebBanHang.Models.ViewModels
{
    public static class AdminActionLabels
    {
        public const string Detail = "Xem chi tiết";
        public const string Edit = "Chỉnh sửa";
        public const string Delete = "Xóa";
    }

    public class AdminActionButtonsViewModel
    {
        public string Area { get; set; } = "Admin";

        public string Controller { get; set; } = string.Empty;

        public object? Id { get; set; }

        public bool ShowDetail { get; set; } = true;

        public bool ShowEdit { get; set; } = true;

        public bool ShowDelete { get; set; } = true;

        public bool DetailDisabled { get; set; }

        public bool EditDisabled { get; set; }

        public bool DeleteDisabled { get; set; }

        public string DetailTitle { get; set; } = AdminActionLabels.Detail;

        public string EditTitle { get; set; } = AdminActionLabels.Edit;

        public string DeleteTitle { get; set; } = AdminActionLabels.Delete;

        public string? DetailAction { get; set; }

        public string? EditAction { get; set; }

        public string? DeleteAction { get; set; }

        /// <summary>Liên kết chi tiết tùy chỉnh (vd. Customer/Product/Details).</summary>
        public string? DetailUrl { get; set; }

        public bool DetailOpenInNewTab { get; set; }

        /// <summary>Mở modal chi tiết thay vì navigate (vd. #detailModal).</summary>
        public string? DetailModalTarget { get; set; }

        /// <summary>Xóa qua modal xác nhận thay vì trang Delete.</summary>
        public bool DeleteUsesModal { get; set; }

        public string DeleteModalTarget { get; set; } = "#deleteModal";

        public string? DeleteTriggerClass { get; set; }

        public Dictionary<string, string>? DeleteModalData { get; set; }
    }
}
