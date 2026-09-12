$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
dotnet publish src/CodexUsage.csproj -c Release -r win-x64 --self-contained true -o dist/app
if ($LASTEXITCODE) { throw 'Publish failed' }
npm.cmd install --prefix .vendor @openai/codex@0.154.0
if ($LASTEXITCODE) { throw 'Codex download failed' }
Copy-Item '.vendor/node_modules/@openai/codex-win32-x64/vendor/x86_64-pc-windows-msvc/bin/codex.exe' 'dist/app/codex.exe'
Invoke-WebRequest 'https://raw.githubusercontent.com/openai/codex/rust-v0.154.0/LICENSE' -OutFile dist/app/CODEX-LICENSE.txt
Invoke-WebRequest 'https://raw.githubusercontent.com/openai/codex/rust-v0.154.0/NOTICE' -OutFile dist/app/CODEX-NOTICE.txt
if (!(Test-Path '.tools/wix.exe')) { dotnet tool install wix --tool-path .tools --version 6.0.2 }
& .tools/wix.exe build installer/Package.wxs -arch x64 -d "PublishDir=$PSScriptRoot/dist/app" -o dist/CodexUsageWidget-1.2.0-x64.msi
if ($LASTEXITCODE) { throw 'MSI build failed' }
