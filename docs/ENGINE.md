# Arquitetura atual

Esta página descreve a organização atual do motor. Relatos de implementação anteriores em `docs/` são evidência histórica e podem conter políticas substituídas.

## Componentes

| Componente | Código | Dependências |
| --- | --- | --- |
| Tracos.Core | `src/Core/` | Bibliotecas do .NET Framework |
| Tracos.WindowsInk | `src/WindowsInk/` | Core, System.Drawing, Microsoft.Ink |
| Editor WPF | `examples/Tracos.Demo/` | Core, WindowsInk, WPF |
| Consumidor JSON | `examples/RecognizeInk/` | Core, WindowsInk |
| Testes/avaliações | `tests/Tracos.Tests/` | Core; WindowsInk no build completo |

O contrato `IRecognitionEngine` e o resultado `InkAnswer` pertencem ao núcleo. O adaptador converte DIPs para HIMETRIC, chama o reconhecedor instalado e normaliza a saída em NFC. O núcleo não contém um modelo treinado próprio.

## Fluxo

1. O consumidor captura pontos e conclui traços.
2. O agrupamento espacial organiza traços em regiões.
3. A política temporal do consumidor escolhe quando reconhecer.
4. A fila `RecognitionScheduler` processa a revisão pendente mais recente por região em uma thread STA.
5. O callback devolve texto, alternativas ou erro. O consumidor valida sessão/documento/revisão antes de aplicar.
6. O consumidor decide como apresentar, editar e armazenar o resultado.

A chamada direta a `Recognize` não agrupa, agenda, salva ou descarta tinta. O scheduler atual usa pt-BR (LCID 1046); o contrato direto recebe o idioma solicitado.

## Concorrência

Traços concluídos são tratados como imutáveis. Snapshots possuem seus próprios contêineres, mas podem compartilhar pontos e traços. O callback do scheduler ocorre no worker: o consumidor deve transferir atualizações de interface para sua thread gráfica.

Cancelamento é lógico: uma chamada nativa em curso pode terminar. Tickets e validação de revisão evitam aplicar respostas obsoletas. `Dispose` sinaliza o encerramento do scheduler, sem prometer interromper uma chamada COM em curso.

## Política da demonstração

O editor aplica conversão definitiva: guarda texto e retângulo, limpa os traços da região e o histórico anterior. Texto convertido não recebe novas marcas. Arquivos antigos podem migrar ao abrir; trabalhe sobre cópias quando precisar preservar a tinta original.

Persistência usa JSON, escrita atômica e backup. Recuperação automática fica em pasta local ignorada. A API é experimental e ainda não tem garantia de compatibilidade estável.

## Avaliação

Os testes do núcleo verificam lógica com motores controlados. Os comandos experimentais do build completo chamam o reconhecedor real, usando amostras sintéticas ou um corpus fornecido pelo usuário. Essas duas categorias não são equivalentes.
