IF NOT EXISTS (
SELECT 1
    FROM sys.types t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.is_table_type = 1
      AND t.name = 'tb_ParameterXMLLIST'
      AND s.name = 'dbo'
)
BEGIN
CREATE TYPE [dbo].[tb_ParameterXMLLIST] AS TABLE(
	[J_Ui] [varchar](max) NULL,
	[strSql] [varchar](max) NULL,
	[X_Filter] [varchar](max) NULL,
	[X_GFilter] [varchar](max) NULL,
	[J_Api] [varchar](max) NULL
)
END
GO

IF NOT EXISTS (
SELECT 1
    FROM sys.types t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.is_table_type = 1
      AND t.name = 'tb_ParamList'
      AND s.name = 'dbo'
)
BEGIN
CREATE TYPE [dbo].[tb_ParamList] AS TABLE(
	[ParameterName] [varchar](max) NULL,
	[ParameterValue] [varchar](max) NULL,
	[HeaderName] [varchar](100) NULL,
	[JsonTag] [int] NULL
)
END
GO

IF NOT EXISTS (
SELECT 1
    FROM sys.types t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.is_table_type = 1
      AND t.name = 'UserAccessList'
      AND s.name = 'dbo'
)
BEGIN
CREATE TYPE [dbo].[UserAccessList] AS TABLE(
	[ClientCode] [varchar](20) NULL,
	[ClientName] [varchar](200) NULL
)
END
GO

CREATE FUNCTION [dbo].[ReturnTable]
(
	@ItemList NVARCHAR(4000), 
	@delimiter CHAR(1)
)
RETURNS @IDTable TABLE (Value VARCHAR(100))  
AS      
BEGIN    
	DECLARE @tempItemList NVARCHAR(4000)
	SET @tempItemList = @ItemList
	DECLARE @i INT    
	DECLARE @Item NVARCHAR(4000)
	--SET @tempItemList = REPLACE (@tempItemList, ' ', '')
	SET @i = CHARINDEX(@delimiter, @tempItemList)
	WHILE (LEN(@tempItemList) > 0)
	BEGIN
		IF @i = 0
			SET @Item = @tempItemList
		ELSE
			SET @Item = LEFT(@tempItemList, @i - 1)
		INSERT INTO @IDTable(VALUE) VALUES(@Item)
		IF @i = 0
			SET @tempItemList = ''
		ELSE
			SET @tempItemList = RIGHT(@tempItemList, LEN(@tempItemList) - @i)
		SET @i = CHARINDEX(@delimiter, @tempItemList)
	END 
	RETURN
END  
GO

CREATE FUNCTION fn_FindCorporateAction
(
    @strISin VARCHAR(50),
    @strNarration VARCHAR(10),
    @strMkttype VARCHAR(20),
    @strSettlement VARCHAR(50),
    @strExecdt VARCHAR(20),
    @strDrCr VARCHAR(10),
    @strDMOrderNo VARCHAR(50)
)
RETURNS VARCHAR(500)
AS
BEGIN
    DECLARE @Result VARCHAR(500);
    
    -- First attempt
    SELECT TOP 1 @Result = RTRIM(cr_desc)
    FROM CorporateAction_master(NOLOCK)
    WHERE cr_isin = @strISin AND cr_execdt = @strExecdt 
        AND cr_dmorderno = @strDMOrderNo AND cr_drcrind = @strDrCr;
    
    IF @Result IS NOT NULL RETURN @Result;
    
    -- Second attempt
    SELECT TOP 1 @Result = RTRIM(cr_desc)
    FROM CorporateAction_master(NOLOCK)
    WHERE cr_isin = @strISin AND cr_execdt = @strExecdt AND cr_drcrind = 'S';
    
    IF @Result IS NOT NULL RETURN @Result;
    
    -- Third attempt
    SELECT TOP 1 @Result = cr_description
    FROM Corporate_master(NOLOCK)
    WHERE cr_isin = @strISin AND cr_date = @strExecdt;
    
    IF @Result IS NOT NULL RETURN @Result;
    
    -- Fourth attempt
    IF @strNarration IN ('082', '088')
        SELECT TOP 1 @Result = ac_desc FROM Auto_corporate(NOLOCK)
        WHERE ac_creditisin = @strISin AND ac_executiondt = @strExecdt;
    ELSE
        SELECT TOP 1 @Result = ac_desc FROM Auto_corporate(NOLOCK)
        WHERE ac_debitisin = @strISin AND ac_executiondt = @strExecdt;
    
    RETURN ISNULL(@Result, 'Corporate Action');
END
GO

CREATE PROCEDURE [dbo].[stpr_GetClientAccessListNew] @i_vcUserid VARCHAR(500), @i_vcSelection VARCHAR(50), @i_vcCode VARCHAR(20) WITH ENCRYPTION AS
BEGIN
  DECLARE @strUserType VARCHAR(1) = '', @strUserAccessValue VARCHAR(50)='', @strString VARCHAR(MAX)='',
  @strCMSCHEDULE VARCHAR(20)= (SELECT TOP 1 sp_sysvalue FROM sysparameter(NOLOCK) WHERE sp_parmcd = 'CMSCHEDULE')
  DECLARE @tbl_UserAccessRights TABLE(UserType VARCHAR(1), UserAccessValue VARCHAR(50))
  CREATE TABLE #tb_UserList1 (ClientCode VARCHAR(20), ClientName VARCHAR(200))

  IF @i_vcUserid <> ''
  BEGIN
    IF (SELECT COUNT(*) FROM sysobjects with (noLock) where name='LoginAccess') > 0
	BEGIN
	  SET @strString = 'SELECT LA_grouping, LA_GrCode FROM LoginAccess(NOLOCK) '
	    +' WHERE LA_UserId IN(SELECT VALUE FROM DBO.ReturnTable('''+@i_vcUserid+''','',''))  '
	  INSERT INTO @tbl_UserAccessRights
	  EXEC(@strString)
      
	  DECLARE db_CursorClientList CURSOR FOR         
      SELECT distinct UserType, UserAccessValue 
      FROM @tbl_UserAccessRights  
 
      OPEN db_CursorClientList       
      FETCH NEXT FROM db_CursorClientList INTO @strUserType, @strUserAccessValue 
      WHILE @@FETCH_STATUS = 0     
      BEGIN
        IF  @strUserType = 'B'
	    BEGIN
	      INSERT INTO #tb_UserList1(ClientCode, ClientName)
	      SELECT DISTINCT cm_cd, cm_name from client_master(nolock) WHERE cm_brboffcode = @strUserAccessValue and cm_schedule = @strCMSCHEDULE
	    END  
	    ELSE IF @strUserType = 'F'
	    BEGIN
	      INSERT INTO #tb_UserList1(ClientCode, ClientName)
	      SELECT DISTINCT cm_cd, cm_name from client_master(nolock) WHERE cm_familycd = @strUserAccessValue   and cm_schedule = @strCMSCHEDULE
	    END
	    ELSE IF @strUserType = 'A'
	    BEGIN
	      INSERT INTO #tb_UserList1(ClientCode, ClientName)
	      SELECT DISTINCT cm_cd, cm_name from client_master(nolock) WHERE cm_schedule = @strCMSCHEDULE
	    END
	    ELSE IF @strUserType = 'G'
	    BEGIN
	      INSERT INTO #tb_UserList1(ClientCode, ClientName)
	      SELECT DISTINCT cm_cd, cm_name from client_master(nolock) WHERE cm_groupcd = @strUserAccessValue  and cm_schedule = @strCMSCHEDULE
	    END
	    ELSE IF @strUserType = 'C'
	    BEGIN
	      INSERT INTO #tb_UserList1(ClientCode, ClientName)
	      SELECT DISTINCT cm_cd, cm_name from client_master(nolock) WHERE cm_cd = @strUserAccessValue  and cm_schedule = @strCMSCHEDULE
	    END
		
        FETCH NEXT FROM db_CursorClientList INTO @strUserType, @strUserAccessValue 
      END        
      CLOSE db_CursorClientList        
      DEALLOCATE db_CursorClientList
    END
	ELSE IF EXISTS(SELECT 1 FROM user_master(NOLOCK) WHERE um_user_id IN(SELECT VALUE FROM DBO.ReturnTable(@i_vcUserid,',')))
	BEGIN
	  
	  DECLARE @TBL_BranchList TABLE(un_branchCode VARCHAR(MAX))
	  INSERT INTO @TBL_BranchList(un_branchCode)
	  SELECT um_brcode FROM user_master with (noLock) 
	  WHERE um_user_id IN(SELECT VALUE FROM DBO.ReturnTable(@i_vcUserid,',')) 
	  
	  IF EXISTS(SELECT 1 FROM @TBL_BranchList WHERE un_branchCode LIKE '%<ALL>%')
	  BEGIN
	    INSERT INTO #tb_UserList1(ClientCode, ClientName)
	    SELECT DISTINCT cm_cd, cm_name from client_master(nolock) WHERE cm_schedule = @strCMSCHEDULE
	  END
	  ELSE
	  BEGIN
  	    DECLARE @strBranchList VARCHAR(MAX)=''
		SELECT @strBranchList = @strBranchList+'|'+un_branchCode
		FROM  @TBL_BranchList
		
		INSERT INTO #tb_UserList1(ClientCode, ClientName)
	    SELECT DISTINCT cm_cd, SUBSTRING(cm_name,1,200) from client_master(nolock) 
		WHERE cm_brboffcode IN(SELECT VALUE FROM (SELECT * FROM DBO.RETURNTABLE(@strBranchList,'|')) X1 WHERE ISNULL(X1.VALUE,'') <> '')
		AND cm_schedule = @strCMSCHEDULE
	  END   
	END
	ELSE 
	BEGIN
	  INSERT INTO #tb_UserList1(ClientCode, ClientName)
	  SELECT DISTINCT cm_cd, SUBSTRING(cm_name,1,200) from client_master(nolock) WHERE CM_CD IN(SELECT VALUE FROM DBO.ReturnTable(@i_vcUserid,',')) 
	  AND cm_schedule = @strCMSCHEDULE
	END
  END 	
  ELSE
  BEGIN
    INSERT INTO #tb_UserList1(ClientCode, ClientName)
	SELECT DISTINCT cm_cd, SUBSTRING(cm_name,1,200) from client_master(nolock) WHERE cm_schedule = @strCMSCHEDULE
  END

  DECLARE @strWhere VARCHAR(MAX) = ''
  IF UPPER(@i_vcSelection) = 'CLIENT' AND LTRIM(RTRIM(@i_vcCode)) <> ''
  BEGIN
     DELETE FROM #tb_UserList1 WHERE ClientCode <> @i_vcCode
  END
  ELSE IF UPPER(@i_vcSelection) = 'BRANCH' AND LTRIM(RTRIM(@i_vcCode)) <> ''
  BEGIN
     DELETE FROM #tb_UserList1 WHERE ClientCode NOT IN(SELECT distinct cm_cd FROM Client_master where cm_cd = ClientCode and cm_brboffcode = @i_vcCode)
  END
  ELSE IF UPPER(@i_vcSelection) = 'FAMILY' AND LTRIM(RTRIM(@i_vcCode)) <> ''
  BEGIN
     DELETE FROM #tb_UserList1 WHERE ClientCode NOT IN(SELECT distinct cm_cd FROM Client_master where cm_cd = ClientCode and cm_familycd = @i_vcCode)
  END
  ELSE IF UPPER(@i_vcSelection) = 'GROUP' AND LTRIM(RTRIM(@i_vcCode)) <> ''
  BEGIN
     DELETE FROM #tb_UserList1 WHERE ClientCode NOT IN(SELECT distinct cm_cd FROM Client_master where cm_cd = ClientCode and cm_groupcd = @i_vcCode)
  END
  ELSE IF UPPER(@i_vcSelection) = 'BACKOFFICECD' AND LTRIM(RTRIM(@i_vcCode)) <> ''
  BEGIN
     DELETE FROM #tb_UserList1 WHERE ClientCode NOT IN(SELECT distinct cm_cd FROM Client_master where cm_cd = ClientCode and cm_blsavingcd = @i_vcCode)
  END

  SELECT * FROM #tb_UserList1

  DROP TABLE #tb_UserList1
END
GO

CREATE PROCEDURE stpr_LedgerStatement @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, 
	@o_vcErrorMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN
	--- Parameter Declaration
  DECLARE @dtFromDate VARCHAR(8), @dtToDt VARCHAR(8), @strUserId VARCHAR(500) = '', @strLevel VARCHAR(1), 
  @strCode VARCHAR(500), @strSelection VARCHAR(100), @XMLData XML, @strOutputType VARCHAR(1) = 'G', 
  @strRequestFrom VARCHAR(1) = 'W', @XMLDATA1 XML =''

  IF @vcXML = ''
  BEGIN
	SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
  END
  --SELECT @vcXML
  DECLARE @tbl_UserList dbo.UserAccessList;
  SET @XMLData = CAST('<root>' + @vcXML + '</root>' AS XML)

  SELECT @dtFromDate = ISNULL(x.value('(FromDate)[1]', 'VARCHAR(8)'), ''), 
  @dtToDt = ISNULL(x.value('(ToDate)[1]', 'VARCHAR(8)'), ''), 
  @strUserId = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'), ''), 
  @strLevel = ISNULL(x.value('(Level)[1]', 'VARCHAR(1)'), ''), 
  @strCode = ISNULL(x.value('(Code)[1]', 'VARCHAR(500)'), ''), 
  @strSelection = ISNULL(x.value('(Selection)[1]', 'VARCHAR(20)'), ''), 
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'), ''), 
  @strRequestFrom = ISNULL(x.value('(RequestFrom)[1]', 'VARCHAR(1)'), '')
  FROM @XMLData.nodes('/root') AS XTbl(x)

  
  
  IF ISNULL(@dtFromDate,'') = ''
  BEGIN
    SELECT @dtFromDate = ISNULL(x.value('(FromDt)[1]', 'VARCHAR(8)'), '')
    FROM @XMLData.nodes('/root') AS XTbl(x)
  END
  
  IF ISNULL(@dtToDt,'') = ''
  BEGIN
    SELECT @dtToDt = ISNULL(x.value('(ToDt)[1]', 'VARCHAR(8)'), '')
    FROM @XMLData.nodes('/root') AS XTbl(x)
  END
  

  IF ISNULL(@strSelection,'') = ''
  BEGIN
    SET @strSelection = 'CLIENT'
  END

  INSERT INTO @tbl_UserList
  EXEC dbo.stpr_GetClientAccessListNew @strUserid, @strSelection, @StrCode
  
  DECLARE @tbl_LedgerReport 
  TABLE(SerialNo INT IDENTITY(1,1), ClientCode VARCHAR(16), Date VARCHAR(15), voucherNo VARCHAR(50),
  Narration VARCHAR(500), DebitofCredit VARCHAR(1), ChequeNo VARCHAR(50),
  DebitAmount MONEY, CreditAmount MONEY, Balance MONEY, BalanceTag VARCHAR(2), ClientName VARCHAR(150))
  BEGIN TRY
  INSERT INTO @tbl_LedgerReport(ClientCode, Date, voucherNo, Narration, DebitofCredit, ChequeNo, DebitAmount,
  CreditAmount, Balance, BalanceTag)
  SELECT ld_clientcd AS ClientCode, ld_dt, Voucher AS voucherNo, ld_particular AS Narration, 
		ld_debitflag AS DebitofCredit, ld_chequeno AS ChequeNo, DebitAmount = CASE WHEN ld_amount > 0 THEN ld_amount 
			ELSE 0 END, CreditAmount = ABS(CASE WHEN ld_amount < 0 THEN ld_amount ELSE 0 END), Balance = (sum(ld_amount) 
			OVER (
				PARTITION BY ld_clientcd ORDER BY tag, LD_DT, ld_particular, ld_accyear, ld_documentno, 
					ld_documenttype, SerialNo
				))*-1, BalanceTag = CASE WHEN (
					sum(ld_amount) OVER (
						PARTITION BY ld_clientcd ORDER BY tag, LD_DT, ld_particular, ld_accyear, ld_documentno, 
							ld_documenttype, SerialNo
						)
					) > 0 THEN 'Dr' ELSE 'Cr' END
	FROM (
		SELECT 1 AS tag, ld_clientcd, CONVERT(VARCHAR, @dtFromDate, 112) AS ld_dt, Voucher = '', CAST(SUM(CASE SIGN(
							DATEDIFF(DAY, @dtFromDate, CAST(CONVERT(VARCHAR, ld_dt, 112) AS DATE))) WHEN - 1 THEN ld_amount 
						ELSE 0 END) AS DECIMAL(15, 2)) AS ld_amount, 'Opening Balance' AS ld_particular, CASE SIGN(SUM(
						ld_amount)) WHEN 1 THEN 'D' ELSE 'C' END AS ld_debitflag, '' AS ld_chequeno, 'O' AS ld_documenttype, 
			ld_common = '', LookUp = '', ld_accyear = '', ld_documentno = '', SerialNo = 0
		FROM dbo.Ledger(NOLOCK)
		WHERE CAST(CONVERT(VARCHAR, ld_dt, 112) AS DATE) < @dtFromDate
		GROUP BY ld_clientcd
		HAVING SUM(ld_amount) <> 0
		
		UNION ALL
		
		SELECT 2 AS tag, ld_clientcd, ld_dt = CONVERT(VARCHAR, ld_dt, 112), ld_documenttype + '/' + 
			ld_documentno AS 'Voucher', CAST(ld_amount AS DECIMAL(15, 2)) AS ld_amount, ld_particular, ld_debitflag, 
			ld_chequeno, ld_documenttype, ld_common, CASE WHEN ld_documentType = 'B' THEN SUBSTRING(LD_DPID, 2, 1) + 
						'/' + SUBSTRING(LD_DPID, 3, 1) + '/' + ld_common + '/' + ld_commondt ELSE '' END AS LookUp, ld_accyear, 
			ld_documentno,
			SerialNo = ROW_NUMBER() OVER (ORDER BY    LD_DT, ld_particular, ld_accyear, ld_documentno, 
							ld_documenttype, NEWID())
		FROM dbo.Ledger(NOLOCK)
		WHERE CAST(CONVERT(VARCHAR, ld_dt, 112) AS DATE) >= @dtFromDate AND CAST(
				CONVERT(VARCHAR, ld_dt, 112) AS DATE) <= @dtToDt
		) XMAIN,  @tbl_UserList X
		WHERE XMAIN.ld_clientcd = X.ClientCode
	ORDER BY ld_clientcd, TAG, ld_dt
	
  UPDATE A SET A.ClientName = cm_name
  FROM @tbl_LedgerReport A, CLIENT_MASTER(NOLOCK)
  WHERE ClientCode = CM_CD
  END TRY
  BEGIN CATCH 
    SELECT ERROR_MESSAGE()
	
  END CATCH  
 
	
  IF ISNULL(@strOutputType,'G') = 'G'
  BEGIN
    SELECT ClientCode, ClientName, Date, voucherNo, Narration, DebitofCredit, ChequeNo, DebitAmount,
    CreditAmount, Balance, BalanceTag
    FROM  @tbl_LedgerReport
	ORDER BY SerialNo
	SET @o_vcErrorMessage = 'Process Executed'
	SET @o_vcErrorFlag = 'S'
	RETURN 1
  END
  ELSE IF ISNULL(@strOutputType,'G') = 'X'	
  BEGIN
    IF NOT EXISTS(SELECT 1 FROM @tbl_LedgerReport)
	BEGIN
	  SET @XMLDATA1 = 
	  (SELECT ClientCode = '', ClientName = '', Date = '', voucherNo = '', Narration = '', DebitofCredit = '', 
	  ChequeNo = '', DebitAmount = '', CreditAmount = '', Balance = '' , BalanceTag =''
	  FOR XML PATH('Detail'))
    END 
	ELSE
	BEGIN
      SET @XMLDATA1 = 
	  (SELECT ClientCode, ClientName, Date, voucherNo, Narration, DebitofCredit, ChequeNo, DebitAmount,
      CreditAmount, Balance, BalanceTag
	  FROM  @tbl_LedgerReport ORDER BY SerialNo FOR XML PATH('Detail'))
	END
	SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	SET @o_vcErrorFlag = 'S'
	RETURN 1
  END
END
GO

CREATE PROCEDURE stpr_TransactionStatement @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN
  --- Parameter Declaration

  DECLARE @dtFromDate VARCHAR(8), @dtToDt VARCHAR(8), @strUserId VARCHAR(500) = '', @strLevel VARCHAR(1), @strCode VARCHAR(500), @strSelection VARCHAR(100
    ), @XMLData XML, @strOutputType VARCHAR(1) = 'G', @strRequestFrom VARCHAR(1) = 'W', @XMLDATA1 XML = '', @strShowValuation VARCHAR(20) = '', @strISIN VARCHAR
    (20), @strTrxType VARCHAR(200) = '', @strMarketAndEarlyPay VARCHAR(10) = '', @strOffMarket VARCHAR(10) = '', @strInterDepository VARCHAR(10) = '', 
    @strDematAndDestat VARCHAR(10) = '', @strPledge VARCHAR(10) = '', @strCorporateAction VARCHAR(10) = '', @strSQL NVARCHAR(MAX) = '', @strReportDisplay 
    VARCHAR(1) = 'P', @strJsonFormat VARCHAR(1) = 'N', @strJsonText VARCHAR(MAX) = ''

  IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'

    RETURN 1
  END

  --SELECT @vcXML
  DECLARE @tbl_UserList dbo.UserAccessList;

  SET @XMLData = CAST('<root>' + @vcXML + '</root>' AS XML)

  SELECT @dtFromDate = ISNULL(x.value('(FromDate)[1]', 'VARCHAR(8)'), ''), @dtToDt = ISNULL(x.value('(ToDate)[1]', 'VARCHAR(8)'), ''), @strUserId = 
    ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'), ''), @strLevel = ISNULL(x.value('(Level)[1]', 'VARCHAR(1)'), ''), @strCode = ISNULL(x.value(
        '(Code)[1]', 'VARCHAR(500)'), ''), @strSelection = ISNULL(x.value('(Selection)[1]', 'VARCHAR(1)'), ''), @strOutputType = ISNULL(x.value(
        '(OutputType)[1]', 'VARCHAR(1)'), ''), @strRequestFrom = ISNULL(x.value('(RequestFrom)[1]', 'VARCHAR(1)'), ''), @strISIN = ISNULL(x.value(
        '(ISIN)[1]', 'VARCHAR(20)'), ''), @strMarketAndEarlyPay = ISNULL(x.value('(MarketAndEarlyPay)[1]', 'VARCHAR(10)'), ''), @strOffMarket = ISNULL
    (x.value('(OffMarket)[1]', 'VARCHAR(10)'), ''), @strInterDepository = ISNULL(x.value('(InterDepository)[1]', 'VARCHAR(10)'), ''), 
    @strDematAndDestat = ISNULL(x.value('(DematAndDestat)[1]', 'VARCHAR(10)'), ''), @strPledge = ISNULL(x.value('(Pledge)[1]', 'VARCHAR(10)'), ''), 
    @strCorporateAction = ISNULL(x.value('(CorporateAction)[1]', 'VARCHAR(10)'), ''), @strReportDisplay = ISNULL(x.value('(ReportDisplay)[1]', 
        'VARCHAR(10)'), ''), @strJsonFormat = ISNULL(x.value('(JsonFormat)[1]', 'VARCHAR(1)'), '')
  FROM @XMLData.nodes('/root') AS XTbl(x)

  IF ISNULL(@dtFromDate, '') = ''
  BEGIN
    SELECT @dtFromDate = ISNULL(x.value('(FromDt)[1]', 'VARCHAR(8)'), '')
    FROM @XMLData.nodes('/root') AS XTbl(x)
  END

  IF ISNULL(@dtToDt, '') = ''
  BEGIN
    SELECT @dtToDt = ISNULL(x.value('(ToDt)[1]', 'VARCHAR(8)'), '')
    FROM @XMLData.nodes('/root') AS XTbl(x)
  END

  DECLARE @tbl_trxrtpe TABLE (TRXTYPE VARCHAR(10))

  IF @strMarketAndEarlyPay = 'true'
  BEGIN
    INSERT INTO @tbl_trxrtpe
    VALUES ('052')

    INSERT INTO @tbl_trxrtpe
    VALUES ('054')
  END

  IF @strOffMarket = 'true'
  BEGIN
    INSERT INTO @tbl_trxrtpe
    VALUES ('044')

    INSERT INTO @tbl_trxrtpe
    VALUES ('042')
  END

  IF @strInterDepository = 'true'
  BEGIN
    INSERT INTO @tbl_trxrtpe
    VALUES ('202')

    INSERT INTO @tbl_trxrtpe
    VALUES ('204')
  END

  IF @strDematAndDestat = 'true'
  BEGIN
    INSERT INTO @tbl_trxrtpe
    VALUES ('011')

    INSERT INTO @tbl_trxrtpe
    VALUES ('012')

    INSERT INTO @tbl_trxrtpe
    VALUES ('013')
  END

  IF @strPledge = 'true'
  BEGIN
    INSERT INTO @tbl_trxrtpe
    VALUES ('091')

    INSERT INTO @tbl_trxrtpe
    VALUES ('092')

    INSERT INTO @tbl_trxrtpe
    VALUES ('093')
  END

  IF @strCorporateAction = 'true'
  BEGIN
    INSERT INTO @tbl_trxrtpe
    VALUES ('082')
  END

  IF NOT EXISTS (
      SELECT 1
      FROM @tbl_trxrtpe
      )
  BEGIN
    INSERT INTO @tbl_trxrtpe
    SELECT NR_CODE
    FROM Narration
  END

  DECLARE @tbl_Transaction TABLE (
    SerialNo INT IDENTITY(1, 1), ClientCode VARCHAR(20), ClientName VARCHAR(200), TrxnDate VARCHAR(20), ISIN VARCHAR(20), 
	ScripName VARCHAR(100), TrxnNo 
    VARCHAR(50), Description VARCHAR(50), Narration NVARCHAR(500), DebitCredit VARCHAR(50), DebitQty MONEY, CreditQty MONEY, Runingbalace MONEY
    )

  IF ISNULL(@strSelection, '') = ''
  BEGIN
    SET @strSelection = 'CLIENT'
  END

  INSERT INTO @tbl_UserList
  EXEC dbo.stpr_GetClientAccessListNew @strUserid, @strSelection, @strCode

  IF @strReportDisplay IN ('D', 'E', 'A')
  BEGIN
    DECLARE @o_Message NVARCHAR(MAX) = ''

    EXEC stpr_TypestHeaderProc @strUserid, @strSelection, @strCode, @o_Message OUTPUT

    SET @o_Message = REPLACE(@o_Message, '##ReportName##', 'Transaction Statement')
    SET @o_Message = REPLACE(@o_Message, '##Grid1##', 'Transaction Statement')
    SET @o_Message = REPLACE(@o_Message, '##Grid2##', '')
    SET @o_Message = REPLACE(@o_Message, '##Grid3##', '')
    SET @o_Message = REPLACE(@o_Message, '##Grid4##', '')

    IF ISNULL(@strReportDisplay, '') <> 'A'
    BEGIN
      SET @o_Message = REPLACE(@o_Message, '##FormatGrid##', 'Grid1')
    END
    ELSE
    BEGIN
      SET @o_Message = REPLACE(@o_Message, '##FormatGrid##', '')
    END

    SET @o_Message = '<root>' + @o_Message + '</root>';

    DECLARE @xmlData2 XML = CAST(@o_Message AS XML);
    DECLARE @DynamicColumns NVARCHAR(MAX);

    SET @DynamicColumns = '';

    WITH DistinctNodes
    AS (
      SELECT DISTINCT NodeName = X.n.value('local-name(.)', 'NVARCHAR(100)')
      FROM @xmlData2.nodes('/root/ClientDetail/*') AS X(n)
      )
    SELECT @DynamicColumns = @DynamicColumns + ',' + 'LTRIM(RTRIM(X1.value(''(' + XX.NodeName + ')[1]'', ''NVARCHAR(MAX)''))) AS [' + XX.NodeName + ']'
    FROM DistinctNodes XX

    SELECT @DynamicColumns = SUBSTRING(@DynamicColumns, 2, LEN(@DynamicColumns) - 1)

    DECLARE @sql NVARCHAR(MAX);

    SET @sql = '
    SELECT ' + @DynamicColumns + '
    FROM @xmlData2.nodes(''/root/ClientDetail'') AS T(X1)';

    EXEC sp_executesql @sql, N'@xmlData2 XML', @xmlData2;

    IF ISNULL(@strReportDisplay, '') <> 'A'
    BEGIN
      SELECT ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, ColumnFormat, Columnstyle, DecimalPlace, ColumnTotal
      FROM tbl_ReportGridViewFormat(NOLOCK)
      WHERE ReportCode = 'CrossNet' AND ReportCategroy = 'TransactionStatement'
      ORDER BY Orderby
    END
  END

  DECLARE @dtFromDateFormatted AS CHAR(10) = CAST(@dtFromDate AS DATE)
  DECLARE @strToDtFormatted AS CHAR(10) = CAST(@dtToDt AS DATE)
  
  INSERT INTO @tbl_Transaction (ClientCode, TrxnDate, ISIN, ScripName, TrxnNo, Description, Narration, DebitCredit, DebitQty, CreditQty, Runingbalace
    )
  SELECT XX.ClientCode, DATE, ISIN, ISINName, TrxnNo = '', Type, Particular, DebitCredit = CASE WHEN Balance < 0 THEN 'D' ELSE 'C' END, Debit = (CASE WHEN Balance < 0 THEN (Balance * - 1) ELSE 0 END
      ), Credit = (CASE WHEN Balance > 0 THEN Balance ELSE 0 END), Balance = sum(Balance) OVER (
      PARTITION BY XX.ClientCode, ISIN, Type ORDER BY XX.ClientCode, ISINName, ActType, MarketType, Settlement, TAG, SerialNo
      )
  FROM (
    SELECT Tag = 1, td_ac_code AS 'ClientCode', @dtFromDate AS 'Date', td_isin_code AS 'ISIN', sc_company_name AS 'ISINName', td_ac_type AS 'ActType', '' AS 
      'MarketType', '' AS 'Settlement', bt_description AS 'Type', 'Opening Balance' AS 'Particular', sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE 
            td_qty * (- 1) END) AS 'Balance', SerialNo = 0
    FROM Trxdetail(NOLOCK) a
    LEFT JOIN Market_type(NOLOCK) ON (td_market_type = mt_code AND LTRIM(RTRIM(ISNULL(td_clear_corpn, ''))) = LTRIM(RTRIM(mt_ccid))), Security(
        NOLOCK), Client_master(NOLOCK), Client_Backoffice(NOLOCK), Narration(NOLOCK), Beneficiary_type(NOLOCK), Beneficiary_category(NOLOCK), 
      Beneficiary_status(NOLOCK)
    WHERE td_ac_code = cm_cd AND cm_cd = cb_cmcd AND bc_code = td_category AND bs_code = cm_active AND td_isin_code = sc_isincode AND td_narration = nr_code AND 
      cm_statements <> 'N' AND td_ac_type = bt_code AND td_Narration NOT IN ('001') AND td_ac_type <> '30' AND (
        td_booking_type <> '02' OR Rtrim(td_booking_type) + Rtrim(td_blockedcd) IN (
          SELECT '02' + Rtrim(blc_code)
          FROM Block_code(NOLOCK)
          WHERE blc_Flag = 'B' AND blc_YesNo = 'Y'
          )
        ) AND td_curdate < @dtFromDate AND CM_CD IN(SELECT CLIENTCODE FROM @tbl_UserList)
    GROUP BY td_ac_code, td_isin_code, sc_company_name, td_ac_type, bt_description
    HAVING sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE td_qty * (- 1) END) <> 0
    
    UNION ALL
    
    SELECT Tag = 2, cm_cd AS 'ClientCode', CONVERT(CHAR(8), td_curdate, 112) AS 'Date', td_isin_code AS 'ISIN', sc_company_name AS 'ISINName', td_ac_type AS 
      'ActType', td_market_type AS 'MarketType', td_settlement AS 'Settlement', bt_description AS 'Type',
      --nr_description AS 'Particular', 
      CASE td_debit_credit WHEN 'D' THEN 'To ' ELSE 'By ' END + COALESCE(CASE WHEN (td_narration IN ('042', '044')) AND 
            td_market_type != '' AND LTRIM(RTRIM(td_settlement)) != '' AND (td_booking_type != '02' OR td_booking_type != '03') THEN CASE WHEN 
                  td_countercmbpid IS NULL OR LTRIM(RTRIM(td_countercmbpid)) = '' THEN CASE WHEN (
                          SELECT TOP 1 bp_role
                          FROM Bpmaster(NOLOCK)
                          WHERE bp_id = td_counterdp
                          ) = '03' THEN 'CM ' + LEFT(LTRIM(RTRIM((
                                  SELECT bp_name
                                  FROM Bpmaster(NOLOCK)
                                  WHERE bp_id = td_counterdp
                                  ))), 25) + ' ' + (select top 1 mt_description from Market_type where mt_code=td_market_type and mt_ccid = td_clear_corpn) + '/' + td_settlement ELSE (
                          SELECT TOP 1 bp_name
                          FROM Bpmaster(NOLOCK)
                          WHERE bp_id = td_counterdp
                          ) + '/ ' + CASE WHEN td_billcode IN ('11542', '11543') THEN (
                                SELECT TOP 1 bp_name
                                FROM Bpmaster(NOLOCK)
                                WHERE bp_id = td_counterdp
                                ) + ' eDIS/Block Mechanism / ' + (
                                SELECT TOP 1 mt_description
                                FROM Market_type
                                WHERE mt_code = LTRIM(RTRIM(td_market_type))
                                ) + LTRIM(RTRIM(td_settlement)) ELSE '' END END ELSE CASE WHEN (
                        SELECT TOP 1 bp_role
                        FROM Bpmaster(NOLOCK)
                        WHERE bp_id = td_countercmbpid
                        ) = '03' THEN 'CM ' + LEFT(LTRIM(RTRIM((
                                SELECT TOP 1 bp_name
                                FROM Bpmaster(NOLOCK)
                                WHERE bp_id = td_countercmbpid
                                ))), 25) + ' ' + (select top 1 mt_description from Market_type where mt_code=td_market_type and mt_ccid = td_clear_corpn) + '/' + td_settlement ELSE (
                        SELECT TOP 1 bp_name
                        FROM Bpmaster(NOLOCK)
                        WHERE bp_id = td_countercmbpid
                        ) + '/ ' + CASE WHEN td_billcode IN ('11542', '11543') THEN (
                              SELECT TOP 1 bp_name
                              FROM Bpmaster(NOLOCK)
                              WHERE bp_id = td_countercmbpid
                              ) + ' eDIS/Block Mechanism / ' + (
                              SELECT TOP 1 mt_description
                              FROM Market_type
                              WHERE mt_code = LTRIM(RTRIM(td_market_type))
                              ) + LTRIM(RTRIM(td_settlement)) ELSE '' END END END ELSE CASE WHEN (td_narration IN ('042', '044')
                  ) AND td_market_type = '' AND td_settlement = '' THEN CASE WHEN td_counterdp <> '' THEN (
                          SELECT TOP 1 bp_name
                          FROM Bpmaster(NOLOCK)
                          WHERE bp_id = td_counterdp
                          ) ELSE nr_description END ELSE CASE WHEN td_narration = '307' AND td_ac_type = '22' THEN 'Pledge Request ' + (
                        SELECT TOP 1 bp_name
                        FROM Bpmaster(NOLOCK)
                        WHERE bp_id = td_counterdp
                        ) ELSE CASE WHEN td_narration = '307' AND td_ac_type = '26' THEN 'Pledge Request' ELSE CASE WHEN td_narration = '307' AND td_ac_type = 
                            '29' THEN 'Pledge Request' ELSE CASE WHEN td_narration = '091' AND td_ac_type = '26' THEN 'Pledge ' + (
                                    SELECT TOP 1 bp_name
                                    FROM Bpmaster(NOLOCK)
                                    WHERE bp_id = td_counterdp
                                    ) ELSE CASE WHEN td_narration = '091' AND td_ac_type = '22' THEN 'Pledge ' + (
                                        SELECT TOP 1 bp_name
                                        FROM Bpmaster(NOLOCK)
                                        WHERE bp_id = td_counterdp
                                        ) ELSE CASE WHEN td_narration = '092' THEN 'Pledge Closure' ELSE CASE WHEN td_narration = '321' OR td_narration = 
                                            '309' THEN 'Pledge Request ' + (
                                                SELECT TOP 1 bp_name
                                                FROM Bpmaster(NOLOCK)
                                                WHERE bp_id = td_counterdp
                                                ) ELSE NULL END END END END END END END END END, CASE WHEN td_narration = '093' THEN nr_description + (
                SELECT TOP 1 bp_name
                FROM Bpmaster(NOLOCK)
                WHERE bp_id = td_counterdp
                ) ELSE CASE WHEN td_narration = '201' OR td_narration = '202' THEN '' ELSE CASE WHEN td_narration = '203' AND td_booking_type = '02' THEN 
                      'Inter Dp Rejection' ELSE CASE WHEN td_narration = '012' AND td_debit_credit = 'C' THEN 'Dematerilization' ELSE CASE WHEN td_narration 
                            = '012' AND td_debit_credit = 'D' THEN 'Dematerialisation request confirmation' ELSE CASE WHEN td_narration = '011' AND 
                                td_debit_credit = 'C' THEN 'Dematerilization Request' ELSE CASE WHEN td_narration = '204' THEN 
                                      'Inter Depository transfer CDS' ELSE CASE WHEN td_narration = '231' AND td_booking_type = '02' THEN 'Freeze [' + (
                                            SELECT TOP 1 blc_description
                                            FROM Block_code(NOLOCK)
                                            WHERE blc_flag = td_blocked AND blc_code = td_blockedcd
                                            ) + ']' ELSE CASE WHEN td_narration = '082' THEN DBO.fn_FindCorporateAction(td_isin_code, td_Narration, 
                                                td_market_type, td_settlement, CONVERT(VARCHAR(8), td_curdate, 112), td_debit_credit, td_reference
                                              ) END END END END END END END END END, CASE WHEN td_narration = '083' THEN DBO.fn_FindCorporateAction(
                td_isin_code, td_Narration, td_market_type, td_settlement, CONVERT(VARCHAR(8), td_curdate, 112), td_debit_credit, td_reference) ELSE 
            CASE WHEN td_narration = '999' OR (td_narration = '000' AND td_booking_type = '02' AND td_blocked = 'B' AND td_market_type <> '') 
                THEN (
                    SELECT TOP 1 bp_name
                    FROM Bpmaster(NOLOCK)
                    WHERE bp_id = td_clear_corpn
                    ) + '/' + mt_description + '/' + td_settlement ELSE CASE WHEN td_billcode = '11551' OR td_billcode = '11552' THEN 'Margin Re-pledge ' + 
                      td_counterdp + ' / ' ELSE CASE WHEN td_billcode = '11553' OR td_billcode = '11554' THEN 'Margin Re-pledge ' + td_counterdp + ' / ' ELSE 
                        nr_description END END END END) + ' ' + CASE WHEN td_beneficiery <> '00000000' THEN CASE WHEN td_clear_corpn <> '' THEN '' ELSE 
                td_beneficiery END ELSE CASE WHEN td_counterdp = 'IN000026' AND td_cds <> '' THEN td_cds ELSE '' END END AS Particular, (CASE td_debit_credit WHEN 'C' THEN td_qty ELSE td_qty * (- 1) END
        ) AS 'Balance', SerialNo = ROW_NUMBER() OVER (
        ORDER BY cm_cd, td_curdate
        )
    FROM Trxdetail(NOLOCK) a
    LEFT JOIN Market_type ON (td_market_type = mt_code AND ltrim(rtrim(isnull(td_clear_corpn, ''))) = ltrim(rtrim(mt_ccid))), Security(NOLOCK), 
      Client_master(NOLOCK), Client_Backoffice(NOLOCK), Narration(NOLOCK), Beneficiary_type(NOLOCK), Beneficiary_category(NOLOCK), 
      Beneficiary_status(NOLOCK)
    WHERE td_ac_code = cm_cd AND cm_cd = cb_cmcd AND bc_code = td_category AND bs_code = cm_active AND td_isin_code = sc_isincode AND td_narration = nr_code AND 
      cm_statements <> 'N' AND td_ac_type = bt_code AND td_Narration NOT IN ('001') AND td_ac_type <> '30' AND (
        td_booking_type <> '02' OR Rtrim(td_booking_type) + Rtrim(td_blockedcd) IN (
          SELECT '02' + Rtrim(blc_code)
          FROM Block_code(NOLOCK)
          WHERE blc_Flag = 'B' AND blc_YesNo = 'Y'
          )
        ) AND td_curdate BETWEEN @dtFromDateFormatted AND @strToDtFormatted 
		AND CM_CD IN(SELECT CLIENTCODE FROM @tbl_UserList)
		AND (Rtrim(td_ac_code) + Rtrim(td_ac_type) + Rtrim(td_isin_code) + Rtrim(td_reference) + Rtrim(td_booking_type)
        ) NOT IN (
        SELECT (Rtrim(td_ac_code) + Rtrim(td_ac_type) + Rtrim(td_isin_code) + Rtrim(td_reference) + Rtrim(td_booking_type))
        FROM TrxDetail(NOLOCK)
        WHERE td_curdate BETWEEN @dtFromDateFormatted AND @strToDtFormatted AND td_ac_code = a.td_ac_code AND td_ac_type = a.td_ac_type AND td_isin_code = a.
          td_isin_code AND Ltrim(Rtrim(td_reference)) <> '' AND td_booking_type = '04' AND td_narration IN ('000', '999') AND td_blocked = 'B'
        )
    ) X1, @tbl_UserList XX
  WHERE X1.CLIENTCODE = XX.ClientCODE

  UPDATE A
  SET A.ClientName = C.CM_NAME
  FROM @tbl_Transaction A, Client_Master(NOLOCK) C
  WHERE A.ClientCode = C.CM_cD

  IF @strJsonFormat = 'Y'
  BEGIN
    SET @strJsonText = (
        SELECT DISTINCT ClientCode, ClientName, [Scrip Detail] = (
            SELECT DISTINCT ISIN, ScripName, Description, [Transaction] = (
                SELECT TrxnDate, TrxnNo, Narration, DebitQty, CreditQty, Runingbalace
                FROM @tbl_Transaction xdtl
                WHERE xdtl.ClientCode = xsc.ClientCode AND ISIN = xsc.ISIN AND Description = xsc.Description
                FOR JSON PATH
                )
            FROM @tbl_Transaction xsc
            WHERE xmain.ClientCode = xsc.ClientCode
            FOR JSON PATH
            )
        FROM @tbl_Transaction xmain
        FOR JSON PATH
        )

    SELECT @strJsonText

    RETURN 1
  END

  IF ISNULL(@strOutputType, 'G') = 'G'
  BEGIN
    SELECT ClientCode, ClientName, TrxnDate, ISIN, ScripName, TrxnNo, Description, Narration, DebitCredit, DebitQty, CreditQty, Runingbalace, 
      'Scrip Details : - ' + ISIN + ' / ' + ScripName + ' / ' + Description AS GroupColumn
    FROM @tbl_Transaction
    ORDER BY SerialNo

    SET @o_vcErrorMessage = 'Process Executed'
    SET @o_vcErrorFlag = 'S'

    RETURN 1
  END
  ELSE IF ISNULL(@strOutputType, 'G') = 'X'
  BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM @tbl_Transaction
        )
    BEGIN
      SET @XMLDATA1 = (
          SELECT ClientCode = '', ClientName = '', TrxnDate = '', ISIN = '', ScripName = '', TrxnNo = '', Description = '', Narration = '', DebitCredit = '', DebitQty = 
            '', CreditQty = '', Runingbalace = '', GroupColumn = ''
          FOR XML PATH('Detail')
          )
    END
    ELSE
    BEGIN
      SET @XMLDATA1 = (
          SELECT ClientCode, ClientName, TrxnDate, ISIN, ScripName, TrxnNo, Description, Narration, DebitCredit, DebitQty, CreditQty, Runingbalace, 
            'Scrip Details : - ' + ISIN + ' / ' + ScripName + ' / ' + Description AS GroupColumn
          FROM @tbl_Transaction
          ORDER BY SerialNo
          FOR XML PATH('Detail')
          )
    END

    SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
    SET @o_vcErrorFlag = 'S'

    RETURN 1
  END
END
GO

CREATE PROCEDURE stpr_HoldingStatement @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN
  --- Parameter Declaration
  DECLARE @dtFromDate VARCHAR(8), @dtToDt VARCHAR(8), @strUserId VARCHAR(500) = '', @strLevel VARCHAR(1), @strCode VARCHAR(500), 
    @strSelection VARCHAR(100), @XMLData XML, @strOutputType VARCHAR(1) = 'G', @strRequestFrom VARCHAR(1) = 'W', @XMLDATA1 XML = '', 
    @strShowValuation VARCHAR(20) = '', @strReportType VARCHAR(30) = '', @strISIN VARCHAR(50) = '', @strBenefType VARCHAR(50) = '', @strSql1 
    NVARCHAR(MAX) = '', @strCallingFrom VARCHAR(1) = ''

  IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'

    RETURN 1
  END

  --SELECT @vcXML
  DECLARE @tbl_UserList dbo.UserAccessList;

  SET @XMLData = CAST('<root>' + @vcXML + '</root>' AS XML)

  SELECT @dtFromDate = ISNULL(x.value('(FromDate)[1]', 'VARCHAR(8)'), ''), @dtToDt = ISNULL(x.value('(ToDate)[1]', 'VARCHAR(8)'), ''), 
    @strUserId = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'), ''), @strLevel = ISNULL(x.value('(Level)[1]', 'VARCHAR(1)'), ''), 
    @strCode = ISNULL(x.value('(Code)[1]', 'VARCHAR(500)'), ''), @strSelection = ISNULL(x.value('(Selection)[1]', 'VARCHAR(50)'), ''), 
    @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'), ''), @strRequestFrom = ISNULL(x.value('(RequestFrom)[1]', 
        'VARCHAR(1)'), ''), @strShowValuation = ISNULL(x.value('(ShowValuation)[1]', 'VARCHAR(10)'), ''), @strISIN = ISNULL(x.value(
        '(ISIN)[1]', 'VARCHAR(30)'), ''), @strBenefType = ISNULL(x.value('(BenefType)[1]', 'VARCHAR(30)'), ''), @strCallingFrom = 
    ISNULL(x.value('(CallingFrom)[1]', 'VARCHAR(30)'), ''), @strReportType = ISNULL(x.value('(ReportType)[1]', 'VARCHAR(30)'), '')
  FROM @XMLData.nodes('/root') AS XTbl(x)

  IF ISNULL(@strCallingFrom, '') = ''
  BEGIN
    SET @strCallingFrom = ''
  END

  IF ISNULL(@dtFromDate, '') = ''
  BEGIN
    SELECT @dtFromDate = ISNULL(x.value('(FromDt)[1]', 'VARCHAR(8)'), '')
    FROM @XMLData.nodes('/root') AS XTbl(x)
  END

  IF ISNULL(@dtToDt, '') = ''
  BEGIN
    SELECT @dtToDt = ISNULL(x.value('(ToDt)[1]', 'VARCHAR(8)'), '')
    FROM @XMLData.nodes('/root') AS XTbl(x)
  END

  IF @strShowValuation = ''
  BEGIN
    SET @strShowValuation = 'true'
  END

  IF ISNULL(@strSelection, '') = ''
  BEGIN
    SET @strSelection = 'CLIENT'
  END

  IF ISNULL(@strCallingFrom, '') = 'T'
  BEGIN
    DECLARE @strConn1 VARCHAR(MAX) = '', @tplusUser VARCHAR(50) = ''

    SELECT @strConn1 = sp_sysvalue
    FROM Sysparameter
    WHERE sp_parmcd = 'TplusConn'

    SELECT @tplusUser = SUBSTRING(@strConn1, CHARINDEX('/', @strConn1) + 1, CHARINDEX('/', @strConn1, CHARINDEX('/', @strConn1) + 1) - 
        CHARINDEX('/', @strConn1) - 1)

    SET @strSql1 = 
      'DECLARE @tbl_AccessRights TABLE(Client VARCHAR(50), clientcode VARCHAR(20)) 
	INSERT INTO @tbl_AccessRights(Client)
	SELECT CLIENT_CODE FROM [' 
      + @tplusUser + '].DBO.fn_GetClients(''' + @strUserid + 
      ''','''','''')
	UPDATE A SET clientcode = b.da_actno
	FROM @tbl_AccessRights A, [' + @tplusUser + 
      '].DBO.DEMATACT B
	WHERE A.Client = B.da_clientcd
	AND DA_STATUS = ''A''
	AND da_defaultyn = ''Y''
	SELECT ClientCode from @tbl_AccessRights'

    INSERT INTO @tbl_UserList (ClientCode)
    EXEC (@strSql1)
  END
  ELSE
  BEGIN
    INSERT INTO @tbl_UserList
    EXEC dbo.stpr_GetClientAccessListNew @strUserid, @strSelection, @StrCode
  END

  DECLARE @TBL_CloseRate TABLE (ISIN VARCHAR(30), CloseRate MONEY)

  CREATE TABLE #tbl_HoldingRepDP (
    ClientCode VARCHAR(50), ClientName VARCHAR(100), BranchCode VARCHAR(50), ScripCode VARCHAR(50), ScripName VARCHAR(100), ISIN VARCHAR
    (20), Qty MONEY, ClosingPrice MONEY, MarketValue MONEY, AccountType VARCHAR(500)
    )

  IF @strReportType = 'ASONDATE'
  BEGIN
    CREATE TABLE [#TmpHolding] (
      [hld_hold_date] [datetime] NOT NULL, [hld_ac_code] [char](8) NOT NULL, [hld_cat] [char](2) NOT NULL, [hld_isin_code] [char](12) NOT 
      NULL, [hld_ac_type] [char](2) NOT NULL, [hld_ac_pos] [money] NOT NULL, [hld_ccid] [char](8) NULL, [hld_market_type] [char](2) NOT NULL
      , [hld_settlement] [char](7) NULL, [hld_blf] [char](1) NULL, [hld_blc] [char](2) NULL, [hld_lrd] [datetime] NULL, [hld_Rate] [money] 
      NULL
      )

    INSERT INTO #TmpHolding (
      hld_hold_date, hld_ac_code, hld_cat, hld_isin_code, hld_ac_type, hld_ac_pos, hld_ccid, hld_market_type, hld_settlement, hld_blf, 
      hld_blc, hld_lrd, hld_Rate
      )
    SELECT CAST(@dtFromDate AS DATE), td_ac_code, '02', td_isin_code, td_ac_type, Sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE (- 1) * 
            td_qty END) AS hold, '', '00', '', 'F', 0, NULL, 0 AS rate
    FROM Trxdetail x, @tbl_UserList  
    WHERE td_ac_code = clientcode AND td_curdate <= CAST(@dtFromDate AS DATE) AND td_category <> '03' 
	AND td_ac_type NOT IN ('17', '18') AND (
        td_booking_type <> '02' OR Rtrim(td_booking_type) + Rtrim(td_blockedcd) IN (
          SELECT '02' + Rtrim(blc_code)
          FROM Block_code
          WHERE blc_Flag = 'B' AND blc_YesNo = 'Y'
          )
        ) AND (Rtrim(td_ac_code) + Rtrim(td_ac_type) + Rtrim(td_isin_code) + Rtrim(td_reference) + Rtrim(td_booking_type)) NOT IN 
        (
        SELECT (Rtrim(td_ac_code) + Rtrim(td_ac_type) + Rtrim(td_isin_code) + Rtrim(td_reference) + Rtrim(td_booking_type)
            )
        FROM TrxDetail
        WHERE td_curdate <= CAST(@dtFromDate AS DATE) AND td_ac_code = x.td_ac_code AND td_ac_type = x.td_ac_type 
		AND td_isin_code = x.td_isin_code AND 
          Ltrim(Rtrim(td_reference)) <> '' AND td_booking_type = '04' AND td_narration IN ('000', '999') AND td_blocked = 'B'
        ) AND td_narration <> '001' AND td_rdate <= CAST(@dtFromDate AS DATE) 
    GROUP BY td_ac_code, td_isin_code, td_ac_type
    HAVING sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE (- 1) * td_qty END) > 0

    INSERT INTO #TmpHolding (
      hld_hold_date, hld_ac_code, hld_cat, hld_isin_code, hld_ac_type, hld_ac_pos, hld_ccid, hld_market_type, hld_settlement, hld_blf, 
      hld_blc, hld_lrd, hld_Rate
      )
    SELECT CAST(@dtFromDate AS DATE), td_ac_code, '02', td_isin_code, td_ac_type, sum(CASE td_debit_credit WHEN 'D' THEN td_qty ELSE (- 1) * 
            td_qty END) AS hold, '', '00', '', 'B', td_blockedcd, NULL, 0 AS rate
    FROM Trxdetail x, @tbl_UserList 
    WHERE td_ac_code = clientcode AND td_curdate <= CAST(@dtFromDate AS DATE) AND td_category <> '03' AND td_ac_type NOT IN ('17', '18') AND 
      td_booking_type = '02' AND (
        td_booking_type <> '02' OR Rtrim(td_booking_type) + Rtrim(td_blockedcd) IN (
          SELECT '02' + Rtrim(blc_code)
          FROM Block_code
          WHERE blc_Flag = 'B' AND blc_YesNo = 'Y'
          )
        ) AND (Rtrim(td_ac_code) + Rtrim(td_ac_type) + Rtrim(td_isin_code) + Rtrim(td_reference)) NOT IN (
        SELECT (Rtrim(td_ac_code) + Rtrim(td_ac_type) + Rtrim(td_isin_code) + Rtrim(td_reference))
        FROM TrxDetail
        WHERE td_curdate <= @dtFromDate AND td_ac_code = x.td_ac_code AND td_ac_type = x.td_ac_type AND td_isin_code = x.td_isin_code AND 
          Ltrim(Rtrim(td_reference)) <> '' AND td_booking_type = '04' AND td_narration IN ('000', '999') AND td_blocked = 'B'
        ) AND td_narration <> '001' AND td_rdate <= CAST(@dtFromDate AS DATE) 
    GROUP BY td_ac_code, td_isin_code, td_ac_type, td_blockedcd
    HAVING sum(CASE td_debit_credit WHEN 'D' THEN td_qty ELSE (- 1) * td_qty END) > 0

    INSERT INTO #TmpHolding (
      hld_hold_date, hld_ac_code, hld_cat, hld_isin_code, hld_ac_type, hld_ac_pos, hld_ccid, hld_market_type, hld_settlement, hld_blf, 
      hld_blc, hld_lrd, hld_Rate
      )
    SELECT @dtFromDate, td_ac_code, '02', td_isin_code, td_ac_type, sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE (- 1) * 
            td_qty END) AS hold, '', '00', '', td_blocked, 0, convert(CHAR, td_rdate, 112), 0 AS rate
    FROM Trxdetail x, @tbl_UserList
    WHERE td_ac_code = clientcode AND td_curdate <= @dtFromDate AND td_category <> '03' AND td_ac_type IN ('17', '18', '23', '26') AND (
        td_booking_type <> '02' OR Rtrim(td_booking_type) + Rtrim(td_blockedcd) IN (
          SELECT '02' + Rtrim(blc_code)
          FROM Block_code
          WHERE blc_Flag = 'B' AND blc_YesNo = 'Y'
          )
        ) AND (Rtrim(td_ac_code) + Rtrim(td_ac_type) + Rtrim(td_isin_code) + Rtrim(td_reference) + Rtrim(td_booking_type)) NOT IN 
        (
        SELECT (Rtrim(td_ac_code) + Rtrim(td_ac_type) + Rtrim(td_isin_code) + Rtrim(td_reference) + Rtrim(td_booking_type)
            )
        FROM TrxDetail
        WHERE td_curdate <= @dtFromDate AND td_ac_code = x.td_ac_code AND td_ac_type = x.td_ac_type AND td_isin_code = x.td_isin_code AND 
          Ltrim(Rtrim(td_reference)) <> '' AND td_booking_type = '04' AND td_narration IN ('000', '999') AND td_blocked = 'B'
        ) AND td_narration <> '001' AND td_rdate > CAST(@dtFromDate AS DATE) 
    GROUP BY td_ac_code, td_isin_code, td_blocked, td_rdate, td_ac_type
    HAVING sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE (- 1) * td_qty END) > 0

    INSERT INTO #TmpHolding (
      hld_hold_date, hld_ac_code, hld_cat, hld_isin_code, hld_ac_type, hld_ac_pos, hld_ccid, hld_market_type, hld_settlement, hld_blf, 
      hld_blc, hld_lrd, hld_Rate
      )
    SELECT @dtFromDate, td_ac_code, '03', td_isin_code, td_ac_type, sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE (- 1) * 
            td_qty END) AS hold, td_clear_corpn, td_market_type, td_settlement, 'F', 0, NULL, 0 AS rate
    FROM Trxdetail, @tbl_UserList
    WHERE td_ac_code = clientcode AND td_curdate <= @dtFromDate AND td_category = '03' AND td_ac_type <> '20' 
	AND td_narration NOT IN ('201') 
    GROUP BY td_ac_code, td_clear_corpn, td_isin_code, td_settlement, td_market_type, td_ac_type
    HAVING sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE (- 1) * td_qty END) > 0
  END

  IF @strReportType = 'UPTODATE'
  BEGIN
    INSERT INTO #tbl_HoldingRepDP (ClientCode, ClientName, BranchCode, ScripName, ISIN, AccountType, Qty, ClosingPrice, MarketValue
      )
    SELECT cm_cd AS ClientCode, cm_name AS ClientName, cm_brboffcode AS BranchCode, ScripName, td_isin_code AS ISIN, bt_description, 
      BalanceQty AS Qty, 0, 0
    FROM (
      SELECT td_ac_code, td_isin_code, ScripName, bt_description, td_ac_type, BalanceQty = sum(BalanceQty)
      FROM (
        SELECT td_ac_code = hld_ac_code, ScripName = sc_company_name, td_isin_code = hld_isin_code, td_ac_type = hld_ac_type, SUM(
            hld_ac_pos) AS BalanceQty
        FROM DBO.Holding(NOLOCK), DBO.Security(NOLOCK), @tbl_UserList x
        WHERE hld_ac_code = x.clientcode AND hld_isin_code = sc_isincode
        GROUP BY hld_ac_code, hld_isin_code, hld_ac_type, sc_company_name
        HAVING SUM(hld_ac_pos) <> 0
        ) x1
      LEFT JOIN DBO.Beneficiary_type BN ON (td_ac_type = BN.bt_code)
      GROUP BY td_ac_code, td_isin_code, bt_description, td_ac_type, ScripName
      ) A, [dbo].client_master(NOLOCK)
    WHERE cm_schedule = (
        SELECT sp_sysvalue
        FROM Sysparameter
        WHERE sp_parmcd = 'cmschedule'
        ) AND td_ac_code = cm_cd AND ((td_isin_code = @strISIN AND @strISIN <> '') OR @strISIN = '') 
	 AND ((td_ac_type = @strBenefType AND @strBenefType <> '') 
	 OR @strBenefType = '')
  END
  ELSE IF @strReportType = 'ASONDATE'
  BEGIN
    INSERT INTO #tbl_HoldingRepDP (ClientCode, ClientName, BranchCode, ScripName, ISIN, AccountType, Qty, ClosingPrice, MarketValue
      )
    SELECT cm_cd AS ClientCode, cm_name AS ClientName, cm_brboffcode AS BranchCode, ScripName, td_isin_code AS ISIN, bt_description, 
      BalanceQty AS Qty, 0, 0
    FROM (
      SELECT td_ac_code, td_isin_code, ScripName, bt_description, td_ac_type, BalanceQty = sum(BalanceQty)
      FROM (
        SELECT td_ac_code = hld_ac_code, ScripName = sc_company_name, td_isin_code = hld_isin_code, td_ac_type = hld_ac_type, SUM(
            hld_ac_pos) AS BalanceQty
        FROM #TmpHolding, DBO.Security(NOLOCK), @tbl_UserList x
        WHERE hld_ac_code = x.clientcode AND hld_isin_code = sc_isincode
        GROUP BY hld_ac_code, hld_isin_code, hld_ac_type, sc_company_name
        HAVING SUM(hld_ac_pos) <> 0
        ) x1
      LEFT JOIN DBO.Beneficiary_type BN ON (td_ac_type = BN.bt_code)
      GROUP BY td_ac_code, td_isin_code, bt_description, td_ac_type, ScripName
      ) A, [dbo].client_master(NOLOCK)
    WHERE cm_schedule = (
        SELECT sp_sysvalue
        FROM Sysparameter
        WHERE sp_parmcd = 'cmschedule'
        ) AND td_ac_code = cm_cd AND ((td_isin_code = @strISIN AND @strISIN <> '') OR @strISIN = '') 
		AND ((td_ac_type = @strBenefType AND @strBenefType <> '') OR @strBenefType = '')
		
	DROP TABLE #TmpHolding
		
  END

  IF ISNULL(@StrShowValuation, 'false') = 'true'
  BEGIN
    DECLARE @dtMax DATE

    SELECT @dtMax = max(rm_trx_date)
    FROM DBO.Rate_master
    WHERE rm_trx_date <= CAST(@dtFromDate AS DATE)

    INSERT INTO @TBL_CloseRate (ISIN, CloseRate)
    SELECT rm_isin_code, rm_rate
    FROM DBO.Rate_master(NOLOCK) X
    WHERE rm_trx_date = @dtMax AND EXISTS (
        SELECT 1
        FROM #tbl_HoldingRepDP
        WHERE ISIN = X.rm_isin_code
        )

    UPDATE A
    SET A.ClosingPrice = B.CloseRate
    FROM #tbl_HoldingRepDP A, @TBL_CloseRate b
    WHERE A.ISIN = B.ISIN

    INSERT INTO @TBL_CloseRate (ISIN, CloseRate)
    SELECT rm_isin_code, rm_rate
    FROM DBO.Rate_master(NOLOCK) X, (
        SELECT DISTINCT ISIN
        FROM #tbl_HoldingRepDP
        WHERE isnull(ClosingPrice, 0) = 0
        ) B
    WHERE x.rm_isin_code = B.ISIN AND rm_trx_date = (
        SELECT max(rm_trx_date)
        FROM DBO.Rate_master
        WHERE rm_trx_date <= CAST(@dtFromDate AS DATE) AND rm_isin_code = X.rm_isin_code
        )

    UPDATE A
    SET A.ClosingPrice = B.CloseRate
    FROM #tbl_HoldingRepDP A, @TBL_CloseRate b
    WHERE A.ISIN = B.ISIN AND isnull(ClosingPrice, 0) = 0

    IF ISNULL(@strOutputType, 'G') = 'G'
    BEGIN
      SELECT ClientCode, ClientName, [ISINName] = ScripName, ISIN, [BalanceType] = AccountType, Qty, Rate = ClosingPrice, [Value] = ROUND(Qty 
          * ClosingPrice, 2)
      FROM #tbl_HoldingRepdp
      ORDER BY ClientCode, AccountType, ScripName

      DROP TABLE #tbl_HoldingRepdp

      SET @o_vcErrorMessage = 'Process Executed'
      SET @o_vcErrorFlag = 'S'

      RETURN 1
    END
    ELSE IF ISNULL(@strOutputType, 'G') = 'X'
    BEGIN
      IF NOT EXISTS (
          SELECT 1
          FROM #tbl_HoldingRepdp
          )
      BEGIN
        SET @XMLDATA1 = (
            SELECT ClientCode = '', ClientName = '', ISINName = '', ISIN = '', BalanceType = '', Qty = '', Rate = '', [Value] = ''
            FOR XML PATH('Detail')
            )
      END
      ELSE
      BEGIN
        SET @XMLDATA1 = (
            SELECT ClientCode, ClientName, ISINName = ScripName, ISIN, BalanceType = AccountType, Qty, Rate = ClosingPrice, [Value] = ROUND(
                Qty * ClosingPrice, 2)
            FROM #tbl_HoldingRepdp
            ORDER BY ClientCode, AccountType, ScripName
            FOR XML PATH('Detail')
            )
      END

      DROP TABLE #tbl_HoldingRepdp

      SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
      SET @o_vcErrorFlag = 'S'

      RETURN 1
    END
  END
  ELSE
  BEGIN
    IF ISNULL(@strOutputType, 'G') = 'G'
    BEGIN
      SELECT ClientCode, ClientName, [ISIN Name] = ScripName, ISIN, [Balance Type] = AccountType, Qty
      FROM #tbl_HoldingRepdp
      ORDER BY ClientCode, AccountType, ScripName

      DROP TABLE #tbl_HoldingRepdp

      SET @o_vcErrorMessage = 'Process Executed'
      SET @o_vcErrorFlag = 'S'

      RETURN 1
    END
    ELSE IF ISNULL(@strOutputType, 'G') = 'X'
    BEGIN
      IF NOT EXISTS (
          SELECT 1
          FROM #tbl_HoldingRepdp
          )
      BEGIN
        SET @XMLDATA1 = (
            SELECT ClientCode = '', ClientName = '', ISINName = '', ISIN = '', BalanceType = '', Qty = ''
            FOR XML PATH('Detail')
            )
      END
      ELSE
      BEGIN
        SET @XMLDATA1 = (
            SELECT ClientCode, ClientName, ISINName = ScripName, ISIN, BalanceType = AccountType, Qty
            FROM #tbl_HoldingRepdp
            ORDER BY ClientCode, AccountType, ScripName
            FOR XML PATH('Detail')
            )
      END

      DROP TABLE #tbl_HoldingRepdp

      SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
      SET @o_vcErrorFlag = 'S'

      RETURN 1
    END
  END
END
GO

CREATE PROCEDURE [dbo].[stpr_trxholdbill] @strClientCode VARCHAR(8), @strFromDt CHAR(8), @strToDt CHAR(8)
AS
BEGIN
	DECLARE @strCmSchedule VARCHAR(25) = (SELECT TOP 1 sp_sysvalue
			FROM sysparameter(NOLOCK)
			WHERE sp_parmcd = 'CMSCHEDULE')
	DECLARE @strBillDueDate VARCHAR(25) = (SELECT TOP 1 sp_sysvalue
			FROM sysparameter(NOLOCK)
			WHERE sp_parmcd = 'BILLDUEDATE')
	DECLARE @intBillDueDate INT = (SELECT CASE WHEN @strBillDueDate <> '' THEN CAST(@strBillDueDate AS INT) ELSE 0 END)

	SELECT cm_cd BOID, cm_name BOName, cm_add1 Add1, cm_add2 Add2, cm_add3 Add3, cm_add4 Add4, cm_pin Pin, cm_tele1 
		Phone, CASE cm_sech_name WHEN '' THEN '--' ELSE cm_sech_name + ', ' END + cm_thih_name AS 'Joints', 
		cm_familycd + ' (' + (SELECT fm_desc FROM Family_master(NOLOCK) WHERE fm_cd = cm_familycd ) + ')' AS FamilyCode, 
		(SELECT bt_description FROM Beneficiary_type(NOLOCK) WHERE bt_code = cm_clienttype ) AS Type, 
		(SELECT bs_description FROM Beneficiary_status(NOLOCK) WHERE bs_code = cm_active) AS STATUS, 
		(SELECT bc_description FROM Beneficiary_category(NOLOCK) WHERE bc_code = cm_acctype) AS 'Category', 
		CONVERT(VARCHAR(10), CAST(@strFromDt AS DATE), 103) AS 'From', 
		CONVERT(VARCHAR(10), CAST(@strToDt AS DATE), 103) AS 'To', cm_brboffcode + ' (' + 
		(SELECT bm_branchname FROM Branch_master(NOLOCK) WHERE bm_branchcd = cm_brboffcode) + ')' AS 'BranchCode', 
		cm_groupcd + ' (' + (SELECT gr_desc FROM Group_master(NOLOCK) WHERE gr_cd = cm_groupcd) + ')' AS 'GroupCode'
	FROM Client_master(NOLOCK), Client_Backoffice(NOLOCK)
	WHERE cm_cd = cb_cmcd AND cm_cd = @strClientCode

	--Transaction
	
	DECLARE @strFromDtFormatted AS CHAR(10) = CAST(@strFromDt AS DATE)
	DECLARE @strToDtFormatted AS CHAR(10) = CAST(@strToDt AS DATE)

	SELECT
		ClientCode, Date, ISIN, ISINName,
		Type, Particular, Debit = (CASE WHEN Balance < 0 THEN (Balance * - 1) ELSE 0 END
			), Credit = (CASE WHEN Balance > 0 THEN Balance ELSE 0 END), 
			Balance = sum(Balance) OVER (PARTITION BY ClientCode, ISIN, Type
			ORDER BY ClientCode, ISINName, ActType, MarketType, Settlement, TAG, SerialNo)
	FROM ( SELECT Tag = 1, td_ac_code AS 'ClientCode', @strFromDt AS 'Date', td_isin_code AS 'ISIN', sc_company_name 
			AS 'ISINName', td_ac_type AS 'ActType', '' AS 'MarketType', '' AS 'Settlement', 
			bt_description AS 'Type', 'Opening Balance' AS 'Particular', sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE td_qty * (- 1
										) END) AS 'Balance', SerialNo = 0
		FROM Trxdetail(NOLOCK) a
		LEFT JOIN Market_type(NOLOCK) ON (td_market_type = mt_code 
		AND LTRIM(RTRIM(ISNULL(td_clear_corpn, ''))) = LTRIM(RTRIM(mt_ccid))), 
		Security(NOLOCK), Client_master(NOLOCK), Client_Backoffice(NOLOCK), Narration(NOLOCK), Beneficiary_type(NOLOCK), 
			Beneficiary_category(NOLOCK), Beneficiary_status(NOLOCK)
		WHERE td_ac_code = cm_cd AND cm_cd = cb_cmcd AND bc_code = td_category AND bs_code = cm_active AND 
			td_isin_code = sc_isincode AND a.td_ac_code = @strClientCode AND td_narration = nr_code AND 
			cm_statements <> 'N' AND td_ac_type = bt_code AND td_Narration NOT IN ('001') AND 
			td_ac_type <> '30' AND (td_booking_type <> '02' OR Rtrim(td_booking_type) + Rtrim(td_blockedcd) IN (
					SELECT '02' + Rtrim(blc_code)
					FROM Block_code(NOLOCK)
					WHERE blc_Flag = 'B' AND blc_YesNo = 'Y')
				) AND td_curdate < @strFromDt
		GROUP BY td_ac_code, td_isin_code, sc_company_name, td_ac_type,  bt_description
        HAVING sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE td_qty * (- 1) END) <> 0
			
		
		UNION ALL
		
		SELECT Tag = 2, cm_cd AS 'ClientCode', CONVERT(CHAR(8), td_curdate, 112) AS 'Date', td_isin_code AS 'ISIN'
			, sc_company_name AS 'ISINName', td_ac_type AS 'ActType', td_market_type AS 'MarketType', 
			td_settlement AS 'Settlement', bt_description AS 'Type',
			--nr_description AS 'Particular', 
			CASE td_debit_credit WHEN 'D' THEN 'To ' ELSE 'By ' END + COALESCE(CASE WHEN (td_narration IN ('042', '044')
							) AND td_market_type != '' AND LTRIM(RTRIM(td_settlement)) != '' AND (td_booking_type != '02' OR td_booking_type != '03'
							) THEN CASE WHEN td_countercmbpid IS NULL OR LTRIM(RTRIM(td_countercmbpid)) = '' 
									THEN CASE WHEN (
													SELECT  TOP 1 bp_role
													FROM Bpmaster(NOLOCK)
													WHERE bp_id = td_counterdp
													) = '03' THEN 'CM ' + LEFT(LTRIM(RTRIM((
																	SELECT bp_name
																	FROM Bpmaster(NOLOCK)
																	WHERE bp_id = td_counterdp
																	))), 25) + ' ' ELSE (
													SELECT  TOP 1 bp_name
													FROM Bpmaster(NOLOCK)
													WHERE bp_id = td_counterdp
													) + '/ ' + CASE WHEN td_billcode IN ('11542', '11543'
															) THEN (
																SELECT  TOP 1 bp_name
																FROM Bpmaster(NOLOCK)
																WHERE bp_id = td_counterdp
																) + ' eDIS/Block Mechanism / ' + (
																SELECT  TOP 1 mt_description
																FROM Market_type
																WHERE mt_code = LTRIM(RTRIM(
																			td_market_type))
																) + LTRIM(RTRIM(td_settlement)) ELSE '' 
													END END ELSE CASE WHEN (
												SELECT  TOP 1 bp_role
												FROM Bpmaster(NOLOCK)
												WHERE bp_id = td_countercmbpid
												) = '03' THEN 'CM ' + LEFT(LTRIM(RTRIM((
																SELECT  TOP 1 bp_name
																FROM Bpmaster(NOLOCK)
																WHERE bp_id = td_countercmbpid
																))), 25) + ' ' ELSE (
												SELECT  TOP 1 bp_name
												FROM Bpmaster(NOLOCK)
												WHERE bp_id = td_countercmbpid
												) + '/ ' + CASE WHEN td_billcode IN ('11542', '11543'
														) THEN (
															SELECT  TOP 1 bp_name
															FROM Bpmaster(NOLOCK)
															WHERE bp_id = td_countercmbpid
															) + ' eDIS/Block Mechanism / ' + (
															SELECT TOP 1 mt_description
															FROM Market_type
															WHERE mt_code = LTRIM(RTRIM(td_market_type)
																)
															) + LTRIM(RTRIM(td_settlement)) ELSE '' END 
										END END ELSE CASE WHEN (
									td_narration IN ('042', '044'
										)
									) AND td_market_type = '' AND td_settlement = '' THEN CASE WHEN td_counterdp 
											<> '' THEN (
													SELECT TOP 1 bp_name
													FROM Bpmaster(NOLOCK)
													WHERE bp_id = td_counterdp
													) ELSE nr_description END ELSE CASE WHEN td_narration = 
										'307' AND td_ac_type = '22' THEN 'Pledge Request ' + (
												SELECT TOP 1 bp_name
												FROM Bpmaster(NOLOCK)
												WHERE bp_id = td_counterdp
												) ELSE CASE WHEN td_narration = '307' AND td_ac_type = '26' 
												THEN 'Pledge Request' ELSE CASE WHEN td_narration = '307' 
														AND td_ac_type = '29' THEN 'Pledge Request' ELSE 
														CASE WHEN td_narration = '091' AND td_ac_type = '26' 
																THEN 'Pledge ' + (
																		SELECT TOP 1 bp_name
																		FROM Bpmaster(NOLOCK)
																		WHERE bp_id = td_counterdp
																		) ELSE CASE WHEN td_narration = 
																		'091' AND td_ac_type = '22' THEN 
																			'Pledge ' + (
																				SELECT TOP 1 bp_name
																				FROM Bpmaster(NOLOCK)
																				WHERE bp_id = 
																					td_counterdp
																				) ELSE CASE WHEN 
																				td_narration = '092' 
																				THEN 'Pledge Closure' 
																			ELSE CASE WHEN td_narration 
																						= '321' OR 
																						td_narration 
																						= '309' THEN 
																							'Pledge Request ' 
																							+ (
																									SELECT TOP 1 
																									bp_name
																								FROM 
																									Bpmaster(NOLOCK)
																								WHERE 
																									bp_id 
																									= 
																									td_counterdp
																								) 
																					ELSE NULL END END 
																	END END END END END END END, CASE WHEN 
						td_narration = '093' THEN nr_description + (
								SELECT TOP 1 bp_name
								FROM Bpmaster(NOLOCK)
								WHERE bp_id = td_counterdp
								) ELSE CASE WHEN td_narration = '201' OR td_narration = '202' THEN '' ELSE CASE 
									WHEN td_narration = '203' AND td_booking_type = '02' THEN 
											'Inter Dp Rejection' ELSE CASE WHEN td_narration = '012' AND 
												td_debit_credit = 'C' THEN 'Dematerilization' ELSE CASE 
													WHEN td_narration = '012' AND td_debit_credit = 'D' THEN 
															'Dematerialisation request confirmation' 
													ELSE CASE WHEN td_narration = '011' AND td_debit_credit 
																= 'C' THEN 'Dematerilization Request' 
															ELSE CASE WHEN td_narration = '204' THEN 
																			'Inter Depository transfer CDS' 
																	ELSE CASE WHEN td_narration = '231' 
																				AND td_booking_type = 
																				'02' THEN 'Freeze [' + (
																						SELECT 
																							TOP 1 blc_description
																						FROM 
																							Block_code(NOLOCK)
																						WHERE 
																							blc_flag 
																							= 
																							td_blocked 
																							AND 
																							blc_code 
																							= 
																							td_blockedcd
																						) + ']' ELSE 
																				CASE WHEN td_narration 
																						= '082' THEN 
							DBO.fn_FindCorporateAction(td_isin_code,td_Narration,td_market_type, td_settlement, CONVERT(varchar(8), td_curdate, 112), td_debit_credit,td_reference)
                            														END END END END END 
											END END END END, CASE WHEN td_narration = '083' THEN 
							DBO.fn_FindCorporateAction(td_isin_code,td_Narration,td_market_type, td_settlement, CONVERT(varchar(8), td_curdate, 112), td_debit_credit,td_reference) 
							ELSE CASE WHEN td_narration = '999' OR (
									td_narration = '000' AND td_booking_type = '02' AND td_blocked = 'B' AND 
									td_market_type <> ''
									) THEN (
										SELECT TOP 1 bp_name
										FROM Bpmaster(NOLOCK)
										WHERE bp_id = td_clear_corpn
										) + '/' + mt_description + '/' + td_settlement ELSE CASE WHEN 
										td_billcode = '11551' OR td_billcode = '11552' THEN 
											'Margin Re-pledge ' + td_counterdp + ' / ' ELSE CASE WHEN 
												td_billcode = '11553' OR td_billcode = '11554' THEN 
													'Margin Re-pledge ' + td_counterdp + ' / ' ELSE 
												nr_description END END END END) + ' ' + CASE WHEN 
					td_beneficiery <> '00000000' THEN CASE WHEN td_clear_corpn <> '' THEN '' ELSE td_beneficiery 
							END ELSE CASE WHEN td_counterdp = 'IN000026' AND td_cds <> '' THEN td_cds ELSE '' END 
				END AS Particular, (CASE td_debit_credit WHEN 'C' THEN td_qty ELSE td_qty * (- 1) END
				) AS 'Balance', SerialNo = ROW_NUMBER() OVER (
				ORDER BY cm_cd, td_curdate
				)
		FROM Trxdetail(NOLOCK) a
		LEFT JOIN Market_type ON (td_market_type = mt_code AND ltrim(rtrim(isnull(td_clear_corpn, ''))) = ltrim(rtrim(mt_ccid))), 
		Security(NOLOCK), Client_master(NOLOCK), Client_Backoffice(NOLOCK), Narration(NOLOCK), Beneficiary_type(NOLOCK), 
			Beneficiary_category(NOLOCK), Beneficiary_status(NOLOCK)
		WHERE td_ac_code = cm_cd AND cm_cd = cb_cmcd AND bc_code = td_category AND bs_code = cm_active AND 
			td_isin_code = sc_isincode AND a.td_ac_code = @strClientCode AND td_narration = nr_code AND 
			cm_statements <> 'N' AND td_ac_type = bt_code AND td_Narration NOT IN ('001') AND 
			td_ac_type <> '30' AND (
				td_booking_type <> '02' OR Rtrim(td_booking_type) + Rtrim(td_blockedcd) IN (
					SELECT '02' + Rtrim(blc_code)
					FROM Block_code(NOLOCK)
					WHERE blc_Flag = 'B' AND blc_YesNo = 'Y'
					)) AND td_curdate BETWEEN @strFromDtFormatted 
					   AND @strToDtFormatted AND (
				Rtrim(td_ac_code) + Rtrim(td_ac_type) + Rtrim(td_isin_code) + Rtrim(td_reference) + Rtrim(
					td_booking_type)
				) NOT IN (
				SELECT (
						Rtrim(td_ac_code) + Rtrim(td_ac_type) + Rtrim(td_isin_code) + Rtrim(td_reference) + 
						Rtrim(td_booking_type)
						)
				FROM TrxDetail(NOLOCK)
				WHERE td_curdate BETWEEN @strFromDtFormatted AND @strToDtFormatted AND td_ac_code = a.
					td_ac_code AND td_ac_type = a.td_ac_type AND td_isin_code = a.td_isin_code AND Ltrim(Rtrim
						(td_reference)) <> '' AND td_booking_type = '04' AND td_narration IN ('000', '999'
						) AND td_blocked = 'B'
				)
		) X1

	-- Holding
	
	SELECT ClientCode, ISIN, ISINName, BalanceCode, BalanceType, Quantity = SUM(Quantity), Rate = MAX(Rate), Value = 
	SUM(Quantity * Rate)
    FROM (
	SELECT td_ac_code AS 'ClientCode', td_isin_code AS 'ISIN', sc_company_name AS 'ISINName', bt_code AS 
		BalanceCode, bt_description AS 'BalanceType', sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE td_qty 
					* (- 1) END) AS 'Quantity', Rate = IsNull((
				SELECT rm_rate
				FROM Rate_master(NOLOCK)
				WHERE rm_isin_code = td_isin_code AND rm_trx_date = (
						SELECT Max(rm_trx_date)
						FROM Rate_master(NOLOCK)
						WHERE rm_isin_code = td_isin_code AND rm_trx_date <= @strToDt
						)
				), 0)
	FROM Trxdetail(NOLOCK) A
	LEFT JOIN Security(NOLOCK) ON (td_isin_code = sc_isincode), Client_master, Beneficiary_type
	WHERE cm_cd = td_ac_code AND bt_code = td_ac_type AND td_Narration NOT IN ('001') AND (
			td_booking_type <> '02' OR Rtrim(td_booking_type) + Rtrim(td_blockedcd) IN (
				SELECT '02' + Rtrim(blc_code)
				FROM Block_code(NOLOCK)
				WHERE blc_Flag = 'B' AND blc_YesNo = 'Y'
				)
			) AND td_curdate <= CAST(@strToDt AS DATE) AND td_ac_code = @strClientCode
		AND cm_schedule = @strCmSchedule
		AND (
				Rtrim(td_ac_code) + Rtrim(td_ac_type) + Rtrim(td_isin_code) + Rtrim(td_reference) + Rtrim(
					td_booking_type)
				) NOT IN (
				SELECT (
						Rtrim(td_ac_code) + Rtrim(td_ac_type) + Rtrim(td_isin_code) + Rtrim(td_reference) + 
						Rtrim(td_booking_type)
						)
				FROM TrxDetail(NOLOCK)
				WHERE td_curdate BETWEEN @strFromDtFormatted AND @strToDtFormatted AND td_ac_code = a.
					td_ac_code AND td_ac_type = a.td_ac_type AND td_isin_code = a.td_isin_code AND Ltrim(Rtrim
						(td_reference)) <> '' AND td_booking_type = '04' AND td_narration IN ('000', '999'
						) AND td_blocked = 'B'
				)
	GROUP BY td_ac_code, td_isin_code, sc_company_name, bt_code, bt_description
	) X1
   GROUP BY ClientCode, ISIN, ISINName, BalanceCode, BalanceType
   HAVING SUM(Quantity) >  0


   -- Bill
	
   SELECT bl_bill_no AS 'Bill No', Convert(CHAR, bl_bill_from, 112) AS FromDate, Convert(CHAR, bl_bill_to, 112) AS 
   ToDate, Convert(CHAR, bl_bill_dt, 112) AS BillDate, 
   CONVERT(CHAR(8), DATEADD(DAY, @intBillDueDate, bl_bill_dt), 112) AS DueDate, 
   bl_amount AS BillAmount
   FROM Billing(NOLOCK), Client_master(NOLOCK)
   WHERE bl_client_id = cm_cd 
   AND cm_cd          = @strClientCode 
   AND cm_acctype     <> '03' 
   AND cm_active IN ('01') 
   AND bl_bill_dt BETWEEN @strFromDt AND @strToDt

END
GO