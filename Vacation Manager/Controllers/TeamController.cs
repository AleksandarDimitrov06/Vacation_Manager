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
    public class TeamController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public TeamController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Показване на всички екипи
        public async Task<IActionResult> Index()
        {
            var teams = await _context.Teams
                .Include(t => t.TeamLeader)
                .Include(t => t.Members)
                .Include(t => t.Project)
                .ToListAsync();

            return View(teams);
        }

        // GET: Детайли за екип
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var team = await _context.Teams
                .Include(t => t.TeamLeader)
                .Include(t => t.Members)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TeamId == id);

            if (team == null)
            {
                return NotFound();
            }

            return View(team);
        }

        // GET: Създаване на нов екип
        public async Task<IActionResult> Create()
        {
            // Зареждане на всички потребители с роля Team Lead
            var teamLeads = await _userManager.GetUsersInRoleAsync("Team Lead");
            ViewBag.TeamLeads = new SelectList(teamLeads, "Id", "UserName");

            // Зареждане на всички проекти
            var projects = await _context.Projects.ToListAsync();
            ViewBag.Projects = new SelectList(projects, "ProjectId", "ProjectName");

            return View();
        }

        // POST: Създаване на нов екип
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TeamName,TeamLeaderId,ProjectId")] Team team)
        {
            if (ModelState.IsValid)
            {
                _context.Add(team);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            
            var teamLeads = await _userManager.GetUsersInRoleAsync("Team Lead");
            ViewBag.TeamLeads = new SelectList(teamLeads, "Id", "UserName");
            var projects = await _context.Projects.ToListAsync();
            ViewBag.Projects = new SelectList(projects, "ProjectId", "ProjectName");

            return View(team);
        }

        // GET: Редактиране на екип
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                return NotFound();
            }

            // Зареждане на всички потребители с роля Team Lead
            var teamLeads = await _userManager.GetUsersInRoleAsync("Team Lead");
            ViewBag.TeamLeads = new SelectList(teamLeads, "Id", "UserName", team.TeamLeaderId);

            // Зареждане на всички проекти
            var projects = await _context.Projects.ToListAsync();
            ViewBag.Projects = new SelectList(projects, "ProjectId", "ProjectName", team.ProjectId);

            return View(team);
        }

        // POST: Редактиране на екип
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TeamId,TeamName,TeamLeaderId,ProjectId")] Team team)
        {
            if (id != team.TeamId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(team);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeamExists(team.TeamId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // Презареждане на списъка с лидери и проекти при грешка
            var teamLeads = await _userManager.GetUsersInRoleAsync("Team Lead");
            ViewBag.TeamLeads = new SelectList(teamLeads, "Id", "UserName", team.TeamLeaderId);
            var projects = await _context.Projects.ToListAsync();
            ViewBag.Projects = new SelectList(projects, "ProjectId", "ProjectName", team.ProjectId);

            return View(team);
        }

        // GET: Изтриване на екип
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var team = await _context.Teams
                .Include(t => t.TeamLeader)
                .Include(t => t.Members)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.TeamId == id);

            if (team == null)
            {
                return NotFound();
            }

            return View(team);
        }

        // POST: Потвърждение за изтриване на екип
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team != null)
            {
                // Проверка дали екипът има потребители
                if (team.Members != null && team.Members.Any())
                {
                    ModelState.AddModelError("", "Не може да изтриете екип, който има потребители.");
                    return View("Delete", team);
                }

                _context.Teams.Remove(team);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Проверка дали екипът съществува
        private bool TeamExists(int id)
        {
            return _context.Teams.Any(e => e.TeamId == id);
        }
    }
} 