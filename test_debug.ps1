$dash = curl.exe -s -b c:\Temp\ExpenseTracker\harsh_cookie.txt http://localhost:5247/Dashboard/Index
$dash -split "`n" | Select-String -Pattern "Budget remaining" -Context 0,5
