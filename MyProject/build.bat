@echo off
echo Building Telegram Signal Bot for cTrader
echo ======================================

echo.
echo Building Telegram Bot Service...
dotnet restore
dotnet build --configuration Release

echo.
echo Building cTrader Bot...
cd CTraderBot
dotnet restore
dotnet build --configuration Release

echo.
echo Build completed!
echo.
echo Files created:
echo - Telegram bot: bin\Release\net6.0\TelegramSignalBot.exe
echo - cTrader bot: CTraderBot\bin\Release\net6.0\TelegramSignalBot.dll
echo.
echo To run the Telegram bot:
echo   cd bin\Release\net6.0
echo   TelegramSignalBot.exe
echo.
echo To deploy to cTrader:
echo   1. Copy CTraderBot\bin\Release\net6.0\TelegramSignalBot.dll to your cTrader robots folder
echo   2. Add the bot to a chart in cTrader
echo   3. Configure the parameters
echo.
pause 