
using System.Text.Json;
using TenJames.CompMap.Example;
using TenJames.CompMap.Mappper;
var userCreate = new UserCreateDto()
{
    Name = "John Doe",
    Guid = Guid.NewGuid()
};


var mapper = new BaseMapper();
var options = new JsonSerializerOptions()
{
    WriteIndented = true
};
Console.WriteLine("--- User Create ---");
Console.WriteLine(JsonSerializer.Serialize(userCreate, options));
var u = mapper.Map<User>(userCreate);
Console.WriteLine("--- User ---");
Console.WriteLine(JsonSerializer.Serialize(u, options));
var userDto = mapper.Map<UserReadDto>(u);
Console.WriteLine("--- UserReadDto ---");
Console.WriteLine(JsonSerializer.Serialize(userDto, options));