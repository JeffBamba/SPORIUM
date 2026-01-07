$ErrorActionPreference = 'Stop'

# Linking Toast — Audit script
# Generates a matrix of NotificationTypeSpecDefaults vs actual trigger usage in Scripts.

$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$defaults = Join-Path $root 'Assets\_Project\Scripts\UI\UIToolkit\NotificationsFoundation\NotificationTypeSpecDefaults.cs'
$scriptRoot = Join-Path $root 'Assets\_Project\Scripts'
$outMd = Join-Path $root 'Assets\_Project\Docs\NOTIFICATIONS_LINKING_TOAST_MATRIX.md'

function Get-QuotedValue([string]$line) {
  $s = $line.IndexOf('"')
  $e = $line.LastIndexOf('"')
  if ($s -ge 0 -and $e -gt $s) { return $line.Substring($s + 1, $e - $s - 1) }
  return ''
}

Write-Host "[LinkingToast] Root: $root"
Write-Host "[LinkingToast] Defaults: $defaults"
Write-Host "[LinkingToast] ScriptRoot: $scriptRoot"

$lines = Get-Content -LiteralPath $defaults
$specs = New-Object System.Collections.Generic.List[object]

for ($i = 0; $i -lt $lines.Count; $i++) {
  $l = $lines[$i]
  # Example line:
  # Spec("SYS-100", NotificationCategory.System, NotificationSeverity.Info, NotificationChannel.Gameplay, ..., 30f,
  if ($l -match 'Spec\(\"(?<code>[A-Z0-9\-]+)\",\s*NotificationCategory\.(?<cat>\w+),\s*NotificationSeverity\.(?<sev>\w+),\s*NotificationChannel\.(?<ch>\w+).*?,\s*(?<cool>\d+(?:\.\d+)?)f,') {
    $code = $matches.code
    $cat = $matches.cat
    $sev = $matches.sev
    $ch = $matches.ch
    $cool = $matches.cool

    $loc = ''; $it = ''; $en = ''
    for ($j = $i + 1; $j -lt [Math]::Min($i + 14, $lines.Count); $j++) {
      if (-not $lines[$j].Contains('"')) { continue }
      if ($loc -eq '') { $loc = Get-QuotedValue $lines[$j]; continue }
      if ($it -eq '') { $it = Get-QuotedValue $lines[$j]; continue }
      if ($en -eq '') { $en = Get-QuotedValue $lines[$j]; break }
    }

    $specs.Add([pscustomobject]@{
      Code     = $code
      Channel  = $ch
      Severity = $sev
      Category = $cat
      Cooldown = $cool
      LocKey   = $loc
      IT       = $it
      EN       = $en
    }) | Out-Null
  }
}

Write-Host "[LinkingToast] Parsed specs: $($specs.Count)"

# Scan triggers (multiline-safe)
$postToast = @{}
$upsert = @{}

$allFiles = Get-ChildItem -LiteralPath $scriptRoot -Recurse -File -Filter '*.cs'
$searchFiles = $allFiles | Where-Object { $_.FullName -ne $defaults }
foreach ($f in $allFiles) {
  $raw = Get-Content -LiteralPath $f.FullName -Raw
  if ([string]::IsNullOrEmpty($raw)) { continue }

  foreach ($m in [regex]::Matches($raw, 'PostToast\s*\(\s*"(?<code>[A-Z0-9-]+)"', [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
    $c = $m.Groups['code'].Value
    if (-not $postToast.ContainsKey($c)) { $postToast[$c] = @() }
    $lineNum = ($raw.Substring(0, $m.Index).Split("`n")).Count
    $postToast[$c] += "$($f.FullName):$lineNum"
  }

  foreach ($m in [regex]::Matches($raw, 'UpsertDanger\s*\(\s*[^,]*?,\s*"(?<code>[A-Z0-9-]+)"', [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
    $c = $m.Groups['code'].Value
    if (-not $upsert.ContainsKey($c)) { $upsert[$c] = @() }
    $lineNum = ($raw.Substring(0, $m.Index).Split("`n")).Count
    $upsert[$c] += "$($f.FullName):$lineNum"
  }
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('## Linking Toast — Matrix')
[void]$sb.AppendLine('Auto-generated from `NotificationTypeSpecDefaults` + scan of `Assets/_Project/Scripts` for `PostToast(\"CODE\")` and `UpsertDanger(...,\"CODE\")`.')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('|Code|Channel|Severity|Category|Cooldown(s)|TriggerStatus|TriggerFound|')
[void]$sb.AppendLine('|---|---|---|---|---:|---|---|')

foreach ($s in ($specs | Sort-Object Code)) {
  $status = 'MISSING'
  $found = ''

  if ($s.Channel -eq 'Lore') {
    $status = 'TRIGGERED'
    $found = 'LoreScheduler (channel=Lore)'
  }
  if ($upsert.ContainsKey($s.Code)) {
    $status = 'TRIGGERED'
    $found = 'Watcher UpsertDanger: ' + (($upsert[$s.Code] | Select-Object -First 2) -join '; ')
  }
  if ($postToast.ContainsKey($s.Code)) {
    $status = 'TRIGGERED'
    $found = 'CallSite PostToast: ' + (($postToast[$s.Code] | Select-Object -First 2) -join '; ')
  }

  if ($status -eq 'MISSING') {
    $hits = Select-String -Path $searchFiles.FullName -SimpleMatch -Pattern $s.Code -ErrorAction SilentlyContinue
    if ($hits) {
      $status = 'REFERENCED'
      $found = 'Referenced: ' + (($hits | Select-Object -First 2 | ForEach-Object { $_.Path + ':' + $_.LineNumber }) -join '; ')
    }
  }

  $safeFound = $found -replace '\|', '/'
  [void]$sb.AppendLine(('|' + $s.Code + '|' + $s.Channel + '|' + $s.Severity + '|' + $s.Category + '|' + $s.Cooldown + '|' + $status + '|' + $safeFound + '|'))
}

Set-Content -LiteralPath $outMd -Value $sb.ToString() -Encoding UTF8
Write-Host "[LinkingToast] Wrote: $outMd"


