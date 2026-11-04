<#
.SYNOPSIS
    VeggieLife Database Schema Generator & Validator Tool (Sprint 5: D1-D2)
.DESCRIPTION
    Tự động đọc tài liệu "06. Logical Data Model Specification.docx" để đối soát và sinh schema SQL.
.PARAMETER VerifyDb
    Kiểm tra đếm trực tiếp trên SQL Server database.
#>
[CmdletBinding()]
param (
    [switch]$VerifyDb
)

$ErrorActionPreference = "Stop"

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::InputEncoding  = [System.Text.Encoding]::UTF8
$OutputEncoding           = [System.Text.Encoding]::UTF8
chcp 65001 | Out-Null

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pyScript = Join-Path $scriptDir "generate_schema.py"

$pyArgs = @()
if ($VerifyDb) {
    $pyArgs += "--verify-db"
}

python $pyScript @pyArgs