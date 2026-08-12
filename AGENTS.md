# Instruções para Agentes de IA

## Objetivo do projeto

Este é um repositório colaborativo de Fórmulas Visuais para a Linha ERP RM da TOTVS.

## Idioma

Toda documentação deve ser escrita em português do Brasil.

## Organização

As fórmulas devem ser organizadas por módulo ou processo. As categorias principais são:

- Financeiro
- Compras
- Estoque
- Faturamento
- Fiscal
- Contabilidade
- Patrimonio
- Educacional
- RH
- Integracoes
- Utilitarios

## Estrutura de uma fórmula

Cada fórmula deve utilizar a seguinte estrutura:

```text
<Modulo>/<NomeDaFormula>/
├── README.md
└── formula.txt
```

## Documentação obrigatória

O `README.md` de cada fórmula deve informar, quando aplicável:

- Nome
- Objetivo
- Módulo
- Processo
- Versão do RM
- Pré-requisitos
- Entradas/parâmetros
- Código fonte
- Como utilizar
- Resultado esperado
- Observações
- Limitações
- Autor/contribuidor

## Compatibilidade

Nunca assumir que uma fórmula funciona em todas as versões do RM.

Sempre registrar a versão do RM em que a fórmula foi criada ou validada quando essa informação estiver disponível.

## Segurança

Nunca incluir no repositório:

- Senhas
- Tokens
- Chaves de API
- Credenciais
- Connection strings contendo credenciais
- Dados pessoais reais
- Dados de clientes
- Informações confidenciais

Quando um exemplo precisar desses dados, utilizar valores fictícios.

## Qualidade

Não criar fórmulas fictícias apenas para preencher diretórios.

Não alterar uma fórmula existente sem compreender sua finalidade.

Preservar a lógica original quando estiver documentando uma fórmula existente.

## SQL

Quando uma Fórmula Visual utilizar SQL, documentar:

- Banco de dados utilizado, quando conhecido
- Tabelas envolvidas
- Finalidade da consulta
- Dependências específicas do ambiente RM

## Pull Requests

As alterações devem ser pequenas e focadas.

Não misturar reorganização estrutural com alterações funcionais sem necessidade.

## Validação

Antes de finalizar uma alteração:

- Verificar arquivos modificados
- Verificar links relativos
- Verificar sintaxe dos arquivos, quando aplicável
- Revisar o `git diff`
- Garantir que não foram incluídas credenciais ou informações sensíveis

## Regra principal

O repositório deve priorizar clareza, reutilização, documentação e manutenção.

Não inventar informações técnicas sobre uma Fórmula Visual quando elas não estiverem disponíveis.
