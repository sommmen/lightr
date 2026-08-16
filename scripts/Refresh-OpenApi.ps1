[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectDirectory = Join-Path $PSScriptRoot '..\src\Lightr\Lightr'

# Run from the project directory because dotnet-openapi resolves its output path from the current directory.
Push-Location $projectDirectory
try {
    & dotnet tool run dotnet-openapi -- refresh 'https://app.lightr.nl/api/docs.json' --updateProject 'Lightr.csproj'
    $refreshExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($refreshExitCode -ne 0) {
    throw 'The OpenAPI document could not be refreshed.'
}
