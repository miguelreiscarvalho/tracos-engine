> Documento histórico de uma revisão anterior. Consulte a [arquitetura atual](ENGINE.md) e o [mapa da documentação](README.md) para o estado vigente.

# Fluidez e estabilidade da escrita — 06/11/2026

## Diagnóstico

Vídeo: `2026-11-06 20-16-10.mp4`, 135,08 s, 1920 × 1080. Quadros locais mostram conversões entre traços de `ficção` e mudanças bruscas de rolagem perto do final. O JSON contém traços cuja coordenada vertical abrange centenas de DIPs em um contato, compatível com a superfície se deslocando durante captura. Não é possível provar pelo vídeo qual gesto/driver iniciou cada deslocamento.

Problemas confirmados no código:

- Reconhecimento elegível com apenas 160 ms após cada traço.
- `History.Record` serializava todos os pontos do documento antes de cada alteração.
- Preparação de cada chamada fazia encode+decode do documento inteiro na UI.
- Autosave serializava e gravava sincronamente na UI.
- Bounds percorriam e alocavam todos os pontos repetidamente; tinta também reconstruía geometria em cada frame.
- Resposta concluída durante escrita voltava à fila em vez de aguardar apresentação.
- A folha pedia foco ao iniciar cada contato e não desativava gestos nativos de caneta.

## Alterações

1. `RecognitionTiming` usa ritmo recente, contato ativo, proximidade da caneta e encerramento espacial do bloco. Base 1–2,2 s (inicial 1,2 s); proximidade adiciona 60%; bloco encerrado do qual o usuário já saiu pode usar 60% da base, com mínimo de 400 ms. Voltar a uma palavra antiga para acrescentar acentos restaura a espera de escrita ativa. É uma política de pausa/estado, não garantia de que toda pausa encerra uma palavra.
2. O scheduler suspende início de novas tarefas durante contato. Uma inferência nativa que já começou pode terminar; o resultado só é aplicado após nova validação e pausa. Resposta válida é guardada, sem repetir inferência da mesma revisão.
3. Uma região em revisão volta a mostrar seus traços completos até receber a próxima conversão estável. Textos manuais permanecem protegidos.
4. Snapshots do histórico possuem metadados e listas próprios, compartilhando traços concluídos. O editor nunca modifica pontos de traços já inseridos. Editores externos precisam criar novas versões, não mutar esses pontos. O modelo DTO público ainda não impõe essa imutabilidade pelo sistema de tipos.
5. Reconhecimento recebe somente `new List<InkTrace>(region.Traces)`. Salvamento usa snapshot rápido e worker serial com coalescência; temporário/flush/replace continuam atômicos. Fechamento espera gravação/exportação, com timeout reportado. Tinta pendente pode retomar após abrir; conversões completas existentes não são recalculadas.
6. Cache de bounds por traço concluído, união por região e cache de geometria da tinta; sem recriar todos os pontos a cada renderização. Geometria tipográfica continua em cache.
7. Folha sem foco automático, com RequestBringIntoView bloqueado e flick/press-and-hold/feedback desativados apenas nela. Panning desativado no ScrollViewer; rolagem durante contato é bloqueada/restaurada. Transformação janela→documento é capturada no pen-down e mantida até pen-up. Rolagem normal por barra permanece disponível.
8. Altura de revisão usa envelope local da palavra: muitos traços curtos não reduzem a margem a ponto de excluir til/cedilha posteriores.

## Medições reproduzíveis

Comando: `Tracos.Tests.exe --profile local/before-stability-fix.json`. Documento local: 4.552.996 bytes, 106 regiões, 364 traços, 39.214 pontos. Mesma execução C#, comparação do caminho antigo reconstituído com as operações novas. Uma chamada de aquecimento por operação; 12 repetições para caminhos JSON, 24 para operações novas. Ordem fixa, sem amostragem prolongada; percentis abaixo são desse microbenchmark.

| Operação | p50 (ms) | p95 (ms) |
|---|---:|---:|
| Histórico antigo: encode JSON completo | 118,9978 | 125,8897 |
| Preparação antiga: encode+decode completo | 252,7780 | 311,1624 |
| Histórico novo: snapshot de metadados/listas | 0,0713 | 0,2210 |
| Preparação nova: lista da região | 0,0001 | 0,0001 |
| Captura de snapshot para autosave | 0,1325 | 0,3264 |
| Consulta de bounds de todo o documento, cache aquecido | 0,0212 | 0,0368 |

O valor de 0,0001 ms está próximo da resolução de medição e não deve ser tratado como previsão de latência real. Essas medidas não são pen→pixel, uso de CPU total, fluidez p95 do compositor ou tempo total de reconhecimento. O encode JSON continua existindo para salvar, mas sai do caminho de captura/UI.

Observação adicional: processo anterior estava com aproximadamente 1,5 GB de memória privada após o uso. Processo recém-aberto com a correção estava em aproximadamente 204 MB. São sessões/históricos diferentes; não é benchmark de memória normalizado e parte da diferença vem de reiniciar o processo. A retirada de snapshots JSON completos elimina a fonte conhecida de duplicação a cada traço.

## Precisão de `ficção`

Quatro tentativas do vídeo foram avaliadas com corpo, til e cedilha reunidos na cópia local. Motor nativo/pipeline completo: `ficção`, `fiação`, `fecção`, `ficção` (2/4 exatos). O agrupamento corrigido preservou os diacríticos; não se pode atribuir os dois erros restantes somente à velocidade. O motor ainda confunde escrita mesmo com todos os traços. Esse conjunto foi usado no diagnóstico, não é teste independente de acurácia geral.

As oito amostras anteriores de `?`, `!`, `:` continuam 8/8 no pipeline corrigido. Fixtures e CSVs com escrita do usuário ficam em `local/`, fora do Git. Nenhum texto existente foi reinterpretado para substituir seu documento.

## Validação e limites

Build Framework sem avisos; **52 verificações passaram**. Testes cobrem espera entre traços, contato/proximidade, fila pausada/cancelamento, snapshots isolados, atualização de bounds, til cursivo e autosave coalescido/consistente/erro, além das regressões anteriores. Comparação após desfazer a marca de smoke e fechar confirmou os mesmos textos, 106 regiões e 364 traços do documento original.

Smoke desktop com mouse: navegação explícita por barra foi possível; início de traço em folha rolada manteve offset 655,68 DIPs e mostrou tinta antes da conversão. Teste não simula driver físico de caneta, palma ou todos os gestos Windows. Correção de causas conhecidas não comprova ausência de todo salto em hardware real.

Referências oficiais de mecanismos WPF: [flicks](https://learn.microsoft.com/en-us/dotnet/api/system.windows.input.stylus.isflicksenabled), [ScrollViewer](https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.scrollviewer), [foco](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/focus-overview).

Persistem: pausas muito longas podem parecer fim de palavra; aproximação de caneta depende de hover do dispositivo; agrupamento de cursiva é heurístico; não há modelo linguístico novo ou garantia de reconhecer toda palavra. Testar novamente com caneta física, incluindo `ficção` com diacríticos tardios e escrita perto das bordas após rolagem.
