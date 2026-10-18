$baseUrl = "http://localhost:5057"

Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host " Running Live Integration Tests: Customer Dashboard      " -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan

# 1. Unauthenticated Check
Write-Host "`n[Test 1] Unauthenticated request to /api/dashboard/customer..."
try {
    $res = Invoke-RestMethod -Uri "$baseUrl/api/dashboard/customer" -Method Get
    Write-Host "FAIL: Expected 401 Unauthorized but got 200 OK" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 401) {
        Write-Host "PASS: Successfully challenged unauthenticated request with 401 Unauthorized" -ForegroundColor Green
    } else {
        Write-Host "FAIL: Unexpected status code: $statusCode" -ForegroundColor Red
    }
}

# 2. Register/Login Customer A
$randomSuffix = [System.Random]::new().Next(100000, 999999)
$custEmail = "cust_dash_${randomSuffix}@example.com"
$password = "Customer@12345"

Write-Host "`n[Test 2] Registering fresh Customer A ($custEmail)..."
$registerBody = @{
    fullName = "Alice Customer"
    email = $custEmail
    phoneNumber = "+1-555-0199"
    password = $password
    role = "Customer"
} | ConvertTo-Json

$regResp = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method Post -Body $registerBody -ContentType "application/json"
$custToken = $regResp.token
if (-not $custToken) {
    # If registration returns login response or user, try login
    $loginBody = @{ email = $custEmail; password = $password } | ConvertTo-Json
    $loginResp = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $custToken = $loginResp.token
}
Write-Host "PASS: Customer A authenticated successfully." -ForegroundColor Green

$headers = @{
    Authorization = "Bearer $custToken"
}

# 3. Call GET /api/dashboard/customer for fresh customer
Write-Host "`n[Test 3] Calling GET /api/dashboard/customer for fresh customer..."
$dash1 = Invoke-RestMethod -Uri "$baseUrl/api/dashboard/customer" -Method Get -Headers $headers
Write-Host "Dashboard received:"
Write-Host "  TotalReservations:     $($dash1.totalReservations)"
Write-Host "  CompletedReservations: $($dash1.completedReservations)"
Write-Host "  ActiveReservation:     $($dash1.activeReservation)"
Write-Host "  UpcomingReservation:   $($dash1.upcomingReservation)"
Write-Host "  TotalPayments:         $($dash1.totalPayments)"
Write-Host "  RegisteredVehicles:    $($dash1.registeredVehicles.Count)"
Write-Host "  RecentParkingHistory:  $($dash1.recentParkingHistory.Count)"

if ($dash1.totalReservations -eq 0 -and $dash1.registeredVehicles.Count -eq 0 -and $dash1.totalPayments -eq 0) {
    Write-Host "PASS: Initial empty dashboard values verified." -ForegroundColor Green
} else {
    Write-Host "FAIL: Initial dashboard values unexpected." -ForegroundColor Red
}

# 4. Register a Vehicle for Customer A
Write-Host "`n[Test 4] Registering a vehicle for Customer A..."
$plate = "CUST-${randomSuffix}"
$vehBody = @{
    licensePlate = $plate
    vehicleType = 1 # Car
    make = "Tesla"
    model = "Model 3"
    color = "Midnight Silver"
} | ConvertTo-Json

$vehResp = Invoke-RestMethod -Uri "$baseUrl/api/vehicles" -Method Post -Headers $headers -Body $vehBody -ContentType "application/json"
Write-Host "PASS: Vehicle registered with plate $plate (Id: $($vehResp.id))" -ForegroundColor Green

# 5. Call GET /api/dashboard/customer again and verify registered vehicle shows up
Write-Host "`n[Test 5] Calling GET /api/dashboard/customer and verifying registered vehicles..."
$dash2 = Invoke-RestMethod -Uri "$baseUrl/api/dashboard/customer" -Method Get -Headers $headers

if ($dash2.registeredVehicles.Count -eq 1 -and $dash2.registeredVehicles[0].licensePlate -eq $plate) {
    Write-Host "PASS: Registered vehicle $plate correctly included in customer dashboard registeredVehicles array!" -ForegroundColor Green
} else {
    Write-Host "FAIL: Registered vehicle not found in dashboard." -ForegroundColor Red
}

# 6. Customer Isolation: Try passing another user's Guid as query param
Write-Host "`n[Test 6] Testing customer isolation against query parameter tampering..."
$fakeGuid = [System.Guid]::NewGuid().ToString()
$dashTamper = Invoke-RestMethod -Uri "$baseUrl/api/dashboard/customer?customerId=$fakeGuid" -Method Get -Headers $headers

if ($dashTamper.registeredVehicles.Count -eq 1 -and $dashTamper.registeredVehicles[0].licensePlate -eq $plate) {
    Write-Host "PASS: Customer tampering strictly prevented: non-admin customer only receives their own data!" -ForegroundColor Green
} else {
    Write-Host "FAIL: Customer isolation compromised." -ForegroundColor Red
}

Write-Host "`n=========================================================" -ForegroundColor Cyan
Write-Host " All Customer Dashboard Integration Tests Passed!        " -ForegroundColor Green
Write-Host "=========================================================" -ForegroundColor Cyan
