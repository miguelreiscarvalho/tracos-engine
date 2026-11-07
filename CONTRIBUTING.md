# Como contribuir

Traços está em fase alpha. Issues e pull requests são bem-vindos; descreva casos reproduzíveis e mantenha as alterações pequenas.

## Preparar o ambiente

1. Clone o repositório e crie uma branch descritiva.
2. Leia o README e a arquitetura em `docs/ENGINE.md`.
3. Execute `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -CoreOnly -Test`.
4. Para alterar o adaptador ou a demonstração, instale os requisitos Windows Ink e execute `build.ps1 -Test`.

Não há projeto Android/Gradle neste repositório. `testDebugUnitTest` e `assembleDebug` não se aplicam.

## Implementar e verificar

- Preserve o contrato de pontos em DIPs e a ordem temporal dos traços.
- Não altere pontos de um traço concluído: snapshots podem compartilhá-los.
- Mantenha o núcleo sem referências WPF ou Windows Ink.
- Mudanças de comportamento exigem testes relevantes. Não confunda testes com motor controlado com avaliação do reconhecedor real.
- Execute `git diff --check` e revise os arquivos antes de commitar.
- Use mensagens claras, por exemplo `fix: preserve late accents`.
- Documente incompatibilidades da API e do formato JSON.

O CI executa testes do núcleo em Windows sem instalar o backend proprietário. Alterações no adaptador exigem também build completo e validação local do reconhecimento; informe versão do Windows, idioma e limitações observadas.

## Dados de escrita

Use exemplos sintéticos ou amostras com autorização explícita de publicação. Nunca anexe recuperação automática, documentos pessoais ou vídeos sem revisar seu conteúdo. Resultados experimentais devem indicar referência esperada, origem, idioma e método; não declare acurácia a partir de poucos exemplos.

## Enviar o PR

Preencha o modelo, descreva motivação, comandos executados, resultados e riscos. Mudanças visuais devem incluir screenshots sem dados pessoais. Contribuições são oferecidas sob a licença MIT do projeto.
