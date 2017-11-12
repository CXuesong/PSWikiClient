../TestConfig.ps1

Context "Shared WikiClient" {
    $client = New-WikiClient

    Describe "New-WikiSite" {
        It "Works" {
            $site = New-WikiSite $client $Global:TestConfig.TestApiEndpoint
            Write-Verbose -Verbose $site.SiteInfo.SiteName
        }
    }

    Describe "Login-WikiSite" {
        It "CanLoginLogout" {
            $site = New-WikiSite $client $Global:TestConfig.TestApiEndpoint
            & $Global:TestConfig.LoginScriptPath $site
            Write-Verbose -Verbose $site.AccountInfo
            $site.AccountInfo.IsUser | Should -Be $true
            $site.AccountInfo.IsAnonymous | Should -Be $false
            Logout-WikiSite $site
        }
    }
}
