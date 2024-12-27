using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebUı.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using WebUı.ViewModel;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using WebUı.Hubs;

namespace WebUı.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly INotyfService _notyfService;
        private readonly AppDbContext _context;

        private readonly IHubContext<CommentHub> _hubContext;

        public HomeController(ILogger<HomeController> logger, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<AppRole> roleManager, INotyfService notyfService, AppDbContext context, IHubContext<CommentHub> hubContext)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _notyfService = notyfService;
            _context = context;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            var categories = _context.Categories
                .Include(c => c.News.Where(n => n.IsActive))
                .Where(c => c.IsActive)
                .ToList();

            return View(categories);
        }

        // Kategori detay sayfası için yeni action
        public IActionResult CategoryNews(int categoryId)
        {
            var category = _context.Categories
                .Include(c => c.News.Where(n => n.IsActive))
                .FirstOrDefault(c => c.Id == categoryId);

            if (category == null)
                return NotFound();

            return View(category);
        }

        public async Task<IActionResult> NewsDetail(int id)
        {
            var news = await _context.News
                .Include(n => n.Comments)
                    .ThenInclude(c => c.User)
                .Include(n => n.Category)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (news == null)
                return NotFound();

            // Görüntülenme sayısını artır
            news.ViewCount++;
            await _context.SaveChangesAsync();

            return View(news);
        }

        [HttpPost]
        public async Task<JsonResult> AddComment([FromBody] CommentViewModel model)
        {
            if (string.IsNullOrEmpty(model.CommentText))
            {
                return Json(new { success = false, message = "Yorum alanı boş bırakılamaz!" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Giriş yapmalısınız!" });
            }

            var news = await _context.News.FirstOrDefaultAsync(n => n.Id == model.NewsId);

            var comment = new Comment
            {
                NewsId = model.NewsId,
                UserId = user.Id,
                Content = model.CommentText,
                CreatedDate = DateTime.Now
            };

            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();

            // SignalR ile tüm bağlı clientlara yeni yorumu gönder
            await _hubContext.Clients.All.SendAsync("ReceiveComment",
                user.Email,
                model.CommentText,
                DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                news?.Title ?? "");

            return Json(new { success = true });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

     


        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
                    if (result.Succeeded)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        if (roles.Contains("Admin"))
                        {
                            return RedirectToAction("Index", "Admin"); // Yapılacak
                        }
                        else if (roles.Contains("Gazetici"))
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }
                _notyfService.Warning("Email veya şifre hatalı");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

               


                if (result.Succeeded)
                {
                    // Varsayılan olarak "User" rolünü atayalım
                    await _userManager.AddToRoleAsync(user, "User");
                    await _signInManager.SignInAsync(user, false);
                    _notyfService.Success("Kayıt işlemi başarılı bir şekilde gerçekleşti.");
                  return RedirectToAction("Index", "Home");

                  
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                    _notyfService.Warning(error.Description);
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
