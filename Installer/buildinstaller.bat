:: Build script for vocaluxe installer (x86 and x64 versions)
:: 
:: Requirements
:: - wget.exe: http://gnuwin32.sourceforge.net/packages/wget.htm
:: - unzip.exe: http://gnuwin32.sourceforge.net/packages/unzip.htm
:: - nuget.exe: https://www.nuget.org/downloads
:: - Visual Studio with devenv.exe (should be located in somewhere in Visual Studio folder, e.g. C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE)
:: - AdvancedInstaller

@ECHO OFF
echo Cloning vocaluxe from github and checkout master
if exist "Files" (
	cd Files
	git checkout -q develop >> log.txt
	git pull -q >> log.txt
	cd ../
) else (
	git clone -q https://github.com/Vocaluxe/Vocaluxe.git Files > log.txt
	cd Files
	git checkout -q develop >> log.txt
	cd ../
)
echo - Done
echo.

echo Download ambient videos and move them to themes folder
wget --append-output=log.txt -q -O videos.zip https://github.com/lukeIam/VocaluxeDependencies/blob/master/zips/themes/vocaluxe.themes.ambient.video.zip?raw=true || goto :error
unzip -q -o -d Files/Output videos.zip || goto :error
del /q videos.zip >> log.txt || goto :error
echo - Done
echo.

echo Delete lib-folder to prevent packing wrong dlls
del /q Files\Output\libs >> log.txt || goto :error
echo - Done
echo.

echo Restore dependencies and build x64
nuget restore Files/Vocaluxe.sln  >> log.txt || goto :error
devenv Files/Vocaluxe.sln /Build "InstallerWin|x64"  >> log.txt || goto :error
echo - Done
echo.

echo Build x64 installer
AdvancedInstaller.com /rebuild setup.aip -buildslist x64  >> log.txt || goto :error
echo - Done
echo.

echo Delete lib-folder to prevent packing wrong dlls
del /q Files\Output\libs >> log.txt || goto :error
echo - Done
echo.

echo Restore dependencies and build x86
nuget restore Files/Vocaluxe.sln  >> log.txt || goto :error
devenv Files/Vocaluxe.sln /Build "InstallerWin|x86"  >> log.txt || goto :error
echo - Done
echo.

echo Build x86 installer
AdvancedInstaller.com /rebuild setup.aip -buildslist x86  >> log.txt || goto :error
echo - Done
echo.

echo Everything is done!

pause
goto :EOF

:error
echo Failed with error #%errorlevel%.
echo See log.txt for more details.
pause
exit /b %errorlevel%