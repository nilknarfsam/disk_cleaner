# CONTEXT.md — Contexto do Produto e Negócio

## 🎯 Objetivo do Aplicativo

O **Limpador / Formatador de HD e SSD USB** foi criado para resolver um problema comum de técnicos, administradores de sistemas e usuários de TI: a necessidade de limpar e reformatar pendrives, HDs e SSDs externos conectados via USB de forma rápida, em lote e sem o risco de apagar por engano o HD principal (disco C:) do computador.

---

## 👥 Público-Alvo e Casos de Uso

1. **Técnicos de Manutenção e TI**:
   - Preparação ou limpeza de vários pendrives de boot (Ventoy, Rufus, ISOs).
   - Formatação rápida de HDs/SSDs externos recebidos de clientes.
2. **Usuários Domésticos / Avançados**:
   - Limpeza de partições ocultas ou corrompidas em pendrives velhos.
   - Restauração de espaço total não alocado em pendrives particionados.

---

## ⚡ Modos de Operação

1. **Limpar e Deixar Não Alocado (`clean_only`)**:
   - Executa `diskpart clean`.
   - Remove a tabela de partições, MBR/GPT e volumes, deixando o disco como "Espaço Não Alocado". Ideal para reusar em gravadores de ISO ou refazer partições do zero.
2. **Formatação Rápida (`format_quick`)**:
   - Executa `diskpart clean` + `convert gpt` + `create partition primary` + `format fs={NTFS|exFAT|FAT32} quick label="{rótulo}"` + `assign`.
   - Entrega o disco formatado, montado e pronto para uso com letra atribuída no Windows Explorer.

---

## 💡 Recursos de UX Integrados

- **Atualização Automática (3s)**: Discos conectados ou removidos aparecem instantaneamente na tela.
- **Checkboxes e Seleção Fácil**: Clique em qualquer lugar da linha da tabela ou aperte a tecla `Espaço` para marcar a caixa de seleção.
- **Espaço Usado e Livre**: Exibição detalhada de capacidade total, ocupada (`GB` e `%`) e livre.
- **Letras de Unidade**: Exibe quais letras (ex: `E:`, `F:`) pertencem àquele disco USB.
- **Modal de Confirmação**: Evita acionamentos acidentais exigindo revisão antes da execução.
