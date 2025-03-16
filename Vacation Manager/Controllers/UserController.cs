using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vacation_Manager.Models;
using Vacation_Manager.Data;

namespace Vacation_Manager.Controllers
{
    [Authorize(Roles = "CEO")]
    public class UserController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: Показване на всички потребители
        public async Task<IActionResult> Index(string searchString, string roleFilter, int page = 1, int pageSize = 10)
        {
            // Извличане на всички роли за филтъра
            ViewBag.Roles = await _roleManager.Roles.ToListAsync();
            ViewBag.CurrentSearchString = searchString;
            ViewBag.CurrentRoleFilter = roleFilter;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            // Извличане на всички потребители
            IQueryable<User> users = _userManager.Users;

            // Прилагане на филтъра по роля
            if (!string.IsNullOrEmpty(roleFilter))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleFilter);
                var userIds = usersInRole.Select(u => u.Id).ToList();
                users = users.Where(u => userIds.Contains(u.Id));
            }

            // Прилагане на филтъра по търсене
            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u =>
                    u.UserName.Contains(searchString) ||
                    u.FirstName.Contains(searchString) ||
                    u.LastName.Contains(searchString) ||
                    u.Email.Contains(searchString));
            }

            // Пагинация
            var totalUsers = await users.CountAsync();
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            var pagedUsers = await users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(u => u.Team)
                .ToListAsync();

            // Добавяне на ролите за всеки потребител
            var usersWithRoles = new List<(User User, List<string> Roles)>();
            foreach (var user in pagedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add((user, roles.ToList()));
            }

            // Сортиране на потребителите по роля, първо име и фамилия
            usersWithRoles = usersWithRoles
                .OrderBy(u => u.Roles.FirstOrDefault() ?? "Unassigned") // Сортиране по първа роля (или "Unassigned" ако няма роля)
                .ThenBy(u => u.User.FirstName)
                .ThenBy(u => u.User.LastName)
                .ToList();

            return View(usersWithRoles);
        }

        // GET: Детайли за потребител
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;

            // Зареждане на информация за екипа
            user = await _context.Users
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Id == id);

            return View(user);
        }

        // GET: Редактиране на потребител
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Зареждане на всички екипи за падащото меню
            var teams = await _context.Teams.ToListAsync();
            ViewBag.Teams = new SelectList(teams, "TeamId", "TeamName", user.TeamId);

            // Зареждане на текущата роля на потребителя
            var userRoles = await _userManager.GetRolesAsync(user);
            ViewBag.CurrentRole = userRoles.FirstOrDefault();
            ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name");

            return View(user);
        }

        // POST: Редактиране на потребител
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, User model, string role, int? teamId)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Обновяване на основните данни
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.Email = model.Email;
                    user.TeamId = teamId;

                    // Обновяване на потребителя
                    await _userManager.UpdateAsync(user);

                    // Обновяване на ролята
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    
                    if (!string.IsNullOrEmpty(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                    else
                    {
                        // Ако не е избрана роля, задаваме Unassigned по подразбиране
                        await _userManager.AddToRoleAsync(user, "Unassigned");
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Ако достигнем тук, означава че има грешка, презареждаме формата
            var teams = await _context.Teams.ToListAsync();
            ViewBag.Teams = new SelectList(teams, "TeamId", "TeamName", teamId);
            ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name", role);

            return View(model);
        }

        // GET: Изтриване на потребител
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;

            return View(user);
        }

        // POST: Потвърждение за изтриване на потребител
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Проверка дали потребителят не е лидер на екип
            var teamLed = await _context.Teams.FirstOrDefaultAsync(t => t.TeamLeaderId == id);
            if (teamLed != null)
            {
                ModelState.AddModelError("", "Потребителят е лидер на екип и не може да бъде изтрит.");
                var roles = await _userManager.GetRolesAsync(user);
                ViewBag.Roles = roles;
                return View(user);
            }

            await _userManager.DeleteAsync(user);
            return RedirectToAction(nameof(Index));
        }

        // Проверка дали потребителят съществува
        private async Task<bool> UserExists(string id)
        {
            return await _userManager.FindByIdAsync(id) != null;
        }
    }
} 