using Example.Entities;
using TenJames.CompMap.Attributes;
using TenJames.CompMap.Mappper;

namespace Example.DTOS;

/// <summary>
/// DTO for reading user data - maps FROM the User entity
/// </summary>
[MapFrom(typeof(User))]
public partial class UserReadDto
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ICollection<DocumentReadDto> Documents { get; set; } = new List<DocumentReadDto>();

    // Implementation required for unmapped properties
    private static partial UserUnmappedProperties GetUserUnmappedProperties(IMapper mapper, User source)
    {
        return new UserUnmappedProperties
        {
            // Login doesn't exist on User, so we derive it from Email
            Login = source.Email.Split('@')[0]
        };
    }
}
