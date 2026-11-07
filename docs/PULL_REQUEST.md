# fix: make handwriting conversion irreversible

Esta descrição substitui a revisão histórica abaixo: a conversão agora descarta os traços e encerra o histórico anterior, por preferência explícita do usuário. Novas escritas no mesmo lugar não reabrem o gráfico convertido. Campos Converted/ConvertedBounds preservam somente o retângulo para layout; arquivos antigos migram blocos completamente reconhecidos ao abrir.

Validação desta revisão: testes não executados por solicitação expressa do usuário. Somente compilação; teste manual será feito pelo usuário. Testes antigos de conversão reversível precisam de atualização. Risco principal: descarte intencional de tinta e limpeza de undo/redo após converter. Validar escrever→converter→escrever no mesmo local→desfazer sem recuperar tinta. Branch atual: codex/irreversible-ink-conversion; base 0064bd9. Sem publicação remota.

---

Descrição histórica da revisão anterior, não aplicável à política de conversão definitiva:

## Resumo

O vídeo do usuário mostra frase fragmentada, fonte irregular e revisão acidental de palavras antigas. Este PR separa a composição tipográfica do reconhecimento: usa fonte sem escala por palavra, organiza blocos em linhas e restringe a janela de revisão; traços posteriores sobre texto pronto ficam como anotações.

## Mudanças principais

- Composição visual de linhas com baseline, fonte e espaços comuns; opção Posição original.
- Remoção do encaixe/escala de glifos no renderer, mantendo cache de métricas e geometria.
- Janela de revisão de 8 s; sobreposições posteriores preservadas em InkOnly.
- Ferramenta Tinta livre para fórmulas/desenhos, sem prometer reconhecimento matemático.
- Alternativas nativas no diálogo de correção; snapshots/persistência/tickets compatíveis com campos novos.
- Finalização de traço ativo ao fechar antes do snapshot; análise com limites e testes.

## Motivação

Reduzir latência não corrigia o modelo de apresentação. A escala independente dos glifos tornava o tamanho selecionado inconsistente, e a revisão geométrica sem expiração permitia que destaques posteriores alterassem palavras já convertidas.

## Como validar

1. Executar `build.ps1 -Test` e `run.ps1` no PowerShell.
2. Escrever na folha com mouse/caneta; levantar e observar conversão automática.
3. Escrever uma frase em uma linha com palavras de tamanhos diferentes; confirmar fonte uniforme, baseline e espaços tipográficos. Alternar Texto em linhas/Posição original.
4. Desfazer conversão e conferir tinta original; Refazer deve reaplicar texto.
5. Alterar fonte 26→18 e verificar mudança comum em todos os blocos. Após mais de 8 s, riscar/circular palavra antiga: o texto deve permanecer e o traço ficar como anotação. Acrescentar acento dentro da janela de revisão.
6. Salvar, fechar e abrir; conferir texto e tinta preservados no JSON.
7. Marcar Tinta livre e escrever fórmula/desenho; não deve entrar no motor. Selecionar palavra e Editar para ver alternativas nativas quando presentes.

## Testes

- Compilação .NET Framework desktop e harness: executada sem avisos.
- `build.ps1 -Test`: 60 verificações passaram; compilação sem avisos. Oito verificações novas de layout, revisão protegida e persistência.
- Smoke com mouse: frase composta em linha, fonte 26→18 uniforme; traço sobre palavra antiga persistiu InkOnly e texto `representa` permaneceu intacto.
- Desfazer restaurou fonte 26 na folha e no seletor; textos/55 regiões/118 traços originais preservados após retirar as marcas de teste. Pontuação anterior permanece 8/8 na amostra local.
- Transcrição e imagens analisadas localmente; trechos incertos identificados, sem usar o áudio como autorização de sistema.
- Caneta física, CER/WER em corpus humano, testes de longa duração e falha de energia: não executados.
- Gradle/Android não se aplicam; não existe projeto Gradle. SDK moderno `dotnet` fora do PATH não bloqueou o compilador Framework disponível.

## Riscos / pontos de atenção

- Dependência de Microsoft.Ink e modelo pt-BR instalado; motor proprietário/legado.
- Agrupamento visual de linhas é heurístico, sem wrapping ou editor de parágrafo com cursor contínuo. Edição continua por região.
- Janela de 8 s é política explícita; acentos posteriores exigem Editar. Sobreposições ambíguas dentro da janela ainda precisam de testes físicos.
- Campos Layout/InkOnly são aditivos ao JSON v1; arquivos antigos adotam composição por linhas ao abrir, sem reprocessar conteúdo.
- Matemática bidimensional continua pendente; Tinta livre preserva a geometria. Precisão lexical do motor permanece limitada.
- Pesos, áudio, vídeo, transcrição e fixtures privadas permanecem fora do Git; não publicar material do usuário automaticamente.
- Uma página/camada; sem migração de banco. JSON possui versão 1 e backup.

## Screenshots ou gravação

Mudança visual: captura local em `local/review4-ui-after.png`; anexar somente com autorização para publicar o conteúdo do usuário. Quadros, áudio e transcrição permanecem locais.

## Checklist

- [x] Branch criada com nome descritivo
- [x] Commits revisados
- [x] Sem arquivos temporários/build/secrets
- [x] Testes executados ou bloqueio explicado
- [x] Mudanças manuais validadas no smoke com mouse (caneta física pendente)
- [x] PR descreve riscos e próximos passos

## Publicação

Branch: `codex/fix-line-typography-and-revision-boundaries`. Base local: `codex/fix-writing-stability-and-ui-stalls` (`2505ca4`). Nenhum remote configurado; comparação feita com a base local. Esta descrição está preparada para revisão; PR remoto não foi criado, pois não existe destino configurado.
