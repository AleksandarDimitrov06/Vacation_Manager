using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vacation_Manager.Data;
using Vacation_Manager.Models;

[Authorize(Roles = "Team Lead,CEO")]
public class RequestApprovalController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public RequestApprovalController(
        ApplicationDbContext context,
        UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: RequestApproval
    public async Task<IActionResult> Index(DateTime? fromDate, string status = "All")
    {
        // Проверка дали параметърът status е бил явно подаден в URL-а или е взел стойността по подразбиране
        if (Request.Query["status"].Count == 0)
        {
            return RedirectToAction(nameof(Index), new { status = "All", fromDate });
        }
        
        var currentUser = await _userManager.GetUserAsync(User);
        var isCEO = User.IsInRole("CEO");

        // взимане на заявки според ролята
        IQueryable<VacationRequest> requests;

        if (isCEO)
        {
            // CEO вижда всички заявки
            requests = _context.VacationRequests
                .Include(v => v.Requester)
                .Include(v => v.Approver);
        }
        else
        {
            // Team Lead може да види заявки само от съответния екип
            // Първо проверяваме дали потребителят е Team Lead на някой екип
            var leadTeam = await _context.Teams
                .FirstOrDefaultAsync(t => t.TeamLeaderId == currentUser.Id);

            if (leadTeam == null)
            {
                // Ако потребителят не е Team Lead на никой екип, връщаме празен списък
                TempData["ErrorMessage"] = "Не сте назначен като лидер на екип.";
                return View(new List<VacationRequest>());
            }

            // Вземаме членовете на екипа
            requests = _context.VacationRequests
                .Include(v => v.Requester)
                .Include(v => v.Approver)
                .Where(v => v.Requester.TeamId == leadTeam.TeamId);
        }

        // Филтрираме собствените заявки на потребителя - не може да одобрява собствените си заявки
        requests = requests.Where(v => v.RequesterId != currentUser.Id);

        // филтриране по дата
        if (fromDate.HasValue)
        {
            requests = requests.Where(r => r.CreationDate >= fromDate.Value);
        }

        // филтриране по статус
        switch (status)
        {
            case "Approved":
                requests = requests.Where(r => r.Approved);
                break;
            case "Pending":
                requests = requests.Where(r => !r.Approved);
                break;
        }

        ViewBag.FromDate = fromDate;
        ViewBag.Status = status;
        return View(await requests.ToListAsync());
    }

    // POST: RequestApproval/Approve/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var isCEO = User.IsInRole("CEO");

        var request = await _context.VacationRequests
            .Include(v => v.Requester)
            .ThenInclude(r => r.Team)
            .FirstOrDefaultAsync(v => v.RequestId == id);

        if (request == null)
        {
            return NotFound();
        }

        // Проверка дали потребителят не се опитва да одобри собствената си заявка
        if (request.RequesterId == currentUser.Id)
        {
            TempData["ErrorMessage"] = "Не можете да одобрите собствената си заявка за отпуск!";
            return RedirectToAction(nameof(Index));
        }

        bool canApprove = false;

        if (isCEO)
        {
            canApprove = true; // CEO може да одобри всяка заявка
        }
        else
        {
            // Проверяваме дали потребителят е Team Lead на екипа на заявителя
            var leadTeam = await _context.Teams
                .FirstOrDefaultAsync(t => t.TeamLeaderId == currentUser.Id);
            
            if (leadTeam != null && request.Requester.TeamId == leadTeam.TeamId)
            {
                canApprove = true; // Team Lead може да одобри заявка на член от екипа
            }
        }

        if (!canApprove)
        {
            TempData["ErrorMessage"] = "Нямате право да одобрите тази заявка.";
            return RedirectToAction(nameof(Index));
        }

        // Одобри заявката
        request.Approved = true;
        request.ApproverId = currentUser.Id;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Заявката беше одобрена успешно.";

        return RedirectToAction(nameof(Index));
    }
}