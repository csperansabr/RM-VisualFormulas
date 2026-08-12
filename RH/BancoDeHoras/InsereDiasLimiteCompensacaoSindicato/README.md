# Insere Dias Limite de Compensação Sindicato

## Classificação

- Linha: ERP RM
- Módulo: RH
- Área: Banco de Horas
- Processo: Compensação / Limite de dias
- Tipo: Fórmula Visual C#
- Banco de dados: SQL Server

## Objetivo

Esta fórmula automatiza a criação de uma nova vigência do limite de dias de compensação sindical utilizado pelo Banco de Horas.

## Versão do RM

Versão do RM: não informada no código-fonte original.

## Tabelas utilizadas

### APARCOL

Armazena os parâmetros de coligada avaliados pela consulta. São utilizados `CODCOLIGADA`, `CODIGO`, `DESCRICAO` e `USABANCOHORAS`.

### ALIMBANCOHOR

Armazena as vigências de limite de compensação do Banco de Horas. A consulta utiliza `CODCOLIGADA`, `CODPARCOL`, `LIMDIASCOMPENSACAO` e `DATAINICIO`; os registros aplicáveis são inseridos nessa tabela.

### APERIODO

Fornece o período mensal ativo utilizado para comparar a nova vigência calculada. São utilizados `CODCOLIGADA`, `ATIVO` e `INICIOMENSAL`.

## Regras de negócio

1. A execução somente ocorre antes do dia 21.
2. A partir do dia 21 a execução é ignorada.
3. A consulta avalia parâmetros da `APARCOL`.
4. As vigências existentes são consultadas em `ALIMBANCOHOR`.
5. A nova vigência é calculada pela regra SQL existente no código-fonte.
6. A nova vigência é comparada com o período mensal ativo.
7. Registros aplicáveis são inseridos em `ALIMBANCOHOR`.

## Fluxo

```text
Início
  → valida data
  → consulta parâmetros
  → verifica registros
  → processa registros
  → insere vigência
  → registra sucesso/erro
  → gera resumo
  → envia e-mail
```

## Tratamento de erros

Cada registro é processado individualmente em um bloco `try/catch`. Uma falha ao processar ou inserir um registro não interrompe necessariamente o processamento dos demais registros.

## Log

A fórmula registra no campo `LogProcess` as etapas de execução, os registros encontrados, os resultados de inserção e os erros. O mesmo conteúdo é gerado como um arquivo TXT, anexado ao e-mail com o nome `LOG_LIMITE_COMPENSACAO_SINDICATO_*.txt`.

## E-mail

A fórmula envia um resumo em HTML e anexa o arquivo de log. O envio utiliza `RMSMailParams`, `RMSMailSenderParams`, `RMSBroker` e `IRMSMailServer`.

## Pontos de atenção

1. A consulta possui atualmente a restrição `WHERE A.CODCOLIGADA = 1`.
2. O campo `USABANCOHORAS` é consultado, mas não é utilizado diretamente na lógica C#.
3. `mailParams.CodSistema` está definido como `"P"` e deve ser validado no ambiente de destino.
4. A fórmula deve ser validada em homologação antes da produção.

## Código fonte

O código original está em [formula.cs](formula.cs).

## Como utilizar

Configure a Fórmula Visual no processo aplicável de Banco de Horas, preservando os componentes e parâmetros esperados pelo código original, incluindo `DBS`, `LogProcess` e `mail`. Valide o comportamento em homologação antes de utilizá-la em produção.

## Resultado esperado

Antes do dia 21, a fórmula avalia os parâmetros aplicáveis e insere novas vigências de limite de dias de compensação quando a regra SQL determinar. Ao final, registra o resumo e envia o e-mail com o log anexado.

## Observações

O código foi publicado sem refatoração, com a lógica original preservada.

## Limitações

O código-fonte original não informa a versão do RM e contém uma restrição fixa de coligada. A aplicabilidade em outros ambientes deve ser validada.

## Autor

Cleiton Speransa de Souza
