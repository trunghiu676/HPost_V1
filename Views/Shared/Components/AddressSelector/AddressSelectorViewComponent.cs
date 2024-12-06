using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Models.Provinces;
using App.Models;

namespace App.ViewComponents
{
    public class AddressSelectorViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public AddressSelectorViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string? selectedProvinceCode, string? selectedDistrictCode, string? selectedWardCode)
        {
            // Lấy danh sách tỉnh/thành
            var provinces = await _context.Provinces
                .Select(p => new { p.Code, p.FullName })
                .ToListAsync();

            // Danh sách quận/huyện dựa trên tỉnh đã chọn
            var districts = !string.IsNullOrEmpty(selectedProvinceCode)
                ? await _context.Districts
                    .Where(d => d.ProvinceCode == selectedProvinceCode)
                    .Select(d => new { d.Code, d.FullName })
                    .ToListAsync()
                : null;

            // Danh sách xã/phường dựa trên quận đã chọn
            var wards = !string.IsNullOrEmpty(selectedDistrictCode)
                ? await _context.Wards
                    .Where(w => w.DistrictCode == selectedDistrictCode)
                    .Select(w => new { w.Code, w.FullName })
                    .ToListAsync()
                : null;

            // Truyền dữ liệu sang View
            ViewBag.Provinces = provinces;
            ViewBag.Districts = districts;
            ViewBag.Wards = wards;
            ViewBag.SelectedProvinceCode = selectedProvinceCode;
            ViewBag.SelectedDistrictCode = selectedDistrictCode;
            ViewBag.SelectedWardCode = selectedWardCode;

            return View();
        }
    }

}