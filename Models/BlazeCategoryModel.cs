namespace Wadevo.Models;

public sealed class BlazeCategoryModel
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public string Name { get; set; } = "";

    public string Slug { get; set; } = "";

    public string ImageUrl { get; set; } = "";
}
