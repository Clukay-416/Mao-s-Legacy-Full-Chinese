param([Parameter(Mandatory=$true)][string]$GameRoot,[string]$Python='python')
$ErrorActionPreference='Stop'
$GameRoot=[IO.Path]::GetFullPath($GameRoot)
$original=$GameRoot
if(Test-Path -LiteralPath (Join-Path $GameRoot '.MaoChineseBackup\China_Data\resources.assets')){$original=Join-Path $GameRoot '.MaoChineseBackup'}
$work=Join-Path $PSScriptRoot '.build'
$output=Join-Path $work 'rebuilt'
$deps=Join-Path $work 'deps'
New-Item -ItemType Directory -Path $output,$deps -Force | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not (Test-Path -LiteralPath $compiler)){throw 'The .NET Framework C# compiler was not found.'}
$cecil=Join-Path $deps 'Mono.Cecil.dll'
if(-not (Test-Path -LiteralPath $cecil)){
    [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
    $zip=Join-Path $deps 'cecil.zip'
    Invoke-WebRequest -Uri 'https://api.nuget.org/v3-flatcontainer/mono.cecil/0.11.6/mono.cecil.0.11.6.nupkg' -OutFile $zip -UseBasicParsing
    Expand-Archive -LiteralPath $zip -DestinationPath (Join-Path $deps 'cecil') -Force
    Copy-Item -LiteralPath (Join-Path $deps 'cecil\lib\net40\Mono.Cecil.dll') -Destination $cecil
}
& $Python (Join-Path $PSScriptRoot 'Source\generate_inputs.py') $original $output
if($LASTEXITCODE -ne 0){throw 'Resource build failed.'}
$managed=Join-Path $GameRoot 'China_Data\Managed'
$target=Join-Path $output 'China_Data\Managed'
New-Item -ItemType Directory -Path $target -Force | Out-Null
$refs=@('UnityEngine.CoreModule.dll','UnityEngine.TextRenderingModule.dll','UnityEngine.UI.dll','UnityEngine.UIModule.dll','UnityEngine.Physics2DModule.dll','UnityEngine.InputLegacyModule.dll','Unity.TextMeshPro.dll','UnityEngine.ScreenCaptureModule.dll','UnityEngine.ImageConversionModule.dll') | ForEach-Object {'/r:'+(Join-Path $managed $_)}
$argsList=@('/nologo','/target:library',('/out:'+(Join-Path $target 'MaoChinese.dll')),('/resource:'+(Join-Path $output 'dictionary.tsv')+',dictionary.tsv'))+$refs+@((Join-Path $PSScriptRoot 'Source\MaoChinese.cs'),(Join-Path $PSScriptRoot 'Source\TutorialArt.cs'))
& $compiler $argsList
if($LASTEXITCODE -ne 0){throw 'Display layer compilation failed.'}
& $compiler /nologo /target:exe ('/r:'+$cecil) ('/out:'+(Join-Path $deps 'Inject.exe')) (Join-Path $PSScriptRoot 'Source\Inject.cs')
if($LASTEXITCODE -ne 0){throw 'Injector compilation failed.'}
foreach($name in @('Assembly-CSharp.dll','NewResolutionDialog.dll')){
    & (Join-Path $deps 'Inject.exe') (Join-Path $original ('China_Data\Managed\'+$name)) (Join-Path $target 'MaoChinese.dll') (Join-Path $target $name)
    if($LASTEXITCODE -ne 0){throw ('Injection failed: '+$name)}
}
& $Python (Join-Path $PSScriptRoot 'Source\make_package.py') $original $output $PSScriptRoot
if($LASTEXITCODE -ne 0){throw 'Rebuilt package preparation failed.'}
Write-Host ('Rebuilt package: '+(Join-Path $work 'package'))
