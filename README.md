# Traços

Motor experimental de conversão de escrita manuscrita em texto editável a partir de **traços digitais** de caneta ou mouse.

**Estado: alpha · Windows · C# / .NET Framework · licença MIT**

Traços oferece um núcleo reutilizável, um adaptador para o reconhecedor local Windows Ink e um editor WPF de demonstração. O código do projeto é aberto; o reconhecedor e os modelos de idioma do Windows são componentes externos da Microsoft.

> English: Experimental digital ink-to-text engine with a reusable C# core, a Windows Ink backend and a WPF demo. Input is pen trajectories, not scanned images. The code is MIT licensed; the Windows recognition backend is an external dependency.

## Começar

Requisitos para a implementação completa:

- Windows x64 com .NET Framework 4.8 e WPF.
- `Microsoft.Ink.dll` instalada pelo ambiente Windows/Tablet PC.
- Reconhecedor manuscrito de Português (Brasil) para reconhecimento real.
- Windows PowerShell 5.1. O build usa o compilador do Framework; não exige NuGet ou SDK .NET moderno.

```powershell
git clone https://github.com/miguelreiscarvalho/tracos-engine.git
cd tracos-engine
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
.\run.ps1
```

Se a biblioteca Ink estiver em outro local, use `-InkAssemblyPath 'C:\caminho\Microsoft.Ink.dll'` ou a variável `TRACOS_INK_ASSEMBLY_PATH`. Para enumerar reconhecedores instalados, execute `tools/probe.ps1`. Se pt-BR estiver ausente, verifique o recurso Manuscrito nas opções desse idioma no Windows.

Para desenvolver e executar os testes do núcleo **sem Windows Ink instalado**:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -CoreOnly -Test
```

Esse modo ainda requer Windows/.NET Framework e grava em `build/core/`. Os testes usam um motor controlado e verificam a lógica; não medem acurácia manuscrita.

## Integrar em outro aplicativo

O build completo gera:

| Arquivo | Responsabilidade |
| --- | --- |
| `Tracos.Core.dll` | Contrato do motor, traços, documento, agrupamento, fila e persistência |
| `Tracos.WindowsInk.dll` | Adaptador do reconhecedor instalado no Windows |
| `Tracos.exe` | Editor WPF de demonstração |
| `Tracos.RecognizeInk.exe` | Exemplo de consumo por linha de comando |
| `Tracos.Tests.exe` | Testes e ferramentas de avaliação |

As bibliotecas ficam em `build/`. Um consumidor referencia as duas DLLs do projeto e disponibiliza a dependência Windows Ink instalada no sistema.

```csharp
// Executar em uma thread STA. traces: IList<InkTrace>, em ordem temporal.
IRecognitionEngine engine = new WindowsInkEngine();
InkAnswer answer = engine.Recognize(traces, 1046); // pt-BR
Console.WriteLine(answer.Text);
```

Exemplo executável com uma curva sintética:

```powershell
.\build\Tracos.RecognizeInk.exe .\examples\RecognizeInk\traces.json
```

O resultado depende do reconhecedor instalado; a amostra não é um benchmark. Consulte o [guia de integração](docs/INTEGRACAO.md) para unidades, threading, erros e implementação de outros backends.

## Estrutura

```text
src/
  Core/                  Núcleo sem WPF e sem Microsoft.Ink
  WindowsInk/            Backend Windows
examples/
  Tracos.Demo/           Editor de demonstração WPF
  RecognizeInk/          Consumidor mínimo com entrada JSON
tests/Tracos.Tests/      Testes e avaliações
docs/                    Integração, arquitetura e histórico técnico
experiments/             Resultados sintéticos versionados
.github/                 CI, issues e modelo de pull request
build/                   Saídas locais ignoradas
local/                   Documentos e resultados privados ignorados
```

## Editor de demonstração

Escreva na folha e faça uma pausa para converter. Há fonte/tamanho, zoom, rolagem, edição de texto por duplo clique e modo **Tinta livre** para desenhos e fórmulas.

**A conversão na demonstração é definitiva:** os traços convertidos são descartados e o histórico anterior é limpo. Desfazer não recupera essa tinta. Novas marcas no mesmo lugar criam outros blocos. Essa é uma política do editor, não uma exigência de `IRecognitionEngine.Recognize`, que apenas devolve o resultado.

Salvar/Abrir usa JSON. A recuperação automática fica em `local/recovery.json`; esses arquivos não devem entrar no repositório. Documentos antigos com blocos totalmente reconhecidos migram para a política definitiva ao abrir.

## Capacidades e limites

- Entrada por trajetórias com coordenadas, tempo e pressão; reconhecimento local pt-BR com alternativas.
- Agrupamento geométrico, espera adaptativa e fila STA com controle de revisões.
- Texto Unicode, acentos e pontuação; alternativas não representam probabilidades calibradas.
- Sem serviço de rede no fluxo de reconhecimento atual.
- Não inclui OCR de fotos/PDFs, modelo próprio treinado, reconhecimento matemático estruturado ou backend multiplataforma.
- Acurácia em escrita humana, latência com caneta física e cobertura completa de caracteres ainda precisam de avaliação sistemática.

## Documentação e colaboração

- [Arquitetura atual](docs/ENGINE.md)
- [Integração e API](docs/INTEGRACAO.md)
- [Mapa da documentação e evidências históricas](docs/README.md)
- [Publicação e manutenção no GitHub](docs/PUBLICACAO.md)
- [Roadmap](ROADMAP.md) · [Changelog](CHANGELOG.md)
- [Como contribuir](CONTRIBUTING.md) · [Conduta](CODE_OF_CONDUCT.md) · [Segurança](SECURITY.md)
- [Licença MIT](LICENSE) · [Dependências externas](THIRD_PARTY_NOTICES.md)

Não publique documentos pessoais, gravações, modelos do Windows ou `Microsoft.Ink.dll`. Builds e dados de uso permanecem locais.
