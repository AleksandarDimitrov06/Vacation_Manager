using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vacation_Manager.Models;

namespace Vacation_Manager.Controllers
{
   // [Authorize(Roles = "CEO")]
    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;

        public RoleController(RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        // Показване на всички роли
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            
            // Добавяме броя потребители във всяка роля за изгледа
            var roleList = new List<(IdentityRole Role, int UserCount)>();

            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
                roleList.Add((role, usersInRole.Count));
            }

            return View(roleList);
        }

        // Показване на потребители в дадена роля
        public async Task<IActionResult> UsersInRole(string roleId, string searchString)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return NotFound();
            }

            ViewBag.RoleName = role.Name;
            ViewBag.RoleId = roleId;

            var users = await _userManager.GetUsersInRoleAsync(role.Name);
            
            // Филтриране по търсене
            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => 
                    u.UserName.Contains(searchString) || 
                    u.FirstName.Contains(searchString) || 
                    u.LastName.Contains(searchString) || 
                    u.Email.Contains(searchString)).ToList();
            }

            return View(users);
        }

        // Показване на всички потребители с възможност за филтриране по роля
        public async Task<IActionResult> AllUsers(string roleFilter, string searchString, int page = 1, int pageSize = 10)
        {
            ViewBag.Roles = await _roleManager.Roles.ToListAsync();
            ViewBag.CurrentRoleFilter = roleFilter;
            ViewBag.CurrentSearchString = searchString;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            IQueryable<User> usersQuery = _userManager.Users;

            // Филтриране по роля
            if (!string.IsNullOrEmpty(roleFilter))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleFilter);
                var userIds = usersInRole.Select(u => u.Id).ToList();
                usersQuery = usersQuery.Where(u => userIds.Contains(u.Id));
            }

            // Филтриране по търсене
            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u => 
                    u.UserName.Contains(searchString) || 
                    u.FirstName.Contains(searchString) || 
                    u.LastName.Contains(searchString) || 
                    u.Email.Contains(searchString));
            }

            // Пагинация
            var totalUsers = await usersQuery.CountAsync();
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            var users = await usersQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Добавяме ролите на всеки потребител за изгледа
            var usersWithRoles = new List<(User User, List<string> Roles)>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add((user, roles.ToList()));
            }

            return View(usersWithRoles);
        }

        // Управление на ролите на потребител
        [HttpGet]
        public async Task<IActionResult> ManageUserRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.UserName = user.UserName;
            ViewBag.UserId = userId;
            ViewBag.FullName = $"{user.FirstName} {user.LastName}";

            var allRoles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);
            
            var roleSelections = new List<(string RoleId, string RoleName, bool IsSelected)>();

            foreach (var role in allRoles)
            {
                roleSelections.Add((role.Id, role.Name, userRoles.Contains(role.Name)));
            }

            return View(roleSelections);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageUserRoles(string selectedRole, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(selectedRole))
            {
                ModelState.AddModelError("", "Трябва да изберете роля за потребителя.");
                return RedirectToAction(nameof(ManageUserRoles), new { userId = userId });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Не могат да бъдат премахнати съществуващите роли");
                return RedirectToAction(nameof(ManageUserRoles), new { userId = userId });
            }

            result = await _userManager.AddToRoleAsync(user, selectedRole);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Не може да бъде добавена избраната роля");
                return RedirectToAction(nameof(ManageUserRoles), new { userId = userId });
            }

            return RedirectToAction(nameof(AllUsers));
        }
    }
} 