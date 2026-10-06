param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [switch]$RecoverStaleLock
)

$ErrorActionPreference = "Stop"
Push-Location $Root
try {
    Write-Host "Validating Hermes working set..."
    python .\cron\validate_working_set.py --root .
    if ($LASTEXITCODE -ne 0) {
        throw "Working-set validation failed."
    }

    $argsList = @(".\cron\prepare_run.py", "--root", ".")
    if ($RecoverStaleLock) {
        $argsList += "--recover-stale-lock"
    }

    Write-Host "Preparing exactly one story..."
    python @argsList
    $code = $LASTEXITCODE

    switch ($code) {
        0 {
            Write-Host ""
            Write-Host "Run prepared. Hermes must now read:"
            Write-Host "  .hermes\current-run\PROMPT.md"
            Write-Host "  .hermes\current-run\selected_agent_package.md"
            Write-Host ""
            Get-Content .\.hermes\current-run\PROMPT.md
        }
        20 { Write-Host "All stories are DONE."; exit 0 }
        21 { Write-Host "Active sprint has no eligible story. Do not jump to a later sprint."; exit 0 }
        22 { throw "Backlog conflict: multiple IN PROGRESS stories." }
        30 { Write-Host "Another Hermes run owns the lock; exiting without changes."; exit 0 }
        default { throw "prepare_run.py failed with exit code $code" }
    }
}
finally {
    Pop-Location
}
