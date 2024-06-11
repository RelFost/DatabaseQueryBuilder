param (
    [string]$CsprojPath,
    [string]$DllName
)

# Установить политику выполнения скриптов для текущего сеанса
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process -Force

# Обновление версии в csproj
Write-Output "Updating version in $CsprojPath"

[xml]$xml = Get-Content $CsprojPath
$versionNode = $xml.Project.PropertyGroup.Version

if ($versionNode -ne $null) {
    $versionRegex = [regex] '(\d+)\.(\d+)\.(\d+)\.(\d+)'
    if ($versionRegex.IsMatch($versionNode)) {
        $matches = $versionRegex.Match($versionNode)
        $major = $matches.Groups[1].Value
        $minor = $matches.Groups[2].Value
        $build = [int]$matches.Groups[3].Value
        $revision = [int]$matches.Groups[4].Value + 1
        $newVersion = "$major.$minor.$build.$revision"
        Write-Output "newVersion $newVersion."
        
        # Найдем все узлы PropertyGroup
        $propertyGroups = $xml.Project.PropertyGroup
        
        foreach ($group in $propertyGroups) {
            if ($group.Version -ne $null) {
                $group.Version = $newVersion
                Write-Output "Version updated successfully."
                break
            }
        }
        
        $xml.Save($CsprojPath)
    } else {
        Write-Output "Version format is incorrect."
        exit 1
    }
} else {
    Write-Output "Version node not found in the .csproj file."
    exit 1
}

# Сборка проекта
Write-Output "Starting build process..."
cd src
dotnet build -c Carbon
if ($LASTEXITCODE -ne 0) {
    Write-Output "Build failed."
    exit 1
}
cd ..

# Перемещение собранного файла
$sourcePath = "src/bin/Carbon/netframework4.8.1/$DllName.dll"
$destinationPath = "build/carbon/extensions/"
if (Test-Path -Path $sourcePath) {
    Move-Item -Path $sourcePath -Destination $destinationPath -Force
    Write-Output "Files moved successfully."
} else {
    Write-Output "Failed to find the file."
    exit 1
}
