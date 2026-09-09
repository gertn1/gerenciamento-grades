-- =============================================================================
-- grade_precos_auditoria
--
-- Registra toda operação de INSERT/UPDATE/DELETE feita pela API de
-- Gerenciamento de Grades, tanto sobre a grade em si (nome/sigla) quanto
-- sobre o vínculo de um SKU a uma grade (PRODUTO_MESTRE.CODIGO_GRADE_PRECOS).
--
-- Uma linha sempre se refere a UMA grade (CODIGO_GRADE). Quando a operação
-- é sobre um SKU específico (vincular/desvincular), a coluna SKU é
-- preenchida; quando é sobre a grade (criar/renomear/excluir), SKU fica NULL.
--
-- ESTADO_ANTERIOR/ESTADO_NOVO guardam o snapshot em JSON em vez de colunas
-- fixas por campo: o "formato" do estado muda conforme o tipo de operação
-- (ex.: {"nome":..,"sigla":..} para a grade, {"codigoGrade":..} para o
-- vínculo do SKU), e JSON evita uma tabela larga cheia de colunas
-- opcionais/NULL só para acomodar cada caso.
-- =============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'grade_precos_auditoria')
BEGIN
    CREATE TABLE grade_precos_auditoria (
        ID                  INT IDENTITY(1,1)   NOT NULL,
        DATA_HORA           DATETIME2(3)        NOT NULL CONSTRAINT DF_grade_precos_auditoria_data_hora DEFAULT SYSDATETIME(),
        TIPO_OPERACAO       VARCHAR(10)         NOT NULL,
        CODIGO_GRADE        INT                 NOT NULL,
        SKU                 VARCHAR(20)         NULL,
        ESTADO_ANTERIOR     NVARCHAR(MAX)       NULL,
        ESTADO_NOVO         NVARCHAR(MAX)       NULL,
        MATRICULA_USUARIO   VARCHAR(20)         NOT NULL,

        CONSTRAINT PK_grade_precos_auditoria PRIMARY KEY CLUSTERED (ID),
        CONSTRAINT CK_grade_precos_auditoria_tipo_operacao
            CHECK (TIPO_OPERACAO IN ('INSERT', 'UPDATE', 'DELETE')),
        CONSTRAINT CK_grade_precos_auditoria_estado_anterior_json
            CHECK (ESTADO_ANTERIOR IS NULL OR ISJSON(ESTADO_ANTERIOR) = 1),
        CONSTRAINT CK_grade_precos_auditoria_estado_novo_json
            CHECK (ESTADO_NOVO IS NULL OR ISJSON(ESTADO_NOVO) = 1)
    );

    CREATE INDEX IX_grade_precos_auditoria_grade_data
        ON grade_precos_auditoria (CODIGO_GRADE, DATA_HORA DESC);

    CREATE INDEX IX_grade_precos_auditoria_sku
        ON grade_precos_auditoria (SKU)
        WHERE SKU IS NOT NULL;

    CREATE INDEX IX_grade_precos_auditoria_matricula
        ON grade_precos_auditoria (MATRICULA_USUARIO, DATA_HORA DESC);
END
GO

-- Exemplos de consulta:
--
-- Histórico completo de uma grade (renomeações, criação, exclusão e SKUs
-- vinculados/desvinculados):
--   SELECT * FROM grade_precos_auditoria WHERE CODIGO_GRADE = 383 ORDER BY DATA_HORA DESC;
--
-- Histórico de um SKU específico (para quais grades ele já foi movido):
--   SELECT * FROM grade_precos_auditoria WHERE SKU = '7957554' ORDER BY DATA_HORA DESC;
--
-- O que a matrícula 12345 alterou:
--   SELECT * FROM grade_precos_auditoria WHERE MATRICULA_USUARIO = '12345' ORDER BY DATA_HORA DESC;
--
-- Ler um campo específico do JSON (ex.: sigla anterior de uma renomeação):
--   SELECT ID, JSON_VALUE(ESTADO_ANTERIOR, '$.sigla') AS SiglaAnterior, JSON_VALUE(ESTADO_NOVO, '$.sigla') AS SiglaNova
--   FROM grade_precos_auditoria WHERE TIPO_OPERACAO = 'UPDATE' AND SKU IS NULL;
