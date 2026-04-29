using Korendzh.Domain.Cms;
using Korendzh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Infrastructure.Cms;

public class CmsService : ICmsService
{
    private readonly AppDbContext _db;

    public CmsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SiteSettings> GetSiteSettingsAsync(CancellationToken ct = default)
    {
        // Singleton-запись с Id=1. Если её нет — создаём с дефолтами и сохраняем.
        var s = await _db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        if (s != null) return s;

        s = new SiteSettings { Id = 1 };
        _db.SiteSettings.Add(s);
        await _db.SaveChangesAsync(ct);
        return s;
    }

    public async Task<SiteSettings> UpdateSiteSettingsAsync(SiteSettings update, Guid actorId, CancellationToken ct = default)
    {
        var s = await _db.SiteSettings.FirstOrDefaultAsync(x => x.Id == 1, ct)
                ?? new SiteSettings { Id = 1 };

        s.SiteName = update.SiteName;
        s.HeroTitle = update.HeroTitle;
        s.HeroSubtitle = update.HeroSubtitle;
        s.HeroImageUrl = update.HeroImageUrl;
        s.Phone = update.Phone;
        s.Email = update.Email;
        s.Address = update.Address;
        s.WorkingHours = update.WorkingHours;
        s.InstagramUrl = update.InstagramUrl;
        s.TelegramUrl = update.TelegramUrl;
        s.VkUrl = update.VkUrl;
        s.UpdatedAt = DateTime.UtcNow;
        s.UpdatedById = actorId;

        if (_db.Entry(s).State == EntityState.Detached)
        {
            _db.SiteSettings.Add(s);
        }

        await _db.SaveChangesAsync(ct);
        return s;
    }

    public async Task<List<Service>> GetPublishedServicesAsync(CancellationToken ct = default)
    {
        return await _db.Services
            .AsNoTracking()
            .Where(s => s.IsPublished)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Title)
            .ToListAsync(ct);
    }

    public async Task<Service?> GetServiceBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _db.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug && s.IsPublished, ct);
    }

    public async Task<List<Review>> GetPublishedReviewsAsync(int? take = null, CancellationToken ct = default)
    {
        var q = _db.Reviews
            .AsNoTracking()
            .Where(r => r.IsPublished)
            .OrderBy(r => r.DisplayOrder).ThenByDescending(r => r.ReviewDate);
        if (take.HasValue) q = (IOrderedQueryable<Review>)q.Take(take.Value);
        return await q.ToListAsync(ct);
    }

    public async Task<Page?> GetPageBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _db.Pages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished, ct);
    }

    public async Task<List<Page>> GetMenuPagesAsync(CancellationToken ct = default)
    {
        return await _db.Pages
            .AsNoTracking()
            .Where(p => p.IsPublished && p.ShowInMenu)
            .OrderBy(p => p.MenuOrder).ThenBy(p => p.Title)
            .ToListAsync(ct);
    }
}
