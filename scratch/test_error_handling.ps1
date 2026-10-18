$ErrorActionPreference = "Continue"

$baseUrl = "http://localhost:5057"
Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host " Testing Step 21: Global Error Handling & ExceptionMiddleware" -ForegroundColor Cyan
Write-Host " Base URL: $baseUrl" -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan

$passed = 0
$failed = 0

function Assert-Condition($condition, $testName, $detail = "") {
    if ($condition) {
        $global:passed++
        Write-Host "  [PASS] $testName" -ForegroundColor Green
        if ($detail) {
            Write-Host "         $detail" -ForegroundColor DarkGray
        }
    } else {
        $global:failed++
        Write-Host "  [FAIL] $testName" -ForegroundColor Red
        if ($detail) {
            Write-Host "         $detail" -ForegroundColor Yellow
        }
    }
}

# 1. Test Validation Errors (400 Bad Request)
Write-Host "`n[Test 1] Validation Errors (Invalid Login Request):" -ForegroundColor Yellow
try {
    $invalidBody = @{
        Email = "not-an-email"
    } | ConvertTo-Json

    $resp = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $invalidBody -ContentType "application/json"
    Assert-Condition $false "Expected 400 Bad Request for invalid login body, but succeeded."
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $rawResponse = $reader.ReadToEnd()
    $json = $rawResponse | ConvertFrom-Json

    Assert-Condition ($statusCode -eq 400) "Validation failure returns HTTP 400 Bad Request" "Received: $statusCode"
    Assert-Condition ($json.success -eq $false) "Response has success = false"
    Assert-Condition ($json.message -match "validation" -or $json.message -match "error") "Response contains descriptive validation message" "Message: $($json.message)"
    Assert-Condition ($json.errors.Count -gt 0) "Response errors array contains detailed validation messages" "Count: $($json.errors.Count)"
    Assert-Condition (-not $rawResponse.Contains("stackTrace")) "Response does NOT contain stackTrace"
}

# 2. Test Unauthorized Errors (401 Unauthorized)
Write-Host "`n[Test 2] Unauthorized Error (Accessing Protected Route Without Token):" -ForegroundColor Yellow
try {
    $resp = Invoke-RestMethod -Uri "$baseUrl/api/reservations/my" -Method Get
    Assert-Condition $false "Expected 401 Unauthorized for unauthenticated request, but succeeded."
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $rawResponse = $reader.ReadToEnd()
    $json = $rawResponse | ConvertFrom-Json

    Assert-Condition ($statusCode -eq 401) "Unauthenticated request returns HTTP 401 Unauthorized" "Received: $statusCode"
    Assert-Condition ($json.success -eq $false) "Response has success = false"
    Assert-Condition ($json.message -match "authorized" -or $json.message -match "Authentication") "Response has clear authorization failure message" "Message: $($json.message)"
    Assert-Condition ($json.errors.Count -eq 0) "Errors array is empty for 401"
    Assert-Condition (-not $rawResponse.Contains("stackTrace")) "Response does NOT contain stackTrace"
}

# 3. Test Forbidden Errors (403 Forbidden)
Write-Host "`n[Test 3] Forbidden Error (Customer Accessing Admin-Only Report):" -ForegroundColor Yellow
# First authenticate as Customer to get Customer JWT
$customerLoginBody = @{
    Email = "customer@smartparking.com"
    Password = "Customer@123456"
} | ConvertTo-Json

$custLogin = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $customerLoginBody -ContentType "application/json"
$customerToken = $custLogin.token

try {
    $custHeaders = @{ Authorization = "Bearer $customerToken" }
    $resp = Invoke-RestMethod -Uri "$baseUrl/api/reports/revenue" -Method Get -Headers $custHeaders
    Assert-Condition $false "Expected 403 Forbidden for Customer calling Admin report, but succeeded."
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $rawResponse = $reader.ReadToEnd()
    $json = $rawResponse | ConvertFrom-Json

    Assert-Condition ($statusCode -eq 403) "Customer calling Admin report returns HTTP 403 Forbidden" "Received: $statusCode"
    Assert-Condition ($json.success -eq $false) "Response has success = false"
    Assert-Condition ($json.message -match "permission" -or $json.message -match "access") "Response has clear forbidden message" "Message: $($json.message)"
    Assert-Condition ($json.errors.Count -eq 0) "Errors array is empty for 403"
    Assert-Condition (-not $rawResponse.Contains("stackTrace")) "Response does NOT contain stackTrace"
}

# 4. Test Not Found Errors (404 Not Found)
Write-Host "`n[Test 4] Not Found Error (Querying Non-Existent Vehicle):" -ForegroundColor Yellow
try {
    $nonExistentId = [System.Guid]::NewGuid().ToString()
    $custHeaders = @{ Authorization = "Bearer $customerToken" }
    $resp = Invoke-RestMethod -Uri "$baseUrl/api/vehicles/$nonExistentId" -Method Get -Headers $custHeaders
    Assert-Condition $false "Expected 404 Not Found for non-existent vehicle, but succeeded."
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $rawResponse = $reader.ReadToEnd()
    $json = $rawResponse | ConvertFrom-Json

    Assert-Condition ($statusCode -eq 404) "Non-existent vehicle returns HTTP 404 Not Found" "Received: $statusCode"
    Assert-Condition ($json.success -eq $false) "Response has success = false"
    Assert-Condition ($json.message -match "not found" -or $json.message -match "Vehicle") "Response has not found message" "Message: $($json.message)"
    Assert-Condition ($json.errors.Count -eq 0) "Errors array is empty for 404"
    Assert-Condition (-not $rawResponse.Contains("stackTrace")) "Response does NOT contain stackTrace"
}

# 5. Test Consistent JSON Contract
Write-Host "`n[Test 5] Response Envelope Contract Verification:" -ForegroundColor Yellow
Assert-Condition ($json.PSObject.Properties["success"] -ne $null) "JSON envelope strictly contains 'success'"
Assert-Condition ($json.PSObject.Properties["message"] -ne $null) "JSON envelope strictly contains 'message'"
Assert-Condition ($json.PSObject.Properties["errors"] -ne $null) "JSON envelope strictly contains 'errors'"

Write-Host "`n=========================================================" -ForegroundColor Cyan
Write-Host " Live Error Handling Tests Summary: $passed Passed, $failed Failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "=========================================================" -ForegroundColor Cyan

if ($failed -gt 0) {
    exit 1
}
exit 0
