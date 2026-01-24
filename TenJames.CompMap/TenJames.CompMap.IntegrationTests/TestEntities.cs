namespace TenJames.CompMap.IntegrationTests;

using System;
using System.Collections.Generic;

/// <summary>
/// Entity class representing a product in the database
/// </summary>
public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public string Sku { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActive { get; set; }

    public string InternalNotes { get; set; } = string.Empty;

    public Guid ProductGuid { get; set; }

    public Category Category { get; set; } = null!;

    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

/// <summary>
/// Entity class representing a category
/// </summary>
public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Entity class representing a review
/// </summary>
public class Review
{
    public int Id { get; set; }

    public string Comment { get; set; } = string.Empty;

    public int Rating { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Entity class representing a user in the database
/// </summary>
public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime LastLoginAt { get; set; }

    public bool IsEmailVerified { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

/// <summary>
/// Entity class representing an order
/// </summary>
public class Order
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Base entity class with common properties
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
}

/// <summary>
/// Vehicle entity that inherits from BaseEntity
/// </summary>
public class Vehicle : BaseEntity
{
    public string Make { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int Year { get; set; }

    public string Color { get; set; } = string.Empty;
}

/// <summary>
/// Base DTO class with common properties
/// </summary>
public abstract class BaseDto
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Record entity representing a contact (for testing record support)
/// </summary>
public record Contact
{
    public int Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;
}

/// <summary>
/// Record entity representing an address
/// </summary>
public record Address
{
    public int Id { get; init; }

    public string Street { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string PostalCode { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;
}

/// <summary>
/// Record entity for testing MapTo direction
/// </summary>
public record Note
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Entity for testing AutoPropertyChain - has nested objects
/// </summary>
public class Company
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public CompanyAddress Address { get; set; } = null!;

    public ContactPerson Contact { get; set; } = null!;
}

/// <summary>
/// Nested address for Company
/// </summary>
public class CompanyAddress
{
    public string Street { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public AddressCountry Country { get; set; } = null!;
}

/// <summary>
/// Deeply nested country for testing recursive property chain
/// </summary>
public class AddressCountry
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Nested contact person for Company
/// </summary>
public class ContactPerson
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Flat entity for testing MapTo with AutoPropertyChain
/// </summary>
public class FlatCompany
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string AddressCity { get; set; } = string.Empty;

    public string AddressStreet { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;
}
