param([string] $source, [string] $destination, [string] $pattern, [string[]] $excludes)

foreach ($ext in 'dll', 'pdb', 'xml', 'dll.mdb')
{
    $destinationFiles = [IO.Path]::Combine($destination, '*.' + $ext)
    $sourceFiles = [IO.Path]::Combine($source, $pattern + '.' + $ext)
    [string[]] $excludeFiles = $excludes | %{ '*' + $_ + '.*'}

    if (Test-Path $destinationFiles) { rm $destinationFiles }

    Copy-Item $sourceFiles $destination -Exclude $excludeFiles
}

. ./Pdb2Mdb.ps1

pushd $destination

try
{
    if (Test-XABuildPath)
    {
        ls *.dll | %{ . Convert-Pdb2Mdb $_.FullName }
    }
    else
    {
        $unityPath = Get-Item 'HKCU:\Software\Unity Technologies\Installer\Unity' | Get-ItemProperty -Name 'Location x64' | %{ $_.'Location x64' }
        $monoFolder = $unityPath + '\Editor\Data\MonoBleedingEdge'
        $pdb2mdb = $monoFolder + '\lib\mono\4.5\pdb2mdb.exe'
        $cli = $monoFolder + '\bin\cli.bat'

        $null = ls *.dll | %{ . $cli $pdb2mdb $_.Name } 2>&1
    }
}
catch {}

popd
