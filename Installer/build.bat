REM compile x64
call "%VS110COMNTOOLS%\..\..\VC\vcvarsall.bat" x64
devenv.com ..\Vocaluxe.sln /Rebuild "InstallerWin|x64"
 
REM remove old x64 executables if found and rename
if exist "..\Output\Vocaluxe_x64.exe" del "..\Output\Vocaluxe_x64.exe"
ren "..\Output\Vocaluxe.exe" "Vocaluxe_x64.exe"
 
REM compile x86
call "%VS110COMNTOOLS%\..\..\VC\vcvarsall.bat" x86
devenv.com ..\Vocaluxe.sln /Rebuild "InstallerWin|x86"
 
REM compile installer
AdvancedInstaller.com /rebuild setup.aip