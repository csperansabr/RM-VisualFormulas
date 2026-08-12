# Como contribuir

Obrigado por contribuir com a biblioteca de Fórmulas Visuais para a Linha ERP RM da TOTVS.

## Antes de contribuir

- Verifique se já existe uma fórmula para o mesmo objetivo.
- Não inclua fórmulas fictícias apenas para preencher diretórios.
- Não altere uma fórmula existente sem compreender sua finalidade.
- Preserve a lógica original ao documentar uma fórmula existente.
- Nunca inclua dados pessoais reais, dados de clientes, senhas, tokens, chaves de API, credenciais ou informações confidenciais.

## Inclusão de fórmulas

Crie a fórmula dentro do módulo ou processo mais adequado, usando a estrutura abaixo:

```text
<Modulo>/<NomeDaFormula>/
├── README.md
└── formula.txt
```

O arquivo `README.md` deve seguir o [padrão de fórmulas](docs/padrao-formulas.md). Quando a fórmula utilizar SQL, documente o banco de dados, as tabelas, a finalidade da consulta e as dependências específicas do ambiente RM, quando essas informações forem conhecidas.

Não assuma compatibilidade com todas as versões do RM. Registre a versão em que a fórmula foi criada ou validada sempre que essa informação estiver disponível.

## Pull Requests

- Mantenha as alterações pequenas e focadas.
- Não misture reorganização estrutural e alterações funcionais sem necessidade.
- Descreva claramente o objetivo da alteração.
- Informe limitações e pré-requisitos conhecidos.

## Antes de enviar

- Verifique os arquivos modificados.
- Verifique os links relativos da documentação.
- Verifique a sintaxe dos arquivos, quando aplicável.
- Revise o `git diff`.
- Confirme que não foram incluídas credenciais ou informações sensíveis.
