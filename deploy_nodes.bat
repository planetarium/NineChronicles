@echo off
setlocal

set "SRC_ROOT=D:\Game\9c"
set "SRC_GAME=%SRC_ROOT%\NineChronicles"
set "SRC_HEIMDALL_CLO=%SRC_ROOT%\clo_heimdall.json"
set "SRC_ODIN_CLO=%SRC_ROOT%\clo_odin.json"

set "DST_H1=D:\Game\Heimdall\1\NineChronicles"
set "DST_H2=D:\Game\Heimdall\2\NineChronicles"
set "DST_O1=D:\Game\Odin\1\NineChronicles"
set "DST_O2=D:\Game\Odin\2\NineChronicles"

echo ==========================================
echo [1/3] Copy NineChronicles to 4 destinations
echo ==========================================

echo Please make sure all NineChronicles.exe processes are closed.
echo.

if not exist "%SRC_GAME%\" (
    echo ERROR: Source folder not found: %SRC_GAME%
    exit /b 1
)

call :copy_game "%SRC_GAME%" "%DST_H1%"
call :copy_game "%SRC_GAME%" "%DST_H2%"
call :copy_game "%SRC_GAME%" "%DST_O1%"
call :copy_game "%SRC_GAME%" "%DST_O2%"

echo ==========================================
echo [2/3] Overwrite Heimdall clo.json
echo ==========================================

if not exist "%SRC_HEIMDALL_CLO%" (
    echo ERROR: Source file not found: %SRC_HEIMDALL_CLO%
    exit /b 1
)

copy /Y "%SRC_HEIMDALL_CLO%" "%DST_H1%\NineChronicles_Data\StreamingAssets\clo.json" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy Heimdall clo to %DST_H1%
    exit /b 1
)

copy /Y "%SRC_HEIMDALL_CLO%" "%DST_H2%\NineChronicles_Data\StreamingAssets\clo.json" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy Heimdall clo to %DST_H2%
    exit /b 1
)

echo ==========================================
echo [3/3] Overwrite Odin clo.json
echo ==========================================

if not exist "%SRC_ODIN_CLO%" (
    echo ERROR: Source file not found: %SRC_ODIN_CLO%
    exit /b 1
)

copy /Y "%SRC_ODIN_CLO%" "%DST_O1%\NineChronicles_Data\StreamingAssets\clo.json" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy Odin clo to %DST_O1%
    exit /b 1
)

copy /Y "%SRC_ODIN_CLO%" "%DST_O2%\NineChronicles_Data\StreamingAssets\clo.json" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy Odin clo to %DST_O2%
    exit /b 1
)

echo.
echo Done.
exit /b 0

:copy_game
set "SRC=%~1"
set "DST=%~2"
echo Copying to: %DST%
rem /MIR keeps each node folder identical to the freshly built source.
rem This removes stale files left by older builds.
robocopy "%SRC%" "%DST%" /MIR /R:1 /W:1 /COPY:DAT /DCOPY:DAT /XJ /NFL /NDL /NJH /NJS /NP >nul
set "RC=%ERRORLEVEL%"
if %RC% GEQ 8 (
    echo ERROR: robocopy failed with code %RC% for %DST%
    exit /b 1
)
if not exist "%DST%\NineChronicles.exe" (
    echo ERROR: Build executable missing after copy: %DST%\NineChronicles.exe
    exit /b 1
)
exit /b 0

