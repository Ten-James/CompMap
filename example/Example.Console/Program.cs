using System.Text.Json;
using Example.DTOS;
using Example.Entities;
using TenJames.CompMap.Mapper;

var mapper = new BaseMapper();
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

Console.WriteLine("=== TenJames.CompMap Multi-Assembly Example ===\n");

// Create a user entity (simulating data from a database)
var user = new User
{
    Id = 1,
    Name = "John Doe",
    Email = "john.doe@example.com",
    CreatedAt = DateTime.UtcNow,
    Documents = new List<Document>
    {
        new() { Id = 1, Title = "First Document", Content = "Content of first document", CreatedAt = DateTime.UtcNow },
        new() { Id = 2, Title = "Second Document", Content = "Content of second document", CreatedAt = DateTime.UtcNow }
    }
};

Console.WriteLine("1. User entity (from Example.Entities assembly):");
Console.WriteLine(JsonSerializer.Serialize(user, jsonOptions));

// Map to DTO (demonstrates MapFrom across assemblies)
var userDto = mapper.Map<UserReadDto>(user);

Console.WriteLine("\n2. UserReadDto (from Example.DTOS assembly):");
Console.WriteLine(JsonSerializer.Serialize(userDto, jsonOptions));

Console.WriteLine("\n=== mapping works! ===");
Console.WriteLine("- Models are defined in Example.Entities assembly");
Console.WriteLine("- DTOs with [MapFrom] are defined in Example.DTOS assembly");
Console.WriteLine("- Source generator correctly generates mappings across assembly boundaries");
Console.WriteLine("- Nested collections (Documents -> DocumentReadDto) are also mapped");
