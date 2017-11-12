../TestConfig.ps1

Describe "New-WikiClient" {
    It "Works" {
        $client = New-WikiClient
        $state = Save-WikiClient $client
        $client = New-WikiClient -StateContent:$state
    }
}
