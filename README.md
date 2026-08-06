# Disk Cleaner — Limpador / Formatador de HD e SSD USB (.NET 9 WPF)

Ferramenta desktop nativa para **Windows** desenvolvida em **C# e .NET 9 (WPF)** que detecta discos conectados via USB e permite **limpar** ou **formatar rapidamente** HDs e SSDs externos com alta performance e proteções de segurança.

Substitui scripts BAT manuais com `diskpart` por um aplicativo de interface gráfica nativa em um `.exe` standalone de inicialização instantânea.

### Download (sem compilar)

Baixe o executável pronto na página de [Releases](https://github.com/nilknarfsam/disk_cleaner/releases/latest):

**[Limpador_USB.exe](https://github.com/nilknarfsam/disk_cleaner/releases/download/v2.0.0/Limpador_USB.exe)** — v2.0.0 (Windows 10 / 11)

---

## Funcionalidades

- **Interface Nativa Windows (WPF)**: Visual moderno do Windows 11 com tabela `DataGrid`, checkboxes nativas, ordenação e destaque visual.
- **Detecção automática** de discos com `BusType = USB` (pendrives, HDs e SSDs em case USB).
- **Cálculo de espaço de disco**: Exibe Tamanho Total, Espaço Usado (`GB` e `%`), Espaço Livre e Letra da unidade (ex.: `E:`, `F:`).
- **Caixas de seleção visíveis (Checkboxes)**: Seleção fácil clicando em qualquer parte da linha ou pela tecla `Espaço`.
- **Atualização em tempo real** da lista a cada 3 segundos ao conectar ou remover dispositivos sem travar a interface (`async/await`).
- **Seleção múltipla** para processar vários discos em lote.
- **Dois modos de operação:**
  - **Limpar e deixar Não Alocado** — equivalente a `diskpart clean`
  - **Formatação rápida** — `clean` + `convert gpt` + partição primária + `format quick` + `assign`
- **Escolha de sistema de arquivos:** NTFS, exFAT ou FAT32 (com aviso para discos acima de 32 GB).
- **Rótulo personalizado** do volume na formatação.
- **Barra de progresso e status visual** durante o processamento do `diskpart`.
- **Elevação UAC Nativa**: Manifesto `app.manifest` integrado com `requireAdministrator`.

---

## Proteções de segurança

| Proteção | Descrição |
|----------|-----------|
| Filtro USB | Exibe apenas discos identificados como USB pelo Windows |
| Bloqueio boot/sistema | Ignora discos marcados como `IsBoot` ou `IsSystem` |
| Revalidação antes da execução | Confere novamente o estado do disco imediatamente antes do `diskpart` |
| Bloqueio somente leitura / offline | Impede operação em discos não graváveis ou offline |
| Diálogo modal de confirmação | Exibe aviso destacado com resumo dos discos e botões diretos de **Sim, Iniciar Processo** e **Não, Cancelar** |
| Log de erros | Salva saída completa do `diskpart` em `%TEMP%\Limpador_USB_logs\` em caso de falha |

> **Aviso:** mesmo com todas as proteções, esta ferramenta **apaga dados de forma permanente e irreversível**. Use somente em discos externos de teste ou quando tiver certeza absoluta do que está fazendo.

---

## Requisitos

- **Windows 10 ou 11 (64-bit)**
- **.NET 9 SDK** (necessário apenas para compilar a partir do código-fonte)
- O executável distribuído na pasta `dist/` é **Self-Contained** (roda direto no Windows sem precisar de instalação prévia do .NET ou Python).

---

## Como compilar o projeto

```powershell
# Clone o repositório
git clone https://github.com/nilknarfsam/disk_cleaner.git
cd disk_cleaner

# Compile e rode em modo de desenvolvimento
dotnet run

# Ou gere o executável standalone (.exe)
.\build.bat
```

O executável final compilado será gerado em:

```text
dist\Limpador_USB.exe
```

---

## Como usar o aplicativo

1. Execute `Limpador_USB.exe` — aceite a elevação de administrador nativa do Windows (UAC).
2. Aguarde a listagem dos discos USB detectados.
3. Clique nas linhas desejadas para marcar as caixas de seleção (ou use *Selecionar todos*).
4. Escolha o modo: **Limpar** ou **Formatação rápida**.
5. Se for formatar, defina o sistema de arquivos e o rótulo.
6. Clique em **Executar nos discos selecionados**.
7. Na janela modal de confirmação, revise os discos e clique em **Sim, Iniciar Processo**.

---

## Estrutura do projeto

```text
disk_cleaner/
├── Limpador_USB.csproj   # Arquivo de projeto .NET 9 WPF
├── app.manifest          # Manifesto Windows para elevação de Administrador (UAC)
├── App.xaml              # Estilos e configuração da aplicação WPF
├── App.xaml.cs
├── MainWindow.xaml       # Interface gráfica principal (XAML)
├── MainWindow.xaml.cs    # Lógica de controle e eventos da interface
├── Models/
│   └── UsbDisk.cs        # Modelo de dados dos discos USB
├── Services/
│   ├── DiskService.cs    # Enumeração e cálculo de espaço via WMI/PowerShell
│   └── DiskpartService.cs# Execução e log do diskpart
├── Views/
│   ├── ConfirmWindow.xaml   # Janela modal de confirmação Sim / Não
│   └── ConfirmWindow.xaml.cs
├── build.bat             # Script para compilação do executável standalone
└── README.md
```

---

## Tecnologias

- **C# / .NET 9** — linguagem e runtime de alta performance
- **WPF (Windows Presentation Foundation)** — interface gráfica nativa Windows
- **PowerShell / WMI / CIM** — enumeração segura de discos e partições
- **`diskpart`** — limpeza e formatação no nível do disco
- **Native Single-File Publish** — empacotamento em um único `.exe` nativo

---

## Limitações conhecidas

- Funciona **apenas no Windows** (depende de `diskpart` e cmdlets de disco do Windows).
- Discos que não aparecem como `BusType USB` (ex.: alguns adaptadores específicos) não serão listados.
- FAT32 suporta no máximo **32 GB** por partição — o app avisa antes de tentar formatar discos maiores.
- A formatação rápida sempre converte o disco para **GPT** antes de criar a partição.

---

## Contribuindo

Sugestões, issues e pull requests são bem-vindos no repositório:

**https://github.com/nilknarfsam/disk_cleaner**

---

## Licença

Este projeto é disponibilizado como software utilitário de código aberto. Use por sua conta e risco.
