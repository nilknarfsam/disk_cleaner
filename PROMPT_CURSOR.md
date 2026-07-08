Você é meu engenheiro Python sênior. Vamos transformar meu BAT de limpeza rápida de disco em um aplicativo Windows simples, seguro e funcional.

Contexto:
- O BAT original usa diskpart:
  1. list disk
  2. select disk N
  3. clean
- Quero um app em Python com interface simples.
- O app deve pedir administrador uma única vez ao abrir.
- Deve detectar automaticamente HD/SSD SATA conectado via case USB.
- Deve atualizar a lista automaticamente quando eu conectar ou remover um disco.
- Quero selecionar vários discos USB e executar a ação em lote.
- Deve bloquear/ocultar disco do sistema e mostrar apenas discos USB.
- Deve ter modo:
  1. Limpar e deixar Não Alocado: diskpart clean
  2. Formatação rápida: clean + convert gpt + create partition primary + format quick + assign
- Interface em Tkinter, sem dependências externas.
- Rodar em Windows.

Regras de segurança:
- Nunca executar em disco que seja Boot ou System.
- Mostrar número do disco, nome, tamanho, status, saúde e se é somente leitura.
- Antes de apagar, exigir confirmação digitando APAGAR.
- Toda operação deve ser feita em thread para a interface não travar.
- Gerar logs de saída do diskpart em caso de erro.
- Código organizado, comentado e pronto para virar EXE com PyInstaller.

Tarefa:
Revise, melhore e finalize o arquivo main.py desta pasta. Preserve a simplicidade e a segurança.