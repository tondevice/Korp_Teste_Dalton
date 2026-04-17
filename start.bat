@echo off
setlocal

cd /d "%~dp0"

echo ============================================
echo Iniciando Korp Teste Dalton
echo ============================================

start "Estoque API" cmd /k "cd /d ""%~dp0services\estoque-api"" && dotnet restore && dotnet run"
start "Faturamento API" cmd /k "cd /d ""%~dp0services\faturamento-api"" && dotnet restore && dotnet run"
start "Frontend" cmd /k "cd /d ""%~dp0frontend"" && npm install && npx ng serve --open"

exit /b