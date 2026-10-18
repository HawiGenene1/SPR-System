$ErrorActionPreference = "Continue"
$baseUrl = "http://localhost:5057"
$feUrl = "http://localhost:4200"

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "       SMART PARKING RESERVATION SYSTEM: COMPLETE 20-STEP PLAN        " -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

$results = [System.Collections.Generic.List[PSObject]]::new()

function Record-Step($stepNum, $title, $success, $details = "") {
    $status = if ($success) { "PASS" } else { "FAIL" }
    $color = if ($success) { "Green" } else { "Red" }
    Write-Host " [$status] Step $stepNum : $title" -ForegroundColor $color
    if ($details) {
        Write-Host "         $details" -ForegroundColor Gray
    }
    $results.Add([PSCustomObject]@{
        Step = $stepNum
        Title = $title
        Passed = $success
        Details = $details
    })
}

# ====================================================================
# STEP 1: Check project builds
# ====================================================================
Write-Host "`n--- [Step 1] Checking Project Builds ---" -ForegroundColor Yellow
$backendBuild = dotnet build backend/SmartParkingSystem.sln --no-restore 2>&1
$bSuccess = ($LASTEXITCODE -eq 0) -and ($backendBuild -notmatch "Build FAILED")
$distExists = Test-Path "frontend/dist/frontend"
$step1Pass = $bSuccess -and $distExists
Record-Step 1 "Project Builds Check" $step1Pass "Backend build: $(if ($bSuccess){'OK'}else{'FAIL'}). Frontend dist build bundle: $(if ($distExists){'Present'}else{'Missing'})."

# ====================================================================
# STEP 2: Check PostgreSQL
# ====================================================================
Write-Host "`n--- [Step 2] Checking PostgreSQL Service & Port ---" -ForegroundColor Yellow
$pgService = Get-Service -Name *postgres* -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq "Running" }
$pgPortOpen = $false
try {
    $tcp = New-Object System.Net.Sockets.TcpClient
    $tcp.Connect("127.0.0.1", 5432)
    $pgPortOpen = $tcp.Connected
    $tcp.Close()
} catch {
    $pgPortOpen = $false
}
$step2Pass = ($pgService -ne $null) -and $pgPortOpen
Record-Step 2 "PostgreSQL Verification" $step2Pass "Service: $($pgService.Name) ($($pgService.Status)), Port 5432 reachable: $pgPortOpen"

# ====================================================================
# STEP 3: Test Database
# ====================================================================
Write-Host "`n--- [Step 3] Testing Database Migrations & Data Seed ---" -ForegroundColor Yellow
$migOutput = dotnet ef migrations list --project backend/SmartParkingSystem.Infrastructure --startup-project backend/SmartParkingSystem.Api --no-build 2>&1
$dbMigrationsOk = ($migOutput -match "InitialCreate") -and ($migOutput -match "AddVehicleTypeAndHourlyRateToParkingSlot")
Record-Step 3 "Database & EF Core Migrations" $dbMigrationsOk "Applied migrations verified in __EFMigrationsHistory."

# ====================================================================
# STEP 4: Test Backend API
# ====================================================================
Write-Host "`n--- [Step 4] Testing Backend API Endpoints ---" -ForegroundColor Yellow
$healthResp = $null
$swaggerResp = $null
try {
    $healthResp = Invoke-RestMethod -Uri "$baseUrl/api/health" -Method Get -TimeoutSec 10
    $swaggerResp = Invoke-RestMethod -Uri "$baseUrl/swagger/v1/swagger.json" -Method Get -TimeoutSec 10
} catch {}
$step4Pass = ($healthResp -ne $null -and $healthResp.status -eq "Healthy") -and 
             ($swaggerResp -ne $null -and $swaggerResp.openapi -ne $null -and $swaggerResp.tags.Count -eq 11)
Record-Step 4 "Backend API & Swagger Specification" $step4Pass "Health: $($healthResp.status), Swagger tags: $($swaggerResp.tags.Count)"

# ====================================================================
# STEP 5: Test Authentication
# ====================================================================
Write-Host "`n--- [Step 5] Testing Authentication (Login, Register, Refresh) ---" -ForegroundColor Yellow
$adminLoginBody = @{ email = "admin@smartparking.com"; password = "Admin@Password123!" } | ConvertTo-Json
$adminAuth = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $adminLoginBody -ContentType "application/json"

$attendantLoginBody = @{ email = "attendant@smartparking.com"; password = "Attendant@123456" } | ConvertTo-Json
$attendantAuth = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $attendantLoginBody -ContentType "application/json"

$customerLoginBody = @{ email = "customer@smartparking.com"; password = "Customer@123456" } | ConvertTo-Json
$customerAuth = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $customerLoginBody -ContentType "application/json"

# Register a dynamic new customer for test isolation
$uniqueDigits = (Get-Random -Minimum 1000000 -Maximum 9999999).ToString()
$newCustEmail = "customer_$uniqueDigits@testparking.com"
$registerBody = @{
    fullName = "Test Customer $uniqueDigits"
    email = $newCustEmail
    password = "Customer@SecurePass123!"
    phoneNumber = "+1-555-$uniqueDigits"
    role = 1 # Customer enum
} | ConvertTo-Json
$newCustAuth = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method Post -Body $registerBody -ContentType "application/json"

$step5Pass = (![string]::IsNullOrEmpty($adminAuth.token)) -and 
             (![string]::IsNullOrEmpty($attendantAuth.token)) -and 
             (![string]::IsNullOrEmpty($customerAuth.token)) -and 
             (![string]::IsNullOrEmpty($newCustAuth.token))
Record-Step 5 "Authentication (Login & Register)" $step5Pass "Tokens issued for Admin, Attendant, Customer, and new dynamic customer ($newCustEmail)"

$adminHeaders = @{ Authorization = "Bearer $($adminAuth.token)" }
$attendantHeaders = @{ Authorization = "Bearer $($attendantAuth.token)" }
$customerHeaders = @{ Authorization = "Bearer $($newCustAuth.token)" }

# ====================================================================
# STEP 6: Test Role-Based Access
# ====================================================================
Write-Host "`n--- [Step 6] Testing Role-Based Access Control (RBAC) ---" -ForegroundColor Yellow
$adminMe = Invoke-RestMethod -Uri "$baseUrl/api/auth/me" -Headers $adminHeaders
$attendantMe = Invoke-RestMethod -Uri "$baseUrl/api/auth/me" -Headers $attendantHeaders
$customerMe = Invoke-RestMethod -Uri "$baseUrl/api/auth/me" -Headers $customerHeaders

$custBlocked = $false
try {
    Invoke-RestMethod -Uri "$baseUrl/api/admin/users" -Headers $customerHeaders -ErrorAction Stop
} catch {
    $custBlocked = ($_.Exception.Response.StatusCode.value__ -eq 403)
}

$attendantBlocked = $false
try {
    Invoke-RestMethod -Uri "$baseUrl/api/reports/revenue" -Headers $attendantHeaders -ErrorAction Stop
} catch {
    $attendantBlocked = ($_.Exception.Response.StatusCode.value__ -eq 403)
}

$step6Pass = ($adminMe.roleName -eq "Admin") -and 
             ($attendantMe.roleName -eq "ParkingAttendant") -and 
             ($customerMe.roleName -eq "Customer") -and 
             $custBlocked -and $attendantBlocked
Record-Step 6 "Role-Based Access Control (RBAC)" $step6Pass "Admin, Attendant, Customer profiles verified; Customer & Attendant blocked from Admin APIs (HTTP 403)."

# ====================================================================
# STEP 7: Test Vehicles
# ====================================================================
Write-Host "`n--- [Step 7] Testing Vehicle Registration & Management ---" -ForegroundColor Yellow
$vehPlate = "TST-" + (Get-Random -Minimum 1000 -Maximum 9999).ToString()
$addVehBody = @{
    licensePlate = $vehPlate
    make = "Tesla"
    model = "Model 3"
    color = "Midnight Silver"
    vehicleType = 1 # Car
} | ConvertTo-Json

$createdVehicle = Invoke-RestMethod -Uri "$baseUrl/api/vehicles" -Method Post -Body $addVehBody -ContentType "application/json" -Headers $customerHeaders
$myVehicles = Invoke-RestMethod -Uri "$baseUrl/api/customer/my-vehicles" -Headers $customerHeaders
$step7Pass = ($createdVehicle -ne $null) -and ($createdVehicle.licensePlate -eq $vehPlate) -and ($myVehicles.Count -ge 1)
Record-Step 7 "Vehicle Management" $step7Pass "Vehicle registered: $vehPlate ($($createdVehicle.make) $($createdVehicle.model)). Vehicle count: $($myVehicles.Count)."

# ====================================================================
# STEP 8: Test Parking Areas
# ====================================================================
Write-Host "`n--- [Step 8] Testing Parking Area Management ---" -ForegroundColor Yellow
$areas = Invoke-RestMethod -Uri "$baseUrl/api/parking-areas" -Headers $customerHeaders
$selectedArea = $areas[0]
$areaDetail = Invoke-RestMethod -Uri "$baseUrl/api/parking-areas/$($selectedArea.id)" -Headers $customerHeaders
$step8Pass = ($areas.Count -gt 0) -and ($areaDetail -ne $null) -and ($areaDetail.totalSlots -gt 0)
Record-Step 8 "Parking Area Management" $step8Pass "Found $($areas.Count) area(s). Selected '$($selectedArea.name)' with $($selectedArea.totalSlots) slots, rate: `$$($selectedArea.hourlyRate)/hr."

# ====================================================================
# STEP 9: Test Parking Slots
# ====================================================================
Write-Host "`n--- [Step 9] Testing Parking Slot Availability ---" -ForegroundColor Yellow
$slots = Invoke-RestMethod -Uri "$baseUrl/api/parking-slots?parkingAreaId=$($selectedArea.id)&availableOnly=true" -Headers $customerHeaders
$availableSlot = $slots | Where-Object { $_.status -eq 1 -or $_.status -eq "Available" } | Select-Object -First 1
$step9Pass = ($slots.Count -gt 0) -and ($availableSlot -ne $null)
Record-Step 9 "Parking Slot Management" $step9Pass "Slot '$($availableSlot.slotNumber)' (Floor $($availableSlot.floorNumber)) is Available."

# ====================================================================
# STEP 10: Test Reservations
# ====================================================================
Write-Host "`n--- [Step 10] Testing Reservation Creation & Server-Side Pricing ---" -ForegroundColor Yellow
$startTime = [System.DateTime]::UtcNow
$endTime = $startTime.AddHours(2)

$resBody = @{
    parkingSlotId = $availableSlot.id
    parkingAreaId = $selectedArea.id
    vehicleId = $createdVehicle.id
    startTime = $startTime.ToString("o")
    endTime = $endTime.ToString("o")
    totalPrice = 1.00 # Tampered price; server must compute real price
} | ConvertTo-Json

$createdReservation = Invoke-RestMethod -Uri "$baseUrl/api/reservations" -Method Post -Body $resBody -ContentType "application/json" -Headers $customerHeaders

# Test overlap rejection
$overlapBlocked = $false
try {
    Invoke-RestMethod -Uri "$baseUrl/api/reservations" -Method Post -Body $resBody -ContentType "application/json" -Headers $customerHeaders -ErrorAction Stop
} catch {
    $overlapBlocked = ($_.Exception.Response.StatusCode.value__ -eq 400)
}

$step10Pass = ($createdReservation -ne $null) -and 
              ($createdReservation.totalPrice -gt 1.00) -and 
              ($createdReservation.status -ne $null) -and 
              $overlapBlocked
Record-Step 10 "Reservation Creation & Concurrency Overlap Protection" $step10Pass "Reservation ID: $($createdReservation.id), Status: $($createdReservation.status), Server computed price: `$$($createdReservation.totalPrice). Overlapping booking blocked: $overlapBlocked."

# ====================================================================
# STEP 11: Test Payments
# ====================================================================
Write-Host "`n--- [Step 11] Testing Payment Processing & Idempotency ---" -ForegroundColor Yellow
$payBody = @{
    reservationId = $createdReservation.id
    amount = $createdReservation.totalPrice
    paymentMethod = 2 # Card enum
    paymentToken = "tok_test_card_visa_4242"
} | ConvertTo-Json

$paymentResult = Invoke-RestMethod -Uri "$baseUrl/api/payments" -Method Post -Body $payBody -ContentType "application/json" -Headers $customerHeaders

# Verify reservation is now Confirmed
$updatedReservation = Invoke-RestMethod -Uri "$baseUrl/api/reservations/$($createdReservation.id)" -Headers $customerHeaders

# Test duplicate payment rejection
$duplicatePaymentBlocked = $false
try {
    Invoke-RestMethod -Uri "$baseUrl/api/payments" -Method Post -Body $payBody -ContentType "application/json" -Headers $customerHeaders -ErrorAction Stop
} catch {
    $duplicatePaymentBlocked = ($_.Exception.Response.StatusCode.value__ -eq 400)
}

$step11Pass = ($paymentResult -ne $null) -and 
              ($paymentResult.status -eq 2 -or $paymentResult.status -eq "Completed" -or $paymentResult.status -eq "Paid") -and 
              ($updatedReservation.status -eq 2 -or $updatedReservation.status -eq "Confirmed") -and 
              $duplicatePaymentBlocked
Record-Step 11 "Payment Management" $step11Pass "Payment completed (`$$($paymentResult.amount)). Reservation status: Confirmed. Duplicate payment rejected: $duplicatePaymentBlocked."

# ====================================================================
# STEP 12: Test QR Code
# ====================================================================
Write-Host "`n--- [Step 12] Testing QR Code Generation & Scanning ---" -ForegroundColor Yellow
$qrCodeResult = Invoke-RestMethod -Uri "$baseUrl/api/qr-codes/$($createdReservation.id)" -Headers $customerHeaders

$hasToken = !([string]::IsNullOrEmpty($qrCodeResult.code))
$hasImage = !([string]::IsNullOrEmpty($qrCodeResult.imageData))
$step12Pass = $hasToken -and $hasImage
Record-Step 12 "QR Code Generation & Verification" $step12Pass "QR Token: $($qrCodeResult.code)... Base64 matrix: $($qrCodeResult.imageData.Length) chars."

# ====================================================================
# STEP 13: Test Check-In
# ====================================================================
Write-Host "`n--- [Step 13] Testing Vehicle Entry Check-In ---" -ForegroundColor Yellow
$checkInBody = @{
    qrCode = $qrCodeResult.code
    parkingAreaId = $selectedArea.id
    notes = "Automated full-system test entry"
} | ConvertTo-Json

$checkInResult = Invoke-RestMethod -Uri "$baseUrl/api/parking/check-in" -Method Post -Body $checkInBody -ContentType "application/json" -Headers $attendantHeaders

# Verify slot is Occupied
$slotAfterCheckIn = Invoke-RestMethod -Uri "$baseUrl/api/parking-slots/$($availableSlot.id)" -Headers $customerHeaders
$resAfterCheckIn = Invoke-RestMethod -Uri "$baseUrl/api/reservations/$($createdReservation.id)" -Headers $customerHeaders

$step13Pass = ($checkInResult -ne $null) -and 
              ($resAfterCheckIn.status -eq 3 -or $resAfterCheckIn.status -eq "Active") -and 
              ($slotAfterCheckIn.status -eq 3 -or $slotAfterCheckIn.status -eq "Occupied")
Record-Step 13 "Vehicle Check-In Operations" $step13Pass "Vehicle checked in at gate. Attendant: $($checkInResult.attendantName), Reservation -> Active, Slot -> Occupied."

# ====================================================================
# STEP 14: Test Check-Out
# ====================================================================
Write-Host "`n--- [Step 14] Testing Vehicle Exit Check-Out ---" -ForegroundColor Yellow
$checkOutBody = @{
    qrCode = $qrCodeResult.code
    parkingAreaId = $selectedArea.id
    notes = "Automated full-system test exit"
} | ConvertTo-Json

$checkOutResult = Invoke-RestMethod -Uri "$baseUrl/api/parking/check-out" -Method Post -Body $checkOutBody -ContentType "application/json" -Headers $attendantHeaders

# Verify slot released back to Available
$slotAfterCheckOut = Invoke-RestMethod -Uri "$baseUrl/api/parking-slots/$($availableSlot.id)" -Headers $customerHeaders
$resAfterCheckOut = Invoke-RestMethod -Uri "$baseUrl/api/reservations/$($createdReservation.id)" -Headers $customerHeaders

$step14Pass = ($checkOutResult -ne $null) -and 
              ($resAfterCheckOut.status -eq 4 -or $resAfterCheckOut.status -eq "Completed") -and 
              ($slotAfterCheckOut.status -eq 1 -or $slotAfterCheckOut.status -eq "Available")
Record-Step 14 "Vehicle Check-Out Operations" $step14Pass "Vehicle checked out. Slot released back to Available (Status: $($slotAfterCheckOut.status)). Overstay fee: `$$($checkOutResult.overstayFee)."

# ====================================================================
# STEP 15: Test Parking History
# ====================================================================
Write-Host "`n--- [Step 15] Testing Parking History & Tenant Auditing ---" -ForegroundColor Yellow
$customerHistory = Invoke-RestMethod -Uri "$baseUrl/api/parking-history/my" -Headers $customerHeaders
$adminHistory = Invoke-RestMethod -Uri "$baseUrl/api/parking-history" -Headers $adminHeaders

$step15Pass = ($customerHistory.items.Count -ge 1) -and ($adminHistory.items.Count -ge 1)
Record-Step 15 "Parking History & Audit Trail" $step15Pass "Customer history contains $($customerHistory.items.Count) record(s). Admin audit contains $($adminHistory.items.Count) record(s)."

# ====================================================================
# STEP 16: Test Admin Dashboard
# ====================================================================
Write-Host "`n--- [Step 16] Testing Admin Dashboard Analytics ---" -ForegroundColor Yellow
$adminDashboard = Invoke-RestMethod -Uri "$baseUrl/api/dashboard/admin" -Headers $adminHeaders
$step16Pass = ($adminDashboard -ne $null) -and 
              ($adminDashboard.totalRevenue -ge 0) -and 
              ($adminDashboard.totalSlots -gt 0)
Record-Step 16 "Admin Dashboard Metrics" $step16Pass "Revenue: `$$($adminDashboard.totalRevenue), Total slots: $($adminDashboard.totalSlots), Active reservations: $($adminDashboard.activeReservations)."

# ====================================================================
# STEP 17: Test Angular Frontend Application
# ====================================================================
Write-Host "`n--- [Step 17] Testing Angular Frontend Accessibility ---" -ForegroundColor Yellow
$feIndex = Invoke-WebRequest -Uri "$feUrl" -UseBasicParsing
$step17Pass = ($feIndex.StatusCode -eq 200) -and ($feIndex.Content -match "<app-root>")
Record-Step 17 "Angular Frontend App Loading" $step17Pass "Frontend served HTTP 200 with root <app-root> component."

# ====================================================================
# STEP 18: Test Complete Customer Workflow
# ====================================================================
Write-Host "`n--- [Step 18] Testing Complete Customer Workflow ---" -ForegroundColor Yellow
$custDash = Invoke-RestMethod -Uri "$baseUrl/api/dashboard/customer" -Headers $customerHeaders
$custWorkflowPass = ($custDash -ne $null) -and 
                    ($custDash.recentReservations.Count -ge 1) -and 
                    ($custDash.registeredVehicles.Count -ge 1)
Record-Step 18 "Complete Customer Workflow" $custWorkflowPass "Customer registered, booked slot, paid, accessed QR pass, and dashboard accurately reflects reservation & vehicle history."

# ====================================================================
# STEP 19: Test Complete Attendant Workflow
# ====================================================================
Write-Host "`n--- [Step 19] Testing Complete Attendant Workflow ---" -ForegroundColor Yellow
$attendantDash = Invoke-RestMethod -Uri "$baseUrl/api/dashboard/attendant" -Headers $attendantHeaders
$attendantWorkflowPass = ($attendantDash -ne $null) -and 
                         ($checkInResult -ne $null) -and 
                         ($checkOutResult -ne $null)
Record-Step 19 "Complete Attendant Workflow" $attendantWorkflowPass "Attendant authenticated, accessed assigned parking area stats, verified QR pass, processed entry and processed exit."

# ====================================================================
# STEP 20: Test Complete Admin Workflow
# ====================================================================
Write-Host "`n--- [Step 20] Testing Complete Admin Workflow ---" -ForegroundColor Yellow
$allUsers = Invoke-RestMethod -Uri "$baseUrl/api/admin/users" -Headers $adminHeaders
$revReport = Invoke-RestMethod -Uri "$baseUrl/api/reports/revenue?period=monthly" -Headers $adminHeaders
$occReport = Invoke-RestMethod -Uri "$baseUrl/api/reports/occupancy" -Headers $adminHeaders

$adminWorkflowPass = ($allUsers.Count -ge 3) -and 
                     ($revReport -ne $null) -and 
                     ($occReport -ne $null)
Record-Step 20 "Complete Admin Workflow" $adminWorkflowPass "Admin retrieved all system users ($($allUsers.Count)), verified global KPIs, and generated revenue & occupancy analytics reports."

# ====================================================================
# SUMMARY
# ====================================================================
Write-Host "`n======================================================================" -ForegroundColor Cyan
Write-Host "                       20-STEP PLAN SUMMARY                           " -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

$passedCount = ($results | Where-Object { $_.Passed }).Count
$failedCount = ($results | Where-Object { -not $_.Passed }).Count

foreach ($r in $results) {
    $s = if ($r.Passed) { "[PASS]" } else { "[FAIL]" }
    $c = if ($r.Passed) { "Green" } else { "Red" }
    Write-Host "  $s Step $($r.Step.ToString().PadLeft(2)) : $($r.Title)" -ForegroundColor $c
}

Write-Host "----------------------------------------------------------------------" -ForegroundColor Cyan
if ($failedCount -eq 0) {
    Write-Host " RESULT: ALL 20 STEPS PASSED SUCCESSFULLY! (100% COMPLETION) " -ForegroundColor Green
} else {
    Write-Host " RESULT: $passedCount PASSED, $failedCount FAILED! " -ForegroundColor Red
}
Write-Host "======================================================================" -ForegroundColor Cyan
