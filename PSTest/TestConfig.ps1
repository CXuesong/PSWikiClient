param(
    [switch] $Force
)

class TestConfig {
    [string] $ModulePath
    [bool] $IsDebug

    [string] $TestApiEndpoint
    [string] $LoginScriptPath

    [scriptblock] $Login
}

if ($Force -or -not $Global:testConfig)
{
    $Global:testConfig = [TestConfig]@{
        ModulePath = Join-Path $PSScriptRoot "../PSWikiClient/bin/Debug/netstandard2.0/PSWikiClient.psd1" -Resolve
        LoginScriptPath = Join-Path $PSScriptRoot "_private/Login.ps1" -Resolve
        IsDebug = $true

        TestApiEndpoint = "https://test2.wikipedia.org/w/api.php"
    }

    Import-Module $Global:testConfig.ModulePath
}

