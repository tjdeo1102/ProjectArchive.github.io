param(
    [Parameter(Mandatory = $true)]
    [string] $WebBuildPath
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$webBuildRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Builds\WebGL'))
$source = [IO.Path]::GetFullPath($WebBuildPath)
if (-not $source.StartsWith($webBuildRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'WebBuildPath must be a versioned build inside Builds/WebGL.'
}
if (-not (Test-Path -LiteralPath (Join-Path $source 'index.html') -PathType Leaf)) {
    throw 'WebGL index.html is missing.'
}
if (-not (Test-Path -LiteralPath (Join-Path $source 'Build') -PathType Container)) {
    throw 'WebGL Build directory is missing.'
}

$sensitiveNames = @('llm_config.json', 'llm_config.local.json')
$sensitiveFiles = @(Get-ChildItem -LiteralPath $source -Recurse -File | Where-Object {
    $sensitiveNames -contains $_.Name -or $_.Name -match '(?i)(api[_-]?key|secret|credential|\.env)'
})
if ($sensitiveFiles.Count -gt 0) {
    throw 'WebGL output contains a sensitive configuration filename; packaging stopped.'
}

$version = Split-Path -Path $source -Leaf
if ($version -notmatch '^v[0-9]+\.[0-9]+\.[0-9]+$') {
    throw 'The WebGL output directory must have a vMAJOR.MINOR.PATCH name.'
}
$destination = Join-Path $projectRoot "Builds\Deploy\LocalWebPreview-$version"
if (Test-Path -LiteralPath $destination) {
    throw "The package already exists: $destination"
}

$serviceProject = Join-Path $PSScriptRoot 'AgentFrameworkService.csproj'
dotnet publish $serviceProject --configuration Release --output $destination --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Agent Framework publish failed with exit code $LASTEXITCODE."
}

$webRoot = Join-Path $destination 'wwwroot'
New-Item -ItemType Directory -Path $webRoot -ErrorAction Stop | Out-Null
Get-ChildItem -LiteralPath $source -Force | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $webRoot -Recurse -Force
}

Write-Output "Local package ready: $destination"
Write-Output 'Each visitor uploads a personal Provider JSON file in the browser; no server Provider secret is required.'
Write-Output 'Do not enable AGENT_FRAMEWORK_LOCAL_DEV_MODE for this package.'
