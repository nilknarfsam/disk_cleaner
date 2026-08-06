# AGENTS.md — Antigravity & AI Agents Guide

Este arquivo contém o contexto completo, histórico, regras de segurança e instruções técnicas para qualquer assistente de IA (Cursor, Antigravity, ChatGPT, Claude, Copilot) que for dar manutenção ou evoluir este projeto.

---

## 📌 Visão Geral do Projeto

- **Nome**: Limpador / Formatador de HD e SSD USB (`Limpador_USB`)
- **Propósito**: Utilitário desktop seguro para Windows que identifica discos conectados via USB (pendrives, HDs e SSDs externos) e permite formatar rapidamente ou limpar (deixar não alocado) com proteções estritas contra apagamento acidental de discos de sistema.
- **Tecnologia Atual**: **C# / .NET 9 WPF (Windows Presentation Foundation)**
- **Versão Atual**: `v2.0.0`
- **Repositório**: [https://github.com/nilknarfsam/disk_cleaner](https://github.com/nilknarfsam/disk_cleaner)

---

## 🏛️ Arquitetura e Estrutura de Arquivos

```text
c:\src\projects\usb_disk_cleaner_python\
├── Limpador_USB.csproj       # Projeto .NET 9.0-windows com UseWPF
├── app.manifest              # Manifesto do Windows com UAC (requireAdministrator)
├── App.xaml / App.xaml.cs    # Ponto de entrada WPF
├── MainWindow.xaml           # Interface principal com DataGrid, filtros e botões
├── MainWindow.xaml.cs        # Lógica de interface, bindings e loop de atualização (DispatcherTimer 3s)
├── Models/
│   └── UsbDisk.cs            # Modelo de dados dos discos (INotifyPropertyChanged)
├── Services/
│   ├── DiskService.cs        # Enumeração segura via WMI/PowerShell, cálculo de espaço usado/livre
│   └── DiskpartService.cs    # Construção dos comandos diskpart, execução assíncrona e salvar logs
├── Views/
│   ├── ConfirmWindow.xaml    # Janela modal de confirmação (Sim, Iniciar / Não, Cancelar)
│   └── ConfirmWindow.xaml.cs
├── build.bat                 # Script de compilação do executável standalone (.exe)
├── releases/                 # Binários versionados (ex: Limpador_USB-v2.0.0.exe)
├── OLD/                      # Protótipo antigo em Python / Tkinter (legado)
├── README.md                 # Documentação pública principal
├── CONTEXT.md                # Visão de contexto funcional do produto
├── MEMORY.md                 # Histórico de decisões e alterações do projeto
└── AGENTS.md                 # Este guia para IAs
```

---

## 🔒 Regras Críticas de Segurança (NÃO REMOVER)

Qualquer alteração ou nova funcionalidade DEVE respeitar estas proteções obrigatórias:

1. **Filtro Estrito USB**: Apenas unidades com `BusType == "USB"` devem ser listadas ou processadas.
2. **Bloqueio de Sistema/Boot**: Discos onde `IsBoot == True` ou `IsSystem == True` NUNCA devem ser exibidos ou aceitos para limpeza.
3. **Bloqueio de Somente Leitura e Offline**: Discos com `IsReadOnly == True` ou `IsOffline == True` são bloqueados.
4. **Revalidação Pré-Execução**: Imediatamente antes de disparar o `diskpart`, a aplicação consulta o estado dos discos novamente via `DiskService` para garantir que a unidade não mudou de estado.
5. **Limite do FAT32**: Partições FAT32 não aceitam discos maiores que 32 GB. O app avisa o usuário e impede a operação se FAT32 for selecionado para discos > 32 GB.
6. **Confirmação Modal Obrigatória**: Toda operação destrutiva exige confirmação pela janela modal `ConfirmWindow` antes de executar.
7. **Gravação de Logs em Erros**: Falhas do `diskpart` gravam o log completo em `%TEMP%\Limpador_USB_logs\`.

---

## 🛠️ Comandos de Build e Manutenção

- **Testar em Modo Desenvolvimento**:
  ```powershell
  dotnet run
  ```
- **Verificar Compilação**:
  ```powershell
  dotnet build -c Release
  ```
- **Gerar Executável Standalone (.exe)**:
  ```powershell
  .\build.bat
  ```
  *O binário compilado único (~10MB) será gerado em `dist\Limpador_USB.exe`.*

---

## 📝 Instruções para IAs ao Editar Este Código

- Sempre mantenha os métodos de consulta E/S em threads secundárias (`Task.Run` / `async/await`) para não congelar a interface WPF.
- Sempre que criar uma nova versão compilada, copie o `.exe` para a pasta `releases/` no formato `Limpador_USB-vX.Y.Z.exe`.
- Mantenha `MEMORY.md`, `CONTEXT.md` e `AGENTS.md` atualizados ao introduzir novas funcionalidades ou refatorações.
