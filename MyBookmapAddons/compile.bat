@echo off
echo Compiling Rithmic Connection Monitor for Bookmap...
echo.

REM Create build directory
if not exist "build\libs" mkdir "build\libs"

REM Check if Java is available
java -version >nul 2>&1
if errorlevel 1 (
    echo Error: Java is not installed or not in PATH
    echo Please install Java 17 or later
    pause
    exit /b 1
)

REM Check if javac is available
javac -version >nul 2>&1
if errorlevel 1 (
    echo Error: Java compiler (javac) is not available
    echo Please install Java Development Kit (JDK) 17 or later
    pause
    exit /b 1
)

echo Java version:
java -version
echo.

echo Note: This is a manual compilation script.
echo For proper building with dependencies, please install Gradle and use 'gradle jar'
echo.
echo Manual compilation requires:
echo 1. Bookmap L1 API JAR files in the classpath
echo 2. Apache HttpClient and Gson libraries
echo 3. Proper classpath setup
echo.
echo It's recommended to use Gradle for building this project.
echo.

pause 