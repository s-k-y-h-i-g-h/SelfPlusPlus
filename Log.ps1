param(
    [Parameter(Mandatory = $false)]
    [string]$Action,

    [string]$Type,
    [string]$Category,
    [string]$Name,
    [string]$Amount
)

function Get-LogFilePath {
    param(
        [switch]$Ensure
    )

    if ($IsWindows) {
        $base = $env:LOCALAPPDATA
        if (-not $base) { $base = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData) }
        $dir = Join-Path -Path $base -ChildPath 'SelfPlusPlus'
    } elseif ($IsMacOS) {
        $userHomePath = $HOME
        if (-not $userHomePath) { $userHomePath = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile) }
        $dir = Join-Path -Path $userHomePath -ChildPath 'Library'
        $dir = Join-Path -Path $dir -ChildPath 'Application Support'
        $dir = Join-Path -Path $dir -ChildPath 'SelfPlusPlus'
    } else {
        $base = $env:XDG_DATA_HOME
        if (-not $base) {
            $userHomePath = $HOME
            if (-not $userHomePath) { $userHomePath = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile) }
            $base = Join-Path -Path $userHomePath -ChildPath '.local'
            $base = Join-Path -Path $base -ChildPath 'share'
        }
        $dir = Join-Path -Path $base -ChildPath 'SelfPlusPlus'
    }

    if ($Ensure) {
        if (-not (Test-Path -LiteralPath $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
    }

    return (Join-Path -Path $dir -ChildPath 'Log.json')
}

function Show-Usage {
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\Log.ps1 -Action Add -Type <Type> -Category <Category> -Name <Name> -Amount <Amount>" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Parameters:" -ForegroundColor Yellow
    Write-Host "  -Action    The action to perform. Supported: Add" -ForegroundColor Yellow
    Write-Host "  -Type      The type of log entry (required for Add)" -ForegroundColor Yellow
    Write-Host "  -Category  The category of the log entry (required for Add)" -ForegroundColor Yellow
    Write-Host "  -Name      The item name (required for Add)" -ForegroundColor Yellow
    Write-Host "  -Amount    The item amount, e.g. 8g (required for Add)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host ("Default log file: {0}" -f (Get-LogFilePath)) -ForegroundColor Yellow
}

function Add-LogEntry {
    param(
        [string]$FilePath = $(Get-LogFilePath -Ensure),
        [string]$Type,
        [string]$Category,
        [string]$Name,
        [string]$Amount
    )

    $newEntry = [PSCustomObject]@{
        Timestamp = (Get-Date).ToUniversalTime().ToString("o")
        Type      = $Type
        Category  = $Category
        Name      = $Name
        Amount    = $Amount
    }

    try {
        if (Test-Path -LiteralPath $FilePath) {
            $raw = Get-Content -LiteralPath $FilePath -Raw -ErrorAction Stop
            if ($null -eq $raw -or [string]::IsNullOrWhiteSpace([string]$raw)) {
                $data = @()
            } else {
                $parsed = $raw | ConvertFrom-Json -ErrorAction Stop
                if ($parsed -is [System.Array]) {
                    $data = $parsed
                } else {
                    $data = ,$parsed
                }
            }
        } else {
            $data = @()
        }

        $data += $newEntry

        $jsonOut = ConvertTo-Json -InputObject $data -Depth 10
        Set-Content -LiteralPath $FilePath -Value $jsonOut -Encoding UTF8 -Force

        return $null
    }
    catch {
        throw "Failed to add log entry: $($_.Exception.Message)"
    }
}

if (-not $PSBoundParameters.ContainsKey('Action')) {
    Show-Usage
    exit 1
}

if ($Action -eq 'Add') {
    if (-not $Type -or -not $Category -or -not $Name -or -not $Amount) {
        Write-Host "Error: -Type, -Category, -Name, and -Amount are required for Action 'Add'." -ForegroundColor Red
        Show-Usage
        exit 1
    }
    Add-LogEntry -Type $Type -Category $Category -Name $Name -Amount $Amount
} else {
    Write-Host "Error: Unsupported Action '$Action'." -ForegroundColor Red
    Show-Usage
    exit 1
}