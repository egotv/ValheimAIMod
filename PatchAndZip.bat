@echo off
setlocal

REM Set the working directory to the script's location
cd /d "%~dp0"

REM Define the base directory of Valheim
set "VALHEIM_BASE_DIR=C:\Program Files (x86)\Steam\steamapps\common\ValheimDev"

REM Define the source directories
set "LOADER_DLL=ValheimAIModLoader\bin\Debug\ValheimAIModLoader.dll"
set "PATCH_DLL=ValheimAIModLoader\bin\Debug\ValheimAIModLivePatch.dll"

REM Define the target directories
set "PLUGINS_DIR=%VALHEIM_BASE_DIR%\BepInEx\plugins"
set "SCRIPTS_DIR=%VALHEIM_BASE_DIR%\BepInEx\scripts"
set "PACKAGE_DIR=PackagedMod\plugins"

REM Copy the files
echo Copying %LOADER_DLL% to %PLUGINS_DIR%
xcopy /y "%LOADER_DLL%" "%PLUGINS_DIR%\"
xcopy /y "%LOADER_DLL%" "%PACKAGE_DIR%\"

echo Copying %PATCH_DLL% to %SCRIPTS_DIR%
xcopy /y "%PATCH_DLL%" "%SCRIPTS_DIR%\"
xcopy /y "%PATCH_DLL%" "%PACKAGE_DIR%\"

echo Files copied successfully.

REM Zip the contents of PackagedMod folder
echo Creating Thrall.zip...
powershell Compress-Archive -Path PackagedMod\* -DestinationPath Thrall.zip -Force

echo Thrall.zip created successfully.

endlocal
