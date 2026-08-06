# 🗺️ ROADMAP — Limpador / Formatador de HD e SSD USB

Plano estratégico de evolução contínua para a aplicação C# .NET 9 WPF, organizado por fases de prioridade, impacto e nível de risco.

---

## 📌 Fase 1: Curto Prazo (v2.2.0) — Usabilidade, Resiliência & Notificações
- [x] **🔔 Notificação Sonora de Conclusão**: Emitir um sinal sonoro leve do sistema (`SystemSounds.Asterisk`) ao concluir a formatação/limpeza dos discos.
- [x] **📋 Copiar Log com 1 Clique**: Opção na caixa de erro para copiar o caminho do log direto para a área de transferência.
- [x] **🔓 Desmontagem Preventiva de Volumes**: Desmontagem preventiva de volumes via PowerShell antes do `diskpart clean` para evitar erros de "Acesso Negado".
- [x] **💾 Persistência do Tema**: Persistência da preferência do tema (Modo Escuro / Modo Claro) em arquivo local de configuração.

---

## 📌 Fase 2: Médio Prazo (v2.3.0) — Diagnóstico de Hardware & Benchmark
- [ ] **⚡ Teste Rápido de Velocidade (USB Speed Test)**: Teste seguro de 5 segundos para medir taxas reais de Leitura e Escrita (MB/s) e detectar pendrives falsos/lentos.
- [ ] **🌐 Suporte Multilíngue (i18n)**: Suporte a alternador de idiomas (Português, Inglês, Espanhol).
- [ ] **🏷️ Lista de Rótulos Sugeridos**: Atributos rápidos de rótulos em ComboBox (ex.: `MIDIA_USB`, `BACKUP_EXT`, `INSTALADOR`).

---

## 📌 Fase 3: Longo Prazo (v3.0.0) — Sanitização Avançada & Diagnóstico SMART
- [ ] **🧹 Sanitização Completa (Zero Fill / DoD Wipe)**: Suporte ao modo `diskpart clean all` para sobrescrever todos os setores com zeros e impedir recuperação forense.
- [ ] **🩺 Telemetria SMART de Saúde**: Leitura de atributos WMI/CIM de temperatura, contagem de ciclos de escrita e setores realocados.

---

## 🛡️ Diretrizes Globais de Segurança (Invariáveis)
1. Nenhuma funcionalidade pode remover o **Filtro Estrito USB** ou permitir formatação de discos `IsBoot` / `IsSystem`.
2. Toda operação destrutiva DEVE passar obrigatoriamente pela revalidação de estado e pela confirmação modal.
