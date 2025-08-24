# OAuth2 Test Client Node.js Auto Launch Script
# PowerShell Version

param(
    [int]$Port = 3000,
    [switch]$OpenBrowser = $true
)

# Script Information
Write-Host "OAuth2 Test Client Node.js Auto Launch Script" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# Current Directory Check
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$TestClientsDir = Join-Path $ProjectRoot "test-clients"
$NodeScript = Join-Path $TestClientsDir "start-oauth2-client.js"

Write-Host "Project Root: $ProjectRoot" -ForegroundColor Yellow
Write-Host "Test Clients Directory: $TestClientsDir" -ForegroundColor Yellow
Write-Host "Node.js Script: $NodeScript" -ForegroundColor Yellow
Write-Host ""

# Node.js Script Existence Check
if (-not (Test-Path $NodeScript)) {
    Write-Host "Node.js script not found: $NodeScript" -ForegroundColor Red
    Write-Host "Please check if start-oauth2-client.js exists in test-clients directory." -ForegroundColor Red
    exit 1
}

# Port Usage Check
Write-Host "Checking port $Port usage..." -ForegroundColor Yellow
$PortInUse = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue

if ($PortInUse) {
    Write-Host "Port $Port is already in use." -ForegroundColor Yellow
    Write-Host "Processes using the port:" -ForegroundColor Yellow
    $PortInUse | ForEach-Object {
        $Process = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
        if ($Process) {
            Write-Host "  - PID: $($_.OwningProcess), Process: $($Process.ProcessName)" -ForegroundColor Gray
        }
    }
    
    $Choice = Read-Host "Do you want to terminate the processes and continue? (y/N)"
    if ($Choice -eq 'y' -or $Choice -eq 'Y') {
        Write-Host "Terminating processes using port $Port..." -ForegroundColor Yellow
        $PortInUse | ForEach-Object {
            try {
                Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
                Write-Host "  - PID $($_.OwningProcess) terminated" -ForegroundColor Green
            } catch {
                Write-Host "  - Failed to terminate PID $($_.OwningProcess): $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        Start-Sleep -Seconds 2
    } else {
        Write-Host "User cancelled." -ForegroundColor Red
        exit 1
    }
}

# Node.js Installation Check
Write-Host "Checking Node.js installation..." -ForegroundColor Yellow
try {
    $NodeVersion = node --version 2>&1
    Write-Host "Node.js version: $NodeVersion" -ForegroundColor Green
} catch {
    Write-Host "Node.js is not installed or not in PATH." -ForegroundColor Red
    Write-Host "Please install Node.js from https://nodejs.org/ and add it to PATH." -ForegroundColor Red
    exit 1
}

# Change to Test Clients Directory
Write-Host "Changing working directory: $TestClientsDir" -ForegroundColor Yellow
Set-Location $TestClientsDir

# Browser Auto Launch Function
function Start-Browser {
    param([string]$Url)
    
    Write-Host "Opening browser: $Url" -ForegroundColor Green
    
    try {
        Start-Process $Url
        Write-Host "Browser opened successfully." -ForegroundColor Green
    } catch {
        Write-Host "Failed to open browser: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "Please open $Url manually in your browser." -ForegroundColor Yellow
    }
}

# Start Node.js Server
Write-Host ""
Write-Host "Starting OAuth2 Test Client server with Node.js..." -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan

try {
    # Background Node.js Server Start
    $ServerUrl = "http://localhost:$Port"
    
    if ($OpenBrowser) {
        # Auto Launch Browser (with delay)
        Start-Job -ScriptBlock {
            Start-Sleep -Seconds 3
            Start-Process "http://localhost:$using:Port"
        }
    }
    
    # Execute Node.js Server
    node $NodeScript
    
} catch {
    Write-Host "Server start failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    Write-Host ""
    Write-Host "Server stopped." -ForegroundColor Yellow
}
