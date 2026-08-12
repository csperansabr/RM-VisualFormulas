# Saldo de Férias dos Gerentes de Loja

## Classificação

- Linha: ERP RM
- Módulo: RH
- Área: Férias
- Processo: Controle de saldo e limite de gozo
- Público: Gestores / Gerentes de Loja
- Tipo: Fórmula Visual C#
- Banco: SQL Server

## Objetivo

Consulta informações de Gerentes de Loja, períodos de férias, férias programadas e afastamentos para calcular indicadores de saldo, limite de gozo e dias para dobro. A fórmula agrupa os dados por destinatário, envia relatórios HTML individuais e encaminha um resumo operacional com log TXT.

## Periodicidade

A versão original executa somente nos dias 1 e 16. Essa periodicidade pode ser ajustada conforme o ambiente.

## Configurações obrigatórias

Esta versão publicada é um template sanitizado. Os valores de configuração devem ser substituídos antes da utilização em um ambiente real.

No início de [formula.cs](formula.cs), adapte os valores abaixo à regra de negócio do ambiente:

- `codColigadaExcluida`: coligada a excluir da consulta; o exemplo `9999` é fictício.
- `codigoFuncaoGerente`: código da função que identifica Gerentes de Loja; `0000` é fictício.
- `chapaExcluidaExemplo1` e `chapaExcluidaExemplo2`: chapas a excluir, se aplicável; `000000` e `000001` são fictícios.
- `codFilialExcluida`: filial a excluir; `9999` é fictício.
- `codColigadaGestorFilial`, `codFilialGestor`, `codColigadaGestor1` e `codColigadaGestor2`: regras de seleção de e-mail do gestor; `9998`, `9997` e `9996` são exemplos fictícios.
- `prefixoEmailFallback` e `dominioEmailFallback`: padrão do e-mail de fallback por filial. O domínio `empresa.exemplo` é reservado para documentação.
- `emailDpExemplo`: cópia para a equipe interna; `EMAIL_DP_EXEMPLO@empresa.exemplo` é fictício.

## Fluxo

```text
Início → valida dia de execução → identifica Gerentes de Loja → identifica períodos de férias
→ identifica férias programadas → calcula afastamentos → calcula saldo → calcula limite de gozo
→ calcula dias para dobro → identifica gestor → agrupa destinatários → envia relatórios
→ envia resumo → gera log → fim
```

## CTEs

- `FUNCIONARIO_BASE`: seleciona os funcionários pela função configurada e aplica os filtros do template.
- `FERIAS_BASE`: seleciona períodos aquisitivos abertos com saldo positivo.
- `AFASTAMENTOS`: soma os dias de afastamento aplicáveis ao período calculado.
- `FERIAS_PROGRAMADAS`: consolida dias e datas de férias programadas.
- `CALCULO_SALDO`: calcula saldo e saldo total considerando períodos e programação.
- `CALCULO_LIMITES`: calcula limite de gozo e dias para dobro.
- `GESTORES`: obtém gestor, chapa e e-mail corporativo.
- `DADOS_CONSOLIDADOS`: reúne os dados e define o destinatário, inclusive o fallback configurável.

## Tabelas utilizadas

- `PFUNC`: dados funcionais, função, filial e situação.
- `PFUNCAO`: descrição da função.
- `PSECAO`: dados da seção e filial associada.
- `PFUFERIAS`: períodos aquisitivos e saldo de férias.
- `PFUFERIASPER`: férias programadas.
- `PFHSTAFT`: afastamentos.
- `VPCOMPL`: relacionamento com o gestor.
- `PFCOMPL`: e-mail corporativo do gestor.

## Indicadores calculados

- `SALDOCALCULADO`: saldo calculado para o período.
- `SALDOTOTALCALC`: saldo total calculado.
- `LIMITEGOZO`: limite calculado para gozo das férias.
- `DIASPARAODOBRO`: dias restantes para a condição de dobro.

## Comunicação

O envio é individual por destinatário e agrupa os Gerentes de Loja pelo e-mail identificado. Há cópia para a equipe interna configurável, resumo operacional e arquivo TXT com o log. Nenhum endereço real foi publicado.

## Tratamento de erros e logs

Cada envio é protegido individualmente por `try/catch`, permitindo o processamento dos demais grupos após uma falha. O campo `Message` recebe o log e o resumo operacional é enviado com anexo `LOG_SALDO_FERIAS_GERENTES_LOJA_*.txt`.

## Pontos de atenção

1. Há uma aparente contradição entre a exclusão de filial em `FUNCIONARIO_BASE` e a condição posterior de seleção do e-mail do gestor.
2. A CTE `GESTORES` relaciona `PFUNC` por `CHAPA_CHEFE` sem explicitar `CODCOLIGADA`; valide em ambientes multi-coligada.
3. Os `CASE` de `CALCULO_LIMITES` possuem a mesma expressão em `THEN` e `ELSE`.
4. A CTE `AFASTAMENTOS` possui cenários de `DATEDIFF` com e sem acréscimo de um dia; valide a regra de contagem.
5. A consulta usa `WITH(NOLOCK)`; avalie consistência dos dados versus performance.
6. `totalEmailsEnviados` representa e-mails ou grupos enviados, não necessariamente pessoas notificadas.

## Segurança

Valores específicos do ambiente original foram sanitizados para publicação pública, incluindo domínio, e-mail, função, chapas, coligadas e filiais.

## Compatibilidade

Versão do RM: não informada no código-fonte original.

## Limitações

O template exige a substituição das configurações obrigatórias. A fórmula depende do Framework RM e deve ser validada em homologação antes de produção.

## Autor

Cleiton Speransa de Souza
