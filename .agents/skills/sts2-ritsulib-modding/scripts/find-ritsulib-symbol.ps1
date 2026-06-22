param(
    [Parameter(Mandatory = $true)]
    [string]$Pattern,

    [string[]]$Roots
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = "D:\My Projects\GodotProjects\the-knowledge-demon"

if (-not $Roots -or $Roots.Count -eq 0) {
    $discoveredRoots = New-Object System.Collections.Generic.List[string]
    $defaultRoots = @(
        "D:\My Projects\GodotProjects\the-knowledge-demon\.agents\RitsuLib-doc",
        "D:\My Projects\GodotProjects\STS2-RitsuLib-0.4.33",
        "D:\My Projects\GodotProjects\the-queen",
        "D:\SteamLibrary\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64",
        "D:\My Study\Slay The Spire 2 Original",
        $repoRoot
    )

    foreach ($root in $defaultRoots) {
        if (Test-Path -LiteralPath $root) {
            $discoveredRoots.Add($root)
        }
    }

    $Roots = $discoveredRoots.ToArray()
}

$allowedExtensions = @(".cs", ".md", ".json", ".gd", ".tscn", ".csproj", ".xml")
$skipPathPatterns = @(
    "\\\.git\\",
    "\\\.godot\\",
    "\\bin\\",
    "\\obj\\",
    "\\logs\\"
)
$allMatches = New-Object System.Collections.Generic.List[object]

foreach ($root in $Roots) {
    if (-not (Test-Path -LiteralPath $root)) {
        continue
    }

    try {
        $files = Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
            $skip = $false
            foreach ($patternItem in $skipPathPatterns) {
                if ($_.FullName -match $patternItem) {
                    $skip = $true
                    break
                }
            }

            ($allowedExtensions -contains $_.Extension) -and -not $skip
        }
        $matches = $files | Select-String -Pattern $Pattern -SimpleMatch
        foreach ($match in $matches) {
            $allMatches.Add([PSCustomObject]@{
                Root = $root
                Path = $match.Path
                Line = $match.LineNumber
                Text = $match.Line.Trim()
            })
        }
    }
    catch {
        Write-Warning ("Search failed under {0}: {1}" -f $root, $_.Exception.Message)
    }
}

if ($allMatches.Count -eq 0) {
    Write-Output ("No matches found for '{0}'." -f $Pattern)
    exit 0
}

$grouped = $allMatches | Group-Object Root
foreach ($group in $grouped) {
    Write-Output ("== {0} ==" -f $group.Name)
    foreach ($item in $group.Group | Sort-Object Path, Line | Select-Object -First 20) {
        Write-Output ("{0}:{1}: {2}" -f $item.Path, $item.Line, $item.Text)
    }
    if ($group.Count -gt 20) {
        Write-Output ("... ({0} more matches under this root)" -f ($group.Count - 20))
    }
    Write-Output ""
}

$total = $allMatches.Count
$rootsHit = ($grouped | Measure-Object).Count
Write-Output ("Total matches: {0} across {1} root(s)." -f $total, $rootsHit)
