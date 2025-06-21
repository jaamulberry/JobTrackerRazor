$hashes = Get-ChildItem -Recurse -Include *.cshtml,*.cs -Exclude *_contenthash.txt |
    Get-FileHash -Algorithm SHA256 |
    ForEach-Object { $_.Hash }

$combined = $hashes -join "`n"
$finalHash = [System.BitConverter]::ToString((New-Object -TypeName System.Security.Cryptography.SHA256Managed).ComputeHash([System.Text.Encoding]::UTF8.GetBytes($combined))) -replace "-", ""

Set-Content -Path "wwwroot/_contenthash.txt" -Value $finalHash -Encoding ASCII