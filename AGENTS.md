# Instruções para Agentes de IA

## Objetivo

O RM-VisualFormulas é uma biblioteca colaborativa de Fórmulas Visuais para a Linha ERP RM da TOTVS.

O objetivo é disponibilizar códigos reutilizáveis, exemplos práticos e documentação para profissionais que trabalham com implantação, suporte, desenvolvimento e consultoria do ERP RM.

## Idioma

Toda documentação deve utilizar português do Brasil.

O código-fonte deve preservar a linguagem original da fórmula.

## Organização

As fórmulas devem ser organizadas inicialmente por módulo:

- RH
- Financeiro
- Compras
- Estoque
- Faturamento
- Fiscal
- Contabilidade
- Patrimonio
- Educacional
- Integracoes
- Utilitarios

Não criar diretórios de módulos que ainda não possuam conteúdo.

Quando houver necessidade de subdivisão, utilizar categorias funcionais. Exemplo:

```text
RH/
└── BancoDeHoras/
    └── NomeDaFormula/
```

## Estrutura da fórmula

Cada fórmula deverá possuir sua própria pasta. A estrutura padrão é:

```text
NomeDaFormula/
├── README.md
└── formula.<extensão>
```

A extensão do código deve refletir a linguagem utilizada, por exemplo: `formula.cs`, `formula.sql` ou `formula.vb`.

## Nome das fórmulas

Utilizar nomes descritivos, objetivos e reutilizáveis.

Não incluir no nome:

- Nome de cliente
- Ano
- Versão específica
- Ambiente
- Número de chamado
- Informações temporárias

## README da fórmula

Toda fórmula deve possuir documentação contendo, quando aplicável:

- Nome
- Classificação
- Linha
- Módulo
- Área
- Processo
- Objetivo
- Versão do RM
- Banco de dados
- Tabelas utilizadas
- Regras de negócio
- Fluxo
- Dependências
- Parâmetros
- Como utilizar
- Resultado esperado
- Tratamento de erros
- Logs
- Integrações
- Pontos de atenção
- Limitações
- Autor

Nunca inventar informações ausentes. Quando a versão do RM não for conhecida, informar explicitamente que ela não foi informada.

## Código-fonte

Preservar a lógica funcional da fórmula quando o objetivo for apenas publicação ou documentação.

Não refatorar código existente sem solicitação explícita.

Não corrigir regras de negócio por iniciativa própria.

Se forem identificadas melhorias, documentá-las como pontos de atenção ou Issues.

Não criar fórmulas fictícias apenas para preencher diretórios.

Não alterar uma fórmula existente sem compreender sua finalidade.

## Segurança

Nunca publicar:

- Senhas
- Tokens
- API keys ou chaves de API
- Connection strings com credenciais
- Dados pessoais
- Dados reais de clientes
- Informações confidenciais
- Arquivos de configuração contendo segredos

Se houver credenciais no código fornecido, interromper a publicação e informar o problema.

Quando um exemplo precisar de dados sensíveis, utilizar valores fictícios.

## Banco de dados e SQL

Quando houver SQL:

- Identificar as tabelas utilizadas
- Documentar a finalidade da consulta
- Identificar o banco de dados, quando conhecido
- Documentar as dependências específicas do ambiente RM, quando conhecidas
- Preservar a consulta original quando a tarefa for apenas publicação
- Não alterar SQL sem solicitação explícita

## TOTVS RM e compatibilidade

Não assumir compatibilidade entre versões do RM.

Sempre registrar a versão em que a fórmula foi criada ou validada quando essa informação estiver disponível.

Não afirmar que determinada API, classe, método ou tabela existe em uma versão sem evidência.

## Revisão e validação

Antes de concluir qualquer alteração:

- Executar `git status`
- Executar `git diff`
- Executar `git diff --check`
- Revisar os arquivos alterados
- Verificar links relativos
- Verificar a sintaxe dos arquivos, quando aplicável
- Procurar possíveis credenciais ou informações sensíveis

## Git

Para novas alterações:

1. Trabalhar em branch própria.
2. Não alterar diretamente `main`.
3. Criar commits pequenos e objetivos.
4. Utilizar mensagens de commit convencionais.
5. Criar Pull Request para `main`.
6. Não fazer merge sem autorização explícita do usuário.

## Pull Requests

As alterações devem ser pequenas e focadas. Não misturar reorganização estrutural com alterações funcionais sem necessidade.

Toda Pull Request deve informar:

- Objetivo
- Classificação
- Arquivos alterados
- Impacto
- Limitações
- Validações realizadas

## Issues

Issues devem ser utilizadas para:

- Melhorias futuras
- Correções
- Dúvidas de regra de negócio
- Compatibilidade
- Problemas encontrados
- Oportunidades de refatoração

Não modificar código funcional apenas para resolver uma dúvida não confirmada.

## Regra principal

Priorizar:

1. Fidelidade à solução original
2. Clareza
3. Documentação
4. Reutilização
5. Segurança
6. Rastreabilidade

Quando houver conflito entre uma possível melhoria e a preservação da regra de negócio, preservar a regra existente e registrar a melhoria para avaliação.
