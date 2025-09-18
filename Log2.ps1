param(
    [Parameter(Mandatory = $false)]
    [string]$Action,

    [string]$Type,
    [string]$Category,
    [string]$Name,
    [string]$Amount,
    [double]$Value,
    [string]$Unit,
    [string]$Timestamp,
    [hashtable]$Details
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
    Write-Host "  .\Log2.ps1 -Action Add -Type <Type> -Category <Category> -Name <Name> [Type/Category-specific params]" -ForegroundColor Yellow
    Write-Host "" 
    Write-Host "Parameters:" -ForegroundColor Yellow
    Write-Host "  -Action     Supported: Add (Update/Remove not implemented in this script)" -ForegroundColor Yellow
    Write-Host "  -Type       For Add: Consumption | Measurement" -ForegroundColor Yellow
    Write-Host "  -Category   Consumption: Substance | Stack; Measurement: Vitals" -ForegroundColor Yellow
    Write-Host "  -Name       Required for Add (all types)" -ForegroundColor Yellow
    Write-Host "  -Timestamp  Optional ISO 8601 string; if set, used as-is (converted to UTC)" -ForegroundColor Yellow
    Write-Host "" 
    Write-Host "Type/Category specific:" -ForegroundColor Yellow
    Write-Host "  Consumption/Substance: -Amount <string> (required)" -ForegroundColor Yellow
    Write-Host "  Consumption/Stack:     (no additional required fields)" -ForegroundColor Yellow
    Write-Host "  Measurement/Vitals:    -Value <float> -Unit <string> (required)" -ForegroundColor Yellow
    Write-Host "  Custom details:        -Details @{ Key='Value'; ... }" -ForegroundColor Yellow
    Write-Host "" 
    Write-Host ("Default log file: {0}" -f (Get-LogFilePath)) -ForegroundColor Yellow
}

function Resolve-TimestampUtcString {
    param(
        [string]$Input
    )

    if ([string]::IsNullOrWhiteSpace($Input)) {
        return (Get-Date).ToUniversalTime().ToString('o')
    }

    try {
        $dt = [datetime]::Parse($Input, [Globalization.CultureInfo]::InvariantCulture)
        return $dt.ToUniversalTime().ToString('o')
    } catch {
        throw "Invalid Timestamp: '$Input'. Provide ISO 8601 or omit it."
    }
}

function Build-DetailsObject {
    param(
        [string]$Type,
        [string]$Category,
        [string]$Name,
        [string]$Amount,
        [double]$Value,
        [string]$Unit,
        [hashtable]$Details
    )

    $result = @{}
    if ($Details) {
        foreach ($k in $Details.Keys) { $result[$k] = $Details[$k] }
    }

    if ($Type -eq 'Consumption') {
        if ($Category -eq 'Substance') {
            if (-not $Amount) { throw "Consumption/Substance requires -Amount" }
            $result['Amount'] = $Amount
        } elseif ($Category -eq 'Stack') {
            # No additional required fields for Stack in spec
        }
    } elseif ($Type -eq 'Measurement') {
        if ($Category -eq 'Vitals') {
            if ($PSBoundParameters.ContainsKey('Value') -eq $false -or -not $Unit) { throw "Measurement/Vitals requires -Value and -Unit" }
            $result['Value'] = $Value
            $result['Unit'] = $Unit
        }
    }

    return [PSCustomObject]$result
}

function Add-LogEntry {
    param(
        [string]$FilePath = $(Get-LogFilePath -Ensure),
        [string]$Type,
        [string]$Category,
        [string]$Name,
        [string]$Amount,
        [double]$Value,
        [string]$Unit,
        [string]$Timestamp,
        [hashtable]$Details
    )

    $timestampUtc = Resolve-TimestampUtcString -Input $Timestamp
    $detailsObj = Build-DetailsObject -Type $Type -Category $Category -Name $Name -Amount $Amount -Value $Value -Unit $Unit -Details $Details

    $newEntry = [PSCustomObject]@{
        Timestamp = $timestampUtc
        Type      = $Type
        Category  = $Category
        Name      = $Name
        Details   = $detailsObj
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
    if (-not $Type -or -not $Category -or -not $Name) {
        Write-Host "Error: -Type, -Category, and -Name are required for Action 'Add'." -ForegroundColor Red
        Show-Usage
        exit 1
    }

    # Validate allowed values per spec
    switch ($Type) {
        'Consumption' {
            if ($Category -notin @('Substance','Stack')) {
                Write-Host "Error: For Type=Consumption, Category must be Substance or Stack." -ForegroundColor Red
                exit 1
            }
        }
        'Measurement' {
            if ($Category -notin @('Vitals')) {
                Write-Host "Error: For Type=Measurement, Category must be Vitals." -ForegroundColor Red
                exit 1
            }
        }
        default {
            Write-Host "Error: Unsupported Type '$Type'. Supported: Consumption, Measurement." -ForegroundColor Red
            exit 1
        }
    }

    Add-LogEntry -Type $Type -Category $Category -Name $Name -Amount $Amount -Value $Value -Unit $Unit -Timestamp $Timestamp -Details $Details
}
elseif ($Action -in @('Update','Remove')) {
    Write-Host "Error: Action '$Action' is noted in the spec but not implemented in this script yet." -ForegroundColor Red
    Show-Usage
    exit 1
}
else {
    Write-Host "Error: Unsupported Action '$Action'." -ForegroundColor Red
    Show-Usage
    exit 1
}




