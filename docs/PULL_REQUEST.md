# refactor: prepare Traços as an open-source handwriting engine

## Resumo

Separa o motor de reconhecimento do editor Windows para permitir sua integração em outros aplicativos por bibliotecas C#. Prepara o repositório para colaboração no GitHub com licença MIT, documentação de uso, exemplos, testes e CI do núcleo.

## Mudanças principais

- Tracos.Core contém o contrato do motor, traços, agrupamento, fila e persistência, sem WPF ou Microsoft.Ink.
- Tracos.WindowsInk implementa o backend Windows, com o reconhecedor do sistema como dependência externa.
- Editor WPF, consumidor JSON e testes ficam em diretórios separados.
- Build completo e modo CoreOnly produzem e validam as bibliotecas sem NuGet.
- Seis verificações novas cobrem independência do núcleo, conversão definitiva, histórico, nova escrita, persistência e migração legada.
- README, licença, avisos externos, contribuição, segurança, roadmap, issues e modelo de PR.
- GitHub Actions testa o núcleo sem instalar componentes proprietários; documentação anterior permanece identificada como histórica.

## Motivação

O projeto estava organizado como um protótipo de aplicativo com compilação monolítica. Consumidores precisam de um contrato e bibliotecas reutilizáveis, instruções reproduzíveis e clareza sobre o que é aberto e quais dependências pertencem ao Windows.

## Como validar

1. Em Windows x64 com .NET Framework, executar `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -CoreOnly -Test`.
2. Em ambiente com Windows Ink, executar o mesmo comando sem `-CoreOnly`.
3. Executar `build\Tracos.RecognizeInk.exe examples\RecognizeInk\traces.json`; confirmar saída textual sem abrir o editor.
4. Executar `run.ps1`, escrever e aguardar conversão.
5. Confirmar que texto convertido permanece editável, nova escrita cria outro bloco e desfazer não recupera tinta convertida.
6. Revisar os guias de integração e dependências antes de incorporar as DLLs em outro aplicativo.

## Testes

Validação local em 07/11/2026:

- Build completo: passou com warnings tratados como erros.
- Build/testes CoreOnly: passou, sem referência do núcleo a WPF ou Microsoft.Ink.
- Suíte: 66 verificações passaram em cada modo.
- Consumidor JSON com reconhecedor pt-BR real: executou e retornou `C` para a amostra sintética.
- CLI sem argumentos: código 2; arquivo inexistente: código 1.
- Enumeração do backend: reconhecedor Microsoft pt-BR, LCID 1046, encontrado.
- Revisão de whitespace e de arquivos versionados realizada; build/local e dependências proprietárias excluídos.
- Não há projeto Gradle/Android; testDebugUnitTest/assembleDebug não se aplicam.
- Validação manual de escrita física e acurácia em corpus humano não foi executada nesta etapa.

A execução hospedada do GitHub Actions deve ser conferida após o push; execução local não comprova sucesso do CI remoto.

## Riscos / pontos de atenção

- Backend continua dependente do Windows Ink e de modelos instalados; não há backend aberto próprio ou suporte multiplataforma validado.
- Caminhos de código mudaram; integrações antigas devem referenciar as bibliotecas e os novos diretórios.
- Conversão definitiva da demonstração descarta tinta e limpa histórico; abertura de documentos antigos pode migrar blocos já reconhecidos.
- API experimental, sem garantia de compatibilidade estável.
- CI padrão testa o núcleo com motor controlado; não mede qualidade de transcrição nem instala Windows Ink.
- Licença MIT cobre código próprio, não a DLL Microsoft ou modelos externos.

## Screenshots ou gravação

Este PR não altera UI visível. As mudanças são principalmente internas e de organização/documentação.

## Checklist

- [x] Branch criada com nome descritivo
- [x] Commits revisados
- [x] Sem arquivos temporários/build/secrets
- [x] Testes executados ou bloqueio explicado
- [ ] Mudanças manuais validadas com caneta física
- [x] PR descreve riscos e próximos passos

Descrição preparada para revisão. Na importação inicial para um repositório novo, não há uma base remota anterior que exija abrir um PR.
