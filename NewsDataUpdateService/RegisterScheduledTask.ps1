# Run this script as Administrator
# Right-click the file -> "Run with PowerShell" or open PowerShell as Admin and run it

$exePath = "C:\Users\samar\source\repos\FinancialApplication\NewsDataUpdateService\publish\NewsDataUpdateService.exe"
$workingDir = "C:\Users\samar\source\repos\FinancialApplication\NewsDataUpdateService\publish"

Write-Host "Registering News Update Scheduled Task..." -ForegroundColor Cyan

# Create the action
$action = New-ScheduledTaskAction `
    -Execute $exePath `
    -WorkingDirectory $workingDir

# Run daily at 8:00 AM
$trigger = New-ScheduledTaskTrigger -Daily -At "08:00AM"

# Settings
$settings = New-ScheduledTaskSettingsSet `
    -ExecutionTimeLimit (New-TimeSpan -Hours 2) `
    -RunOnlyIfNetworkAvailable `
    -StartWhenAvailable `
    -WakeToRun `
    -MultipleInstances IgnoreNew

# Register the task (runs as SYSTEM so it works even when no user is logged in)
Register-ScheduledTask `
    -TaskName "FinancialApp - News Update" `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Description "Fetches and updates financial news data daily at 8AM" `
    -User "SYSTEM" `
    -Force

if ($?) {
    Write-Host "Task registered successfully!" -ForegroundColor Green
    Write-Host "   Task Name : FinancialApp - News Update" -ForegroundColor White
    Write-Host "   Runs At   : 8:00 AM Daily" -ForegroundColor White
    Write-Host "   Executable: $exePath" -ForegroundColor White
    Write-Host ""
    Write-Host "To verify, open Task Scheduler (taskschd.msc) and look for 'FinancialApp - News Update'" -ForegroundColor Yellow
} else {
    Write-Host "Registration failed. Make sure you are running as Administrator." -ForegroundColor Red
}

Read-Host "Press Enter to exit"
