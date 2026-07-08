param(
    [Parameter(Mandatory = $true)]
    [string] $ProjectDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── White-listed paths (files in these paths are excluded from scanning) ──
$whiteListPatterns = @(
    'wwwroot\css\app.css',
    'Themes\ClayTheme.cs',
    'Services\ClayGridPrintStyles.cs',
    'Services\ClayGridPrintHtmlGenerator.cs',
    'Services\ClayGridExcelGenerator.cs'
)

# ── Prohibited CSS properties (match property name left of colon) ──
$prohibitedProperties = @(
    'color\s*:',
    'background(?:-color)?\s*:',
    'border(?:-(?:top|right|bottom|left|color|style|width|collapse|spacing|radius))?\s*:',
    'box-shadow\s*:',
    'font-(?:family|size|weight|style)\s*:',
    'fill\s*:',
    'stroke\s*:',
    'letter-spacing\s*:',
    'text-transform\s*:'
)

# ── Prohibited value patterns (hex, rgb/rgba) ──
$prohibitedValues = @(
    '#[0-9a-fA-F]{3,8}\b',
    'rgba?\('
)

# ── Allowed structural property prefixes (skip these) ──
$allowedProperties = @(
    'display',
    'flex',
    'gap',
    'width',
    'min-width',
    'max-width',
    'height',
    'min-height',
    'max-height',
    'overflow',
    'text-overflow',
    'position',
    'z-index',
    'top',
    'right',
    'bottom',
    'left',
    'padding',
    'margin',
    'cursor',
    'opacity',
    'visibility',
    'white-space',
    'word-break',
    'align-items',
    'align-self',
    'align-content',
    'justify-content',
    'justify-self',
    'justify-items',
    'flex-wrap',
    'flex-direction',
    'flex-shrink',
    'flex-grow',
    'flex-basis',
    'place-items',
    'user-select',
    'box-sizing',
    'transform',
    'text-align',
    'vertical-align',
    'order',
    'grid',
    'column-gap',
    'row-gap'
)

# ── Regex to find style="..." and Style="..." attributes ──
# Matches both HTML style= and Blazor Style=, capturing content between quotes
$styleAttrRegex = '(?:style|Style)\s*=\s*["\"'']([^"'']*)["\"'']'
# Also catch @( ... ) expressions with Style in them (Blazor inline expressions)
$styleExprRegex = '(?:style|Style)\s*=\s*["\"'']?\@\(?([^"'']*(?:var\([^)]*\)[^"'']*)*)["\"'']?'

function Is-WhiteListed($filePath) {
    foreach ($pattern in $whiteListPatterns) {
        if ($filePath -like "*$pattern*") {
            return $true
        }
    }
    return $false
}

function Test-StyleViolation($styleContent, $filePath, $lineNum) {
    $violations = @()
    $declarations = $styleContent -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }

    foreach ($decl in $declarations) {
        # Extract property name (left of first colon)
        $colonIndex = $decl.IndexOf(':')
        if ($colonIndex -le 0) { continue }
        $propName = $decl.Substring(0, $colonIndex).Trim().ToLower()

        # Skip allowed structural properties
        $isAllowed = $false
        foreach ($allowed in $allowedProperties) {
            if ($propName -eq $allowed) {
                $isAllowed = $true
                break
            }
        }
        if ($isAllowed) { continue }

        # Check against prohibited property patterns
        foreach ($pattern in $prohibitedProperties) {
            if ($decl -match $pattern) {
                $violations += "  [line $lineNum] property '$propName' in style=`"$decl`""
                break
            }
        }
    }

    # Check against prohibited value patterns (hex, rgb/rgba)
    foreach ($pattern in $prohibitedValues) {
        $matches = [regex]::Matches($styleContent, $pattern)
        foreach ($m in $matches) {
            $violations += "  [line $lineNum] prohibited value '$($m.Value)' in style=`"$styleContent`""
        }
    }

    return $violations
}

# ── Main ──
$exitCode = 0
$razorFiles = Get-ChildItem -Path $ProjectDir -Filter *.razor -Recurse -File `
    | Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' }
$csFiles = Get-ChildItem -Path $ProjectDir -Filter *.cs -Recurse -File `
    | Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' }

$allFiles = @($razorFiles) + @($csFiles)

foreach ($file in $allFiles) {
    $filePath = $file.FullName
    if (Is-WhiteListed $filePath) { continue }

    $lines = Get-Content -Path $filePath -Encoding UTF8
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $lineNum = $i + 1

        # Find style="..." and Style="..." attributes
        $styleMatches = [regex]::Matches($line, $styleAttrRegex)
        foreach ($match in $styleMatches) {
            $styleContent = $match.Groups[1].Value
            if ([string]::IsNullOrWhiteSpace($styleContent)) { continue }

            $violations = Test-StyleViolation $styleContent $filePath $lineNum
            if ($violations.Count -gt 0) {
                $relPath = $filePath.Replace($ProjectDir, '')
                Write-Host "error CLAY002: $relPath — visual inline style violation:"
                foreach ($v in $violations) {
                    Write-Host $v
                }
                $exitCode = 1
            }
        }
    }

    # Check .cs files for <style tags (generate inline CSS)
    if ($filePath -like '*.cs') {
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            if ($line -match '<style\b') {
                $relPath = $filePath.Replace($ProjectDir, '')
                Write-Host "error CLAY002: $relPath ($($i+1)) — <style> tag in .cs file outside white-list"
                $exitCode = 1
            }
        }
    }
}

exit $exitCode
