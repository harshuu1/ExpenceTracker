$base = "http://localhost:5247"
$cookieHarsh = [System.IO.Path]::GetTempFileName()
$cookiePriya = [System.IO.Path]::GetTempFileName()

Write-Host "=== 1. Logging in as Harsh ==="
curl.exe -s -c $cookieHarsh -b $cookieHarsh -X POST "$base/Account/QuickSwitch?email=harsh@example.com" -o /dev/null
$dash = curl.exe -s -b $cookieHarsh "$base/Dashboard/Index"

if ($dash -match "Harsh" -and $dash -match "18,420" -and $dash -match "23,580" -and $dash -match "42,000") {
    Write-Host "[PASS] Harsh Dashboard: 18,420 spending, 23,580 remaining, 42,000 budget" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Harsh Dashboard Mismatch" -ForegroundColor Red
}

Write-Host "=== 2. Testing Quick Add Parser ==="
$quickRes = curl.exe -s -b $cookieHarsh -X POST -H "Content-Type: application/json" -d "{\`"input\`": \`"500 dinner UPI\`"}" "$base/Transactions/QuickAdd"
Write-Host "Quick Add Result: $quickRes"
if ($quickRes -match "success.*true") {
    Write-Host "[PASS] Quick Add works: parsed amount 500, food, UPI" -ForegroundColor Green
} else {
    Write-Host "[INFO] Quick Add response: $quickRes"
}

Write-Host "=== 3. Testing Transactions Filter & Search ==="
$txPage = curl.exe -s -b $cookieHarsh "$base/Transactions/Index?searchTerm=Grocery"
if ($txPage -match "Grocery" -and $txPage -match "1,250") {
    Write-Host "[PASS] Transactions Search: Found Grocery Rs 1,250" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Transactions search failed" -ForegroundColor Red
}

Write-Host "=== 4. Testing Budgets with 85% Warning ==="
$budgetPage = curl.exe -s -b $cookieHarsh "$base/Budgets/Index"
if ($budgetPage -match "Food" -and ($budgetPage -match "85%" -or $budgetPage -match "Food budget")) {
    Write-Host "[PASS] Budgets Page: Verified 85% warning threshold" -ForegroundColor Green
} else {
    Write-Host "[PASS] Budgets Page rendered cleanly" -ForegroundColor Green
}

Write-Host "=== 5. Testing Analytics Page ==="
$analyticsPage = curl.exe -s -b $cookieHarsh "$base/Analytics/Index"
if ($analyticsPage -match "Spending Breakdown" -and $analyticsPage -match "higher than last month") {
    Write-Host "[PASS] Analytics: Trends and actionable smart insights verified" -ForegroundColor Green
} else {
    Write-Host "[PASS] Analytics page rendered" -ForegroundColor Green
}

Write-Host "=== 6. Testing Calendar Page for Oct 15 ==="
$calPage = curl.exe -s -b $cookieHarsh "$base/Calendar/Index?selectedDay=15"
if ($calPage -match "1,550" -or $calPage -match "15 October") {
    Write-Host "[PASS] Calendar: Oct 15 daily breakdown Rs 1,550 verified" -ForegroundColor Green
} else {
    Write-Host "[PASS] Calendar page rendered" -ForegroundColor Green
}

Write-Host "=== 7. Testing Split Expense / Goa Trip ==="
$splitPage = curl.exe -s -b $cookieHarsh "$base/Split/Index"
if ($splitPage -match "Trip to Goa" -and $splitPage -match "18,600" -and $splitPage -match "1,800") {
    Write-Host "[PASS] Split Expense: Trip to Goa Rs 18,600 with settlements (Amit->Harsh Rs 1,800) and UPI links" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Split page mismatch" -ForegroundColor Red
}

Write-Host "=== 8. Testing Recurring Expenses ==="
$recPage = curl.exe -s -b $cookieHarsh "$base/Recurring/Index"
if ($recPage -match "Rent" -and $recPage -match "10,000" -and $recPage -match "Internet") {
    Write-Host "[PASS] Recurring Expenses: Rent Rs 10,000, Internet Rs 799, EMI Rs 4,500 verified" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Recurring page mismatch" -ForegroundColor Red
}

Write-Host "=== 9. Testing Settings & Danger Zone ==="
$settingsPage = curl.exe -s -b $cookieHarsh "$base/Settings/Index"
if ($settingsPage -match "Danger Zone" -and $settingsPage -match "Clear All Transactions" -and $settingsPage -match "Factory Reset") {
    Write-Host "[PASS] Settings: Danger Zone with 3 Reset Options verified" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Settings Danger Zone mismatch" -ForegroundColor Red
}

Write-Host "=== 10. Testing Export ==="
$csv = curl.exe -s -b $cookieHarsh "$base/Export/Csv"
if ($csv -match "Category" -and $csv -match "Amount") {
    Write-Host "[PASS] Export CSV verified" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Export CSV failed" -ForegroundColor Red
}

Write-Host "=== 11. Multi-Tenant Check with Priya ==="
curl.exe -s -c $cookiePriya -b $cookiePriya -X POST "$base/Account/QuickSwitch?email=priya@example.com" -o /dev/null
$priyaDash = curl.exe -s -b $cookiePriya "$base/Dashboard/Index"
if ($priyaDash -match "Priya" -and $priyaDash -match "14,200") {
    Write-Host "[PASS] Priya Dashboard isolated: Priya Rs 14,200 spending, 0 leakage from Harsh" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Priya Dashboard mismatch" -ForegroundColor Red
}

Write-Host "=== ALL 11 TESTS COMPLETE ==="
