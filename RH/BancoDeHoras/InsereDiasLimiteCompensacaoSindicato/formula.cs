private void codeActivity1_ExecuteCode(object sender, System.EventArgs args)
{
    System.Text.StringBuilder log = new System.Text.StringBuilder();
    DateTime hoje = DateTime.Now;

    log.AppendLine("**** INICIADO O PROCESSO - INSERE DIAS LIMITE COMPENSAÇÃO SINDICATO - " + hoje.ToString("dd/MM/yyyy HH:mm") + " ****");
    log.AppendLine("---------------------------------------------------------------------------------------------");

    // 1 - Só processa até o dia 20 (antes do fechamento do dia 21)
    if (hoje.Day >= 21)
    {
        log.AppendLine(string.Format("Execução ignorada: hoje é dia {0}. As informações só são processadas antes do dia 21.", hoje.Day));
        log.AppendLine("**** FIM DO PROCESSO - " + hoje.ToString("dd/MM/yyyy HH:mm") + " ****");
        this.LogProcess.Value = log.ToString();
        return;
    }

    string sql = @"
        SELECT CODCOLIGADA, CODIGO, LIMDIASCOMPENSACAO, DESCRICAO, USABANCOHORAS,
               ULTDATAINICIOCOMP, NOVADATA, MAXDATAINICIO, NOVOLIMDIASCOMPENSACAO
        FROM
        (
            SELECT
                A.CODCOLIGADA,
                A.CODIGO,
                A.DESCRICAO,
                A.USABANCOHORAS,
                MAX(B.LIMDIASCOMPENSACAO) LIMDIASCOMPENSACAO,
                (
                    SELECT MAX(DATAINICIO)
                    FROM ALIMBANCOHOR
                    WHERE CODCOLIGADA=A.CODCOLIGADA
                      AND CODPARCOL=A.CODIGO
                      AND LIMDIASCOMPENSACAO>0
                ) ULTDATAINICIOCOMP,
                Max(B.DATAINICIO) MAXDATAINICIO,
                DATEADD(
                    D,
                    (30+(21-DAY(DATEADD(D,30,Max(B.DATAINICIO))))),
                    Max(B.DATAINICIO)
                ) NOVADATA,
                CASE
                    WHEN DATEDIFF(
                        D,
                        (
                            SELECT MAX(DATAINICIO)
                            FROM ALIMBANCOHOR
                            WHERE CODCOLIGADA=A.CODCOLIGADA
                              AND CODPARCOL=A.CODIGO
                              AND LIMDIASCOMPENSACAO>0
                        ),
                        DATEADD(
                            D,
                            (30+(21-DAY(DATEADD(D,30,Max(B.DATAINICIO))))),
                            Max(B.DATAINICIO)
                        )
                    ) >= MAX(B.LIMDIASCOMPENSACAO)
                    THEN MAX(B.LIMDIASCOMPENSACAO)
                    ELSE 0
                END NOVOLIMDIASCOMPENSACAO
            FROM APARCOL A
            LEFT JOIN ALIMBANCOHOR B
                ON (
                    B.CODCOLIGADA=A.CODCOLIGADA
                    AND B.CODPARCOL=A.CODIGO
                )
            WHERE A.CODCOLIGADA=1
            GROUP BY
                A.CODCOLIGADA,
                A.CODIGO,
                A.DESCRICAO,
                A.USABANCOHORAS
        ) DADOS
        WHERE DADOS.NOVADATA <=
        (
            SELECT INICIOMENSAL
            FROM APERIODO
            WHERE CODCOLIGADA=DADOS.CODCOLIGADA
              AND ATIVO=1
        )";

    DataTable dtDados = this.DBS.QuerySelect("ALIMBANCOHOR", sql);

    if (dtDados == null || dtDados.Rows.Count == 0)
    {
        log.AppendLine("Nenhum registro encontrado na consulta SQL.");
        log.AppendLine("**** FIM DO PROCESSO - " + hoje.ToString("dd/MM/yyyy HH:mm") + " ****");
        this.LogProcess.Value = log.ToString();

        EnviarEmailResumoLimiteCompensacao(log, 0, 0, 0, true);
        return;
    }

    log.AppendLine("Registros encontrados: " + dtDados.Rows.Count);
    log.AppendLine();

    int totalInseridos = 0;
    int totalErros = 0;

    foreach (DataRow row in dtDados.Rows)
    {
        string codParcol = row["CODIGO"] != DBNull.Value
            ? row["CODIGO"].ToString()
            : string.Empty;

        string descricao = row["DESCRICAO"] != DBNull.Value
            ? row["DESCRICAO"].ToString()
            : string.Empty;

        log.AppendLine(new string('=', 100));

        try
        {
            int codColigada = Convert.ToInt32(row["CODCOLIGADA"]);
            int limDiasComp = Convert.ToInt32(row["NOVOLIMDIASCOMPENSACAO"]);
            DateTime novaData = Convert.ToDateTime(row["NOVADATA"]);
            DateTime maxDataInicio = Convert.ToDateTime(row["MAXDATAINICIO"]);

            log.AppendLine(string.Format(
                "CODCOLIGADA: {0} | CODIGO: {1} | DESCRICAO: {2}",
                codColigada,
                codParcol,
                descricao));

            log.AppendLine(string.Format(
                " Última data: {0} | Dias comp.: {1} | Nova data: {2}",
                maxDataInicio.ToString("dd/MM/yyyy"),
                limDiasComp,
                novaData.ToString("dd/MM/yyyy")));

            string sqlInsert = @"
                INSERT INTO [dbo].[ALIMBANCOHOR]
                (
                    [CODCOLIGADA],
                    [CODPARCOL],
                    [DATAINICIO],
                    [LIMDIASCOMPENSACAO],
                    [RECCREATEDBY],
                    [RECCREATEDON]
                )
                VALUES
                (
                    :P1,
                    :P2,
                    :P3,
                    :P4,
                    :P5,
                    :P6
                )";

            this.DBS.QueryExec(
                sqlInsert,
                codColigada.ToString(),
                codParcol,
                novaData.ToString("yyyy-MM-dd"),
                limDiasComp,
                "$SYSTEM",
                hoje);

            totalInseridos++;

            log.AppendLine(" [INSERIDO COM SUCESSO]");
        }
        catch (Exception ex)
        {
            totalErros++;

            log.AppendLine(string.Format(
                "CODIGO: {0} | DESCRICAO: {1}",
                codParcol,
                descricao));

            log.AppendLine(
                " [ERRO] Falha ao processar/inserir: " + ex.Message);
        }
    }

    log.AppendLine(new string('=', 100));
    log.AppendLine();
    log.AppendLine("------ RESUMO ------");
    log.AppendLine("Total de registros encontrados: " + dtDados.Rows.Count);
    log.AppendLine("Total inserido com sucesso: " + totalInseridos);
    log.AppendLine("Total com erro: " + totalErros);
    log.AppendLine(
        "**** FIM DO PROCESSO - " +
        DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") +
        " ****");

    this.LogProcess.Value = log.ToString();

    EnviarEmailResumoLimiteCompensacao(
        log,
        dtDados.Rows.Count,
        totalInseridos,
        totalErros,
        false);
}

private void EnviarEmailResumoLimiteCompensacao(
    System.Text.StringBuilder log,
    int totalRegistros,
    int totalInseridos,
    int totalErros,
    bool semRegistros)
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

    byte[] logBytes =
        System.Text.Encoding.UTF8.GetBytes(log.ToString());

    string nomeAnexo =
        "LOG_LIMITE_COMPENSACAO_SINDICATO_" +
        DateTime.Now.ToString("yyyyMMdd_HHmmss") +
        ".txt";

    RMSMailParams mailParams = new RMSMailParams();

    mailParams.CodSistema = "P";
    mailParams.CodColigada = this.Context.CodColigada;
    mailParams.CodUsuario = this.Context.CodUsuario;

    mailParams.Sender =
        !string.IsNullOrEmpty(this.mail.SenderAddress)
            ? new RMSMailSenderParams(
                this.mail.Sender,
                this.mail.SenderAddress,
                this.mail.SenderAddress)
            : new RMSMailSenderParams(this.mail.Sender);

    mailParams.To.AddRange((IEnumerable<string>)listaTo);
    mailParams.CC.AddRange((IEnumerable<string>)listaCc);
    mailParams.BCC.AddRange((IEnumerable<string>)listaBcc);

    mailParams.Subject =
        (totalErros > 0 ? "[ATENÇÃO] " : "[RESUMO] ") +
        "Insere Dias Limite Compensação Sindicato - " +
        DateTime.Now.ToString("dd/MM/yyyy");

    mailParams.Body =
        MontarEmailHtmlResumoLimiteCompensacao(
            totalRegistros,
            totalInseridos,
            totalErros,
            semRegistros);

    mailParams.IsHTMLBody = true;
    mailParams.Timeout = int.MaxValue;
    mailParams.Assync = this.mail.SendAsyncMail;

    mailParams.Attachments.Add(nomeAnexo, logBytes);

    using (
        IRMSMailServer server =
            RMSBroker.CreateServer<IRMSMailServer>("RMSMailServer"))
    {
        server.Send(mailParams);
    }
}

private string MontarEmailHtmlResumoLimiteCompensacao(
    int totalRegistros,
    int totalInseridos,
    int totalErros,
    bool semRegistros)
{
    System.Text.StringBuilder html =
        new System.Text.StringBuilder();

    html.AppendLine(
        "<html><body style='font-family:Arial; font-size:12px; color:#333; max-width:700px; margin:0 auto;'>");

    html.AppendLine(
        " <div style='background-color:#003366; padding:16px 24px; border-radius:6px 6px 0 0;'>");

    html.AppendLine(
        " <h2 style='color:#ffffff; margin:0; font-size:17px;'>Resumo  Insere Dias Limite Compensação Sindicato</h2>");

    html.AppendLine(
        " <p style='color:#cce5ff; margin:4px 0 0 0; font-size:11px;'>Gerado em: " +
        DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") +
        "</p>");

    html.AppendLine(" </div>");

    html.AppendLine(
        " <div style='border:1px solid #dee2e6; border-top:none; padding:24px; border-radius:0 0 6px 6px;'>");

    if (semRegistros)
    {
        html.AppendLine(
            " <div style='text-align:center; padding:40px 20px; background-color:#f8f9fa; border:1px solid #dee2e6; border-radius:8px; margin:20px 0;'>");

        html.AppendLine(
            " <h3 style='color:#495057; margin:0 0 8px 0;'>Nenhum registro encontrado</h3>");

        html.AppendLine(
            " <p style='color:#6c757d; margin:0;'>Não há parâmetros de coligada pendentes de nova vigência de limite de compensação no momento.</p>");

        html.AppendLine(" </div>");
    }
    else
    {
        html.AppendLine(
            " <p style='margin:0 0 16px 0;'>O processo avaliou os parâmetros de coligada (APARCOL) e inseriu a nova vigência de limite de dias de compensação em ALIMBANCOHOR quando aplicável. O detalhamento completo, registro a registro, está no arquivo de log anexado a este e-mail.</p>");

        if (totalErros > 0)
        {
            html.AppendLine(
                " <div style='background-color:#f8d7da; border-left:4px solid #721c24; padding:12px 16px; border-radius:4px; margin:0 0 20px 0;'>");

            html.AppendLine(
                " <p style='margin:0; color:#721c24;'>&#9888;&#65039; <strong>Atenção:</strong> " +
                totalErros +
                " registro(s) falharam ao inserir. Consulte o log anexado para o motivo de cada erro.</p>");

            html.AppendLine(" </div>");
        }

        html.AppendLine(
            " <table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse; font-size:12px; width:100%;'>");

        html.AppendLine(" <thead>");
        html.AppendLine(
            " <tr style='background-color:#003366; color:#ffffff; text-align:center;'>");
        html.AppendLine(
            " <th>Indicador</th><th>Quantidade</th>");
        html.AppendLine(" </tr>");
        html.AppendLine(" </thead>");

        html.AppendLine(" <tbody>");

        html.AppendLine(
            " <tr style='background-color:#f8f9fa;'><td><strong>Registros encontrados</strong></td><td style='text-align:center;'>" +
            totalRegistros +
            "</td></tr>");

        html.AppendLine(
            " <tr style='background-color:#d4edda;'><td><strong>Inseridos com sucesso</strong></td><td style='text-align:center;'>" +
            totalInseridos +
            "</td></tr>");

        if (totalErros > 0)
        {
            html.AppendLine(
                " <tr style='background-color:#f8d7da;'><td><strong>&#9888;&#65039; Com erro</strong></td><td style='text-align:center; color:#721c24; font-weight:bold;'>" +
                totalErros +
                "</td></tr>");
        }
        else
        {
            html.AppendLine(
                " <tr style='background-color:#f8f9fa;'><td><strong>Com erro</strong></td><td style='text-align:center;'>0</td></tr>");
        }

        html.AppendLine(" </tbody>");
        html.AppendLine(" </table>");
        html.AppendLine(" <br/>");

        html.AppendLine(
            " <p style='margin:0; color:#6c757d; font-size:11px;'>&#128206; Consulte o arquivo <strong>LOG_LIMITE_COMPENSACAO_SINDICATO_*.txt</strong> anexado para o detalhamento completo (cada parâmetro avaliado e o resultado do INSERT).</p>");
    }

    html.AppendLine(
        " <p style='color:#999; font-size:10px; margin-top:20px;'>Mensagem gerada automaticamente pelo sistema TOTVS RM.</p>");

    html.AppendLine(" </div>");
    html.AppendLine("</body></html>");

    return html.ToString();
}
