param([string] $source, [string] $destination, [string] $pattern, [string[]] $excludes)

'Copy DLLs'

if (-not (Test-Path $destination))
{
    mkdir $destination
}

foreach ($ext in 'dll', 'pdb', 'xml', 'dll.mdb')
{
    $destinationFiles = [IO.Path]::Combine($destination, '*.' + $ext)
    $sourceFiles = [IO.Path]::Combine($source, $pattern + '.' + $ext)

    $excludeFiles = $excludes
    $len = $excludes.Length
    for ($i = 0; $i -lt $len; $i++)
    {
        # When end with '$' character, excludes above extention specified files and full match ones only.
        if ($excludes[$i].EndWith("$"))
        {
            $excludeFiles[$i] = $excludes[$i].Substring(0, $excludes[$i].Length - 1) + '.' + $ext
        }
        else
        {
            $excludeFiles[$i] = $excludes[$i] + ".*"
        }
    }

    if (Test-Path $destinationFiles) { rm $destinationFiles }

    Copy-Item $sourceFiles $destination -Exclude $excludeFiles
}

. ./Pdb2Mdb.ps1

pushd $destination

if (Test-XABuildPath)
{
    ls *.dll | %{ try {
        $dll = $_.FullName
        $pdb = $dll -replace "\.dll", ".pdb"
        if (Test-Path $pdb)
        {
            . Convert-Pdb2Mdb $dll
        }
    }
    catch {
@"
pdb2mdb error
    $dll
    $($_.Exception.Message)
"@
    }}
}
else
{
    $unityPath = Get-Item 'HKCU:\Software\Unity Technologies\Installer\Unity' | Get-ItemProperty -Name 'Location x64' | %{ $_.'Location x64' }
    $monoFolder = $unityPath + '\Editor\Data\MonoBleedingEdge'
    $pdb2mdb = $monoFolder + '\lib\mono\4.5\pdb2mdb.exe'
    $cli = $monoFolder + '\bin\cli.bat'

    $null = ls *.dll | %{ try { . $cli $pdb2mdb $_.Name } catch { } } 2>&1
}

popd
