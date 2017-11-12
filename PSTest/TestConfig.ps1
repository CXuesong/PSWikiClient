param(
    [bool] $Force
)

class TestConfig {
    [string] $ModulePath
    [bool] $IsDebug

    [string] $TestApiEndpoint
}

if ($Force -or $Global:testConfig -eq $null)
{
    $Global:testConfig = [TestConfig]@{
        ModulePath = "../PSWikiClient/bin/Debug/netstandard2.0/PSWikiClient.dll"
        IsDebug = $true

        TestApiEndpoint = "https://test2.wikipedia.org/w/api.php"
    }

    Import-Module $Global:testConfig.ModulePath
}
