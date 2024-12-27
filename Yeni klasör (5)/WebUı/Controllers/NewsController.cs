using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebUı.Models;

namespace WebUı.Controllers
{

    [Authorize(Roles= "Admin,Gazetici")]
    public class NewsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyfService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public NewsController(AppDbContext context, INotyfService notyfService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _notyfService = notyfService;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var news = await _context.News
                .Include(n => n.Category)
                .Include(n => n.User)
                .ToListAsync();
            return View(news);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(News news, IFormFile image)
        {

            if (image != null)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadDir = Path.Combine(wwwRootPath, "images", "news");

                // Klasör yoksa oluştur
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                // Benzersiz dosya adı oluştur
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                string filePath = Path.Combine(uploadDir, fileName);

                // Dosyayı kaydet
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                news.ImagePath = "/images/news/" + fileName;
            }


            news.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                news.CreatedDate = DateTime.Now;

                await _context.News.AddAsync(news);
                await _context.SaveChangesAsync();
                _notyfService.Success("Haber başarıyla eklendi");
             
            

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(news);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(news);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(News news, IFormFile? image)
        {

            var existingNews = await _context.News.FindAsync(news.Id);
            if (existingNews == null)
            {
                return NotFound();
            }

            if (image != null) // Yeni resim yüklendiyse
            {
                // Eski resmi sil
                if (!string.IsNullOrEmpty(existingNews.ImagePath))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingNews.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Yeni resmi kaydet
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadDir = Path.Combine(wwwRootPath, "images", "news");

                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                string filePath = Path.Combine(uploadDir, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                existingNews.ImagePath = "/images/news/" + fileName;
            }

            // Mevcut haberin bilgilerini güncelle
            existingNews.Title = news.Title;
            existingNews.Content = news.Content;
            existingNews.CategoryId = news.CategoryId;
            existingNews.IsActive = news.IsActive;
            existingNews.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            existingNews.CreatedDate = DateTime.Now;

            // Değişiklikleri kaydet
            _context.News.Update(existingNews);
            await _context.SaveChangesAsync();
            _notyfService.Success("Haber başarıyla güncellendi");

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return Json(new { success = false, message = "Haber bulunamadı" });
            }

            // Resmi sil
            if (!string.IsNullOrEmpty(news.ImagePath))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, news.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.News.Remove(news);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Haber başarıyla silindi" });
        }


    }
}
