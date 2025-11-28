using System;
using System.Collections.Generic;
using System.Linq;
using TenJames.CompMap.Mappper;
using Xunit;

namespace TenJames.CompMap.IntegrationTests;

public class MappingIntegrationTests
{
    private readonly IMapper _mapper;

    public MappingIntegrationTests()
    {
        _mapper = new BaseMapper();
    }

    [Fact]
    public void ProductReadDto_MapFrom_ShouldMapAllMatchingProperties()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "Electronics",
            Description = "Electronic devices"
        };

        var reviews = new List<Review>
        {
            new() { Id = 1, Comment = "Great!", Rating = 5, CreatedAt = DateTime.Now },
            new() { Id = 2, Comment = "Good", Rating = 4, CreatedAt = DateTime.Now }
        };

        var product = new Product
        {
            Id = 100,
            Name = "Laptop",
            Description = "High performance laptop",
            Price = 999.99m,
            StockQuantity = 10,
            Sku = "LAP-001",
            CreatedAt = new DateTime(2024, 1, 1),
            UpdatedAt = new DateTime(2024, 1, 15),
            IsActive = true,
            InternalNotes = "Premium product",
            ProductGuid = Guid.NewGuid(),
            Category = category,
            Reviews = reviews
        };

        // Act
        var dto = ProductReadDto.MapFrom(_mapper, product);

        // Assert - Matching properties
        Assert.Equal(product.Id, dto.Id);
        Assert.Equal(product.Name, dto.Name);
        Assert.Equal(product.Description, dto.Description);
        Assert.Equal(product.Price, dto.Price);
        Assert.Equal(product.StockQuantity, dto.StockQuantity);
        Assert.Equal(product.Sku, dto.Sku);
        Assert.Equal(product.CreatedAt, dto.CreatedAt);
        Assert.Equal(product.IsActive, dto.IsActive);
        Assert.Equal(product.ProductGuid, dto.ProductGuid);

        // Assert - Unmapped properties (computed)
        Assert.Equal("Laptop (LAP-001)", dto.DisplayName);
        Assert.True(dto.IsAvailable); // IsActive && StockQuantity > 0
        Assert.Equal("$999.99", dto.FormattedPrice);
        Assert.Equal(2, dto.ReviewCount);
        Assert.Equal(4.5, dto.AverageRating);
    }

    [Fact]
    public void ProductReadDto_MapFrom_WithNoStock_ShouldSetIsAvailableToFalse()
    {
        // Arrange
        var product = new Product
        {
            Id = 101,
            Name = "Out of Stock Item",
            Description = "Test",
            Price = 50m,
            StockQuantity = 0,
            Sku = "OOS-001",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsActive = true,
            InternalNotes = "",
            ProductGuid = Guid.NewGuid(),
            Category = new Category { Id = 1, Name = "Test", Description = "Test" },
            Reviews = new List<Review>()
        };

        // Act
        var dto = ProductReadDto.MapFrom(_mapper, product);

        // Assert
        Assert.False(dto.IsAvailable);
    }

    [Fact]
    public void UserReadDto_MapFrom_ShouldMapAllMatchingProperties()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() { Id = 1, OrderNumber = "ORD-001", TotalAmount = 100m, CreatedAt = DateTime.Now },
            new() { Id = 2, OrderNumber = "ORD-002", TotalAmount = 200m, CreatedAt = DateTime.Now }
        };

        var user = new User
        {
            Id = 1,
            Username = "johndoe",
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 5, 15),
            PhoneNumber = "+1234567890",
            Address = "123 Main St",
            CreatedAt = new DateTime(2020, 1, 1),
            LastLoginAt = DateTime.Now,
            IsEmailVerified = true,
            PasswordHash = "hashed_password_should_not_be_mapped",
            Orders = orders
        };

        // Act
        var dto = UserReadDto.MapFrom(_mapper, user);

        // Assert - Matching properties
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.Username, dto.Username);
        Assert.Equal(user.Email, dto.Email);
        Assert.Equal(user.FirstName, dto.FirstName);
        Assert.Equal(user.LastName, dto.LastName);
        Assert.Equal(user.DateOfBirth, dto.DateOfBirth);
        Assert.Equal(user.PhoneNumber, dto.PhoneNumber);
        Assert.Equal(user.CreatedAt, dto.CreatedAt);
        Assert.Equal(user.IsEmailVerified, dto.IsEmailVerified);

        // Assert - Unmapped properties (computed)
        Assert.Equal("John Doe", dto.FullName);
        var expectedAge = DateTime.Now.Year - 1990;
        if (DateTime.Now < new DateTime(DateTime.Now.Year, 5, 15)) expectedAge--;
        Assert.Equal(expectedAge, dto.Age);
        Assert.True(dto.IsAdult);
        Assert.True(dto.MembershipDuration > 0);
        Assert.Equal(2, dto.TotalOrders);
        Assert.Equal("j***@example.com", dto.MaskedEmail);
    }

    [Fact]
    public void CategoryDto_MapFrom_ShouldMapAllProperties()
    {
        // Arrange
        var category = new Category
        {
            Id = 5,
            Name = "Books",
            Description = "All kinds of books"
        };

        // Act
        var dto = CategoryDto.MapFrom(_mapper, category);

        // Assert
        Assert.Equal(category.Id, dto.Id);
        Assert.Equal(category.Name, dto.Name);
        Assert.Equal(category.Description, dto.Description);
    }

    [Fact]
    public void ReviewDto_MapFrom_ShouldMapPropertiesAndComputeFormattedRating()
    {
        // Arrange
        var review = new Review
        {
            Id = 10,
            Comment = "Excellent product!",
            Rating = 5,
            CreatedAt = new DateTime(2024, 11, 1)
        };

        // Act
        var dto = ReviewDto.MapFrom(_mapper, review);

        // Assert
        Assert.Equal(review.Id, dto.Id);
        Assert.Equal(review.Comment, dto.Comment);
        Assert.Equal(review.Rating, dto.Rating);
        Assert.Equal(review.CreatedAt, dto.CreatedAt);
        Assert.Equal("5/5 stars", dto.FormattedRating);
    }

    [Fact]
    public void OrderDto_MapFrom_ShouldMapAllProperties()
    {
        // Arrange
        var order = new Order
        {
            Id = 42,
            OrderNumber = "ORD-12345",
            TotalAmount = 599.99m,
            CreatedAt = new DateTime(2024, 10, 15)
        };

        // Act
        var dto = OrderDto.MapFrom(_mapper, order);

        // Assert
        Assert.Equal(order.Id, dto.Id);
        Assert.Equal(order.OrderNumber, dto.OrderNumber);
        Assert.Equal(order.TotalAmount, dto.TotalAmount);
        Assert.Equal(order.CreatedAt, dto.CreatedAt);
    }

    [Fact]
    public void ProductCreateDto_MapTo_ShouldCreateProductWithUnmappedProperties()
    {
        // Arrange
        var category = new Category
        {
            Id = 3,
            Name = "Clothing",
            Description = "Fashion items"
        };

        var createDto = new ProductCreateDto
        {
            Name = "T-Shirt",
            Description = "Cotton T-Shirt",
            Price = 29.99m,
            StockQuantity = 100,
            Sku = "TSH-001",
            IsActive = true,
            Category = category
        };

        // Act
        var product = createDto.MapTo(_mapper);

        // Assert - Matching properties
        Assert.Equal(createDto.Name, product.Name);
        Assert.Equal(createDto.Description, product.Description);
        Assert.Equal(createDto.Price, product.Price);
        Assert.Equal(createDto.StockQuantity, product.StockQuantity);
        Assert.Equal(createDto.Sku, product.Sku);
        Assert.Equal(createDto.IsActive, product.IsActive);
        Assert.Equal(createDto.Category, product.Category);

        // Assert - Unmapped properties (auto-generated)
        Assert.Equal(0, product.Id); // Default for new entity
        Assert.NotEqual(default(DateTime), product.CreatedAt);
        Assert.NotEqual(default(DateTime), product.UpdatedAt);
        Assert.Equal(string.Empty, product.InternalNotes);
        Assert.NotEqual(Guid.Empty, product.ProductGuid);
        Assert.NotNull(product.Reviews);
        Assert.Empty(product.Reviews);
    }

    [Fact]
    public void BaseMapper_ShouldMapNestedCollections()
    {
        // Arrange
        var reviews = new List<Review>
        {
            new() { Id = 1, Comment = "Great!", Rating = 5, CreatedAt = DateTime.Now },
            new() { Id = 2, Comment = "Good", Rating = 4, CreatedAt = DateTime.Now }
        };

        var product = new Product
        {
            Id = 200,
            Name = "Test Product",
            Description = "Test",
            Price = 100m,
            StockQuantity = 5,
            Sku = "TEST-001",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsActive = true,
            InternalNotes = "",
            ProductGuid = Guid.NewGuid(),
            Category = new Category { Id = 1, Name = "Test", Description = "Test" },
            Reviews = reviews
        };

        // Act
        var dto = ProductReadDto.MapFrom(_mapper, product);

        // Assert
        Assert.NotNull(dto.Reviews);
        Assert.Equal(2, dto.Reviews.Count);

        var reviewDtos = dto.Reviews.ToList();
        Assert.Equal(reviews[0].Id, reviewDtos[0].Id);
        Assert.Equal(reviews[0].Comment, reviewDtos[0].Comment);
        Assert.Equal(reviews[1].Id, reviewDtos[1].Id);
        Assert.Equal(reviews[1].Comment, reviewDtos[1].Comment);
    }

    [Fact]
    public void BaseMapper_ShouldMapNestedObject()
    {
        // Arrange
        var category = new Category
        {
            Id = 7,
            Name = "Sports",
            Description = "Sports equipment"
        };

        var product = new Product
        {
            Id = 300,
            Name = "Basketball",
            Description = "Professional basketball",
            Price = 49.99m,
            StockQuantity = 20,
            Sku = "BALL-001",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsActive = true,
            InternalNotes = "",
            ProductGuid = Guid.NewGuid(),
            Category = category,
            Reviews = new List<Review>()
        };

        // Act
        var dto = ProductReadDto.MapFrom(_mapper, product);

        // Assert
        Assert.NotNull(dto.Category);
        Assert.Equal(category.Id, dto.Category.Id);
        Assert.Equal(category.Name, dto.Category.Name);
        Assert.Equal(category.Description, dto.Category.Description);
    }

    [Fact]
    public void UserReadDto_WithNoOrders_ShouldHandleEmptyCollection()
    {
        // Arrange
        var user = new User
        {
            Id = 2,
            Username = "janedoe",
            Email = "jane@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            DateOfBirth = new DateTime(1995, 8, 20),
            PhoneNumber = "+9876543210",
            Address = "456 Oak St",
            CreatedAt = DateTime.Now,
            LastLoginAt = DateTime.Now,
            IsEmailVerified = false,
            PasswordHash = "hashed",
            Orders = new List<Order>()
        };

        // Act
        var dto = UserReadDto.MapFrom(_mapper, user);

        // Assert
        Assert.NotNull(dto.Orders);
        Assert.Empty(dto.Orders);
        Assert.Equal(0, dto.TotalOrders);
    }

    [Fact]
    public void ProductReadDto_WithNoReviews_ShouldSetAverageRatingToZero()
    {
        // Arrange
        var product = new Product
        {
            Id = 400,
            Name = "New Product",
            Description = "Brand new",
            Price = 199.99m,
            StockQuantity = 50,
            Sku = "NEW-001",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsActive = true,
            InternalNotes = "",
            ProductGuid = Guid.NewGuid(),
            Category = new Category { Id = 1, Name = "Test", Description = "Test" },
            Reviews = new List<Review>()
        };

        // Act
        var dto = ProductReadDto.MapFrom(_mapper, product);

        // Assert
        Assert.Equal(0, dto.ReviewCount);
        Assert.Equal(0.0, dto.AverageRating);
    }
}
