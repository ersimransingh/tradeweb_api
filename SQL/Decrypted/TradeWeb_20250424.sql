CREATE TYPE [dbo].[tb_ParamList] AS TABLE(
	[ParameterName] [varchar](max) NULL,
	[ParameterValue] [varchar](max) NULL,
	[HeaderName] [varchar](100) NULL,
	[JsonTag] [int] NULL
)
GO

CREATE TYPE [dbo].[UserAccessList] AS TABLE(
	[ClientCode] [varchar](20) NULL,
	[ClientName] [varchar](200) NULL
)
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

CREATE FUNCTION [dbo].[fn_GetClients](@i_vcUserid VARCHAR(50), @i_vcSelectListTag VARCHAR(1)='', @i_vcSelectListCode VARCHAR(500)='') 
RETURNS @o_tbOutPutTable TABLE(Client_Code VARCHAR(50))
AS       
BEGIN 
  /*
 ///////////////////////////////////////////////////////////////////////////////////////////
 // Create By     : VAIBHAV GARG
 // Created Date  : 21-NOV-2023
 // Description   : 
 // Reviewed By   : 
 // Review Date   : 
 //////////////////////////////////////////////////////////////////////////////////////////
 */

  DECLARE @strUserType VARCHAR(1) = '', @strUserAccessValue VARCHAR(50)=''
  DECLARE @tbl_UserAccessRights TABLE(UserType VARCHAR(1), UserAccessValue VARCHAR(50))
  
  IF @i_vcUserid <> ''
  BEGIN
    INSERT INTO @tbl_UserAccessRights
    SELECT LA_grouping, LA_GrCode from LoginAccess(NOLOCK) 
    WHERE LA_UserId IN(SELECT VALUE FROM ReturnTable(@i_vcUserid,','))

    DECLARE db_CursorClientList CURSOR FOR         
    SELECT distinct UserType, UserAccessValue 
    FROM @tbl_UserAccessRights  
 
    OPEN db_CursorClientList       
    FETCH NEXT FROM db_CursorClientList INTO @strUserType, @strUserAccessValue 
    WHILE @@FETCH_STATUS = 0     
    BEGIN
      IF  @strUserType = 'B'
	  BEGIN
	    INSERT INTO @o_tbOutPutTable(Client_Code)
	    SELECT DISTINCT cm_cd from client_master(nolock) where cm_brboffcode = @strUserAccessValue and cm_schedule = '49843750'
	  END  
	  ELSE IF @strUserType = 'F'
	  BEGIN
	    INSERT INTO @o_tbOutPutTable(Client_Code)
	    SELECT DISTINCT cm_cd from client_master(nolock) where cm_familycd = @strUserAccessValue   and cm_schedule = '49843750'
	  END
	  ELSE IF @strUserType = 'A'
	  BEGIN
	    INSERT INTO @o_tbOutPutTable(Client_Code)
	    SELECT DISTINCT cm_cd from client_master(nolock) where ISNULL(cm_freezeyn,'N') = 'N'  and cm_schedule = '49843750'
	  END
	  ELSE IF @strUserType = 'G'
	  BEGIN
	    INSERT INTO @o_tbOutPutTable(Client_Code)
	    SELECT DISTINCT cm_cd from client_master(nolock) where cm_groupcd = @strUserAccessValue  and cm_schedule = '49843750'
	  END
	  ELSE IF @strUserType = 'C'
	  BEGIN
	    INSERT INTO @o_tbOutPutTable(Client_Code)
	    SELECT DISTINCT cm_cd from client_master(nolock) where cm_cd = @strUserAccessValue  and cm_schedule = '49843750'
	  END
      FETCH NEXT FROM db_CursorClientList INTO @strUserType, @strUserAccessValue 
    END        
    CLOSE db_CursorClientList        
    DEALLOCATE db_CursorClientList
  END
  ELSE
  BEGIN
    INSERT INTO @o_tbOutPutTable(Client_Code)
	SELECT DISTINCT cm_cd from client_master(nolock) WHERE cm_schedule = 49843750
  END

   IF @i_vcSelectListTag = 'B'
   BEGIN
     DELETE FROM @o_tbOutPutTable 
	 WHERE Client_Code NOT IN(SELECT DISTINCT cm_cd from client_master(nolock) where cm_brboffcode IN( SELECT VALUE FROM DBO.ReturnTable(@i_vcSelectListCode,',')))
   END
   ELSE IF @i_vcSelectListTag = 'F'
   BEGIN
     DELETE FROM @o_tbOutPutTable 
	 WHERE Client_Code NOT IN(SELECT DISTINCT cm_cd from client_master(nolock) where cm_familycd IN( SELECT VALUE FROM DBO.ReturnTable(@i_vcSelectListCode,','))) 
   END
  ELSE IF @i_vcSelectListTag = 'C'
  BEGIN
    DELETE FROM @o_tbOutPutTable 
	WHERE Client_Code NOT IN(SELECT VALUE FROM DBO.ReturnTable(@i_vcSelectListCode,',')) 
  END
  ELSE IF @i_vcSelectListTag = 'G'
  BEGIN
    DELETE FROM @o_tbOutPutTable 
	WHERE Client_Code NOT IN(SELECT DISTINCT cm_cd from client_master(nolock) where cm_groupcd IN( SELECT VALUE FROM DBO.ReturnTable(@i_vcSelectListCode,','))) 
  END

  IF NOT EXISTS(SELECT 1 FROM @o_tbOutPutTable)
  BEGIN
    INSERT INTO @o_tbOutPutTable(Client_Code)
	VALUES(@i_vcUserid)
  END
  RETURN
END
GO

CREATE PROCEDURE [dbo].[SP_ParameterXMLRep] (@vcXML XML, @o_vcParameterOutput VARCHAR(MAX) OUTPUT)
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
			SELECT ColumnName, ColumnValue = ISNULL(ColumnValue,''), MasterTag, JsonLevel
			FROM #tbl_jsonoutput
			FOR XML PATH('Parameter')
			)
	SET @o_vcParameterOutput = CAST(@xmlOutput AS VARCHAR(MAX))
	DROP TABLE #tbl_jsonoutput
	RETURN
END
GO

CREATE PROCEDURE stpr_Rpt_LedgerNew @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT 
WITH ENCRYPTION
AS
BEGIN
 /*
 ///////////////////////////////////////////////////////////////////////////////////////////
 // Create By     : VAIBHAV GARG
 // Created Date  : 20-NOV-2023
 // Description   : "AccountType":IF VALUE = 'EM'  THEN INCLUDE MARGIN 
 //                                 VALUE = 'MTF' THEN INCLUDE MTF
 //                                 VALUE = 'CX' THEN INCLUDE COMM TRARING
 //                                 VALUE = 'CM' THEN INCLUDE COMM MARGIN A/C
 // Reviewed By   : 
 // Review Date   : 
 //////////////////////////////////////////////////////////////////////////////////////////
*/
 --- Parameter Declaration
  DECLARE @dtFromDate VARCHAR(8), @dtToDt VARCHAR(8), @strUserId VARCHAR(500) = '', @strExchSeg VARCHAR(100),
  @XMLData XML, @strAccountType VARCHAR(500)='', @strTable VARCHAR(50)='', @strsql VARCHAR(MAX) = '',
  @blnTplusCommex BIT, @StrCommexConn VARCHAR(MAX) = '', @strCommTable VARCHAR(100)='', @strCommClientMaster VARCHAR(100)='',
  @strCommCompanyExchange VARCHAR(100)='', @strsql1 VARCHAR(500)='', @strsqlstart VARCHAR(MAX)='', @strsqlLast VARCHAR(500)='',
  @strsqlHeader VARCHAR(MAX)='', @strSqlMain VARCHAR(MAX)='', @strSqlExecute VARCHAR(MAX)='', @strOutputType VARCHAR(1), 
  @strProduct VARCHAR(50)='', @strSelectTag VARCHAR(1)='', @strSelectUsers VARCHAR(500)='', @strSplFilter VARCHAR(MAX)='',
  @strCompanyCode VARCHAR(1)='A', @strRequestFrom VARCHAR(1)='W'
  
  IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
  END 

  SET @XMLData = CAST('<root>'+@vcXML+'</root>' AS XML)
  
  SELECT @dtFromDate = ISNULL(x.value('(FromDt)[1]', 'VARCHAR(8)'),''),
  @strProduct = ISNULL(x.value('(Product)[1]', 'VARCHAR(50)'),''),
  @dtToDt = ISNULL(x.value('(ToDt)[1]', 'VARCHAR(8)'),''),
  @strUserId  = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'),''),
  @strExchSeg = ISNULL(x.value('(ExchSeg)[1]', 'VARCHAR(500)'),''),
  @strAccountType = ISNULL(x.value('(AccountType)[1]', 'VARCHAR(500)'),''),
  @strSelectTag = ISNULL(x.value('(SelectTag)[1]', 'VARCHAR(1)'),''),
  @strSelectUsers = ISNULL(x.value('(SelectUsers)[1]', 'VARCHAR(500)'),''),
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'),''),
  @strSplFilter = ISNULL(x.value('(SplFilter)[1]', 'VARCHAR(MAX)'),''),
  @strCompanyCode  = ISNULL(x.value('(CompanyCode)[1]', 'VARCHAR(1)'),''),
  @strRequestFrom  = ISNULL(x.value('(RequestFrom)[1]', 'VARCHAR(1)'),'')
  FROM @XMLData.nodes('/root') AS XTbl(x) 
  
  IF ISNULL(@strRequestFrom,'') = ''
  BEGIN
    SET @strRequestFrom = 'W'
  END
  
  
  IF ISNULL(@strCompanyCode,'') = ''
  BEGIN
    SET @strCompanyCode = 'A'
  END
 

  
  DECLARE @tbl_ReportOptions TABLE(ReportType VARCHAR(50))
  
  INSERT INTO @tbl_ReportOptions (ReportType)
  SELECT VALUE FROM ReturnTable(@strAccountType,',')
  
  SET @strTable = ' Ledger '
  --SET @blnTplusCommex = dbo.mfnGetSysSplFeatureCommodity('TCM');
  SET @blnTplusCommex = 1
  IF @blnTplusCommex = 1
  BEGIN
    SELECT @StrCommexConn = LTRIM(RTRIM(OP_DataBase)) 
	FROM Other_Products(NOLOCK) WHERE OP_Product = 'Commex' and RTRIM(LTRIM(op_status)) = 'A'
	
    IF @StrCommexConn IS NOT NULL AND @StrCommexConn <> ''
    BEGIN
      SET @strCommTable = @StrCommexConn + '.DBO.ledger';
      SET @strCommClientMaster = @StrCommexConn + '.DBO.Client_master';
      SET @strCommCompanyExchange = @StrCommexConn + '.DBO.CompanyExchangeSegments';
    END
  END
  ELSE
  BEGIN
    SET @strCommTable = 'ledger';
    SET @strCommClientMaster = 'Client_master';
    SET @strCommCompanyExchange = 'CompanyExchangeSegments';
  END  
  
  DECLARE @tbl_LenderReort TABLE(SERIALNO INT IDENTITY(1,1), ClientCode varchar(16), Date varchar(10), ExchSeg varchar(100), 
  Voucher varchar(50), Particular nvarchar(500), Debitflag varchar(20), Chequeno varchar(50), 
  DebitAmount MONEY, CreditAmount MONEY, Balance MONEY, BalanceTag VARCHAR(2), Documenttype varchar(100), Common varchar(100),
  Ldate varchar(15), CESCD varchar(100), LookUp varchar(max))
  
  DECLARE @tbl_LedgerDetail table(tag INT,
  ld_clientcd VARCHAR(50),ld_dt VARCHAR(15),ExchSeg VARCHAR(50), Voucher VARCHAR(100),
  ld_amount MONEY,ld_particular VARCHAR(500),ld_debitflag VARCHAR(10), ld_chequeno VARCHAR(50),
  ld_documenttype VARCHAR(100),ld_common VARCHAR(100),Ldate VARCHAR(15),ld_dpid VARCHAR(100),LookUp VARCHAR(MAX), ld_accyear VARCHAR(50), ld_documentno VARCHAR(50))
  
  SET @strsqlstart = ' DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) INSERT INTO @tbl_UserList(Client_Code) 
                  SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''') '
  IF @strProduct = 'Trading'
  BEGIN  
    SET @strsql = @strsql +' SELECT 1 as tag, A.Client_Code AS ld_clientcd,    '''+@dtFromDate+''' AS ld_dt,
                  '''' AS [ExchSeg], '''' AS ''Voucher'',
                  CAST(SUM(CASE SIGN(DATEDIFF(DAY, '''+@dtFromDate+''', ld_dt)) WHEN -1 THEN ld_amount ELSE 0 END) AS DECIMAL(15, 2)) AS ld_amount,
                  ''Opening Balance'' AS ld_particular,
                  CASE SIGN(SUM(ld_amount)) WHEN 1 THEN ''D'' ELSE ''C'' END AS ld_debitflag,
                  '''' AS ld_chequeno, ''O'' AS ld_documenttype, '''' AS ld_common, '''+@dtFromDate+''' AS Ldate, 
                  '''' AS ld_dpid, '''' AS LookUp, '''' as ld_accyear, '''' as ld_documentno
                  FROM @tbl_UserList A, @@##client_master@@## (NOLOCK), @@##Ledger@@## (NOLOCK) LEFT OUTER JOIN @@##Companyexchangesegments@@## (NOLOCK) ON (LD_DPID = CES_Cd) @@##MTF##@@  
				  WHERE A.Client_Code = cm_cd AND SUBSTRING(LD_DPID,1,1) = '''+@strCompanyCode+''' ##@@Condition@@## '
	IF @strSplFilter <> ''
    BEGIN
	  SET @strsql =   @strsql+' AND '+@strSplFilter
    END	
    SET @strsql =   @strsql+' AND ld_dt < '''+@dtFromDate+''' '
    IF ISNULL(@strExchSeg,'') <> ''
    BEGIN
      SET @strsql =   @strsql+' AND ld_dpid IN(SELECT VALUE FROM ReturnTable('''+@strExchSeg+''', '','')) '
    END
    SET @strsql =   @strsql+' GROUP BY Client_Code
                 HAVING SUM(ld_amount) <> 0
                 UNION ALL 
                 SELECT 2 as tag, A.Client_Code AS ld_clientcd, ld_dt,
                 RTRIM(CES_Exchange) + ''-'' + CES_Segment AS [ExchSeg], ld_documenttype + ''/'' + ld_documentno AS ''Voucher'',
                 CAST(ld_amount AS DECIMAL(15, 2))  AS ld_amount, ld_particular, ld_debitflag, ld_chequeno,  ld_documenttype,  ld_common, ld_dt as Ldate, 
                 ld_dpid, CASE WHEN ld_documentType = ''B'' THEN SUBSTRING(LD_DPID, 2, 1) + ''/'' + SUBSTRING(LD_DPID, 3, 1) + ''/'' 
				 + ld_common + ''/'' + ld_commondt ELSE '''' END AS LookUp, ld_accyear, ld_documentno
                 FROM @tbl_UserList A,  @@##client_master@@##(NOLOCK) CM1, @@##Ledger@@## (NOLOCK) LEFT OUTER JOIN @@##Companyexchangesegments@@## (NOLOCK) ON (LD_DPID = CES_Cd) @@##MTF##@@ 
                 WHERE A.Client_Code = cm_cd AND SUBSTRING(LD_DPID,1,1) = '''+@strCompanyCode+'''  ##@@Condition@@## '
    IF @strSplFilter <> ''
    BEGIN
	  SET @strsql =   @strsql+' AND '+@strSplFilter
    END	
	
    SET @strsql =   @strsql+' AND ld_dt >= '''+@dtFromDate+''' AND ld_dt <= '''+@dtToDt+''''
    IF ISNULL(@strExchSeg,'') <> ''
    BEGIN
      SET @strsql =   @strsql+' AND ld_dpid IN(SELECT VALUE FROM ReturnTable('''+@strExchSeg+''', '','')) '
    END
  
    SET @strSqlExecute = @strsql
    SET @strSqlExecute = replace(@strSqlExecute,'##@@Condition@@##',' AND ld_clientcd = cm_cd  ')
    SET @strSqlExecute = replace(@strSqlExecute,'@@##MTF##@@',' ')
    SET @strSqlExecute = replace(@strSqlExecute,'@@##client_master@@##',' Client_Master ')
    SET @strSqlExecute = replace(@strSqlExecute,'@@##Ledger@@##',' Ledger ')
    SET @strSqlExecute = replace(@strSqlExecute,'@@##Companyexchangesegments@@##',' Companyexchangesegments ')

    BEGIN TRY
	  INSERT INTO @tbl_LedgerDetail( tag,ld_clientcd,ld_dt,ExchSeg,Voucher,ld_amount,ld_particular,ld_debitflag,ld_chequeno,ld_documenttype,
	  ld_common,Ldate,ld_dpid,LookUp,ld_accyear, ld_documentno)
	  EXEC(@strsqlstart+' '+@strSqlExecute)
    END TRY
    BEGIN CATCH
	  SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = ERROR_MESSAGE()
	  RETURN 1
    END CATCH
  
    IF EXISTS(SELECT 1 FROM @tbl_ReportOptions WHERE ReportType = 'EM')
    BEGIN
      SET @strSqlExecute = @strsql
      SET @strSqlExecute = replace(@strSqlExecute,'##@@Condition@@##',' AND ld_clientcd = cm_brkggroup  ') 
	  SET @strSqlExecute = replace(@strSqlExecute,'@@##MTF##@@',' ')
      SET @strSqlExecute = replace(@strSqlExecute,'@@##client_master@@##',' Client_Master ')
      SET @strSqlExecute = replace(@strSqlExecute,'@@##Ledger@@##',' Ledger ')
      SET @strSqlExecute = replace(@strSqlExecute,'@@##Companyexchangesegments@@##',' Companyexchangesegments ')

	  BEGIN TRY
        INSERT INTO @tbl_LedgerDetail( tag,ld_clientcd,ld_dt,ExchSeg,Voucher,ld_amount,ld_particular,ld_debitflag,ld_chequeno,
	    ld_documenttype,ld_common,Ldate,ld_dpid,LookUp,ld_accyear, ld_documentno)
	    EXEC(@strsqlstart+' '+@strSqlExecute)
	  END TRY
      BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = ERROR_MESSAGE()
	    RETURN 1
      END CATCH  
    END
  
    IF EXISTS(SELECT 1 FROM @tbl_ReportOptions WHERE ReportType = 'MTF')
	AND EXISTS(SELECT 1 FROM SYS.TABLES WHERE NAME= 'MrgTdgFin_Clients')
    BEGIN
      set @strSqlExecute = @strsql
      SET @strSqlExecute = replace(@strSqlExecute,'@@##MTF##@@',' , MrgTdgFin_Clients(NOLOCK) ')
      SET @strSqlExecute = replace(@strSqlExecute,'##@@Condition@@##',' AND ld_clientcd = MTFC_FillerB AND MTFC_CMCD = CM_CD AND MTFC_FillerB <> '''' ' ) 
      SET @strSqlExecute = replace(@strSqlExecute,'@@##client_master@@##',' Client_Master ')
      SET @strSqlExecute = replace(@strSqlExecute,'@@##Ledger@@##',' Ledger ')
      SET @strSqlExecute = replace(@strSqlExecute,'@@##Companyexchangesegments@@##',' Companyexchangesegments ')
   
	  BEGIN TRY
        INSERT INTO @tbl_LedgerDetail( tag,ld_clientcd,ld_dt,ExchSeg,Voucher,ld_amount,ld_particular,ld_debitflag,ld_chequeno,
	    ld_documenttype,ld_common,Ldate,ld_dpid,LookUp,ld_accyear, ld_documentno)
	    EXEC(@strsqlstart+' '+@strSqlExecute)
	  END TRY
      BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = ERROR_MESSAGE()
	    RETURN 1
      END CATCH  
    END


    IF EXISTS(SELECT 1 FROM @tbl_ReportOptions WHERE ReportType = 'CX') AND @StrCommexConn <> ''
    BEGIN
      set @strSqlExecute = @strsql
      SET @strSqlExecute = replace(@strSqlExecute,'@@##MTF##@@',' ')
      SET @strSqlExecute = replace(@strSqlExecute,'##@@Condition@@##',' AND ld_clientcd = cm_cd ' ) 
    
      SET @strSqlExecute = replace(@strSqlExecute,'@@##client_master@@##',@strCommClientMaster)
      SET @strSqlExecute = replace(@strSqlExecute,'@@##Ledger@@##',@strCommTable)
      SET @strSqlExecute = replace(@strSqlExecute,'@@##Companyexchangesegments@@##',@strCommCompanyExchange)
    
	  BEGIN TRY
        INSERT INTO @tbl_LedgerDetail( tag,ld_clientcd,ld_dt,ExchSeg,Voucher,ld_amount,ld_particular,ld_debitflag,ld_chequeno,
	    ld_documenttype,ld_common,Ldate,ld_dpid,LookUp,ld_accyear, ld_documentno)
	    EXEC(@strsqlstart+' '+@strSqlExecute)
	  END TRY
      BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = ERROR_MESSAGE()
	    RETURN 1
      END CATCH  
    END
  
    IF EXISTS(SELECT 1 FROM @tbl_ReportOptions WHERE ReportType = 'CM') AND @StrCommexConn <> ''
    BEGIN
      set @strSqlExecute = @strsql
      SET @strSqlExecute = replace(@strSqlExecute,'@@##MTF##@@',' ')
      SET @strSqlExecute = replace(@strSqlExecute,'##@@Condition@@##',' AND ld_clientcd = cm_brkggroup ' ) 
    
      SET @strSqlExecute = replace(@strSqlExecute,'@@##client_master@@##',@strCommClientMaster)
      SET @strSqlExecute = replace(@strSqlExecute,'@@##Ledger@@##',@strCommTable)
      SET @strSqlExecute = replace(@strSqlExecute,'@@##Companyexchangesegments@@##',@strCommCompanyExchange)
   
	  BEGIN TRY
        INSERT INTO @tbl_LedgerDetail( tag,ld_clientcd,ld_dt, ExchSeg, Voucher, ld_amount,ld_particular,ld_debitflag,ld_chequeno,
	   ld_documenttype,ld_common,Ldate,ld_dpid,LookUp,ld_accyear, ld_documentno)
	    EXEC(@strsqlstart+' '+@strSqlExecute)
	  END TRY
      BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = ERROR_MESSAGE()
	    RETURN 1
      END CATCH  
    END
  END
  ELSE IF @strProduct = 'DP'
  BEGIN
  
   DECLARE @strCrossCon VARCHAR(50)='', @strEstroCon VARCHAR(50)='', @strDefaultConn VARCHAR(50)='', @strCrossServer VARCHAR(50), @strEstroServer VARCHAR(50),
   @strDefaultServer VARCHAR(50)=''
   SELECT @strCrossCon = RTRIM(LTRIM(OP_DataBase)), @strCrossServer = RTRIM(LTRIM(OP_Server))  FROM Other_Products where OP_Product = 'Cross' and RTRIM(LTRIM(op_status)) = 'A'
   SELECT @strEstroCon = RTRIM(LTRIM(OP_DataBase)), @strEstroServer = RTRIM(LTRIM(OP_Server))  FROM Other_Products where OP_Product = 'Estro' and RTRIM(LTRIM(op_status)) = 'A'
   IF @strCrossCon = ''
   BEGIN
     SET @strDefaultConn = @strEstroCon
	 SET @strDefaultServer = @strEstroServer
   END
   ELSE
   BEGIN
     SET @strDefaultConn = @strCrossCon
	 SET @strDefaultServer = @strCrossServer
   END
   
   SET @strsql = @strsql +' UPDATE A SET A.DPClientCode = B.da_actno FROM @tbl_UserList A, Dematact B WHERE A.Client_Code = B.da_clientcd AND da_defaultyn=''Y'' '
   SET @strsql = @strsql +' UPDATE A SET A.DPClientCode = A.Client_code FROM @tbl_UserList A WHERE  ISNULL(DPClientCode,'''') = '''''
   SET @strsql = @strsql +' SELECT 1 as tag, A.DPClientCode AS ld_clientcd,    CONVERT(VARCHAR,'''+@dtFromDate+''',112) AS ld_dt,
                  '''' AS [ExchSeg], '''' AS ''Voucher'',
                  CAST(SUM(CASE SIGN(DATEDIFF(DAY, '''+@dtFromDate+''', CAST(CONVERT(VARCHAR,ld_dt,112) AS DATE))) WHEN -1 THEN ld_amount ELSE 0 END) AS DECIMAL(15, 2)) AS ld_amount,
                  ''Opening Balance'' AS ld_particular,
                  CASE SIGN(SUM(ld_amount)) WHEN 1 THEN ''D'' ELSE ''C'' END AS ld_debitflag,
                  '''' AS ld_chequeno, ''O'' AS ld_documenttype, '''' AS ld_common,  CONVERT(VARCHAR,'''+@dtFromDate+''',112) AS Ldate, 
                  '''' AS ld_dpid, '''' AS LookUp, '''' as ld_accyear, '''' as ld_documentno
                  FROM @tbl_UserList A, @@##client_master@@## (NOLOCK), @@##Ledger@@## (NOLOCK)
				  WHERE A.DPClientCode = cm_cd AND cm_cd = ld_clientcd '
  
    SET @strsql =   @strsql+' AND CAST(CONVERT(VARCHAR,ld_dt,112) AS DATE) < '''+@dtFromDate+''' '
    
    SET @strsql =   @strsql+' GROUP BY DPClientCode
                 HAVING SUM(ld_amount) <> 0
                 UNION ALL 
                 SELECT 2 as tag, A.DPClientCode AS ld_clientcd, ld_dt = CAST(CONVERT(VARCHAR,ld_dt,112) AS DATE),
                 '''' AS [ExchSeg], ld_documenttype + ''/'' + ld_documentno AS ''Voucher'',
                 CAST(ld_amount AS DECIMAL(15, 2))  AS ld_amount, ld_particular, ld_debitflag, ld_chequeno,  ld_documenttype,  ld_common, CAST(CONVERT(VARCHAR,ld_dt,112) AS DATE) as Ldate, 
                 ld_dpid, CASE WHEN ld_documentType = ''B'' THEN SUBSTRING(LD_DPID, 2, 1) + ''/'' + SUBSTRING(LD_DPID, 3, 1) + ''/'' 
				 + ld_common + ''/'' + ld_commondt ELSE '''' END AS LookUp, ld_accyear, ld_documentno
                 FROM @tbl_UserList A,  @@##client_master@@##(NOLOCK) CM1, @@##Ledger@@## (NOLOCK)  
                 WHERE A.DPClientCode = cm_cd  AND cm_cd = ld_clientcd '
  
    SET @strsql =   @strsql+' AND CAST(CONVERT(VARCHAR,ld_dt,112) AS DATE) >= '''+@dtFromDate+''' AND CAST(CONVERT(VARCHAR,ld_dt,112) AS DATE) <= '''+@dtToDt+''''
    
    SET @strSqlExecute = @strsql
    SET @strSqlExecute = replace(@strSqlExecute,'@@##client_master@@##',@strDefaultConn+'.dbo.Client_Master ')
    SET @strSqlExecute = replace(@strSqlExecute,'@@##Ledger@@##',@strDefaultConn+'.dbo.Ledger ')
    
    BEGIN TRY
	  INSERT INTO @tbl_LedgerDetail( tag,ld_clientcd,ld_dt,ExchSeg,Voucher,ld_amount,ld_particular,ld_debitflag,ld_chequeno,ld_documenttype,
	  ld_common,Ldate,ld_dpid,LookUp,ld_accyear, ld_documentno)
	  EXEC(@strsqlstart+' '+@strSqlExecute)
    END TRY
    BEGIN CATCH
	  SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = ERROR_MESSAGE()
	  RETURN 1
    END CATCH
  END
  

  IF EXISTS(SELECT 1 FROM @tbl_LedgerDetail)
  BEGIN
   INSERT INTO @tbl_LenderReort( ClientCode, Date, ExchSeg, Voucher, Particular, Debitflag, Chequeno,
   DebitAmount, CreditAmount, Balance, BalanceTag, Documenttype, Common, Ldate, CESCD, LookUp)
   SELECT ld_clientcd, ld_dt, ExchSeg, Voucher, ld_particular, ld_debitflag, ld_chequeno,
	              DebitAmount = CASE WHEN ld_amount>0 THEN ld_amount ELSE 0 END, 
				  CreditAmount =  ABS(CASE WHEN ld_amount < 0 THEN ld_amount ELSE 0 END),
	              Balance = ABS(sum(ld_amount) OVER(PARTITION  BY ld_clientcd  ORDER BY tag, LD_DT, ld_particular, ld_accyear, ld_documentno,ld_documenttype)),
	              BalanceTag = CASE WHEN (sum(ld_amount) OVER(PARTITION  BY ld_clientcd  
				  ORDER BY tag, LD_DT, ld_particular, ld_accyear, ld_documentno,ld_documenttype))>0 THEN 'Dr' ELSE'Cr' END,
	              ld_documenttype, ld_common,Ldate,
	              ld_dpid,LookUp
   FROM( SELECT tag = 1,ld_clientcd,ld_dt,ExchSeg,Voucher,ld_amount = SUM(ld_amount) ,
   ld_particular,ld_debitflag = CASE SIGN(SUM(ld_amount)) WHEN 1 THEN 'D' ELSE 'C' END,
   ld_chequeno, ld_documenttype,ld_common,Ldate,ld_dpid,LookUp,ld_accyear, ld_documentno 
   FROM @tbl_LedgerDetail WHERE TAG = 1 GROUP BY ld_clientcd, ld_dt, ExchSeg, Voucher , ld_particular, ld_chequeno,
   ld_documenttype,ld_common,Ldate,ld_dpid,LookUp ,ld_accyear, ld_documentno
   UNION ALL 
   SELECT tag,ld_clientcd,ld_dt,ExchSeg,Voucher,ld_amount,ld_particular,ld_debitflag,ld_chequeno,
   ld_documenttype,ld_common,Ldate,ld_dpid,LookUp,ld_accyear, ld_documentno
   FROM @tbl_LedgerDetail WHERE TAG = 2) XMAIN 
   ORDER BY ld_clientcd, TAG, ld_dt
   

  END
  
  IF @strOutputType = 'X'
  BEGIN
    DECLARE @XMLDATA1 XML
	IF @strRequestFrom = 'W'
	BEGIN
      SET @XMLDATA1 = (SELECT SERIALNO, ClientCode, Date = convert(varchar,cast(Date as date),112), ExchSeg, Voucher, Particular, Debitflag, Chequeno,
	  DebitAmount, CreditAmount, Balance, BalanceTag, Documenttype, Common, Ldate, CESCD, LookUp FROM @tbl_LenderReort 
	  ORDER BY SerialNo desc FOR XML PATH('Ledger'))
	END
	ELSE IF @strRequestFrom = 'M'
	BEGIN
	  SET @XMLDATA1 = (SELECT SERIALNO, ClientCode, Date = convert(varchar,cast(Date as date),112), ExchSeg, Voucher, Particular, 
	  Amount = (CASE WHEN DebitAmount > 0 THEN DebitAmount*-1 ELSE  CreditAmount END),
	  Balance = CAST(ABS(cast(Balance*(CASE WHEN Balance >0 THEN -1 ELSE 1 END) as Numeric(20,2))) AS VARCHAR)+' '+(CASE WHEN Balance >0 THEN 'Cr' ELSE 'Dr' END)
	  FROM @tbl_LenderReort 
	  ORDER BY SerialNo desc FOR XML PATH('Ledger'))
	END
	SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
  END
   IF @strOutputType = 'G'
   BEGIN
    SELECT SERIALNO, ClientCode, Date = convert(varchar,cast(Date as date),112), ExchSeg, Voucher, Particular, Debitflag, 
	Chequeno,
	DebitAmount, CreditAmount, Balance, BalanceTag, Documenttype, Common, Ldate, CESCD, LookUp  
	FROM @tbl_LenderReort ORDER BY SerialNo
   END	
  SET @o_vcErrorFlag  = 'S'
  --SET @o_vcErrorMessage = 'Process Completed'
  RETURN 1
END
GO


CREATE PROCEDURE stpr_Rpt_HoldingNew @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT 
WITH ENCRYPTION
AS
BEGIN
 /*
 ///////////////////////////////////////////////////////////////////////////////////////////
 // Create By     : VAIBHAV GARG
 // Created Date  : 20-NOV-2023
 // Description   : 
 // Reviewed By   : 
 // Review Date   : 
 //////////////////////////////////////////////////////////////////////////////////////////
*/
  DECLARE @dtAsOnDate VARCHAR(8), @strUserId VARCHAR(50), @strProduct VARCHAR(50), @strOutputType VARCHAR(1)='', @XMLData XML,
  @strSelectTag VARCHAR(1)='', @strSelectUsers VARCHAR(500)='', @StrString VARCHAR(MAX)='', @strSplFilter VARCHAR(MAX)='', @strCompanyCode VARCHAR(1),
  @strScripCode VARCHAR(20)=''
  IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
  END 
  DECLARE @dp_Server VARCHAR(50)='', @dp_Database VARCHAR(50)='', @dp_Owner  VARCHAR(50)='', @strHairCut VARCHAR(1)=''
  
  SET @XMLData = CAST('<root>'+@vcXML+'</root>' AS XML)
  
  SELECT @dtAsOnDate = ISNULL(x.value('(AsOnDate)[1]', 'VARCHAR(8)'),''),
  @strProduct = ISNULL(x.value('(Product)[1]', 'VARCHAR(50)'),''),
  @strUserId = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'),''),
  @strSelectTag = ISNULL(x.value('(SelectTag)[1]', 'VARCHAR(1)'),''),
  @strSelectUsers = ISNULL(x.value('(SelectUsers)[1]', 'VARCHAR(500)'),''),
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'),''),
  @strSplFilter = ISNULL(x.value('(SplFilter)[1]', 'VARCHAR(MAX)'),''),
  @strCompanyCode = ISNULL(x.value('(CompanyCode)[1]', 'VARCHAR(1)'),''),
  @strHairCut = ISNULL(x.value('(HairCut)[1]', 'VARCHAR(1)'),''),
  @strScripCode = ISNULL(x.value('(ScripCode)[1]', 'VARCHAR(20)'),'')
  
  FROM @XMLData.nodes('/root') AS XTbl(x) 
  
  
  IF ISNULL(@strCompanyCode,'') = ''
  BEGIN
    SET @strCompanyCode = 'A'
  END
 
  
  DECLARE @tbl_HoldingDate TABLE(HoldingDate VARCHAR(8))
  
  CREATE TABLE #tbl_HoldingRep (ClientCode VARCHAR(50), ClientName VARCHAR(100),
  BranchCode VARCHAR(50), Product VARCHAR(50), ScripCode VARCHAR(15),
  ScripName VARCHAR(100), ISIN VARCHAR(20),Qty MONEY, ClosingPrice MONEY,
  MarketValue MONEY, Haircut MONEY, NetValue MONEY)



  IF @strProduct IN('Trading','BOTH')
  BEGIN

	SET @StrString = ' DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) '
    +' INSERT INTO @tbl_UserList(Client_Code) '
    +' SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''') '
	+' SELECT CmCd As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode, DType As Product, '
	+' SSCD as ScripCode, ScripName , im_isin As ISIN, Qty As Qty, '
	+' 0, 0, 100, 0 '
    +' FROM ( '
	+' SELECT dm_clientcd CMCD, dm_scripcd SSCD, SS_Name As ScripName,  ''Ben'' DType, sum(dm_qty) * -1 Qty   '
    +' FROM Demat(NOLOCK), OurDps(NOLOCK), Settlements(NOLOCK), Client_master(NOLOCK), securities(NOLOCK)  '
    +' WHERE dm_clientcd = cm_cd and dm_ourdp = od_cd And dm_stlmnt = se_stlmnt '
	+' AND ISNULL(od_ActPurpose, '''') <> ''W'' '
    +' AND dm_type = ''BC'' and od_acttype = ''B'' and dm_locked = ''N'' and dm_transfered = ''N'' and ss_cd = dm_scripcd '
    +' AND dm_clientcd in(select client_code from  @tbl_UserList) AND dm_companycode = '''+@strCompanyCode+'''     '

    SET @StrString =   @StrString+' GROUP BY dm_clientcd, dm_scripcd, SS_Name '
    +' UNION ALL '
    +' SELECT dm_clientcd CMCD, dm_scripcd SSCD,  SS_Name As ScripName,  '
    +' ''EXP'' DType, sum(dm_qty) * -1 Qty '
    +' From Demat(NOLOCK), OurDps(NOLOCK), Settlements(NOLOCK), Client_master(NOLOCK), securities (NOLOCK) '
    +' Where dm_clientcd = cm_cd and dm_ourdp = od_cd And dm_stlmnt = se_stlmnt and dm_type = ''BC'''
    +' and od_acttype in (''P'', ''R'') and ss_cd = dm_scripcd  '
	+' and dm_clientcd in(select client_code from  @tbl_UserList) AND dm_companycode = '''+@strCompanyCode+'''      '

    SET @StrString =   @StrString+' and se_payoutdt > '''+@dtAsOnDate+''' '
    +' GROUP BY dm_clientcd, dm_scripcd, SS_Name '
	+' UNION ALL '
	+' SELECT dm_clientcd CMCD, dm_scripcd SSCD,  SS_Name As ScripName,  '
    +' ''POOL'' DType, sum(dm_qty) * -1 Qty '
    +' From Demat(NOLOCK), OurDps(NOLOCK), Settlements(NOLOCK), Client_master(NOLOCK), securities (NOLOCK) '
    +' Where dm_clientcd = cm_cd and dm_ourdp = od_cd And dm_stlmnt = se_stlmnt and dm_type = ''BC'''
    +' and od_acttype in (''P'', ''R'') and ss_cd = dm_scripcd and dm_locked = ''N'' '
	+' and dm_clientcd in(select client_code from  @tbl_UserList)  AND dm_companycode = '''+@strCompanyCode+'''     '

    SET @StrString =   @StrString+' and dm_transfered = ''N'' and  se_payoutdt <= '''+@dtAsOnDate+''' '
    +' GROUP BY dm_clientcd, dm_scripcd, SS_Name '
	
    +' UNION ALL '
    +' SELECT dm_clientcd CMCD, dm_scripcd SSCD, SS_Name As ScripName, ''UNDEL'' Dtype, sum(dm_qty) * -1 Qty  '
    +' FROM Demat(NOLOCK), OurDps(NOLOCK), Settlements(NOLOCK), Client_master(NOLOCK), securities (NOLOCK) '
    +' Where dm_ourdp = od_cd And dm_stlmnt = se_stlmnt and dm_type = ''CB'' and od_acttype in (''P'', ''R'') '
    +' and dm_clientcd in(select client_code from  @tbl_UserList)  AND dm_companycode = '''+@strCompanyCode+'''     '

    SET @StrString =   @StrString+' and dm_clientcd = cm_cd and dm_locked = ''N'' and dm_transfered<> ''S'' '
	+' and se_payoutdt > '''+@dtAsOnDate+'''  '
    +' and ss_cd = dm_scripcd Group By dm_clientcd, dm_scripcd, SS_Name '
    +' UNION ALL '
    +' SELECT CUP_clientcd cmcd, CUP_scripcd sscd, SS_Name As ScripName, ''CUSPA'' Dtype, sum(case CUP_DRCR  when ''C'' then CUP_Qty else CUP_Qty * (-1) end) Qty  '
    +' From CUSAPledge_TRX(NOLOCK), Client_master(NOLOCK), securities (NOLOCK) '
    +' Where CUP_dt <= '''+@dtAsOnDate+''' and CUP_TRXFlag = ''P''  and ss_cd = CUP_scripcd and cm_cd = CUP_clientcd  '
    +' and cm_cd in(select client_code from  @tbl_UserList)  AND CUP_companycode = '''+@strCompanyCode+'''    '

    SET @StrString =   @StrString+' GROUP BY CUP_clientcd , CUP_scripcd, SS_Name '
    +' Having (sum(case CUP_DRCR  when ''C'' then CUP_Qty else CUP_Qty * (-1) end)) > 0 '
    +' UNION ALL '
    +' SELECT MPT_clientcd cmcd, MPT_scripcd sscd, SS_Name As ScripName,  ''FOCOLL'' DType, sum(case MPT_DRCR  when ''C'' then MPT_Qty else MPT_Qty * (-1) end) Qty  '
    +' FROM MrgPledge_TRX(NOLOCK), Client_master(NOLOCK), securities (NOLOCK) '
    +' WHERE MPT_dt <= '''+@dtAsOnDate+''' and MPT_TRXFlag = ''P'' and MPT_clientcd = cm_cd and ss_cd = MPT_scripcd  '
    +' AND cm_cd in(select client_code from  @tbl_UserList)   AND MPT_companycode = '''+@strCompanyCode+'''   '

    SET @StrString =   @StrString+' GROUP BY MPT_clientcd, MPT_scripcd, SS_Name '
    +' Having (sum(case MPT_DRCR  when ''C'' then MPT_Qty else MPT_Qty * (-1) end)) > 0 '
    
	IF EXISTS(SELECT 1 FROM SYS.TABLES WHERE NAME= 'MrgTdgFin_Pledge')
	BEGIN
	  SET @StrString = @StrString +' UNION ALL '
      +' SELECT MPT_clientcd CMCD, MPT_scripcd SSCD, SS_Name As ScripName, ''MTFBENF'' DType , sum(case MPT_DRCR  when ''C'' then MPT_Qty else MPT_Qty * (-1) end) '
      +' From MrgTdgFin_Pledge(NOLOCK), Ourdps(NOLOCK), Client_master(NOLOCK), securities (NOLOCK) '
      +' Where MPT_OurDP = od_cd and od_acttype = ''G'' and MPT_dt <= '''+@dtAsOnDate+''' and MPT_TRXFlag = ''P'' '
      +' and MPT_clientcd = cm_cd and ss_cd = MPT_scripcd and cm_cd in(select client_code from  @tbl_UserList) AND MPT_companycode  = '''+@strCompanyCode+'''     '
	
      SET @StrString =   @StrString+' Group By MPT_clientcd , MPT_scripcd, SS_Name  '
      +' Having sum(case MPT_DRCR  when ''C'' then MPT_Qty else MPT_Qty * (-1) end) > 0 '
      +' UNION ALL '
      +' SELECT MPT_clientcd CMCD, MPT_scripcd SSCD, SS_Name As ScripName, ''MTFCOLL'' Dtype ,sum(case MPT_DRCR  when ''C'' then MPT_Qty else MPT_Qty * (-1) end) Qty  '
      +' From MrgTdgFin_Pledge(NOLOCK), Ourdps(NOLOCK), Client_master(NOLOCK), securities(NOLOCK)  '
      +' Where MPT_OurDP = od_cd and od_acttype = ''H'' and MPT_dt <= '''+@dtAsOnDate+''' and MPT_TRXFlag = ''P''  '
      +' and MPT_clientcd = cm_cd and ss_cd = MPT_scripcd  and cm_cd in(select client_code from  @tbl_UserList) AND MPT_companycode  = '''+@strCompanyCode+'''  '
      SET @StrString =   @StrString  +' Group By MPT_clientcd , MPT_scripcd, SS_Name '
      +' Having sum(case MPT_DRCR  when ''C'' then MPT_Qty else MPT_Qty * (-1) end) > 0 '
	END
    
	SET @StrString = @StrString +' ) A , client_master(NOLOCK) , isin(NOLOCK), Securities(NOLOCK) '
      +' WHERE cm_schedule = (select sp_sysvalue from Sysparameter where sp_parmcd = ''cmschedule'') '
      +' AND CMCD = cm_cd and SSCD = im_scripcd  '
	IF @strSplFilter <> ''
    BEGIN
	   SET @StrString =   @StrString+' AND '+@strSplFilter
    END	
      SET @StrString =   @StrString+' AND im_priority in (select min(im_priority) from isin(NOLOCK) where SSCD = im_scripcd and sscd = ss_cd) '
	  
	IF @strScripCode <> ''
	BEGIN
	  SET @StrString =   @StrString+' AND ss_cd = '''+@strScripCode+''' '
	END
	  
	BEGIN TRY

	  INSERT INTO #tbl_HoldingRep(ClientCode, ClientName, BranchCode, Product, ScripCode, ScripName,
	  ISIN, Qty, ClosingPrice, MarketValue, Haircut, NetValue)
      EXEC(@StrString)
    END TRY
    BEGIN CATCH
	  SET @o_vcErrorFlag  = 'E'
      SET @o_vcErrorMessage = ERROR_MESSAGE()
      RETURN 1
    END CATCH
  
    SELECT @dp_Server = LTRIM(RTRIM(OP_Server)), @dp_Database = LTRIM(RTRIM(OP_DataBase)),
    @dp_Owner = LTRIM(RTRIM(OP_Owner)) FROM Other_Products(NOLOCK) 
    WHERE OP_Product = 'Cross'
    AND op_Status = 'A'
	
  
    DELETE FROM @tbl_HoldingDate
    IF @dp_Database <> ''
    BEGIN
	  SET @StrString = 'select max(hld_hold_date) from '+@dp_Database+'.[dbo].Holding '
	  INSERT INTO @tbl_HoldingDate
	  EXEC(@StrString)
	
	  IF EXISTS(SELECT 1 FROM @tbl_HoldingDate WHERE HoldingDate <= @dtAsOnDate)
	  BEGIN
	    SET @StrString = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) INSERT INTO @tbl_UserList(Client_Code) 
                SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''') '
        SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = B.da_actno FROM @tbl_UserList A, Dematact B WHERE A.Client_Code = B.da_clientcd AND da_defaultyn=''Y'' '
        SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = A.Client_code FROM @tbl_UserList A WHERE  ISNULL(DPClientCode,'''') = '''''
        SET @StrString = @StrString +' SELECT CmCd As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode, DType As Product, '
	    +' ss_cd as ScripCode, ScripName , im_isin As ISIN, hld_ac_pos As Qty, 0, 0, 100, 0 '
        +' FROM (SELECT Client_code cmcd,  im_scripcd ss_cd, SS_Name As ScripName, ISNULL(cm_poaforpayin,''N'')+''DP'' Dtype, hld_ac_pos As hld_ac_pos, im_isin '
        +' FROM '+@dp_Database+'.[dbo].Holding, Isin(NOLOCK), Securities(NOLOCK), @tbl_UserList, '+@dp_Database+'.DBO.CLIENT_MASTER(NOLOCK) CM1   '
        +' where hld_ac_type = ''11'' '
	    +' and hld_isin_code = im_isin  '
        +' AND ss_cd = im_scripcd '
		+' AND DPClientCode = CM1.CM_CD '
	    +' AND hld_ac_code = DPClientCode  and  im_priority = (Select min(im_priority) from ISIN(NOLOCK) Where im_scripcd = ss_cd)) A, client_master(NOLOCK) '
        +' WHERE cm_schedule = (select sp_sysvalue from Sysparameter where sp_parmcd = ''cmschedule'') '
        +' AND CMCD = cm_cd  '
	    IF @strSplFilter <> ''
        BEGIN
	      SET @StrString =   @StrString+' AND '+@strSplFilter
        END	
      END
	  ELSE
	  BEGIN
	    SET @StrString = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) INSERT INTO @tbl_UserList(Client_Code) 
                        SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''') '
	    SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = B.da_actno FROM @tbl_UserList A, Dematact B WHERE A.Client_Code = B.da_clientcd AND da_defaultyn=''Y'' '
        SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = A.Client_code FROM @tbl_UserList A WHERE  ISNULL(DPClientCode,'''') = '''''
        SET @StrString = @StrString +' Select td_ac_code As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode,  '
		+' ISNULL(cm_poaforpayin,''N'')+''DP'' Dtype, ss_cd, ss_name AS ScripName, '
	    +' td_isin_code As ISIN,  Qty = ABS(Qty) , 0, 0, 100, 0 '
        +' from( Select td_ac_code, td_isin_code, cm_poaforpayin, sum(OpenQty) as Qty '
        +' FROM( SELECT td_ac_code = Client_code, td_isin_code, cm_poaforpayin = ISNULL(cm_poaforpayin,''N'') ,'
        +' SUM(CASE WHEN td_debit_credit=''C'' THEN -td_qty ELSE td_qty END) as OpenQty  '
        +' FROM '+@dp_Database+'.DBO.Trxdetail(nolock), @tbl_UserList, '+@dp_Database+'.DBO.CLIENT_MASTER(NOLOCK) CM1 '
		+' where td_ac_code =  DPClientCode and td_curdate < '''+@dtAsonDate+''' '
        +' AND td_booking_type not in (''13'')  '
		+' AND DPClientCode = CM1.CM_CD '
        +' GROUP BY Client_code, td_isin_code, ISNULL(cm_poaforpayin,''N'')  '
        +' HAVING SUM(CASE WHEN td_debit_credit = ''C'' THEN td_qty ELSE -td_qty END) <> 0) x1  '
        +' group by td_ac_code, td_isin_code, cm_poaforpayin '
        +' UNION ALL  '
        +' SELECT td_ac_code = Client_code, td_isin_code, cm_poaforpayin = ISNULL(cm_poaforpayin,''N''), OpenQty = (Case td_debit_credit  when ''D'' then td_qty else -td_qty end) '
        +' FROM '+@dp_Database+'.DBO.Trxdetail(nolock), @tbl_UserList, '+@dp_Database+'.DBO.CLIENT_MASTER(NOLOCK) CM1 ' 
		+' where td_ac_code = DPClientCode   '
        +' and td_curdate = '''+@dtAsonDate+'''' 
		+' AND DPClientCode = CM1.CM_CD '
        +' AND td_booking_type not in (''13'')) x , '+@dp_Database+'.DBO.Security(NOLOCK) SC, Client_master cm, Isin(NOLOCK), Securities(NOLOCK) '
        +' WHERE X.td_isin_code = SC.sc_isincode and x.td_ac_code = cm.cm_cd AND  td_isin_code = im_isin  '
        +' AND ss_cd = im_scripcd  AND im_priority = (Select min(im_priority) from ISIN(NOLOCK) Where im_scripcd = ss_cd) '
	    IF @strSplFilter <> ''
        BEGIN
	      SET @StrString =   @StrString+' AND '+@strSplFilter
        END	
	  
	  END
	  
	  IF @strScripCode <> ''
	  BEGIN
	    SET @StrString =   @StrString+' AND ss_cd = '''+@strScripCode+''' '
	  END
     -- SELECT (@StrString)
	  BEGIN TRY
	    INSERT INTO #tbl_HoldingRep(ClientCode, ClientName, BranchCode, Product, ScripCode, ScripName,
	    ISIN, Qty, ClosingPrice, MarketValue, Haircut, NetValue)
	    EXEC(@StrString)
	  END TRY
	  BEGIN CATCH
	     SET @o_vcErrorFlag  = 'E'
         SET @o_vcErrorMessage = ERROR_MESSAGE()
         RETURN 1
	  END CATCH
	 
    END
    SET @dp_Server = ''
    SET @dp_Database = ''
    SET @dp_Owner = ''
	
    SELECT @dp_Server = LTRIM(RTRIM(OP_Server)), @dp_Database = LTRIM(RTRIM(OP_DataBase)),
    @dp_Owner = LTRIM(RTRIM(OP_Owner)) FROM Other_Products(NOLOCK) 
    WHERE OP_Product = 'Estro'
    AND op_Status = 'A'
	
    DELETE FROM @tbl_HoldingDate
	
    IF @dp_Database <> ''
    BEGIN
      SET @StrString = 'select CONVERT(VARCHAR,max(hld_hold_date),112) from '+@dp_Database+'.[dbo].Holding '
      INSERT INTO @tbl_HoldingDate
      EXEC(@StrString)
	  
      IF EXISTS(SELECT 1 FROM @tbl_HoldingDate WHERE HoldingDate <= @dtAsOnDate)
      BEGIN
	    SET @StrString = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) INSERT INTO @tbl_UserList(Client_Code) 
               SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''') '
        SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = B.da_actno FROM @tbl_UserList A, Dematact B WHERE A.Client_Code = B.da_clientcd AND da_defaultyn=''Y'' '
        SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = A.Client_code FROM @tbl_UserList A WHERE  ISNULL(DPClientCode,'''') = '''''
        SET @StrString = @StrString + ' SELECT CmCd As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode, DType As Product, '
	    +' ss_cd as ScripCode, ScripName , im_isin As ISIN, hld_ac_pos As Qty, 0, 0, 100, 0 '
        +' FROM (SELECT Client_CODE cmcd, im_scripcd ss_cd, SS_Name As ScripName, ISNULL(cm_poaforpayin,''N'')+''DP'' Dtype, hld_ac_pos As hld_ac_pos, im_isin '
        +' FROM '+@dp_Database+'.[dbo].Holding, Isin(NOLOCK), Securities(NOLOCK), @tbl_UserList, '+@dp_Database+'.DBO.CLIENT_MASTER(NOLOCK) CM1   '
        +' where hld_ac_type = ''22'' '
	    +' and hld_isin_code = im_isin  '
        +' AND ss_cd = im_scripcd '
		+' AND DPClientCode = CM1.CM_CD '
	    +' AND hld_ac_code = DPClientCode) A, client_master(NOLOCK) '
        +' WHERE cm_schedule = (select sp_sysvalue from Sysparameter where sp_parmcd = ''cmschedule'') '
        +' AND CMCD = cm_cd   '
	    IF @strSplFilter <> ''
        BEGIN
	       SET @StrString =   @StrString+' AND '+@strSplFilter
        END	
      END
      ELSE
      BEGIN
	    SET @StrString = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) INSERT INTO @tbl_UserList(Client_Code) 
         SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''') '
	   SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = B.da_actno FROM @tbl_UserList A, Dematact B WHERE A.Client_Code = B.da_clientcd AND da_defaultyn=''Y'' '
       SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = A.Client_code FROM @tbl_UserList A WHERE  ISNULL(DPClientCode,'''') = '''''
       SET @StrString = @StrString +' Select td_ac_code As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode,  ''DP'' Dtype, ss_cd, ss_name AS ScripName, '
	   +' td_isin_code As ISIN,  Qty = ABS(Qty) , 0, 0, 100, 0 '
       +' from( Select td_ac_code, td_isin_code, sum(OpenQty) as Qty '
       +' FROM( SELECT td_ac_code = Client_CODE, td_isin_code, SUM(CASE WHEN td_debit_credit=''C'' THEN -td_qty ELSE td_qty END) as OpenQty  '
       +' FROM '+@dp_Database+'.DBO.Trxdetail(nolock), @tbl_UserList '
	   +' where td_ac_code =  DPClientCode and td_curdate < '''+@dtAsonDate+''' '
       +' AND td_booking_type not in (''13'')  '
       +' GROUP BY Client_CODE, td_isin_code  '
       +' HAVING SUM(CASE WHEN td_debit_credit=''C'' THEN td_qty ELSE -td_qty END) <> 0) x1  '
       +' group by td_ac_code, td_isin_code '
       +' UNION ALL  '
       +' SELECT td_ac_code = Client_CODE, td_isin_code, OpenQty = (Case td_debit_credit  when ''D'' then td_qty else -td_qty end) '
       +' FROM '+@dp_Database+'.DBO.Trxdetail(nolock),  @tbl_UserList where td_ac_code =  DPClientCode  '
       +' and td_curdate = '''+@dtAsonDate+''') x , '+@dp_Database+'.DBO.Security(NOLOCK) SC, Client_master cm, Isin(NOLOCK), Securities(NOLOCK) '
       +' WHERE X.td_isin_code = SC.sc_isincode and x.td_ac_code = cm.cm_cd AND  td_isin_code = im_isin  '
       +' AND ss_cd = im_scripcd  AND im_priority = (Select min(im_priority) from ISIN(NOLOCK) Where im_scripcd = ss_cd) '
	   IF @strSplFilter <> ''
       BEGIN
	      SET @StrString =   @StrString+' AND '+@strSplFilter
       END
     END
	 
	  IF @strScripCode <> ''
	  BEGIN
	    SET @StrString =   @StrString+' AND ss_cd = '''+@strScripCode+''' '
	  END
	 
     BEGIN TRY
	   INSERT INTO #tbl_HoldingRep(ClientCode, ClientName, BranchCode, Product, ScripCode, ScripName,
	   ISIN, Qty, ClosingPrice, MarketValue, Haircut, NetValue)
	   EXEC(@StrString)
     END TRY
     BEGIN CATCH
 	   SET @o_vcErrorFlag  = 'E'
       SET @o_vcErrorMessage = ERROR_MESSAGE()
       RETURN 1
     END CATCH
    end
  END
  
  ELSE IF @strProduct ='DP'
  BEGIN

    DECLARE @TBL_CloseRate TABLE(ISIN VARCHAR(30), CloseRate MONEY)
	    
	
	CREATE TABLE #tbl_HoldingRepDP (ClientCode VARCHAR(50), ClientName VARCHAR(100), BranchCode VARCHAR(50),
    ScripCode VARCHAR(50), ScripName VARCHAR(100), ISIN VARCHAR(20), AccountType VARCHAR(100), Qty MONEY, ClosingPrice MONEY,
    MarketValue MONEY)
	
    SELECT @dp_Server = LTRIM(RTRIM(OP_Server)), @dp_Database = LTRIM(RTRIM(OP_DataBase)),
    @dp_Owner = LTRIM(RTRIM(OP_Owner)) FROM Other_Products(NOLOCK) 
    WHERE OP_Product = 'Cross'
    AND op_Status = 'A'
	
    DELETE FROM @tbl_HoldingDate
	
    IF @dp_Database <> ''
    BEGIN
	  SET @StrString = 'select max(hld_hold_date) from '+@dp_Database+'.[dbo].Holding '
	  INSERT INTO @tbl_HoldingDate
	  EXEC(@StrString)
	
	  IF NOT EXISTS(SELECT 1 FROM @tbl_HoldingDate WHERE HoldingDate <= @dtAsOnDate)
	  BEGIN
	    
		SET @StrString = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) INSERT INTO @tbl_UserList(Client_Code) 
                SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''') '
        SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = B.da_actno FROM @tbl_UserList A, Dematact B WHERE A.Client_Code = B.da_clientcd AND da_defaultyn=''Y'' '
        SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = A.Client_code FROM @tbl_UserList A WHERE  ISNULL(DPClientCode,'''') = '''''
        
	  
	    SET @StrString = @StrString+' SELECT cm_cd As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode, '
	    +' ScripName , td_isin_code As ISIN, bt_description, BalanceQty As Qty, 0, 0 '
        +' FROM (Select td_ac_code, td_isin_code, sc_isinname AS ScripName, bt_description , '
        +' BalanceQty =sum(BalanceQty) '
        +' FROM( '
        +' SELECT td_ac_code, td_isin_code, td_ac_type ,  SUM(CASE WHEN td_debit_credit=''D'' THEN -td_qty ELSE td_qty END) as BalanceQty  '
        +' FROM '+@dp_Database+'.DBO.Trxdetail(nolock)  '
        +' where td_ac_code =  DPClientCode   '
        +' and td_curdate <= '''+@dtAsonDate+''' '
        +' GROUP BY td_ac_code, td_isin_code, td_ac_type  '
        +' HAVING SUM(CASE WHEN td_debit_credit=''D'' THEN td_qty ELSE -td_qty END) <> 0) x1 '
		+' LEFT OUTER JOIN  '+@dp_Database+'.DBO.Beneficiary_type BN ON(td_ac_type = BN.bt_code), '
        +' '+@dp_Database+'.DBO.Security(NOLOCK) '
        +' WHERE td_isin_code = sc_isincode '
        +' GROUP BY td_ac_code, td_isin_code, bt_description , sc_isinname '
        +'  ) A, '+@dp_Database+'.[dbo].client_master(NOLOCK) '
        +' WHERE cm_schedule = (select sp_sysvalue from Sysparameter where sp_parmcd = ''cmschedule'') '
        +' AND td_ac_code = cm_cd  '
	  END	
	  ELSE
	  BEGIN
	    SET @StrString = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) INSERT INTO @tbl_UserList(Client_Code) 
                SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''') '
        SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = B.da_actno FROM @tbl_UserList A, Dematact B WHERE A.Client_Code = B.da_clientcd AND da_defaultyn=''Y'' '
        SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = A.Client_code FROM @tbl_UserList A WHERE  ISNULL(DPClientCode,'''') = '''''
        
	    SET @StrString = @StrString+' SELECT cm_cd As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode, '
	    +' ScripName , td_isin_code As ISIN, bt_description, BalanceQty As Qty, 0, 0 '
        +' FROM (Select td_ac_code, td_isin_code, ScripName , bt_description , BalanceQty =sum(BalanceQty)  '
        +' FROM(  '
        +' SELECT td_ac_code = hld_ac_code, ScripName = sc_isinname, td_isin_code = hld_isin_code, td_ac_type = hld_ac_type ,  SUM(hld_ac_pos) as BalanceQty  '
        +' FROM '+@dp_Database+'.DBO.Holding(nolock), '+@dp_Database+'.DBO.Security(nolock), @tbl_UserList where hld_ac_code = DPClientCode '
		+' AND hld_isin_code = sc_isincode '
        +' GROUP BY hld_ac_code, hld_isin_code, hld_ac_type, sc_isinname  '
        +' HAVING SUM(hld_ac_pos) <> 0) x1 LEFT OUTER JOIN  '+@dp_Database+'.DBO.Beneficiary_type BN ON(td_ac_type = BN.bt_code)  '
        +' GROUP BY td_ac_code, td_isin_code, bt_description, ScripName  ) A, '+@dp_Database+'.[dbo].client_master(NOLOCK) '
        +' WHERE cm_schedule = (select sp_sysvalue from Sysparameter where sp_parmcd = ''cmschedule'') '
        +' AND td_ac_code = cm_cd  '
	  END
     --SELECT @StrString
	  BEGIN TRY
	    INSERT INTO #tbl_HoldingRepDP(ClientCode, ClientName, BranchCode, ScripName,
	    ISIN, AccountType, Qty, ClosingPrice, MarketValue)
	    EXEC(@StrString)
	  END TRY
	  BEGIN CATCH
	   SET @o_vcErrorFlag  = 'E'
       SET @o_vcErrorMessage = ERROR_MESSAGE()
       RETURN 1
	  END CATCH
	  
	  SET @StrString = 'SELECT rm_isin_code, rm_rate   '
      +' FROM '+@dp_Database+'.DBO.Rate_master(NOLOCK) X '
      +' WHERE rm_trx_date = (select max(rm_trx_date) from '+@dp_Database+'.DBO.Rate_master where rm_trx_date  <= '''+@dtAsOnDate+''' ) '
	
	  INSERT INTO @TBL_CloseRate(ISIN, CloseRate)
	  EXEC(@StrString)
	
	  UPDATE A SET A.ClosingPrice = B.CloseRate
	  FROM #tbl_HoldingRepDP A, @TBL_CloseRate b
	  WHERE A.ISIN = B.ISIN
    END
	
	SET @dp_Server = ''
    SET @dp_Database = ''
    SET @dp_Owner = ''
	
    SELECT @dp_Server = LTRIM(RTRIM(OP_Server)), @dp_Database = LTRIM(RTRIM(OP_DataBase)),
    @dp_Owner = LTRIM(RTRIM(OP_Owner)) FROM Other_Products(NOLOCK) 
    WHERE OP_Product = 'Estro'
    AND op_Status = 'A'
	
    DELETE FROM @tbl_HoldingDate
	DECLARE @dtHoldingDate VARCHAR(8)=''
	
    IF @dp_Database <> ''
    BEGIN
      SET @StrString = 'select CONVERT(VARCHAR,max(hld_hold_date),112) from '+@dp_Database+'.[dbo].Holding '
      INSERT INTO @tbl_HoldingDate
      EXEC(@StrString)
	  
	  SELECT @dtHoldingDate = HoldingDate FROM @tbl_HoldingDate
	  
      IF EXISTS(SELECT 1 FROM @tbl_HoldingDate WHERE HoldingDate <= @dtAsOnDate)
      BEGIN
	    
		SET @StrString = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) INSERT INTO @tbl_UserList(Client_Code) 
                SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''') '
        SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = B.da_actno FROM @tbl_UserList A, Dematact B WHERE A.Client_Code = B.da_clientcd AND da_defaultyn=''Y'' '
        SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = A.Client_code FROM @tbl_UserList A WHERE  ISNULL(DPClientCode,'''') = '''''
        
		SET @StrString = @StrString+ ' SELECT Client_CODE = hld_ac_code, cm_name AS ClientName, BranchCode = cm_brboffcode,  '
		+' sscd = hld_isin_code, ScripName = sc_company_name,  AccountType =bt_description,  '
        +' hld_ac_pos As Qty, ClosingPrice = 0, MarketValue = 0 '
        +' FROM  '+@dp_Database+'.DBO.Holding(NOLOCK) TD LEFT OUTER JOIN  '+@dp_Database+'.DBO.Beneficiary_type BN '
		+' ON((CASE WHEN hld_blf = ''L'' THEN CASE hld_ac_type WHEN ''22'' THEN ''17'' WHEN ''21''
						THEN ''17'' WHEN ''29'' THEN ''18'' ELSE hld_ac_type END
		ELSE CASE hld_ac_type WHEN ''25'' THEN ''28'' ELSE hld_ac_type END END) = BN.bt_code)'
		+' ,  '+@dp_Database+'.DBO.Security(nolock), '+@dp_Database+'.DBO.client_master(NOLOCK), @tbl_UserList '
        +' where /* hld_ac_type = ''22'' '
        +' AND */ hld_ac_code = DPClientCode '
        +' and hld_isin_code = sc_isincode '
        +' AND hld_ac_code = CM_CD  AND hld_hold_date ='''+@dtHoldingDate+''' '
	    IF @strSplFilter <> ''
        BEGIN
	       SET @StrString =   @StrString+' AND '+@strSplFilter
        END	
      END
      ELSE
      BEGIN
	    
		SET @StrString = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) INSERT INTO @tbl_UserList(Client_Code) 
                SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''') '
        SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = B.da_actno FROM @tbl_UserList A, Dematact B WHERE A.Client_Code = B.da_clientcd AND da_defaultyn=''Y'' '
        SET @StrString = @StrString +' UPDATE A SET A.DPClientCode = A.Client_code FROM @tbl_UserList A WHERE  ISNULL(DPClientCode,'''') = '''''
        
		SET @StrString = @StrString+' Select td_ac_code As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode,  td_isin_code As ISIN, sc_company_name AS ScripName,  '
         +' bt_description,   Qty = ABS(Qty) , 0, 0 '
         +' from( Select td_ac_code, td_isin_code, td_ac_type,  sum(OpenQty) as Qty  '
         +' FROM( SELECT td_ac_code = td_ac_code, td_isin_code, td_ac_type, SUM(CASE WHEN td_debit_credit=''C'' THEN -td_qty ELSE td_qty END) as OpenQty  '
         +' FROM  '+@dp_Database+'.DBO.Trxdetail(nolock), @tbl_UserList '
         +' where td_ac_code = DPClientCode and CONVERT(VARCHAR,td_curdate,112) < '''+@dtAsonDate+''''
         +' AND td_booking_type not in (''13'')   '
         +' GROUP BY td_ac_code, td_isin_code, td_ac_type   '
         +' HAVING SUM(CASE WHEN td_debit_credit = ''C'' THEN td_qty ELSE -td_qty END) <> 0) x1   '
         +' group by td_ac_code, td_isin_code , td_ac_type '
         +' UNION ALL   '
         +' SELECT td_ac_code , td_isin_code, td_booking_type, OpenQty = (Case td_debit_credit  when ''D'' then td_qty else -td_qty end)  '
         +' FROM  '+@dp_Database+'.DBO.Trxdetail(nolock), @tbl_UserList   where td_ac_code = DPClientCode  and td_booking_type not in (''13'')   '
         +' and td_curdate = '''+@dtAsonDate+''') x LEFT OUTER JOIN  '+@dp_Database+'.DBO.Beneficiary_type BN ON(X.td_ac_type = BN.bt_code), '+@dp_Database+'.DBO.Security(NOLOCK) SC, '+@dp_Database+'.DBO.Client_master cm '
         +' WHERE X.td_isin_code = SC.sc_isincode and x.td_ac_code = cm.cm_cd  '
	   IF @strSplFilter <> ''
       BEGIN
	      SET @StrString =   @StrString+' AND '+@strSplFilter
       END
     END
	-- select @StrString
	 BEGIN TRY
	   INSERT INTO #tbl_HoldingRepDP(ClientCode, ClientName, BranchCode, 
	    ISIN, ScripName, AccountType, Qty, ClosingPrice, MarketValue)
	   EXEC(@StrString)
     END TRY
     BEGIN CATCH
 	   SET @o_vcErrorFlag  = 'E'
       SET @o_vcErrorMessage = ERROR_MESSAGE()
       SELECT ERROR_MESSAGE()
	   RETURN 1
     END CATCH
      
    DELETE FROM @TBL_CloseRate
	 
    SET @StrString = 'SELECT rm_isin_code, rm_rate   '
    +' FROM '+@dp_Database+'.DBO.Rate_master(NOLOCK) X '
    +' WHERE rm_trx_date = (select max(rm_trx_date) from '+@dp_Database+'.DBO.Rate_master where rm_trx_date  <= '''+@dtAsOnDate+''' ) '
	
	 INSERT INTO @TBL_CloseRate(ISIN, CloseRate)
	 EXEC(@StrString)
	
	UPDATE A SET A.ClosingPrice = B.CloseRate
	FROM #tbl_HoldingRepDP A, @TBL_CloseRate b
	WHERE A.ISIN = B.ISIN
	
	
   END   
  END
  IF @strProduct = 'TRADING'
  BEGIN
  If(@strHairCut=0)
    Begin
    UPDATE #tbl_HoldingRep set Haircut = Case When vm_exchange = 'N' Or vm_exchange = 'Z' 
    then vm_applicable_var 
    ELSE vm_margin_rate END  
	FROM VarMargin(NOLOCK) WHERE vm_scripcd = ScripCode and vm_Exchange = 'B'  
    AND vm_dt = (select max(vm_dt) FROM VarMargin(NOLOCK) where vm_scripcd = ScripCode and vm_exchange = 'B'  
		--and vm_dt >= DATEADD(DAY,-180,@dtAsOnDate) 
		and vm_dt  <=  @dtAsOnDate)

    UPDATE #tbl_HoldingRep SET Haircut = CASE WHEN vm_exchange = 'N' Or vm_exchange = 'Z' then vm_applicable_var 
    ELSE vm_margin_rate END  
    FROM VarMargin(NOLOCK) WHERE vm_scripcd = ScripCode and vm_Exchange = 'N'  
    AND vm_dt =(SELECT MAX(vm_dt) from VarMargin(NOLOCK) 
		WHERE vm_scripcd = ScripCode and vm_exchange = 'N'  --and vm_dt >=DATEADD(DAY,-180,@dtAsOnDate) 
		and vm_dt  <=  @dtAsOnDate)
	--AND Haircut = 100
  
   End
  	Else  If(@strHairCut=1)
    Begin
		UPDATE #tbl_HoldingRep set Haircut = a.Haircut
		 From (Select MAX(MPS_Haircut) Haircut,MPS_scripcd from MrgPledge_Securities 
		Where  MPS_Dt = (select max(MPS_Dt) from MrgPledge_Securities Where MPS_Dt <= @dtAsOnDate) Group by MPS_scripcd) a 
	    Where ScripCode = a.MPS_scripcd	
   End
  	
	UPDATE #tbl_HoldingRep set ClosingPrice = mk_closerate 
    FROM Market_rates(NOLOCK) 
    WHERE mk_scripcd = ScripCode and mk_exchange ='B' 
    AND mk_dt = (select max(mk_dt) from Market_rates where mk_exchange = 'B' and mk_scripcd = ScripCode  
    --and mk_dt >=DATEADD(DAY,-180,@dtAsOnDate) 
	and mk_dt  <= @dtAsOnDate )
	
  
    UPDATE #tbl_HoldingRep set ClosingPrice = mk_closerate 
    FROM Market_rates(NOLOCK) 
    WHERE mk_scripcd = ScripCode and mk_exchange ='N' 
    AND mk_dt = (select max(mk_dt) from Market_rates where mk_exchange = 'N' and mk_scripcd = ScripCode  
    --and mk_dt >=DATEADD(DAY,-180,@dtAsOnDate) 
	and mk_dt  <= @dtAsOnDate )
	--AND ISNULL(ClosingPrice,0) = 0 

	
  END
  DECLARE @XMLDATA1 XML
  IF @strOutputType = 'X'
  BEGIN
     IF @strProduct IN('DP')
     BEGIN
	   SET @XMLDATA1 = (SELECT ClientCode, ClientName, ScripName, ISIN,
	   AccountType, Holding = Qty, ClosingPrice, MarketValue = ROUND(Qty* ClosingPrice,2)
       FROM #tbl_HoldingRepdp FOR XML PATH('DPHolding'))
	   SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	   DROP TABLE #tbl_HoldingRepdp
    END
	ELSE IF @strProduct IN('TRADING')
	BEGIN
	  SET @XMLDATA1 = (SELECT ClientCode, ClientName, BranchCode, ScripCode, ScripName, ISIN, sum(Case when Product = 'FOCOLL' then Qty Else 0 End) FOCOLL,
       sum(Case when Product = 'CUSPA' then Qty Else 0 End) Cuspa,
	   sum(Case when Product IN('YDP','NDP','DP') then Qty Else 0 End) DP,
       sum(Case when Product = 'YDP' then Qty Else 0 End) DP_POA,
	   sum(Case when Product = 'NDP' then Qty Else 0 End) DP_NONPOA,
       sum(Case when Product = 'EXP' then Qty Else 0 End) EXP,
       sum(Case when Product = 'UNDEL' then Qty Else 0 End) UNDEL,
       sum(Case when Product = 'MTFBENF' then Qty Else 0 End) MTFBENF,
       sum(Case when Product = 'MTFCOLL' then Qty Else 0 End) MTFCOLL,
       sum(Case when Product = 'POOL' then Qty Else 0 End) POOL,
       sum(Case when Product = 'BEN' then Qty Else 0 End) Ben,
	   SUM(Qty) TotalQty, 
	   ClosingPrice, MarketValue = SUM(ROUND( Qty  * ClosingPrice,2)), 
	   Haircut, NetValue = SUM(round(((Qty* ClosingPrice)*(100- Haircut))/100,2))
      FROM #tbl_HoldingRep
	  GROUP BY ClientCode, ClientName, BranchCode, ScripCode, ScripName, ISIN, ClosingPrice, Haircut
      ORDER BY ClientCode, ScripName FOR XML PATH('DPHolding'))
	  SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	  DROP TABLE #tbl_HoldingRep
	  RETURN 1
	END
  END
  ELSE IF @strOutputType = 'G'
  BEGIN
   IF @strProduct IN('TRADING','BOTH')
   BEGIN
     DECLARE @tbl_invplAvgRate TABLE(ARClient VARCHAR(50), ARScrip VARCHAR(50), BuyRate MONEY)
     
	 /*IF EXISTS(SELECT 1 FROM SYS.TABLES WHERE NAME= 'INVPL_Holding')
	 BEGIN
   	   SET @StrString =  'SELECT IH_Clientcd, IH_Scripcd, BuyRate = CASE WHEN BuyQty >0 THEN BuyValue/BuyQty ELSE 0 END '
       +' FROM( SELECT IH_Clientcd, IH_Scripcd, BuyQty = sum(case when IH_BSFlag = ''B'' THEN IH_Qty ELSE 0 END), '
       +' sum(case when IH_BSFlag = ''B'' THEN IH_Qty*(isnull(IH_BRate,0)+isnull(IH_BChrg,0)+isnull(IH_BStt,0)) ELSE 0 END)  AS BuyValue '
       +' FROM INVPL_Holding(NOLOCK) WHERE IH_BSFlag = ''B'' '
       +' GROUP BY IH_Clientcd, IH_Scripcd) X1 '
       
       BEGIN TRY
	     INSERT INTO @tbl_invplAvgRate(ARClient, ARScrip, BuyRate)
	     EXEC(@StrString)
       END TRY
       BEGIN CATCH
 	     SET @o_vcErrorFlag  = 'E'
         SET @o_vcErrorMessage = ERROR_MESSAGE()
         SELECT ERROR_MESSAGE(), '1'
	     RETURN 1
       END CATCH
	 END  
	 */
	 
	 CREATE TABLE #tbl_holdingmain
	 (ClientCode VARCHAR(50), ClientName VARCHAR(200),
	  BranchCode VARCHAR(50), ScripCode VARCHAR(20),
	  ScripName VARCHAR(100), ISIN VARCHAR(50), FOCOLL MONEY,
	  Cuspa MONEY, DP MONEY, [EXP] MONEY, EXP_WITHOUT_MTF MONEY, EXP_MTF MONEY, UNDEL MONEY, MTFBENF MONEY,
	  MTFCOLL MONEY, [POOL] MONEY,  Ben MONEY, MTFQTY MONEY,
	  TotalQty MONEY,  AvgRate MONEY, ClosingPrice MONEY, MarketValue MONEY, Haircut MONEY,
	  NetValue MONEY, DP_POA MONEY, DP_NONPOA MONEY)
	  
     INSERT INTO #tbl_holdingmain(ClientCode, ClientName, BranchCode, ScripCode, ScripName, ISIN, FOCOLL, Cuspa, DP, DP_POA, 
	 DP_NONPOA, [EXP], UNDEL, MTFBENF, MTFCOLL, [POOL], Ben, TotalQty, ClosingPrice, MarketValue, Haircut, NetValue)
     SELECT ClientCode, ClientName, BranchCode, ScripCode, ScripName, ISIN, sum(Case when Product = 'FOCOLL' then Qty Else 0 End) FOCOLL,
       sum(Case when Product = 'CUSPA' then Qty Else 0 End) Cuspa,
	   sum(Case when Product IN('YDP','NDP','DP') then Qty Else 0 End) DP,
       sum(Case when Product = 'YDP' then Qty Else 0 End) DP_POA,
	   sum(Case when Product = 'NDP' then Qty Else 0 End) DP_NONPOA,
       sum(Case when Product = 'EXP' then Qty Else 0 End) EXP,
       sum(Case when Product = 'UNDEL' then Qty Else 0 End) UNDEL,
       sum(Case when Product = 'MTFBENF' then Qty Else 0 End) MTFBENF,
       sum(Case when Product = 'MTFCOLL' then Qty Else 0 End) MTFCOLL,
       sum(Case when Product = 'POOL' then Qty Else 0 End) POOL,
       sum(Case when Product = 'BEN' then Qty Else 0 End) Ben,
	   SUM(Qty) TotalQty, 
	   ClosingPrice, MarketValue = SUM(ROUND( Qty  * ClosingPrice,2)), 
	   Haircut, NetValue = SUM(round(((Qty* ClosingPrice)*(100- Haircut))/100,2))
      FROM #tbl_HoldingRep
	  GROUP BY ClientCode, ClientName, BranchCode, ScripCode, ScripName, ISIN, ClosingPrice, Haircut
      ORDER BY ClientCode, ScripCode

	 DELETE FROM #tbl_HoldingRep
	 
	 IF EXISTS(SELECT 1 FROM SYS.TABLES WHERE NAME= 'MrgTdgFin_TRX')
	 BEGIN
	   SET @StrString = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) '
       +' INSERT INTO @tbl_UserList(Client_Code) '
       +' SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''') ' 
	   +' SELECT MTtd_clientcd, cm_name AS ClientName, cm_brboffcode As BranchCode,  '
	   +' DType = ''MTFEXPQTY'', MTtd_scripcd, SS_Name As ScripName, im_isin, MFTQty = SUM(MTtd_bqty-MTtd_sqty), '
       +' 0, 0, 100, 0 '
       +' FROM MrgTdgFin_TRX(NOLOCK), Client_master(NOLOCK), Settlements(NOLOCK),  '
	   +' securities (NOLOCK), ISIN(NOLOCK)	   '
       +' WHERE MTtd_companycode = '''+@strCompanyCode+''' '
	   +' AND MTtd_clientcd = CM_CD '
	   +' AND MTtd_Stlmnt = se_stlmnt and ss_cd = MTtd_scripcd '
       +'  AND se_stdt <= '''+@dtAsonDate+''' '
	   +' AND se_payoutdt > '''+@dtAsonDate+''' '
	   +' AND MTtd_TrxFlag = ''N'' '
	   +' AND cm_schedule = (select sp_sysvalue from Sysparameter where sp_parmcd = ''cmschedule'')  '
       +' and ss_cd = im_scripcd '
	   +' AND MTtd_clientcd IN(SELECT client_code from  @tbl_UserList) '
	   +' and im_priority in (select min(im_priority) from isin(NOLOCK) where ss_cd = im_scripcd ) '
	   +' GROUP BY MTtd_clientcd, MTtd_scripcd, SS_Name, cm_name, cm_brboffcode, im_isin '
	   +' HAVING  SUM(MTtd_bqty-MTtd_sqty) > 0 '
	 BEGIN TRY
	    INSERT INTO #tbl_HoldingRep(ClientCode, ClientName, BranchCode, Product, ScripCode, ScripName,
	    ISIN, Qty, ClosingPrice, MarketValue, Haircut, NetValue)
	    EXEC(@StrString)
	  END TRY
	  BEGIN CATCH
	     SET @o_vcErrorFlag  = 'E'
         SET @o_vcErrorMessage = ERROR_MESSAGE()
         RETURN 1
	  END CATCH
	 END  
	 
	 
     CREATE INDEX indx_holdingmain ON #tbl_holdingmain (ClientCode, ScripCode)
	 
	 UPDATE A SET A.[EXP_WITHOUT_MTF] = A.[EXP] - ISNULL(Qty,0),
	 EXP_MTF = ISNULL(Qty,0)
	 FROM #tbl_holdingmain A, #tbl_HoldingRep B
	 WHERE A.ClientCode = B.ClientCode
	 AND A.ScripCode = B.ScripCode
	 AND ISNULL(B.Qty,0) > 0 AND [EXP] > 0

	 UPDATE A SET EXP_WITHOUT_MTF = ISNULL(EXP,0)
	 FROM #tbl_holdingmain A
	 WHERE NOT EXISTS(SELECT 1 FROM #tbl_HoldingRep B WHERE A.ClientCode = B.ClientCode
	 AND A.ScripCode = B.ScripCode
	 AND ISNULL(B.Qty,0) > 0 AND ISNULL(EXP,0) > 0)
	 
	 UPDATE A SET A.AvgRate = B.BuyRate
	 FROM #tbl_holdingmain A, @tbl_invplAvgRate B
	 WHERE A.ClientCode = B.ARClient 
	 AND A.ScripCode = B.ARScrip
	  
	 SELECT ClientCode, ClientName, BranchCode, ScripCode, ScripName, ISIN, FOCOLL, Cuspa, DP, 
	 [EXP], UNDEL, MTFBENF, MTFCOLL, [POOL], Ben, TotalQty = CONVERT(Numeric(20),TotalQty), AvgRate = ISNULL(AvgRate,0), 
	 TotalCost = TotalQty*ISNULL(AvgRate,0), ClosingPrice, MarketValue, 
	 Haircut, NetValue, DP_POA, DP_NONPOA, MTF_EXP = ISNULL(EXP_MTF,0), 
	 MTF_WITHOUTEXP = ISNULL(EXP_WITHOUT_MTF,0)
	 FROM #tbl_holdingmain
	 ORDER BY ScripName, ClientCode, ScripCode
	 
	 DROP TABLE #tbl_holdingmain
   END
   ELSE IF @strProduct IN('DP')
   BEGIN
     SELECT ClientCode, ClientName, ScripName, ISIN,
	 AccountType, Holding = Qty, ClosingPrice, MarketValue = ROUND(Qty* ClosingPrice,2)
     FROM #tbl_HoldingRepdp
	 --GROUP BY ClientCode, ClientName, BranchCode, ScripCode, ScripName, ISIN, ClosingPrice, AccountType
     ORDER BY ScripName, ClientCode, AccountType
	 DROP TABLE #tbl_HoldingRepdp
   END
   SET @o_vcErrorMessage = 'Process Completed'
  END
  DROP TABLE #tbl_HoldingRep
  SET @o_vcErrorFlag  = 'S'
  RETURN 1
END
GO


CREATE PROCEDURE sp_LedgerBalance @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN
  DECLARE @dtAsOnDate VARCHAR(8), @strUserId VARCHAR(50), @strProduct VARCHAR(50), @strOutputType VARCHAR(1)='', @XMLData XML,
  @strSelectTag VARCHAR(1)='', @strSelectUsers VARCHAR(500)='', @strString VARCHAR(MAX)='', @strExchSeg VARCHAR(50), @strSplFilter VARCHAR(MAX)='',
  @strCompanyCode VARCHAR(1)
  IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
  END 

  SET @XMLData = CAST('<root>'+@vcXML+'</root>' AS XML)
  
  BEGIN TRY
  
  SELECT @dtAsOnDate = ISNULL(x.value('(AsOnDate)[1]', 'VARCHAR(8)'),''),
  @strProduct = ISNULL(x.value('(AccountType)[1]', 'VARCHAR(50)'),''),
  @strUserId = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'),''),
  @strExchSeg = ISNULL(x.value('(ExchSeg)[1]', 'VARCHAR(500)'),''),
  @strSelectTag = ISNULL(x.value('(SelectTag)[1]', 'VARCHAR(1)'),''),
  @strSelectUsers = ISNULL(x.value('(SelectUsers)[1]', 'VARCHAR(500)'),''),
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'),''),
  @strSplFilter = ISNULL(x.value('(SplFilter)[1]', 'VARCHAR(MAX)'),''),
  @strCompanyCode = ISNULL(x.value('(CompanyCode)[1]', 'VARCHAR(1)'),'')
  FROM @XMLData.nodes('/root') AS XTbl(x) 
  
  IF ISNULL(@strCompanyCode,'') = ''
  BEGIN
    SET @strCompanyCode = 'A'
  END
  

  DECLARE @strCommexConn VARCHAR(50)=''
  DECLARE @o_tbOutPutTable TABLE(Client_Code VARCHAR(50), LedgerBalance MONEY)
  SELECT @strCommexConn = LTRIM(RTRIM(OP_DataBase)) 
  FROM Other_Products(NOLOCK) WHERE OP_Product = 'Commex' and RTRIM(LTRIM(op_status)) = 'A'
	
  SET @strString = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) INSERT INTO @tbl_UserList(Client_Code) '
                  +' SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''')  '
				  +' SELECT ld_clientcd, SUM(LenderBalance) AS LenderBalance FROM( '
  +' SELECT ld_clientcd,  SUM(ld_amount) LenderBalance '
  +' FROM LEDGER(NOLOCK), client_master(NOLOCK) CM  WHERE ld_clientcd = CM_CD AND ld_clientcd IN(SELECT Client_Code From @tbl_UserList)'
  +' AND SUBSTRING(ld_dpid,1,1) ='''+@strCompanyCode+''' '
  IF @strSplFilter <> ''
  BEGIN
    SET @strString =   @strString+' AND '+@strSplFilter
  END
  IF ISNULL(@strExchSeg,'') <> ''
  BEGIN
    SET @strString =   @strString+' AND ld_dpid IN(SELECT VALUE FROM ReturnTable('''+@strExchSeg+''', '','')) '
  END
  SET @strString =  @strString +' GROUP BY ld_clientcd '
  +' UNION ALL '
  +' SELECT cm_cd, SUM(ld_amount) LenderBalance  '
  +' FROM LEDGER(NOLOCK) ld, client_master(NOLOCK) cm  '
  +' WHERE ld_clientcd = cm.cm_brkggroup '
  +' and cm_cd iN(SELECT Client_Code From @tbl_UserList) AND SUBSTRING(ld_dpid,1,1) ='''+@strCompanyCode+''' '
  IF @strSplFilter <> ''
  BEGIN
    SET @strString =   @strString+' AND '+@strSplFilter
  END
  IF ISNULL(@strExchSeg,'') <> ''
  BEGIN
    SET @strString =   @strString+' AND ld_dpid IN(SELECT VALUE FROM ReturnTable('''+@strExchSeg+''', '','')) '
  END
  SET @strString =  @strString +' and charindex(''EM'','''+@strProduct+''') > 1 '
  +' GROUP BY cm_cd '
 
  IF EXISTS(SELECT 1 FROM SYS.TABLES WHERE NAME= 'MrgTdgFin_Clients')
  BEGIN 
    SET @strString = @strString +' UNION ALL ' 
	+' SELECT MTFC_CMCD AS cm_cd, SUM(ld_amount) LenderBalance  '
    +' FROM Ledger(NOLOCK) ld, MrgTdgFin_Clients(NOLOCK) MTF, Client_MASTER(NOLOCK)  '
    +' WHERE ld_clientcd =  MTFC_FillerB '
	+' AND MTFC_CMCD = CM_CD '
    +' AND MTFC_CMCD iN(SELECT Client_Code From @tbl_UserList) AND SUBSTRING(ld_dpid,1,1) ='''+@strCompanyCode+''''
  IF @strSplFilter <> ''
  BEGIN
    SET @strString =   @strString+' AND '+@strSplFilter
  END
  IF ISNULL(@strExchSeg,'') <> ''
  BEGIN
    SET @strString =   @strString+' AND ld_dpid IN(SELECT VALUE FROM ReturnTable('''+@strExchSeg+''', '','')) '
  END
  SET @strString =  @strString +' AND charindex(''MTF'','''+@strProduct+''') > 1 '
    +' GROUP BY MTFC_CMCD '

  END
  IF  @strCommexConn <> ''
  BEGIN
    SET @strString = @strString     +' UNION ALL '
	+ ' SELECT cm.cm_cd, SUM(ld_amount) LenderBalance '
    +' FROM '+@strCommexConn+'.dbo.Ledger(NOLOCK) ld, '+@strCommexConn+'.dbo.client_master cm  '
    +' WHERE ld.ld_clientcd = cm.cm_cd '
    +' and ld_clientcd iN(SELECT Client_Code From @tbl_UserList) AND SUBSTRING(ld_dpid,1,1) ='''+@strCompanyCode+''''
	IF @strSplFilter <> ''
    BEGIN
      SET @strString =   @strString+' AND '+@strSplFilter
    END
	IF ISNULL(@strExchSeg,'') <> ''
    BEGIN
      SET @strString =   @strString+' AND ld_dpid IN(SELECT VALUE FROM ReturnTable('''+@strExchSeg+''', '','')) '
    END
    SET @strString =  @strString +' and charindex(''CX'','''+@strProduct+''')>1 '
    +' group by cm.cm_cd '
    +' UNION ALL '
    +' SELECT cm_cd, SUM(ld_amount) LenderBalance  '
    +' FROM '+@strCommexConn+'.dbo.Ledger(NOLOCK) ld, '+@strCommexConn+'.dbo.client_master cm   '
    +' WHERE ld_clientcd = cm.cm_brkggroup '
    +' and cm_cd iN(SELECT Client_Code From @tbl_UserList) AND SUBSTRING(ld_dpid,1,1) ='''+@strCompanyCode+''''
	IF @strSplFilter <> ''
    BEGIN
      SET @strString =   @strString+' AND '+@strSplFilter
    END
	
	IF ISNULL(@strExchSeg,'') <> ''
    BEGIN
      SET @strString =   @strString+' AND ld_dpid IN(SELECT VALUE FROM ReturnTable('''+@strExchSeg+''', '','')) '
    END
    SET @strString =  @strString +' and charindex(''CM'','''+@strProduct+''') > 1 '
    +' GROUP BY cm_cd'
  END	
  SET @strString = @strString+' ) X1 '
    +' GROUP BY ld_clientcd '
 BEGIN TRY
   INSERT INTO @o_tbOutPutTable(Client_Code, LedgerBalance)
   EXEC(@strString)
 END TRY
 BEGIN CATCH
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = ERROR_MESSAGE()
 END CATCH
 SET @o_vcErrorMessage = 'Process Completed'
 IF @strOutputType = 'X'
  BEGIN
    DECLARE @XMLDATA1 XML
    SET @XMLDATA1 = (SELECT * FROM @o_tbOutPutTable FOR XML PATH('LedgerBalance'))
	SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
  END
  ELSE IF @strOutputType = 'G'
  BEGIN
    SELECT Client_Code, LedgerBalance = CAST(ABS(LedgerBalance) AS VARCHAR)+CASE WHEN LedgerBalance >= 0 THEN ' Dr' ELSE  ' Cr' END
	FROM @o_tbOutPutTable
  END
  END TRY
  BEGIN CATCH
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = ERROR_MESSAGE()
    RETURN 1
  END CATCH
  SET @o_vcErrorFlag  = 'S'

  RETURN 1
END
GO

CREATE PROCEDURE stpr_Rpt_OSPositionNew @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT 
WITH ENCRYPTION
AS
BEGIN
 /*
 ///////////////////////////////////////////////////////////////////////////////////////////
 // Create By     : VAIBHAV GARG
 // Created Date  : 23-NOV-2023
 // Description   : 
 // Reviewed By   : 
 // Review Date   : 
 //////////////////////////////////////////////////////////////////////////////////////////
*/
  DECLARE @dtAsOnDate VARCHAR(8), @strUserId VARCHAR(50), @strProduct VARCHAR(50), @strOutputType VARCHAR(1)='', @XMLData XML,
  @strSelectTag VARCHAR(1)='', @strSelectUsers VARCHAR(500)='', @strString VARCHAR(MAX)='', @ExchSeg VARCHAR(100)='', @strStringMin VARCHAR(MAX)='',
  @strSplFilter VARCHAR(MAX)='', @strCompanyCode VARCHAR(1)='A'
  IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
  END 

  SET @XMLData = CAST('<root>'+@vcXML+'</root>' AS XML)

  SELECT @dtAsOnDate = ISNULL(x.value('(AsOnDate)[1]', 'VARCHAR(8)'),''),
  @strUserId = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'),''),
  @ExchSeg = ISNULL(x.value('(ExchSeg)[1]', 'VARCHAR(500)'),''),
  @strSelectTag = ISNULL(x.value('(SelectTag)[1]', 'VARCHAR(1)'),''),
  @strSelectUsers = ISNULL(x.value('(SelectUsers)[1]', 'VARCHAR(500)'),''),
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'),''),
  @strSplFilter = ISNULL(x.value('(SplFilter)[1]', 'VARCHAR(MAX)'),''),
  @strCompanyCode = ISNULL(x.value('(CompanyCode)[1]', 'VARCHAR(1)'),'')
  FROM @XMLData.nodes('/root') AS XTbl(x) 

   
  IF ISNULL(@strCompanyCode,'') = ''
  BEGIN
    SET @strCompanyCode = 'A'
  END
  
  
  
  SET @strStringMin  = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50)) '
  +' INSERT INTO @tbl_UserList(Client_Code) '
  +' SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''') '
  
  CREATE TABLE #tbl_Positions (td_clientcd VARCHAR(50),
  td_companycode VARCHAR(1), td_exchange VARCHAR(2), td_Segment VARCHAR(1), td_dt VARCHAR(8),
  td_seriesid [numeric](18, 0), sm_seriesid [numeric](18, 0), sm_symbol VARCHAR(10),
  sm_sname VARCHAR(30), sm_expirydt VARCHAR(8), buy [numeric](18, 0), sale [numeric](18, 0), net [numeric](18, 0), 
  sm_multiplier MONEY, td_rate MONEY, CloseRate MONEY, ExpMarginPer MONEY, ExpMargin MONEY ) 
  
  BEGIN TRY
  SET @strString  =  @strStringMin +' SELECT td_clientcd, td_companycode, td_exchange, td_Segment, td_dt, td_seriesid,  sm_seriesid, sm_symbol, '
  +' sm_sname, sm_expirydt, td_bqty buy, td_sqty sale, td_bqty - td_sqty  net ,sm_multiplier, '
  +' td_rate, CloseRate = 0 '
  +' FROM TRADES (NOLOCK), @tbl_UserList X , Series_master(NOLOCK) ##@@CLIENTMASTER@@## '
  +' WHERE td_dt <= '''+@dtAsOnDate +''' '
  +' AND td_clientcd  =  X.Client_Code '
  +' AND td_exchange = sm_exchange and td_segment = sm_segment and td_seriesid = sm_seriesid  '
  +' AND td_companycode = '''+@strCompanyCode+''' '
  +' AND ltrim(rtrim(td_groupid)) <> ''B''  '
  +' AND td_expirydt >= '''+@dtAsOnDate+''' '
  
  IF @strSplFilter <> ''
  BEGIN
    SET @strString =   @strString+' AND td_clientcd  =  CM_CD and cm_schedule = 49843750 '
    SET @strString =   @strString+' AND '+@strSplFilter
	SET @strString = REPLACE(@strString,'##@@CLIENTMASTER@@##',',CLIENT_MASTER(NOLOCK) ')
  END
  ELSE
  BEGIN
   SET @strString = REPLACE(@strString,'##@@CLIENTMASTER@@##',' ')
  END
  IF ISNULL(@ExchSeg,'') <> ''
  BEGIN
    SET @strString =   @strString+'  AND td_companycode+td_exchange+td_segment IN(SELECT VALUE FROM  ReturnTable('''+@ExchSeg+''','',''))'
  END	
  --SELECT @strString
  BEGIN TRY
    INSERT INTO #tbl_Positions( td_clientcd, td_companycode, td_exchange, td_Segment, td_dt, 
    td_seriesid, sm_seriesid, sm_symbol, sm_sname, sm_expirydt, buy, sale, net, sm_multiplier, td_rate, CloseRate)
	EXEC(@strString)
  END TRY
  BEGIN CATCH
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = ERROR_MESSAGE()
	DROP TABLE #tbl_Positions
    RETURN 1
  END CATCH   
  
  

  SET @strString  =  @strStringMin + 'SELECT ex_clientcd, ex_companycode, ex_exchange, EX_Segment, ex_dt, ex_seriesid, sm_seriesid, sm_symbol, sm_sname, sm_expirydt,  '
  +' ex_aqty  buy ,ex_eqty  sale, ex_eqty - ex_aqty  net, sm_multiplier ,abs(ex_diffbrokrate) AS td_rate, CloseRate = 0 '
  +' FROM Exercise(NOLOCK), Series_master(NOLOCK) ##@@CLIENTMASTER@@## '
  +' WHERE ex_exchange = sm_exchange and ex_segment = sm_segment  '
  +' AND ex_seriesid = sm_seriesid  and ex_companycode = ''A'' '
  +' AND ex_clientcd  IN(SELECT Client_Code FROM @tbl_UserList) AND ex_companycode = '''+@strCompanyCode+''' '
  +' AND ex_dt <= '''+@dtAsOnDate+''' and sm_expirydt >= '''+@dtAsOnDate+''' '
  
  IF @strSplFilter <> ''
  BEGIN
    SET @strString =   @strString+' AND ex_clientcd  =  CM_CD  and cm_schedule = 49843750 '
    SET @strString =   @strString+' AND '+@strSplFilter
	SET @strString = REPLACE(@strString,'##@@CLIENTMASTER@@##',',CLIENT_MASTER(NOLOCK) ')
  END
  ELSE
  BEGIN
   SET @strString = REPLACE(@strString,'##@@CLIENTMASTER@@##',' ')
  END
  IF ISNULL(@ExchSeg,'') <> ''
  BEGIN
    SET @strString =   @strString+'  AND ex_companycode+ex_exchange+ex_segment IN(SELECT VALUE FROM  ReturnTable('''+@ExchSeg+''','',''))'
  END	

  BEGIN TRY
    INSERT INTO #tbl_Positions( td_clientcd, td_companycode, td_exchange, td_Segment, td_dt, 
    td_seriesid, sm_seriesid, sm_symbol, sm_sname, sm_expirydt, buy, sale, net, sm_multiplier, td_rate, CloseRate)
	EXEC(@strString)
  END TRY
  BEGIN CATCH
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = ERROR_MESSAGE()
	DROP TABLE #tbl_Positions
    RETURN 1
  END CATCH   
  
  
  
  DECLARE @tbl_closerate TABLE(ms_exchange VARCHAR(1), ms_segment VARCHAR(2), ms_seriesid  [numeric](18, 0), ms_lastprice MONEY)
  
  
  INSERT INTO @tbl_closerate( ms_exchange, ms_segment, ms_seriesid, ms_lastprice)
  SELECT  ms_exchange, ms_segment,  ms_seriesid, ms_lastprice from Market_summary m
  where ms_dt = (select max(ms_dt) from Market_summary(NOLOCK) where ms_exchange = m.ms_exchange and ms_segment = m.ms_segment
  AND ms_seriesid = M.ms_seriesid and ms_dt >= DATEADD(day,-180, @dtAsOnDate) and ms_dt <= @dtAsOnDate)
  AND exists(select 1 from #tbl_Positions where td_seriesid  = m.ms_seriesid)
  
  UPDATE a set a.CloseRate = B.ms_lastprice
  from #tbl_Positions a, @tbl_closerate B
  WHERE A.td_exchange = B.ms_exchange
  AND A.td_seriesid = B.ms_seriesid  
  AND B.ms_segment = A.td_Segment
  
  
  
  IF EXISTS(SELECT 1 from(
  SELECT VALUE as Exchange FROM ReturnTable(@ExchSeg,',')) X1
  WHERE substring(X1.Exchange,2,1) = 'X')
  BEGIN
    DECLARE @SehmentCH_ClgHs VARCHAR(1)=''
	SELECT @SehmentCH_ClgHs = (CASE WHEN CH_ClgHs = 'I' THEN 'B' ELSE CH_ClgHs END) FROM ClearingHouse(NOLOCK)
    WHERE CH_CompanyCode = 'A' AND CH_Segment = 'F' 
	AND CH_EffDt = (SELECT Min(CH_EffDt) FROM ClearingHouse
	WHERE CH_CompanyCode = 'A' AND CH_Segment = 'F' AND CH_EffDt <= @dtAsOnDate)   
  
    DELETE FROM @tbl_closerate
    
	INSERT INTO @tbl_closerate( ms_exchange, ms_segment, ms_seriesid, ms_lastprice)
    SELECT  ms_exchange, ms_segment,  ms_seriesid, ms_lastprice from Market_summary m
    where ms_dt = (select max(ms_dt) from Market_summary(NOLOCK) where ms_exchange = @SehmentCH_ClgHs and ms_segment = m.ms_segment
    AND ms_seriesid = M.ms_seriesid and ms_dt >= DATEADD(day,-180, @dtAsOnDate) and ms_dt <= @dtAsOnDate) 
	AND ms_exchange = @SehmentCH_ClgHs and ms_segment = m.ms_segment
    AND exists(select 1 from #tbl_Positions where td_seriesid  = m.ms_seriesid)
  
    UPDATE a set a.CloseRate = B.ms_lastprice
    from #tbl_Positions a, @tbl_closerate B
    WHERE @SehmentCH_ClgHs = B.ms_exchange
    AND A.td_seriesid = B.ms_seriesid  
    AND B.ms_segment = A.td_Segment
  END
 
  
  ---

  DECLARE @strCommDataBase VARCHAR(50)='', @strCommOwner VARCHAR(50)
  SELECT @strCommOwner = LTRIM(RTRIM(OP_Owner)), @strCommDataBase = LTRIM(RTRIM(OP_DataBase)) 
  FROM Other_Products(NOLOCK) WHERE OP_Product = 'Commex'
  AND OP_Status ='A'
  
  
  IF @strCommDataBase <> ''
  BEGIN
    SET @strString  = ' DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) INSERT INTO @tbl_UserList(Client_Code) 
                  SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''') '
    SET @strString  = @strString + ' SELECT td_clientcd, td_companycode, td_exchange, '''' td_Segment, td_dt, td_seriesid,  sm_seriesid, sm_symbol, '
    +' sm_sname, sm_expirydt, td_bqty buy, td_sqty sale, td_bqty - td_sqty  net ,sm_multiplier, td_rate, CloseRate = 0'
    +' FROM '+@strCommDataBase+'.'+@strCommOwner+'.TRADES (NOLOCK) , '+@strCommDataBase+'.'+@strCommOwner+'.Series_master(NOLOCK) ##@@CLIENTMASTER@@## '
    +' WHERE td_exchange = sm_exchange and td_seriesid = sm_seriesid  '
    +' AND td_companycode = '''+@strCompanyCode+''' '
    +' AND ltrim(rtrim(td_groupid)) <> ''B''  '
    +' AND td_clientcd  IN(SELECT Client_Code FROM @tbl_UserList) '
    +' AND td_dt <= '''+@dtAsOnDate+''' and sm_expirydt >= '''+@dtAsOnDate+''' '
	IF @strSplFilter <> ''
    BEGIN
      SET @strString =   @strString+' AND td_clientcd  =  CM_CD and cm_schedule = 49843750 '
      SET @strString =   @strString+' AND '+@strSplFilter
	  SET @strString = REPLACE(@strString,'##@@CLIENTMASTER@@##',','+@strCommDataBase+'.'+@strCommOwner+'.CLIENT_MASTER(NOLOCK) ')
    END
    ELSE
    BEGIN
     SET @strString = REPLACE(@strString,'##@@CLIENTMASTER@@##',' ')
    END
	
	IF @ExchSeg <> ''
	BEGIN
	  SET @strString  = @strString + ' AND td_companycode+td_exchange IN(SELECT SUBSTRING(VALUE,1,2) FROM  ReturnTable('''+@ExchSeg+''','',''))'
	END
	
    BEGIN TRY
      INSERT INTO #tbl_Positions(td_clientcd, td_companycode, td_exchange, td_Segment,  td_dt, td_seriesid, sm_seriesid, sm_symbol,
      sm_sname, sm_expirydt, buy, sale, net, sm_multiplier, td_rate, CloseRate)
	  EXEC(@strString)
    END TRY
    BEGIN CATCH
      SET @o_vcErrorFlag  = 'E'
      SET @o_vcErrorMessage = ERROR_MESSAGE()
      RETURN 1
    END CATCH
  
    SET @strString  = ' DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) INSERT INTO @tbl_UserList(Client_Code) 
                  SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''') '
    SET @strString  = @strString + 'INSERT INTO #tbl_Positions(td_clientcd, td_companycode, td_exchange, td_Segment,  td_dt, td_seriesid, sm_seriesid, sm_symbol, '
    +' sm_sname, sm_expirydt, buy, sale, net, sm_multiplier, td_rate, CloseRate) '
    +' SELECT ex_clientcd, ex_companycode, ex_exchange, '''' AS EX_Segment, ex_dt, ex_seriesid, sm_seriesid, sm_symbol, sm_sname, sm_expirydt,  '
    +' ex_aqty  buy ,ex_eqty  sale, ex_eqty - ex_aqty  net, sm_multiplier ,abs(ex_diffbrokrate) AS td_rate, CloseRate = 0 '
    +' FROM '+@strCommDataBase+'.'+@strCommOwner+'.Exercise(NOLOCK), '+@strCommDataBase+'.'+@strCommOwner+'.Series_master(NOLOCK) ##@@CLIENTMASTER@@## '
    +' WHERE ex_exchange = sm_exchange '
    +' AND ex_seriesid = sm_seriesid  and ex_companycode = '''+@strCompanyCode+''' '
    +' AND ex_clientcd  IN(SELECT Client_Code FROM @tbl_UserList) '
    +' AND EX_dt <= '''+@dtAsOnDate+''' and sm_expirydt >= '''+@dtAsOnDate+''' '
    IF @ExchSeg <> ''
	BEGIN
	  SET @strString  = @strString + ' AND ex_companycode+ex_exchange IN(SELECT SUBSTRING(VALUE,1,2) FROM  ReturnTable('''+@ExchSeg+''','',''))'
	END
	
	IF @strSplFilter <> ''
    BEGIN
      SET @strString =   @strString+' AND ex_clientcd  =  CM_CD and cm_schedule = 49843750 '
      SET @strString =   @strString+' AND '+@strSplFilter
	  SET @strString = REPLACE(@strString,'##@@CLIENTMASTER@@##',','+@strCommDataBase+'.'+@strCommOwner+'.CLIENT_MASTER(NOLOCK) ')
    END
    ELSE
    BEGIN
     SET @strString = REPLACE(@strString,'##@@CLIENTMASTER@@##',' ')
    END
	
    BEGIN TRY
      INSERT INTO #tbl_Positions(td_clientcd, td_companycode, td_exchange, td_Segment,  td_dt, td_seriesid, sm_seriesid, sm_symbol,
      sm_sname, sm_expirydt, buy, sale, net, sm_multiplier, td_rate, CloseRate)
	  EXEC(@strString)
    END TRY
    BEGIN CATCH
      SET @o_vcErrorFlag  = 'E'
      SET @o_vcErrorMessage = ERROR_MESSAGE()
      RETURN 1
    END CATCH
  
    IF EXISTS(SELECT 1 FROM #tbl_Positions WHERE td_Segment = '')
	BEGIN
	  SET @strString  = ' SELECT  ms_exchange,  ms_seriesid, ms_lastprice from '+@strCommDataBase+'.'+@strCommOwner+'.Market_summary(NOLOCK) m '
      +' where ms_dt = (select max(ms_dt) from '+@strCommDataBase+'.'+@strCommOwner+'.Market_summary(NOLOCK) '
	  +' where ms_exchange = m.ms_exchange '
      +' AND ms_seriesid = M.ms_seriesid and ms_dt >= DATEADD(day,-180, '''+@dtAsOnDate+''') and  ms_dt <= '''+@dtAsOnDate+''' ) '
  
      DELETE FROM @tbl_closerate 
	  INSERT INTO @tbl_closerate(ms_exchange, ms_seriesid, ms_lastprice)
	  EXEC(@strString)
	END  
	
    UPDATE a set a.CloseRate = B.ms_lastprice 
    from #tbl_Positions a, @tbl_closerate B
    WHERE A.td_exchange =  B.ms_exchange
    AND A.td_seriesid  = B.ms_seriesid
	AND CloseRate = 0
  END
 
  declare @tbl_Segment TABLE(Segmentdpid VARCHAR(20),  CES_Exchange varchar(50), CES_Segment VARCHAR(20))
  declare @StrSegment VARCHAR(MAX)=''
  
  SET @StrSegment  = 'select CES_Cd, LTRIM(RTRIM(CES_Exchange)), CES_Segment as SegmentExchange '
  +' from CompanyExchangeSegments(NOLOCK) WHERE CES_CompanyCd ='''+@strCompanyCode+''' '
  +' UNION ALL '
  +' select CES_Cd, LTRIM(RTRIM(CES_Exchange)), CES_Segment as SegmentExchange '
  +' from '+@strCommDataBase+'.'+@strCommOwner+'.CompanyExchangeSegments(NOLOCK) WHERE CES_CompanyCd ='''+@strCompanyCode+''' '
 
  INSERT INTO @tbl_Segment(Segmentdpid, CES_Exchange, CES_Segment)
  exec(@StrSegment)
  
  UPDATE A SET A.ExpMarginPer = B.pm_exposuremargin
  FROM #tbl_Positions A , (SELECT distinct sm_seriesid, pm_exposuremargin
  from Product_master PR(NOLOCK) , Series_master SR(NOLOCK)
  WHERE  PR.pm_assetcd = SR.sm_symbol AND pm_cd = SR.sm_productcd and pm_Segment = sm_Segment
  AND pm_type = sm_prodtype and sm_exchange = pm_exchange) b
  where a.td_seriesid = b.sm_seriesid

 DECLARE @XMLDATA1 VARCHAR(MAX)=''
  IF @strOutputType = 'X'
  BEGIN
  	  SET @XMLDATA1 = (
    SELECT td_clientcd As ClientCode, ClientName = cm_name, 
	Exchange = (case when td_exchange = 'N'  AND ISNULL(td_Segment,'') <> ''  
	THEN 'NSE' when td_exchange = 'B' THEN 'BSE' 
    when td_exchange = 'M' THEN 'MCX' ELSE 'NCDEX' END), 
	Segment = (CASE WHEN td_Segment = 'F' THEN 'F&O' WHEN td_Segment = 'K' THEN 'Curr' else 'Comm' END),
	--Exchange = CES_Exchange, Segment = CES_Segment,
	Symbol = sm_symbol, SymbolDesc = sm_sname, ExpiryDdate = sm_expirydt, 
    Multiplier = sm_multiplier, 
    SUM(buy) as Buy, 
    ROUND(CASE SUM(buy) when 0 then 0 else abs(sum(buy*td_rate)/sum(buy))end,2)  BuyAvgRate,  
    SUM(buy*td_rate*isnull(sm_multiplier,1.00)) BuyValue,
    sum(sale)  as Sale, 
    ROUND(CASE SUM(sale) when 0 then 0 else abs(sum(sale*td_rate)/sum(sale)) end,2)  SaleAvgRate, 
    SUM(sale*td_rate*isnull(sm_multiplier,1.00)) SaleValue,
    sum(buy-sale) As Net,  
    ROUND(CASE SUM(buy - sale) when 0 then 0 else sum((buy -sale)*td_rate)/sum(buy-sale) end,2)  as AvgRate, 
    --round(sum(buy-sale)*ROUND(CASE SUM(buy - sale) when 0 then 0 else sum((buy -sale)*td_rate*isnull(sm_multiplier,1.00))/sum(buy-sale) end,2),2) as NetValue,
	round(((sum(buy-sale))* max(CloseRate)*isnull(sm_multiplier,1)),2)  NetValue,
    max(CloseRate) as CloseRate, ProfitLoss =  round((sum(buy-sale)* max(CloseRate)*isnull(sm_multiplier,1)) - 
    (sum(buy-sale)*   ROUND(CASE SUM(buy - sale) when 0 then 0 else sum((buy -sale)*td_rate*isnull(sm_multiplier,1.00))/sum(buy-sale) end,2)),2),
    ExpMarginPer = ISNULL(MAX(ExpMarginPer),0), 
	--ExpMargin  =	ROUND((MAX(ExpMarginPer) * round((sum(buy-sale)* max(CloseRate)*isnull(sm_multiplier,1)) - 
    --(sum(buy-sale)*   ROUND(CASE SUM(buy - sale) when 0 then 0 else sum((buy -sale)*td_rate*isnull(sm_multiplier,1.00))/sum(buy-sale) end,2)),2))/100,2)
	ExpMargin  =	ISNULL(ROUND((MAX(ExpMarginPer) * round((abs(sum(buy-sale))* max(CloseRate) * isnull(sm_multiplier,1)),2))/100,2),0)
    FROM #tbl_Positions X /*LEFT OUTER JOIN @tbl_Segment S 
	ON('A'+X.td_exchange+X.td_Segment = S.Segmentdpid)*/
	,Client_master M (nolock)
    WHERE X.td_clientcd = M.cm_cd
    GROUP BY  td_clientcd, cm_name, td_companycode, td_exchange, sm_symbol, sm_sname, sm_expirydt, sm_multiplier, 
	(CASE WHEN td_Segment = 'F' THEN 'F&O' WHEN td_Segment = 'K' THEN 'Curr' else 'Comm' END), td_Segment--, 
	--CES_Exchange, CES_Segment
    HAVING SUM(buy - sale) <> 0 
    ORDER BY td_clientcd, (case when td_exchange = 'N'  AND ISNULL(td_Segment,'') <> ''  
	THEN 'NSE' when td_exchange = 'B' THEN 'BSE' 
    when td_exchange = 'M' THEN 'MCX' ELSE 'NCDEX' END), (CASE WHEN td_Segment = 'F' THEN 'F&O' WHEN td_Segment = 'K' THEN 'Curr' else 'Comm' END), 
	sm_sname FOR XML PATH('OSPosition'))
	SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
  END
  ELSE IF @strOutputType = 'G'
  BEGIN
    SELECT td_clientcd As ClientCode, ClientName = cm_name, 
	Exchange = (case when td_exchange = 'N'  AND ISNULL(td_Segment,'') <> ''  
	THEN 'NSE' when td_exchange = 'B' THEN 'BSE' 
    when td_exchange = 'M' THEN 'MCX' ELSE 'NCDEX' END), 
	Segment = (CASE WHEN td_Segment = 'F' THEN 'F&O' WHEN td_Segment = 'K' THEN 'Curr' else 'Comm' END),
	--Exchange = CES_Exchange, Segment = CES_Segment,
	Symbol = sm_symbol, SymbolDesc = sm_sname, ExpiryDdate = sm_expirydt, 
    Multiplier = sm_multiplier, 
    SUM(buy) as Buy, 
    ROUND(CASE SUM(buy) when 0 then 0 else abs(sum(buy*td_rate)/sum(buy))end,2)  BuyAvgRate,  
    SUM(buy*td_rate*isnull(sm_multiplier,1.00)) BuyValue,
    sum(sale)  as Sale, 
    ROUND(CASE SUM(sale) when 0 then 0 else abs(sum(sale*td_rate)/sum(sale)) end,2)  SaleAvgRate, 
    SUM(sale*td_rate*isnull(sm_multiplier,1.00)) SaleValue,
    sum(buy-sale) As Net,  
    ROUND(CASE SUM(buy - sale) when 0 then 0 else sum((buy -sale)*td_rate)/sum(buy-sale) end,2)  as AvgRate, 
    --round(sum(buy-sale)*ROUND(CASE SUM(buy - sale) when 0 then 0 else sum((buy -sale)*td_rate*isnull(sm_multiplier,1.00))/sum(buy-sale) end,2),2) as NetValue,
	round(((sum(buy-sale))* max(CloseRate)*isnull(sm_multiplier,1)),2)  NetValue,
    max(CloseRate) as CloseRate, ProfitLoss =  round((sum(buy-sale)* max(CloseRate)*isnull(sm_multiplier,1)) - 
    (sum(buy-sale)*   ROUND(CASE SUM(buy - sale) when 0 then 0 else sum((buy -sale)*td_rate*isnull(sm_multiplier,1.00))/sum(buy-sale) end,2)),2),
    ExpMarginPer = ISNULL(MAX(ExpMarginPer),0), 
	--ExpMargin  =	ROUND((MAX(ExpMarginPer) * round((sum(buy-sale)* max(CloseRate)*isnull(sm_multiplier,1)) - 
    --(sum(buy-sale)*   ROUND(CASE SUM(buy - sale) when 0 then 0 else sum((buy -sale)*td_rate*isnull(sm_multiplier,1.00))/sum(buy-sale) end,2)),2))/100,2)
	ExpMargin  =	ISNULL(ROUND((MAX(ExpMarginPer) * round((abs(sum(buy-sale))* max(CloseRate) * isnull(sm_multiplier,1)),2))/100,2),0)
    FROM #tbl_Positions X /*LEFT OUTER JOIN @tbl_Segment S 
	ON('A'+X.td_exchange+X.td_Segment = S.Segmentdpid)*/
	,Client_master M (nolock)
    WHERE X.td_clientcd = M.cm_cd
    GROUP BY  td_clientcd, cm_name, td_companycode, td_exchange, sm_symbol, sm_sname, sm_expirydt, sm_multiplier, 
	(CASE WHEN td_Segment = 'F' THEN 'F&O' WHEN td_Segment = 'K' THEN 'Curr' else 'Comm' END), td_Segment--, 
	--CES_Exchange, CES_Segment
    HAVING SUM(buy - sale) <> 0 
    ORDER BY td_clientcd, (case when td_exchange = 'N'  AND ISNULL(td_Segment,'') <> ''  
	THEN 'NSE' when td_exchange = 'B' THEN 'BSE' 
    when td_exchange = 'M' THEN 'MCX' ELSE 'NCDEX' END), (CASE WHEN td_Segment = 'F' THEN 'F&O' WHEN td_Segment = 'K' THEN 'Curr' else 'Comm' END), 
	sm_sname

  END
  DROP TABLE #tbl_Positions
  END TRY
  BEGIN CATCH
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = ERROR_MESSAGE()
	DROP TABLE #tbl_Positions
    RETURN 1
  END CATCH
  SET @o_vcErrorFlag  = 'S'
  --SET @o_vcErrorMessage = 'Process Completed'
  RETURN 1
END
GO

CREATE PROCEDURE stpr_APICommanProcedure @dsXml VARCHAR(MAX) WITH ENCRYPTION  
AS
BEGIN
  DECLARE @vcXML VARCHAR(MAX)='', @i_vcReportCode VARCHAR(50),
  @i_vcProjectCode VARCHAR(50)='TradeWebAPI', @i_vcReportCategory VARCHAR(100)='',
  @o_vcErrorFlag VARCHAR(1)='', @o_vcErrorMessage VARCHAR(MAX)='',@i_vcReportOption VARCHAR(100)='',
  @strActionName VARCHAR(50), @strOption VARCHAR(50), @strFromDate VARCHAR(8)='', @strToDate VARCHAR(8)='',
  @strUserid VARCHAR(50)='', @strRequestFrom VARCHAR(1), @strCompanyCode VARCHAR(1), @strLevel VARCHAR(1)='1', @StrBillSettlementNo VARCHAR(20)='', 
  @StrPlScripCode VARCHAR(20)=''
  
  DECLARE @CommanXMLString VARCHAR(MAX) ='<FromDt>##FROMDT##</FromDt><ToDt>##ToDt##</ToDt><AsOnDate>##AsOnDate##</AsOnDate><ExchSeg>##ExchSeg##</ExchSeg><UserId>##UserId##</UserId><Product>##Product##</Product><OutputType>##OutputType##</OutputType>'
  SET @CommanXMLString = @CommanXMLString+'<SplFilter></SplFilter><RepType>##RepType##</RepType><RepSubType>##RepSubType##</RepSubType>'
  SET @CommanXMLString = @CommanXMLString+'<CompanyCode>##CompanyCode##</CompanyCode><SettType>##SettType##</SettType>'
  SET @CommanXMLString = @CommanXMLString+'<SettNo>##SettNo##</SettNo><Option112A>##Option112A##</Option112A>'
  SET @CommanXMLString = @CommanXMLString+'<OptionBF>##OptionBF##</OptionBF><ClientCode></ClientCode><ScripCode>##ScripCode##</ScripCode>'
  SET @CommanXMLString = @CommanXMLString+'<DetailReportCode></DetailReportCode><DetailReportCategroy></DetailReportCategroy>'
  SET @CommanXMLString = @CommanXMLString+'<FIFOTag></FIFOTag><RequestFrom>##RequestFrom##</RequestFrom>'
    
  IF @dsXml <> ''
  BEGIN
   SET @vcXML = @dsXml
   DECLARE @strNewVal INT = 0
   IF CHARINDEX('<J_Ui>',@vcXML) > 1
   BEGIN
     SET @strNewVal = 1
   END
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
	 
	 SELECT @strActionName = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'ActionName'
	 SELECT @strOption = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Option'
	 SELECT @strFromDate = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'FromDate'
	 SELECT @strToDate = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'ToDate'
	 SELECT TOP 1 @strUserid = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Userid'
	 SELECT @strRequestFrom = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'RequestFrom'
	 SELECT @strLevel = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Level'
	 SELECT @StrBillSettlementNo = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'SettlementNo'
	 SELECT @StrPlScripCode = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Scrip'
	 

	 
	 IF ISNULL(@strFromDate,'') =''
	 BEGIN
	   SET @strFromDate = ''
	 END
	 
	 IF ISNULL(@strToDate,'') =''
	 BEGIN
	   SET @strToDate = ''
	 END
	 
	 IF isnull(@strFromDate,'') = '' and isnull(@strToDate,'') = ''
	 BEGIN
	   SET @strFromDate = CONVERT(VARCHAR,GETDATE(),112)
	 END
	 IF ISNULL(@strUserid,'') = ''
	 BEGIN
	   SELECT '<Flag>E</Flag><Message>'+ERROR_MESSAGE()+'</Message>'
	   RETURN 1
	 END
	 SELECT @strCompanyCode = em_cd 
	 FROM Entity_Master with (nolock) where em_cd =(select min(em_cd) from Entity_master)
	 
	 SET @CommanXMLString = REPLACE(@CommanXMLString,'##FROMDT##',@strFromDate)
	 
	 
	 
	 IF ISNULL(@strToDate,'') = ''
	 BEGIN
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ToDt##',@strFromDate)
	 END
	 ELSE
	 BEGIN
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ToDt##',@strToDate)
	 END  
	 SET @CommanXMLString = REPLACE(@CommanXMLString,'##AsOnDate##',@strFromDate)
	 SET @CommanXMLString = REPLACE(@CommanXMLString,'##UserId##',@strUserid)
	 SET @CommanXMLString = REPLACE(@CommanXMLString,'##CompanyCode##',@strCompanyCode)
	 SET @CommanXMLString = REPLACE(@CommanXMLString,'##RequestFrom##',@strRequestFrom)

     
	 IF @strActionName = 'TradeWeb' and @strOption = 'BILL'
	 BEGIN
	   DECLARE @strBillSegment VARCHAR(50)=''
	   SELECT @strBillSegment = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Segment'
	  
	   SET @i_vcReportCode = 'BILL'
	   IF @strBillSegment = 'CASH'
	   BEGIN
	     SET @i_vcReportCategory ='Cash_Summary'
       END
	   ELSE IF @strBillSegment = 'DERV'
	   BEGIN
	     SET @i_vcReportCategory ='Derv_Summary'
	   END
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##',@strBillSegment)  
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##SettNo##',@StrBillSettlementNo)  
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Summary') 
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##','') 
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##SettType##','')
       SET @CommanXMLString = REPLACE(@CommanXMLString,'##Option112A##','')
       SET @CommanXMLString = REPLACE(@CommanXMLString,'##OptionBF##','')	
       SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##','')
   
	 END 
     ELSE 
     IF @strActionName = 'TradeWeb' and @strOption = 'PROFITLOSS'
	 BEGIN
	   DECLARE @strPLSegment VARCHAR(50)='DERV', @StrplExchange VARCHAR(20)=''
	   SELECT @strPLSegment = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Segment'
	   SELECT @StrPlExchange = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Exchange'
	   
	   SET @i_vcReportCode = 'InvestorReport'
	   IF @strPLSegment = 'CASH'
	   BEGIN
	     IF @strRequestFrom = 'W'
		 BEGIN
	       SET @i_vcReportCategory ='Cash_ScripWise Summary'
		   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Cash_ScripWise Summary') 
		   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ScripCode##','') 
		 END
         ELSE
		 BEGIN
		   IF @strLevel = '1'
		   BEGIN
		     SET @i_vcReportCategory ='MCash_ScripWise Summary'
			 SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Cash_ScripWise Summary') 
			 SET @CommanXMLString = REPLACE(@CommanXMLString,'##ScripCode##','') 
		   END
           ELSE IF @strLevel = '2'
		   BEGIN
		     SET @i_vcReportCategory ='Cash_ScripWise Summary'
			 SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Cash_ScripWise Summary') 
			 SET @CommanXMLString = REPLACE(@CommanXMLString,'##ScripCode##',@StrPlScripCode) 
		   END
		 END
	     SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##','Market Rate') 
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##OptionBF##','N')	
       END
	   ELSE IF @strPLSegment = 'DERV'
	   BEGIN
	     IF @strRequestFrom = 'W'
		 BEGIN
	       SET @i_vcReportCategory ='DERV_Series Wise'
		   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Series Wise') 
		   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ScripCode##','')
		 END
         ELSE
		 BEGIN
		   IF @strLevel = '1'
		   BEGIN
		     SET @i_vcReportCategory ='MDERV_Series Wise'
		     SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Series Wise') 
			 SET @CommanXMLString = REPLACE(@CommanXMLString,'##ScripCode##','')
		   END	 
		   ELSE IF @strLevel = '2'
		   BEGIN
		     SET @i_vcReportCategory ='DERV_Series Wise'
		     SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Series Wise') 
			 SET @CommanXMLString = REPLACE(@CommanXMLString,'##ScripCode##',@StrPlScripCode) 
		   END
		 END
	     SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##','Do not Valuate') 
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##OptionBF##','Y')	
	   END
	   
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##',@strPLSegment)  
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##SettNo##','')  

	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##SettType##','')
       SET @CommanXMLString = REPLACE(@CommanXMLString,'##Option112A##','')
       
       SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##',@StrPlExchange)
	 END  
	 ELSE 
     IF @strActionName = 'TradeWeb' and @strOption = 'LEDGER'
	 BEGIN
	   DECLARE @strLedgerReportType VARCHAR(50)='', @strIncludeMTF VARCHAR(1)
	   SELECT @strLedgerReportType = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'ReportType'
	   SELECT @strIncludeMTF = (CASE WHEN ParameterValue = 'true' THEN 'Y' 
	   WHEN ParameterValue = 'False' THEN 'N' else ParameterValue END) FROM @tbl_InputJSONTable WHERE ParameterName = 'IncludeMTF'
	   if ISNULL(@strLedgerReportType,'')=''
	   BEGIN
	     SET @strLedgerReportType = 'Trading' 
	   END
	   
	   if ISNULL(@strIncludeMTF,'')=''
	   BEGIN
	     SET @strIncludeMTF = 'Y' 
	   END
	   IF @strRequestFrom = 'W'
	   BEGIN
	     SET @i_vcReportCategory ='Trading_Detail'
	   END
	   ELSE IF @strRequestFrom = 'M'
	   BEGIN
	     SET @i_vcReportCategory ='MTrading_Detail'
	   END
	   
	   SET @i_vcReportCode = 'Ledger'
	    SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##','')
	   IF @strLedgerReportType IN('Trading') 
	   BEGIN
	     IF ISNULL(@strIncludeMTF,'N') = 'Y'
		 BEGIN
	       SET @CommanXMLString = @CommanXMLString+'<AccountType>EM,MTF,CX,CM</AccountType>'
		 END
         ELSE
         BEGIN
		   SET @CommanXMLString = @CommanXMLString+'<AccountType>EM,CX,CM</AccountType>'
         END 		 
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','Trading') 
	   END 
	   ELSE
	   BEGIN
	     SET @CommanXMLString = @CommanXMLString+'<AccountType>EM,MTF,CX,CM</AccountType>'
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','DP') 
	   END 
	 END 
	 IF @strActionName = 'TradeWeb' and @strOption = 'LedgerBalance'
	 BEGIN
	   DECLARE @strLedgerBReportType VARCHAR(50)='', @strIncludeBMTF VARCHAR(1)
	   SELECT @strLedgerBReportType = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'ReportType'
	   SELECT @strIncludeBMTF = (CASE WHEN ParameterValue = 'true' THEN 'Y' 
	   WHEN ParameterValue = 'False' THEN 'N' else ParameterValue END) FROM @tbl_InputJSONTable WHERE ParameterName = 'IncludeMTF'
	   if ISNULL(@strLedgerBReportType,'')=''
	   BEGIN
	     SET @strLedgerBReportType = 'Trading' 
	   END
	   
	   if ISNULL(@strIncludeBMTF,'')=''
	   BEGIN
	     SET @strIncludeBMTF = 'Y' 
	   END
	   IF @strRequestFrom = 'W'
	   BEGIN
	     SET @i_vcReportCategory ='Trading_Detail'
	   END
	   ELSE IF @strRequestFrom = 'M'
	   BEGIN
	     SET @i_vcReportCategory ='MTrading_Detail'
	   END
	   
	   SET @i_vcReportCode = 'LedgerBalance'
	    SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##','')
	   IF @strLedgerReportType IN('Trading') 
	   BEGIN
	     IF ISNULL(@strIncludeBMTF,'N') = 'Y'
		 BEGIN
	       SET @CommanXMLString = @CommanXMLString+'<AccountType>EM,MTF,CX,CM</AccountType>'
		 END
         ELSE
         BEGIN
		   SET @CommanXMLString = @CommanXMLString+'<AccountType>EM,CX,CM</AccountType>'
         END 		 
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','Trading') 
	   END 
	   ELSE
	   BEGIN
	     SET @CommanXMLString = @CommanXMLString+'<AccountType>EM,MTF,CX,CM</AccountType>'
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','DP') 
	   END 
	 END 
	 ELSE
	 IF @strActionName = 'TradeWeb' and @strOption = 'DASHBOARD'
	 BEGIN
	  declare @dsUserid VARCHAR(50)='', @dsReportType VARCHAR(50)='', @dsTag VARCHAR(100)='', @dsDescName VARCHAR(100)=''
	  SELECT @dsUserid = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Userid'
	  SELECT @dsReportType = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'ReportType'
	  SELECT @dsTag = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Tag'
	  SELECT @dsDescName = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'DescName'
	  SET @CommanXMLString = '<UserId>'+@dsUserid+'</UserId><RepType>'+@dsReportType+'</RepType><Tag>'+@dsTag+'</Tag><DescName>'+@dsDescName+'</DescName>'
	  SET @strNewVal = 2
	  SET @i_vcReportCategory ='TRADING'
	  SET @i_vcReportCode = 'DASHBOARD'
	 END 
	 ELSE
	 IF @strActionName = 'TradeWeb' and @strOption = 'HOLDING'
	 BEGIN
	    DECLARE @OSProduct VARCHAR(20)=''
	    SELECT @OSProduct = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Product'
		
		IF ISNULL(@OSProduct,'') = ''
	    BEGIN
	      SET @OSProduct = 'Trading'
		END	 
		ELSE IF ISNULL(@OSProduct,'') = 'DP'
		BEGIN
		  SET @i_vcReportCategory ='DP'
		END
		
	    IF @strRequestFrom = 'W'
	    BEGIN
	      SET @i_vcReportCode = 'CombinedHolding'
		  --SET @i_vcReportCategory ='TRADING'
		END
        ELSE IF @strRequestFrom = 'M'
	    BEGIN
		   IF @strLevel = '1'
		   BEGIN
	         SET @i_vcReportCode = 'CombinedHolding'
		     SET @i_vcReportCategory ='MTRADING'
		   END
           ELSE IF @strLevel = '2'
		   BEGIN
	         SET @i_vcReportCode = 'CombinedHolding'
		     SET @i_vcReportCategory ='TRADING'
			 SET @CommanXMLString = REPLACE(@CommanXMLString,'##ScripCode##',@StrPlScripCode)
		   END
		END		
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##','')
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##',@OSProduct)
       
	 END  	
     ELSE
	 IF @strActionName = 'TradeWeb' and @strOption = 'OSPosition'
	 BEGIN
	   DECLARE @OSSegment VARCHAR(10)='', @ex varchar(100)=''
	   SELECT @OSSegment = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Segment'
	   
	   SELECT @ex =@ex +','+ ces_Cd FROM CompanyExchangeSegments(NOLOCK) 
       WHERE  RIGHT(CES_cD,1) =  (CASE WHEN @OSSegment = 'DERV' THEN 'F'
       WHEN @OSSegment = 'CURR' THEN 'K'
       WHEN @OSSegment = 'COMM' THEN 'X' ELSE '' END)
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##',@ex) 
	   IF @strRequestFrom = 'W'
	   BEGIN
	     SET @i_vcReportCategory ='MOBILE'
	     SET @i_vcReportCode = 'OSPosition'
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','DERV') 
		 SET @CommanXMLString = @CommanXMLString+'<ReportName>OPENPOS</ReportName>'
	   END
       ELSE
	   BEGIN
	     SET @i_vcReportCategory ='MOBILE'
	     SET @i_vcReportCode = 'OSPosition'
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','DERV') 
		 SET @CommanXMLString = @CommanXMLString+'<ReportName>OPENPOS</ReportName>'
	   END  
	   
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##','')
	   
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','Trading') 
	 END 
     ELSE
	 IF @strActionName = 'TradeWeb' and @strOption = 'CGain'
	 BEGIN
	   DECLARE @strOption112A VARCHAR(1)='Y', @strCGReportType VARCHAR(10)=''
	   
	   SET @i_vcReportCode = 'CaptialGain'
	   SELECT @strOption112A = (case when ParameterValue = 'true' then 'Y'  
	   WHEN ParameterValue = 'False' THEN 'N' ELSE ParameterValue END)
	   FROM @tbl_InputJSONTable WHERE ParameterName = 'Option112A'
	   if ISNULL(@strOption112A,'') = ''
	   BEGIN
	     SET @strOption112A = 'Y'
	   END
	   SELECT @strCGReportType = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'ReportType'
	   IF ISNULL(@strCGReportType,'') = ''
	   BEGIN
	     SET @strCGReportType = 'Summary'
	   END
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##','')
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','CASH') 
	   IF @strCGReportType = 'Detail'
	   BEGIN
	     SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Actual PL_Detail') 
	     SET @i_vcReportCategory ='Actual PL_Detail'	 
	   END
	   ELSE
	   IF @strCGReportType = 'Summary'
	   BEGIN
	     IF @strRequestFrom = 'W'
	     BEGIN
	       SET @i_vcReportCategory ='Actual PL_Summary'	 
		   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Actual PL_Summary') 
		 end
		 ELSE
	     IF @strRequestFrom = 'M'
	     BEGIN
		   IF @strLevel = '1'
		   BEGIN
		     SET @i_vcReportCategory ='MActual PL_Summary'
             SET @CommanXMLString = @CommanXMLString+'<POS>N</POS>'		   
			 SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Actual PL_Summary') 
			 SET @CommanXMLString = REPLACE(@CommanXMLString,'##ScripCode##','') 
		   END
		   ELSE IF @strLevel = '2'
		   BEGIN
		     SET @i_vcReportCategory ='Actual PL_Detail'
             SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Actual PL_Detail')	
             SET @CommanXMLString = REPLACE(@CommanXMLString,'##ScripCode##',@StrPlScripCode) 			 
			 SET @CommanXMLString = @CommanXMLString+'<POS>N</POS>'		
		   END
		 END
	   END
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##','') 
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Option112A##',@strOption112A)
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ScripCode##','') 
	 END  
     ELSE
	 IF @strActionName = 'TradeWeb' and @strOption = 'CGainListing'
	 BEGIN
	   SET @CommanXMLString = @CommanXMLString+'<ReportName>CapitalGain</ReportName>'
	   SET @i_vcReportCode = 'CapitalGain'
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##','')
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','') 
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','TradeListing') 
	   IF @strRequestFrom = 'W'
	   BEGIN
	   	SET @i_vcReportCategory ='TradeListing'	 
	   END
	   ELSE 
	   IF @strRequestFrom = 'M'
	   BEGIN
	     SET @i_vcReportCategory ='MTradeListing'
	   END

	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##','ItemWise') 
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Option112A##','')
	 END  
     ELSE
     IF @strActionName = 'TradeWeb' and @strOption = 'CGainHolding'
	 BEGIN
	   DECLARE @strCHOption112A VARCHAR(1)='Y', @strCHReportType VARCHAR(10)=''
	   
	   SET @i_vcReportCode = 'CaptialGain'
	   SELECT @strCHOption112A = (case when ParameterValue = 'true' then 'Y'  
	   WHEN ParameterValue = 'False' THEN 'N' ELSE ParameterValue END) FROM @tbl_InputJSONTable WHERE ParameterName = 'Option112A'
	   SELECT @strCHReportType = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'ReportType'
	   
	   if ISNULL(@strCHOption112A,'') = ''
	   BEGIN
	     SET @strCHOption112A = 'Y'
	   END
	   IF ISNULL(@strCHReportType,'') = ''
	   BEGIN
	     SET @strCHReportType = 'Summary'
	   END
	   
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##','')
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','CASH') 
	   IF @strCHReportType = 'Detail'
	   BEGIN
	     SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Notional_Detail') 
	     SET @i_vcReportCategory ='Notional_Detail'	 
	   END
	   ELSE
	   IF @strCHReportType = 'Summary'
	   BEGIN
	     IF @strRequestFrom = 'W'
	     BEGIN
		   SET @i_vcReportCategory ='Notional_Summary'	 
		   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Notional_Summary')
		 END
	     ELSE IF @strRequestFrom = 'M'
		 BEGIN
		   IF @strLevel = '1'
		   BEGIN
		     SET @i_vcReportCategory ='MNotional_Summary'	 
			 SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Notional_Summary')
		   END
           ELSE IF @strLevel = '2'
		   BEGIN
		     SET @i_vcReportCategory ='Notional_Detail'	 
			 SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Notional_Detail')
			 SET @CommanXMLString = REPLACE(@CommanXMLString,'##ScripCode##',@StrPlScripCode) 	
		   END
		 END
	   END
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##','') 
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Option112A##',@strCHOption112A)
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ScripCode##','') 	
	 END 
	 ELSE
	 IF @strActionName = 'TradeWeb' and @strOption = 'Margin'
	 BEGIN
	   SET @i_vcReportCode = 'Margin'
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##','')
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','CASH') 
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','') 
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##','') 
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Option112A##','')
	   SET @i_vcReportCategory ='ShortFall'	 
	 END 
	 ELSE
	  IF @strActionName = 'TradeWeb' and @strOption = 'Download'
	  BEGIN
	   SET @i_vcReportCode = 'Download'
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##','')
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','') 
	   
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##','') 
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Option112A##','')
	   DECLARE @strDocumentType VARCHAR(50)='', @strDocumentNo VARCHAR(10)='', @strdocReportType VARCHAR(50)=''
	   SELECT @strdocReportType = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'RepType'
	   SELECT @strDocumentType = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'DocumentType'
	   SELECT @strDocumentNo = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'DocumentNo'
	  
	  IF ISNULL(@strDocumentNo,'') <> '' AND ISNULL(@strdocReportType,'') = ''
	   BEGIN
	     SET @strdocReportType = 'DOCUMENT'
	   END
	   ELSE IF ISNULL(@strDocumentNo,'') = '' AND ISNULL(@strdocReportType,'')  = ''
	   BEGIN
	     SET @strdocReportType = 'LIST'
	   END
	  
	   
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##',@strdocReportType) 
	   SET @CommanXMLString = @CommanXMLString+'<ReportName>Download</ReportName><DocumentType>'+@strDocumentType+'</DocumentType><DocumentNo>'+@strDocumentNo+'</DocumentNo>'
	   SET @i_vcReportCategory ='TRADING'	 
	 END 
	 ELSE
	  IF @strActionName = 'TradeWeb' and @strOption = 'IPO'
	  BEGIN
	   SET @i_vcReportCode = 'IPO'
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##','')
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','') 
	   
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##','') 
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Option112A##','')
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','') 
	   SET @CommanXMLString = @CommanXMLString+'<ReportName>IPO_GETDATA</ReportName>'
	   SET @i_vcReportCategory ='GETDATA'	 
	 END 
	 ELSE
     IF @strActionName = 'TradeWeb' and @strOption = 'CGainDividend'
	 BEGIN
	   SET @CommanXMLString = @CommanXMLString+'<ReportName>CapitalGain</ReportName>'
	   SET @i_vcReportCode = 'CapitalGain'
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##','')
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','CASH') 
	   SET @i_vcReportCategory ='Dividend'	 
	   
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Dividend') 	   
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##','ItemWise') 
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Option112A##','')
	 END 
	 IF @strActionName = 'TradeListing' and @strOption = 'FIND'
	 BEGIN
	   DECLARE @strTRDReportType VARCHAR(20)=''
	   SELECT @strTRDReportType = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'RepType'
	   SET @CommanXMLString = @CommanXMLString+'<ReportName>TradeListing</ReportName>'
	   SET @i_vcReportCode = 'TradeListing'
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ExchSeg##','')
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##','') 
	   
	   IF @strTRDReportType = 'SUMMARY'
	   BEGIN
	     SET @i_vcReportCategory ='GET_SUMMARY'	 
	     SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','SUMMARY') 	   
	   END
	   ELSE
	   BEGIN
	     SET @i_vcReportCategory ='GET_DETAIL'	 
	     SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','DETAIL') 	   
	   END
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##','') 
	   SET @CommanXMLString = REPLACE(@CommanXMLString,'##Option112A##','')
	 END 
	 ELSE
	 IF @strActionName = 'TradeWeb' and @strOption = 'Transaction'
	 BEGIN
	   DECLARE @strtrReportType VARCHAR(50)='Trades', @strtrReportView varchar(50)='ItemWise', @strtrSegment VARCHAR(50)='CASH'
	   SELECT @strtrReportType = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'ReportType'
	   SELECT @strtrReportView = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'ReportView'
	   SELECT @strtrSegment = ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Segment'
	   
	   SET @CommanXMLString = @CommanXMLString+'<ReportName>Transaction</ReportName>'
	   SET @i_vcReportCode = 'Transaction'
	   
	   IF @strtrReportType = 'Trades'
	   BEGIN
	     IF @strLevel = '1'
		 BEGIN
	       SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','TradeSummary') 	   
		   SET @i_vcReportCategory ='TradeSummary_'+@strtrReportView	 
		   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##',@strtrReportView) 
		 END
         ELSE IF @strLevel = '2' 		 
		 BEGIN
		   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','TradeSummary') 	   
		   SET @i_vcReportCategory ='TradeSummary_DateWise'	 
		   SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##','DateWise') 
		   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ScripCode##',@StrPlScripCode)
		 END
	   END
	   ELSE
	   IF @strtrReportType = 'Deliveries'
	   BEGIN
	     SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Deliveries') 	   
		 SET @i_vcReportCategory ='Deliveries_'+@strtrReportView	 
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##',@strtrReportView) 
	   END
	   ELSE
	   IF @strtrReportType = 'Receipts'
	   BEGIN
	     SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Receipts') 	   
		 SET @i_vcReportCategory ='Receipts'
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##',@strtrReportView) 
	   END
	   ELSE
	   IF @strtrReportType = 'Payments'
	   BEGIN
	     SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Payments') 	   
		 SET @i_vcReportCategory ='Payments'
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##',@strtrReportView) 
	   END
	   ELSE
	   IF @strtrReportType = 'Journals'
	   BEGIN
	     SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Journals') 	   
		 SET @i_vcReportCategory ='Journals'
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##',@strtrReportView) 
	   END
	   ELSE
	   IF @strtrReportType = 'Bills'
	   BEGIN
	     SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','Bills') 	   
		 SET @i_vcReportCategory ='Bills'
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##',@strtrReportView) 
	   END
	   ELSE
	   IF @strtrReportType = 'AGTS'
	   BEGIN
	     SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepType##','AGTS') 	   
		 SELECT @i_vcReportCategory = 'AGTS_'+(CASE WHEN @strtrSegment = 'FX' THEN 'FO' ELSE @strtrSegment END)
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##RepSubType##',@strtrSegment+'_'+@strtrReportView) 
		 SET @CommanXMLString = REPLACE(@CommanXMLString,'##Product##',@strtrSegment) 
	   END
	 END 
   END
   
   SET @CommanXMLString = REPLACE(@CommanXMLString,'##ScripCode##','')
   
   IF @i_vcReportCode IN('LEDGER','CombinedHolding','OSPosition',
   'InvestorReport','CaptialGain','CapitalGain','Transaction','BILL')
   BEGIN
	 SET @CommanXMLString = REPLACE(@CommanXMLString,'##OutputType##','X')
   END
   ELSE
   BEGIN
	 SET @CommanXMLString = REPLACE(@CommanXMLString,'##OutputType##','G')
   END
 
   SELECT @vcXML = @CommanXMLString
   --SELECT @vcXML
   EXEC stpr_Rpt_GridReports @vcXML, @i_vcProjectCode, @i_vcReportCode, @i_vcReportCategory, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT, @strNewVal
 END
END
GO

CREATE PROCEDURE stpr_Rpt_GridReports @vcXML NVARCHAR(MAX)='', 
@i_vcProjectCode VARCHAR(50), 
@i_vcReportCode VARCHAR(50), @i_vcReportCategory VARCHAR(50), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT,
@strNewVal INT = 0 WITH ENCRYPTION  
AS
BEGIN
 /*
 ///////////////////////////////////////////////////////////////////////////////////////////
 // Create By     : VAIBHAV GARG
 // Created Date  : 12-DEC-2023
 // Description   : 
 // Reviewed By   : 
 // Review Date   : 
 //////////////////////////////////////////////////////////////////////////////////////////
*/
 --- Parameter Declaration

  IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
  END 
  DECLARE @strString NVARCHAR(MAX)='',
    @OutputValue varchar(10)='',
    @outputmessage VARCHAR(MAX)='', @vcProcedureName VARCHAR(50)=''
  --SELECT @i_vcProjectCode, @i_vcReportCode, @i_vcReportCategory
  SELECT TOP 1 @vcProcedureName = ProcedureName 
  FROM tbl_ReportGridViewFormat (NOLOCK) 
  WHERE ProductCode = @i_vcProjectCode AND ReportCode = @i_vcReportCode AND ReportCategroy = @i_vcReportCategory;
  IF @vcProcedureName = ''
  BEGIN
    SELECT TOP 1 @vcProcedureName = ProcedureName 
    FROM tbl_ReportGridViewFormat (NOLOCK) 
    WHERE ProductCode = '' AND ReportCode = @i_vcReportCode AND ReportCategroy = @i_vcReportCategory;
  END
    
  SET @vcXML = REPLACE(@vcXML,'''','''''')


  SET @strString = 'EXEC DBO.' + @vcProcedureName + ' ''' + @vcXML + ''', @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT';
  
  BEGIN TRY
    IF @strNewVal = 0 
	BEGIN
      IF EXISTS(SELECT 1 FROM tbl_ReportGridViewFormat(NOLOCK) 
      WHERE ProductCode = @i_vcProjectCode AND ReportCode = @i_vcReportCode
      AND ReportCategroy = @i_vcReportCategory)
	  BEGIN
        SELECT ColumnType, ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, ColumnFormat, 
		ColumnStyle, DecimalPlace, ColumnTotal, OrderBy--, DetailProductCode, DetailReportCode, DetailReportCategroy, Detailxml
        FROM tbl_ReportGridViewFormat(NOLOCK) 
        WHERE ProductCode = @i_vcProjectCode AND ReportCode = @i_vcReportCode
        AND ReportCategroy = @i_vcReportCategory
        ORDER BY ORDERBY  
	  END
      ELSE	
	  BEGIN
	    SELECT ColumnType, ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, ColumnFormat, 
		ColumnStyle, DecimalPlace, ColumnTotal, OrderBy--, DetailProductCode, DetailReportCode, DetailReportCategroy, Detailxml
        FROM tbl_ReportGridViewFormat(NOLOCK) 
        WHERE ProductCode = '' AND ReportCode = @i_vcReportCode
        AND ReportCategroy = @i_vcReportCategory
        ORDER BY ORDERBY  
	  END
    END
	--SELECT @strString
    EXEC sp_executesql @strString, N'@o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT', @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT;
	DECLARE @XMLData XML, @strUserId VARCHAR(50)='', @strClientName VARCHAR(150)='',
	@strReportHeader VARCHAR(MAX)='', @AsOnDate VARCHAR(12)='', @ToDt VARCHAR(11)='',
	@RequestFrom VARCHAR(1)=''
	SET @XMLData = CAST('<root>'+@vcXML+'</root>' AS XML)
  
    SELECT @strUserId = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'),''),
	@AsOnDate = ISNULL(x.value('(AsOnDate)[1]', 'VARCHAR(12)'),''),
	@ToDt = ISNULL(x.value('(ToDt)[1]', 'VARCHAR(12)'),''),
	@RequestFrom = ISNULL(x.value('(RequestFrom)[1]', 'VARCHAR(1)'),'')
    FROM @XMLData.nodes('/root') AS XTbl(x) 	  
   
	IF @i_vcReportCode IN('LEDGER','CombinedHolding','OSPosition','InvestorReport','CaptialGain',
	'CapitalGain','Transaction','Bill')
	BEGIN
	  DECLARE @strtag VARCHAR(50) = ''
	  
	  IF @i_vcReportCode = 'LEDGER'
	  BEGIN
	    SET @strtag = 'Ledger'
	  END
	  ELSE IF @i_vcReportCode = 'CombinedHolding'
	  BEGIN
	    SET @strtag = 'DPHolding'
	  END
	  ELSE IF @i_vcReportCode = 'OSPosition'
	  BEGIN
	    SET @strtag = 'OSPosition'
	  END
	  ELSE IF @i_vcReportCode = 'InvestorReport'
	  BEGIN
	    SET @strtag = 'ProfitLoss'
	  END
	  ELSE IF @i_vcReportCode IN('CaptialGain','CapitalGain','Transaction')
	  BEGIN
	    SET @strtag = 'CapGain'
	  END
	  ELSE IF @i_vcReportCode IN('BILL')
	  BEGIN
	    SET @strtag = 'Bill'
	  END

	  
	  EXEC stpr_GenerateMobileGrid @o_vcErrorMessage, @strtag, @i_vcReportCode, @i_vcReportCategory, @RequestFrom
	END
    --SELECT @i_vcReportCode, @i_vcReportCategory, @strNewVal
	IF @strNewVal = 1 
	BEGIN
	
	  DECLARE @TotalList VARCHAR(MAX)='', @RightList VARCHAR(MAX)='',@HideList VARCHAR(MAX)='',
	  @DateFormat VARCHAR(50)='DD-MMM-YYYY',@DateFormatList VARCHAR(MAX)='', @Dec2List VARCHAR(MAX)='', @Dec4List VARCHAR(MAX)='',
	  @DrCRColorList VARCHAR(MAX)='', @PnLColorList VARCHAR(MAX)='', @PrimaryKey VARCHAR(MAX)=''
	 
	  SELECT @TotalList = @TotalList+','+
	  CASE WHEN @i_vcReportCode IN('LEDGER','CombinedHolding','OSPosition','InvestorReport',
	  'CaptialGain','CapitalGain','Transaction','BILL') 
	  THEN REPLACE(ColumnHeading,' ','') ELSE  ColumnName END
      FROM tbl_ReportGridViewFormat(NOLOCK) 
      WHERE ReportCode = @i_vcReportCode
      AND ReportCategroy = @i_vcReportCategory
	  and ColumnTotal ='Y' 
	  AND (Productid = @RequestFrom OR Productid = '')
      
	  SELECT @RightList = @RightList+','+ CASE WHEN @i_vcReportCode 
	  IN('LEDGER','CombinedHolding','OSPosition','InvestorReport','CaptialGain','CapitalGain','Transaction','BILL') 
	  THEN  REPLACE(ColumnHeading,' ','') ELSE  ColumnName END
      FROM tbl_ReportGridViewFormat(NOLOCK) 
      WHERE ReportCode = @i_vcReportCode
      AND ReportCategroy = @i_vcReportCategory
	  and ColumnAlignement ='R'
	  AND (Productid = @RequestFrom OR Productid = '')
	  
	  SELECT @DateFormatList = @DateFormatList+','+ CASE WHEN @i_vcReportCode 
	  IN('LEDGER','CombinedHolding','OSPosition','InvestorReport','CaptialGain','CapitalGain','Transaction','BILL') 
	  THEN  REPLACE(ColumnHeading,' ','') ELSE  ColumnName END
      FROM tbl_ReportGridViewFormat(NOLOCK) 
      WHERE ReportCode = @i_vcReportCode
      AND ReportCategroy = @i_vcReportCategory
	  and ColumnFormat <> ''
	  AND (Productid = @RequestFrom OR Productid = '')
	  
	  SELECT @Dec2List = @Dec2List+','+ CASE WHEN @i_vcReportCode 
	  IN('LEDGER','CombinedHolding','OSPosition','InvestorReport','CaptialGain','CapitalGain','Transaction','BILL') 
	  THEN  REPLACE(ColumnHeading,' ','') ELSE  ColumnName END
      FROM tbl_ReportGridViewFormat(NOLOCK) 
      WHERE ReportCode = @i_vcReportCode
      AND ReportCategroy = @i_vcReportCategory
	  and DecimalPlace = 2
	  AND (Productid = @RequestFrom OR Productid = '')
	  
	  SELECT @HIDEList = @HIDEList+','+ CASE WHEN @i_vcReportCode 
	  IN('LEDGER','CombinedHolding','OSPosition','InvestorReport','CaptialGain','CapitalGain','Transaction','BILL') 
	  THEN  REPLACE(ColumnHeading,' ','') ELSE  ColumnName END
      FROM tbl_ReportGridViewFormat(NOLOCK) 
      WHERE ReportCode = @i_vcReportCode
      AND ReportCategroy = @i_vcReportCategory
	  and ColumnVisible = 1
	  AND (Productid = @RequestFrom OR Productid = '')
	  
	  
	  SELECT @DrCRColorList = @DrCRColorList+','+ CASE WHEN @i_vcReportCode 
	  IN('LEDGER','CombinedHolding','OSPosition','InvestorReport','CaptialGain','CapitalGain','Transaction','BILL') 
	  THEN  REPLACE(ColumnHeading,' ','') ELSE  ColumnName END
      FROM tbl_ReportGridViewFormat(NOLOCK) 
      WHERE ReportCode = @i_vcReportCode
      AND ReportCategroy = @i_vcReportCategory
	  and ColumnStyle = 'B'
	  AND (Productid = @RequestFrom OR Productid = '')	  
		  
	  DECLARE @CompanyName VARCHAR(100) = Isnull((select em_name from Entity_Master with (nolock) 
	  where em_cd =(select min(em_cd) from Entity_master)),'')
	  DECLARE @CompanyAdd1 VARCHAR(1000) = '', @CompanyAdd2 VARCHAR(1000) = '', @CompanyAdd3 VARCHAR(1000) = ''

      SELECT @CompanyName = em_name, @CompanyAdd1 = em_add1, @CompanyAdd2 = em_add2, @CompanyAdd3 = em_add3+' '+em_add4 
	  FROM Entity_Master(NOLOCK) 
	  WHERE em_cd = (select min(em_cd) from Entity_master(NOLOCK))
      
	  
	  SELECT @strClientName = um_user_name FROM User_master(NOLOCK) WHERE um_user_id = @strUserId
	  
	  IF ISNULL(@strClientName,'') = ''
	  BEGIN
	    SELECT @strClientName = CM_name FROM CLIENT_master(NOLOCK) WHERE CM_CD = @strUserId  
	  END
	  
	  IF ISNULL(@ToDt,'') = ''
	  BEGIN
	    SET @strReportHeader = @i_vcReportCategory + 'As on Date - ' +CONVERT(VARCHAR(10), CAST(@AsOnDate AS DATE), 103)+ ' \n ' + RTRIM(@strClientName) + ' ( ' + RTRIM(@strUserId) +' )'
	  END
	  BEGIN
	    SET @strReportHeader = @i_vcReportCategory + 'From Date - ' +CONVERT(VARCHAR(10), CAST(@AsOnDate AS DATE), 103)+ ' to ' + CONVERT(VARCHAR(10), CAST(@ToDt AS DATE), 103) + ' \n ' + RTRIM(@strClientName) + ' ( ' + RTRIM(@strUserId) +' )'
	  END

	  SELECT '<XmlData><TotalList>'+@TotalList+'</TotalList>
						<RightList>'+@RightList+'</RightList>
						<HideList>'+@HIDEList+'</HideList>
						<DateFormat>'+@DateFormat+'</DateFormat>
						<DateFormatList>'+@DateFormatList+'</DateFormatList>
						<Dec2List>'+@Dec2List+'</Dec2List>
						<Dec4List></Dec4List>
						<DrCRColorList>'+@DrCRColorList+'</DrCRColorList>
						<PnLColorList></PnLColorList>
						<PrimaryKey></PrimaryKey>
						<CompanyName>' + @CompanyName + '</CompanyName>
						<CompanyAdd1>' + @CompanyAdd1 +'</CompanyAdd1>
						<CompanyAdd2>' + @CompanyAdd2 + '</CompanyAdd2>
						<CompanyAdd3>' + @CompanyAdd3 + '</CompanyAdd3>
						<ReportHeader>'+@strReportHeader+'</ReportHeader>
					    <PDFWidth>520</PDFWidth>
						<PDFHeight>269</PDFHeight>
					</XmlData>' 
      AS Settings
	END
	
	RETURN 1
  END TRY
  BEGIN CATCH   
     SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = ERROR_MESSAGE()
    RETURN 1  
  END CATCH
  RETURN 1
END
GO

CREATE PROCEDURE [dbo].[stpr_GetClientsFromMobile] @dsXml VARCHAR(MAX)  
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
  
  DECLARE @StrRequestFrom VARCHAR(1)='M'
  
  DECLARE @strMobile VARCHAR(12) = '', @strTrading VARCHAR(MAX) = '', @strDemat VARCHAR(MAX) = '', @jsonv VARCHAR(MAX)=''
  SET @strMobile = (SELECT ParameterValue FROM @tbl_InputJSONTable WHERE ParameterName = 'Mobile')

  IF ISNULL(@strMobile,'') = ''
  BEGIN
    SELECT '<Flag>E</Flag><Message>Mobile No cannot be blank</Message>'
	RETURN 1
  END

  DECLARE @TempTable TABLE (ClientCode VARCHAR(20),Product NVARCHAR(10));

  INSERT INTO @TempTable 
  SELECT LTRIM(RTRIM(cm_cd)), 'Trading'  
  FROM Client_master(NOLOCK) WHERE LTRIM(RTRIM(cm_mobile)) = @strMobile

  DECLARE @StrString VARCHAR(MAX), @dp_Server VARCHAR(50)='', @dp_Database VARCHAR(50)='', @dp_Owner  VARCHAR(50)=''

  SELECT TOP 1 @dp_Server = LTRIM(RTRIM(OP_Server)), @dp_Database = LTRIM(RTRIM(OP_DataBase)), @dp_Owner = LTRIM(RTRIM(OP_Owner)) 
  FROM Other_Products(NOLOCK) 
  WHERE op_Status = 'A' AND OP_Product IN ('Cross', 'Estro')

  IF @dp_Database <> ''
  BEGIN
    SET @StrString = 'SELECT LTRIM(RTRIM(cm_cd)), ''Demat'' FROM '+@dp_Database+'.[dbo].Client_master(NOLOCK) WHERE LTRIM(RTRIM(cm_mobile)) = ''' + @strMobile + ''''
	INSERT INTO @TempTable 
    EXEC(@StrString)
  END

  SELECT @strTrading = @strTrading+',"'+LTRIM(RTRIM(ClientCode))+'"'  
  from @TempTable where Product = 'Trading'
  SELECT @strDemat = @strDemat+',"'+LTRIM(RTRIM(ClientCode))+'"'  
  from @TempTable where Product = 'Demat'

  SET @jsonv = '{"Trading": ##Trading##,"Demat": ##Demat##}'
  SET @jsonv = REPLACE(@jsonv,'##Trading##','['+SUBSTRING(@strTrading,2,LEN(@strTrading))+']')
  SET @jsonv = REPLACE(@jsonv,'##Demat##','['+SUBSTRING(@strDemat,2,LEN(@strDemat))+']')
  SELECT @jsonv

  --SELECT '['+SUBSTRING(@strTrading,2,LEN(@strTrading))+']' as 'Trading', SUBSTRING(@strDemat,2,LEN(@strDemat)) as 'Demat'
END




