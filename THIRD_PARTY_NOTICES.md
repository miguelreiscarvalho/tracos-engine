# Dependências externas

O código original deste repositório está sob a licença MIT. Essa licença não concede direitos sobre componentes externos.

| Dependência | Uso | Distribuição neste repositório |
| --- | --- | --- |
| Microsoft .NET Framework / WPF | Runtime, compilação e editor Windows | Não incluídos |
| Microsoft.Ink / Windows Ink | API COM/interop e reconhecimento | Não incluídos |
| Reconhecedores e modelos de idioma do Windows | Transcrição local | Não incluídos |

O build completo copia a DLL Ink da instalação do usuário para a pasta local ignorada `build/`. Isso não autoriza redistribuí-la em releases, pacotes ou instaladores. Cada usuário fornece sua própria instalação e observa os termos do componente.

Links de pesquisa em `docs/` são referências; não significam que pesos, código ou serviços desses projetos estejam incorporados.
