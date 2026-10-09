using System.ComponentModel.DataAnnotations;
using AdminDashboard.Validation;
namespace AdminDashboard.Services;

public record OrderItemDto(int ProductId, int Quantity);
public record CreateOrderDto(List<OrderItemDto> Items);
public record ProductDto(int Id, string Name, decimal Price, int Stock, string Category);
public record UserDto(int Id, string Email, string FullName, string Role);
public record OrderDto(int Id, string User, DateTime CreatedAt, decimal Total, int ItemsCount);
public record CartLine(int ProductId, string Name, decimal Price, int Quantity);

public class CreateProductDto
{
    [UniqueProductName] public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
}
public class CreateUserDto
{
    [UniqueEmail] public string Email { get; set; } = "";
    [Required] public string FullName { get; set; } = "";
    [Required, MinLength(6)] public string Password { get; set; } = "";
    public string Role { get; set; } = "Sales";
}
public class AddToCartDto { public int ProductId { get; set; } public int Quantity { get; set; } = 1; }
