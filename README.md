# 💰 PaisaFlow — Personal Finance & Expense Tracker

> **Product Philosophy:** *One tap to add. One glance to understand. Zero financial clutter.*

A high-performance personal finance and expense tracking platform built with **.NET 10**, **Entity Framework Core**, **PostgreSQL / SQLite multi-provider support**, and a responsive **iOS Glassmorphism** user experience.

---

## 📱 Features

1. **Home Dashboard**
   - Live greeting: *"Good evening, Harsh 👋"*
   - Total Balance with 1-tap Privacy Masking (`₹••••••`)
   - October Spending: **₹18,420**
   - Monthly Budget Remaining: **₹23,580 / ₹42,000** with progress bar
   - Top Spending Categories with emojis (🍔 Food, 🚕 Travel, etc.)
   - Recent Activity timeline with payment method badges

2. **⚡ < 5-Second Quick Add Entry (The Core Feature)**
   - Open app → Enter Amount → Choose Category → Save.
   - Smart defaults: Remembers past habits (e.g. ₹150 defaults to Food & UPI).
   - Natural Language Quick Add bar: Type `500 lunch UPI` or `120 auto cash` — auto-parsed and recorded in 1 tap!

3. **🍔 16+ Pre-Seeded Categories & Custom Creator**
   - Food, Groceries, Transport, Rent, Bills, Recharge, Shopping, Entertainment, Health, Education, Travel, Work, Family, Investment, EMI, Other.
   - Income categories: Salary, Freelance, Business, Bonus, Interest, Other Income.
   - Custom category creation with emoji picker and color selection.

4. **💼 Dual Income & Expense Tracking**
   - Full support for Income vs. Expense tracking with net savings calculation.

5. **🏦 Accounts & Payment Methods**
   - Track balances across SBI Bank, HDFC Bank, Cash, Credit Card, and UPI.
   - Balances automatically update as expenses or incomes are recorded.

6. **🔍 Activity History & Deep Search**
   - Search by keyword (`"grocery"`, `"500"`, `"UPI"`).
   - Filter by Date, Category, Payment Method, Type (Income/Expense), and Amount.

7. **🎯 Monthly Budgets & 85% Warning Alerts**
   - Overall monthly budget + individual category budgets.
   - Visual progress bars with remaining limits.
   - Non-intrusive alert: *⚠️ "You've used 85% of your Food budget."*

8. **🔄 Recurring Expenses & Subscriptions**
   - Track monthly bills: Rent (1st), Jio Fiber/Internet (15th), EMI (7th).
   - 1-tap "Pay Now" processing and auto-generation on due dates.

9. **📊 Actionable Smart Analytics**
   - Category spend percentages.
   - Daily spending trend timeline (Days 1 to 31).
   - Actionable insights:
     - 💡 *Your food spending is 18% higher than last month.*
     - 💡 *You spent ₹2,400 on shopping this month.*
     - 💡 *Your average daily spending is ₹614.*
     - 💡 *You have saved 56% of your income so far.*

10. **📅 iOS Calendar View**
    - Monthly calendar grid with visual spending dots on active days.
    - Click any day (e.g., 15 October) to inspect all transactions and daily total.

11. **🌴 Friends & Group Expense Split (Trip to Goa)**
    - Group cost sharing for trips and outings.
    - Optimal Debt Settlement Algorithm (Min-Cash-Flow):
      - Amit → Harsh: ₹1,800
      - Rahul → Harsh: ₹1,650
      - Akash → Harsh: ₹900
      - Akash → Amit: ₹750
    - Direct **1-tap UPI Payment Intent links** (`upi://pay?pa=...&pn=...&am=...`).

12. **📤 Data Export & Backup**
    - One-click export to **CSV**, **Excel (.xls / TSV)**, and **JSON**.
    - Full JSON Backup & Restore for complete data portability.

13. **🔐 Security & Privacy**
    - "Hide Balance" toggle (`₹••••••`).
    - Face ID / Biometric PIN lock simulation.
    - Local and Cloud backup indicators.

---

## 🏗️ Architecture & Database

Modeled after `C:\Temp\jobApply`:

```
c:\Temp\ExpenseTracker\
 ├── src\
 │    ├── PaisaFlow.Core\             # Entities, Enums, DTOs, Interfaces
 │    ├── PaisaFlow.Infrastructure\   # AppDbContext, DbInitializer, Services
 │    └── PaisaFlow.Web\              # Controllers, Views, Glassmorphism UI, Program.cs
 └── PaisaFlow.sln
```

### Multi-Database Support
- **SQLite (Default Fallback):** Zero-configuration out of the box (`paisaflow.db`).
- **PostgreSQL (Neon / Cloud / Local):** Configurable in `appsettings.json` under `NeonPostgresConnection`.
- **SQL Server:** Configurable via `SqlServerFallback`.
- **Auto-Initialization:** Database tables and initial seed data (Harsh's account, categories, Goa trip, recurring bills) are automatically created on first launch via `DbInitializer.InitializeAsync`.

---

## 🚀 Running the App

```powershell
cd C:\Temp\ExpenseTracker
dotnet run --project src/PaisaFlow.Web/PaisaFlow.Web.csproj --urls "http://localhost:5247"
```

Open your browser at: **`http://localhost:5247`**
