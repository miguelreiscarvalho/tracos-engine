> Documento histórico de uma revisão anterior. Consulte a [arquitetura atual](ENGINE.md) e o [mapa da documentação](README.md) para o estado vigente.

# Resultados observados

Revisão atual: [vídeo 20-40-54, tipografia e revisão](ANALISE_VIDEO_204054.md), 60 verificações. Documentos anteriores abaixo são registros históricos de cada implementação, não garantias de cobertura atual de reconhecimento.

Atualizações: [posicionamento](CORRECAO_POSICIONAMENTO.md), [pontuação/repertório](PONTUACAO_E_COBERTURA.md) e [fluidez/estabilidade](FLUIDEZ_E_ESTABILIDADE.md). Os 19 testes e a política por linha descritos abaixo registram o baseline inicial, não a revisão atual. Resultados atuais e limites estão nos documentos de cada correção.

Execução local em 06/11/2026. Sem histórico de outras conversas. Não foram baixados pesos, instalados modelos ou treinadas redes.

## Ambiente

Windows; compilador `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`; Microsoft.Ink instalado. `dotnet` não estava no PATH. Não há Gradle ou projeto Android neste repositório: `testDebugUnitTest`/`assembleDebug` não se aplicam. Builds C# e testes locais foram executados em seu lugar.

`tools/probe.ps1` enumerou:

```
Reconhecedor de Manuscrito da Microsoft para Português (Brasil) | languages=1046
Microsoft Gesture Recognizer | languages=
```

## Experimento real, tinta sintética

Baseline bruto preservado em `experiments/baseline-2026-11-06.csv`. Fixtures construídas manualmente como polilinhas; sem amostragem de escritores. Execução sequencial A→B→C, sem aleatorização. Uma única execução por configuração: estes tempos são observações, não percentis ou estimativas de produção.

| Entrada | A: palavra | B: componente | C: prefixo de região |
|---|---|---|---|
| quatro formas sintéticas de `casa` | casa, 374,79 ms (frio) | Ca5a, 51,39 ms total | C → ca → cas → casa; 12,51–16,53 ms por chamada |
| haste e ponto tardio | i, 16,63 ms | i, 13,36 ms | L → i; 13,72 / 9,66 ms |
| laço contínuo sem transcrição rotulada | s., 14,46 ms | s., 14,51 ms | s., 14,85 ms |

Para o único alvo rotulado `casa`: A e C completos têm CER/WER 0; B tem CER 0,5 e WER 1 (sensível a maiúsculas). O denominador é uma palavra sintética; não representa precisão humana. O último laço não permite calcular CER/WER. A revisão L→i demonstra possibilidade de reprocessamento, não prova tratamento geral de acentos.

## Verificações automatizadas

`build.ps1 -Test`: **19 verificações passaram**, compilação sem avisos na revisão final:

- Tickets: aceita revisão atual; rejeita revisão antiga, sessão antiga, região ausente e edição manual.
- Histórico: restaura tinta e reaplica texto sem chamar motor.
- Persistência: Unicode/pressão; gravação atômica com backup.
- Geometria: ponto/acento tardio agrupado, lacuna entre palavras, originais inalterados.
- Métricas: CER normaliza Unicode NFC; WER por palavras.
- Concorrência: worker não bloqueia chamador, conclui duas tarefas e coalesce revisão intermediária; resultado atrasado rejeitado após edição/sessão nova.

São testes de comportamento com engine controlado para a concorrência; precisão do motor não é testada por respostas falsas. O experimento separado usa Microsoft Ink real.

## Verificação da interface

Aplicativo aberto e inspecionado por automação desktop. Um arraste vertical com **mouse** apareceu como tinta, depois foi substituído automaticamente por `|`; status registrou 176 ms no motor nessa chamada. Isso não é prova de reconhecimento da letra `l`. Desfazer restaurou o traço azul; Refazer restaurou o glifo. Duplo clique abriu correção; texto `ação — café` foi aplicado e apareceu na folha. Fechamento salvou recuperação. Reabertura mostrou `ação — café`; JSON recuperado continha dois traços e cinco pontos (inclui um toque da interação de duplo clique). Slider de zoom foi alterado para 1,5 e retornou a 1, com mudança visual observada. Captura local de revisão: `local/ui-smoke.png`, não versionada.

Tempo de desenho exibido foi cerca de 0,1 ms em uma folha quase vazia: mede apenas OnRender CPU, **não latência caneta→pixel**, latência de compositor ou p95. Precisão de coordenadas sob zoom/rolagem, inclinação e rejeição de palma exigem validação física. Nenhuma captura de escrita de outro aplicativo foi usada.

## Limitações e próximos passos

- Sem amostra humana rotulada: CER/WER, acentos, números/termos técnicos e precisão de espaços humanos **não medidos**.
- Sem teste com caneta física. Captura WPF guarda pressão e inclinação disponíveis; mouse usa pressão convencional 0,5. Timestamp é tempo de recepção do lote, não tempo de cada pacote no hardware.
- Sem inferência durante contato; uma longa palavra cursiva em um traço será reconhecida ao levantar. A/B/C estão no harness; não há alegação de escolha definitiva de melhor estratégia.
- Espaçamento final delegado ao motor; heurística geométrica só experimental. Agrupamento de linhas usa margens fixas e pode unir regiões próximas ou separar letras grandes.
- Nenhuma porcentagem de confiança. Hipóteses podem oscilar; originais sempre permanecem no arquivo.
- Gestos de riscar/circular/traço vertical e inserção pressionando: pesquisa de desambiguação documentada; implementação pendente. Seleção explícita funciona como alternativa no protótipo.
- Sem reflow, cursor por caractere, wrapping ou inserção dentro de texto convertido; correção edita a região inteira. Formatação global, sem estilos por fragmento.
- Histórico em memória usa snapshots completos e não atravessa sessões. Renderização redesenha documento inteiro; desempenho em documentos longos/10 minutos não medido. CPU/memória e percentis pendentes.
- Cancelamento lógico, sem interromper inferência COM em curso. Falha mantém tinta. Persistência atômica reduz risco, mas falha de energia forçada não foi ensaiada.
- Núcleo sem WPF, mas toolchain/adaptador atuais Windows. Migração para .NET moderno/WinRT deve comparar benefícios antes de substituir a integração que foi validada.

Próxima validação: corpus pt-BR consentido, replay idêntico nas políticas A/B/C, ordens contrabalanceadas, métricas das metas arquiteturais; depois gestos contextuais e correção por posição textual.
