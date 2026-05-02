@echo off
echo Pulizia cartelle bin e obj...
for /d /r . %%d in (bin,obj) do (
    if exist "%%d" (
        echo   Elimino: %%d
        rd /s /q "%%d"
    )
)
echo.
echo Fatto! Ora in Visual Studio: Compila → Ricompila soluzione
pause
