using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUı.Models;

namespace WebUı.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly INotyfService _notyfService;
        private readonly AppDbContext _appDbContext;

        public AdminController(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, INotyfService notyfService, AppDbContext appDbContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _notyfService = notyfService;
            _appDbContext = appDbContext;
        }

        public IActionResult Index()
        {
            var recentComments = _appDbContext.Comments
           .Include(c => c.User)
           .Include(c => c.News)
           .OrderByDescending(c => c.CreatedDate)
           .Take(10)
           .ToList();

            return View(recentComments);
        }
        [Authorize(Roles = "Admin")]
        public IActionResult UsersList()
        {
            return View();
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userList = new List<object>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userList.Add(new
                    {
                        id = user.Id,
                        email = user.Email,
                        userName = user.UserName,
                        roles = roles
                    });
                }

                return Json(new { data = userList }); // DataTables "data" property'sini bekler
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateUserRole(string userId, string newRole)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "Kullanıcı bulunamadı!" });

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, newRole);

                _notyfService.Success("Rol başarıyla güncellendi!");    
                return Json(new { success = true, message = "Rol başarıyla güncellendi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "Kullanıcı bulunamadı!" });

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded) {
                    _notyfService.Success("Kullanıcı başarıyla Silindi!");
                    return Json(new { success = true, message = "Kullanıcı başarıyla silindi!" });
                }
                else
                    return Json(new { success = false, message = "Kullanıcı silinemedi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

    }
}
