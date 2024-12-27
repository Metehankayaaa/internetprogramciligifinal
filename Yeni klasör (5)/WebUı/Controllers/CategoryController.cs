using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUı.Models;

namespace WebUı.Controllers
{
    [Authorize(Roles="Admin")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyfService;

        public CategoryController(AppDbContext context, INotyfService notyfService)
        {
            _context = context;
            _notyfService = notyfService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = _context.Categories.Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                x.IsActive,
                CreatedDate = x.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                NewsCount = x.News.Count
            }).ToList();

            return Json(new { data = categories });
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory([FromBody] Category category)
        {
            try
            {
             
                category.CreatedDate = DateTime.Now;
                _context.Categories.Add(category);
                _context.SaveChanges();
                
                _notyfService.Success("Kategori başarıyla eklendi");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = _context.Categories.Find(id);
            return Json(category);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCategory([FromBody] Category category)
        {
            try
            {
                var existingCategory = _context.Categories.Find(category.Id);
                if (existingCategory == null)
                    return Json(new { success = false, message = "Kategori bulunamadı" });

                existingCategory.Name = category.Name;
                existingCategory.Description = category.Description;
                existingCategory.IsActive = category.IsActive;

                _context.SaveChanges();
                _notyfService.Success("Kategori başarıyla güncellendi");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = _context.Categories.Find(id);
                if (category == null)
                    return Json(new { success = false, message = "Kategori bulunamadı" });

                _context.Categories.Remove(category);
                _context.SaveChanges();
                _notyfService.Success("Kategori başarıyla silindi");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
