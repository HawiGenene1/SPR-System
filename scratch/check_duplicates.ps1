Get-ChildItem -Path backend -Recurse -Filter '*.cs' | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
    $file = $_.FullName
    Get-Content $file | ForEach-Object {
        if ($_ -match '^\s*public\s+(?:abstract\s+|sealed\s+|static\s+)?(class|interface|enum|record)\s+([A-Za-z0-9_]+)') {
            [PSCustomObject]@{
                Type = $matches[1]
                Name = $matches[2]
                File = $file
            }
        }
    }
} | Group-Object Name | Where-Object { $_.Count -gt 1 } | ForEach-Object {
    [PSCustomObject]@{
        Name = $_.Name
        Count = $_.Count
        Files = ($_.Group.File -join '; ')
    }
} | Format-Table -AutoSize
