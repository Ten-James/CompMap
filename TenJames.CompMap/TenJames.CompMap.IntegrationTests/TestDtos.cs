namespace TenJames.CompMap.IntegrationTests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Attributes;
using Mappper;

/// <summary>
/// DTO for reading product data
/// Has 5 unmapped properties: DisplayName, IsAvailable, FormattedPrice, ReviewCount, AverageRating
/// </summary>
[MapFrom(typeof(Product))]
public partial class ProductReadDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public string Sku { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public Guid ProductGuid { get; set; }

    public CategoryDto Category { get; set; } = null!;

    public ICollection<ReviewDto> Reviews { get; set; } = new List<ReviewDto>();

    // Unmapped properties (not in Product entity)
    public required string DisplayName { get; set; }

    public required bool IsAvailable { get; set; }

    public required string FormattedPrice { get; set; }

    public required int ReviewCount { get; set; }

    public required double AverageRating { get; set; }

    // Implementation of unmapped properties mapping
    private static partial ProductUnmappedProperties GetProductUnmappedProperties(IMapper mapper, Product source) =>
        new()
        {
            DisplayName = $"{source.Name} ({source.Sku})",
            IsAvailable = source.IsActive && source.StockQuantity > 0,
            FormattedPrice = $"${source.Price.ToString("F2", CultureInfo.InvariantCulture)}",
            ReviewCount = source.Reviews?.Count ?? 0,
            AverageRating = source.Reviews?.Count > 0
                ? source.Reviews.Average(r => r.Rating)
                : 0.0
        };
}

/// <summary>
/// DTO for category data
/// </summary>
[MapFrom(typeof(Category))]
public partial class CategoryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// DTO for review data
/// Has 1 unmapped property: FormattedRating
/// </summary>
[MapFrom(typeof(Review))]
public partial class ReviewDto
{
    public int Id { get; set; }

    public string Comment { get; set; } = string.Empty;

    public int Rating { get; set; }

    public DateTime CreatedAt { get; set; }

    // Unmapped property
    public required string FormattedRating { get; set; }

    private static partial ReviewUnmappedProperties GetReviewUnmappedProperties(IMapper mapper, Review source) => new()
    {
        FormattedRating = $"{source.Rating}/5 stars"
    };
}

/// <summary>
/// DTO for reading user data
/// Has 6 unmapped properties: FullName, Age, IsAdult, MembershipDuration, TotalOrders, MaskedEmail
/// Excludes sensitive fields like PasswordHash
/// </summary>
[MapFrom(typeof(User))]
public partial class UserReadDto
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsEmailVerified { get; set; }

    public ICollection<OrderDto> Orders { get; set; } = new List<OrderDto>();

    // Unmapped properties (computed/derived fields)
    public required string FullName { get; set; }

    public required int Age { get; set; }

    public required bool IsAdult { get; set; }

    public required int MembershipDuration { get; set; }

    public required int TotalOrders { get; set; }

    public required string MaskedEmail { get; set; }

    private static partial UserUnmappedProperties GetUserUnmappedProperties(IMapper mapper, User source)
    {
        var age = DateTime.Now.Year - source.DateOfBirth.Year;
        if (DateTime.Now < source.DateOfBirth.AddYears(age))
        {
            age--;
        }

        var membershipDays = (DateTime.Now - source.CreatedAt).Days;

        return new UserUnmappedProperties
        {
            FullName = $"{source.FirstName} {source.LastName}",
            Age = age,
            IsAdult = age >= 18,
            MembershipDuration = membershipDays,
            TotalOrders = source.Orders?.Count ?? 0,
            MaskedEmail = MaskEmail(source.Email)
        };
    }


    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return string.Empty;
        }
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return email;
        }
        return $"{email[0]}***{email.Substring(atIndex)}";
    }
}

/// <summary>
/// DTO for order data
/// </summary>
[MapFrom(typeof(Order))]
public partial class OrderDto
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating/updating products - using MapTo
/// Has 3 unmapped properties that need to be set from somewhere else
/// </summary>
[MapTo(typeof(Product))]
public partial class ProductCreateDto
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public string Sku { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public Category Category { get; set; } = null!;

    // Product has these additional fields that need to be populated
    private static partial ProductUnmappedProperties GetProductUnmappedProperties(IMapper mapper,
        ProductCreateDto source) => new()
    {
        Id = 0, // Will be set by database
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        InternalNotes = string.Empty,
        ProductGuid = Guid.NewGuid(),
        Reviews = new List<Review>()
    };
}

/// <summary>
/// DTO for Vehicle that inherits from BaseDto
/// Tests that properties from base classes are properly mapped
/// </summary>
[MapFrom(typeof(Vehicle))]
public partial class VehicleReadDto : BaseDto
{
    public string Make { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int Year { get; set; }

    public string Color { get; set; } = string.Empty;
}

/// <summary>
/// DTO for creating Vehicle that inherits from BaseDto
/// Tests MapTo with inheritance
/// </summary>
[MapTo(typeof(Vehicle))]
public partial class VehicleCreateDto : BaseDto
{
    public string Make { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int Year { get; set; }

    public string Color { get; set; } = string.Empty;

    private static partial VehicleUnmappedProperties GetVehicleUnmappedProperties(IMapper mapper,
        VehicleCreateDto source) => new() { IsDeleted = false };

}

/// <summary>
/// Record DTO for reading contact data (testing MapFrom with records)
/// Has 1 unmapped property: FullName
/// </summary>
[MapFrom(typeof(Contact))]
public partial record ContactReadDto
{
    public int Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    // Unmapped property (computed)
    public required string FullName { get; init; }

    private static partial ContactUnmappedProperties GetContactUnmappedProperties(IMapper mapper, Contact source) =>
        new() { FullName = $"{source.FirstName} {source.LastName}" };
}

/// <summary>
/// Record DTO for reading address data (testing simple MapFrom with records, no unmapped properties)
/// </summary>
[MapFrom(typeof(Address))]
public partial record AddressReadDto
{
    public int Id { get; init; }

    public string Street { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string PostalCode { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;
}

/// <summary>
/// Record DTO for creating notes (testing MapTo with records)
/// </summary>
[MapTo(typeof(Note))]
public partial record NoteCreateDto
{
    public string Title { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    private static partial NoteUnmappedProperties GetNoteUnmappedProperties(IMapper mapper, NoteCreateDto source) =>
        new()
        {
            Id = 0,
            CreatedAt = DateTime.UtcNow
        };
}

/// <summary>
/// DTO for testing AutoPropertyChain with MapFrom
/// Flattens Company.Address.City to AddressCity, Company.Contact.Email to ContactEmail, etc.
/// </summary>
[MapFrom(typeof(Company))]
[AutoPropertyChain]
public partial class CompanyFlatDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Auto-mapped via property chain: Address.City
    public string AddressCity { get; set; } = string.Empty;

    // Auto-mapped via property chain: Address.Street
    public string AddressStreet { get; set; } = string.Empty;

    // Auto-mapped via property chain: Address.PostalCode
    public string AddressPostalCode { get; set; } = string.Empty;

    // Auto-mapped via property chain: Contact.FirstName
    public string ContactFirstName { get; set; } = string.Empty;

    // Auto-mapped via property chain: Contact.Email
    public string ContactEmail { get; set; } = string.Empty;

    // Deeply nested: Address.Country.Name (3 levels deep)
    public string AddressCountryName { get; set; } = string.Empty;

    // Deeply nested: Address.Country.Code (3 levels deep)
    public string AddressCountryCode { get; set; } = string.Empty;
}

/// <summary>
/// DTO for testing AutoPropertyChain with MapTo
/// Maps flat properties back to nested objects
/// </summary>
[MapTo(typeof(FlatCompany))]
[AutoPropertyChain]
public partial class CompanyWithNestedDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // These nested properties will be flattened to AddressCity, AddressStreet, ContactEmail
    public NestedAddress Address { get; set; } = null!;

    public NestedContact Contact { get; set; } = null!;
}

/// <summary>
/// Nested address for CompanyWithNestedDto
/// </summary>
public class NestedAddress
{
    public string City { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;
}

/// <summary>
/// Nested contact for CompanyWithNestedDto
/// </summary>
public class NestedContact
{
    public string Email { get; set; } = string.Empty;
}
