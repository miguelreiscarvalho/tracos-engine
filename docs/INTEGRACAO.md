# Guia de integração

## Compilar e referenciar

Execute `build.ps1 -Test`. Referencie `build/Tracos.Core.dll` e `build/Tracos.WindowsInk.dll` em um aplicativo .NET Framework compatível. Para outro backend, apenas Core e sua implementação de `IRecognitionEngine` são necessários.

O consumidor Windows Ink precisa da biblioteca Microsoft.Ink e dos reconhecedores instalados. Não inclua componentes Microsoft na sua distribuição sem verificar os termos aplicáveis.

## Chamada mínima

```csharp
using System;
using System.Collections.Generic;
using Tracos;

public static class Example {
    [STAThread]
    public static void Main() {
        var trace = new InkTrace();
        trace.Points.Add(new InkPoint { X = 0, Y = 0, Time = 0, Pressure = 0.5f });
        trace.Points.Add(new InkPoint { X = 10, Y = 30, Time = 20, Pressure = 0.5f });
        IRecognitionEngine engine = new WindowsInkEngine();
        InkAnswer answer = engine.Recognize(new List<InkTrace> { trace }, 1046);
        Console.WriteLine(answer.Text);
    }
}
```

A linha sintética ilustra o contrato; não pressupõe uma transcrição específica. Uma versão completa que lê JSON está em `examples/RecognizeInk/Program.cs`.

## Entrada e saída

| Campo | Contrato |
| --- | --- |
| InkTrace.Points | Pontos na ordem de aquisição; não fornecer listas/pontos nulos |
| InkPoint.X / Y | Coordenadas finitas em DIPs, 96 unidades por polegada |
| InkPoint.Time | Tempo em ms na base temporal do consumidor; manter ordem coerente para heurísticas |
| Pressure | Pressão registrada; o adaptador atual envia somente geometria ao Windows Ink |
| TiltX / TiltY | Metadados opcionais; o adaptador atual não os transmite |
| language | LCID do reconhecedor; 1046 para pt-BR |
| InkAnswer.Text | Melhor hipótese, normalizada em NFC pelo adaptador |
| Alternatives | Alternativas nativas ordenadas, sem confiança calibrada |
| Engine | Nome do reconhecedor utilizado |

Não modifique pontos de traços concluídos: o modelo compartilha esses valores entre snapshots e mantém cache dos limites geométricos.

## Threading e erros

A chamada é síncrona e pode bloquear. Use uma thread STA dedicada ou `RecognitionScheduler`; não chame o backend a cada movimento na thread gráfica.

O scheduler coalesce trabalhos por região e usa pt-BR. O callback executa na thread de reconhecimento. Valide `Ticket.Matches` com o documento atual antes de aplicar; chamadas antigas podem terminar após edição/cancelamento.

Quando o idioma solicitado não está instalado, o adaptador lança `InvalidOperationException` (a mensagem atual menciona pt-BR). Falhas do componente nativo também podem gerar exceções. O scheduler entrega exceções em `Completion.Error`. Um resultado vazio pode indicar ausência de hipótese; não o trate automaticamente como erro de instalação.

## Implementar outro backend

Implemente `InkAnswer Recognize(IList<InkTrace> traces, int language)`. Documente plataforma, modelos, licença e qualquer transmissão de dados. Não invente probabilidades a partir da ordem de alternativas. Evite mutações na entrada.

A chamada de reconhecimento não descarta tinta. Para usar o modelo completo de documento, a decisão de chamar `InkConversion.Commit` e `History.ConversionBarrier` pertence ao consumidor.

## Avaliações opcionais

Após build completo, na raiz do repositório:

```powershell
.\build\Tracos.Tests.exe --experiment
.\build\Tracos.Tests.exe --evaluate-symbols caminho\amostras.json
.\build\Tracos.Tests.exe --replay caminho\documento.json
.\build\Tracos.Tests.exe --profile caminho\documento.json
```

Os comandos gravam resultados em `local/`. Para símbolos, a entrada é uma lista de objetos com `Name`, `Expected` e `Traces`. Perfil requer documento com pelo menos uma região contendo traços; documentos totalmente convertidos não atendem a essa condição. Replay usa somente tinta ainda presente no arquivo.

O experimento padrão usa desenhos sintéticos. Reporte corpus, idioma e versão do ambiente ao publicar métricas.
