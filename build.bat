@echo off
title Build Limpador USB (.NET 9 WPF)
echo Compilando executavel C# standalone...

dotnet publish Limpador_USB.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist\

echo.
if exist dist\Limpador_USB.exe (
    echo Build concluido com sucesso!
    echo Executavel gerado em: dist\Limpador_USB.exe
) else (
    echo Ocorreu uma falha ao gerar o executavel.
)
pause
