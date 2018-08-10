@ECHO OFF
REM This script will Compile the APK, Deploy the app, unlock your device, launch the unit test app,
REM  and retrive the test report upon completion

REM Configuration:
SET ADB_PATH="C:\Program Files (x86)\Android\android-sdk\platform-tools\adb"
SET APK_ID=com.mstestx.TestAppRunner
SET ACTIVITY_NAME=TestAppRunner.RunTestsActivity
SET PROJECT_TO_BUILD=..\src\TestAppRunner\TestAppRunner.Android\TestAppRunner.Android.csproj
SET APK_PATH=..\src\TestAppRunner\TestAppRunner.Android\bin\Debug\%APK_ID%-Signed.apk
SET DEVICE_PIN=1234


REM LOCATE VS2017
SET VSPATH=
IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise" (
   SET VSPATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\VsDevCmd.bat"
) ELSE IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional" (
   SET VSPATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\Common7\Tools\VsDevCmd.bat"
) ELSE IF NOT DEFINED VS150COMNTOOLS (
   ECHO ERROR: VISUAL STUDIO 2017 NOT FOUND
   GOTO Quit
)
IF NOT %VSPATH%=="" (
  IF NOT DEFINED VS150COMNTOOLS (
    CALL %VSPATH%
  )
)

REM Compile Android App:
MSBUILD /restore /t:PackageForAndroid /t:signandroidpackage %PROJECT_TO_BUILD% /p:Configuraton=Debug

if NOT ["%errorlevel%"]==["0"] ( exit /b %errorlevel% )

ECHO Installing app...
%ADB_PATH% install -r %APK_PATH%
if NOT ["%errorlevel%"]==["0"] ( exit /b %errorlevel% )

ECHO Unlocking the device
REM Pressing the lock button
%ADB_PATH% shell input keyevent 26
REM Swipe UP
%ADB_PATH% shell input touchscreen swipe 930 880 930 380
REM Entering your passcode
%ADB_PATH% shell input text %DEVICE_PIN%
REM Pressing Enter
%ADB_PATH% shell input keyevent 66
echo "Device is unlocked...."

ECHO Launching app...
%ADB_PATH% shell am start -n %APK_ID%/%ACTIVITY_NAME% --ez AutoRun true --es ReportFile TestRunReport
@timeout /t 2 > NUL

REM Wait for app to complete. We just keep checking the Process ID until it no longer returns a value
ECHO Waiting for app to complete...
%ADB_PATH% shell pidof %APK_ID% > PID.txt
set size=0
call :filesize "PID.txt"

SET /p PID=<PID.txt
:while1
    if %size% GTR 0 (
        ECHO .
	    @timeout /t 2 > NUL
        %ADB_PATH% shell pidof %APK_ID% > PID.txt
        call :filesize "PID.txt"
        goto :while1
    )
DEL PID.txt

REM Copy the test reports back to the host OS
ECHO Retriving TestReport
%ADB_PATH% exec-out run-as %APK_ID% cat /data/data/%APK_ID%/files/TestRunReport.trx > TestRunReport.trx
%ADB_PATH% exec-out run-as %APK_ID% cat /data/data/%APK_ID%/files/TestRunReport.log > TestRunReport.log

GOTO :eof

:: set filesize of 1st argument in %size% variable, and return
:filesize
  set size=%~z1
  exit /b 0
  
:eof