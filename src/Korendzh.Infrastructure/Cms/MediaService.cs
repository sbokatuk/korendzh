using System.Security.Cryptography;
using Korendzh.Domain.Cms;
using Korendzh.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Korendzh.Infrastructure.Cms;

public class MediaService : IMediaService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private const long MaxBytes = 5 * 1024 * 1024; // 5 МБ

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public MediaService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<MediaAsset> UploadAsync(IFormFile file, Guid actorId, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0) throw new InvalidOperationException("Файл пустой.");
        if (file.Length > MaxBytes) throw new InvalidOperationException("Файл больше 5 МБ.");

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext)) throw new InvalidOperationException("Недопустимое расширение. Разрешены jpg, jpeg, png, webp.");
        if (!AllowedContentTypes.Contains(file.ContentType)) throw new InvalidOperationException("Недопустимый MIME-тип.");

        // Считаем хэш для имени файла и идемпотентности.
        await using var stream = file.OpenReadStream();
        var hash = await ComputeSha256Async(stream, ct);
        stream.Position = 0;

        var nowUtc = DateTime.UtcNow;
        var subFolder = Path.Combine("uploads", nowUtc.ToString("yyyy"), nowUtc.ToString("MM"));
        var fileName = hash + ext.ToLowerInvariant();
        var relUrl = "/" + subFolder.Replace('\\', '/') + "/" + fileName;
        var absDir = Path.Combine(_env.WebRootPath, subFolder);
        Directory.CreateDirectory(absDir);
        var absPath = Path.Combine(absDir, fileName);

        // Идемпотентность: если такой файл уже есть на диске — повторно не пишем.
        if (!File.Exists(absPath))
        {
            await using var fs = File.Create(absPath);
            await stream.CopyToAsync(fs, ct);
        }

        // Уже есть запись с тем же хэшем — переиспользуем (избегаем дублей в каталоге).
        var existing = await _db.MediaAssets.FirstOrDefaultAsync(a => a.ContentHash == hash, ct);
        if (existing != null) return existing;

        var asset = new MediaAsset
        {
            Url = relUrl,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            ContentHash = hash,
            UploadedById = actorId,
            UploadedAt = nowUtc,
        };
        _db.MediaAssets.Add(asset);
        await _db.SaveChangesAsync(ct);
        return asset;
    }

    public async Task<List<MediaAsset>> ListRecentAsync(int take = 50, CancellationToken ct = default)
    {
        return await _db.MediaAssets
            .AsNoTracking()
            .OrderByDescending(a => a.UploadedAt)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var asset = await _db.MediaAssets.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null) return false;

        // Удаляем запись; физический файл остаётся (на случай, если на него ссылаются услуги/отзывы).
        // Полная чистка — отдельной задачей по orphan-сканированию.
        _db.MediaAssets.Remove(asset);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct)
    {
        using var sha = SHA256.Create();
        var hashBytes = await sha.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
