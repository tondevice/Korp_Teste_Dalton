@echo off
setlocal

cd /d "%~dp0"

echo ============================================
echo Resetando dados do projeto
echo ============================================

set "ESTOQUE_DB=%~dp0services\estoque-api\data\estoque.db"
set "FATURAMENTO_DB=%~dp0services\faturamento-api\data\faturamento.db"

if exist "%ESTOQUE_DB%" (
    del /f /q "%ESTOQUE_DB%"
    echo estoque.db removido com sucesso.
) else (
    echo estoque.db nao encontrado.
)

if exist "%FATURAMENTO_DB%" (
    del /f /q "%FATURAMENTO_DB%"
    echo faturamento.db removido com sucesso.
) else (
    echo faturamento.db nao encontrado.
)

echo.
echo Na proxima inicializacao, os bancos serao recriados automaticamente.
pause
exit /b