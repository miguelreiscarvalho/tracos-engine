# Traços — protótipo de escrita para texto

Aplicativo desktop Windows: escreva livremente na folha usando caneta ou mouse e o reconhecedor local pt-BR substitui automaticamente a tinta por texto editável. A primeira implementação foi precedida por pesquisa, enumeração do motor e comparação A/B/C com dados sintéticos.

## Executar

No PowerShell, na pasta do projeto:

```powershell
.\build.ps1 -Test
.\run.ps1
```

Ou abra `build\Tracos.exe` depois da compilação. O projeto usa o compilador do .NET Framework do Windows, sem NuGet, Node ou SDK .NET moderno. Requer Windows com .NET Framework 4.8, WPF e `C:\Program Files\Common Files\microsoft shared\ink\Microsoft.Ink.dll`. O build copia essa biblioteca **localmente para build**, não a versiona ou publica. Licenciamento do componente é o do Windows; não distribuir a DLL por conta própria.

Se faltar o modelo: Configurações do Windows → Hora e idioma → Idioma e região → Português (Brasil) → Opções → Manuscrito. Verifique os motores realmente disponíveis com:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\probe.ps1
```

Nesta máquina, o motor pt-BR foi encontrado mesmo que a lista de idiomas preferenciais mostrasse apenas en-US. A enumeração da API é a verificação usada pelo aplicativo. Não há chamadas de rede no aplicativo nem envio de tinta.

## Usar

1. Escreva na folha e faça uma pausa natural. A conversão aguarda completar os traços, sem botão ou confirmação.
2. Continue escrevendo: novas revisões da região podem substituir hipóteses anteriores, inclusive após pontos e acentos tardios.
3. Selecione fonte/tamanho; use o slider para zoom e as barras para rolagem.
4. Duplo clique numa região para corrigir o texto. Uma correção manual protege essa região de revisão automática. Para apagar uma região, ative Selecionar, clique e use Apagar seleção.
5. Desfazer restaura a tinta anterior à conversão; Refazer restaura o resultado sem nova inferência. Após desfazer, a região só volta a ser reconhecida quando recebe nova escrita.
6. Salvar/Abrir usa JSON com tinta original, texto e formatação. A recuperação automática está em `local\recovery.json`, com backup `.bak`; ela é carregada ao iniciar. Arquivos locais não entram no Git.

A folha é fixa (2400 × 1800 DIPs), com uma página e camada no protótipo. Texto fica ancorado e não reorganiza a folha sob a caneta. A conversão pode mudar hipóteses completas, não necessariamente uma letra de cada vez.

## Validação reproduzível

```powershell
.\build.ps1 -Test
.\build\Tracos.Tests.exe --experiment
```

O primeiro comando executa as verificações de pontuação, acentos/cedilha, preservação de caracteres, posicionamento, espera adaptativa, histórico e concorrência (resultado atualizado em [fluidez e estabilidade](docs/FLUIDEZ_E_ESTABILIDADE.md)). O segundo chama o **motor real** nas políticas A/B/C e grava `local\experiments.csv`. As entradas são polilinhas sintéticas em `src/Experiments.cs`, não amostras humanas ou um benchmark de precisão.

## Conversão após pausa e fluidez

A tinta fica visível enquanto você completa uma palavra. A espera se adapta aos intervalos entre traços (1–2,2 s como base), dá tempo adicional quando a caneta está próxima e permite reconhecer antes um bloco encerrado quando você muda de lugar. Nenhum resultado é apresentado durante contato ativo. Acentos ou traços novos invalidam a revisão anterior; uma resposta já calculada pode aguardar uma pausa sem chamar o motor novamente.

Histórico e reconhecimento deixaram de serializar o documento inteiro na interface. O histórico copia metadados/listas e compartilha traços já concluídos, que o editor trata como imutáveis. Salvamento automático serializa e grava em worker separado; ao fechar, o app aguarda a última gravação. Núcleos integrados a outros aplicativos devem respeitar a mesma regra: nunca alterar pontos de um traço concluído; criar uma nova versão em seu lugar.

A folha bloqueia flicks/press-and-hold de caneta, não pede foco/BringIntoView ao começar um traço e preserva a transformação de coordenadas durante o contato. Rolagem explícita continua disponível fora da escrita. Teste físico de caneta ainda é necessário para confirmar o comportamento do driver.

Para medir operações internas usando uma cópia do seu documento:

```powershell
.\build\Tracos.Tests.exe --profile caminho\documento.json
```

O CSV fica em `local\performance-stability.csv`. Ele mede operações de preparação, não latência caneta→pixel ou uso total de CPU.

## Pontuação e repertório

Corpo e ponto de interrogação/exclamação, pontos empilhados e pequenos complementos inferiores agora podem pertencer ao mesmo bloco, inclusive se o corpo já foi convertido. O reconhecedor recebe o símbolo completo. Para dois pontos, uma regra restrita resolve `°` versus `:` quando há dois traços pequenos empilhados e `:` foi devolvido pelo motor como alternativa; não inventa candidatos nem converte posições em confiança.

O alvo documentado é **ASCII imprimível + acentos pt-BR em Unicode**, não os códigos de controle ASCII. Suporte a armazenamento/exibição não garante acurácia de reconhecimento. Os oito exemplos de `?`, `!` e `:` do segundo vídeo passaram no replay; cobertura manuscrita de todo o repertório ainda está pendente. Resultados e riscos: [pontuação e cobertura](docs/PONTUACAO_E_COBERTURA.md).

Para avaliar amostras rotuladas próprias:

```powershell
.\build\Tracos.Tests.exe --evaluate-symbols caminho\amostras.json
```

Formato: lista de objetos com `Name`, `Expected` e `Traces` (mesma representação de traços do documento). O resultado vai para `local\symbol-evaluation.csv`, comparando traços isolados, motor nativo com traços completos e agrupamento corrigido. O arquivo não substitui o documento aberto.

## Posicionamento local

Atualização após análise do vídeo 20-40-54: o padrão agora é **Texto em linhas**, com fonte uniforme, baseline comum e espaços tipográficos. O modo **Posição original** mantém as âncoras individuais. Ambos usam o tamanho selecionado, sem deformar/comprimir cada palavra para caber no desenho. Detalhes e limites: [análise e correção](docs/ANALISE_VIDEO_204054.md).

Textos convertidos têm janela de revisão de 8 s para pontos/acentos. Depois, escrever por cima preserva uma anotação separada; use Selecionar/Editar para mudar o texto pronto. O diálogo de correção oferece alternativas nativas quando disponíveis. Para fórmulas e desenhos, marque **Tinta livre** antes de escrever: os traços mantêm a forma original e não viram letras incorretas. Reconhecimento matemático estruturado ainda não foi implementado.

As descrições de encaixe na caixa abaixo registram o comportamento da revisão anterior e foram substituídas pela apresentação descrita acima.

A versão corrigida usa blocos locais de escrita, em vez de juntar a linha inteira. Uma lacuna ou mudança de linha encerra o bloco anterior; pontos, acentos e pequenos complementos sobrepostos podem rever somente o bloco correspondente. O texto automático ocupa a área dos traços originais, com redução de escala quando necessário, sem concatenar palavras distantes numa âncora antiga. Correção digitada manualmente mantém o tamanho de fonte escolhido e pode ultrapassar essa área.

Documentos antigos mantêm o texto reconhecido e os traços: não são reprocessados automaticamente nem divididos retroativamente. Blocos recuperados ficam encerrados para não incorporar novas palavras. Para inspecionar o agrupamento em uma cópia, sem substituir o documento original:

```powershell
.\build\Tracos.Tests.exe --replay caminho\documento.json
```

Esse comando grava apenas `local\replay-local-placement.json`. Margens geométricas continuam heurísticas: letras muito espaçadas podem gerar blocos separados e a redução de contexto pode alterar a qualidade de reconhecimento.

## Estado e documentos

- [Pesquisa, fontes e comparação](docs/PESQUISA.md)
- [Arquitetura, diagrama, decisões e metas](docs/ARQUITETURA.md)
- [Resultados observados e limitações](docs/RESULTADOS.md)
- [Descrição de PR para revisão local](docs/PULL_REQUEST.md)

Implementado: captura/renderização, motor real pt-BR, hipóteses revisáveis, edição tipográfica, fontes/tamanho, histórico, persistência e worker separado. Parcial: detecção geométrica de regiões e espaços, suporte a caneta (caminho implementado, teste físico pendente). Planejado: gestos contextuais, inserção por posição textual, múltiplas páginas/camadas, alinhamento por caractere, classificador treinado de espaços e corpus humano.

Os espaços da interface vêm do reconhecedor Windows; o agrupador geométrico independente é usado no experimento A/B, ainda não validado para produzir o texto final. Não há garantia de acurácia em cursiva ou conversão verdadeiramente causal dentro de um único traço contínuo.
