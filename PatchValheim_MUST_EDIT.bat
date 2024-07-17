@echo off
setlocal

REM Define the base directory of Valheim
set "VALHEIM_BASE_DIR=H:\SteamLibrary\steamapps\common\Valheim"

REM Define the source directories
set "LOADER_DLL=ValheimAIModLoader\bin\Debug\ValheimAIModLoader.dll"
set "PATCH_DLL=ValheimAIModLoader\bin\Debug\ValheimAIModLivePatch.dll"

REM Define the target directories
set "PLUGINS_DIR=%VALHEIM_BASE_DIR%\BepInEx\plugins"
set "SCRIPTS_DIR=%VALHEIM_BASE_DIR%\BepInEx\scripts"

REM Copy the files
echo Copying %LOADER_DLL% to %PLUGINS_DIR%
xcopy /y "%LOADER_DLL%" "%PLUGINS_DIR%\"

echo Copying %PATCH_DLL% to %SCRIPTS_DIR%
xcopy /y "%PATCH_DLL%" "%SCRIPTS_DIR%\"

echo Files copied successfully.

endlocal
pause
