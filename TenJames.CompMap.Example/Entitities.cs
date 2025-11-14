//using TenJames.CompMap.Attributes;

using TenJames.CompMap.Attributes;
using TenJames.CompMap.Mappper;

namespace TenJames.CompMap.Example;


public class User {
    public int Id { get; set; }
    public string Name { get; set; }
    public Guid Guid { get; set; }
    public ICollection<Document> Documents { get; set; }
}


public class Document {
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
}


[MapFrom(typeof(User))]
public partial class UserReadDto {
    public int Id { get; set; }
    public string Name { get; set; }
    public Guid Guid { get; set; }
    public required string Title { get; set; }
    public ICollection<DocumentDto> Documents { get; set; }

    private static partial UserUnmappedProperties GetUserUnmappedProperties(IMapper mapper, User source)
    {
        return new UserUnmappedProperties()
        {
            Title = source.Name + "'s Title",
        };
    }
}

[MapTo(typeof(User))]
public partial class UserCreateDto {
    public string Name { get; set; }
    public Guid Guid { get; set; }

    private static partial UserUnmappedProperties GetUserUnmappedProperties(IMapper mapper, UserCreateDto source)
    {
        return new UserUnmappedProperties()
        {
            
        };
    }
}

[MapFrom(typeof(Document))]
public partial class DocumentDto {
    public string Title { get; set; }
    public string Content { get; set; }
}