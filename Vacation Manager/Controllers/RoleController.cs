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
    [Authorize(Roles = "CEO")]
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
    }
} 