#Requires -Version 5.1
param([switch]$TrustDevelopmentCertificate)
$ErrorActionPreference = 'Stop'
$package = Get-ChildItem -LiteralPath $PSScriptRoot -Filter 'KznStore-*.msix' | Select-Object -First 1
if (-not $package) { throw 'Place the Kzn Store MSIX next to this script.' }
if ($TrustDevelopmentCertificate) {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run as administrator to trust the local development certificate.' }
    $certificate = Join-Path $PSScriptRoot 'KznStore-Development.cer'
    Import-Certificate -FilePath $certificate -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null
}
Add-AppxPackage -Path $package.FullName
Write-Output 'Kzn Store installed. Open it from Start.'
