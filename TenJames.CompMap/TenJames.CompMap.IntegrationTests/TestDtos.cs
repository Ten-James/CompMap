using System;
using System.Collections.Generic;
using TenJames.CompMap.Attributes;
using TenJames.CompMap.Mappper;

namespace TenJames.CompMap.IntegrationTests;

using System.Linq;

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
    private static partial ProductUnmappedProperties GetProductUnmappedProperties(IMapper mapper, Product source)
    {
        return new ProductUnmappedProperties
        {
            DisplayName = $"{source.Name} ({source.Sku})",
            IsAvailable = source.IsActive && source.StockQuantity > 0,
            FormattedPrice = $"${source.Price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}",
            ReviewCount = source.Reviews?.Count ?? 0,
            AverageRating = source.Reviews?.Count > 0
                ? source.Reviews.Average(r => r.Rating)
                : 0.0
        };
    }
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

    private static partial ReviewUnmappedProperties GetReviewUnmappedProperties(IMapper mapper, Review source)
    {
        return new ReviewUnmappedProperties
        {
            FormattedRating = $"{source.Rating}/5 stars"
        };
    }
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
        if (DateTime.Now < source.DateOfBirth.AddYears(age)) age--;

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
        if (string.IsNullOrEmpty(email)) return string.Empty;
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1) return email;
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
    private static partial ProductUnmappedProperties GetProductUnmappedProperties(IMapper mapper, ProductCreateDto source)
    {
        return new ProductUnmappedProperties
        {
            Id = 0, // Will be set by database
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            InternalNotes = string.Empty,
            ProductGuid = Guid.NewGuid(),
            Reviews = new List<Review>()
        };
    }
}
