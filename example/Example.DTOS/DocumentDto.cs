using Example.Entities;
using TenJames.CompMap.Attributes;

namespace Example.DTOS;

/// <summary>
/// DTO for reading document data - maps FROM the Document entity
/// </summary>
[MapFrom(typeof(Document))]
public partial class DocumentReadDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
