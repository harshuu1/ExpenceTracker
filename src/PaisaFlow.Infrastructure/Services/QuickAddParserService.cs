using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.DTOs;
using PaisaFlow.Core.Enums;
using PaisaFlow.Core.Interfaces;
using PaisaFlow.Infrastructure.Data;

namespace PaisaFlow.Infrastructure.Services;

public class QuickAddParserService : IQuickAddParser
{
    private readonly AppDbContext _context;

    public QuickAddParserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<QuickAddResult> ParseInputAsync(int userId, string input)
    {
        var result = new QuickAddResult
        {
            Success = false,
            Date = DateTime.Now,
            PaymentMethod = "UPI",
            Type = TransactionType.Expense
        };

        if (string.IsNullOrWhiteSpace(input))
        {
            result.Message = "Please enter an amount and description (e.g., '500 lunch UPI')";
            return result;
        }

        var cleaned = input.Trim();

        // 1. Extract Amount
        var amountMatch = Regex.Match(cleaned, @"(?:₹|rs\.?|inr)?\s*(\d+(?:\.\d{1,2})?)", RegexOptions.IgnoreCase);
        if (!amountMatch.Success || !decimal.TryParse(amountMatch.Groups[1].Value, out var amount) || amount <= 0)
        {
            result.Message = "Could not find a valid amount in your input.";
            return result;
        }
        result.Amount = amount;

        // Remove the parsed amount from token matching
        var remainingText = cleaned.Remove(amountMatch.Index, amountMatch.Length).Trim();

        // 2. Extract Payment Method
        if (Regex.IsMatch(remainingText, @"\b(cash|notes?|currency)\b", RegexOptions.IgnoreCase))
        {
            result.PaymentMethod = "Cash";
            remainingText = Regex.Replace(remainingText, @"\b(cash|notes?|currency)\b", "", RegexOptions.IgnoreCase).Trim();
        }
        else if (Regex.IsMatch(remainingText, @"\b(credit(?:\s*card)?|cc)\b", RegexOptions.IgnoreCase))
        {
            result.PaymentMethod = "Credit Card";
            remainingText = Regex.Replace(remainingText, @"\b(credit(?:\s*card)?|cc)\b", "", RegexOptions.IgnoreCase).Trim();
        }
        else if (Regex.IsMatch(remainingText, @"\b(debit(?:\s*card)?|dc)\b", RegexOptions.IgnoreCase))
        {
            result.PaymentMethod = "Debit Card";
            remainingText = Regex.Replace(remainingText, @"\b(debit(?:\s*card)?|dc)\b", "", RegexOptions.IgnoreCase).Trim();
        }
        else if (Regex.IsMatch(remainingText, @"\b(bank(?:\s*transfer)?|neft|imps|rtgs)\b", RegexOptions.IgnoreCase))
        {
            result.PaymentMethod = "Bank Transfer";
            remainingText = Regex.Replace(remainingText, @"\b(bank(?:\s*transfer)?|neft|imps|rtgs)\b", "", RegexOptions.IgnoreCase).Trim();
        }
        else if (Regex.IsMatch(remainingText, @"\b(upi|gpay|google\s*pay|phonepe|paytm)\b", RegexOptions.IgnoreCase))
        {
            result.PaymentMethod = "UPI";
            remainingText = Regex.Replace(remainingText, @"\b(upi|gpay|google\s*pay|phonepe|paytm)\b", "", RegexOptions.IgnoreCase).Trim();
        }

        // 3. Detect Income keywords
        if (Regex.IsMatch(remainingText, @"\b(salary|freelance|stipend|bonus|interest|income|credited)\b", RegexOptions.IgnoreCase))
        {
            result.Type = TransactionType.Income;
        }

        // 4. Resolve Categories from database & smart keyword synonyms
        var categories = await _context.Categories
            .Where(c => c.UserId == null || c.UserId == userId)
            .ToListAsync();

        var lowerRemaining = remainingText.ToLowerInvariant();
        var matchedCat = categories.FirstOrDefault(c => 
            lowerRemaining.Contains(c.Name.ToLowerInvariant()));

        if (matchedCat == null)
        {
            // Synonym mapping
            if (Regex.IsMatch(lowerRemaining, @"\b(lunch|dinner|breakfast|food|snack|tea|coffee|cafe|burger|pizza|biryani|swiggy|zomato|restaurant|mcdonalds|starbucks)\b"))
            {
                matchedCat = categories.FirstOrDefault(c => c.Name.Equals("Food", StringComparison.OrdinalIgnoreCase));
            }
            else if (Regex.IsMatch(lowerRemaining, @"\b(uber|ola|auto|cab|taxi|metro|bus|train|petrol|fuel|diesel|flight|travel|ride)\b"))
            {
                matchedCat = categories.FirstOrDefault(c => c.Name.Equals("Transport", StringComparison.OrdinalIgnoreCase) || c.Name.Equals("Travel", StringComparison.OrdinalIgnoreCase));
            }
            else if (Regex.IsMatch(lowerRemaining, @"\b(grocery|groceries|supermarket|blinkit|zepto|instamart|veggies|vegetables|fruits|milk|dmart)\b"))
            {
                matchedCat = categories.FirstOrDefault(c => c.Name.Equals("Groceries", StringComparison.OrdinalIgnoreCase));
            }
            else if (Regex.IsMatch(lowerRemaining, @"\b(recharge|jio|airtel|vi|broadband|wifi|mobile)\b"))
            {
                matchedCat = categories.FirstOrDefault(c => c.Name.Equals("Recharge", StringComparison.OrdinalIgnoreCase));
            }
            else if (Regex.IsMatch(lowerRemaining, @"\b(bill|bills|electricity|water|gas|cylinder|current|bescom|tata\s*power)\b"))
            {
                matchedCat = categories.FirstOrDefault(c => c.Name.Equals("Bills", StringComparison.OrdinalIgnoreCase));
            }
            else if (Regex.IsMatch(lowerRemaining, @"\b(shopping|clothes|shoes|dress|amazon|flipkart|myntra|zara|h&m)\b"))
            {
                matchedCat = categories.FirstOrDefault(c => c.Name.Equals("Shopping", StringComparison.OrdinalIgnoreCase));
            }
            else if (Regex.IsMatch(lowerRemaining, @"\b(movie|cinema|netflix|prime|hotstar|party|club|pub|game|entertainment)\b"))
            {
                matchedCat = categories.FirstOrDefault(c => c.Name.Equals("Entertainment", StringComparison.OrdinalIgnoreCase));
            }
            else if (Regex.IsMatch(lowerRemaining, @"\b(rent|room|flat|apartment|brokerage)\b"))
            {
                matchedCat = categories.FirstOrDefault(c => c.Name.Equals("Rent", StringComparison.OrdinalIgnoreCase));
            }
            else if (Regex.IsMatch(lowerRemaining, @"\b(medicine|doctor|pharmacy|hospital|tablet|pills|clinic|health)\b"))
            {
                matchedCat = categories.FirstOrDefault(c => c.Name.Equals("Health", StringComparison.OrdinalIgnoreCase));
            }
            else if (Regex.IsMatch(lowerRemaining, @"\b(salary|paycheck|monthly\s*pay)\b"))
            {
                matchedCat = categories.FirstOrDefault(c => c.Name.Equals("Salary", StringComparison.OrdinalIgnoreCase));
                result.Type = TransactionType.Income;
            }
            else if (Regex.IsMatch(lowerRemaining, @"\b(emi|loan|interest)\b"))
            {
                matchedCat = categories.FirstOrDefault(c => c.Name.Equals("EMI", StringComparison.OrdinalIgnoreCase));
            }
        }

        // Smart default fallback based on amount if no keyword:
        if (matchedCat == null)
        {
            if (amount <= 300)
            {
                // Typical food or transport
                matchedCat = categories.FirstOrDefault(c => c.Name.Equals("Food", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                matchedCat = categories.FirstOrDefault(c => c.Name.Equals("Other", StringComparison.OrdinalIgnoreCase))
                             ?? categories.FirstOrDefault();
            }
        }

        if (matchedCat != null)
        {
            result.CategoryId = matchedCat.Id;
            result.CategoryName = matchedCat.Name;
            result.CategoryIcon = matchedCat.Icon;
        }

        // Assign Account
        var accounts = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
        if (result.PaymentMethod == "Cash")
        {
            result.AccountId = accounts.FirstOrDefault(a => a.Type == AccountType.Cash)?.Id;
        }
        else if (result.PaymentMethod == "Credit Card")
        {
            result.AccountId = accounts.FirstOrDefault(a => a.Type == AccountType.CreditCard)?.Id;
        }
        else
        {
            result.AccountId = accounts.FirstOrDefault(a => a.IsDefault)?.Id ?? accounts.FirstOrDefault()?.Id;
        }

        result.Note = string.IsNullOrWhiteSpace(remainingText) ? null : remainingText.Trim();
        result.Success = true;
        result.Message = $"Parsed ₹{result.Amount:N0} for {result.CategoryName} via {result.PaymentMethod}";

        return result;
    }
}
