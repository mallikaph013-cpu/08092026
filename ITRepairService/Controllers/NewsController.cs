using ITRepairService.Data;
using ITRepairService.Models;
using ITRepairService.ViewModels.News;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace ITRepairService.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ITSupport}")]
public class NewsController(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment webHostEnvironment) : Controller
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
    private const long MaxAttachmentSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions =
    [
        ".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx", ".xls", ".xlsx", ".txt"
    ];

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var news = await _dbContext.NewsItems
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return View(news);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateNewsViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var newsItem = await _dbContext.NewsItems.FirstOrDefaultAsync(n => n.Id == id);
        if (newsItem is null)
        {
            return NotFound();
        }

        var model = new EditNewsViewModel
        {
            Id = newsItem.Id,
            Title = newsItem.Title,
            Content = newsItem.Content,
            IsActive = newsItem.IsActive,
            CurrentAttachmentFileName = newsItem.AttachmentFileName
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateNewsViewModel model)
    {
        string? savedAttachmentUrl = null;
        string? savedAttachmentName = null;

        if (model.Attachment is not null && model.Attachment.Length > 0)
        {
            if (model.Attachment.Length > MaxAttachmentSize)
            {
                ModelState.AddModelError(nameof(model.Attachment), "Attachment size must not exceed 10 MB.");
            }

            var extension = Path.GetExtension(model.Attachment.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension.ToLowerInvariant()))
            {
                ModelState.AddModelError(nameof(model.Attachment), "Unsupported file type.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Attachment is not null && model.Attachment.Length > 0)
        {
            var uploadRoot = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "news");
            Directory.CreateDirectory(uploadRoot);

            var extension = Path.GetExtension(model.Attachment.FileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadRoot, storedFileName);

            await using var stream = System.IO.File.Create(fullPath);
            await model.Attachment.CopyToAsync(stream);

            savedAttachmentUrl = $"/uploads/news/{storedFileName}";
            savedAttachmentName = Path.GetFileName(model.Attachment.FileName);
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var actorName = GetActorName(currentUser);
        var nowLocal = DateTime.Now;

        var newsItem = new NewsItem
        {
            Title = model.Title.Trim(),
            Content = model.Content.Trim(),
            IsActive = model.IsActive,
            CreatedByName = actorName,
            UpdatedByName = actorName,
            AttachmentFileName = savedAttachmentName,
            AttachmentUrl = savedAttachmentUrl,
            CreatedAt = nowLocal,
            UpdatedAt = nowLocal
        };

        _dbContext.NewsItems.Add(newsItem);
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "News created successfully.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditNewsViewModel model)
    {
        var newsItem = await _dbContext.NewsItems.FirstOrDefaultAsync(n => n.Id == model.Id);
        if (newsItem is null)
        {
            return NotFound();
        }

        if (model.Attachment is not null && model.Attachment.Length > 0)
        {
            if (model.Attachment.Length > MaxAttachmentSize)
            {
                ModelState.AddModelError(nameof(model.Attachment), "Attachment size must not exceed 10 MB.");
            }

            var extension = Path.GetExtension(model.Attachment.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension.ToLowerInvariant()))
            {
                ModelState.AddModelError(nameof(model.Attachment), "Unsupported file type.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.CurrentAttachmentFileName = newsItem.AttachmentFileName;
            return View(model);
        }

        newsItem.Title = model.Title.Trim();
        newsItem.Content = model.Content.Trim();
        newsItem.IsActive = model.IsActive;

        if (model.RemoveAttachment)
        {
            DeleteAttachmentIfExists(newsItem.AttachmentUrl);
            newsItem.AttachmentUrl = null;
            newsItem.AttachmentFileName = null;
        }

        if (model.Attachment is not null && model.Attachment.Length > 0)
        {
            DeleteAttachmentIfExists(newsItem.AttachmentUrl);

            var uploadRoot = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "news");
            Directory.CreateDirectory(uploadRoot);

            var extension = Path.GetExtension(model.Attachment.FileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadRoot, storedFileName);

            await using var stream = System.IO.File.Create(fullPath);
            await model.Attachment.CopyToAsync(stream);

            newsItem.AttachmentUrl = $"/uploads/news/{storedFileName}";
            newsItem.AttachmentFileName = Path.GetFileName(model.Attachment.FileName);
        }

        var currentUser = await _userManager.GetUserAsync(User);
        newsItem.UpdatedByName = GetActorName(currentUser);
        newsItem.UpdatedAt = DateTime.Now;

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "News updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var newsItem = await _dbContext.NewsItems.FirstOrDefaultAsync(n => n.Id == id);
        if (newsItem is null)
        {
            return NotFound();
        }

        DeleteAttachmentIfExists(newsItem.AttachmentUrl);

        _dbContext.NewsItems.Remove(newsItem);
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "News deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private void DeleteAttachmentIfExists(string? attachmentUrl)
    {
        if (string.IsNullOrWhiteSpace(attachmentUrl))
        {
            return;
        }

        var relativePath = attachmentUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }

    private static string GetActorName(ApplicationUser? user)
    {
        if (!string.IsNullOrWhiteSpace(user?.FullName))
        {
            return user.FullName;
        }

        return user?.UserName ?? user?.Email ?? "System";
    }
}
