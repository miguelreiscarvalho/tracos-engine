# Publicação e manutenção

Repositório previsto: [miguelreiscarvalho/tracos-engine](https://github.com/miguelreiscarvalho/tracos-engine).

## Antes de publicar alterações

1. Execute `git status`, `git diff --check` e revise o diff.
2. Execute `build.ps1 -CoreOnly -Test`; para backend/demo, execute também `build.ps1 -Test`.
3. Confirme que somente código, documentação e dados sintéticos autorizados estão versionados.
4. Não inclua `build/`, `local/`, documentos pessoais, DLLs Microsoft, credenciais, caches ou gravações.
5. Faça commits pequenos e abra um PR com o modelo.

O histórico Git também será publicado no primeiro push. Remover um arquivo apenas do diretório atual não o remove de commits anteriores.

## CI

O workflow `.github/workflows/ci.yml` executa build e testes do núcleo em runner Windows. Ele não depende de Microsoft.Ink nem de um modelo de idioma instalado. O reconhecedor real e o uso com caneta exigem validação adicional em ambiente preparado.

## Releases futuras

Não há pacote estável nesta preparação. Antes de uma release:

- Defina versão e incompatibilidades no changelog.
- Valide o backend no ambiente Windows pretendido e registre limitações.
- Distribua somente arquivos próprios cuja licença permita a publicação.
- Não envie a pasta `build/` inteira: ela contém uma cópia local de Microsoft.Ink.dll.
- Inclua licença e avisos de dependências nos artefatos autorizados.

## Configuração do GitHub

Depois da primeira publicação, confira a execução do CI na aba Actions. Proteção de branch, revisão obrigatória, relato privado de vulnerabilidades e permissões de colaboradores devem ser configurados conforme a manutenção do projeto; os arquivos locais não ativam essas opções.
