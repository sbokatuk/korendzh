using System.ComponentModel.DataAnnotations;

namespace Korendzh.Web.Pages.Admin.Cms.Services;

public class ServiceFormInput
{
    public Guid Id { get; set; }

    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;

    [Required, MaxLength(100), RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Только латиница, цифры и дефис")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)] public string ShortDescription { get; set; } = string.Empty;
    public string DescriptionHtml { get; set; } = string.Empty;

    [MaxLength(100)] public string PriceLabel { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 100;

    [MaxLength(500)] public string? ImageUrl { get; set; }

    public bool IsPublished { get; set; } = true;
}
