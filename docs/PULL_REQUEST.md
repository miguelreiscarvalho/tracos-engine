# fix: preserve punctuation strokes and document character coverage

## Resumo

Corrige o agrupamento que enviava corpos e pontos de interrogação/exclamação como regiões separadas. Reúne componentes verticais próximos e pequenos complementos inferiores, normaliza Unicode NFC e resolve um caso restrito de `°` versus `:` entre alternativas reais do motor.

## Mudanças principais

- Agrupamento de ponto e corpo compacto, em ambos os sentidos de desenho.
- Pequenos complementos inferiores para cedilha, mantendo proteção de palavras distantes.
- Seleção de `:` somente com dois traços pequenos empilhados e candidato nativo existente.
- Harness comparando traços isolados, motor completo e pipeline corrigido.
- Repertório de 95 ASCII imprimíveis + 26 acentos pt-BR, com matriz de evidência por alvo.

## Motivação

O segundo vídeo mostrou `?` virando `2`/`7` e `!` virando hastes, com os pontos inferiores isolados. A reexecução dos traços confirmou que o motor reconhece os exemplos completos quando recebe ambos os componentes; dois pontos também exigiu desambiguação entre alternativas.

## Como validar

1. Executar `build.ps1 -Test` e `run.ps1` no PowerShell.
2. Escrever na folha com mouse/caneta; levantar e observar conversão automática.
3. Escrever `?`, `!` e `:` separadamente e no fim de frases, incluindo ponto após corpo já convertido; verificar hipótese final e posição.
4. Desfazer conversão e conferir tinta original; Refazer deve reaplicar texto.
5. Acrescentar pontos/acentos/cedilha e testar negativos (`1`, `l`, `o`, círculos); conferir que palavras distantes não mudam.
6. Salvar, fechar e abrir; conferir texto e tinta preservados no JSON.

## Testes

- Compilação .NET Framework desktop e harness: executada sem avisos.
- `build.ps1 -Test`: 37 verificações passaram, dez novas verificações de pontuação/diacríticos e roundtrip.
- Oito amostras do segundo vídeo: isoladas 0/8; motor com traços completos 7/8; pipeline corrigido 8/8. Conjunto usado no ajuste, sem generalização alegada.
- Matriz de 121 alvos distingue roundtrip verificado de reconhecimento manuscrito ainda pendente.
- Editor reaberto; documento existente mantido, sem reconverter seu texto automaticamente.
- Caneta física, CER/WER em corpus humano, testes de longa duração e falha de energia: não executados.
- Gradle/Android não se aplicam; não existe projeto Gradle. SDK moderno `dotnet` fora do PATH não bloqueou o compilador Framework disponível.

## Riscos / pontos de atenção

- Dependência de Microsoft.Ink e modelo pt-BR instalado; motor proprietário/legado.
- Agrupamento geométrico pode confundir letras muito pequenas com pontos; exige avaliação física em escrita contínua.
- Dois pequenos círculos empilhados podem ser intencionais; seleção de `:` é uma regra limitada, sem confiança calibrada.
- Oito exemplos não validam todos os 121 alvos; acurácia de palavras/acentos em corpus amplo permanece pendente.
- Documentos antigos não são reprocessados automaticamente. Traços de usuário e vídeos ficam locais e fora dos commits.
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

Branch: `codex/fix-punctuation-and-diacritics`. Base local: `codex/fix-local-text-placement` (`326cc24`). Nenhum remote configurado; comparação feita com a base local. Esta descrição está preparada para revisão; PR remoto não foi criado, pois não existe destino configurado.
