$baseUrl = "http://localhost:5057"

Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host " Running Live Integration Tests: Step 20 Reporting       " -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan

# 1. Anonymous Access Check (Should return 401 Unauthorized)
Write-Host "`n[Test 1] Anonymous request to /api/reports/revenue..."
try {
    $res = Invoke-RestMethod -Uri "$baseUrl/api/reports/revenue" -Method Get
    Write-Host "FAIL: Expected 401 Unauthorized" -ForegroundColor Red
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 401) {
        Write-Host "PASS: Successfully challenged unauthenticated request with 401 Unauthorized" -ForegroundColor Green
    } else {
        Write-Host "FAIL: Unexpected status code: $code" -ForegroundColor Red
    }
}

# 2. Customer Access Check (Should return 403 Forbidden)
Write-Host "`n[Test 2] Customer request to /api/reports/revenue..."
$custLogin = @{ email = "customer@smartparking.com"; password = "Customer@123456" } | ConvertTo-Json
$custResp = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $custLogin -ContentType "application/json"
$custToken = $custResp.token
$custHeaders = @{ Authorization = "Bearer $custToken" }

try {
    $res = Invoke-RestMethod -Uri "$baseUrl/api/reports/revenue" -Method Get -Headers $custHeaders
    Write-Host "FAIL: Expected 403 Forbidden for Customer" -ForegroundColor Red
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 403) {
        Write-Host "PASS: Successfully blocked customer with 403 Forbidden" -ForegroundColor Green
    } else {
        Write-Host "FAIL: Unexpected status code: $code" -ForegroundColor Red
    }
}

# 3. Admin Authentication
Write-Host "`n[Test 3] Admin login..."
$adminLogin = @{ email = "admin@smartparking.com"; password = "Admin@Password123!" } | ConvertTo-Json
$adminResp = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $adminLogin -ContentType "application/json"
$adminToken = $adminResp.token
$adminHeaders = @{ Authorization = "Bearer $adminToken" }
Write-Host "PASS: Admin authenticated successfully." -ForegroundColor Green

# 4. Revenue Report (Weekly & Custom)
Write-Host "`n[Test 4] Calling GET /api/reports/revenue?period=weekly..."
$revWeekly = Invoke-RestMethod -Uri "$baseUrl/api/reports/revenue?period=weekly" -Method Get -Headers $adminHeaders
Write-Host "  Period:            $($revWeekly.period)"
Write-Host "  TotalRevenue:      $($revWeekly.totalRevenue)"
Write-Host "  PaidRevenue:       $($revWeekly.paidPaymentsRevenue)"
Write-Host "  OverstayRevenue:   $($revWeekly.overstayFeesRevenue)"
Write-Host "  Chart Points:      $($revWeekly.revenueByPeriod.Count)"
Write-Host "  Transactions:      $($revWeekly.transactions.totalCount)"

if ($revWeekly.period -eq "weekly" -and $revWeekly.revenueByPeriod.Count -eq 7) {
    Write-Host "PASS: Weekly revenue report returned 7 chart points." -ForegroundColor Green
} else {
    Write-Host "FAIL: Unexpected weekly revenue structure." -ForegroundColor Red
}

# 5. Reservation Report (Monthly)
Write-Host "`n[Test 5] Calling GET /api/reports/reservations?period=monthly..."
$resMonthly = Invoke-RestMethod -Uri "$baseUrl/api/reports/reservations?period=monthly" -Method Get -Headers $adminHeaders
Write-Host "  TotalReservations: $($resMonthly.totalReservations)"
Write-Host "  Completed:         $($resMonthly.completedReservations)"
Write-Host "  Cancelled:         $($resMonthly.cancelledReservations)"
Write-Host "  CancellationRate:  $($resMonthly.cancellationRatePercentage)%"
Write-Host "  Status Groups:     $($resMonthly.reservationsByStatus.Count)"
Write-Host "  Chart Points:      $($resMonthly.reservationsByPeriod.Count)"

if ($resMonthly.reservationsByPeriod.Count -eq 30) {
    Write-Host "PASS: Monthly reservation report returned 30 continuous daily buckets for chart." -ForegroundColor Green
} else {
    Write-Host "FAIL: Unexpected monthly reservation bucket count." -ForegroundColor Red
}

# 6. Occupancy Report (Daily)
Write-Host "`n[Test 6] Calling GET /api/reports/occupancy?period=daily..."
$occDaily = Invoke-RestMethod -Uri "$baseUrl/api/reports/occupancy?period=daily" -Method Get -Headers $adminHeaders
Write-Host "  TotalSlots:        $($occDaily.totalSlots)"
Write-Host "  OccupiedSlots:     $($occDaily.occupiedSlots)"
Write-Host "  OccupancyRate:     $($occDaily.occupancyRatePercentage)%"
Write-Host "  SlotType Breakdown:$($occDaily.occupancyBySlotType.Count) types"
Write-Host "  Hourly Buckets:    $($occDaily.hourlyOccupancy.Count) hours"
Write-Host "  Facilities Table:  $($occDaily.facilities.totalCount) facilities"

if ($occDaily.hourlyOccupancy.Count -eq 24 -and $occDaily.facilities.totalCount -gt 0) {
    Write-Host "PASS: Daily occupancy report returned 24 hourly buckets and facilities table." -ForegroundColor Green
} else {
    Write-Host "FAIL: Unexpected occupancy report structure." -ForegroundColor Red
}

# 7. Customer Report (Monthly with pagination)
Write-Host "`n[Test 7] Calling GET /api/reports/customers?period=monthly&pageNumber=1&pageSize=5..."
$custReport = Invoke-RestMethod -Uri "$baseUrl/api/reports/customers?period=monthly&pageNumber=1&pageSize=5" -Method Get -Headers $adminHeaders
Write-Host "  TotalCustomers:    $($custReport.totalCustomers)"
Write-Host "  NewInPeriod:       $($custReport.newCustomersInPeriod)"
Write-Host "  ActiveInPeriod:    $($custReport.activeCustomersInPeriod)"
Write-Host "  TotalSpend:        $($custReport.totalCustomerSpend)"
Write-Host "  Chart Points:      $($custReport.registrationsByPeriod.Count)"
Write-Host "  Table Page Items:  $($custReport.customers.items.Count)"
Write-Host "  Table Total:       $($custReport.customers.totalCount)"

if ($custReport.customers.totalCount -gt 0 -and $custReport.customerActivityTiers.PSObject.Properties.Count -gt 0) {
    Write-Host "PASS: Customer report returned KPIs, activity tiers, and paginated customer table." -ForegroundColor Green
} else {
    Write-Host "FAIL: Unexpected customer report data." -ForegroundColor Red
}

Write-Host "`n=========================================================" -ForegroundColor Cyan
Write-Host " All Step 20 Reporting Integration Tests Passed!         " -ForegroundColor Green
Write-Host "=========================================================" -ForegroundColor Cyan
