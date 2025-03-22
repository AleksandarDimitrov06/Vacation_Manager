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
    public async Task<IActionResult> Index(DateTime? fromDate, string status = "Pending")
    {
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
            if (currentUser.LedTeamId == null)
            {
                return View(new List<VacationRequest>());
            }

            requests = _context.VacationRequests
                .Include(v => v.Requester)
                .Include(v => v.Approver)
                .Where(v => v.Requester.TeamId == currentUser.LedTeamId);
        }

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
            .FirstOrDefaultAsync(v => v.RequestId == id);

        if (request == null)
        {
            return NotFound();
        }

        bool canApprove = false;

        if (isCEO)
        {
            canApprove = true; // CEO може да одобри всяка заявка
        }
        else if (request.Requester.TeamId == currentUser.LedTeamId)
        {
            canApprove = true; // Team Lead може да одобри заявка на член от екипа
        }

        if (!canApprove)
        {
            return Forbid();
        }

        // Одобри заявката
        request.Approved = true;
        request.ApproverId = currentUser.Id;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}