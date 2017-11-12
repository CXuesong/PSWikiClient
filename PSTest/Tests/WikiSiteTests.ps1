../TestConfig.ps1

Describe "New-WikiSite" {
    $client = New-WikiClient
    $site = New-WikiSite $client $Global:TestConfig.TestApiEndpoint
    It "Works" {
        Out-Host $site.SiteInfo
    }
}
