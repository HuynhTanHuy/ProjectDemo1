using Microsoft.AspNetCore.Mvc;
using WebBanHang.Models.ViewModels;

namespace WebBanHang.ViewComponents
{
    public class AdminActionButtonsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(AdminActionButtonsViewModel model)
        {
            return View(model);
        }
    }
}
