# Padrão de documentação de Fórmulas Visuais

Cada fórmula deve ser armazenada em `<Modulo>/<NomeDaFormula>/` e conter os arquivos `README.md` e `formula.txt`.

O conteúdo do `README.md` deve registrar apenas informações confirmadas sobre a fórmula. Quando uma informação não estiver disponível, indique isso explicitamente em vez de inventá-la.

## Modelo de README

```md
# Nome da fórmula

## Objetivo

Descreva o problema resolvido pela fórmula.

## Módulo

Informe o módulo ou processo do RM.

## Processo

Informe o processo em que a fórmula é utilizada.

## Versão do RM

Informe a versão em que a fórmula foi criada ou validada, quando disponível.

## Pré-requisitos

Liste permissões, cadastros, parâmetros ou configurações necessários, quando aplicável.

## Entradas e parâmetros

Descreva as entradas, os parâmetros e os formatos esperados, quando aplicável.

## Código fonte

O código completo está disponível em [formula.txt](formula.txt).

## Como utilizar

Descreva as etapas para configurar ou executar a fórmula.

## Resultado esperado

Descreva o resultado produzido pela fórmula.

## Observações

Registre comportamentos relevantes ou cuidados de uso.

## Limitações

Descreva limitações conhecidas.

## Autor ou contribuidor

Informe o autor ou contribuidor, quando disponível.
```

## Fórmulas com SQL

Quando uma fórmula utilizar SQL, a documentação também deve informar, quando conhecido:

- Banco de dados utilizado
- Tabelas envolvidas
- Finalidade da consulta
- Dependências específicas do ambiente RM

Não inclua credenciais, connection strings com credenciais, dados pessoais reais, dados de clientes ou informações confidenciais. Utilize valores fictícios quando exemplos exigirem esses dados.
