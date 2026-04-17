@echo off
setlocal

cd /d "%~dp0"

echo ============================================
echo Iniciando Estoque API
echo ============================================

start "Estoque API" cmd /k "cd /d ""%~dp0services\estoque-api"" && dotnet restore && dotnet run"

exit /b