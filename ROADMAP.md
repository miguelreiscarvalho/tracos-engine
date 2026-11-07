# Roadmap

As etapas abaixo são intenções, sem prazo ou garantia de entrega.

## Fundação disponível

- [x] Contrato público de reconhecimento e modelo de traços.
- [x] Bibliotecas separadas de núcleo e adaptador Windows Ink.
- [x] Editor de demonstração e consumidor mínimo por JSON.
- [x] Testes do núcleo sem backend instalado.
- [x] Licença, documentação de integração e configuração de CI.

## Próximas prioridades

- [ ] Corpus humano pt-BR consentido, com ground truth e avaliação CER/WER.
- [ ] Medição de latência, memória e escrita contínua com caneta física.
- [ ] Testes de integração do adaptador em ambientes Windows com modelos instalados.
- [ ] Versionamento formal da API e do formato de documento.
- [ ] Empacotamento das bibliotecas sem dependências proprietárias.
- [ ] Melhor diagnóstico de idioma, inicialização e instalação.

## Exploração futura

- [ ] Backends adicionais implementando `IRecognitionEngine`.
- [ ] Núcleo portável com runtime moderno, após validar threading e persistência.
- [ ] Políticas configuráveis para retenção de tinta no aplicativo consumidor.
- [ ] Reconhecimento matemático e gestos somente após pesquisa e avaliação.
