param([string] $source, [string] $destination, [string] $pattern, [string[]] $excludes)

'Copy DLLs'

if (-not (Test-Path $destination)) {
    mkdir $destination
}

foreach ($ext in 'dll', 'pdb', 'xml') {
    $destinationFiles = [IO.Path]::Combine($destination, '*.' + $ext)
    $sourceFiles = [IO.Path]::Combine($source, $pattern + '.' + $ext)

    $len = $excludes.Length
    $excludeFiles = [string[]]::new($len)
    for ($i = 0; $i -lt $len; $i++) {
        # `$` means end of a file name excluding the extension.
        if ($excludes[$i].EndsWith("$")) {
            $excludeFiles[$i] = $excludes[$i].Substring(0, $excludes[$i].Length - 1) + '.' + $ext
        }
        else {
            $excludeFiles[$i] = $excludes[$i] + ".*"
        }
    }

    if (Test-Path $destinationFiles) { Remove-Item $destinationFiles }

    Copy-Item $sourceFiles $destination -Exclude $excludeFiles
}
