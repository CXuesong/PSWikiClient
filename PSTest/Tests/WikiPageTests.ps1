../TestConfig.ps1

Context "Shared WikiSite" {
    $client = New-WikiClient
    $site = New-WikiSite $client $Global:TestConfig.TestApiEndpoint
    & $Global:TestConfig.LoginScriptPath $site

    Describe "New-WikiPage" {
        It "Works" {
            # Or "Project:Sandbox" | New-WikiPage $site
            $page = New-WikiPage $site Project:Sandbox
            $page.Title | Should -Be Wikipedia:Sandbox
        }
    }

    Describe "Read-WikiPage" {
        It "Works" {
            $pages = @("Project:Sandbox", "Main Page", "Non-existent page") | %{New-WikiPage $site $_}
            Refresh-WikiPage $pages
            $pages[0].Exists | Should -Be $true
            $pages[1].Exists | Should -Be $true
            $pages[2].Exists | Should -Be $false
        }
    }

    Describe "Write-WikiPage" {
        It "Works" {
            $page = New-WikiPage $site Project:Sandbox
            Refresh-WikiPage $page -Content
            $page.Content += "\n\nTest edit."
            Update-WikiPage $page -Summary:"Test edit from PSWikiClient." -Bot -Minor
        }
    }

    Logout-WikiSite $site
}
