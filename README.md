<!-- organizacao-github:inicio -->
> [!NOTE]
> **📌 Organização do GitHub (22/10/2026): ✅ MANTER (vitrine)**
>
> v2.1.0, completo. Fixar no perfil.
>
> _Nota temporária do Claude. Some quando a organização terminar._
<!-- organizacao-github:fim -->

# 💾 Limpador & Formatador de HD/SSD USB (.NET 9 WPF)

[![Build .NET](https://img.shields.io/badge/.NET-9.0--windows-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Plataforma](https://img.shields.io/badge/Plataforma-Windows%2010%20%7C%2011-0078D4?style=flat-square&logo=windows)](https://microsoft.com)
[![Versão](https://img.shields.io/badge/Versão-v2.1.0-emerald?style=flat-square)](https://github.com/nilknarfsam/disk_cleaner/releases)
[![Licença](https://img.shields.io/badge/Licença-MIT-blue?style=flat-square)](LICENSE)

Utilitário desktop de alta performance para **Windows 10 e 11** desenvolvido em **C# e .NET 9 WPF**. Projetado para identificar exclusivamente discos conectados via USB (pendrives, HDs e SSDs externos) e permitir a **limpeza total** ou **formatação rápida** em lote, com proteções estritas contra apagamento acidental de discos do sistema.

---

## 📸 Recursos em Destaque (v2.1.0)

- 🎨 **Interface WPF Nativa**: Suporte a alternador dinâmico de **Modo Claro / Modo Escuro (Dark Mode)**.
- 🔍 **Pesquisa Rápida em Tempo Real**: Filtro instantâneo por nome do dispositivo, modelo, número do disco ou letra atribuída.
- 📊 **Barra Gráfica de Capacidade**: Coluna visual de ocupação com indicador colorido de porcentagem.
- 🔌 / 💽 **Identificação de Dispositivos**: Diferenciação visual entre Pendrives USB (`🔌`) e HDs/SSDs Externos (`💽`).
- 🔤 **Letra de Unidade Customizada**: Opção de definir uma letra específica (ex: `E:`, `G:`, `Z:`) ao formatar.
- ⏏️ **Ejeção Segura de USB**: Desmontagem e ejeção do dispositivo com segurança direto pela interface.
- ⚡ **Detecção em Tempo Real (3s)**: Atualização automática via `async/await` sem travar a interface.
- 🛡️ **Elevação UAC Nativa**: Manifesto `app.manifest` integrado com suporte a privilégios de administrador.

---

## 🔒 Proteções Estritas de Segurança

| Proteção | Funcionamento |
| :--- | :--- |
| **Filtro Estrito USB** | Exibe e processa exclusivamente unidades identificadas como `BusType == USB`. |
| **Bloqueio de Sistema/Boot** | Impede a exibição e o apagamento de unidades marcadas como `IsBoot` ou `IsSystem`. |
| **Bloqueio Somente Leitura/Offline** | Bloqueia operações em mídias gravadas em modo somente leitura ou offline. |
| **Revalidação Pré-Execução** | Consulta o estado dos discos novamente via WMI imediatamente antes de disparar o `diskpart`. |
| **Confirmação Modal Obrigatória** | Exibe aviso destacado com resumo dos discos afetados e botões explícitos de **Sim, Iniciar** e **Não, Cancelar**. |
| **Logs de Diagnóstico** | Grava automaticamente logs detalhados em `%TEMP%\Limpador_USB_logs\` em caso de falhas. |

> [!CAUTION]
> **Aviso de Responsabilidade**: Mesmo com todas as travas de segurança ativas, esta ferramenta realiza **operações destrutivas irreversíveis**. Utilize apenas em mídias externas de teste ou quando tiver certeza absoluta dos dados.

---

## 🛠️ Requisitos e Compilação

### Requisitos de Sistema
- **Windows 10 ou 11 (64-bit)**
- **Privilégios de Administrador** (solicitado automaticamente ao abrir o app)
- **.NET 9 SDK** (apenas para compilação a partir do código-fonte)

### Compilando via Terminal

```powershell
# 1. Clone o repositório
git clone https://github.com/nilknarfsam/disk_cleaner.git
cd disk_cleaner

# 2. Teste em modo de desenvolvimento
dotnet run

# 3. Gere o executável standalone (.exe)
.\build.bat
```

O binário compilado único e independente será gerado em:
```text
dist\Limpador_USB.exe
```

---

## 📂 Estrutura da Arquitetura

```text
disk_cleaner/
├── Limpador_USB.csproj   # Configuração do projeto .NET 9 WPF
├── app.manifest          # Manifesto Windows para elevação de Administrador (UAC)
├── App.xaml / App.xaml.cs# Ponto de entrada do WPF
├── MainWindow.xaml       # View principal (XAML)
├── MainWindow.xaml.cs    # Lógica de interface, bindings e pesquisa
├── Models/
│   └── UsbDisk.cs        # Data Model com INotifyPropertyChanged
├── Services/
│   ├── DiskService.cs    # Enumeração WMI/PowerShell, espaço e ejeção segura
│   └── DiskpartService.cs# Execução e log dos scripts diskpart
├── Views/
│   ├── ConfirmWindow.xaml# Modal de confirmação Sim / Não
│   └── ConfirmWindow.xaml.cs
├── build.bat             # Script de compilação do executável standalone
├── AGENTS.md             # Instruções e diretrizes para assistentes de IA
├── CONTEXT.md            # Visão de contexto do produto e casos de uso
├── MEMORY.md             # Memória do histórico de versões e decisões
└── README.md             # Documentação principal
```

---

## 📄 Licença

Este projeto é disponibilizado sob a licença de código aberto. Sinta-se livre para utilizar, modificar e contribuir!
