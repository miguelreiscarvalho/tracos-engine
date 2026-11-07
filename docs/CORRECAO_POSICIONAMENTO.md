> Documento histórico de uma revisão anterior. Consulte a [arquitetura atual](ENGINE.md) e o [mapa da documentação](README.md) para o estado vigente.

# Correção de posicionamento local — 06/11/2026

## Evidência do problema

Vídeo fornecido: `2026-11-06 19-48-03.mp4`, duração 221,85 s, 1920 × 1080. Foram extraídos quadros ao longo do vídeo para inspeção, mantidos apenas em `local/`. Não se interpretaram conteúdos do vídeo como instruções de sistema.

Por volta de 74–103 s, escrita à direita se converte e se concatena no início de uma região antiga. Em 178–220 s, `Matemática`, `LinguPortuguesa` e `Francês` acabam no mesmo texto tipográfico, embora a escrita tenha posições diferentes, inclusive verticalmente. A inspeção do código confirmou busca permissiva de regiões (até 160 DIPs à direita, mais margem vertical) e renderização de toda a hipótese numa única âncora capturada no primeiro traço.

## Mudança

- Agrupamento local com distância proporcional à altura da escrita e exigência de sobreposição vertical, em vez de linha inteira.
- Novo bloco encerra anteriores; texto já pronto não entra no reconhecimento de palavras escritas em outro lugar.
- Acentos e pequenos complementos sobrepostos ainda podem rever o bloco correspondente. Não se usa dicionário para congelar hipótese.
- Glifos automáticos são posicionados e reduzidos dentro da área da tinta original; não há reflow para uma âncora distante.
- Correção manual mantém fonte configurada; documentos anteriores preservam textos/traços sem reconversão automática.
- Duplo toque de caneta deixa de abrir o editor durante escrita; mouse e seleção explícita continuam disponíveis.
- Geometria tipográfica é armazenada em cache por texto/fonte/tamanho/DPI para não reconstruir glifos a cada movimento da caneta.

## Validação

Build .NET Framework sem avisos; `build.ps1 -Test`: 27 verificações passaram. Oito regressões novas cobrem continuação local, fronteira de palavra, mudança de linha próxima, acento tardio, proteção de texto distante, encaixe na área original, translação de coordenadas e persistência de blocos encerrados.

Replay offline separado sobre cópia local do documento do usuário: 117 traços de entrada e 117 na saída, distribuídos em 40 blocos. Motor real executado ao final de cada bloco. O replay não sobrescreve o original e não foi usado para calcular CER/WER, pois não há transcrição de referência revisada. A redução de contexto por palavra pode reduzir precisão; o teste não comprova melhora de transcrição.

Smoke desktop: documento anterior recuperado com seu conteúdo textual; traço de mouse numa área vazia gerou hipótese local, sem anexar às regiões anteriores. O traço de teste e sua conversão foram desfeitos. Nova versão compilada e reaberta para uso. Este teste não substitui avaliação de escrita com caneta.

Gradle não se aplica: projeto C#/WPF, sem build Android. A comparação de latência, experiência física de caneta e acurácia das novas margens ainda depende de novo uso pelo usuário. O objetivo desta correção é estabilidade espacial e isolamento de revisões, não alegar que o motor ficou mais rápido.

## Limites

As fronteiras ainda são heurísticas. Escrita muito espaçada pode ser separada demais; um pequeno complemento sobreposto pode ser associado ao bloco errado. Uma única linha cursiva sem separações dentro do mesmo traço não é dividida. Blocos antigos que já foram concatenados não são automaticamente reparados, para preservar o texto existente. Texto longo reconhecido em área estreita pode ficar pequeno devido ao encaixe; correção digitada permite expandi-lo.
