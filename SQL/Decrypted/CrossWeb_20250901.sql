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
  IF UPPER(@i_vcSelection) = 'CLIENT' AND TRIM(@i_vcCode) <> ''
  BEGIN
     DELETE FROM #tb_UserList1 WHERE ClientCode <> @i_vcCode
  END
  ELSE IF UPPER(@i_vcSelection) = 'BRANCH' AND TRIM(@i_vcCode) <> ''
  BEGIN
     DELETE FROM #tb_UserList1 WHERE ClientCode NOT IN(SELECT distinct cm_cd FROM Client_master where cm_cd = ClientCode and cm_brboffcode = @i_vcCode)
  END
  ELSE IF UPPER(@i_vcSelection) = 'FAMILY' AND TRIM(@i_vcCode) <> ''
  BEGIN
     DELETE FROM #tb_UserList1 WHERE ClientCode NOT IN(SELECT distinct cm_cd FROM Client_master where cm_cd = ClientCode and cm_familycd = @i_vcCode)
  END
  ELSE IF UPPER(@i_vcSelection) = 'GROUP' AND TRIM(@i_vcCode) <> ''
  BEGIN
     DELETE FROM #tb_UserList1 WHERE ClientCode NOT IN(SELECT distinct cm_cd FROM Client_master where cm_cd = ClientCode and cm_groupcd = @i_vcCode)
  END
  ELSE IF UPPER(@i_vcSelection) = 'BACKOFFICECD' AND TRIM(@i_vcCode) <> ''
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
