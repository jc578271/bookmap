@echo off
echo Building Rithmic Connection Monitor for Bookmap...
echo.

REM Check if gradle is available
gradle --version >nul 2>&1
if errorlevel 1 (
    echo Error: Gradle is not installed or not in PATH
    echo Please install Gradle from https://gradle.org/install/
    pause
    exit /b 1
)

REM Build the project
echo Running gradle build...
gradle jar

if errorlevel 1 (
    echo.
    echo Build failed! Check the error messages above.
    pause
    exit /b 1
)

echo.
echo Build successful!
echo JAR file created at: build\libs\bm-rithmic-connection-monitor.jar
echo.
echo To install in Bookmap:
echo 1. Copy the JAR file to a convenient location
echo 2. In Bookmap, go to Settings -> Api plugins configuration
echo 3. Click Add and select the JAR file
echo 4. Select "Rithmic Connection Monitor" from the list
echo 5. Enable the addon using the checkbox
echo.
pause 