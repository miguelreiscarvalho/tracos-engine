# fix: stabilize writing and remove UI serialization stalls

## Resumo

A conversão aguardava somente 160 ms entre traços, enquanto histórico e reconhecimento serializavam o documento inteiro na interface, causando substituições prematuras e bloqueios. Este PR adia a apresentação até uma pausa estável, remove essas serializações do caminho de escrita e protege a folha de rolagem automática durante contato.

## Mudanças principais

- Espera adaptativa, tinta visível durante revisão e respostas diferidas sem repetir inferência.
- Snapshots leves de histórico/região e autosave assíncrono com coalescência/flush ao fechar.
- Bounds e geometria de tinta em cache; revisão de til/cedilha usa envelope da palavra.
- Sem foco/BringIntoView/flicks na folha; coordenadas fixadas no começo do contato e viewport protegido durante escrita.
- Harness de medição reproduzível e testes de pausa, fila, histórico e persistência.

## Motivação

O terceiro vídeo mostrou oscilação durante palavras como `ficção`, travamentos e deslocamentos da folha. Medição local no documento confirmou aproximadamente 119 ms para cada snapshot JSON do histórico e 253 ms para cada preparação JSON de reconhecimento, executados na thread da UI.

## Como validar

1. Executar `build.ps1 -Test` e `run.ps1` no PowerShell.
2. Escrever na folha com mouse/caneta; levantar e observar conversão automática.
3. Escrever `ficção` com vários traços, til e cedilha tardios. A tinta deve continuar durante a escrita e converter após pausa; não se garante transcrição correta em toda amostra.
4. Desfazer conversão e conferir tinta original; Refazer deve reaplicar texto.
5. Rolar pela barra, escrever perto do rodapé e observar se a posição se mantém. Testar continuação numa palavra antiga sem converter entre os acentos.
6. Salvar, fechar e abrir; conferir texto e tinta preservados no JSON.

## Testes

- Compilação .NET Framework desktop e harness: executada sem avisos.
- `build.ps1 -Test`: 52 verificações passaram; compilação sem avisos.
- Microbenchmark local: histórico p50 118,9978 → 0,0713 ms; preparação completa antiga 252,7780 ms → cópia de lista da região próxima da resolução do cronômetro. Não são métricas de pen→pixel ou CPU total.
- Pontuação anterior: 8/8 preservados. Quatro tentativas de `ficção` com todos os traços: 2/4 corretas; os dois erros restantes também ocorrem no motor nativo com entrada completa.
- Smoke com mouse após rolagem: tinta antes de conversão, offset 655,68 mantido; undo e fechamento com flush preservaram textos/106 regiões/364 traços.
- Caneta física, CER/WER em corpus humano, testes de longa duração e falha de energia: não executados.
- Gradle/Android não se aplicam; não existe projeto Gradle. SDK moderno `dotnet` fora do PATH não bloqueou o compilador Framework disponível.

## Riscos / pontos de atenção

- Dependência de Microsoft.Ink e modelo pt-BR instalado; motor proprietário/legado.
- Snapshots compartilham traços concluídos sob contrato de imutabilidade; integração futura não deve modificar seus pontos in-place.
- Pausas muito longas podem antecipar conversão; agrupamento e proximidade são heurísticos e dependem do dispositivo.
- Bloqueios WPF corrigem causas conhecidas de rolagem; ausência de saltos em caneta física ainda precisa de novo teste.
- Salvar/fechar agora coordena um worker; timeout/erro é reportado e fechamento é cancelado. Sem migração de formato JSON.
- Precisão lexical do motor continua limitada; não se forçam palavras conhecidas do vídeo.
- Uma página/camada; sem migração de banco. JSON possui versão 1 e backup.

## Screenshots ou gravação

Mudança visual: anexar captura da versão corrigida ao publicar o PR. Quadros extraídos do vídeo do usuário permanecem locais e não devem ser publicados automaticamente.

## Checklist

- [x] Branch criada com nome descritivo
- [x] Commits revisados
- [x] Sem arquivos temporários/build/secrets
- [x] Testes executados ou bloqueio explicado
- [x] Mudanças manuais validadas no smoke com mouse (caneta física pendente)
- [x] PR descreve riscos e próximos passos

## Publicação

Branch: `codex/fix-writing-stability-and-ui-stalls`. Base local: `codex/fix-punctuation-and-diacritics` (`c714e54`). Nenhum remote configurado; comparação feita com a base local. Esta descrição está preparada para revisão; PR remoto não foi criado, pois não existe destino configurado.
