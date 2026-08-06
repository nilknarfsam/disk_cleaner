@echo off
title Build Limpador USB
echo Instalando PyInstaller, se necessario...
py -m pip install pyinstaller

echo.
echo Gerando executavel...
py -m PyInstaller --noconsole --onefile --name Limpador_USB main.py

echo.
echo Pronto. Veja a pasta dist\Limpador_USB.exe
pause