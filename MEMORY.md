# MEMORY.md — Memória e Linha do Tempo do Projeto

## 📜 Histórico de Decisões e Versões

### Versão 1.0.0 (Legado - Python / Tkinter)
- **Tecnologia**: Python 3.11 + Tkinter / `ttk.Treeview` + `diskpart` via `subprocess`.
- **Compilação**: PyInstaller via `build_exe.bat`.
- **Limitações**:
  - Exibia apenas tamanho total (sem espaço usado/livre).
  - Seleção exibia texto `"Sim"` / `"Não"` e exigia clicar exatamente na palavra da primeira coluna.
  - Exigia digitar a palavra `"APAGAR"` em uma caixa de texto simples.
  - Reabria o processo via `ShellExecuteW("runas")` para conseguir elevação UAC.

---

### Versão 1.1.0 (Melhorias de UI no Python)
- **Melhorias**:
  - Adicionado cálculo de espaço usado (`GB` e `%`), espaço livre e letras de unidade via consulta estendida do PowerShell.
  - Adicionados marcadores visuais `☑ Sim` e `☐ Não` na tabela com seleção ao clicar em qualquer célula da linha.
  - Substituída a confirmação digitada por uma janela modal com resumo dos discos e botões **Sim, Iniciar Processo** e **Não, Cancelar**.
  - Adicionada `Progressbar` visual durante a formatação.

---

### Versão 2.0.0 (Migração para C# / .NET 9 WPF)
- **Motivação**: O usuário solicitou migrar para uma tecnologia mais estável, rápida e nativa do Windows.
- **Decisão**: Migração para **C# .NET 9 WPF (Windows Presentation Foundation)**.
- **Ganhos**:
  - **UAC Nativo**: Criado `app.manifest` com `<requestedExecutionLevel level="requireAdministrator" />`. O Windows solicita elevação nativa ao abrir o `.exe`.
  - **Interface WPF**: Tabela `DataGrid` estilizada com DataBinding nativo, checkboxes de sistema, seleção visual com destaque e suporte a temas.
  - **Executável Standalone**: `.exe` único de ~10 MB gerado via `dotnet publish` em `dist/Limpador_USB.exe` e salvo em `releases/Limpador_USB-v2.0.0.exe`.
  - **Estrutura Organizada**: Arquivos Python legados movidos para a pasta `OLD/`.

---

### Versão 2.1.0 (Pacote de Melhorias de UI & Disco)
- **Data**: Agosto 2026
- **Novos Recursos**:
  - **Modo Escuro (Dark Mode)**: Suporte a alternador de tema claro/escuro (`BtnTheme`).
  - **Pesquisa Rápida**: Campo `TxtSearch` para filtrar discos em tempo real por nome, disco ou letra.
  - **Barra Gráfica de Ocupação**: Exibição visual do nível de uso do disco com `ProgressBar` estilizada no DataGrid.
  - **Ícones de Tipo de Dispositivo**: Diferenciação entre Pendrives (`🔌`) e HDs/SSDs Externos (`💽`).
  - **Atribuição de Letra Específica**: ComboBox `ComboDriveLetter` com escolha de letras livres (`D:` a `Z:`) ou `Automática`.
  - **Ejeção Segura de USB**: Botão `BtnEject` para ejetar/desmontar discos USB com segurança via Shell API.

---

### Versão 2.2.0 (Fase 1 Concluída - Resiliência & Notificações)
- **Data**: Agosto 2026
- **Novos Recursos**:
  - **Notificação Sonora**: Disparo sonoro nativo (`SystemSounds.Asterisk`) ao finalizar formatação.
  - **Desmontagem Preventiva (`ForceDismountVolumesAsync`)**: Liberação de volumes abertos antes do `diskpart clean`, evitando erros de acesso negado.
  - **Copiar Log de Erros**: Opção com 1 clique para copiar o caminho do log de falhas para a área de transferência.
  - **Persistência de Tema**: Salva a preferência de Modo Escuro / Claro entre reinicializações do app.

---

## 🛠️ Estado Atual do Repositório

- **Status**: Compilando sem avisos ou erros (`v2.2.0`).
- **Ramo Principal**: `main` no GitHub ([https://github.com/nilknarfsam/disk_cleaner](https://github.com/nilknarfsam/disk_cleaner)).
- **Binários**: Executável standalone compilado em `dist/Limpador_USB.exe` e versionado em `releases/Limpador_USB-v2.2.0.exe`.


