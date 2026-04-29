using Korendzh.Domain.Cms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Korendzh.Infrastructure.Persistence.Configurations;

public class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> b)
    {
        b.ToTable("SiteSettings");
        b.HasKey(x => x.Id);
        // Singleton: Id всегда = 1, не auto-increment.
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.SiteName).IsRequired().HasMaxLength(100);
        b.Property(x => x.HeroTitle).HasMaxLength(200);
        b.Property(x => x.HeroSubtitle).HasMaxLength(500);
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.Email).HasMaxLength(256);
        b.Property(x => x.Address).HasMaxLength(300);
        b.Property(x => x.WorkingHours).HasMaxLength(200);
        b.Property(x => x.HeroImageUrl).HasMaxLength(500);
        b.Property(x => x.InstagramUrl).HasMaxLength(500);
        b.Property(x => x.TelegramUrl).HasMaxLength(500);
        b.Property(x => x.VkUrl).HasMaxLength(500);
    }
}

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> b)
    {
        b.ToTable("Services");
        b.HasKey(x => x.Id);
        b.Property(x => x.Slug).IsRequired().HasMaxLength(100);
        b.HasIndex(x => x.Slug).IsUnique();
        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.ShortDescription).HasMaxLength(500);
        b.Property(x => x.DescriptionHtml).HasColumnType("nvarchar(max)");
        b.Property(x => x.PriceLabel).HasMaxLength(100);
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.HasIndex(x => new { x.IsPublished, x.DisplayOrder });
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.ToTable("Reviews");
        b.HasKey(x => x.Id);
        b.Property(x => x.AuthorName).IsRequired().HasMaxLength(150);
        b.Property(x => x.Text).IsRequired().HasMaxLength(2000);
        b.Property(x => x.AuthorPhotoUrl).HasMaxLength(500);
        b.HasIndex(x => new { x.IsPublished, x.DisplayOrder });
    }
}

public class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> b)
    {
        b.ToTable("Pages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Slug).IsRequired().HasMaxLength(100);
        b.HasIndex(x => x.Slug).IsUnique();
        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.ContentHtml).HasColumnType("nvarchar(max)");
        b.HasIndex(x => new { x.IsPublished, x.ShowInMenu, x.MenuOrder });
    }
}

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> b)
    {
        b.ToTable("MediaAssets");
        b.HasKey(x => x.Id);
        b.Property(x => x.Url).IsRequired().HasMaxLength(500);
        b.Property(x => x.OriginalFileName).HasMaxLength(300);
        b.Property(x => x.ContentType).HasMaxLength(100);
        b.Property(x => x.ContentHash).HasMaxLength(64);
        b.HasIndex(x => x.ContentHash);
        b.HasIndex(x => x.UploadedById);
    }
}
