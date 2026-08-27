param(
    [Parameter(Mandatory = $true)]
    [string] $AssemblyVersion,

    [Parameter(Mandatory = $true)]
    [string] $Tag,

    [Parameter(Mandatory = $true)]
    [string] $OutputPath
)

$owner = "miqote69"
$sourceRepository = "MinionMirage"
$distributionRepository = "MinionMirage-Distribution"
$opaquePath = "2d478e48ca56c3c2453cc2e69d41d02e54c41e23c6c69cf805b332a916273011"
$downloadBaseUrl = "https://downloads.miqote69.com/minion-mirage/$Tag"
$lastUpdate = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()

$entry = [ordered]@{
    Author = $owner
    Name = "Minion Mirage"
    Description = "Replaces supported local-player minions with fixed NPC appearances."
    InternalName = "MinionMirage"
    AssemblyVersion = $AssemblyVersion
    TestingAssemblyVersion = $null
    RepoUrl = "https://github.com/$owner/$sourceRepository"
    ApplicableVersion = "any"
    DalamudApiLevel = 15
    Punchline = "Transform your minions into familiar NPCs."
    Tags = @(
        "minion",
        "companion",
        "npc",
        "cosmetic"
    )
    MinimumDalamudVersion = "15.0.0.0"
    IsHide = $false
    IsTestingExclusive = $false
    IconUrl = "https://raw.githubusercontent.com/$owner/$distributionRepository/main/.dalamud/$opaquePath/icon.png"
    DownloadLinkInstall = "$downloadBaseUrl/install"
    DownloadLinkTesting = "$downloadBaseUrl/testing"
    DownloadLinkUpdate = "$downloadBaseUrl/update"
    LastUpdate = $lastUpdate
}

$absoluteOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [System.IO.Path]::GetDirectoryName($absoluteOutputPath)
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
}

$json = ConvertTo-Json -InputObject @($entry) -Depth 5
[System.IO.File]::WriteAllText($absoluteOutputPath, $json, [System.Text.UTF8Encoding]::new($false))
