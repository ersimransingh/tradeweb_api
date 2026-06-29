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

CREATE Function [dbo].[fn_ParameterXMLRep] (@vcXML XML)  RETURNS @tbl_ParameterList TABLE(J_Ui VARCHAR(MAX), SSql VARCHAR(MAX), X_Filter VARCHAR(MAX), X_GFilter VARCHAR(MAX),
J_Api VARCHAR(MAX)) AS
BEGIN
   INSERT INTO @tbl_ParameterList(J_Ui, SSql, X_Filter, X_GFilter, J_Api)
   SELECT strJ_Ui =  ISNULL(x.value('(J_Ui)[1]', 'VARCHAR(MAX)'),''),
   strSql = ISNULL(x.value('(Sql)[1]', 'VARCHAR(MAX)'),''),
   strX_Filter = cast(@vcXML.query('/dsXml/X_Filter') as varchar(max)),
   strX_GFilter = cast(@vcXML.query('/dsXml/X_GFilter') as varchar(max)),
   strJ_Api = ISNULL(x.value('(J_Api)[1]', 'VARCHAR(MAX)'),'')
   FROM @vcXML.nodes('/dsXml') AS XTbl(x) 
   RETURN 
END
GO

CREATE FUNCTION [dbo].[fn_SplitString] (@input NVARCHAR(MAX), @delimiter CHAR(1))
RETURNS @output TABLE (Position INT, Value NVARCHAR(MAX))
AS
BEGIN
	DECLARE @start INT, @end INT, @position INT

	SET @start = 1
	SET @position = 1

	WHILE @start <= LEN(@input)
	BEGIN
		SET @end = CHARINDEX(@delimiter, @input, @start)

		IF @end = 0
			SET @end = LEN(@input) + 1

		INSERT INTO @output (Position, Value)
		VALUES (@position, SUBSTRING(@input, @start, @end - @start))

		SET @start = @end + 1
		SET @position = @position + 1
	END

	RETURN
END
GO

CREATE FUNCTION [dbo].[fnGetAccYearFromDate] (@strDate VARCHAR(10))
RETURNS VARCHAR(10)
AS
BEGIN
    DECLARE @lngFrom INT;
    DECLARE @lngTo INT;
    DECLARE @lngTemp INT;
    DECLARE @strAccYearFromDate VARCHAR(10);

    DECLARE @ACPERIOD VARCHAR(5);
    SELECT @ACPERIOD = sp_sysvalue FROM Sysparameter(NOLOCK) 
    WHERE sp_parmcd='ACPERIOD'

    SET @lngFrom = CAST(LEFT(@ACPERIOD, 2) AS INT);
    SET @lngTo = CAST(RIGHT(@ACPERIOD, 2) AS INT);

    IF @lngTo = 12
        SET @lngTemp = 0;
    ELSE
        SET @lngTemp = 1;

    IF CAST(SUBSTRING(@strDate, 4, 2) AS INT) > @lngTo
    BEGIN
        SET @strAccYearFromDate = 
            RIGHT(@strDate, 2) + 
            RIGHT('00' + CAST(@lngFrom AS VARCHAR), 2) + 
            RIGHT('00' + CAST(CAST(RIGHT(@strDate, 2) AS INT) + @lngTemp AS VARCHAR), 2) + 
            RIGHT('00' + CAST(@lngTo AS VARCHAR), 2);
    END
    ELSE
    BEGIN
        SET @strAccYearFromDate = 
            RIGHT('00' + CAST(CAST(RIGHT(@strDate, 4) AS INT) - @lngTemp AS VARCHAR), 2) + 
            RIGHT('00' + CAST(@lngFrom AS VARCHAR), 2) + 
            RIGHT(@strDate, 2) + 
            RIGHT('00' + CAST(@lngTo AS VARCHAR), 2);
    END

    RETURN @strAccYearFromDate;
END;
GO

CREATE FUNCTION [dbo].[fnStampDutyRate] (@strSecType NVARCHAR(10))
RETURNS FLOAT
AS
BEGIN
    DECLARE @dblRate FLOAT = 0;
    DECLARE @strPer NVARCHAR(MAX);
    DECLARE @strPerParts TABLE (Id INT IDENTITY(1, 1), Value NVARCHAR(MAX));

    -- Retrieve the value of the STAMPDUTY system parameter
    SET @strPer = ISNULL((SELECT LTRIM(RTRIM(sp_sysvalue)) from Sysparameter(NOLOCK) 
	where sp_parmcd='STAMPDUTY'),'') 

    -- Split the parameter into parts using ~ as the delimiter
    INSERT INTO @strPerParts (Value)
    SELECT value
    FROM ReturnTable(@strPer, '~');

    -- Determine the appropriate rate based on @strSecType
    IF CHARINDEX(',' + LTRIM(RTRIM(@strSecType)) + ',', ',12,13,14,15,19,') > 0
    BEGIN
        SELECT TOP 1 @dblRate = TRY_CAST(Value AS FLOAT)
        FROM @strPerParts
        WHERE Id = 2; -- Second part (index starts from 1)
    END
    ELSE IF CHARINDEX(',' + LTRIM(RTRIM(@strSecType)) + ',', ',16,20,25,') > 0
    BEGIN
        SELECT TOP 1 @dblRate = TRY_CAST(Value AS FLOAT)
        FROM @strPerParts
        WHERE Id = 3; -- Third part
    END
    ELSE
    BEGIN
        SELECT TOP 1 @dblRate = TRY_CAST(Value AS FLOAT)
        FROM @strPerParts
        WHERE Id = 1; -- First part
    END;

    RETURN @dblRate;
END;
GO

CREATE PROCEDURE stpr_GetClientAccessListNew @i_vcUserid VARCHAR(500), @i_vcSelection VARCHAR(50), @i_vcCode VARCHAR(20) WITH ENCRYPTION AS
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
		ELSE IF @strUserType = 'R'
	    BEGIN
	      INSERT INTO #tb_UserList1(ClientCode, ClientName)
	      SELECT DISTINCT cm_cd, cm_name from client_master(nolock) WHERE cm_remisser = @strUserAccessValue  and cm_schedule = @strCMSCHEDULE
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

CREATE PROCEDURE stpr_TypestHeaderProc @strUserid  VARCHAR(100), @strSelection  VARCHAR(100), @strCode   VARCHAR(100), 
@o_Message VARCHAR(MAX) OUTPUT 
WITH ENCRYPTION
AS
BEGIN
  
  DECLARE @strBLAddress1 VARCHAR(200), @strBLAddress2 VARCHAR(200), @strBLAddress3 VARCHAR(200), @strBLAddress4 VARCHAR(200)
  
  DECLARE @tbl_UserList dbo.UserAccessList;
  INSERT INTO @tbl_UserList EXEC dbo.stpr_GetClientAccessListNew @strUserid , @strSelection, @strCode 
  
    
  DECLARE @XMLDATA XML, @XMLDATA1 XML
	
  SET @XMLDATA = (SELECT ClientCode, [CompanyName] = ISNULL((SELECT sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd ='NAME'),''),
  [Address1] = ISNULL((SELECT sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd ='ADD1'),''),
  [Address2] = ISNULL((SELECT sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd ='ADD2'),''),
  [Address3] = ISNULL((SELECT sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd ='ADD3'),''),
  [CINNo] = ISNULL((SELECT sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd ='CINNO'),''),  
  [BillingAddress] = ISNULL((SELECT bm_add1+' '+bm_add2+' '+bm_add3+' '+bm_city+' '+bm_pin+' '+bm_phone 
  FROM Branch_master(NOLOCK) WHERE bm_branchcd = cm_brboffcode),''),
  [ReportName] = '##ReportName##',
  [Client] = CM_NAME+' ( '+CM_CD+' )',
  [ClientAddress1] = cm_add1, [ClientAddress2] = cm_add2, [ClientAddress3] = cm_add3,
  [ClientCity] = cm_city, [Clientpin] = cm_pin, [Telephone] = cm_mobile, 
  [status] = (SELECT bs_description FROM Beneficiary_status(NOLOCK) WHERE bs_code = cm_active),
  [Category] = (SELECT bc_description FROM Beneficiary_category(NOLOCK) WHERE bc_code = cm_acctype), 
  [Scheme] = cm_chgsscheme, [Branch] = cm_brboffcode + ' (' + 
  (SELECT bm_branchname FROM Branch_master(NOLOCK) WHERE bm_branchcd = cm_brboffcode) + ')',
  [GroupCode] = cm_groupcd + ' (' + (SELECT gr_desc FROM Group_master(NOLOCK) WHERE gr_cd = cm_groupcd) + ')' ,
  cm_familycd + ' (' + (SELECT fm_desc FROM Family_master(NOLOCK) WHERE fm_cd = cm_familycd ) + ')' AS [Family],
  [Grid1] = '##Grid1##',
  [Grid2] = '##Grid2##',	
  [Grid3] = '##Grid3##',	
  [Grid4] = '##Grid4##',
  [FormatGrid] = '##FormatGrid##'
  FROM CLIENT_MASTER C, @tbl_UserList 
  WHERE CM_cD = ClientCode FOR XML PATH('ClientDetail'))
  SET @o_Message = ''
  SET @o_Message = CAST(@XMLDATA AS VARCHAR(MAX))

  RETURN 1
END
GO

CREATE PROCEDURE stpr_HoldingStatement @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, 
	@o_vcErrorMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN
	--- Parameter Declaration
  DECLARE @dtFromDate VARCHAR(20), @dtToDt VARCHAR(20), @strUserId VARCHAR(500) = '', @strLevel VARCHAR(1), 
  @strCode VARCHAR(500), @strSelection VARCHAR(100), @XMLData XML, @strOutputType VARCHAR(1) = 'G', 
  @strRequestFrom VARCHAR(1) = 'W', @XMLDATA1 XML ='', @strShowValuation VARCHAR(20)='', @strReportType VARCHAR(30)='',
  @strReportDisplay VARCHAR(1)='P'

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
  @strSelection = ISNULL(x.value('(Selection)[1]', 'VARCHAR(50)'), ''), 
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'), ''), 
  @strRequestFrom = ISNULL(x.value('(RequestFrom)[1]', 'VARCHAR(1)'), ''),
  @strShowValuation = ISNULL(x.value('(ShowValuation)[1]', 'VARCHAR(10)'), ''),
  @strReportType = ISNULL(x.value('(ReportType)[1]', 'VARCHAR(30)'), ''),
  @strReportDisplay = ISNULL(x.value('(ReportDisplay)[1]', 'VARCHAR(30)'), '')
  FROM @XMLData.nodes('/root') AS XTbl(x)
  
  IF ISNULL(@dtFromDate,'') = '' 
  BEGIN
    SELECT @dtFromDate = ISNULL(x.value('(FromDt)[1]', 'VARCHAR(8)'), ''), 
    @dtToDt = ISNULL(x.value('(ToDt)[1]', 'VARCHAR(8)'), '')
    FROM @XMLData.nodes('/root') AS XTbl(x)
  END	
  
  IF ISNULL(@dtFromDate,'') = ''
  BEGIN
    SET @dtFromDate = @dtToDt
  END
  
  
  IF @strShowValuation = ''
  BEGIN
    SET @strShowValuation = 'true'
  END
  
  IF ISNULL(@strSelection,'') = ''
  BEGIN
    SET @strSelection = 'CLIENT'
  END

  IF ISNULL(@strReportType,'')  =''
  BEGIN
    SET @strReportType = 'UPTODATE'
  END
  
  INSERT INTO @tbl_UserList
  EXEC dbo.stpr_GetClientAccessListNew @strUserid, @strSelection, @StrCode
  
  IF @strReportDisplay IN('D','E','A')
  BEGIN
    DECLARE @o_Message NVARCHAR(MAX)=''
    EXEC stpr_TypestHeaderProc @strUserid, @strSelection, @strCode, @o_Message OUTPUT
  
    SET @o_Message = REPLACE(@o_Message,'##ReportName##','Holding Statement')
    SET @o_Message = REPLACE(@o_Message,'##Grid1##','Holding Statement')
    SET @o_Message = REPLACE(@o_Message,'##Grid2##','')
    SET @o_Message = REPLACE(@o_Message,'##Grid3##','')
    SET @o_Message = REPLACE(@o_Message,'##Grid4##','')
	
	IF ISNULL(@strReportDisplay,'') <> 'A'
	BEGIN
	  SET @o_Message = REPLACE(@o_Message,'##FormatGrid##','Grid1')
	END   
	ELSE
	BEGIN
	  SET @o_Message = REPLACE(@o_Message,'##FormatGrid##','')
	END
	
	
    SET @o_Message = '<root>' + @o_Message + '</root>';
  
    DECLARE @xmlData2 XML = CAST(@o_Message AS XML);
	
	DECLARE @DynamicColumns NVARCHAR(MAX);
    
	SET  @DynamicColumns = '';
  
	
    WITH DistinctNodes AS (
    SELECT DISTINCT
       NodeName = X.n.value('local-name(.)', 'NVARCHAR(100)')
    FROM @xmlData2.nodes('/root/ClientDetail/*') AS X(n))
    
    SELECT @DynamicColumns = @DynamicColumns+','+
        'LTRIM(RTRIM(X1.value(''(' + XX.NodeName + ')[1]'', ''NVARCHAR(MAX)''))) AS [' + XX.NodeName + ']'
    FROM DistinctNodes XX
   
    SELECT @DynamicColumns = SUBSTRING(@DynamicColumns,2,LEN(@DynamicColumns)-1) 
  
    DECLARE @sql NVARCHAR(MAX);
    SET @sql = '
    SELECT ' + @DynamicColumns + '
    FROM @xmlData2.nodes(''/root/ClientDetail'') AS T(X1)';
  
    EXEC sp_executesql @sql, N'@xmlData2 XML', @xmlData2;
	IF ISNULL(@strReportDisplay,'') <> 'A'
	BEGIN
	  SELECT ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, ColumnFormat, Columnstyle, DecimalPlace, ColumnTotal 
      FROM tbl_ReportGridViewFormat(NOLOCK) 
	  WHERE ReportCode='CrossNet'
      AND ReportCategroy = 'Disp_Holding'
      Order By Orderby
	  
	END   
  END	

   
  DECLARE @TBL_CloseRate TABLE(ISIN VARCHAR(30), CloseRate MONEY)
   
  CREATE TABLE #tbl_HoldingRepDP (ClientCode VARCHAR(50), ClientName VARCHAR(100), BranchCode VARCHAR(50),
  ScripCode VARCHAR(50), ScripName VARCHAR(100), ISIN VARCHAR(20), Qty MONEY, ClosingPrice MONEY,
  MarketValue MONEY, AccountType VARCHAR(500))
    
  IF @strReportType = 'UPTODATE'
  BEGIN  
    INSERT INTO #tbl_HoldingRepDP(ClientCode, ClientName, BranchCode, ScripName,
    ISIN, AccountType, Qty, ClosingPrice, MarketValue)
    SELECT cm_cd As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode, 
    ScripName , td_isin_code As ISIN, bt_description, BalanceQty As Qty, 0, 0 
    FROM (Select td_ac_code, td_isin_code, ScripName , bt_description , BalanceQty = sum(BalanceQty)  
    FROM(  
    SELECT td_ac_code = hld_ac_code, ScripName = sc_isinname, td_isin_code = hld_isin_code, td_ac_type = hld_ac_type ,  SUM(hld_ac_pos) as BalanceQty  
    FROM DBO.Holding(nolock), DBO.Security(nolock) , @tbl_UserList x
    where hld_ac_code = x.clientcode 
    AND hld_isin_code = sc_isincode 
    GROUP BY hld_ac_code, hld_isin_code, hld_ac_type, sc_isinname  
    HAVING SUM(hld_ac_pos) <> 0) x1 LEFT OUTER JOIN  DBO.Beneficiary_type BN ON(td_ac_type = BN.bt_code)  
    GROUP BY td_ac_code, td_isin_code, bt_description, ScripName  ) A, [dbo].client_master(NOLOCK) 
    WHERE cm_schedule = (select sp_sysvalue from Sysparameter where sp_parmcd = 'cmschedule') 
    AND td_ac_code = cm_cd  
	SET @dtFromDate = CONVERT(VARCHAR,GETDATE(),112)
  END
  ELSE IF @strReportType = 'ASONDATE'
  BEGIN  
    INSERT INTO #tbl_HoldingRepDP(ClientCode, ClientName, BranchCode, ScripName,
    ISIN, AccountType, Qty, ClosingPrice, MarketValue)
	
    SELECT cm_cd As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode, 
    ScripName , td_isin_code As ISIN, bt_description, BalanceQty As Qty, 0, 0 
    FROM (Select td_ac_code, td_isin_code, ScripName , bt_description , BalanceQty = sum(BalanceQty)  
    FROM(  
    SELECT td_ac_code = td_ac_code, ScripName = sc_isinname, td_isin_code = td_isin_code, td_ac_type = td_ac_type ,  
    SUM(CASE WHEN td_debit_credit = 'D' THEN td_qty* -1 ELSE td_qty  END) as BalanceQty  
    FROM Trxdetail(NOLOCK), DBO.Security(nolock) , @tbl_UserList x
    where td_ac_code = x.clientcode  
    AND td_isin_code = sc_isincode 
	AND td_curdate <= @dtFromDate
    GROUP BY td_ac_code, td_isin_code, td_ac_type, sc_isinname  
    HAVING  SUM(CASE WHEN td_debit_credit = 'D' THEN td_qty ELSE td_qty * -1 END) <> 0) x1 LEFT OUTER JOIN  DBO.Beneficiary_type BN ON(td_ac_type = BN.bt_code)  
    GROUP BY td_ac_code, td_isin_code, bt_description, ScripName  ) A, [dbo].client_master(NOLOCK) 
    WHERE cm_schedule = (select sp_sysvalue from Sysparameter where sp_parmcd = 'cmschedule') 
    AND td_ac_code = cm_cd  
  END
  
  IF ISNULL(@dtFromDate,'') = ''
  BEGIN
    SET @dtFromDate = CONVERT(VARCHAR,GETDATE(),112)
  END
   
  IF ISNULL(@StrShowValuation,'false') = 'true'
  BEGIN
     DECLARE @dtMax VARCHAR(10)=''
     SELECT @dtMax = max(rm_trx_date) from  DBO.Rate_master where rm_trx_date  <= @dtFromDate
	
     INSERT INTO @TBL_CloseRate(ISIN, CloseRate)
     SELECT rm_isin_code, rm_rate    
     FROM  DBO.Rate_master(NOLOCK) X 
     WHERE rm_trx_date = @dtMax
	 AND EXISTS(SELECT 1 FROM #tbl_HoldingRepDP WHERE ISIN = X.rm_isin_code)
	
     UPDATE A SET A.ClosingPrice = B.CloseRate
     FROM #tbl_HoldingRepDP A, @TBL_CloseRate b
     WHERE A.ISIN = B.ISIN
	

     INSERT INTO @TBL_CloseRate(ISIN, CloseRate)
     SELECT rm_isin_code, rm_rate    
     FROM  DBO.Rate_master(NOLOCK) X, (SELECT DISTINCT ISIN from #tbl_HoldingRepDP where isnull(ClosingPrice,0) = 0) B
     WHERE x.rm_isin_code = B.ISIN
	 AND rm_trx_date = (select max(rm_trx_date) from  DBO.Rate_master where rm_trx_date  <= CONVERT(VARCHAR,GETDATE(),112)
	 AND rm_isin_code = X.rm_isin_code) 
	 
	 UPDATE A SET A.ClosingPrice = B.CloseRate
     FROM #tbl_HoldingRepDP A, @TBL_CloseRate b
     WHERE A.ISIN = B.ISIN
	 AND isnull(ClosingPrice,0) = 0


     IF ISNULL(@strOutputType,'G') = 'G'
     BEGIN
       SELECT ClientCode, ClientName, [ISINName] = ScripName, ISIN, [BalanceType] = AccountType, 
       Qty, Rate = ClosingPrice, [Value] = ROUND(Qty* ClosingPrice,2)
       FROM #tbl_HoldingRepdp
	   ORDER BY ClientCode, AccountType, ScripName
	   DROP TABLE #tbl_HoldingRepdp
	   SET @o_vcErrorMessage = 'Process Executed'
	   SET @o_vcErrorFlag = 'S'
	   RETURN 1
     END
     ELSE IF ISNULL(@strOutputType,'G') = 'X'	
     BEGIN
       IF NOT EXISTS(SELECT 1 FROM #tbl_HoldingRepdp)
	   BEGIN
	     SET @XMLDATA1 = 
	     (SELECT ClientCode = '', ClientName = '', ISINName = '', ISIN ='', BalanceType = '', 
         Qty = '', Rate = '', [Value] = '' FOR XML PATH('Detail'))
       END 
	   ELSE
	   BEGIN
         SET @XMLDATA1 = 
	     (SELECT ClientCode, ClientName, ISINName = ScripName, ISIN, BalanceType = AccountType, 
         Qty, Rate = ClosingPrice, [Value] = ROUND(Qty* ClosingPrice,2)
         FROM #tbl_HoldingRepdp
	     ORDER BY ClientCode, AccountType, ScripName FOR XML PATH('Detail'))
	   END
	   DROP TABLE #tbl_HoldingRepdp
	   SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	   SET @o_vcErrorFlag = 'S'
	   RETURN 1
     END
   END 	 
   ELSE
   BEGIN
     IF ISNULL(@strOutputType,'G') = 'G'
     BEGIN
       SELECT ClientCode, ClientName, [ISIN Name] = ScripName, ISIN, [Balance Type] = AccountType, Qty
       FROM #tbl_HoldingRepdp
	   ORDER BY ClientCode, AccountType, ScripName
	   DROP TABLE #tbl_HoldingRepdp
	   SET @o_vcErrorMessage = 'Process Executed'
	   SET @o_vcErrorFlag = 'S'
	   RETURN 1
	 END
	 ELSE IF ISNULL(@strOutputType,'G') = 'X'	
     BEGIN
       IF NOT EXISTS(SELECT 1 FROM #tbl_HoldingRepdp)
	   BEGIN
	     SET @XMLDATA1 = 
	     (SELECT ClientCode ='', ClientName ='', ISINName = '', 
		 ISIN ='', BalanceType = '', Qty ='' FOR XML PATH('Detail'))
       END 
	   ELSE
	   BEGIN
         SET @XMLDATA1 = 
	     (SELECT ClientCode, ClientName, ISINName = ScripName, ISIN, BalanceType = AccountType, Qty
          FROM #tbl_HoldingRepdp
	      ORDER BY ClientCode, AccountType, ScripName FOR XML PATH('Detail'))
	   END
	   DROP TABLE #tbl_HoldingRepdp
	   SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	   SET @o_vcErrorFlag = 'S'
	   RETURN 1
     END
  END
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
  @strRequestFrom VARCHAR(1) = 'W', @XMLDATA1 XML ='', @strReportDisplay VARCHAR(1)='P'

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
  @strRequestFrom = ISNULL(x.value('(RequestFrom)[1]', 'VARCHAR(1)'), ''),
  @strReportDisplay = ISNULL(x.value('(ReportDisplay)[1]', 'VARCHAR(1)'), '')
  FROM @XMLData.nodes('/root') AS XTbl(x)


  IF ISNULL(@dtFromDate,'') = '' 
  BEGIN
    SELECT @dtFromDate = ISNULL(x.value('(FromDt)[1]', 'VARCHAR(8)'), ''), 
    @dtToDt = ISNULL(x.value('(ToDt)[1]', 'VARCHAR(8)'), '')
    FROM @XMLData.nodes('/root') AS XTbl(x)
  END	
  
  IF ISNULL(@strReportDisplay,'')=''
  BEGIN
    SET @strReportDisplay = 'P'
  END
  

  IF ISNULL(@strSelection,'') = ''
  BEGIN
    SET @strSelection = 'CLIENT'
  END

  INSERT INTO @tbl_UserList
  EXEC dbo.stpr_GetClientAccessListNew @strUserid, @strSelection, @StrCode
  
    
  IF @strReportDisplay IN('D','E','A')
  BEGIN
    DECLARE @o_Message NVARCHAR(MAX)=''
    EXEC stpr_TypestHeaderProc @strUserid, @strSelection, @strCode, @o_Message OUTPUT
  
    SET @o_Message = REPLACE(@o_Message,'##ReportName##','Client Ledger')
    SET @o_Message = REPLACE(@o_Message,'##Grid1##','Ledger')
    SET @o_Message = REPLACE(@o_Message,'##Grid2##','')
    SET @o_Message = REPLACE(@o_Message,'##Grid3##','')
    SET @o_Message = REPLACE(@o_Message,'##Grid4##','')
	
	IF ISNULL(@strReportDisplay,'') <> 'A'
	BEGIN
	  SET @o_Message = REPLACE(@o_Message,'##FormatGrid##','Grid1')
	END   
	ELSE
	BEGIN
	  SET @o_Message = REPLACE(@o_Message,'##FormatGrid##','')
	END
	
    SET @o_Message = '<root>' + @o_Message + '</root>';
  
    DECLARE @xmlData2 XML = CAST(@o_Message AS XML);
	
	DECLARE @DynamicColumns NVARCHAR(MAX);
    
	SET  @DynamicColumns = '';
  
	
    WITH DistinctNodes AS (
    SELECT DISTINCT
       NodeName = X.n.value('local-name(.)', 'NVARCHAR(100)')
    FROM @xmlData2.nodes('/root/ClientDetail/*') AS X(n))
    
    SELECT @DynamicColumns = @DynamicColumns+','+
        'LTRIM(RTRIM(X1.value(''(' + XX.NodeName + ')[1]'', ''NVARCHAR(MAX)''))) AS [' + XX.NodeName + ']'
    FROM DistinctNodes XX
   
    SELECT @DynamicColumns = SUBSTRING(@DynamicColumns,2,LEN(@DynamicColumns)-1) 
  
    DECLARE @sql NVARCHAR(MAX);
    SET @sql = '
    SELECT ' + @DynamicColumns + '
    FROM @xmlData2.nodes(''/root/ClientDetail'') AS T(X1)';
  
    EXEC sp_executesql @sql, N'@xmlData2 XML', @xmlData2;
	IF ISNULL(@strReportDisplay,'') <> 'A'
	BEGIN
	  SELECT ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, ColumnFormat, Columnstyle, DecimalPlace, ColumnTotal 
      FROM tbl_ReportGridViewFormat(NOLOCK) 
	  WHERE ReportCode='CrossNet'
      AND ReportCategroy = 'DisLedger'
      Order By Orderby
	  
	END   
  END	
  
  
  DECLARE @tbl_LedgerReport 
  TABLE(SerialNo INT IDENTITY(1,1), ClientCode VARCHAR(16), Date VARCHAR(15), voucherNo VARCHAR(50),
  Narration VARCHAR(500), DebitofCredit VARCHAR(1), ChequeNo VARCHAR(50),
  DebitAmount MONEY, CreditAmount MONEY, Balance MONEY, BalanceTag VARCHAR(2), ClientName VARCHAR(150))

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
		
		SELECT 2 AS tag, ld_clientcd, ld_dt = CAST(CONVERT(VARCHAR, ld_dt, 112) AS DATE), ld_documenttype + '/' + 
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

CREATE PROCEDURE stpr_TransactionStatement @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, 
	@o_vcErrorMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN
	--- Parameter Declaration
  DECLARE @dtFromDate VARCHAR(8), @dtToDt VARCHAR(8), @strUserId VARCHAR(500) = '', @strLevel VARCHAR(1), 
  @strCode VARCHAR(500), @strSelection VARCHAR(100), @XMLData XML, @strOutputType VARCHAR(1) = 'G', 
  @strRequestFrom VARCHAR(1) = 'W', @XMLDATA1 XML ='', @strShowValuation VARCHAR(20)='',
  @strISIN VARCHAR(20), @strTrxType VARCHAR(200) = '',
  @strMarketAndEarlyPay VARCHAR(10)='', @strOffMarket VARCHAR(10)='', @strInterDepository VARCHAR(10)='', @strDematAndDestat VARCHAR(10)='',
  @strPledge VARCHAR(10)='', @strCorporateAction VARCHAR(10)='', @strSQL NVARCHAR(MAX)='', @strReportDisplay VARCHAR(1)='P'

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
  @strSelection = ISNULL(x.value('(Selection)[1]', 'VARCHAR(10)'), ''), 
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'), ''), 
  @strRequestFrom = ISNULL(x.value('(RequestFrom)[1]', 'VARCHAR(1)'), ''),
  @strISIN = ISNULL(x.value('(ISIN)[1]', 'VARCHAR(20)'), ''),
  @strMarketAndEarlyPay = ISNULL(x.value('(MarketAndEarlyPay)[1]', 'VARCHAR(10)'), ''),
  @strOffMarket = ISNULL(x.value('(OffMarket)[1]', 'VARCHAR(10)'), ''),
  @strInterDepository = ISNULL(x.value('(InterDepository)[1]', 'VARCHAR(10)'), ''),
  @strDematAndDestat = ISNULL(x.value('(DematAndDestat)[1]', 'VARCHAR(10)'), ''),
  @strPledge = ISNULL(x.value('(Pledge)[1]', 'VARCHAR(10)'), ''),
  @strCorporateAction = ISNULL(x.value('(CorporateAction)[1]', 'VARCHAR(10)'), ''),
  @strReportDisplay = ISNULL(x.value('(ReportDisplay)[1]', 'VARCHAR(10)'), '')
  FROM @XMLData.nodes('/root') AS XTbl(x)
  
  IF ISNULL(@dtFromDate,'') = '' 
  BEGIN
    SELECT @dtFromDate = ISNULL(x.value('(FromDt)[1]', 'VARCHAR(8)'), ''), 
    @dtToDt = ISNULL(x.value('(ToDt)[1]', 'VARCHAR(8)'), '')
    FROM @XMLData.nodes('/root') AS XTbl(x)
  END	
  
  DECLARE @tbl_trxrtpe TABLE(TRXTYPE VARCHAR(10))
  
  IF ISNULL(@strISIN,'') =''
  BEGIN
    SET @strISIN = ''
  END

  IF @strMarketAndEarlyPay = 'true'
  BEGIN
    INSERT INTO @tbl_trxrtpe 
	VALUES('052')
	INSERT INTO @tbl_trxrtpe 
	VALUES('054')
  END
  IF @strOffMarket = 'true'
  BEGIN
    INSERT INTO @tbl_trxrtpe 
	VALUES('044')
	INSERT INTO @tbl_trxrtpe 
	VALUES('042')
  END
  IF @strInterDepository = 'true'
  BEGIN
    INSERT INTO @tbl_trxrtpe 
	VALUES('202')
	INSERT INTO @tbl_trxrtpe 
	VALUES('204')
  END
  IF @strDematAndDestat = 'true'
  BEGIN
    INSERT INTO @tbl_trxrtpe 
	VALUES('011')
	INSERT INTO @tbl_trxrtpe 
	VALUES('012')
	INSERT INTO @tbl_trxrtpe 
	VALUES('013')
  END
  IF @strPledge = 'true'
  BEGIN
  	INSERT INTO @tbl_trxrtpe 
	VALUES('091')
	INSERT INTO @tbl_trxrtpe 
	VALUES('092')
	INSERT INTO @tbl_trxrtpe 
	VALUES('093')
  END
  IF @strCorporateAction = 'true'
  BEGIN
    INSERT INTO @tbl_trxrtpe 
	VALUES('082')
  END
  
  IF NOT EXISTS(SELECT 1 FROM @tbl_trxrtpe)
  BEGIN
    INSERT INTO @tbl_trxrtpe
	SELECT NR_CODE FROM Narration 
  END


  DECLARE @tbl_Transaction TABLE(SerialNo INT IDENTITY(1,1),ClientCode VARCHAR(20),
  ClientName VARCHAR(200), TrxnDate VARCHAR(20),
  ISIN VARCHAR(20), ScripName VARCHAR(100),
  TrxnNo VARCHAR(50), Description VARCHAR(50),Narration VARCHAR(500),DebitCredit VARCHAR(50),DebitQty MONEY,
  CreditQty MONEY,Runingbalace MONEY)
  
  IF ISNULL(@strSelection,'') = ''
  BEGIN
    SET @strSelection = 'CLIENT'
  END
  
  INSERT INTO @tbl_UserList 
  EXEC dbo.stpr_GetClientAccessListNew @strUserid, @strSelection, @strCode
  
  IF @strReportDisplay IN('D','E','A')
  BEGIN
    DECLARE @o_Message NVARCHAR(MAX)=''
    EXEC stpr_TypestHeaderProc @strUserid, @strSelection, @strCode, @o_Message OUTPUT
  
    SET @o_Message = REPLACE(@o_Message,'##ReportName##','Transaction Statement')
    SET @o_Message = REPLACE(@o_Message,'##Grid1##','Transaction Statement')
    SET @o_Message = REPLACE(@o_Message,'##Grid2##','')
    SET @o_Message = REPLACE(@o_Message,'##Grid3##','')
    SET @o_Message = REPLACE(@o_Message,'##Grid4##','')
	
	IF ISNULL(@strReportDisplay,'') <> 'A'
	BEGIN
	  SET @o_Message = REPLACE(@o_Message,'##FormatGrid##','Grid1')
	END   
	ELSE
	BEGIN
	  SET @o_Message = REPLACE(@o_Message,'##FormatGrid##','')
	END
	
    SET @o_Message = '<root>' + @o_Message + '</root>';
  
    DECLARE @xmlData2 XML = CAST(@o_Message AS XML);
	
	DECLARE @DynamicColumns NVARCHAR(MAX);
    
	SET  @DynamicColumns = '';
  
	
    WITH DistinctNodes AS (
    SELECT DISTINCT
       NodeName = X.n.value('local-name(.)', 'NVARCHAR(100)')
    FROM @xmlData2.nodes('/root/ClientDetail/*') AS X(n))
    
    SELECT @DynamicColumns = @DynamicColumns+','+
        'LTRIM(RTRIM(X1.value(''(' + XX.NodeName + ')[1]'', ''NVARCHAR(MAX)''))) AS [' + XX.NodeName + ']'
    FROM DistinctNodes XX
   
    SELECT @DynamicColumns = SUBSTRING(@DynamicColumns,2,LEN(@DynamicColumns)-1) 
  
    DECLARE @sql NVARCHAR(MAX);
    SET @sql = '
    SELECT ' + @DynamicColumns + '
    FROM @xmlData2.nodes(''/root/ClientDetail'') AS T(X1)';
  
    EXEC sp_executesql @sql, N'@xmlData2 XML', @xmlData2;
	IF ISNULL(@strReportDisplay,'') <> 'A'
	BEGIN
	  SELECT ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, ColumnFormat, Columnstyle, DecimalPlace, ColumnTotal 
      FROM tbl_ReportGridViewFormat(NOLOCK) 
	  WHERE ReportCode='CrossNet'
      AND ReportCategroy = 'TransactionStatement'
      Order By Orderby
	  
	END   
  END	
  
  INSERT INTO @tbl_Transaction(ClientCode,ClientName,TrxnDate,ISIN,ScripName,TrxnNo,Description,Narration,DebitCredit,DebitQty,CreditQty,Runingbalace)
  SELECT td_ac_code As ClientCode, cm_name As ClientName, td_curdate As TrxnDate, td_isin_code As ISIN, sc_isinname AS ScripName, td_trxno AS TrxnNo , 
  bt_description as Description, Narration = 'By '+ISNULL(td_description,'')+'/'+ISNULL(td_beneficiery,'')+iif(len(td_settlement)>3,
  '/'+td_settlement,'')+iif(ISNULL(td_PledgeDesc,'')='','',' ['+td_PledgeDesc+']'), td_debit_credit As DebitCredit, DebitQty,
  CreditQty = ABS(CreditQty),
  Runingbalace = ABS(SUM(DebitQty+CreditQty) OVER (PARTITION  BY td_ac_code, sc_isinname, bt_description,  td_ac_type 
  ORDER BY td_ac_code, sc_isinname, bt_description, td_curdate, td_trxno, SerialNo)) 
  FROM( 
  Select td_ac_code, td_curdate = @dtFromDate, TAG = 1, td_isin_code, '0' as td_trxno, td_description ='Opening Balance', td_narration ='', 
  td_beneficiery = '', td_settlement = '', 
  td_ac_type, td_PledgeDesc = '', iif(OpenQty>0,'D','C') AS td_debit_credit, DebitQty = iif(OpenQty>0,OpenQty,0), 
  CreditQty = iif(OpenQty<0,OpenQty,0), SerialNo = 0 
  FROM( 
  SELECT td_ac_code, td_isin_code, td_ac_type , SUM(iif(td_debit_credit='C',-td_qty,td_qty)) as OpenQty  
  FROM DBO.Trxdetail(nolock) where td_curdate < @dtFromDate
  and td_booking_type not in ('13') AND (td_narration IN(SELECT TRXTYPE FROM @tbl_trxrtpe) or td_narration = '') 
  GROUP BY td_ac_code, td_isin_code, td_ac_type 
  HAVING SUM(iif(td_debit_credit='C',td_qty,-td_qty)) <> 0
  ) x1 
  UNION ALL 
  SELECT td_ac_code, td_curdate, TAG = 2, td_isin_code, td_trxno = CAST(td_reference AS VARCHAR), td_description , td_narration, td_beneficiery, 
  td_settlement, td_ac_type ,  Rtrim(td_UCC) + case When Rtrim(isNull(td_segment,'')) <> '' Then  '/' + RTrim(isNull(td_segment,''))  
  else '' end + case When Rtrim(isNull(td_cmid,'')) <> '' Then  '/' + RTrim(isNull(td_cmid,'')) else '' end  +
  case When Rtrim(isNull(td_tmid,'')) <> '' Then  '/' + RTrim(isNull(td_tmid,'')) else '' end td_PledgeDesc, 
  td_debit_credit, DebitQty = (Case td_debit_credit  when 'D' then td_qty else 0 end), 
  CreditQty = (Case td_debit_credit  when 'C' then td_qty*-1 else 0 end), SerialNo = ROW_NUMBER() OVER (order by td_ac_code, td_curdate, td_reference)
  FROM DBO.Trxdetail(nolock) where td_curdate >= @dtFromDate
  and td_curdate <= @dtToDt
  AND td_booking_type not in ('13')) x LEFT OUTER JOIN DBO.narration N 
  ON(td_narration=nr_code) 
  LEFT OUTER JOIN DBO.Beneficiary_type BN ON(td_ac_type = BN.bt_code), 
  DBO.Security(NOLOCK) SC, DBO.Client_master cm, @tbl_UserList XX
  WHERE X.td_isin_code = SC.sc_isincode and x.td_ac_code = cm.CM_CD 
  AND cm.CM_CD  = XX.ClientCODE
  AND (td_narration IN(SELECT TRXTYPE FROM @tbl_trxrtpe) or td_narration = '') 
  AND ((td_isin_code = @strISIN AND @strISIN <> '') OR @strISIN = '')
  ORDER BY td_ac_code, sc_isinname, bt_description, td_curdate, td_trxno, SerialNo
  
  --SELECT * FROM @tbl_Transaction
  --SELECT * FROM @tbl_UserList
  --SELECT @dtFromDate, @dtToDt
  
  IF ISNULL(@strOutputType,'G') = 'G'
  BEGIN
    SELECT ClientCode, ClientName, TrxnDate = TrxnDate, ISIN, ScripName, TrxnNo, Description, Narration, DebitCredit, DebitQty,
	CreditQty, Runingbalace ,'Scrip Details : - '+ISIN + ' / ' + ScripName + ' / ' + Description as GroupColumn
    FROM  @tbl_Transaction
	ORDER BY SerialNo
	SET @o_vcErrorMessage = 'Process Executed'
	SET @o_vcErrorFlag = 'S'
	RETURN 1
  END
  ELSE IF ISNULL(@strOutputType,'G') = 'X'	
  BEGIN
    IF NOT EXISTS(SELECT 1 FROM @tbl_Transaction)
	BEGIN
	  SET @XMLDATA1 = 
	  (SELECT ClientCode = '', ClientName = '', TrxnDate = '', ISIN = '', ScripName = '', 
	  TrxnNo = '', Description = '', Narration = '', DebitCredit = '', DebitQty = '',
	  CreditQty = '', Runingbalace = '' ,GroupColumn = ''
	  FOR XML PATH('Detail'))
    END 
	ELSE
	BEGIN
      SET @XMLDATA1 = 
	  (SELECT ClientCode, ClientName, TrxnDate = TrxnDate, ISIN, ScripName, TrxnNo, Description, Narration, DebitCredit, DebitQty,
	  CreditQty, Runingbalace
	  FROM  @tbl_Transaction ORDER BY SerialNo 
	  FOR XML PATH('Detail'))
	END
	SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	SET @o_vcErrorFlag = 'S'
	RETURN 1
END
  
END
GO

CREATE PROCEDURE sp_ChangePassword @dsXml AS XML 
WITH ENCRYPTION
AS
BEGIN
   DECLARE @tbl_Variable dbo.tb_ParamList;  
   DECLARE @o_ParameterList varchar(max)='', @o_ParameterListxml XML;  
   DECLARE @tb_ParamListDetail DBO.tb_ParamList ;      
   --- PARAMETER LIST  
   EXEC SP_ParameterXMLRep @dsXml, @o_ParameterList output 

   IF ISNULL(@o_ParameterList,'') <> ''  
   BEGIN  
	 SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)  
	 
	 INSERT INTO @tb_ParamListDetail(ParameterName,  ParameterValue, HeaderName)   
	 SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code ,  
		 Parameter.value('(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue,  
		 Parameter.value('(MasterTag)[1]', 'VARCHAR(MAX)') AS MasterTag  
	 FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)  
   END  
   DECLARE @StrOption VARCHAR(50)='', @RequestFrom VARCHAR(1)='W'
   SELECT @StrOption = ParameterValue  From @tb_ParamListDetail  WHERE ParameterName = 'Option' 
   
   DECLARE @ClientCode Varchar(20)='', @OldPassword NVARCHAR(50) = ''  , @NewPassword NVARCHAR(50)='',
   @otp VARCHAR(10)='', @otp_D VARCHAR(10)='', @otp_Expiry VARCHAR(25)
	 
   SELECT @ClientCode = ParameterValue  From @tb_ParamListDetail  WHERE ParameterName = 'ClientCode' 
   SELECT @OldPassword = ParameterValue  From @tb_ParamListDetail  WHERE ParameterName = 'OldPassword'
   SELECT @NewPassword = ParameterValue  From @tb_ParamListDetail  WHERE ParameterName = 'NewPassword' 
   SELECT @otp = ParameterValue  From @tb_ParamListDetail  WHERE ParameterName = 'otp' 
   SELECT @RequestFrom = ParameterValue  From @tb_ParamListDetail  WHERE ParameterName = 'RequestFrom' 
   
   
   IF ISNULL(@NewPassword,'') <> ''
   BEGIN
     DECLARE @strPassWordLen INT = 0  , @strCheckPassword VARCHAR(10)=''
	 DECLARE @IsValid BIT = 1;
	 SELECT @strPassWordLen = sp_sysvalue FROM Sysparameter WHERE sp_parmcd='PWDMINCHR'
	 
	 IF ISNULL(@strPassWordLen,0) > 0
	 BEGIN
       IF LEN(@NewPassword) < @strPassWordLen
	   BEGIN
	     Select '{"Flag":"F","Message":"Password length should not less then 8 Char"}'
		 Return 1
       END	   
	 END   
	 SELECT @strCheckPassword = sp_sysvalue FROM Sysparameter WHERE sp_parmcd='PWDALPHANUM'
	 IF ISNULL(@strCheckPassword,'') = 'Y'
	 BEGIN
	   IF @NewPassword NOT LIKE '%[A-Za-z]%'
	   BEGIN
	     Select '{"Flag":"F","Message":"Password should contains at least one letter (A-Z or a-z)"}'
		 Return 1
       END	   
		
	  IF @NewPassword NOT LIKE '%[0-9]%'
	  BEGIN
	    Select '{"Flag":"F","Message":"Password should contains at least one digit (0-9)"}'
		Return 1
      END	

	  IF @NewPassword NOT LIKE '%[^A-Za-z0-9]%'
	  BEGIN
	    Select '{"Flag":"F","Message":"Password should contains at least one special character (e.g., !, @, #, $, etc.)"}'
		Return 1
      END
	END    
   END
   
   IF ISNULL(@RequestFrom,'') =''
   BEGIN
	SET @RequestFrom  = ''
   END	
   
   IF @StrOption = 'ChangePassword'
   BEGIN
	 IF ISNULL(@otp,'') = ''
	 BEGIN
	   IF EXISTS(SELECT * FROM Client_master WITH(NOLOCK) Where cm_cd = @ClientCode And cm_pwd = DBO.fn_EncryptStringTW(@OldPassword))
	   BEGIN
		 UPDATE Client_master Set cm_pwd = DBO.fn_EncryptStringTW(@NewPassword)  
		 WHERE cm_cd = @ClientCode And cm_pwd = DBO.fn_EncryptStringTW(@OldPassword)
		 SELECT  '{"Flag":"S","Message":"Password changed successfully!"}'
		 RETURN 1
	   END
	   ELSE
	   BEGIN
		 Select '{"Flag":"F","Message":"Old password not matched."}'
		 Return 1
	   END
	 END   
	 ELSE IF ISNULL(@otp,'') <> ''
	 BEGIN
	   SELECT @otp_D = OTP_OTP, @otp_Expiry = CAST(CAST(OTP_ValidTillDate AS DATE) AS VARCHAR)+' '+OTP_ValidTillTime 
	   FROM OTP_Master(NOLOCK) WHERE OTP_ClientCode = @ClientCode
	   AND OTP_SentDate = CONVERT(VARCHAR,GETDATE(),112) AND OTP_Status = 'P' 
	   AND OTP_Product in('CrossWeb','CrossNet') /*(CASE WHEN @RequestFrom = 'W' THEN  'tradeweb' ELSE 
	   'TradeMobile' END)	 */  
	   AND OTP_Identity in(select max(OTP_Identity) from OTP_Master(NOLOCK) WHERE OTP_ClientCode = @ClientCode
	   AND OTP_SentDate = CONVERT(VARCHAR,GETDATE(),112) and OTP_Product in('CrossWeb','CrossNet')
	   /*(CASE WHEN @RequestFrom = 'W' THEN  'tradeweb' ELSE 
	   'TradeMobile' END)*/ and OTP_Item = 'MO')
	   and OTP_Item = 'MO'

	   IF GETDATE() > CAST(@otp_Expiry AS datetime)
	   BEGIN
		 SELECT '{"Flag":"F","Message":"OTP is Expire"}'
		 RETURN 1
	   END
	   IF ISNULL(@otp,'') <> isnull(@otp_D,'')
	   BEGIN
		 SELECT '{"Flag":"F","Message":"Wrong OTP"}'
		 RETURN 1
	   END
	   ELSE
	   BEGIN
		 UPDATE Client_master Set cm_pwd = @NewPassword
		 WHERE cm_cd = @ClientCode 
		 
		 UPDATE OTP_Master SET OTP_Status = 'E' 
		 WHERE OTP_ClientCode = @ClientCode
		 AND OTP_SentDate = CONVERT(VARCHAR,GETDATE(),112) AND OTP_Status = 'P' AND OTP_Product in('CrossWeb','CrossNet')
		 /*(CASE WHEN @RequestFrom = 'W' THEN  'tradeweb' ELSE 
		'TradeMobile' END)*/
		 AND OTP_Identity IN(SELECT MAX(OTP_Identity) from OTP_Master(NOLOCK) WHERE OTP_ClientCode = @ClientCode
		 AND OTP_SentDate = CONVERT(VARCHAR,GETDATE(),112) and OTP_Product in('CrossWeb','CrossNet')
		 /*=(CASE WHEN @RequestFrom = 'W' THEN  'tradeweb' ELSE 
		'TradeMobile' END)*/  and OTP_Item = 'MO')
		 AND OTP_Item = 'MO'
         SELECT '{"Flag":"S","Message":"Password changed successfully"}'
		 RETURN 1
	   END
	 END
   END
END   
GO

CREATE PROCEDURE SP_InitializeLogin @dsXml AS XML
WITH ENCRYPTION
AS
BEGIN
	DECLARE @DPID VARCHAR(100) = '', @strBase64 NVARCHAR(MAX)=''

	SET @DPID = (SELECT sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd='DPID')

	DECLARE @CompanyName VARCHAR(50) = (
			SELECT Cast(sp_sysvalue AS VARCHAR(100))
			FROM sysparameter WITH (NOLOCK)
			WHERE sp_parmcd = 'NAME'
			)
	DECLARE @PasswordMaxLength INT = (
			SELECT character_maximum_length AS 'Max Length'
			FROM information_schema.columns
			WHERE table_name = 'Client_Master' AND COLUMN_NAME = 'CM_PWD'
			)


   SELECT @strBase64 = 
   (SELECT CAST(img_logo AS VARBINARY(MAX)) 
    FOR XML PATH(''), BINARY BASE64) 
  FROM Images(NOLOCK) 
  WHERE img_desc = 'Company Logo'
  
	SELECT @DPID AS DPID, @CompanyName AS CompanyName, @PasswordMaxLength AS PasswordMaxLength, CompanyLogo = @strBase64
END
GO

CREATE PROCEDURE SP_ParameterXMLRep (@vcXML XML, @o_vcParameterOutput VARCHAR(MAX) OUTPUT)
WITH ENCRYPTION
AS
BEGIN
	SET @o_vcParameterOutput = ''
	DECLARE @jsonCutterOutput VARCHAR(MAX)='', @strDanSql NVARCHAR(MAX)=''
	DECLARE @JsonCutterXML XML
	DECLARE @J_Ui VARCHAR(MAX) = '', @strSql VARCHAR(MAX) = '', @X_Filter VARCHAR(max) = '', @X_GFilter VARCHAR(max) = '', @J_Api VARCHAR(
			max) = '', @strString VARCHAR(MAX) = '', @X_Data VARCHAR(MAX) = '', @X_Filter_Multiple VARCHAR(MAX)=''
	SELECT @J_Ui = ISNULL(x.value('(J_Ui)[1]', 'VARCHAR(MAX)'), ''), @strSql = ISNULL(x.value('(Sql)[1]', 'VARCHAR(MAX)'), ''), 
		@X_Filter = cast(@vcXML.query('/dsXml/X_Filter') AS VARCHAR(max)), 
		@X_GFilter = cast(@vcXML.query('/dsXml/X_GFilter') AS VARCHAR(max)), 
		@J_Api = ISNULL(x.value('(J_Api)[1]', 'VARCHAR(MAX)'), ''), 
		@X_Data = cast(@vcXML.query('/dsXml/X_Data') AS VARCHAR(max)),
		@X_Filter_Multiple = cast(@vcXML.query('/dsXml/X_Filter_Multiple') AS VARCHAR(max))
	FROM @vcXML.nodes('/dsXml') AS XTbl(x)
	
	DECLARE @strtradeplustempdb VARCHAR(50) = ''
	SELECT @strtradeplustempdb = sp_sysvalue
	FROM WebParameter(NOLOCK)
	WHERE sp_parmcd = 'TRADEPLUSTEMPDB'
	IF OBJECT_ID('tempdb..#tbl_jsonoutput') IS NOT NULL
		DROP TABLE #tbl_jsonoutput
	CREATE TABLE #tbl_jsonoutput (SerialNo INT, ColumnName VARCHAR(50), ColumnValue VARCHAR(MAX), MasterTag VARCHAR(100), JSONLEVEL INT, MASTERLEVEL INT
		)
	IF @J_Ui <> ''
	BEGIN
	    SET @J_Ui = '{' + @J_Ui + '}'
		BEGIN TRY
		    SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@J_Ui+''' , @jsonCutterOutput OUTPUT';
            EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
            SET @JsonCutterXML= CAST(@jsonCutterOutput AS XML)
            INSERT INTO #tbl_jsonoutput(SerialNo, ColumnName, ColumnValue, MasterTag, JsonLevel, MasterLevel)
		    SELECT X1.* FROM(
            SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
	        JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
            JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
		    JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
	        JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
	        JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
            FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1
		   
		    /*
			SET @strString = 'SELECT SerialNo, ColumnName, ColumnValue, MasterTag,JSONLEVEL, MASTERLEVEL FROM ' + 
				@strtradeplustempdb + '.DBO.FN_JSONCUTTER(''' + @J_Ui + ''') '
			INSERT INTO #tbl_jsonoutput (SerialNo, ColumnName, ColumnValue, MasterTag, JSONLEVEL, MASTERLEVEL)
			EXEC (@strString)
			*/
		END TRY
		BEGIN CATCH
			PRINT '1'
		END CATCH
	END
	IF @J_Api <> ''
	BEGIN
		SET @J_Api = '{' + @J_Api + '}'
		BEGIN TRY
			SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@J_Api+''' , @jsonCutterOutput OUTPUT';
            EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
            SET @JsonCutterXML= CAST(@jsonCutterOutput AS XML)
            INSERT INTO #tbl_jsonoutput(SerialNo, ColumnName, ColumnValue, MasterTag, JsonLevel, MasterLevel)
		    SELECT X1.* FROM(
            SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
	        JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
            JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
		    JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
	        JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
	        JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
            FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1
			/*SET @strString = 'SELECT SerialNo, ColumnName, ColumnValue, MasterTag,JSONLEVEL, MASTERLEVEL FROM ' + 
				@strtradeplustempdb + '.DBO.FN_JSONCUTTER(''' + @J_Api + ''') '
			INSERT INTO #tbl_jsonoutput (SerialNo, ColumnName, ColumnValue, MasterTag, JSONLEVEL, MASTERLEVEL)
			EXEC (@strString)*/
		END TRY
		BEGIN CATCH
			PRINT '1'
		END CATCH
	END
	IF ISNULL(@X_Filter, '') <> ''
	BEGIN
		DECLARE @X_FilterXml XML
		SET @X_FilterXml = CAST(@X_Filter AS XML);
		INSERT INTO #tbl_jsonoutput (ColumnName, ColumnValue, MasterTag)
		SELECT nodes.node.value('local-name(.)', 'NVARCHAR(50)') AS TagName, nodes.node.value('.', 'NVARCHAR(MAX)') AS TagValue, 
			'X_Filter'
		FROM @X_FilterXml.nodes('//*') AS nodes(node);
	END
	IF ISNULL(@X_GFilter, '') <> ''
	BEGIN
		DECLARE @X_GFilterXml XML
		SET @X_GFilterXml = CAST(@X_GFilter AS XML);
		INSERT INTO #tbl_jsonoutput (ColumnName, ColumnValue, MasterTag)
		SELECT nodes.node.value('local-name(.)', 'NVARCHAR(50)') AS TagName, nodes.node.value('.', 'NVARCHAR(MAX)') AS TagValue, 
			'X_GFilter'
		FROM @X_GFilterXml.nodes('//*') AS nodes(node);
	END
	IF ISNULL(@X_Filter_Multiple, '') <> ''
	BEGIN
		DECLARE @X_Filter_MultipleXml XML
		SET @X_Filter_MultipleXml = CAST(@X_Filter_Multiple AS XML);
		INSERT INTO #tbl_jsonoutput (ColumnName, ColumnValue, MasterTag)
		SELECT nodes.node.value('local-name(.)', 'NVARCHAR(50)') AS TagName, nodes.node.value('.', 'NVARCHAR(MAX)') AS TagValue, 
			'X_Filter_Multiple'
		FROM @X_Filter_MultipleXml.nodes('//*') AS nodes(node);
	END
	
	IF ISNULL(@X_Data, '') <> ''
	BEGIN
		DECLARE @i_vcPayloadJson XML
		SET @i_vcPayloadJson = CAST(@X_Data AS XML);
		DECLARE @tbl_xmlCutter TABLE (SerialNo INT identity(1, 1), ColumnName VARCHAR(100), ColumnValue VARCHAR(MAX), MasterTag VARCHAR(50), JsonLevel INT
			)
		DECLARE @sql NVARCHAR(MAX), @XCOUNTER1 INT = 0, @OLDTAG VARCHAR(50) = '', @TagCounter INT = 0, @tabcount1 INT = 0
		DECLARE @tabname VARCHAR(MAX) = ''
		DECLARE db_CursorxmlTag CURSOR
		FOR
		SELECT TagName, count(*) AS counta
		FROM (
			SELECT c.value('local-name(.)', 'NVARCHAR(MAX)') AS TagName
			FROM @i_vcPayloadJson.nodes('//*[(*)]') AS t(c)
			) x1
		WHERE TagName = 'item'
		GROUP BY TagName
		OPEN db_CursorxmlTag
		FETCH NEXT
		FROM db_CursorxmlTag
		INTO @tabname, @tabcount1
		WHILE @@FETCH_STATUS = 0
		BEGIN
			SET @TagCounter = 1
			WHILE @TagCounter <= @tabcount1
			BEGIN
				SET @sql = 
					'SELECT ''SecondLevelData'' as MasterTag,  
          c.value(''local-name(.)'', ''NVARCHAR(MAX)'') AS ColumnName,
          ISNULL(c.value(''(./text())[1]'', ''NVARCHAR(MAX)''),'''') AS ColumnValue, 
		  JsonLevel = '''+ CAST(@TagCounter AS VARCHAR) + '''
 FROM @i_vcPayloadJson.nodes(''/X_Data/items/item[' + CAST(
						@TagCounter AS VARCHAR) + ']/*'') AS t(c) '
				
				INSERT INTO @tbl_xmlCutter (MasterTag, ColumnName, ColumnValue, JsonLevel)
				EXEC sp_executesql @sql, N'@i_vcPayloadJson XML', @i_vcPayloadJson
				
				
				SET @sql = 
					'SELECT ''SecondLevelData'' as MasterTag,  
          c.value(''local-name(.)'', ''NVARCHAR(MAX)'') AS ColumnName,
          ISNULL(c.value(''(./text())[1]'', ''NVARCHAR(MAX)''),'''') AS ColumnValue, 
		  JsonLevel = '''+ CAST(@TagCounter AS VARCHAR) + '''
 FROM @i_vcPayloadJson.nodes(''/X_Data/Items/item[' + CAST(
						@TagCounter AS VARCHAR) + ']/*'') AS t(c) '
				
				INSERT INTO @tbl_xmlCutter (MasterTag, ColumnName, ColumnValue, JsonLevel)
				EXEC sp_executesql @sql, N'@i_vcPayloadJson XML', @i_vcPayloadJson
				
				SET @TagCounter = @TagCounter + 1
			END
			SET @OLDTAG = @tabname
			FETCH NEXT
			FROM db_CursorxmlTag
			INTO @tabname, @tabcount1
		END
		CLOSE db_CursorxmlTag
		DEALLOCATE db_CursorxmlTag
		INSERT INTO @tbl_xmlCutter (MasterTag, ColumnName, ColumnValue, JsonLevel)
		SELECT 'MasterLevelData' AS MasterTag, c.value('local-name(.)', 'NVARCHAR(MAX)') AS ColumnName, isnull(c.value('(./text())[1]', 
				'NVARCHAR(MAX)'),'') AS ColumnValue, JsonLevel = '1'
		FROM @i_vcPayloadJson.nodes('/X_Data/*') AS t(c)
		INSERT INTO #tbl_jsonoutput (ColumnName, ColumnValue, MasterTag, JsonLevel)
		SELECT ColumnName, ColumnValue, MasterTag, JsonLevel
		FROM @tbl_xmlCutter
	END
	DECLARE @xmlOutput XML
	SET @xmlOutput = (
			SELECT ColumnName, ColumnValue = CASE WHEN ColumnName = 'UserId' THEN REPLACE(ISNULL(ColumnValue,''),'|','') ELSE ISNULL(ColumnValue,'') END, MasterTag, JsonLevel
			FROM #tbl_jsonoutput
			FOR XML PATH('Parameter')
			)
	SET @o_vcParameterOutput = CAST(@xmlOutput AS VARCHAR(MAX))
	DROP TABLE #tbl_jsonoutput
	RETURN
END
GO

CREATE PROCEDURE stpr_GenerateMobileGrid @i_xmldata VARCHAR(MAX), @strtag VARCHAR(50), 
@strReportCode VARCHAR(50), @strReportCategroy VARCHAR(50), @strRequestFrom VARCHAR(1) WITH ENCRYPTION AS
BEGIN
  DECLARE @DynamicColumns NVARCHAR(MAX);
  DECLARE @strXMLString VARCHAR(MAX) = @i_xmldata
  
  SET @strXMLString = REPLACE(@strXMLString, '<' + @strtag + '>', '<XMLDATA1>');
  SET @strXMLString = REPLACE(@strXMLString, '</' + @strtag + '>', '</XMLDATA1>');
  SET @strXMLString = '<root>' + @strXMLString + '</root>';
  SET @DynamicColumns = '';

  DECLARE @text1 VARCHAR(MAX)='';
  
  DECLARE @tbl_String TABLE(TextValue VARCHAR(MAX))
  DECLARE @InputString NVARCHAR(MAX) 
  
  DECLARE @tbl_StringMain TABLE (SerialNo INT IDENTITY(1,1),
  TextValue VARCHAR(MAX), Heading VARCHAR(100))	 
  
  /*
  INSERT INTO @tbl_StringMain (TextValue, Heading)
  SELECT Filler2, ColumnHeading
  FROM tbl_ReportGridViewFormat  WITH (NOLOCK) 
  WHERE  ReportCode  =  @strReportCode
    AND ReportCategroy = @strReportCategroy
    AND (ProductID = @strRequestFrom OR ProductID  = '')
	AND PRODUCTCODE = '' 
	AND ISNULL(ColumnName,'') = '' and ISNULL(Filler2,'') <> ''  AND ColumnVisible = 0
    ORDER BY Orderby

  DECLARE @iSerialNo INT, @strTextValue VARCHAR(MAX)='', @strHeading VARCHAR(MAX)=''
  DECLARE Cur0 CURSOR FOR 
  SELECT * from @tbl_StringMain
  OPEN Cur0
  FETCH NEXT FROM Cur0 INTO @iSerialNo, @strTextValue, @strHeading
  WHILE @@FETCH_STATUS = 0
  BEGIN
  	DELETE FROM @tbl_String
    SET @InputString =  @strTextValue;

    WITH Extracted AS (
    SELECT
        ValueStart = CHARINDEX('<<', @InputString),
        ValueEnd = CHARINDEX('>>', @InputString)
    UNION ALL
    SELECT
        CHARINDEX('<<', @InputString, ValueEnd + 2),
        CHARINDEX('>>', @InputString, ValueEnd + 2)
    FROM Extracted
    WHERE CHARINDEX('<<', @InputString, ValueEnd + 2) > 0)
  
    INSERT INTO @tbl_String
    SELECT ExtractedValue = SUBSTRING(@InputString,ValueStart + 2,ValueEnd - ValueStart - 2)
    FROM Extracted
    WHERE ValueStart > 0 AND ValueEnd > 0;

    
    DECLARE Cur1 CURSOR FOR 
    SELECT TextValue from @tbl_String
    OPEN Cur1 
    FETCH NEXT FROM Cur1 INTO @text1
    WHILE @@FETCH_STATUS = 0
    BEGIN
      SET @InputString = REPLACE(@InputString,'<<'+@text1+'>>','X1.value(''('+@text1+')[1]'', ''NVARCHAR(MAX)'') ')
      FETCH NEXT FROM Cur1 INTO @text1
    END 
   CLOSE Cur1 
   DEALLOCATE Cur1 

   UPDATE A SET A.TextValue = @InputString
   FROM @tbl_StringMain A
   WHERE SERIALNO = @iSerialNo

   FETCH NEXT FROM Cur0 INTO @iSerialNo, @strTextValue, @strHeading
  END 
  CLOSE Cur0
  DEALLOCATE Cur0
  */
  DECLARE @xmlData XML = CAST(@strXMLString AS XML);

  WITH DistinctNodes AS (
   SELECT DISTINCT
        NodeName = X.n.value('local-name(.)', 'NVARCHAR(100)')
    FROM 
        @xmlData.nodes('/root/XMLDATA1/*') AS X(n))

  SELECT 
    @DynamicColumns = @DynamicColumns+','+
        'LTRIM(RTRIM(X1.value(''(' + XX.NodeName + ')[1]'', ''NVARCHAR(MAX)''))) AS [' + XX1.ColumnHeading + ']'
    
  FROM DistinctNodes XX JOIN 
    tbl_ReportGridViewFormat XX1 WITH (NOLOCK) 
    ON XX.NodeName = XX1.ColumnName
  WHERE 
    XX1.ReportCode = @strReportCode
    AND XX1.ReportCategroy = @strReportCategroy
   -- AND (XX1.ProductID = @strRequestFrom OR XX1.ProductID  = '')
	AND XX1.PRODUCTCODE = '' AND ColumnName <> '' AND ColumnVisible = 0
	ORDER BY ORDERBY

  SELECT @DynamicColumns = @DynamicColumns+','+TextValue+' AS [' + Heading + ']'
  FROM @tbl_StringMain
  ORDER BY SerialNo


  SELECT @DynamicColumns = SUBSTRING(@DynamicColumns,2,LEN(@DynamicColumns)-1)

  IF @DynamicColumns IS NULL
  BEGIN
     PRINT 'No matching columns found.';
     RETURN;
  END;

  DECLARE @sql NVARCHAR(MAX);
  SET @sql = '
  SELECT ' + @DynamicColumns + '
  FROM @xmlData.nodes(''/root/XMLDATA1'') AS T(X1)';
  --SELECT @sql
  EXEC sp_executesql @sql, N'@xmlData XML', @xmlData;
  
END
GO

CREATE PROCEDURE stpr_APIReportGeneration @dsXml AS XML = NULL WITH ENCRYPTION AS
BEGIN

   DECLARE @tbl_Variable dbo.tb_ParamList;
   DECLARE @tbl_UserList dbo.UserAccessList;
   DECLARE @o_ParameterList varchar(max)='', @o_ParameterListxml XML;
   DECLARE @tb_ParamListDetail DBO.tb_ParamList ;
   
   --- PARAMETER LIST
   
   EXEC SP_ParameterXMLRep @dsXml, @o_ParameterList output
   IF ISNULL(@o_ParameterList,'') <> ''
   BEGIN
     SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)
   
     INSERT INTO @tb_ParamListDetail(ParameterName,  ParameterValue, HeaderName)
     SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code ,
     Parameter.value('(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue,
	 Parameter.value('(MasterTag)[1]', 'VARCHAR(MAX)') AS MasterTag
     FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
   END 

   DECLARE @strUserid VARCHAR(500)='', @strLevel INT = 0, @dtFromDate VARCHAR(8), @dtToDate VARCHAR(8),
   @StrCode VARCHAR(50)='', @strSelection VARCHAR(50) = '', @strActionName VARCHAR(50)='', @strOption VARCHAR(50)='',
   @XMLString varchar(MAX) = '<FromDate>##FromDate##</FromDate><ToDate>##ToDate##</ToDate><UserId>##UserId##</UserId><Level>##Level##</Level>'
   SET @XMLString = @XMLString+'<Code>##Code##</Code><Selection>##Selection##</Selection><OutputType>X</OutputType><RequestFrom>W</RequestFrom><ShowValuation>##ShowValuation##</ShowValuation><ReportDisplay>##ReportDisplay##</ReportDisplay>'
   
   DECLARE @i_vcProjectCode VARCHAR(50)='CROSSNET', @strString NVARCHAR(MAX)='',
   @o_vcErrorFlag VARCHAR(1), @o_vcErrorMessage VARCHAR(MAX)='', @vcProcedureName varchar(max)='', @RequestFrom VARCHAR(1)='W',
   @strShowValuation VARCHAR(20)='', @strISIN VARCHAR(20)='', @strTrxType VARCHAR(200) = '',
   @strMarketAndEarlyPay VARCHAR(10)='', @strOffMarket VARCHAR(10)='', @strInterDepository VARCHAR(10)='', @strDematAndDestat VARCHAR(10)='',
   @strPledge VARCHAR(10)='', @strCorporateAction VARCHAR(10)='', @strBillDate  VARCHAR(20), @strBillType  VARCHAR(20), @strIncludeLedBal VARCHAR(20),
   @strReportDisplay VARCHAR(1)='P'
   
   DECLARE @strType VARCHAR(50),@strStatus VARCHAR(50),@strDateType VARCHAR(50), @strOption1 VARCHAR(100)='', @strUserType VARCHAR(50)
   
   SELECT @strActionName = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'ActionName'
   SELECT @strOption = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'Option'
   SELECT @strOption1 = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'Option'
   SELECT TOP 1 @strUserid = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'UserId'
   SELECT @strLevel = CAST(ParameterValue AS INT) FROM @tb_ParamListDetail where ParameterName = 'Level'
   SELECT @strShowValuation = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'ShowValuation'
   SELECT @strReportDisplay = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'ReportDisplay'
   
   IF ISNULL(@strReportDisplay,'') = ''  
   BEGIN
     SET @strReportDisplay = 'P'
   END
   
   SET @XMLString = REPLACE(@XMLString,'##ReportDisplay##',@strReportDisplay)
   
   SELECT @strISIN = ISNULL(ParameterValue,'')  From @tb_ParamListDetail WHERE ParameterName = 'ISIN' AND HeaderName = 'X_Filter'

   SELECT @strMarketAndEarlyPay = ISNULL(ParameterValue,'')   From @tb_ParamListDetail WHERE ParameterName = 'MarketAndEarlyPay' AND HeaderName = 'X_Filter'
   SELECT @strOffMarket = ISNULL(ParameterValue,'')   From @tb_ParamListDetail WHERE ParameterName = 'OffMarket' AND HeaderName = 'X_Filter'
   SELECT @strInterDepository = ISNULL(ParameterValue,'')   From @tb_ParamListDetail WHERE ParameterName = 'InterDepository' AND HeaderName = 'X_Filter'
   SELECT @strDematAndDestat = ISNULL(ParameterValue,'')   From @tb_ParamListDetail  WHERE ParameterName = 'DematAndDestat' AND HeaderName = 'X_Filter'
   SELECT @strPledge = ISNULL(ParameterValue,'')   From @tb_ParamListDetail  WHERE ParameterName = 'Pledge' AND HeaderName = 'X_Filter'
   SELECT @strCorporateAction = ISNULL(ParameterValue,'')   From @tb_ParamListDetail   WHERE ParameterName = 'CorporateAction' AND HeaderName = 'X_Filter'
   
   SELECT @strType = ISNULL(ParameterValue,'')   From @tb_ParamListDetail   WHERE ParameterName = 'Type' AND HeaderName = 'X_Filter'
   SELECT @strStatus = ISNULL(ParameterValue,'')   From @tb_ParamListDetail   WHERE ParameterName = 'Status' AND HeaderName = 'X_Filter'
   SELECT @strDateType = ISNULL(ParameterValue,'')   From @tb_ParamListDetail   WHERE ParameterName = 'DateType' AND HeaderName = 'X_Filter'
   SELECT @strUserType = ISNULL(ParameterValue,'')   From @tb_ParamListDetail   WHERE ParameterName = 'UserType' 
   
   
   IF @strDateType = '0'
   BEGIN
     SET @strDateType = 'Transaction'
   END   
   ELSE
   BEGIN
     SET @strDateType = 'Execution'
   END
   
   if ISNULL(@strType,'') = ''
   BEGIN
     SET @strType = ''
   END 
   
   IF ISNULL(@strStatus,'') = ''
   BEGIN
     SET @strStatus = ''
   END 
   
   SELECT @strBillDate = ISNULL(ParameterValue,'')   From @tb_ParamListDetail   WHERE ParameterName = 'BillDate' AND HeaderName = 'X_Filter'
   SELECT @strBillType = ISNULL(ParameterValue,'')   From @tb_ParamListDetail   WHERE ParameterName = 'BillType' AND HeaderName = 'X_Filter'
   SELECT @strIncludeLedBal = ISNULL(ParameterValue,'')   From @tb_ParamListDetail   WHERE ParameterName = 'IncludeLedBal' AND HeaderName = 'X_Filter'
   
  
   if isnull(@strBillType,'') = ''
   BEGIN
     set @strBillType = 'Bill'
   end
   
   IF ISNULL(@strBillDate,'') =''
   BEGIN
     SELECT  @strBillDate = MAX(bl_bill_dt) FROM Billing(NOLOCK)
   END
   
   IF ISNULL(@strIncludeLedBal,'')=''
   BEGIN
     SET @strIncludeLedBal = 'false'
   END
   
   
   IF @strUserType = 'User' and @strOption = 'ClientListing'
	 BEGIN
	   SET @XMLString = REPLACE(@XMLString,'##FromDate##','20000101')
       SET @XMLString = REPLACE(@XMLString,'##ToDate##',CONVERT(VARCHAR,GETDATE(),112))
   END
	 
   SELECT @dtFromDate = CONVERT(VARCHAR,CAST(ParameterValue AS date),112) FROM @tb_ParamListDetail 
   WHERE ParameterName = 'FromDate' AND HeaderName = 'X_Filter'

   SELECT @dtToDate =  CONVERT(VARCHAR,CAST(ParameterValue AS date),112) FROM @tb_ParamListDetail 
   WHERE ParameterName = 'ToDate' AND HeaderName = 'X_Filter'
   
   SELECT @StrCode = ParameterValue  FROM @tb_ParamListDetail 
   WHERE ParameterName = 'Code' AND HeaderName = 'X_Filter'

   SELECT @strSelection = ParameterValue  FROM @tb_ParamListDetail 
   WHERE ParameterName = 'Selection' AND HeaderName = 'X_Filter'
   
   
   IF ISNULL(@dtFromDate,'') = ''
   BEGIN
     SET @dtFromDate = CONVERT(VARCHAR,GETDATE(),112)
   END
   
   IF ISNULL(@dtToDate,'') = ''
   BEGIN
     SET @dtToDate = CONVERT(VARCHAR,GETDATE(),112)
   END
   
   IF ISNULL(@strSelection,'') = ''
   BEGIN
     SET @strSelection = 'Client'
   END
   DECLARE @strReportType VARCHAR(50)=''
   SELECT @strReportType = ParameterValue  FROM @tb_ParamListDetail 
   WHERE ParameterName = 'ReportType' AND HeaderName = 'X_Filter'
   
   --- USER ACCESS RIGHTS
   
   IF @StrCode <> '' AND UPPER(@strSelection) = 'CLIENT'
   BEGIN
     SET @strUserid = @StrCode
   END
   
   IF @strOption = 'Disp_Holding'
   BEGIN
     IF ISNULL(@strReportType,'') = ''
	 BEGIN
	   SET @strReportType = 'UPTODATE'
	 END
     SET @XMLString = @XMLString+'<ReportType>'+@strReportType+'</ReportType>'
   END
   
   IF @strOption = 'TransactionStatement'
   BEGIN
     SET @XMLString = @XMLString+'<ISIN>'+@strISIN+'</ISIN>'+'<MarketAndEarlyPay>'+@strMarketAndEarlyPay+'</MarketAndEarlyPay>'
	 SET @XMLString = @XMLString+'<OffMarket>'+@strOffMarket+'</OffMarket>'+'<InterDepository>'+@strInterDepository+'</InterDepository>'
	 SET @XMLString = @XMLString+'<DematAndDestat>'+@strDematAndDestat+'</DematAndDestat>'+'<Pledge>'+@strPledge+'</Pledge>'
	 SET @XMLString = @XMLString+'<CorporateAction>'+@strCorporateAction+'</CorporateAction>'
   END
   ELSE IF @strOption = 'TransactionStatus'
   BEGIN
     SET @XMLString = @XMLString+'<Type>'+@strType+'</Type>'+'<Status>'+@strStatus+'</Status>'
	 SET @XMLString = @XMLString+'<DateType>'+@strDateType+'</DateType>'
   END
   ELSE IF @strOption = 'ClientOutstanding'
   BEGIN
     DECLARE @strBalanceFrom VARCHAR(50) = ''
     DECLARE @strBalanceTo VARCHAR(50) = ''
     DECLARE @strIncCreditBal VARCHAR(50) = 'False'
     DECLARE @strPStatus VARCHAR(50) = ''
     DECLARE @strEmail VARCHAR(50) = 'False'
     DECLARE @strTelephone VARCHAR(50) = 'False'
	 
     SELECT @strBalanceFrom = ISNULL(ParameterValue,'')
     FROM @tb_ParamListDetail
	 WHERE ParameterName = 'BalanceFrom' AND HeaderName = 'X_Filter'

	 SELECT @strBalanceTo = ISNULL(ParameterValue,'')
	 FROM @tb_ParamListDetail
	 WHERE ParameterName = 'BalanceTo' AND HeaderName = 'X_Filter'

	 SELECT @strIncCreditBal = ISNULL(ParameterValue,'False')
	 FROM @tb_ParamListDetail
	 WHERE ParameterName = 'IncCreditBal' AND HeaderName = 'X_Filter'

	 SELECT @strPStatus = ISNULL(ParameterValue,'')
	 FROM @tb_ParamListDetail
	 WHERE ParameterName = 'Status' AND HeaderName = 'X_Filter'

	 SELECT @strEmail = ISNULL(ParameterValue,'False')
	 FROM @tb_ParamListDetail
	 WHERE ParameterName = 'Email' AND HeaderName = 'X_Filter'

	
	 SELECT @strTelephone = ISNULL(ParameterValue,'False')
	 FROM @tb_ParamListDetail
	 WHERE ParameterName = 'Telephone' AND HeaderName = 'X_Filter'
	 
	 SET @XMLString = @XMLString+'<BalanceFrom>'+@strBalanceFrom+'</BalanceFrom>'+'<BalanceTo>'+@strBalanceTo+'</BalanceTo>'
	 SET @XMLString = @XMLString+'<IncCreditBal>'+ISNULL(@strIncCreditBal,'False')+'</IncCreditBal>'+'<Status>'+ISNULL(@strPStatus,'')+'</Status>'
	 SET @XMLString = @XMLString+'<Email>'+ISNULL(@strEmail,'False')+'</Email>'
	 SET @XMLString = @XMLString+'<Telephone>'+ISNULL(@strTelephone,'False')+'</Telephone>'
   END
   
   ELSE IF @strOption = 'DematStatus'
   BEGIN
     DECLARE @strDEmail VARCHAR(50) = 'False'
     DECLARE @strDTelephone VARCHAR(50) = 'False'
	 


	 SELECT @strDEmail = ISNULL(ParameterValue,'False')
	 FROM @tb_ParamListDetail
	 WHERE ParameterName = 'Email' AND HeaderName = 'X_Filter'

	
	 SELECT @strDTelephone = ISNULL(ParameterValue,'False')
	 FROM @tb_ParamListDetail
	 WHERE ParameterName = 'Mobile' AND HeaderName = 'X_Filter'
	 
	 SET @XMLString = @XMLString+'<Email>'+ISNULL(@strDEmail,'False')+'</Email>'
	 SET @XMLString = @XMLString+'<Mobile>'+ISNULL(@strDTelephone,'False')+'</Mobile>'
   END
   ELSE IF @strOption = 'DematListing'
   BEGIN
     DECLARE @StrDematType VARCHAR(60)=''
	 SELECT @StrDematType = ISNULL(ParameterValue,'')
	 FROM @tb_ParamListDetail
	 WHERE ParameterName = 'types' AND HeaderName = 'X_Filter'
	 SET @XMLString = @XMLString+'<Type>'+ISNULL(@StrDematType,'')+'</Type>'
	 
   END
   
   ELSE IF @strOption = 'ClientListing'
   BEGIN
     DECLARE @StrClientListingType VARCHAR(60)=''
	 SELECT @StrClientListingType = ISNULL(ParameterValue,'')
	 FROM @tb_ParamListDetail
	 WHERE ParameterName = 'OptionType' AND HeaderName = 'X_Filter'
	 SET @XMLString = @XMLString+'<OptionType>'+ISNULL(@StrClientListingType,'')+'</OptionType>'
	
	 IF @StrClientListingType IN('Listing','')
	 BEGIN
	   SET @strOption = 'ClientListing_1'
	 END
	 ELSE IF @StrClientListingType = 'Bank'
	 BEGIN
	   SET @strOption = 'ClientListing_2'
	 END
	 ELSE IF @StrClientListingType = 'Scheme'
	 BEGIN
	   SET @strOption = 'ClientListing_3'
	 END
	 ELSE IF @StrClientListingType = 'POA'
	 BEGIN
	   SET @strOption = 'ClientListing_4'
	 END
	 ELSE IF @StrClientListingType = 'NOMINEE'
	 BEGIN
	   SET @strOption = 'ClientListing_5'
	 END
	 ELSE IF @StrClientListingType = 'HOLDING'
	 BEGIN
	   SET @strOption = 'ClientListing_6'
	 END
   END
   
   
   IF @strOption = 'Disp_BillSummary'
   BEGIN
     IF isnull(@strIncludeLedBal,'false') = 'true'
	 BEGIN
	   set @strOption = 'BillSummary_L'
	 END
	 ELSE
	 BEGIN
	  set @strOption = 'BillSummary'
	 END
     SET @XMLString = @XMLString+'<BillDate>'+@strBillDate+'</BillDate>'+'<BillType>'+@strBillType+'</BillType>'
	 SET @XMLString = @XMLString+'<IncludeLedBal>'+@strIncludeLedBal+'</IncludeLedBal>'
   END
   DECLARE @strReport VARCHAR(50)='', @strData VARCHAR(50)=''
   SELECT @strReport = ISNULL(ParameterValue,'')   From @tb_ParamListDetail   WHERE ParameterName = 'Report' AND HeaderName = 'X_Filter'
   SELECT @strData = ISNULL(ParameterValue,'')   From @tb_ParamListDetail   WHERE ParameterName = 'Data' AND HeaderName = 'X_Filter'
   
   IF ISNULL(@strReport,'') =''
   BEGIN
     SET @strReport = ''
   END   
   
   IF ISNULL(@strData,'') =''
   BEGIN
     SET @strData = ''
   END   
   
   IF @strOption = 'PerformanceReport'
   BEGIN
     IF isnull(@strReport,'') = 'ISIN'
	 BEGIN
	   set @strOption = 'PerformanceReport_I'
	 END
	 ELSE
	 BEGIN
	  set @strOption = 'PerformanceReport'
	 END
     SET @XMLString = @XMLString+'<Report>'+@strReport+'</Report>'+'<Data>'+@strData+'</Data>'
   END
   
   
   SET @XMLString = REPLACE(@XMLString,'##FromDate##',isnull(@dtFromDate,''))
   SET @XMLString = REPLACE(@XMLString,'##ToDate##',ISNULL(@dtToDate,''))
   
   SET @XMLString = REPLACE(@XMLString,'##Level##',@strLevel)
   
   IF ISNULL(@StrCode,'') = '' AND @strOption NOT IN('BillSummary','BillSummary_L','TransactionStatus','TransactionStatement','PerformanceReport',
   'PerformanceReport_I','ClientOutstanding','DematStatus','DematListing','ClientListing_1','ClientListing_2','ClientListing_3','ClientListing_4','ClientListing_5',
   'ClientListing_6')
   BEGIN
     DELETE FROM @tbl_UserList
	
	 INSERT INTO @tbl_UserList
     EXEC dbo.stpr_GetClientAccessListNew @strUserid, @strSelection, ''
	
	 SELECT TOP 1 @StrCode = ClientCode FROM @tbl_UserList 
	 
	 /*IF @strOption = 'DisLedger'
	 BEGIN
       SET @strUserid =  @StrCode
     END*/
   END	  
   
   SET @XMLString = REPLACE(@XMLString,'##UserId##',@strUserid)
   SET @XMLString = REPLACE(@XMLString,'##Code##',@StrCode)
   SET @XMLString = REPLACE(@XMLString,'##Selection##',@strSelection)
   
   
   
   SET @XMLString = REPLACE(@XMLString,'##ShowValuation##',isnull(@strShowValuation,'False'))

      
  --SELECT @XMLString 
  
  SELECT TOP 1 @vcProcedureName = ProcedureName 
  FROM tbl_ReportGridViewFormat (NOLOCK) 
  WHERE ProductCode = @i_vcProjectCode AND ReportCode = @strActionName AND ReportCategroy = @strOption;
  IF @vcProcedureName = ''
  BEGIN
    SELECT TOP 1 @vcProcedureName = ProcedureName 
    FROM tbl_ReportGridViewFormat (NOLOCK) 
    WHERE ProductCode = '' AND ReportCode = @strActionName AND ReportCategroy = @strOption;
  END
 
  
  SET @XMLString = REPLACE(@XMLString,'''','''''')
  --select @XMLString
  SET @strString = 'EXEC DBO.' + @vcProcedureName + ' ''' + @XMLString + ''', @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT';
 -- SELECT @strString
  EXEC sp_executesql @strString, N'@o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT', @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT;
  
  --SELECT @o_vcErrorMessage
  IF ISNULL(@o_vcErrorMessage,'') = ''
  BEGIN
    RETURN 1
  END  

  EXEC stpr_GenerateMobileGrid @o_vcErrorMessage, 'Detail', @strActionName, @strOption, 'W'  
  
  DECLARE @TotalList VARCHAR(MAX)='', @RightList VARCHAR(MAX)='',@HideList VARCHAR(MAX)='',
  @DateFormat VARCHAR(50)='DD/MM/YYYY',@DateFormatList VARCHAR(MAX)='', @Dec2List VARCHAR(MAX)='', @Dec4List VARCHAR(MAX)='',
  @DrCRColorList VARCHAR(MAX)='', @PnLColorList VARCHAR(MAX)='', @PrimaryKey VARCHAR(MAX)=''
	 
  
  DECLARE @MobileColumns VARCHAR(MAX)='', @TabletColumns VARCHAR(MAX)='',@WebColumns VARCHAR(MAX)='',
  @AsOnDate VARCHAR(12)=@dtFromDate, @ToDt VARCHAR(11)=@dtToDate,
  @StrMenuOption VARCHAR(100)=@strOption1, @StrProduct VARCHAR(100)='', @trxRepType VARCHAR(MAX)=''
	 
  SELECT @MobileColumns = @MobileColumns+','+ ColumnHeading
  FROM tbl_ReportGridViewFormat(NOLOCK) 
  WHERE ReportCode = @strActionName
  AND ReportCategroy = @strOption
  AND Productid = 'M'
  AND ColumnVisible = 0
  AND ISNULL(ProductCode,'') = ''
  ORDER BY ORDERBY
	 
  SET @MobileColumns = SUBSTRING(@MobileColumns,2,LEN(@MobileColumns))
	 
  SELECT @TabletColumns = @TabletColumns+','+  ColumnHeading
  FROM tbl_ReportGridViewFormat(NOLOCK) 
  WHERE ReportCode = @strActionName
  AND ReportCategroy = @strOption
  AND Productid IN('T','M')
  and ColumnVisible = 0
  AND ISNULL(ProductCode,'') = ''
  ORDER BY ORDERBY
	 
  SET @TabletColumns = SUBSTRING(@TabletColumns,2,LEN(@TabletColumns))
	 
 SELECT @WebColumns = @WebColumns+','+ ColumnHeading
  FROM tbl_ReportGridViewFormat(NOLOCK) 
  WHERE ReportCode = @strActionName
  AND ReportCategroy = @strOption
  and ColumnVisible = 0
  AND ISNULL(ProductCode,'') = ''
  ORDER BY ORDERBY
	 
  SET @WebColumns = SUBSTRING(@WebColumns,2,LEN(@WebColumns))
 
  DECLARE @CompanyName VARCHAR(100) = Isnull((SELECT sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd = 'NAME'),'')
  DECLARE @CompanyAdd1 VARCHAR(1000) = Isnull((SELECT sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd = 'ADD1'),'') 
  DECLARE @CompanyAdd2 VARCHAR(1000) = Isnull((SELECT sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd = 'ADD2'),'')
  DECLARE @CompanyAdd3 VARCHAR(1000) = Isnull((SELECT sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd = 'ADD3'),'')
  DECLARE @strClientName VARCHAR(200)='', @strReportHeader VARCHAR(500)=''
	  
  SELECT @strClientName = um_user_name FROM User_master(NOLOCK) WHERE um_user_id = @strUserId
	  
  IF ISNULL(@strClientName,'') = ''
  BEGIN
	SELECT @strClientName = CM_name FROM CLIENT_master(NOLOCK) WHERE CM_CD = @strUserId  
  END
	  
  IF ISNULL(@dtToDate,'') = ''
  BEGIN
	SET @strReportHeader = @strOption + 'As on Date - ' +CONVERT(VARCHAR(10), CAST(@dtFromDate AS DATE), 103)+ ' \n ' + RTRIM(@strClientName) + ' ( ' + RTRIM(@strUserId) +' )'
  END
  ELSE
  BEGIN
	SET @strReportHeader = @strOption + 'From Date - ' +CONVERT(VARCHAR(10), CAST(@dtFromDate AS DATE), 103)+ ' to ' + CONVERT(VARCHAR(10), CAST(@dtToDate AS DATE), 103) + ' \n ' + RTRIM(@strClientName) + ' ( ' + RTRIM(@strUserId) +' )'
  END
  
  
  DECLARE @StrShowFilter1 VARCHAR(MAX)='', @StrShowFilter2 VARCHAR(MAX)='', @StrShowFilter3 VARCHAR(MAX)=''
  SELECT TOP 1 @StrShowFilter1 = ShowFilter1, @StrShowFilter2 = ShowFilter2, @StrShowFilter3 = ShowFilter3
  FROM tbl_CrossNetMenu(NOLOCK) WHERE  MenuTag = @StrMenuOption
      
  IF ISNULL(@StrShowFilter1,'') = ''
   SET @StrShowFilter1 = ''
	  
  IF ISNULL(@StrShowFilter2,'') = ''
   SET @StrShowFilter2 = ''
	  
  IF ISNULL(@StrShowFilter3,'') = ''
   SET @StrShowFilter3 = ''
	  
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<FROMDATE>>',CONVERT(VARCHAR(10), CAST(@AsOnDate AS DATE), 103))
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<TODATE>>',CONVERT(VARCHAR(10), CAST(@ToDt AS DATE), 103))
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<SEGMENT>>',ISNULL(@StrProduct,''))
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<RepType>>',ISNULL(@trxRepType,''))
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<SELECTION>>',ISNULL(@strSelection,''))
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<CODE>>',ISNULL(@StrCode,''))
  
  IF ISNULL(@strReportType,'') = 'ASONDATE'
  BEGIN
    SET @strReportType = @strReportType+' Date :- '+CONVERT(VARCHAR(10), CAST(@AsOnDate AS DATE), 103)
  END
  
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<ReportType>>',ISNULL(@strReportType,''))
  
  
  DECLARE @RateDate VARCHAR(15)='', @HoldingDate VARCHAR(15)=''
  
  IF CHARINDEX('<<RateDate>>',@StrShowFilter1)>0 OR  CHARINDEX('<<RateDate>>',@StrShowFilter2)>0 OR  CHARINDEX('<<RateDate>>',@StrShowFilter3)>0
  BEGIN
    SELECT @RateDate = max(rm_trx_date) from  DBO.Rate_master where rm_trx_date  <= @dtFromDate
  END
  
  IF CHARINDEX('<<HoldingDate>>',@StrShowFilter1)>0 OR  CHARINDEX('<<HoldingDate>>',@StrShowFilter2)>0 OR  CHARINDEX('<<HoldingDate>>',@StrShowFilter3)>0
  BEGIN
    SELECT @HoldingDate = MAX(hld_hold_date) FROM Holding(NOLOCK)
  END
  
  
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<RateDate>>',CONVERT(VARCHAR(10), CAST(@RateDate AS DATE), 103))
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<HoldingDate>>',CONVERT(VARCHAR(10), CAST(@HoldingDate AS DATE), 103))
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<Type>>',@strType)
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<Status>>',@strStatus)
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<DateType>>',@strDateType)
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<BillDate>>',CONVERT(VARCHAR(10), CAST(@strBillDate AS DATE), 103))
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<Report>>',ISNULL(@strReport,''))
  SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<Data>>',ISNULL(@strData,''))
  
	
  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<FROMDATE>>',CONVERT(VARCHAR(10), CAST(@AsOnDate AS DATE), 103))
  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<TODATE>>',CONVERT(VARCHAR(10), CAST(@ToDt AS DATE), 103))
  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<SEGMENT>>',ISNULL(@StrProduct,''))
  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<RepType>>',ISNULL(@trxRepType,''))
  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<SELECTION>>',ISNULL(@strSelection,''))
  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<CODE>>',ISNULL(@StrCode,''))

  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<ReportType>>',ISNULL(@strReportType,''))
  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<RateDate>>',CONVERT(VARCHAR(10), CAST(@RateDate AS DATE), 103))
  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<HoldingDate>>',CONVERT(VARCHAR(10), CAST(@HoldingDate AS DATE), 103))

  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<Type>>',@strType)
  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<Status>>',@strStatus)
  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<DateType>>',@strDateType)
  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<BillDate>>',CONVERT(VARCHAR(10), CAST(@strBillDate AS DATE), 103))
  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<Report>>',ISNULL(@strReport,''))
  SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<Data>>',ISNULL(@strData,''))
  
	
	  
  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<FROMDATE>>',CONVERT(VARCHAR(10), CAST(@AsOnDate AS DATE), 103))
  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<TODATE>>',CONVERT(VARCHAR(10), CAST(@ToDt AS DATE), 103))
	    
  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<SEGMENT>>',ISNULL(@StrProduct,''))
  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<RepType>>',ISNULL(@trxRepType,''))
  
  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<SELECTION>>',ISNULL(@strSelection,''))
  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<CODE>>',ISNULL(@StrCode,''))
  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<ReportType>>',ISNULL(@strReportType,''))	
  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<RateDate>>',CONVERT(VARCHAR(10), CAST(@RateDate AS DATE), 103))
  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<HoldingDate>>',CONVERT(VARCHAR(10), CAST(@HoldingDate AS DATE), 103))
  
  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<Type>>',@strType)
  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<Status>>',@strStatus)
  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<DateType>>',@strDateType)
  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<BillDate>>',CONVERT(VARCHAR(10), CAST(@strBillDate AS DATE), 103))

  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<Report>>',ISNULL(@strReport,''))
  SET @StrShowFilter3 = REPLACE(@StrShowFilter3,'<<Data>>',ISNULL(@strData,''))

  DECLARE @XMLStringFilter VARCHAR(MAX)=''

  SELECT @XMLStringFilter = '<XmlData>
						<CompanyName>' + @CompanyName + '</CompanyName>
						<CompanyAdd1>' + @CompanyAdd1 +'</CompanyAdd1>
						<CompanyAdd2>' + @CompanyAdd2 + '</CompanyAdd2>
						<CompanyAdd3>' + @CompanyAdd3 + '</CompanyAdd3>
						<ReportHeader>'+@strReportHeader+'</ReportHeader>
					    <PDFWidth>520</PDFWidth>
						<PDFHeight>269</PDFHeight>
						<MobileColumns>'+@MobileColumns+'</MobileColumns>
						<TabletColumns>'+@TabletColumns+'</TabletColumns>
                        <WebColumns>'+@WebColumns+'</WebColumns>
						<Headings>'
  IF ISNULL(@StrShowFilter1,'') <> ''
  BEGIN
	SET @XMLStringFilter = @XMLStringFilter+'<Heading>'+@StrShowFilter1+'</Heading>'				
  END                        
  IF ISNULL(@StrShowFilter2,'') <> ''
  BEGIN
	SET @XMLStringFilter = @XMLStringFilter+'<Heading>'+@StrShowFilter2+'</Heading>'				
  END                        
		
  IF ISNULL(@StrShowFilter3,'') <> ''
  BEGIN
	SET @XMLStringFilter = @XMLStringFilter+'<Heading>'+@StrShowFilter3+'</Heading>'				
  END                        
  SET @XMLStringFilter = @XMLStringFilter+'</Headings></XmlData>' 
  SELECT @XMLStringFilter AS [Settings]
END  
GO

CREATE PROCEDURE stpr_BillBreakup @dsXml AS XML = NULL WITH ENCRYPTION AS  
BEGIN  
  DECLARE @tbl_Variable dbo.tb_ParamList;  
   --DECLARE @tbl_UserList dbo.UserAccessList;  
   DECLARE @o_ParameterList varchar(max)='', @o_ParameterListxml XML;  
   DECLARE @tb_ParamListDetail DBO.tb_ParamList ;  
     
   --- PARAMETER LIST  
     
   EXEC SP_ParameterXMLRep @dsXml, @o_ParameterList output  
   IF ISNULL(@o_ParameterList,'') <> ''  
   BEGIN  
     SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)  
     
     INSERT INTO @tb_ParamListDetail(ParameterName,  ParameterValue, HeaderName)   
     SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code ,  
     Parameter.value('(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue,  
     Parameter.value('(MasterTag)[1]', 'VARCHAR(MAX)') AS MasterTag  
     FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)  
   END  
  
   DECLARE @strSQL NVARCHAR(MAX) = ''  
   DECLARE @strFromDate VARCHAR(50) = ''  
   DECLARE @strToDate VARCHAR(50) = ''  
   DECLARE @strLevel CHAR(5) = ''  
   DECLARE @strUserid VARCHAR(500)='', @strSelection VARCHAR(30), @strCode VARCHAR(20), @strReportDisplay VARCHAR(1)  
  
   SELECT @strFromDate = ParameterValue  From @tb_ParamListDetail   
   WHERE ParameterName = 'FromDate' AND HeaderName = 'X_Filter'  
   SELECT @strToDate = ParameterValue  From @tb_ParamListDetail   
   WHERE ParameterName = 'ToDate' AND HeaderName = 'X_Filter'  
   
   SELECT @strLevel = ParameterValue  From @tb_ParamListDetail   
   WHERE ParameterName = 'Level'  
   SELECT @strUserid = ParameterValue From @tb_ParamListDetail where ParameterName = 'UserId'  
   SELECT @StrCode = ParameterValue  From @tb_ParamListDetail 
   WHERE ParameterName = 'Code' AND HeaderName = 'X_Filter'
   SELECT @strSelection = ParameterValue  From @tb_ParamListDetail 
   WHERE ParameterName = 'Selection' AND HeaderName = 'X_Filter'
   
   SELECT @strReportDisplay = ParameterValue  From @tb_ParamListDetail 
   WHERE ParameterName = 'ReportDisplay' 
  
   --- USER ACCESS RIGHTS  
     
   	IF ISNULL(@strSelection,'') = ''
    BEGIN
      SET @strSelection = 'CLIENT'
    END

   
   IF @StrCode <> '' AND UPPER(@strSelection) = 'CLIENT'
   BEGIN
     SET @strUserid = @StrCode
   END

   DECLARE @tbl_UserList dbo.UserAccessList;
   INSERT INTO @tbl_UserList EXEC dbo.stpr_GetClientAccessListNew @strUserid, @strSelection,@strCode
   
   
  IF @strReportDisplay IN('D','E','A')
  BEGIN
    DECLARE @o_Message NVARCHAR(MAX)=''
    EXEC stpr_TypestHeaderProc @strUserid, @strSelection, @strCode, @o_Message OUTPUT
  
    SET @o_Message = REPLACE(@o_Message,'##ReportName##','Transaction Statement')
    SET @o_Message = REPLACE(@o_Message,'##Grid1##','Transaction Statement')
    SET @o_Message = REPLACE(@o_Message,'##Grid2##','')
    SET @o_Message = REPLACE(@o_Message,'##Grid3##','')
    SET @o_Message = REPLACE(@o_Message,'##Grid4##','')
	
	IF ISNULL(@strReportDisplay,'') <> 'A'
	BEGIN
	  SET @o_Message = REPLACE(@o_Message,'##FormatGrid##','Grid1')
	END   
	ELSE
	BEGIN
	  SET @o_Message = REPLACE(@o_Message,'##FormatGrid##','')
	END
	
    SET @o_Message = '<root>' + @o_Message + '</root>';
  
    DECLARE @xmlData2 XML = CAST(@o_Message AS XML);
	
	DECLARE @DynamicColumns NVARCHAR(MAX);
    
	SET  @DynamicColumns = '';
  
	
    WITH DistinctNodes AS (
    SELECT DISTINCT
       NodeName = X.n.value('local-name(.)', 'NVARCHAR(100)')
    FROM @xmlData2.nodes('/root/ClientDetail/*') AS X(n))
    
    SELECT @DynamicColumns = @DynamicColumns+','+
        'LTRIM(RTRIM(X1.value(''(' + XX.NodeName + ')[1]'', ''NVARCHAR(MAX)''))) AS [' + XX.NodeName + ']'
    FROM DistinctNodes XX
   
    SELECT @DynamicColumns = SUBSTRING(@DynamicColumns,2,LEN(@DynamicColumns)-1) 
  
    DECLARE @sql NVARCHAR(MAX);
    SET @sql = '
    SELECT ' + @DynamicColumns + '
    FROM @xmlData2.nodes(''/root/ClientDetail'') AS T(X1)';
  
    EXEC sp_executesql @sql, N'@xmlData2 XML', @xmlData2;
	IF ISNULL(@strReportDisplay,'') <> 'A'
	BEGIN
	  SELECT ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, ColumnFormat, Columnstyle, DecimalPlace, ColumnTotal 
      FROM tbl_ReportGridViewFormat(NOLOCK) 
	  WHERE ReportCode='CrossNet'
      AND ReportCategroy = 'BillBreakup'
      Order By Orderby
	END   
  END	
  
 
      
  SELECT ClientCode AS [Client ID], ClientName, [Date], [ISIN Name] = [Security], [ISIN], [Particular], td_debit_credit AS [Buy/Sell], td_qty as [Qty], 
  td_amount as [Value], [OurCharges] = [ChargesAmount] --, [CDSLChanges] = [ActualCDSLCharges]  
  FROM(
  SELECT 1 as tag ,  oc_clientId AS ClientCode, ClientName, oc_description [Particular], [Date] = '', [Security] = '', [ISIN] = '', td_debit_credit = '', td_qty = 0, 
  td_amount = 0, Sum(isNull(oc_amt,0)) [ChargesAmount], [ActualCDSLCharges] = 0
  FROM Other_Charges with (nolock) , @tbl_UserList
  WHERE oc_date BETWEEN @strFromDate AND @strToDate 
  AND oc_clientId = CLIENTCODE
  Group By oc_clientId, ClientName, oc_chargecode,oc_description Having Sum(isNull(oc_amt,0)) <> 0
  UNION ALL
  SELECT 2 as tag ,  oc_clientId AS ClientCode, ClientName, oc_description [Particular], [Date] = '', [Security] = '', [ISIN] = '', td_debit_credit = '', td_qty = 0, 
   td_amount = 0, 0 [ChargesAmount], [ActualCDSLCharges] = Sum(CASE oc_chargecode WHEN '04' THEN 0 ELSE 
				oc_actualcdslcharge END)
   FROM Other_charges WITH (NOLOCK) , @tbl_UserList
   WHERE clientcode = oc_clientid 
   AND oc_date BETWEEN @strFromDate AND @strToDate
   GROUP BY oc_clientid, ClientName, oc_chargecode, oc_description
   HAVING Sum(CASE oc_chargecode WHEN '04' THEN 0 ELSE 
				oc_actualcdslcharge END) <> 0
  UNION ALL
  SELECT 0 AS tag,  td_ac_code as ClientCode, ClientName, nr_description [Particular], td_curdate AS [Date], sc_isinname AS [Security], 
  [ISIN] = td_isin_code, td_debit_credit,  
  td_qty = SUM(td_qty), td_amount = SUM(td_qty*isnull(td_rate,0)),
  Sum(isNull(td_amount,0))+sum(isNull(td_cdslcharge,0)) [ChargesAmount] , sum(isNull(TD_ACTUALCDSLCHARGE,0)) AS [ActualCDSLCharges]
  From trxdetail  with (nolock), narration with (nolock), Security(NOLOCK) , Chargesmaster WITH (NOLOCK) , @tbl_UserList
  Where td_narration = nr_code 
  AND td_ac_code = CLIENTCODE
  and cast(cg_code AS NUMERIC) = td_charge_code
  AND sc_isincode = td_isin_code
  AND td_curdate BETWEEN @strFromDate AND @strToDate 
  GROUP BY nr_code , td_ac_code,ClientName, nr_code, nr_description, td_curdate , sc_isinname, td_isin_code, td_debit_credit
  HAVING SUM(ISNULL(td_amount,0)+isNull(td_cdslcharge,0)+isNull(TD_ACTUALCDSLCHARGE,0)) <> 0) xxx
  ORDER BY ClientCode, tag, [Particular]
  
/*  
   SELECT * INTO #tbl_charges from(
   SELECT oc_clientid AS td_ac_code, oc_chargecode, ChargesName = oc_description, ISNULL(sum(isNull(oc_amt, 0)),0) AS td_ourbil
   FROM Other_charges WITH (NOLOCK), @tbl_UserList
   WHERE oc_clientid = CLIENTCODE AND oc_date BETWEEN @strFromDate AND @strToDate 
   GROUP BY oc_clientid, oc_chargecode, oc_description
   HAVING sum(isNull(oc_amt, 0)) <> 0
   UNION ALL
   SELECT td_ac_code, cg_schedule, ChargesName = isnull((select cm_name from client_master where cm_Cd = cg_schedule),''),
   SUM(ISNULL(td_amount, 0) + isNull(td_cdslcharge, 0)) AS td_ourbil
   FROM trxdetail WITH (NOLOCK), Chargesmaster WITH (NOLOCK), @tbl_UserList
   WHERE cast(cg_code AS NUMERIC) = td_charge_code 
   AND CLIENTCODE = td_ac_code 
   AND td_curdate BETWEEN  @strFromDate AND @strToDate  
   GROUP BY td_ac_code, cg_schedule
   HAVING sum(isNull(td_amount, 0) + isNull(td_cdslcharge, 0))  <> 0
   UNION ALL
   SELECT td_ac_code, cg_schedule='', ChargesName = 'CDSL Charges',
   SUM(ISNULL(TD_ACTUALCDSLCHARGE, 0)) AS td_ourbil		
   FROM trxdetail WITH (NOLOCK), Chargesmaster WITH (NOLOCK), @tbl_UserList
   WHERE cast(cg_code AS NUMERIC) = td_charge_code AND CLIENTCODE = td_ac_code 
   AND td_curdate BETWEEN @strFromDate AND @strToDate 
   GROUP BY td_ac_code, cg_schedule
   HAVING SUM(ISNULL (Td_actualcdslcharge, 0)) <> 0
   UNION ALL
   SELECT oc_clientid, oc_chargecode ='', ChargesName = 'CDSL Charges', Sum(CASE oc_chargecode WHEN '04' THEN 0 ELSE 
				oc_actualcdslcharge END) td_ourbil
   FROM Other_charges WITH (NOLOCK) , @tbl_UserList
   WHERE clientcode = oc_clientid 
   AND oc_date BETWEEN @strFromDate AND @strToDate
   GROUP BY oc_clientid, oc_chargecode, oc_description) x1
   ORDER BY  CAST((CASE WHEN ChargesName LIKE '%GST%' THEN 9 ELSE 0 END) AS INT)
   
*/
   
   /*IF EXISTS(SELECT 1 FROM #tbl_charges)
   BEGIN
  
     DECLARE @tbl_header TABLE (SerialNo INT IDENTITY(1,1), CHARGESNAME VARCHAR(100))
     --SELECT * FROM #tbl_charges
     INSERT INTO @tbl_header
     SELECT *
     FROM(
     SELECT DISTINCT CHARGESNAME
     FROM #tbl_charges) X1
     ORDER BY CAST((CASE WHEN ChargesName LIKE '%GST%' THEN 9 ELSE 1 END) AS INT)
 
     declare @cols VARCHAR(MAX)='', @pivotCount NVARCHAR(MAX)='';
     DECLARE @sql NVARCHAR(MAX)='', @cols1 VARCHAR(MAX)='', @cols2 VARCHAR(MAX)=''
 
     SELECT @cols = @cols + ',[' + CHARGESNAME + ']' 
     FROM @tbl_header
     ORDER BY SerialNo

     SELECT @cols1 = @cols1 + ',ISNULL([' + CHARGESNAME + '],0) AS [ '+ CHARGESNAME + ']' 
     FROM @tbl_header
     ORDER BY SerialNo
  
     SELECT @cols2 = @cols2 + '+ISNULL([' + CHARGESNAME + '],0)' 
     FROM @tbl_header --WHERE CHARGESNAME <> 'CDSL Charges'
     ORDER BY SerialNo
  
     SET @cols  = SUBSTRING(@cols,2,LEN(@cols))
     SET @cols1  = SUBSTRING(@cols1,2,LEN(@cols1))
  
    SET @pivotCount = 'SELECT * INTO #TBL1 FROM (SELECT td_ac_code, ChargesName, td_ourbil  FROM #tbl_charges) x  
                      PIVOT (SUM(td_ourbil) FOR ChargesName IN (' + @cols + ')) p; 
					  SELECT td_ac_code as [Client ID] , CM_NAME AS [Client Name],'+ @cols2 + ' AS [Total Charges], 
					  '+@cols1+' FROM #TBL1, CLIENT_MASTER CM WHERE td_ac_code = CM_CD ; 
					  DROP TABLE #TBL1   ' 
    EXEC(@pivotCount)  
  END	*/
  --DROP TABLE #tbl_charges
END
GO

CREATE PROCEDURE stpr_CrossNetMenu @dsXml AS XML = NULL
WITH ENCRYPTION
AS
BEGIN
	DECLARE @tbl_Variable dbo.tb_ParamList;
	DECLARE @tbl_UserList dbo.UserAccessList;
	DECLARE @o_ParameterList VARCHAR(max) = '', @o_ParameterListxml XML;
	DECLARE @tb_ParamListDetail DBO.tb_ParamList;

	--- PARAMETER LIST
	EXEC SP_ParameterXMLRep @dsXml, @o_ParameterList OUTPUT

	IF ISNULL(@o_ParameterList, '') <> ''
	BEGIN
		SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)

		INSERT INTO @tb_ParamListDetail (ParameterName, ParameterValue, HeaderName)
		SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code, Parameter.value(
				'(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue, Parameter.value('(MasterTag)[1]', 
				'VARCHAR(MAX)') AS MasterTag
		FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
	END

	DECLARE @strUserid VARCHAR(500) = '', @strLevel INT = 0, @dtFromDate DATE, @dtToDate DATE, @StrClientCode 
		VARCHAR(50) = '', @strOption VARCHAR(50) = ''

	SELECT @strUserid = ParameterValue
	FROM @tb_ParamListDetail
	WHERE ParameterName = 'UserId'

	SELECT @strOption = ParameterValue
	FROM @tb_ParamListDetail
	WHERE ParameterName = 'Option'

	IF @strOption = 'AccessRights'
	BEGIN
	    DECLARE @TBL_Menu TABLE(SerNo int identity(1,1), ModuleId INT, MenuName VARCHAR(100),
		MenuCode VARCHAR(20), MenuTag VARCHAR(50), DenyRights VARCHAR(100), 
		TargetForm VARCHAR(100),[Path] VARCHAR(100) ,Enable VARCHAR(20) ,[Add]  VARCHAR(20), [Edit] VARCHAR(20),[Delete]  VARCHAR(20),
		[View] VARCHAR(20), Filters VARCHAR(MAX))
		
		DECLARE @tbl_Data TABLE([Value] VARCHAR(MAX), [DisplayName] VARCHAR(100))
        DECLARE @iSerialNo INT, @strdbquery VARCHAR(MAX) ='', @strcolumnname VARCHAR(100)='',
        @ojson VARCHAR(MAX)='', @JsonMain VARCHAR(MAX)='', @o_strJson  VARCHAR(MAX)=''
		
		INSERT INTO @TBL_Menu(ModuleId, MenuName, MenuCode, MenuTag, DenyRights, TargetForm, [Path], Enable, [Add], [Edit],
		[Delete],[View], Filters)
		SELECT ModuleId = ModuleCode, MenuName = MenuName, MenuCode = MenuCode, MenuTag = MenuTag, DenyRights = 
			DenyRights, TargetForm = TargetForm, [Path] = [Path], [Enable] = CASE [Enable] WHEN 'Y' THEN 'true' 
				ELSE 'false' END, [Add] = CASE SUBSTRING(Rights, 1, 1) WHEN 'Y' THEN 'true' ELSE 'false' END, [Edit] 
			= CASE SUBSTRING(Rights, 2, 1) WHEN 'Y' THEN 'true' ELSE 'false' END, [Delete] = CASE SUBSTRING(Rights, 
					3, 1) WHEN 'Y' THEN 'true' ELSE 'false' END, [View] = (CASE SUBSTRING(Rights, 4, 1) WHEN 'Y' THEN 
						'true' ELSE 'false' END), Filters
		FROM tbl_CrossNetMenu(NOLOCK)
		WHERE MenuType = 'C' AND Enable = 'Y'
        
		DECLARE CurfilterUpd
        CURSOR FOR SELECT  x.SerNo, dbquery, columnname
        FROM @TBL_Menu X,  tbl_InsertUpdateXMLDropDownQuery(NOLOCK) xq
        WHERE x.MenuTag = xq.ModuleName AND USERCODE <> 'X'
        and charindex('##'+columnname+'##',X.Filters)>0 AND DBQueryType ='Q'
        AND dbquery <> ''
       order by xq.SerialNo
  
       OPEN CurfilterUpd 
       FETCH NEXT FROM CurfilterUpd INTO @iSerialNo, @strdbquery, @strcolumnname
       WHILE @@FETCH_STATUS = 0
       BEGIN 
         
         DELETE FROM @tbl_Data
         INSERT INTO @tbl_Data([Value], [DisplayName])
         EXEC(@strdbquery)
         SET @ojson = ''
         IF EXISTS(SELECT 1 FROM @tbl_Data)
         BEGIN
           SET @ojson = (SELECT [label] = [DisplayName], [Value]  FROM @tbl_Data FOR JSON PATH) 
         END
         ELSE
         BEGIN
           SET @ojson = (SELECT [label] = '', [Value] ='' FOR JSON PATH) 
         END
		
         UPDATE A SET A.Filters = REPLACE(A.Filters, '"##'+@strcolumnname+'##"',@ojson)
         FROM @TBL_Menu A
         WHERE A.SerNo = @iSerialNo
	     FETCH NEXT FROM CurfilterUpd INTO   @iSerialNo, @strdbquery, @strcolumnname
      END
      CLOSE CurfilterUpd
      DEALLOCATE CurfilterUpd
	  
	  UPDATE X SET X.Filters = REPLACE(X.Filters,'##FromDate##',CONVERT(VARCHAR,DATEADD(DAY,-7,GETDATE()),112))
      FROM @TBL_Menu X
  
      UPDATE X SET X.Filters = REPLACE(X.Filters,'##ToDate##',CONVERT(VARCHAR,GETDATE(),112))
      FROM @TBL_Menu X
	  
      SELECT * FROM  @TBL_Menu
	  ORDER BY ModuleId
	  RETURN
	END
	ELSE IF @strOption = 'Routes'
	BEGIN
		SELECT [name] = MenuName, [path] = [Path], element = MenuTag
		FROM tbl_CrossNetMenu
		WHERE Enable = 'Y'

		RETURN
	END
	ELSE IF @strOption = 'List'
	BEGIN
		SELECT ModuleCode AS 'id', MenuName AS 'title', '' AS 'path', '' AS 'icon', submenu = (
				SELECT REPLACE((
							SELECT ModuleCode AS 'id', MenuName AS 'title', [Path] AS 'path', '' AS 'icon'
							FROM tbl_CrossNetMenu b
							WHERE b.ParentMenu = a.MenuName AND b.Enable = 'Y'
							FOR JSON PATH
							), '\\', '')
				)
		FROM tbl_CrossNetMenu a
		WHERE a.MenuType = 'P' AND a.Enable = 'Y'
		ORDER BY MenuCode
	END
END
GO

CREATE PROCEDURE CommonSerch @i_vcinput XML, @o_vcFlag VARCHAR(1) OUTPUT, @o_vcMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN
  DECLARE @tbl_InputJSONTable DBO.tb_ParamList;
  DECLARE @o_ParameterList VARCHAR(max) = '', @o_ParameterListxml XML;
  DECLARE @strModuleName VARCHAR(50) = '', @strOption VARCHAR(50) = '', @strUserid VARCHAR(50)='',
  @strUserType VARCHAR(10), @strSelection varchar(1)='C', @strString VARCHAR(MAX)='', @strClientString VARCHAR(MAX)='',
  @strCode VARCHAR(50)=''
  
  EXEC SP_ParameterXMLRep @i_vcinput, @o_ParameterList OUTPUT
  
  IF ISNULL(@o_ParameterList, '') <> ''
  BEGIN
	SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)

	INSERT INTO @tbl_InputJSONTable (ParameterName, ParameterValue, HeaderName, Jsontag)
	SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code, Parameter.value(
				'(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue, Parameter.value('(MasterTag)[1]', 
				'VARCHAR(MAX)') AS MasterTag, Parameter.value('(JsonLevel)[1]', 'VARCHAR(MAX)') AS 
			JsonLevel
	FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
  END
	
  SELECT TOP 1 @strUserid = ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'UserId'
  SELECT @strSelection = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Selection'
  SELECT @strUserType = REPLACE(ParameterValue,'null','')  FROM @tbl_InputJSONTable WHERE ParameterName = 'UserType'
  
  IF ISNULL(@strSelection,'C') = 'C'
  BEGIN
	SET @strSelection = 'C'
  END
  
  IF ISNULL(@strUserType,'') = 'User'
  BEGIN
    SET @strSelection = 'C'
  END
  ELSE IF ISNULL(@strUserType,'') = 'Branch'
  BEGIN
    SET @strSelection = 'B'
  END
  ELSE
  BEGIN
    SET @strSelection = 'C'
  END
  
  DECLARE @tbl_UserList dbo.UserAccessList;
  INSERT INTO @tbl_UserList EXEC dbo.stpr_GetClientAccessListNew @strUserid , @strSelection, @strCode 
  
  
  SELECT DISTINCT LTRIM(RTRIM(cm_cd)) AS [Value], cm_name + ' [' + cm_cd + ']' AS [DisplayName]
  FROM Client_master(NOLOCK) XMAIN, @tbl_UserList 
  WHERE cm_schedule = 49843750 
  AND CM_CD = ClientCode
  RETURN 1

END	
GO

CREATE PROCEDURE stpr_CrossNetSearch @dsXml AS XML = NULL WITH ENCRYPTION AS
BEGIN

DECLARE @tbl_Variable dbo.tb_ParamList;
   DECLARE @tbl_UserList dbo.UserAccessList;
   DECLARE @o_ParameterList varchar(max)='', @o_ParameterListxml XML;
   DECLARE @tb_ParamListDetail DBO.tb_ParamList ;

--- PARAMETER LIST
   
   EXEC SP_ParameterXMLRep @dsXml, @o_ParameterList output
   IF ISNULL(@o_ParameterList,'') <> ''
   BEGIN
     SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)
   
     INSERT INTO @tb_ParamListDetail (ParameterName,  ParameterValue, HeaderName)
     SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code ,
     Parameter.value('(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue,
	 Parameter.value('(MasterTag)[1]', 'VARCHAR(MAX)') AS MasterTag
     FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
   END 

   DECLARE @strUserid VARCHAR(500)='', @strOption VARCHAR(500)=''

   SELECT @strUserid = ParameterValue From @tb_ParamListDetail where ParameterName = 'UserId'

   SELECT @strOption = ParameterValue From @tb_ParamListDetail 
   where ParameterName = 'Option'

   IF @strOption IN('Client','Search')
   BEGIN
     INSERT INTO @tbl_UserList
     EXEC dbo.stpr_GetClientAccessList @strUserid

     SELECT ClientCode AS 'Value', ClientName + ' [' + ClientCode + ']' AS 'DisplayName' from @tbl_UserList
     RETURN
   END
   
   --ELSE IF @strOption = 'HoldingDate'
   --BEGIN
   ----FORMAT(GETDATE(), 'dd-MMM-yyyy')
   --Select 'Latest' AS 'Value', 'Latest' AS 'DisplayName'
   --UNION ALL
   --Select FORMAT(CAST(right(Rtrim(Name),8) AS DATE), 'dd-MMM-yyyy') 'Value', right(Rtrim(Name),8) AS 'DisplayName' From SysObjects where xtype = 'U' and name like 'Holding_%' and  isnumeric(right(Rtrim(Name),8)) = 1 
   --order by DisplayName desc
   --RETURN
   --END

END
GO

CREATE PROCEDURE stpr_GetSlipNo @i_vcinput XML, @o_vcFlag VARCHAR(1) OUTPUT, @o_vcMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN

DECLARE @tbl_InputJSONTable DBO.tb_ParamList ;
  DECLARE @o_ParameterList varchar(max)='', @o_ParameterListxml XML;
  DECLARE @strModuleName VARCHAR(50)='', @strOption VARCHAR(50)=''
   
   --- PARAMETER LIST
   
  EXEC SP_ParameterXMLRep @i_vcinput, @o_ParameterList output
  IF ISNULL(@o_ParameterList,'') <> ''
  BEGIN
    SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)
   
    INSERT INTO @tbl_InputJSONTable (ParameterName,  ParameterValue, HeaderName, Jsontag) 
    SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code ,
    Parameter.value('(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue,
	Parameter.value('(MasterTag)[1]', 'VARCHAR(MAX)') AS MasterTag,
	Parameter.value('(JsonLevel)[1]', 'VARCHAR(MAX)') AS JsonLevel
    FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
  END 

  DECLARE @strtb_instcd VARCHAR(10)='', @strtb_booksize VARCHAR(10)='', @strOption VARCHAR(20)='',
  @strtb_SlipNo VARCHAR(20)=''

  SELECT @strtb_instcd = ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'InstrumentType'
  SELECT @strtb_booksize = ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'Leaves'
  SELECT @strOption = ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'Option'
  SELECT @strtb_SlipNo = ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'SlipNo'
  
  
  IF @strOption = 'chm_chqno'
  BEGIN
    DECLARE @strtb_chqno VARCHAR(100) = '',  @strtb_tochqno VARCHAR(100) = ''
    
    SELECT TOP 1 @strtb_chqno = ISNULL(chm_chqno,0), @strtb_tochqno = ((chm_chqno + chm_booksize)-1) from Chequemaster(NOLOCK) 
	where chm_instcd=@strtb_instcd
    and chm_booksize =@strtb_booksize and chm_status='N'

    IF @strtb_chqno <> 0
    BEGIN
      SET @o_vcFlag = 'S'
      SET @o_vcMessage = '<Message></Message>'+'<SlipNo>' + @strtb_chqno +'</SlipNo><ToNo>'+@strtb_tochqno+ '</ToNo>'
      RETURN 1
    END
    ELSE
    BEGIN
     SET @o_vcFlag = 'E'
     SET @o_vcMessage = '<Message>Cheque book is not entered in stock.</Message>'
     RETURN 1
    END
  END	
  IF @strOption = 'chm_chqno_validate'
  BEGIN
    IF NOT EXISTS(select 1 from Stock_master(NOLOCK) WHERE @strtb_SlipNo BETWEEN stm_fromno and stm_tono and stm_instcd = @strtb_instcd)
	BEGIN
	  SET @o_vcFlag = 'E'
      SET @o_vcMessage = '<Message>Slip is not entered in stock.</Message>'
      RETURN 1
	END
  END
END
GO

CREATE PROCEDURE stpr_CrosstrxholdbillNew @dsXml NVARCHAR(MAX) 
WITH ENCRYPTION
AS
BEGIN

  DECLARE @tbl_Variable dbo.tb_ParamList;
  DECLARE @tbl_UserList dbo.UserAccessList;
  DECLARE @o_ParameterList varchar(max)='', @o_ParameterListxml XML;
  DECLARE @tb_ParamListDetail DBO.tb_ParamList ;
   
   --- PARAMETER LIST
   
  EXEC SP_ParameterXMLRep @dsXml, @o_ParameterList output
  IF ISNULL(@o_ParameterList,'') <> ''
  BEGIN
    SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)
   
    INSERT INTO @tb_ParamListDetail(ParameterName,  ParameterValue, HeaderName)
    SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code ,
    Parameter.value('(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue,
	Parameter.value('(MasterTag)[1]', 'VARCHAR(MAX)') AS MasterTag
    FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
  END 
  
  DECLARE @strFromDate VARCHAR(8), @strToDate VARCHAR(8), @strCode VARCHAR(500) = '', @strActionName VARCHAR(50),
  @strOption VARCHAR(50), @strUserid VARCHAR(50), @strLevel INT, @strRequestFrom VARCHAR(1), @strReportDisplay VARCHAR(1)='P',
  @strSelection VARCHAR(10)=''
    
  
  SELECT @strActionName = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'ActionName'
  SELECT @strOption = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'Option'
  SELECT TOP 1 @strUserid = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'UserId'
  SELECT @strLevel = CAST(ParameterValue AS INT) FROM @tb_ParamListDetail where ParameterName = 'Level'
  SELECT @strRequestFrom = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'RequestFrom'
  SELECT @strReportDisplay = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'ReportDisplay'
   
  SELECT @strFromDate = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'FromDate' 
  SELECT @strToDate = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'ToDate' 
  SELECT TOP 1 @strCode = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'Code' 
  SELECT TOP 1 @strSelection = ParameterValue FROM @tb_ParamListDetail where ParameterName = 'Selection' 

  
  IF ISNULL(@strSelection,'') = ''
  BEGIN
    SET @strSelection = 'CLIENT'
  END

  INSERT INTO @tbl_UserList EXEC dbo.stpr_GetClientAccessListNew @strUserid , @strSelection, @strCode 
		
  IF @strReportDisplay IN('D','E','A')
  BEGIN
    DECLARE @o_Message NVARCHAR(MAX)=''
    EXEC stpr_TypestHeaderProc @strUserid, @strSelection, @strCode, @o_Message OUTPUT
  
    SET @o_Message = REPLACE(@o_Message,'##ReportName##','Transaction/Holding/Bill')
    SET @o_Message = REPLACE(@o_Message,'##Grid1##','Transaction Statement')
    SET @o_Message = REPLACE(@o_Message,'##Grid2##','Holding Statement')
    SET @o_Message = REPLACE(@o_Message,'##Grid3##','Charges')
    SET @o_Message = REPLACE(@o_Message,'##Grid4##','Ledger')
	IF @strReportDisplay IN('E','D')
	BEGIN
	  SET @o_Message = REPLACE(@o_Message,'##FormatGrid##','Grid1,Grid2,Grid3,Grid4')
	END 
    ELSE 
	BEGIN
	  SET @o_Message = REPLACE(@o_Message,'##FormatGrid##','')
	END 
	
    SET @o_Message = '<root>' + @o_Message + '</root>';
  
    DECLARE @xmlData XML = CAST(@o_Message AS XML);
	
	DECLARE @DynamicColumns NVARCHAR(MAX);
    
	SET  @DynamicColumns = '';
  
	
    WITH DistinctNodes AS (
    SELECT DISTINCT
       NodeName = X.n.value('local-name(.)', 'NVARCHAR(100)')
    FROM @xmlData.nodes('/root/ClientDetail/*') AS X(n))
    
    SELECT @DynamicColumns = @DynamicColumns+','+
        'LTRIM(RTRIM(X1.value(''(' + XX.NodeName + ')[1]'', ''NVARCHAR(MAX)''))) AS [' + XX.NodeName + ']'
    FROM DistinctNodes XX
   
    SELECT @DynamicColumns = SUBSTRING(@DynamicColumns,2,LEN(@DynamicColumns)-1) 
  
    DECLARE @sql NVARCHAR(MAX);
    SET @sql = '
    SELECT ' + @DynamicColumns + '
    FROM @xmlData.nodes(''/root/ClientDetail'') AS T(X1)';
  
    EXEC sp_executesql @sql, N'@xmlData XML', @xmlData;
	
	IF @strReportDisplay NOT IN('A')
	BEGIN
	  SELECT ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, ColumnFormat, Columnstyle, DecimalPlace, ColumnTotal 
      FROM tbl_ReportGridViewFormat(NOLOCK) WHERE ReportCode='CrossNet'
      and ReportCategroy = 'TransactionStatement'
      Order By Orderby

      SELECT ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, ColumnFormat, Columnstyle, DecimalPlace, ColumnTotal 
      FROM tbl_ReportGridViewFormat(NOLOCK) WHERE ReportCode='CrossNet'
      and ReportCategroy = 'Disp_Holding'
      Order By Orderby
  
      SELECT ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, ColumnFormat, Columnstyle, DecimalPlace, ColumnTotal 
      FROM tbl_ReportGridViewFormat(NOLOCK) WHERE ReportCode='CrossNet'
      and ReportCategroy = 'BillDetails'
      Order By Orderby

      SELECT ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, ColumnFormat, Columnstyle, DecimalPlace, ColumnTotal 
      FROM tbl_ReportGridViewFormat(NOLOCK) WHERE ReportCode='CrossNet'
      and ReportCategroy = 'TransactionLedger'
      Order By Orderby
    END
  END	
  
  DECLARE @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1)='',  @o_vcErrorMessage VARCHAR(MAX) =''
  
  SET @dsXml = '<FromDate>'+@strFromDate+'</FromDate><ToDate>'+@strToDate+'</ToDate><UserId>'+@strUserid+'</UserId><Code>'+@strCode+'</Code><Selection>'+@strSelection+'</Selection><OutputType>G</OutputType><RequestFrom>W</RequestFrom>'
  EXEC stpr_TransactionStatement @dsXml , @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT
  
  
  SET @dsXml = '<FromDate>'+@strFromDate+'</FromDate><ToDate>'+@strToDate+'</ToDate><UserId>'+@strUserid+'</UserId><Code>'
  +@strCode+'</Code><Selection>'+@strSelection+'</Selection><OutputType>G</OutputType><RequestFrom>W</RequestFrom><ShowValuation>true</ShowValuation>'
  EXEC stpr_HoldingStatement @dsXml, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT
    
  
  SELECT ClientCode AS [ClientCode], ClientName, [Particular], [Date], [Security], [ISIN], td_debit_credit AS [Buy/Sell], td_qty as [Qty], 
  td_amount as [Value], [ChargesAmount] FROM(
  SELECT 1 as tag , oc_clientId AS ClientCode, ClientName, oc_description [Particular], [Date] = '', [Security] = '', [ISIN] = '', td_debit_credit = '', td_qty = 0, 
  td_amount = 0, Sum(isNull(oc_amt,0)) [ChargesAmount]
  FROM Other_Charges with (nolock), @tbl_UserList X  
  WHERE oc_date BETWEEN @strFromDate AND @strToDate 
  AND oc_clientId = X.ClientCode
  Group By oc_clientId,ClientName, oc_chargecode,oc_description Having Sum(isNull(oc_amt,0)) <> 0
  UNION ALL
  SELECT 0 AS tag, td_ac_code as ClientCode, ClientName, nr_description [Particular], td_curdate AS [Date], sc_isinname AS [Security], [ISIN] = td_isin_code, td_debit_credit,  
  td_qty = SUM(td_qty), td_amount = SUM(td_qty*isnull(td_rate,0)),
  Sum(isNull(td_amount,0)+isNull(td_cdslcharge,0)) [ChargesAmount]
  From trxdetail  with (nolock) , narration with (nolock), Security(NOLOCK), @tbl_UserList X    
  Where td_narration = nr_code 
  AND td_ac_code = X.ClientCode
  AND sc_isincode = td_isin_code
  AND td_curdate BETWEEN @strFromDate AND @strToDate
  GROUP BY nr_code ,td_ac_code,ClientName, nr_code, nr_description, td_curdate , sc_isinname, td_isin_code, td_debit_credit
  Having Sum(isNull(td_amount,0)+isNull(td_cdslcharge,0)) <> 0) xxx
  ORDER BY ClientCode, tag, [Particular]
  
  
  SELECT ClientCode, CONVERT(VARCHAR, cast( isNull((sum(Case when ld_dt < @strFromDate then ld_amount else 0 end)),0) as decimal(15,2))) + ' ' + 
  CASE WHEN ISNULL(SUM(CASE WHEN ld_dt < @strFromDate THEN ld_amount ELSE 0 END), 0) < 0 THEN 'Cr' 
        WHEN ISNULL(SUM(CASE WHEN ld_dt < @strFromDate THEN ld_amount ELSE 0 END), 0) > 0 THEN 'Dr' 
        ELSE '' END as [Previous Balance] , 
  CONVERT(VARCHAR, cast( isNull((sum(Case when ld_dt >= @strFromDate and ld_documentType = 'R' then ld_amount else 0 end)),0) as decimal(15,2))) + ' ' +
  CASE WHEN ISNULL(SUM(CASE WHEN ld_dt >= @strFromDate and ld_documentType = 'R' THEN ld_amount ELSE 0 END), 0) < 0 THEN 'Cr' 
  WHEN ISNULL(SUM(CASE WHEN ld_dt >= @strFromDate and ld_documentType = 'R' 
  THEN ld_amount ELSE 0 END), 0) > 0 THEN 'Dr' ELSE '' END as [Payment Received] , 
  CONVERT(VARCHAR, cast( isNull((sum(Case when ld_dt < @strFromDate then 0 else Case when (ld_documentType = 'B' Or ld_documentType = 'R') 
  then 0 else ld_amount end end)),0) as decimal(15,2))) + ' ' + CASE WHEN ISNULL(SUM(CASE WHEN ld_dt < @strFromDate 
  THEN 0 ELSE CASE WHEN (ld_documentType = 'B' OR ld_documentType = 'R') THEN 0 ELSE ld_amount END END), 0) < 0 THEN 'Cr' 
  WHEN ISNULL(SUM(CASE WHEN ld_dt < @strFromDate THEN 0 ELSE CASE WHEN (ld_documentType = 'B' 
  OR ld_documentType = 'R') THEN 0 ELSE ld_amount END END), 0) > 0 THEN 'Dr' ELSE '' 
  END as [Other Adjustments]  ,
  CONVERT(VARCHAR, cast( isNull((sum(Case when ld_dt >= @strFromDate and ld_documentType = 'B' 
  then ld_amount else 0 end)),0) as decimal(15,2))) as [Current Bill] ,
  CAST(ISNULL(SUM(CASE WHEN ld_dt < @strFromDate THEN ld_amount ELSE 0 END), 0) + 
        ISNULL(SUM(CASE WHEN ld_dt >= @strFromDate AND ld_documentType = 'R' THEN ld_amount ELSE 0 END), 0) + 
        ISNULL(SUM(CASE WHEN ld_dt < @strFromDate THEN 0 ELSE 
        CASE WHEN (ld_documentType = 'B' OR ld_documentType = 'R') THEN 0 ELSE ld_amount END END), 0) +  
        ISNULL(SUM(CASE WHEN ld_dt >= @strFromDate AND ld_documentType = 'B' THEN ld_amount ELSE 0 END), 0)
		AS DECIMAL(15,2)) AS [Balance]
  FROM Ledger with (nolock) , @tbl_UserList X    
  WHERE ld_clientcd = X.ClientCode
  AND ld_dt <=@strToDate
  GROUP BY ClientCode
  
END
GO

CREATE PROCEDURE stpr_FetchEntryData @dsXml XML
WITH ENCRYPTION
AS
BEGIN
	DECLARE @tbl_InputJSONTable DBO.tb_ParamList;
	DECLARE @o_ParameterList VARCHAR(max) = '', @o_ParameterListxml XML;
	DECLARE @strXMl VARCHAR(MAX) = CAST(@dsXml AS VARCHAR(MAX))

	EXEC SP_ParameterXMLRep @dsXml, @o_ParameterList OUTPUT

	IF ISNULL(@o_ParameterList, '') <> ''
	BEGIN
		SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)

		INSERT INTO @tbl_InputJSONTable (ParameterName, ParameterValue, HeaderName, Jsontag)
		SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code, Parameter.value(
				'(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue, Parameter.value('(MasterTag)[1]', 
				'VARCHAR(MAX)') AS MasterTag, Parameter.value('(JsonLevel)[1]', 'VARCHAR(MAX)') AS JsonLevel
		FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
	END

	DECLARE @strModuleName VARCHAR(100) = '', @strString NVARCHAR(MAX), @strOption VARCHAR(100) = '', @strUserid VARCHAR(50)='',
	@strSelection VARCHAR(50)='', @strCode VARCHAR(50)='', @strFromDate VARCHAR(8)='', @strToDate VARCHAR(8)='', 
	@strFIntRefNo VARCHAR(50)='', @strFPledgerId VARCHAR(20)='', @strSlipStatus VARCHAR(20)=''

	SELECT @strModuleName = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName = 'ActionName'

	SELECT @strOption = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName = 'Option'
	
	SELECT @strSelection = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName = 'Selection'
	
	SELECT @strCode = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName = 'Code'
		
	SELECT @strUserid = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName = 'Userid'
	
	SELECT @strFromDate = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName = 'FromDate'
	
	SELECT @strToDate = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName = 'ToDate'
	
	SELECT @strFIntRefNo = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName IN('IntRefNo','DRFNo')
	
	
	SELECT @strFPledgerId = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName IN('PledgerId')
	
		
	SELECT @strSlipStatus = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName = 'SlipStatus'
	
		
	IF @strModuleName = 'CROSSNET' 
	BEGIN
	  SET @strModuleName = @strOption
	  SET @strOption = 'FIND'
	END
	
	SELECT TOP 1 @strUserid = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName = 'UserID'
   

	IF @strOption = 'Find'
	BEGIN
		DECLARE @TargetTableName VARCHAR(100) = (
				SELECT TOP 1 TableName
				FROM tbl_GenericTemplateDefinition(NOLOCK)
				WHERE TemplateName = @strModuleName AND ParentTemplateCode = 'Entry'
				)
		DECLARE @Sql NVARCHAR(MAX) = N'SELECT DISTINCT ';
		DECLARE @Delimiter NVARCHAR(2) = ', ';
		--DECLARE @DefaultXml XML = (select DefaultDetailXML from tbl_InsertUpdateConfig where ModuleName = @strModuleName);
		DECLARE @DefaultXml XML = (
				SELECT DefaultMasterXML
				FROM tbl_InsertUpdateConfig
				WHERE ModuleName = @strModuleName
				);
		-- Fetch the relevant column definitions for the specified table
		DECLARE @ColumnDefinitions TABLE (ColumnName NVARCHAR(128), TagName NVARCHAR(128));

		INSERT INTO @ColumnDefinitions (ColumnName, TagName)
		SELECT FieldName, TagName
		FROM tbl_GenericTemplateDefinition(NOLOCK)
		WHERE TableName = @TargetTableName AND TemplateName = @strModuleName and DisplayLendingPage = 'Y'
		AND ParentTemplateCode = 'Entry'
		ORDER BY OrderBy;

		-- Build the dynamic SELECT statement
		DECLARE @ColumnName NVARCHAR(128);
		DECLARE @TagName NVARCHAR(128);

		DECLARE ColumnCursor CURSOR
		FOR
		SELECT ColumnName, TagName
		FROM @ColumnDefinitions;

		OPEN ColumnCursor;

		FETCH NEXT
		FROM ColumnCursor
		INTO @ColumnName, @TagName;

		WHILE @@FETCH_STATUS = 0
		BEGIN
          IF  @ColumnName = 'tb_reasfortrade'
		  BEGIN
 		    SET @Sql = @Sql + 'CASE WHEN ISNULL(tb_NFiller2,0) = 0 THEN LTRIM(RTRIM(tb_reasfortrade)) ELSE CAST(tb_NFiller2 AS VARCHAR)+''~''+CAST(LTRIM(RTRIM(tb_reasfortrade)) AS VARCHAR) END AS Reason '+@Delimiter
		  END
		  ELSE IF  @ColumnName = 'id_reasfortrade'
		  BEGIN
 		    SET @Sql = @Sql + 'CASE WHEN ISNULL(id_NFiller2,0) = 0 THEN LTRIM(RTRIM(id_reasfortrade)) ELSE CAST(id_NFiller2 AS VARCHAR)+''~''+CAST(LTRIM(RTRIM(id_reasfortrade)) AS VARCHAR) END AS Reason '+@Delimiter
		  END
		  ELSE
          BEGIN  		  
			SET @Sql = @Sql + QUOTENAME(@ColumnName) + ' AS ' + QUOTENAME(@TagName) + @Delimiter;
		  END	
			FETCH NEXT
			FROM ColumnCursor
			INTO @ColumnName, @TagName;
		END;

		CLOSE ColumnCursor;

		DEALLOCATE ColumnCursor;
		IF UPPER(@strModuleName) in('OffMarketEntry','onMarketEntry','EARLYPAYIN')
		BEGIN
		   SET @Sql = @Sql +' CASE WHEN tb_trx_allow IN(''N'',''R'') THEN ''false'' else ''true'' end as isUpdated, '
		   +' CASE WHEN tb_trx_allow IN(''N'',''R'') THEN ''false'' else ''true'' end as isDeleted' +@Delimiter; 
        END   
		ELSE IF UPPER(@strModuleName) in('InterDepository')
		BEGIN
		   SET @Sql = @Sql +' CASE WHEN id_allow  IN(''N'',''R'') THEN ''false'' else ''true'' end as isUpdated, '
		   +' CASE WHEN id_allow IN(''N'',''R'') THEN ''false'' else ''true'' end as isDeleted' +@Delimiter; 
        END   
		ELSE IF UPPER(@strModuleName) in('DEMATENTRY')
		BEGIN
		   SET @Sql = @Sql +' CASE WHEN dm_trx_allow IN(''N'',''R'') THEN ''false'' else ''true'' end as isUpdated, '
		   +' CASE WHEN dm_trx_allow IN(''N'',''R'') THEN ''false'' else ''true'' end as isDeleted' +@Delimiter; 
        END   
		ELSE IF UPPER(@strModuleName) in('PLEDGESETUP')
		BEGIN
		   SET @Sql = @Sql +' CASE WHEN pl_trx_allow IN(''N'',''R'') THEN ''false'' else ''true'' end as isUpdated, '
		   +' CASE WHEN pl_trx_allow IN(''N'',''R'') THEN ''false'' else ''true'' end as isDeleted' +@Delimiter; 
        END   

		ELSE IF UPPER(@strModuleName) in('Receipts','Payments')
		  BEGIN
			Declare @Cond nvarchar(max)=''
			   Set @Cond =   'Case when rc_receiptdt < (Select TOP 1 sp_sysvalue From Sysparameter where sp_parmcd = ''ACFROMDT'') Then  ''false'' 
				 When rc_receiptdt > (Select TOP 1 sp_sysvalue From Sysparameter where sp_parmcd = ''ACTODT'') Then  ''false''
				 When rc_receiptdt < (Select TOP 1 sp_sysvalue From Sysparameter where sp_parmcd = ''LOCKDATA'') Then  ''false''
				 When rc_common = ''CONTRA''  Then  ''false''
				 When rc_cleareddt <> ''''  Then  ''false'' Else '''' End'
			   SET @Sql = Replace(@Sql,'[rc_amount]','Abs(rc_amount)')

			   SET @Sql = @Sql +' CASE WHEN rc_status IN(''N'',''Y'', '''') and Isnull(rc_authdt1,'''')<>'''' THEN ''false'' When '+@Cond+' <> '''' Then ''true'' else ''false'' end as isUpdated, '
				  +' CASE WHEN rc_status IN(''N'',''Y'', '''') and Isnull(rc_authdt1,'''')<>'''' THEN ''false'' When '+@Cond+' <> '''' Then ''true'' else ''false'' end as isDeleted' +@Delimiter; 
			END   
		ELSE IF UPPER(@strModuleName) in('Journals')
		  BEGIN
			Declare @CondJr nvarchar(max)=''
			   Set @CondJr =   'Case when jr_dt < (Select TOP 1 sp_sysvalue From Sysparameter where sp_parmcd = ''ACFROMDT'') Then  ''false'' 
				 When jr_dt > (Select TOP 1 sp_sysvalue From Sysparameter where sp_parmcd = ''ACTODT'') Then  ''false''
				 When jr_dt < (Select TOP 1 sp_sysvalue From Sysparameter where sp_parmcd = ''LOCKDATA'') Then  ''false''  Else '''' End'
			
			   SET @Sql = @Sql +' CASE WHEN jr_status IN(''N'',''Y'', '''') and Isnull(jr_authdt1,'''')<>'''' THEN ''false'' When '+@CondJr+' <> '''' Then ''true'' else ''false'' end as isUpdated, '
				  +' CASE WHEN jr_status IN(''N'',''Y'', '''') and Isnull(jr_authdt1,'''')<>'''' THEN ''false'' When '+@CondJr+' <> '''' Then ''true'' else ''false'' end as isDeleted' +@Delimiter; 
			END 
		-- Remove the trailing delimiter
		SET @Sql = LEFT(@Sql, LEN(@Sql) - LEN(@Delimiter));
		-- Append the table name
		SET @Sql = @Sql + ' FROM ' + QUOTENAME(@TargetTableName) + 'WHERE 1=1';

		DECLARE @SQL1 NVARCHAR(MAX) = '';
		DECLARE @param NVARCHAR(100), @value NVARCHAR(100);
        
		DECLARE @Cursor CURSOR;
			-- Create a cursor to loop through each element in the XML
			SET @Cursor = CURSOR
		FOR
		SELECT
			--x.value('local-name(.)', 'NVARCHAR(100)') AS ParamName,
			--(SELECT FieldName FROM tbl_fieldtagmapping WHERE TableName = @TargetTableName and TagName= x.value('local-name(.)', 'NVARCHAR(100)')) AS ParamName,
			(
				SELECT FieldName
				FROM tbl_GenericTemplateDefinition
				WHERE TableName = @TargetTableName AND TemplateName = @strModuleName AND TagName = x.value(
						'local-name(.)', 'NVARCHAR(100)')
				) AS ParamName, x.value('(text())[1]', 'NVARCHAR(100)') AS ParamValue
		FROM @dsXml.nodes('/dsXml/X_Filter/*') AS T(x)
		
		UNION ALL
		
		SELECT y.value('local-name(.)', 'NVARCHAR(100)') AS ParamName, y.value('(text())[1]', 'NVARCHAR(100)') 
			AS ParamValue
		FROM @DefaultXml.nodes('/DefaultXml/*') AS U(y);

		PRINT @param

		OPEN @Cursor;

		FETCH NEXT
		FROM @Cursor
		INTO @param, @value;

		-- Loop through each element and build the WHERE clause
		WHILE @@FETCH_STATUS = 0
		BEGIN
			IF @param IS NOT NULL AND @value IS NOT NULL
			BEGIN
			  SET @SQL1 = @SQL1 + ' AND ' + QUOTENAME(@param) + ' = ' + QUOTENAME(@value, '''');
			END

			FETCH NEXT
			FROM @Cursor
			INTO @param, @value;
		END

		CLOSE @Cursor;

		DEALLOCATE @Cursor;
		
		--DECLARE @tbl_UserList dbo.UserAccessList; 
        --INSERT INTO @tbl_UserList EXEC dbo.stpr_GetClientAccessListNew @strUserid , @strSelection, @strCode 
		DECLARE @strHeaderString VARCHAR(MAX)=''
		SET @strHeaderString = 'DECLARE @tbl_UserList dbo.UserAccessList; '
        +' INSERT INTO @tbl_UserList '
        +' EXEC dbo.stpr_GetClientAccessListNew '''+@strUserid+''' , '''+@strSelection+''', '''+@strCode+''' '
  
		
		IF UPPER(@strModuleName) in('OffMarketEntry','onMarketEntry','EARLYPAYIN')
		BEGIN
		  SET @SQL1 = @SQL1 + ' AND tb_client_id in(select ClientCode from @tbl_UserList) '
		  IF ISNULL(@strFromDate,'') <> ''
		  BEGIN
		     SET @SQL1 = @SQL1 + ' AND tb_trx_date >= '''+@strFromDate+''' AND  tb_trx_date <= '''+ISNULL(@strtoDate,@strFromDate)+''' '
		  END
		  IF ISNULL(@strFIntRefNo,'') <> ''
		  BEGIN
		    SET @SQL1 = @SQL1 + ' AND tb_internal_refno = '''+@strFIntRefNo+''' '
		  END
		  IF ISNULL(@strSlipStatus,'') <> ''
		  BEGIN
		    SET @SQL1 = @SQL1 + ' AND ((ISNULL(tb_trx_allow,''N'') = ''N'' AND '''+@strSlipStatus+''' = ''Pending'')  '
			+' OR (ISNULL(tb_trx_allow,''N'') = ''R'' AND ISNULL(tb_authcode1,''N'') = ''R'' AND '''+@strSlipStatus+''' = ''Rejected'') '
			+' OR (ISNULL(tb_trx_allow,''N'') = ''Y'' AND '''+@strSlipStatus+''' = ''Authorise'') '
			+' OR (ISNULL(tb_trx_allow,''N'') = ''N'' AND ISNULL(tb_authcode1,''N'') = ''Y'' AND tb_status IN(''10'',''01'') AND '''+@strSlipStatus+''' = ''UnAuthorized'') )'
		  END
		END
		
		IF UPPER(@strModuleName) in('DematEntry')
		BEGIN
		  SET @SQL1 = @SQL1 + ' AND dm_client_id in(select ClientCode from @tbl_UserList) '
		  IF ISNULL(@strFromDate,'') <> ''
		  BEGIN
		     SET @SQL1 = @SQL1 + ' AND dm_dmat_date >= '''+@strFromDate+''' AND  dm_dmat_date <= '''+ISNULL(@strtoDate,@strFromDate)+''' '
		  END
		  IF ISNULL(@strFIntRefNo,'') <> ''
		  BEGIN
		    SET @SQL1 = @SQL1 + ' AND dm_irn = '''+@strFIntRefNo+''' '
		  END
  		  
		  IF ISNULL(@strSlipStatus,'') <> ''
		  BEGIN
		    SET @SQL1 = @SQL1 + ' AND ((ISNULL(dm_trx_allow,''N'') = ''N'' AND '''+@strSlipStatus+''' = ''Pending'')  '
			+' OR (ISNULL(dm_trx_allow,''N'') = ''R'' AND ISNULL(dm_authcode2,''N'') = ''R'' AND '''+@strSlipStatus+''' = ''Rejected'') '
			+' OR (ISNULL(dm_trx_allow,''N'') = ''Y'' AND '''+@strSlipStatus+''' = ''Authorise'') '
			+' OR (ISNULL(dm_trx_allow,''N'') = ''N'' AND ISNULL(dm_authcode2,''N'') = ''Y'' AND dm_status IN(''10'',''01'') AND '''+@strSlipStatus+''' = ''UnAuthorized'') )'
		  END
		END
		
		IF UPPER(@strModuleName) in('InterDepository')
		BEGIN
		  SET @SQL1 = @SQL1 + ' AND id_clientid in(select ClientCode from @tbl_UserList) '
		  IF ISNULL(@strFromDate,'') <> ''
		  BEGIN
		     SET @SQL1 = @SQL1 + ' AND id_trx_date >= '''+@strFromDate+''' AND  id_trx_date <= '''+ISNULL(@strtoDate,@strFromDate)+''' '
		  END
		  IF ISNULL(@strFIntRefNo,'') <> ''
		  BEGIN
		    SET @SQL1 = @SQL1 + ' AND id_internalrefno = '''+@strFIntRefNo+''' '
		  END
		  
		  IF ISNULL(@strSlipStatus,'') <> ''
		  BEGIN
		    SET @SQL1 = @SQL1 + ' AND ((ISNULL(id_allow,''N'') = ''N'' AND '''+@strSlipStatus+''' = ''Pending'')  '
			+' OR (ISNULL(id_allow,''N'') = ''R'' AND ISNULL(id_authcode1,''N'') = ''R'' AND '''+@strSlipStatus+''' = ''Rejected'') '
			+' OR (ISNULL(id_allow,''N'') = ''Y'' AND '''+@strSlipStatus+''' = ''Authorise'') '
			+' OR (ISNULL(id_allow,''N'') = ''N'' AND ISNULL(id_authcode1,''N'') = ''Y'' AND id_status IN(''10'',''01'') AND '''+@strSlipStatus+''' = ''UnAuthorized'') )'
		  END
		END
		
		IF UPPER(@strModuleName) in('PLEDGESETUP')
		BEGIN
		  SET @SQL1 = @SQL1 + ' AND pl_client_id in(select ClientCode from @tbl_UserList) '
		  IF ISNULL(@strFromDate,'') <> ''
		  BEGIN
		     SET @SQL1 = @SQL1 + ' AND pl_trx_date >= '''+@strFromDate+''' AND  pl_trx_date <= '''+ISNULL(@strtoDate,@strFromDate)+''' '
		  END
		  IF ISNULL(@strFIntRefNo,'') <> ''
		  BEGIN
		    SET @SQL1 = @SQL1 + ' AND pl_irn = '''+@strFIntRefNo+''' '
		  END
		  
		  IF ISNULL(@strFPledgerId,'') <> ''
		  BEGIN
		    SET @SQL1 = @SQL1 + ' AND pl_client_id = '''+@strFPledgerId+''' '
		  END
		  
		  IF ISNULL(@strSlipStatus,'') <> ''
		  BEGIN
		    SET @SQL1 = @SQL1 + ' AND ((ISNULL(pl_trx_allow,''N'') = ''N'' AND '''+@strSlipStatus+''' = ''Pending'')  '
			+' OR (ISNULL(pl_trx_allow,''N'') = ''R'' AND ISNULL(pl_authid1,''N'') = ''R'' AND '''+@strSlipStatus+''' = ''Rejected'') '
			+' OR (ISNULL(pl_trx_allow,''N'') = ''Y'' AND '''+@strSlipStatus+''' = ''Authorise'') '
			+' OR (ISNULL(pl_trx_allow,''N'') = ''N'' AND ISNULL(pl_authid1,''N'') = ''Y'' AND pl_status IN(''10'',''01'') AND '''+@strSlipStatus+''' = ''UnAuthorized'') )'
		  END
		END
		
		IF UPPER(@strModuleName) in('Receipts','Payments')
		BEGIN
		  SET @SQL1 = @SQL1 + ' AND rc_clientcd in(select ClientCode from @tbl_UserList) '
		  IF ISNULL(@strFromDate,'') <> ''
		  BEGIN
		     SET @SQL1 = @SQL1 + ' AND rc_receiptdt >= '''+@strFromDate+''' AND  rc_receiptdt <= '''+ISNULL(@strtoDate,@strFromDate)+''' '
		  END
		  IF ISNULL(@strFIntRefNo,'') <> ''
		  BEGIN
		    SET @SQL1 = @SQL1 + ' AND rc_voucherno = '''+@strFIntRefNo+''' '
		  END
		END

	  IF UPPER(@strModuleName) in('Journals')
		BEGIN
		  SET @SQL1 = @SQL1 + ' AND jr_clientcd in(select ClientCode from @tbl_UserList) '
		  IF ISNULL(@strFromDate,'') <> ''
		  BEGIN
		     SET @SQL1 = @SQL1 + ' AND jr_dt >= '''+@strFromDate+''' AND  jr_dt <= '''+ISNULL(@strtoDate,@strFromDate)+''' '
		  END
		  IF ISNULL(@strFIntRefNo,'') <> ''
		  BEGIN
		    SET @SQL1 = @SQL1 + ' AND jr_voucherno = '''+@strFIntRefNo+''' '
		  END
		END

		SET @Sql = @strHeaderString +' '+ @Sql + @SQL1
		--Select @Sql

		IF UPPER(@strModuleName) NOT IN('Slipissue','Authorise','ClientOffLine','ClientAuthorise','ClientMaster')
		BEGIN
		  EXEC sp_executesql @Sql;
		END
       
		ELSE IF UPPER(@strModuleName) = 'Slipissue'
        BEGIN
		  DECLARE @strSlipBOid VARCHAR(20), @strSlipNo INT, @strSlipInstrument VARCHAR(10)=''
		  
		  SELECT @strSlipBOid = ParameterValue
	      FROM @tbl_InputJSONTable
	      WHERE ParameterName = 'BOID'
		  
		  SELECT @strSlipNo = ParameterValue
	      FROM @tbl_InputJSONTable
	      WHERE ParameterName = 'SlipNo'
		  
		  SELECT @strSlipInstrument = ParameterValue
	      FROM @tbl_InputJSONTable
	      WHERE ParameterName = 'InstrumentType'
		  
	  
		  SELECT ISNULL(chm_cmcd,'') AS ClientCode, ISNULL(cm_name,'') AS BOName, 
		  chm_instcd as InstrumentType, chm_chqno as SlipNo, LTRIM(RTRIM(ISNULL(chm_refno,''))) as RefNo, ISNULL(chm_chqno,0) + ISNULL(chm_booksize,'') - 1 as TONo ,
          LTRIM(RTRIM(ISNULL(chm_refdate,''))) as RefDate, ISNULL(chm_lotno,0) as NoofBooks, LTRIM(RTRIM(ISNULL(chm_issuedate,''))) as [Date], 
          ISNULL(cm_city,'') as [City], ISNULL(cm_state,'') as [State], ISNULL(cm_pin,'') as [PIN],
		  ISNULL(cm_chgsscheme,'') as [Scheme], [Type] = (CASE WHEN ISNULL(chm_cmcd,'') LIKE '22%' THEN 'POA' ELSE 'CLIENT' END)
          FROM Chequemaster(NOLOCK) LEFT OUTER JOIN CLIENT_MASTER CM ON(chm_cmcd = CM.CM_cD)
          WHERE --ISNULL(chm_cmcd,'') = ISNULL(@strSlipBOid,'') AND 
		  chm_instcd = @strSlipInstrument
		  AND chm_chqno = @strSlipNo
        END  
		ELSE IF UPPER(@strModuleName) = 'Authorise'
		BEGIN
		  declare @strFTrxnType VARCHAR(MAX)=''
		  SELECT @strFTrxnType = ParameterValue
	      FROM @tbl_InputJSONTable
	      WHERE ParameterName = 'TransactionType'
		  EXEC stpr_GetInstructionAuthrizeNew @strFTrxnType, 'P', @strUserid
		END
		ELSE IF UPPER(@strModuleName) IN('ClientOffLine','ClientAuthorise','ClientMaster')
		BEGIN
		  
		  DECLARE @strMulAccountType VARCHAR(3)='', @StrMulBranch VARCHAR(20)='',
		  @StrMulClientPANNo VARCHAR(20)='', @StrMulStatus VARCHAR(20)='', @StrMulBOID VARCHAR(16)=''
		  
		  SELECT @strMulAccountType = ParameterValue
	      FROM @tbl_InputJSONTable
	      WHERE ParameterName = 'AccountType'
		  
		  SELECT @StrMulBranch = ParameterValue
	      FROM @tbl_InputJSONTable
	      WHERE ParameterName = 'Branch'
		  
		  SELECT @StrMulStatus = ParameterValue
	      FROM @tbl_InputJSONTable
	      WHERE ParameterName = 'Status'
		  
		  SELECT @StrMulClientPANNo = ParameterValue
	      FROM @tbl_InputJSONTable
	      WHERE ParameterName = 'ClientPANNo'
		  
		  SELECT @StrMulBOID = ParameterValue
	      FROM @tbl_InputJSONTable
	      WHERE ParameterName = 'BOID'
		  
		  IF @strModuleName = 'ClientOffLine'
		  BEGIN
		    SELECT [InternalRefNo] =  InternalRefNo, [Branch] = BranchCode,
		    [Branch Name] = bm_branchname,
		    [Account Type] = ISNULL((Select cs_desc as [DisplayName]  FROM Clientsub_master(NOLOCK) where cs_module = 'CS09'  
		    and cs_code  = AccountType),''),
		    [ClientFormNo] = ClientFormNo, [ClientPANNo] = clientpanno, 
		    [Entry Date] = CONVERT(VARCHAR,ENTRYDATE,106), [Entry Time] = EntryTime, 
		    Status = CASE WHEN EntryStep = 1 THEN 'Incomplete' 
			WHEN EntryStep = 2 AND Entrystatus = 'Y' THEN 'Sumbited' 
			WHEN Entrystatus = 'A' THEN 'Approved' 
			WHEN Entrystatus = 'R' THEN 'Rejected' else 'Pending' end,
		    @strModuleName AS EntryName,
		    CASE WHEN EntryStep = 1 THEN 'false' else 'true' end as isUpdated, 
		    CASE WHEN EntryStep = 1 THEN 'false' else 'true' end as isDeleted
		    FROM tbl_MultiEntryIncompleteMaster(NOLOCK) X, Branch_master(NOLOCK)
		    WHERE X.BranchCode = bm_branchcd
		    AND ((X.AccountType = @strMulAccountType AND ISNULL(@strMulAccountType,'') <> '')
			OR ISNULL(@strMulAccountType,'') = '')
			AND ((bm_branchcd = @StrMulBranch AND ISNULL(@StrMulBranch,'') <> '')
			OR ISNULL(@StrMulBranch,'') = '')
			AND ((ClientPanNo = @StrMulClientPANNo AND ISNULL(@StrMulClientPANNo,'') <> '')
			OR ISNULL(@StrMulClientPANNo,'') = '')
		  END	
		  ELSE IF @strModuleName = 'ClientAuthorise'
		  BEGIN
		    SELECT [InternalRefNo] =  InternalRefNo, [Branch] = BranchCode,
		    [BranchName] = bm_branchname,
		    [AccountType] = ISNULL((Select cs_desc as [DisplayName]  FROM Clientsub_master(NOLOCK) where cs_module = 'CS09'  
		    and cs_code  = AccountType),''),
		    [ClientFormNo] = ClientFormNo, [ClientPANNo] = clientpanno, 
		    [EntryDate] = CONVERT(VARCHAR,ENTRYDATE,106), [EntryTime] = EntryTime, 
		    Status = CASE WHEN EntryStep = 1 THEN 'Incomplete' 
			WHEN EntryStep = 2 AND Entrystatus = 'Y' THEN 'Sumbited' 
			WHEN Entrystatus = 'A' THEN 'Approved' 
			WHEN Entrystatus = 'R' THEN 'Rejected' else 'Pending' end,
			EntryType AS EntryName,
			FormType = 'multientry','' AS DocumentType, '' AS NewStatus, Remarks AS Reason
		    FROM tbl_MultiEntryIncompleteMaster(NOLOCK) X, Branch_master(NOLOCK)
		    WHERE X.BranchCode = bm_branchcd
		    AND ((EntryStep = 1 AND @StrMulStatus = 'T' and Entrystatus = 'N') OR (EntryStep = 2 AND @StrMulStatus = 'P' and Entrystatus = 'Y')
			 OR (EntryStep = 2 AND Entrystatus ='R' AND @StrMulStatus = 'R')
			 OR (EntryStep = 2 AND Entrystatus ='A' AND @StrMulStatus = 'A'))
		  END
		  ELSE IF @strModuleName = 'ClientMaster'
		  BEGIN
	         SELECT [Branch] = bm_branchcd,
		    [BranchName] = bm_branchname, 
			 AccountType = ISNULL((SELECT cs_desc FROM Clientsub_master WHERE  cs_module = 'CS09' and cs_code=cm_productcd),''),
			CM_CD as BOID, CM_name as ClientName , cb_panno AS ClientPANNo, cm_opendate as OpenDate, 
		    cm_mobile as MobileNo, cm_active = CASE WHEN  cm_active ='01' THEN 'Active'
            ELSE 'NonActive' end,
			'false'   as isUpdated, 'true' as isDeleted
            FROM CLIENT_MASTER(NOLOCK), Client_Backoffice(NOLOCK), Branch_master(NOLOCK)
            where cm_Cd = cb_cmcd
            and cm_schedule = 49843750 
			AND cm_brboffcode = bm_branchcd
			AND ((cm_productcd = @strMulAccountType AND ISNULL(@strMulAccountType,'') <> '')
			OR ISNULL(@strMulAccountType,'') = '')
			AND ((bm_branchcd = @StrMulBranch AND ISNULL(@StrMulBranch,'') <> '')
			OR ISNULL(@StrMulBranch,'') = '')
			AND ((cb_panno = @StrMulClientPANNo AND ISNULL(@StrMulClientPANNo,'') <> '')
			OR ISNULL(@StrMulClientPANNo,'') = '')
			AND ((CM_cD = @StrMulBOID AND ISNULL(@StrMulBOID,'') <> '')
			OR ISNULL(@StrMulBOID,'') = '')
		  END
		END
		
		DECLARE @StrShowFilter1 VARCHAR(MAX)='', @StrShowFilter2 VARCHAR(MAX)='', @StrShowFilter3 VARCHAR(MAX)=''
        SELECT TOP 1 @StrShowFilter1 = ShowFilter1, @StrShowFilter2 = ShowFilter2, @StrShowFilter3 = ShowFilter3
        FROM tbl_CrossNetMenu(NOLOCK) WHERE  MenuTag = @strModuleName
      
        IF ISNULL(@StrShowFilter1,'') = ''
          SET @StrShowFilter1 = ''
	  
        IF ISNULL(@StrShowFilter2,'') = ''
          SET @StrShowFilter2 = ''
	  
        IF ISNULL(@StrShowFilter3,'') = ''
          SET @StrShowFilter3 = ''
	  
        SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<SELECTION>>',ISNULL(@strSelection,''))
        SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<CODE>>',ISNULL(@StrCode,''))
		SET @StrShowFilter1 = REPLACE(@StrShowFilter1,'<<SlipStatus>>',ISNULL(@strSlipStatus,''))
  
        SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<SELECTION>>',ISNULL(@strSelection,''))
        SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<CODE>>',ISNULL(@StrCode,''))
		SET @StrShowFilter2 = REPLACE(@StrShowFilter2,'<<SlipStatus>>',ISNULL(@strSlipStatus,''))
        
		DECLARE @XMLStringFilter VARCHAR(MAX)=''

        SELECT @XMLStringFilter = '<XmlData>
						<Headings>'
        IF ISNULL(@StrShowFilter1,'') <> ''
        BEGIN
	      SET @XMLStringFilter = @XMLStringFilter+'<Heading>'+@StrShowFilter1+'</Heading>'				
        END                        
        IF ISNULL(@StrShowFilter2,'') <> ''
        BEGIN
	      SET @XMLStringFilter = @XMLStringFilter+'<Heading>'+@StrShowFilter2+'</Heading>'				
        END                        
		
        IF ISNULL(@StrShowFilter3,'') <> ''
        BEGIN
	      SET @XMLStringFilter = @XMLStringFilter+'<Heading>'+@StrShowFilter3+'</Heading>'				
        END                        
        SET @XMLStringFilter = @XMLStringFilter+'</Headings></XmlData>' 
        SELECT @XMLStringFilter AS [Settings]
		RETURN 1;
	END
	ELSE IF @strOption = 'Delete'
	BEGIN
		DECLARE @TargetTableName1 VARCHAR(100) = (
				SELECT TOP 1 TableName
				FROM tbl_GenericTemplateDefinition
				WHERE TemplateName = @strModuleName
				)
		DECLARE @strAutoGenColumnName VARCHAR(100) = (
				SELECT DetailAutoGenColumnName
				FROM tbl_InsertUpdateConfig
				WHERE ModuleName = @strModuleName
				)
		DECLARE @Sql2 NVARCHAR(MAX) = N'Delete from ' + @TargetTableName1;
		DECLARE @SQLWhere NVARCHAR(MAX) = '';
		DECLARE @strPriKeyValue INT;
		DECLARE @param1 NVARCHAR(100), @value1 NVARCHAR(100);

		DECLARE @Cursor1 CURSOR;DECLARE @tb_pri_key_found BIT = 0;SET @Cursor1 = CURSOR
		FOR
		SELECT (
				SELECT FieldName
				FROM tbl_GenericTemplateDefinition
				WHERE TableName = @TargetTableName1 AND TagName = x.value('local-name(.)', 'NVARCHAR(100)') AND 
					TemplateName = @strModuleName
				) AS ParamName, x.value('(text())[1]', 'NVARCHAR(100)') AS ParamValue
		FROM @dsXml.nodes('/dsXml/X_Filter/*') AS T(x)

		PRINT @param1

		OPEN @Cursor1;

		FETCH NEXT
		FROM @Cursor1
		INTO @param1, @value1;

		-- Loop through each element and build the WHERE clause
		WHILE @@FETCH_STATUS = 0
		BEGIN
			IF @param1 IS NOT NULL AND @value1 IS NOT NULL
			BEGIN
				IF @param1 = @strAutoGenColumnName
				BEGIN
					SET @tb_pri_key_found = 1;
					SET @strPriKeyValue = @value1;
				END

				SET @SQLWhere = @SQLWhere + ' AND ' + QUOTENAME(@param1) + ' = ' + QUOTENAME(@value1, '''');
			END

			FETCH NEXT
			FROM @Cursor1
			INTO @param1, @value1;
		END

		IF @tb_pri_key_found = 0
		BEGIN
			PRINT 'tb_pri_key was NOT found in the parameters.';

			RETURN 1;
		END

		CLOSE @Cursor1;

		DEALLOCATE @Cursor1;

		SET @SQLWhere = SUBSTRING(@SQLWhere, 5, LEN(@SQLWhere) - 4)

		PRINT (@SQL2 + ' WHERE ' + @SQLWhere)

		IF @strModuleName = 'OffMarketEntry'
		BEGIN
			INSERT INTO Backoffice_delete (
				bd_instcd, bd_trx_type, bd_trx_date, bd_client_id, bd_isin, bd_qty, bd_market_type, bd_settlement, 
				bd_exchangeid, bd_chid, bd_execution_date, bd_other_dp_id, bd_other_client_id, bd_other_cmbp_id, 
				bd_internal_refno, bd_other_markettype, bd_other_settle_no, bd_status, bd_trx_allow, bd_pri_key, 
				bd_authcode1, bd_authuserid1, bd_authdt1, bd_authcode2, bd_authuserid2, bd_authdt2, bd_branchcd, 
				bd_branchbatchcd, bd_cash, bd_reasonfortrade, bd_authcode3, bd_authuserid3, bd_authdt3, 
				bd_authremarks, bd_scrollno, bd_obligationid, bd_serialno, bd_tradeid, bd_br_code, bd_br_pri_key, 
				bd_computername, mkrtm, mkrid, mkrdt, mkrtmold, mkridold, mkrdtold, bd_authtm1, bd_authtm2, 
				bd_authtm3, bd_totalcertificate
				) (
				SELECT tb_instcd, tb_trx_type, tb_trx_date, tb_client_id, tb_isin, tb_qty, isnull(tb_market_type, ''
				), IsNull(tb_settlement, ''), '' tb_exchangeid, '' tb_chid, tb_exec_date, IsNull(tb_other_dp_id, ''), 
				IsNull(tb_other_client_id, ''), '' tb_other_cmbp_id, tb_internal_refno, '' tb_other_markettype, '' 
				tb_other_settle_no, tb_status, tb_trx_allow, tb_pri_key, IsNull(tb_authcode1, ''), IsNull(
					tb_authuserid1, ''), IsNull(tb_authdt1, ''), IsNull(tb_authcode2, ''), IsNull(tb_authuserid2, '')
				, IsNull(tb_authdt2, ''), tb_branchcd, IsNull(tb_branchbatchcd, ''), '' tb_cash, '' tb_reasonfortrade
				, IsNull(tb_authcode3, ''), IsNull(tb_authuserid3, ''), IsNull(tb_authdt3, ''), IsNull(
					tb_authremarks, ''), IsNull(tb_scrollno, 0), 0 tb_obligationid, 0 tb_serialno, '' tb_tradeid, isnull
				(tb_br_code, ''), isnull(tb_br_pri_key, ''), '', convert(CHAR, getdate(), 108), 'UserID', convert(
					CHAR, getdate(), 112), '', '', '', isnull(tb_authtm1, ''), isnull(tb_authtm2, ''), isnull(tb_authtm3
					, ''), 0 FROM Trxbackoffice WHERE tb_pri_key = @strPriKeyValue
				)

			DECLARE @intUnique INT = (
					SELECT max(bd_unique)
					FROM Backoffice_delete
					)

			UPDATE Backoffice_audit
			SET ba_delunique = @intUnique
			WHERE ba_pri_key = @intUnique AND ba_branchcd = '000000' AND ba_trx_type = '904'

			DECLARE @strInstructionType VARCHAR(50) = @dsXml.value('(/dsXml/X_Filter/InstrumentType)[1]', 
					'NVARCHAR(50)')
			DECLARE @strIntRefNo VARCHAR(50) = @dsXml.value('(/dsXml/X_Filter/IntRefNo)[1]', 'NVARCHAR(50)')

			IF NOT EXISTS (
					SELECT 1
					FROM Trxbackoffice
					WHERE tb_trx_type = '904' AND tb_instcd = @strInstructionType AND tb_internal_refno = @strIntRefNo
					)
			BEGIN
				DELETE
				FROM used_slip
				WHERE us_trxtype = '904' AND us_instcd = @strInstructionType AND us_irn = @strIntRefNo

				DELETE
				FROM Other_charges
				WHERE oc_billno = 'TB' + @strInstructionType + @strIntRefNo AND oc_chargecode = '43';
			END
		END
		RETURN 1;
	END
	ELSE IF @strOption = 'display'
	BEGIN
		DECLARE @strIntRefNo1 VARCHAR(50) = @dsXml.value('(/dsXml/X_Filter/IntRefNo)[1]', 'VARCHAR(50)')
		DECLARE @strInstType1 VARCHAR(50) = @dsXml.value('(/dsXml/X_Filter/InstrumentType)[1]', 'VARCHAR(50)')
		DECLARE @rc_srno VARCHAR(50) = @dsXml.value('(/dsXml/X_Filter/SerialNo)[1]', 'VARCHAR(50)')
		DECLARE @rc_ClientCode VARCHAR(50) = @dsXml.value('(/dsXml/X_Filter/ClientCode)[1]', 'VARCHAR(50)')
		DECLARE @rc_receiptdt VARCHAR(50) = @dsXml.value('(/dsXml/X_Filter/ReceiptDate)[1]', 'VARCHAR(50)')
		
		
		IF UPPER(@strModuleName) = 'OFFMARKETENTRY'
		BEGIN
			SELECT TOP 1 tb_instcd AS 'InstrumentType', tb_trx_date AS 'TransactionDate', tb_internal_refno AS 
				'IntRefNo', tb_client_id AS 'BOID', tb_exec_date AS 'ExecutionDate', '' AS 'oc_amt', 
				tb_instreceivemode AS 'ReceiveMode', '' AS 'ChildFormData'
			FROM Trxbackoffice
			WHERE tb_internal_refno = @strIntRefNo1 AND tb_instcd = @strInstType1

			SELECT tb_pri_key AS 'SerialNo', tb_other_client_id AS 'CounterBOID', tb_other_dp_id AS 'DP', tb_PaymentMode 
				AS 'PaymentMode', LTRIM(RTRIM(tb_isin)) AS 'ISIN', LTRIM(RTRIM(tb_PayeeName)) AS 'PayeeName', LTRIM(
					RTRIM(tb_other_settle_no)) AS 'FromSettNo', LTRIM(RTRIM(tb_qty)) AS 'Qty', LTRIM(RTRIM(
						tb_ChequeNo)) AS 'ChqRefNo', LTRIM(RTRIM(tb_settlement)) AS 'CounterSett', LTRIM(RTRIM(
						tb_Paymentdate)) AS 'DateofIssue', LTRIM(RTRIM(tb_remark)) AS 'Remark', 
						CASE WHEN ISNULL(tb_NFiller2,0) = 0 THEN LTRIM(RTRIM(tb_reasfortrade)) ELSE CAST(tb_NFiller2 AS VARCHAR)+'~'+CAST(LTRIM(RTRIM(tb_reasfortrade)) AS VARCHAR) END 
						AS 'Reason', LTRIM(RTRIM(tb_BankActNo)) AS 'BankAcNo', LTRIM(RTRIM(tb_consideration)) AS 'Consideration', 
						tb_NFiller1 AS 'StampDuty', LTRIM(RTRIM(tb_Filler1)) AS 
				'PaidBy', LTRIM(RTRIM(tb_Bankname)) AS 'BankName', LTRIM(RTRIM(tb_UCCEXid)) AS 'Exchange', LTRIM(RTRIM(tb_SegmentID)) AS 'Segment',
				LTRIM(RTRIM(tb_UCC)) AS 'UCC', LTRIM(RTRIM(tb_BankBranch)) AS 
				'BranchName', LTRIM(RTRIM(tb_UCCCmid)) AS 'CMID', LTRIM(RTRIM(tb_EntityIden)) AS 'EntryBy', LTRIM(RTRIM(tb_UCCTMCPCode)) AS 'TMID',
				[Status] = isnull((select sx_description as [Value] FROM Statusof_trx(NOLOCK) where sx_code = tb_status and sx_trxtype = tb_trx_type),'') 
			FROM Trxbackoffice
			WHERE tb_internal_refno = @strIntRefNo1 AND tb_instcd = @strInstType1
		END
		ELSE IF UPPER(@strModuleName) = 'INTERDEPOSITORY'
		BEGIN
			SELECT TOP 1 id_instcd AS 'InstrumentType', id_trxdate AS 'TransactionDate', id_internalrefno AS 
				'IntRefNo', id_clientid AS 'BOID', id_exec_date AS 'ExecutionDate', '' AS 'oc_amt', id_instreceivemode 
				AS 'ReceiveMode', '' AS 'ChildFormData'
			FROM Interdepository(NOLOCK)
			WHERE id_internalrefno = @strIntRefNo1 AND id_instcd = @strInstType1

			SELECT id_pri_key AS 'SerialNo', id_otherclientid AS 'CounterBOID', id_otherdpid AS 'DP', id_PaymentMode AS 
				'PaymentMode', LTRIM(RTRIM(id_isin)) AS 'ISIN', LTRIM(RTRIM(id_PayeeName)) AS 'PayeeName', LTRIM(
					RTRIM(id_other_settno)) AS 'FromSettNo', LTRIM(RTRIM(id_qty)) AS 'Qty', LTRIM(RTRIM(id_ChequeNo)
				) AS 'ChqRefNo', LTRIM(RTRIM(id_settlementno)) AS 'CounterSett', LTRIM(RTRIM(id_Paymentdate)) AS 
				'DateofIssue', LTRIM(RTRIM(id_remark)) AS 'Remark', 
				CASE WHEN ISNULL(id_NFiller2,0) = 0 THEN LTRIM(RTRIM(id_reasfortrade)) ELSE CAST(id_NFiller2 AS VARCHAR)+'~'+CAST(LTRIM(RTRIM(id_reasfortrade)) AS VARCHAR) END 
				AS 'Reason', 
				LTRIM(RTRIM(id_BankActNo)) AS 'BankAcNo', LTRIM(RTRIM(id_consideration)) AS 'Consideration', 
				id_NFiller1 AS 'StampDuty', LTRIM(RTRIM(id_Filler1)) AS 'PaidBy', LTRIM(RTRIM(id_Bankname)) AS 
				'BankName', LTRIM(RTRIM(id_UCCEXid)) AS 'Exchange', LTRIM(RTRIM(id_SegmentID)) AS 'Segment', LTRIM(
					RTRIM(id_UCC)) AS 'UCC', LTRIM(RTRIM(id_BankBranch)) AS 'BranchName', LTRIM(RTRIM(id_UCCCmid)) AS 
				'CMID', LTRIM(RTRIM(id_EntityIden)) AS 'EntryBy', LTRIM(RTRIM(id_UCCTMCPCode)) AS 'TMID',
				[Cuspa_accno] = id_CuspaDPID, [Cuspa_client_id] = id_CuspaClientID , [EarlyPayin] = id_EarlyPayIden,
				[Status] = isnull((select sx_description as [Value] FROM Statusof_trx(NOLOCK) where sx_code = '01' and sx_trxtype = id_trxtype),'') 
			FROM Interdepository(NOLOCK)
			WHERE id_internalrefno = @strIntRefNo1 AND id_instcd = @strInstType1
		END
		ELSE IF UPPER(@strModuleName) = 'ONMARKETENTRY'
		BEGIN
			SELECT TOP 1 tb_instcd AS 'InstrumentType', tb_trx_date AS 'TransactionDate', tb_internal_refno AS 
				'IntRefNo', tb_client_id AS 'BOID', tb_exec_date AS 'ExecutionDate', '' AS 'oc_amt', 
				tb_instreceivemode AS 'ReceiveMode', '' AS 'ChildFormData'
			FROM Trxbackoffice
			WHERE tb_internal_refno = @strIntRefNo1 AND tb_instcd = @strInstType1

			SELECT tb_pri_key AS 'SerialNo', tb_other_client_id AS 'CounterBOID', tb_other_dp_id AS 'DP', tb_PaymentMode 
				AS 'PaymentMode', LTRIM(RTRIM(tb_isin)) AS 'ISIN', LTRIM(RTRIM(tb_PayeeName)) AS 'PayeeName', LTRIM(
					RTRIM(tb_other_settle_no)) AS 'FromSettNo', LTRIM(RTRIM(tb_qty)) AS 'Qty', LTRIM(RTRIM(
						tb_ChequeNo)) AS 'ChqRefNo', LTRIM(RTRIM(tb_settlement)) AS 'CounterSett', LTRIM(RTRIM(
						tb_Paymentdate)) AS 'DateofIssue', LTRIM(RTRIM(tb_remark)) AS 'Remark', LTRIM(RTRIM(tb_obligationid)) AS 'ObligNo', 
						CASE WHEN ISNULL(tb_NFiller2,0) = 0 THEN LTRIM(RTRIM(tb_reasfortrade)) ELSE CAST(tb_NFiller2 AS VARCHAR)+'~'+CAST(LTRIM(RTRIM(tb_reasfortrade)) AS VARCHAR) END 
						AS 'Reason', LTRIM(RTRIM(tb_BankActNo)) AS 'BankAcNo', LTRIM(RTRIM(
						tb_consideration)) AS 'Consideration', tb_NFiller1 AS 'StampDuty', LTRIM(RTRIM(tb_Filler1)) AS 
				'PaidBy', LTRIM(RTRIM(tb_Bankname)) AS 'BankName', LTRIM(RTRIM(tb_UCCEXid)) AS 'Exchange', LTRIM(
					RTRIM(tb_SegmentID)) AS 'Segment', LTRIM(RTRIM(tb_UCC)) AS 'UCC', LTRIM(RTRIM(tb_BankBranch)) AS 
				'BranchName', LTRIM(RTRIM(tb_UCCCmid)) AS 'CMID_drop', LTRIM(RTRIM(tb_EntityIden)) AS 'EntryBy', LTRIM(
					RTRIM(tb_UCCTMCPCode)) AS 'TMID', LTRIM(RTRIM(tb_serialno)) AS 'SerialNoKey',
			[Status] = isnull((select sx_description as [Value] FROM Statusof_trx(NOLOCK) where sx_code = tb_status and sx_trxtype = tb_trx_type),'')  		
			FROM Trxbackoffice(NOLOCK)
			WHERE tb_internal_refno = @strIntRefNo1 AND tb_instcd = @strInstType1
		END
		ELSE IF UPPER(@strModuleName) = 'EARLYPAYIN'
		BEGIN
			SELECT TOP 1 tb_instcd AS 'InstrumentType', tb_trx_date AS 'TransactionDate', tb_internal_refno AS 
				'IntRefNo', tb_client_id AS 'BOID', tb_exec_date AS 'ExecutionDate', '' AS 'oc_amt', 
				tb_instreceivemode AS 'ReceiveMode', '' AS 'ChildFormData'
			FROM Trxbackoffice
			WHERE tb_internal_refno = @strIntRefNo1 AND tb_instcd = @strInstType1

			SELECT tb_pri_key AS 'SerialNo', tb_other_client_id AS 'CounterBOID', tb_other_dp_id AS 'DP', tb_PaymentMode 
				AS 'PaymentMode', LTRIM(RTRIM(tb_isin)) AS 'ISIN', LTRIM(RTRIM(tb_PayeeName)) AS 'PayeeName', LTRIM(
					RTRIM(tb_other_settle_no)) AS 'FromSettNo', LTRIM(RTRIM(tb_qty)) AS 'Qty', LTRIM(RTRIM(
						tb_ChequeNo)) AS 'ChqRefNo', LTRIM(RTRIM(tb_settlement)) AS 'CounterSett', LTRIM(RTRIM(
						tb_Paymentdate)) AS 'DateofIssue', LTRIM(RTRIM(tb_remark)) AS 'Remark', LTRIM(RTRIM(
						tb_reasfortrade)) AS 'Reason', LTRIM(RTRIM(tb_BankActNo)) AS 'BankAcNo', LTRIM(RTRIM(
						tb_consideration)) AS 'Consideration', tb_NFiller1 AS 'StampDuty', LTRIM(RTRIM(tb_Filler1)) AS 
				'PaidBy', LTRIM(RTRIM(tb_Bankname)) AS 'BankName', LTRIM(RTRIM(tb_UCCEXid)) AS 'Exchange', LTRIM(
					RTRIM(tb_SegmentID)) AS 'Segment', LTRIM(RTRIM(tb_UCC)) AS 'UCC', LTRIM(RTRIM(tb_BankBranch)) AS 
				'BranchName', LTRIM(RTRIM(tb_other_cmbp_id)) AS 'CMID_drop', LTRIM(RTRIM(tb_EntityIden)) AS 'EntryBy', LTRIM(
					RTRIM(tb_UCCTMCPCode)) AS 'TMID', LTRIM(RTRIM(tb_UCCCmid)) AS 'CMID', --LTRIM(RTRIM(tb_UCCCmid)) AS 'CMID_drop',
			[Status] = isnull((select sx_description as [Value] FROM Statusof_trx(NOLOCK) where sx_code = tb_status and sx_trxtype = tb_trx_type),'') 
			FROM Trxbackoffice(NOLOCK)
			WHERE tb_internal_refno = @strIntRefNo1 AND tb_instcd = @strInstType1
		END
		ELSE IF UPPER(@strModuleName) = 'Receipts'
		BEGIN
		  SELECT SerialNo = rc_srno, VoucherNo = rc_voucherno, ClientCode = rc_clientcd, ReceiptDate = rc_receiptdt,
          Amount = rc_amount, Narration = rc_particular, BankClientCode = rc_bankclientcd, ChequeNo = rc_chequeno, MICR = rc_micr, ClearedDate = RTRIM(LTRIM(rc_cleareddt))
          FROM Receipts(NOLOCK)
          WHERE  rc_srno = @rc_srno and rc_clientcd = @rc_ClientCode and rc_receiptdt = @rc_receiptdt
		END
		ELSE IF UPPER(@strModuleName) = 'Payments'
		BEGIN
		  SELECT SerialNo = rc_srno, VoucherNo = rc_voucherno, ClientCode = rc_clientcd, ReceiptDate = rc_receiptdt,
       Amount = rc_amount, Narration = rc_particular, BankClientCode = rc_bankclientcd, ChequeNo = rc_chequeno, MICR = rc_micr, ClearedDate = RTRIM(LTRIM(rc_cleareddt))
          FROM Receipts(NOLOCK)
          WHERE  rc_srno = @rc_srno and rc_clientcd = @rc_ClientCode and rc_receiptdt = @rc_receiptdt
		END
		ELSE IF UPPER(@strModuleName) = 'Journals'
		BEGIN
		  SET @rc_srno = @dsXml.value('(/dsXml/X_Filter/SerialNo)[1]', 'VARCHAR(50)')
		  SET @rc_ClientCode = @dsXml.value('(/dsXml/X_Filter/ClientCode)[1]', 'VARCHAR(50)')
		  SET @rc_receiptdt = @dsXml.value('(/dsXml/X_Filter/JournalDate)[1]', 'VARCHAR(50)')
		
		
		  SELECT SerialNo = jr_srno, JournalDate = jr_dt, VoucherNo = jr_voucherno, Particular = jr_sparticular, '' AS 'ChildFormData'
		  FROM Journal(NOLOCK)
          WHERE  jr_srno = @rc_srno and jr_dt = @rc_receiptdt
		  --and jr_clientcd = @rc_ClientCode
		
		  SELECT ClientCode = jr_clientcd, Balance = jr_amount, ParticularB = jr_eparticular, DrCrFlag = jr_debitflag
          FROM Journal(NOLOCK)
          WHERE  jr_srno = @rc_srno and jr_dt = @rc_receiptdt
		  --and jr_clientcd = @rc_ClientCode
		END
		ELSE IF UPPER(@strModuleName) = 'PledgeSetup'
		BEGIN
		  DECLARE @pl_srno AS VARCHAR(50) = @dsXml.value('(/dsXml/X_Filter/SerialNo)[1]', 'VARCHAR(50)')
		
		  select [InstrumentType] = LTRIM(RTRIM(pl_instcd)), 
		  [IntRefNo] = LTRIM(RTRIM(pl_irn)), [Date] = LTRIM(RTRIM(pl_trx_date)), [Branch] = LTRIM(RTRIM(pl_branchcd)), 
		  [Status] = LTRIM(RTRIM(pl_status)), [PledgerId] = LTRIM(RTRIM(pl_client_id)), [AgreementNo] = LTRIM(RTRIM(pl_agreementno)),
		  [CDSLPSNNo] = LTRIM(RTRIM(pl_reference)), [ReceiveMode] = LTRIM(RTRIM(pl_instrecvmode)), [LotNo] = LTRIM(RTRIM(pl_lotno)), 
		  [ClientId] = LTRIM(RTRIM(pl_otherclientid)), [Exchange] = LTRIM(RTRIM(pl_Exchange)), [Segment] = LTRIM(RTRIM(pl_segment)), 
		  [UCC] = LTRIM(RTRIM(pl_UCC)), [CMID] = LTRIM(RTRIM(pl_cmid)),
		  [MarginPSN] = pl_Oreference, [CCID] = pl_ccid, [EntryBy] = pl_EntryBy, [TMID] = pl_tmid, [Remark] = pl_remarks, [ISIN] = pl_isin_code, 
		  [Qty] = pl_qty, [Value] = pl_value, [ExpiryDate] = pl_expirydt, [ExecDate] = pl_exec_date,
		  [PledgeType] = LTRIM(RTRIM(pl_PledgeType)), [Reason] = LTRIM(RTRIM(pl_NFiller1)), [SerialNo] = LTRIM(RTRIM(pl_pri_key)),
          [DpId] = 	LTRIM(RTRIM(SUBSTRING(pl_otherclientid,1,8))) ,
          [Dptype] = 	CASE WHEN LTRIM(RTRIM(SUBSTRING(pl_otherclientid,1,8))) IN(select sp_sysvalue from Sysparameter(NOLOCK) 
		   where sp_parmcd='DPID') THEN 'Intra' else 'Inter' end,
          [BalanceType] = 	LTRIM(RTRIM(pl_lockinflag)),
          [LockinID] = ltrim(rtrim(pl_lockinid))		  
		  FROM pledge(NOLOCK) where pl_pri_key = @pl_srno
		END
		ELSE IF UPPER(@strModuleName) = 'DematEntry'
		BEGIN
		  DECLARE @dm_instcd AS VARCHAR(50) = @dsXml.value('(/dsXml/X_Filter/InstrumentType)[1]', 'VARCHAR(50)')
		  --DECLARE @dm_pri_key AS VARCHAR(50) = @dsXml.value('(/dsXml/X_Filter/InwardNo)[1]', 'VARCHAR(50)')
		  DECLARE @dm_DRFNo AS VARCHAR(50) = @dsXml.value('(/dsXml/X_Filter/DRFNo)[1]', 'VARCHAR(50)')
		  DECLARE @dm_branchcd AS VARCHAR(50) = @dsXml.value('(/dsXml/X_Filter/Branch)[1]', 'VARCHAR(50)')
		
		  SELECT [InstrumentType] = dm_instcd, [Branch] = dm_branchcd, [Date] = dm_dmat_date, DRFNo = LTRIM(RTRIM(dm_irn)), DRNNo = dm_drn, 
		  InwardNo = dm_pri_key, [ClientId] = dm_client_id, 
		  [DispatchDate] = dm_dispatchdt, [ISIN] = dm_isin_code, [DocType] = dm_doctype, [DematQty] = dm_dematqty, TotalCertificates = dm_total_certificate,
          ReceiveMode = dm_instreceivemode, Remark = dm_remarks, [Status] = LTRIM(RTRIM(dm_status)), 
		  [DocType] = dm_doctype, [Locked] = LTRIM(RTRIM(dm_lockstatus)), [LockedDate] = LTRIM(RTRIM(isnull(dm_locked_date,''))), 
		  [LockedReason] = LTRIM(RTRIM(dm_locked_reason)),
		  '' AS 'ChildFormData'
          FROM Dematmaster(NOLOCK)
          --WHERE dm_instcd = @dm_instcd AND dm_pri_key = @dm_pri_key AND dm_branchcd = @dm_branchcd
		  WHERE dm_instcd = @dm_instcd AND dm_irn = @dm_DRFNo AND dm_branchcd = @dm_branchcd

		  SELECT [SerialNo] = dd_pri_key, [FolioNo] = LTRIM(RTRIM(dd_folio_no)), [CertificateNo] = dd_cert_from, 
		  [DistinNoFrom] = dd_dist_from, [CerticateQty] = dd_qty, [DistinNoTo] = dd_dist_to 
          from Dematdetail WITH (NOLOCK)
          WHERE dd_irn = @dm_DRFNo and dd_instcd = @dm_instcd and dd_branchcd= @dm_branchcd order by dd_pri_key
		END
		ELSE IF UPPER(@strModuleName) = 'SlipIssue'
		BEGIN
		  DECLARE @strDlipBOid VARCHAR(20), @strDSlipNo INT, @strDSlipInstrument VARCHAR(10)=''
		  
		  --SET @strDlipBOid = @dsXml.value('(/dsXml/X_Filter/ClientCode)[1]', 'VARCHAR(50)')
		  SET @strDSlipNo = @dsXml.value('(/dsXml/X_Filter/SlipNo)[1]', 'INT')
		  SET @strDSlipInstrument = @dsXml.value('(/dsXml/X_Filter/InstrumentType)[1]', 'VARCHAR(50)')
		  
		  SELECT ISNULL(chm_cmcd,'') AS ClientCode, ISNULL(cm_name,'') AS BOName, 
		  chm_instcd as InstrumentType, chm_chqno as SlipNo, LTRIM(RTRIM(ISNULL(chm_refno,''))) as RefNo, ISNULL(chm_chqno,0) + ISNULL(chm_booksize,'') - 1 as TONo ,
          LTRIM(RTRIM(ISNULL(chm_refdate,''))) as RefDate, ISNULL(chm_lotno,0) as NoofBooks, LTRIM(RTRIM(ISNULL(chm_issuedate,''))) as [Date], 
          ISNULL(cm_city,'') as [City], ISNULL(cm_state,'') as [State], ISNULL(cm_pin,'') as [PIN],
		  ISNULL(cm_chgsscheme,'') as [Scheme], Leaves = ISNULL(chm_booksize,0),
		  [Type] = (CASE WHEN ISNULL(chm_cmcd,'') LIKE '22%' THEN 'POA' ELSE 'CLIENT' END),
		  '' AS 'ChildFormData'
          FROM Chequemaster(NOLOCK) LEFT OUTER JOIN CLIENT_MASTER CM ON(chm_cmcd = CM.CM_cD)
          WHERE --ISNULL(chm_cmcd,'') = ISNULL(@strDlipBOid,'') AND 
		  chm_instcd = @strDSlipInstrument
		  AND chm_chqno = @strDSlipNo
		END  
		RETURN
	END
END
GO

CREATE PROCEDURE stpr_GenericInsertUpdateTable @xmlInput VARCHAR(MAX), @strTableName VARCHAR(100), @i_vcmode VARCHAR(10),
@i_vcUserCode VARCHAR(50), @strPrimaryKeyName VARCHAR(200), @o_iReturnMainSlNo INT OUTPUT,   
  @o_vcFlag VARCHAR(1) OUTPUT,   
  @o_vcMessage VARCHAR(1000) OUTPUT 
WITH ENCRYPTION  
AS
BEGIN

  DECLARE @XMLDATA1 XML = CAST(@xmlInput AS XML)
  DECLARE @tbl_InputJSONTable DBO.tb_ParamList ;
  
  DELETE FROM  @tbl_InputJSONTable 
  
  INSERT INTO @tbl_InputJSONTable (ParameterName,  ParameterValue, HeaderName) 
  SELECT DATA1.value('(ParameterName)[1]', 'VARCHAR(MAX)') AS Client_Code ,
  LTRIM(RTRIM(ISNULL(DATA1.value('(ParameterValue)[1]', 'VARCHAR(MAX)'),''))) AS ColumnValue,
  ISNULL(DATA1.value('(HeaderName)[1]', 'VARCHAR(MAX)'),'') AS MasterTag
  FROM @XMLDATA1.nodes('/DATA1') AS XTbl(DATA1)
  
  IF EXISTS(SELECT 1 FROM @tbl_InputJSONTable WHERE ParameterName = 'IsInserted' AND ParameterValue = 'true')
  BEGIN
    SET @i_vcmode = 'add'
  END

  DECLARE @iPrimaryTableColumnExists INT = 0,    
   @iColumnID INT = 0, 
   @strColumnDataType VARCHAR(20), @iColumnDataLength INT = 0, @strMainColumnvalue NVARCHAR(MAX),    
   @strInsertStringValue NVARCHAR(MAX) = '', @strInsertStringHeader NVARCHAR(MAX) = '',     
   @strUpdateStringValue NVARCHAR(MAX)='', @strMainString NVARCHAR(MAX) ='',    
   @iReturnSerialNo INT = 0, @strIdentityColumnName VARCHAR(50)='',  
   @i_vcColumnList NVARCHAR(MAX) = '',   
   @i_vcColumnValueList NVARCHAR(MAX) = '', @strColumnName NVARCHAR(MAX) = '', 
   @strColumnValue NVARCHAR(MAX) = '',  @strTempColumnName NVARCHAR(MAX) = '', @strTempColumnValue NVARCHAR(MAX) = ''
  
  IF @strTableName <> ''
  BEGIN
    DECLARE @tbl_PrimaryKey TABLE(KeyName VARCHAR(100), KeyValue VARCHAR(100))
	
	    
	IF @i_vcmode <> 'add' and @i_vcmode <> ''
	BEGIN
	  --SELECT @strPrimaryKeyName
      INSERT INTO @tbl_PrimaryKey(KeyName)
	  SELECT VALUE FROM DBO.RETURNTABLE(@strPrimaryKeyName,'|')
	  	 
      UPDATE A SET A.KeyValue = B.ParameterValue
      FROM @tbl_PrimaryKey A, @tbl_InputJSONTable B
      WHERE A.KeyName = B.ParameterName	
      
	  DECLARE @strwhere VARCHAR(MAX)=''
      SELECT @strwhere = @strwhere+' '+ ' AND '+KeyName+' = '''+CAST(KeyValue AS VARCHAR)+''''
      FROM  @tbl_PrimaryKey		 

	  DELETE FROM @tbl_InputJSONTable WHERE ParameterName in(select KeyName from @tbl_PrimaryKey)
	  
	  IF ISNULL(@strPrimaryKeyName,'') = '' OR ISNULL(@strwhere,'') = ''
	  BEGIN
	    SET @o_vcFlag = 'F'  
        SET @o_vcMessage = '<Message>'+'Unique Key not Define in Master Table'
		RETURN 1
	  END

    END
 
    DECLARE CURSOR_BIND CURSOR FOR  
    SELECT ParameterName, ParameterValue = LTRIM(RTRIM(IIF(ParameterValue='',' ',ParameterValue)))   
    FROM @tbl_InputJSONTable
	
  
    OPEN CURSOR_BIND   
    FETCH NEXT FROM CURSOR_BIND INTO @strTempColumnName, @strTempColumnValue  
    WHILE @@FETCH_STATUS = 0   
    BEGIN  
      IF @strColumnName <> ''  
      BEGIN  
        SET @strColumnName = @strColumnName+'|'+@strTempColumnName  
        SET @strColumnValue = @strColumnValue+'|'+@strTempColumnValue  
      END  
      ELSE IF @strColumnName = ''  
      BEGIN  
        SET @strColumnName = @strTempColumnName  
        SET @strColumnValue = @strTempColumnValue  
      END  
      FETCH NEXT FROM CURSOR_BIND INTO @strTempColumnName, @strTempColumnValue  
    END   
    CLOSE CURSOR_BIND   
    DEALLOCATE CURSOR_BIND   

    DECLARE @sColumn_Name VARCHAR(100)='', @sdata_type VARCHAR(20)=''
	DECLARE Cur31Main
    CURSOR FOR SELECT Column_Name, data_type FROM INFORMATION_SCHEMA.COLUMNS 
	WHERE TABLE_NAME = @strTableName
	AND column_name NOT IN(SELECT ParameterName FROM @tbl_InputJSONTable)
	AND column_name NOT IN(SELECT KeyName FROM @tbl_PrimaryKey)
	AND column_name NOT IN (select COLUMN_NAME from( SELECT COLUMN_NAME, 
    COLUMNPROPERTY(OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'IsIdentity') AS IsIdentity
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = @strTableName) x1 where IsIdentity =  1)

    OPEN Cur31Main 
    FETCH NEXT FROM Cur31Main INTO @sColumn_Name, @sdata_type  
    WHILE @@FETCH_STATUS = 0
    BEGIN 
      IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @strTableName 
	  and COLUMN_NAME = @sColumn_Name) 
      BEGIN
        SET @strColumnName = @strColumnName +'|'+@sColumn_Name
        SET @strColumnValue = @strColumnValue +'|'+CAST((CASE WHEN @sdata_type IN('int','money','numeric') THEN '0' ELSE '' END) AS VARCHAR)
      END
      FETCH NEXT FROM Cur31Main INTO @sColumn_Name, @sdata_type  
    END
    CLOSE Cur31Main
    DEALLOCATE Cur31Main
	
    IF @strColumnValue = ''  
    BEGIN  
      SET @o_vcFlag ='F'    
      SET @o_vcMessage = '<Message>'+@o_vcMessage+'</Message>'
      RETURN 1    
    END  
    
	SET @i_vcColumnList = @strColumnName   
    SET @i_vcColumnValueList = @strColumnValue    
	
	SET @o_vcFlag =''    
    SET @o_vcMessage =''    
    SET @strMainString = ''   
	
	/*
	DECLARE @strCompCode VARCHAR(1)='A', @strUsercode VARCHAR(10)=''
	SELECT @strCompCode = em_cd from Entity_Master with (nolock) 
	WHERE em_cd =(select min(em_cd) from Entity_master)
	
	SELECT TOP 1 @strUsercode = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Userid' 
	
	SET @i_vcColumnValueList = REPLACE(@i_vcColumnValueList,'##COMP##',@strCompCode)
	SET @i_vcColumnValueList = REPLACE(@i_vcColumnValueList,'##USERID##',@strUsercode)
	*/
	
	-------- VAIBHAV
	
	
	DECLARE CURSOR_TABLE_COLUMNS CURSOR FOR           
    SELECT  Position, ColumnName = Value FROM dbo.fn_SplitString(@i_vcColumnList,'|')     
    WHERE VALUE NOT IN ('UpdateTimeStamp', 'UserCode','mkrid','mkrdt')  
    ORDER BY Position    
  
    OPEN CURSOR_TABLE_COLUMNS     
    FETCH NEXT FROM CURSOR_TABLE_COLUMNS INTO @iColumnID, @strColumnName    
    WHILE @@FETCH_STATUS = 0   
    BEGIN     
      SET @strColumnDataType = ''    
      SET @iColumnDataLength = ''    
      SET @strMainColumnvalue = ''    
      SET @iPrimaryTableColumnExists= 0    
         
      SELECT @strMainColumnValue = VALUE FROM dbo.fn_SplitString(@i_vcColumnValueList,'|')   
      WHERE Position = @iColumnID   
    
      IF @iColumnID > 0    
      BEGIN    
        SELECT @iPrimaryTableColumnExists = 1, @strColumnDataType = DATA_TYPE,   
        @iColumnDataLength = CHARACTER_MAXIMUM_LENGTH  
        FROM INFORMATION_SCHEMA.COLUMNS   
        WHERE TABLE_NAME = @strTableName    
        AND COLUMN_NAME = @strColumnName    
    
        IF @strColumnDataType <> 'VARBINARY'  
        BEGIN  
          IF @iPrimaryTableColumnExists > 0  
          BEGIN  
            SET @strInsertStringHeader = @strInsertStringHeader + @strColumnName+','  
          END  
        END  
        
		--Checking the Column Data Type For VARCHAR/NVARCHAR/CHAR,   
      
        IF @strColumnDataType IN ('VARCHAR', 'CHAR')   
        BEGIN   
          IF LEN(@strMainColumnvalue) > @iColumnDataLength AND @iColumnDataLength <> -1   
          BEGIN  
            SET @o_vcFlag = 'F'  
            SET @o_vcMessage = '<Message>'+'Column: '+@strColumnName+' Data Length is more than the Table Column Length'+'</Message>'  
            CLOSE CURSOR_TABLE_COLUMNS      
            DEALLOCATE CURSOR_TABLE_COLUMNS     
            RETURN 1    
          END    
          IF @iPrimaryTableColumnExists > 0    
          BEGIN    
            SET @strInsertStringValue = @strInsertStringValue     
            + ''''+@strMainColumnvalue+''''+','    
            SET @strUpdateStringValue = @strUpdateStringValue      
            + @strColumnName+' = '+''''+@strMainColumnvalue+''''+','    
          END    
        END       
        ELSE    
        --Checking the Column Data Type For DATETIME/DATE,   
        -- then Preparing the Insert/Update String accordingly  
        IF @strColumnDataType IN ('DATETIME', 'DATE')    
        BEGIN    
          IF @strMainColumnvalue IN ('','0')    
          BEGIN    
            SET @strMainColumnvalue = '01-JAN-1900' 
		  END    
          IF ISDATE(@strMainColumnvalue) = 0 --If it Returns 1- Valid Date , 0 - invalid date    
          BEGIN    
            SET @o_vcFlag ='F'    
            SET @o_vcMessage = '<Message>'+'Column: '+ @strColumnName+' Value ' +      
           @strMainColumnvalue+ ' Is Not a Valid Date.'+'</Message>'  
            CLOSE CURSOR_TABLE_COLUMNS      
            DEALLOCATE CURSOR_TABLE_COLUMNS     
            RETURN 1    
          END    
          IF @iPrimaryTableColumnExists > 0    
          BEGIN   
         SET @strInsertStringValue =  @strInsertStringValue     
              + ''''+@strMainColumnvalue+''''+','    
            SET @strUpdateStringValue = @strUpdateStringValue      
            + @strColumnName+' = '+''''+@strMainColumnvalue+''''+','    
          END    
        END     
        ELSE  
        --Checking the Column Data Type Other than   
        -- VARBINARY/VARCHAR/CHAR/DATETIME/DATE   
        -- then Preparing the Insert/Update String accordingly  
        IF @strColumnDataType NOT IN ('VARBINARY', 'VARCHAR', 'CHAR', 'DATETIME', 'DATE')   
        BEGIN    
          IF ISNULL(@strMainColumnvalue,'0') = ''     
          BEGIN    
            SET @strMainColumnvalue = '0'    
          END    
          IF @iPrimaryTableColumnExists > 0    
          BEGIN    
            SET @strInsertStringValue =  @strInsertStringValue + @strMainColumnvalue+','    
            SET @strUpdateStringValue = @strUpdateStringValue      
            + @strColumnName+' = '+ @strMainColumnvalue+','    
          END    
        END     
      END    
      FETCH NEXT FROM CURSOR_TABLE_COLUMNS INTO @iColumnID, @strColumnName    
    END      
    CLOSE CURSOR_TABLE_COLUMNS      
    DEALLOCATE CURSOR_TABLE_COLUMNS     
    SET @strInsertStringHeader = @strInsertStringHeader + ' mkrid, mkrdt '    
    SET @strInsertStringValue = @strInsertStringValue   
    +  ''''+@i_vcUserCode+''''+' , '+''''+CONVERT(VARCHAR,GETDATE(),112)+''''+' '    
    
    IF @i_vcmode = 'add'
	BEGIN
	  SET @strMainString = ''  
  
      SET @strMainString = 'INSERT INTO ' +@strTableName+'('  
      +@strInsertStringHeader +') VALUES ( '+@strInsertStringValue+' )'    
      BEGIN TRY  
	    --select @strMainString
        EXEC(@strMainString)     
        SELECT @iReturnSerialNo =  IDENT_CURRENT(@strTableName)    
		SET @o_iReturnMainSlNo = ISNULL(@iReturnSerialNo,0)
      END TRY    
      BEGIN CATCH    
        SET @o_vcFlag ='F'    
        SET @o_vcMessage ='<Message>'+'Insertion Failed: '+ERROR_MESSAGE()+'</Message>'
        RETURN 1    
      END CATCH    
	END
	ELSE IF @i_vcmode <> 'add' AND @i_vcmode <> ''
	BEGIN
	   IF @strPrimaryKeyName <> ''
	   BEGIN
	     IF EXISTS(SELECT 1 FROM @tbl_InputJSONTable WHERE ParameterName = 'IsDeleted' AND ParameterValue = 'true')
		 BEGIN
		   SET @strMainString = 'DELETE FROM '+@strTableName  + ' WHERE 1 = 1 '+@strwhere  
		   --SELECT @strMainString
		 END
		 ELSE
		 BEGIN
	       SET @strMainString = 'UPDATE '+@strTableName   
           +' SET '+@strUpdateStringValue+ ' mkrid= '''  
           + @i_vcUserCode+''', mkrdt = '''+CONVERT(VARCHAR,GETDATE(),112)+''' '    
           + ' WHERE 1 = 1 '+@strwhere    
		 END   
         BEGIN TRY  
		   --select @strMainString
		  --SELECT @strMainString, @strTableName, @strUpdateStringValue, @i_vcUserCode, @strwhere
           EXEC(@strMainString)     
           SELECT @iReturnSerialNo = 0    
         END TRY    
         BEGIN CATCH    
           SET @o_vcFlag ='F'    
           SET @o_vcMessage ='<Message>'+'UPDATE '+ERROR_MESSAGE()+'</Message>'    
           RETURN 1    
         END CATCH    
	   END   
       ELSE
       BEGIN
	     SET @o_vcFlag ='F'    
         SET @o_vcMessage ='<Message>'+'Primary key not define'+'</Message>'    
         RETURN 1    
       END	   
	END
	
	IF ISNULL(@o_iReturnMainSlNo,0)  = 0
	BEGIN
	
	  DECLARE @strMasterAutoGenColumnName VARCHAR(100)='' 
      SELECT top 1 @strMasterAutoGenColumnName = MasterAutoGenColumnName 
      FROM tbl_InsertUpdateConfig(NOLOCK)
      WHERE MasterTableName = @strTableName 
	  
	  IF ISNULL(@strMasterAutoGenColumnName,'') = ''
	  BEGIN
	    SELECT top 1 @strMasterAutoGenColumnName = DetailAutoGenColumnName
        FROM tbl_InsertUpdateConfig(NOLOCK)
        WHERE detailTableName = @strTableName
	  END	
	  
	  IF ISNULL(@strMasterAutoGenColumnName,'') = ''
	  BEGIN
	    SELECT top 1 @strMasterAutoGenColumnName = MasterAutoGenColumnName
        FROM tbl_InsertUpdateConfig(NOLOCK)
        WHERE detailTableName = @strTableName
	  END
	  
	  --SELECT @strMasterAutoGenColumnName, @strTableName
 	  SELECT @o_iReturnMainSlNo = ParameterValue 
	  FROM @tbl_InputJSONTable WHERE ParameterName = @strMasterAutoGenColumnName
	  
	END
	
	SET @o_vcFlag ='S'    
	IF @i_vcmode = 'add'
	BEGIN
      SET @o_vcMessage ='<Message>'+'Inserted into '+@strTableName+'</Message>'
	END
    ELSE
    BEGIN
	  SET @o_vcMessage ='<Message>'+'Update Data into '+@strTableName+'</Message>'
    END  	
    RETURN 1    
  END
  ELSE 
  BEGIN
   SET @o_vcFlag ='F'    
   SET @o_vcMessage ='<Message>'+'Table Name not found'+'</Message>'
   RETURN 1   
  END
END
GO

CREATE PROCEDURE stpr_GetFormAPIData @dsXml XML WITH ENCRYPTION AS
BEGIN
  DECLARE @o_ParameterList VARCHAR(max) = '', @o_ParameterListxml XML, @strOption VARCHAR(100)='';
  DECLARE @tbl_InputJSONTable DBO.tb_ParamList;
  DECLARE @tbl_InputJSONTable1 DBO.tb_ParamList;
  
  DECLARE @o_XML VARCHAR(MAX)=''
  
  EXEC SP_ParameterXMLRep @dsXml, @o_ParameterList OUTPUT
  IF ISNULL(@o_ParameterList, '') <> ''
  BEGIN
   SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)
   INSERT INTO @tbl_InputJSONTable (ParameterName, ParameterValue, HeaderName, Jsontag)
   SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code, REPLACE(Parameter.value('(ColumnValue)[1]', 'VARCHAR(MAX)'), 
    'null', '') AS ColumnValue, Parameter.value('(MasterTag)[1]', 'VARCHAR(MAX)') AS MasterTag, Parameter.value('(JsonLevel)[1]', 
    'VARCHAR(MAX)') AS JsonLevel
   FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
  END


  DECLARE @strUserid VARCHAR(500)='', @StrClientCode VARCHAR(50)='',
  @strHeaderString VARCHAR(MAX)='', @strxml VARCHAR(MAX)= CAST(@dsXml AS VARCHAR(MAX)), @strUsercode VARCHAR(20)='C'
  SELECT @strUserid = ParameterValue From @tbl_InputJSONTable where ParameterName = 'UserId'
  
  DECLARE @strModuleName  VARCHAR(100)=''
  SELECT @strModuleName = ParameterValue From @tbl_InputJSONTable where ParameterName = 'ActionName'
  
  SELECT @strOption = ParameterValue From @tbl_InputJSONTable where ParameterName = 'Option'
   
  IF @StrClientCode <> ''
  BEGIN
    SET @strUserid = @StrClientCode
  END
  
  SET @strHeaderString = 'DECLARE @tbl_UserList dbo.UserAccessList; '
  +' INSERT INTO @tbl_UserList '
  +' EXEC dbo.stpr_GetClientAccessList '''+@strUserid+''''
  
  DECLARE @strUniqueKeyValue VARCHAR(50) = '',@iCurrentSerialNo INT = 0, 
  @strCurrFieldName VARCHAR(100), @strCurTableName VARCHAR(100), @strCurFieldType  VARCHAR(50)='',
  @strString NVARCHAR(MAX)='', @StrGroupName VARCHAR(100)=''
    
  DECLARE @tbl_TemplateData TABLE(SerialNo INT IDENTITY(1,1), TemplateCode VARCHAR(13),               
  TemplateName VARCHAR(50), ParentTemplateCode VARCHAR(50), DisplayName VARCHAR(100), ControlType VARCHAR(50),               
  FieldValue VARCHAR(25), DisplayValue VARCHAR(25), FieldWidth INT,              
  FieldType VARCHAR(20),FieldSize INT, OrderBy INT, TableName VARCHAR(150), FieldName VARCHAR(50),               
  IsMandatory VARCHAR(1), AllowEdit  VARCHAR(1),               
  DependentFields VARCHAR(1000), GroupName VARCHAR(50), TagName VARCHAR(100), 
  FielddataValue VARCHAR(1000), DropDownAPI VARCHAR(MAX), ValidationAPI VARCHAR(MAX), TabName VARCHAR(MAX), GroupTagName VARCHAR(100), ColumnVisible VARCHAR(1))
  
  DECLARE @tbl_TemplateDataDetail TABLE(SerialNo INT IDENTITY(1,1), 
  FielddataValue VARCHAR(1000), 
  RefSerialNo INT)
  
  --QueryOutput xml, Rowid INT, UniqueKeyvalue VARCHAR(MAX), FieldNewValue VARCHAR(MAX) NOT NULL DEFAULT '', SubGroup VARCHAR(50))   

  DECLARE @tblQuery TABLE (FieldQuery VARCHAR(MAX))

  INSERT INTO @tbl_TemplateData(TemplateCode, TemplateName, ParentTemplateCode, DisplayName, ControlType, FieldValue, DisplayValue, FieldWidth, FieldType,
  FieldSize, OrderBy, TableName, FieldName, IsMandatory, AllowEdit, DependentFields, GroupName, TagName, TabName, GroupTagName, ColumnVisible)
  SELECT TemplateCode, TemplateName,ParentTemplateCode,DisplayName,ControlType,FieldValue,DisplayValue,FieldWidth,FieldType,FieldSize,
  OrderBy,TableName,FieldName,IsMandatory,AllowEdit,'',GroupName, TagName, UpdateBy , DependentFields, FieldList
  FROM  tbl_GenericTemplateDefinition(nolock) 
  WHERE TemplateName = @strModuleName
  ORDER BY TemplateName, CASE WHEN GROUPNAME = 'MASTER' THEN 1 ELSE 2 END, ORDERBY 
  
  DECLARE @MasterTableName VARCHAR(100), @MasterAutoGenColumnName  VARCHAR(100), 
  @MasterPrimaryKeyName  VARCHAR(100), @DetailTableName  VARCHAR(100), @DetailAutoGenColumnName  VARCHAR(100), @DetailPrimaryKeyName  VARCHAR(500),
  @ValidationSPName  VARCHAR(500), @DefaultMasterXML  VARCHAR(MAX), @DefaultDetailXML  VARCHAR(MAX), @DeleteMasterPrimaryKey  VARCHAR(100), 
  @DeleteDetailPrimaryKey  VARCHAR(100)
  
  SELECT @MasterTableName = MasterTableName, @MasterAutoGenColumnName = MasterAutoGenColumnName, 
  @MasterPrimaryKeyName = MasterPrimaryKeyName, @DetailTableName = DetailTableName, @DetailAutoGenColumnName = DetailAutoGenColumnName, 
  @DetailPrimaryKeyName = DetailPrimaryKeyName, @ValidationSPName = ValidationSPName, 
  @DefaultMasterXML = DefaultMasterXML, @DefaultDetailXML = DefaultDetailXML, @DeleteMasterPrimaryKey = DeleteMasterPrimaryKey, 
  @DeleteDetailPrimaryKey  = DeleteDetailPrimaryKey 
  FROM tbl_InsertUpdateConfig(NOLOCK) 
  WHERE ModuleName = @strModuleName
  
  DECLARE @tbl_PrimaryKey TABLE(TagName VARCHAR(100), KeyValue VARCHAR(100), KeyName VARCHAR(100))
  DECLARE @strwhere VARCHAR(MAX)='', @strDetailwhere VARCHAR(MAX)=''
  
  DECLARE @tbl_Insertdata TABLE(TagName VARCHAR(100), Rowid INT, KeyValue VARCHAR(100), KeyName VARCHAR(100))
  
  DECLARE @tblData TABLE (FielddataValue VARCHAR(1000))   
  
  DECLARE  @xmlResult XML;
  DECLARE @XML XML, @strrColumnName VARCHAR(MAX)=''
  DECLARE @DefaultXML XML
  
  IF @MasterTableName <> ''
  BEGIN
  
    DELETE FROM @tbl_PrimaryKey
    INSERT INTO @tbl_PrimaryKey(KeyName)
    SELECT VALUE FROM DBO.RETURNTABLE(@MasterPrimaryKeyName,'|')
  
    UPDATE A SET A.Tagname = B.TagName
    FROM @tbl_PrimaryKey A, @tbl_TemplateData B
    WHERE A.KeyName = B.FieldName
 
	  	 
    UPDATE A SET A.KeyValue = B.ParameterValue
    FROM @tbl_PrimaryKey A, @tbl_InputJSONTable B 
    WHERE A.Tagname = B.ParameterName
 
	
    SELECT @strwhere = @strwhere+' '+ ' AND '+KeyName+' = '''+CAST(KeyValue AS VARCHAR)+''''
    FROM @tbl_PrimaryKey	
	WHERE ISNULL(KeyValue,'') <> ''
    
    IF ISNULL(@strwhere,'') <> ''
	BEGIN
	  
	  SELECT TOP 1 @strrColumnName = COLUMN_NAME 
      FROM INFORMATION_SCHEMA.COLUMNS 
      WHERE TABLE_NAME = @MasterTableName
	  SET @strString = N'SELECT @xmlResult = (SELECT ROW_NUMBER() OVER (ORDER BY '+@strrColumnName+') AS RunningSerialNo, * FROM ' 
           + QUOTENAME(@MasterTableName) + 
          ' WHERE 1 = 1 '+@strwhere+' FOR XML PATH, ROOT(''GetData''))';
	

      EXEC sp_executesql @strString, N'@xmlResult XML OUTPUT', @xmlResult OUTPUT;
      SET @XML = @xmlResult;
      WITH ParsedXML AS (
      SELECT 
        T.Row.value('(RunningSerialNo/text())[1]', 'INT') AS RowID,
        T.Row.query('*') AS FullRow
      FROM @XML.nodes('/GetData/row') AS T(Row))
    
      INSERT INTO @tbl_Insertdata(TagName, Rowid, KeyValue, KeyName)   
      SELECT 'Master',
      P.RowID,
      X.Col.value('local-name(.)', 'NVARCHAR(100)') AS KeyName,
      X.Col.value('(text())[1]', 'NVARCHAR(MAX)') AS KeyValue
      FROM ParsedXML P
      CROSS APPLY P.FullRow.nodes('*') AS X(Col) 
      WHERE X.Col.value('local-name(.)', 'NVARCHAR(100)') <> 'RunningSerialNo'; -- Exclude RowID	
    END
  END
  
  IF ISNULL(@DetailTableName,'') <> ''
  BEGIN
 
  
    SET @strDetailwhere = ''

    DELETE FROM @tbl_PrimaryKey
    INSERT INTO @tbl_PrimaryKey(KeyName)
    SELECT VALUE FROM DBO.RETURNTABLE(@DetailPrimaryKeyName,'|')
	
	
    UPDATE A SET A.Tagname = B.TagName
    FROM @tbl_PrimaryKey A, @tbl_TemplateData B
    WHERE A.KeyName = B.FieldName
    
	
    UPDATE A SET A.KeyValue = B.ParameterValue
    FROM @tbl_PrimaryKey A, @tbl_InputJSONTable B 
    WHERE A.Tagname = B.ParameterName
    
    SELECT @strDetailwhere = @strDetailwhere+' '+ ' AND '+KeyName+' = '''+CAST(KeyValue AS VARCHAR)+''''
    FROM  @tbl_PrimaryKey	
    WHERE ISNULL(KeyValue,'') <> ''
	
	/*IF ISNULL(@strDetailwhere,'') = ''
	BEGIN
	  SET @o_XML = (SELECT ErrorFlag = 'E', ErrorMessage = 'Please Define Master Table Primay Key For'+@strModuleName FOR JSON PATH, ROOT('GetData'))
	  RETURN 1
	END
	*/
	IF ISNULL(@strDetailwhere,'') <> ''
	BEGIN
	  SET @strrColumnName = ''
	  SELECT TOP 1 @strrColumnName = COLUMN_NAME 
      FROM INFORMATION_SCHEMA.COLUMNS 
      WHERE TABLE_NAME = @DetailTableName
	
	  SET @strString = N'SELECT @xmlResult = (SELECT ROW_NUMBER() OVER (ORDER BY '+@strrColumnName+') AS RunningSerialNo, * FROM ' 
           + QUOTENAME(@DetailTableName) + 
          ' WHERE 1 = 1 '+@strDetailwhere+' FOR XML PATH, ROOT(''GetData''))';
      EXEC sp_executesql @strString, N'@xmlResult XML OUTPUT', @xmlResult OUTPUT;
	
	  SET @XML = @xmlResult;

      WITH ParsedXML AS (
      SELECT 
        T.Row.value('(RunningSerialNo/text())[1]', 'INT') AS RowID,
        T.Row.query('*') AS FullRow
      FROM @XML.nodes('/GetData/row') AS T(Row))
    
      INSERT INTO @tbl_Insertdata(TagName, Rowid, KeyValue, KeyName)   
      SELECT 'Detail',
      P.RowID,
      X.Col.value('local-name(.)', 'NVARCHAR(100)') AS KeyName,
      X.Col.value('(text())[1]', 'NVARCHAR(MAX)') AS KeyValue
      FROM ParsedXML P
      CROSS APPLY P.FullRow.nodes('*') AS X(Col) 
      WHERE X.Col.value('local-name(.)', 'NVARCHAR(100)') <> 'RunningSerialNo'; -- Exclude RowID		  
    END
  END
  
  DECLARE  @strFielddataValue1 VARCHAR(500)='', @strFieldQuery VARCHAR(MAX)='', @strFilters VARCHAR(MAX)=''
    
  DECLARE @StrDepValue VARCHAR(50)=''  , @strTagName VARCHAR(100)='', 
  @dbQueryType VARCHAR(10)='', @strLUsercode VARCHAR(20)=''
   
  DECLARE CURSOR_LENDINGPAGE_DropDown  CURSOR FOR                            
  Select SerialNo, TagName                              
  FROM @tbl_TemplateData                             
  ORDER BY SerialNo                            
      
  OPEN CURSOR_LENDINGPAGE_DropDown       
  FETCH NEXT FROM CURSOR_LENDINGPAGE_DropDown INTO @iCurrentSerialNo,  @strTagName
  WHILE @@FETCH_STATUS = 0                             
  BEGIN                          
                           
    SET @strFilters = ''
	SET @dbQueryType = ''
	SET @strLUsercode = ''
    
	SELECT @strFilters = Filters, @dbQueryType = DBQueryType  
    FROM tbl_InsertUpdateXMLDropDownQuery WHERE ModuleName = @strModuleName  
    AND CASE WHEN ColumnName = 'Received_drop' THEN 'BankClientCode' 
	ELSE ColumnName END = @strTagName
    
	
	
	IF @dbQueryType = 'Q'
	BEGIN
	  UPDATE A SET A.DropDownAPI = @strFilters
	  FROM @tbl_TemplateData A
	  WHERE SERIALNO = @iCurrentSerialNo
	END  
	ELSE IF @dbQueryType = 'P'
	BEGIN
	  UPDATE A SET A.ValidationAPI = @strFilters
	  FROM @tbl_TemplateData A
	  WHERE SERIALNO = @iCurrentSerialNo
	END  
	
	IF EXISTS(SELECT 1 FROM tbl_InsertUpdateXMLDropDownQuery(NOLOCK) 
	WHERE ModuleName = @strModuleName  AND dbQueryType='Q' AND DBQuery LIKE '%'+@strTagName+'%')
	BEGIN
	  
	  SELECT @strFilters = Filters FROM tbl_InsertUpdateXMLDropDownQuery(NOLOCK) 
	  WHERE ModuleName = @strModuleName  AND dbQueryType='Q' AND DBQuery LIKE '%'+@strTagName+'%'
	
	  UPDATE A SET A.ValidationAPI = @strFilters
	  FROM @tbl_TemplateData A
	  WHERE SERIALNO = @iCurrentSerialNo
	END
	
	
    FETCH NEXT FROM CURSOR_LENDINGPAGE_DropDown INTO  @iCurrentSerialNo, @strTagName       
  END            
  CLOSE CURSOR_LENDINGPAGE_DropDown                             
  DEALLOCATE CURSOR_LENDINGPAGE_DropDown    
  
     
  SET @o_XML = (SELECT DisplayName, ControlType,FieldValue,DisplayValue,FieldWidth,FieldType,FieldSize,
  OrderBy, FieldName, IsMandatory, AllowEdit,  GroupTagName, GroupName, TagName, DropDownAPI = ISNULL(DropDownAPI,''), 
  ValidationAPI = ISNULL(ValidationAPI,''), TabName,
  FielddataValue = (SELECT * FROM(SELECT ROWID, ISNULL(KEYNAME,'') AS DataValue FROM @tbl_Insertdata WHERE KEYVALUE = A.FieldName 
  UNION ALL SELECT ROWID = 0, '' AS DataValue) X1 FOR JSON PATH), 
  ColumnVisible 
  FROM @tbl_TemplateData A ORDER BY SerialNo FOR JSON PATH, ROOT('GetData'))
  
  SELECT @o_XML
  RETURN 1
END  
GO

CREATE PROCEDURE stpr_InsertUpdateXMLData @dsXml XML, @o_iReturnMainSlNo INT OUTPUT, @o_vcFlag VARCHAR(1) OUTPUT, 
@o_vcMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN
 /*      
///////////////////////////////////////////////////////////////////////////////////////////      
// Create By     : VAIBHAV GARG      
// Created Date  :       
// CCT NO        :     
// Version No    :       
// Description   :     
// Reviewed By   :      
// Review Date   :     
//////////////////////////////////////////////////////////////////////////////////////////      
*/
  DECLARE @tbl_InputJSONTable TABLE(ParameterName VARCHAR(50), ParameterValue VARCHAR(MAX), HeaderName VARCHAR(50), Jsontag VARCHAR(50), tagnew VARCHAR(MAX));
  DECLARE @o_ParameterList VARCHAR(max) = '', @o_ParameterListxml XML;
  DECLARE @strXMl VARCHAR(MAX) = CAST(@dsXml AS VARCHAR(MAX))
  DECLARE @strSecondLevelData VARCHAR(50) = 'SecondLevelData'
  SET @o_vcFlag = 'S'
  SET @o_vcMessage = ''
  DECLARE @tbl_PrimaryKey TABLE (KeyName VARCHAR(100), KeyValue VARCHAR(100))
  DECLARE @DeleteMasterPrimaryKey VARCHAR(MAX) = '', @DeleteDetailPrimaryKey VARCHAR(MAX) = '' --- PARAMETER LIST   
  
  EXEC SP_ParameterXMLRep @dsXml, @o_ParameterList OUTPUT
  IF ISNULL(@o_ParameterList, '') <> ''
  BEGIN
    SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)
    INSERT INTO @tbl_InputJSONTable (ParameterName, ParameterValue, HeaderName, Jsontag, tagnew)
    SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code, REPLACE(Parameter.value('(ColumnValue)[1]', 'VARCHAR(MAX)'), 
    'null', '') AS ColumnValue, Parameter.value('(MasterTag)[1]', 'VARCHAR(MAX)') AS MasterTag, Parameter.value('(JsonLevel)[1]', 
    'VARCHAR(MAX)') AS JsonLevel,
	Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code1
    FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
  END
  
 
    
  DECLARE @strModuleName VARCHAR(100) = '', @strString NVARCHAR(MAX),
  @strJ_accyear1 VARCHAR(8)='' , @J_dpid1  VARCHAR(8)=''
  
  SELECT @strModuleName = ParameterValue
  FROM @tbl_InputJSONTable
  WHERE ParameterName = 'ActionName'
  
  
  IF ISNULL(@strModuleName, '') = ''
  BEGIN
    SET @o_vcFlag = 'F'
    SET @o_vcMessage = '<Message>Module Name can not be blank</Message>'
    RETURN 1
  END
  
 
  DECLARE @i_vcmode VARCHAR(20) = '' 
  
  SELECT @i_vcmode = ParameterValue
  FROM @tbl_InputJSONTable
  WHERE ParameterName = 'Option'
 
  DECLARE @strTrxType AS VARCHAR(100)='', @strInstcd AS VARCHAR(100) = '', @strIntRefNo AS VARCHAR(100) ='', @strClientID AS VARCHAR(100) = '',
  @strExecDate VARCHAR(100) ='', @strRemark VARCHAR(100) = '', @strUserID VARCHAR(100) = '',
  @strPriKey VARCHAR(100) ='', @strQty DECIMAL(18, 2) =0, @strRate DECIMAL(18, 2) = 0
 
  DECLARE @strType VARCHAR(50)='', @strSlipcmcd VARCHAR(25)='', @strSlipNo INT = 0, @strSlipbooksize INT=0;
  DECLARE @strwhere VARCHAR(MAX) = '', @strDetailwhere VARCHAR(MAX)=''
 
  SELECT TOP 1 @strUserID = ParameterValue
  FROM @tbl_InputJSONTable
  WHERE ParameterName = 'UserId'
  
  
  IF @strModuleName = 'ClientAuthorise' AND @i_vcmode IN('ADD' ,'EDIT')
  BEGIN
    DECLARE @i_vStrEntryNewStatus varchar(50)='', @i_vStrSClientFormNo VARCHAR(20)='', @i_vStrSEntryName varchar(50)='',
	@i_vStrSReason VARCHAR(200)=''
   
    SELECT @i_vStrSEntryName = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'EntryName'
	
	SELECT @i_vStrSClientFormNo = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'ClientFormNo'
	
	SELECT @i_vStrEntryNewStatus = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'NewStatus'
	
	SELECT @i_vStrSReason = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'Reason'
	
	
	IF ISNULL(@i_vStrSEntryName,'') = ''
	BEGIN
	  SET @o_vcFlag = 'E'
	  SET @o_vcMessage = '<Message>EntryName can not be blank</Message>'
	  RETURN 1
	END
	
	IF ISNULL(@i_vStrEntryNewStatus,'') = ''
	BEGIN
	  SET @o_vcFlag = 'E'
	  SET @o_vcMessage = '<Message>Status can not be blank</Message>'
	  RETURN 1
	END
	
	IF ISNULL(@i_vStrSClientFormNo,'') = ''
	BEGIN
	  SET @o_vcFlag ='E'
	  SET @o_vcMessage = '<Message>Client Form No can not be blank</Message>'
	  RETURN 1
	END
	
	IF ISNULL(@i_vStrSReason,'') = '' AND @i_vStrEntryNewStatus = 'R'
	BEGIN
	  SET @o_vcFlag ='E'
	  SET @o_vcMessage = '<Message>Reason can not be blank in case of rejection</Message>'
	  RETURN 1
	END
    IF ISNULL(@i_vStrEntryNewStatus,'') IN('A','R') AND EXISTS(sELECT 1 FROM  
	 tbl_MultiEntryIncompleteMaster A WHERE a.ClientFormNo = @i_vStrSClientFormNo 
	 AND EntryType = @i_vStrSEntryName AND Entrystatus = 'Y')
	BEGIN 
	  UPDATE A SET A.Entrystatus = @i_vStrEntryNewStatus, 
	  A.Remarks = CASE WHEN  @i_vStrSReason = '' THEN 'Approved' ELSE @i_vStrSReason END
      FROM tbl_MultiEntryIncompleteMaster A
      WHERE a.ClientFormNo = @i_vStrSClientFormNo
	  AND EntryType = @i_vStrSEntryName
	  IF @i_vStrSEntryName = 'Clientoffline'
	  BEGIN
	    UPDATE A SET A.cx_allow  = CASE WHEN @i_vStrEntryNewStatus ='A' THEN 'Y' ELSE 'I' END
        FROM Client_export A
        WHERE a.cx_instrno = @i_vStrSClientFormNo
	  END	
	END
	ELSE
	BEGIN
	  SET @o_vcFlag ='E'
	  SET @o_vcMessage ='Form Should be Sumbited State'
	  SET @o_vcMessage = '<Message>'+@o_vcMessage+'</Message>'
	  RETURN 1
	END
	SET @o_vcFlag ='S'
	SET @o_vcMessage ='Process Completed'
	SET @o_vcMessage = '<Message>'+@o_vcMessage+'</Message>'
	RETURN 1
  END	
  
  IF @strModuleName = 'MakerSave' AND @i_vcmode IN('ADD' ,'EDIT')
  BEGIN
    DECLARE @i_vStrEntryName varchar(50)='', @i_vStrClientFormNo VARCHAR(20)=''
    SELECT @i_vStrEntryName = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'EntryName'
	
	SELECT @i_vStrClientFormNo = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'ClientFormNo'
	
	IF ISNULL(@i_vStrEntryName,'') = ''
	BEGIN
	  SET @o_vcFlag = 'E'
	  SET @o_vcMessage = '<Message>EntryName can not be blank</Message>'
	  RETURN 1
	END
	
	IF ISNULL(@i_vStrClientFormNo,'') = ''
	BEGIN
	  SET @o_vcFlag ='E'
	  SET @o_vcMessage = '<Message>Client Form No can not be blank</Message>'
	  RETURN 1
	END
	
 
    DECLARE @xstring VARCHAR(MAX)=''
	SELECT @xstring = CAST(@dsXml.query('/dsXml/X_DataJson') AS VARCHAR(MAX))
	SELECT @xstring = SUBSTRING(@xstring,CHARINDEX('<X_DataJson>',@xstring)+12,CHARINDEX('</X_DataJson>',@xstring)-13)
	
	EXEC stpr_InsertMultiEntryJson @xstring, 'INSERT', @i_vStrEntryName, @strUserID, @o_vcFlag OUTPUT, @o_vcMessage OUTPUT
	IF @o_vcFlag = 'S'
	BEGIN 
	  UPDATE A SET A.Entrystatus = 'Y', A.Remarks = 'Approved', EntryStep = 2
      FROM tbl_MultiEntryIncompleteMaster A
      WHERE a.ClientFormNo = @i_vStrClientFormNo
	  AND EntryType = @i_vStrEntryName
	END
	SET @o_vcMessage = '<Message>'+@o_vcMessage+'</Message>'
	RETURN 1
  END	
  
 
  DECLARE @strMasterTableName VARCHAR(100), @strMasterAutoGenColumnName VARCHAR(500), @strMasterPrimaryKeyName VARCHAR(500), 
  @strDetailTableName VARCHAR(500), @strDetailAutoGenColumnName VARCHAR(500), @strDetailPrimaryKeyName VARCHAR(500), 
  @strValidationSPName VARCHAR(100), @strDefaultMasterXML VARCHAR(MAX) = '', @strDefaultDetailXML VARCHAR(MAX) = ''
  SELECT @strMasterTableName = MasterTableName, @strMasterAutoGenColumnName = MasterAutoGenColumnName, @strMasterPrimaryKeyName = 
  MasterPrimaryKeyName, @strDetailTableName = DetailTableName, @strDetailAutoGenColumnName = DetailAutoGenColumnName, 
  @strDetailPrimaryKeyName = DetailPrimaryKeyName, @strValidationSPName = ValidationSPName, @strDefaultMasterXML = DefaultMasterXML, 
  @strDefaultDetailXML = DefaultDetailXML, @DeleteMasterPrimaryKey = DeleteMasterPrimaryKey, @DeleteDetailPrimaryKey = 
  DeleteDetailPrimaryKey
  FROM tbl_InsertUpdateConfig(NOLOCK)
  WHERE ModuleName = @strModuleName

  
  UPDATE A
  SET A.ParameterName = B.FieldName
  FROM @tbl_InputJSONTable A, tbl_GenericTemplateDefinition(NOLOCK) B
  WHERE B.TemplateName = @strModuleName AND A.ParameterName = B.TagName
  AND ((((HeaderName = 'SecondLevelData' and ParentTemplateCode = 'ChildEntry') or 
    (HeaderName <> 'SecondLevelData' and ParentTemplateCode = 'Entry')) 
  AND TemplateName = 'DematEntry') or TemplateName <> 'DematEntry')
    
    --- MASTER DATA INSERT / UPDATE   
  
  DECLARE @DefaultXML XML 
  IF ISNULL(@strDefaultMasterXML, '') <> ''
  BEGIN
    SET @DefaultXML = CAST(@strDefaultMasterXML AS XML)
    INSERT INTO @tbl_InputJSONTable (HeaderName, ParameterName, ParameterValue, Jsontag)
    SELECT * FROM (
    SELECT 'MasterLevelData' AS MasterTag, c.value('local-name(.)', 'NVARCHAR(MAX)') AS ColumnName, 
	c.value('(./text())[1]', 'NVARCHAR(MAX)') AS ColumnValue, JsonLevel = '1' 
	FROM @DefaultXML.nodes('/DefaultXml/*') AS t(c)) X1 
	WHERE X1.ColumnName NOT IN (SELECT ParameterName FROM @tbl_InputJSONTable)
  END
 
  IF ISNULL(@strValidationSPName, '') <> '' AND @i_vcmode in('add','edit')
  BEGIN
   SET @strString = 'EXEC DBO.' + @strValidationSPName + ' ''' + @strXMl + ''', @o_vcFlag OUTPUT, @o_vcMessage OUTPUT';
   BEGIN TRY
     EXEC sp_executesql @strString, N'@o_vcFlag VARCHAR(1) OUTPUT, @o_vcMessage VARCHAR(500) OUTPUT', @o_vcFlag OUTPUT, @o_vcMessage OUTPUT;
   END TRY

   BEGIN CATCH
     SET @o_vcFlag = 'F'
     SET @o_vcMessage = '<Message>' + @o_vcMessage + '</Message>'
     RETURN 1
   END CATCH
  END
  IF @o_vcFlag <> 'S'
  BEGIN
    SET @o_iReturnMainSlNo = 0
    RETURN 1
  END
  
  DECLARE @tbl_DataValidate TABLE (
  ColumnName VARCHAR(100), ColumnValue VARCHAR(MAX), MasterTag VARCHAR(100), JsonLevel VARCHAR(10), ColumnDescp VARCHAR(100), 
  ValidationValue VARCHAR(MAX), ValidationMessage VARCHAR(MAX), ValidationQuery VARCHAR(MAX))
   
    
  INSERT INTO @tbl_DataValidate (ColumnName, ColumnValue, MasterTag, JsonLevel)
  SELECT ParameterName, ParameterValue, HeaderName, Jsontag
  FROM @tbl_InputJSONTable
    
  UPDATE A 
  SET A.ColumnDescp = B.TagName, A.ValidationValue = B.ValidationValue, A.ValidationMessage = B.ValidationMessage, 
  A.ValidationQuery = B.ValidationQuery
  FROM @tbl_DataValidate A, tbl_GenericTemplateDefinition(NOLOCK) B
  WHERE B.TemplateName = @strModuleName AND A.ColumnName = B.FieldName
  AND ((((MasterTag = 'SecondLevelData' and ParentTemplateCode = 'ChildEntry') or 
  (MasterTag <> 'SecondLevelData' and ParentTemplateCode = 'Entry')) AND TemplateName = 'DematEntry') or TemplateName <> 'DematEntry')
  

  /*AND ((((MasterTag = 'SecondLevelData' and ParentTemplateCode IN 'ChildEntry') or 
  (MasterTag <> 'SecondLevelData' and ParentTemplateCode = 'Entry')) AND TemplateName = 'DematEntry') or TemplateName <> 'DematEntry')
*/
  ----- STATIC VALIDATION USING TEMPLATE ValidationValue/ValidationMessage
  if @i_vcmode in('add','edit')
  BEGIN
    DECLARE @TBL_OutputJSON TABLE (ErrorTag VARCHAR(100), ErrorMessage VARCHAR(MAX))
    DECLARE @strColumnValue VARCHAR(MAX) = '', @strColumnName VARCHAR(100) = '', 
    @strValidationValue VARCHAR(500), @strValidationmessage VARCHAR(MAX) = ''
    DECLARE CurValidation CURSOR
    FOR
    SELECT ColumnValue = (CASE WHEN ColumnValue = 'True' THEN 'Y' WHEN ColumnValue = 'False' THEN 'N' ELSE ColumnValue END), 
    ColumnName = ColumnDescp, ValidationValue, ValidationMessage
    FROM @tbl_DataValidate m
    WHERE ValidationValue <> '' AND ColumnValue <> '' AND ValidationValue NOT IN ('GETDATE', 'GETDATE,FUTUREDATE', 'GETDATE,PASTDATE')
    
    OPEN CurValidation;
    FETCH NEXT FROM CurValidation INTO @strColumnValue, @strColumnName, @strValidationValue, @strValidationmessage
    WHILE @@FETCH_STATUS = 0
    BEGIN
      IF NOT EXISTS (SELECT 1 FROM ( SELECT VALUE AS ColumnValue FROM dbo.ReturnTable(@strValidationValue, '|')) x1
      WHERE x1.ColumnValue = @strColumnValue)
      BEGIN
        INSERT INTO @TBL_OutputJSON (ErrorTag, ErrorMessage)
        VALUES ('E', ISNULL(@strColumnName, '') + ' Value Should be ' + iif(@strValidationmessage = '', @strValidationValue, @strValidationmessage))
      END
      FETCH NEXT FROM CurValidation INTO @strColumnValue, @strColumnName, @strValidationValue, @strValidationmessage
    END;
    CLOSE CurValidation;
    DEALLOCATE CurValidation;
 
   ----- STATIC VALIDATION USING TEMPLATE ValidationValue --> GETDATE/GETDATE,FUTUREDATE/'GETDATE,PASTDATE'
 
    DECLARE CurDateValidation CURSOR FOR 
    SELECT ColumnValue = (CASE WHEN ColumnValue = 'True' THEN 'Y' WHEN ColumnValue = 'False' THEN 'N' ELSE ColumnValue END), 
    ColumnName = ColumnDescp, ValidationValue, ValidationMessage
    FROM @tbl_DataValidate m
    WHERE ValidationValue <> '' AND ColumnValue <> '' AND ValidationValue IN ('GETDATE', 'GETDATE,FUTUREDATE', 'GETDATE,PASTDATE')
    OPEN CurDateValidation;
    FETCH NEXT FROM CurDateValidation
    INTO @strColumnValue, @strColumnName, @strValidationValue, @strValidationmessage
    WHILE @@FETCH_STATUS = 0
    BEGIN
      IF ISDATE(@strColumnValue) = 0
      BEGIN
        INSERT INTO @TBL_OutputJSON (ErrorTag, ErrorMessage)
        VALUES ('E', ISNULL(@strColumnName, '') + ' Value Should be in Date Format')
      END
      IF @strValidationValue = 'GETDATE' AND ISDATE(@strColumnValue) > 0
      BEGIN
        IF CONVERT(VARCHAR, CAST(@strColumnValue AS DATE), 112) <> CONVERT(VARCHAR, GETDATE(), 112)
        BEGIN
          INSERT INTO @TBL_OutputJSON (ErrorTag, ErrorMessage)
          VALUES ('E', ISNULL(@strColumnName, '') + ' Value Should be in Todays Date')
        END
      END
      IF @strValidationValue = 'GETDATE,FUTUREDATE' AND ISDATE(@strColumnValue) > 0
      BEGIN
        IF CONVERT(VARCHAR, CAST(@strColumnValue AS DATE), 112) < CONVERT(VARCHAR, GETDATE(), 112)
        BEGIN
          INSERT INTO @TBL_OutputJSON (ErrorTag, ErrorMessage)
          VALUES ('E', ISNULL(@strColumnName, '') + ' Value Should be in Todays Date or Future Date')
        END
      END
      IF @strValidationValue = 'GETDATE,PASTDATE' AND ISDATE(@strColumnValue) > 0
      BEGIN
        IF CONVERT(VARCHAR, CAST(@strColumnValue AS DATE), 112) > CONVERT(VARCHAR, GETDATE(), 112)
        BEGIN
          INSERT INTO @TBL_OutputJSON (ErrorTag, ErrorMessage)
          VALUES ('E', ISNULL(@strColumnName, '') + ' Value Should be in Todays Date or Previous Date')
        END
      END
      FETCH NEXT FROM CurDateValidation INTO @strColumnValue, @strColumnName, @strValidationValue, @strValidationmessage
    END;
    CLOSE CurDateValidation;
    DEALLOCATE CurDateValidation;
  
    IF EXISTS(SELECT 1 FROM @TBL_OutputJSON)
    BEGIN
      SET @o_vcFlag = 'F'
	  DECLARE @StrValidmsg VARCHAR(MAX)=''
	  SELECT @StrValidmsg = @StrValidmsg+' / '+ErrorMessage FROM @TBL_OutputJSON
	  SET @o_vcMessage = '<Message>' + @StrValidmsg + '</Message>'
      RETURN 1
    END
  END  
  
  IF @i_vcmode = 'DELETE'
  BEGIN
    IF ISNULL(@strMasterTableName,'') <> ''
	BEGIN
      DELETE FROM @tbl_PrimaryKey
    
	  INSERT INTO @tbl_PrimaryKey (KeyName)
      SELECT VALUE
      FROM DBO.RETURNTABLE(@DeleteMasterPrimaryKey, '|')
   
      UPDATE A 
      SET A.KeyValue =  case when C.ControlType='DatePicker' Then CONVERT(VARCHAR(8), CONVERT(DATETIME, B.ParameterValue, 103), 112) Else B.ParameterValue End
      FROM @tbl_PrimaryKey A, @tbl_InputJSONTable B, tbl_GenericTemplateDefinition(nolock) C
      WHERE A.KeyName = B.ParameterName
			AND C.TemplateNAME = @strModuleName
			AND B.tagnew = C.TagName
      SET @strwhere = ''
   
      SELECT @strwhere = @strwhere + ' ' + ' AND ' + KeyName + ' = ''' + CAST(KeyValue AS VARCHAR) + ''''
      FROM @tbl_PrimaryKey
      WHERE isnull(KeyValue, '') <> ''
   
      IF EXISTS (SELECT 1 FROM @tbl_PrimaryKey WHERE isnull(KeyValue, '') = '')
      BEGIN
        SET @o_vcFlag = 'F'
        SET @o_vcMessage = '<Message>Primary Key Value Not Avaiable in XML</Message>'
        RETURN 1
      END
	
	  IF ISNULL(@DeleteDetailPrimaryKey,'') <> ''
	  BEGIN
	    DELETE FROM @tbl_PrimaryKey
    
	    INSERT INTO @tbl_PrimaryKey (KeyName)
        SELECT VALUE
        FROM DBO.RETURNTABLE(@DeleteDetailPrimaryKey, '|')
   
        UPDATE A 
        SET A.KeyValue = B.ParameterValue
        FROM @tbl_PrimaryKey A, @tbl_InputJSONTable B
        WHERE A.KeyName = B.ParameterName
   
        SET @strDetailwhere = ''
   
        SELECT @strDetailwhere = @strDetailwhere + ' ' + ' AND ' + KeyName + ' = ''' + CAST(KeyValue AS VARCHAR) + ''''
        FROM @tbl_PrimaryKey
        WHERE isnull(KeyValue, '') <> ''
	  END
    END
  END	
  
  -- AUTO INCREMENT VALUE   

  DECLARE @StrAutoKeyName VARCHAR(100) = '', @strAutoKeyValue VARCHAR(10) = '',
  @strMasterAutoKeyValue VARCHAR(50)='', @strMasterAutoKeyValue1 VARCHAR(50)=''
  DECLARE @tbl_AutoIncrement TABLE (ColumnValue VARCHAR(100))
 
  ---- AUTO COLUMN GENERATEION AS INCREMENT BY 1
 
  IF ISNULL(@strMasterAutoGenColumnName, '') <> '' AND @i_vcmode = 'add'
  BEGIN
    DECLARE CURSOR_MasterAuto CURSOR
    FOR SELECT VALUE AS KeyName
    FROM DBO.RETURNTABLE(@strMasterAutoGenColumnName, '|');
    OPEN CURSOR_MasterAuto
    FETCH NEXT FROM CURSOR_MasterAuto
    INTO @strAutoKeyName
    WHILE @@FETCH_STATUS = 0
    BEGIN
      DELETE FROM @tbl_AutoIncrement
      IF ISNULL(@strMasterTableName,'') <> ''
      BEGIN
        SET @strString = 'SELECT MAX(' + @StrAutoKeyName + ')+1 FROM ' + @strMasterTableName + ' '
        INSERT INTO @tbl_AutoIncrement (ColumnValue)
        EXEC (@strString)
        SELECT @strAutoKeyValue = ColumnValue
        FROM @tbl_AutoIncrement
        
		INSERT INTO @tbl_InputJSONTable (ParameterName, ParameterValue, HeaderName, Jsontag)
        SELECT @strAutoKeyName, @strAutoKeyValue, 'MasterLevelData', '1'
      END
	  IF ISNULL(@strDetailTableName,'') <> ''
	  BEGIN
	    SET @strString = 'SELECT MAX(' + @StrAutoKeyName + ')+1 FROM ' + @strDetailTableName + ' '
        INSERT INTO @tbl_AutoIncrement (ColumnValue)
        EXEC (@strString)
        SELECT @strMasterAutoKeyValue = ColumnValue FROM @tbl_AutoIncrement
		SET @strMasterAutoKeyValue1 = @StrAutoKeyName
	  END
      FETCH NEXT FROM CURSOR_MasterAuto INTO @StrAutoKeyName
    END
    CLOSE CURSOR_MasterAuto
    DEALLOCATE CURSOR_MasterAuto
  END
  
  DECLARE @strTempColumnName NVARCHAR(MAX) = '', @strTempColumnValue NVARCHAR(MAX) = ''
  SET @strColumnValue = ''
  SET @strColumnName = ''
 
  DECLARE @curParameterValue VARCHAR(MAX)='', @curParameterName VARCHAR(50)=''
  
  DECLARE @o_iReturnMasterSlNo INT = 0
  DECLARE @strSQLString VARCHAR(MAX) = '', @strDetailSQLString VARCHAR(MAX)=''
  DECLARE @xmlOutput1 XML, @o_vcDataOutput1 VARCHAR(MAX) = ''
  DECLARE @MasterDeleteQuery VARCHAR(MAX) = '', @PreDetailDeleteQuery VARCHAR(MAX) = '', 
  @PostDetailDeleteQuery VARCHAR(MAX) = '', @MasterPreDeleteEvent VARCHAR(MAX) = '', 
  @MasterPostDeleteEvent VARCHAR(MAX) = ''
  
  IF @strMasterTableName <> ''
  BEGIN
    IF @i_vcmode = 'Delete'
    BEGIN
	  IF ISNULL(@strDetailwhere,'') <> ''
      BEGIN
	    SET @strDetailSQLString = 'Delete FROM ' + @strDetailTableName + ' WHERE 1 = 1 ' + @strDetailwhere		
      END
	 
      SET @strSQLString = 'Delete from ' + @strMasterTableName + ' WHERE 1 = 1 ' + @strwhere
     
	  IF EXISTS ( SELECT 1 FROM tbl_InsertUpdateXMLDropDownQuery(NOLOCK) 
      WHERE ModuleName = @strModuleName AND ColumnName = 'MasterPreDeleteEvent')
      BEGIN
	   
	    SELECT @MasterPreDeleteEvent = DBQUERY
        FROM tbl_InsertUpdateXMLDropDownQuery
        WHERE ModuleName = @strModuleName AND ColumnName = 'MasterPreDeleteEvent'
	   
	    DECLARE Cur12 CURSOR FOR 
        SELECT ParameterValue, ParameterName FROM @tbl_InputJSONTable 
	    --WHERE HeaderName = 'MasterLevelData' AND CHARINDEX(@curParameterName,@MasterPreDeleteEvent) > 0
        OPEN Cur12 
        FETCH NEXT FROM Cur12 INTO @curParameterValue, @curParameterName
        WHILE @@FETCH_STATUS = 0
        BEGIN 
		  SET @MasterPreDeleteEvent = REPLACE(@MasterPreDeleteEvent,'<<'+@curParameterName+'>>',@curParameterValue)
		  FETCH NEXT FROM Cur12 INTO @curParameterValue, @curParameterName
        END 
        CLOSE Cur12 
        DEALLOCATE Cur12 
	    SET @MasterPreDeleteEvent = REPLACE(@MasterPreDeleteEvent, '<<XWHERE>>', @strwhere)
      END   
	 
      IF EXISTS (SELECT 1 FROM tbl_InsertUpdateXMLDropDownQuery(NOLOCK) 
      WHERE ModuleName = @strModuleName AND ColumnName = 'MasterPostDeleteEvent')
      BEGIN
	    SELECT @MasterPostDeleteEvent = DBQUERY
        FROM tbl_InsertUpdateXMLDropDownQuery
        WHERE ModuleName = @strModuleName AND ColumnName = 'MasterPostDeleteEvent'

        DECLARE Cur13 CURSOR FOR 
        SELECT ParameterValue, ParameterName FROM @tbl_InputJSONTable 
	    --WHERE HeaderName = 'MasterLevelData' AND CHARINDEX(@curParameterName,@MasterPreDeleteEvent) > 0
        OPEN Cur13 
        FETCH NEXT FROM Cur13 INTO @curParameterValue, @curParameterName
        WHILE @@FETCH_STATUS = 0
        BEGIN 
		  SET @MasterPostDeleteEvent = REPLACE(@MasterPostDeleteEvent,'<<'+@curParameterName+'>>',@curParameterValue)
		  FETCH NEXT FROM Cur13 INTO @curParameterValue, @curParameterName
        END 
        CLOSE Cur13 
        DEALLOCATE Cur13 
	    SET @MasterPostDeleteEvent = REPLACE(@MasterPostDeleteEvent, '<<XWHERE>>', @strwhere)
      END  
	 		 
      BEGIN TRANSACTION
      BEGIN TRY
	    IF ISNULL(@MasterPreDeleteEvent,'') <> ''
	    BEGIN
          EXEC(@MasterPreDeleteEvent)
	    END  
	    IF ISNULL(@strDetailSQLString,'') <> ''
	    BEGIN
	      EXEC(@strDetailSQLString)
	    END
        EXEC(@strSQLString)
	    IF ISNULL(@MasterPostDeleteEvent,'') <> ''
	    BEGIN
	      EXEC(@MasterPostDeleteEvent)
	    END  
        COMMIT;
      END TRY
      BEGIN CATCH
        ROLLBACK;
        SET @o_vcFlag = 'F'
        SET @o_vcMessage = '<Message>' + error_message() + '</Message>'
        RETURN 1
      END CATCH
      SET @o_vcFlag = 'S'
      SET @o_vcMessage = '<Message>Record Deleted</Message>'
      RETURN 1
    END 
	
	IF @strModuleName = 'SlipIssue'
    BEGIN
	  SET @xmlOutput1 = (SELECT * FROM @tbl_InputJSONTable FOR XML PATH('DATA1'))
	  EXEC stpr_APISlipIssue @xmlOutput1, @i_vcmode, @strwhere, @strMasterTableName, @strMasterPrimaryKeyName, 
      @o_iReturnMasterSlNo OUTPUT, @o_vcFlag OUTPUT, @o_vcMessage OUTPUT
      SET @o_iReturnMainSlNo  = @o_iReturnMasterSlNo
	  RETURN 1
    END
    ELSE 
    IF @strModuleName IN ('Receipts','Payments')
    BEGIN
      SET @xmlOutput1 = (SELECT * FROM @tbl_InputJSONTable FOR XML PATH('DATA1'))
	  EXEC stpr_APIReceiptPayment @xmlOutput1, @i_vcmode, @strwhere, @strMasterTableName, @strMasterPrimaryKeyName, @strModuleName,
      @o_iReturnMasterSlNo OUTPUT, @o_vcFlag OUTPUT, @o_vcMessage OUTPUT
      SET @o_iReturnMainSlNo  = @o_iReturnMasterSlNo
	  RETURN 1
    END
    ELSE 
    IF @strModuleName IN ('PledgeSetup') AND UPPER(@i_vcmode) in('ADD','Edit')
    BEGIN
      SET @xmlOutput1 = (SELECT * FROM @tbl_InputJSONTable FOR XML PATH('DATA1'))
	  EXEC stpr_APIPledgeEntry @xmlOutput1, @i_vcmode, @strwhere, @strMasterTableName, @strMasterPrimaryKeyName, 
      @o_iReturnMasterSlNo OUTPUT, @o_vcFlag OUTPUT, @o_vcMessage OUTPUT
      SET @o_iReturnMainSlNo  = @o_iReturnMasterSlNo
	  RETURN 1
    END
    ELSE 
    IF @strModuleName IN ('DematEntry') AND UPPER(@i_vcmode) in('ADD','Edit')
    BEGIN
      SET @xmlOutput1 = (SELECT * FROM @tbl_InputJSONTable FOR XML PATH('DATA1'))
	  
	  EXEC stpr_APIDematEntry @xmlOutput1, @i_vcmode, @strwhere, @strMasterTableName, @strMasterPrimaryKeyName, 
      @o_iReturnMasterSlNo OUTPUT, @o_vcFlag OUTPUT, @o_vcMessage OUTPUT
      SET @o_iReturnMainSlNo  = @o_iReturnMasterSlNo
	  IF @o_vcFlag <> 'S'
	  BEGIN
	    RETURN 1
	   END	
    END
	ELSE 
	BEGIN
	  SET @xmlOutput1 = (SELECT * FROM @tbl_InputJSONTable FOR XML PATH('DATA1'))
	  
	  EXEC stpr_APICommanInsertUpdate @xmlOutput1, @i_vcmode, @strwhere, @strMasterTableName, @strMasterPrimaryKeyName, 
      @o_iReturnMasterSlNo OUTPUT, @o_vcFlag OUTPUT, @o_vcMessage OUTPUT
      SET @o_iReturnMainSlNo  = @o_iReturnMasterSlNo
	  RETURN 1
	END 
  END
  
  IF @strDetailTableName <> ''
  BEGIN
    UPDATE A
    SET A.ParameterName = B.FieldName
    FROM @tbl_InputJSONTable A, tbl_GenericTemplateDefinition(NOLOCK) B
    WHERE B.TemplateName = @strModuleName AND A.ParameterName = B.TagName
    AND ((((HeaderName = 'SecondLevelData' and ParentTemplateCode = 'ChildEntry') or 
   (HeaderName <> 'SecondLevelData' and ParentTemplateCode = 'Entry')) AND TemplateName = 'DematEntry') or TemplateName <> 'DematEntry')  
 
   IF @i_vcmode = 'Delete'
   BEGIN
     DELETE FROM @tbl_PrimaryKey
     
	 INSERT INTO @tbl_PrimaryKey (KeyName)
     SELECT VALUE
     FROM DBO.RETURNTABLE(@DeleteDetailPrimaryKey, '|')
     
	 UPDATE A
     SET A.KeyValue = ISNULL(B.ParameterValue, '')
     FROM @tbl_PrimaryKey A, @tbl_InputJSONTable B
     WHERE A.KeyName = B.ParameterName
     SET @strwhere = ''
     
	 SELECT @strwhere = @strwhere + ' ' + ' AND ' + KeyName + ' = ''' + CAST(KeyValue AS VARCHAR) + ''''
     FROM @tbl_PrimaryKey
     WHERE isnull(KeyValue, '') <> ''
     IF EXISTS ( SELECT 1 FROM @tbl_PrimaryKey WHERE isnull(KeyValue, '') = '')
     BEGIN
       SET @o_vcFlag = 'F'
       SET @o_vcMessage = '<Message>Primary Key Value Not Avaiable in XML</Message>'
       RETURN 1
     END
	 
     DECLARE @tb_instcd VARCHAR(20) = '', @tb_internal_refno VARCHAR(50) = ''
     
	 IF EXISTS (SELECT 1 FROM tbl_InsertUpdateXMLDropDownQuery(NOLOCK)
        WHERE ModuleName = @strModuleName AND ColumnName LIKE '%DetailDeleteEvent')
     BEGIN
       SELECT @PreDetailDeleteQuery = DBQUERY
       FROM tbl_InsertUpdateXMLDropDownQuery
       WHERE ModuleName = @strModuleName AND ColumnName = 'PREDetailDeleteEvent'
       
	   SELECT @PostDetailDeleteQuery = DBQUERY
       FROM tbl_InsertUpdateXMLDropDownQuery
       WHERE ModuleName = @strModuleName AND ColumnName = 'PostDetailDeleteEvent'

	   DECLARE Cur121 CURSOR FOR 
       SELECT ParameterValue, ParameterName FROM @tbl_InputJSONTable 
	   --WHERE CHARINDEX(@curParameterName,@PreDetailDeleteQuery) > 0
       OPEN Cur121 
       FETCH NEXT FROM Cur121 INTO @curParameterValue, @curParameterName
       WHILE @@FETCH_STATUS = 0
       BEGIN 
		 SET @PreDetailDeleteQuery = REPLACE(@PreDetailDeleteQuery,'<<'+@curParameterName+'>>',@curParameterValue)
		 FETCH NEXT FROM Cur121 INTO @curParameterValue, @curParameterName
       END 
       CLOSE Cur121 
       DEALLOCATE Cur121 
	  
	   DECLARE Cur121 CURSOR FOR 
       SELECT ParameterValue, ParameterName FROM @tbl_InputJSONTable 
	   --WHERE CHARINDEX(@curParameterName,@PostDetailDeleteQuery) > 0
       OPEN Cur121 
       FETCH NEXT FROM Cur121 INTO @curParameterValue, @curParameterName
       WHILE @@FETCH_STATUS = 0
       BEGIN 
		 SET @PostDetailDeleteQuery = REPLACE(@PostDetailDeleteQuery,'<<'+@curParameterName+'>>',@curParameterValue)
		 FETCH NEXT FROM Cur121 INTO @curParameterValue, @curParameterName
       END 
       CLOSE Cur121 
       DEALLOCATE Cur121 
	   SET @PreDetailDeleteQuery = REPLACE(@PreDetailDeleteQuery, '<<XWHERE>>', @strwhere)
       SET @PostDetailDeleteQuery = REPLACE(@PostDetailDeleteQuery, '<<XWHERE>>', @strwhere)
       SET @PreDetailDeleteQuery = REPLACE(@PreDetailDeleteQuery, '<<Userid>>', @strUserID)
	   SET @PostDetailDeleteQuery = REPLACE(@PostDetailDeleteQuery, '<<Userid>>', @strUserID)   
	 END
	 
     SET @strSQLString = 'Delete from ' + @strDetailTableName + ' WHERE 1 = 1 ' + @strwhere
	 
	/* SELECT @PreDetailDeleteQuery
	 SELECT @strSQLString
	 SELECT @PostDetailDeleteQuery
	 */
	 
     BEGIN TRANSACTION
     BEGIN TRY
	    IF ISNULL(@PreDetailDeleteQuery,'') <> ''
		BEGIN
          EXEC (@PreDetailDeleteQuery)
		END  
        IF ISNULL(@strSQLString,'') <> ''
		BEGIN
          EXEC (@strSQLString)
		END  
        IF ISNULL(@PostDetailDeleteQuery,'') <> ''
		BEGIN
          EXEC (@PostDetailDeleteQuery)
		END  
        COMMIT;
     END TRY
     BEGIN CATCH
       ROLLBACK;
       SET @o_vcFlag = 'F'
       SET @o_vcMessage = '<Message>' + error_message() + '</Message>'
       RETURN 1
     END CATCH
     SET @o_vcFlag = 'S'
     SET @o_vcMessage = '<Message>Record Deleted</Message>'
     RETURN 1
   END
   
   IF CURSOR_STATUS('GLOBAL', 'CURSOR_DetailInsert') = 1
   BEGIN
     CLOSE CURSOR_DetailInsert
	 DEALLOCATE CURSOR_DetailInsert
   END
   
   
   IF @strModuleName IN ('OffMarketEntry','OnMarketEntry','EarlyPayin','InterDepository') 
   AND UPPER(@i_vcmode) IN('ADD','EDIT') AND NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable 
   WHERE HeaderName = @strSecondLevelData AND ParameterName IN('tb_isin','id_isin')  
   and ISNULL(ParameterValue,'') <> '')
   BEGIN
	 SET @o_vcFlag = 'F'
     SET @o_vcMessage = '<Message>Isin Not Found</Message>'
     RETURN 1
   END
   
   DECLARE @icurJsonTag VARCHAR(10) = ''
   DECLARE CURSOR_DetailInsert CURSOR
   FOR
   SELECT DISTINCT JsonTag
   FROM @tbl_InputJSONTable
   WHERE HeaderName = @strSecondLevelData
   OPEN CURSOR_DetailInsert
   FETCH NEXT
   FROM CURSOR_DetailInsert
   INTO @icurJsonTag
   WHILE @@FETCH_STATUS = 0
   BEGIN
     IF ISNULL(@strDefaultDetailXML, '') <> ''
     BEGIN
       SET @DefaultXML = CAST(@strDefaultDetailXML AS XML)
       INSERT INTO @tbl_InputJSONTable (HeaderName, ParameterName, ParameterValue, Jsontag)
       SELECT *
       FROM (
       SELECT @strSecondLevelData AS MasterTag, c.value('local-name(.)', 'NVARCHAR(MAX)') AS ColumnName, c.value('(./text())[1]', 
       'NVARCHAR(MAX)') AS ColumnValue, JsonLevel = @icurJsonTag
       FROM @DefaultXML.nodes('/DefaultXml/*') AS t(c)) X1
       WHERE X1.ColumnName NOT IN ( SELECT ParameterName FROM @tbl_InputJSONTable WHERE Jsontag = @icurJsonTag)
     END
	 
	 IF EXISTS(SELECT 1 FROM @tbl_InputJSONTable WHERE HeaderName = @strSecondLevelData AND JSONTAG = @icurJsonTag
	 AND ParameterName = 'IsAdd' AND ParameterValue = 'false')
	 BEGIN
	   SET @i_vcmode = 'Edit'
	 END
     ELSE IF exists(SELECT 1 FROM @tbl_InputJSONTable WHERE HeaderName = @strSecondLevelData AND JSONTAG = @icurJsonTag
	 AND ParameterName = 'IsAdd' AND ParameterValue = 'true')
	 BEGIN
	   SET @i_vcmode = 'Add'
	 END

	 DECLARE @Jsontag VARCHAR(10) = ''
	 
	 IF ISNULL(@strDetailAutoGenColumnName, '') <> '' AND (
     @i_vcmode = 'add' OR EXISTS ( SELECT 1 FROM @tbl_InputJSONTable 
	 WHERE ParameterName = 'IsInserted' AND ParameterValue = 'true' AND Jsontag = @icurJsonTag))
     BEGIN
       DECLARE CURSOR_DetailAuto 
	   CURSOR FOR
       SELECT Jsontag, KeyName FROM (
       SELECT DISTINCT Jsontag
       FROM @tbl_InputJSONTable
       WHERE HeaderName = @strSecondLevelData AND Jsontag = @icurJsonTag) A, 
	   (SELECT VALUE AS KeyName FROM DBO.RETURNTABLE(@strDetailAutoGenColumnName, '|') ) B;
       
	   OPEN CURSOR_DetailAuto
       FETCH NEXT FROM CURSOR_DetailAuto INTO @Jsontag, @strAutoKeyName
       WHILE @@FETCH_STATUS = 0
	   BEGIN
         DELETE FROM @tbl_AutoIncrement
         SET @strString = 'SELECT MAX(' + @StrAutoKeyName + ')+' + @Jsontag + ' FROM ' + @strDetailTableName + ' '
         
		 INSERT INTO @tbl_AutoIncrement (ColumnValue)
         EXEC (@strString)
         SELECT @strAutoKeyValue = ColumnValue FROM @tbl_AutoIncrement
         
		 INSERT INTO @tbl_InputJSONTable (ParameterName, ParameterValue, HeaderName, Jsontag)
         SELECT @strAutoKeyName, @strAutoKeyValue, @strSecondLevelData, @Jsontag
         FETCH NEXT FROM CURSOR_DetailAuto INTO @Jsontag, @StrAutoKeyName
       END
       CLOSE CURSOR_DetailAuto
       DEALLOCATE CURSOR_DetailAuto
     END
	
	
     
	 SET @xmlOutput1 = (SELECT DISTINCT ParameterName, ParameterValue = ltrim(rtrim(ParameterValue)), HeaderName
     FROM (SELECT ParameterName, ParameterValue, HeaderName, Jsontag
     FROM @tbl_InputJSONTable X
     WHERE HeaderName = @strSecondLevelData AND JSONTAG = @icurJsonTag
	 UNION ALL
     SELECT ParameterName, ParameterValue, HeaderName = @strSecondLevelData, Jsontag
     FROM @tbl_InputJSONTable
     WHERE HeaderName = 'MasterLevelData' AND ISNULL(@strMasterTableName, '') IN('')) X1
	 FOR XML PATH('DATA1'))
	 
     SET @o_vcDataOutput1 = CAST(@xmlOutput1 AS VARCHAR(MAX))
	
     IF @strModuleName IN ('Journals') AND @i_vcmode in('add','edit')
     BEGIN
       EXEC stpr_APIJournalsEntry @xmlOutput1, @i_vcmode, @strwhere, @strDetailTableName, @strDetailPrimaryKeyName, 
	   @strMasterAutoKeyValue1, @strMasterAutoKeyValue, @icurJsonTag, @o_iReturnMasterSlNo OUTPUT, @o_vcFlag OUTPUT, @o_vcMessage OUTPUT,
	   @strJ_accyear1 OUTPUT , @J_dpid1 OUTPUT
       SET @o_iReturnMainSlNo  = @o_iReturnMasterSlNo
     END
     
     IF @strModuleName IN ('OffMarketEntry','OnMarketEntry','EarlyPayin','InterDepository') 
     AND UPPER(@i_vcmode) IN('ADD','EDIT') AND EXISTS(SELECT 1 FROM @tbl_InputJSONTable 
	 WHERE HeaderName = @strSecondLevelData AND JSONTAG = @icurJsonTag
	 AND ParameterName IN('tb_isin','id_isin')  and ISNULL(ParameterValue,'') <> '')
     BEGIN
	   
	   IF ISNULL(@strValidationSPName, '') <> ''
       BEGIN
	     DECLARE @strValidatexml VARCHAR(MAX)=CAST(@xmlOutput1 AS VARCHAR(MAX))
         SET @strString = 'EXEC DBO.' + @strValidationSPName + ' ''' + @strValidatexml + ''', @o_vcFlag OUTPUT, @o_vcMessage OUTPUT, ''D''';
         BEGIN TRY
            EXEC sp_executesql @strString, N'@o_vcFlag VARCHAR(1) OUTPUT, @o_vcMessage VARCHAR(500) OUTPUT', @o_vcFlag OUTPUT, @o_vcMessage OUTPUT;
         END TRY

         BEGIN CATCH
           SET @o_vcFlag = 'F'
           SET @o_vcMessage = '<Message>' + @o_vcMessage + '</Message>'
           RETURN 1
         END CATCH
        END
       IF @o_vcFlag <> 'S'
       BEGIN
         SET @o_iReturnMainSlNo = 0
         RETURN 1
       END
       
       EXEC stpr_APIOffMarketEntry @xmlOutput1, @i_vcmode, @strwhere, @strDetailTableName, @strDetailPrimaryKeyName, 
	   @icurJsonTag, @strModuleName, @o_iReturnMasterSlNo OUTPUT, @o_vcFlag OUTPUT, @o_vcMessage OUTPUT
       SET @o_iReturnMainSlNo  = @o_iReturnMasterSlNo
     END 
	 
	IF @strModuleName IN ('DematEntry') AND UPPER(@i_vcmode) in('ADD','Edit')
    BEGIN
      
	  UPDATE A
      SET A.ParameterName = B.FieldName
      FROM @tbl_InputJSONTable A, tbl_GenericTemplateDefinition(NOLOCK) B
      WHERE B.TemplateName = @strModuleName AND A.tagnew = B.TagName
      AND ParentTemplateCode = 'ChildEntry'
   
	 -- SELECT @xmlOutput1
	  SET @xmlOutput1 = (SELECT DISTINCT ParameterName, ParameterValue = ltrim(rtrim(ParameterValue)), HeaderName
      FROM (SELECT ParameterName, ParameterValue, HeaderName, Jsontag
      FROM @tbl_InputJSONTable X
      WHERE HeaderName = @strSecondLevelData AND JSONTAG = @icurJsonTag
	  AND ParameterVALUE <> 'undefined'
	  UNION ALL
      SELECT ParameterName, ParameterValue, HeaderName = @strSecondLevelData, Jsontag
      FROM @tbl_InputJSONTable
      WHERE HeaderName = 'MasterLevelData') X1
	  FOR XML PATH('DATA1'))
      SET @o_vcDataOutput1 = CAST(@xmlOutput1 AS VARCHAR(MAX))
	
	  EXEC stpr_APIDematEntry_Detail @xmlOutput1, @i_vcmode, @strwhere, @strDetailTableName, @strDetailPrimaryKeyName, 
      @o_iReturnMasterSlNo OUTPUT, @o_vcFlag OUTPUT, @o_vcMessage OUTPUT
      SET @o_iReturnMainSlNo  = @o_iReturnMasterSlNo
	  --RETURN 1
    END
	 
	 FETCH NEXT FROM CURSOR_DetailInsert INTO @icurJsonTag
   END
   CLOSE CURSOR_DetailInsert
   DEALLOCATE CURSOR_DetailInsert
   
   IF @strModuleName IN ('Journals') AND UPPER(@i_vcmode) = 'ADD' AND @o_vcFlag = 'S' 
   BEGIN
     IF NOT EXISTS (SELECT 1 FROM Auth_accounts(NOLOCK) WHERE aa_documenttype = 'J' 
	 AND aa_amount <= (SELECT SUM(jr_amount) FROM Journal(NOLOCK) 
	 WHERE jr_debitflag = 'D' AND jr_srno = @o_iReturnMainSlNo 
	 AND jr_accyear = @strJ_accyear1 and jr_dpid = @J_dpid1))
	 BEGIN
	   UPDATE Journal SET jr_status = 'Y', jr_authid1 = '', jr_authid2 = '' , jr_commondt = jr_dt
	   WHERE jr_srno = @o_iReturnMainSlNo AND jr_accyear = @strJ_accyear1
       AND jr_dpid = @J_dpid1
		  		 
	   INSERT INTO Ledger (ld_documentno, ld_clientcd, ld_amount, ld_documenttype, ld_debitflag, ld_chequeno, 
	   ld_particular, ld_dt, ld_entryno, ld_accyear, ld_dpid, mkrid, ld_commondt, ld_common, ld_costcenter, Mkrdt)
       SELECT jr_srno, jr_clientcd, jr_amount, 'J', jr_debitflag, '', jr_eparticular, jr_dt, jr_entryno, 
	   jr_accyear, jr_dpid, @strUserID, jr_commondt, jr_common, jr_costcenter, CONVERT(VARCHAR,GETDATE(),112)
	   FROM Journal(NOLOCK)
	   WHERE jr_accyear = @strJ_accyear1 AND jr_srno = @o_iReturnMainSlNo AND jr_dpid = @J_dpid1 AND jr_status = 'Y'
	 END
	 ELSE
	 BEGIN
	   UPDATE Journal SET jr_status = 'N', jr_authid1 = '', jr_authid2 = '' 
	   WHERE jr_srno = @o_iReturnMainSlNo AND jr_accyear = @strJ_accyear1
       AND jr_dpid = @J_dpid1
	 END
   END
 END
 RETURN 1
END
GO

CREATE PROCEDURE stpr_InsertUpdateXMLDropDown @dsXml XML WITH ENCRYPTION AS
BEGIN
  DECLARE @tbl_InputJSONTable DBO.tb_ParamList ;
  DECLARE @o_ParameterList varchar(max)='', @o_ParameterListxml XML; 
  DECLARE @tbl_UserList dbo.UserAccessList;
  
  EXEC SP_ParameterXMLRep @dsXml, @o_ParameterList OUTPUT
  
  IF ISNULL(@o_ParameterList,'') <> ''
  BEGIN
    SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)
   
    INSERT INTO @tbl_InputJSONTable (ParameterName,  ParameterValue, HeaderName, Jsontag) 
    SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code ,
    Parameter.value('(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue,
	Parameter.value('(MasterTag)[1]', 'VARCHAR(MAX)') AS MasterTag,
	Parameter.value('(JsonLevel)[1]', 'VARCHAR(MAX)') AS JsonLevel
    FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
  END 
  
     --- USER ACCESS RIGHTS
	 
  DECLARE @strUserid VARCHAR(500)='', @StrClientCode VARCHAR(50)='', @strString NVARCHAR(MAX)='',
  @strxfilter VARCHAR(MAX)='',
  @strHeaderString VARCHAR(MAX)='', @strxml VARCHAR(MAX)= CAST(@dsXml AS VARCHAR(MAX)), @strUsercode VARCHAR(20)='C'
  SELECT @strUserid = ParameterValue From @tbl_InputJSONTable where ParameterName = 'UserId'
   
  IF @StrClientCode <> ''
  BEGIN
    SET @strUserid = @StrClientCode
  END
  
  SET @strHeaderString = 'DECLARE @tbl_UserList dbo.UserAccessList; '
  +' INSERT INTO @tbl_UserList '
  +' EXEC dbo.stpr_GetClientAccessList '''+@strUserid+''''
  
  DECLARE @strModuleName VARCHAR(100)='', @strOption VARCHAR(50)='', @strQuery NVARCHAR(MAX), @strQueryType VARCHAR(1)='Q',
  @o_vcFlag VARCHAR(1)='S', @o_vcMessage VARCHAR(MAX)=''
  SELECT @strModuleName = ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'ActionName'
  SELECT @strOption = ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'Option'
  IF ISNULL(@strModuleName,'') <> ''
  BEGIN
    
    SELECT @strQuery = DBQuery, @strQueryType = DBQueryType, @strUsercode  = Usercode  
	FROM tbl_InsertUpdateXMLDropDownQuery(NOLOCK) 
	WHERE ModuleName = @strModuleName AND ColumnName = @strOption
	
	IF ISNULL(@strQuery,'') <> '' AND ISNULL(@strQueryType,'Q') = 'Q'
	BEGIN
	  IF @strUserid <> '' AND UPPER(@strQuery) LIKE '%CLIENT_MASTER%' AND ISNULL(@strUsercode,'C') <> 'B'
	  BEGIN
	    SET @strString = @strHeaderString+' '+@strQuery 
	    SET @strString = @strString+' '+ ' AND EXISTS(SELECT 1 FROM @tbl_UserList WHERE ClientCode = CM_CD) '
	  END
	  ELSE
	  BEGIN
	    SET @strString = @strQuery 
	  END
      
	 
	  --REPLACE CHANGES
	  IF CHARINDEX('<<FILTER>>', @strString) > 0 
	  BEGIN
		SET @strString = REPLACE(@strString, '<<FILTER>>', (SELECT TOP 1 ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'X_Filter' 
		and HeaderName = 'X_Filter'))
	  END
	  
	  --END REPLACE CHANGES
	 -- SELECT * FROM @tbl_InputJSONTable
	  DECLARE @curParameterValue VARCHAR(MAX)='', @curParameterName VARCHAR(50)=''
      IF EXISTS(SELECT 1 FROM @tbl_InputJSONTable 
	  WHERE HeaderName = 'X_Filter_Multiple' AND ISNULL(ParameterValue,'') <> '')
	  BEGIN
	    DECLARE Cur12 CURSOR FOR 
        SELECT ParameterValue, ParameterName FROM @tbl_InputJSONTable 
	    WHERE HeaderName = 'X_Filter_Multiple' AND ISNULL(ParameterValue,'') <> ''
        OPEN Cur12 
        FETCH NEXT FROM Cur12 INTO @curParameterValue, @curParameterName
        WHILE @@FETCH_STATUS = 0
        BEGIN 
		  SET @strString = REPLACE(@strString,'<<'+@curParameterName+'>>',@curParameterValue)
		  FETCH NEXT FROM Cur12 INTO @curParameterValue, @curParameterName
        END 
        CLOSE Cur12 
        DEALLOCATE Cur12 
	  END
	--  select @strString
	  EXEC(@strString)
	END
	ELSE IF ISNULL(@strQuery,'') <> '' AND ISNULL(@strQueryType,'Q') = 'P'
	BEGIN
	   SET @strString = 'EXEC DBO.' + @strQuery + ' ''' + @strxml + ''', @o_vcFlag OUTPUT, @o_vcMessage OUTPUT';
	   BEGIN TRY
	     EXEC sp_executesql @strString, N'@o_vcFlag VARCHAR(1) OUTPUT, @o_vcMessage VARCHAR(MAX) OUTPUT', @o_vcFlag OUTPUT, @o_vcMessage OUTPUT;
	   END TRY
       BEGIN CATCH
	      SELECT '<Flag>E</Flag><Message>'+ERROR_MESSAGE()+'</Message>'
		  RETURN 1
       END CATCH	   
	   SELECT '<Flag>'+@o_vcFlag+'</Flag>'+@o_vcMessage
	   RETURN 1
	END
  END
END
GO

CREATE PROCEDURE stpr_MenuProcedure @dsXml VARCHAR(MAX)  
WITH ENCRYPTION 
AS
BEGIN 
  DECLARE @tbl_InputJSONTable DBO.tb_ParamList ;
  DECLARE @o_ParameterList varchar(max)='', @o_ParameterListxml XML; 
  DECLARE @tbl_UserList dbo.UserAccessList;
  
  EXEC SP_ParameterXMLRep @dsXml, @o_ParameterList OUTPUT
  
  IF ISNULL(@o_ParameterList,'') <> ''
  BEGIN
	SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)
    INSERT INTO @tbl_InputJSONTable (ParameterName,  ParameterValue, HeaderName, Jsontag) 
    SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code ,
    Parameter.value('(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue,
	Parameter.value('(MasterTag)[1]', 'VARCHAR(MAX)') AS MasterTag,
	Parameter.value('(JsonLevel)[1]', 'VARCHAR(MAX)') AS JsonLevel
    FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
  END	  
  
  DECLARE @StrRequestFrom VARCHAR(1)='M', @strJson VARCHAR(MAX)='', @strUserid VARCHAR(50)='', @strUserType VARCHAR(50)='',
  @o_strJson2 VARCHAR(MAX)
  DECLARE @TBL_json TABLE(SerialNo int, Jsonvalue VARCHAR(MAX), ParentName VARCHAR(MAX), TargetForm VARCHAR(50), MenuNameP VARCHAR(100),
  PMenuCode VARCHAR(50))

  SELECT @strRequestFrom = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'RequestFrom'
  SELECT @strUserid = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Userid'
  
  
  SELECT @strUserType = ParameterValue  FROM @tbl_InputJSONTable 
   WHERE ParameterName = 'Selection' AND HeaderName = 'X_Filter'
   
   IF ISNULL(@strUserType,'') = ''
   BEGIN
     SELECT TOP 1 @strUserType = ParameterValue  FROM @tbl_InputJSONTable 
     WHERE ParameterName = 'UserType'
   END
     
  IF @strUserType = 'branch'
  BEGIN
    SET @strUserType = 'Branch'
  END 
  ELSE IF @strUserType = 'User'
  BEGIN
    SET @strUserType = 'Client'
  END 
  
  IF ISNULL(@strUserType,'') = ''
  BEGIN
    SET @strUserType = 'Client'
  END
    
  
  INSERT INTO @TBL_json(SerialNo, Jsonvalue, ParentName, TargetForm, MenuNameP, PMenuCode)
  SELECT SrNo, Jsonvalue = (SELECT SrNo, SrNo as [id], [title] = MenuName,
  [componentName] = Menutag, [componentType] = CASE WHEN ISNULL(ICONPATH,'') = 'E' THEN 'entry' 
  when ISNULL(ICONPATH,'') = 'M' THEN 'multientry' else 'report' end,
  [icon] = '',
  [iconType] = '' ,
   [pageData] = (SELECT [wPage] = Menutag, [level] = CASE WHEN ISNULL(ParentMenu,'') = 'Reports' then MenuName else MenuName+'('+ParentMenu+')' end,
   [isShortAble] = (CASE WHEN MenuName = 'Ledger' then 'false' else 'true' end),
   [gridType] = 'card', 
   [horizontalScroll] = 0,
   [autoFetch] = (CASE WHEN ISNULL(ICONPATH,'') = 'E' then 'false' else 'true' end),
   filters = '##filters##', --downloadFilters = '##downloadFilters##' ,
   [Entry] = '##Entry##',
   levels = (SELECT * FROM(SELECT name = MenuName, primaryHeaderKey, [primaryKey] = primaryHeaderKey, level = 1, 
   [summary] = '##summary##',
   J_Ui = '##J_Ui##',
   [settings] = '##settings##'
   FROM tbl_CrossNetMenu(NOLOCK) XMM WHERE XMM.SrNo = XMAIN.SrNo 
   UNION ALL
   SELECT name = 'Details', primaryHeaderKey, [primaryKey] = '', level = 2, [summary] = '[]', J_Ui = '##J_Ui2##',
   [settings] = '##settings2##'
   FROM tbl_CrossNetMenu(NOLOCK) XMM WHERE XMM.SrNo = XMAIN.SrNo) X1 FOR JSON PATH)
   FROM tbl_CrossNetMenu(NOLOCK) XM WHERE XM.SrNo = XMAIN.SrNo FOR JSON PATH)
   FROM tbl_CrossNetMenu(NOLOCK) XMAIN
   WHERE XMAIN.SrNo = XXX.SrNo FOR JSON PATH), ParentMenu , ISNULL(TargetForm,''), ISNULL(MENUTAG,''), 
   ISNULL(MENUCode,'')  
   FROM tbl_CrossNetMenu(NOLOCK) XXX
   WHERE MenuType = 'C' /*AND ((CASE WHEN ISNULL(@strRequestFrom,'') = 'W' THEN  'T' ELSE @strRequestFrom END) = Productid or isnull(Productid,'') = '')
   and*/AND  ISNULL(enable,'Y') ='Y'
   AND MenuName NOT IN('DashBoard')
   
   
   IF @strUserType = 'Client'
   BEGIN
     UPDATE X SET X.Jsonvalue = REPLACE(REPLACE(REPLACE(X.Jsonvalue,'"##filters##"',CASE WHEN ISNULL(XX.filters,'') = '' 
     THEN '[]' ELSE '['+ISNULL(XX.filters,'')+']'  END),
    '##UserId##',@strUserid),'##UserType##',@strUserType)
     FROM @TBL_json X, tbl_CrossNetMenu xx
     WHERE x.SerialNo = xx.SrNo
     AND ISNULL(XX.filters,'') <> ''
  END
  ELSE IF @strUserType <> 'Client'
  BEGIN
  

	UPDATE X SET X.Jsonvalue = REPLACE(REPLACE(REPLACE(X.Jsonvalue,'"##filters##"',CASE WHEN ISNULL(XX.filters,'') = '' 
    THEN '[]' ELSE '['+ISNULL(XX.filters,'')+']'  END),
    '##UserId##',@strUserid),'##UserType##',@strUserType)
    FROM @TBL_json X, tbl_CrossNetMenu xx
    WHERE x.SerialNo = xx.SrNo
    AND ISNULL(XX.filters,'') <> ''
    
  END	
  
   

  UPDATE X SET X.Jsonvalue = REPLACE(X.Jsonvalue,'"##filters##"',CASE WHEN ISNULL(XX.filters,'') = '' THEN '[]' ELSE '['+ISNULL(XX.filters,'')+']'  END)
  FROM @TBL_json X, tbl_CrossNetMenu xx
  WHERE x.SerialNo = xx.SrNo
  
  DECLARE @InputDate DATE = ISNULL((SELECT CAST(MAX(hld_hold_date) AS DATE) FROM Holding(NOLOCK)),GETDATE()), 
  @FinancialYearStart DATE=''
  SET @FinancialYearStart = CASE WHEN MONTH(@InputDate) >= 4 THEN DATEFROMPARTS(YEAR(@InputDate), 4, 1) 
                            ELSE DATEFROMPARTS(YEAR(@InputDate) - 1, 4, 1) END;
  
  
  UPDATE X SET X.Jsonvalue = REPLACE(X.Jsonvalue,'##FromDate##',CONVERT(VARCHAR,@FinancialYearStart,112))
  FROM @TBL_json X
  
  UPDATE X SET X.Jsonvalue = REPLACE(X.Jsonvalue,'##7FromDate##',CONVERT(VARCHAR,DATEADD(DAY,-7,@InputDate),112))
  FROM @TBL_json X
  
  UPDATE X SET X.Jsonvalue = REPLACE(X.Jsonvalue,'##ToDate##',CONVERT(VARCHAR,@InputDate,112))
  FROM @TBL_json X
    
  UPDATE X SET X.Jsonvalue = REPLACE(X.Jsonvalue,'"##summary##"',CASE WHEN ISNULL(XX.downloadFilters,'') = '' THEN '{}' ELSE ISNULL(XX.downloadFilters,'')  END)
  FROM @TBL_json X, tbl_CrossNetMenu xx
  WHERE x.SerialNo = xx.SrNo
  AND ISNULL(XX.IconPath,'') NOT IN( 'E','M')
        

  
  UPDATE X SET X.Jsonvalue = REPLACE(X.Jsonvalue,'"##J_Ui##"','{"ActionName":"CrossNet","Option":"'+xx.menutag+'","Level":1, "RequestFrom":"'+@strRequestFrom+'"}')
  FROM @TBL_json X, tbl_CrossNetMenu xx
  WHERE x.SerialNo = xx.SrNo
  
  UPDATE X SET X.Jsonvalue = REPLACE(X.Jsonvalue,'"##J_Ui2##"','{"ActionName":"CrossNet","Option":"'+xx.menutag+'","Level":2, "RequestFrom":"'+@strRequestFrom+'"}')
  FROM @TBL_json X, tbl_CrossNetMenu xx
  WHERE x.SerialNo = xx.SrNo

  UPDATE X SET X.Jsonvalue = REPLACE(X.Jsonvalue,'"##settings##"',CASE WHEN ISNULL(XX.LevelSetting,'') = '' THEN '{}' ELSE ISNULL(XX.LevelSetting,'')  END)
  FROM @TBL_json X, tbl_CrossNetMenu xx
  WHERE x.SerialNo = xx.SrNo

  UPDATE X SET X.Jsonvalue = REPLACE(X.Jsonvalue,'"##settings2##"',CASE WHEN ISNULL(XX.Level2Setting,'') = '' THEN '{}' ELSE ISNULL(XX.Level2Setting,'')  END)
  FROM @TBL_json X, tbl_CrossNetMenu xx
  WHERE x.SerialNo = xx.SrNo


  DECLARE @tbl_Data TABLE([Value] VARCHAR(MAX), [DisplayName] VARCHAR(100))
  declare @iSerialNo INT, @strdbquery VARCHAR(MAX) ='', @strcolumnname VARCHAR(100)='',
  @ojson VARCHAR(MAX)='', @JsonMain VARCHAR(MAX)='', @o_strJson  VARCHAR(MAX)='',
  @strLedgerJson VARCHAR(MAX)='', @strString NVARCHAR(MAX)='', @strtradeplustempdb VARCHAR(MAX)=ISNULL((SELECT strtradeplustempdb = sp_sysvalue
  FROM WebParameter(NOLOCK)
  WHERE sp_parmcd = 'TRADEPLUSTEMPDB'),'')
  
  DECLARE CurfilterUpd
  CURSOR FOR SELECT  x.SerialNo, dbquery, columnname
  FROM @TBL_json X, tbl_CrossNetMenu xx, tbl_InsertUpdateXMLDropDownQuery xq
  WHERE x.SerialNo = xx.SrNo
  and xx.MenuTag = xq.ModuleName AND USERCODE <> 'X'
  and charindex('##'+columnname+'##',xx.filters)>0 AND DBQueryType ='Q'
  AND dbquery <> ''
  order by xq.SerialNo
  
  OPEN CurfilterUpd 
  FETCH NEXT FROM CurfilterUpd INTO @iSerialNo, @strdbquery, @strcolumnname
  WHILE @@FETCH_STATUS = 0
  BEGIN 
    SET @strLedgerJson = ''
    DELETE FROM @tbl_Data
    INSERT INTO @tbl_Data([Value], [DisplayName])
    EXEC(@strdbquery)
    SET @ojson = ''
    IF EXISTS(SELECT 1 FROM @tbl_Data)
    BEGIN
      SET @ojson = (SELECT [label] = [DisplayName], [Value]  FROM @tbl_Data FOR JSON PATH) 
    END
    ELSE
    BEGIN
      SET @ojson = (SELECT [label] = '', [Value] ='' FOR JSON PATH) 
    END
    UPDATE A SET A.Jsonvalue = REPLACE(A.Jsonvalue, '"##'+@strcolumnname+'##"',@ojson)
    FROM @TBL_json A
    WHERE A.SerialNo = @iSerialNo
	
	--SELECT @o_strJson
    FETCH NEXT FROM CurfilterUpd INTO   @iSerialNo, @strdbquery, @strcolumnname
  END
  CLOSE CurfilterUpd
  DEALLOCATE CurfilterUpd
  
  DECLARE CurEditUpd
  CURSOR FOR SELECT  x.SerialNo, dbquery, columnname
  FROM @TBL_json X, tbl_CrossNetMenu(NOLOCK) xx, tbl_InsertUpdateXMLDropDownQuery(NOLOCK) xq
  WHERE x.SerialNo = xx.SrNo
  and xx.MenuTag = xq.ModuleName AND USERCODE <> 'X'
  and charindex('~~'+columnname+'~~',xx.LevelSetting)>0 AND DBQueryType ='Q'
  and charindex('EditableColumn',xx.LevelSetting) > 0 AND dbquery <> ''
  order by xq.SerialNo
  
  OPEN CurEditUpd 
  FETCH NEXT FROM CurEditUpd INTO @iSerialNo, @strdbquery, @strcolumnname
  WHILE @@FETCH_STATUS = 0
  BEGIN 
    SET @strLedgerJson = ''
    DELETE FROM @tbl_Data
    INSERT INTO @tbl_Data([Value], [DisplayName])
    EXEC(@strdbquery)
	
    SET @ojson = ''
    IF EXISTS(SELECT 1 FROM @tbl_Data)
    BEGIN
      SET @ojson = (SELECT [label] = [DisplayName], [Value]  FROM @tbl_Data FOR JSON PATH) 
    END
    ELSE
    BEGIN
      SET @ojson = (SELECT [label] = '', [Value] ='' FOR JSON PATH) 
    END
    UPDATE A SET A.Jsonvalue = REPLACE(A.Jsonvalue, '"~~'+@strcolumnname+'~~"',@ojson)
    FROM @TBL_json A
    WHERE A.SerialNo = @iSerialNo
	
	--SELECT @ojson, @strcolumnname, @strdbquery
    FETCH NEXT FROM CurEditUpd INTO   @iSerialNo, @strdbquery, @strcolumnname
  END
  CLOSE CurEditUpd
  DEALLOCATE CurEditUpd
  
  
  DECLARE CurfilterUpd2
  CURSOR FOR SELECT  x.SerialNo, dbquery, columnname
  FROM @TBL_json X, tbl_CrossNetMenu xx, tbl_InsertUpdateXMLDropDownQuery xq
  WHERE x.SerialNo = xx.SrNo
  and xx.MenuTag = xq.ModuleName AND USERCODE <> 'X'
  and charindex('##'+columnname+'##',xx.LevelSetting)>0 AND DBQueryType ='Q'
  AND dbquery <> ''
  order by xq.SerialNo
  
  OPEN CurfilterUpd2 
  FETCH NEXT FROM CurfilterUpd2 INTO @iSerialNo, @strdbquery, @strcolumnname
  WHILE @@FETCH_STATUS = 0
  BEGIN 
    SET @strLedgerJson = ''
    DELETE FROM @tbl_Data
    INSERT INTO @tbl_Data([Value], [DisplayName])
    EXEC(@strdbquery)
    SET @ojson = ''
    IF EXISTS(SELECT 1 FROM @tbl_Data)
    BEGIN
      SET @ojson = (SELECT [label] = [DisplayName], [Value]  FROM @tbl_Data FOR JSON PATH) 
    END
    ELSE
    BEGIN
      SET @ojson = (SELECT [label] = '', [Value] ='' FOR JSON PATH) 
    END
    UPDATE A SET A.Jsonvalue = REPLACE(A.Jsonvalue, '"##'+@strcolumnname+'##"',@ojson)
    FROM @TBL_json A
    WHERE A.SerialNo = @iSerialNo
	
	--SELECT @o_strJson
    FETCH NEXT FROM CurfilterUpd2 INTO   @iSerialNo, @strdbquery, @strcolumnname
  END
  CLOSE CurfilterUpd2
  DEALLOCATE CurfilterUpd2
  
  
  UPDATE X SET X.Jsonvalue = REPLACE(X.Jsonvalue,'"##Entry##"',CASE WHEN ISNULL(XX.downloadFilters,'') = '' THEN '{}' ELSE ISNULL(XX.downloadFilters,'') END)
  FROM @TBL_json X, tbl_CrossNetMenu xx
  WHERE x.SerialNo = xx.SrNo
  AND ISNULL(XX.IconPath,'') IN('E','M')
  
  UPDATE X SET X.Jsonvalue = REPLACE(X.Jsonvalue,'"##Entry##"','{}')
  FROM @TBL_json X, tbl_CrossNetMenu xx
  WHERE x.SerialNo = xx.SrNo
  AND ISNULL(XX.IconPath,'') NOT IN('E','M')
  
  declare @strClientJson varchar(max)=''
  
  IF EXISTS(SELECT 1 FROM @TBL_json WHERE CHARINDEX('##Selectioncatogery##',Jsonvalue) > 0)
  BEGIN
    
	SET @strClientJson = (select [label] =  [DisplayName], [Value] from(select 'Client' as [Value], 'BO ID' as [DisplayName] union all select 'Group' as [Value], 'Group' as [DisplayName] 
	union all select 'Family' as [Value], 'Family' as [DisplayName] union all select 'Branch' as [Value], 'Branch' as [DisplayName] 
	union all select 'Scheme' as [Value], 'Scheme' as [DisplayName] union all select 'Backofficecd' as [Value], 'BackOffice CD' as [DisplayName] ) x1 
	FOR JSON PATH) 
	
    UPDATE A SET A.Jsonvalue = REPLACE(A.Jsonvalue, '"##Selectioncatogery##"',@strClientJson)
	FROM @TBL_json A
  END
  
  UPDATE A SET A.Jsonvalue = REPLACE(A.Jsonvalue, '##UserId##',@strUserid)
  FROM @TBL_json A

  
  /*
  DECLARE CurfilterUpd0
  CURSOR FOR SELECT DISTINCT Menuname from tbl_CrossNetMenu(NOLOCK)
  WHERE MenuType = 'P' AND ((CASE WHEN ISNULL(@strRequestFrom,'') = 'W' THEN  'T' 
  ELSE @strRequestFrom END) = Productid or isnull(Productid,'') = '')
  OPEN CurfilterUpd0 
  FETCH NEXT FROM CurfilterUpd0 INTO @strMenuname
  WHILE @@FETCH_STATUS = 0
  BEGIN 
    SET @icount = @icount+1
    SET @o_strJson = '{"id": ##ID##,"title": "##title##",
        "componentName": "##title##",
        "icon": "##icon##","iconType":"##iconType##","submenu":[ '
    SET @o_strJson = REPLACE(@o_strJson,'##ID##',CAST(@icount AS VARCHAR))
	SET @o_strJson = REPLACE(@o_strJson,'##title##',@strMenuname)
	
	IF 	@strMenuname = 'Capital Gain'
	BEGIN
	  SET @o_strJson = REPLACE(@o_strJson,'##iconType##','entypo')
	  SET @o_strJson = REPLACE(@o_strJson,'##icon##','area-graph')
	END
	ELSE IF @strMenuname = 'Reports'
	BEGIN
	  SET @o_strJson = REPLACE(@o_strJson,'##iconType##','octicons')
	  SET @o_strJson = REPLACE(@o_strJson,'##icon##','report')
	END
	
    
    DECLARE CurfilterUpd1
    CURSOR FOR SELECT Jsonvalue FROM @TBL_json 
    WHERE  ParentName = @strMenuname
    ORDER BY SERIALNO
  
    OPEN CurfilterUpd1 
    FETCH NEXT FROM CurfilterUpd1 INTO @strJsonvalue
    WHILE @@FETCH_STATUS = 0
    BEGIN 
      IF ISNULL(@o_strJson,'') <> ''
	  BEGIN
	  --select @o_strJson, @strJsonvalue
	    delete from @tbl_fn_json_merge
	    SET @strString = 'SELECT '+@strtradeplustempdb+'.DBO.fn_json_merge('''+@o_strJson+''','''+@strJsonvalue+''') '
        INSERT INTO @tbl_fn_json_merge(JsonValue)
        EXEC(@strString)
	    select @o_strJson = JsonValue from @tbl_fn_json_merge
	    SET @o_strJson = substring(@o_strJson,2,LEN(@o_strJson))
	    SET @o_strJson = substring(@o_strJson,1,LEN(@o_strJson)-1)
	    set @o_strJson = replace(@o_strJson,'[ ,{','[{')
	  END
      ELSE
      BEGIN
	    SET @o_strJson = @strJsonvalue
	  END
	--SELECT @o_strJson, @strJsonvalue
      FETCH NEXT FROM CurfilterUpd1 INTO   @strJsonvalue
    END
    CLOSE CurfilterUpd1
    DEALLOCATE CurfilterUpd1
    IF ISNULL(@o_strJson1,'') = ''
    BEGIN
      SET @o_strJson1 = @o_strJson+']}'  
    END
    ELSE
    BEGIN  
      SET @o_strJson1 = @o_strJson1+','+@o_strJson+']}'
    END	
    FETCH NEXT FROM CurfilterUpd0 INTO   @strMenuname
  END
  CLOSE CurfilterUpd0
  DEALLOCATE CurfilterUpd0
  */
  
  DECLARE @tbl_fn_json_merge table(JsonValue varchar(max))

  DECLARE @strJsonvalue varchar(max)='', @strMenuname VARCHAR(MAX)='', @icount INT = 1,
  @o_strJson1 VARCHAR(MAX)='', @strSubMenuName VARCHAR(MAX)='', @icount1 INT = 1

  IF (@strRequestFrom = 'M' OR (@strRequestFrom = 'W' AND @strUserType = 'CLIENT'))
  BEGIN
    DECLARE CurfilterUpd0
    CURSOR FOR SELECT Menuname from tbl_CrossNetMenu(NOLOCK)
    WHERE MenuType = 'P' AND ISNULL(Enable,'N') = 'Y' 
	AND ((ISNULL(@strRequestFrom,'') = 'W' AND ISNULL(Productid,'') IN('','T','B'))
	  OR (ISNULL(@strRequestFrom,'') = 'M' AND ISNULL(Productid,'') IN('','M')))
    AND ((@strUserType = 'Client' AND ISNULL(ICONPATH,'') = '') OR @strUserType <> 'Client')
	AND Menuname IN(SELECT DISTINCT TargetForm FROM tbl_CrossNetMenu WHERE ISNULL(Enable,'N') = 'Y' 
	AND ISNULL(Productid,'') IN('','T','B')
	AND ISNULL(MENUType,'') = 'C' )
	Order by MenuCode
	
    OPEN CurfilterUpd0 
    FETCH NEXT FROM CurfilterUpd0 INTO @strMenuname
    WHILE @@FETCH_STATUS = 0
    BEGIN 
      SET @icount = @icount+1
      SET @o_strJson = '{"id": ##ID##,"title": "##title##",
        "componentName": "##title##",
        "icon": "##icon##","iconType":"##iconType##","submenu":[ '
      SET @o_strJson = REPLACE(@o_strJson,'##ID##',CAST(@icount AS VARCHAR))
	  SET @o_strJson = REPLACE(@o_strJson,'##title##',@strMenuname)
	
	  IF @strMenuname = 'Capital Gain'
	  BEGIN
	    SET @o_strJson = REPLACE(@o_strJson,'##iconType##','entypo')
	    SET @o_strJson = REPLACE(@o_strJson,'##icon##','area-graph')
	  END
	  ELSE IF @strMenuname = 'Reports'
	  BEGIN
	    SET @o_strJson = REPLACE(@o_strJson,'##iconType##','octicons')
	    SET @o_strJson = REPLACE(@o_strJson,'##icon##','report')
	  END
	  
      DECLARE CurfilterUpd1
      CURSOR FOR SELECT Jsonvalue FROM @TBL_json,  tbl_CrossNetMenu(NOLOCK) XX
      WHERE  SerialNo = SrNo
	  AND ISNULL(Enable,'N') = 'Y' 
	  AND ((ISNULL(@strRequestFrom,'') = 'W' AND ISNULL(Productid,'') IN('','T','B'))
	  OR (ISNULL(@strRequestFrom,'') = 'M' AND ISNULL(Productid,'') IN('','M')))
	  AND ISNULL(MENUType,'') = 'C' 
      AND XX.TargetForm = @strMenuname
      ORDER BY SERIALNO
  
      OPEN CurfilterUpd1 
      FETCH NEXT FROM CurfilterUpd1 INTO @strJsonvalue
      WHILE @@FETCH_STATUS = 0
      BEGIN 
        IF ISNULL(@o_strJson,'') <> ''
	    BEGIN
	      --select @o_strJson, @strJsonvalue
	      DELETE FROM @tbl_fn_json_merge
		  
		  --SET @strString = 'EXEC '+@strtradeplustempdb+'.DBO.' + 'SP_json_merge' + ' ''' + @o_strJson + ''', '''+@strJsonvalue+''', @o_strJson OUTPUT';
		  --SELECT @strString
          --EXEC sp_executesql @strString, N'@o_strJson VARCHAR(MAX) OUTPUT', @o_strJson OUTPUT;
	      SET @strString = 'SELECT '+@strtradeplustempdb+'.DBO.fn_json_merge1('''+@o_strJson+''','''+@strJsonvalue+''') '
		  INSERT INTO @tbl_fn_json_merge(JsonValue)
          EXEC(@strString)
		  select @o_strJson = JsonValue from @tbl_fn_json_merge
	      SET @o_strJson = substring(@o_strJson,2,LEN(@o_strJson))
	      SET @o_strJson = substring(@o_strJson,1,LEN(@o_strJson)-1)
	      SET @o_strJson = replace(@o_strJson,'[ ,{','[{')
	    END
        ELSE
        BEGIN
	      SET @o_strJson = @strJsonvalue
	    END
	    --SELECT @o_strJson, @strJsonvalue
        FETCH NEXT FROM CurfilterUpd1 INTO   @strJsonvalue
      END
      CLOSE CurfilterUpd1
      DEALLOCATE CurfilterUpd1
	  
	  
      IF ISNULL(@o_strJson2,'') = ''
      BEGIN
        SET @o_strJson2 = @o_strJson+']}'  
      END
      ELSE
      BEGIN  
        SET @o_strJson2 = @o_strJson2+','+@o_strJson+']}'
      END	
      FETCH NEXT FROM CurfilterUpd0 INTO   @strMenuname
    END
    CLOSE CurfilterUpd0
    DEALLOCATE CurfilterUpd0
  END
  ELSE
  BEGIN
    DECLARE CurfilterUpd2
    CURSOR FOR SELECT Menuname from tbl_CrossNetMenu(NOLOCK)
    WHERE MenuType = 'P' AND ISNULL(Enable,'N') = 'Y' AND isnull(Productid,'') IN('','N','B')
	Order by MenuCode
	
    OPEN CurfilterUpd2 
    FETCH NEXT FROM CurfilterUpd2 INTO @strMenuname
    WHILE @@FETCH_STATUS = 0
    BEGIN 
	  SET @o_strJson1 = ''
      SET @icount = @icount+1
      SET @o_strJson = '{"id": ##ID##,"title": "##title##",
        "componentName": "##title##",
        "icon": "##icon##","iconType":"##iconType##" '
      SET @o_strJson = REPLACE(@o_strJson,'##ID##',CAST(@icount AS VARCHAR))
	  SET @o_strJson = REPLACE(@o_strJson,'##title##',@strMenuname)
	
	  IF @strMenuname = 'Capital Gain'
	  BEGIN
	    SET @o_strJson = REPLACE(@o_strJson,'##iconType##','entypo')
	    SET @o_strJson = REPLACE(@o_strJson,'##icon##','area-graph')
	  END
	  ELSE IF @strMenuname = 'Reports'
	  BEGIN
	    SET @o_strJson = REPLACE(@o_strJson,'##iconType##','octicons')
	    SET @o_strJson = REPLACE(@o_strJson,'##icon##','report')
	  END
      SET @icount1 = 1
      IF EXISTS(SELECT Menuname from tbl_CrossNetMenu(NOLOCK)
      WHERE  ParentMenu = @strMenuname AND ISNULL(MENUType,'') = 'S'
	  AND ISNULL(Enable,'N') = 'Y' AND isnull(Productid,'') IN('','N','B'))
	  BEGIN
        DECLARE CurfilterUpd3
        CURSOR FOR SELECT Menuname from tbl_CrossNetMenu(NOLOCK)
        WHERE  ParentMenu = @strMenuname AND ISNULL(MENUType,'') = 'S'
	    AND ISNULL(Enable,'N') = 'Y' AND isnull(Productid,'') IN('','N','B')
		Order by MenuCode
	  --AND Menuname='Accounts'
      
	    OPEN CurfilterUpd3 
        FETCH NEXT FROM CurfilterUpd3 INTO @strSubMenuName
        WHILE @@FETCH_STATUS = 0
        BEGIN 
          
		  IF @icount1 = 1
          BEGIN
            SET @o_strJson = @o_strJson+',"submenu":[{"id": ##ID##,"title": "##title##",
            "componentName": "##title##",
            "icon": "##icon##","iconType":"##iconType##","submenu":[ '
          END
          ELSE
          BEGIN  
            SET @o_strJson = @o_strJson+']},{"id": ##ID##,"title": "##title##",
            "componentName": "##title##",
            "icon": "##icon##","iconType":"##iconType##","submenu":[ '
          END
		  
		  SET @icount1 = @icount1+1
		  
          SET @o_strJson = REPLACE(@o_strJson,'##ID##',CAST(@icount1 AS VARCHAR))
	      SET @o_strJson = REPLACE(@o_strJson,'##title##',@strSubMenuName)
		
          DECLARE CurfilterUpd4
          CURSOR FOR SELECT Jsonvalue FROM @TBL_json,  tbl_CrossNetMenu(NOLOCK)
          WHERE  ParentName = @strSubMenuName AND SerialNo = SrNo
		  AND ISNULL(Enable,'N') = 'Y' AND isnull(Productid,'') IN('','N','B')
		  AND ISNULL(MENUType,'') = 'C'
          ORDER BY SERIALNO
  
          OPEN CurfilterUpd4 
          FETCH NEXT FROM CurfilterUpd4 INTO @strJsonvalue
          WHILE @@FETCH_STATUS = 0
          BEGIN 
            IF ISNULL(@o_strJson,'') <> ''
	        BEGIN
	          delete from @tbl_fn_json_merge
	          SET @strString = 'SELECT '+@strtradeplustempdb+'.DBO.fn_json_merge1('''+@o_strJson+''','''+@strJsonvalue+''') '
              INSERT INTO @tbl_fn_json_merge(JsonValue)
              EXEC(@strString)
	          select @o_strJson = JsonValue from @tbl_fn_json_merge
	          SET @o_strJson = substring(@o_strJson,2,LEN(@o_strJson))
	          SET @o_strJson = substring(@o_strJson,1,LEN(@o_strJson)-1)
	          set @o_strJson = replace(@o_strJson,'[ ,{','[{')
	        END
            ELSE
            BEGIN
	          SET @o_strJson = @strJsonvalue
	        END
	      
            FETCH NEXT FROM CurfilterUpd4 INTO   @strJsonvalue
          END
          CLOSE CurfilterUpd4
          DEALLOCATE CurfilterUpd4
	      FETCH NEXT FROM CurfilterUpd3 INTO   @strSubMenuName
        END
        CLOSE CurfilterUpd3
        DEALLOCATE CurfilterUpd3
		IF ISNULL(@o_strJson1,'') = ''
        BEGIN
          SET @o_strJson1 = @o_strJson  
        END
        ELSE
        BEGIN  
          SET @o_strJson1 = @o_strJson1+','+@o_strJson
        END	
		IF @icount1 > 2
        BEGIN		
		  SET @o_strJson1 = @o_strJson1+']}]}'
		END
        ELSE
        BEGIN
		  SET @o_strJson1 = @o_strJson1+']}]}'
        END 		
	  END	
	  ELSE
      BEGIN
	    SET @o_strJson = @o_strJson+',"submenu":['
	    DECLARE CurfilterUpd5
        CURSOR FOR SELECT Jsonvalue FROM @TBL_json,  tbl_CrossNetMenu(NOLOCK)
        WHERE  ParentName = @strMenuname AND SerialNo = SrNo
		AND ISNULL(Enable,'N') = 'Y' AND isnull(Productid,'') IN('','N','B')
		AND ISNULL(MENUType,'') = 'C'
        ORDER BY SERIALNO
  
        OPEN CurfilterUpd5 
        FETCH NEXT FROM CurfilterUpd5 INTO @strJsonvalue
        WHILE @@FETCH_STATUS = 0
        BEGIN 
          IF ISNULL(@o_strJson,'') <> ''
	      BEGIN
	        delete from @tbl_fn_json_merge
	        SET @strString = 'SELECT '+@strtradeplustempdb+'.DBO.fn_json_merge1('''+@o_strJson+''','''+@strJsonvalue+''') '
            INSERT INTO @tbl_fn_json_merge(JsonValue)
            EXEC(@strString)
	        select @o_strJson = JsonValue from @tbl_fn_json_merge
	        SET @o_strJson = substring(@o_strJson,2,LEN(@o_strJson))
	        SET @o_strJson = substring(@o_strJson,1,LEN(@o_strJson)-1)
	        set @o_strJson = replace(@o_strJson,'[ ,{','[{')
	      END
          ELSE
          BEGIN
	        SET @o_strJson = @strJsonvalue
	      END
	      
          FETCH NEXT FROM CurfilterUpd5 INTO   @strJsonvalue
        END
        CLOSE CurfilterUpd5
        DEALLOCATE CurfilterUpd5
		
		IF ISNULL(@o_strJson1,'') = ''
        BEGIN
          SET @o_strJson1 = @o_strJson  
        END
        ELSE
        BEGIN  
          SET @o_strJson1 = @o_strJson1+','+@o_strJson
        END	
		SET @o_strJson1 = @o_strJson1+']}'
	  END 	

      IF ISNULL(@o_strJson2,'') = ''
      BEGIN
        SET @o_strJson2 = @o_strJson1  
      END
      ELSE
      BEGIN  
        SET @o_strJson2 = @o_strJson2+','+@o_strJson1
      END	
      FETCH NEXT FROM CurfilterUpd2 INTO   @strMenuname
    END
    CLOSE CurfilterUpd2
    DEALLOCATE CurfilterUpd2
  END  
  SET @o_strJson2 = REPLACE(@o_strJson2,'[,','[')
  
  
  
  SELECT '[{
        "id": 1,
        "title": "Dashboard",
        "componentName": "Dashboard",
        "icon": "home"},'+@o_strJson2+',{
        "id": 20,
        "title": "Change Password",
        "componentName": "ChangePassword",
        "icon": "password",
		"iconType": "materialIcons"
    },
    {"id": 21,"title":"Downloads","componentName":"Downloads","icon":"download"},
	{"id": 22,"title":"Theme","componentName":"Theme","icon":"theme-light-dark"},
	{"id": 23,"title": "Logout","componentName": "Logout","icon": "logout"}]'
END
GO

CREATE PROCEDURE stpr_ValidateOffLineClient @i_vcinput XML, @o_vcFlag VARCHAR(1) OUTPUT, 
	@o_vcMessage VARCHAR(MAX) OUTPUT, @i_detailflag VARCHAR(1)='S'
WITH ENCRYPTION
AS
BEGIN
  DECLARE @tbl_InputJSONTable DBO.tb_ParamList;
  DECLARE @o_ParameterList VARCHAR(max) = '', @o_ParameterListxml XML;
  DECLARE @strModuleName VARCHAR(50) = '', @strOption VARCHAR(50) = '', @StrXML XML, @StrData1 VARCHAR(MAX)='' 
  DECLARE @ClientCd NVARCHAR(50), @ClientName NVARCHAR(100), @BackOfficeCd NVARCHAR(50), 
  @ClientType NVARCHAR(50), @BranchCd NVARCHAR(50), @LotNo BIGINT, @SlipMode NVARCHAR(50), 
  @ErrorMessage NVARCHAR(255)

  EXEC SP_ParameterXMLRep @i_vcinput, @o_ParameterList OUTPUT

  IF ISNULL(@o_ParameterList, '') <> ''
  BEGIN
	SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)
	INSERT INTO @tbl_InputJSONTable (ParameterName, ParameterValue, HeaderName, Jsontag)
    SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code, Parameter.value(
				'(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue, Parameter.value('(MasterTag)[1]', 
				'VARCHAR(MAX)') AS MasterTag, Parameter.value('(JsonLevel)[1]', 'VARCHAR(MAX)') AS JsonLevel
	FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
  END

  SELECT @strModuleName = ParameterValue
  FROM @tbl_InputJSONTable
  WHERE ParameterName = 'ActionName'

  SELECT @strOption = ParameterValue
  FROM @tbl_InputJSONTable
  WHERE ParameterName = 'Option'
  

  IF (@strModuleName IN ('ClientOffLine') AND (@strOption = 'GetFormNo' ))  
  BEGIN
	DECLARE @strClientFormNo VARCHAR(50)=''
	SELECT @strClientFormNo = ISNULL(MAX(CONVERT(numeric,ClientFormNo))+1,0) 
	FROM (SELECT cx_instrno AS ClientFormNo
	FROM Client_export(NOLOCK)
	UNION ALL
	SELECT  ClientFormNo FROM tbl_MultiEntryIncompleteMaster(NOLOCK)) X1
	
	SET @o_vcFlag = 'S'
    SET @o_vcMessage = '<Message></Message>'+'<ClientFormNo>'+@strClientFormNo+'</ClientFormNo>'
	RETURN 1
  END
  IF (@strModuleName IN ('ClientOffLine') AND (@strOption = 'ValidateSameAddress' ))  
  BEGIN
    DECLARE @strAddress1 VARCHAR(75), @strAddress2 VARCHAR(75),
	@strAddress3 VARCHAR(75), @strPinCode VARCHAR(75), @strState VARCHAR(75)
	, @strCountry VARCHAR(75), @strSameCorrAddress VARCHAR(20), @strCity VARCHAR(50)=''
    SELECT @strAddress1 = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'Address1'
	
	SELECT @strAddress2 = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'Address2'
	
	SELECT @strAddress3 = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'Address3'
	
	SELECT @strPinCode = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'PinCode'
	
	SELECT @strCity = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'City'
	
	SELECT @strState = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'State'
	
	SELECT @strCountry = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'Country'
	
	SELECT @strSameCorrAddress = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'SameCorrAddress'
    
	IF ISNULL(@strSameCorrAddress,'Y') IN('true','Y')
	BEGIN
	  SET @o_vcFlag = 'S'
      SET @o_vcMessage = '<Message></Message>'+'<PER_Address1>'+@strAddress1+'</PER_Address1><PER_Address2>'+@strAddress2+'</PER_Address2>'
	  +'<PER_Address3>'+@strAddress3+'</PER_Address3>'
	  +'<PER_Pincode>'+@strPinCode+'</PER_Pincode><PER_City>'+@strCity+'</PER_City><PER_State>'+@strState+'</PER_State><PER_Country>'+@strCountry+'</PER_Country>'
	  RETURN 1
	END
	ELSE 
	BEGIN
	 SET @o_vcFlag = 'D'
     SET @o_vcMessage = '<Message></Message>'+'<PER_Address1>true</PER_Address1><PER_Address2>true</PER_Address2>'
	  +'<PER_Address3>true</PER_Address3>'
	  +'<PER_Pincode>true</PER_Pincode><PER_City>true</PER_City><PER_State>true</PER_State><PER_Country>true</PER_Country>'
	  RETURN 1
	END
  END
  IF (@strModuleName IN ('ClientAuthorise') AND (@strOption = 'ValidateNewStatus' ))  
  BEGIN
	DECLARE @strNewStatus VARCHAR(50)='', @strOldStatus VARCHAR(50)=''
    SELECT @strNewStatus = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'NewStatus'
	
	SELECT @strOldStatus = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'Status'
	
	IF ISNULL(@strOldStatus,'') <> 'Sumbited'
	BEGIN
	  SET @o_vcFlag = 'E'
      SET @o_vcMessage = '<Message>Form No not in Sumbited State</Message>'
	  RETURN 1  
	END
	ELSE
	BEGIN
	  SET @o_vcFlag = 'S'
      SET @o_vcMessage = '<Message></Message>'
	  RETURN 1  
    END
  END
  ELSE IF (@strModuleName IN ('ClientOffLine') AND (@strOption = 'Validate_POAforPaying' ))  
  BEGIN
	DECLARE @strPOAforPaying VARCHAR(10)=''
	
	SELECT @strPOAforPaying = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'POAforPaying'
	
	IF ISNULL(@strPOAforPaying,'false') = 'true'
	BEGIN
	  SET @o_vcFlag = 'D'
      SET @o_vcMessage = '<Message></Message>'+'<POARegDate>true</POARegDate><SetupDate>true</SetupDate><Operate>true</Operate>'
	  +'<POARegNo>true</POARegNo><FromDate>true</FromDate><ToDate>true</ToDate>'
	  RETURN 1
	END
	else IF ISNULL(@strPOAforPaying,'false') = 'false'
	BEGIN
	  SET @o_vcFlag = 'D'
      SET @o_vcMessage = '<Message></Message>'+'<POARegDate>false</POARegDate><SetupDate>false</SetupDate><Operate>false</Operate>'
	  +'<POARegNo>false</POARegNo><FromDate>false</FromDate><ToDate>false</ToDate>'
	  RETURN 1
	END
  END
  ELSE IF (@strModuleName IN ('CROSSNET') AND (@strOption = 'TabChangeAPI' ))
  BEGIN
    DECLARE @strEntryName VARCHAR(50)=''
    SELECT @strEntryName = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'EntryName'
	IF @strEntryName = 'ClientOffLine'
	BEGIN
	  DECLARE @strAccountType VARCHAR(50)='', @strSubStatus1 varchar(100)='',
	  @strPMSDisable VARCHAR(10)='', @strNOMDisable VARCHAR(10)=''
	  SELECT @strAccountType = ltrim(rtrim(ParameterValue))
      FROM @tbl_InputJSONTable
      WHERE ParameterName = 'AccountType'
	  
	  SELECT @strSubStatus1 = ltrim(rtrim(ParameterValue))
      FROM @tbl_InputJSONTable
      WHERE ParameterName = 'SubStatus'
	  
	  IF NOT EXISTS(Select 1 From Beneficiary_Type, BeneficiaryType_Mapping where bm_Code=bt_code 
      and bt_description in('Individual Resident - PMS','Ind Resident-PMS Negative Nomination','Custodian NRI Repat PMS No Nomination','Custodian NRI Non-Repatriable - PMS',
      'Custodian NRI Non Repat PMS No Nomination','Corporate Body - Domestic - PMS','LLP - PMS','HUF - PMS','NRI Repatriable - PMS','NRI Non Repatriable - PMS','NRI Non-Repatriable - PMS Negative Nomination','NRI Repatriable - PMS Negative Nomination',
      'Custodian NRI Repatriable - PMS','Trust - PMS') and bm_Code = @strSubStatus1)
	  BEGIN
	    SET @strPMSDisable = 'false'
	  END
	  ELSE
	  BEGIN
	   SET @strPMSDisable = 'true'
	  END
	  
	  IF NOT EXISTS(Select 1 From Beneficiary_Type, BeneficiaryType_Mapping where bm_Code=bt_code 
      and bt_description in('Individual - Resident','Individual - Promoters','Resident Individual - Minor','Resident Individual - Depository Receipt',
      'Individual Resident - PMS','Ind Resident - Minor Negative Nomination','Individual Resident - Minor - Promoter','Individual - Client Collateral Account',
      'Individual -TM Client Securities Margin Pledge a/c','Individual -CM Client Securities Margin Pledge a/c','Individual - TM/CM CMPA',
      'Custodian Bank','Custodian FI-Govt Sponsored FI','Custodian FI-Others','Custodian Bank Foreign','Custodian Bank Others',
      'Custodian NRI Non-Repatriable - PMS','Custodian Corporate Body Others','Custodian Scheduled Commercial Banks','Custodian Corporate Body Domestic',
      'Custodian Alternate Investment Fund','Trusteeship Company','Local Authority','Individual TM CUSPA','Individual CM CUSPA','LLP TM CUSPA',
      'LLP CM CUSPA','Individual TM CUSPA Negative Nomination','Individual CM CUSPA Negative Nomination','Individual TM/CM CUSPA',
      'Individual TM/CM CUSPA Negative Nominatn','Individual TM Client Nodal MFOS','Individual CM Client Nodal MFOS','Individual TM/CM Client Nodal MFOS'))
	 BEGIN
	    SET @strNOMDisable = 'false'
	  END
	  ELSE
	  BEGIN
	   SET @strNOMDisable = 'true'
	  END
	  
	  IF @strAccountType = '1'
	  BEGIN
	    SET @o_vcFlag = 'T'
	    SET @o_vcMessage = '<Message></Message><AuthoriseSignatory>false</AuthoriseSignatory><PMSManager>'+@strPMSDisable+'</PMSManager><NomineeDetails>'+@strNOMDisable+'</NomineeDetails>'
	    RETURN 1
	  END
	  ELSE IF @strAccountType <> '1'
	  BEGIN
	    SET @o_vcFlag = 'T'
	    SET @o_vcMessage = '<Message></Message><OtherHolders>true</OtherHolders><AuthoriseSignatory>true</AuthoriseSignatory><PMSManager>'+@strPMSDisable+'</PMSManager><NomineeDetails>'+@strNOMDisable+'</NomineeDetails>'
	    RETURN 1
	  END
	END
  END
  ELSE IF (@strModuleName IN ('ClientOffLine') AND (@strOption = 'Validate_DemiseofFirstholder' ))  
  BEGIN
	DECLARE @strDemiseofFirstholder VARCHAR(10)=''
	
	SELECT @strDemiseofFirstholder = ParameterValue
    FROM @tbl_InputJSONTable
    WHERE ParameterName = 'DemiseofFirstholder'
	
	IF ISNULL(@strDemiseofFirstholder,'false') = 'true'
	BEGIN
	  SET @o_vcFlag = 'D'
      SET @o_vcMessage = '<Message></Message>'+'<BOId>true</BOId><DemiseDate>true</DemiseDate>'
	  RETURN 1
	END
	else IF ISNULL(@strPOAforPaying,'false') = 'false'
	BEGIN
	  SET @o_vcFlag = 'D'
      SET @o_vcMessage = '<Message></Message>'+'<BOId>false</BOId><DemiseDate>false</DemiseDate>'
	  RETURN 1
	END
  END

  IF @ErrorMessage <> ''
  BEGIN
	SET @o_vcFlag = 'E'
	SET @o_vcMessage = @ErrorMessage
  END
  ELSE
  BEGIN
	SET @o_vcFlag = 'S'
	SET @o_vcMessage = '<Message>Process Completed</Message>'
  END
  RETURN;
END
GO

CREATE PROCEDURE stpr_GenerateClientModifyData @strClientCode VARCHAR(16), @strTabName VARCHAR(50), @o_Json VARCHAR(MAX) OUTPUT WITH ENCRYPTION AS
BEGIN
  DECLARE @jsontext VARCHAR(MAX)=''
  IF @strTabName ='MASTER'
  BEGIN
    SET @jsontext =(SELECT cm_cd AS BOID, cm_active AS Status, cm_opendate AS ACOpenDate, cm_acc_closuredate AS ACCloseDate, 
	cb_boacopensrc AS Source, 
    LTRIM(RTRIM(cm_productcd)) AS ProductNo, cm_clienttype AS AcType, cm_brboffcode AS Branch, cb_remarks AS Remarks, cb_BonafideFlag AS BonafideFlag FROM CLIENT_MASTER(NOLOCK), Client_Backoffice(NOLOCK) 
    WHERE CM_CD = cb_cmcd AND CM_cD = @strClientCode FOR JSON PATH)
	SET @o_Json = @jsontext
	RETURN 1
  END
  IF @strTabName ='ClientDetails'
  BEGIN
    SET @jsontext =(select cm_name AS Name, cm_middlename AS MiddleName, cm_lastname AS LastSearchName, cm_title AS Title, 
    cb_fh_NameVerify AS NameVerificationFlag, cm_suffix AS Suffix, cb_fathername AS FathersSpouseName, cm_add1 AS Address1, cm_add2 AS Address2, cm_add3 AS Address3, 
    cm_city AS City, cm_pin AS Pincode, cm_state AS State, cm_country AS Country, cb_fh_addressverify AS AddressVerificationFlag, cm_phoneindicator AS PrimaryTeleIndicator, 
    cb_FillerX2 AS FamilyAccountFlag, cb_MobileISD AS PrimaryMobileISD, cm_tele1 AS PrimaryMobileNumber, cb_SmartIndicator AS SmartRegistrationIndicator, cb_fh_mobverify AS VerificationFlag,
    cm_phoneindicator2 AS SecondaryTelInd, cb_fh_emailverify AS EmailVerificationFlag, cb_MobileISD2 AS SecondaryTelephoneISD, cm_tele2  AS SecondaryTelephone, 
    cm_email AS SecondaryEmail, cm_tele3 AS AditionalTelephones, cm_fax AS FaxNo, cb_panno AS PANGIRNo, cb_fh_panverify AS PanVerified, cm_itcircle AS ITCircleWardDistrict, cb_UID1 AS UID, 
    cb_UIDVerifyFlag AS Verified, cm_poaregdate AS POARegistrationDate, cm_poaforpayin AS POAForPayin
    FROM CLIENT_MASTER(NOLOCK), Client_Backoffice(NOLOCK) 
    WHERE CM_CD = cb_cmcd AND CM_cD = @strClientCode
    FOR JSON PATH)
    SET @o_Json = @jsontext
    RETURN 1
  END
END
GO

CREATE PROCEDURE stpr_PopulateDataAPI @dsXml XML WITH ENCRYPTION AS
BEGIN
  DECLARE @tbl_InputJSONTable DBO.tb_ParamList ;
  DECLARE @o_ParameterList varchar(max)='', @o_ParameterListxml XML; 
  DECLARE @tbl_UserList dbo.UserAccessList;
  
  EXEC SP_ParameterXMLRep @dsXml, @o_ParameterList OUTPUT
  
  IF ISNULL(@o_ParameterList,'') <> ''
  BEGIN
    SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)
   
    INSERT INTO @tbl_InputJSONTable (ParameterName,  ParameterValue, HeaderName, Jsontag) 
    SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code ,
    Parameter.value('(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue,
	Parameter.value('(MasterTag)[1]', 'VARCHAR(MAX)') AS MasterTag,
	Parameter.value('(JsonLevel)[1]', 'VARCHAR(MAX)') AS JsonLevel
    FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
  END 
  
     --- USER ACCESS RIGHTS
	 
  DECLARE @strUserid VARCHAR(500)='', @StrClientCode VARCHAR(50)='', @strString NVARCHAR(MAX)='',
  @strHeaderString VARCHAR(MAX)='', @strxml VARCHAR(MAX)= CAST(@dsXml AS VARCHAR(MAX))
  SELECT @strUserid = ParameterValue From @tbl_InputJSONTable where ParameterName = 'UserId'
   
  IF @StrClientCode <> ''
  BEGIN
    SET @strUserid = @StrClientCode
  END
  
  SET @strHeaderString = 'DECLARE @tbl_UserList dbo.UserAccessList; '
  +' INSERT INTO @tbl_UserList '
  +' EXEC dbo.stpr_GetClientAccessList '''+@strUserid+''''
  
  DECLARE @strModuleName VARCHAR(100)='', @strOption VARCHAR(50)='', @strQuery NVARCHAR(MAX), @strQueryType VARCHAR(1)='Q',
  @o_vcFlag VARCHAR(1)='S', @o_vcMessage VARCHAR(MAX)='', @strEntryName varchar(100)=''
  
  Declare @String1 VARCHAR(MAX)='', @strmenutype1 VARCHAR(1)=''
  
  
  SELECT @strModuleName = ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'ActionName'
  SELECT @strOption = ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'Option'
  SELECT @strEntryName = ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'EntryName'
  
  IF ISNULL(@strEntryName,'') <> ''
  BEGIN
    SELECT @strmenutype1 = ICONPATH FROM tbl_CrossNetMenu(NOLOCK) WHERE MenuTag = @strEntryName
  END
  
  IF @strModuleName = 'Common' AND @strOption = 'DP'
  BEGIN
    DECLARE @strdpid VARCHAR(200)=''
    SELECT @strdpid = ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'X_Filter'
    SELECT bp_name from Bpmaster(noLock) 
    WHERE bp_role = '04' and bp_id =SUBSTRING(@strdpid,4,5)  
  END
  ELSE IF @strOption = 'Theme'
  BEGIN
    SELECT LevelSetting 
    FROM tbl_CrossNetMenu(NOLOCK) WHERE Menucode = 'F0000000'
	
	SELECT [LevelSetting1] = '{"fontSettings":{"sidebar":"Arial","content":"Arial"}'
  END  
  ELSE IF @strOption = 'UserProfile'
  BEGIN
    DECLARE @UserProfilejson VARCHAR(MAX) ='', @ProfileUser VARCHAR(50)=''
	
	SELECT TOP 1 @ProfileUser = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'ClientCode'
	IF isnull(@ProfileUser,'') = ''
	BEGIN
	  SELECT TOP 1 @ProfileUser = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Userid'
	END  
	IF EXISTS(SELECT 1 FROM CLIENT_MASTER WHERE CM_cD = @ProfileUser)
	BEGIN
      SET @UserProfilejson =(SELECT [Personal Detail] = (select [Code] = ltrim(rtrim(CM_CD)),
         [Name] = ltrim(rtrim(CM_NAME)), 	
         [Mobile] = CASE WHEN ISNULL(cm_MOBILE,'') <> '' THEN RIGHT('XXXXXX', LEN(ltrim(rtrim(cm_MOBILE))) - 4) + RIGHT(ltrim(rtrim(cm_MOBILE)), 4)
         ELSE '' END, [Email] = CASE WHEN ISNULL(cm_email,'') <> '' THEN LEFT(ltrim(rtrim(cm_email)), 3) + '****@' + 
         SUBSTRING(ltrim(rtrim(cm_email)), CHARINDEX('@', ltrim(rtrim(cm_email))) + 1, LEN(ltrim(rtrim(cm_email)))) else '' end,
         [Gender] = CASE WHEN cb_sexcode = 'M' THEN 'Male' WHEN cb_sexcode = 'F' THEN 'Female' WHEN cb_sexcode = 'O' THEN 'Other' END,
		 [PAN] = RIGHT('XXXXXX', LEN(ltrim(rtrim(cb_panno))) - 4) + RIGHT(ltrim(rtrim(cb_panno)), 4)
         FROM CLIENT_MASTER(NOLOCK), Client_Backoffice(NOLOCK) 
         WHERE cm_cd = @ProfileUser
		 AND CM_cD = cb_cmcd FOR JSON PATH),
         [Address Details] = (select [Address1] = ltrim(rtrim(cm_add1)),
         [Address2] = ltrim(rtrim(cm_add2)), [Address3] = ltrim(rtrim(CM_ADD3)), [City] = ltrim(rtrim(cm_city)),
         [State] = ltrim(rtrim(cm_state))+' - '+ltrim(rtrim(cm_pin)),
         [Country] = ltrim(rtrim(cm_bankactno)) 
         FROM CLIENT_MASTER(NOLOCK)
         WHERE CM_cD = @ProfileUser  FOR JSON PATH),
         [Bank Details] = ( SELECT * FROM (SELECT [Name] = ltrim(rtrim(mas.bk_name)), [Account No] = RIGHT('XXXXXXXXXX', LEN(ltrim(rtrim(cm_divbankacno))) - 4) + RIGHT(ltrim(rtrim(cm_divbankacno)), 4) , 
					[IFSC] = ltrim(rtrim(bk_branch)), [MICR] 
					= ltrim(rtrim(cm_divbankcode)),
					[AccountType] =''
				FROM CLIENT_MASTER x, Bank_master mas
				WHERE x.cm_divbankcode = mas.bk_micr  and CM_cD = @ProfileUser
				) X111
				FOR JSON PATH),
         [Demat Details] = (SELECT [DP ID] = '', [DP Account No] = ''  FOR JSON PATH) FOR JSON PATH)
	  SELECT @UserProfilejson 
	  RETURN 1
    END  
  END
  ELSE IF @strOption IN('Entry','ChildEntry','Master_Edit','ChildEntry_Edit')
  BEGIN
    DECLARE @StrTempateJson VARCHAR(MAX)=''
	--IF @strEntryName NOT IN('Rekyc','ClientOffLine')
	IF ISNULL(@strmenutype1,'') = 'E'
	BEGIN
	  SELECT @StrTempateJson = '[' + STUFF((
      SELECT ',' +
      '{"Srno": '+CAST(ORDERBY AS VARCHAR)+',"type": "'+
      CASE WHEN CONTROLTYPE='DropDown' THEN 'WDropDownBox' 
         WHEN CONTROLTYPE='EditBox' THEN 'WTextBox' 
         WHEN CONTROLTYPE='DatePicker' THEN 'WDateBox' 
         WHEN CONTROLTYPE='DisplayBox' THEN 'WDisplayBox' 
		 WHEN CONTROLTYPE='File' THEN 'WFile'  WHEN CONTROLTYPE='CheckBox' THEN 'WCheckBox' ELSE CONTROLTYPE 
      END+
      '","label": "'+ISNULL(displayname,'')+'","wKey": "'+ISNULL(TagName,'')+'","FieldWidth":"'+CAST(FieldWidth AS VARCHAR)+'","FieldSize":"'+CAST(FieldSize AS VARCHAR)+'",'+'
	  "FieldEnabledTag":"'+CASE WHEN ISNULL(FieldList,'') ='Y' THEN 'Y' ELSE 'N' END+'",
	  "FieldVisibleTag":"'+CASE WHEN ISNULL(FieldList,'') ='X' THEN 'N' ELSE 'Y' END+'",'+'"iscreatable":"'+CASE WHEN isnull(FieldQuery,'') = 'XX' THEN 'true' else 'false' end +'",'+
	  '"isMandatory":"'+CASE WHEN isnull(isMandatory,'') = 'Y' THEN 'true' else 'false' end +'",
	  "FieldType":"'+CASE WHEN CONTROLTYPE = 'FILE' 
	   THEN case when TagName = 'SignAttachment' then 'jpg,png' else 'jpg,png,gif,tiff,bmp,PDF' end else CAST(FieldType AS VARCHAR(100)) end+'",'+CASE WHEN @strOption in('Master_Edit','ChildEntry_Edit') THEN '"wValue":"~~'+FieldName+'~~",'
	  ELSE '' END+'"FileType":"'+CASE WHEN CONTROLTYPE = 'FILE' 
	   THEN case when TagName = 'SignAttachment' then 'jpg,png' else 'jpg,png,gif,tiff,bmp,PDF' end ELSE '' END  +'",
	   "isBR":"'+CASE WHEN ISNULL(UpdateBy,'N') ='Y' THEN 'true' ELSE 'false' end +'",
	  "childDependents":'+ISNULL((SELECT '["'+STUFF((
      SELECT ', ' + TAGNAME
      FROM tbl_GenericTemplateDefinition(NOLOCK) GG , tbl_InsertUpdateXMLDropDownQuery(NOLOCK) dd 
      WHERE GG.TEMPLATENAME =  @strEntryName
      AND GG.ParentTemplateCode = 'ChildEntry'
	  AND ISNULL(DD.DependentField,'') = G.TAGNAME
	  AND GG.TEMPLATENAME = DD.ModuleName AND GG.TAGNAME = DD.ColumnName
      FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS AllValues)+'"]','[]')+',
      "ValidationAPI":'+
      CASE WHEN ISNULL(ValidationQuery,'') = '' THEN '{}' 
      ELSE ISNULL(
        (SELECT TOP 1 REPLACE(ISNULL(DependentField,'{}'),'<<USERID>>',@strUserid) 
         FROM tbl_InsertUpdateXMLDropDownQuery WITH(NOLOCK) 
         WHERE ModuleName = @strEntryName
         AND ColumnName = ISNULL(g.ValidationQuery,'')),
        '{}') END +
       CASE WHEN CONTROLTYPE='DropDown' THEN 
        CASE WHEN ISNULL(DependentField,'') <> '' THEN 
            ',"dependsOn": {"field": ["'+ISNULL(DependentField,'')+'"],
            "wQuery": {"Sql": "","J_Ui": 
            {"ActionName": "'+ISNULL(MODULENAME,'')+'","Option": "'+ISNULL(ColumnName,'')+'","RequestFrom": "W"},
            "X_Filter_Multiple": "'+ISNULL(DependentField,'')+'","X_Filter": "","J_Api": {"UserId": "'+@strUserid+'","AccYear": 24,"MyDbPrefix": "SVVS","MemberCode": "undefined",
            "SecretKey": "undefined","MenuCode": 7}}},"wDropDownKey":{"key": "DisplayName","value": "Value"}}'
        ELSE ',"wQuery": {"Sql": "","J_Ui": 
            {"ActionName": "'+ISNULL(MODULENAME,'')+'","Option": "'+ISNULL(ColumnName,'')+'","RequestFrom": "W"},
            "X_Filter_Multiple": "","X_Filter": "","J_Api": {"UserId": "'+@strUserid+'","AccYear": 24,"MyDbPrefix": "SVVS","MemberCode": "undefined",
            "SecretKey": "undefined","MenuCode": 7}},"wDropDownKey":{"key": "DisplayName","value": "Value"}}' 
        END
        WHEN CONTROLTYPE='DatePicker' AND @strOption not in('Master_Edit','ChildEntry_Edit') THEN 
        ',"wValue":"'+CONVERT(VARCHAR,GETDATE(),112)+'"}' 
        ELSE '}' END
       FROM tbl_GenericTemplateDefinition g WITH(NOLOCK) 
       LEFT OUTER JOIN tbl_InsertUpdateXMLDropDownQuery d WITH(NOLOCK) 
        ON(g.TEMPLATENAME = d.ModuleName AND g.TAGNAME = d.ColumnName)
       WHERE g.TEMPLATENAME = @strEntryName
       AND g.ParentTemplateCode = CASE WHEN @strOption = 'Master_Edit' THEN 'Entry' 
	   WHEN @strOption = 'ChildEntry_Edit' THEN 'ChildEntry' else @strOption end
       ORDER BY g.OrderBy
       FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '') + ']' 
    END
	ELSE IF ISNULL(@strmenutype1,'') = 'M'
	BEGIN
	  SET @StrTempateJson = '[' + STUFF((
	  SELECT ',{' +'"TabName":"'+GroupName+'",
	  "tableData":'+CASE WHEN @strOption in('Master_Edit','ChildEntry_Edit') THEN '"~~'+GroupName+'~~"' ELSE '[]' END +',
	  "Settings":{"maxAllowedRecords": "10","isGroup":"true"'
	  	+CASE WHEN GroupName = 'FinancialDetails' THEN ',"MakerSaveAPI":{"J_Ui": {"ActionName": "MakerSave","Option": "ADD"},"Sql": {},
	  "X_Filter_Multiple": {"EntryName": "'+@strEntryName+'","ClientFormNo":"##ClientFormNo##","ClientPANNo":"##ClientPANNo##","ClientCode": 
	  "##ClientCode##"},"X_DataJson":"##X_DataJson##",
		"J_Api": {"UserId": "##UserId##"}}' ELSE '' END +',
	  "isTable": "'+CASE WHEN GroupName in('NomineeDetails','OtherHolders','PowerofAttorney','AuthoriseSignatory') then 'true' else 'false' 
	  end +'"'+CASE WHEN GroupName = 'NomineeDetails' THEN ',"IsChildEntryAllowed":"true",
	  "ChildEntryAPI":{"J_Ui": {"ActionName": "CROSSNET","Option": "ChildEntry"},
	  "Sql": {},"X_Filter": {"EntryName": "'+@strEntryName+'",
	  "ClientFormNo":"##ClientFormNo##","ClientPANNo":"##ClientPANNo##","NomSerial": "##NomSerial##"},
	  "J_Api": {"UserId": "##UserId##"}},' ELSE ',' END+'
	  "TabChangeAPI":{"J_Ui": {"ActionName": "CROSSNET","Option": "TabChangeAPI"},
	  "Sql": {},"X_Filter": {"EntryName": "'+@strEntryName+'",
	  "TabName":"'+GroupName+'","ClientFormNo":"##ClientFormNo##","AccountType":"##AccountType##","Branch":"##Branch##","NominationotpOut":"##NominationotpOut##",
	  "POAforPaying":"##POAforPaying##","SubStatus":"##SubStatus##"},
	  "J_Api": {"UserId": "##UserId##"}},"SaveNextAPI":{"J_Ui": {"ActionName": "CROSSNET","Option": "SaveNext"},"Sql": {},
	  "X_Filter_Multiple": {"EntryName": "'+@strEntryName+'","ClientCode": 
	  "##ClientCode##","TabName":"'+GroupName+'","ClientFormNo":"##ClientFormNo##","ClientPANNo":"##ClientPANNo##"},
	  "X_DataJson":"##X_DataJson##", "J_Api": {"UserId": "##UserId##"}}},"Data":'+data1 from(
	  SELECT DISTINCT GroupName, 'Data1' = isnull((select '[{' + STUFF((SELECT ',' +
      '{"Srno": '+CAST(ORDERBY AS VARCHAR)+',"type": "'+ CASE WHEN CONTROLTYPE='DropDown' THEN 'WDropDownBox'  WHEN CONTROLTYPE='EditBox' THEN 'WTextBox'  
	  WHEN CONTROLTYPE='DatePicker' THEN 'WDateBox' 
         WHEN CONTROLTYPE='DisplayBox' THEN 'WDisplayBox' WHEN CONTROLTYPE='File' THEN 'WFile'  WHEN CONTROLTYPE='CheckBox' THEN 'WCheckBox' 
		 WHEN CONTROLTYPE='DateTimePicker' THEN 'WDateTimePicker' ELSE CONTROLTYPE END+
      '","label": "'+ISNULL(displayname,'')+'","wKey": "'+ISNULL(TagName,'')+'","FieldWidth":"'+CAST(FieldWidth AS VARCHAR)+'","FieldSize":"'+CASE WHEN CONTROLTYPE='File' THEN '-1' ELSE CAST(FieldSize AS VARCHAR) END+'",'+'
	  "isResizable":"'+case when CONTROLTYPE='File' THEN 'true' ELSE 'false' end+'",
	  "FieldEnabledTag":"'+CASE WHEN ISNULL(FieldList,'') ='Y' THEN CASE WHEN @strOption in('Master_Edit','ChildEntry_Edit') 
	   AND ISNULL(AllowEdit,'N') = 'N' THEN 'N' ELSE 'Y' END ELSE 'N' END+'",
	  "FieldVisibleTag":"'+CASE WHEN ISNULL(FieldList,'') ='X' THEN 'N' ELSE 'Y' END+'","CombinedName":"'+CAST(DisplayValue AS VARCHAR)+'",'+'"iscreatable":"'+CASE WHEN isnull(FieldQuery,'') = 'XX' THEN 'true' else 'false' end +'",'+
	  '"isMandatory":"'+CASE WHEN isnull(isMandatory,'') = 'Y' THEN 'true' else 'false' end +'",
	  "FieldType":"'+CASE WHEN CONTROLTYPE = 'FILE' 
	   THEN case when TagName = 'SignAttachment' then 'jpg,png' else 'jpg,png,gif,tiff,bmp,PDF' end else CAST(FieldType AS VARCHAR(100)) end+'","redirectUrl":"'+CASE WHEN TagName IN('CorrAddress1','BankAccNo') THEN 'true' else 'false' end +'",
	  "GetResponseFlag":"'+CASE WHEN TagName IN('BankAccNo') THEN 'true' else 'false' end +
	   '","isVisibleinTable":"'+CASE WHEN DisplayLendingPage ='Y' THEN 'true' else 'false' end +
	  '","isBR":"'+CASE WHEN ISNULL(UpdateBy,'N') ='Y' THEN 'true' ELSE 'false' end +'","OTPRequire":"'+CASE WHEN FieldQuery = '12' THEN 'OLD|NEW' WHEN FieldQuery = '2' THEN 'NEW'
	   WHEN FieldQuery = '1' THEN 'OLD' else '' end+'"'+
	   ',"isChildFormEntryRequired":"'+CASE WHEN GroupName = 'NomineeDetails' AND TAGNAME ='NomineeDOB' THEN 'true'  else 'false' end+'"'+
	   ',"FileType":"'+CASE WHEN CONTROLTYPE = 'FILE' 
	   THEN case when TagName = 'SignAttachment' then 'jpg,png' else 'jpg,png,gif,tiff,bmp,PDF' end ELSE '' END  +'","OlddataValue":"'+case when TagName in('Email','Mobile') then '##'+TagName+'1##' else '' end+'","OTPSend":'+CASE WHEN ISNULL(G.FieldQuery,'') IN('12','1','2')  
	  THEN CASE WHEN ISNULL(FieldValue,'') = '' THEN '{}' 
      ELSE ISNULL(
        (SELECT TOP 1 REPLACE(ISNULL(DependentField,'{}'),'<<USERID>>',@strUserid) 
         FROM tbl_InsertUpdateXMLDropDownQuery WITH(NOLOCK) 
         WHERE ModuleName = @strEntryName
         AND ColumnName = ISNULL(g.FieldValue,'')),
        '{}') END
		ELSE '{}' END +
		',"OTPValidate":'+CASE WHEN ISNULL(G.FieldQuery,'') IN('12','1','2')  
	  THEN CASE WHEN ISNULL(FieldValue,'') = '' THEN '{}' 
      ELSE ISNULL(
        (SELECT TOP 1 REPLACE(ISNULL(DependentField,'{}'),'<<USERID>>',@strUserid) 
         FROM tbl_InsertUpdateXMLDropDownQuery WITH(NOLOCK) 
         WHERE ModuleName = @strEntryName
         AND ColumnName = ISNULL(g.ValidationQuery,'')),
        '{}') END
		ELSE '{}' END +',"childDependents":'+ISNULL((SELECT '["'+STUFF((
      SELECT ', ' + TAGNAME
      FROM tbl_GenericTemplateDefinition(NOLOCK) GG , tbl_InsertUpdateXMLDropDownQuery(NOLOCK) dd 
      WHERE GG.TEMPLATENAME =  @strEntryName
      AND GG.ParentTemplateCode = 'ChildEntry'
	  AND ISNULL(DD.DependentField,'') = G.TAGNAME
	  AND GG.TEMPLATENAME = DD.ModuleName AND GG.TAGNAME = DD.ColumnName
      FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS AllValues)+'"]','[]')+',"ThirdPartyAPI":'+CASE WHEN G.TableName = 'TJSON' 
	  THEN CASE WHEN (ISNULL(FieldValue,'') = '' OR ISNULL(G.FieldQuery,'') IN('12','1','2')) THEN '{}' 
      ELSE ISNULL(
        (SELECT TOP 1 REPLACE(ISNULL(DependentField,'{}'),'<<USERID>>',@strUserid) 
         FROM tbl_InsertUpdateXMLDropDownQuery WITH(NOLOCK) 
         WHERE ModuleName = @strEntryName
         AND ColumnName = ISNULL(g.FieldValue,'')),
        '{}') END
		ELSE '{}' END +',"ValidationAPI":'+
      CASE WHEN (ISNULL(ValidationQuery,'') = '' OR ISNULL(G.FieldQuery,'') IN('12','1','2'))   THEN '{}' 
      ELSE ISNULL(
        (SELECT TOP 1 REPLACE(ISNULL(DependentField,'{}'),'<<USERID>>',@strUserid) 
         FROM tbl_InsertUpdateXMLDropDownQuery WITH(NOLOCK) 
         WHERE ModuleName = @strEntryName
         AND ColumnName = ISNULL(g.ValidationQuery,'')),
        '{}') END +
      CASE WHEN CONTROLTYPE='DropDown' THEN 
        CASE WHEN ISNULL(DependentField,'') <> '' THEN 
            ',"dependsOn": {"field": ["'+ISNULL(DependentField,'')+'"],
            "wQuery": {"Sql": "","J_Ui": 
            {"ActionName": "'+ISNULL(MODULENAME,'')+'","Option": "'+ISNULL(ColumnName,'')+'","RequestFrom": "W"},
            "X_Filter_Multiple": "'+ISNULL(DependentField,'')+'","X_Filter": "","J_Api": {"UserId": "'+@strUserid+'","AccYear": 24,"MyDbPrefix": "SVVS","MemberCode": "undefined",
            "SecretKey": "undefined","MenuCode": 7}}},"wDropDownKey":{"key": "DisplayName","value": "Value"}}'
        ELSE 
            ',"wQuery": {"Sql": "","J_Ui": 
            {"ActionName": "'+ISNULL(MODULENAME,'')+'","Option": "'+ISNULL(ColumnName,'')+'","RequestFrom": "W"},
            "X_Filter_Multiple": "","X_Filter": "","J_Api": {"UserId": "'+@strUserid+'","AccYear": 24,"MyDbPrefix": "SVVS","MemberCode": "undefined",
            "SecretKey": "undefined","MenuCode": 7}},"wDropDownKey":{"key": "DisplayName","value": "Value"}}' 
        END
       WHEN CONTROLTYPE='DatePicker' AND @strOption not in('Master_Edit','ChildEntry_Edit') 
	   and ISNULL(FieldList,'') ='Y' THEN ',"wValue":"'+CONVERT(VARCHAR,GETDATE(),112)+'"}' 
	   WHEN CONTROLTYPE='CheckNox' AND @strOption not in('Master_Edit','ChildEntry_Edit') 
	   THEN ',"wValue":"false"}' 
      ELSE '}' 
      END as Text1
      FROM tbl_GenericTemplateDefinition g WITH(NOLOCK) 
      LEFT OUTER JOIN tbl_InsertUpdateXMLDropDownQuery d WITH(NOLOCK) 
        ON(g.TEMPLATENAME = d.ModuleName AND g.TAGNAME = d.ColumnName)
      WHERE g.TEMPLATENAME = @strEntryName
      AND g.ParentTemplateCode = CASE WHEN @strOption = 'Master_Edit' THEN 'Entry' 
	  WHEN @strOption = 'ChildEntry_Edit' THEN 'ChildEntry' else @strOption end
	  and g.GroupName = xmain.GroupName order by g.OrderBy FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS AllValues)+']}','[]')
      FROM tbl_GenericTemplateDefinition xmain
      where TEMPLATENAME = @strEntryName 
	  AND xmain.ParentTemplateCode = CASE WHEN @strOption = 'Master_Edit' THEN 'Entry' 
	  WHEN @strOption = 'ChildEntry_Edit' THEN 'ChildEntry' else @strOption end
	  ) x111 ORDER BY CASE WHEN GROUPNAME  = 'MASTER' THEN 1 WHEN GROUPNAME  = 'ClientDetails' then 2
	   WHEN GROUPNAME  = 'BODetail' THEN 3 WHEN GROUPNAME  = 'BackOffice' then 4 WHEN GROUPNAME  = 'OtherHolders' then 5 
	   WHEN GROUPNAME  = 'FinancialDetails' then 9 else 6 end FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '') + ']' 
	END
  	
	SET @StrTempateJson = REPLACE(@StrTempateJson,'##UserId##',@strUserid)
	
    IF @strOption IN('Entry','ChildEntry')
	BEGIN
	  SELECT @StrTempateJson
	  RETURN 1
	END
	
	
    DECLARE @strtradeplustempdb VARCHAR(50) = ''
    SELECT @strtradeplustempdb = sp_sysvalue
    FROM WebParameter(NOLOCK)
    WHERE sp_parmcd = 'TRADEPLUSTEMPDB'
    IF OBJECT_ID('tempdb..#tbl_jsonoutput1') IS NOT NULL
    BEGIN
	   DROP TABLE #tbl_jsonoutput1
    END

    CREATE TABLE #tbl_jsonoutput1 (SerialNo INT, ColumnName VARCHAR(50), ColumnValue VARCHAR(MAX), MasterTag VARCHAR(100), 
	JSONLEVEL INT, MASTERLEVEL INT, FieldName VARCHAR(50))

    DECLARE @jsonCutterOutput VARCHAR(MAX)='', @strDanSql NVARCHAR(MAX)=''
    DECLARE @JsonCutterXML XML
	
	IF ISNULL(@strmenutype1,'') = 'M' AND @strOption = 'Master_Edit'
	BEGIN
	  
	  IF @strEntryName IN('ClientOffLine','ClientMaster')
	  BEGIN
	    DECLARE @strInternalRefNo VARCHAR(20) = '', @strClientFormNo VARCHAR(20)='' 
	    DECLARE @strFormGroupName VARCHAR(30)='', @strFormJSON VARCHAR(MAX)=''
	  
		
		SELECT @strInternalRefNo = ParameterValue From @tbl_InputJSONTable where ParameterName = 'InternalRefNo'  
	    SELECT @strClientFormNo = ParameterValue From @tbl_InputJSONTable where ParameterName = 'ClientFormNo'  
	  
	    IF ISNULL(@strInternalRefNo,'') = '' and @strEntryName = 'ClientOffLine'
	    BEGIN
	      SELECT [Flag] = 'E', [Message] = 'Internal Ref No is require'
          RETURN 1 
	    END
	    DECLARE @strUserBOID VARCHAR(16)= '', @o_Json VARCHAR(MAX)=''
	    SELECT @strUserBOID = ParameterValue From @tbl_InputJSONTable where ParameterName = 'BOID'    
	  
	    DECLARE @json NVARCHAR(MAX) = N'{';
        DECLARE @categories TABLE (Category NVARCHAR(100));
        DECLARE @category NVARCHAR(500);
        DECLARE @key_value_pairs NVARCHAR(MAX) = N'';
        
		  
		DECLARE ColumnMultiFormData CURSOR FOR
        SELECT DISTINCT GROUPNAME FROM tbl_GenericTemplateDefinition(NOLOCK)
        WHERE TemplateName = @strEntryName AND GROUPNAME NOT IN('MASTER')
	    
        OPEN ColumnMultiFormData;
        FETCH NEXT FROM ColumnMultiFormData INTO @strFormGroupName
        WHILE @@FETCH_STATUS = 0
        BEGIN
		  SET @strFormJSON = ''
		  IF EXISTS(SELECT 1 FROM tbl_MultiEntryIncompleteDetail 
		  WHERE InternalRefNo = @strInternalRefNo
	      AND ClientFormNo = @strClientFormNo AND EntryTabName = @strFormGroupName)
		  BEGIN
	        SELECT @strFormJSON = EntryJson from tbl_MultiEntryIncompleteDetail 
		    WHERE InternalRefNo = @strInternalRefNo
	        AND ClientFormNo = @strClientFormNo AND EntryTabName = @strFormGroupName
		  END
          ELSE
          BEGIN
		    SET @strFormJSON = ''
          END		  
	      
		  IF ISNULL(@strFormJSON,'') = ''
		  BEGIN
		    SET @o_Json = ''
		    EXEC stpr_GenerateClientModifyData @strUserBOID, @strFormGroupName,@o_Json output
			SET @StrTempateJson = REPLACE(@StrTempateJson,'"~~'+@strFormGroupName+'~~"', ISNULL(@o_Json,''))
		  END	
		  
		  IF ISNULL(@strFormJSON,'') <> '' and @strFormGroupName NOT IN('NomineeDetails','GuardianDetails')
	      BEGIN
		    SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@strFormJSON+''' , @jsonCutterOutput OUTPUT';
            EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
            SET @JsonCutterXML= CAST(@jsonCutterOutput AS XML)
			
			DELETE FROM #tbl_jsonoutput1

            INSERT INTO #tbl_jsonoutput1(SerialNo, ColumnName, ColumnValue, MasterTag, JsonLevel, MasterLevel)
            SELECT X1.* FROM(
            SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
            JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
            JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
            JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
            JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
            JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
            FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1
			
			/*
			UPDATE A SET A.ColumnValue = ''
			FROM  #tbl_jsonoutput1 A, tbl_GenericTemplateDefinition X
			WHERE A.MasterTag = X.GROUPNAME
			AND X.TemplateName = @strEntryName 
			AND A.ColumnName = X.TAGNAME
			AND X.ControlType = 'FILE'
			*/
			
			SET @json = N'[';
			
            DELETE FROM @categories
	        
			INSERT INTO @categories 
            SELECT DISTINCT MasterTag 
            FROM #tbl_jsonoutput1 WHERE MasterTag <> 'Master';
	        SET @category = ''
			DECLARE @strJSONLevel INT = 0

            DECLARE category_cursor CURSOR FOR 
            SELECT Category FROM @categories;

            OPEN category_cursor;
            FETCH NEXT FROM category_cursor INTO @category;

            WHILE @@FETCH_STATUS = 0
            BEGIN
			  DECLARE category_cursor_2 CURSOR FOR 
			  SELECT DISTINCT JsonLevel 
              FROM #tbl_jsonoutput1
			  WHERE MasterTag = @category
			  
			  OPEN category_cursor_2;
              FETCH NEXT FROM category_cursor_2 INTO @strJSONLevel;
              
              WHILE @@FETCH_STATUS = 0
              BEGIN
			     SET @json += N' {';
                 SET @key_value_pairs = N'';
				         
		         SELECT @key_value_pairs += 
                 CASE WHEN @key_value_pairs <> N'' THEN N',' ELSE N'' END +
                 N'"' + ColumnName + N'": "' + REPLACE(ColumnValue, '"', '\"') + N'"'
                 FROM #tbl_jsonoutput1 
                 WHERE MasterTag = @category 
			     AND JsonLevel = @strJSONLevel
                 SET @json += @key_value_pairs + N'}';
                 FETCH NEXT FROM category_cursor_2 INTO @strJSONLevel;
              
			     IF @@FETCH_STATUS = 0
                   SET @json += N',';
			  END   
			  CLOSE category_cursor_2;
              DEALLOCATE category_cursor_2;
              FETCH NEXT FROM category_cursor INTO @category;
			  IF @@FETCH_STATUS = 0
                SET @json += N',';
            END
            CLOSE category_cursor;
            DEALLOCATE category_cursor;
            SET @json += N']';
	        SET @StrTempateJson = REPLACE(@StrTempateJson,'"~~'+@strFormGroupName+'~~"', @json)
          END
		  ELSE IF ISNULL(@strFormJSON,'') <> ''
		  BEGIN
		    DECLARE @strDanSql1 NVARCHAR(MAX)='', @outputJson NVARCHAR(MAX)=''
			IF ISNULL(@strFormJSON,'') <> ''
			BEGIN
		      SET @strDanSql1 = 'EXEC '+@strtradeplustempdb+'.DBO.SP_RemoveMasterJsoninArray'+' '''+@strFormJSON+''' ,'''+@strFormGroupName+''',  @outputJson OUTPUT';
              EXEC sp_executesql @strDanSql1, N'@outputJson VARCHAR(MAX) OUTPUT', @outputJson OUTPUT;
			
		 --   SELECT @strFormJSON = JSON_QUERY(Nominee.value) AS Nominee FROM OPENJSON(@strFormJSON, '$.NomineeDetails') AS Nominee
           -- FOR JSON PATH
		    END
			SET @StrTempateJson = REPLACE(@StrTempateJson,'"~~'+@strFormGroupName+'~~"', ISNULL(@outputJson,''))
		  END
	      FETCH NEXT FROM ColumnMultiFormData INTO @strFormGroupName
        END;
        CLOSE ColumnMultiFormData;
        DEALLOCATE ColumnMultiFormData;
        

		SET @json = N'[';
		
		IF NOT EXISTS(SELECT 1 FROM #tbl_jsonoutput1 WHERE MasterTag = 'Master')
		BEGIN
		  SET @o_Json = ''
		  EXEC stpr_GenerateClientModifyData @strUserBOID, 'Master',@o_Json output
		  SET @StrTempateJson = REPLACE(@StrTempateJson,'"~~Master~~"', ISNULL(@o_Json,''))
		END	
		ELSE
        BEGIN 		
          DELETE FROM @categories
	        
		  INSERT INTO @categories 
          SELECT DISTINCT MasterTag 
          FROM #tbl_jsonoutput1 WHERE MasterTag = 'Master';
	      SET @category = ''

          DECLARE category_cursor1 CURSOR FOR 
          SELECT Category FROM @categories;

          OPEN category_cursor1;
          FETCH NEXT FROM category_cursor1 INTO @category;

          WHILE @@FETCH_STATUS = 0
          BEGIN
            SET @json += N' {';
            SET @key_value_pairs = N'';
        
		    SELECT @key_value_pairs += 
            CASE WHEN @key_value_pairs <> N'' THEN N',' ELSE N'' END +
            N'"' + ColumnName + N'": "' + REPLACE(ColumnValue, '"', '\"') + N'"'
            FROM #tbl_jsonoutput1 
            WHERE MasterTag = @category 
            SET @json += @key_value_pairs + N'}';
    
            FETCH NEXT FROM category_cursor1 INTO @category;
              
		    IF @@FETCH_STATUS = 0
             SET @json += N',';
           END
           CLOSE category_cursor1;
           DEALLOCATE category_cursor1;
           SET @json += N']';
	       SET @StrTempateJson = REPLACE(@StrTempateJson,'"~~Master~~"', @json)
	    END
	  END	
	  SELECT @StrTempateJson
	  RETURN 1
	END 
    
	DECLARE @StrGetFilter VARCHAR(MAX)='', @strTableName VARCHAR(100)='', @strChildtableName VARCHAR(100)=''
    
	SELECT @strTableName = CASE WHEN @strOption= 'Master_Edit' 
    THEN CASE WHEN isnull(MasterTableName,'') = '' THEN DetailTableName else MasterTableName end
    WHEN @strOption= 'ChildEntry_Edit' then CASE WHEN isnull(DetailTableName,'') = '' THEN MasterTableName else DetailTableName end 
    ELSE DetailTableName end
    FROM tbl_InsertUpdateConfig(NOLOCK)
    WHERE ModuleName = @strEntryName

    SELECT @strChildtableName = CASE WHEN isnull(DetailTableName,'') = '' 
	THEN MasterTableName else DetailTableName end
    FROM tbl_InsertUpdateConfig(NOLOCK)
    WHERE ModuleName = @strEntryName


    SELECT @StrGetFilter = downloadFilters from tbl_CrossNetMenu(NOLOCK) WHERE MenuTag = @strEntryName

	
    SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@StrGetFilter+''' , @jsonCutterOutput OUTPUT';
    EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
    SET @JsonCutterXML= CAST(@jsonCutterOutput AS XML)

    INSERT INTO #tbl_jsonoutput1(SerialNo, ColumnName, ColumnValue, MasterTag, JsonLevel, MasterLevel)
    SELECT X1.* FROM(
    SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
    JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
    JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
    JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
    JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
    JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
    FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1

    DECLARE @strwhere VARCHAR(MAX)='', @String NVARCHAR(MAX)=''

    UPDATE A SET A.ColumnValue = B.ParameterValue
    FROM #tbl_jsonoutput1 A, @tbl_InputJSONTable B
    WHERE A.MASTERTAG = 'X_Filter'
    AND B.ParameterName = A.ColumnName

    UPDATE A set A.FieldName = b.FieldName , 
    ColumnValue = case when B.ControlType='DatePicker' Then CONVERT(VARCHAR(8), CONVERT(DATETIME, ColumnValue, 103), 112) Else ColumnValue End
    FROM #tbl_jsonoutput1 A, tbl_GenericTemplateDefinition(nolock) B
    WHERE B.TemplateNAME = @strEntryName
    AND A.ColumnName = B.TagName
    AND B.ParentTemplateCode = CASE WHEN @strOption= 'Master_Edit' THEN 'Entry' else 'ChildEntry' end

    
    SELECT @strwhere = @strwhere+' '+ ' AND '+FieldName+' = '''+CAST(ColumnValue AS VARCHAR)+''''
    FROM(
    SELECT DISTINCT COLUMNNAME, ColumnValue, FieldName FROM #tbl_jsonoutput1 
    WHERE MASTERTAG = 'X_Filter'
    AND ISNULL(FieldName,'') <> '') X1
    
	

    DECLARE @ColumnDefinitions TABLE (ColumnName NVARCHAR(128), TagName NVARCHAR(128));

    INSERT INTO @ColumnDefinitions (ColumnName, TagName)
    SELECT FieldName, TagName
    FROM tbl_GenericTemplateDefinition(NOLOCK)
    WHERE ParentTemplateCode = CASE WHEN @strOption= 'Master_Edit' THEN 'Entry' else 'ChildEntry' end
    AND TemplateName = @strEntryName and ISNULL(FieldName,'') <> ''
    ORDER BY OrderBy;
    --SELECT @StrTempateJson
    DECLARE @TBL_ColumnValue TABLE(ColumnValue VARCHAR(MAX))

    DECLARE @ColumnName NVARCHAR(128);
    DECLARE @TagName NVARCHAR(128);

    DECLARE ColumnCursor CURSOR FOR
    SELECT ColumnName, TagName
    FROM @ColumnDefinitions
    OPEN ColumnCursor;
    FETCH NEXT FROM ColumnCursor INTO @ColumnName, @TagName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
      DELETE FROM 	@TBL_ColumnValue
	  IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @strTableName and COLUMN_NAME = @ColumnName)
	  BEGIN
	    IF @ColumnName = 'tb_reasfortrade'
	    BEGIN
	      SET @String = 'SELECT CASE WHEN ISNULL(tb_NFiller2,0) = 0 THEN LTRIM(RTRIM(tb_reasfortrade)) ELSE CAST(tb_NFiller2 AS VARCHAR)+''~''+CAST(LTRIM(RTRIM(tb_reasfortrade)) AS VARCHAR) END AS Reason from '+@strTableName+' WHERE 1 = 1  '+@strwhere
		  --SET @Sql = @Sql + 'CASE WHEN ISNULL(tb_NFiller2,0) = 0 THEN LTRIM(RTRIM(tb_reasfortrade)) ELSE CAST(tb_NFiller2 AS VARCHAR)+''~''+CAST(LTRIM(RTRIM(tb_reasfortrade)) AS VARCHAR) END AS Reason '+@Delimiter
	    END
	    ELSE IF @ColumnName = 'id_reasfortrade'
	    BEGIN
	      SET @String = 'SELECT CASE WHEN ISNULL(id_NFiller2,0) = 0 THEN LTRIM(RTRIM(id_reasfortrade)) ELSE CAST(id_NFiller2 AS VARCHAR)+''~''+CAST(LTRIM(RTRIM(id_reasfortrade)) AS VARCHAR) END AS Reason from '+@strTableName+' WHERE 1 = 1  '+@strwhere
		  --SET @Sql = @Sql + 'CASE WHEN ISNULL(tb_NFiller2,0) = 0 THEN LTRIM(RTRIM(tb_reasfortrade)) ELSE CAST(tb_NFiller2 AS VARCHAR)+''~''+CAST(LTRIM(RTRIM(tb_reasfortrade)) AS VARCHAR) END AS Reason '+@Delimiter
	    END
		ELSE IF @ColumnName = 'rc_amount'
	    BEGIN
	      SET @String = 'SELECT ABS(rc_amount) AS Amount from '+@strTableName+' WHERE 1 = 1  '+@strwhere
	    END
	    ELSE 
	    BEGIN
	      SET @String = 'SELECT distinct '+@ColumnName+' from '+@strTableName+' WHERE 1 = 1  '+@strwhere
	    END
      
        INSERT INTO @TBL_ColumnValue(ColumnValue)
        EXEC(@String)
      
        SET @StrTempateJson = REPLACE(@StrTempateJson,'~~'+@ColumnName+'~~',ISNULL((SELECT TOP 1 LTRIM(RTRIM(ColumnValue)) FROM @TBL_ColumnValue),''))
      END 
      FETCH NEXT FROM ColumnCursor INTO @ColumnName, @TagName;
    END;

    SET @StrTempateJson = REPLACE(@StrTempateJson,'~~~~','')

    CLOSE ColumnCursor;
    DEALLOCATE ColumnCursor;
	
	DELETE FROM @ColumnDefinitions
	
	DECLARE @Sql NVARCHAR(MAX) = N'SELECT ';
	DECLARE @Delimiter NVARCHAR(2) = ', ';
	DECLARE @strDataType VARCHAR(50)=''	
	
	INSERT INTO @ColumnDefinitions (ColumnName, TagName)
    SELECT FieldName, TagName
    FROM tbl_GenericTemplateDefinition(NOLOCK)
    WHERE ParentTemplateCode = CASE WHEN @strOption= 'Master_Edit' THEN 'ChildEntry' else '' end
    AND TemplateName = @strEntryName and ISNULL(FieldName,'') <> ''
    ORDER BY OrderBy;

	DECLARE ColumnCursor1 CURSOR FOR
	SELECT ColumnName, TagName
	FROM @ColumnDefinitions;

	OPEN ColumnCursor1;

	FETCH NEXT FROM ColumnCursor1 INTO @ColumnName, @TagName;

	WHILE @@FETCH_STATUS = 0
	BEGIN
	  SELECT @strDataType = DATA_TYPE  
      FROM INFORMATION_SCHEMA.COLUMNS 
      WHERE TABLE_NAME = @strChildtableName AND COLUMN_NAME = @ColumnName

      IF @ColumnName = 'tb_reasfortrade'
	  BEGIN
		SET @Sql = @Sql + ' CASE WHEN ISNULL(tb_NFiller2,0) = 0 THEN LTRIM(RTRIM(tb_reasfortrade)) ELSE CAST(tb_NFiller2 AS VARCHAR)+''~''+CAST(LTRIM(RTRIM(tb_reasfortrade)) AS VARCHAR) END AS [Reason] ' + @Delimiter;
	  end
      ELSE IF @strDataType IN('varchar','char','DATE','DATETIME')
	  BEGIN
	     SET @Sql = @Sql + 'ISNULL('+QUOTENAME(@ColumnName)+','''')' + ' AS ' + QUOTENAME(@TagName) + @Delimiter;
	  END
      ELSE
      BEGIN
	    SET @Sql = @Sql + 'ISNULL('+QUOTENAME(@ColumnName)+',0)' + ' AS ' + QUOTENAME(@TagName) + @Delimiter;
      END 	
	  
	  FETCH NEXT FROM ColumnCursor1 INTO @ColumnName, @TagName;
	END;

	CLOSE ColumnCursor1;
    DEALLOCATE ColumnCursor1;
	--SELECT @Sql

    UPDATE A set A.FieldName = b.FieldName
    FROM #tbl_jsonoutput1 A, tbl_GenericTemplateDefinition(nolock) B
    WHERE B.TemplateNAME = @strEntryName
    AND A.ColumnName = B.TagName
    AND B.ParentTemplateCode = 'ChildEntry'
	
	--SELECT * FROM #tbl_jsonoutput1

    SET @strwhere = ''
    SELECT @strwhere = @strwhere+' '+ ' AND '+FieldName+' = '''+CAST(ColumnValue AS VARCHAR)+''''
    FROM(
    SELECT DISTINCT COLUMNNAME, ColumnValue, FieldName FROM #tbl_jsonoutput1 
    WHERE MASTERTAG = 'X_Filter'
    AND ISNULL(FieldName,'') <> '' AND ISNULL(ColumnValue,'') NOT LIKE '##%') X1

    
		-- Remove the trailing delimiter
	SET @Sql = LEFT(@Sql, LEN(@Sql) - LEN(@Delimiter));
		-- Append the table name
	SET @Sql = @Sql + ' FROM ' + QUOTENAME(@strChildtableName) + ' WHERE 1=1 '+@strwhere;
	
	SELECT @StrTempateJson
	--SELECT @Sql
	IF @strOption= 'Master_Edit' AND EXISTS(SELECT 1 FROM @ColumnDefinitions)
	BEGIN
	  EXEC sp_executesql @Sql;
	END  
  END  
 END
GO

CREATE PROCEDURE stpr_ClientListing @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, 
	@o_vcErrorMessage VARCHAR(MAX) OUTPUT WITH ENCRYPTION AS 
BEGIN
  SET @o_vcErrorMessage = 'Process Executed'
  SET @o_vcErrorFlag = 'S'
	--- Parameter Declaration
  DECLARE @dtFromDate VARCHAR(8), @dtToDt VARCHAR(8), @strUserId VARCHAR(500) = '', @strLevel VARCHAR(1), 
		@strCode VARCHAR(500), @strSelection VARCHAR(100), @XMLData XML, @strOutputType VARCHAR(1) = 'G', 
		@strRequestFrom VARCHAR(1) = 'W', @XMLDATA1 XML = '', @strShowValuation VARCHAR(20) = '', @strISIN VARCHAR(
			20), @strTrxType VARCHAR(200) = '', @strReport VARCHAR(50) = '', @strData VARCHAR(50) = '', @strDateType 
		VARCHAR(50) = '', @strSQL NVARCHAR(MAX) = ''

  IF @vcXML = ''
  BEGIN
	SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
	RETURN 1
  END

	--SELECT @vcXML


  SET @XMLData = CAST('<root>' + @vcXML + '</root>' AS XML)
  

  DECLARE @strOptionType VARCHAR(50) = ''  

  SELECT @dtFromDate = ISNULL(x.value('(FromDate)[1]', 'VARCHAR(8)'), ''), 
  @dtToDt = ISNULL(x.value('(ToDate)[1]', 'VARCHAR(8)'), ''), 
  @strUserId = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'), ''), 
  @strLevel = ISNULL(x.value('(Level)[1]', 'VARCHAR(1)'), ''), 
  @strCode = ISNULL(x.value('(Code)[1]', 'VARCHAR(500)'), ''), 
  @strSelection = ISNULL(x.value('(Selection)[1]', 'VARCHAR(50)'), ''), 
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'), ''), 
  @strRequestFrom = ISNULL(x.value('(RequestFrom)[1]', 'VARCHAR(1)'), ''), 
  @strOptionType = ISNULL(x.value('(OptionType)[1]', 'VARCHAR(50)'), '')
  FROM @XMLData.nodes('/root') AS XTbl(x)

  IF ISNULL(@strSelection,'') = ''
  BEGIN
    SET @strSelection = 'CLIENT'
  END

  
  DECLARE @tbl_UserList dbo.UserAccessList; 
  INSERT INTO @tbl_UserList EXEC dbo.stpr_GetClientAccessListNew @strUserid , @strSelection, @strCode 
  

  IF ISNULL(@strOptionType,'') = ''
  BEGIN
    SET @strOptionType = 'Listing'
  END
   
   
  IF @strOptionType = 'Listing'
  BEGIN
    SET @XMLDATA1 = (SELECT cm_cd [ClientId], cm_name [ClientName], [FatherHusbandName] = cb_FatherName, 
    [Address] = cm_add1+' '+cm_add2+' '+cm_add3+' '+cm_city+' - '+cm_pin+' '+cm_state+' '+cm_country, bm_branchname AS [Branch],
    rtrim(gr_desc) + ' (' + cm_groupcd + ')' AS [Group],  
	rtrim(fm_desc) + ' (' + cm_familycd + ')' AS [Family],  cm_blsavingcd 'TradingBOCode',
    rtrim(bo_description) AS [Occupation], [AcOpeningDate] = cm_opendate, cm_email [Email], cm_mobile as [Mobile],  
    [Tel1] = cm_tele1, [Tel2]= isnull(cm_tele2, ''), [AcClosureDt] = cm_acc_closuredate,  [PAN] = cb_panno,  
	convert(CHAR, convert(DATETIME, cm_dateofbirth), 103) [Dateofbirth], [Gender] = cb_sexcode, 
	rtrim(bc_description) AS [AccountType],  
	CASE cm_poaforpayin WHEN 'Y' THEN 'Yes' ELSE 'No' END [POA], rtrim(bs_description) AS [Status], 
	[Scheme] = cm_chgsscheme
    FROM Client_master WITH (NOLOCK) , branch_master WITH (NOLOCK), group_master WITH (NOLOCK), 
	family_master WITH (NOLOCK), client_backoffice WITH (NOLOCK), Beneficiary_category WITH (NOLOCK), 
	Beneficiary_occupation WITH (NOLOCK), Beneficiary_status WITH (NOLOCK), 
	Beneficiary_type WITH (NOLOCK), @tbl_UserList 
	WHERE cm_groupcd = gr_cd AND cm_familycd = fm_cd AND cm_cd = cb_cmcd AND cm_acctype = 
	bc_code AND bo_code = cm_occupation AND bs_code = cm_active AND cm_brboffcode = bm_branchcd AND cm_clienttype = 
	bt_code AND cm_schedule = 49843750 AND cm_active = '01' AND cm_opendate BETWEEN @dtFromDate AND @dtToDt
	AND CM_cD = CLIENTCODE FOR XML PATH('Detail'))
	
   END  
   ELSE IF @strOptionType = 'Bank'
   BEGIN
     SET @XMLDATA1 = (SELECT [ClientId] = cm_cd, [ClientName] = CM_NAME, [BankACNo] = cm_divbankacno, 
     [MICR] = cm_divbankcode, 
     [BankName] = (select bk_name from Bank_master with (noLock) where bk_micr = cm_divbankcode and bk_branch  = cb_voicemail),
     [IFSCCode] = cb_voicemail, 
	 [ACType] = ISNULL((SELECT rtrim(bt_description) from Beneficiary_type(NOLOCK) where bt_code =  cm_divbankccy),''), 
	 (CASE isNull(cb_ecs, '')  WHEN 'Y' THEN 'Yes' WHEN 'N' THEN 'No' ELSE '' END) AS [ECSMandate]
      FROM Client_Master WITH (NOLOCK), Client_Backoffice WITH (NOLOCK), @tbl_UserList 
     WHERE cm_cd = cb_cmcd  AND CM_cD = CLIENTCODE AND cm_schedule = 49843750 
	 AND cm_active = '01' AND cm_opendate BETWEEN @dtFromDate AND @dtToDt 
	 FOR XML PATH('Detail'))
   END
   ELSE IF @strOptionType = 'Scheme'
   BEGIN
     SET @XMLDATA1 = (SELECT [ClientId] = m.cm_cd, [ClientName] = m.CM_NAME, cm_chgsscheme [SchemeCode], 
     cg_name, CASE WHEN cd_fixed_amount = 0 THEN '-' ELSE ltrim(rtrim(convert(CHAR, cast(cd_fixed_amount AS 
							DECIMAL(15, 2))))) END AS cd_fixed_amount, CASE WHEN cd_perc_amount = 0 THEN '-' ELSE ltrim(
				rtrim(convert(CHAR, cast(cd_perc_amount AS DECIMAL(15, 2))))) END AS cd_perc_amount, CASE WHEN 
			cd_min_amount = 0 THEN '-' ELSE ltrim(rtrim(convert(CHAR, cast(cd_min_amount AS DECIMAL(15, 2))))) END 
	 AS cd_min_amount, CASE WHEN cd_max_amount = 0 THEN '-' ELSE ltrim(rtrim(convert(CHAR, cast(cd_max_amount AS 
							DECIMAL(15, 2))))) END AS cd_max_amount, CASE WHEN cd_per_certificate = 0 THEN '-' ELSE 
			ltrim(rtrim(convert(CHAR, cast(cd_per_certificate AS DECIMAL(15, 2))))) END AS cd_per_certificate
     FROM Chargesdetail WITH (NOLOCK), chargesmaster WITH (NOLOCK), Client_Master m, @tbl_UserList 
     WHERE cd_code = cg_code AND cd_scheme =  cm_chgsscheme AND cm_schedule = 49843750 AND cm_active = '01' 
	 AND cm_opendate BETWEEN @dtFromDate AND @dtToDt 
	 AND CM_cD = CLIENTCODE 
     ORDER BY cm_cd, CASE cd_code WHEN 4 THEN 1 ELSE 2 END, cd_code  FOR XML PATH('Detail'))
   END
   ELSE IF @strOptionType = 'POA'
   BEGIN
     SET @XMLDATA1 = (SELECT [ClientId] = cm_cd, [ClientName] = CM_NAME, cpm_firstname cm_name, 
	 convert(CHAR, convert(DATETIME, cpd_setupdate), 103) cm_poaregdate, cpd_poaid, 
	 cpd_holderno, CASE cpd_POAStatus WHEN 'D' THEN 'Deleted' WHEN 'E' THEN 'Expired' ELSE 'Setup' END cpd_POAStatus
     FROM client_poa_details WITH (NOLOCK), Corporate_poa_master WITH (NOLOCK), Client_Master(NOLOCK) CM, @tbl_UserList 
     WHERE cpd_poaid = cpm_poaid AND cpd_boid = CM_CD AND CM_cD = CLIENTCODE 
     AND cm_schedule = 49843750 AND cm_active = '01' AND cm_opendate BETWEEN @dtFromDate AND @dtToDt  FOR XML PATH('Detail'))
  END
  ELSE IF @strOptionType = 'NOMINEE'
  BEGIN
    SET @XMLDATA1 = (SELECT [ClientId] = cm_cd, [ClientName] = CM_NAME,cn_NomTitle, (
		cn_NomName + CASE WHEN cn_NomMidNm = '' THEN '' ELSE ' ' + cn_NomMidNm END + CASE WHEN cn_NomlastNm = '' THEN '' 
			ELSE ' ' + cn_NomlastNm END
		) AS [NomName],  [FatherHusbandName] = cn_FathHusbnm,  
		[Address] = cn_NomAdd1+' '+cn_NomAdd2+' '+cn_NomAdd3+' '+cn_City+' '+cn_NomPin+' '+cn_State+' '+cn_Country,
		[DOB] = cn_NomDOB, [PAN] = cn_NomPAN, [UID] = cn_NomUID, [Email] = cn_NomEmail, [Tel] = cn_NomTel, 
		Ltrim(Rtrim(Convert(CHAR, cn_purposeCd))) + '|' + Ltrim(Rtrim(Convert(CHAR, 
				cn_NomSrno))) AS Code,  CASE Ltrim(Rtrim(Convert(CHAR, cn_purposeCd))) + '|' + Ltrim(Rtrim(Convert(CHAR, 
					cn_NomSrno))) WHEN '7|1' THEN 1 WHEN '6|1' THEN 2 WHEN '8|1' THEN 3 WHEN '6|2' THEN 4 WHEN '8|2' THEN 5 
		WHEN '6|3' THEN 6 WHEN '8|3' THEN 7 ELSE 8 END OrderCd
    FROM Client_NomineeDetails WITH (NOLOCK), Client_Master cm, @tbl_UserList 
    WHERE  cn_cmcd = cm_Cd  AND cm_schedule = 49843750 AND cm_active = '01' AND cm_opendate BETWEEN @dtFromDate AND @dtToDt
	AND CM_cD = CLIENTCODE 
    ORDER BY cm_cd, OrderCd  FOR XML PATH('Detail'))
  END
  ELSE IF @strOptionType = 'HOLDING'
  BEGIN
    SET @XMLDATA1 = (SELECT [ClientId] = cm_cd, [ClientName] = CM_NAME, hld_isin_code as [ISIN], 
	sc_isinname [ScripName], bt_description as [Type], cast((hld_ac_pos) AS DECIMAL(15, 3)) AS [Qty], 
    cast((sc_rate) AS DECIMAL(15, 2)) AS [Rate], cast((sc_rate * hld_ac_pos) AS DECIMAL(15, 2)) AS [NetValue]
    FROM Holding(NOLOCK), Security(NOLOCK), Client_master(NOLOCK), Beneficiary_type(NOLOCK), branch_master(NOLOCK), @tbl_UserList 
    WHERE cm_brboffcode = bm_branchcd AND hld_isin_code = sc_isincode AND hld_ac_code = cm_cd AND bt_code = hld_ac_type 
	AND hld_ac_code = CLIENTCODE AND cm_schedule = 49843750 AND cm_active = '01' AND cm_opendate BETWEEN @dtFromDate AND @dtToDt
    ORDER BY cm_cd, hld_ac_type, sc_isinname FOR XML PATH('Detail'))
  END
  SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
  SET @o_vcErrorFlag = 'S'
  RETURN 1  
END  
GO

CREATE PROCEDURE stpr_ClientOutstanding @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, 
	@o_vcErrorMessage VARCHAR(MAX) OUTPUT WITH ENCRYPTION AS 
BEGIN
  SET @o_vcErrorMessage = 'Process Executed'
  SET @o_vcErrorFlag = 'S'
	--- Parameter Declaration
  DECLARE @dtFromDate VARCHAR(8), @dtToDt VARCHAR(8), @strUserId VARCHAR(500) = '', @strLevel VARCHAR(1), 
		@strCode VARCHAR(500), @strSelection VARCHAR(100), @XMLData XML, @strOutputType VARCHAR(1) = 'G', 
		@strRequestFrom VARCHAR(1) = 'W', @XMLDATA1 XML = '', @strShowValuation VARCHAR(20) = '', @strISIN VARCHAR(
			20), @strTrxType VARCHAR(200) = '', @strReport VARCHAR(50) = '', @strData VARCHAR(50) = '', @strDateType 
		VARCHAR(50) = '', @strSQL NVARCHAR(MAX) = ''

  IF @vcXML = ''
  BEGIN
	SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
	RETURN 1
  END

	--SELECT @vcXML


  SET @XMLData = CAST('<root>' + @vcXML + '</root>' AS XML)
  
  DECLARE @strBalanceFrom VARCHAR(50) = ''
  DECLARE @strBalanceTo VARCHAR(50) = ''
  DECLARE @strIncCreditBal VARCHAR(50) = 'False'
  DECLARE @strStatus VARCHAR(50) = ''
  DECLARE @strEmail VARCHAR(50) = 'False'
  DECLARE @strTelephone VARCHAR(50) = 'False'


  SELECT @dtFromDate = ISNULL(x.value('(FromDate)[1]', 'VARCHAR(8)'), ''), 
  @dtToDt = ISNULL(x.value('(ToDate)[1]', 'VARCHAR(8)'), ''), 
  @strUserId = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'), ''), 
  @strLevel = ISNULL(x.value('(Level)[1]', 'VARCHAR(1)'), ''), 
  @strCode = ISNULL(x.value('(Code)[1]', 'VARCHAR(500)'), ''), 
  @strSelection = ISNULL(x.value('(Selection)[1]', 'VARCHAR(50)'), ''), 
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'), ''), 
  @strRequestFrom = ISNULL(x.value('(RequestFrom)[1]', 'VARCHAR(1)'), ''), 
  @strBalanceFrom = ISNULL(x.value('(BalanceFrom)[1]', 'VARCHAR(50)'), ''), 
  @strBalanceTo = ISNULL(x.value('(BalanceTo)[1]', 'VARCHAR(50)'), ''), 
  @strIncCreditBal = ISNULL(x.value('(IncCreditBal)[1]', 'VARCHAR(50)'), 'False'), 
  @strStatus = ISNULL(x.value('(Status)[1]', 'VARCHAR(50)'), ''), 
  @strEmail = ISNULL(x.value('(Email)[1]', 'VARCHAR(50)'), 'False'),
  @strTelephone = ISNULL(x.value('(Telephone)[1]', 'VARCHAR(50)'), 'False')
  FROM @XMLData.nodes('/root') AS XTbl(x)

  IF ISNULL(@strSelection,'') = ''
  BEGIN
    SET @strSelection = 'CLIENT'
  END
  

  
  DECLARE @tbl_UserList dbo.UserAccessList; 
  INSERT INTO @tbl_UserList EXEC dbo.stpr_GetClientAccessListNew @strUserid , @strSelection, @strCode 
		
		

  SET @strSQL =' SELECT X1.*, Balance  FROM(SELECT CM_CD, cm_Name as [ClientName], cm_blsavingcd as [BackofficeCD], '
	+' cm_chgsscheme as [Scheme], bm_branchname as [Branch], cm_email as [Email], gr_desc as [Group],'
    +'	fm_desc as [Family], cm_tele1 as [Telephone] '
	+' FROM Client_master(NOLOCK), group_master(NOLOCK), branch_master(NOLOCK), family_master(NOLOCK)  '
  SET @strSQL = @strSQL + 
		' where cm_groupcd = gr_cd and cm_familycd = fm_cd and cm_brboffcode = bm_branchcd '
  SET @strSQL = @strSQL + 
		' and cm_schedule = (Select top 1 sp_sysvalue from sysparameter(NOLOCK) where sp_parmcd=''CMSCHEDULE'') '
  IF UPPER(@strStatus) = 'ACTIVE'
  BEGIN
	SET @strSQL = @strSQL + ' and  cm_active = ''01'') X1, '
  END
  ELSE IF UPPER(@strStatus) = 'INACTIVE'
  BEGIN
	SET @strSQL = @strSQL + ' and cm_active != ''01'') X1, '
  END
  ELSE
  BEGIN
    SET @strSQL = @strSQL + ' ) X1, '
  END
  
    SET @strSQL = @strSQL + '(SELECT ld_clientcd, SUM(ld_amount) Balance FROM Ledger(NOLOCK) '
    +' GROUP BY ld_clientcd Having sum(ld_amount)  <> 0 '
    
  IF TRIM(@strBalanceFrom) <> '' AND TRIM(@strBalanceTo) <> ''
  BEGIN
	SET @strSQL = @strSQL + 'and  abs(sum(ld_amount)) between ' + @strBalanceFrom + ' and ' + @strBalanceTo + ' '
  END

  IF TRIM(@strBalanceFrom) <> ''
  BEGIN
	SET @strSQL = @strSQL + 'and  abs(sum(ld_amount)) >=' + @strBalanceFrom
  END
	
  IF @strIncCreditBal <> 'true'
  BEGIN
	SET @strSQL = @strSQL + ' and (sum(ld_amount)) > 0 '
  END
  SET @strSQL = @strSQL+') X2 WHERE X1.CM_CD = X2.ld_clientcd ORDER BY X1.ClientName '
	
  CREATE TABLE #TBL_FinalData (ClientCode VARCHAR(50), ClientName VARCHAR(100), 
  BackofficeCD VARCHAR(50), Scheme VARCHAR(100), Branch VARCHAR(100), 
  Email VARCHAR(MAX), GroupCd VARCHAR(100), Family VARCHAR(100), Telephone  VARCHAR(50),Balance MONEY)
  

  INSERT INTO #TBL_FinalData
  EXEC sp_executesql @strSQL
  
  
  IF ISNULL(@strOutputType,'G') = 'X'
  BEGIN
    IF ISNULL(@strEmail,'FALSE') <> 'true' AND ISNULL(@strTelephone,'FALSE') <> 'true'
    BEGIN
     SET @XMLDATA1 = 
	    (SELECT X.ClientCode, X.ClientName, [BackofficeCD] = ISNULL(BackofficeCD,''), [Balance] = ISNULL(Balance,0), [Scheme] = ISNULL(Scheme,''), 
		[Branch] = ISNULL(Branch,''), 
		[Group] = ISNULL(GroupCd,''),[Family] = ISNULL(Family,'') FROM #TBL_FinalData X, @tbl_UserList Y WHERE X.ClientCode = Y.ClientCode ORDER BY X.ClientName  FOR XML PATH('Detail'))
	END
    ELSE IF ISNULL(@strEmail,'FALSE') <> 'true' AND ISNULL(@strTelephone,'FALSE') = 'true'
    BEGIN
      SET @XMLDATA1 = 
	    (SELECT X.ClientCode, X.ClientName, [BackofficeCD] = ISNULL(BackofficeCD,''), [Balance] = ISNULL(Balance,0), [Scheme] = ISNULL(Scheme,''), 
		[Branch] = ISNULL(Branch,''), 
		[Group] = ISNULL(GroupCd,''),[Family] = ISNULL(Family,''), [Telephone] = ISNULL(Telephone,'')
		FROM #TBL_FinalData X, @tbl_UserList Y WHERE X.ClientCode = Y.ClientCode ORDER BY X.ClientName  FOR XML PATH('Detail'))
	END
	ELSE IF ISNULL(@strEmail,'FALSE') = 'true' AND ISNULL(@strTelephone,'FALSE') = 'true'
    BEGIN
      SET @XMLDATA1 = 
	    (SELECT X.ClientCode, X.ClientName, [BackofficeCD] = ISNULL(BackofficeCD,''), [Balance] = ISNULL(Balance,0), [Scheme] = ISNULL(Scheme,''), 
		[Branch] = ISNULL(Branch,''), 
		[Group] = ISNULL(GroupCd,''),[Family] = ISNULL(Family,''), [Email] = ISNULL(Email,''), [Telephone] = ISNULL(Telephone,'')
		FROM #TBL_FinalData X, @tbl_UserList Y WHERE X.ClientCode = Y.ClientCode  ORDER BY X.ClientName  FOR XML PATH('Detail'))
	END
   	ELSE IF ISNULL(@strEmail,'FALSE') = 'true' AND ISNULL(@strTelephone,'FALSE') <> 'true'
    BEGIN
      SET @XMLDATA1 = 
	    (SELECT X.ClientCode, X.ClientName, [BackofficeCD] = ISNULL(BackofficeCD,''), [Balance] = ISNULL(Balance,0), [Scheme] = ISNULL(Scheme,''), 
		[Branch] = ISNULL(Branch,''), 
		[Group] = ISNULL(GroupCd,''),[Family] = ISNULL(Family,''), [Email] = ISNULL(Email,'')
		FROM #TBL_FinalData X, @tbl_UserList Y WHERE X.ClientCode = Y.ClientCode ORDER BY X.ClientName  FOR XML PATH('Detail'))
	 END
   DROP TABLE #TBL_FinalData  
   SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
   SET @o_vcErrorFlag = 'S'
   RETURN 1
  END
END  
GO

CREATE PROCEDURE stpr_ClientPerformancerep @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, 
	@o_vcErrorMessage VARCHAR(MAX) OUTPUT WITH ENCRYPTION AS 
BEGIN  
  SET @o_vcErrorMessage = 'Process Executed'
  SET @o_vcErrorFlag = 'S'
	--- Parameter Declaration
  DECLARE @dtFromDate VARCHAR(8), @dtToDt VARCHAR(8), @strUserId VARCHAR(500) = '', @strLevel VARCHAR(1), 
		@strCode VARCHAR(500), @strSelection VARCHAR(100), @XMLData XML, @strOutputType VARCHAR(1) = 'G', 
		@strRequestFrom VARCHAR(1) = 'W', @XMLDATA1 XML = '', @strShowValuation VARCHAR(20) = '', @strISIN VARCHAR(
			20), @strTrxType VARCHAR(200) = '', @strReport VARCHAR(50) = '', @strData VARCHAR(50) = '', @strDateType 
		VARCHAR(50) = '', @strSQL NVARCHAR(MAX) = ''

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
  @strSelection = ISNULL(x.value('(Selection)[1]', 'VARCHAR(50)'), ''), 
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'), ''), 
  @strRequestFrom = ISNULL(x.value('(RequestFrom)[1]', 'VARCHAR(1)'), ''), 
  @strReport = ISNULL(x.value('(Report)[1]', 'VARCHAR(50)'), ''), 
  @strData = ISNULL(x.value('(Data)[1]', 'VARCHAR(50)'), '')
  FROM @XMLData.nodes('/root') AS XTbl(x)



  IF isnull(@strReport,'') = ''
  BEGIN
    SET @strReport = 'BO'
  END
  
  IF isnull(@strData,'') = ''
  BEGIN
    SET @strData = '0'
  END
   --- USER ACCESS RIGHTS  
  
  IF ISNULL(@strSelection,'') = ''
  BEGIN
    SET @strSelection = 'CLIENT'
  END

     
   
  IF @StrCode <> '' AND UPPER(@strSelection) = 'CLIENT'
  BEGIN
    SET @strUserid = @StrCode
  END
  

  
  INSERT INTO @tbl_UserList EXEC dbo.stpr_GetClientAccessListNew @strUserid, @strSelection,@strCode
   
  CREATE TABLE #TempTrxDetail (
  td_ac_code VARCHAR(16), td_isin_code VARCHAR(12), DematCr NUMERIC(18, 3), OnMktDr NUMERIC(18, 3), 
  OnMktCr NUMERIC(18, 3), OffMktDr NUMERIC(18, 3), OffMktCr NUMERIC(18, 3), CACr NUMERIC(18, 3), IPOCr NUMERIC(18, 3), 
  EarlypayinDr NUMERIC(18, 3), InterDPBODr NUMERIC(18, 3), InterDPExDr NUMERIC(18, 3), 
  InterDPBOCr NUMERIC(18, 3), InterDPExCr NUMERIC(18, 3))

    
  INSERT INTO #TempTrxDetail
  SELECT td_ac_code, td_isin_code, 
  ISNULL(Sum(CASE td_debit_credit WHEN 'C' THEN CASE td_narration WHEN '011' 
  THEN (CASE WHEN ISNULL(@strData,'0') = '0' THEN td_qty WHEN ISNULL(@strData,'0') = '2' 
											THEN td_qty*td_rate ELSE td_qty END) END END), 0) 'DematCr', 
  ISNULL(Sum(CASE td_debit_credit WHEN 'D' THEN CASE td_narration WHEN '052' 
  THEN (CASE WHEN ISNULL(@strData,'0') = '0' THEN td_qty WHEN ISNULL(@strData,'0') = '2' 
											THEN td_qty*td_rate ELSE td_qty END) END END), 0) 'OnMktDr', 
  ISNULL(Sum(CASE td_debit_credit WHEN 'C' THEN CASE td_narration WHEN '052' 
  THEN (CASE WHEN ISNULL(@strData,'0') = '0' THEN td_qty WHEN ISNULL(@strData,'0') = '2' 
											THEN td_qty*td_rate ELSE td_qty END) END END), 0) 'OnMktCr', 
  isnull(Sum(CASE td_debit_credit WHEN 'D' THEN CASE WHEN td_narration IN ('044', '042') 
  THEN (CASE WHEN ISNULL(@strData,'0') = '0' THEN td_qty WHEN ISNULL(@strData,'0') = '2' 
											THEN td_qty*td_rate ELSE td_qty END) END END), 0) 'OffMktDr', 
  ISNULL(SUM(CASE td_debit_credit WHEN 'C' THEN CASE WHEN td_narration IN ('044', '042') 
  THEN (CASE WHEN ISNULL(@strData,'0') = '0' THEN td_qty WHEN ISNULL(@strData,'0') = '2' 
											THEN td_qty*td_rate ELSE td_qty END) END END), 0) 'OffMktCr', 
  ISNULL(SUM(CASE td_debit_credit WHEN 'C' THEN CASE td_narration WHEN '082' 
  THEN (CASE WHEN ISNULL(@strData,'0') = '0' THEN td_qty WHEN ISNULL(@strData,'0') = '2' 
											THEN td_qty*td_rate ELSE td_qty END) END END), 0) 'CACr', 
  ISNULL(Sum(CASE td_debit_credit WHEN 'C' THEN CASE td_narration WHEN '082' 
  THEN (CASE WHEN ISNULL(@strData,'0') = '0' THEN td_qty WHEN ISNULL(@strData,'0') = '2' 
											THEN td_qty*td_rate ELSE td_qty END) END END), 0) 'IPOCr', 
  ISNULL(SUM(CASE td_debit_credit WHEN 'D' THEN CASE td_narration WHEN '054' 
  THEN (CASE WHEN ISNULL(@strData,'0') = '0' THEN td_qty WHEN ISNULL(@strData,'0') = '2' 
											THEN td_qty*td_rate ELSE td_qty END) END END), 0) 'EarlypayinDr', 
  ISNULL(SUM(CASE WHEN td_debit_credit = 'D' AND td_narration IN ('202', '204') 
  THEN CASE WHEN Len(ltrim(Rtrim(td_counterdp))) = 8 AND Len(ltrim(Rtrim(td_beneficiery))) = 8 AND Left(Ltrim(Rtrim(td_counterdp)), 2) = 'IN' 
  THEN (CASE WHEN ISNULL(@strData,'0') = '0' THEN td_qty WHEN ISNULL(@strData,'0') = '2' THEN td_qty*td_rate ELSE td_qty END) ELSE 0 END ELSE 0 END), 0) 'InterDPBODr', 
  ISNULL(SUM(CASE WHEN td_debit_credit = 'D' AND td_narration IN ('202', '204') THEN CASE WHEN Len(ltrim(Rtrim(td_counterdp))) = 8 
  AND Len(ltrim(Rtrim(td_beneficiery))) = 8 AND Left(Ltrim(Rtrim(td_counterdp)), 2) = 'IN' THEN 0 
  ELSE (CASE WHEN ISNULL(@strData,'0') = '0' THEN td_qty WHEN ISNULL(@strData,'0') = '2' THEN td_qty*td_rate ELSE td_qty END) END ELSE 0 END), 0) 'InterDPExDr', 
  isnull(sum(CASE WHEN td_debit_credit = 'C' AND td_narration IN ('202', '204') THEN CASE WHEN Len(ltrim(Rtrim(td_counterdp))) = 8 
  AND Len(ltrim(Rtrim(td_beneficiery))) = 8 AND Left(Ltrim(Rtrim(td_counterdp)), 2) = 'IN' THEN (CASE WHEN ISNULL(@strData,'0') = '0' THEN td_qty 
  WHEN ISNULL(@strData,'0') = '2' THEN td_qty*td_rate ELSE td_qty END) ELSE 0 END ELSE 0 END), 0) 'InterDPBOCr', 
  isnull(sum(CASE WHEN td_debit_credit = 'C' AND td_narration IN ('202', '204') THEN CASE WHEN Len(ltrim(Rtrim(td_counterdp))) = 8 
  AND Len(ltrim(Rtrim(td_beneficiery))) = 8 AND Left(Ltrim(Rtrim(td_counterdp)), 2) = 'IN' THEN 0 ELSE (CASE WHEN ISNULL(@strData,'0') = '0' 
  THEN td_qty WHEN ISNULL(@strData,'0') = '2' THEN td_qty*td_rate ELSE td_qty END) END ELSE 0 END), 0) 'InterDPExCr'
  FROM trxdetail WITH (NOLOCK), @tbl_UserList X
  WHERE td_ac_code = ClientCode 
  AND td_curdate BETWEEN @dtFromDate AND @dtToDt
  GROUP BY td_ac_code, td_isin_code
    
	
  IF ISNULL(@strOutputType,'G') = 'G'
  BEGIN
    IF @strReport = 'BO'
	BEGIN
	  SELECT cm_cd AS [ClientID], cm_name AS [ClientName], cast((sum(DematCr)) AS DECIMAL(15, 0)) [DematCr], cast((sum(OnMktDr)
			) AS DECIMAL(15, 0)) [OnMarketDr], cast((sum(OnMktCr)) AS DECIMAL(15, 0)) [OnMarketCr], cast((sum(OffMktDr)
			) AS DECIMAL(15, 0)) [OffMarketDr], cast((sum(OffMktCr)) AS DECIMAL(15, 0)) [OffMarketCr], cast((sum(CACr)
			) AS DECIMAL(15, 0)) [CACr], cast((sum(IPOCr)) AS DECIMAL(15, 0)) IPOCr, cast((sum(EarlypayinDr)
			) AS DECIMAL(15, 0)) [EarlypayinDr], cast((sum(InterDPBODr)) AS DECIMAL(15, 0)) [InterDPBODr], 
	   cast((sum(InterDPExDr)) AS DECIMAL(15, 0)) [InterDPExDr], cast((sum(InterDPBOCr)) AS 
		DECIMAL(15, 0)) [InterDPBOCr], cast((sum(InterDPExCr)) AS DECIMAL(15, 0)) [InterDPExCr]
       FROM #TempTrxDetail, Client_master (NOLOCK), ChargesDetail WITH (NOLOCK), Security(NOLOCK)
       WHERE td_ac_code = cm_cd AND td_isin_code = sc_isincode  AND cd_code = 4
       GROUP BY cm_cd, cm_name
       HAVING (sum(OnMktCr) > 0 OR sum(OnMktDr) > 0 OR sum(OffMktCr) > 0 OR sum(OffMktDr) > 0 OR sum(DematCr) > 0 OR sum(
			InterDPBODr) > 0 OR sum(IPOCr) > 0 OR sum(InterDPExDr) > 0 OR sum(InterDPBOCr) > 0 OR sum(InterDPExCr) > 0
			OR SUM(CACr)> 0 OR SUM(EarlypayinDr)>0)
       ORDER BY cm_cd, cm_name
	END
    ELSE IF @strReport = 'isin'
	BEGIN
	  SELECT td_isin_code AS [ISIN], sc_isinName AS [ISINName], cast((sum(DematCr)) AS DECIMAL(15, 0)) [DematCr], cast((sum(OnMktDr)
			) AS DECIMAL(15, 0)) [OnMarketDr], cast((sum(OnMktCr)) AS DECIMAL(15, 0)) [OnMarketCr], cast((sum(OffMktDr)
			) AS DECIMAL(15, 0)) [OffMarketDr], cast((sum(OffMktCr)) AS DECIMAL(15, 0)) [OffMarketCr], cast((sum(CACr)
			) AS DECIMAL(15, 0)) [CACr], cast((sum(IPOCr)) AS DECIMAL(15, 0)) IPOCr, cast((sum(EarlypayinDr)
			) AS DECIMAL(15, 0)) [EarlypayinDr], cast((sum(InterDPBODr)) AS DECIMAL(15, 0)) [InterDPBODr], 
	   cast((sum(InterDPExDr)) AS DECIMAL(15, 0)) [InterDPExDr], cast((sum(InterDPBOCr)) AS 
		DECIMAL(15, 0)) [InterDPBOCr], cast((sum(InterDPExCr)) AS DECIMAL(15, 0)) [InterDPExCr]
      FROM #TempTrxDetail, Client_master (NOLOCK), ChargesDetail WITH (NOLOCK), Security(NOLOCK)
       WHERE td_ac_code = cm_cd AND td_isin_code = sc_isincode and cm_chgsscheme = cd_scheme AND cd_code = 4
	   AND td_isin_code = sc_isincode
       GROUP BY td_isin_code, sc_isinName
       HAVING (sum(OnMktCr) > 0 OR sum(OnMktDr) > 0 OR sum(OffMktCr) > 0 OR sum(OffMktDr) > 0 OR sum(DematCr) > 0 OR sum(
			InterDPBODr) > 0 OR sum(IPOCr) > 0 OR sum(InterDPExDr) > 0 OR sum(InterDPBOCr) > 0 OR sum(InterDPExCr) > 0
			OR SUM(CACr)> 0 OR SUM(EarlypayinDr)>0)
       ORDER BY sc_isinName
	END
    SET @o_vcErrorMessage = 'Process Executed'
	SET @o_vcErrorFlag = 'S'
	RETURN 1
  END	
  ELSE IF ISNULL(@strOutputType,'G') = 'X'
  BEGIN
	IF @strReport = 'BO'
	BEGIN
	  SET @XMLDATA1 = 
	    (SELECT cm_cd AS [ClientID], cm_name AS [ClientName], cast((sum(DematCr)) AS DECIMAL(15, 0)) [DematCr], cast((sum(OnMktDr)
			) AS DECIMAL(15, 0)) [OnMarketDr], cast((sum(OnMktCr)) AS DECIMAL(15, 0)) [OnMarketCr], cast((sum(OffMktDr)
			) AS DECIMAL(15, 0)) [OffMarketDr], cast((sum(OffMktCr)) AS DECIMAL(15, 0)) [OffMarketCr], cast((sum(CACr)
			) AS DECIMAL(15, 0)) [CACr], cast((sum(IPOCr)) AS DECIMAL(15, 0)) IPOCr, cast((sum(EarlypayinDr)
			) AS DECIMAL(15, 0)) [EarlypayinDr], cast((sum(InterDPBODr)) AS DECIMAL(15, 0)) [InterDPBODr], 
	     cast((sum(InterDPExDr)) AS DECIMAL(15, 0)) [InterDPExDr], cast((sum(InterDPBOCr)) AS 
		 DECIMAL(15, 0)) [InterDPBOCr], cast((sum(InterDPExCr)) AS DECIMAL(15, 0)) [InterDPExCr]
         FROM #TempTrxDetail, Client_master (NOLOCK), ChargesDetail WITH (NOLOCK), Security(NOLOCK)
         WHERE td_ac_code = cm_cd AND td_isin_code = sc_isincode  AND cd_code = 4
         GROUP BY cm_cd, cm_name
         HAVING (sum(OnMktCr) > 0 OR sum(OnMktDr) > 0 OR sum(OffMktCr) > 0 OR sum(OffMktDr) > 0 OR sum(DematCr) > 0 OR sum(
			InterDPBODr) > 0 OR sum(IPOCr) > 0 OR sum(InterDPExDr) > 0 OR sum(InterDPBOCr) > 0 OR sum(InterDPExCr) > 0
			OR SUM(CACr)> 0 OR SUM(EarlypayinDr)>0)
         ORDER BY cm_cd, cm_name FOR XML PATH('Detail'))
	END
    ELSE IF @strReport = 'isin'
	BEGIN
	  SET @XMLDATA1 = 
	      (SELECT td_isin_code AS [ISIN], sc_isinName AS [ISINName], cast((sum(DematCr)) AS DECIMAL(15, 0)) [DematCr], cast((sum(OnMktDr)
			) AS DECIMAL(15, 0)) [OnMarketDr], cast((sum(OnMktCr)) AS DECIMAL(15, 0)) [OnMarketCr], cast((sum(OffMktDr)
			) AS DECIMAL(15, 0)) [OffMarketDr], cast((sum(OffMktCr)) AS DECIMAL(15, 0)) [OffMarketCr], cast((sum(CACr)
			) AS DECIMAL(15, 0)) [CACr], cast((sum(IPOCr)) AS DECIMAL(15, 0)) IPOCr, cast((sum(EarlypayinDr)
			) AS DECIMAL(15, 0)) [EarlypayinDr], cast((sum(InterDPBODr)) AS DECIMAL(15, 0)) [InterDPBODr], 
	      cast((sum(InterDPExDr)) AS DECIMAL(15, 0)) [InterDPExDr], cast((sum(InterDPBOCr)) AS 
		  DECIMAL(15, 0)) [InterDPBOCr], cast((sum(InterDPExCr)) AS DECIMAL(15, 0)) [InterDPExCr]
          FROM #TempTrxDetail, Client_master (NOLOCK), ChargesDetail WITH (NOLOCK), Security(NOLOCK)
          WHERE td_ac_code = cm_cd AND td_isin_code = sc_isincode and cm_chgsscheme = cd_scheme AND cd_code = 4
	      AND td_isin_code = sc_isincode
          GROUP BY td_isin_code, sc_isinName
          HAVING (sum(OnMktCr) > 0 OR sum(OnMktDr) > 0 OR sum(OffMktCr) > 0 OR sum(OffMktDr) > 0 OR sum(DematCr) > 0 OR sum(
		  InterDPBODr) > 0 OR sum(IPOCr) > 0 OR sum(InterDPExDr) > 0 OR sum(InterDPBOCr) > 0 OR sum(InterDPExCr) > 0
		  OR SUM(CACr)> 0 OR SUM(EarlypayinDr)>0)
         ORDER BY sc_isinName FOR XML PATH('Detail'))
	END
	SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	SET @o_vcErrorFlag = 'S'
	RETURN 1
  END
END
GO

CREATE PROCEDURE stpr_DematListing @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, 
	@o_vcErrorMessage VARCHAR(MAX) OUTPUT WITH ENCRYPTION AS 
BEGIN
  SET @o_vcErrorMessage = 'Process Executed'
  SET @o_vcErrorFlag = 'S'
	--- Parameter Declaration
  DECLARE @strFromDate VARCHAR(8), @strToDate VARCHAR(8), @strUserId VARCHAR(500) = '', @strLevel VARCHAR(1), 
		@strCode VARCHAR(500), @strSelection VARCHAR(100), @XMLData XML, @strOutputType VARCHAR(1) = 'G', 
		@strRequestFrom VARCHAR(1) = 'W', @XMLDATA1 XML = '', @strShowValuation VARCHAR(20) = '', @strISIN VARCHAR(
			20), @strTrxType VARCHAR(200) = '', @strReport VARCHAR(50) = '', @strData VARCHAR(50) = '', @strDateType 
		VARCHAR(50) = '', @strSQL NVARCHAR(MAX) = ''

  IF @vcXML = ''
  BEGIN
	SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
	RETURN 1
  END

	--SELECT @vcXML

  SET @XMLData = CAST('<root>' + @vcXML + '</root>' AS XML)

  DECLARE @strType VARCHAR(50) = ''

  SELECT @strFromDate = ISNULL(x.value('(FromDate)[1]', 'VARCHAR(8)'), ''), 
  @strToDate = ISNULL(x.value('(ToDate)[1]', 'VARCHAR(8)'), ''), 
  @strUserId = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'), ''), 
  @strLevel = ISNULL(x.value('(Level)[1]', 'VARCHAR(1)'), ''), 
  @strCode = ISNULL(x.value('(Code)[1]', 'VARCHAR(500)'), ''), 
  @strSelection = ISNULL(x.value('(Selection)[1]', 'VARCHAR(50)'), ''), 
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'), ''), 
  @strRequestFrom = ISNULL(x.value('(RequestFrom)[1]', 'VARCHAR(1)'), ''), 
  @strType = ISNULL(x.value('(Type)[1]', 'VARCHAR(50)'), 'False')
  FROM @XMLData.nodes('/root') AS XTbl(x)

  IF ISNULL(@strSelection,'') = ''
  BEGIN
    SET @strSelection = 'CLIENT'
  END

  
  DECLARE @tbl_UserList dbo.UserAccessList; 
  INSERT INTO @tbl_UserList EXEC dbo.stpr_GetClientAccessListNew @strUserid , @strSelection, @strCode 
	
  IF ISNULL(@strType,'') = ''
  BEGIN
	 SET @strType = 'ALL'
  END
  IF ISNULL(@strOutputType,'G') = 'X'
  BEGIN
    IF @strType = 'Demat Request'
    BEGIN
	  SET @XMLDATA1 = (SELECT CONVERT(CHAR, convert(DATETIME, a.td_curdate), 106) AS DATE, a.td_reference AS [DRNNo], 
	  a.td_internal_refno AS [DRFNo], cm_cd AS [ClientCode], cm_name AS [ClientName], a.td_isin_code AS [ISIN], 
	  sc_isinname AS [ISINName], cast((a.td_qty) AS DECIMAL(15, 3)) AS Qty, a.td_description AS [Description]
	  FROM Trxdetail(NOLOCK) a
	  LEFT OUTER JOIN Dematmaster(NOLOCK) ON ABS(td_reference) = dm_drn, Client_master(NOLOCK), 
	  Security(NOLOCK), @tbl_UserList
	   WHERE cm_cd = ClientCode AND a.td_ac_code = cm_cd AND a.td_narration = '011' AND a.td_isin_code = sc_isincode 
	  AND td_curdate BETWEEN @strFromDate AND @strToDate ORDER BY TD_CURDATE FOR XML PATH('Detail'))
	END
	ELSE IF @strType = 'Partially Confirmed'
	BEGIN
	 SET @XMLDATA1 = (SELECT convert(CHAR, convert(DATETIME, a.td_curdate), 106) AS DATE, 
	 a.td_reference AS [DRNNo], a.td_internal_refno AS [DRFNo], cm_cd AS [ClientCode], cm_name AS [ClientName], 
	 a.td_isin_code AS [ISIN], sc_isinname AS [ISINName], cast((a.td_qty) AS DECIMAL(15, 3)) AS Qty, 
	 td_description AS [Description]
	 FROM Trxdetail(NOLOCK) a, Client_master(NOLOCK), Security(NOLOCK), @tbl_UserList
	 WHERE cm_cd = ClientCode AND a.td_ac_code = cm_cd AND a.td_isin_code = sc_isincode AND a.td_narration = '011' 
	 AND td_booking_type NOT IN ('13') AND a.td_qty = (SELECT sum(b.td_qty) FROM trxdetail(NOLOCK) b
	 WHERE b.td_reference = a.td_reference AND b.td_narration = '012' AND b.td_debit_credit = 'C' 
	 AND td_curdate BETWEEN @strFromDate AND @strToDate) ORDER BY TD_CURDATE  FOR XML PATH('Detail'))
	END
	ELSE IF @strType = 'Demat Confirmation'
	BEGIN
	  SET @XMLDATA1 = (SELECT CONVERT(CHAR, convert(DATETIME, a.td_curdate), 106) AS DATE, a.td_reference AS [DRNNo], 
	  a.td_internal_refno AS [DRFNo], cm_cd AS [ClientCode], cm_name AS [ClientName], a.td_isin_code AS [ISIN], 
	  sc_isinname AS [ISINName], cast((a.td_qty) AS DECIMAL(15, 3)) AS Qty, a.td_description AS [Description]
	  FROM Trxdetail(NOLOCK) a
	  LEFT OUTER JOIN Dematmaster(NOLOCK) ON abs(td_reference) = dm_drn, Client_master(NOLOCK), Security(NOLOCK), @tbl_UserList 
	  WHERE cm_cd = ClientCode AND a.td_ac_code = cm_cd AND a.td_narration = '012' AND a.td_debit_credit = 'C' AND a.
	  td_isin_code = sc_isincode AND td_curdate BETWEEN @strFromDate AND @strToDate ORDER BY TD_CURDATE   FOR XML PATH('Detail'))
	END
	ELSE IF @strType = 'Demat Rejection'
	BEGIN
	  SET @XMLDATA1 = (SELECT convert(CHAR, convert(DATETIME, a.td_curdate), 106) AS DATE, a.td_reference AS [DRNNo], 
	  a.td_internal_refno AS [DRFNo], cm_cd AS [ClientCode], cm_name AS [ClientName], a.td_isin_code AS [ISIN], 
	  sc_isinname AS [ISINName], cast((a.td_qty) AS DECIMAL(15, 3)) AS Qty, a.td_description AS [Description] 
	  FROM Trxdetail(NOLOCK) a LEFT OUTER JOIN Dematmaster(NOLOCK) ON abs(td_reference) = dm_drn, Client_master(NOLOCK), 
	  Security(NOLOCK), @tbl_UserList
	  WHERE cm_cd = ClientCode AND a.td_ac_code = cm_cd AND a.td_narration IN ('013', '014') 
	  AND a.td_isin_code = sc_isincode AND td_curdate BETWEEN @strFromDate AND @strToDate ORDER BY TD_CURDATE   FOR XML PATH('Detail'))
	END
	ELSE IF @strType = 'Pending Demat'
	BEGIN
		SET @XMLDATA1 = (SELECT convert(CHAR, convert(DATETIME, a.td_curdate), 106) AS DATE, a.td_reference AS [DRNNo], a.
			td_internal_refno AS [DRFNo], cm_cd AS [ClientCode], cm_name AS [ClientName], a.td_isin_code AS [ISIN]
			, sc_isinname AS [ISINName], cast((a.td_qty) AS DECIMAL(15, 3)) AS Qty, 
			td_description AS [Description]
		FROM Trxdetail(NOLOCK) a, Client_master(NOLOCK), Security(NOLOCK), @tbl_UserList --,sc_company_name
		WHERE cm_cd = ClientCode AND td_ac_code = cm_cd AND td_isin_code = sc_isincode AND td_narration = '011' AND 
			td_booking_type NOT IN ('13') AND td_reference NOT IN (
				SELECT td_reference
				FROM Trxdetail(NOLOCK)
				WHERE td_narration IN ('012', '013', '014') AND td_reference = a.td_reference 
					AND td_curdate >= a.td_curdate
				) AND a.td_curdate <= convert(CHAR, dateadd(day, - 0, CONVERT(VARCHAR(8), GETDATE(), 112)), 112) ORDER BY TD_CURDATE   FOR XML PATH('Detail'))
			--change days
	END
	ELSE IF @strType = 'ALL'
	BEGIN
		SET @XMLDATA1 = (SELECT convert(CHAR, convert(DATETIME, a.td_curdate), 106) AS DATE, a.td_reference AS [DRNNo], a.
			td_internal_refno AS [DRFNo], cm_cd AS [ClientCode], cm_name AS [ClientName], a.td_isin_code AS [ISIN]
			, sc_isinname AS [ISINName], cast((a.td_qty) AS DECIMAL(15, 3)) AS Qty, a.
			td_description AS [Description] 
		FROM Trxdetail(NOLOCK) a
		LEFT OUTER JOIN Dematmaster(NOLOCK) ON abs(td_reference) = dm_drn, Client_master(NOLOCK), Security(NOLOCK), @tbl_UserList
		WHERE cm_cd = ClientCode AND a.td_ac_code = cm_cd AND a.td_narration IN ('011', '012', '013', '014'
				) AND a.td_isin_code = sc_isincode AND td_curdate BETWEEN @strFromDate AND @strToDate AND (
				td_Narration = '011' OR td_Narration = '013' OR (td_Narration = '012' AND td_debit_credit = 'C'
					)
				) ORDER BY TD_CURDATE   FOR XML PATH('Detail'))
	END
  END
  SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
  SET @o_vcErrorFlag = 'S'
  RETURN 1  
END
GO

CREATE PROCEDURE stpr_TransactionStatus @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, 
	@o_vcErrorMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN
    SET @o_vcErrorMessage = 'Process Executed'
	SET @o_vcErrorFlag = 'S'
	--- Parameter Declaration
	DECLARE @dtFromDate VARCHAR(8), @dtToDt VARCHAR(8), @strUserId VARCHAR(500) = '', @strLevel VARCHAR(1), 
		@strCode VARCHAR(500), @strSelection VARCHAR(100), @XMLData XML, @strOutputType VARCHAR(1) = 'G', 
		@strRequestFrom VARCHAR(1) = 'W', @XMLDATA1 XML = '', @strShowValuation VARCHAR(20) = '', @strISIN VARCHAR(
			20), @strTrxType VARCHAR(200) = '', @strType VARCHAR(50) = '', @strStatus VARCHAR(50) = '', @strDateType 
		VARCHAR(50) = '', @strSQL NVARCHAR(MAX) = ''

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
	@strSelection = ISNULL(x.value('(Selection)[1]', 'VARCHAR(50)'), ''), 
	@strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'), ''), 
    @strRequestFrom = ISNULL(x.value('(RequestFrom)[1]', 'VARCHAR(1)'), ''), 
	@strType = ISNULL(x.value('(Type)[1]', 'VARCHAR(50)'), ''), 
	@strStatus = ISNULL(x.value('(Status)[1]', 'VARCHAR(50)'), ''), 
	@strDateType = ISNULL(x.value('(DateType)[1]', 'VARCHAR(50)'), '')
	FROM @XMLData.nodes('/root') AS XTbl(x)

	SET @strStatus = ISNULL(@strStatus, '')
	SET @strDateType = ISNULL(@strDateType, '')
	SET @strType = ISNULL(@strType,'')

	IF ISNULL(@strSelection,'') = ''
    BEGIN
      SET @strSelection = 'CLIENT'
    END


	INSERT INTO @tbl_UserList
	EXEC dbo.stpr_GetClientAccessListNew @strUserid, @strSelection, @strCode
	
	
	
	
	

    DECLARE @TBL_TRXDETAIL TABLE(KeyField VARCHAR(50), 
	SlipNo VARCHAR(20),TrxDate VARCHAR(20),
	ClientCode VARCHAR(20), ClientName VARCHAR(200), ISIN VARCHAR(20),
	CompanyName VARCHAR(200),Qty MONEY, STATUS  VARCHAR(200),ExecDate VARCHAR(20), NoOfTrx INT)
    
	IF ISNULL(@strType,'') = ''
	BEGIN
	  SET @strType ='ALL'
	END
	
	IF @strType = '925' OR ISNULL(@strType,'ALL') = 'ALL'
	BEGIN
	    INSERT INTO @TBL_TRXDETAIL(KeyField, SlipNo, TrxDate, ClientCode, ClientName, ISIN, CompanyName, Qty,
		STATUS, ExecDate, NoOfTrx) 
		SELECT id_trxtype + '/' + id_instcd + '/' + id_internalrefno KeyField, id_internalrefno AS SlipNo, CONVERT(
				CHAR, convert(DATETIME, id_trxdate), 103) TrxDate, id_clientid ClientCode, cm_name ClientName, 
			id_isin AS ISIN, sc_isinname AS CompanyName, id_qty AS [Qty], sx_description AS STATUS, CONVERT(CHAR, 
				convert(DATETIME, id_exec_date), 103) ExecDate, Count(0) NoOfTrx
		FROM Interdepository(NOLOCK), Security(NOLOCK), Statusof_trx(NOLOCK), client_master(NOLOCK), 
			@tbl_UserList
		WHERE cm_cd = ClientCode AND id_isin = sc_isincode AND sx_code = id_status AND sx_trxtype = id_trxtype AND 
			id_clientid = cm_cd AND (
				(sx_code = @strStatus AND @strStatus NOT IN ('13', '')) OR 
				@strStatus IN ('13', '')
				) AND (
				(id_trxdate BETWEEN @dtFromDate AND @dtToDt AND @strDateType = 'Transaction') 
				OR (@strDateType <> 'Transaction' AND id_exec_date BETWEEN @dtFromDate AND @dtToDt
					)
				)
		GROUP BY id_internalrefno, id_clientid, cm_name, id_trxdate, id_exec_date, id_instcd, id_trxtype, 
			id_isin, sc_isinname, id_qty, sx_description
	END
	ELSE IF @strType = '901' OR ISNULL(@strType,'ALL') = 'ALL'
	BEGIN
	    INSERT INTO @TBL_TRXDETAIL(KeyField, SlipNo, TrxDate, ClientCode, ClientName, ISIN, CompanyName, Qty,
		STATUS, ExecDate, NoOfTrx) 
		SELECT '901' + '/' + dm_instcd + '/' + dm_irn KeyField, dm_irn AS SlipNo, convert(CHAR, convert(DATETIME, 
					dm_dmat_date), 103) TrxDate, dm_client_id ClientCode, cm_name ClientName, dm_isin_code AS ISIN
			, sc_company_name AS CompanyName, dm_dematqty AS [Qty], sx_description AS STATUS, convert(CHAR, 
				convert(DATETIME, dm_dmat_date), 103) ExecDate, SUM(dm_total_certificate) NoOfTrx
		FROM DematMaster(NOLOCK), Security(NOLOCK), Statusof_trx(NOLOCK), client_master(NOLOCK), 
			@tbl_UserList
		WHERE cm_cd = ClientCode AND dm_isin_code = sc_isincode AND dm_status = sx_code AND sx_trxtype = '901' AND 
			dm_client_id = cm_cd AND (
				(sx_code = @strStatus AND @strStatus NOT IN ('13', '')) OR 
				@strStatus IN ('13', '')
				) AND dm_dmat_date BETWEEN @dtFromDate AND @dtToDt
		GROUP BY dm_irn, dm_client_id, cm_name, dm_dmat_date, dm_instcd, dm_isin_code, sc_company_name, 
			dm_dematqty, sx_description
	END
	ELSE IF (@strType = '906' OR @strType = '904' OR @strType = '903' OR ISNULL(@strType,'ALL') = 'ALL' ) 
	BEGIN
		INSERT INTO @TBL_TRXDETAIL(KeyField, SlipNo, TrxDate, ClientCode, ClientName, ISIN, CompanyName, Qty,
		STATUS, ExecDate, NoOfTrx) 
		SELECT tb_trx_type + '/' + tb_instcd + '/' + tb_internal_refno KeyField, tb_internal_refno AS SlipNo, 
			convert(CHAR, convert(DATETIME, tb_trx_date), 103) TrxDate, tb_client_id ClientCode, cm_name 
			ClientName, tb_isin AS ISIN, sc_isinname AS CompanyName, tb_qty AS [Qty], sx_description AS STATUS, 
			convert(CHAR, convert(DATETIME, tb_exec_date), 103) ExecDate, Count(0) NoOfTrx
		FROM TrxBackoffice(NOLOCK), Security(NOLOCK), Statusof_trx(NOLOCK), client_master(NOLOCK), 
			@tbl_UserList
		WHERE cm_cd = ClientCode AND tb_isin = sc_isincode AND sx_code = tb_status AND sx_trxtype = tb_trx_type AND 
			tb_client_id = cm_cd AND (
				(sx_code = @strStatus AND @strStatus NOT IN ('13', '')) OR 
				@strStatus IN ('13', '')
				) AND (
				(tb_trx_date BETWEEN @dtFromDate AND @dtToDt AND @strDateType = 'Transaction'
					) OR (@strDateType <> 'Transaction' AND tb_exec_date BETWEEN @dtFromDate AND @dtToDt
					)
				) AND ((tb_trx_type = @strType AND @strType <> 'ALL') OR  @strType = 'ALL')
		GROUP BY tb_internal_refno, tb_client_id, cm_name, tb_trx_date, tb_exec_date, tb_trx_type, tb_instcd, 
			tb_isin, sc_isinname, tb_qty, sx_description
	END
	
	IF ISNULL(@strOutputType,'G') = 'G'
    BEGIN
      SELECT SlipNo, TrxDate, ClientCode, ClientName, ISIN, CompanyName, Qty,
	  STATUS, ExecDate, NoOfTrx
      FROM @TBL_TRXDETAIL
	  ORDER BY ClientCode, TrxDate, CompanyName 
      SET @o_vcErrorMessage = 'Process Executed'
	  SET @o_vcErrorFlag = 'S'
	  RETURN 1
    END
    ELSE IF ISNULL(@strOutputType,'G') = 'X'	
    BEGIN
      IF NOT EXISTS(SELECT 1 FROM @TBL_TRXDETAIL)
	  BEGIN
	    SET @XMLDATA1 = 
	    (SELECT SlipNo ='', TrxDate ='', ClientCode ='', ClientName ='', ISIN ='', 
		CompanyName = '', Qty = '',STATUS = '', 
		ExecDate ='', 
		NoOfTrx = '' FOR XML PATH('Detail'))
      END 
	  ELSE
	  BEGIN
        SET @XMLDATA1 = 
	    (SELECT SlipNo, TrxDate, ClientCode, ClientName, ISIN, CompanyName, Qty,
	    STATUS, ExecDate, NoOfTrx
        FROM @TBL_TRXDETAIL
	    ORDER BY ClientCode, TrxDate, CompanyName FOR XML PATH('Detail'))
	  END
	  SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	  SET @o_vcErrorFlag = 'S'
	  RETURN 1
    END
	
END
GO

CREATE PROCEDURE stpr_WebBillSummary @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, 
	@o_vcErrorMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN
    SET @o_vcErrorMessage = 'Process Executed'
	SET @o_vcErrorFlag = 'S'
	--- Parameter Declaration
	DECLARE @dtFromDate VARCHAR(8), @dtToDt VARCHAR(8), @strUserId VARCHAR(500) = '', @strLevel VARCHAR(1), 
		@strCode VARCHAR(500), @strSelection VARCHAR(100), @XMLData XML, @strOutputType VARCHAR(1) = 'G', 
		@strRequestFrom VARCHAR(1) = 'W', @XMLDATA1 XML = '', @strShowValuation VARCHAR(20) = '', @strISIN VARCHAR(
			20), @strTrxType VARCHAR(200) = '', @strBillDate VARCHAR(20) = '', @strBillType VARCHAR(50) = '', @strIncludeLedBal 
		VARCHAR(50) = '', @strSQL NVARCHAR(MAX) = ''

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
	@strSelection = ISNULL(x.value('(Selection)[1]', 'VARCHAR(50)'), ''), 
	@strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'), ''), 
    @strRequestFrom = ISNULL(x.value('(RequestFrom)[1]', 'VARCHAR(1)'), ''), 
	@strBillDate = ISNULL(x.value('(BillDate)[1]', 'VARCHAR(50)'), ''), 
	@strBillType = ISNULL(x.value('(BillType)[1]', 'VARCHAR(50)'), ''), 
	@strIncludeLedBal = ISNULL(x.value('(IncludeLedBal)[1]', 'VARCHAR(50)'), '')
	FROM @XMLData.nodes('/root') AS XTbl(x)

	SET @strBillDate = ISNULL(@strBillDate, '')
	SET @strBillType = ISNULL(@strBillType, '')
	SET @strIncludeLedBal = ISNULL(@strIncludeLedBal,'false')

	IF ISNULL(@strSelection,'') = ''
    BEGIN
      SET @strSelection = 'CLIENT'
    END



	INSERT INTO @tbl_UserList
	EXEC dbo.stpr_GetClientAccessListNew @strUserid, @strSelection, @strCode

    
    DECLARE @strstring VARCHAR(MAX)=''
   

  DECLARE @TBL_BILLSUMMARY TABLE(SerialNo INT IDENTITY(1,1), BillNo VARCHAR(50), ClientID VARCHAR(50), ClientName VARCHAR(200), BillAmount MONEY, LedgerBalance MONEY)
   
  IF @strLevel = '1'
  BEGIN
    IF @strBillType = 'Obligated'
    BEGIN
	  IF @strIncludeLedBal = 'true'
	  BEGIN
	    INSERT INTO @TBL_BILLSUMMARY(BillNo, ClientID, ClientName, BillAmount, LedgerBalance)
	    SELECT Rtrim(Ltrim(convert(CHAR, bl_series))) + '/' + Rtrim(Ltrim(convert(CHAR, bl_bill_no))) AS 'BillNo', 
	    bl_client_id AS 'ClientID', cm_name AS 'ClientName', CAST((bl_amount) AS DECIMAL(15, 2)) AS 
	    'BillAmount', CAST((SELECT SUM(ld_amount) FROM Ledger(NOLOCK) WHERE ld_clientcd = cm_cd) AS DECIMAL(15, 2)) AS 'LedgerBalance'
        FROM Bill_obg(NOLOCK), Client_Master(NOLOCK), @tbl_UserList XX
        WHERE bl_client_id = cm_cd AND bl_bill_dt = CONVERT(VARCHAR, CAST(@StrBillDate AS DATE), 112) AND cm_schedule IN (
		SELECT sp_sysvalue
		FROM Sysparameter(NOLOCK)
		WHERE sp_parmcd = 'cmschedule')
		AND CM_cD = XX.CLIENTCODE
        ORDER BY bl_bill_no
	  END
      ELSE
      BEGIN
	    INSERT INTO @TBL_BILLSUMMARY(BillNo, ClientID, ClientName, BillAmount, LedgerBalance)
	    SELECT Rtrim(Ltrim(convert(CHAR, bl_series))) + '/' + Rtrim(Ltrim(convert(CHAR, bl_bill_no))) AS 'BillNo', 
	    bl_client_id AS 'ClientID', cm_name AS 'ClientName', CAST((bl_amount) AS DECIMAL(15, 2)) AS 
	    'BillAmount', 0 AS 'LedgerBalance'
        FROM Bill_obg(NOLOCK), Client_Master(NOLOCK), @tbl_UserList XX
        WHERE bl_client_id = cm_cd AND bl_bill_dt = CONVERT(VARCHAR, CAST(@StrBillDate AS DATE), 112) AND cm_schedule IN (
		SELECT sp_sysvalue
		FROM Sysparameter(NOLOCK)
		WHERE sp_parmcd = 'cmschedule')
		AND CM_cD = XX.CLIENTCODE
        ORDER BY bl_bill_no
      END	  
    END	
	ELSE IF @strBillType = 'Bill'
    BEGIN
	  IF @strIncludeLedBal = 'true'
	  BEGIN
	    INSERT INTO @TBL_BILLSUMMARY(BillNo, ClientID, ClientName, BillAmount, LedgerBalance)
	    SELECT Rtrim(Ltrim(convert(CHAR, bl_series))) + '/' + Rtrim(Ltrim(convert(CHAR, bl_bill_no))) AS 'BillNo', 
	    bl_client_id AS 'ClientID', cm_name AS 'ClientName', CAST((bl_amount) AS DECIMAL(15, 2)) AS 
	    'BillAmount', CAST((SELECT SUM(ld_amount) FROM Ledger(NOLOCK) WHERE ld_clientcd = cm_cd) AS DECIMAL(15, 2)) AS 'LedgerBalance'
        FROM Billing(NOLOCK), Client_Master(NOLOCK), @tbl_UserList XX
        WHERE bl_client_id = cm_cd AND bl_bill_dt = CONVERT(VARCHAR, CAST(@StrBillDate AS DATE), 112) AND cm_schedule IN (
		SELECT sp_sysvalue
		FROM Sysparameter(NOLOCK)
		WHERE sp_parmcd = 'cmschedule')
		AND CM_cD = XX.CLIENTCODE
        ORDER BY bl_bill_no
	  END
      ELSE
      BEGIN
	    INSERT INTO @TBL_BILLSUMMARY(BillNo, ClientID, ClientName, BillAmount, LedgerBalance)
	    SELECT Rtrim(Ltrim(convert(CHAR, bl_series))) + '/' + Rtrim(Ltrim(convert(CHAR, bl_bill_no))) AS 'BillNo', 
	    bl_client_id AS 'ClientID', cm_name AS 'ClientName', CAST((bl_amount) AS DECIMAL(15, 2)) AS 
	    'BillAmount', 0 AS 'LedgerBalance'
        FROM Billing(NOLOCK), Client_Master(NOLOCK), @tbl_UserList XX
        WHERE bl_client_id = cm_cd AND bl_bill_dt = CONVERT(VARCHAR, CAST(@StrBillDate AS DATE), 112) AND cm_schedule IN (
		SELECT sp_sysvalue
		FROM Sysparameter(NOLOCK)
		WHERE sp_parmcd = 'cmschedule')
		AND CM_cD = XX.CLIENTCODE
        ORDER BY bl_bill_no
      END	  
    END	
  
	IF ISNULL(@strOutputType,'G') = 'G'
    BEGIN
      SELECT BillNo, ClientID, ClientName, BillAmount, LedgerBalance
      FROM @TBL_BILLSUMMARY
	  ORDER BY SerialNo
      SET @o_vcErrorMessage = 'Process Executed'
	  SET @o_vcErrorFlag = 'S'
	  RETURN 1
    END
    ELSE IF ISNULL(@strOutputType,'G') = 'X'	
    BEGIN
      IF NOT EXISTS(SELECT 1 FROM @TBL_BILLSUMMARY)
	  BEGIN
	    SET @XMLDATA1 = 
	    (SELECT BillNo = '', ClientID = '', ClientName = '', BillAmount = '', LedgerBalance = '' FOR XML PATH('Detail'))
      END 
	  ELSE
	  BEGIN
        SET @XMLDATA1 = 
	    (SELECT BillNo, ClientID, ClientName, BillAmount, LedgerBalance
        FROM @TBL_BILLSUMMARY
	    ORDER BY SerialNo FOR XML PATH('Detail'))
	  END
	  SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	  SET @o_vcErrorFlag = 'S'
	  RETURN 1
    END
  END
END   
GO

CREATE PROCEDURE stpr_ValidateOffMarketEntry @i_vcinput XML, @o_vcFlag VARCHAR(1) OUTPUT, 
	@o_vcMessage VARCHAR(MAX) OUTPUT, @i_detailflag VARCHAR(1)='S'
WITH ENCRYPTION
AS
BEGIN
	DECLARE @tbl_InputJSONTable DBO.tb_ParamList;
	DECLARE @o_ParameterList VARCHAR(max) = '', @o_ParameterListxml XML;
	DECLARE @strModuleName VARCHAR(50) = '', @strOption VARCHAR(50) = '', @StrXML XML, @StrData1 VARCHAR(MAX)='' 
	DECLARE @ClientCd NVARCHAR(50), @ClientName NVARCHAR(100), @BackOfficeCd NVARCHAR(50), @ClientType 
		NVARCHAR(50), @BranchCd NVARCHAR(50), @LotNo BIGINT, @SlipMode NVARCHAR(50), @ErrorMessage NVARCHAR(
			255)

	--- PARAMETER LIST  
	EXEC SP_ParameterXMLRep @i_vcinput, @o_ParameterList OUTPUT

	IF ISNULL(@o_ParameterList, '') <> ''
	BEGIN
		SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)

		INSERT INTO @tbl_InputJSONTable (ParameterName, ParameterValue, HeaderName, Jsontag)
		SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code, Parameter.value(
				'(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue, Parameter.value('(MasterTag)[1]', 
				'VARCHAR(MAX)') AS MasterTag, Parameter.value('(JsonLevel)[1]', 'VARCHAR(MAX)') AS JsonLevel
		FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
	END

	SELECT @strModuleName = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName = 'ActionName'

	SELECT @strOption = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName = 'Option'

	IF (@strModuleName IN ('OffMarketEntry', 'InterDepository', 'EarlyPayin', 'OnMarketEntry','PledgeSetup') AND 
		(@strOption = 'IntRefNo' ))  --tb_internal_refno  
	BEGIN
		DECLARE @strtb_internal_refno VARCHAR(50) = '', @strtb_instcd VARCHAR(10) = '', @strtb_insttype VARCHAR(
				10) = '', @strSlipExists INT = 0

		SELECT @strtb_internal_refno = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'IntRefNo'

		SELECT @strtb_instcd = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'InstrumentType' --tb_instcd  

		SELECT @strtb_insttype = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'TransactionType' --tb_trx_type  

		SELECT @strSlipExists = COUNT(*)
		FROM Chequemaster(NOLOCK), Client_master(NOLOCK)
		WHERE chm_cmcd = cm_cd AND @strtb_internal_refno BETWEEN chm_chqno AND chm_chqno + chm_booksize - 1 AND 
		chm_instcd = @strtb_instcd


        IF @strModuleName = 'PledgeSetup' AND EXISTS(SELECT 1 FROM pledge(NOLOCK) 
		WHERE pl_irn = @strtb_internal_refno and pl_instcd = @strtb_instcd)
		BEGIN
		  SET @StrXML = (SELECT TOP 1 [InstrumentType] = LTRIM(RTRIM(pl_instcd)), 
		  [IntRefNo] = LTRIM(RTRIM(pl_irn)), [Date] = LTRIM(RTRIM(pl_trx_date)), [Branch] = LTRIM(RTRIM(pl_branchcd)), 
		  [Status] = LTRIM(RTRIM(pl_status)), [PledgerId] = LTRIM(RTRIM(pl_client_id)), [AgreementNo] = LTRIM(RTRIM(pl_agreementno)),
		  [CDSLPSNNo] = LTRIM(RTRIM(pl_reference)), [ReceiveMode] = LTRIM(RTRIM(pl_instrecvmode)), [LotNo] = LTRIM(RTRIM(pl_lotno)), 
		  [ClientId] = LTRIM(RTRIM(pl_otherclientid)), [Exchange] = LTRIM(RTRIM(pl_Exchange)), [Segment] = LTRIM(RTRIM(pl_segment)), 
		  [UCC] = LTRIM(RTRIM(pl_UCC)), [CMID] = LTRIM(RTRIM(pl_cmid)),
		  [MarginPSN] = pl_Oreference, [CCID] = pl_ccid, [EntryBy] = pl_EntryBy, [TMID] = pl_tmid, [Remark] = pl_remarks, [ISIN] = '', 
		  [Qty] = '', [Value] = '', [ExpiryDate] = pl_expirydt, [ExecDate] = pl_exec_date,
		  [PledgeType] = LTRIM(RTRIM(pl_PledgeType)), [Reason] = LTRIM(RTRIM(pl_NFiller1)), --[SerialNo] = LTRIM(RTRIM(pl_pri_key)),
          [DpId] = 	LTRIM(RTRIM(SUBSTRING(pl_otherclientid,1,8))) ,
          [Dptype] = 	CASE WHEN LTRIM(RTRIM(SUBSTRING(pl_otherclientid,1,8))) IN(select sp_sysvalue from Sysparameter(NOLOCK) 
		   where sp_parmcd='DPID') THEN 'Intra' else 'Inter' end,
          [BalanceType] = 	LTRIM(RTRIM(pl_lockinflag)),
          [LockinID] = ltrim(rtrim(pl_lockinid))		    
		  FROM Pledge(NOLOCK) WHERE pl_irn = @strtb_internal_refno and pl_instcd = @strtb_instcd FOR XML PATH('Data'))
		  SET @StrData1 = CAST(@StrXML AS VARCHAR(MAX))
		  SET @o_vcFlag = 'M'
		  SET @StrData1 = REPLACE(@StrData1,'<Data>','')
		  SET @StrData1 = REPLACE(@StrData1,'</Data>','')
		  SET @o_vcMessage = '<Message></Message>'+@StrData1
		  RETURN 1
		END
		IF ISNULL(@strSlipExists, 0) <= 0
		BEGIN
		  IF @strOption = 'IntRefNo' 
		  BEGIN
		    SET @o_vcFlag = 'E'
		    SET @o_vcMessage = '<Message>BO ID Not Found</Message>'
		  END
          ELSE IF @strOption = 'ADD' 
		  BEGIN
		    SET @o_vcFlag = 'S'
		    SET @o_vcMessage = ''
		  END
		  RETURN 1
		END
		DECLARE @blnInwardValidation BIT = 1

		IF @blnInwardValidation = 1
		BEGIN
			DECLARE @strInwardstatus CHAR(1) = 'N'
			DECLARE @strInwardentry VARCHAR(100) = (
					SELECT sp_sysvalue
					FROM Sysparameter
					WHERE sp_parmcd = 'INWARDENTRY'
					)

			IF @strInwardentry <> ''
			BEGIN
				DECLARE @intiPos INT;

				-- Finding the position of '925' in the string  
				SET @intiPos = CHARINDEX('925', @strInwardentry);

				-- If the position is greater than 0, extract the character  
				IF @intiPos > 0
				BEGIN
					-- Extracting 1 CHARACTER FROM the position found + 4  
					SET @strInwardstatus = SUBSTRING(@strInwardentry, @intiPos + 4, 1);
				END
			END

			IF @strInwardstatus = 'A' OR @strInwardstatus = 'O'
			BEGIN
				DECLARE @TempTable TABLE (
					ie_cmcd NVARCHAR(50), cm_name NVARCHAR(100), cm_blsavingcd NVARCHAR(50), ie_lotno BIGINT, 
					ie_mode NVARCHAR(50), cm_acctype NVARCHAR(50), cm_brboffcode NVARCHAR(50)
					);

				INSERT INTO @TempTable
				SELECT ie_cmcd, cm_name, cm_blsavingcd, ie_lotno, ie_mode, cm_acctype, cm_brboffcode
				FROM Inward_entry, Lot_size, Instrument_master, Client_master
				WHERE ie_cmcd = cm_cd AND ie_lotno = lz_lotno AND ie_trxtype = lz_type;

				IF EXISTS (
						SELECT 1
						FROM @TempTable
						)
				BEGIN
					SELECT TOP 1 @ClientCd = ie_cmcd, @ClientName = cm_name, @BackOfficeCd = cm_blsavingcd, 
						@ClientType = cm_acctype, @BranchCd = cm_brboffcode, @LotNo = ie_lotno, @SlipMode = ie_mode
					FROM @TempTable;

					SET @ErrorMessage = NULL;
				END
				ELSE
				BEGIN
					IF @strInwardstatus = 'A'
					BEGIN
						SET @ErrorMessage = '<Message>No inward entry found for current slip</Message>'
						SET @o_vcFlag = 'E'
						SET @o_vcMessage = @ErrorMessage

						RETURN 1;
					END
					ELSE
					BEGIN
						SET @ErrorMessage = 
							'<Message>No inward entry found for current slip. Proceed</Message>'
						SET @o_vcFlag = 'E'
						SET @o_vcMessage = @ErrorMessage

						RETURN 1;
					END
				END
			END
		END

		DECLARE @TempCheck TABLE (
			us_trxtype CHAR(3), us_instcd CHAR(3), us_irn NUMERIC(18, 0), us_clientcd CHAR(16), us_execdt CHAR(8)
			, us_archiveyn CHAR(1)
			);

		INSERT INTO @TempCheck
		SELECT us_trxtype, us_instcd, us_irn, us_clientcd, us_execdt, us_archiveyn
		FROM Used_slip
		WHERE us_instcd = @strtb_instcd AND us_irn = @strtb_internal_refno

		IF NOT EXISTS (
				SELECT 1
				FROM @TempCheck
				)
		BEGIN
			--IF @blnInwardValidation  = 1  
			--BEGIN  
			--END  
			DECLARE @TempCheck1 TABLE (
				chm_cmcd CHAR(16), chm_chqno NUMERIC(18, 0), chm_instcd INT, chm_status VARCHAR(1), chm_branchcd 
				CHAR(6), chm_allow CHAR(1)
				);

			INSERT INTO @TempCheck1
			SELECT chm_cmcd, chm_chqno, chm_instcd, chm_status, chm_branchcd, isNull(chm_allow, '') chm_allow
			FROM Chequemaster, Client_master
			WHERE chm_cmcd = cm_cd AND @strtb_internal_refno BETWEEN chm_chqno AND chm_chqno + chm_booksize - 1 AND 
				chm_instcd = @strtb_instcd --" + GetBranch(UserId).ToString()  

			IF EXISTS (
					SELECT 1
					FROM @TempCheck1
					)
			BEGIN
				DECLARE @strChm_cmcd CHAR(16) = '', @strchm_chqno NUMERIC(18, 0), @strchm_instcd INT, 
					@strchm_status VARCHAR(1), @strchm_branchcd CHAR(6), @strchm_allow CHAR(1)

				SELECT TOP 1 @strChm_cmcd = chm_cmcd, @strchm_chqno = chm_chqno, @strchm_instcd = @strchm_instcd, 
					@strchm_status = chm_status, @strchm_branchcd = chm_branchcd, @strchm_allow = chm_allow
				FROM @TempCheck1

				IF @strchm_status = 'N'
				BEGIN
					SET @ErrorMessage = '<Message>Slip Not Issued</Message>'
					SET @o_vcFlag = 'E'
					SET @o_vcMessage = @ErrorMessage

					RETURN 1;
				END
				ELSE IF @strchm_status = 'D'
				BEGIN
					SET @ErrorMessage = '<Message>Slip is Destroyed</Message>'
					SET @o_vcFlag = 'E'
					SET @o_vcMessage = @ErrorMessage

					RETURN 1;
				END
				ELSE IF @strchm_status = 'A' OR @strchm_status = 'L'
				BEGIN
					SET @ErrorMessage = '<Message>Used as Loose Slip</Message>'
					SET @o_vcFlag = 'E'
					SET @o_vcMessage = @ErrorMessage

					RETURN 1;
				END
				ELSE IF @strchm_status = 'P'
				BEGIN
					SET @ErrorMessage = '<Message>Slip is Issued to POA</Message>'
					SET @o_vcFlag = 'E'
					SET @o_vcMessage = @ErrorMessage

					RETURN 1;
				END
				ELSE IF @strchm_status = 'B'
				BEGIN
					SET @ErrorMessage = '<Message>Slip is Issued to Branch</Message>'
					SET @o_vcFlag = 'E'
					SET @o_vcMessage = @ErrorMessage

					RETURN 1;
				END

				--ELSE IF @strchm_status = 'I'  
				--BEGIN  
				--END  
				IF EXISTS (
						SELECT chs_status
						FROM Chequestop
						WHERE chs_chqno = @strtb_internal_refno AND chs_instcd = @strtb_instcd
						)
				BEGIN
					DECLARE @strchs_status CHAR(1) = ''

					SELECT @strchs_status = chs_status
					FROM Chequestop
					WHERE chs_chqno = @strtb_internal_refno AND chs_instcd = @strtb_instcd

					IF @strchs_status = 'S'
					BEGIN
						SET @ErrorMessage = '<Message>Slip No. is under Stop Status.</Message>'
						SET @o_vcFlag = 'E'
						SET @o_vcMessage = @ErrorMessage

						RETURN 1;
					END
					ELSE IF @strchs_status = 'D'
					BEGIN
						SET @ErrorMessage = '<Message>Slip is Destroyed</Message>'
						SET @o_vcFlag = 'E'
						SET @o_vcMessage = @ErrorMessage

						RETURN 1;
					END
				END

				IF @strChm_cmcd <> ''
				BEGIN
					SET @ClientCd = @strChm_cmcd
				END
				ELSE IF @strchm_status = 'R'
				BEGIN
					SET @ErrorMessage = '<Message>Slip has been Sent to Printer</Message>'
					SET @o_vcFlag = 'E'
					SET @o_vcMessage = @ErrorMessage

					RETURN 1;
				END
				ELSE
				BEGIN
					SET @ErrorMessage = '<Message>Invalid Slip Status</Message>'
					SET @o_vcFlag = 'E'
					SET @o_vcMessage = @ErrorMessage

					RETURN 1;
				END
			END
			ELSE
			BEGIN
				SET @intiPos = 0;

				DECLARE @strSlipmnt CHAR(1) = '';
				DECLARE @strSlipMaintanace VARCHAR(50) = (
						SELECT sp_sysvalue
						FROM Sysparameter
						WHERE sp_parmcd = 'SLIPMNT'
						)

				IF @strSlipMaintanace <> ''
				BEGIN
					SET @intiPos = CHARINDEX('925', @strSlipMaintanace);

					IF @intiPos > 0
					BEGIN
						-- Extract the character after '925' (4 characters ahead)  
						SET @strSlipmnt = SUBSTRING(@strSlipMaintanace, @intiPos + 4, 1);
					END
				END

				IF @strSlipmnt = 'F'
				BEGIN
					SET @ErrorMessage = '<Message>Slip Not Found in Stock.</Message>'
					SET @o_vcFlag = 'E'
					SET @o_vcMessage = @ErrorMessage

					RETURN 1;
				END
				ELSE IF @strSlipmnt = 'P'
				BEGIN
					SET @ErrorMessage = '<Message>Slip Not Found in Stock.! Proceed</Message>'
					SET @o_vcFlag = 'E'
					SET @o_vcMessage = @ErrorMessage

					RETURN 1;
				END
				ELSE IF @strSlipmnt <> 'N'
				BEGIN
					SET @ErrorMessage = 
						'<Message>Invalid system parameter found in slip maintenance</Message>'
					SET @o_vcFlag = 'E'
					SET @o_vcMessage = @ErrorMessage

					RETURN 1;
				END
						--IF et.ClientCd <> null  
						--BEGIN  
						--END  
			END

			IF @strchm_allow = 'Y' OR @strchm_allow = 'E' OR @strchm_allow = ''
			BEGIN
			   IF @strModuleName = 'PledgeSetup'
			   BEGIN
			     SET @ErrorMessage = '<Message></Message><PledgerId>' 
					+ @ClientCd + '</PledgerId>'
				SET @o_vcFlag = 'S'
			   END
			   ELSE
			   BEGIN
			     IF NOT EXISTS(SELECT 1 FROM CLIENT_MASTER WHERE CM_cD = @ClientCd AND CM_ACTIVE  ='01')
			     BEGIN
			       SET @ErrorMessage = 
						'<Message>BOId is Not Active - '+@ClientCd+'</Message>'
				   SET @o_vcFlag = 'E'
				   SET @o_vcMessage = @ErrorMessage
				   RETURN 1;
			     END
				 
				 SET @ErrorMessage = '<Message>Issue of this DIS (' + @strtb_internal_refno + 
					') has not been registered with CDSL Or you have not imported response of DIS issue Upload. Do you Still want to Proceed ?</Message><BOID>' 
					+ @ClientCd + '</BOID>'
				 SET @o_vcFlag = 'M'	
			   END		
			   SET @o_vcMessage = @ErrorMessage

				RETURN 1;
			END

			--New Change
			SET @o_vcFlag = 'S'
			SET @o_vcMessage = '<Message></Message><BOID>' + @ClientCd + '</BOID>'

			RETURN 1
				--End Change
		END

		--ELSE  
		--BEGIN  
		--  IF @strtb_insttype = '908' AND @strInwardstatus = 'N'  
		--  BEGIN  
		--  END  
		--  IF @blnInwardValidation = 1  
		--  BEGIN  
		--  END  
		--END  
		DECLARE @strUs_archiveyn CHAR(1) = ''

		SELECT @strUs_archiveyn = us_archiveyn
		FROM @TempCheck

		IF @strUs_archiveyn = 'Y'
		BEGIN
			SET @ErrorMessage = '<Message>Invalid State, Data is Archived or Deleted</Message>'
			SET @o_vcFlag = 'E'
			SET @o_vcMessage = @ErrorMessage

			RETURN 1;
		END
		ELSE IF @strtb_insttype != '908'
		BEGIN
			SET @ErrorMessage = '<Message>Internal reference number already exist</Message>'
			SET @o_vcFlag = 'E'
			SET @o_vcMessage = @ErrorMessage

			RETURN 1;
		END
	END
	ELSE IF @strModuleName IN ('OffMarketEntry', 'InterDepository') AND @strOption IN ('add', 'Edit'
			) AND @i_detailflag = 'D'
	BEGIN
		DECLARE @strTransactionDate VARCHAR(50) = (
				SELECT ParameterValue
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'TransactionDate'
				)
		DECLARE @strExecDate VARCHAR(50) = (
				SELECT ParameterValue
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'ExecutionDate'
				)


		IF @strTransactionDate > @strExecDate
		BEGIN
			SET @ErrorMessage = 
				'<Message>Transaction Date - Cannot be greater than Execution Date</Message>'
			SET @o_vcFlag = 'E'
			SET @o_vcMessage = @ErrorMessage

			RETURN 1;
		END
		
		IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'Qty' AND ISNULL(ParameterValue,'') <> '')
		BEGIN
			SET @ErrorMessage = 
				'<Message>Qty can not be blank</Message>'
			SET @o_vcFlag = 'E'
			SET @o_vcMessage = @ErrorMessage

			RETURN 1;
        END		
		
		DECLARE @stroffResion VARCHAR(50) = (
				SELECT ParameterValue
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'Reason'), @strVPaymentMode VARCHAR(20)=(SELECT ParameterValue
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'PaymentMode' AND ISNULL(ParameterValue,'') <> '')

       IF @stroffResion = '2~2'
	   BEGIN
	    if NOT EXISTS(SELECT 1
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'PaymentMode' AND ISNULL(ParameterValue,'') <> '')
		BEGIN
		  SET @ErrorMessage = 
				'<Message>Payment Mode can not be blank</Message>'
			SET @o_vcFlag = 'E'
			SET @o_vcMessage = @ErrorMessage

			RETURN 1;
        END		
		
		if NOT EXISTS(SELECT 1
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'ChqRefNo' AND ISNULL(ParameterValue,'') <> '') AND @strVPaymentMode <> '3'
		BEGIN
		  SET @ErrorMessage = 
				'<Message>Chq Ref No can not be blank</Message>'
			SET @o_vcFlag = 'E'
			SET @o_vcMessage = @ErrorMessage

			RETURN 1;
        END		
		
		if NOT EXISTS(SELECT 1
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'DateofIssue' AND ISNULL(ParameterValue,'') <> '') 
		BEGIN
		  SET @ErrorMessage = 
				'<Message>Date of Issue can not be blank</Message>'
			SET @o_vcFlag = 'E'
			SET @o_vcMessage = @ErrorMessage

			RETURN 1;
        END		
		
		if NOT EXISTS(SELECT 1
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'PayeeName' AND ISNULL(ParameterValue,'') <> '')
		BEGIN
		  SET @ErrorMessage = 
				'<Message>Payee Name can not be blank</Message>'
			SET @o_vcFlag = 'E'
			SET @o_vcMessage = @ErrorMessage

			RETURN 1;
        END		
		if NOT EXISTS(SELECT 1
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'BankAcNo' AND ISNULL(ParameterValue,'') <> '')  AND @strVPaymentMode <> '3'
		BEGIN
		  SET @ErrorMessage = 
				'<Message>Bank Ac No can not be blank</Message>'
			SET @o_vcFlag = 'E'
			SET @o_vcMessage = @ErrorMessage

			RETURN 1;
        END		
		
		if NOT EXISTS(SELECT 1
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'PaidBy' AND ISNULL(ParameterValue,'') <> '')
		BEGIN
		  SET @ErrorMessage = 
				'<Message>Paid By can not be blank</Message>'
			SET @o_vcFlag = 'E'
			SET @o_vcMessage = @ErrorMessage

			RETURN 1;
        END		
		
		if NOT EXISTS(SELECT 1
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'BankName' AND ISNULL(ParameterValue,'') <> '')  AND @strVPaymentMode <> '3'
		BEGIN
		  SET @ErrorMessage = 
				'<Message>Bank Name can not be blank</Message>'
			SET @o_vcFlag = 'E'
			SET @o_vcMessage = @ErrorMessage

			RETURN 1;
        END		
		
		if NOT EXISTS(SELECT 1
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'consideration' AND ISNULL(ParameterValue,'') <> '')
		BEGIN
		  SET @ErrorMessage = 
				'<Message>consideration can not be blank</Message>'
			SET @o_vcFlag = 'E'
			SET @o_vcMessage = @ErrorMessage

			RETURN 1;
        END		
		
		if NOT EXISTS(SELECT 1 
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'BranchName' AND ISNULL(ParameterValue,'') <> '')  AND @strVPaymentMode <> '3'
		BEGIN
		  SET @ErrorMessage = 
				'<Message>Branch Name can not be blank</Message>'
			SET @o_vcFlag = 'E'
			SET @o_vcMessage = @ErrorMessage

			RETURN 1;
        END		
	  END	
	END
	ELSE IF @strModuleName IN ('PledgeSetup') AND @strOption IN ('add', 'Edit')
	BEGIN
	  IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'IntRefNo' AND ISNULL(ParameterValue,'') <> '')
	  BEGIN
        SET @ErrorMessage = '<Message>Internal Reference No cannot be blank.</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;	  
  	  END
	  
	  IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'PledgerId' AND ISNULL(ParameterValue,'') <> '')
	  BEGIN
        SET @ErrorMessage = '<Message>Pledger Code cannot be left blank.</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;	  
  	  END
	  
	  IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'DpId' AND ISNULL(ParameterValue,'') <> '')
	  BEGIN
        SET @ErrorMessage = '<Message>Pledgee Code cannot be left blank.</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;	  
  	  END
     
	  IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'DpId' AND ISNULL(ParameterValue,'') <> '')
	  BEGIN
        SET @ErrorMessage = '<Message>Pledgee Code cannot be left blank.</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;	  
  	  END
      IF EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'BalanceType' AND ISNULL(ParameterValue,'')='L')
	  AND NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'LockinID' AND ISNULL(ParameterValue,'') <> '')			
      BEGIN
        SET @ErrorMessage = '<Message>Lockin Details cannot be left blank.</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;	  
  	  END

      IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'ISIN' AND ISNULL(ParameterValue,'') <> '')			
      BEGIN
        SET @ErrorMessage = '<Message>ISIN Code cannot be Left Blank.</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;	  
	  END
	 
	  DECLARE @strPlgQty VARCHAR(10)=''
	  SELECT @strPlgQty = ParameterValue FROM @tbl_InputJSONTable
				WHERE ParameterName = 'Qty'
	  
	  IF EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'Qty' AND ISNUMERIC(@strPlgQty) = 0)			
      BEGIN
        SET @ErrorMessage = '<Message>Quantity cannot be zero.</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;	  
  	  END
	  
	  IF @strPlgQty = '0'
      BEGIN
        SET @ErrorMessage = '<Message>Quantity cannot be blank.</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;	  
  	  END
	  
 
      
	  IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'Date' AND ISNULL(ParameterValue,'') <> '')			
      BEGIN
        SET @ErrorMessage = '<Message>Transaction Date cannot be blank.</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;	  
  	  END
	  
	  IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'ExpiryDate' AND ISNULL(ParameterValue,'') <> '')			
      BEGIN
        SET @ErrorMessage = '<Message>Expiry Date cannot be blank.</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;	  
  	  END
	  
	  IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'ExecDate' AND ISNULL(ParameterValue,'') <> '')			
      BEGIN
        SET @ErrorMessage = '<Message>Execution Date cannot be blank.</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;	  
  	  END
	  
	  IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'AgreementNo' AND ISNULL(ParameterValue,'') <> '')			
      BEGIN
        SET @ErrorMessage = '<Message>Agreement Number is blank. Do You Wish To Proceed?</Message>'
		SET @o_vcFlag = 'M'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;	  
  	  END
	  
	  DECLARE @STRPledgeType VARCHAR(20)='', @strplSegment VARCHAR(20)=''
	  SELECT @STRPledgeType = ISNULL(ParameterValue,'') 
	  FROM @tbl_InputJSONTable
	  WHERE ParameterName = 'PledgeType'
	  
	  SELECT @strplSegment = ISNULL(ParameterValue,'') FROM @tbl_InputJSONTable
	  WHERE ParameterName = 'Segment'
	  
	  DECLARE @strMsg NVARCHAR(MAX) = '', @cmbEntityIdent VARCHAR(100)=''

      SELECT @cmbEntityIdent = ISNULL(ParameterValue,'') FROM @tbl_InputJSONTable
				WHERE ParameterName = 'EntryBy'
	  
	  IF @STRPledgeType IN('MP','MR')
      BEGIN				
	 	 
        IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'Exchange' AND ISNULL(ParameterValue,'') <> '')
		BEGIN		
		  SET @strMsg = @strMsg + 'Exchange' + CHAR(13)
		END
        IF ISNULL(@strplSegment,'') = ''
		BEGIN		
		  SET @strMsg = @strMsg + 'Segment' + CHAR(13)
		END
		IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'CMID' AND ISNULL(ParameterValue,'') <> '')
		BEGIN		
		  SET @strMsg = @strMsg + 'CMID' + CHAR(13)
		END
		IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'TMID' AND ISNULL(ParameterValue,'') <> '')
		BEGIN	
          SET @strMsg = @strMsg + IIF(LTRIM(RTRIM(@cmbEntityIdent)) = 'CP', 'CP Code', 'TMID') + CHAR(13)
        END
		IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'CCID' AND ISNULL(ParameterValue,'') <> '')
		BEGIN	
          SET @strMsg = @strMsg + 'CCID' + CHAR(13)
		END
		IF LTRIM(RTRIM(@cmbEntityIdent)) = ''
			SET @strMsg = @strMsg + 'Entry By' + CHAR(13)

		/*IF @STRPledgeType = 'MP' AND NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'CDSLPSNNo' AND ISNULL(ParameterValue,'') <> '') 
		BEGIN
		  SET @strMsg = @strMsg + 'Margin PSN' + CHAR(13)
		END
		*/
        PRINT 'C'
	    IF @strMsg <> ''
		BEGIN
		  SET @strMsg = 'Following fields cannot be left blank for ' + @STRPledgeType + CHAR(13) + @strMsg
          SET @ErrorMessage = '<Message>'+@strMsg+'</Message>'
		  SET @o_vcFlag = 'E'
		  SET @o_vcMessage = @ErrorMessage
		  RETURN 1;
        END

		IF @cmbEntityIdent <> 'CP' AND 
		EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'UCC' AND ISNULL(ParameterValue,'') = '')
		BEGIN
		  SET @ErrorMessage = '<Message>UCC Field is blank ,Do you want to Continue?</Message>'
		  SET @o_vcFlag = 'M'
		  SET @o_vcMessage = @ErrorMessage
		  RETURN 1;	
		END
	 END
	 
	-- Validate Client Type for Margin Pledge and Margin Repledge
	DECLARE @strClienttype NVARCHAR(50), @txtClient_id VARCHAR(20)='', @txtpledgee_Clientid VARCHAR(20)=''
	DECLARE @strPledgeAc NVARCHAR(MAX) = 
	'25130,21131,21132,25133,25134,21135,21136,25137,21138,21139,21140,21141,25142,21143,25144'
    
	SELECT @txtpledgee_Clientid = ISNULL(ParameterValue,'')  FROM @tbl_InputJSONTable
	WHERE ParameterName = 'ClientId'
	
	SELECT @txtClient_id = ISNULL(ParameterValue,'')  FROM @tbl_InputJSONTable
	WHERE ParameterName = 'PledgerId'
				
	IF @STRPledgeType = 'MP'
	BEGIN
	  SELECT @strClienttype = cm_clienttype
	  FROM Client_master
	  WHERE cm_cd = LTRIM(RTRIM(@txtClient_id))

	  IF CHARINDEX(',' + @strClienttype + ',', ',' + @strPledgeAc + ',') > 0
	  BEGIN
		SET @ErrorMessage = '<Message>Margin Pledge a/c cannot be entered in Pledgor</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;
	  END
      SET @strClienttype = ''
	  SELECT @strClienttype = cm_clienttype
	  FROM Client_master
	  WHERE cm_cd = LTRIM(RTRIM(@txtpledgee_Clientid))

	  IF CHARINDEX(',' + @strClienttype + ',', ',' + @strPledgeAc + ',') = 0 AND ISNULL(@strClienttype,'') <> ''
	  BEGIN
		SET @ErrorMessage = '<Message>Enter Margin Pledge a/c in Pledgee</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;
	  END
	END
	
	ELSE IF @STRPledgeType = 'MR'
	BEGIN
		SELECT @strClienttype = cm_clienttype
		FROM Client_master
		WHERE cm_cd = LTRIM(RTRIM(@txtClient_id))

		IF CHARINDEX(',' + @strClienttype + ',', ',' + @strPledgeAc + ',') = 0
		BEGIN
		  SET @ErrorMessage = '<Message>Enter Margin Pledge a/c in Pledgor</Message>'
		  SET @o_vcFlag = 'E'
		  SET @o_vcMessage = @ErrorMessage
		  RETURN 1;
	    END
	END
	ELSE
	BEGIN
	  SELECT @strClienttype = cm_clienttype
	  FROM Client_master
	  WHERE cm_cd = LTRIM(RTRIM(@txtClient_id))

	  IF CHARINDEX(',' + @strClienttype + ',', ',' + @strPledgeAc + ',') > 0
	  BEGIN
		SET @ErrorMessage = '<Message>Margin Pledge a/c cannot be entered in Pledgor</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;	
	  END

	  SELECT @strClienttype = cm_clienttype
	  FROM Client_master
	  WHERE cm_cd = LTRIM(RTRIM(@txtpledgee_Clientid))

	  IF CHARINDEX(',' + @strClienttype + ',', ',' + @strPledgeAc + ',') > 0
	  BEGIN
		SET @ErrorMessage = '<Message>Margin Pledge a/c cannot be entered in Pledgee</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;
	  END
	END  
   END
	
	
	ELSE IF @strModuleName IN ('DematEntry') AND @strOption IN ('add', 'Edit'
			)
	BEGIN
	  DECLARE @strDematClientid VARCHAR(50) = (SELECT TOP 1 ParameterValue
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'ClientId')
	  DECLARE @strDematISIN VARCHAR(50) = (SELECT TOP 1 ParameterValue
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'ISIN')
	  DECLARE @strDematDate VARCHAR(50) = (SELECT TOP 1 ParameterValue
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'Date')	
      DECLARE @strDispatchDate VARCHAR(50) = (SELECT TOP 1 ParameterValue
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'DispatchDate')		
      DECLARE @strDRNNo VARCHAR(50) = (SELECT TOP 1 ParameterValue
				FROM @tbl_InputJSONTable
				WHERE ParameterName = 'DRNNo')	
      SET @ErrorMessage = ''				
	  IF dbo.fn_DematCheckTrxAllow(0, @strDematClientid, @strDematISIN, '', @strDematDate) = 0
	  BEGIN
		SET @ErrorMessage = 
				'<Message>Transaction not allowed for the provided details.</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
  	    RETURN 1;
	  END

		-- If the calling context requires “Demat Entry” details (for example, DispatchDate is provided)
	  IF isnull(@strDispatchDate,'') <> ''
	  BEGIN
		IF NOT EXISTS (SELECT 1
		FROM @tbl_InputJSONTable WHERE ParameterName = 'FolioNo' and LTRIM(RTRIM(ParameterValue)) <> '')
		BEGIN
          SET @ErrorMessage = '<Message>Folio no., Certificate No., DN range From and To are required as per Enhanced Due Diligence.</Message>'
		  SET @o_vcFlag = 'E'
		  SET @o_vcMessage = @ErrorMessage
  	      RETURN 1;			
 		END
      END
	  IF CONVERT(DATE, GETDATE()) >= '2023-11-28' AND isnull(@strDispatchDate,'') = ''
	  BEGIN
		SET @ErrorMessage = '<Message>Dispatch Date cannot be left blank.</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
  	    RETURN 1;	
	  END
		
	  DECLARE @ie_rejected INT;

	  SELECT @ie_rejected = ie_rejected
	  FROM Inward_entry(NOLOCK)
	  WHERE ie_trxtype = '901' AND ie_slipno = CAST(@strDRNNo AS VARCHAR(20));

	  IF @ie_rejected IS NOT NULL AND @ie_rejected > 0
	  BEGIN
		SET @ErrorMessage = '<Message>Cannot add record; Inward Entry for this record has been rejected.</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
  	    RETURN 1;	
	  END	

	  DECLARE @pandetailMsg VARCHAR(500);

	  SET @pandetailMsg = dbo.fn_GetPandetail(@strDematClientid);

	  IF LEN(LTRIM(RTRIM(@pandetailMsg))) > 0
	  BEGIN
		SET @ErrorMessage = '<Message>'+@pandetailMsg+'</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;
      END
	  
	  IF ISNULL(@strDematDate,'') = ''
	  BEGIN
	  SET @ErrorMessage = '<Message>Demat Date Can not be Blank</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;
	  END
	  
	  IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'DRFNo' AND ParameterValue <> '')
      BEGIN
	  SET @ErrorMessage = '<Message>IRN No Can not be Blank</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;
	  END
	  
	  IF ISNULL(@strDematClientid,'') = ''
      BEGIN
	  SET @ErrorMessage = '<Message>Demat Client id Can not be Blank</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;
	  END
	  
	  IF ISNULL(@strDematISIN,'') = ''
      BEGIN
	  SET @ErrorMessage = '<Message>ISIN Can not be Blank</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;
	  END
	  
	  IF NOT EXISTS(SELECT 1 FROM @tbl_InputJSONTable
				WHERE ParameterName = 'DematQty' AND ParameterValue <> '')
      BEGIN
	    SET @ErrorMessage = '<Message>Demat Qty Can not be Blank</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;
	  END
	  
	  
	  
	  DECLARE @StrTotalCerticateQty MONEY = 0, @StrTotalCerticateFrom INT = 0, @CertificateTo INT = 0
	  
	  
	  
	  
	  SELECT @StrTotalCerticateQty = SUM(CAST(ParameterValue AS MONEY)) 
	  FROM @tbl_InputJSONTable WHERE ParameterName = 'CerticateQty'
	  
	  SELECT @StrTotalCerticateFrom = COUNT(DISTINCT CAST(ParameterValue AS INT)) 
	  FROM @tbl_InputJSONTable WHERE ParameterName = 'CertificateFrom'
	  
	  SELECT @CertificateTo = COUNT(DISTINCT CAST(ParameterValue AS INT)) 
	  FROM @tbl_InputJSONTable WHERE ParameterName = 'CertificateTo'
	  
	  
	  IF ISNULL(@StrTotalCerticateQty,0) <> (SELECT TOP 1 CAST(ParameterValue AS MONEY)
	  FROM @tbl_InputJSONTable WHERE ParameterName = 'DematQty')
	  BEGIN
	    SET @ErrorMessage = '<Message>Certificate Qty Can not be Matched</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;
	  END
	 /*
	  IF (ISNULL(@CertificateTo,0)-isnull(@StrTotalCerticateFrom,0))+1 <> (SELECT TOP 1 CAST(ParameterValue AS INT)
	  FROM @tbl_InputJSONTable WHERE ParameterName = 'TotalCertificates')
	  BEGIN
	    SET @ErrorMessage = '<Message>Certicate No Can not be Matched</Message>'
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
		RETURN 1;
	  END
	  */
	  
	  SET @ErrorMessage = '<Message>Process Completed</Message>'
	  SET @o_vcFlag = 'S'
	  SET @o_vcMessage = @ErrorMessage
	  RETURN 1;
	  
	END
	
	ELSE IF @strModuleName IN ('InterDepository', 'OnMarketEntry') AND @strOption IN ('ValidateSettlement', 'SettlementId'
			)
	BEGIN
		DECLARE @SettlementNo NVARCHAR(50), @TransactionDate VARCHAR(10), @IsSettPocket BIT;

		SELECT @SettlementNo = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'CounterSett'

		SELECT @TransactionDate = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'TransactionDate'

		SELECT @IsSettPocket = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'IsSettPocket'

		IF @IsSettPocket = 1
		BEGIN
			IF LEN(@SettlementNo) <> 13
			BEGIN
				SET @ErrorMessage = '<Message>Enter 13 Digits Settlement No.</Message>'
				SET @o_vcFlag = 'E'
				SET @o_vcMessage = @ErrorMessage

				RETURN 1;
			END

			IF @SettlementNo <> '' AND @SettlementNo <> REPLICATE('9', 13)
			BEGIN
				DECLARE @PayoutDate DATE;

				SELECT @PayoutDate = cc_payout_dt
				FROM Cc_calender(NOLOCK)
				WHERE cc_settle_no = @SettlementNo AND LEFT(LTRIM(cc_settle_no), 2) <> '98';

				IF @PayoutDate IS NULL
				BEGIN
					SET @ErrorMessage = '<Message>Settlement Not Found</Message>'
					SET @o_vcFlag = 'E'
					SET @o_vcMessage = @ErrorMessage

					RETURN 1;
				END

				IF @PayoutDate < @TransactionDate
				BEGIN
					SET @ErrorMessage = 
						'<Message>Settlement is not active. Do you wish to proceed?</Message>'
					SET @o_vcFlag = 'E'
					SET @o_vcMessage = @ErrorMessage

					RETURN 1;
				END
			END
			IF ISNULL(@SettlementNo,'') <> ''
	        BEGIN
	            SET @o_vcFlag = 'D'
		        SET @o_vcMessage = '<Message></Message>'+'<Exchange>true</Exchange>'
			     +'<Segment>true</Segment><UCC>true</UCC><CMID>true</CMID><EntryBy>true</EntryBy><TMID>true</TMID><EarlyPayin>true</EarlyPayin>'
		       RETURN 1
	        END
	        ELSE
	        BEGIN
	          SET @o_vcFlag = 'D'
		      SET @o_vcMessage = '<Message></Message>'+'<Exchange>false</Exchange>'
			   +'<Segment>false</Segment><UCC>false</UCC><CMID>false</CMID><EntryBy>false</EntryBy><TMID>false</TMID><EarlyPayin>false</EarlyPayin>'
		      RETURN 1
	        END
		END
		ELSE
		BEGIN
			IF LEN(@SettlementNo) <> 9
			BEGIN
				SET @ErrorMessage = '<Message>Enter 9 Digits Settlement No.</Message>'
				SET @o_vcFlag = 'E'
				SET @o_vcMessage = @ErrorMessage

				RETURN;
			END
		END
	END
	ELSE IF @strModuleName IN ('OffMarketEntry', 'InterDepository') AND @strOption IN ('StampDuty'
			)
	BEGIN
		DECLARE @ISIN NVARCHAR(50), @sc_security_type VARCHAR(50), @Consideration MONEY;
		DECLARE @strRate MONEY = 0, @StampDutyAmount MONEY = 0

		SELECT @ISIN = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'ISIN'

		SELECT @Consideration = CAST(ParameterValue AS MONEY)
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'Consideration'

		SELECT @sc_security_type = sc_security_type
		FROM Security(NOLOCK)
		WHERE sc_isincode = @ISIN

		SET @strRate = DBO.fnStampDutyRate(@sc_security_type)
		SET @StampDutyAmount = CONVERT(NUMERIC(10, 4), ROUND(@Consideration * @strRate / 100, 2))
		SET @o_vcFlag = 'S'
		SET @o_vcMessage = '<Message></Message><StampDuty>' + CAST(@StampDutyAmount AS VARCHAR) + '</StampDuty>'

		RETURN 1
	END
	ELSE IF @strModuleName IN ('OffMarketEntry', 'InterDepository') AND @strOption IN ('BOID_Validate')
	BEGIN
	  DECLARE @StrVBOID VARCHAR(16)=''
	  SELECT TOP 1 @StrVBOID = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'BOID'
	  
	  DECLARE @strParamDayend VARCHAR(1)='', @strParamDayendDL VARCHAR(1)='', @strDE_DormantFreeze VARCHAR(1)='', 
	  @de_debitfreeze VARCHAR(1)='', @dtDE_LastTrxDt VARCHAR(11)=''
      
	  SELECT @strParamDayend = sp_sysvalue FROM  Sysparameter WHERE sp_parmcd='DAYENDD'
      SELECT @strParamDayendDL = sp_sysvalue FROM  Sysparameter WHERE sp_parmcd='DAYENDL'
      
	  SELECT @strDE_DormantFreeze = DE_DormantFreeze, @de_debitfreeze = de_debitfreeze, @dtDE_LastTrxDt = DE_LastTrxDt  
      FROM DayEnd(NOLOCK) WHERE DE_CmCd = @StrVBOID

     IF (@strDE_DormantFreeze = 'Y' and @strParamDayend <> 'A') Or (@strParamDayendDL <>'A' And @de_debitfreeze = 'Y')
     BEGIN
	   IF @strDE_DormantFreeze = 'Y' AND @de_debitfreeze ='Y'
	   BEGIN
         SET @ErrorMessage = '<Message>Dormant (his Previous Transaction was on '+@dtDE_LastTrxDt+') and is also  Freezed for Debit?</Message>'
	   END
	   ELSE IF @strDE_DormantFreeze = 'Y' AND @de_debitfreeze <> 'Y'
	   BEGIN
	    SET @ErrorMessage = '<Message>Dormant (his Previous Transaction was on '+@dtDE_LastTrxDt+'?</Message>'
	   END	
	   IF (@strDE_DormantFreeze = 'Y' and @strParamDayend = 'D') Or (@strParamDayendDL = 'D' And @de_debitfreeze = 'Y')
	   BEGIN
	    SET @o_vcFlag = 'E'
	   END
       ELSE 	   
	   BEGIN
	     SET @o_vcFlag = 'M'
	   END
	   SET @o_vcMessage = @ErrorMessage
	   RETURN 1;	
     END
	END
	ELSE IF @strModuleName IN ('DematEntry') AND @strOption IN ('delete')
	BEGIN
	  DECLARE @strAllow VARCHAR(1), @StrInwardNo VARCHAR(10)='', @dm_instcd VARCHAR(20)='',
	  @dm_branchcd VARCHAR(20)='', @dm_authcode1 VARCHAR(1)='', @dm_authcode2 VARCHAR(1)='',
	  @dm_authcode3 VARCHAR(1)=''

	  SELECT TOP 1 @StrInwardNo = ParameterValue
	  FROM @tbl_InputJSONTable
	  WHERE ParameterName = 'InwardNo'
		
	  SELECT TOP 1 @dm_instcd = ParameterValue
	  FROM @tbl_InputJSONTable
	  WHERE ParameterName = 'InstrumentType'
		
	  SELECT TOP 1 @dm_branchcd = ParameterValue
	  FROM @tbl_InputJSONTable
	  WHERE ParameterName = 'Branch'

	  SELECT @strAllow = dm_trx_allow, @dm_authcode1 = dm_authcode1 ,
	  @dm_authcode2 = dm_authcode2, @dm_authcode3 = dm_authcode3
	  FROM dematmaster(NOLOCK) WHERE dm_pri_key = @StrInwardNo 
	  AND dm_instcd = @dm_instcd AND dm_branchcd = @dm_branchcd

      IF @strAllow IN ('E', 'S')
      BEGIN
        SET @o_vcFlag = 'E'
		SET @o_vcMessage = '<Message>Data already exported. To update changes, import Change order of the day file.</Message>'
		RETURN 1
      END
		
	  IF @dm_authcode1 = 'A' OR @dm_authcode2 = 'A' OR @dm_authcode3 = 'A'
      BEGIN
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = '<Message>Data already authorised.</Message>'
		RETURN 1
	  END
	  SET @o_vcFlag = 'S'
	  SET @o_vcMessage = '<Message></Message>'+ CAST(@StampDutyAmount AS VARCHAR)
	  RETURN 1
	END
	
	IF @strModuleName = 'DematEntry' AND @strOption = 'GetCharge'
    BEGIN
	  DECLARE @strDRFNo VARCHAR(20), @strInstrumentType VARCHAR(10), @dblTotalCertificates NUMERIC(18, 0), 
	  @dblDematQty NUMERIC(18, 0), @ie_noofcert NUMERIC(18, 0), @strClientID VARCHAR(16)

	  SELECT @strDRFNo = ParameterValue
	  FROM @tbl_InputJSONTable
	  WHERE ParameterName = 'DRFNo'

	  SELECT @strInstrumentType = ParameterValue
	  FROM @tbl_InputJSONTable
	  WHERE ParameterName = 'InstrumentType'

	  SELECT @dblTotalCertificates = CAST(ParameterValue AS NUMERIC(18, 0))
	  FROM @tbl_InputJSONTable
	  WHERE ParameterName = 'TotalCertificates'

	  SELECT @dblDematQty = CAST(ParameterValue AS NUMERIC(18, 0))
	  FROM @tbl_InputJSONTable
	  WHERE ParameterName = 'DematQty'

	  SELECT @strClientID = ParameterValue
	  FROM @tbl_InputJSONTable
	  WHERE ParameterName = 'ClientID'

	  IF ISNULL(@dblTotalCertificates,0) > ISNULL(@dblDematQty,0)
	  BEGIN
	    SET @o_vcFlag = 'E'
	    SET @o_vcMessage = '<Message>Total Certificates Cannot Be Greater Than Quantity.</Message>'

	    RETURN 1
      END

      SELECT @ie_noofcert = ie_noofcert
      FROM inward_entry
      WHERE ie_slipno = @strDRFNo AND ie_trxtype = @strInstrumentType;

      IF ISNULL(@ie_noofcert,0) <> 0  AND @dblTotalCertificates > @ie_noofcert
      BEGIN
	    SET @o_vcFlag = 'E'
	    SET @o_vcMessage = 'Only ' + CAST(@ie_noofcert AS VARCHAR) + ' certificates are allowed in inward entry.'
	    RETURN 1;
      END

      DECLARE @cd_min_amount DECIMAL(18, 2);
      DECLARE @cd_max_amount DECIMAL(18, 2);
      DECLARE @cd_per_certificate DECIMAL(18, 2);
      DECLARE @cd_fixed_amount DECIMAL(18, 2);
      DECLARE @curamount DECIMAL(18, 2) = 0;

		-- Fetch charge details
      SELECT @cd_min_amount = cd_min_amount, @cd_max_amount = cd_max_amount, 
      @cd_per_certificate = cd_per_certificate, @cd_fixed_amount = cd_fixed_amount
      FROM Chargesdetail(NOLOCK)
      WHERE cd_scheme = (SELECT cm_chgsscheme FROM Client_master(NOLOCK)
      WHERE cm_cd = @strClientID ) AND cd_code = 2;

		-- Calculate charge amount
      IF ISNULL(@cd_per_certificate,0) > 0
      BEGIN
	    SET @curamount = @cd_per_certificate * @dblDematQty;
      END

      IF ISNULL(@cd_fixed_amount,0) > 0
      BEGIN
	    SET @curamount = @cd_fixed_amount + @curamount;
      END

      IF ISNULL(@cd_max_amount,0) > 0 AND ISNULL(@curamount,0) > ISNULL(@cd_max_amount,0)
      BEGIN
	    SET @curamount = @cd_max_amount;
      END

      IF @cd_min_amount > 0 AND @curamount < @cd_min_amount
      BEGIN
	    SET @curamount = @cd_min_amount;
      END
	  DECLARE @ledgerbalance  MONEY = 0
	  SELECT @ledgerbalance = SUM(ld_AMOUNT) FROM LEDGER(NOLOCK) WHERE ld_clientcd = @strClientID

      DECLARE @unbilled DECIMAL(18, 2), @balance DECIMAL(18, 2), @charge DECIMAL(18, 2)

		-- Update output values
      SET @unbilled = ISNULL(@unbilled,0) + ISNULL(@curamount,0);
      SET @balance = ISNULL(@balance,0) + ISNULL(@curamount,0);
      SET @charge = ISNULL(@curamount,0);
      SET @o_vcFlag = 'S'
      SET @o_vcMessage = '<Message></Message>'+'<charge>' + CAST(@charge AS VARCHAR) + '</charge><unbilled>' + CAST(@unbilled AS VARCHAR) + '</unbilled>'
	  +' <balance>'+CAST(@balance AS VARCHAR)+'</balance>'+'<Ledger>'+CAST(@ledgerbalance AS VARCHAR)+'</Ledger>'

      RETURN 1
    END
	
	
	ELSE IF @strModuleName IN ('DematEntry') AND @strOption IN ('ValidateIRNNO')
	BEGIN
		DECLARE @DRFNo NVARCHAR(50) = '', @DematInstrumentType VARCHAR(50) = '', @DematEddt VARCHAR(10) = '', 
			@DematAction VARCHAR(50) = ''

		SELECT @DRFNo = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'DRFNo'

		SELECT @DematInstrumentType = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'InstrumentType'

		SELECT @DematEddt = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'Date'

		SELECT @DematAction = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'DematAction'

		EXEC Stpr_ValidateIRNNO @DRFNo, @DematInstrumentType, @DematEddt, @DematAction, @o_vcFlag OUTPUT, 
			@o_vcMessage OUTPUT

		RETURN 1
	END
	ELSE IF @strModuleName IN ('Journals') AND @strOption IN ('add', 'Edit')
	BEGIN
		DECLARE @FtValue MONEY = 0

		SELECT @FtValue = SUM(CAST(ParameterValue AS MONEY))
		FROM @tbl_InputJSONTable
		WHERE HEADERNAME = 'SecondLevelData' AND ParameterName = 'Balance'

		IF ISNULL(@FtValue, 0) <> 0
		BEGIN
			SET @o_vcFlag = 'E'
			SET @o_vcMessage = '<Message>Dr/Cr Amount Should be Equal</Message>'

			RETURN 1
		END
	END
	ELSE IF @strModuleName IN ('OffMarketEntry', 'InterDepository', 'EarlyPayin', 'OnMarketEntry') 
		AND @strOption IN ('CheckHolding')
	BEGIN
		DECLARE @FISIN NVARCHAR(50), @BOID VARCHAR(20) = ''

		SELECT @FISIN = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'ISIN'

		SELECT @BOID = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName in('ClientCode','BOID')

		IF NOT EXISTS (
				SELECT 1
				FROM HOLDING(NOLOCK)
				WHERE hld_ac_code = @BOID AND hld_isin_code = @FISIN
				)
		BEGIN
			SET @o_vcFlag = 'M'
			SET @o_vcMessage = '<Message>Holding Does not Exists For ' + @BOID + ' in ' + @FISIN + 
				'. Proceed </Message>'

			RETURN 1
		END
		ELSE
		BEGIN
			SET @o_vcFlag = 'S'
			SET @o_vcMessage = '<Message>Holding Exists For ' + @BOID + ' in ' + @ISIN + '</Message>'

			RETURN 1
		END
	END
	ELSE IF @strModuleName IN ('OffMarketEntry', 'InterDepository') AND @strOption IN ('PaymentMode_Enable')
	BEGIN
	  DECLARE @PaymentMode VARCHAR(10)=''
	  SELECT @PaymentMode = ParameterValue
	  FROM @tbl_InputJSONTable
	  WHERE ParameterName = 'PaymentMode'
	  IF @PaymentMode = '3'
	  BEGIN
	    SET @o_vcFlag = 'D'
		SET @o_vcMessage = '<Message></Message>'+'<ChqRefNo>false</ChqRefNo><BankAcNo>false</BankAcNo><BankName>false</BankName><BranchName>false</BranchName>'
		RETURN 1
	  END
	  ELSE
	  BEGIN
	    SET @o_vcFlag = 'D'
		SET @o_vcMessage = '<Message></Message>'+'<ChqRefNo>true</ChqRefNo><BankAcNo>true</BankAcNo><BankName>true</BankName><BranchName>true</BranchName>'
		RETURN 1
	  END
	END
	ELSE IF @strModuleName IN ('OffMarketEntry', 'InterDepository','earlypayin') AND @strOption IN ('CounterSett_Enable')
	BEGIN
	  DECLARE @CounterSett VARCHAR(100)=''
	  SELECT @CounterSett = ParameterValue
	  FROM @tbl_InputJSONTable
	  WHERE ParameterName = 'CounterSett'
	  IF ISNULL(@CounterSett,'') <> ''
	  BEGIN
	    SET @o_vcFlag = 'D'
		SET @o_vcMessage = '<Message></Message>'+'<Exchange>true</Exchange>'
			+'<Segment>true</Segment><UCC>true</UCC><CMID>true</CMID><EntryBy>true</EntryBy><TMID>true</TMID>'
		RETURN 1
	  END
	  ELSE
	  BEGIN
	    SET @o_vcFlag = 'D'
		SET @o_vcMessage = '<Message></Message>'+'<Exchange>false</Exchange>'
			+'<Segment>false</Segment><UCC>false</UCC><CMID>false</CMID><EntryBy>false</EntryBy><TMID>false</TMID>'
		RETURN 1
	  END
	END
	ELSE IF @strModuleName IN ('OffMarketEntry', 'InterDepository') AND @strOption IN ('ReasonStatus'
			)
	BEGIN
		DECLARE @Reason NVARCHAR(500), @ReasonCD NVARCHAR(20), @message VARCHAR(MAX) = '', @XOUTPUTXML XML

		SELECT @ReasonCD = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'Reason'

		DECLARE @blnisFamilyTransfer BIT, @txtField6 NVARCHAR(255)
		DECLARE @strDate VARCHAR(8) = CONVERT(VARCHAR, GETDATE(), 112);

		EXEC dbo.prfillReasonCombo 1, @strDate, 'X', @message OUTPUT

		SET @XOUTPUTXML = CAST(@message AS XML)

		SELECT @Reason = DisplayName
		FROM (
			SELECT ISNULL(x.value('(Value)[1]', 'VARCHAR(50)'), '') AS Value, ISNULL(x.value(
						'(DisplayName)[1]', 'VARCHAR(500)'), '') AS DisplayName
			FROM @XOUTPUTXML.nodes('/Reason') AS XTbl(x)
			) x1
		WHERE x1.Value = @ReasonCD

		EXEC dbo.HandleReasonCode @Reason, @blnisFamilyTransfer OUTPUT, @txtField6 OUTPUT
		IF @ReasonCD  <> '2~2'
		BEGIN
		  SET @o_vcFlag = 'D'
		  IF ISNULL(@blnisFamilyTransfer,'') = ''
		  BEGIN
		    SET @o_vcMessage = '<Message></Message>'+'<PaymentMode>false</PaymentMode><ChqRefNo>false</ChqRefNo><DateofIssue>false</DateofIssue>'
			+'<PayeeName>false</PayeeName><BankAcNo>false</BankAcNo><PaidBy>false</PaidBy><BankName>false</BankName><StampDuty>false</StampDuty><Consideration>false</Consideration><BranchName>false</BranchName>'
		  END
		  ELSE
		  BEGIN
		    SET @o_vcMessage = '<Message></Message>'+'<FamilyTransfer>' + CAST(@blnisFamilyTransfer AS VARCHAR) + 
			'</FamilyTransfer><Remark>' + CAST(@txtField6 AS VARCHAR) + '</Remark>'+'<PaymentMode>false</PaymentMode><ChqRefNo>false</ChqRefNo><DateofIssue>false</DateofIssue>'
			+'<PayeeName>false</PayeeName><BankAcNo>false</BankAcNo><PaidBy>false</PaidBy><BankName>false</BankName><StampDuty>false</StampDuty><Consideration>false</Consideration><BranchName>false</BranchName>'
		  END	
		  RETURN 1
		END
		SET @o_vcFlag = 'D'
		IF ISNULL(@blnisFamilyTransfer,'') = ''
		BEGIN
		SET @o_vcMessage = '<Message></Message>'+'<PaymentMode>true</PaymentMode><ChqRefNo>true</ChqRefNo><DateofIssue>true</DateofIssue>'
			+'<PayeeName>true</PayeeName><BankAcNo>true</BankAcNo><PaidBy>true</PaidBy><BankName>true</BankName><StampDuty>true</StampDuty><Consideration>true</Consideration><BranchName>true</BranchName>'
		END
		ELSE
		BEGIN
		  SET @o_vcMessage = '<Message></Message>'+'<FamilyTransfer>' + CAST(@blnisFamilyTransfer AS VARCHAR) + 
			'</FamilyTransfer><Remark>' + CAST(@txtField6 AS VARCHAR) + '</Remark>'+'<PaymentMode>true</PaymentMode><ChqRefNo>true</ChqRefNo><DateofIssue>true</DateofIssue>'
			+'<PayeeName>true</PayeeName><BankAcNo>true</BankAcNo><PaidBy>true</PaidBy><BankName>true</BankName><StampDuty>true</StampDuty><Consideration>true</Consideration><BranchName>true</BranchName>'
		END	
		RETURN 1
	END
	ELSE IF @strModuleName IN ('InterDepository', 'OnMarketEntry', 'EarlyPayin','PledgeSetup','OffMarketEntry') AND @strOption IN ('GetMarginDetail'
			)
	BEGIN
		DECLARE @blnMatchRec INT, @strCmcd NVARCHAR(50), @strExch NVARCHAR(50), @strSeg NVARCHAR(50), @strUCC 
			NVARCHAR(50) = '', @strcmId NVARCHAR(50) = '', @strTMID NVARCHAR(50) = ''

		SELECT @blnMatchRec = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'MatchRec'

		SELECT @strCmcd = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName IN('BOID','PledgerId')

		SELECT @strExch = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'Exchange'

		SELECT @strSeg = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'Segment'

		SELECT @strUCC = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'UCC'

		SELECT @strcmId = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'CMID'

		SELECT @strTMID = ParameterValue
		FROM @tbl_InputJSONTable
		WHERE ParameterName = 'TMID'

		DECLARE @strUCCseg NVARCHAR(50);
		DECLARE @msg NVARCHAR(50) = 'OK';

		SET @strUCCseg = CASE WHEN @strSeg = 'CM' THEN '01' WHEN @strSeg = 'FO' THEN '02' WHEN @strSeg = 'CD' THEN '03' WHEN 
					@strSeg = 'SB' THEN '04' WHEN @strSeg = 'CO' THEN '05' WHEN @strSeg = 'DT' THEN '06' ELSE @strSeg END;

		CREATE TABLE #MarginPledge (
			Exchange NVARCHAR(50), Segment NVARCHAR(50), UCC NVARCHAR(50), CMID NVARCHAR(50), TMID NVARCHAR(50), 
			msg NVARCHAR(50)
			);

		IF @blnMatchRec = 1
		BEGIN
			DECLARE @blnMatch BIT = 0;

			-- Query for matching records  
			INSERT INTO #MarginPledge (Exchange, Segment, UCC, CMID, tmid, msg)
			SELECT cud_Exchnge, cud_segment, cud_UCC, cud_cmid, cud_tmid, 'NOMATCH'
			FROM Client_UCC_Details(NOLOCK)
			WHERE cud_boid = @strCmcd;

			IF EXISTS (
					SELECT 1
					FROM Client_UCC_Details
					WHERE cud_boid = @strCmcd AND UPPER(LTRIM(RTRIM(cud_Exchnge))) = UPPER(LTRIM(RTRIM(@strExch)
							)) AND (
							LTRIM(RTRIM(@strUCCseg)) = 'AL' OR UPPER(LTRIM(RTRIM(@strUCCseg))) = UPPER(LTRIM(
									RTRIM(cud_segment)))
							) AND UPPER(LTRIM(RTRIM(@strUCC))) = UPPER(LTRIM(RTRIM(cud_UCC))) AND UPPER(LTRIM(
								RTRIM(@strcmId))) = UPPER(LTRIM(RTRIM(cud_cmid))) AND UPPER(LTRIM(RTRIM(@strTMID
								))) = UPPER(LTRIM(RTRIM(cud_tmid)))
					)
			BEGIN
				SET @blnMatch = 1;

				UPDATE #MarginPledge
				SET msg = 'OK';
			END

			SET @o_vcFlag = 'S'
			SET @ErrorMessage = (
					SELECT TOP 1 *
					FROM #MarginPledge
					FOR XML PATH('')
					);
			SET @o_vcMessage = '<Message></Message>'+@ErrorMessage
		END
		ELSE
		BEGIN
			-- Query for non-matching records  
			INSERT INTO #MarginPledge (Exchange, Segment, ucc, cmid, tmid, msg)
			SELECT TOP 1 cud_Exchnge, cud_segment = CASE WHEN cud_segment =  '01'  THEN 'CM' WHEN cud_segment = '02'  THEN  'FO' WHEN cud_segment = '03' THEN  'CD' WHEN 
					cud_segment = '04'  THEN  'SB' WHEN cud_segment =  '05' THEN  'CO' WHEN cud_segment = '06'  THEN  'DT' ELSE cud_segment END, cud_UCC, cud_cmid, cud_tmid, 'NOMATCH'
			FROM Client_UCC_Details(NOLOCK)
			WHERE cud_boid = @strCmcd AND cud_Exchnge = @strExch
				--AND (LTRIM(RTRIM(@strUCCseg)) = 'AL' OR UPPER(LTRIM(RTRIM(@strUCCseg))) = UPPER(LTRIM(RTRIM(cud_segment)))) 
				AND cud_segment = @strUCCseg;

			UPDATE #MarginPledge
			SET msg = 'OK'
			WHERE EXISTS (
					SELECT 1
					FROM Client_UCC_Details
					WHERE cud_boid = @strCmcd AND cud_Exchnge = @strExch AND cud_segment = @strUCCseg
					);

			-- Return result for non-matching records  
			SET @o_vcFlag = 'S'
			SET @ErrorMessage = (
					SELECT TOP 1 *
					FROM #MarginPledge
					FOR XML PATH('')
					);
			SET @o_vcMessage = '<Message></Message>'+@ErrorMessage
		END

		DROP TABLE #MarginPledge;

		IF ISNULL(@o_vcMessage, '') = ''
		BEGIN
			SET @o_vcMessage = ''
		END

		RETURN 1;
	END

	IF @ErrorMessage <> ''
	BEGIN
		SET @o_vcFlag = 'E'
		SET @o_vcMessage = @ErrorMessage
	END
	ELSE
	BEGIN
		SET @o_vcFlag = 'S'
		--SET @o_vcMessage = '<Message>Process Completed</Message>'  
		SET @o_vcMessage = '<Message>Process Completed</Message>'
	END

	RETURN;
END
GO

CREATE PROCEDURE stpr_ValidateTemplateData @i_vcinput XML, @o_vcFlag VARCHAR(1) OUTPUT, 
	@o_vcMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN
  DECLARE @tbl_InputJSONTable DBO.tb_ParamList;
  DECLARE @o_ParameterList VARCHAR(max) = '', @o_ParameterListxml XML, @strFlag VARCHAR(20)='';
  DECLARE @strModuleName VARCHAR(50) = '', @strOption VARCHAR(50) = '', @StrXML XML, @StrData1 VARCHAR(MAX)='' ,
  @strISIN VARCHAR(20)=''
  DECLARE @strMICR VARCHAR(50) = '', @strClientCode VARCHAR(50) = '', @strQty1 MONEY=0
	--- PARAMETER LIST  
  EXEC SP_ParameterXMLRep @i_vcinput, @o_ParameterList OUTPUT

  IF ISNULL(@o_ParameterList, '') <> ''
  BEGIN
	SET @o_ParameterListxml = CAST(@o_ParameterList AS XML)

	INSERT INTO @tbl_InputJSONTable (ParameterName, ParameterValue, HeaderName, Jsontag)
	SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code, Parameter.value(
				'(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue, Parameter.value('(MasterTag)[1]', 
				'VARCHAR(MAX)') AS MasterTag, Parameter.value('(JsonLevel)[1]', 'VARCHAR(MAX)') AS JsonLevel
		FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
  END

  SELECT @strModuleName = ParameterValue
  FROM @tbl_InputJSONTable
  WHERE ParameterName = 'ActionName'

  SELECT @strOption = ParameterValue
  FROM @tbl_InputJSONTable
  WHERE ParameterName = 'Option'
  
  SELECT @strISIN = ParameterValue
  FROM @tbl_InputJSONTable
  WHERE ParameterName = 'ISIN'
  
  SELECT @strMICR = ParameterValue
  FROM @tbl_InputJSONTable
  WHERE ParameterName = 'MICR'
  
  SELECT @strClientCode = ParameterValue
  FROM @tbl_InputJSONTable
  WHERE ParameterName = 'ClientCode'
  
  SELECT @strQty1 = ParameterValue
  FROM @tbl_InputJSONTable
  WHERE ParameterName = 'Qty'
  
  IF @strModuleName IN('OffMarketEntry','InterDepository','earlypayin') AND @strOption = 'CounterBOID_Validate'
  BEGIN
	DECLARE @strCounterBOID VARCHAR(16)='', @SettlementTag int, @strCurBOID VARCHAR(20)=''
	
	SELECT @strCounterBOID = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName = 'CounterBOID'
	
	SELECT @strCurBOID = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName = 'BOID'
	
	IF ISNULL(@strCounterBOID,'') = ISNULL(@strCurBOID,'')
	BEGIN
	  SET @o_vcFlag = 'E'
	  SET @o_vcMessage = '<Message>Counter BO id is not Same with BOID</Message>'
	  RETURN 1
	END
	
	
	IF ISNULL(@strCounterBOID,'') <> ''
	BEGIN
      SELECT @strFlag = CASE WHEN count(0) > 0 THEN 'true' ELSE 'false' END FROM Client_master(NOLOCK) 
	  WHERE cm_clienttype = 2624 AND cm_cd =@strCounterBOID   
       
	  DECLARE @strReason1 VARCHAR(100)='', @strDPDisable VARCHAR(10)='',  @strCounterSettDisable VARCHAR(10)=''
	   
	  SELECT @SettlementTag = COUNT(0) from SettlementCMList(NOLOCK) where se_cmcd = @strCounterBOID
	  
	  IF @strModuleName ='InterDepository'
	  BEGIN
	    IF SUBSTRING(ISNULL(RIGHT(@strCounterBOID,8),''),1,2) ='IN'
	    BEGIN
	      SET @strDPDisable = 'false'
		  SET @strCounterSettDisable = 'true'
	    END
	    ELSE IF SUBSTRING(ISNULL(RIGHT(@strCounterBOID,8),''),1,2) <> 'IN'
	    BEGIN
	      SET @strDPDisable = 'true'
		  SET @strCounterSettDisable = 'false'
        END	
	  END
	  ELSE 
	  BEGIN
	    SET @strDPDisable = 'true'
		SET @strCounterSettDisable = 'true'
	  END
	  IF @strFlag = 'true' and @SettlementTag = 0
	  BEGIN
		SET @strReason1 = 'false'
		SET @o_vcFlag = 'D'
      END
	  ELSE
	  BEGIN
		SET @o_vcFlag = 'D'
		SET @strReason1 = 'true'
	  END
      SET @o_vcMessage = '<Message></Message><Reason>'+@strReason1+'</Reason><DP>'+@strDPDisable+'</DP><CounterSett>'+@strCounterSettDisable+'</CounterSett>'    
	  RETURN 1
	END
  END
  IF @strModuleName IN('Receipts','Payments') AND @strOption = 'ValidateMicr'
  BEGIN
   DECLARE @strBankName VARCHAR(200)=''
	SELECT TOP 1 @strBankName =  REPLACE(BK_NAME,'&','') FROM BANK_MASTER(NOLOCK) WHERE bk_micr = @strMICR
	IF ISNULL(@strBankName,'') = ''
	BEGIN
	  SET @o_vcFlag = 'M'
	  SET @o_vcMessage = '<Message>MICR number is not valid, Do you want to proceed?</Message>'
	  RETURN 1
	END
	ELSE
	BEGIN
	  SET @o_vcFlag = 'S'
	  SET @o_vcMessage = '<Message></Message><Bank>'+@strBankName+'</Bank>'
	  RETURN 1
	END
  END
  ELSE IF @strModuleName IN('Receipts','Payments') AND @strOption = 'Balance'
  BEGIN
   DECLARE @strBalance VARCHAR(200)=''
	SELECT TOP 1 @strBalance =  CAST(ISNULL(ABS(sum(ld_amount)),0) as VARCHAR) + CASE WHEN ISNULL(sum(ld_amount),0)>0 THEN ' Dr' else ' Cr' end  FROM ledger(NOLOCK) WHERE ld_clientcd = @strClientCode 
	IF ISNULL(@strBalance,'') = ''
	BEGIN
	  SET @o_vcFlag = 'E'
	  SET @o_vcMessage = '<Message>Balance not showing</Message>'
	  RETURN 1
	END
	ELSE
	BEGIN
	  SET @o_vcFlag = 'S'
	  SET @o_vcMessage = '<Message></Message><Balance>'+@strBalance+'</Balance>'
	  RETURN 1
	END
  END
  ELSE IF @strModuleName IN('OffMarketEntry','InterDepository','earlypayin','PLEDGESETUP','OnMarketEntry') AND @strOption = 'GetRate'
  BEGIN
    DECLARE @ftRate MONEY =0, @StrValue1 MONEY=0
    SELECT @ftRate = cast((sc_rate) as decimal(15,2)) 
    FROM  Security with (noLock) ,Security_status with (noLock)  
    where sc_security_status = ss_code and sc_isincode = @strISIN
	IF ISNULL(@strqty1,'') > 0
	BEGIN
	  SET @StrValue1 = @ftRate*@strqty1
	END
    SET @o_vcFlag = 'S'
	SET @o_vcMessage = '<Message></Message><Rate>'+CAST(@ftRate AS VARCHAR)+'</Rate><Value>'+CAST(@StrValue1 AS VARCHAR)+'</Value>'
	RETURN 1
  END	
  ELSE IF @strModuleName IN('PLEDGESETUP') AND @strOption = 'BalanceType_V'
  BEGIN
    DECLARE @strBalanceType VARCHAR(1)=''
    SELECT @strBalanceType = ParameterValue
	FROM @tbl_InputJSONTable
	WHERE ParameterName = 'BalanceType'
    SET @o_vcFlag = 'D'
	IF @strBalanceType = 'F'
	BEGIN
	  SET @o_vcMessage = '<Message></Message><LockinID>false</LockinID>'
	END  
	ELSE
	BEGIN
	  SET @o_vcMessage = '<Message></Message><LockinID>true</LockinID>'
	END
	RETURN 1
  END	
END
GO

CREATE PROCEDURE Stpr_ValidateIRNNO
    @IRN NVARCHAR(50),
    @InstrumentType NVARCHAR(50),
    @Eddt DATE,
    @Action NVARCHAR(10), @o_vcFlag VARCHAR(1) OUTPUT, @o_vcMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN
  SET NOCOUNT ON;
  DECLARE @LotSize INT, @InwardEntry NVARCHAR(50), @x INT, @SlipMode CHAR(1), @ClientID NVARCHAR(50), 
  @ClientName NVARCHAR(100), @CertQty INT, @BranchCode NVARCHAR(50), @ErrorMessage VARCHAR(MAX)='',
  @ClientCd  NVARCHAR(50), @BackOfficeCd   NVARCHAR(50),
  @ClientType NVARCHAR(50), @BranchCd   
  NVARCHAR(50), @LotNo BIGINT
    
	
    -- Fetch transaction details
  SELECT @LotSize = ie_nooftrx 
  FROM inward_entry(NOLOCK) 
  WHERE ie_instno = @InstrumentType 
  AND ie_slipno = @IRN;

  IF @LotSize IS NULL
      SET @LotSize = 0;

    -- Get system parameter
  SELECT @InwardEntry = sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd ='InwardEntry'
  SET @x = CHARINDEX('901', @InwardEntry);
  SET @InwardEntry = SUBSTRING(@InwardEntry, @x + 4, 1);

  IF UPPER(@Action) = 'ADD'
  BEGIN
        -- Slip check validation (assuming a function mfnSlipCheck exists)
    DECLARE @SlipCheckResult BIT;
	
	DECLARE @strtb_internal_refno VARCHAR(50) = '', @strtb_instcd VARCHAR(10) = '', @strtb_insttype VARCHAR(10) = '901',   
    @strSlipExists INT = 0  
    SET @strtb_internal_refno = @IRN
    SET @strtb_instcd = @InstrumentType
  
    /*
	SELECT @strSlipExists = COUNT(*)  
    FROM Chequemaster(NOLOCK), Client_master(NOLOCK)  
    WHERE chm_cmcd = cm_cd AND @strtb_internal_refno BETWEEN chm_chqno AND chm_chqno + chm_booksize - 1 AND chm_instcd = @strtb_instcd  
    IF ISNULL(@strSlipExists, 0) <= 0  
    BEGIN  
      SET @o_vcFlag = 'E'  
      SET @o_vcMessage = '<Message>BO ID Not Found</Message>'  
      RETURN 1  
    END  
	*/
    DECLARE @blnInwardValidation BIT = 1  
    IF @blnInwardValidation = 1  
    BEGIN  
      DECLARE @strInwardstatus CHAR(1) = 'N'  
      DECLARE @strInwardentry VARCHAR(100) = (  
      SELECT sp_sysvalue  
      FROM Sysparameter (NOLOCK) WHERE sp_parmcd = 'INWARDENTRY')  
      IF @strInwardentry <> ''  
      BEGIN  
        DECLARE @intiPos INT;  
        SET @intiPos = CHARINDEX('925', @strInwardentry);  
     
        IF @intiPos > 0  
        BEGIN  
          SET @strInwardstatus = SUBSTRING(@strInwardentry, @intiPos + 4, 1);  
        END  
      END  
      IF @strInwardstatus = 'A' OR @strInwardstatus = 'O'  
      BEGIN  
        DECLARE @TempTable TABLE (  
        ie_cmcd NVARCHAR(50), cm_name NVARCHAR(100), cm_blsavingcd NVARCHAR(50), ie_lotno BIGINT, ie_mode NVARCHAR(50),   
        cm_acctype NVARCHAR(50), cm_brboffcode NVARCHAR(50));  
        INSERT INTO @TempTable  
        SELECT ie_cmcd, cm_name, cm_blsavingcd, ie_lotno, ie_mode, cm_acctype, cm_brboffcode  
        FROM Inward_entry(NOLOCK), Lot_size(NOLOCK), Instrument_master(NOLOCK), Client_master (NOLOCK) 
        WHERE ie_cmcd = cm_cd AND ie_lotno = lz_lotno AND ie_trxtype = lz_type;  
        IF EXISTS (SELECT 1  FROM @TempTable  )  
        BEGIN  
          SELECT TOP 1 @ClientCd = ie_cmcd, @ClientName = cm_name, @BackOfficeCd = cm_blsavingcd, @ClientType = cm_acctype,   
          @BranchCd = cm_brboffcode, @LotNo = ie_lotno, @SlipMode = ie_mode  
          FROM @TempTable;  
          SET @ErrorMessage = NULL;  
        END  
        ELSE  
        BEGIN  
        IF @strInwardstatus = 'A'  
        BEGIN  
          SET @ErrorMessage = '<Message>No inward entry found for current slip</Message>'  
          SET @o_vcFlag = 'E'  
          SET @o_vcMessage = @ErrorMessage  
          RETURN 1;  
        END  
        ELSE  
        BEGIN  
          SET @ErrorMessage = '<Message>No inward entry found for current slip. Proceed</Message>'  
          SET @o_vcFlag = 'E'  
          SET @o_vcMessage = @ErrorMessage  
          RETURN 1;  
        END  
      END  
    END  
   END  
   
   DECLARE @TempCheck TABLE (us_trxtype CHAR(3), us_instcd CHAR(3), us_irn NUMERIC(18, 0), us_clientcd CHAR(16), us_execdt CHAR(8), us_archiveyn CHAR(1));  
   INSERT INTO @TempCheck    
   SELECT us_trxtype, us_instcd, us_irn, us_clientcd, us_execdt, us_archiveyn  
   FROM Used_slip(NOLOCK)  
   WHERE us_instcd = @strtb_instcd AND us_irn = @strtb_internal_refno  
   IF NOT EXISTS (SELECT 1  FROM @TempCheck )  
   BEGIN  
     DECLARE @TempCheck1 TABLE (  
     chm_cmcd CHAR(16), chm_chqno NUMERIC(18, 0), chm_instcd INT, chm_status VARCHAR(1), 
	 chm_branchcd CHAR(6), chm_allow CHAR  (1)  );  
     
	 INSERT INTO @TempCheck1  
     SELECT chm_cmcd, chm_chqno, chm_instcd, chm_status, chm_branchcd, isNull(chm_allow, '') chm_allow  
     FROM Chequemaster, Client_master  
     WHERE chm_cmcd = cm_cd AND @strtb_internal_refno BETWEEN chm_chqno AND chm_chqno + chm_booksize - 1 AND chm_instcd =   
      @strtb_instcd --" + GetBranch(UserId).ToString()  
     IF EXISTS (  SELECT 1  FROM @TempCheck1  )  
     BEGIN  
       DECLARE @strChm_cmcd CHAR(16) = '', @strchm_chqno NUMERIC(18, 0), @strchm_instcd INT, @strchm_status VARCHAR(1),   
       @strchm_branchcd CHAR(6), @strchm_allow CHAR(1)  
       SELECT TOP 1 @strChm_cmcd = chm_cmcd, @strchm_chqno = chm_chqno, @strchm_instcd = @strchm_instcd, @strchm_status =   
       chm_status, @strchm_branchcd = chm_branchcd, @strchm_allow = chm_allow  
       FROM @TempCheck1  
       IF @strchm_status = 'N'  
       BEGIN  
         SET @ErrorMessage = '<Message>Slip Not Issued</Message>'  
         SET @o_vcFlag = 'E'  
         SET @o_vcMessage = @ErrorMessage  
         RETURN 1;  
       END  
       ELSE IF @strchm_status = 'D'  
       BEGIN  
         SET @ErrorMessage = '<Message>Slip is Destroyed</Message>'  
         SET @o_vcFlag = 'E'  
         SET @o_vcMessage = @ErrorMessage  
         RETURN 1;  
       END  
       ELSE IF @strchm_status = 'A' OR @strchm_status = 'L'  
       BEGIN  
         SET @ErrorMessage = '<Message>Used as Loose Slip</Message>'  
         SET @o_vcFlag = 'E'  
         SET @o_vcMessage = @ErrorMessage  
         RETURN 1;  
       END  
       ELSE IF @strchm_status = 'P'  
       BEGIN  
         SET @ErrorMessage = '<Message>Slip is Issued to POA</Message>'  
         SET @o_vcFlag = 'E'  
         SET @o_vcMessage = @ErrorMessage  
         RETURN 1;  
       END  
       ELSE IF @strchm_status = 'B'  
       BEGIN  
         SET @ErrorMessage = '<Message>Slip is Issued to Branch</Message>'  
         SET @o_vcFlag = 'E'  
         SET @o_vcMessage = @ErrorMessage  
         RETURN 1;  
       END  
    
       IF EXISTS (SELECT chs_status  FROM Chequestop(NOLOCK)  
       WHERE chs_chqno = @strtb_internal_refno AND chs_instcd = @strtb_instcd )  
       BEGIN  
         DECLARE @strchs_status CHAR(1) = ''  
         SELECT @strchs_status = chs_status  
         FROM Chequestop(NOLOCK)  
         WHERE chs_chqno = @strtb_internal_refno AND chs_instcd = @strtb_instcd  
         IF @strchs_status = 'S'  
         BEGIN  
           SET @ErrorMessage = '<Message>Slip No. is under Stop Status.</Message>'  
           SET @o_vcFlag = 'E'  
           SET @o_vcMessage = @ErrorMessage  
           RETURN 1;  
         END  
         ELSE IF @strchs_status = 'D'  
         BEGIN  
           SET @ErrorMessage = '<Message>Slip is Destroyed</Message>'  
           SET @o_vcFlag = 'E'  
           SET @o_vcMessage = @ErrorMessage  
           RETURN 1;  
         END  
       END  
       IF @strChm_cmcd <> ''  
       BEGIN  
         SET @ClientCd = @strChm_cmcd  
       END  
       ELSE IF @strchm_status = 'R'  
       BEGIN  
         SET @ErrorMessage = '<Message>Slip has been Sent to Printer</Message>'  
         SET @o_vcFlag = 'E'  
         SET @o_vcMessage = @ErrorMessage  
         RETURN 1;  
       END  
       ELSE  
       BEGIN  
         SET @ErrorMessage = '<Message>Invalid Slip Status</Message>'  
         SET @o_vcFlag = 'E'  
         SET @o_vcMessage = @ErrorMessage  
         RETURN 1;  
       END  
     END  
     ELSE  
     BEGIN  
       SET @intiPos = 0;  
       DECLARE @strSlipmnt CHAR(1) = '';  
       DECLARE @strSlipMaintanace VARCHAR(50) = (  
       SELECT sp_sysvalue  
       FROM Sysparameter(NOLOCK)  
       WHERE sp_parmcd = 'SLIPMNT' )  
       IF @strSlipMaintanace <> ''  
       BEGIN  
         SET @intiPos = CHARINDEX('925', @strSlipMaintanace);  
         IF @intiPos > 0  
         BEGIN  
           SET @strSlipmnt = SUBSTRING(@strSlipMaintanace, @intiPos + 4, 1);  
         END  
       END  
       IF @strSlipmnt = 'F'  
       BEGIN  
         SET @ErrorMessage = '<Message>Slip No. has not been issued.</Message>'  
         SET @o_vcFlag = 'E'  
         SET @o_vcMessage = @ErrorMessage  
         RETURN 1;  
       END  
       ELSE IF @strSlipmnt = 'P'  
       BEGIN  
         SET @ErrorMessage = '<Message>Slip Not Found in Stock.! Proceed</Message>'  
         SET @o_vcFlag = 'E'  
         SET @o_vcMessage = @ErrorMessage  
         RETURN 1;  
       END  
       ELSE IF @strSlipmnt <> 'N'  
       BEGIN  
         SET @ErrorMessage = '<Message>Invalid system parameter found in slip maintenance</Message>'  
         SET @o_vcFlag = 'E'  
         SET @o_vcMessage = @ErrorMessage  
         RETURN 1;  
       END  
     END  
     IF @strchm_allow = 'Y' OR @strchm_allow = 'E' OR @strchm_allow = ''  
     BEGIN  
       SET @ErrorMessage = '<Message>Issue of this DIS (' + @strtb_internal_refno +   
       ') has not been registered with CDSL Or you have not imported response of DIS issue Upload. Do you Still want to Proceed ?</Message><ClientId>'   
       + @ClientCd + '</ClientId>'  
      SET @o_vcFlag = 'S'  
      SET @o_vcMessage = @ErrorMessage  
     END
     SET @o_vcFlag = 'S'  
     SET @o_vcMessage = '<Message></Message><ClientId>'+ @ClientCd + '</ClientId>'
   END  
   ELSE
   IF @InstrumentType != '901'  
   BEGIN  
     SET @ErrorMessage = '<Message>Internal reference number already exist</Message>'  
     SET @o_vcFlag = 'E'  
     SET @o_vcMessage = @ErrorMessage  
     RETURN 1;  
   END 
   
   DECLARE @strUs_archiveyn CHAR(1) = ''  
   SELECT @strUs_archiveyn = us_archiveyn  
   FROM @TempCheck  
   IF @strUs_archiveyn = 'Y'  
   BEGIN  
     SET @ErrorMessage = '<Message>Invalid State, Data is Archived or Deleted</Message>'  
     SET @o_vcFlag = 'E'  
     SET @o_vcMessage = @ErrorMessage  
     RETURN 1;  
   END  
  
   
   IF ISNULL(@o_vcFlag,'E') = 'S'	
   BEGIN
     SELECT @ClientID = @ClientCD, @CertQty = ie_noofcert
     FROM Inward_entry(NOLOCK)
     WHERE ie_slipno = @IRN AND ie_trxtype = '901';

        -- Determine Receive Mode based on SlipMode
     SET @SlipMode = @strchm_status
        
     DECLARE @RecvMode INT;
     SET @RecvMode = CASE @SlipMode WHEN 'S' THEN 0 WHEN 'F' THEN 1 WHEN 'T' THEN 2
     WHEN 'O' THEN 3 WHEN 'E' THEN 4 WHEN 'V' THEN 5 WHEN 'P' THEN 6 WHEN 'C' THEN 7
             WHEN 'H' THEN 8 ELSE 0 END;

        -- Return values
     SET @o_vcMessage = @o_vcMessage+(SELECT ISNULL(@ClientID,'') AS ClientId, ISNULL(@ClientName,'') AS ClientName, ISNULL(@CertQty,0) AS CertificateQty, ISNULL(@RecvMode ,'')AS ReceiveMode
	 FOR XML PATH)

   END
   ELSE IF UPPER(@Action) = 'FIND'
   BEGIN
     DECLARE @blnmutualfund BIT = 0; -- Adjust based on actual input source
     DECLARE @SQLQuery NVARCHAR(MAX);
        
     SET @SQLQuery = 'SELECT * FROM Dematmaster(NOLOCK), Instrument_master(NOLOCK) WHERE dm_irn = @IRN AND dm_instcd = im_instcd AND im_desc = @InstrumentType';
        
     IF @blnmutualfund = 1
      SET @SQLQuery = @SQLQuery + ' AND ISNULL(dm_type, '''') = ''M''';
     ELSE
      SET @SQLQuery = @SQLQuery + ' AND ISNULL(dm_type, ''O'') = ''O''';
        
      EXEC sp_executesql @SQLQuery, N'@IRN NVARCHAR(50), @InstrumentType NVARCHAR(50)', @IRN, @InstrumentType;
     END
   END
END
GO