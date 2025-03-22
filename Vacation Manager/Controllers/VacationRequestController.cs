using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vacation_Manager.Data;
using Vacation_Manager.Models;

public class VacationRequestController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IWebHostEnvironment _hostEnvironment;

    public VacationRequestController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        IWebHostEnvironment hostEnvironment)
    {
        _context = context;
        _userManager = userManager;
        _hostEnvironment = hostEnvironment;
    }

    // GET: VacationRequest
    public async Task<IActionResult> Index(DateTime? fromDate)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var requests = _context.VacationRequests
            .Include(v => v.Requester)
            .Include(v => v.Approver)
            .Where(v => v.RequesterId == currentUser.Id);

        if (fromDate.HasValue)
        {
            requests = requests.Where(r => r.CreationDate >= fromDate.Value);
        }

        return View(await requests.ToListAsync());
    }

    // GET: VacationRequest/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vacationRequest = await _context.VacationRequests
            .Include(v => v.Requester)
            .Include(v => v.Approver)
            .FirstOrDefaultAsync(m => m.RequestId == id);

        if (vacationRequest == null)
        {
            return NotFound();
        }

        return View(vacationRequest);
    }

    // GET: VacationRequest/Create
    public IActionResult Create()
    {
        ViewBag.RequestTypes = Enum.GetValues(typeof(RequestType))
            .Cast<RequestType>()
            .Select(r => new SelectListItem
            {
                Text = r.ToString(),
                Value = ((int)r).ToString()
            });

        return View();
    }

    // POST: VacationRequest/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VacationRequest vacationRequest, IFormFile sickNote)
    {
        Console.WriteLine("Model state valid: " + ModelState.IsValid);
        foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
        {
            Console.WriteLine("Error: " + error.ErrorMessage);
        }
        if (ModelState.IsValid)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            vacationRequest.RequesterId = currentUser.Id;
            vacationRequest.CreationDate = DateTime.Now;
            vacationRequest.Approved = false;

            // За болничен отпуск
            if (vacationRequest.RequestType == RequestType.Sick && sickNote != null)
            {
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "sickNotes");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = $"{Guid.NewGuid()}_{sickNote.FileName}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await sickNote.CopyToAsync(fileStream);
                }

                vacationRequest.AttachmentFilePath = uniqueFileName;
            }

            // без опция за половин ден при болничен отпуск
            if (vacationRequest.RequestType == RequestType.Sick)
            {
                vacationRequest.HalfDay = false;
            }

            _context.Add(vacationRequest);
            try
            {
                _context.Add(vacationRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving to database: " + ex.Message);
                ModelState.AddModelError("", "Failed to save: " + ex.Message);
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.RequestTypes = Enum.GetValues(typeof(RequestType))
            .Cast<RequestType>()
            .Select(r => new SelectListItem
            {
                Text = r.ToString(),
                Value = ((int)r).ToString()
            });

        return View(vacationRequest);
    }

    // GET: VacationRequest/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var vacationRequest = await _context.VacationRequests
            .FirstOrDefaultAsync(m => m.RequestId == id && m.RequesterId == currentUser.Id);

        if (vacationRequest == null)
        {
            return NotFound();
        }

        // не могат да се редактират одобрени заявки
        if (vacationRequest.Approved)
        {
            TempData["ErrorMessage"] = "Approved requests cannot be edited.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.RequestTypes = Enum.GetValues(typeof(RequestType))
            .Cast<RequestType>()
            .Select(r => new SelectListItem
            {
                Text = r.ToString(),
                Value = ((int)r).ToString()
            });

        return View(vacationRequest);
    }

    // POST: VacationRequest/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VacationRequest vacationRequest, IFormFile sickNote)
    {
        if (id != vacationRequest.RequestId)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var existingRequest = await _context.VacationRequests
            .FirstOrDefaultAsync(r => r.RequestId == id && r.RequesterId == currentUser.Id);

        if (existingRequest == null)
        {
            return NotFound();
        }

        // не могат да се редактират одобрени заявки
        if (existingRequest.Approved)
        {
            TempData["ErrorMessage"] = "Approved requests cannot be edited.";
            return RedirectToAction(nameof(Index));
        }

        if (ModelState.IsValid)
        {
            try
            {
                existingRequest.StartDate = vacationRequest.StartDate;
                existingRequest.EndDate = vacationRequest.EndDate;
                existingRequest.RequestType = vacationRequest.RequestType;

                // Само половин ден за тези, които не са болни
                if (existingRequest.RequestType != RequestType.Sick)
                {
                    existingRequest.HalfDay = vacationRequest.HalfDay;
                }
                else
                {
                    existingRequest.HalfDay = false;
                }

                // Качване на файла за болничен
                if (existingRequest.RequestType == RequestType.Sick && sickNote != null)
                {
                    // Изтриване на стар файл, ако съществува
                    if (!string.IsNullOrEmpty(existingRequest.AttachmentFilePath))
                    {
                        string oldFilePath = Path.Combine(_hostEnvironment.WebRootPath, "sickNotes", existingRequest.AttachmentFilePath);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "sickNotes");
                    string uniqueFileName = $"{Guid.NewGuid()}_{sickNote.FileName}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await sickNote.CopyToAsync(fileStream);
                    }

                    existingRequest.AttachmentFilePath = uniqueFileName;
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VacationRequestExists(vacationRequest.RequestId))
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

        ViewBag.RequestTypes = Enum.GetValues(typeof(RequestType))
            .Cast<RequestType>()
            .Select(r => new SelectListItem
            {
                Text = r.ToString(),
                Value = ((int)r).ToString()
            });

        return View(vacationRequest);
    }

    // GET: VacationRequest/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var isManager = User.IsInRole("Team Lead") || User.IsInRole("CEO");

        var vacationRequest = await _context.VacationRequests
            .Include(v => v.Requester)
            .Include(v => v.Approver)
            .FirstOrDefaultAsync(m => m.RequestId == id);

        if (vacationRequest == null)
        {
            return NotFound();
        }

        // Проверка дали изискващият потребител съвпада с текущия
        // или потребителя е мениджър
        if (vacationRequest.RequesterId != currentUser.Id && !isManager)
        {
            return Forbid();
        }

        if (vacationRequest.RequesterId == currentUser.Id && vacationRequest.Approved)
        {
            TempData["ErrorMessage"] = "Approved requests cannot be deleted by requester.";
            return RedirectToAction(nameof(Index));
        }

        return View(vacationRequest);
    }

    // POST: VacationRequest/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var isManager = User.IsInRole("Team Lead") || User.IsInRole("CEO");

        var vacationRequest = await _context.VacationRequests
            .FirstOrDefaultAsync(m => m.RequestId == id);

        if (vacationRequest == null)
        {
            return NotFound();
        }

        // Проверка дали изискващият потребител съвпада с текущия
        // или потребителя е мениджър
        if (vacationRequest.RequesterId != currentUser.Id && !isManager)
        {
            return Forbid();
        }

        if (vacationRequest.RequesterId == currentUser.Id && vacationRequest.Approved && !isManager)
        {
            TempData["ErrorMessage"] = "Approved requests cannot be deleted by requester.";
            return RedirectToAction(nameof(Index));
        }

        // изтриване на файла, ако съществува
        if (!string.IsNullOrEmpty(vacationRequest.AttachmentFilePath))
        {
            string filePath = Path.Combine(_hostEnvironment.WebRootPath, "sickNotes", vacationRequest.AttachmentFilePath);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        _context.VacationRequests.Remove(vacationRequest);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: VacationRequest/AttachmentFilePath/5
    public async Task<IActionResult> DownloadSickNote(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vacationRequest = await _context.VacationRequests
            .FirstOrDefaultAsync(m => m.RequestId == id);

        if (vacationRequest == null || string.IsNullOrEmpty(vacationRequest.AttachmentFilePath))
        {
            return NotFound();
        }

        // Проверка дали потребителя има право да изтегли файла
        var currentUser = await _userManager.GetUserAsync(User);
        var isManager = User.IsInRole("Team Lead") || User.IsInRole("CEO");

        if (vacationRequest.RequesterId != currentUser.Id && !isManager)
        {
            return Forbid();
        }

        string filePath = Path.Combine(_hostEnvironment.WebRootPath, "sickNotes", vacationRequest.AttachmentFilePath);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var fileNameWithoutGuid = vacationRequest.AttachmentFilePath.Substring(
            vacationRequest.AttachmentFilePath.IndexOf('_') + 1);

        return PhysicalFile(filePath, "application/octet-stream", fileNameWithoutGuid);
    }

    private bool VacationRequestExists(int id)
    {
        return _context.VacationRequests.Any(e => e.RequestId == id);
    }
}