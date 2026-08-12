Exit code: 0
Wall time: 1.1 seconds
Output:
private void SaldoFeriasGerentesLoja_ExecuteCode(object sender, System.EventArgs args)
{
    System.Text.StringBuilder log = new System.Text.StringBuilder();

    // Configurações obrigatórias do template: substitua pelos valores do ambiente de destino.
    int codColigadaExcluida = 9999;
    string codigoFuncaoGerente = "0000";
    string chapaExcluidaExemplo1 = "000000";
    string chapaExcluidaExemplo2 = "000001";
    int codFilialExcluida = 9999;
    int codColigadaGestorFilial = 9998;
    int codFilialGestor = 9998;
    int codColigadaGestor1 = 9997;
    int codColigadaGestor2 = 9996;
    string prefixoEmailFallback = "ger";
    string dominioEmailFallback = "empresa.exemplo";
    string emailDpExemplo = "EMAIL_DP_EXEMPLO@empresa.exemplo";

    log.AppendLine("**** INICIADO O PROCESSO - SALDO FÉRIAS GERENTES LOJA - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " ****");
    log.AppendLine("---------------------------------------------------------------------------------------------");

    // 1 - Só executa nos dias 1 ou 16 do mês (mesma condição das demais Fórmulas Visuais quinzenais)
    int diaAtual = DateTime.Now.Day;
    if (diaAtual != 1 && diaAtual != 16)
    {
        log.AppendLine(string.Format("Execução ignorada: hoje é dia {0}, e este processo só deve rodar nos dias 1 ou 16.", diaAtual));
        log.AppendLine("**** FIM DO PROCESSO - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " ****");
        this.Message.Value = log.ToString();
        return;
    }

    // 2 - Consulta o saldo de férias dos Gerentes de Loja pela função configurada, já com o gestor calculado.
    //     Ajuste em relação à query original: propaguei CL.DIAS_PARA_O_DOBRO até o SELECT final como
    //     DIASPARAODOBRO — a query recebida calculava esse valor na CTE CALCULO_LIMITES mas não o
    //     repassava adiante, e o HTML de e-mail espera essa coluna (coluna "Dias Dobro" da tabela).
    string query = string.Format(@"
        ;WITH
        -- 1. CTE para dados base do funcionário
        FUNCIONARIO_BASE AS (
            SELECT
                P.CODCOLIGADA,
                P.CHAPA,
                P.NOME,
                P.CODFILIAL,
                P.CODSITUACAO,
                P.CODFUNCAO,
                P.CODPESSOA,
                P.CODSECAO,
                P.CODTIPO,
                FN.NOME AS FUNCAO_NOME,
                S.CODFILIAL AS COD_FILIAL_SECAO
            FROM PFUNC P WITH(NOLOCK)
            INNER JOIN PFUNCAO FN WITH(NOLOCK)
                ON FN.CODCOLIGADA = P.CODCOLIGADA
                AND FN.CODIGO = P.CODFUNCAO
            INNER JOIN PSECAO S WITH(NOLOCK)
                ON S.CODCOLIGADA = P.CODCOLIGADA
                AND S.CODIGO = P.CODSECAO
            WHERE P.CODCOLIGADA <> {0}
                AND P.CODFUNCAO = '{1}'
                AND P.CODTIPO <> 'D'
                AND P.CODSITUACAO <> 'D'
                AND P.CHAPA NOT IN ('{2}', '{3}')
                AND S.CODFILIAL <> {4}
        ),

        -- 2. CTE para dados de férias
        FERIAS_BASE AS (
            SELECT
                F.CODCOLIGADA,
                F.CHAPA,
                F.SALDO,
                F.INICIOPERAQUIS,
                F.FIMPERAQUIS,
                F.PERIODOABERTO,
                DATEADD(YEAR, 1, F.FIMPERAQUIS) AS FIM_MAIS_1ANO,
                F.FIMPERAQUIS + 1 AS INICIO_PERIODO_CALCULO
            FROM PFUFERIAS F WITH(NOLOCK)
            WHERE F.PERIODOABERTO = 1
                AND F.SALDO > 0
        ),

        -- 3. CTE para cálculo de afastamentos
        AFASTAMENTOS AS (
            SELECT
                H.CODCOLIGADA,
                H.CHAPA,
                H.FIMPERAQUIS,
                H.INICIO_PERIODO_CALCULO,
                H.FIM_MAIS_1ANO,
                SUM(
                    CASE
                        WHEN A.DTINICIO >= H.INICIO_PERIODO_CALCULO
                            AND A.DTFINAL <= H.FIM_MAIS_1ANO
                        THEN DATEDIFF(DAY, A.DTINICIO, A.DTFINAL) + 1
                        WHEN A.DTINICIO <= H.INICIO_PERIODO_CALCULO
                            AND A.DTFINAL <= H.FIM_MAIS_1ANO
                        THEN DATEDIFF(DAY, H.INICIO_PERIODO_CALCULO, A.DTFINAL) + 1
                        WHEN A.DTINICIO <= H.INICIO_PERIODO_CALCULO
                            AND A.DTFINAL >= H.FIM_MAIS_1ANO
                        THEN DATEDIFF(DAY, H.INICIO_PERIODO_CALCULO, H.FIM_MAIS_1ANO) + 1
                        WHEN A.DTINICIO >= H.INICIO_PERIODO_CALCULO
                            AND A.DTFINAL >= H.FIM_MAIS_1ANO
                        THEN DATEDIFF(DAY, A.DTINICIO, A.DTFINAL)
                        ELSE 0
                    END
                ) AS TOTAL_DIAS_AFASTAMENTO
            FROM FERIAS_BASE H
            LEFT JOIN PFHSTAFT A WITH(NOLOCK)
                ON A.CODCOLIGADA = H.CODCOLIGADA
                AND A.CHAPA = H.CHAPA
                AND A.TIPO IN ('I', 'L', 'M', 'P', 'T', 'Q', 'E', 'W')
                AND (A.DTFINAL >= H.INICIO_PERIODO_CALCULO OR A.DTFINAL IS NULL)
                AND (
                    A.DTINICIO BETWEEN H.INICIO_PERIODO_CALCULO AND H.FIM_MAIS_1ANO
                    OR A.DTFINAL BETWEEN H.INICIO_PERIODO_CALCULO AND H.FIM_MAIS_1ANO
                    OR A.DTFINAL IS NULL
                )
            GROUP BY
                H.CODCOLIGADA,
                H.CHAPA,
                H.FIMPERAQUIS,
                H.INICIO_PERIODO_CALCULO,
                H.FIM_MAIS_1ANO
        ),

        -- 4. CTE para férias programadas
        FERIAS_PROGRAMADAS AS (
            SELECT
                FP.CODCOLIGADA,
                FP.CHAPA,
                FP.FIMPERAQUIS,
                SUM(FP.NRODIASFERIAS) AS TOTAL_DIAS_PROGRAMADOS,
                MAX(FP.DATAINICIO) AS DATA_INICIO_PROG,
                MAX(FP.DATAFIM) AS DATA_FIM_PROG
            FROM PFUFERIASPER FP WITH(NOLOCK)
            WHERE FP.SITUACAOFERIAS IN ('M', 'G', 'D', 'P')
                OR FP.SITUACAOFERIAS IS NULL
            GROUP BY
                FP.CODCOLIGADA,
                FP.CHAPA,
                FP.FIMPERAQUIS
        ),

        -- 5. CTE para cálculo do saldo e períodos
        CALCULO_SALDO AS (
            SELECT
                FB.CODCOLIGADA,
                FB.CHAPA,
                FB.FIMPERAQUIS,
                FB.INICIOPERAQUIS,
                FB.SALDO,
                FB.FIM_MAIS_1ANO,
                FB.INICIO_PERIODO_CALCULO,
                CASE
                    WHEN FB.FIMPERAQUIS > GETDATE() THEN
                        ((CASE
                            WHEN DATEPART(DAY, FB.FIMPERAQUIS) < 15
                            THEN DATEDIFF(MONTH, DATEADD(YEAR, -1, FB.FIMPERAQUIS), GETDATE()) + 1
                            ELSE DATEDIFF(MONTH, DATEADD(YEAR, -1, FB.FIMPERAQUIS), GETDATE())
                        END) * 2.5) +
                        CASE
                            WHEN FB.FIMPERAQUIS > GETDATE() THEN 0
                            ELSE 30 - ISNULL(FP.TOTAL_DIAS_PROGRAMADOS, 0)
                        END
                    ELSE 30 - ISNULL(FP.TOTAL_DIAS_PROGRAMADOS, 0)
                END AS SALDO_CALCULADO,
                ((CASE
                    WHEN DATEPART(DAY, FB.FIMPERAQUIS) < 15
                    THEN DATEDIFF(MONTH, DATEADD(YEAR, -1, FB.FIMPERAQUIS), GETDATE()) + 1
                    ELSE DATEDIFF(MONTH, DATEADD(YEAR, -1, FB.FIMPERAQUIS), GETDATE())
                END) * 2.5) +
                CASE
                    WHEN FB.FIMPERAQUIS > GETDATE() THEN 0
                    ELSE 30 - ISNULL(FP.TOTAL_DIAS_PROGRAMADOS, 0)
                END AS SALDO_TOTAL_CALC,
                FP.DATA_INICIO_PROG,
                FP.DATA_FIM_PROG,
                FP.TOTAL_DIAS_PROGRAMADOS
            FROM FERIAS_BASE FB
            LEFT JOIN FERIAS_PROGRAMADAS FP
                ON FP.CODCOLIGADA = FB.CODCOLIGADA
                AND FP.CHAPA = FB.CHAPA
                AND FP.FIMPERAQUIS = FB.FIMPERAQUIS
        ),

        -- 6. CTE para cálculo do LIMITEGOZO e DIASPARAODOBRO
        CALCULO_LIMITES AS (
            SELECT
                CS.*,
                AF.TOTAL_DIAS_AFASTAMENTO,
                CASE
                    WHEN CS.SALDO_CALCULADO = 0 THEN
                        (CS.FIM_MAIS_1ANO + ISNULL(AF.TOTAL_DIAS_AFASTAMENTO, 0))
                        - CS.SALDO_TOTAL_CALC + 1
                    ELSE
                        (CS.FIM_MAIS_1ANO + ISNULL(AF.TOTAL_DIAS_AFASTAMENTO, 0))
                        - CS.SALDO_TOTAL_CALC + 1
                END AS LIMITE_GOZO,
                CASE
                    WHEN CS.SALDO_CALCULADO = 0 THEN
                        DATEDIFF(DAY, GETDATE(),
                            (CS.FIM_MAIS_1ANO + ISNULL(AF.TOTAL_DIAS_AFASTAMENTO, 0))
                            - CS.SALDO_TOTAL_CALC + 1
                        )
                    ELSE
                        DATEDIFF(DAY, GETDATE(),
                            (CS.FIM_MAIS_1ANO + ISNULL(AF.TOTAL_DIAS_AFASTAMENTO, 0))
                            - CS.SALDO_TOTAL_CALC + 1
                        )
                END AS DIAS_PARA_O_DOBRO
            FROM CALCULO_SALDO CS
            LEFT JOIN AFASTAMENTOS AF
                ON AF.CODCOLIGADA = CS.CODCOLIGADA
                AND AF.CHAPA = CS.CHAPA
                AND AF.FIMPERAQUIS = CS.FIMPERAQUIS
        ),

        -- 7. CTE para dados dos gestores
        GESTORES AS (
            SELECT
                VP.CODPESSOA,
                VP.CHAPA_CHEFE,
                P1.NOME AS NOME_GESTOR,
                PC.EMAIL_CORPORATIVO AS EMAIL_GESTOR
            FROM VPCOMPL VP WITH(NOLOCK)
            LEFT JOIN PFUNC P1 WITH(NOLOCK)
                ON P1.CHAPA = VP.CHAPA_CHEFE
            LEFT JOIN PFCOMPL PC WITH(NOLOCK)
                ON PC.CODCOLIGADA = P1.CODCOLIGADA
                AND PC.CHAPA = VP.CHAPA_CHEFE
        ),

        -- 8. CTE para consolidação final dos dados
        DADOS_CONSOLIDADOS AS (
            SELECT
                FB.CODCOLIGADA,
                FB.CODFILIAL,
                FB.CHAPA,
                FB.NOME,
                FB.CODSITUACAO,
                FB.FUNCAO_NOME AS FUNCAO,
                FORMAT(CL.INICIOPERAQUIS, 'dd/MM/yyyy') AS INICIOPERAQUIS,
                FORMAT(CL.FIMPERAQUIS, 'dd/MM/yyyy') AS FIMPERAQUIS,
                FORMAT(CL.DATA_INICIO_PROG, 'dd/MM/yyyy') AS INICIOPROG,
                FORMAT(CL.DATA_FIM_PROG, 'dd/MM/yyyy') AS FIMPROG,
                CL.LIMITE_GOZO AS LIMITEGOZO,
                CL.SALDO_CALCULADO AS SALDOCALCULADO,
                CL.SALDO_TOTAL_CALC AS SALDOTOTALCALC,
                CL.DIAS_PARA_O_DOBRO AS DIASPARAODOBRO,
                G.CHAPA_CHEFE AS CHAPA_GESTOR,
                G.NOME_GESTOR,
                CASE
                    WHEN (FB.CODCOLIGADA = {5} AND FB.COD_FILIAL_SECAO = {6})
                         OR FB.CODCOLIGADA IN ({7}, {8})
                    THEN G.EMAIL_GESTOR
                    ELSE '{9}' + CONVERT(VARCHAR, FB.COD_FILIAL_SECAO) + '@{10}'
                END AS EMAIL_GESTOR
            FROM FUNCIONARIO_BASE FB
            INNER JOIN CALCULO_LIMITES CL
                ON CL.CODCOLIGADA = FB.CODCOLIGADA
                AND CL.CHAPA = FB.CHAPA
            LEFT JOIN GESTORES G
                ON G.CODPESSOA = FB.CODPESSOA
        )

        -- 9. Seleção final
        SELECT
            CODCOLIGADA,
            CODFILIAL,
            CHAPA,
            NOME,
            CODSITUACAO,
            FUNCAO,
            INICIOPERAQUIS,
            FIMPERAQUIS,
            INICIOPROG,
            FIMPROG,
            FORMAT(LIMITEGOZO, 'dd/MM/yyyy') AS LIMITEGOZO,
            SALDOCALCULADO,
            SALDOTOTALCALC,
            DIASPARAODOBRO,
            CHAPA_GESTOR,
            NOME_GESTOR,
            EMAIL_GESTOR
        FROM DADOS_CONSOLIDADOS
        ORDER BY CODFILIAL, NOME",
        codColigadaExcluida,
        codigoFuncaoGerente.Replace("'", "''"),
        chapaExcluidaExemplo1.Replace("'", "''"),
        chapaExcluidaExemplo2.Replace("'", "''"),
        codFilialExcluida,
        codColigadaGestorFilial,
        codFilialGestor,
        codColigadaGestor1,
        codColigadaGestor2,
        prefixoEmailFallback.Replace("'", "''"),
        dominioEmailFallback.Replace("'", "''"));

    DataTable regs = this.DBS.QuerySelect("PFUNC", query);

    if (regs == null || regs.Rows.Count == 0)
    {
        log.AppendLine("Nenhum Gerente de Loja com período de férias aberto encontrado.");
        log.AppendLine("**** FIM DO PROCESSO - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " ****");
        this.Message.Value = log.ToString();
        EnviarEmailResumoSaldoFerias(log, 0, 0, 0, true);
        return;
    }

    log.AppendLine("Gerentes de Loja com período de férias aberto: " + regs.Rows.Count);
    log.AppendLine();

    // 3 - Agrupa os gerentes por EMAIL_GESTOR (gestor corporativo ou e-mail de filial de fallback)
    Dictionary<string, List<DataRow>> porEmailGestor = new Dictionary<string, List<DataRow>>();

    foreach (DataRow row in regs.Rows)
    {
        string emailGestor = row["EMAIL_GESTOR"] != DBNull.Value ? row["EMAIL_GESTOR"].ToString().Trim() : string.Empty;

        // A query sempre gera um e-mail de fallback por filial, exceto quando o gestor
        // corporativo está previsto pelas configurações de coligada e filial, mas não tem
        // e-mail corporativo cadastrado — fica registrado como inconsistência de cadastro.
        if (string.IsNullOrEmpty(emailGestor))
            emailGestor = "SEM_EMAIL_IDENTIFICADO";

        if (!porEmailGestor.ContainsKey(emailGestor))
            porEmailGestor[emailGestor] = new List<DataRow>();

        porEmailGestor[emailGestor].Add(row);
    }

    log.AppendLine("Destinatários (gestor/filial) identificados: " + porEmailGestor.Keys.Count);
    log.AppendLine();

    int totalEmailsEnviados = 0;
    int totalErros = 0;

    // 4 - Envia um e-mail por gestor, com cópia para a equipe interna configurada, listando
    //     os Gerentes de Loja sob sua responsabilidade
    foreach (string emailGestor in porEmailGestor.Keys)
    {
        List<DataRow> gerentes = porEmailGestor[emailGestor];

        string nomeGestor = gerentes[0]["NOME_GESTOR"] != DBNull.Value ? gerentes[0]["NOME_GESTOR"].ToString() : string.Empty;
        string chapaGestor = gerentes[0]["CHAPA_GESTOR"] != DBNull.Value ? gerentes[0]["CHAPA_GESTOR"].ToString() : "N/A";

        log.AppendLine(string.Format("[GESTOR] Chapa: {0} | Nome: {1} | E-mail: {2} | Gerentes: {3}",
            chapaGestor, string.IsNullOrEmpty(nomeGestor) ? "(não identificado)" : nomeGestor, emailGestor, gerentes.Count));

        foreach (DataRow gerente in gerentes)
        {
            log.AppendLine(string.Format("  - Filial: {0} | Chapa: {1} | Nome: {2} | Saldo: {3} | Limite Gozo: {4} | Dias p/ Dobro: {5}",
                gerente["CODFILIAL"], gerente["CHAPA"], gerente["NOME"], gerente["SALDOCALCULADO"], gerente["LIMITEGOZO"], gerente["DIASPARAODOBRO"]));
        }

        if (emailGestor == "SEM_EMAIL_IDENTIFICADO")
        {
            totalErros++;
            log.AppendLine("  [ERRO] Sem e-mail de destino identificado — e-mail não enviado para este grupo.");
            log.AppendLine();
            continue;
        }

        try
        {
            EnviarEmailGestorSaldoFerias(emailGestor, nomeGestor, gerentes);
            totalEmailsEnviados++;
            log.AppendLine(string.Format("  [E-MAIL ENVIADO] Para: {0} | CC: {1}", emailGestor, emailDpExemplo));
        }
        catch (Exception ex)
        {
            totalErros++;
            log.AppendLine(string.Format("  [ERRO] Falha ao enviar e-mail para {0}: {1}", emailGestor, ex.Message));
        }

        log.AppendLine();
    }

    log.AppendLine();
    log.AppendLine("------ RESUMO ------");
    log.AppendLine("Total de Gerentes de Loja avaliados:    " + regs.Rows.Count);
    log.AppendLine("Total de gestores notificados:          " + totalEmailsEnviados);
    log.AppendLine("Total de erros no envio:                " + totalErros);
    log.AppendLine("**** FIM DO PROCESSO - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " ****");

    this.Message.Value = log.ToString();

    // 5 - Envia o e-mail de resumo para a equipe, com o log completo anexado
    EnviarEmailResumoSaldoFerias(log, regs.Rows.Count, totalEmailsEnviados, totalErros, false);
}

// -----------------------------------------------------------
// Envia o e-mail para um gestor (ou e-mail de filial de fallback), listando os Gerentes de
// Loja sob sua responsabilidade, com cópia para a equipe interna configurada.
// -----------------------------------------------------------
private void EnviarEmailGestorSaldoFerias(string emailDestino, string nomeGestor, List<DataRow> gerentes)
{
    RMSMailParams mailParams = new RMSMailParams();
    mailParams.CodSistema = "P"; // TODO: ajustar para o código do sistema/módulo correto do ambiente
    mailParams.CodColigada = this.Context.CodColigada;
    mailParams.CodUsuario = this.Context.CodUsuario;
    mailParams.Sender = !string.IsNullOrEmpty(this.mail.SenderAddress)
        ? new RMSMailSenderParams(this.mail.Sender, this.mail.SenderAddress, this.mail.SenderAddress)
        : new RMSMailSenderParams(this.mail.Sender);

    mailParams.To.Add(emailDestino);
    mailParams.CC.Add(emailDpExemplo);

    mailParams.Subject = "Envio de Relatório de Férias - Gerentes de Loja";
    mailParams.Body = MontarEmailHtmlSaldoFerias(nomeGestor, gerentes);
    mailParams.IsHTMLBody = true;
    mailParams.Timeout = int.MaxValue;
    mailParams.Assync = this.mail.SendAsyncMail;

    using (IRMSMailServer server = RMSBroker.CreateServer<IRMSMailServer>("RMSMailServer"))
        server.Send(mailParams);
}

// -----------------------------------------------------------
// Corpo HTML do e-mail individual, com a tabela de saldo de férias dos Gerentes de Loja
// -----------------------------------------------------------
private string MontarEmailHtmlSaldoFerias(string nomeGestor, List<DataRow> gerentes)
{
    System.Text.StringBuilder html = new System.Text.StringBuilder();

    html.AppendLine("<html><body style='font-family:Arial; font-size:13px; color:#333; max-width:1100px; margin:0 auto;'>");

    html.AppendLine("  <div style='background-color:#003366; padding:16px 24px; border-radius:6px 6px 0 0;'>");
    html.AppendLine("    <h2 style='color:#ffffff; margin:0; font-size:17px;'>Envio de Relatório de Férias - Gerentes de Loja</h2>");
    html.AppendLine("  </div>");

    html.AppendLine("  <div style='border:1px solid #dee2e6; border-top:none; padding:24px; border-radius:0 0 6px 6px;'>");

    string saudacao = !string.IsNullOrEmpty(nomeGestor) ? nomeGestor : "Gestor(a)";
    html.AppendLine("    <p style='margin:0 0 20px 0;'>Prezado(a), <strong>" + saudacao + "</strong><br/><br/>Segue lista de Gerentes e suas respectivas férias:</p>");

    html.AppendLine("    <div style='overflow-x:auto;'>");
    html.AppendLine("    <table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse; font-size:11px; width:100%; margin-bottom:20px;'>");
    html.AppendLine("      <thead>");
    html.AppendLine("        <tr style='background-color:#003366; color:#ffffff; text-align:center;'>");
    html.AppendLine("          <th>Coligada</th>");
    html.AppendLine("          <th>Filial</th>");
    html.AppendLine("          <th>Matrícula</th>");
    html.AppendLine("          <th style='text-align:left;'>Nome</th>");
    html.AppendLine("          <th>Cod. Situação</th>");
    html.AppendLine("          <th>Início Per. Aquisitivo</th>");
    html.AppendLine("          <th>Fim Per. Aquisitivo</th>");
    html.AppendLine("          <th>Início Programação</th>");
    html.AppendLine("          <th>Fim Programação</th>");
    html.AppendLine("          <th>Saldo</th>");
    html.AppendLine("          <th>Limite de Gozo</th>");
    html.AppendLine("          <th>Dias Dobro</th>");
    html.AppendLine("          <th style='text-align:left;'>Função</th>");
    html.AppendLine("        </tr>");
    html.AppendLine("      </thead>");
    html.AppendLine("      <tbody>");

    int idx = 0;
    foreach (DataRow gerente in gerentes)
    {
        string fundo = (idx % 2 == 0) ? "#ffffff" : "#f2f7ff";
        string diasParaODobro = gerente["DIASPARAODOBRO"] != DBNull.Value ? gerente["DIASPARAODOBRO"].ToString() : string.Empty;

        int diasParaODobroNum;
        bool alertaDobro = int.TryParse(diasParaODobro, out diasParaODobroNum) && diasParaODobroNum <= 60;
        string corDobro = alertaDobro ? "#721c24; font-weight:bold;" : "inherit;";

        html.AppendLine("        <tr style='background-color:" + fundo + ";'>");
        html.AppendLine("          <td style='text-align:center;'>" + Valor(gerente, "CODCOLIGADA") + "</td>");
        html.AppendLine("          <td style='text-align:center;'>" + Valor(gerente, "CODFILIAL") + "</td>");
        html.AppendLine("          <td style='text-align:center;'>" + Valor(gerente, "CHAPA") + "</td>");
        html.AppendLine("          <td>" + Valor(gerente, "NOME") + "</td>");
        html.AppendLine("          <td style='text-align:center;'>" + Valor(gerente, "CODSITUACAO") + "</td>");
        html.AppendLine("          <td style='text-align:center;'>" + Valor(gerente, "INICIOPERAQUIS") + "</td>");
        html.AppendLine("          <td style='text-align:center;'>" + Valor(gerente, "FIMPERAQUIS") + "</td>");
        html.AppendLine("          <td style='text-align:center;'>" + Valor(gerente, "INICIOPROG") + "</td>");
        html.AppendLine("          <td style='text-align:center;'>" + Valor(gerente, "FIMPROG") + "</td>");
        html.AppendLine("          <td style='text-align:center;'>" + Valor(gerente, "SALDOCALCULADO") + "</td>");
        html.AppendLine("          <td style='text-align:center;'>" + Valor(gerente, "LIMITEGOZO") + "</td>");
        html.AppendLine("          <td style='text-align:center; color:" + corDobro + "'>" + diasParaODobro + "</td>");
        html.AppendLine("          <td>" + Valor(gerente, "FUNCAO") + "</td>");
        html.AppendLine("        </tr>");

        idx++;
    }

    html.AppendLine("      </tbody>");
    html.AppendLine("    </table>");
    html.AppendLine("    </div>");

    html.AppendLine("    <hr style='border:none; border-top:1px solid #dee2e6; margin:24px 0 16px 0;'/>");
    html.AppendLine("    <p style='margin:0 0 2px 0;'>Atenciosamente,</p>");
    html.AppendLine("    <p style='margin:0;'>Departamento Pessoal</p>");

    html.AppendLine("    <div style='margin-top:20px; padding:10px 14px; background-color:#f8f9fa; border-radius:4px;'>");
    html.AppendLine("      <p style='margin:0; color:#6c757d; font-size:11px;'><i>Obs.: Este é um e-mail automático. Em caso de dúvidas, contate o setor de Férias.</i></p>");
    html.AppendLine("    </div>");

    html.AppendLine("  </div>");
    html.AppendLine("</body></html>");

    return html.ToString();
}
// Lê um campo do DataRow com segurança contra DBNull, evitando repetir a checagem em cada célula da tabela
private string Valor(DataRow row, string coluna)
{
    return row[coluna] != DBNull.Value ? row[coluna].ToString() : string.Empty;
}

// -----------------------------------------------------------
// Envia o e-mail de resumo (para a equipe interna) com o log completo anexado, no mesmo
// padrão (RMSMailParams / RMSBroker / IRMSMailServer / this.mail.*) usado nos demais processos.
// -----------------------------------------------------------
private void EnviarEmailResumoSaldoFerias(System.Text.StringBuilder log, int totalGerentes, int totalEnviados, int totalErros, bool semRegistros)
{
    List<string> listaTo = new List<string>();
    if (this.mail.To != null)
        listaTo.AddRange((IEnumerable<string>)this.mail.To);

    List<string> listaCc = new List<string>();
    if (this.mail.Cc != null)
        listaCc.AddRange((IEnumerable<string>)this.mail.Cc);

    List<string> listaBcc = new List<string>();
    if (this.mail.Bcc != null)
        listaBcc.AddRange((IEnumerable<string>)this.mail.Bcc);

    byte[] logBytes = System.Text.Encoding.UTF8.GetBytes(log.ToString());
    string nomeAnexo = "LOG_SALDO_FERIAS_GERENTES_LOJA_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";

    RMSMailParams mailParams = new RMSMailParams();
    mailParams.CodSistema = "P"; // TODO: ajustar para o código do sistema/módulo correto do ambiente
    mailParams.CodColigada = this.Context.CodColigada;
    mailParams.CodUsuario = this.Context.CodUsuario;
    mailParams.Sender = !string.IsNullOrEmpty(this.mail.SenderAddress)
        ? new RMSMailSenderParams(this.mail.Sender, this.mail.SenderAddress, this.mail.SenderAddress)
        : new RMSMailSenderParams(this.mail.Sender);

    mailParams.To.AddRange((IEnumerable<string>)listaTo);
    mailParams.CC.AddRange((IEnumerable<string>)listaCc);
    mailParams.BCC.AddRange((IEnumerable<string>)listaBcc);

    mailParams.Subject = (totalErros > 0 ? "[ATENÇÃO] " : "[RESUMO] ") + "Saldo Férias Gerentes Loja - " + DateTime.Now.ToString("dd/MM/yyyy");
    mailParams.Body = MontarEmailHtmlResumoSaldoFerias(totalGerentes, totalEnviados, totalErros, semRegistros);
    mailParams.IsHTMLBody = true;
    mailParams.Timeout = int.MaxValue;
    mailParams.Assync = this.mail.SendAsyncMail;

    mailParams.Attachments.Add(nomeAnexo, logBytes);

    using (IRMSMailServer server = RMSBroker.CreateServer<IRMSMailServer>("RMSMailServer"))
        server.Send(mailParams);
}

// -----------------------------------------------------------
// Corpo HTML do e-mail de resumo (equipe interna)
// -----------------------------------------------------------
private string MontarEmailHtmlResumoSaldoFerias(int totalGerentes, int totalEnviados, int totalErros, bool semRegistros)
{
    System.Text.StringBuilder html = new System.Text.StringBuilder();

    html.AppendLine("<html><body style='font-family:Arial; font-size:12px; color:#333; max-width:700px; margin:0 auto;'>");

    html.AppendLine("  <div style='background-color:#003366; padding:16px 24px; border-radius:6px 6px 0 0;'>");
    html.AppendLine("    <h2 style='color:#ffffff; margin:0; font-size:17px;'>Resumo — Saldo Férias Gerentes Loja</h2>");
    html.AppendLine("    <p style='color:#cce5ff; margin:4px 0 0 0; font-size:11px;'>Gerado em: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "</p>");
    html.AppendLine("  </div>");

    html.AppendLine("  <div style='border:1px solid #dee2e6; border-top:none; padding:24px; border-radius:0 0 6px 6px;'>");

    if (semRegistros)
    {
        html.AppendLine("  <div style='text-align:center; padding:40px 20px; background-color:#f8f9fa; border:1px solid #dee2e6; border-radius:8px; margin:20px 0;'>");
        html.AppendLine("    <h3 style='color:#495057; margin:0 0 8px 0;'>Nenhum registro encontrado</h3>");
        html.AppendLine("    <p style='color:#6c757d; margin:0;'>Não há Gerentes de Loja com período de férias aberto no momento.</p>");
        html.AppendLine("  </div>");
    }
    else
    {
        html.AppendLine("  <p style='margin:0 0 16px 0;'>Foi enviado um e-mail individual, com cópia para a equipe interna configurada, para cada gestor — ou e-mail de filial, quando não há gestor corporativo identificado — com o saldo de férias dos Gerentes de Loja sob sua responsabilidade. O detalhamento completo está no arquivo de log anexado a este e-mail.</p>");

        if (totalErros > 0)
        {
            html.AppendLine("  <div style='background-color:#f8d7da; border-left:4px solid #721c24; padding:12px 16px; border-radius:4px; margin:0 0 20px 0;'>");
            html.AppendLine("    <p style='margin:0; color:#721c24;'>&#9888;&#65039; <strong>Atenção:</strong> " + totalErros + " e-mail(s) falharam ao enviar ou ficaram sem destinatário identificado. Consulte o log anexado para o motivo de cada caso.</p>");
            html.AppendLine("  </div>");
        }

        html.AppendLine("  <table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse; font-size:12px; width:100%;'>");
        html.AppendLine("    <thead>");
        html.AppendLine("      <tr style='background-color:#003366; color:#ffffff; text-align:center;'>");
        html.AppendLine("        <th>Indicador</th><th>Quantidade</th>");
        html.AppendLine("      </tr>");
        html.AppendLine("    </thead>");
        html.AppendLine("    <tbody>");
        html.AppendLine("      <tr style='background-color:#f8f9fa;'><td><strong>Gerentes de Loja avaliados</strong></td><td style='text-align:center;'>" + totalGerentes + "</td></tr>");
        html.AppendLine("      <tr style='background-color:#d4edda;'><td><strong>Gestores notificados</strong></td><td style='text-align:center;'>" + totalEnviados + "</td></tr>");

        if (totalErros > 0)
            html.AppendLine("      <tr style='background-color:#f8d7da;'><td><strong>&#9888;&#65039; Com erro</strong></td><td style='text-align:center; color:#721c24; font-weight:bold;'>" + totalErros + "</td></tr>");
        else
            html.AppendLine("      <tr style='background-color:#f8f9fa;'><td><strong>Com erro</strong></td><td style='text-align:center;'>0</td></tr>");

        html.AppendLine("    </tbody>");
        html.AppendLine("  </table>");
        html.AppendLine("  <br/>");
        html.AppendLine("  <p style='margin:0; color:#6c757d; font-size:11px;'>&#128206; Consulte o arquivo <strong>LOG_SALDO_FERIAS_GERENTES_LOJA_*.txt</strong> anexado para o detalhamento completo (por gestor e por Gerente de Loja).</p>");
    }

    html.AppendLine("  <p style='color:#999; font-size:10px; margin-top:20px;'>Mensagem gerada automaticamente pelo sistema TOTVS RM.</p>");
    html.AppendLine("  </div>");
    html.AppendLine("</body></html>");

    return html.ToString();
}
