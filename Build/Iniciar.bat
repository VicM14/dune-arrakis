@echo off
echo Cerrando instancias anteriores...
taskkill /f /im "Dune.SimulationService.exe" 2>nul
taskkill /f /im "Dune.PersistenceService.exe" 2>nul
timeout /t 2 /nobreak > nul

set ASPNETCORE_ENVIRONMENT=Development

echo ============================================
echo   DUNE: ARRAKIS DOMINION DISTRIBUTED
echo ============================================
echo.

echo [1/3] Iniciando PersistenceService (puerto 5032)...
start "PersistenceService" /D "%~dp0PersistenceService" "PersistenceService\Dune.PersistenceService.exe" --urls "http://localhost:5032"
timeout /t 5 /nobreak > nul

echo [2/3] Iniciando SimulationService (puerto 5000)...
start "SimulationService" /D "%~dp0SimulationService" "SimulationService\Dune.SimulationService.exe" --urls "http://localhost:5000"
timeout /t 8 /nobreak > nul

echo [3/3] Iniciando juego...
start "Dune Game" "Game\Dune.Unity.exe"

echo.
echo Todos los componentes iniciados.
pause