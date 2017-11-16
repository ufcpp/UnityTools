# Program Files (x86) があればそれを、なければ Program Files を使う
if (Test-Path ${env:ProgramFiles(x86)})
{
    $Script:ProgramFiles = ${env:ProgramFiles(x86)}
}
else
{
    $Script:ProgramFiles = $env:ProgramFiles
}

# 候補。上から順に調べて、最初に見つけたやつを返す
$Script:items =
    # VS 2017
    # side by side インストールできるようになったせいで何か所か探さないとダメ
    '\Microsoft Visual Studio\2017\Community\',
    '\Microsoft Visual Studio\2017\Professional\',
    '\Microsoft Visual Studio\2017\Enterprise\',
    '\Microsoft Visual Studio\2017\BuildTools\',
    # VS 2015, 2013
    $env:VS140COMNTOOLS + '../../../',
    $env:VS120COMNTOOLS + '../../../'

foreach ($Script:item in $Script:items)
{
    $script:XABuildTask = $Script:ProgramFiles + $Script:item + 'msbuild/Xamarin/Android/Xamarin.Android.Build.Tasks.dll'
    if (Test-Path $script:XABuildTask) { break; }
}

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
