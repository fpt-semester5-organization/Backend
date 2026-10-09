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

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pyScript = Join-Path $scriptDir "generate_schema.py"

$pyArgs = @()
if ($VerifyDb) {
    $pyArgs += "--verify-db"
}

python $pyScript @pyArgs
