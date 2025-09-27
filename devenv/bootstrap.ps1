# Check if running as Administrator
if (-not ([Security.Principal.WindowsPrincipal] `
    [Security.Principal.WindowsIdentity]::GetCurrent()
    ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {

    Write-Error "This script must be run as Administrator."
    exit 1
}

# --- Your script logic below ---
Write-Host "Setting up development environment..."

# Install Jekyll
winget install RubyInstallerTeam.RubyWithDevKit.3.2

$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

gem install jekyll bundler

winget install Microsoft.DotNet.SDK.8