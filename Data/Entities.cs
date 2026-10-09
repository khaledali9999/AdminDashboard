namespace AdminDashboard.Data;

public class User { public int Id { get; set; } public string Email { get; set; } = ""; public string FullName { get; set; } = ""; public string PasswordHash { get; set; } = ""; public string Role { get; set; } = "Sales"; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class Category { public int Id { get; set; } public string Name { get; set; } = ""; }
public class Product { public int Id { get; set; } public string Name { get; set; } = ""; public decimal Price { get; set; } public int Stock { get; set; } public int CategoryId { get; set; } public Category? Category { get; set; } }
public class Order { public int Id { get; set; } public int UserId { get; set; } public User? User { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public decimal Total { get; set; } public List<OrderItem> Items { get; set; } = new(); }
public class OrderItem { public int Id { get; set; } public int OrderId { get; set; } public int ProductId { get; set; } public Product? Product { get; set; } public int Quantity { get; set; } public decimal UnitPrice { get; set; } }
