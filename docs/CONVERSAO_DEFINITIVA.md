# Conversão definitiva

Preferência explícita do usuário: converter e descartar o gráfico, sem retorno pelo histórico.

InkConversion.Commit conserva texto, alternativas e retângulo de layout; esvazia traços e marca Converted. Agrupamento, tickets e proteção de sobreposição ignoram blocos convertidos para nova escrita. O cache de tinta é limpo após converter.

A conversão limpa undo/redo, impedindo recuperar snapshots com manuscrito anterior. O texto continua editável. Tinta livre e falhas mantêm traços. JSON legado migra blocos cujo texto representa todos os traços ao abrir.

Não foram executados testes, por solicitação expressa do usuário. Somente compilação para disponibilizar o app. Testes históricos da política reversível precisam de atualização antes de serem usados para validar esta nova semântica.
