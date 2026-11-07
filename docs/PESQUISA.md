> Documento histórico de uma revisão anterior. Consulte a [arquitetura atual](ENGINE.md) e o [mapa da documentação](README.md) para o estado vigente.

# Pesquisa e viabilidade — 06/11/2026

Escopo: repositório inicialmente vazio; nenhuma conversa anterior foi consultada.
Esta pesquisa fundamenta um protótipo Windows, não uma alegação de superioridade geral.

## Evidências e candidatos

| Candidato | Entrada / implementação | Desktop, português, offline | Distribuição / integração / limites |
|---|---|---|---|
| Windows InkRecognizerContainer | Traços; WinRT; RecognizeAsync devolve resultados por palavra | Windows, modelos de idioma instalados; verificar pt-BR por enumeração | Motor proprietário do Windows, sem pesos; SDK/projeções WinRT necessários. Não oferece contrato de streaming por letra nem confiança numérica documentada. |
| Microsoft.Ink RecognizerContext | Traços; biblioteca gerenciada sobre Tablet PC COM | Encontrado localmente com reconhecedor LCID 1046 (pt-BR); inferência local | Biblioteca do sistema, não redistribuída no projeto. API legada, dependência Windows. Pode executar em worker STA; extensão incremental pertence ao editor. |
| ML Kit Digital Ink | Traços x,y,t; SDK Android/iOS | Português na família de modelos; inferência offline depois do download; desktop não suportado diretamente | Motor/pesos não abertos; não confundir exemplos públicos com licença do motor. Não escolhido por exigir runtime móvel. Latência local desconhecida. |
| MyScript iink | Tinta e edição interativa; exemplos C#/WPF e UWP | Windows disponível; recursos e idiomas dependem do pacote contratado | SDK proprietário, certificado e condições comerciais; sem aquisição de chave neste trabalho. Exemplos públicos não tornam o motor aberto. |
| SimpleHTR | Python/TensorFlow, CNN + duas camadas BiLSTM + CTC; imagens | Executável em desktop; pesos IAM, adequação a português não demonstrada | Código MIT verificado; pesos disponíveis por link externo no README, não executados. Perde tempo/pressão; dependências TF e pré-processamento raster. |
| LstmOnlineHTR | Python/PyTorch, Conv1D + BiLSTM + CTC; deltas x,y,t, estado | Desktop possível; treinamento IAM; português/acento não demonstrados | README declara MIT, mas arquivo LICENSE na rota consultada retornou 404: não reutilizar código sem esclarecer. Caminho de checkpoint descrito não comprova pesos prontos. Código do modelo inspecionado; não executado. |
| TrOCR base handwritten | Transformer visual encoder-decoder; imagem de linha | Desktop com PyTorch/Transformers, pesos públicos; ajuste IAM, português não validado | Card oficial identifica MIT; download e memória maiores que o adaptador local. Não é modelo nativo de trajetórias nem streaming de caneta. |

Não há benchmark comum entre esses candidatos neste computador. Não se atribuem latências de artigos a esta máquina. Bibliotecas legadas podem continuar disponíveis sem serem a direção recomendada para um produto novo. Para manutenção, a disponibilidade das páginas/código foi verificada nesta data; não foi auditado o histórico completo de releases, issues ou vulnerabilidades. Nenhum candidato open source foi copiado.

## Modelos e modalidades

CTC aprende sequências sem fronteiras rotuladas por caractere; não exige separar letras cursivas. Uma RNN causal pode emitir prefixos; BiLSTM usa contexto futuro e requer reprocessar a janela. Transformers com atenção global também não são automaticamente causais. Encoder-decoder pode omitir/repetir conteúdo; o trabalho sobre rescoring CTC/S2S investiga esse problema. A publicação sobre canetas IMU encontrou CNN/BiLSTM superior ao Transformer em seu benchmark: não prova o mesmo para coordenadas de uma mesa digitalizadora.

Imagem permite reutilizar OCR e modelos visuais, mas descarta ordem e adiciona rasterização/segmentação. Trajetórias preservam ordem e pequenos acentos; pressão/inclinação podem ser armazenadas mesmo se ignoradas pelo motor. Combinação visual/vetorial é hipótese futura; não há evidência local para justificar dois motores agora. Não treinar do zero: não existe corpus pt-BR representativo neste repositório.

## Estratégias a testar

A: agrupar por lacunas espaciais e reconhecer palavras ao fechar grupo. B: reconhecer componentes espaciais sobrepostos como possíveis caracteres; componentes não equivalem a letras. C: reconhecer prefixos da região e revisar a região inteira quando chegam novos traços. Uma quarta opção é streaming causal treinado com prefixos e alinhamento, dependente de modelo/corpus que não estão disponíveis aqui.

As três políticas serão executadas com o mesmo motor local e tinta sintética explicitamente identificada. Isso permite testar integração e medir chamadas, mas não escolher a mais precisa para escrita humana. A decisão de produto permanece condicionada a coleta humana pareada (letra de forma/cursiva, acentos tardios, números e termos técnicos).

## Referências primárias consultadas

- https://learn.microsoft.com/en-us/uwp/api/windows.ui.input.inking.inkrecognizercontainer.recognizeasync — contrato assíncrono, resultados por palavra.
- https://learn.microsoft.com/en-us/windows/uwp/ui-input/convert-ink-to-text — candidatos ordenados e instalação de idiomas.
- https://learn.microsoft.com/en-us/windows/win32/tablet/text-recognizers — idiomas dos reconhecedores Win32.
- https://learn.microsoft.com/en-us/globalization/locale/handwriting-settings — dependência de modelos locais.
- https://developers.google.com/ml-kit/vision/digital-ink-recognition/ — trajetória e plataformas móveis.
- https://developers.google.com/ml-kit/vision/digital-ink-recognition/base-models — idiomas/modelos.
- https://developer.myscript.com/ e https://developer.myscript.com/doc/interactive-ink/4.3/overview/platforms/ — exemplos WPF, certificados e plataformas.
- https://github.com/githubharald/SimpleHTR — README, src/model.py e LICENSE.md examinados.
- https://github.com/kmr-rohit/LstmOnlineHTR — README e src/models/model.py examinados; licença incompletamente verificada.
- https://huggingface.co/microsoft/trocr-base-handwritten — card e pesos.
- https://www.cs.toronto.edu/~graves/icml_2006.pdf — CTC, Graves et al., 2006.
- https://arxiv.org/abs/2202.07036 — comparação de arquiteturas para canetas IMU, não extrapolar resultados.
- https://arxiv.org/abs/2110.05909 — rescoring CTC/sequence-to-sequence.
