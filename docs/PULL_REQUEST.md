# fix: keep handwriting conversion in its original area

## Resumo

Corrige o agrupamento que concatenava palavras/linhas distantes e as convertia na âncora inicial, alterando conteúdo anterior. A conversão agora usa blocos locais e posiciona os glifos na área da tinta original; blocos encerrados só aceitam pequenos complementos sobrepostos.

## Mudanças principais

- Fronteiras locais por lacunas/posição vertical e persistência de blocos encerrados.
- Encaixe dos glifos na área manuscrita, com cache por fonte/texto/tamanho/DPI.
- Revisão restrita de pontos/acentos e proteção de regiões recuperadas.
- Caneta não aciona edição por duplo toque durante escrita.
- Oito regressões novas e replay separado de 117 traços preservados.

## Motivação

O vídeo do usuário mostrou palavras escritas à direita ou em outra altura voltando para uma região anterior. A causa estava no agrupamento espacial permissivo e na renderização de toda a região em uma única âncora, além de contatos rápidos de caneta poderem acionar edição.

## Como validar

1. Executar `build.ps1 -Test` e `run.ps1` no PowerShell.
2. Escrever na folha com mouse/caneta; levantar e observar conversão automática.
3. Escrever outra palavra à direita com espaço e outra em linha próxima; confirmar que cada uma permanece perto da tinta e que a anterior não muda.
4. Desfazer conversão e conferir tinta original; Refazer deve reaplicar texto.
5. Acrescentar ponto/acento sobre uma palavra anterior; testar correção manual, fonte/tamanho e zoom.
6. Salvar, fechar e abrir; conferir texto e tinta preservados no JSON.

## Testes

- Compilação .NET Framework desktop e harness: executada sem avisos.
- `build.ps1 -Test`: 27 verificações passaram, oito novas regressões espaciais.
- Replay com Microsoft Ink real em cópia local: 117 traços antes/depois, 40 blocos; sem substituir documento ou alegar CER/WER.
- Smoke desktop: carregamento, conversão local com mouse e undo verificados. Comparação de conteúdo confirmou as mesmas oito regiões e 117 traços originais após desfazer teste.
- Caneta física, CER/WER em corpus humano, testes de longa duração e falha de energia: não executados.
- Gradle/Android não se aplicam; não existe projeto Gradle. SDK moderno `dotnet` fora do PATH não bloqueou o compilador Framework disponível.

## Riscos / pontos de atenção

- Dependência de Microsoft.Ink e modelo pt-BR instalado; motor proprietário/legado.
- Agrupamento geométrico é heurístico; espaçamento irregular pode separar palavras demais e reduzir contexto do motor.
- Encaixe pode diminuir texto em região estreita; correções digitadas podem expandir. Documentos antigos não são reprocessados para reparar erros já existentes.
- Espaços finais dependem do motor; gestos contextuais e cursor por caractere pendentes.
- Histórico usa memória proporcional a snapshots; desempenho em documentos longos não medido.
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

Branch: `codex/fix-local-text-placement`. Base local: `codex/incremental-handwriting-prototype` (`66e9a28`). Nenhum remote configurado; comparação feita com a base local. Esta descrição está preparada para revisão; PR remoto não foi criado, pois não existe destino configurado.
