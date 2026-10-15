namespace PaisaFlow.Core.Entities;

public class SplitGroup
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public UserAccount? User { get; set; }

    public string Name { get; set; } = "Trip to Goa";
    public string? Description { get; set; }
    public string Currency { get; set; } = "₹";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SplitMember> Members { get; set; } = new List<SplitMember>();
    public ICollection<SplitExpense> Expenses { get; set; } = new List<SplitExpense>();
}

public class SplitMember
{
    public int Id { get; set; }
    public int SplitGroupId { get; set; }
    public SplitGroup? SplitGroup { get; set; }

    public string Name { get; set; } = string.Empty; // e.g. "Harsh", "Amit", "Rahul", "Akash"
    public string? UpiId { get; set; } // e.g. "harsh@okaxis"
    public string? PhoneNumber { get; set; }
    public bool IsCurrentUser { get; set; } = false;

    public ICollection<SplitExpense> ExpensesPaid { get; set; } = new List<SplitExpense>();
    public ICollection<SplitParticipant> ExpenseShares { get; set; } = new List<SplitParticipant>();
}

public class SplitExpense
{
    public int Id { get; set; }
    public int SplitGroupId { get; set; }
    public SplitGroup? SplitGroup { get; set; }

    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;

    public int PaidByMemberId { get; set; }
    public SplitMember? PaidByMember { get; set; }

    public ICollection<SplitParticipant> Participants { get; set; } = new List<SplitParticipant>();
}

public class SplitParticipant
{
    public int Id { get; set; }
    public int SplitExpenseId { get; set; }
    public SplitExpense? SplitExpense { get; set; }

    public int SplitMemberId { get; set; }
    public SplitMember? SplitMember { get; set; }

    public decimal ShareAmount { get; set; }
}
