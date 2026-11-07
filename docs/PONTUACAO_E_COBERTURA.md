> Documento histórico de uma revisão anterior. Consulte a [arquitetura atual](ENGINE.md) e o [mapa da documentação](README.md) para o estado vigente.

# Pontuação e cobertura de caracteres — 06/11/2026

## Requisito e terminologia

O editor deve reconhecer letras, números, pontuação e acentos em pt-BR. ASCII tem 95 caracteres imprimíveis (U+0020–U+007E); os demais códigos são controles, não glifos manuscritos. Acentos são representados por Unicode, com formas precompostas ou sequências combinantes. Não existe uma única tabela universal de “ASCII estendida”. O alvo inicial está em `CharacterCoverage.Target`: 95 imprimíveis mais `ÁÀÂÃÉÊÍÓÔÕÚÜÇáàâãéêíóôõúüç` (121 caracteres). Isso não limita a saída do motor: outros caracteres Unicode continuam permitidos.

Distinguimos três capacidades:

1. Representar/salvar o caractere: teste de roundtrip do repertório inteiro implementado.
2. Exibir o caractere: dependente da fonte; o teste de roundtrip não mede renderização.
3. Reconhecer suas diversas formas manuscritas: exige amostras rotuladas e métricas por caractere/contexto. Não foi validado para todos os 121 alvos.

Referências: [Unicode, letras e diacríticos](https://www.unicode.org/faq/latin_cyrillic.html), [Unicode, combinações](https://www.unicode.org/faq/char_combmark.html). São referências de representação de caracteres, não promessas de acurácia do Windows Ink.

## Diagnóstico do vídeo

Arquivo fornecido: `2026-11-06 20-02-04.mp4`, 181,77 s, 1920 × 1080. Quadros foram inspecionados ao longo do vídeo. Exemplos por volta de 25–45 s e 160–175 s mostram interrogações/exclamações cujos corpos viram `2`, `7`, `|` ou `I`, e os pontos inferiores viram regiões independentes. Dois pontos viram dois pequenos `o`. Os traços originais correspondentes foram recuperados de uma cópia do JSON local, sem redesenhar o vídeo nem publicar a escrita do usuário.

O agrupador aceitava principalmente sobreposição vertical ou acentos superiores. O ponto inferior fica fora desse teste, logo o motor recebia pedaços do caractere. Reunir os traços recupera sete dos oito símbolos sem alterar o modelo. Para o exemplo restante (dois pontos), o motor retorna `°` primeiro e `:` entre as alternativas.

## Implementação

- Agrupamento provisório de ponto e corpo compacto acima/abaixo, nos dois sentidos temporais. Limitado a região de um traço anterior, proximidade horizontal/vertical e tamanho; não agrega uma palavra inteira a um ponto.
- Complementos pequenos inferiores também podem revisar uma palavra para acomodar cedilha.
- Candidatos normalizados em Unicode NFC, sem remover acentos, pontuação ou impor um whitelist ASCII.
- Desambiguação `°`/`:` restrita a dois traços pequenos empilhados, saída nativa compatível e alternativa `:` realmente existente. A ordem nativa completa até dez alternativas permanece disponível; não é porcentagem de confiança.
- Harness para avaliação rotulada, com políticas separadas `isolated-strokes`, `native-combined` e `local-grouped`.

## Resultados efetivamente observados

Oito casos selecionados/rotulados a partir do vídeo: quatro `?`, três `!`, um `:`. Todos foram usados durante diagnóstico/ajuste: não são um conjunto de teste independente.

| Casos | Traços isolados | Motor nativo com corpo+ponto | Agrupador e seleção corrigidos |
|---|---|---|---|
| 4 interrogações | 0/4 exatos (`2.`, `7o`, `2.`, `7.`) | 4/4 | 4/4 |
| 3 exclamações | 0/3 exatos (`|.`, `I.`, `1r`) | 3/3 | 3/3 |
| 1 dois pontos | 0/1 (`oo`) | 0/1 (`°`) | 1/1 (`:`) |
| Total da amostra | 0/8 | 7/8 | 8/8 |

Esses números não significam 100% de acurácia geral. A política de traços isolados reproduz o mecanismo da falha, não uma reexecução integral da versão anterior. A avaliação envia todos os traços de cada amostra conhecida; escrita contínua livre ainda precisa de teste físico. Não se calculou CER/WER de todas as palavras do vídeo: não há transcrição completa validada.

CSV e fixtures de usuário ficam em `local/symbol-evaluation.csv` e `local/symbol-cases.json`, fora do Git. Não há garantia de reprodutibilidade em outra máquina sem os mesmos traços/modelo instalado. Cada linha registra referência, resultado, correspondência exata, CER e duração; ordem fixa/frio versus aquecido impedem tratar os tempos como benchmark comparável.

`build.ps1 -Test`: **37 verificações passaram**, compilação sem avisos. Dez novas verificações cobrem `?`, `!`, `:`, ordem invertida de traços, ausência de candidato, pontos horizontais/distantes, cedilha inferior e roundtrip dos 121 alvos. Não se executou Gradle: projeto Windows C#/WPF.

## Cobertura pendente e riscos

- A [matriz por caractere](REPERTORIO.csv) identifica o alvo, roundtrip e o que ainda não possui amostras. Os três símbolos avaliados têm evidência local pequena; os demais não receberam validação manuscrita sistemática nesta etapa.
- Espaço requer teste de fronteira entre palavras, não um traço isolado. Controles como NUL/ESC/DEL não são letras e não entram no alvo manuscrito.
- Acento agudo/grave, circunflexo, til, cedilha, maiúsculas/minúsculas, dígitos e todos os símbolos ASCII precisam de corpus por usuário e em palavras/frases, incluindo negativos (`1`/`l`/`!`, `2`/`?`, `o`/`°`/`:`). Armazenar corretamente não comprova reconhecimento correto.
- A geometria não distingue toda intenção. Uma pequena letra em outra linha pode se parecer com um ponto, e dois círculos empilhados podem ser intencionais. A regra de dois pontos é determinística, não um novo classificador treinado.
- Hipóteses parciais ainda podem aparecer antes do último ponto; sua revisão é esperada. Erros lexicais do motor, como trocar letras em palavras completas, não são corrigidos por esta mudança.
- Textos antigos não são reinterpretados automaticamente. Os traços e as hipóteses originais continuam preservados.

Próxima etapa: recolher múltiplas amostras independentes dos 121 alvos (espaço em contexto), medir matriz de confusão/CER por classe e frases, e comparar outro motor apenas se as limitações persistirem com agrupamento correto. Não prometer tabela inteira reconhecida sem esses testes.
