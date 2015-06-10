$script:vstools = if ($env:VS140COMNTOOLS -ne $null) { $env:VS140COMNTOOLS } `
    elseif ($env:VS120COMNTOOLS -ne $null) { $env:VS120COMNTOOLS } `
    elseif ($env:VS110COMNTOOLS -ne $null) { $env:VS110COMNTOOLS }

$script:XABuildTask = [io.path]::Combine($script:vstools, '../../../msbuild/Xamarin/Android/Xamarin.Android.Build.Tasks.dll')

if (Test-Path $script:XABuildTask)
{
    Add-Type -Path $script:XABuildTask
}

function Test-XABuildPath
{
    Test-Path $script:XABuildTask
}

function Convert-Pdb2Mdb([string] $dllPath)
{
    [Pdb2Mdb.Converter]::Convert($dllPath)
}
