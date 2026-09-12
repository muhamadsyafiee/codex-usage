[CmdletBinding()]
param(
    [string]$Repository = 'muhamadsyafiee/codex-usage',
    [string]$Tag
)

$ErrorActionPreference = 'Stop'
$headers = @{ 'User-Agent' = 'CodexUsageWidget-download-report' }
$baseUrl = "https://api.github.com/repos/$Repository/releases"
$releaseUrl = if ($Tag) { "$baseUrl/tags/$Tag" } else { "${baseUrl}?per_page=100" }
$response = Invoke-RestMethod -Uri $releaseUrl -Headers $headers
$releases = @($response)
if ($releases.Count -eq 0) { Write-Output 'No releases found.'; exit 0 }

$rows = foreach ($release in $releases) {
    $assets = @($release.assets)
    $installer = $assets | Where-Object { $_.name -match '^CodexUsageWidget-.*-x64\.msi$' } | Select-Object -First 1
    [pscustomobject]@{
        Version = $release.tag_name
        Published = ([datetime]$release.published_at).ToLocalTime().ToString('yyyy-MM-dd HH:mm')
        InstallerDownloads = if ($installer) { [int64]$installer.download_count } else { 0 }
        AllAssetDownloads = [int64](($assets | Measure-Object -Property download_count -Sum).Sum)
        Installer = if ($installer) { $installer.name } else { 'No MSI asset' }
    }
}

$rows | Format-Table -AutoSize
Write-Output "Total MSI downloads: $([int64](($rows | Measure-Object -Property InstallerDownloads -Sum).Sum))"
Write-Output "Total asset downloads: $([int64](($rows | Measure-Object -Property AllAssetDownloads -Sum).Sum))"
