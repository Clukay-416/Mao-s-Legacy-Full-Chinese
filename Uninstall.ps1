param([string]$GameRoot='')
$ErrorActionPreference='Stop'
if(-not $GameRoot){
    $candidates=@("D:\SteamLibrary\steamapps\common\Mao's Legacy")
    $steam=(Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamPath
    if($steam){
        $candidates+=Join-Path $steam "steamapps\common\Mao's Legacy"
        $libraries=Join-Path $steam 'steamapps\libraryfolders.vdf'
        if(Test-Path -LiteralPath $libraries){
            foreach($line in Get-Content -LiteralPath $libraries){
                if($line -match '"path"\s+"([^"]+)"'){
                    $library=$Matches[1].Replace('\\','\')
                    $candidates+=Join-Path $library "steamapps\common\Mao's Legacy"
                }
            }
        }
    }
    $found=@($candidates | Select-Object -Unique | Where-Object {Test-Path -LiteralPath (Join-Path $_ 'China.exe') -PathType Leaf})
    if($found.Count -eq 1){$GameRoot=$found[0]}
    else{$GameRoot=Read-Host 'Enter the game folder containing China.exe'}
}
if(-not $GameRoot){throw 'A game folder is required.'}
$GameRoot=[IO.Path]::GetFullPath($GameRoot).TrimEnd('\')
function TargetPath([string]$relative){
    if([IO.Path]::IsPathRooted($relative)){throw 'Invalid path.'}
    $path=[IO.Path]::GetFullPath((Join-Path $GameRoot $relative))
    if(-not $path.StartsWith($GameRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Path is outside the game folder.'}
    return $path
}
function Hash([string]$path){if(Test-Path -LiteralPath $path -PathType Leaf){return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()};return $null}
$running=Get-Process China -ErrorAction SilentlyContinue | Where-Object {$_.Path -and $_.Path.Equals((TargetPath 'China.exe'),[StringComparison]::OrdinalIgnoreCase)}
if($running){throw 'Close the game before uninstalling the patch.'}
$backup=TargetPath '.MaoChineseBackup'
$record=Join-Path $backup 'manifest.json'
if(-not (Test-Path -LiteralPath $record)){throw 'Original backup was not found.'}
$manifest=Get-Content -LiteralPath $record -Raw -Encoding UTF8 | ConvertFrom-Json
foreach($entry in $manifest.files){
    $current=Hash (TargetPath $entry.path)
    if($current -ne $entry.patched -and $current -ne $entry.original){throw ('The game file has changed. Refusing to overwrite: '+$entry.path)}
    if($entry.original -and (Hash (Join-Path $backup $entry.path)) -ne $entry.original){throw ('Backup checksum failed: '+$entry.path)}
}
foreach($entry in $manifest.files){
    $target=TargetPath $entry.path
    if($entry.original){Copy-Item -LiteralPath (Join-Path $backup $entry.path) -Destination $target -Force}
    elseif((Hash $target) -eq $entry.patched){Remove-Item -LiteralPath $target}
}
Write-Host 'Original game files restored. The backup folder has been retained.'
