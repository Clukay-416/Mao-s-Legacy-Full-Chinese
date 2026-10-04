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
$GameRoot=[IO.Path]::GetFullPath($GameRoot.Trim('"')).TrimEnd('\')
$packageRoot=[IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')
function ScopedPath([string]$root,[string]$relative){
    if([IO.Path]::IsPathRooted($relative)){throw 'Invalid package path.'}
    $path=[IO.Path]::GetFullPath((Join-Path $root $relative))
    if(-not $path.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Path is outside the selected folder.'}
    return $path
}
function Hash([string]$path){if(Test-Path -LiteralPath $path -PathType Leaf){return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()};return $null}
$exe=ScopedPath $GameRoot 'China.exe'
if(-not (Test-Path -LiteralPath $exe -PathType Leaf)){throw 'China.exe was not found in the selected game folder.'}
$running=Get-Process China -ErrorAction SilentlyContinue | Where-Object {$_.Path -and [IO.Path]::GetFullPath($_.Path).Equals($exe,[StringComparison]::OrdinalIgnoreCase)}
if($running){throw 'Close the game before installing the Chinese patch.'}
$manifest=Get-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$backup=ScopedPath $GameRoot '.MaoChineseBackup'
$allInstalled=$true
foreach($entry in $manifest.files){
    if((Hash (ScopedPath $packageRoot $entry.payload)) -ne $entry.payload_sha256){throw ('Package checksum failed: '+$entry.payload)}
    $current=Hash (ScopedPath $GameRoot $entry.path)
    if($current -ne $entry.patched){$allInstalled=$false}
    if($current -ne $entry.original -and $current -ne $entry.patched){throw ('Unsupported game version or modified file: '+$entry.path)}
    if($entry.original){
        $backupFile=ScopedPath $backup $entry.path
        if(Test-Path -LiteralPath $backupFile){
            if((Hash $backupFile) -ne $entry.original){throw ('Backup checksum failed: '+$entry.path)}
        }elseif($current -ne $entry.original){throw ('The original backup is missing: '+$entry.path)}
    }
}
if($allInstalled){Write-Host 'Chinese patch is already installed.';exit 0}
if(-not ('MaoDelta' -as [type])){Add-Type -Path (Join-Path $packageRoot 'Source\Delta.cs')}
$stage=ScopedPath $GameRoot ('.MaoChineseStaging-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
$writesStarted=$false
try{
    # Reconstruct and verify every result before changing installed files.
    foreach($entry in $manifest.files){
        $prepared=ScopedPath $stage $entry.path
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($prepared)) -Force | Out-Null
        $payload=ScopedPath $packageRoot $entry.payload
        if($entry.original){
            $original=ScopedPath $backup $entry.path
            if(-not (Test-Path -LiteralPath $original)){$original=ScopedPath $GameRoot $entry.path}
            [MaoDelta]::Apply($original,$payload,$prepared)
        }else{Copy-Item -LiteralPath $payload -Destination $prepared}
        if((Hash $prepared) -ne $entry.patched){throw ('Reconstructed checksum failed: '+$entry.path)}
    }
    foreach($entry in $manifest.files){
        if($entry.original){
            $backupFile=ScopedPath $backup $entry.path
            if(-not (Test-Path -LiteralPath $backupFile)){
                New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($backupFile)) -Force | Out-Null
                Copy-Item -LiteralPath (ScopedPath $GameRoot $entry.path) -Destination $backupFile
                if((Hash $backupFile) -ne $entry.original){throw ('Backup checksum failed: '+$entry.path)}
            }
        }
    }
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $packageRoot 'manifest.json') -Destination (Join-Path $backup 'manifest.json') -Force
    $writesStarted=$true
    foreach($entry in $manifest.files){
        $target=ScopedPath $GameRoot $entry.path
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
        Copy-Item -LiteralPath (ScopedPath $stage $entry.path) -Destination $target -Force
        if((Hash $target) -ne $entry.patched){throw ('Installed checksum failed: '+$entry.path)}
    }
}catch{
    if($writesStarted){
        foreach($entry in $manifest.files){
            $target=ScopedPath $GameRoot $entry.path
            if($entry.original){Copy-Item -LiteralPath (ScopedPath $backup $entry.path) -Destination $target -Force}
            elseif((Hash $target) -eq $entry.patched){Remove-Item -LiteralPath $target}
        }
    }
    throw
}finally{
    # Delete only this installer-owned, validated staging directory.
    if($stage.StartsWith($GameRoot+'\',[StringComparison]::OrdinalIgnoreCase) -and [IO.Path]::GetFileName($stage).StartsWith('.MaoChineseStaging-')){Remove-Item -LiteralPath $stage -Recurse -Force}
}
Write-Host ('Chinese patch installed. Original files are backed up in '+$backup)
Write-Host 'Start the game normally through Steam.'
