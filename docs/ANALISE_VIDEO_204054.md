> Documento histórico de uma revisão anterior. Consulte a [arquitetura atual](ENGINE.md) e o [mapa da documentação](README.md) para o estado vigente.

# Análise do vídeo 20-40-54 — tipografia e fronteiras de revisão

Vídeo fornecido em 06/11/2026, duração 248,72 s. Áudio em português transcrito localmente com faster-whisper (passagens small/medium e recortes difíceis), comparado a quadros do vídeo. A transcrição aproximada, com tempos e dúvidas, fica em `local/Transcricao_video_20-40-54.md`; áudio, pesos e resultados brutos permanecem fora do Git. Não se afirma transcrição humana/verbatim garantida. Implementação da transcrição: https://github.com/SYSTRAN/faster-whisper.

## O que foi observado

| Evidência | Causa identificada | Tratamento nesta revisão |
|---|---|---|
| Palavras com alturas/larguras diferentes apesar do mesmo tamanho no controle | Renderer aplicava escala X/Y por caixa manuscrita (`TextPlacement.Fit`) | Renderer usa métricas reais da fonte, sem escala por palavra |
| Frase “picada”, distante e sem baseline comum | Cada bloco tinha posição tipográfica independente | Composição visual em linhas, com baseline e espaços comuns |
| Escrever/circular sobre palavra antiga pode alterar sua hipótese | Revisão geométrica não tinha expiração | Janela de revisão de 8 s; sobreposição posterior vira anotação livre |
| `xa` em vez de x², B/D e expoentes separados | Motor de palavras, sem análise matemática bidimensional | Ferramenta Tinta livre para preservar fórmulas/desenhos; não se promete transcrição matemática |
| Trocas como `cotmndo` e `pele` | Erro lexical do motor, distinto do renderer | Alternativas nativas no diálogo de correção; não se força a palavra presumida pela imagem |

Leitura visual provável da primeira frase: “O conteúdo apresentado está sendo copiado de um quadro…”; isso é interpretação do analista, não transcrição da fala nem correção automática aplicada. A letra mencionada como B/D no áudio permanece marcada como incerta na transcrição.

## Arquitetura implementada

Reconhecimento continua por regiões locais e preserva traços. `TextLineLayout` compõe a apresentação a partir das métricas da fonte e das posições de origem. O conteúdo de regiões diferentes não é enviado de volta ao motor como uma só hipótese. O modo **Texto em linhas** é o padrão; **Posição original** preserva as âncoras individuais, igualmente sem comprimir glifos.

Linhas usam sobreposição vertical, proximidade e uma referência inicial; a baseline não é ajustada ao adicionar cada palavra. A posição vertical é ancorada no início da linha manuscrita. Fonte/tamanho são aplicados de maneira comum a todos os blocos, incluindo correções digitadas. Mudança de fonte ou edição pode alterar a largura da frase; isso é layout tipográfico, não alteração dos traços.

Composição em linhas é visual, não um novo editor de parágrafos com cursor contínuo. A edição ainda é por região, com alternativas realmente devolvidas pelo motor. Não há correção linguística automática silenciosa.

Controles de fonte/tamanho/layout são sincronizados com o documento após desfazer/abrir, evitando mostrar um tamanho no seletor e outro na folha. Fechar o diálogo de correção retoma regiões pendentes, sem perder os traços que aguardavam reconhecimento.

Uma região convertida aceita complementos por até 8 s após o último traço, para pontos, til e cedilha. Depois disso, sobreposições na caixa de origem **ou na caixa tipográfica exibida** são armazenadas como `InkOnly`, sem revisar a palavra pronta. Correção posterior usa Editar. Desenho não é assumido como gesto de exclusão/seleção por similaridade.

**Tinta livre** permite escrever fórmulas, diagramas e anotações sem reconhecimento, preservando a disposição 2D. `InkOnly` e `Layout` são campos aditivos do JSON v1; arquivos antigos sem Layout assumem Texto em linhas ao abrir, mantendo seus textos/traços. Snapshots e tickets conhecem InkOnly; regiões de tinta livre não entram no scheduler. Fechamento também conclui o traço ativo antes do snapshot.

## Validação

- `build.ps1 -Test`: 60 verificações passaram, incluindo baseline comum, espaço tipográfico, ausência de compressão, linhas separadas, modo original, proteção de texto antigo, acento recente e persistência dos novos campos.
- Dois testes do helper legado TextPlacement permanecem como compatibilidade do núcleo; ele já não é usado para renderizar a UI. Eles não são evidência da nova composição.
- Smoke desktop: documento do usuário abriu com frase em linha e fonte uniforme. Alterar 26→18 reduziu todos os blocos tipográficos de maneira comum; desfazer restaurou 26 tanto na folha quanto no seletor. Traço de mouse sobre palavra antiga persistiu como InkOnly, sem mudar o texto `representa`; marcas/fontes de teste foram desfeitas. Comparação confirmou os mesmos textos, 55 regiões e 118 traços originais. Captura de revisão em `local/review4-ui-after.png`, fora do Git.
- As oito amostras locais anteriores de pontuação continuaram 8/8 no pipeline corrigido. Isso não mede a acurácia de palavras completas ou de fórmulas.
- Áudio e vídeo foram analisados como dados; não conferem instruções de sistema nem autorização para publicar a gravação.
- Gradle não se aplica ao projeto C#/WPF. Validação física de caneta e uso contínuo precisam de novo teste.

## Limites e riscos que permanecem

- O motor pode continuar confundindo conteúdo, B/D, `pelo`/`pele` e outros caracteres. A lista de alternativas nem sempre contém a resposta correta.
- Equações, expoentes e relações matemáticas não foram implementados como reconhecimento estrutural. Tinta livre preserva sua escrita, não produz LaTeX/MathML editável.
- Agrupamento visual de linhas ainda é geométrico: escrita inclinada, colunas próximas e inserções no meio da linha podem exigir ajuste. Não há wrapping automático ou reflow de página inteira.
- Janela de 8 s é uma política explícita do editor, não estimativa probabilística de intenção; acento muito tardio exige edição explícita. Dentro da janela, ambiguidade geométrica ainda existe.
- O novo modo mantém âncora da linha e uniformiza fonte/espaçamento; não mantém simultaneamente cada distância manuscrita. O usuário pode escolher Posição original.
- Legado já convertido erroneamente não é reprocessado ou apagado automaticamente. Anotações antigas já confundidas com letras continuam no documento.
- A transcrição tem dois trechos incertos e um longo intervalo de escrita sem fala inteligível identificada; não devem ser preenchidos com suposições da imagem.
