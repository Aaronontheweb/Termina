function Get-ReleaseNotes {
    param (
        [Parameter(Mandatory=$true)]
        [string]$MarkdownFile
    )

    # Read markdown file content
    $content = Get-Content -Path $MarkdownFile -Raw

    # Output object to store result
    $outputObject = [PSCustomObject]@{
        Version       = $null
        Date          = $null
        ReleaseNotes  = $null
    }

    # Split on #### delimiter: section 0 = header, section 1 = release body
    $sections = $content -split "####"
    
    if ($sections.Count -ge 2) {
        $headerSection = $sections[0].Trim()
        $releaseNotes = $sections[1].Trim()

        # Extract version from "# Release Notes — Termina 0.14.0-beta.2"
        if ($headerSection -match "^# Release Notes — Termina\s+(.+)") {
            $outputObject.Version = $matches[1]
        }
        
        # Extract date from "**Release date:** 2026-06-20"
        if ($headerSection -match "\*\*Release date:\*\*\s+(\d{4}-\d{2}-\d{2})") {
            $outputObject.Date = $matches[1]
        }

        $outputObject.ReleaseNotes = $releaseNotes
    }

    return $outputObject
}

# Call function example:
#$result = Get-ReleaseNotes -MarkdownFile "$PSScriptRoot\RELEASE_NOTES.md"
#Write-Output "Version: $($result.Version)"
#Write-Output "Date: $($result.Date)"
#Write-Output "Release Notes:"
#Write-Output $result.ReleaseNotes
