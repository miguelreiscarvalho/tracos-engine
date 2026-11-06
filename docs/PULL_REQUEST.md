# feat: add Windows incremental handwriting prototype

## Resumo

Adiciona uma superfície Windows que captura escrita e a substitui automaticamente por texto usando o reconhecedor local pt-BR. A escolha de reprocessar regiões revisáveis foi precedida por pesquisa e experimento A/B/C com traços sintéticos, sem afirmar precisão geral com escrita humana.

## Mudanças principais

- Documento com tinta original, texto, âncoras e formatação; histórico e JSON com backup.
- Captura WPF, worker STA coalescido e rejeição de resultados obsoletos.
- Motor Microsoft Ink substituível, fontes, zoom, seleção e correção textual.
- Pesquisa, arquitetura, baseline sintético e testes de integridade/concorrência.

## Motivação

O repositório estava vazio. O objetivo é validar escrita livre com conversão automática sem bloquear a captura e preservando a possibilidade de recuperar tinta e trocar o reconhecedor.

## Como validar

1. Executar `build.ps1 -Test` e `run.ps1` no PowerShell.
2. Escrever na folha com mouse/caneta; levantar e observar conversão automática.
3. Acrescentar ponto/acento e conferir revisão; testar frases portuguesas, cursiva e espaços.
4. Desfazer conversão e conferir tinta original; Refazer deve reaplicar texto.
5. Duplo clique para corrigir; testar fonte/tamanho e zoom.
6. Salvar, fechar e abrir; conferir texto e tinta preservados no JSON.

## Testes

- Compilação .NET Framework desktop e harness: executada sem avisos.
- `build.ps1 -Test`: 19 verificações passaram.
- Harness A/B/C: executado com Microsoft Ink real sobre três fixtures sintéticas; baseline e limitações documentados.
- Smoke desktop: captura com mouse, conversão automática, undo/redo, correção acentuada, recuperação e zoom visual verificados.
- Caneta física, CER/WER em corpus humano, testes de longa duração e falha de energia: não executados.
- Gradle/Android não se aplicam; não existe projeto Gradle. SDK moderno `dotnet` fora do PATH não bloqueou o compilador Framework disponível.

## Riscos / pontos de atenção

- Dependência de Microsoft.Ink e modelo pt-BR instalado; motor proprietário/legado.
- Hipóteses podem oscilar e agrupamento geométrico é heurístico.
- Espaços finais dependem do motor; gestos contextuais e cursor por caractere pendentes.
- Histórico usa memória proporcional a snapshots; desempenho em documentos longos não medido.
- Uma página/camada; sem migração de banco. JSON possui versão 1 e backup.

## Screenshots ou gravação

Mudança visual: anexar `local/ui-smoke.png` ao publicar o PR. Captura de revisão foi produzida localmente e permanece fora do Git.

## Checklist

- [x] Branch criada com nome descritivo
- [x] Commits revisados
- [x] Sem arquivos temporários/build/secrets
- [x] Testes executados ou bloqueio explicado
- [x] Mudanças manuais validadas no smoke com mouse (caneta física pendente)
- [x] PR descreve riscos e próximos passos

## Publicação

Branch: `codex/incremental-handwriting-prototype`. Repositório criado sem commits prévios, branch principal local sem histórico e nenhum remote configurado; não há `origin/main` para comparação. Esta descrição está preparada para revisão local. PR remoto não foi criado, pois não existe destino configurado.
