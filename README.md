# Disk Cleaner — Limpador / Formatador de HD e SSD USB

Ferramenta desktop para **Windows** que detecta discos conectados via USB e permite **limpar** ou **formatar rapidamente** HDs e SSDs externos — com interface gráfica simples e proteções de segurança.

Substitui scripts BAT manuais com `diskpart` por um aplicativo Tkinter pronto para uso ou distribuição como `.exe`.

### Download (sem compilar)

Baixe o executável pronto na página de [Releases](https://github.com/nilknarfsam/disk_cleaner/releases/latest):

**[Limpador_USB.exe](https://github.com/nilknarfsam/disk_cleaner/releases/download/v1.0.0/Limpador_USB.exe)** — v1.0.0

---

## Funcionalidades

- **Detecção automática** de discos com `BusType = USB` (pendrives, HDs e SSDs em case USB)
- **Cálculo de espaço de disco**: Exibe Tamanho Total, Espaço Usado (`GB` e `%`), Espaço Livre e Letra da unidade (ex.: `E:`)
- **Caixas de seleção visíveis (Checkboxes)**: Marcadores `☑` / `☐` com seleção fácil clicando em qualquer parte da linha ou pela tecla `Espaço`
- **Atualização em tempo real** da lista a cada 3 segundos ao conectar ou remover dispositivos
- **Seleção múltipla** para processar vários discos em lote
- **Dois modos de operação:**
  - **Limpar e deixar Não Alocado** — equivalente a `diskpart clean`
  - **Formatação rápida** — `clean` + `convert gpt` + partição primária + `format quick` + `assign`
- **Escolha de sistema de arquivos:** NTFS, exFAT ou FAT32 (com aviso para discos acima de 32 GB)
- **Rótulo personalizado** do volume na formatação
- **Barra de progresso e status visual** durante o processamento do `diskpart`
- **Elevação de administrador** solicitada uma única vez ao abrir o app
- **Interface responsiva** — operações pesadas rodam em thread separada

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

- **Windows 10 ou 11**
- **Python 3.11+** (apenas para executar a partir do código-fonte ou gerar o `.exe`)
- **Privilégios de administrador** (necessário para `diskpart` e `Get-Disk`)
- Nenhuma dependência externa para rodar o app — usa apenas biblioteca padrão do Python

---

## Uso rápido (código-fonte)

```powershell
# Clone o repositório
git clone https://github.com/nilknarfsam/disk_cleaner.git
cd disk_cleaner

# Execute (o Windows solicitará permissão de administrador)
py main.py
```

---

## Gerar executável (.exe)

O projeto inclui um script de build. No Windows:

```powershell
.\build_exe.bat
```

Ou manualmente:

```powershell
py -m pip install pyinstaller
py -m PyInstaller --noconsole --onefile --name Limpador_USB main.py
```

O executável será gerado em:

```text
dist\Limpador_USB.exe
```

---

## Como usar o aplicativo

1. Execute `main.py` ou `Limpador_USB.exe` — aceite a elevação de administrador.
2. Aguarde a listagem dos discos USB detectados.
3. Clique na coluna **Selecionado** para marcar os discos desejados (ou use *Selecionar todos*).
4. Escolha o modo: **Limpar** ou **Formatação rápida**.
5. Se for formatar, defina o sistema de arquivos e o rótulo.
6. Clique em **Executar nos discos selecionados**.
7. Digite **`APAGAR`** na caixa de confirmação.

---

## Estrutura do projeto

```text
disk_cleaner/
├── main.py            # Aplicativo principal (Tkinter + diskpart)
├── build_exe.bat      # Script para gerar o executável com PyInstaller
├── PROMPT_CURSOR.md   # Prompt de desenvolvimento usado no Cursor
└── README.md
```

---

## Tecnologias

- **Python 3** — lógica e interface
- **Tkinter / ttk** — interface gráfica nativa
- **PowerShell `Get-Disk`** — enumeração segura de discos
- **`diskpart`** — limpeza e formatação no nível do disco
- **PyInstaller** — empacotamento como executável standalone (opcional)

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
