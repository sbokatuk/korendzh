using Korendzh.Domain.Cms;

namespace Korendzh.Infrastructure.Cms;

/// <summary>
/// Высокоуровневые операции над CMS-контентом для публичных страниц и админки.
/// </summary>
public interface ICmsService
{
    Task<SiteSettings> GetSiteSettingsAsync(CancellationToken ct = default);
    Task<SiteSettings> UpdateSiteSettingsAsync(SiteSettings settings, Guid actorId, CancellationToken ct = default);

    Task<List<Service>> GetPublishedServicesAsync(CancellationToken ct = default);
    Task<Service?> GetServiceBySlugAsync(string slug, CancellationToken ct = default);

    Task<List<Review>> GetPublishedReviewsAsync(int? take = null, CancellationToken ct = default);

    Task<Page?> GetPageBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<Page>> GetMenuPagesAsync(CancellationToken ct = default);
}
