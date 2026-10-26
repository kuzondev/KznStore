param([string]$SdkBin, [string]$SigningCertificate, [string]$CertificatePassword, [switch]$DevelopmentCertificate)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$manifest = Join-Path $projectRoot 'packaging\AppxManifest.xml'
$source = Join-Path $projectRoot 'artifacts\KznStore-win-x64'
if (-not $SdkBin) {
    $candidate = Get-ChildItem -LiteralPath (Join-Path $projectRoot '.tools\windows-sdk\bin') -Filter makeappx.exe -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.Directory.Name -eq 'x64' } | Select-Object -First 1
    if (-not $candidate) { $candidate = Get-ChildItem -LiteralPath (Join-Path (Join-Path $projectRoot '..\..') '.tools\windows-sdk\bin') -Filter makeappx.exe -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.Directory.Name -eq 'x64' } | Select-Object -First 1 }
    if (-not $candidate) { $candidate = Get-ChildItem -LiteralPath (Join-Path (Join-Path (Split-Path -Parent $projectRoot) 'kzn_notch') '.tools\windows-sdk\bin') -Filter makeappx.exe -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.Directory.Name -eq 'x64' } | Select-Object -First 1 }
    if (-not $candidate) { $candidate = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin\*\x64\makeappx.exe' -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1 }
    if (-not $candidate) { throw 'Windows SDK Build Tools are required; pass -SdkBin with the makeappx.exe folder.' }
    $SdkBin = $candidate.Directory.FullName
}
if (-not (Test-Path (Join-Path $source 'KznStore.exe'))) { throw 'Run scripts/build.ps1 -Publish first.' }
$project = [xml](Get-Content -LiteralPath (Join-Path $projectRoot 'KznStore.csproj'))
$version = $project.Project.PropertyGroup.Version | Select-Object -First 1
$versionParts = $version.Split('.')
if ($versionParts.Count -gt 4) { throw 'Store package version must have no more than four numeric parts.' }
while ($versionParts.Count -lt 4) { $versionParts += '0' }
$packageVersion = $versionParts -join '.'
$manifestText = Get-Content -LiteralPath $manifest -Raw
$manifestText = [regex]::Replace($manifestText, 'Version="[0-9.]+"', "Version=`"$packageVersion`"")
$stage = Join-Path $projectRoot ('artifacts\msix-stage-' + [guid]::NewGuid().ToString('N'))
$output = Join-Path $projectRoot 'artifacts\KznStore-installer'
New-Item -ItemType Directory -Force -Path $stage,$output | Out-Null
Copy-Item -Path (Join-Path $source '*') -Destination $stage -Recurse -Force
Set-Content -LiteralPath (Join-Path $stage 'AppxManifest.xml') -Value $manifestText -NoNewline -Encoding utf8
New-Item -ItemType Directory -Force -Path (Join-Path $stage 'Assets') | Out-Null
Add-Type -AssemblyName System.Drawing
foreach ($asset in @(@('StoreLogo.png',50),@('Square44x44Logo.png',44),@('Square150x150Logo.png',150))) {
    $size = [int]$asset[1]; $bitmap = [Drawing.Bitmap]::new($size,$size); $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = 'AntiAlias'; $graphics.Clear([Drawing.Color]::FromArgb(16,18,22))
    $blue = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(63,142,255)); $white = [Drawing.SolidBrush]::new([Drawing.Color]::White)
    $graphics.FillEllipse($blue,$size*.08,$size*.08,$size*.84,$size*.84)
    $graphics.FillRectangle($white,$size*.27,$size*.27,$size*.16,$size*.16); $graphics.FillRectangle($white,$size*.57,$size*.27,$size*.16,$size*.16)
    $graphics.FillRectangle($white,$size*.27,$size*.57,$size*.16,$size*.16); $graphics.FillRectangle($white,$size*.57,$size*.57,$size*.16,$size*.16)
    $bitmap.Save((Join-Path $stage ('Assets\' + $asset[0])),[Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose(); $bitmap.Dispose(); $blue.Dispose(); $white.Dispose()
}
$package = Join-Path $output "KznStore-$version-x64.msix"
$packOutput = & (Join-Path $SdkBin 'makeappx.exe') pack /d $stage /p $package /o 2>&1
if ($LASTEXITCODE -ne 0) { $packOutput | Select-Object -Last 15 | Write-Output; throw 'Store MSIX validation failed.' }
if ($DevelopmentCertificate) {
    $keyDirectory = Join-Path $projectRoot '.tools\signing'; New-Item -ItemType Directory -Force $keyDirectory | Out-Null
    $SigningCertificate = Join-Path $keyDirectory 'Notchify-Development.pfx'; $passwordPath = Join-Path $keyDirectory 'password.txt'
    if (-not (Test-Path $SigningCertificate)) {
        $randomBytes = New-Object byte[] 32; $random = [Security.Cryptography.RandomNumberGenerator]::Create(); $random.GetBytes($randomBytes)
        $CertificatePassword = [Convert]::ToBase64String($randomBytes); $random.Dispose()
        $rsa = [Security.Cryptography.RSA]::Create(3072)
        $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=Notchify Development',$rsa,[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pkcs1)
        $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new($false,$false,0,$true))
        $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new([Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature,$true))
        $oids = [Security.Cryptography.OidCollection]::new(); $oids.Add([Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.3')) | Out-Null
        $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($oids,$true))
        $certificate = $request.CreateSelfSigned([DateTimeOffset]::Now.AddMinutes(-5),[DateTimeOffset]::Now.AddYears(1))
        [IO.File]::WriteAllBytes($SigningCertificate,$certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Pfx,$CertificatePassword))
        [IO.File]::WriteAllText($passwordPath,$CertificatePassword); $certificate.Dispose(); $rsa.Dispose()
    } else { $CertificatePassword = [IO.File]::ReadAllText($passwordPath) }
    $certificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new($SigningCertificate,$CertificatePassword)
    [IO.File]::WriteAllBytes((Join-Path $output 'KznStore-Development.cer'),$certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Cert)); $certificate.Dispose()
}
if ($SigningCertificate) {
    & (Join-Path $SdkBin 'signtool.exe') sign /fd SHA256 /f $SigningCertificate /p $CertificatePassword $package
    if ($LASTEXITCODE -ne 0) { throw 'Store MSIX signing failed.' }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-KznStore.ps1') -Destination $output -Force
Write-Output "Kzn Store package created: $package"
