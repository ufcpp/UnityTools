cd /d %~dp0
set ProjectDir=%1
set TargetDir=%2

PowerShell -File postbuild.ps1
