namespace PaisaFlow.Core.Entities;

public class Budget
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public UserAccount? User { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public decimal LimitAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int AlertThresholdPercent { get; set; } = 85;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
