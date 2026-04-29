using Korendzh.Domain.Cms;
using Microsoft.AspNetCore.Http;

namespace Korendzh.Infrastructure.Cms;

public interface IMediaService
{
    /// <summary>
    /// Сохраняет файл в wwwroot/uploads и регистрирует MediaAsset.
    /// Возвращает запись с публичным URL.
    /// </summary>
    Task<MediaAsset> UploadAsync(IFormFile file, Guid actorId, CancellationToken ct = default);

    Task<List<MediaAsset>> ListRecentAsync(int take = 50, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
