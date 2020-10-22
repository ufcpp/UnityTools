$ProjectDir = [string]$env:ProjectDir
$ProjectDir = $ProjectDir.TrimStart('"').TrimEnd('"')

$TargetDir = [string]$env:TargetDir
$TargetDir = $TargetDir.TrimStart('"').TrimEnd('"')

$settingPath = $ProjectDir + '\CopySettings.json'
$settings = Get-Content $settingPath -Encoding UTF8 -Raw  | ConvertFrom-Json

$dllPath = [IO.Path]::Combine($ProjectDir, $settings.destination)
[string[]] $excludes = $settings.excludes

$pattern = $settings.pattern
if ($null -eq $pattern) { $pattern = '*' }

$excludesFromFolder = @()

foreach ($excludeFolder in $settings.exclude_folders) {
    foreach ($excludeFile in Get-ChildItem ($ProjectDir + $excludeFolder)) {
        if (-not ([string]$excludeFile).EndsWith(('meta'))) {
            $excludesFromFolder += [IO.Path]::GetFileNameWithoutExtension($excludeFile.Name)
        }
    }
}

foreach ($excludeFile in ($excludesFromFolder | Get-Unique)) {
    $excludes += $excludeFile
}

. ./copy_dlls.ps1 $TargetDir $dllPath $pattern $excludes
