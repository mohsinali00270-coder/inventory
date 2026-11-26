using SQLite;

namespace ShopLedger.Models;

public class Product
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed, MaxLength(150)] public string Name { get; set; } = "";
    [Indexed, MaxLength(80)] public string Category { get; set; } = "General";
    [MaxLength(20)] public string Unit { get; set; } = "pcs";   // pcs / kg / litre
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal Stock { get; set; }
    public decimal LowStockLevel { get; set; } = 5;
    [Indexed, MaxLength(64)] public string? Barcode { get; set; }
    public string? PhotoPath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Ignore] public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath) && File.Exists(PhotoPath);
    [Ignore] public bool IsLowStock => Stock <= LowStockLevel;
    [Ignore] public bool IsOutOfStock => Stock <= 0;
    /// <summary>Low but not empty (for the amber "Low stock" badge).</summary>
    [Ignore] public bool IsRunningLow => IsLowStock && !IsOutOfStock;
    [Ignore] public bool IsInStock => !IsLowStock;
}
