param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Add','Update','Remove')]
    [string]$Action,

    [string]$Type,
    [string]$Category,
    [string]$Name,
    [string]$Amount,
    [Nullable[double]]$Value,
    [string]$Unit,
    [string]$Timestamp
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
    Write-Host "  .\Log2.ps1 -Action Add -Type <Consumption|Measurement> -Category <...> -Name <Name> [other params]" -ForegroundColor Yellow
    Write-Host "  .\Log2.ps1 -Action Update -Timestamp <ISO8601 or local> [fields to change]" -ForegroundColor Yellow
    Write-Host "  .\Log2.ps1 -Action Remove -Timestamp <ISO8601 or local>" -ForegroundColor Yellow
    Write-Host ""; Write-Host "Parameters:" -ForegroundColor Yellow
    Write-Host "  -Action     Add | Update | Remove" -ForegroundColor Yellow
    Write-Host "  -Type       Consumption | Measurement (required for Add, optional for Update)" -ForegroundColor Yellow
    Write-Host "  -Category   For Consumption: Substance | Stack. For Measurement: Vitals" -ForegroundColor Yellow
    Write-Host "  -Name       Entry name (required for Add, optional for Update)" -ForegroundColor Yellow
    Write-Host "  -Amount     String amount (required for Consumption:Substance)" -ForegroundColor Yellow
    Write-Host "  -Value      Float value (required for Measurement)" -ForegroundColor Yellow
    Write-Host "  -Unit       Unit string (required for Measurement)" -ForegroundColor Yellow
    Write-Host "  -Timestamp  Optional for Add; if given, used as event time. Required for Update/Remove" -ForegroundColor Yellow
    Write-Host ""; Write-Host ("Default log file: {0}" -f (Get-LogFilePath)) -ForegroundColor Yellow
}

function Read-LogEntries {
    param([string]$FilePath = $(Get-LogFilePath -Ensure))
    if (Test-Path -LiteralPath $FilePath) {
        $raw = Get-Content -LiteralPath $FilePath -Raw -ErrorAction Stop
        if ($null -eq $raw -or [string]::IsNullOrWhiteSpace([string]$raw)) { return @() }
        $parsed = $raw | ConvertFrom-Json -ErrorAction Stop
        if ($parsed -is [System.Array]) { return $parsed } else { return ,$parsed }
    }
    return @()
}

function Write-LogEntries {
    param(
        [Parameter(Mandatory = $true)][AllowNull()][AllowEmptyCollection()]$Entries,
        [string]$FilePath = $(Get-LogFilePath -Ensure)
    )
    $jsonOut = ConvertTo-Json -InputObject $Entries -Depth 10
    Set-Content -LiteralPath $FilePath -Value $jsonOut -Encoding UTF8 -Force
}

function Convert-DateTimeString {
    param([string]$s)
    if ([string]::IsNullOrWhiteSpace($s)) { return $null }
    $result = $null
    try {
        $dto = [System.DateTimeOffset]::ParseExact($s, 'o', [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind)
        $result = $dto
    } catch {}
    if ($null -eq $result) {
        foreach ($culture in @([System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.CultureInfo]::CurrentCulture)) {
            foreach ($style in @([System.Globalization.DateTimeStyles]::AssumeUniversal, [System.Globalization.DateTimeStyles]::AdjustToUniversal, [System.Globalization.DateTimeStyles]::None)) {
                try {
                    $dto = [System.DateTimeOffset]::Parse($s, $culture, $style)
                    $result = $dto
                    break
                } catch {}
            }
            if ($result) { break }
            try {
                $dt = [System.DateTime]::Parse($s, $culture, [System.Globalization.DateTimeStyles]::AssumeUniversal)
                $result = [System.DateTimeOffset]::new($dt)
            } catch {}
            if ($result) { break }
        }
    }
    return $result
}

function Format-Timestamp {
    param(
        [string]$s,
        [switch]$GenerateNowIfMissing
    )
    if ([string]::IsNullOrWhiteSpace($s)) {
        if ($GenerateNowIfMissing) { return (Get-Date).ToUniversalTime().ToString('o') } else { return $null }
    }
    $parsed = Convert-DateTimeString -s $s
    if ($parsed) { return $parsed.ToUniversalTime().ToString('o') } else { return $s }
}

function Find-EntryIndexByTimestamp {
    param(
        [Parameter(Mandatory = $true)]$Entries,
        [Parameter(Mandatory = $true)][string]$TimestampString
    )
    if (-not $Entries) { return -1 }
    $normalizedInput = Format-Timestamp -s $TimestampString
    for ($i = 0; $i -lt $Entries.Count; $i++) {
        $ts = [string]$Entries[$i].Timestamp
        if ($ts -eq $TimestampString) { return $i }
        $n = Format-Timestamp -s $ts
        if ($n -and $normalizedInput -and $n -eq $normalizedInput) { return $i }
    }
    return -1
}

function Test-EntryFields {
    param(
        [hashtable]$Fields
    )
    if (-not $Fields.ContainsKey('Timestamp') -or [string]::IsNullOrWhiteSpace([string]$Fields.Timestamp)) {
        throw "Timestamp is required."
    }
    if (-not $Fields.ContainsKey('Type') -or [string]::IsNullOrWhiteSpace([string]$Fields.Type)) {
        throw "Type is required."
    }
    if (-not $Fields.ContainsKey('Category') -or [string]::IsNullOrWhiteSpace([string]$Fields.Category)) {
        throw "Category is required."
    }
    if (-not $Fields.ContainsKey('Name') -or [string]::IsNullOrWhiteSpace([string]$Fields.Name)) {
        throw "Name is required."
    }

    $type = [string]$Fields.Type
    $category = [string]$Fields.Category
    switch -Regex ($type) {
        '^Consumption$' {
            if (@('Substance','Stack') -notcontains $category) {
                throw "For Type 'Consumption', Category must be 'Substance' or 'Stack'."
            }
            if ($category -eq 'Substance') {
                if (-not $Fields.ContainsKey('Amount') -or [string]::IsNullOrWhiteSpace([string]$Fields.Amount)) {
                    throw "Amount is required when Type='Consumption' and Category='Substance'."
                }
            }
        }
        '^Measurement$' {
            if ($category -ne 'Vitals') {
                throw "For Type 'Measurement', Category must be 'Vitals'."
            }
            if (-not $Fields.ContainsKey('Value') -or $null -eq $Fields.Value) {
                throw "Value (float) is required when Type='Measurement'."
            }
            if (-not $Fields.ContainsKey('Unit') -or [string]::IsNullOrWhiteSpace([string]$Fields.Unit)) {
                throw "Unit is required when Type='Measurement'."
            }
        }
        default { throw "Unsupported Type '$type'. Allowed: Consumption, Measurement." }
    }
}

function New-EntryObject {
    param(
        [hashtable]$Fields
    )
    Test-EntryFields -Fields $Fields
    $obj = [PSCustomObject]@{
        Timestamp = [string]$Fields.Timestamp
        Type      = [string]$Fields.Type
        Category  = [string]$Fields.Category
        Name      = [string]$Fields.Name
    }
    if ($Fields.ContainsKey('Amount') -and -not [string]::IsNullOrWhiteSpace([string]$Fields.Amount)) { $obj | Add-Member -NotePropertyName Amount -NotePropertyValue ([string]$Fields.Amount) }
    if ($Fields.ContainsKey('Value') -and $null -ne $Fields.Value) { $obj | Add-Member -NotePropertyName Value -NotePropertyValue ([double]$Fields.Value) }
    if ($Fields.ContainsKey('Unit') -and -not [string]::IsNullOrWhiteSpace([string]$Fields.Unit)) { $obj | Add-Member -NotePropertyName Unit -NotePropertyValue ([string]$Fields.Unit) }
    return $obj
}

function Add-LogEntry2 {
    param(
        [string]$FilePath = $(Get-LogFilePath -Ensure),
        [string]$Type,
        [string]$Category,
        [string]$Name,
        [string]$Amount,
        [Nullable[double]]$Value,
        [string]$Unit,
        [string]$Timestamp
    )

    $entries = Read-LogEntries -FilePath $FilePath

    $tsToUse = Format-Timestamp -s $Timestamp -GenerateNowIfMissing
    $fields = @{}
    $fields.Timestamp = $tsToUse
    $fields.Type = $Type
    $fields.Category = $Category
    $fields.Name = $Name
    if ($PSBoundParameters.ContainsKey('Amount')) { $fields.Amount = $Amount }
    if ($PSBoundParameters.ContainsKey('Value')) { $fields.Value = $Value }
    if ($PSBoundParameters.ContainsKey('Unit')) { $fields.Unit = $Unit }

    $newEntry = New-EntryObject -Fields $fields
    $entries += $newEntry
    Write-LogEntries -Entries $entries -FilePath $FilePath
}

function Update-LogEntry2 {
    param(
        [string]$FilePath = $(Get-LogFilePath -Ensure),
        [string]$Timestamp,
        [string]$Type,
        [string]$Category,
        [string]$Name,
        [string]$Amount,
        [Nullable[double]]$Value,
        [string]$Unit
    )

    if ([string]::IsNullOrWhiteSpace([string]$Timestamp)) { throw "-Timestamp is required for Update." }
    $entries = Read-LogEntries -FilePath $FilePath
    if (-not $entries -or $entries.Count -eq 0) { throw "No entries found to update." }

    $idx = Find-EntryIndexByTimestamp -Entries $entries -TimestampString $Timestamp
    if ($idx -lt 0) { throw "No entry found with the specified Timestamp." }

    $existing = $entries[$idx]

    $fields = @{}
    $fields.Timestamp = [string]$existing.Timestamp
    $fields.Type = [string]$existing.Type
    $fields.Category = [string]$existing.Category
    $fields.Name = [string]$existing.Name
    if ($existing.PSObject.Properties.Name -contains 'Amount') { $fields.Amount = [string]$existing.Amount }
    if ($existing.PSObject.Properties.Name -contains 'Value') { $fields.Value = [double]$existing.Value }
    if ($existing.PSObject.Properties.Name -contains 'Unit') { $fields.Unit = [string]$existing.Unit }

    if ($PSBoundParameters.ContainsKey('Type')) { $fields.Type = $Type }
    if ($PSBoundParameters.ContainsKey('Category')) { $fields.Category = $Category }
    if ($PSBoundParameters.ContainsKey('Name')) { $fields.Name = $Name }
    if ($PSBoundParameters.ContainsKey('Amount')) { $fields.Amount = $Amount }
    if ($PSBoundParameters.ContainsKey('Value')) { $fields.Value = $Value }
    if ($PSBoundParameters.ContainsKey('Unit')) { $fields.Unit = $Unit }

    $updated = New-EntryObject -Fields $fields
    $entries[$idx] = $updated
    Write-LogEntries -Entries $entries -FilePath $FilePath
}

function Remove-LogEntry2 {
    param(
        [string]$FilePath = $(Get-LogFilePath -Ensure),
        [string]$Timestamp
    )
    if ([string]::IsNullOrWhiteSpace([string]$Timestamp)) { throw "-Timestamp is required for Remove." }
    $entries = Read-LogEntries -FilePath $FilePath
    if (-not $entries -or $entries.Count -eq 0) { throw "No entries found to remove." }

    $idx = Find-EntryIndexByTimestamp -Entries $entries -TimestampString $Timestamp
    if ($idx -lt 0) { throw "No entry found with the specified Timestamp." }

    $newList = @()
    for ($i = 0; $i -lt $entries.Count; $i++) {
        if ($i -ne $idx) { $newList += $entries[$i] }
    }
    Write-LogEntries -Entries $newList -FilePath $FilePath
}

try {
    switch ($Action) {
        'Add' {
            if (-not $Type -or -not $Category -or -not $Name) {
                Write-Host "Error: -Type, -Category, and -Name are required for Action 'Add'." -ForegroundColor Red
                Show-Usage
                exit 1
            }
            Add-LogEntry2 -Type $Type -Category $Category -Name $Name -Amount $Amount -Value $Value -Unit $Unit -Timestamp $Timestamp
        }
        'Update' {
            if (-not $Timestamp) {
                Write-Host "Error: -Timestamp is required for Action 'Update'." -ForegroundColor Red
                Show-Usage
                exit 1
            }
            Update-LogEntry2 -Timestamp $Timestamp -Type $Type -Category $Category -Name $Name -Amount $Amount -Value $Value -Unit $Unit
        }
        'Remove' {
            if (-not $Timestamp) {
                Write-Host "Error: -Timestamp is required for Action 'Remove'." -ForegroundColor Red
                Show-Usage
                exit 1
            }
            Remove-LogEntry2 -Timestamp $Timestamp
        }
        default {
            Write-Host "Error: Unsupported Action '$Action'." -ForegroundColor Red
            Show-Usage
            exit 1
        }
    }
}
catch {
    Write-Host ("Error: {0}" -f $_.Exception.Message) -ForegroundColor Red
    exit 1
}

