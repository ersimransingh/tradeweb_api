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


CREATE PROCEDURE stpr_Rpt_LedgerNew @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(500) OUTPUT 
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
  @strCompanyCode VARCHAR(1)='A'
  
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
  @strCompanyCode  = ISNULL(x.value('(CompanyCode)[1]', 'VARCHAR(1)'),'')
  FROM @XMLData.nodes('/root') AS XTbl(x) 
  
  
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
    SELECT (
    SELECT ClientCode, Date = convert(varchar,Date,112), ExchSeg, Voucher, Particular, Debitflag, Chequeno,
	DebitAmount, CreditAmount, Balance, BalanceTag, Documenttype, Common, Ldate , CESCD, LookUp 
    FROM @tbl_LenderReort ORDER BY SERIALNO FOR XML PATH('Ledger'), TYPE ) as 'Data', 
    (select ReportName, Product, ColumnType, ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, DecimalPlace, ColumnTotal, OrderBy FROM tbl_ChatbotPDFConfig(NOLOCK) 
    WHERE ReportName = 'Ledger Report' and Product = @strProduct 
     ORDER BY OrderBy FOR XML PATH('Format'), TYPE)
    as 'PDFFormat' 
    FOR XML PATH('Response');
  END
   IF @strOutputType = 'G'
   BEGIN
    SELECT ClientCode, Date = convert(varchar,cast(Date as date),112), ExchSeg, Voucher, Particular, Debitflag, Chequeno,
	DebitAmount, CreditAmount, Balance, BalanceTag, Documenttype, Common, Ldate, CESCD, LookUp  
	FROM @tbl_LenderReort ORDER BY SerialNo
   END	
  SET @o_vcErrorFlag  = 'S'
  SET @o_vcErrorMessage = 'Process Completed'
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
  @strSelectTag VARCHAR(1)='', @strSelectUsers VARCHAR(500)='', @StrString VARCHAR(MAX)='', @strSplFilter VARCHAR(MAX)='', @strCompanyCode VARCHAR(1)
  IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
  END 
  DECLARE @dp_Server VARCHAR(50)='', @dp_Database VARCHAR(50)='', @dp_Owner  VARCHAR(50)=''
  
  SET @XMLData = CAST('<root>'+@vcXML+'</root>' AS XML)

  SELECT @dtAsOnDate = ISNULL(x.value('(AsOnDate)[1]', 'VARCHAR(8)'),''),
  @strProduct = ISNULL(x.value('(Product)[1]', 'VARCHAR(50)'),''),
  @strUserId = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'),''),
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

    SET @StrString =   @StrString+' GROUP BY MPT_clientcd , MPT_scripcd, SS_Name '
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
        SET @StrString = @StrString + ' SELECT CmCd As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode, DType As Product, '
	    +' SSCD as ScripCode, ScripName , im_isin As ISIN, hld_ac_pos As Qty, 0, 0, 100, 0 '
        +' FROM (SELECT Client_code cmcd,  im_scripcd sscd, SS_Name As ScripName, ''DP'' Dtype, hld_ac_pos As hld_ac_pos, im_isin '
        +' FROM '+@dp_Database+'.[dbo].Holding, Isin(NOLOCK), Securities(NOLOCK), @tbl_UserList   '
        +' where hld_ac_type = ''11'' '
	    +' and hld_isin_code = im_isin  '
        +' AND ss_cd = im_scripcd '
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
        SET @StrString = @StrString +' Select td_ac_code As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode,  ''DP'' Dtype, ss_cd, ss_name AS ScripName, '
	    +' td_isin_code As ISIN,  Qty = ABS(Qty) , 0, 0, 100, 0 '
        +' from( Select td_ac_code, td_isin_code, sum(OpenQty) as Qty '
        +' FROM( SELECT td_ac_code = Client_code, td_isin_code, SUM(CASE WHEN td_debit_credit=''C'' THEN -td_qty ELSE td_qty END) as OpenQty  '
        +' FROM '+@dp_Database+'.DBO.Trxdetail(nolock), @tbl_UserList where td_ac_code =  DPClientCode and td_curdate < '''+@dtAsonDate+''' '
        +' AND td_booking_type not in (''13'')  '
        +' GROUP BY Client_code, td_isin_code  '
        +' HAVING SUM(CASE WHEN td_debit_credit = ''C'' THEN td_qty ELSE -td_qty END) <> 0) x1  '
        +' group by td_ac_code, td_isin_code '
        +' UNION ALL  '
        +' SELECT td_ac_code = Client_code, td_isin_code, OpenQty = (Case td_debit_credit  when ''D'' then td_qty else -td_qty end) '
        +' FROM '+@dp_Database+'.DBO.Trxdetail(nolock), @tbl_UserList where td_ac_code = DPClientCode   '
        +' and td_curdate = '''+@dtAsonDate+'''' 
        +' AND td_booking_type not in (''13'')) x , '+@dp_Database+'.DBO.Security(NOLOCK) SC, Client_master cm, Isin(NOLOCK), Securities(NOLOCK) '
        +' WHERE X.td_isin_code = SC.sc_isincode and x.td_ac_code = cm.cm_cd AND  td_isin_code = im_isin  '
        +' AND ss_cd = im_scripcd  AND im_priority = (Select min(im_priority) from ISIN(NOLOCK) Where im_scripcd = ss_cd) '
	    IF @strSplFilter <> ''
        BEGIN
	      SET @StrString =   @StrString+' AND '+@strSplFilter
        END	
	  
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
	    +' SSCD as ScripCode, ScripName , im_isin As ISIN, hld_ac_pos As Qty, 0, 0, 100, 0 '
        +' FROM (SELECT Client_CODE cmcd, im_scripcd sscd, SS_Name As ScripName, ''DP'' Dtype, hld_ac_pos As hld_ac_pos, im_isin '
        +' FROM '+@dp_Database+'.[dbo].Holding, Isin(NOLOCK), Securities(NOLOCK), @tbl_UserList   '
        +' where hld_ac_type = ''22'' '
	    +' and hld_isin_code = im_isin  '
        +' AND ss_cd = im_scripcd '
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
	    SET @StrString = ' SELECT cm_cd As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode, '
	    +' ScripName , td_isin_code As ISIN, bt_description, BalanceQty As Qty, 0, 0 '
        +' FROM (Select td_ac_code, td_isin_code, sc_isinname AS ScripName, bt_description , '
        +' BalanceQty =sum(BalanceQty) '
        +' FROM( '
        +' SELECT td_ac_code, td_isin_code, td_ac_type ,  SUM(CASE WHEN td_debit_credit=''D'' THEN -td_qty ELSE td_qty END) as BalanceQty  '
        +' FROM '+@dp_Database+'.DBO.Trxdetail(nolock)  '
        +' where td_ac_code =  '''+@strUserId+'''   '
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
	    SET @StrString = ' SELECT cm_cd As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode, '
	    +' ScripName , td_isin_code As ISIN, bt_description, BalanceQty As Qty, 0, 0 '
        +' FROM (Select td_ac_code, td_isin_code, ScripName , bt_description , BalanceQty =sum(BalanceQty)  '
        +' FROM(  '
        +' SELECT td_ac_code = hld_ac_code, ScripName = sc_isinname, td_isin_code = hld_isin_code, td_ac_type = hld_ac_type ,  SUM(hld_ac_pos) as BalanceQty  '
        +' FROM '+@dp_Database+'.DBO.Holding(nolock), '+@dp_Database+'.DBO.Security(nolock) where hld_ac_code = '''+@strUserId+''' '
		+' AND hld_isin_code = sc_isincode '
        +' GROUP BY hld_ac_code, hld_isin_code, hld_ac_type, sc_isinname  '
        +' HAVING SUM(hld_ac_pos) <> 0) x1 LEFT OUTER JOIN  '+@dp_Database+'.DBO.Beneficiary_type BN ON(td_ac_type = BN.bt_code)  '
        +' GROUP BY td_ac_code, td_isin_code, bt_description, ScripName  ) A, '+@dp_Database+'.[dbo].client_master(NOLOCK) '
        +' WHERE cm_schedule = (select sp_sysvalue from Sysparameter where sp_parmcd = ''cmschedule'') '
        +' AND td_ac_code = cm_cd  '
	  END
   
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
	    SET @StrString =  ' SELECT Client_CODE = hld_ac_code, cm_name AS ClientName, BranchCode = cm_brboffcode,  '
		+' sscd = hld_isin_code, ScripName = sc_company_name,  AccountType =bt_description,  '
        +' hld_ac_pos As Qty, ClosingPrice = 0, MarketValue = 0 '
        +' FROM  '+@dp_Database+'.DBO.Holding(NOLOCK) TD LEFT OUTER JOIN  '+@dp_Database+'.DBO.Beneficiary_type BN '
		+' ON((CASE WHEN hld_blf = ''L'' THEN CASE hld_ac_type WHEN ''22'' THEN ''17'' WHEN ''21''
						THEN ''17'' WHEN ''29'' THEN ''18'' ELSE hld_ac_type END
		ELSE CASE hld_ac_type WHEN ''25'' THEN ''28'' ELSE hld_ac_type END END) = BN.bt_code)'
		+' ,  '+@dp_Database+'.DBO.Security(nolock), '+@dp_Database+'.DBO.client_master(NOLOCK) '
        +' where /* hld_ac_type = ''22'' '
        +' AND */ hld_ac_code = '''+@strUserId+''' '
        +' and hld_isin_code = sc_isincode '
        +' AND hld_ac_code = CM_CD  AND hld_hold_date ='''+@dtHoldingDate+''' '
	    IF @strSplFilter <> ''
        BEGIN
	       SET @StrString =   @StrString+' AND '+@strSplFilter
        END	
      END
      ELSE
      BEGIN
	    SET @StrString = 'Select td_ac_code As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode,  td_isin_code As ISIN, sc_company_name AS ScripName,  '
         +' bt_description,   Qty = ABS(Qty) , 0, 0 '
         +' from( Select td_ac_code, td_isin_code, td_ac_type,  sum(OpenQty) as Qty  '
         +' FROM( SELECT td_ac_code = td_ac_code, td_isin_code, td_ac_type, SUM(CASE WHEN td_debit_credit=''C'' THEN -td_qty ELSE td_qty END) as OpenQty  '
         +' FROM  DBO.Trxdetail(nolock) '
         +' where td_ac_code =  '''+@strUserId+''' and CONVERT(VARCHAR,td_curdate,112) < '''+@dtAsonDate+''''
         +' AND td_booking_type not in (''13'')   '
         +' GROUP BY td_ac_code, td_isin_code, td_ac_type   '
         +' HAVING SUM(CASE WHEN td_debit_credit=''C'' THEN td_qty ELSE -td_qty END) <> 0) x1   '
         +' group by td_ac_code, td_isin_code , td_ac_type '
         +' UNION ALL   '
         +' SELECT td_ac_code , td_isin_code, td_booking_type, OpenQty = (Case td_debit_credit  when ''D'' then td_qty else -td_qty end)  '
         +' FROM DBO.Trxdetail(nolock)  where td_ac_code = '''+@strUserId+'''  and td_booking_type not in (''13'')   '
         +' and td_curdate = '''+@dtAsonDate+''') x LEFT OUTER JOIN  DBO.Beneficiary_type BN ON(X.td_ac_type = BN.bt_code), DBO.Security(NOLOCK) SC, Client_master cm '
         +' WHERE X.td_isin_code = SC.sc_isincode and x.td_ac_code = cm.cm_cd  '
	   IF @strSplFilter <> ''
       BEGIN
	      SET @StrString =   @StrString+' AND '+@strSplFilter
       END
     END
	 
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
    UPDATE #tbl_HoldingRep set Haircut = Case When vm_exchange = 'N' Or vm_exchange = 'Z' 
    then vm_applicable_var 
    ELSE vm_margin_rate END  FROM VarMargin(NOLOCK) WHERE vm_scripcd = ScripCode and vm_Exchange = 'B'  
    AND vm_dt = (select max(vm_dt) FROM VarMargin(NOLOCK) where vm_scripcd = ScripCode and vm_exchange = 'B'  
    and vm_dt >= DATEADD(DAY,-180,@dtAsOnDate) and vm_dt  <=  @dtAsOnDate)

    UPDATE #tbl_HoldingRep SET Haircut = Case When vm_exchange = 'N' Or vm_exchange = 'Z' then vm_applicable_var 
    ELSE vm_margin_rate END  
    FROM VarMargin(NOLOCK) WHERE vm_scripcd = ScripCode and vm_Exchange = 'N'  
    AND vm_dt =(select max(vm_dt) from VarMargin(NOLOCK) 
    WHERE vm_scripcd = ScripCode and vm_exchange = 'N'  and vm_dt >=DATEADD(DAY,-180,@dtAsOnDate) and vm_dt  <=  @dtAsOnDate)
	AND Haircut = 100
  
  	
	UPDATE #tbl_HoldingRep set ClosingPrice = mk_closerate 
    FROM Market_rates(NOLOCK) 
    WHERE mk_scripcd = ScripCode and mk_exchange ='B' 
    AND mk_dt = (select max(mk_dt) from Market_rates where mk_exchange = 'B' and mk_scripcd = ScripCode  
    and mk_dt >=DATEADD(DAY,-180,@dtAsOnDate) and mk_dt  <= @dtAsOnDate )
	
  
    UPDATE #tbl_HoldingRep set ClosingPrice = mk_closerate 
    FROM Market_rates(NOLOCK) 
    WHERE mk_scripcd = ScripCode and mk_exchange ='N' 
    AND mk_dt = (select max(mk_dt) from Market_rates where mk_exchange = 'N' and mk_scripcd = ScripCode  
    and mk_dt >=DATEADD(DAY,-180,@dtAsOnDate) and mk_dt  <= @dtAsOnDate )
	AND ISNULL(ClosingPrice,0) = 0 

	
  END
  
   IF @strOutputType = 'X'
  BEGIN
     IF @strProduct IN('DP')
     BEGIN
	   DECLARE @XMLDATA1 XML
       SET @XMLDATA1 = (SELECT ClientCode, ClientName, ScripName, ISIN,
	   AccountType, Holding = Qty, ClosingPrice, MarketValue = ROUND(Qty* ClosingPrice,2)
       FROM #tbl_HoldingRepdp FOR XML PATH('DPHolding'))
	   SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	   DROP TABLE #tbl_HoldingRepdp
    END
  END
  ELSE IF @strOutputType = 'G'
  BEGIN
   IF @strProduct IN('TRADING','BOTH')
   BEGIN
     SELECT ClientCode, ClientName, BranchCode, ScripCode, ScripName, ISIN, sum(Case when Product = 'FOCOLL' then Qty Else 0 End) FOCOLL,
       sum(Case when Product = 'CUSPA' then Qty Else 0 End) Cuspa,
       sum(Case when Product = 'DP' then Qty Else 0 End) DP,
       sum(Case when Product = 'EXP' then Qty Else 0 End) EXP,
       sum(Case when Product = 'UNDEL' then Qty Else 0 End) UNDEL,
       sum(Case when Product = 'MTFBENF' then Qty Else 0 End) MTFBENF,
       sum(Case when Product = 'MTFCOLL' then Qty Else 0 End) MTFCOLL,
       sum(Case when Product = 'POOL' then Qty Else 0 End) POOL,
       sum(Case when Product = 'BEN' then Qty Else 0 End) Ben, SUM(Qty) TotalQty, ClosingPrice, MarketValue = SUM(ROUND(Qty* ClosingPrice,2)), 
	   Haircut, NetValue = SUM(round(((Qty* ClosingPrice)*(100- Haircut))/100,2))
      FROM #tbl_HoldingRep
	  GROUP BY ClientCode, ClientName, BranchCode, ScripCode, ScripName, ISIN, ClosingPrice, Haircut
      ORDER BY ClientCode, ScripName

   END
   ELSE IF @strProduct IN('DP')
   BEGIN
     SELECT ClientCode, ClientName, ScripName, ISIN,
	 AccountType, Holding = Qty, ClosingPrice, MarketValue = ROUND(Qty* ClosingPrice,2)
     FROM #tbl_HoldingRepdp
	 --GROUP BY ClientCode, ClientName, BranchCode, ScripCode, ScripName, ISIN, ClosingPrice, AccountType
     ORDER BY ClientCode, AccountType, ScripName
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

CREATE PROCEDURE stpr_Rpt_OSPositionNew @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(500) OUTPUT 
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
	SELECT @SehmentCH_ClgHs = CH_ClgHs FROM ClearingHouse(NOLOCK)
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

 
  IF @strOutputType = 'X'
  BEGIN
    SELECT(
    SELECT td_clientcd As ClientCode, ClientName = cm_name, 
	Segment = (CASE WHEN td_Segment = 'F' THEN 'F&O' WHEN td_Segment = 'K' THEN 'Curr' else 'Comm' end),
	Exchange = (case when td_exchange = 'N' AND ISNULL(td_Segment,'')< > '' THEN 'NSE' when td_exchange = 'B' THEN 'BSE' 
    when td_exchange = 'M' THEN 'MCX' ELSE 'NCDEX' END), 
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
    round(sum(buy-sale)*ROUND(CASE SUM(buy - sale) when 0 then 0 else sum((buy -sale)*td_rate*isnull(sm_multiplier,1.00))/sum(buy-sale) end,2),2) as NetValue,
    max(CloseRate) as CloseRate, ProfitLoss =  round((sum(buy-sale)* max(CloseRate)*isnull(sm_multiplier,1)) - 
    (sum(buy-sale)*   ROUND(CASE SUM(buy - sale) when 0 then 0 else sum((buy -sale)*td_rate*isnull(sm_multiplier,1.00))/sum(buy-sale) end,2)),2) 
    FROM #tbl_Positions X, Client_master M(nolock)
    WHERE X.td_clientcd = M.cm_cd
    GROUP BY  td_clientcd, cm_name, td_companycode, td_exchange, sm_symbol, sm_sname, sm_expirydt, sm_multiplier, 
	(CASE WHEN td_Segment = 'F' THEN 'F&O' WHEN td_Segment = 'K' THEN 'Curr' else 'Comm' END), td_Segment
    HAVING SUM(buy - sale) <> 0 
    ORDER BY td_clientcd, td_exchange, sm_sname FOR XML PATH('OSPosition'), TYPE ) as 'Data', 
    (select ReportName, Product, ColumnType, ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, DecimalPlace, ColumnTotal, OrderBy FROM tbl_ChatbotPDFConfig(NOLOCK) 
    WHERE ReportName = 'Outstanding Position' and Product = @strProduct 
    ORDER BY OrderBy FOR XML PATH('Format'), TYPE)
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
  SET @o_vcErrorMessage = 'Process Completed'
  RETURN 1
END
GO

CREATE  PROCEDURE [dbo].[GetCMLData]
@vcXML Nvarchar(Max), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN

DECLARE @xmlVal xml
SET @xmlVal = CAST(@vcXML AS XML)

IF @vcXML = ''
BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
END

DECLARE  @ClientCode Varchar(16)=''
DECLARE  @strProduct Varchar(50)=''
SET @ClientCode = Isnull(@xmlVal.value('(UserId)[1]', 'VARCHAR(16)'),'')
SET @strProduct = Isnull(@xmlVal.value('(Product)[1]', 'Varchar(50)'),'')

IF @strProduct='DP'
BEGIN

DECLARE @dp_Server VARCHAR(50)='', @dp_Database VARCHAR(50)='', @dp_Owner  VARCHAR(50)='', @StrString VARCHAR(MAX)=''

SELECT @dp_Server = LTRIM(RTRIM(OP_Server)), @dp_Database = LTRIM(RTRIM(OP_DataBase)),
    @dp_Owner = LTRIM(RTRIM(OP_Owner)) FROM Other_Products(NOLOCK) 
    WHERE OP_Product = 'Cross'
    AND op_Status = 'A'

IF @dp_Database <> ''
BEGIN

SET @StrString = 'select LEFT(cm_cd,8) as DPID, RIGHT(cm_cd,8) as ClientID, CASE cb_sexcode WHEN ''M'' THEN ''Male'' WHEN ''F'' THEN ''Female'' ELSE ''OTHER'' END as Sex, '
+' cm_dpintrefno as IntRefNo, bs_description AS AcStatus, CASE cm_opendate WHEN '''' THEN '''' ELSE CONVERT(VARCHAR, CAST(cm_opendate AS DATE), 103) END AS AcOpenDate, CASE cm_confirmationwaived WHEN ''Y'' THEN ''Yes'' ELSE ''NO'' END AS PurchaseViawer, '
+' BOStatus = ISNULL((select cs_desc from '+@dp_Database+'.DBO.Clientsub_master where cs_module = ''CS09'' and cs_code = cm_productcd),''''), bt_description AS BOSubStatus, bc_description AS AcCategory, case cm_freezeyn when 0 then ''Active'' when 1 then ''Freeze for Debit'' when 2 then ''Freeze for Credit'' when 3 then ''Freeze for All'' else ''Other'' end AS FreezeStatus, '
+' RegForEasi = CASE ISNULL((select de_Easyyn from '+@dp_Database+'.DBO.Dayend where de_cmcd = cm_cd),'''') WHEN ''Y'' THEN ''Yes'' ELSE ''No'' END, CASE cb_nationality WHEN 01 then ''INDIAN'' ELSE ''OTHER'' END AS Nationality, '''' AS StatementCycle, bo_description AS Occupation, '''' AS ClosureIntBy, cm_acc_closuredate AS AcClosureDate, RegForEasiest = CASE ISNULL((select de_Easyyn from '+@dp_Database+'.DBO.Dayend where de_cmcd = cm_cd),'''') WHEN ''Y'' THEN ''Yes'' ELSE ''No'' END, CASE cb_SmartIndicator WHEN ''Y'' THEN ''Yes'' ELSE ''No'' END AS SMSReg, '
+' cm_tele1 AS SMSMobNo, cb_UID1 AS ''UID'', cm_rbirefno AS RBIRefNo, cm_rbiappdate AS RBIApvDate, cm_fax AS PhoneFax, CASE cb_poastate WHEN ''Y'' THEN ''Yes'' ELSE ''No'' END AS BSDAFlag, '
+' CASE cb_poaadd1 WHEN ''Y'' THEN ''Yes'' ELSE ''No'' END AS RGESSFlag, CASE cb_poaadd3 WHEN ''Y'' THEN ''Yes'' ELSE ''No'' END AS PledgeSIFlag, '
+' CASE cb_poauserfield1 WHEN ''Y'' THEN ''Yes'' ELSE ''No'' END AS EmailDLFlag, CASE cb_poaadd2 WHEN 1 THEN ''Physical'' WHEN 2 THEN ''Electronic'' WHEN 3 THEN ''Both'' ELSE '''' END AS AnnualReportFlag, cm_email AS Email, '
+' cm_name As FHolderName, cb_panno AS FHolderPAN, CASE cm_dateofbirth WHEN '''' THEN '''' ELSE CONVERT(VARCHAR, CAST(cm_dateofbirth AS DATE), 103) END AS FHolderDOB, cm_sech_name AS SHolderName, cb_sechpanno AS SHolderPAN, '''' AS SHolderDOB, cm_thih_name AS THolderName, cb_thirdpanno AS THolderPAN, '''' AS THolderDOB, '
+' cm_add1 AS CorrAdd1, cm_add2 AS CorrAdd2, cm_add3 AS CorrAdd3, cm_city + ''/'' + Cm_State + ''/'' + cm_country + ''/'' + cm_pin AS CorrAdd4, '
+' BankName = (select top 1 bk_name from bank_master where  cm_divbankcode = bk_micr), BankAdd1 = (select top 1 bk_add1 from bank_master where  cm_divbankcode = bk_micr), BankAdd2 = (select top 1 bk_add2 from bank_master where  cm_divbankcode = bk_micr), BankAdd3 = (select top 1 bk_add3 from bank_master where  cm_divbankcode = bk_micr), CASE cm_divbranchno WHEN 10 THEN ''Saving Account'' WHEN 11 THEN ''Current Account'' WHEN 13 THEN ''Cash Credit'' ELSE ''Other'' END AS BankAcType, cm_divbankacno AS BankAcNo, cm_divbankcode AS BankMICR, cb_voicemail AS BankIFSC, '
+' cb_fadd1 AS PerAdd1, cb_fadd2 AS PerAdd2, cb_fadd3 AS PerAdd3, cb_fcity + ''/'' + cb_fstate + ''/'' + cb_fcountry + ''/'' + cb_fpin AS PerAdd4,'''' LogoPath '
+' from '+@dp_Database+'.DBO.Client_master, '+@dp_Database+'.DBO.Client_BackOffice,'+@dp_Database+'.DBO.Beneficiary_status,'+@dp_Database+'.DBO.Beneficiary_type,'+@dp_Database+'.DBO.Beneficiary_category,'+@dp_Database+'.DBO.Beneficiary_occupation '
+' where cm_cd = cb_cmcd and cm_active = bs_code and cm_clienttype= bt_code and cm_acctype = bc_code and cm_occupation =bo_code and cm_schedule = 49843750 and cm_cd= ''' + @ClientCode + ''' '
EXEC(@StrString)

DECLARE @NomineeDetails TABLE (
        FNomName NVARCHAR(255),
        FNomPer NVARCHAR(255),
        FNomRel NVARCHAR(255),
        SNomName NVARCHAR(255),
        SNomPer NVARCHAR(255),
        SNomRel NVARCHAR(255),
        TNomName NVARCHAR(255),
        TNomPer NVARCHAR(255),
        TNomRel NVARCHAR(255),
        FGuardName NVARCHAR(255),
        SGuardName NVARCHAR(255),
        TGuardName NVARCHAR(255)
    );

    INSERT INTO @NomineeDetails DEFAULT VALUES;

    DECLARE @RelationDesc NVARCHAR(255);

	CREATE TABLE #NomineeData (
        cn_NomName NVARCHAR(255),
        cn_NomPershare NVARCHAR(255),
        cn_Relation NVARCHAR(255),
        cn_nomsrno INT,
        ordercd NVARCHAR(20)
    );

    SET @StrString ='INSERT INTO #NomineeData SELECT cn_NomName + '' '' + cn_NomMidNm + '' '' + cn_NomlastNm AS cn_NomName, cn_NomPershare, cn_Relation, cn_nomsrno, LTRIM(RTRIM(CONVERT(CHAR, cn_purposeCd))) + ''|'' + LTRIM(RTRIM(CONVERT(CHAR, cn_NomSrno))) AS ordercd '
    +' FROM '+@dp_Database+'.DBO.client_nomineedetails, '+@dp_Database+'.DBO.client_master '
    +' WHERE cn_Cmcd = cm_cd AND cn_cmcd = ''' + @ClientCode + ''' ';
	EXEC(@StrString)

	DECLARE @NomSrno INT, @OrderCd NVARCHAR(10), @NomName NVARCHAR(255), @NomPer NVARCHAR(255);

	DECLARE NomCursor CURSOR FOR 
    SELECT cn_NomSrno, ordercd, cn_NomName, cn_NomPershare, cn_Relation
    FROM #NomineeData;

    OPEN NomCursor;

    FETCH NEXT FROM NomCursor INTO @NomSrno, @OrderCd, @NomName, @NomPer, @RelationDesc;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        --SET @RelationDesc = (SELECT cs_desc FROM Clientsub_master WHERE cs_module = 'CS19' AND cs_code = @RelationDesc);
		SET @RelationDesc = '';

        IF @OrderCd IN ('6|1', '6|2', '6|3')
        BEGIN
            IF @NomSrno = 1
            BEGIN
                UPDATE @NomineeDetails
                SET FNomName = @NomName, FNomPer = @NomPer, FNomRel = @RelationDesc;
            END
            ELSE IF @NomSrno = 2
            BEGIN
                UPDATE @NomineeDetails
                SET SNomName = @NomName, SNomPer = @NomPer, SNomRel = @RelationDesc;
            END
            ELSE IF @NomSrno = 3
            BEGIN
                UPDATE @NomineeDetails
                SET TNomName = @NomName, TNomPer = @NomPer, TNomRel = @RelationDesc;
            END
        END
        ELSE IF @OrderCd IN ('7|0', '7|1', '8|1', '8|2', '8|3')
        BEGIN
            IF @NomSrno = 0
            BEGIN
                UPDATE @NomineeDetails
                SET FNomPer = @NomPer, FNomRel = @RelationDesc, FGuardName = @NomName;
            END
            ELSE IF @NomSrno = 1
            BEGIN
                UPDATE @NomineeDetails
                SET SNomPer = @NomPer, SNomRel = @RelationDesc, SGuardName = @NomName;
            END
            ELSE IF @NomSrno = 2 OR @NomSrno = 3
            BEGIN
                UPDATE @NomineeDetails
                SET TNomPer = @NomPer, TNomRel = @RelationDesc, TGuardName = @NomName;
            END
        END

        FETCH NEXT FROM NomCursor INTO @NomSrno, @OrderCd, @NomName, @NomPer, @RelationDesc;
    END

    CLOSE NomCursor;
    DEALLOCATE NomCursor;

	select * from @NomineeDetails

	SET @StrString = 'select MAX(CASE WHEN cpd_holderno = 1 THEN cpd_poaid ELSE ''-'' END) FPOAMasterID, '
	+' MAX(CASE WHEN cpd_holderno = 1 THEN cpm_firstname ELSE ''-'' END) AS FPOAName, '
	+' MAX(CASE WHEN cpd_holderno = 1 THEN cpd_poaregno ELSE ''-'' END) FPOARef, '
	+' MAX(CASE WHEN cpd_holderno = 1 THEN ''First'' ELSE ''-'' END) FPOAHolder, '
    +' MAX(CASE WHEN cpd_holderno = 2 THEN cpd_poaid ELSE ''-'' END) SPOAMasterID, '
	+' MAX(CASE WHEN cpd_holderno = 2 THEN cpm_firstname ELSE ''-'' END) AS SPOAName, '
	+' MAX(CASE WHEN cpd_holderno = 2 THEN cpd_poaregno ELSE ''-'' END) SPOARef, '
	+' MAX(CASE WHEN cpd_holderno = 2 THEN ''Second'' ELSE ''-'' END) SPOAHolder, '
    +' MAX(CASE WHEN cpd_holderno = 3 THEN cpd_poaid ELSE ''-'' END) TPOAMasterID, '
	+' MAX(CASE WHEN cpd_holderno = 3 THEN cpm_firstname ELSE ''-'' END) AS TPOAName, '
	+' MAX(CASE WHEN cpd_holderno = 3 THEN cpd_poaregno ELSE ''-'' END) TPOARef, '
	+' MAX(CASE WHEN cpd_holderno = 3 THEN ''Third'' ELSE ''-'' END) TPOAHolder '
    +' from '+@dp_Database+'.DBO.client_poa_Details a,'+@dp_Database+'.DBO.client_master,'+@dp_Database+'.DBO.corporate_poa_master '
    +' where cpd_boid=cm_cd and cpd_poaid = cpm_poaid and cm_cd = ''' + @ClientCode + ''' '
    +' GROUP BY cm_cd '
	EXEC(@StrString)

END

SELECT @dp_Server = LTRIM(RTRIM(OP_Server)), @dp_Database = LTRIM(RTRIM(OP_DataBase)),
    @dp_Owner = LTRIM(RTRIM(OP_Owner)) FROM Other_Products(NOLOCK) 
    WHERE OP_Product = 'Estro'
    AND op_Status = 'A'

IF @dp_Database <> ''
BEGIN

SET @StrString = 'select '''' as DPID, RIGHT(cm_cd,8) as ClientID, '
+' CASE cm_indicator WHEN ''Y'' THEN ''YES'' ELSE ''NO'' END AS StandInstr, cm_sname AS ShortName, bc_description AS AcCategory, bt_description AS ClientType, '
+' CASE WHEN cm_opendate IS NULL THEN '''' ELSE RIGHT(''0'' + CONVERT(VARCHAR(2), DATEPART(dd, cm_opendate)), 2) + ''/'' + RIGHT(''0'' + CONVERT(VARCHAR(2), DATEPART(mm, cm_opendate)), 2) + ''/'' + CONVERT(VARCHAR(4), DATEPART(yyyy, cm_opendate)) END AS AcActDate, '
+' bs_description AS AcStatus, '''' AS StatusChangeReason, Branch = (Select bm_branchName from Branch_master where bm_branchcd = cm_brboffcode), '''' As SubType,  '
+' CASE WHEN cm_acc_closuredate IS NULL THEN '''' ELSE RIGHT(''0'' + CONVERT(VARCHAR(2), DATEPART(dd, cm_acc_closuredate)), 2) + ''/'' + RIGHT(''0'' + CONVERT(VARCHAR(2), DATEPART(mm, cm_acc_closuredate)), 2) + ''/'' + CONVERT(VARCHAR(4), DATEPART(yyyy, cm_acc_closuredate)) END AS AcClosureDate, '
+' cm_name AS FHolderName, CASE cb_emailstatement WHEN ''Y'' THEN ''Enabled'' ELSE ''Disabled'' END AS RecEStatement, ISNULL(cb_first_fh_name,'''') AS FHolderFName, bo_description AS Occupation, '
+' cb_nsadd1 AS CorrAdd1, ISNULL(cb_nsadd2,'''') AS CorrAdd2, ISNULL(cb_nsadd3,'''') AS CorrAdd3, ISNULL(cb_nsadd4,'''') AS CorrAdd4, ISNULL(cb_nspin,'''') AS CorrPincode, CorrCountry = (Select cs_desc from '+@dp_Database+'.DBO.Clientsub_master where cs_code = cb_CountryCdPer and cs_module = ''CS21''), CorrState = (Select cs_desc from '+@dp_Database+'.DBO.Clientsub_master where cs_code = cb_StateCdPer and cs_module = ''CS22''), ISNULL(cb_nstele,'''') AS MobileNo, ISNULL(cb_nsfax,'''') AS FaxNo, '
+' cb_fadd1 AS PerAdd1, cb_fadd2 AS PerAdd2, cb_fadd3 AS PerAdd3, cb_fadd4 AS PerAdd4, cb_fpin AS PerPincode, PerState = (Select cs_desc from '+@dp_Database+'.DBO.Clientsub_master where cs_code = cb_StateCdCor and cs_module = ''CS22''), PerCountry = (Select cs_desc from '+@dp_Database+'.DBO.Clientsub_master where cs_code = cb_CountryCor and cs_module = ''CS21''), '
+' ISNULL(cb_ftele,'''') AS MobileNo2, ISNULL(cb_ffax,'''') AS FaxNo2, '''' AS EDISFlag, CASE cb_GSECIDT WHEN ''Y'' THEN ''Yes'' ELSE ''No'' END AS IDTFlag, '
+' ISNULL(cm_bankactno,'''') AS BankAccNo,  BankAcType = (select ba_description from '+@dp_Database+'.DBO.Bankaccount_type where ba_code = cm_bankacttype) , ISNULL(cm_bankbranch,'''') As BankIFSC, ISNULL(cm_micr,'''') As BankMICR, '
+' ISNULL(cb_LEINo,'''') As LEINo, cb_UPIID As UPIID, cm_bankname AS BankName, cm_bankadd1 AS BankAdd1, cm_bankadd2 AS BankAdd2, cm_bankadd3 AS BankAdd3, cm_bankpin AS BankPinCode, CASE cm_attorney WHEN ''Y'' THEN ''Assigned'' ELSE ''Not Assigned'' END AS POADDPI, cm_taxstatus TaxStatus, '
+' CASE WHEN cb_sadd1 = ''01'' OR cb_sadd1 = ''11'' THEN ''Below 1 Lac'' WHEN cb_sadd1 = ''02'' OR cb_sadd1 = ''12'' THEN ''1-5 Lacs'' WHEN cb_sadd1 = ''03'' OR cb_sadd1 = ''13'' THEN ''5-10 Lacs'' WHEN cb_sadd1 = ''04'' OR cb_sadd1 = ''14'' THEN ''10-25 Lacs'' WHEN cb_sadd1 = ''05'' OR cb_sadd1 = ''06'' THEN ''More than 25 Lacs'' WHEN cb_sadd1 = ''15'' THEN ''1 Crore'' WHEN cb_sadd1 = ''16'' THEN ''More than 1 Crore'' ELSE '''' END AS GrossAnnIncome, LTRIM(RTRIM(cb_Networth)) AS NetWorth, cb_NetworthDt AS NetWorthAsOnDate, cb_panno AS FHolderPan, cm_mobile AS FHolderMobile, cm_email AS FHolderEmail, CASE cb_firstfamilymobflag WHEN ''Y'' THEN ''Enabled'' ELSE '''' END AS FHolderFamilyFlagMobile, CASE cb_firstfamilyemailflag WHEN ''Y'' THEN ''Enabled'' ELSE '''' END AS FHolderFamilyFlagEmail, CASE cm_sms WHEN ''Y'' THEN ''Available'' ELSE '''' END AS SMSFacility, CASE cb_fh_panflag WHEN ''Y'' THEN ''PAN verified and seeded with Aadhaar'' WHEN ''N'' THEN ''PAN not verified'' WHEN ''A'' THEN ''PAN verified and not seeded with Aadhaar'' WHEN ''B'' THEN ''PAN verified and seeding with Aadhaar not required'' ELSE '''' END AS PanFlag, ''Not Assigned'' AS ATHFlag, '''' AS ReceiveReports, cb_DOB AS FHolderDOB, '''' AS FHolderReasonAadhaar, '''' LogoPath '
+' from '+@dp_Database+'.DBO.Client_master, '+@dp_Database+'.DBO.Client_BackOffice,'+@dp_Database+'.DBO.Beneficiary_status,'+@dp_Database+'.DBO.Beneficiary_type,'+@dp_Database+'.DBO.Beneficiary_category,'+@dp_Database+'.DBO.Beneficiary_occupation '
+' where cm_cd = cb_cmcd and cm_active = bs_code and cm_clienttype= bt_code and cm_acctype = bc_code and cm_occup =bo_code and cm_schedule = 49843750 and cm_cd= ''' + @ClientCode + ''' '
EXEC(@StrString)

SET @StrString='Select CASE WHEN cn_purposeCd IN (''3'', ''6'') THEN ''First'' WHEN cn_purposeCd IN (''7'', ''8'') THEN ''Second'' WHEN cn_purposeCd IN (''9'', ''10'') THEN ''Third'' ELSE '''' END NomSeq, CASE WHEN cn_purposeCd = ''3'' THEN CASE WHEN cb_nominee_indicator = ''N'' THEN ''Nominee'' WHEN cb_nominee_indicator = ''G'' THEN ''Guardian'' END WHEN cn_purposeCd IN (''6'', ''8'', ''10'') THEN ''Guardian'' WHEN cn_purposeCd IN (''7'', ''9'') THEN ''Nominee'' ELSE '''' END AS NomType, cn_NomName AS NomName,cn_NomAdd1 AS NomAdd1, '
+' cn_NomAdd2 AS NomAdd2,cn_NomAdd3 AS NomAdd3,cn_NomAdd4 AS NomAdd4,cn_NomPin AS NomPinCode, '
+' cn_NomMob AS NomMobile,cn_NomEmail AS NomEmail,cn_NomPAN AS NomPan, cn_NomAadhar AS NomAadhaar '
+' From '+@dp_Database+'.DBO.Client_Nomineedetails, '+@dp_Database+'.DBO.Client_Backoffice where cn_cd = cb_cmcd and cn_cd =''' + @ClientCode + ''' and cn_purposeCd in(3,6,7,8,9,10) order by cn_purposeCd '
EXEC(@StrString)

SET @StrString = 'select LTRIM(RTRIM(cld_poaid)) AS POADDPIID, CASE cld_poatype WHEN ''C'' THEN ''Corporate POA'' WHEN ''H'' THEN ''Individual POA'' WHEN ''D'' THEN ''DDPI POA'' ELSE '''' END AS POADDPIType, LTRIM(RTRIM(cpm_poadesc)) AS POADDPIName, '''' AS POADDPIFavourOf, CASE cpm_poastatus WHEN 1 THEN ''Active'' ELSE ''Inactive'' END AS POADDPIStatus, '''' AS NoOfSignReq, '''' AS SigType From '+@dp_Database+'.DBO.client_poa_details, '+@dp_Database+'.DBO.corporate_poa_master, '+@dp_Database+'.DBO.client_master where cld_clientid = cm_cd and cld_poaid = cpm_poaid and cld_clientid = ''' + @ClientCode + ''' '
EXEC(@StrString)

END

END

END
GO

CREATE FUNCTION dbo.fnBinaryToBase64(@bin VARBINARY(MAX))
RETURNS NVARCHAR(MAX)
AS
BEGIN
    DECLARE @Base64 NVARCHAR(MAX)
    SET @Base64 = CAST(N'' AS XML).value('xs:base64Binary(xs:hexBinary(sql:variable("@bin")))','NVARCHAR(MAX)')
    RETURN @Base64
END
GO

CREATE   PROCEDURE [dbo].[stpr_Rpt_CapitalGain] @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT 
WITH ENCRYPTION
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
  DECLARE @dtFromDate VARCHAR(8), @strUserId VARCHAR(50), @strProduct VARCHAR(50), 
  @strOutputType VARCHAR(1)='', @XMLData XML,
  @strSelectTag VARCHAR(1)='', @strSelectUsers VARCHAR(500)='', @StrString NVARCHAR(MAX)='', @SQ1 INT = 0,
  @strSplFilter VARCHAR(MAX)='', @strRepType VARCHAR(50)='', @dtToDate VARCHAR(8)='', @strConsider112A VARCHAR(1)='N',
  @strRepSubType VARCHAR(50)='', @xmldata1 XML, @strShowPos VARCHAR(1)='Y', @strScripCode VARCHAR(20)='', @strShortSale VARCHAR(1)
  IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
  END 

  SET @XMLData = CAST('<root>'+@vcXML+'</root>' AS XML)

  SELECT @dtFromDate = ISNULL(x.value('(FromDt)[1]', 'VARCHAR(8)'),''),
  @strProduct = ISNULL(x.value('(Product)[1]', 'VARCHAR(50)'),''),
  @dtToDate = ISNULL(x.value('(ToDt)[1]', 'VARCHAR(8)'),''),
  @strUserId = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'),''),
  @strSelectTag = ISNULL(x.value('(SelectTag)[1]', 'VARCHAR(1)'),''),
  @strSelectUsers = ISNULL(x.value('(SelectUsers)[1]', 'VARCHAR(500)'),''),
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'),''),
  @strSplFilter = ISNULL(x.value('(SplFilter)[1]', 'VARCHAR(MAX)'),''),
  @strRepType = ISNULL(x.value('(RepType)[1]', 'VARCHAR(100)'),''),
  @strRepSubType = ISNULL(x.value('(RepSubType)[1]', 'VARCHAR(100)'),''),
  @strConsider112A = ISNULL(x.value('(Option112A)[1]', 'VARCHAR(1)'),''), 
  @strShowPos = ISNULL(x.value('(POS)[1]', 'VARCHAR(1)'),''),
  @strScripCode = ISNULL(x.value('(ScripCode)[1]', 'VARCHAR(20)'),''),
  @strShortSale = ISNULL(x.value('(ShortSale)[1]', 'VARCHAR(20)'),'')
  FROM @XMLData.nodes('/root') AS XTbl(x) 

  
  IF @strRepType = ''
  begin
    SET @strRepType = 'Actual PL_Detail'
  END
  
  IF @strShowPos = ''
  begin
    SET @strShowPos = 'Y'
  END
  IF ISNULL(@strConsider112A ,'') = ''
  BEGIN
    SET @strConsider112A = 'N'
  END
  
  
  DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50)) 
  
  	
  IF @strSplFilter = ''
  BEGIN
    INSERT INTO @tbl_UserList(Client_Code) 
    SELECT * FROM DBO.[fn_GetClients](@strUserId,@strSelectTag,@strSelectUsers)
  END 	 
  ELSE
  IF @strSplFilter <> ''
  BEGIN
    SET @StrString  = ' SELECT distinct CM_CD FROM Client_master(NOLOCK) WHERE 1 = 1  AND '+@strSplFilter
	INSERT INTO @tbl_UserList(Client_Code) 
	EXEC(@StrString)
  END	
  
  SET @StrString = ''
 
  DECLARE @strDT1 VARCHAR(20)= CONVERT(VARCHAR(20),DATEADD(DAY,-1,@dtFromDate),112), @xmldata2 XML
  declare @vcXML1 NVARCHAR(MAX) = '<FromDt></FromDt><ToDt>'+@strDT1+'</ToDt><UserId>'+@strUserId+'</UserId><Product></Product>'
  +'<SelectTag></SelectTag><SelectUsers></SelectUsers><OutputType>X</OutputType><SplFilter>'+@strSplFilter+'</SplFilter><RepType>Notional_Detail</RepType>'
  +'<RepSubType></RepSubType><Option112A>'+@strConsider112A+'</Option112A><ScripCode>'+@strScripCode+'</ScripCode>'
  
  SET @vcXML1 = REPLACE(@vcXML1,'''','''''')
  SET @strString = 'EXEC DBO.stpr_Rpt_CapitalGainNotional' + ' ''' + @vcXML1 + ''', @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT';
  EXEC sp_executesql @strString, N'@o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT', @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT;
  
  DECLARE @tbl_OpenPosition TABLE(td_SRNO INT, ClientCode VARCHAR(20), ScripCode VARCHAR(20), TradeDate VARCHAR(20),
  td_Stlmnt VARCHAR(20),
  BuyQty MONEY, BuyRate MONEY, BuyValue MONEY, SaleQty MONEY, SaleRate MONEY, SaleValue MONEY, STT MONEY)
  IF @o_vcErrorFlag = 'S'
  BEGIN
    SET @xmldata2 = CAST(@o_vcErrorMessage AS XML)
	
    INSERT INTO @tbl_OpenPosition(td_SRNO, ClientCode, ScripCode, TradeDate, td_Stlmnt, BuyQty, BuyRate, BuyValue,
    SaleQty, SaleRate, SaleValue, STT)
    SELECT CapGain.value('(td_SRNO)[1]', 'INT') AS td_SRNO,
	CapGain.value('(ClientCode)[1]', 'VARCHAR(50)') AS ClientCode ,
    CapGain.value('(ScripCode)[1]', 'VARCHAR(50)') AS ScripCode ,
    ISNULL(CapGain.value('(TradeDate)[1]', 'VARCHAR(20)'),'') AS TradeDate,
	ISNULL(CapGain.value('(td_Stlmnt)[1]', 'VARCHAR(20)'),'') AS td_Stlmnt,
    CapGain.value('(BuyQty)[1]', 'MONEY') AS BuyQty,
    CapGain.value('(BuyRate)[1]', 'MONEY') AS BuyRate,
    CapGain.value('(BuyValue)[1]', 'MONEY') AS BuyValue,
    CapGain.value('(SaleQty)[1]', 'MONEY') AS SaleQty,
    CapGain.value('(SaleRate)[1]', 'MONEY') AS SaleRate,
    CapGain.value('(SaleValue)[1]', 'MONEY') AS SaleValue,
	CapGain.value('(STT)[1]', 'MONEY') AS SaleValue
    FROM @xmldata2.nodes('/CapGain') AS XTbl(CapGain)
  END

  CREATE TABLE #tbl_DelvTrxn (
	td_SRNO INT, td_dt VARCHAR(8), td_Stlmnt VARCHAR(20), td_clientcd VARCHAR(20), td_scripcd VARCHAR(20), 
	td_bsflag VARCHAR(1), Qty_SS Numeric(19,6), VALUES_SS Numeric(19,6), Qty_NS Numeric(19,6), VALUES_NS Numeric(19,6), td_Rate Numeric(19,6), FIFONo INT, 
	XTAG11 INT, LONG_TAG VARCHAR(1), SQR_TAG VARCHAR(1), Tmp_RefNo numeric, td_Filler1 VARCHAR(8), td_stt MONEY
	)

  CREATE TABLE #tbl_DelvTrxn1 (
	td_SRNO INT, td_dt VARCHAR(8), td_Stlmnt VARCHAR(20), td_clientcd VARCHAR(20), td_scripcd VARCHAR(20), 
	td_bsflag VARCHAR(1), Qty_SS Numeric(19,6), VALUES_SS Numeric(19,6), Qty_NS Numeric(19,6), VALUES_NS Numeric(19,6), td_Rate Numeric(19,6), FIFONo INT, 
	XTAG11 INT, LONG_TAG VARCHAR(1), SQR_TAG VARCHAR(1), Tmp_RefNo numeric, td_Filler1 VARCHAR(8), td_stt MONEY
	)

  CREATE TABLE #tbl_DelvTrxn2 (
	td_SRNO INT, td_dt VARCHAR(8), td_Stlmnt VARCHAR(20), td_clientcd VARCHAR(20), td_scripcd VARCHAR(20), 
	td_bsflag VARCHAR(1), Qty_SS Numeric(19,6), VALUES_SS Numeric(19,6), Qty_NS Numeric(19,6), VALUES_NS Numeric(19,6), td_Rate Numeric(19,6), FIFONo INT, 
	XTAG11 INT, LONG_TAG VARCHAR(1), SQR_TAG VARCHAR(1), Tmp_RefNo numeric, td_Filler1 VARCHAR(8), td_stt MONEY
	)

  --- MAIN DATA INSERT INTO TEMP TABLE
  
  INSERT INTO #tbl_DelvTrxn (
	td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_SS, VALUES_SS, Qty_NS, VALUES_NS, td_Rate, Tmp_RefNo, td_Filler1, td_stt, 
	FIFONO
	)
  SELECT X.*, FIFONO = ROW_NUMBER() OVER (
		PARTITION BY td_clientcd ORDER BY td_scripcd, TD_DT, td_SRNO
		)
  FROM (
	SELECT td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, 
	Qty_SS = CASE WHEN (td_TRDType <> 'DL' OR td_Filler2  = 'T') THEN td_bqty - td_Sqty ELSE 0 END, 
	VALUES_SS = (CASE WHEN (td_TRDType <> 'DL' OR td_Filler2  = 'T') THEN
	(CASE WHEN td_bsflag = 'B' THEN td_Rate + Round(td_ServiceTax + 
					td_OtherChrgs1 + td_OtherChrgs2, 4) ELSE 
					td_Rate - Round(td_ServiceTax + td_OtherChrgs1 + td_OtherChrgs2, 4) END) ELSE 
			0 END) * ABS(CASE WHEN (td_TRDType <> 'DL' OR td_Filler2  = 'T') THEN td_bqty - td_Sqty ELSE 0 END), 
			Qty_NS = ABS(CASE WHEN td_TRDType = 'DL' AND td_Filler2  <> 'T' THEN td_bqty - td_Sqty ELSE 0 END)
		, VALUES_NS = (CASE WHEN td_TRDType = 'DL'  AND td_Filler2  <> 'T' THEN 
		(CASE WHEN td_bsflag = 'B' THEN td_Rate + Round(td_ServiceTax + td_OtherChrgs1 + 
					td_OtherChrgs2, 4) ELSE td_Rate - Round(td_ServiceTax + td_OtherChrgs1 + td_OtherChrgs2, 4) END) ELSE 0 END) * 
					ABS(CASE WHEN td_TRDType = 'DL'   AND td_Filler2  <> 'T' THEN td_bqty - td_Sqty ELSE 0 END), 
				td_Rate = (CASE WHEN td_bsflag = 'B' THEN td_Rate + Round(td_ServiceTax + 
				td_OtherChrgs1 + td_OtherChrgs2, 4) ELSE td_Rate - Round(td_ServiceTax + td_OtherChrgs1 + td_OtherChrgs2, 4) END), td_NFiller2, td_Filler1,
				td_stt
	FROM TRX_INVPL(NOLOCK), Client_master(NOLOCK), Branch_master(NOLOCK)
	WHERE cm_cd = td_clientcd AND cm_brboffcode = bm_branchcd AND td_dt >= @dtFromDate AND td_dt <= @dtToDate --and td_Filler2 <> 'T'
	AND cm_cd IN(SELECT Client_Code from @tbl_UserList) --AND td_scripcd = '519570'
	AND ((td_scripcd = @strScripCode AND @strScripCode <> '') OR @strScripCode = '')
	UNION ALL
	SELECT td_SRNO, TradeDate, td_Stlmnt,  ClientCode, ScripCode, td_bsflag = (CASE WHEN BuyQty <> 0 THEN 'B' WHEN SaleQty <> 0 THEN 'S' ELSE '' END),
    Qty_SS = 0,VALUES_SS = 0,Qty_NS = (CASE WHEN BuyQty <> 0 THEN BuyQty WHEN SaleQty <> 0 THEN SaleQty ELSE 0 END),
    VALUES_NS = (CASE WHEN BuyQty <> 0 THEN BuyValue WHEN SaleQty <> 0 THEN SaleValue ELSE 0 END),
    td_Rate = (CASE WHEN BuyQty <> 0 THEN BuyRate WHEN SaleQty <> 0 THEN SaleRate ELSE 0 END), Tmp_RefNo = 0,
    td_Filler1 = '', td_stt = CASE WHEN (CASE WHEN BuyQty <> 0 THEN BuyQty WHEN SaleQty <> 0 THEN SaleQty ELSE 0 END) <>0 THEN ROUND(STT/(CASE WHEN BuyQty <> 0 THEN BuyQty WHEN SaleQty <> 0 THEN SaleQty ELSE 0 END),4)
	ELSE 0 END
    FROM @tbl_OpenPosition) X
	

  /*
	
  INSERT INTO #tbl_DelvTrxn (
	td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_SS, VALUES_SS, Qty_NS, VALUES_NS, td_Rate, Tmp_RefNo, td_Filler1, td_stt, 
	FIFONO
	)
  SELECT td_SRNO, TradeDate, td_Stlmnt,  ClientCode, ScripCode, td_bsflag = (CASE WHEN BuyQty <> 0 THEN 'B' WHEN SaleQty <> 0 THEN 'S' ELSE '' END),
  0,0,Qty_NS = (CASE WHEN BuyQty <> 0 THEN BuyQty WHEN SaleQty <> 0 THEN SaleQty ELSE 0 END),
  VALUES_NS = (CASE WHEN BuyQty <> 0 THEN BuyValue WHEN SaleQty <> 0 THEN SaleValue ELSE 0 END),
  td_Rate = (CASE WHEN BuyQty <> 0 THEN BuyRate WHEN SaleQty <> 0 THEN SaleRate ELSE 0 END), Tmp_RefNo = 0,
  td_Filler1 = '', td_stt = 0, FIFONO = 0
  FROM @tbl_OpenPosition
  */
 
  
 
  DECLARE @UPDtd_SRNO INT, @updTmp_RefNo INT
  DECLARE CursorUpdatedate CURSOR
  FOR
  SELECT td_SRNO, Tmp_RefNo
  FROM #tbl_DelvTrxn x where Tmp_RefNo > 0
  
  OPEN CursorUpdatedate
  FETCH NEXT
  FROM CursorUpdatedate
  INTO @UPDtd_SRNO, @updTmp_RefNo
  WHILE @@FETCH_STATUS = 0
  BEGIN 
    UPDATE A SET a.Qty_NS = a.Qty_NS - b.Qty_NS
	FROM #tbl_DelvTrxn A, #tbl_DelvTrxn B  
    WHERE A.td_SRNO = @UPDtd_SRNO and b.td_SRNO  = @updTmp_RefNo
	
	DELETE FROM #tbl_DelvTrxn WHERE td_SRNO = @updTmp_RefNo
	
    FETCH NEXT FROM CursorUpdatedate
	INTO @UPDtd_SRNO, @updTmp_RefNo
  END
  CLOSE CursorUpdatedate
  DEALLOCATE CursorUpdatedate
  
  DELETE FROM #tbl_DelvTrxn WHERE Qty_NS = 0 AND Qty_SS = 0
  
  UPDATE #tbl_DelvTrxn SET td_dt = td_Filler1 WHERE td_Filler1 NOT IN('','0')
  
  
  
   
  CREATE INDEX indx_DelvTrxn ON #tbl_DelvTrxn (td_clientcd, td_scripcd, td_SRNO, FIFONO)
  CREATE INDEX indx_DelvTrxn1 ON #tbl_DelvTrxn1 (td_clientcd, td_scripcd, td_SRNO, FIFONO)
  INSERT INTO #tbl_DelvTrxn2 (
			  td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO, XTAG11, 
			    LONG_TAG, SQR_TAG, TD_STT)
  SELECT td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO, ISNULL(XTAG11,0), 
			    ISNULL(LONG_TAG,''), ISNULL(SQR_TAG,''), TD_STT
				FROM  #tbl_DelvTrxn X 
  WHERE TD_DT <(SELECT MIN(TD_DT) FROM #tbl_DelvTrxn WHERE td_clientcd = X.td_clientcd AND td_scripcd = X.td_scripcd AND td_bsflag ='B' AND Qty_NS <> 0)
  AND td_bsflag ='S'  AND Qty_NS <> 0
  
  
  DELETE FROM #tbl_DelvTrxn 
  WHERE td_SRNO IN(SELECT td_SRNO FROM #tbl_DelvTrxn2)
  
  
  DECLARE @SQR_QTY MONEY = 0, @SQR_QTY1 MONEY = 0, @td_clientcdC1 VARCHAR(10) = '', @td_scripcdC1 VARCHAR(10) = '', 
	@td_BuyQtyC1 MONEY = 0, @td_SaleQtyC1 MONEY = 0, @COUNTER INT = 0, @td_SRNOB INT, 
	@td_dtB VARCHAR(8), @td_StlmntB VARCHAR(20), @td_clientcdB VARCHAR(50), @td_scripcdB VARCHAR(50), 
	@td_bsflagB VARCHAR(1), @QTY_NSB MONEY, @td_RateB Numeric(19,6), @VALUES_NSB  Numeric(19,6), 
	@td_SRNOS INT, @td_dtS VARCHAR(8), @td_StlmntS VARCHAR(20)='', @td_clientcdS VARCHAR(50), @td_scripcdS 
	VARCHAR(50), @td_bsflagS VARCHAR(1), @QTY_NSS MONEY, @td_RateS Numeric(19,6), @buyQty MONEY = 0, @SaleQty MONEY = 0, 
	@BQTY MONEY = 0, @SQTY MONEY = 0, @VALUES_NSS  Numeric(19,6), @FIFONOB INT, @FIFONOS INT, @SQ MONEY = 0, @TAG VARCHAR(1) = 'C', 
	@strFlag VARCHAR(1)='N', @strsaleflag VARCHAR(1)='', @TD_STTB MONEY, @TD_STTS MONEY

	
  DECLARE CursorC1Main CURSOR
  FOR
  SELECT td_clientcd, td_scripcd, SUM(CASE WHEN td_bsflag = 'B' THEN Qty_NS ELSE 0 END) BuyQty, 
  SUM(CASE WHEN td_bsflag = 'S' THEN Qty_NS ELSE 0 END) SaleQty
  FROM #tbl_DelvTrxn x
  GROUP BY td_clientcd, td_scripcd
  HAVING SUM(CASE WHEN td_bsflag = 'B' THEN Qty_NS ELSE 0 END) <> 0 
  AND SUM(CASE WHEN td_bsflag = 'S' THEN Qty_NS ELSE 0 END) <> 0

  OPEN CursorC1Main
  FETCH NEXT
  FROM CursorC1Main
  INTO @td_clientcdC1, @td_scripcdC1, @td_BuyQtyC1, @td_SaleQtyC1
  WHILE @@FETCH_STATUS = 0
  BEGIN
	IF SIGN(@td_BuyQtyC1 - @td_SaleQtyC1) <> 1
	BEGIN
		SET @SQR_QTY = @td_BuyQtyC1
		SET @SQR_QTY1 = @td_BuyQtyC1
		set @strsaleflag = 'B'
	END
	ELSE
	BEGIN
		SET @SQR_QTY = @td_SaleQtyC1
		SET @SQR_QTY1 = @td_SaleQtyC1
		set @strsaleflag = 'S'
	END
	
	IF @td_BuyQtyC1 = @td_SaleQtyC1
	BEGIN
	  set @strsaleflag = 'S'
	END
	
	DECLARE CursorBMain CURSOR
	FOR
	SELECT td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO, TD_STT
	FROM #tbl_DelvTrxn
	WHERE td_clientcd = @td_clientcdC1 AND td_scripcd = @td_scripcdC1 AND td_bsflag = 'B' AND Qty_NS <> 0
	and isnull(SQR_TAG,'')=''
	ORDER BY td_clientcd, td_scripcd, FIFONO, td_SRNO
	
	DECLARE CursorSMain CURSOR
	FOR
	SELECT td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO,TD_STT
	FROM #tbl_DelvTrxn
	WHERE td_clientcd = @td_clientcdC1 AND td_scripcd = @td_scripcdC1 AND td_bsflag = 'S' AND Qty_NS <> 0
	and isnull(SQR_TAG,'')='' 
	ORDER BY td_clientcd, td_scripcd, FIFONO, td_SRNO
	OPEN CursorBMain
	OPEN CursorSMain
	WHILE @@FETCH_STATUS = 0
	BEGIN
	   SET @COUNTER = @COUNTER + 1
		IF @SQR_QTY <> 0
		BEGIN
			IF @BQTY = 0
			BEGIN
				FETCH NEXT
				FROM CursorBMain
				INTO @td_SRNOB, @td_dtB, @td_StlmntB, @td_clientcdB, @td_scripcdB, @td_bsflagB, @Qty_NSB, @td_RateB, 
					@VALUES_NSB, @FIFONOB, @TD_STTB
				SET @BQTY = @Qty_NSB
			END
		END
		IF @SQR_QTY1 <> 0
		BEGIN
		  IF @SQTY = 0 OR (@td_dtS < @td_dtB and @SQTY > 0)
		  BEGIN
			IF @SQTY > 0 --AND @td_dtS < @td_dtB
			BEGIN
			  INSERT INTO #tbl_DelvTrxn2 (
			  td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO, XTAG11, 
			  LONG_TAG, SQR_TAG, TD_STT)
		      VALUES (@td_SRNOS, @td_dtS, @td_StlmntS, @td_clientcdS, @td_scripcdS, @td_bsflagS, @SQTY, @td_RateS, @SQTY * @td_RateS, 
			  @FIFONOS, @COUNTER,'X', '',  @TD_STTS)
               
			  UPDATE #tbl_DelvTrxn
			  SET SQR_TAG = @TAG
			  WHERE td_clientcd = @td_clientcdS AND FIFONO = @FIFONOS
			  
			  IF @strsaleflag = 'S'
			  BEGIN
			    SET @SQR_QTY1 = @SQR_QTY1 - @SQTY 
		        SET @SQR_QTY = @SQR_QTY - @SQTY
			  END	
			  SET @SQTY = 0	
			  SET @strFlag = 'Y'
			  
			END
			
			FETCH NEXT FROM CursorSMain INTO @td_SRNOS, @td_dtS, @td_StlmntS, @td_clientcdS, @td_scripcdS, @td_bsflagS, @Qty_NSS, @td_RateS, 
			@VALUES_NSS, @FIFONOS, @TD_STTS
			
			IF @td_dtS >= @td_dtB 
			BEGIN
			  SET @SQTY = @Qty_NSS
			END  
			ELSE
			BEGIN
			  IF @Qty_NSS > 0 --AND @td_dtS < @td_dtB
			  BEGIN
			  
			  	SET @SQTY = @Qty_NSS
				IF @strsaleflag = 'S'
			    BEGIN
			      SET @SQR_QTY1 = @SQR_QTY1 - @Qty_NSS 
		          SET @SQR_QTY = @SQR_QTY - @Qty_NSS
				END  
			    IF @SQR_QTY > 0
                BEGIN				
				  INSERT INTO #tbl_DelvTrxn2 (
			      td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO, XTAG11, 
			      LONG_TAG, SQR_TAG, TD_STT)
		          VALUES (@td_SRNOS, @td_dtS, @td_StlmntS, @td_clientcdS, @td_scripcdS, @td_bsflagS, @Qty_NSS, @td_RateS, @Qty_NSS * @td_RateS, 
			      @FIFONOS, @COUNTER,'L', '', @TD_STTS)
				END  
   	
		        SET @SQTY = 0 
                SET @Qty_NSS = 0				
            
				UPDATE #tbl_DelvTrxn
			    SET SQR_TAG = @TAG
			    WHERE td_clientcd = @td_clientcdS AND FIFONO = @FIFONOS
				SET @strFlag = 'Y'
              
			  END	
		    END  
		  END
		END
		
		
		IF @BQTY >= @SQTY 
		BEGIN
		  IF @BQTY > 0
		  BEGIN
		    SET @SQ = @SQTY
		  END	
		END
		ELSE
		BEGIN
		  IF @SQTY > 0
		  BEGIN
		    
		    SET @SQ = @BQTY
		  END	
		END
		
		
		IF @strFlag = 'Y'
		begin
		 --SELECT @SQ
		 SET @SQ = 0
		 SET @strFlag = 'N'
		END
	
		INSERT INTO #tbl_DelvTrxn1 (
			td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO, XTAG11, 
			LONG_TAG, SQR_TAG, TD_STT
			)
		VALUES (
			@td_SRNOB, @td_dtB, @td_StlmntB, @td_clientcdB, @td_scripcdB, @td_bsflagB, @SQ, @td_RateB, @SQ * @td_RateB, 
			@FIFONOB, @COUNTER, (CASE WHEN SIGN(ABS(DATEDIFF(DAY, CAST(@td_dtB AS DATE), CAST(@td_dtS AS DATE))) - 365) = 1 THEN 'L' ELSE '' END
				), 'Y', @TD_STTB
			)
		SET @BQTY = @BQTY - @SQ
     
         		
		INSERT INTO #tbl_DelvTrxn1 (
			td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO, XTAG11, 
			LONG_TAG, SQR_TAG, TD_STT
			)
		VALUES (
			@td_SRNOS, @td_dtS, @td_StlmntS, @td_clientcdS, @td_scripcdS, @td_bsflagS, @SQ, @td_RateS, @SQ * @td_RateS, 
			@FIFONOS, @COUNTER, (CASE WHEN SIGN(ABS(DATEDIFF(DAY, CAST(@td_dtB AS DATE), CAST(@td_dtS AS DATE))) - 365) = 1 THEN 'L' ELSE '' END
				), 'Y', @TD_STTS
			)
		SET @SQTY = @SQTY - @SQ
        --ABD:
	
		IF @BQTY = 0
		BEGIN
			UPDATE #tbl_DelvTrxn
			SET SQR_TAG = @TAG
			WHERE td_clientcd = @td_clientcdB AND FIFONO = @FIFONOB
		END
		
		IF @SQTY = 0
		BEGIN
			UPDATE #tbl_DelvTrxn
			SET SQR_TAG = @TAG
			WHERE td_clientcd = @td_clientcdS AND FIFONO = @FIFONOS
		END

		SET @SQR_QTY1 = @SQR_QTY1 - @SQ 
		SET @SQR_QTY = @SQR_QTY - @SQ
		SET @strFlag = 'N'
		--SELECT 'VAG',@SQ, @SQR_QTY, @BQTY, @SQTY, @FIFONOB, @FIFONOS
		IF (@SQR_QTY1 = 0 AND @SQR_QTY = 0)
		BEGIN
			BREAK;
		END
	END
	CLOSE CursorBMain;
	CLOSE CursorSMain;
	DEALLOCATE CursorBMain;
	DEALLOCATE CursorSMain;
	IF @BQTY <> 0
	BEGIN
		INSERT INTO #tbl_DelvTrxn1 (
			td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO, XTAG11, 
			LONG_TAG, SQR_TAG, TD_STT
			)
		VALUES (
			@td_SRNOB, @td_dtB, @td_StlmntB, @td_clientcdB, @td_scripcdB, @td_bsflagB, @BQTY, @td_RateB, @BQTY * 
			@td_RateB, @FIFONOB, @COUNTER, '', '', @TD_STTB
			)
		UPDATE #tbl_DelvTrxn
		SET SQR_TAG = @TAG
		WHERE td_clientcd = @td_clientcdB AND FIFONO = @FIFONOB
	END
	
	IF @SQTY <> 0
	BEGIN
		INSERT INTO #tbl_DelvTrxn1 (
			td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO, XTAG11, 
			LONG_TAG, SQR_TAG, TD_STT
			)
		VALUES (
			@td_SRNOS, @td_dtS, @td_StlmntS, @td_clientcdS, @td_scripcdS, @td_bsflagS, @SQTY, @td_RateS, @SQTY * 
			@td_RateS, @FIFONOS, @COUNTER, '', '', @TD_STTS
			)
		UPDATE #tbl_DelvTrxn
		SET SQR_TAG = @TAG
		WHERE td_clientcd = @td_clientcdS AND FIFONO = @FIFONOS
	END
	SET @SQ = 0
	SET @BQTY = 0
	SET @SQTY = 0
	SET @SQR_QTY = 0
	SET @SQR_QTY1 = 0
	FETCH NEXT
	FROM CursorC1Main
	INTO @td_clientcdC1, @td_scripcdC1, @td_BuyQtyC1, @td_SaleQtyC1
  END
  CLOSE CursorC1Main
  DEALLOCATE CursorC1Main
  

 
  DELETE
  FROM #tbl_DelvTrxn WHERE SQR_TAG = 'C'
  
  INSERT INTO #tbl_DelvTrxn 
  SELECT * FROM #tbl_DelvTrxn1 WHERE ISNULL(Qty_SS,0)+isnull(Qty_NS,0) <> 0 
  
  --SELECT * FROM #tbl_DelvTrxn WHERE  td_scripcd = '533326'
  
    --- INSERT DELIVERY TRXN
  
  CREATE TABLE #tbl_Rep (
	Tag VARCHAR(50), ClientCode VARCHAR(50), CLientName VARCHAR(100), Scrip_Code VARCHAR(50), ScripName VARCHAR(
		100), XTAG11 INT, BuyDate VARCHAR(20), BuyQty Numeric(19,6), BuyRate Numeric(19,6), BuyValue Numeric(19,6), SaleDate VARCHAR(20), 
	SaleQty Numeric(19,6), SaleRate Numeric(19,6), SaleValue Numeric(19,6), DAYS INT, S_TERM_PL MONEY, L_TERM_PL MONEY, SP_TERM_PL MONEY, 
	NetQty MONEY, NetValue MONEY, CMP MONEY, NOT_PL MONEY, BuySTT MONEY, SaleSTT MONEY, Tmp_112ARate MONEY, T112AFlag VARCHAR(1) NOT NULL DEFAULT 'N',
	BuyBackFlag varchar(1)
	)
  INSERT INTO #tbl_Rep (Tag, ClientCode, Scrip_Code, XTAG11, BuyDate, BuyQty, BuyRate, BuyValue, BuySTT)
  SELECT Tag = 'Delivery Trxn', CLIENT_CODE, SCRIP_CODE, XTAG11, CONVERT(VARCHAR, MAX(cast(BOT_TRXN_DATE AS DATE
			)), 106) BOT_TRXN_DATE, SUM(BOT_QTY) BOT_QTY, cast((CASE WHEN SUM(BOT_QTY) = 0 THEN 0 ELSE ROUND(SUM(BOT_VALUE) / SUM(BOT_QTY), 6
		)  END) as Numeric(19,6)) AS BOT_MKT_RATE, SUM(BOT_VALUE) BOT_VALUE, SUM(abs(BOT_QTY)*TD_STT) AS BuySTT
  FROM (
	SELECT td_clientcd AS Client_Code, SCRIP_CODE = td_scripcd, XTAG11, td_dt AS BOT_TRXN_DATE, QTY_NS BOT_QTY, 
		td_Rate BOT_MKT_RATE, cast(VALUES_NS as  Numeric(19,6)) BOT_VALUE, TD_STT
	FROM #tbl_DelvTrxn
	WHERE td_bsflag = 'B' AND isnull(SQR_TAG, 'N') = 'Y' 
  	) X123
  GROUP BY CLIENT_CODE, SCRIP_CODE, XTAG11
  
  
  --- UPDATE SALE TRANSACTION
  
  UPDATE A
  SET A.SaleQty = B.BOT_QTY, A.SaleRate = B.BOT_MKT_RATE, A.SaleDATE = B.BOT_TRXN_DATE, A.SaleValue = B.BOT_VALUE, A.SaleSTT = B.BuySTT,
  A.BuyBackFlag = B.BuyBackFlag
  FROM #tbl_Rep A, (
		SELECT CLIENT_CODE, SCRIP_CODE, XTAG11, CONVERT(VARCHAR, MAX(cast(BOT_TRXN_DATE AS DATE)), 106) 
			BOT_TRXN_DATE, SUM(BOT_QTY) BOT_QTY, 
			(CASE WHEN SUM(BOT_QTY) = 0 THEN 0 ELSE ROUND(SUM(BOT_VALUE) / SUM(BOT_QTY), 6) END) AS BOT_MKT_RATE, 
			SUM(BOT_VALUE) BOT_VALUE, SUM(abs(BOT_QTY)*TD_STT) AS BuySTT, BuyBackFlag
		FROM (
			SELECT td_clientcd AS Client_Code, SCRIP_CODE = td_scripcd, XTAG11, td_dt AS BOT_TRXN_DATE, QTY_NS BOT_QTY, 
				td_Rate BOT_MKT_RATE, VALUES_NS BOT_VALUE, TD_STT, BuyBackFlag = CASE WHEN se_type='T' THEN 'Y' ELSE 'N' END
			FROM #tbl_DelvTrxn LEFT OUTER JOIN Settlements(NOLOCK) SETT ON(td_Stlmnt = se_stlmnt)
			WHERE td_bsflag = 'S' AND isnull(SQR_TAG, 'N') = 'Y'
			) X123
		GROUP BY CLIENT_CODE, SCRIP_CODE, XTAG11, BuyBackFlag
		) b
  WHERE A.ClientCode = b.Client_Code AND a.Scrip_Code = b.SCRIP_CODE AND a.XTAG11 = b.XTAG11
  

  --- INSERT Spacu TRXN

  INSERT INTO #tbl_Rep (
	Tag, ClientCode, Scrip_Code, XTAG11, BuyDate, BuyQty, BuyRate, BuyValue, SaleDate, SaleQty, SaleRate, 
	SaleValue, BuySTT, SaleSTT
	)
  SELECT 'Spacu Trxn' XTAG111, CLIENT_CODE = td_clientcd, SCRIP_CODE = td_scripcd, XTAG11, 
  (CASE WHEN td_bsflag = 'B' THEN td_dt ELSE '' END) AS BOT_TRXN_DATE, 
  SUM(CASE WHEN td_bsflag = 'B' THEN QTY_SS ELSE 0 END) BOT_QTY, 
		(CASE WHEN SUM(CASE WHEN td_bsflag = 'B' THEN QTY_SS ELSE 0 END) <> 0 THEN
		ROUND(SUM(CASE WHEN td_bsflag = 'B' THEN VALUES_SS ELSE 0 END)/
		SUM(CASE WHEN td_bsflag = 'B' THEN QTY_SS ELSE 0 END),2) ELSE 0 END)  BOT_MKT_RATE
	,  SUM(CASE WHEN td_bsflag = 'B' THEN VALUES_SS ELSE 0 END) BOT_VALUE,
	SaleDate = '01/01/1900', SaleQty = 0, SaleRate = 0, 
	SaleValue = 0, BuySTT = SUM(CASE WHEN td_bsflag = 'B' THEN abs(QTY_SS)*TD_STT ELSE 0 END), SaleSTT = 0
  FROM #tbl_DelvTrxn
  WHERE ISNULL(QTY_SS, 0) <> 0 AND td_bsflag = 'B'
  GROUP BY td_clientcd, td_scripcd, td_bsflag, td_dt, XTAG11

  --- UPDATE SALE TRXN


  UPDATE A
  SET a.SaleQty = B.BOT_QTY, A.SaleRate = B.BOT_MKT_RATE, A.SaleDATE = B.BOT_TRXN_DATE, A.SaleValue = B.BOT_VALUE, A.SaleSTT = B.SaleSTT
  FROM #tbl_Rep A, (
		SELECT CLIENT_CODE, SCRIP_CODE, XTAG11, cast(BOT_TRXN_DATE AS DATE) BOT_TRXN_DATE, SUM(BOT_QTY) BOT_QTY, 
		cast((CASE WHEN SUM(BOT_QTY) = 0 THEN 0 ELSE ROUND(SUM(BOT_VALUE) / SUM(BOT_QTY), 6) END) as Numeric(19,6)) BOT_MKT_RATE, 
		SUM(BOT_VALUE) BOT_VALUE, SaleSTT = SUM(abs(BOT_QTY)*TD_STT)
		FROM (
			SELECT td_clientcd AS Client_Code, SCRIP_CODE = td_scripcd, XTAG11, td_dt AS BOT_TRXN_DATE, QTY_SS BOT_QTY, 
				td_Rate BOT_MKT_RATE, cast(VALUES_SS as Numeric(19,6)) BOT_VALUE, TD_STT
			FROM #tbl_DelvTrxn
			WHERE td_bsflag = 'S' AND ISNULL(QTY_SS, 0) <> 0
			) X123
		GROUP BY CLIENT_CODE, SCRIP_CODE, XTAG11, cast(BOT_TRXN_DATE AS DATE)
		) b
  WHERE A.ClientCode = b.Client_Code AND a.Scrip_Code = b.SCRIP_CODE AND a.BuyDate = b.BOT_TRXN_DATE AND Tag = 'Spacu Trxn'
  
  
  --- INSERT DELIVERY NOT SQR UP TRXN 	
  /*	
  INSERT INTO #tbl_Rep (
	Tag, ClientCode, Scrip_Code, XTAG11, BuyDate, BuyQty, BuyRate, BuyValue, SaleDate, SaleQty, SaleRate, 
	SaleValue, BuySTT, SaleSTT
	)
  SELECT XTAG111, CLIENT_CODE, SCRIP_CODE, XTAG11 = 0, convert(VARCHAR, CAST(BOT_TRXN_DATE AS DATE), 106) 
	BOT_TRXN_DATE, SUM(BOT_QTY) BOT_QTY, (CASE WHEN SUM(BOT_QTY) = 0 THEN 0 ELSE ROUND(SUM(BOT_VALUE) / SUM(BOT_QTY), 4) END) 
	BOT_MKT_RATE, SUM(BOT_VALUE) BOT_VALUE, convert(VARCHAR, CAST(SOLD_TRXN_DATE AS DATE), 106) SOLD_TRXN_DATE, 
	SUM(SOLD_QTY) SOLD_QTY, 
	(CASE WHEN SUM(SOLD_QTY) = 0 THEN 0 ELSE ROUND(SUM(SOLD_VALUE) / SUM(SOLD_QTY), 4) END) SOLD_MKT_RATE, 
	SUM(SOLD_VALUE) SOLD_VALUE, SUM(BuySTT) AS BuySTT, SUM(SaleSTT) AS SaleSTT
  FROM (
	SELECT 'Delivery Trxn' XTAG111, CLIENT_CODE = td_clientcd, SCRIP_CODE = td_scripcd, XTAG11, 
	CASE WHEN td_bsflag = 'B' THEN td_dt ELSE '19000101' END BOT_TRXN_DATE, 
		(CASE WHEN td_bsflag = 'B' THEN QTY_NS ELSE 0 END) BOT_QTY, 
		(CASE WHEN td_bsflag = 'B' THEN td_Rate ELSE 0 END) BOT_MKT_RATE, 
		(CASE WHEN td_bsflag = 'B' THEN VALUES_NS ELSE 0 END) BOT_VALUE, 
		(CASE WHEN td_bsflag = 'S' THEN td_dt ELSE '19000101' END) SOLD_TRXN_DATE, 
		(CASE WHEN td_bsflag = 'S' THEN QTY_NS ELSE 0 END) AS SOLD_QTY, 
		(CASE WHEN td_bsflag = 'S' THEN td_Rate ELSE  0 END) AS SOLD_MKT_RATE, 
		(CASE WHEN td_bsflag = 'S' THEN  VALUES_NS ELSE  0 END) AS SOLD_VALUE, 
			BuySTT =  (CASE WHEN td_bsflag = 'B' THEN td_stt ELSE 0 END),
			SaleSTT =  (CASE WHEN td_bsflag = 'S' THEN td_stt ELSE 0 END)
	FROM #tbl_DelvTrxn
	WHERE ISNULL(SQR_TAG, 'N') = 'N' AND ISNULL(QTY_NS, 0) <> 0 AND td_bsflag <> 'S'
	) x1
  GROUP BY XTAG111, CLIENT_CODE, SCRIP_CODE, convert(VARCHAR, cast(BOT_TRXN_DATE AS DATE), 106), convert(VARCHAR
		, cast(SOLD_TRXN_DATE AS DATE), 106)
  ORDER BY CLIENT_CODE, BOT_TRXN_DATE, SOLD_TRXN_DATE
 */

  -- INSERT SHORT SALES

  INSERT INTO #tbl_Rep (
	Tag, ClientCode, Scrip_Code, XTAG11, BuyDate, BuyQty, BuyRate, BuyValue, SaleDate, SaleQty, SaleRate, 
	SaleValue, BuySTT, SaleSTT
	)
  SELECT XTAG111, CLIENT_CODE, SCRIP_CODE, XTAG11 = 0, convert(VARCHAR, CAST(BOT_TRXN_DATE AS DATE), 106) 
	BOT_TRXN_DATE, SUM(BOT_QTY) BOT_QTY, 
	(CASE WHEN SUM(BOT_QTY) = 0 THEN 0 ELSE round(SUM(BOT_VALUE) / SUM(BOT_QTY),6) END) AS BOT_MKT_RATE, 
	SUM(BOT_VALUE) BOT_VALUE, convert(VARCHAR, CAST(SOLD_TRXN_DATE AS DATE), 106) SOLD_TRXN_DATE, 
	SUM(SOLD_QTY) SOLD_QTY, (CASE WHEN SUM(SOLD_QTY) = 0 THEN 0 ELSE round(SUM(SOLD_VALUE) / SUM(SOLD_QTY),6) END) SOLD_MKT_RATE, 
	SUM(SOLD_VALUE) SOLD_VALUE, SUM(BuySTT) AS BuySTT, SUM(SaleSTT) AS SaleSTT
  FROM (SELECT 'Short Sale' XTAG111, CLIENT_CODE = td_clientcd, SCRIP_CODE = td_scripcd, XTAG11, 
     (CASE WHEN td_bsflag = 'B' THEN td_dt ELSE '19000101' END) BOT_TRXN_DATE, 
		(CASE WHEN td_bsflag = 'B' THEN QTY_NS ELSE 0 END) BOT_QTY, 
		(CASE WHEN td_bsflag = 'B' THEN td_Rate ELSE 0 END) AS BOT_MKT_RATE, 
		(CASE WHEN td_bsflag = 'B' THEN VALUES_NS ELSE 0 END) AS BOT_VALUE, 
		(CASE WHEN td_bsflag = 'S' THEN td_dt ELSE '19000101' END) AS SOLD_TRXN_DATE, 
		(CASE WHEN td_bsflag = 'S' THEN QTY_NS ELSE 0 END) AS SOLD_QTY, 
		(CASE WHEN td_bsflag = 'S' THEN td_Rate ELSE 0 END) AS SOLD_MKT_RATE, 
		(CASE WHEN td_bsflag = 'S' THEN VALUES_NS ELSE 0 END) AS SOLD_VALUE, 
		BuySTT =  (CASE WHEN td_bsflag = 'B' THEN td_stt ELSE 0 END),
		SaleSTT =  (CASE WHEN td_bsflag = 'S' THEN td_stt ELSE 0 END)
	FROM #tbl_DelvTrxn2
	WHERE ISNULL(QTY_NS, 0) <> 0
	) x1
  GROUP BY XTAG111, CLIENT_CODE, SCRIP_CODE, convert(VARCHAR, cast(BOT_TRXN_DATE AS DATE), 106), convert(VARCHAR
		, cast(SOLD_TRXN_DATE AS DATE), 106)
  ORDER BY CLIENT_CODE, BOT_TRXN_DATE, SOLD_TRXN_DATE
  
  
   INSERT INTO #tbl_Rep (Tag, ClientCode, Scrip_Code, XTAG11, SaleDate, SaleQty, SaleRate, 
	SaleValue, BuyDate, BuyQty, BuyRate, BuyValue, BuySTT, SaleSTT, BuyBackFlag)
  SELECT Tag = 'Short Sale', CLIENT_CODE, SCRIP_CODE, XTAG11, CONVERT(VARCHAR, MAX(cast(BOT_TRXN_DATE AS DATE
			)), 106) BOT_TRXN_DATE, SUM(BOT_QTY) BOT_QTY, (CASE WHEN SUM(BOT_QTY) = 0 THEN 0 ELSE round(SUM(BOT_VALUE) / SUM(BOT_QTY),6) END) 
			BOT_MKT_RATE, SUM(BOT_VALUE) BOT_VALUE, BuyDate = '01-JAN-1900', BuyQty = 0, BuyRate = 0, BuyValue = 0, BuySTT = 0,
		SaleSTT = SUM(abs(BOT_QTY)*TD_STT), BuyBackFlag
  FROM (
	SELECT td_clientcd AS Client_Code, SCRIP_CODE = td_scripcd, XTAG11, td_dt AS BOT_TRXN_DATE, QTY_NS BOT_QTY, 
		td_Rate BOT_MKT_RATE, VALUES_NS BOT_VALUE, TD_STT, BuyBackFlag = CASE WHEN se_type='T' THEN 'Y' ELSE 'N' END
	FROM #tbl_DelvTrxn LEFT OUTER JOIN Settlements(NOLOCK) ON(td_Stlmnt = se_stlmnt)
	WHERE td_bsflag = 'S'  AND isnull(SQR_TAG, 'N') IN('N','')
  	) X123
  GROUP BY CLIENT_CODE, SCRIP_CODE, XTAG11, BuyBackFlag
  
  --- COMMENT VAIBHAVGARG
  
  IF ISNULL(@strShowPos,'N') ='Y'
  BEGIN
    INSERT INTO #tbl_Rep (Tag, ClientCode, Scrip_Code, XTAG11, BuyDate, BuyQty, BuyRate, BuyValue, BuySTT)
    SELECT Tag = 'Delivery Trxn', CLIENT_CODE, SCRIP_CODE, XTAG11, CONVERT(VARCHAR, MAX(cast(BOT_TRXN_DATE AS DATE
			)), 106) BOT_TRXN_DATE, SUM(BOT_QTY) BOT_QTY, (CASE WHEN SUM(BOT_QTY) = 0 THEN 0 ELSE 
			round(SUM(BOT_VALUE) / SUM(BOT_QTY),6) END) BOT_MKT_RATE, SUM(BOT_VALUE) BOT_VALUE, BuySTT = SUM(abs(BOT_QTY)*TD_STT)
    FROM (
	SELECT td_clientcd AS Client_Code, SCRIP_CODE = td_scripcd, XTAG11, td_dt AS BOT_TRXN_DATE, QTY_NS BOT_QTY, 
		td_Rate BOT_MKT_RATE, VALUES_NS BOT_VALUE, TD_STT
	FROM #tbl_DelvTrxn
	WHERE td_bsflag = 'B' AND ISNULL(SQR_TAG,'') = '' AND QTY_NS <> 0
  	) X123
    GROUP BY CLIENT_CODE, SCRIP_CODE, XTAG11
  END	
	  
  
  SET DATEFORMAT DMY
  
  IF ISNULL(@strConsider112A,'N') = 'N'
  BEGIN
    /*
    SELECT A.*, A.BuyRate, Round(mk_Rate, 4), Tmp_112ARate, 
	Round(mk_Rate, 4)
     FROM #tbl_Rep A, Market_Rates20180131 ,Securities
     WHERE Scrip_Code = ss_cd
	 AND Scrip_Code = mk_scripcd
	 AND CAST(BuyDate AS DATE) <= '20180131'
	 AND CAST(SaleDate AS DATE) > '20180331'
	 AND DateDiff(d, CAST(BuyDate AS DATE), CAST(SaleDate AS DATE)) >= 365
	 AND Round(mk_Rate, 4) > (BuyRate + (CASE WHEN BuySTT <> 0 and BuyQty <> 0 then round(BuySTT/BuyQty,2) else 0 end ))
	 AND ss_chargestt = 'Y' AND SS_CD='532296'
	 AND (SaleRate) > (BuyRate)	
  */
    UPDATE #tbl_Rep
    SET BuyRate = Round(mk_Rate, 4)
	  ,Tmp_112ARate = Round(mk_Rate, 4)
	  ,BuySTT = 0, T112AFlag = 'Y'
     FROM Market_Rates20180131
	 ,Securities
     WHERE Scrip_Code = ss_cd
	 AND Scrip_Code = mk_scripcd
	 AND CAST(BuyDate AS DATE) <= '20180131'
	 AND CAST(SaleDate AS DATE) > '20180331'
	 AND DateDiff(d, CAST(BuyDate AS DATE), CAST(SaleDate AS DATE)) >= 365
	 AND Round(mk_Rate, 4) > (BuyRate + (CASE WHEN BuySTT <> 0 and BuyQty <> 0 then round(BuySTT/BuyQty,2) else 0 end ))
	 AND ss_chargestt = 'Y'
	 AND SaleRate > BuyRatE
  

  UPDATE #tbl_Rep
  SET Tmp_112ARate = Round(mk_Rate , 4)
  FROM Market_Rates20180131
	,Securities
  WHERE Scrip_Code = ss_cd
	AND Scrip_Code = mk_scripcd	
  END	  
  ELSE
  BEGIN
    UPDATE A SET A.BuyBackFlag = 'N', T112AFlag = 'N'
	FROM #tbl_Rep A
  END

  	
  SET DATEFORMAT MDY
 
  -- UPDATE DAYS

  UPDATE A
  SET A.DAYS = CAST(ISNULL((CASE WHEN (
					cast(isnull(BuyDate, '01-jan-1900') AS DATETIME) < 3 OR Cast(isnull(SaleDate, '01-jan-1900') AS DATETIME) 
					< 3
					) THEN 0 ELSE DATEDIFF(DAY, CAST(BuyDate AS DATE), CAST(SaleDate AS DATE)) END), 0) AS INT)
  FROM #tbl_Rep A
  WHERE Tag = 'Delivery Trxn'


  UPDATE A SET A.CLientName = CM.cm_name, A.ScripName = SC.SS_NAME
  FROM #tbl_Rep A, Client_master CM, Securities SC
  WHERE A.ClientCode = CM.CM_CD AND A.Scrip_Code = SC.ss_cd
  
  UPDATE A
  SET A.SP_TERM_PL = (CASE WHEN ISNULL(A.DAYS, 0) = 0 
  AND (cast(isnull(BuyDate, '01-jan-1900') AS DATETIME) > 3 AND Cast(isnull(SaleDate, '01-jan-1900') AS DATETIME) > 3) THEN  
  (SaleValue - BuyValue) ELSE 0 END), 
  A.S_TERM_PL = (CASE WHEN ISNULL(BuyBackFlag,'N')='Y' THEN 0 ELSE (CASE WHEN ISNULL(A.DAYS, 0) > 0 and ISNULL(A.DAYS, 0)< 365 
  THEN (SaleValue - BuyValue) ELSE 0 END) END), 
  A.L_TERM_PL = (CASE WHEN ISNULL(A.DAYS, 0) >= 365 THEN 
  CASE WHEN ((T112AFlag = 'Y') AND (SaleRate) < BuyRate) THEN 0 
  ELSE ((SaleRate - BuyRate) * BuyQty) END ELSE 0 END), 
  NetQty = BuyQty - ISNULL(abs(SaleQty),0),
  NetValue = CASE WHEN BuyQty - ISNULL(abs(SaleQty),0)<>0 THEN isnull(SaleValue,0) - ISNULL(BUYVALUE,0) ELSE 0 END
  FROM #tbl_Rep A
  
  
  --- UPDATE CMP
  
  UPDATE #tbl_Rep
  SET CMP = mk_closerate
  FROM Market_rates(NOLOCK)
  WHERE mk_scripcd = Scrip_Code AND mk_exchange = 'N' AND mk_dt = (
		SELECT max(mk_dt)
		FROM Market_rates
		WHERE mk_exchange = 'N' AND mk_scripcd = Scrip_Code   AND mk_dt <= @dtToDate
		)
	AND NetQty <> 0
	

  DECLARE @tbl_CapgainMain TABLE(SerialNo int identity(1,1),Tag VARCHAR(50), ClientCode VARCHAR(50),  
  CLientName VARCHAR(100), Scrip_Code  VARCHAR(50), ScripName  VARCHAR(100), BuyDate  VARCHAR(15), BuyQty Numeric(19,6), BuyRate Numeric(19,6), 
  BuyValue Numeric(19,6), SaleDate VARCHAR(15), SaleQty Numeric(19,6), SaleRate Numeric(19,6), SaleValue Numeric(19,6), A112A_Rate VARCHAR(20), DAYS INT, SP_TERM_PL MONEY, 
  S_TERM_PL MONEY, L_TERM_PL MONEY, NetQty MONEY, NetValue MONEY, CMP MONEY, NOT_PL MONEY, STT Money)
  
  INSERT INTO  @tbl_CapgainMain (Tag, ClientCode,  CLientName, Scrip_Code, ScripName, BuyDate, BuyQty, BuyRate, BuyValue, SaleDate, SaleQty, SaleRate, SaleValue, 
  A112A_Rate, DAYS, SP_TERM_PL,
  S_TERM_PL, L_TERM_PL, NetQty, NetValue, CMP, NOT_PL, Stt)
  SELECT Tag, ClientCode, CLientName, Scrip_Code, ScripName, BuyDate = CASE WHEN CONVERT(VARCHAR, CAST(BuyDate AS DATE)
			, 103) = '01/01/1900' THEN '' ELSE CONVERT(VARCHAR, CAST(BuyDate AS DATE), 103) END, BuyQty, BuyRate, BuyValue, SaleDate = 
	ISNULL((CASE WHEN CONVERT(VARCHAR, CAST(SaleDate AS DATE), 103) = '01/01/1900' THEN '' ELSE CONVERT(VARCHAR, CAST(SaleDate AS DATE), 
			103) END),''), SaleQty = ISNULL(abs(SaleQty),0), SaleRate = ABS(ISNULL(SaleRate,0)), 
			SaleValue = ISNULL(SaleValue,0), 
			A112A_Rate = ISNULL(CAST(Tmp_112ARate AS VARCHAR)+
			(CASE WHEN ISNULL(BuyBackFlag,'N') = 'N' THEN 
			(CASE WHEN T112AFlag='Y' THEN '*' ELSE '' END) ELSE '#' END),''),
			DAYS = ISNULL(DAYS, 0), SP_TERM_PL = ISNULL(SP_TERM_PL, 0)
	, S_TERM_PL = ISNULL(S_TERM_PL, 0), L_TERM_PL = ISNULL(L_TERM_PL, 0), NetQty, NetValue,
	CMP = isnull(CMP, 0), NOT_PL = ISNULL(NetValue,0) + round(isnull(NetQty, 0) * isnull(CMP, 0), 2),
	STT =  ISNULL(BUYSTT,0)+ISNULL(SaleSTT,0)
    FROM #tbl_Rep X
    WHERE CONVERT(VARCHAR,CAST(BuyDate AS DATE), 103) <> '01/01/1900'
    ORDER BY CLIENTCODE, Scrip_Code, CAST(BuyDate AS DATE), CAST(SaleDate AS DATE) 
  
  INSERT INTO  @tbl_CapgainMain (Tag, ClientCode,  CLientName, Scrip_Code, ScripName, BuyDate, BuyQty, BuyRate, BuyValue, SaleDate, 
  SaleQty, SaleRate, SaleValue, A112A_Rate, DAYS, SP_TERM_PL,
  S_TERM_PL, L_TERM_PL, NetQty, NetValue, CMP, NOT_PL, Stt)
  SELECT Tag ='Short Sale', ClientCode, CLientName, Scrip_Code, ScripName, BuyDate = CASE WHEN CONVERT(VARCHAR, CAST(BuyDate AS DATE)
			, 103) = '01/01/1900' THEN '' ELSE CONVERT(VARCHAR, CAST(BuyDate AS DATE), 103) END, BuyQty, BuyRate = 0, BuyValue, SaleDate = 
  ISNULL((CASE WHEN CONVERT(VARCHAR, CAST(SaleDate AS DATE), 103) = '01/01/1900' THEN  '' ELSE CONVERT(VARCHAR, CAST(SaleDate AS DATE), 
			103) END),''), SaleQty = ISNULL(abs(SaleQty),0), SaleRate = abs(ISNULL(SaleRate,0)), SaleValue = ISNULL(SaleValue,0), 
			A112A_Rate = '',DAYS = ISNULL(DAYS, 0), SP_TERM_PL = ISNULL(SP_TERM_PL, 0)
	, S_TERM_PL = ISNULL(S_TERM_PL, 0), L_TERM_PL = ISNULL(L_TERM_PL, 0), NetQty, NetValue, CMP = isnull(CMP, 0), 
	NOT_PL = ISNULL(NetValue,0) + round(isnull(NetQty, 0) * isnull(CMP, 0), 2),
	BuySTT =  BUYSTT+SaleSTT
    FROM #tbl_Rep X
    WHERE CONVERT(VARCHAR,CAST(BuyDate AS DATE), 103) = '01/01/1900' AND abs(SaleQty) > 0
    ORDER BY CLIENTCODE, Scrip_Code, CAST(BuyDate AS DATE), CAST(SaleDate AS DATE) 
  
  DROP TABLE #tbl_DelvTrxn
  DROP TABLE #tbl_DelvTrxn1
  DROP TABLE #tbl_DelvTrxn2
  DROP TABLE #tbl_Rep
  
  SET @XMLDATA1 =''
  --SELECT @strOutputType
  IF @strRepType = 'Actual PL_Detail'
  BEGIN
    IF @strOutputType = 'X'
	BEGIN
	  SET @XMLDATA1 = (SELECT * FROM @tbl_CapgainMain 
      ORDER BY SerialNo FOR XML PATH('CapGain'))
	  SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	  RETURN 1
	END
	ELSE
	BEGIN
      IF ISNULL(@strShortSale,'N') ='N'
	  BEGIN
	    SELECT * FROM @tbl_CapgainMain 
        ORDER BY SerialNo
	  END
      ELSE IF ISNULL(@strShortSale,'N') ='Y'
	  BEGIN
	    SELECT * FROM @tbl_CapgainMain WHERE Tag <> 'Short Sale'
        ORDER BY SerialNo
		
		SELECT *, CurrentValue = ISNULL(NetQty,0)*ISNULL(CMP,0) FROM @tbl_CapgainMain WHERE Tag = 'Short Sale'
        ORDER BY SerialNo
	  END
	  RETURN 1
	END  
  END
  ELSE IF  @strRepType = 'Actual PL_Summary'
  BEGIN
  	SET DATEFORMAT DMY
    IF @strOutputType = 'X'
	BEGIN
	  SET @XMLDATA1 = ( SELECT TAG, ClientCode,  CLientName, Scrip_Code, ScripName,
	  BuyQty = SUM(BuyQty), 
	  BuyRate =  ROUND((CASE WHEN SUM(BuyQty)<>0 THEN SUM(BuyValue)/SUM(BuyQty) ELSE 0 END),4), 
	  BuyValue = SUM(BuyValue), SaleQty = SUM(SaleQty), 
	  SaleRate = ROUND((CASE WHEN SUM(SaleQty)<>0 THEN SUM(SaleValue)/SUM(SaleQty) ELSE 0 END),4), 
	  SaleValue = SUM(SaleValue), SP_TERM_PL = SUM(SP_TERM_PL), 
	  S_TERM_PL = SUM(S_TERM_PL), L_TERM_PL = SUM(L_TERM_PL), 
	  NetQty = SUM(NetQty), NetValue = SUM(NetValue), CMP = MAX(CMP), NOT_PL = SUM(NOT_PL),
	  STT =  SUM(STT)
	  FROM(SELECT TAG, ClientCode,  CLientName, Scrip_Code, ScripName, 
	  BuyDate = CASE WHEN ISNULL(BuyDate,'') = '' THEN '01/01/1900' ELSE BuyDate END, 
	  BuyQty, BuyRate =CAST(BuyRate  AS Numeric(19,6)) , CAST(BuyValue  AS Numeric(19,6)) BuyValue , 
	  SaleDate, SaleQty, SaleRate, SaleValue, 
	  SP_TERM_PL, S_TERM_PL, L_TERM_PL, NetQty, NetValue, CMP, NOT_PL, STT, FromDate = CONVERT(VARCHAR, CAST(@dtFromDate AS DATE), 103)
	  FROM @tbl_CapgainMain X)  xmain
	  GROUP BY Tag, ClientCode,  CLientName, Scrip_Code, ScripName
	  ORDER BY ClientCode, CASE WHEN TAG <> 'Short Sale' THEN ScripName ELSE 'zzzz' END FOR XML PATH('CapGain'))
	  SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	  RETURN 1
	END
	ELSE
	BEGIN
	  SELECT TAG, ClientCode,  CLientName, Scrip_Code, ScripName,
	  BuyQty = SUM(BuyQty), 
	  BuyRate =  ROUND((CASE WHEN SUM(BuyQty)<>0 THEN SUM(BuyValue)/SUM(BuyQty) ELSE 0 END),4), 
	  BuyValue = SUM(BuyValue), SaleQty = SUM(SaleQty), 
	  SaleRate = ROUND((CASE WHEN SUM(SaleQty)<>0 THEN SUM(SaleValue)/SUM(SaleQty) ELSE 0 END),4), 
	  SaleValue = SUM(SaleValue), SP_TERM_PL = SUM(SP_TERM_PL), 
	  S_TERM_PL = SUM(S_TERM_PL), L_TERM_PL = SUM(L_TERM_PL), 
	  NetQty = SUM(NetQty), NetValue = SUM(NetValue), CMP = MAX(CMP), NOT_PL = SUM(NOT_PL),
	  STT =  SUM(STT)
	  FROM(SELECT TAG, ClientCode,  CLientName, Scrip_Code, ScripName, 
	  BuyDate = CASE WHEN ISNULL(BuyDate,'') = '' THEN '01/01/1900' ELSE BuyDate END, 
	  BuyQty, BuyRate =CAST(BuyRate  AS Numeric(19,6)) , CAST(BuyValue  AS Numeric(19,6)) BuyValue , 
	  SaleDate, SaleQty, SaleRate, SaleValue, 
	  SP_TERM_PL, S_TERM_PL, L_TERM_PL, NetQty, NetValue, CMP, NOT_PL, STT, FromDate = CONVERT(VARCHAR, CAST(@dtFromDate AS DATE), 103)
	  FROM @tbl_CapgainMain X)  xmain
	  GROUP BY Tag, ClientCode,  CLientName, Scrip_Code, ScripName
	  ORDER BY ClientCode, CASE WHEN TAG<>'Short Sale' THEN ScripName ELSE 'zzzz' END
	  RETURN 1
	END  
	SET DATEFORMAT MDY
  END
  
  SET @o_vcErrorFlag  = 'S'
 -- SET @o_vcErrorMessage = 'Process Completed'
  RETURN 1
  
END
GO

CREATE   PROCEDURE [dbo].[stpr_Rpt_CapitalGainNotional] @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT 
WITH ENCRYPTION
AS
BEGIN
 /*
 ///////////////////////////////////////////////////////////////////////////////////////////
 // Create By     : VAIBHAV GARG
 // Created Date  : 22-FEB-2024
 // Description   : 
 // Reviewed By   : 
 // Review Date   : 
 //////////////////////////////////////////////////////////////////////////////////////////
*/
  DECLARE @dtFromDate VARCHAR(8), @strUserId VARCHAR(50), @strProduct VARCHAR(50), 
  @strOutputType VARCHAR(1)='', @XMLData XML,
  @strSelectTag VARCHAR(1)='', @strSelectUsers VARCHAR(500)='', @StrString VARCHAR(MAX)='', @SQ1 INT = 0,
  @strSplFilter VARCHAR(MAX)='', @strRepType VARCHAR(50)='', @dtToDate VARCHAR(8)='', @strRepSubType VARCHAR(50), @strConsider112A VARCHAR(1)='N',
  @strScripCode VARCHAR(20)=''
  IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
  END 

  SET @XMLData = CAST('<root>'+@vcXML+'</root>' AS XML)
  
  SELECT @dtFromDate = ISNULL(x.value('(FromDt)[1]', 'VARCHAR(8)'),''),
  @strProduct = ISNULL(x.value('(Product)[1]', 'VARCHAR(50)'),''),
  @dtToDate = ISNULL(x.value('(ToDt)[1]', 'VARCHAR(8)'),''),
  @strUserId = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'),''),
  @strSelectTag = ISNULL(x.value('(SelectTag)[1]', 'VARCHAR(1)'),''),
  @strSelectUsers = ISNULL(x.value('(SelectUsers)[1]', 'VARCHAR(500)'),''),
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'),''),
  @strSplFilter = ISNULL(x.value('(SplFilter)[1]', 'VARCHAR(MAX)'),''),
  @strRepType = ISNULL(x.value('(RepType)[1]', 'VARCHAR(100)'),''),
  @strRepSubType = ISNULL(x.value('(RepSubType)[1]', 'VARCHAR(100)'),''),
  @strConsider112A = ISNULL(x.value('(Option112A)[1]', 'VARCHAR(1)'),''),
  @strScripCode = ISNULL(x.value('(ScripCode)[1]', 'VARCHAR(20)'),'')  
  FROM @XMLData.nodes('/root') AS XTbl(x) 

 
  IF @strRepType = ''
  begin
    SET @strRepType = 'Notional_Detail'
  END
  
  if ISNULL(@strConsider112A,'') = ''
  BEGIN
    SET @strConsider112A ='N'
  END
  
  DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50)) 
  
  	
  IF @strSplFilter = ''
  BEGIN
    INSERT INTO @tbl_UserList(Client_Code) 
    SELECT * FROM DBO.[fn_GetClients](@strUserId,@strSelectTag,@strSelectUsers)
  END 	 
  ELSE
  IF @strSplFilter <> ''
  BEGIN
    SET @StrString  = ' SELECT distinct CM_CD FROM Client_master(NOLOCK) WHERE 1 = 1  AND '+@strSplFilter
	INSERT INTO @tbl_UserList(Client_Code) 
	EXEC(@StrString)
  END	
  
  SET @StrString = ''
  
  IF OBJECT_ID('tempdb..#tbl_DelvTrxn') IS NOT NULL
    DROP TABLE #tbl_DelvTrxn
  IF OBJECT_ID('tempdb..#tbl_DelvTrxn1') IS NOT NULL
    DROP TABLE #tbl_DelvTrxn1
  IF OBJECT_ID('tempdb..#TrxSummaryDLV1') IS NOT NULL
    DROP TABLE #TrxSummaryDLV1
  IF OBJECT_ID('tempdb..#TrxDLV1') IS NOT NULL
    DROP TABLE #TrxDLV1
  IF OBJECT_ID('tempdb..#tbl_CloseRate') IS NOT NULL
    DROP TABLE #tbl_CloseRate

  CREATE TABLE #tbl_DelvTrxn1 (
	td_SRNO INT, td_dt VARCHAR(8), td_Stlmnt VARCHAR(20), td_clientcd VARCHAR(20), td_scripcd VARCHAR(20), 
	td_bsflag VARCHAR(1), Qty_NS numeric(19,6), VALUES_NS numeric(19,6), td_Rate numeric(19,6),
	XTAG11 INT, LONG_TAG VARCHAR(1), SQR_TAG VARCHAR(1), Tmp_RefNo numeric, td_Filler1 VARCHAR(8), td_stt MONEY
  )

  CREATE TABLE #tbl_DelvTrxn (
  SerialNo int identity(1,1), td_SRNO INT, td_dt VARCHAR(8), td_Stlmnt VARCHAR(20), td_clientcd VARCHAR(20), td_scripcd VARCHAR(20), 
	td_bsflag VARCHAR(1), Qty_NS numeric(19,6), VALUES_NS numeric(19,6), td_Rate numeric(19,6), 
	XTAG11 INT, LONG_TAG VARCHAR(1), SQR_TAG VARCHAR(1), Tmp_RefNo numeric, td_Filler1 VARCHAR(8), td_stt MONEY
  )
	  
  	  
  INSERT INTO #tbl_DelvTrxn1 (
	td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_NS, VALUES_NS, td_Rate, Tmp_RefNo, td_Filler1, td_stt
	)
    SELECT X.*
    FROM (
    SELECT td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, 
	Qty_NS = ABS(CASE WHEN td_TRDType = 'DL' AND td_Filler2  <> 'T' THEN td_bqty - td_Sqty ELSE 0 END), 
	VALUES_NS = (CASE WHEN td_TRDType = 'DL'  AND td_Filler2  <> 'T' THEN (CASE WHEN td_bsflag = 'B' THEN 
	td_Rate + Round(td_ServiceTax + td_OtherChrgs1 + td_OtherChrgs2, 6) ELSE 
	td_Rate - Round(td_ServiceTax + td_OtherChrgs1 + td_OtherChrgs2, 6) END) ELSE 0 END) * 
			ABS(CASE WHEN td_TRDType = 'DL'   AND td_Filler2  <> 'T' THEN  td_bqty - td_Sqty ELSE 0 END), 
		td_Rate = (CASE WHEN td_bsflag = 'B' THEN td_Rate + Round(td_ServiceTax + 
		td_OtherChrgs1 + td_OtherChrgs2, 6) ELSE td_Rate - Round(td_ServiceTax + td_OtherChrgs1 + td_OtherChrgs2, 6) END), 
		td_NFiller2, td_Filler1, td_stt
    FROM TRX_INVPL(NOLOCK), Client_master(NOLOCK), Branch_master(NOLOCK), @tbl_UserList X
    WHERE cm_cd = td_clientcd AND cm_brboffcode = bm_branchcd AND td_dt <= @dtToDate --and td_Filler2 <> 'T'
    AND cm_cd  = X.CLIENT_CODE AND td_TRDType = 'DL'   AND td_Filler2  <> 'T') X
	WHERE ((td_scripcd = @strScripCode AND @strScripCode <> '') OR @strScripCode = '')
	
    CREATE INDEX indx_DelvTrxntd_SRNO ON #tbl_DelvTrxn1 (td_SRNO, td_Filler1) 
	 
	DECLARE @UPDtd_SRNO INT, @updTmp_RefNo INT
    DECLARE CursorUpdatedate CURSOR
    FOR
    SELECT td_SRNO, Tmp_RefNo
    FROM #tbl_DelvTrxn1 x where Tmp_RefNo > 0
  
    OPEN CursorUpdatedate
    FETCH NEXT
    FROM CursorUpdatedate
    INTO @UPDtd_SRNO, @updTmp_RefNo
    WHILE @@FETCH_STATUS = 0
    BEGIN 
      UPDATE A SET a.Qty_NS = a.Qty_NS - b.Qty_NS
	  FROM #tbl_DelvTrxn1 A, #tbl_DelvTrxn1 B  
      WHERE A.td_SRNO = @UPDtd_SRNO and b.td_SRNO  = @updTmp_RefNo
	
	  DELETE FROM #tbl_DelvTrxn1 WHERE td_SRNO = @updTmp_RefNo
	
      FETCH NEXT FROM CursorUpdatedate
	  INTO @UPDtd_SRNO, @updTmp_RefNo
    END
    CLOSE CursorUpdatedate
    DEALLOCATE CursorUpdatedate
  
    DELETE FROM #tbl_DelvTrxn1 WHERE Qty_NS = 0
  
    UPDATE #tbl_DelvTrxn1 SET td_dt = td_Filler1 WHERE td_Filler1 NOT IN('','0')
	

    CREATE INDEX indx_DelvTrxn1 ON #tbl_DelvTrxn1 (td_clientcd, td_scripcd, td_SRNO) 
	CREATE INDEX indx_DelvTrxn ON #tbl_DelvTrxn (td_clientcd, td_scripcd, td_SRNO) 
	CREATE INDEX indx_DelvTrxnS ON #tbl_DelvTrxn (td_scripcd) 


    insert into #tbl_DelvTrxn(td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_NS, VALUES_NS, td_Rate, td_stt)
    SELECT td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_NS, VALUES_NS, td_Rate, td_stt FROM #tbl_DelvTrxn1 xmain 
	WHERE EXISTS(
    SELECT 1 FROM(
    SELECT td_clientcd, td_scripcd, Qty_NS = SUM(CASE WHEN td_bsflag='B' THEN Qty_NS ELSE Qty_NS*-1 END)  
    FROM #tbl_DelvTrxn1
    GROUP BY td_clientcd, td_scripcd
    HAVING SUM(CASE WHEN td_bsflag='B' THEN Qty_NS ELSE Qty_NS*-1 END) <> 0) x1 
    where x1.td_clientcd = xmain.td_clientcd
    and x1.td_scripcd = xmain.td_scripcd)


    
	CREATE TABLE #TrxSummaryDLV1 (clientcd VARCHAR(8) NOT NULL, scripcd VARCHAR(6) NOT NULL, Buys MONEY, Sells MONEY)
	
	INSERT INTO #TrxSummaryDLV1
	SELECT td_clientcd, td_scripcd, Buys, Sells
    FROM (SELECT td_clientcd, cm_name, td_scripcd, ss_name, 
    ss_nsymbol, ss_nseries, sum(CASE WHEN td_bsflag='B' THEN Qty_ns ELSE 0 END) AS 'Buys', 
	sum(CASE WHEN td_bsflag='S' THEN Qty_ns ELSE 0 END) 'Sells', 
	sum(CASE WHEN td_bsflag='B' THEN Qty_ns ELSE 0 END) - sum(CASE WHEN td_bsflag='S' THEN Qty_ns ELSE 0 END) AS 'Delivery', 
    count(*) Totalrec, ss_group, cm_type
    FROM #tbl_DelvTrxn, Client_master, Securities
    WHERE cm_cd = td_clientcd AND td_scripcd = ss_cd
    GROUP BY td_clientcd, cm_name, td_scripcd, ss_cd, ss_name, ss_nsymbol, ss_nseries, ss_group, cm_type
    HAVING sum(CASE WHEN td_bsflag='B' THEN Qty_ns ELSE 0 END) - sum(CASE WHEN td_bsflag='S' THEN Qty_ns ELSE 0 END) <> 0) a

	CREATE TABLE #TrxDLV1 (SrNo NUMERIC, Qty NUMERIC, FinalQty NUMERIC)
    CREATE INDEX indx_SrNo ON #TrxDLV1 (SrNo) 
	
	INSERT INTO #TrxDLV1
	SELECT SerialNo, Qty, CASE WHEN NetQty >= Running THEN Qty ELSE NetQty - isNull(PrevRunning, 0) END FinalQty
    FROM (
    SELECT SerialNo, td_clientcd, td_scripcd, td_marketrate,  NetQty, Qty, Running, LAG(
		Running) OVER (
		PARTITION BY td_clientcd, td_scripcd ORDER BY td_dt DESC,  SerialNo DESC
		) PrevRunning
    FROM (
	SELECT SerialNo, td_dt, td_clientcd, td_scripcd, Qty_ns Qty, td_marketrate = td_Rate,  abs(Buys - Sells) NetQty, Sum(Qty_ns) OVER (
			PARTITION BY td_clientcd, td_scripcd ORDER BY td_dt DESC,  SerialNo DESC
			) Running
	FROM #tbl_DelvTrxn, Client_master, Securities, #TrxSummaryDLV1
	WHERE cm_cd = td_clientcd AND td_scripcd = ss_cd
		AND td_clientcd = clientcd AND td_scripcd = scripcd AND td_bsflag = CASE WHEN Buys > Sells THEN 'B' ELSE 'S' END) a
    ) b
    WHERE CASE WHEN NetQty >= Running THEN Qty ELSE NetQty - isNull(PrevRunning, 0) END > 0

   ALTER TABLE #tbl_DelvTrxn ADD Tmp_112ARate MONEY, T112AFlag VARCHAR(1) NOT NULL DEFAULT 'N', ActualBuyRate MONEY NOT NULL DEFAULT 0
   
   INSERT INTO #tbl_DelvTrxn(td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, Qty_NS, VALUES_NS, td_Rate, td_stt)
   SELECT td_SRNO, td_dt, td_Stlmnt, td_clientcd, td_scripcd, td_bsflag, 
   Qty - FinalQty, VALUES_NS = (Qty - FinalQty)*td_Rate, td_Rate, td_stt
   FROM #tbl_DelvTrxn , #TrxDLV1
   WHERE SerialNo = srno AND (Qty - FinalQty) > 0


   UPDATE #tbl_DelvTrxn
   SET Qty_NS = FinalQty, VALUES_NS = FinalQty*td_Rate, SQR_TAG = 'Y' 
   FROM #TrxDLV1
   WHERE SerialNo = srno  AND td_bsflag = 'B'

   UPDATE #tbl_DelvTrxn
   SET Qty_NS = FinalQty, VALUES_NS = FinalQty*td_Rate, SQR_TAG = 'Y' 
   FROM #TrxDLV1
   WHERE SerialNo = srno and td_bsflag = 'S'

   DELETE #tbl_DelvTrxn
   WHERE Qty_NS = 0
   
   UPDATE #tbl_DelvTrxn SET ActualBuyRate = td_Rate
   
   SET DATEFORMAT DMY
  
   IF ISNULL(@strConsider112A,'N') = 'N'
   BEGIN
     UPDATE #tbl_DelvTrxn
     SET td_Rate = Round(mk_Rate, 4)
	  ,Tmp_112ARate = Round(mk_Rate, 4),
	  T112AFlag = 'Y'
     FROM Market_Rates20180131
	 ,Securities
     WHERE td_scripcd = ss_cd
	 AND td_scripcd = mk_scripcd
	 AND CAST(td_dt AS DATE) <= '20180131'
	 AND CAST(@dtToDate AS DATE) > '20180331'
	 AND DateDiff(d, CAST(td_dt AS DATE), CAST(@dtToDate AS DATE)) >= 365
	 AND Round(mk_Rate, 4) > (td_Rate)
	 AND ss_chargestt = 'Y'
	 
	 UPDATE #tbl_DelvTrxn
     SET Tmp_112ARate = Round(mk_Rate , 4)
     FROM Market_Rates20180131 ,Securities
     WHERE td_scripcd = ss_cd
	 AND td_scripcd = mk_scripcd	
  END	  
  ELSE
  BEGIN
    UPDATE A SET T112AFlag = 'N'
	FROM #tbl_DelvTrxn A
  END
  
  SET DATEFORMAT MDY


   declare @tbl_HoldingRep TABLE(td_SRNO INT, ClientCode VARCHAR(50), TradeDate VARCHAR(8), td_Stlmnt VARCHAR(20),
   ScripCode VARCHAR(10), BuyQty MONEY, BuyRate MONEY,
   BuyValue MONEY, SaleQty MONEY, SaleRate MONEY,
   SaleValue MONEY, CloseRate MONEY, CurrentValue MONEY, ShortTerm MONEY, LongTerm MONEY, PositionDays INT, Tmp_112ARate MONEY, 
   T112AFlag VARCHAR(1), ActualBuyRate MONEY, SqrTag VARCHAR(1), td_stt MONEY)

   INSERT INTO @tbl_HoldingRep(td_SRNO, ClientCode, TradeDate, td_Stlmnt, ScripCode, BuyQty, BuyRate, BuyValue, SaleQty, SaleRate, 
   SaleValue, Tmp_112ARate, T112AFlag, ActualBuyRate, SqrTag, td_stt)
   
   SELECT td_SRNO, td_clientcd, td_dt, td_Stlmnt, td_scripcd, BuyQty = CASE WHEN td_bsflag='B' THEN Qty_NS ELSE 0 END, 
   Rate = td_rate,  Cost = (CASE WHEN td_bsflag='B' THEN td_rate*Qty_NS ELSE 0.00 END), 
   SaleQty = CASE WHEN td_bsflag = 'S' THEN Qty_NS ELSE 0 END, 
   Rate = td_rate,  Cost = (CASE WHEN td_bsflag='S' THEN td_rate*Qty_NS ELSE 0.00 END),
   ISNULL(Tmp_112ARate,0), T112AFlag, ActualBuyRate, ISNULL(SQR_TAG,'N'), td_stt
   FROM #tbl_DelvTrxn WHERE ISNULL(SQR_TAG,'N')='Y' and CASE WHEN td_bsflag='B' THEN Qty_NS ELSE 0 END > 0
   order by td_scripcd
   
   
   DECLARE @CH_ClgHs VARCHAR(1)=''

   SELECT @CH_ClgHs = (CASE WHEN CH_ClgHs = 'I' THEN 'B' ELSE CH_ClgHs END) FROM ClearingHouse(NOLOCK)
   WHERE CH_CompanyCode = 'A' AND CH_Segment = 'C' 
   AND CH_EffDt = (SELECT Min(CH_EffDt) FROM ClearingHouse
   WHERE CH_CompanyCode = 'A' AND CH_Segment = 'C' AND CH_EffDt <= @dtToDate) 

   CREATE TABLE #tbl_CloseRate (Scrip VARCHAR(20), CloseRate MONEY)
   
   CREATE INDEX INDX_CloseRate ON #tbl_CloseRate (Scrip) 
	
   INSERT INTO #tbl_CloseRate(Scrip, CloseRate)
   SELECT DISTINCT ScripCode, '' AS CloseRate FROM @tbl_HoldingRep

   IF @CH_ClgHs = 'N'
   BEGIN
	 UPDATE A SET A.CloseRate = B.mk_closerate
	 FROM #tbl_CloseRate A, Market_rates(NOLOCK) B 
	 WHERE A.Scrip = B.mk_scripcd AND mk_dt = (select  max(mk_dt) from Market_rates(NOLOCK)  
	 WHERE mk_exchange = 'B' AND mk_scripcd = B.mk_scripcd and mk_dt<=@dtToDate) AND  mk_exchange = 'B'
	  
	 UPDATE A SET A.CloseRate = B.mk_closerate
	 FROM #tbl_CloseRate A, Market_rates(NOLOCK) B 
	 WHERE A.Scrip = B.mk_scripcd 
	 AND mk_dt = (select  max(mk_dt) from Market_rates(NOLOCK)  
	 WHERE mk_exchange = 'N' AND mk_scripcd = B.mk_scripcd 
	 and mk_dt<=@dtToDate) AND  mk_exchange = 'N'
   END  
   ELSE
   IF @CH_ClgHs = 'B'
   BEGIN
	 UPDATE A SET A.CloseRate = B.mk_closerate
	 FROM #tbl_CloseRate A, Market_rates(NOLOCK) B 
	 WHERE A.Scrip = B.mk_scripcd AND mk_dt = (select  max(mk_dt) from Market_rates(NOLOCK)  
	 WHERE mk_exchange = 'N' AND mk_scripcd = B.mk_scripcd and mk_dt<=@dtToDate) AND  mk_exchange = 'N'
	  
	 UPDATE A SET A.CloseRate = B.mk_closerate
	 FROM #tbl_CloseRate A, Market_rates(NOLOCK) B 
	 WHERE A.Scrip = B.mk_scripcd 
	 AND mk_dt = (select  max(mk_dt) from Market_rates(NOLOCK)  
	 WHERE mk_exchange = 'B' AND mk_scripcd = B.mk_scripcd 
	 and mk_dt<=@dtToDate) AND  mk_exchange = 'B'
   END  
   
   UPDATE A SET A.CloseRate = B.mk_closerate
   FROM #tbl_CloseRate A, Market_rates B
   WHERE mk_dt = (select  max(mk_dt) from Market_rates(NOLOCK)  
   WHERE mk_scripcd = B.mk_scripcd and mk_dt<=@dtToDate)
   AND A.Scrip = B.mk_scripcd	
   AND A.CloseRate = 0

   UPDATE A SET A.CloseRate = B.CloseRate, CurrentValue = BuyQty*B.CloseRate
   FROM @tbl_HoldingRep A, #tbl_CloseRate B
   WHERE A.ScripCode = B.Scrip
   DECLARE @XMLDATA1 XML
   IF @strRepType = 'Notional_Summary'
   BEGIN
     IF @strOutputType = 'X'
	 BEGIN
       SET @XMLDATA1 = (SELECT ClientCode, CLientName = M.cm_name, ScripCode, ScripName = SC.SS_NAME, BuyQty = SUM(BuyQty), 
	   BuyRate = ROUND((CASE WHEN SUM(BuyQty)<>0 THEN SUM(BuyValue)/SUM(BuyQty) ELSE 0 END),4), 
	   BuyValue = SUM(BuyValue), 
	   SaleQty = SUM(SaleQty), 
	   SaleRate = ROUND((CASE WHEN SUM(SaleQty)<>0 THEN SUM(SaleValue)/SUM(SaleQty) ELSE 0 END),2), 
	   SaleValue = SUM(SaleValue), 
	   CloseRate = MAX(CloseRate), CurrentValue = SUM(CurrentValue),
       ShortTerm = SUM(CASE WHEN SIGN(ABS(DATEDIFF(DAY, CAST(TradeDate AS DATE), CAST(getdate() AS DATE))) - 365) = 1 THEN '' ELSE CurrentValue - BuyValue
       END), 
	   LongTerm = SUM(CASE WHEN SIGN(ABS(DATEDIFF(DAY, CAST(TradeDate AS DATE), CAST(getdate() AS DATE))) - 365) = 1 THEN 
       CASE WHEN ((T112AFlag = 'Y') AND CloseRate < BuyRate) THEN 0 
       ELSE ((CloseRate - BuyRate) * BuyQty) END ELSE 0 END), T112AFlag, [A112A_Rate] = ISNULL(MAX(Tmp_112ARate),''),
	   [STT] = SUM((SaleQty+BuyQty)*td_stt)
       FROM @tbl_HoldingRep A, Client_master(NOLOCK) M, Securities(NOLOCK) SC  
       WHERE A.ClientCode = M.CM_CD AND A.ScripCode = SC.ss_cd
	   GROUP BY ClientCode, M.cm_name, ScripCode, SC.SS_NAME, T112AFlag
       ORDER BY ClientCode, ScripCode FOR XML PATH('CapGain'))
	   SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	 END
     ELSE
     BEGIN
	   SELECT ClientCode, CLientName = M.cm_name, ScripCode, ScripName = SC.SS_NAME, BuyQty = SUM(BuyQty), 
	   BuyRate = ROUND((CASE WHEN SUM(BuyQty)<>0 THEN SUM(BuyValue)/SUM(BuyQty) ELSE 0 END),4), 
	   BuyValue = SUM(BuyValue), 
	   SaleQty = SUM(SaleQty), 
	   SaleRate = ROUND((CASE WHEN SUM(SaleQty)<>0 THEN SUM(SaleValue)/SUM(SaleQty) ELSE 0 END),2), 
	   SaleValue = SUM(SaleValue), 
	   CloseRate = MAX(CloseRate), CurrentValue = SUM(CurrentValue),
       ShortTerm = SUM(CASE WHEN SIGN(ABS(DATEDIFF(DAY, CAST(TradeDate AS DATE), CAST(getdate() AS DATE))) - 365) = 1 THEN '' ELSE CurrentValue - BuyValue
       END), 
	   LongTerm = SUM(CASE WHEN SIGN(ABS(DATEDIFF(DAY, CAST(TradeDate AS DATE), CAST(getdate() AS DATE))) - 365) = 1 THEN 
       CASE WHEN ((T112AFlag = 'Y') AND CloseRate < BuyRate) THEN 0 
       ELSE ((CloseRate - BuyRate) * BuyQty) END ELSE 0 END), T112AFlag, [A112A_Rate] = ISNULL(MAX(Tmp_112ARate),''),
	   [STT] = SUM((SaleQty+BuyQty)*td_stt)
       FROM @tbl_HoldingRep A, Client_master(NOLOCK) M, Securities(NOLOCK) SC  
       WHERE A.ClientCode = M.CM_CD AND A.ScripCode = SC.ss_cd
	   GROUP BY ClientCode, M.cm_name, ScripCode,  SC.SS_NAME, T112AFlag 
       ORDER BY ClientCode, ScripCode 
     END  	 
   END
   ELSE
   IF @strRepType = 'Notional_Detail'
   BEGIN
     IF @strOutputType = 'X'
	 BEGIN
       SET @XMLDATA1 = ( SELECT ClientCode, CLientName = M.cm_name, ScripCode, ScripName = SC.SS_NAME, TradeDate, td_SRNO, td_Stlmnt = isnull(td_Stlmnt,''), BuyQty, 
	   ActualBuyRate AS BuyRate, BuyValue, CloseRate, CurrentValue,
       ShortTerm = (CASE WHEN SIGN(ABS(DATEDIFF(DAY, CAST(TradeDate AS DATE), CAST(getdate() AS DATE))) - 365) = 1 
	   THEN '' ELSE CurrentValue - BuyValue
       END), 
	 --LongTerm = (CASE WHEN SIGN(ABS(DATEDIFF(DAY, CAST(TradeDate AS DATE), CAST(getdate() AS DATE))) - 365) = 1 
	 --THEN CurrentValue - BuyValue ELSE '' 
     --END), 
	   LongTerm = (CASE WHEN SIGN(ABS(DATEDIFF(DAY, CAST(TradeDate AS DATE), CAST(getdate() AS DATE))) - 365) = 1 THEN 
       CASE WHEN ((T112AFlag = 'Y') AND CloseRate < BuyRate) THEN 0 
       ELSE ((CloseRate - BuyRate) * BuyQty) END ELSE 0 END), 
	   PositionDays = ABS(DATEDIFF(DAY, CAST(TradeDate AS DATE), CAST(getdate() AS DATE))),
	   T112AFlag, [A112A_Rate] = ISNULL(Tmp_112ARate,''), [STT] = (SaleQty+BuyQty)*td_stt
       FROM @tbl_HoldingRep A, Client_master(NOLOCK) M, Securities(NOLOCK) SC  
       WHERE A.ClientCode = M.CM_CD AND A.ScripCode = SC.ss_cd AND ISNULL(BuyQty,0) <> 0
       ORDER BY ClientCode, ScripCode, TradeDate FOR XML PATH('CapGain'))
	   SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
     END  
     ELSE
	 Begin
	  SELECT ClientCode, CLientName = M.cm_name, ScripCode, ScripName = SC.SS_NAME, TradeDate, BuyQty, 
	   ActualBuyRate AS BuyRate, BuyValue, CloseRate, CurrentValue,
       ShortTerm = (CASE WHEN SIGN(ABS(DATEDIFF(DAY, CAST(TradeDate AS DATE), CAST(getdate() AS DATE))) - 365) = 1 
	   THEN '' ELSE CurrentValue - BuyValue
       END), 
	 --LongTerm = (CASE WHEN SIGN(ABS(DATEDIFF(DAY, CAST(TradeDate AS DATE), CAST(getdate() AS DATE))) - 365) = 1 
	 --THEN CurrentValue - BuyValue ELSE '' 
     --END), 
	   LongTerm = (CASE WHEN SIGN(ABS(DATEDIFF(DAY, CAST(TradeDate AS DATE), CAST(getdate() AS DATE))) - 365) = 1 THEN 
       CASE WHEN ((T112AFlag = 'Y') AND CloseRate < BuyRate) THEN 0 
       ELSE ((CloseRate - BuyRate) * BuyQty) END ELSE 0 END), 
	   PositionDays = ABS(DATEDIFF(DAY, CAST(TradeDate AS DATE), CAST(getdate() AS DATE))),
	   T112AFlag, [A112A_Rate] = ISNULL(Tmp_112ARate,''), [STT] = (SaleQty+BuyQty)*td_stt
       FROM @tbl_HoldingRep A, Client_master(NOLOCK) M, Securities(NOLOCK) SC  
       WHERE A.ClientCode = M.CM_CD AND A.ScripCode = SC.ss_cd AND ISNULL(BuyQty,0) <> 0
       ORDER BY ClientCode, ScripCode, TradeDate
     END	 
    
	 --SET @o_vcErrorMessage = 'Process Completed'
   END	
   SET @o_vcErrorFlag  = 'S'
   RETURN 1   
END
GO

CREATE   PROCEDURE [dbo].[stpr_Rpt_OtherReports] @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(500) OUTPUT 
WITH ENCRYPTION
AS
BEGIN
   /*
   ///////////////////////////////////////////////////////////////////////////////////////////
   // Create By     : VAIBHAV GARG
   // Created Date  : 22-FEB-2024
   // Description   : 
   // Reviewed By   : 
   // Review Date   : 
   //////////////////////////////////////////////////////////////////////////////////////////
   */
  DECLARE @dtFromDate VARCHAR(8), @strUserId VARCHAR(50), @strReportName VARCHAR(50), 
  @strOutputType VARCHAR(1)='', @XMLData XML,
  @strSelectTag VARCHAR(1)='', @strSelectUsers VARCHAR(500)='', @StrString VARCHAR(MAX)='', @SQ1 INT = 0,
  @strSplFilter VARCHAR(MAX)='', @strRepType VARCHAR(50)='', @dtToDate VARCHAR(8)='', 
  @strRepSubType VARCHAR(50), @strConsider112A VARCHAR(1)='N', @strProduct VARCHAR(50)='', 
  @strHeader VARCHAR(MAX)='', @strCommmexDB VARCHAR(MAX)=''
  IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
  END 
  
  SET @XMLData = CAST('<root>'+@vcXML+'</root>' AS XML)
  
  SELECT @dtFromDate = ISNULL(x.value('(FromDt)[1]', 'VARCHAR(8)'),''),
  @strProduct = ISNULL(x.value('(Product)[1]', 'VARCHAR(50)'),''),
  @dtToDate = ISNULL(x.value('(ToDt)[1]', 'VARCHAR(8)'),''),
  @strUserId = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'),''),
  @strSelectTag = ISNULL(x.value('(SelectTag)[1]', 'VARCHAR(1)'),''),
  @strSelectUsers = ISNULL(x.value('(SelectUsers)[1]', 'VARCHAR(500)'),''),
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'),''),
  @strSplFilter = ISNULL(x.value('(SplFilter)[1]', 'VARCHAR(MAX)'),''),
  @strReportName = ISNULL(x.value('(ReportName)[1]', 'VARCHAR(100)'),''),
  @strRepType = ISNULL(x.value('(RepType)[1]', 'VARCHAR(100)'),''),
  @strRepSubType = ISNULL(x.value('(RepSubType)[1]', 'VARCHAR(100)'),'')
  FROM @XMLData.nodes('/root') AS XTbl(x) 
  
    
  SET @strHeader = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50), DPClientCode VARCHAR(50) ) '
  
  SELECT @strCommmexDB = LTRIM(RTRIM(OP_DataBase)) FROM Other_Products(NOLOCK) WHERE OP_Product = 'Commex'
  and OP_Status = 'A'	
	
	
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
 	
  IF @strSplFilter = ''
  BEGIN
    SET @strHeader = @strHeader+' INSERT INTO @tbl_UserList(Client_Code) '
    +' SELECT * FROM DBO.[fn_GetClients]('''+@strUserId+''','''+@strSelectTag+''','''+@strSelectUsers+''')'
  END 	 
  ELSE
  IF @strSplFilter <> ''
  BEGIN
    SET @strHeader = @strHeader+' INSERT INTO @tbl_UserList(Client_Code)  SELECT distinct CM_CD FROM Client_master(NOLOCK) WHERE 1 = 1  AND '+@strSplFilter
  END	
  SET @strHeader = @strHeader +' UPDATE A SET A.DPClientCode = B.da_actno FROM @tbl_UserList A, Dematact B WHERE A.Client_Code = B.da_clientcd AND da_defaultyn=''Y'' '
  SET @strHeader = @strHeader +' UPDATE A SET A.DPClientCode = A.Client_code FROM @tbl_UserList A WHERE  ISNULL(DPClientCode,'''') = '''''
  
  
  
  IF @strReportName = 'Confirmation'
  BEGIN
    IF @strRepType = 'Cumulative'
	BEGIN
	  SET @StrString = @strHeader+' '+'SELECT * FROM (
	  SELECT 1 AS SortOrder, ''Equity : '' + td_stlmnt AS type, td_stlmnt stlmnt, td_scripcd scripcode, replace(rtrim
				(ss_Name) + '' ('' + td_scripcd + '')'', ''&'', '''') scripname, sum(td_bqty) ''Buy'', sum(convert(DECIMAL(15, 2), 
				convert(MONEY, td_bqty * td_rate))) ''BuyAmount'', sum(td_sqty) ''Sell'', 
				sum(convert(DECIMAL(15, 4), convert(MONEY, td_sqty * td_rate))) ''SellAmount'', sum(td_bqty - td_sqty) ''Net''
			, sum(convert(DECIMAL(15, 4), convert(MONEY, (td_sqty - td_bqty) * td_rate))
			) ''NetAmount'', cast(convert(MONEY, CASE WHEN sum(td_bqty - td_sqty) = 0 THEN 0 ELSE sum((td_bqty - td_sqty) 
			* td_rate) / sum(td_bqty - td_sqty) END) AS DECIMAL(15, 2)) ''AvgRate'', cast(
				sum(td_brokerage * (td_bqty + td_sqty)) AS DECIMAL(15, 4)) Brokerage, left(
				td_stlmnt, 1) + ''/C/'' + td_scripcd ''Lookup''
	  FROM trx(NOLOCK), securities(NOLOCK), Settlements(NOLOCK), @tbl_UserList X
	  WHERE td_clientcd = X.Client_Code AND td_stlmnt = se_stlmnt AND td_scripcd = ss_cd AND td_dt = '''+@dtFromDate+'''
	  GROUP BY ''Equity : '' + td_stlmnt, td_stlmnt, td_scripcd, ss_Name, td_dt
	  UNION ALL
	  SELECT CASE right(sm_prodtype, 1) WHEN ''F'' THEN 2 ELSE 3 END, ''Equity '' + CASE right(sm_prodtype, 1) WHEN ''F'' THEN 
						''Future'' ELSE ''Option'' END td_type, ''Exp: '' + convert(CHAR(10), convert(DATETIME, 
					sm_expirydt), 105), td_seriesid scripcode, replace(rtrim(sm_symbol) + CASE right(
						sm_prodtype, 1) WHEN ''F'' THEN '''' ELSE '' ('' + ltrim(convert(CHAR(8), sm_strikeprice)) + 
						sm_callput + sm_optionstyle + '')'' END, ''&'', ''''), sum(td_bqty) ''bqty'', sum(convert(DECIMAL(
						15, 2), convert(MONEY, td_bqty * td_rate))) ''bvalue'', sum(td_sqty) ''sqty'', sum(convert(
					DECIMAL(15, 4), convert(MONEY, td_sqty * td_rate))) ''svalue'', sum(td_bqty - td_sqty) ''netqty'', 
			sum(convert(DECIMAL(15, 4), convert(MONEY, (td_sqty - td_bqty) * td_rate))) 
			''netvalue'', cast(convert(MONEY, CASE WHEN sum(td_bqty - td_sqty) = 0 THEN 0 ELSE sum((td_bqty - td_sqty
									) * td_rate) / sum(td_bqty - td_sqty) END) AS DECIMAL(15, 2)) ''average'', cast(
				sum(td_brokerage * (td_bqty + td_sqty)) AS DECIMAL(15, 4)) td_brokerage, 
			td_Exchange + ''/'' + td_Segment + ''/'' + Ltrim(convert(CHAR, td_seriesid))
	  FROM trades(NOLOCK), series_master WITH (NOLOCK), @tbl_UserList X
	  WHERE td_clientcd = X.Client_Code AND td_seriesid = sm_seriesid AND td_Exchange = sm_exchange AND td_Segment = 
			sm_Segment AND td_dt = '''+@dtFromDate+''' AND td_segment IN (''F'') 
	  GROUP BY sm_prodtype, sm_symbol, sm_desc, sm_expirydt, sm_strikeprice, sm_callput, sm_optionstyle, td_dt, 
			td_Exchange, td_Segment, td_seriesid
	  UNION ALL
	  SELECT CASE ex_eaflag WHEN ''E'' THEN 4 ELSE 5 END, CASE ex_eaflag WHEN ''E'' THEN ''Exercise'' ELSE ''Assignment'' END 
			Td_Type, ''Exp: '' + convert(CHAR(10), convert(DATETIME, sm_expirydt), 105), ltrim(convert(CHAR(8), 
					sm_strikeprice)) + sm_callput, replace(rtrim(sm_symbol) + '' ('' + ltrim(convert(CHAR(8), 
						sm_strikeprice)) + sm_callput + sm_optionstyle + '')'', ''&'', ''''), sum(ex_aqty) Bqty, sum(
				ex_aqty * ex_diffrate) BAmt, sum(ex_eqty) Sqty, sum(ex_eqty * ex_diffrate) SAmt, sum(ex_aqty - 
				ex_eqty) NQty, sum((ex_aqty - ex_eqty) * ex_diffrate) NAmt, cast(convert(MONEY, 
					CASE WHEN sum(ex_aqty - ex_eqty) = 0 THEN 0 ELSE sum((ex_aqty - ex_eqty
									) * ex_diffrate) / sum(ex_aqty - ex_eqty) END) AS DECIMAL(15, 2)) ''average'', 
			cast(sum(ex_brokerage * (ex_eqty + ex_aqty)) AS DECIMAL(15, 4)) td_Brokerage, 
			ex_exchange + ''/'' + ex_Segment + ''/'' + Ltrim(convert(CHAR, ex_seriesid))
	  FROM exercise (NOLOCK), series_master (NOLOCK), @tbl_UserList X
	  WHERE ex_clientcd = X.Client_Code AND ex_exchange = sm_exchange AND ex_Segment = sm_Segment AND ex_seriesid = 
			sm_seriesid AND ex_dt = '''+@dtFromDate+''' 
	  GROUP BY ex_eaflag, sm_symbol, sm_desc, sm_expirydt, sm_strikeprice, sm_callput, sm_optionstyle, 
			ex_exchange, ex_Segment, ex_dt, sm_prodtype, ex_seriesid
	  UNION ALL
	  SELECT CASE right(sm_prodtype, 1) WHEN ''F'' THEN 6 ELSE 7 END, CASE right(sm_prodtype, 1) WHEN ''F'' THEN 
						''Currency Future'' ELSE ''Currency Option'' END td_type, ''Exp: '' + convert(CHAR(10), 
				convert(DATETIME, sm_expirydt), 105), td_seriesid scripcode, replace(sm_symbol, ''&'', ''''), sum(
				td_bqty), sum(round(convert(MONEY, td_bqty * td_rate * sm_multiplier), 2)), sum(td_sqty), sum(
				round(convert(MONEY, td_sqty * td_rate * sm_multiplier), 2)), sum(td_bqty - td_sqty), sum(round(
					convert(MONEY, (td_sqty - td_bqty) * td_rate * sm_multiplier), 4)), 
			cast(convert(MONEY, CASE WHEN sum(td_bqty - td_sqty) = 0 THEN 0 ELSE sum((td_sqty - td_bqty
									) * td_rate * sm_multiplier) / sum(td_bqty - td_sqty) END) AS DECIMAL(15, 2)), 
			cast(sum(td_brokerage * (td_bqty + td_sqty)) AS DECIMAL(15, 4)) td_brokerage, 
			td_Exchange + ''/'' + td_Segment + ''/'' + Ltrim(convert(CHAR, td_seriesid))
	  FROM trades (NOLOCK), series_master(NOLOCK),  @tbl_UserList X
	  WHERE td_clientcd = X.Client_Code AND td_seriesid = sm_seriesid AND td_Exchange = sm_exchange AND td_Segment = 
			sm_Segment AND td_dt = '''+@dtFromDate+''' AND td_Segment IN (''K'')
	  GROUP BY sm_prodtype, sm_symbol, sm_desc, sm_expirydt, sm_callput, sm_strikeprice, td_exchange, 
			td_Segment, td_dt, td_seriesid'
	  
	  IF @strCommmexDB <> ''
	  BEGIN
	    SET @StrString = @StrString +' UNION ALL SELECT CASE right(sm_prodtype, 1) WHEN ''F'' THEN 8 ELSE 9 END, CASE right(sm_prodtype, 1) WHEN ''F'' THEN 
						''Commodity Future'' ELSE ''Commodity Option'' END td_type, ''Exp: '' + convert(CHAR(10), 
				convert(DATETIME, sm_expirydt), 105), CASE right(sm_prodtype, 1) WHEN ''F'' THEN '''' ELSE convert(CHAR
						(8), sm_strikeprice) + sm_callput END, replace(sm_symbol, ''&'', ''''), sum(td_bqty), sum(
				round(convert(MONEY, td_bqty * td_rate), 4)), sum(td_sqty), sum(round(convert(MONEY, td_sqty * 
						td_rate), 4)), sum(td_bqty - td_sqty), sum(round(convert(MONEY, (td_sqty - td_bqty
							) * td_rate), 4)), cast(convert(MONEY, CASE WHEN sum(td_bqty - td_sqty) = 0 THEN 0 ELSE sum
							((td_sqty - td_bqty) * td_rate) / sum(td_bqty - 
								td_sqty) END) AS DECIMAL(15, 4)), cast(sum(td_brokerage * (td_bqty + td_sqty
						) * sm_multiplier) AS DECIMAL(15, 4)), td_Exchange + ''/'' + ''X'' + ''/'' + Ltrim(convert(CHAR, 
					td_seriesid))
	    FROM '+@strCommmexDB+'.DBO.Trades, '+@strCommmexDB+'.DBO.Series_master,  @tbl_UserList X
	    WHERE td_clientcd = X.Client_Code AND td_seriesid = sm_seriesid AND td_Exchange = sm_exchange AND td_dt =  '''+@dtFromDate+'''
	    GROUP BY sm_prodtype, sm_symbol, sm_desc, sm_expirydt, sm_callput, sm_strikeprice, td_exchange, td_dt, 
	    td_seriesid '
	  END	 
	  SET @StrString = @StrString +' ) a ORDER BY SortORder, stlmnt, scripname '
	  BEGIN TRY
	    
	    EXEC(@StrString)
		SET @o_vcErrorFlag  = 'S'
        SET @o_vcErrorMessage = 'Process Executed'
		RETURN 1
	  END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag  = 'E'
        SET @o_vcErrorMessage = ERROR_MESSAGE()
        RETURN 1
	  END CATCH
	END
	ELSE IF @strRepType = 'Confirmation'
	BEGIN
	  SET @StrString = @strHeader+' '+'SELECT * FROM ( SELECT 1 AS orderid, 1 AS SortOrder, 
	  ''Equity : '' + td_stlmnt AS type, td_stlmnt stlmnt, td_scripcd scripcode, 
	  replace(ss_name, ''&'', '''') AS scripname, td_bqty AS buy, td_sqty AS sell, 
	  cast((td_marketrate) AS DECIMAL(15, 4)) AS marketrate, cast((td_rate) AS DECIMAL(15, 4)) AS 
		netrate, cast((((td_bqty + td_sqty) * td_rate)
				) * (CASE td_bsflag WHEN ''S'' THEN ''-1'' ELSE ''1'' END) AS DECIMAL(15, 2)) netamount, 
		cast((td_brokerage) AS DECIMAL(15, 4)) brokerage, left(td_stlmnt, 1) + ''/C/'' + 
		td_scripcd AS lookup
	  FROM trx WITH (INDEX (idx_trx_clientcd), NOLOCK), Settlements WITH (NOLOCK
			), securities WITH (NOLOCK),  @tbl_UserList X
	  WHERE td_clientcd = X.Client_Code AND td_stlmnt = se_stlmnt AND td_dt = '''+@dtFromDate+''' AND td_scripcd = ss_cd 
	  UNION ALL
	  SELECT 3, 2 AS td_order, CASE td_segment WHEN ''F'' THEN CASE TD_EXCHANGE WHEN ''N'' THEN ''NSE F&O'' WHEN ''B'' THEN 
								''BSE F&O'' END WHEN ''K'' THEN CASE TD_EXCHANGE WHEN ''M'' THEN ''MCX FX'' WHEN ''N'' THEN 
								''NSE FX'' END END td_type, '''', sm_symbol, replace(sm_desc, ''&'', ''''), td_bqty AS 
		bqty, td_sqty AS sqty, cast((td_marketrate) AS DECIMAL(15, 4)) AS diffrate, cast((td_rate
				) AS DECIMAL(15, 4)) AS netrate, convert(DECIMAL(15, 2), (td_bqty - td_sqty) * 
			td_rate * sm_multiplier) AS amount, cast((td_brokerage) AS DECIMAL(15, 4)), 
		td_exchange + ''/'' + td_Segment + ''/'' + convert(CHAR, td_seriesid) AS td_lookup
	  FROM trades WITH (INDEX (idx_trades_clientcd), NOLOCK), series_master WITH (NOLOCK
			),  @tbl_UserList X
	  WHERE td_seriesid = sm_seriesid AND td_Exchange = sm_exchange AND td_Segment = sm_Segment AND td_clientcd = X.Client_Code 
	  AND td_dt = '''+@dtFromDate+''' AND td_trxflag = ''N''  AND td_dt = '''+@dtFromDate+''' AND 
      td_trxflag = ''N'') a
      ORDER BY type, orderid, SortOrder, stlmnt, scripname '
      BEGIN TRY
	    EXEC(@StrString)
		SET @o_vcErrorFlag  = 'S'
        SET @o_vcErrorMessage = 'Process Executed'
		RETURN 1
	  END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag  = 'E'
        SET @o_vcErrorMessage = ERROR_MESSAGE()
        RETURN 1
	  END CATCH
	END  
  END
  ELSE IF @strReportName = 'LedgerSummary'
  BEGIN
   SET @StrString = @strHeader+' '+'SELECT * FROM ( 
   SELECT 1 AS Ord, ''Trading'' AS [Type], ld_clientcd AS [ClientCode], sum(CASE sign(datediff(d, '''+@dtFromDate+''', 
						ld_dt)) WHEN - 1 THEN ld_amount ELSE 0 END) OpeningBalance, sum(CASE sign(datediff(d, 
						'''+@dtFromDate+''', ld_dt)) WHEN - 1 THEN 0 ELSE CASE ld_debitflag WHEN ''D'' THEN ld_amount ELSE 0 
						END END) Debit, sum(CASE sign(datediff(d, '''+@dtFromDate+''', ld_dt)) WHEN - 1 THEN 0 ELSE CASE 
						ld_debitflag WHEN ''D'' THEN 0 ELSE ld_amount END END) Credit, sum(ld_amount) Balance, 
		Rtrim(CES_Exchange) + ''-'' + CES_Segment [ExchSeg], LD_DPID AS CESCD, 1 AS TypeCode
	FROM ledger WITH (NOLOCK), Companyexchangesegments WITH (NOLOCK), @tbl_UserList X
	WHERE LD_DPID = CES_Cd AND ld_clientcd = X.Client_Code AND ld_dt <= '''+@dtToDate+'''
	GROUP BY ld_dpid, ld_clientcd, CES_Exchange, CES_Segment '
	IF @strRepType IN('MTF','ALL') AND EXISTS(SELECT 1 FROM SYS.TABLES WHERE NAME= 'MrgTdgFin_Clients')
	BEGIN
	  SET @StrString = @StrString+' '+' UNION ALL
	  SELECT 2 AS Ord, ''MTF'' AS [Type], ld_clientcd AS [ClientCode], sum(CASE sign(datediff(d, '''+@dtFromDate+''', 
						ld_dt)) WHEN - 1 THEN ld_amount ELSE 0 END) OpeningBalance, sum(CASE sign(datediff(d, 
						'''+@dtFromDate+''', ld_dt)) WHEN - 1 THEN 0 ELSE CASE ld_debitflag WHEN ''D'' THEN ld_amount ELSE 0 
						END END) Debit, sum(CASE sign(datediff(d, '''+@dtFromDate+''', ld_dt)) WHEN - 1 THEN 0 ELSE CASE 
						ld_debitflag WHEN ''D'' THEN 0 ELSE ld_amount END END) Credit, sum(ld_amount) Balance, 
		Rtrim(CES_Exchange) + ''-'' + CES_Segment [ExchSeg], LD_DPID AS CESCD, 1 AS TypeCode
	  FROM ledger WITH (NOLOCK), Companyexchangesegments WITH (NOLOCK), MrgTdgFin_Clients(NOLOCK), @tbl_UserList X
	  WHERE LD_DPID = CES_Cd AND ld_dt <= '''+@dtToDate+'''
	  AND ld_clientcd = MTFC_FillerB AND MTFC_CMCD = X.Client_Code AND MTFC_FillerB <> ''''
	  GROUP BY ld_dpid, ld_clientcd, CES_Exchange, CES_Segment '
	END
	IF @strRepType IN('Trading-Margin','ALL') 
	BEGIN
	  SET @StrString = @StrString+' '+' UNION ALL
	  SELECT 3 AS Ord, ''Trading-Margin'' AS [Type], ld_clientcd AS [ClientCode], sum(CASE sign(datediff(d, '''+@dtFromDate+''', 
						ld_dt)) WHEN - 1 THEN ld_amount ELSE 0 END) OpeningBalance, sum(CASE sign(datediff(d, 
						'''+@dtFromDate+''', ld_dt)) WHEN - 1 THEN 0 ELSE CASE ld_debitflag WHEN ''D'' THEN ld_amount ELSE 0 
						END END) Debit, sum(CASE sign(datediff(d, '''+@dtFromDate+''', ld_dt)) WHEN - 1 THEN 0 ELSE CASE 
						ld_debitflag WHEN ''D'' THEN 0 ELSE ld_amount END END) Credit, sum(ld_amount) Balance, 
		Rtrim(CES_Exchange) + ''-'' + CES_Segment [ExchSeg], LD_DPID AS CESCD, 1 AS TypeCode
	  FROM ledger WITH (NOLOCK), Companyexchangesegments WITH (NOLOCK), Client_master(NOLOCK), @tbl_UserList X
	  WHERE LD_DPID = CES_Cd AND ld_dt <= '''+@dtToDate+'''
	  and ld_clientcd = cm_brkggroup and cm_cd = X.Client_Code
	  GROUP BY ld_dpid, ld_clientcd, CES_Exchange, CES_Segment '
	END
	IF @strRepType IN('Commodity','ALL') AND @strCommmexDB <> ''
	BEGIN
	  SET @StrString = @StrString+' '+' UNION ALL
	  SELECT 4 AS Ord, ''Commodity'' AS [Type], ld_clientcd AS [ClientCode], sum(CASE sign(datediff(d, '''+@dtFromDate+''', 
						ld_dt)) WHEN - 1 THEN ld_amount ELSE 0 END) OpeningBalance, sum(CASE sign(datediff(d, 
						'''+@dtFromDate+''', ld_dt)) WHEN - 1 THEN 0 ELSE CASE ld_debitflag WHEN ''D'' THEN ld_amount ELSE 0 
						END END) Debit, sum(CASE sign(datediff(d, '''+@dtFromDate+''', ld_dt)) WHEN - 1 THEN 0 ELSE CASE 
						ld_debitflag WHEN ''D'' THEN 0 ELSE ld_amount END END) Credit, sum(ld_amount) Balance, 
	  Rtrim(CES_Exchange) + ''-'' + CES_Segment [ExchSeg], LD_DPID AS CESCD, 1 AS TypeCode
	  FROM '+@strCommmexDB+'.dbo.ledger WITH (NOLOCK), '+@strCommmexDB+'.dbo.Companyexchangesegments WITH (NOLOCK), @tbl_UserList X
	  WHERE LD_DPID = CES_Cd AND ld_dt <= '''+@dtToDate+'''
	  and ld_clientcd = X.Client_Code
	  GROUP BY ld_dpid, ld_clientcd, CES_Exchange, CES_Segment '
	END
	IF @strRepType IN('ommodity-Margin','ALL') AND @strCommmexDB <> ''
	BEGIN
	  SET @StrString = @StrString+' '+' UNION ALL 
	  SELECT 5 AS Ord, ''Commodity-Margin'' AS [Type], ld_clientcd AS [ClientCode], sum(CASE sign(datediff(d, '''+@dtFromDate+''', 
						ld_dt)) WHEN - 1 THEN ld_amount ELSE 0 END) OpeningBalance, sum(CASE sign(datediff(d, 
						'''+@dtFromDate+''', ld_dt)) WHEN - 1 THEN 0 ELSE CASE ld_debitflag WHEN ''D'' THEN ld_amount ELSE 0 
						END END) Debit, sum(CASE sign(datediff(d, '''+@dtFromDate+''', ld_dt)) WHEN - 1 THEN 0 ELSE CASE 
						ld_debitflag WHEN ''D'' THEN 0 ELSE ld_amount END END) Credit, sum(ld_amount) Balance, 
	  Rtrim(CES_Exchange) + ''-'' + CES_Segment [ExchSeg], LD_DPID AS CESCD, 1 AS TypeCode
	  FROM commex.dbo.ledger WITH (NOLOCK), commex.dbo.Companyexchangesegments WITH (NOLOCK), commex.dbo.Client_master(NOLOCK), @tbl_UserList X
	  WHERE LD_DPID = CES_Cd AND ld_dt <= '''+@dtToDate+'''
	  and ld_clientcd = cm_brkggroup and cm_cd = X.Client_Code
	  GROUP BY ld_dpid, ld_clientcd, CES_Exchange, CES_Segment '
	END  
	IF @strRepType IN('NBFC','ALL') AND EXISTS(SELECT 1 FROM SYS.TABLES WHERE NAME= 'NBFC_Ledger')
	BEGIN
	  SET @StrString = @StrString+' '+' UNION ALL
	  SELECT 6 AS Ord, ''NBFC'' AS [Type], ld_clientcd [ClientCode], sum(CASE sign(datediff(d, '''+@dtFromDate+''', ld_dt)) 
				WHEN - 1 THEN ld_amount ELSE 0 END) OpeningBalance, sum(CASE sign(datediff(d, '''+@dtFromDate+''', ld_dt)
				) WHEN - 1 THEN 0 ELSE CASE ld_debitflag WHEN ''D'' THEN ld_amount ELSE 0 END END) Debit, sum(CASE sign(
					datediff(d, '''+@dtFromDate+''', ld_dt)) WHEN - 1 THEN 0 ELSE CASE ld_debitflag WHEN ''D'' THEN 0 ELSE 
							ld_amount END END) Credit, sum(ld_amount) Balance, ''NBFC'' [ExchSeg], ''NBFC'' AS 
		CESCD, 6 AS TypeCode
	  FROM NBFC_Ledger WITH (NOLOCK), @tbl_UserList X
	  WHERE ld_clientcd = X.Client_Code AND ld_dt <= '''+@dtToDate+'''
	  GROUP BY ld_dpid, ld_clientcd '
	END 
	IF @strRepType IN('DP','ALL') AND @strDefaultConn <> ''
	BEGIN
	  SET @StrString = @StrString+' '+' UNION ALL
	  SELECT 7 AS Ord, ''DP'' AS [Type], ld_clientcd [ClientCode], sum(CASE sign(datediff(d, '''+@dtFromDate+''', ld_dt)) 
				WHEN - 1 THEN ld_amount ELSE 0 END) OpeningBalance, sum(CASE sign(datediff(d, '''+@dtFromDate+''', ld_dt)
				) WHEN - 1 THEN 0 ELSE CASE ld_debitflag WHEN ''D'' THEN ld_amount ELSE 0 END END) Debit, sum(CASE sign(
					datediff(d, '''+@dtFromDate+''', ld_dt)) WHEN - 1 THEN 0 ELSE CASE ld_debitflag WHEN ''D'' THEN 0 ELSE 
							ld_amount END END) Credit, sum(ld_amount) Balance, ''DP'' [ExchSeg], ''DP'' AS 
		CESCD, 6 AS TypeCode
	  FROM '+@strDefaultConn+'.DBO.ledger WITH (NOLOCK), @tbl_UserList X
	  WHERE ld_clientcd = X.DPClientCode AND ld_dt <= '''+@dtToDate+'''
	  GROUP BY ld_dpid, ld_clientcd '
	END 
	SET @StrString = @StrString+' '+') a ORDER BY Ord, [Type], ClientCode, CESCD '
	BEGIN TRY
	  EXEC(@StrString)
	  SET @o_vcErrorFlag  = 'S'
      SET @o_vcErrorMessage = 'Process Executed'
	  RETURN 1
	END TRY
	BEGIN CATCH
	  SET @o_vcErrorFlag  = 'E'
      SET @o_vcErrorMessage = ERROR_MESSAGE()
      RETURN 1
	END CATCH
  END
  ELSE IF @strReportName = 'Transaction'
  BEGIN
    IF @strRepType = 'TradeSummary'
	BEGIN
	  if @strRepSubType = 'DateWise' 
	  BEGIN
	    SET @StrString = @strHeader+' '+'SELECT td_clientcd, Td_order ListOrder, Td_Type type, Dt DATE, td_stlmnt Stlmnt, Bqty Buy, BAmt BuyAmount, Sqty Sell, SAmt  '
	    +' SellAmount, NQty Net, NAmt NetAmount, LinkCode, Dt1 Date2, LookUp '
	    +' FROM (SELECT td_clientcd, 1 Td_order, ''Equity ['' + CASE WHEN left(td_stlmnt, 1) = ''B'' THEN ''BSE'' ELSE ''NSE'' END + '']'' Td_Type, ltrim( '
	    +' rtrim(convert(CHAR, convert(DATETIME, td_dt), 103))) Dt, rtrim(td_stlmnt) td_stlmnt, '
	    +' sum(td_bqty) Bqty, convert(DECIMAL(15, 2), sum(td_bqty * td_rate)) BAmt, sum(td_sqty) Sqty, convert( '
	    +' DECIMAL(15, 2), sum(td_sqty * td_rate)) SAmt, sum(td_bqty - td_sqty) NQty, convert(DECIMAL(15, 2), '
	    +' sum((td_bqty - td_sqty) * td_rate)) NAmt, ''Equity|'' + Left(td_Stlmnt, 1) + ''|''' 
	    +' LinkCode, td_dt Dt1, left(td_stlmnt, 1) + ''/C/'' + Max(td_scripcd) AS LookUp '
	    +' FROM trx WITH (INDEX (idx_trx_clientcd), NOLOCK), @tbl_UserList X '
	    +' WHERE td_clientcd = X.Client_COde AND td_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+''' '
	    +' GROUP BY td_clientcd, td_stlmnt, td_dt '
	    +' UNION ALL '
	    +' SELECT td_clientcd, CASE sm_prodtype WHEN ''CF'' THEN 4 ELSE CASE left(sm_productcd, 1) WHEN ''F'' THEN 2 ELSE 3 END END, CASE WHEN '
	    +' td_segment = ''K'' THEN ''Currency '' ELSE ''Equity '' END + CASE left(sm_productcd, 1) WHEN ''F'' THEN '
	    +' ''Future '' ELSE ''Option '' END + ''['' + CASE left(td_exchange, 1) WHEN ''B'' THEN ''BSE'' WHEN ''N'' THEN '
	    +' ''NSE'' ELSE ''MCX'' END + '']'' Td_Type, ltrim(rtrim(convert(CHAR, convert(DATETIME, td_dt), '
	    +' 103))) Dt, CASE left(sm_prodtype, 1) WHEN ''I'' THEN ''Index'' WHEN ''E'' THEN ''Stock'' ELSE '
	    +' ''Currency'' END + CASE right(sm_prodtype, 1) WHEN ''F'' THEN '' Future'' ELSE '' Option'' END, sum( '
	    +' td_bqty) Bqty, convert(DECIMAL(15, 2), sum(td_bqty * td_rate * sm_multiplier)) BAmt, sum(td_sqty) '
	    +' Sqty, convert(DECIMAL(15, 2), sum(td_sqty * td_rate * sm_multiplier)) SAmt, sum(td_bqty - td_sqty) NQty, '
	    +' convert(DECIMAL(15, 2), sum((td_bqty - td_sqty) * td_rate * sm_multiplier)) NAmt, '
	    +' CASE WHEN td_segment = ''K'' THEN ''Currency'' ELSE ''Equity'' END + CASE left(sm_productcd, 1) WHEN ''F'' THEN '
	    +' ''Future'' ELSE ''Option'' END + ''|'' + td_exchange + ''|'' +'' '' + ''|'' +'' ''+ ''|'' +'' ''+ ''|'' + '''' + ''|'' + sm_optionstyle +'
	    + '''|'' + td_segment LinkCode, td_dt Dt1, td_exchange + ''/'' + td_Segment + ''/'' + convert(CHAR, Max(td_seriesid '
	    +' )) AS Lookup FROM trades '
	    +' WITH (INDEX (idx_trades_clientcd), NOLOCK), series_master WITH (NOLOCK),  @tbl_UserList X '
	    +' WHERE td_clientcd = X.Client_COde AND sm_exchange = td_exchange AND sm_Segment = td_Segment AND td_seriesid = '
	    +' sm_seriesid AND td_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+'''  AND td_trxflag <> ''O'' '
	    +' GROUP BY td_clientcd, td_Dt, sm_productcd, td_exchange, td_Segment, sm_prodtype, td_exchange, sm_optionstyle, sm_prodtype '
 	
	    +' UNION ALL '
	
	    +' SELECT ex_clientcd AS td_clientcd, 5, CASE WHEN ex_segment = ''K'' THEN ''Currency '' ELSE ''Equity '' END + CASE ex_eaflag WHEN ''E'' THEN '
	    +' ''Exercise '' ELSE ''Assignment '' END + ''['' + CASE left(ex_exchange, 1) WHEN ''B'' THEN ''BSE'' WHEN '
	    +' ''N'' THEN ''NSE'' ELSE ''MCX'' END + '']'' Td_Type, ltrim(rtrim(convert(CHAR, convert(DATETIME, ex_Dt) '
	    +' , 103))) Dt, ( '
	    +' CASE left(sm_prodtype, 1) WHEN ''I'' THEN ''Index'' WHEN ''E'' THEN ''Stock'' ELSE ''Currency'' END + CASE right( '
	    +' sm_prodtype, 1) WHEN ''F'' THEN '' Future'' ELSE '' Option'' END '
	    +' ), sum(ex_aqty) Bqty, convert(DECIMAL(15, 2), sum(ex_aqty * ex_diffrate * CASE ex_eaflag WHEN ''A''  '
	    +' THEN - 1 ELSE 1 END * sm_multiplier)) BAmt, sum(ex_eqty) Sqty, convert(DECIMAL(15, 2), '
	    +' sum(ex_eqty * ex_diffrate * CASE ex_eaflag WHEN ''A'' THEN - 1 ELSE 1 END * sm_multiplier)) SAmt, sum( '
	    +' ex_aqty - ex_eqty) NQty, convert(DECIMAL(15, 2), sum((ex_aqty - ex_eqty) * '
	    +' ex_diffrate * CASE ex_eaflag WHEN ''A'' THEN - 1 ELSE 1 END * sm_multiplier)) NAmt, CASE WHEN  '
	    +' ex_segment = ''K'' THEN ''Currency'' ELSE ''Equity'' END + CASE ex_eaflag WHEN ''E'' THEN ''Exercise'' ELSE '
	    +' ''Assignment'' END + ''|'' + ex_exchange + ''|'' + ex_segment + ''|'' LinkCode, ex_Dt Dt1, ex_exchange + ''/'' + '
	    +' ex_Segment + ''/'' + Ltrim(convert(CHAR, Max(ex_seriesid))) AS Lookup '
	    +' FROM exercise WITH (NOLOCK), series_master WITH (NOLOCK) ,  @tbl_UserList X '
	    +' WHERE ex_clientcd = X.Client_COde AND ex_exchange = sm_exchange AND ex_Segment = sm_Segment AND ex_seriesid = '
	    +' sm_seriesid AND ex_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+'''  '
	    +' GROUP BY ex_clientcd, ex_Dt, ex_eaflag, ex_exchange, ex_Segment, sm_prodtype '
		IF @strCommmexDB <> ''
		BEGIN
		  SET @StrString = @StrString +' '+' UNION ALL '
		  +' SELECT td_clientcd, 7, '
	      +' ''Commodity''  + ''['' + CASE left(td_exchange, 1) WHEN ''M'' THEN ''MCX'' WHEN ''S'' THEN '
		  +' ''NSEL'' ELSE ''NCDEX'' END + '']'' Td_Type, ltrim(rtrim(convert(CHAR, convert(DATETIME, td_dt), '
		  +' 103))) Dt, ''Commodity'' , sum(td_bqty) Bqty, convert(DECIMAL(15, 2), sum(td_bqty * td_rate * sm_multiplier)) BAmt, sum(td_sqty) '
		  +' Sqty, convert(DECIMAL(15, 2), sum(td_sqty * td_rate * sm_multiplier)) SAmt, sum(td_bqty - td_sqty) NQty, '
		  +' convert(DECIMAL(15, 2), sum((td_bqty - td_sqty) * td_rate * sm_multiplier)) NAmt, '
		  +' ''Commodity''  + ''|'' + td_exchange   LinkCode, td_dt Dt1, td_exchange + ''/''  + convert(CHAR, Max(td_seriesid)) AS Lookup '
	      +' FROM '+@strCommmexDB+'.dbo.trades(NOLOCK), '+@strCommmexDB+'.dbo.series_master(NOLOCK), @tbl_UserList X '
	      +' WHERE td_clientcd = X.Client_COde AND sm_exchange = td_exchange  AND td_seriesid = sm_seriesid AND'
          +' td_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+'''   AND td_trxflag <> ''O'' '
	      +' GROUP BY td_clientcd, td_Dt, sm_productcd, td_exchange,  sm_prodtype, td_exchange, sm_optionstyle, sm_prodtype '
	      +' UNION ALL '
	      +' SELECT ex_clientcd AS td_clientcd, 8, ''Commodity'' + CASE ex_eaflag WHEN ''E'' THEN  '
		  +' ''Exercise '' ELSE ''Assignment '' END + ''['' + CASE left(ex_exchange, 1) WHEN ''M'' THEN ''MCX'' WHEN ''S'' THEN '
		  +' ''NSEL'' ELSE ''NCDEX'' END + '']'' Td_Type, ltrim(rtrim(convert(CHAR, convert(DATETIME, ex_Dt) '
		  +' , 103))) Dt, ''Commodity'' , sum(ex_aqty) Bqty, convert(DECIMAL(15, 2), sum(ex_aqty * ex_diffrate * CASE ex_eaflag WHEN ''A'' '
		  +' THEN - 1 ELSE 1 END * sm_multiplier)) BAmt, sum(ex_eqty) Sqty, convert(DECIMAL(15, 2), '
		  +' sum(ex_eqty * ex_diffrate * CASE ex_eaflag WHEN ''A'' THEN - 1 ELSE 1 END * sm_multiplier)) SAmt, sum( '
		  +' ex_aqty - ex_eqty) NQty, convert(DECIMAL(15, 2), sum((ex_aqty - ex_eqty) * '
		  +' ex_diffrate * CASE ex_eaflag WHEN ''A'' THEN - 1 ELSE 1 END * sm_multiplier)) NAmt, '
		  +' ''Commodity'' + CASE ex_eaflag WHEN ''E'' THEN ''Exercise'' ELSE '
		  +' ''Assignment'' END + ''|'' + ex_exchange  LinkCode, ex_Dt Dt1, ex_exchange + ''/'' + Ltrim(convert(CHAR, Max(ex_seriesid))) AS Lookup '
	      +' FROM '+@strCommmexDB+'.dbo.exercise WITH (NOLOCK), '+@strCommmexDB+'.dbo.series_master WITH (NOLOCK), @tbl_UserList X '
	      +' WHERE ex_clientcd = X.Client_COde AND ex_exchange = sm_exchange  AND ex_seriesid =  '
		  +' sm_seriesid AND ex_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+'''  '
	      +' GROUP BY ex_clientcd, ex_Dt, ex_eaflag, ex_exchange, sm_prodtype '
        END
		
		SET @StrString = @StrString +' ) a '
		BEGIN TRY
	     EXEC(@StrString)
	     SET @o_vcErrorFlag  = 'S'
         SET @o_vcErrorMessage = 'Process Executed'
	     RETURN 1
	    END TRY
	    BEGIN CATCH
	      SET @o_vcErrorFlag  = 'E'
          SET @o_vcErrorMessage = ERROR_MESSAGE()
          RETURN 1
	    END CATCH
      END
	  if @strRepSubType = 'ItemWise' 
	  BEGIN
	    SET @StrString = @strHeader+' '+'SELECT td_clientcd, ListOrder, Td_Type Type, td_scripnm ScripCode, snm ScripName, Bqty Buy, BAmt BuyAmount, Sqty Sell, SAmt '
	    +' SellAmount, NQty Net, NAmt NetAmount, rate AvgRate, LinkCode, LookUp '
        +' FROM ( SELECT td_clientcd, 1 ListOrder, '''' AS td_ac_type, '''' AS td_trxdate, '''' AS td_isin_code, '''' AS sc_company_name, '
		+' cast(( CASE WHEN sum(td_bqty - td_sqty) = 0 THEN 0 ELSE sum((td_bqty - td_sqty) * td_rate) / sum(td_bqty - td_sqty) END '
		+' ) AS DECIMAL(15, 4)) AS rate, ''Equity'' Td_Type, '''' AS FScripNm, '''' AS FExDt, rtrim(td_scripcd) '
		+' td_scripnm, rtrim(ss_name) snm, sum(td_bqty) Bqty, convert(DECIMAL(15, 2), sum(td_bqty * td_rate)) '
		+' BAmt, sum(td_sqty) Sqty, convert(DECIMAL(15, 2), sum(td_sqty * td_rate)) SAmt, sum(td_bqty - td_sqty) '
		+' NQty, convert(DECIMAL(15, 2), sum((td_bqty - td_sqty) * td_rate)) NAmt, '''' AS '
		+' td_debit_credit, 0 AS sm_strikeprice, '''' AS sm_callput, ''Equity|'' + '''' + ''|'' + td_scripcd LinkCode, left( '
		+' td_stlmnt, 1) + ''/C/'' + td_scripcd AS lookup '
	    +' FROM Trx WITH (INDEX (idx_trx_clientcd), NOLOCK), securities WITH (NOLOCK), @tbl_UserList X '
	    +' WHERE td_clientcd = x.client_code AND td_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+'''  AND td_Scripcd = ss_cd '
	    +' GROUP BY td_clientcd, td_scripcd, ss_name, ''Equity|'' + '''' + ''|'' + td_scripcd, left(td_stlmnt, 1) + ''/C/'' + td_scripcd '
	
	    +' UNION ALL '
	
	    +' SELECT td_clientcd, CASE left(sm_productcd, 1) WHEN ''F'' THEN 2 ELSE 3 END, '''', '''', '''' AS td_isin_code, '''' AS sc_company_name,  '
		+' cast((CASE WHEN sum(td_bqty - td_sqty) = 0 THEN 0 ELSE sum((td_bqty - td_sqty) * td_rate) / sum(td_bqty - td_sqty) END '
		+' ) AS DECIMAL(15, 4)) AS rate, CASE WHEN td_segment = ''K'' THEN ''Currency '' ELSE ''Equity '' END + CASE '
		+' left(sm_productcd, 1) WHEN ''F'' THEN ''Future '' ELSE ''Option '' END Td_Type, rtrim(sm_symbol), '
		+' sm_expirydt, rtrim(sm_symbol), CASE left(sm_productcd, 1) WHEN ''F'' THEN ''Fut '' ELSE ''Opt '' END + rtrim( '
		+' sm_symbol) + ''  Exp: '' + ltrim(rtrim(convert(CHAR, convert(DATETIME, sm_expirydt), 103))) + CASE '
		+' left(sm_productcd, 1) WHEN ''F'' THEN '''' ELSE '''' + rtrim(convert(CHAR(9), sm_strikeprice)) + '
		+' sm_callput + sm_optionstyle END, sum(td_bqty) Bqty, convert(DECIMAL(15, 2), sum(td_bqty * '
		+' td_rate * sm_multiplier)) BAmt, sum(td_sqty) Sqty, convert(DECIMAL(15, 2), sum(td_sqty * '
		+' td_rate * sm_multiplier)) SAmt, sum(td_bqty - td_sqty) NQty, convert(DECIMAL(15, 2), sum((td_bqty - td_sqty '
		+' ) * td_rate * sm_multiplier)) NAmt, '''' AS td_debit_credit, sm_strikeprice, sm_callput, CASE '
		+' WHEN td_segment = ''K'' THEN ''Currency'' ELSE ''Equity'' END + CASE left(sm_productcd, 1) WHEN ''F'' THEN '
		+' ''Future'' ELSE ''Option'' END + ''|'' + '''' + ''|'' + replace(sm_symbol, ''&'', ''-'') + ''|'' + left( '
		+' sm_productcd, 1) + ''|'' + sm_expirydt + ''|'' + Rtrim(Ltrim(Convert(CHAR, sm_strikeprice))) + ''|'' + '
		+' sm_callput + ''|'' + sm_optionstyle + ''|'' + td_Segment LinkCode, td_exchange + ''/'' + td_Segment + ''/'' + convert( '
		+' CHAR, td_seriesid) AS Lookup FROM trades WITH (INDEX (idx_trades_clientcd), NOLOCK), series_master WITH (NOLOCK) '
		+', @tbl_UserList X '
	    +' WHERE td_clientcd = x.Client_code AND sm_exchange = td_exchange AND sm_Segment = td_Segment AND td_seriesid = '
		+' sm_seriesid AND td_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+''' AND td_trxflag <> ''O'' '
	    +' GROUP BY td_clientcd, sm_symbol, sm_productcd, td_exchange, td_Segment, sm_expirydt, sm_strikeprice, sm_callput,  '
		+' sm_optionstyle, td_exchange + ''/'' + td_Segment + ''/'' + convert(CHAR, td_seriesid) '
	    +' UNION ALL '
	    +' SELECT ex_clientcd, 4, '''', '''' AS td_trxdate, '''' AS td_isin_code, '''' AS sc_company_name, cast(( '
		+' CASE WHEN sum(ex_aqty - ex_eqty) = 0 THEN 0 ELSE sum((ex_aqty - ex_eqty '
	    +' ) * ex_diffrate * CASE ex_eaflag WHEN ''A'' THEN - 1 ELSE 1 END) / sum(ex_aqty - '
		+' ex_eqty) END) AS DECIMAL(15, 2)) AS rate, CASE WHEN ex_Segment = ''K'' THEN ''Currency '' ELSE ''Equity '' END + CASE '
		+' ex_eaflag WHEN ''E'' THEN ''Exercise '' ELSE ''Assignment '' END Td_Type, '''', '''', rtrim(sm_symbol), CASE '
		+' left(sm_productcd, 1) WHEN ''F'' THEN ''Fut '' ELSE ''Opt '' END + rtrim(sm_symbol) + ''  Exp: '' + ltrim( '
		+' rtrim(convert(CHAR, convert(DATETIME, sm_expirydt), 103))) + CASE left(sm_productcd, 1) WHEN ''F'' ' 
		+' THEN '''' ELSE '''' + rtrim(convert(CHAR(9), sm_strikeprice)) + sm_callput + sm_optionstyle END, sum '
		+' (ex_aqty) Bqty, convert(DECIMAL(15, 2), sum(ex_aqty * ex_diffrate * CASE ex_eaflag WHEN ''A'' THEN - 1 ELSE 1 '
		+' END * sm_multiplier)) BAmt, sum(ex_eqty) Sqty, convert(DECIMAL(15, 2), sum(ex_eqty * '
		+' ex_diffrate * CASE ex_eaflag WHEN ''A'' THEN - 1 ELSE 1 END * sm_multiplier)) SAmt, sum(ex_aqty - '
		+' ex_eqty) NQty, convert(DECIMAL(15, 2), sum((ex_aqty - ex_eqty) * ex_diffrate '
		+' * CASE ex_eaflag WHEN ''A'' THEN - 1 ELSE 1 END * sm_multiplier)) NAmt, '''' AS td_debit_credit, 0, '''', '
		+' CASE WHEN ex_segment = ''K'' THEN ''Currency'' ELSE ''Equity'' END + CASE ex_eaflag WHEN ''E'' THEN ''Exercise'' ELSE '
		+' ''Assignment'' END + ''|'' + '''' + ''|'' + replace(sm_symbol, ''&'', ''-'') + ''|'' + left(sm_productcd, 1) + ''|'' + '
		+' sm_expirydt + ''|'' + Rtrim(Ltrim(Convert(CHAR, sm_strikeprice))) + ''|'' + sm_callput + ''|'' + sm_optionstyle '
		+' + ''|'' + ex_Segment LinkCode, ex_exchange + ''/'' + ex_Segment + ''/'' + Ltrim(convert(CHAR, ex_seriesid)) AS Lookup '
	    +' FROM exercise WITH (NOLOCK), series_master WITH (NOLOCK), @tbl_UserList X'
	    +' WHERE ex_clientcd = x.Client_code AND ex_exchange = sm_exchange AND ex_Segment = sm_Segment AND ex_seriesid = '
		+' sm_seriesid AND ex_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+''' '
	    +' GROUP BY ex_clientcd, ex_eaflag, sm_symbol, ex_Segment, sm_productcd, sm_expirydt, sm_strikeprice, sm_callput, '
		+' sm_optionstyle, ex_exchange, ex_exchange + ''/'' + ex_Segment + ''/'' + Ltrim(convert(CHAR, ex_seriesid)) '
		IF @strCommmexDB <> ''
		BEGIN
		  SET @StrString = @StrString +' '+' UNION ALL '
		  +' SELECT td_clientcd, 7, '''', '''', '''' AS td_isin_code, '''' AS ListOrder, '
		  +' cast((CASE WHEN sum(td_bqty - td_sqty) = 0 THEN 0 ELSE sum((td_bqty - td_sqty ) * td_rate) / sum(td_bqty - td_sqty) END'
		  +' ) AS DECIMAL(15, 4)) AS rate, ''Commodity'' Td_Type, rtrim(sm_symbol), '
		  +' sm_expirydt, rtrim(sm_symbol), CASE left(sm_productcd, 1) WHEN ''F'' THEN ''Fut '' ELSE ''Opt '' END + rtrim( '
		  +' sm_symbol) + ''  Exp: '' + ltrim(rtrim(convert(CHAR, convert(DATETIME, sm_expirydt), 103))) + CASE '
		  +' left(sm_productcd, 1) WHEN ''F'' THEN '''' ELSE '''' + rtrim(convert(CHAR(9), sm_strikeprice)) + '
		  +' sm_callput + sm_optionstyle END, sum(td_bqty) Bqty, convert(DECIMAL(15, 2), sum(td_bqty * '
		  +' td_rate * sm_multiplier)) BAmt, sum(td_sqty) Sqty, convert(DECIMAL(15, 2), sum(td_sqty * '
		  +' td_rate * sm_multiplier)) SAmt, sum(td_bqty - td_sqty) NQty, convert(DECIMAL(15, 2), sum((td_bqty - td_sqty '
		  +' ) * td_rate * sm_multiplier)) NAmt, '''' AS td_debit_credit, sm_strikeprice, sm_callput, '
		  +' ''Commodity'' + ''|'' + '''' + ''|'' + replace(sm_symbol, ''&'', ''-'') + ''|'' + left( '
		  +' sm_productcd, 1) + ''|'' + sm_expirydt + ''|'' + Rtrim(Ltrim(Convert(CHAR, sm_strikeprice))) + ''|'' + '
		  +' sm_callput + ''|'' + sm_optionstyle  LinkCode, td_exchange + ''/'' + convert(CHAR, td_seriesid) AS Lookup '
	      +' FROM '+@strCommmexDB+'.dbo.trades (NOLOCK), '+@strCommmexDB+'.dbo.series_master(NOLOCK),  @tbl_UserList X '
	      +' WHERE td_clientcd = X.Client_COde AND sm_exchange = td_exchange AND  td_seriesid = '
		  +' sm_seriesid AND td_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+''' AND td_trxflag <> ''O'' '
	      +' GROUP BY td_clientcd, sm_symbol, sm_productcd, td_exchange,  sm_expirydt, sm_strikeprice, sm_callput, '
		  +' sm_optionstyle, td_exchange + ''/'' + convert(CHAR, td_seriesid) '
		  +' UNION ALL '
	      +' SELECT ex_clientcd AS td_clientcd, 8, '''', '''' AS td_trxdate, '''' AS td_isin_code, '''' AS sc_company_name, '
		  +' cast((CASE WHEN sum(ex_aqty - ex_eqty) = 0 THEN 0 ELSE sum((ex_aqty - ex_eqty) * ex_diffrate * '
		  +' CASE ex_eaflag WHEN ''A'' THEN - 1 ELSE 1 END) / sum(ex_aqty - ex_eqty) END) AS DECIMAL(15, 2)) AS rate, '
		  +' ''Commodity'' + CASE ex_eaflag WHEN ''E'' THEN ''Exercise '' ELSE ''Assignment '' END Td_Type, '''', '''','
          +' rtrim(sm_symbol), CASE left(sm_productcd, 1) WHEN ''F'' THEN ''Fut '' ELSE ''Opt '' END + rtrim(sm_symbol) + ''  Exp: '' + ltrim('
		  +' rtrim(convert(CHAR, convert(DATETIME, sm_expirydt), 103))) + CASE left(sm_productcd, 1) WHEN ''F'' '
		  +' THEN '''' ELSE '''' + rtrim(convert(CHAR(9), sm_strikeprice)) + sm_callput + sm_optionstyle END, sum '
		  +' (ex_aqty) Bqty, convert(DECIMAL(15, 2), sum(ex_aqty * ex_diffrate * CASE ex_eaflag WHEN ''A'' THEN - 1 ELSE 1 '
		  +' END * sm_multiplier)) BAmt, sum(ex_eqty) Sqty, convert(DECIMAL(15, 2), sum(ex_eqty * '
		  +' ex_diffrate * CASE ex_eaflag WHEN ''A'' THEN - 1 ELSE 1 END * sm_multiplier)) SAmt, '
		  +' sum(ex_aqty - ex_eqty) NQty, convert(DECIMAL(15, 2), sum((ex_aqty - ex_eqty) * ex_diffrate '
		  +' * CASE ex_eaflag WHEN ''A'' THEN - 1 ELSE 1 END * sm_multiplier)) NAmt, '''' AS td_debit_credit, 0, '''', '
		  +' ''Commodity'' + CASE ex_eaflag WHEN ''E'' THEN ''Exercise'' ELSE '
		  +' ''Assignment'' END + ''|'' + '''' + ''|'' + replace(sm_symbol, ''&'', ''-'') + ''|'' + left(sm_productcd, 1) + ''|'' + '
		  +' sm_expirydt + ''|'' + Rtrim(Ltrim(Convert(CHAR, sm_strikeprice))) + ''|'' + sm_callput + ''|'' + sm_optionstyle as '
		  +' LinkCode, ex_exchange + ''/'' + Ltrim(convert(CHAR, ex_seriesid)) AS Lookup '
	      +' FROM '+@strCommmexDB+'.dbo.exercise WITH (NOLOCK), '+@strCommmexDB+'.dbo.series_master WITH (NOLOCK),  @tbl_UserList X '
	      +' WHERE ex_clientcd = X.Client_COde AND ex_exchange = sm_exchange AND ex_seriesid = '
		  +' sm_seriesid AND ex_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+''''
	      +' GROUP BY ex_clientcd, ex_eaflag, sm_symbol, sm_productcd, sm_expirydt, sm_strikeprice, sm_callput, '
		  +' sm_optionstyle, ex_exchange, ex_exchange + ''/'' + Ltrim(convert(CHAR, ex_seriesid)) '
        END
		SET @StrString = @StrString +' ) a ORDER BY ListOrder, td_type, snm, td_scripnm '
		BEGIN TRY
		 EXEC(@StrString) 
	     SET @o_vcErrorFlag  = 'S'
         SET @o_vcErrorMessage = 'Process Executed'
	     RETURN 1
	    END TRY
	    BEGIN CATCH
	      SET @o_vcErrorFlag  = 'E'
          SET @o_vcErrorMessage = ERROR_MESSAGE()
          RETURN 1
	    END CATCH
      END
	END
	ELSE
    IF @strRepType = 'Receipts'
    BEGIN
	  SET @StrString = @strHeader+' '+' SELECT ''1'' Type, ld_clientcd as ClientCode, ld_documentno DocumentNo, ltrim(rtrim(convert(CHAR, convert(DATETIME, ld_dt), 103))) DATE, '
	                   +' ld_Particular Particular, ld_Chequeno Chequeno, convert(DECIMAL(15, 2), '
					   +' CASE ld_documenttype WHEN ''R'' THEN (- 1) ELSE 1 END * ld_amount) Amount '
                       +' FROM ledger WITH (NOLOCK), @tbl_UserList X '
                       +' WHERE ld_documenttype = ''R'' AND ld_clientcd = X.Client_COde '
					   +' AND ld_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+''''  
                       +' ORDER BY ld_dt DESC '
	  BEGIN TRY
		EXEC(@StrString) 
	    SET @o_vcErrorFlag  = 'S'
        SET @o_vcErrorMessage = 'Process Executed'
	    RETURN 1
	  END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag  = 'E'
        SET @o_vcErrorMessage = ERROR_MESSAGE()
        RETURN 1
	  END CATCH			
	END
	ELSE
    IF @strRepType = 'Payments'
    BEGIN
	  SET @StrString = @strHeader+' '+' SELECT ''2'' Type,ld_clientcd as ClientCode,  ld_documentno DocumentNo, ltrim(rtrim(convert(CHAR, convert(DATETIME, ld_dt), 103))) DATE, '
	            +' ld_Particular Particular, ld_Chequeno Chequeno, convert(DECIMAL(15, 2), CASE ld_documenttype WHEN ''R'' THEN '
				+' (- 1) ELSE 1 END * ld_amount) Amount '
                +' FROM ledger WITH (NOLOCK), @tbl_UserList X '
                +' WHERE ld_documenttype = ''P'' AND ld_clientcd = X.Client_COde '
				+' AND ld_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+''''  
                +' ORDER BY ld_dt DESC '
	  BEGIN TRY
		EXEC(@StrString) 
	    SET @o_vcErrorFlag  = 'S'
        SET @o_vcErrorMessage = 'Process Executed'
	    RETURN 1
	  END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag  = 'E'
        SET @o_vcErrorMessage = ERROR_MESSAGE()
        RETURN 1
	  END CATCH			
	END
	ELSE
    IF @strRepType = 'Journals'
    BEGIN
	  SET @StrString = @strHeader+' '+' SELECT ''3'' Type, ld_clientcd as ClientCode, ld_documentno DocumentNo, ltrim(rtrim(convert(CHAR, convert(DATETIME, ld_dt), 103))) DATE, '
	       +' ld_Particular Particular, CASE ld_debitflag WHEN ''D'' THEN convert(DECIMAL(15, 2), ld_amount) ELSE 0 END Debit '
	       +' , CASE ld_debitflag WHEN ''D'' THEN 0 ELSE convert(DECIMAL(15, 2), - ld_amount) END Credit '
           +' FROM ledger WITH (NOLOCK) , @tbl_UserList X '
           +' WHERE ld_documenttype = ''J'' AND ld_clientcd = X.Client_COde  AND ld_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+''''  
           +' ORDER BY ld_dt DESC'
	  BEGIN TRY
		EXEC(@StrString) 
	    SET @o_vcErrorFlag  = 'S'
        SET @o_vcErrorMessage = 'Process Executed'
	    RETURN 1
	  END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag  = 'E'
        SET @o_vcErrorMessage = ERROR_MESSAGE()
        RETURN 1
	  END CATCH			
	END
	ELSE
    IF @strRepType = 'Bills'
    BEGIN
	  SET @StrString = @strHeader+' '+' SELECT ''4'' Type, ld_clientcd as ClientCode, ld_documentno DocumentNo, ltrim(rtrim(convert(CHAR, convert(DATETIME, ld_dt), 103))) DATE, '
	      +' ld_Particular Particular, CASE ld_debitflag WHEN ''D'' THEN convert(DECIMAL(15, 2), ld_amount) ELSE 0 END Debit '
	      +' , CASE ld_debitflag WHEN ''D'' THEN 0 ELSE convert(DECIMAL(15, 2), - ld_amount) END Credit '
          +' FROM ledger WITH (NOLOCK), @tbl_UserList X '
          +' WHERE ld_documenttype = ''B'' AND ld_clientcd = X.Client_COde AND ld_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+''''  
          +' ORDER BY ld_dt DESC '
	  BEGIN TRY
		EXEC(@StrString) 
	    SET @o_vcErrorFlag  = 'S'
        SET @o_vcErrorMessage = 'Process Executed'
	    RETURN 1
	  END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag  = 'E'
        SET @o_vcErrorMessage = ERROR_MESSAGE()
        RETURN 1
	  END CATCH			
	END
	ELSE
    IF @strRepType = 'AGTS'
    BEGIN
	  IF @strRepSubType = 'Cash' 
	  BEGIN
	    SET @StrString = @strHeader+' '+' Select td_clientcd, td_scripcd, ss_name AS ScripName, td_stlmnt, td_dt, td_bsflag, '
		 +' CASE td_bsflag WHEN ''B'' THEN td_bqty WHEN ''S'' THEN td_sqty ELSE 0 END Qty, '
         +' td_marketrate as MarketRate, td_brokerage as Brokerage, td_rate as TradeRate, '
         +' TradeValue = cast(round((CASE td_bsflag WHEN ''B'' THEN td_bqty WHEN ''S'' THEN td_sqty ELSE 0 END)*td_rate,2) as money), '
         +' SEBITO = dbo.fnDecryptN(td_Chrg3), '
         +' GST = dbo.fnDecryptN(td_Chrg6), ExchTrxnChrg = dbo.fnDecryptN(td_Chrg7), '
         +' STT = dbo.fnDecryptN(td_Chrg5), StampDuty = dbo.fnDecryptN(td_Chrg2), OtherCharges = dbo.fnDecryptN(td_Chrg4) '
         +' From GLOBAL_TRX(NOLOCK), Client_master(NOLOCK), Securities(NOLOCK), @tbl_UserList X  '
         +' Where td_clientcd = cm_cd and td_dt between '''+@dtFromDate+''' AND '''+@dtToDate+''' and td_clientcd = x.Client_Code '
         +' AND td_scripcd = ss_cd '
	    BEGIN TRY
		  EXEC(@StrString) 
	      SET @o_vcErrorFlag  = 'S'
          SET @o_vcErrorMessage = 'Process Executed'
	      RETURN 1
	    END TRY
	    BEGIN CATCH
	      SET @o_vcErrorFlag  = 'E'
          SET @o_vcErrorMessage = ERROR_MESSAGE()
          RETURN 1
	    END CATCH			
	  END
	  ELSE IF @strRepSubType in('FO','FX') 
	  BEGIN
	    SET @StrString = @strHeader+' '+' SELECT td_clientcd, td_dt, td_seriesid, sm_symbol, sm_sname, td_bsflag, CASE td_bsflag '
		+' WHEN ''B'' THEN td_bqty WHEN ''S'' THEN td_sqty ELSE 0 END AS Qty, '
        +' td_marketrate as MarketRate, Brokerage = td_brokerage, td_rate as TradeRate, '
        +' cast(CASE td_bsflag WHEN ''B'' THEN Round((td_bqty * td_rate * sm_multiplier), 2) WHEN ''S'' '
        +' THEN Round((td_sqty * td_rate * sm_multiplier), 2) ELSE 0 END as money) TardeValue, '
        +' TrxnChrg = dbo.fnDecryptN(td_Chrg1), StampDuty = dbo.fnDecryptN(td_Chrg2), SEBITO = dbo.fnDecryptN(td_Chrg3),  '
        +' Other = dbo.fnDecryptN(td_Chrg4), STT = dbo.fnDecryptN(td_Chrg5), GST= dbo.fnDecryptN(td_Chrg6) '
        +' FROM Global_Trades(NOLOCK), client_master(NOLOCK), Series_Master(NOLOCK), @tbl_UserList X   '
        +' WHERE td_seriesId = sm_seriesid AND td_exchange = SM_exchange AND td_Segment = sm_Segment AND td_clientcd = cm_cd  '
        +' AND td_dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+''' AND td_Segment = IIF('''+@strRepSubType+'''=''FO'',''F'',''X'') '
		+' AND td_clientcd = x.Client_Code '
        +' order by td_dt, sm_symbol '
	    BEGIN TRY
		  EXEC(@StrString) 
	      SET @o_vcErrorFlag  = 'S'
          SET @o_vcErrorMessage = 'Process Executed'
	      RETURN 1
	    END TRY
	    BEGIN CATCH
	      SET @o_vcErrorFlag  = 'E'
          SET @o_vcErrorMessage = ERROR_MESSAGE()
          RETURN 1
	    END CATCH			
	  END
	END  
	ELSE IF @strRepType = 'Deliveries'
	BEGIN
	  IF @strRepSubType = 'ItemWise' 
	  BEGIN
	    SET @StrString = @strHeader+' '+' SELECT ClientCode,  DATE, TrxNo, Description, Debit, Credit, SUM(Debit-Credit) '
		+' OVER(PARTITION  BY ClientCode  ORDER BY RowNumber) AS Balance, Beneficiery, Settlment FROM('
		+' SELECT x.Client_Code as ClientCode, td_trxdate AS DATE, td_reference AS TrxNo, CASE td_debit_credit WHEN ''D'''
		                               +' THEN cast((td_qty ) AS DECIMAL(15, 0)) ELSE 0 END ''Debit'', '
									   +' CASE td_debit_credit WHEN ''C'' THEN cast((td_qty) AS DECIMAL(15, 0)) '
									   +' ELSE 0 END ''Credit'', 0 ''Balance'', td_description AS Description, '
	                                   +' td_beneficiery AS Beneficiery, td_settlement AS Settlment, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RowNumber '
                        +' FROM ['+@strDefaultServer+'].'+@strDefaultConn+'.[dbo].TrxDetail a WITH (NOLOCK), ' 
						+' ['+@strDefaultServer+'].'+@strDefaultConn+'.[dbo].Security WITH (NOLOCK), Client_master WITH (NOLOCK), '
						+' ['+@strDefaultServer+'].'+@strDefaultConn+'.[dbo].Beneficiary_type WITH (NOLOCK), DematAct WITH (NOLOCK), @tbl_UserList X '
                        +' WHERE td_isin_code = sc_isincode AND td_ac_type = bt_code AND '
						+' td_trxdate BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+'''' 
						+' AND td_ac_code = da_actno AND da_clientcd = cm_cd AND cm_cd = x.Client_Code) X1  '
		BEGIN TRY
		  EXEC(@StrString) 
	      SET @o_vcErrorFlag  = 'S'
          SET @o_vcErrorMessage = 'Process Executed'
	      RETURN 1
	    END TRY
	    BEGIN CATCH
	      SET @o_vcErrorFlag  = 'E'
          SET @o_vcErrorMessage = ERROR_MESSAGE()
          RETURN 1
	    END CATCH			
	  END
	  ELSE IF @strRepSubType = 'DateWise' 
	  BEGIN
	    SET @StrString = @strHeader+' '+' SELECT  ClientCode, DATE, ISIN, SecurityDescription, Debit, Credit, SUM(Debit-Credit) '
		+' OVER(PARTITION  BY ClientCode  ORDER BY RowNumber) AS Balance FROM( '
		+' SELECT X.Client_code as ClientCode, td_trxdate AS DATE, td_isin_code AS ISIN, sc_isinname AS SecurityDescription, CASE td_debit_credit WHEN '
			+' ''D'' THEN cast((td_qty) AS DECIMAL(15, 0)) ELSE 0 END ''Debit'', CASE '
		+' td_debit_credit WHEN ''C'' THEN cast((td_qty) AS DECIMAL(15, 0)) ELSE 0 END ''Credit'', ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RowNumber  '
        +' FROM ['+@strDefaultServer+'].'+@strDefaultConn+'.DBO.TrxDetail a WITH (NOLOCK), '
		+' ['+@strDefaultServer+'].'+@strDefaultConn+'.[dbo].Security WITH (NOLOCK), Client_master WITH (NOLOCK), '
		+' ['+@strDefaultServer+'].'+@strDefaultConn+'.[dbo].Beneficiary_type WITH (NOLOCK), DematAct WITH (NOLOCK), @tbl_UserList X '
        +' WHERE td_isin_code = sc_isincode AND td_ac_type = bt_code '
		+' AND td_trxdate BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+''''  
		+' AND td_ac_code = da_actno AND da_clientcd = cm_cd AND cm_cd  = x.Client_Code) x123 '
		BEGIN TRY
		  EXEC(@StrString) 
	      SET @o_vcErrorFlag  = 'S'
          SET @o_vcErrorMessage = 'Process Executed'
	      RETURN 1
	    END TRY
	    BEGIN CATCH
	      SET @o_vcErrorFlag  = 'E'
          SET @o_vcErrorMessage = ERROR_MESSAGE()
          RETURN 1
	    END CATCH	
	  END
	END
  END
  ELSE if @strReportName = 'CapitalGain'
  BEGIN
    IF @strRepType = 'Dividend'
    BEGIN
	  SET @StrString = @strHeader+' '+'SELECT DV_Clientcd ClientCode, cm_name As ClientName,  DV_Dt DividendDate, DV_Scripcd ScripCode, ss_lname ScripName, '
      +' DV_NoOfShare NoOfShare, cast(DV_Amount / DV_NoOfShare AS MONEY) DivRate, DV_Amount Amount '
      +' FROM INVPL_DIVIDEND(NOLOCK), Securities(NOLOCK), Client_Master(NOLOCK), Branch_master(NOLOCK),  @tbl_UserList X '
      +' WHERE DV_Clientcd = cm_cd AND CM_CD = X.CLIENT_cODE AND cm_brboffcode = bm_branchcd '
	  +' AND DV_scripcd = ss_cd AND DV_Amount > 0 '
	  +' AND DV_Dt BETWEEN '''+@dtFromDate+''' AND '''+@dtToDate+'''' 
      +' ORDER BY DV_Clientcd, DV_Dt, ss_lname '
      BEGIN TRY
		 EXEC(@StrString) 
	     SET @o_vcErrorFlag  = 'S'
         SET @o_vcErrorMessage = 'Process Executed'
	     RETURN 1
	  END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag  = 'E'
        SET @o_vcErrorMessage = ERROR_MESSAGE()
        RETURN 1
	  END CATCH
	END
  END
END
GO

CREATE PROCEDURE stpr_GetChatbotData @vcXML NVARCHAR(MAX)
WITH ENCRYPTION
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
  DECLARE @dtFromDate VARCHAR(8), @dtToDate VARCHAR(8)='', @XMLData XML 
  DECLARE @o_vcErrorFlag VARCHAR(1), @o_vcErrorMessage VARCHAR(500), @i_vcReportCode VARCHAR(50),   
  @i_vcParamValue VARCHAR(50), @i_vcUserCode VARCHAR(50), @i_vcProduct VARCHAR(50), @i_vcMainProduct VARCHAR(50)=''
  DECLARE @InputDate DATE = CAST(GETDATE() AS DATE), @ExchSeg VARCHAR(50) = '', @StrCompanyCode VARCHAR(1)='A', @strSubReportCode VARCHAR(50) = '', @strReportDuration VARCHAR(50) = ''
  DECLARE @FinancialYearStart DATE;
  IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
  END 
  
  SET @XMLData = CAST(@vcXML AS XML)

  SELECT @i_vcReportCode =  Isnull(@XMLData.value('(ReportCode)[1]', 'VARCHAR(50)'),''),
  @i_vcMainProduct =  Isnull(@XMLData.value('(Product)[1]', 'VARCHAR(50)'),''),
  @i_vcParamValue =    Isnull(@XMLData.value('(ParamValue)[1]', 'VARCHAR(50)'),''),
  @i_vcUserCode =  Isnull(@XMLData.value('(UserId)[1]', 'VARCHAR(500)'),''),
  @ExchSeg =  Isnull(@XMLData.value('(ExchSeg)[1]', 'VARCHAR(50)'),''),
  @StrCompanyCode =  Isnull(@XMLData.value('(CompanyCode)[1]', 'VARCHAR(1)'),''),
  @strSubReportCode = Isnull(@XMLData.value('(SubReport)[1]', 'VARCHAR(50)'),'')
  
  IF @i_vcUserCode = ''
  BEGIN
    RETURN 1
  END
  
  if @i_vcMainProduct = 'Demat'
  BEGIN
    SET @i_vcProduct = 'DP'
  END
  ELSE
  BEGIN
    SET @i_vcProduct = @i_vcMainProduct
  END
  IF @i_vcReportCode =  'Ledger Report'
  BEGIN
    IF @i_vcParamValue = 'Last 7 Days'
	BEGIN
	  SET @dtFromDate = CONVERT(VARCHAR,CAST(DATEADD(DAY,-8,GETDATE()) AS DATE) ,112)
      SET @dtToDate = CONVERT(VARCHAR,cast(GETDATE() AS DATE) ,112)
	END
	ELSE IF  @i_vcParamValue = 'Current Month'
	BEGIN
	  IF CAST(GETDATE() AS DATE) = CAST(DATEADD(mm, DATEDIFF(m,0,GETDATE()),0) AS DATE)
	  BEGIN
	    SET @dtFromDate = CONVERT(VARCHAR,CAST(DATEADD(MONTH,-1,GETDATE()) AS DATE) ,112)
        SET @dtToDate = CONVERT(VARCHAR,CAST(GETDATE() AS DATE) ,112)
	  END
	  ELSE
	  BEGIN
	    SET @dtFromDate = CONVERT(VARCHAR,cast(FORMAT(GETDATE(),'yyyy-MM')+'-01' as date) ,112)
        SET @dtToDate = CONVERT(VARCHAR,GETDATE() ,112)
	  END
	END
	ELSE IF @i_vcParamValue = 'Current FY'
	BEGIN
      IF DATEPART(MONTH, @InputDate) >= 4
         SET @FinancialYearStart = DATEFROMPARTS(YEAR(@InputDate), 4, 1);
      ELSE
         SET @FinancialYearStart = DATEFROMPARTS(YEAR(@InputDate) - 1, 4, 1);
      DECLARE @FinancialYearEnd DATE = DATEADD(DAY, -1, DATEFROMPARTS(YEAR(@FinancialYearStart) + 1, 4, 1));
      SET @dtFromDate = CONVERT(VARCHAR,@FinancialYearStart ,112)
      SET @dtToDate = CONVERT(VARCHAR,GETDATE() ,112)
	END
	ELSE IF  @i_vcParamValue = 'Previous FY'
	BEGIN
	  set @InputDate = CAST(GETDATE() AS DATE)
      IF DATEPART(MONTH, @InputDate) >= 4
         SET @FinancialYearStart = DATEFROMPARTS(YEAR(@InputDate), 4, 1);
      ELSE
         SET @FinancialYearStart = DATEFROMPARTS(YEAR(@InputDate) - 1, 4, 1);
         set @FinancialYearEnd = DATEADD(DAY, -1, DATEFROMPARTS(YEAR(@FinancialYearStart) + 1, 4, 1));
	  
      SET @dtFromDate = CONVERT(VARCHAR,dateadd(year,-1,@FinancialYearStart) ,112)
      SET @dtToDate = CONVERT(VARCHAR,dateadd(year,-1,@FinancialYearEnd) ,112)
	END
	SET @vcXML ='<FromDt>'+@dtFromDate+'</FromDt><ToDt>'+@dtToDate+'></ToDt><ExchSeg></ExchSeg><UserId>'+@i_vcUserCode+'</UserId><AccountType>EM,MTF,CX,CM</AccountType><Product>'+@i_vcProduct+'</Product><SelectTag></SelectTag><SelectUsers></SelectUsers><OutputType>G</OutputType><SplFilter></SplFilter><CompanyCode>'+@StrCompanyCode+'</CompanyCode>'
    EXEC stpr_Rpt_LedgerNew @vcXML, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT 
	--RETURN 1
  END	
  IF @i_vcReportCode =  'Holding'
  BEGIN
    SET @dtFromDate = CONVERT(VARCHAR,GETDATE(),112)
    SET @vcXML ='<AsOnDate>'+@dtFromDate+'</AsOnDate><UserId>'+@i_vcUserCode+'</UserId><Product>'+@i_vcProduct+'</Product><SelectTag></SelectTag><SelectUsers></SelectUsers><OutputType>G</OutputType><SplFilter></SplFilter><CompanyCode>'+@StrCompanyCode+'</CompanyCode>'
    EXEC stpr_Rpt_HoldingNew @vcXML, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT 
	--RETURN 1
  END
  IF @i_vcReportCode =  'Outstanding Position'
  BEGIN
    SET @dtFromDate = CONVERT(VARCHAR,GETDATE(),112)
    SET @vcXML ='<AsOnDate>'+@dtFromDate+'</AsOnDate><ExchSeg>'+@ExchSeg+'</ExchSeg><UserId>'+@i_vcUserCode+'</UserId><SelectTag></SelectTag><SelectUsers></SelectUsers><OutputType>G</OutputType><SplFilter></SplFilter><CompanyCode>'+@StrCompanyCode+'</CompanyCode>'
    EXEC stpr_Rpt_OSPositionNew @vcXML, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT 
	--RETURN 1
  END
  IF @i_vcReportCode =  'Ledger Balance'
  BEGIN
    SET @dtFromDate = CONVERT(VARCHAR,GETDATE(),112)
    SET @vcXML ='<AsOnDate>'+@dtFromDate+'</AsOnDate><ExchSeg>'+@ExchSeg+'</ExchSeg><UserId>'+@i_vcUserCode+'</UserId><AccountType>EM,MTF,CX,CM</AccountType><SelectTag></SelectTag><SelectUsers></SelectUsers><OutputType>G</OutputType><SplFilter></SplFilter><CompanyCode>'+@StrCompanyCode+'</CompanyCode>'
    EXEC sp_LedgerBalance @vcXML, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT 
	RETURN 1
  END
  IF @i_vcReportCode =  'Margin Statement' OR @i_vcReportCode = 'Combined Contract Note'
  BEGIN
    DECLARE @tbl_Esign TABLE(EsignBase64  NVARCHAR(MAX), EsignFileName VARCHAR(100))
    DECLARE @StrBinaryFile NVARCHAR(MAX)='', @strString VARCHAR(MAX)='', @strEsignDB VARCHAR(50)='', @strDocType VARCHAR(6)=''

	IF @i_vcReportCode = 'Margin Statement'
	BEGIN
	  SET @strDocType = 'CMRG'
	END
	ELSE
	BEGIN
	  SET @strDocType = 'CNOTE'
	END
   
    SELECT @strEsignDB = '['+ LTRIM(RTRIM(OP_Server)) +'].[' + LTRIM(RTRIM(OP_DataBase)) + ']'
    FROM Other_Products(NOLOCK) WHERE OP_Product = 'ESIGN-TRADEPLUS' and RTRIM(LTRIM(op_status)) = 'A'
    IF @strEsignDB <> ''
	BEGIN
      SET @strString = 'SELECT DBO.fnBinaryToBase64(dd_signature), DD_FileName '
      +' FROM  '+@strEsignDB+'.DBO.Digital_details WITH (NOLOCK) WHERE dd_filetype = ''' + @strDocType + ''' AND dd_clientcd = '''+@i_vcUserCode+'''' 
      +' AND DD_DT IN(SELECT MAX(DD_DT) FROM  '+@strEsignDB+'.DBO.Digital_details WITH (NOLOCK) WHERE dd_filetype = ''' + @strDocType + ''''
      +' AND dd_clientcd = '''+@i_vcUserCode+''') '
      INSERT INTO @tbl_Esign
      EXEC(@strString)
      SELECT * from @tbl_Esign
	END  
  END
  IF @i_vcReportCode =  'CML'
  BEGIN
  SET @vcXML ='<UserId>'+@i_vcUserCode+'</UserId><Product>'+@i_vcProduct+'</Product>'
  EXEC GetCMLData @vcXML, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT
  return 1
  END

  IF @i_vcReportCode =  'Capital Gain Loss'
  BEGIN
    IF  @i_vcParamValue = 'Current Month'
	BEGIN
	  IF CAST(GETDATE() AS DATE) = CAST(DATEADD(mm, DATEDIFF(m,0,GETDATE()),0) AS DATE)
	  BEGIN
	    SET @dtFromDate = CONVERT(VARCHAR,CAST(DATEADD(MONTH,-1,GETDATE()) AS DATE) ,112)
        SET @dtToDate = CONVERT(VARCHAR,CAST(GETDATE() AS DATE) ,112)
	  END
	  ELSE
	  BEGIN
	    SET @dtFromDate = CONVERT(VARCHAR,cast(FORMAT(GETDATE(),'yyyy-MM')+'-01' as date) ,112)
        SET @dtToDate = CONVERT(VARCHAR,GETDATE() ,112)
	  END
	END
	ELSE IF @i_vcParamValue = 'Current FY'
	BEGIN
      IF DATEPART(MONTH, @InputDate) >= 4
         SET @FinancialYearStart = DATEFROMPARTS(YEAR(@InputDate), 4, 1);
      ELSE
         SET @FinancialYearStart = DATEFROMPARTS(YEAR(@InputDate) - 1, 4, 1);
      DECLARE @FinancialYearEnd1 DATE = DATEADD(DAY, -1, DATEFROMPARTS(YEAR(@FinancialYearStart) + 1, 4, 1));
      SET @dtFromDate = CONVERT(VARCHAR,@FinancialYearStart ,112)
      SET @dtToDate = CONVERT(VARCHAR,GETDATE() ,112)
	END
	ELSE IF  @i_vcParamValue = 'Previous FY'
	BEGIN
	  set @InputDate = CAST(GETDATE() AS DATE)
      IF DATEPART(MONTH, @InputDate) >= 4
         SET @FinancialYearStart = DATEFROMPARTS(YEAR(@InputDate), 4, 1);
      ELSE
         SET @FinancialYearStart = DATEFROMPARTS(YEAR(@InputDate) - 1, 4, 1);
         set @FinancialYearEnd1 = DATEADD(DAY, -1, DATEFROMPARTS(YEAR(@FinancialYearStart) + 1, 4, 1));
	  
      SET @dtFromDate = CONVERT(VARCHAR,dateadd(year,-1,@FinancialYearStart) ,112)
      SET @dtToDate = CONVERT(VARCHAR,dateadd(year,-1,@FinancialYearEnd1) ,112)
	END
	ELSE IF  @i_vcParamValue = 'Today Date'
	BEGIN
      SET @dtFromDate = CONVERT(VARCHAR,CAST(GETDATE() AS DATE) ,112)
      SET @dtToDate = CONVERT(VARCHAR,CAST(GETDATE() AS DATE) ,112)
	END
	ELSE IF  @i_vcParamValue = 'Previous FY End Date'
	BEGIN
      SET @InputDate = CAST(GETDATE() AS DATE)
	  DECLARE @PreviousFinancialYearEnd DATE;

	  IF DATEPART(MONTH, @InputDate) >= 4
	  BEGIN
	    SET @PreviousFinancialYearEnd = DATEFROMPARTS(YEAR(@InputDate), 3, 31);
	  END
	  ELSE
	  BEGIN
	    SET @PreviousFinancialYearEnd = DATEFROMPARTS(YEAR(@InputDate) - 1, 3, 31);
	  END

      SET @dtFromDate = CONVERT(VARCHAR,@PreviousFinancialYearEnd ,112)
      SET @dtToDate = CONVERT(VARCHAR,@PreviousFinancialYearEnd ,112)
	END

	DECLARE @strReportCodeNew VARCHAR(100) = ''
	IF @strSubReportCode = 'Actual PL Summary'
	BEGIN
	  SET @strReportCodeNew = 'Actual PL_Summary'
	  SET @strReportDuration = 'From ' + CONVERT(VARCHAR, CAST(@dtFromDate AS DATE), 113) + ' to ' + CONVERT(VARCHAR, CAST(@dtToDate AS DATE), 113)
	  SET @vcXML = '<FromDt>'+@dtFromDate+'</FromDt><ToDt>'+@dtToDate+'</ToDt><Product>'+@i_vcProduct+'</Product><UserId>'+@i_vcUserCode+'</UserId><SelectTag></SelectTag><SelectUsers></SelectUsers><OutputType>G</OutputType><SplFilter></SplFilter><RepType>' + @strReportCodeNew + '</RepType><RepSubType></RepSubType><Option112A>N</Option112A><POS>N</POS>'
	  --SET @vcXML ='<FromDt>'+@dtFromDate+'</FromDt><ToDt>'+@dtToDate+'></ToDt><ExchSeg></ExchSeg><UserId>'+@i_vcUserCode+'</UserId><AccountType>EM,MTF,CX,CM</AccountType><Product>'+@i_vcProduct+'</Product><SelectTag></SelectTag><SelectUsers></SelectUsers><OutputType>G</OutputType><SplFilter></SplFilter><CompanyCode>'+@StrCompanyCode+'</CompanyCode>'
      EXEC stpr_Rpt_CapitalGain @vcXML, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT
	END
	ELSE IF @strSubReportCode = 'Actual PL Detail'
	BEGIN
	  SET @strReportCodeNew = 'Actual PL_Detail'
	  SET @strReportDuration = 'From ' + CONVERT(VARCHAR, CAST(@dtFromDate AS DATE), 113) + ' to ' + CONVERT(VARCHAR, CAST(@dtToDate AS DATE), 113)
	  --SET @vcXML = '<FromDt>'+@dtFromDate+'</FromDt><ToDt>'+@dtToDate+'</ToDt><Product>'+@i_vcProduct+'</Product><UserId>'+@i_vcUserCode+'</UserId><SelectTag></SelectTag><SelectUsers></SelectUsers><OutputType>G</OutputType><SplFilter></SplFilter><RepType>' + @strReportCodeNew + '</RepType><RepSubType></RepSubType><Option112A></Option112A><POS>N</POS>'
	  SET @vcXML ='<FromDt>'+@dtFromDate+'</FromDt><ToDt>'+@dtToDate+'></ToDt><ExchSeg></ExchSeg><UserId>'+@i_vcUserCode+'</UserId><AccountType>EM,MTF,CX,CM</AccountType><Product>'+@i_vcProduct+'</Product><SelectTag></SelectTag><SelectUsers></SelectUsers><OutputType>G</OutputType><SplFilter></SplFilter><Option112A>N</Option112A><POS>N</POS><ShortSale>Y</ShortSale>'
      EXEC stpr_Rpt_CapitalGain @vcXML, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT
	  --PRINT @vcXML
	END
	ELSE IF @strSubReportCode = 'Notional Summary'
	BEGIN
	  SET @strReportCodeNew = 'Notional_Summary'
	  SET @strReportDuration = 'As on ' + CONVERT(VARCHAR, CAST(@dtFromDate AS DATE), 113)
	  SET @vcXML = '<FromDt>'+@dtFromDate+'</FromDt><ToDt>'+@dtToDate+'</ToDt><Product>'+@i_vcProduct+'</Product><UserId>'+@i_vcUserCode+'</UserId><SelectTag></SelectTag><SelectUsers></SelectUsers><OutputType>G</OutputType><SplFilter></SplFilter><RepType>' 
	  + @strReportCodeNew + '</RepType><RepSubType></RepSubType><Option112A>N</Option112A>'
	  EXEC stpr_Rpt_CapitalGainNotional @vcXML, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT
	END 
	ELSE IF @strSubReportCode = 'Notional Detail'
	BEGIN
	  SET @strReportCodeNew = 'Notional_Detail'
	  SET @strReportDuration = 'As on ' + CONVERT(VARCHAR, CAST(@dtFromDate AS DATE), 113)
	  SET @vcXML = '<FromDt>'+@dtFromDate+'</FromDt><ToDt>'+@dtToDate+'</ToDt><Product>'+@i_vcProduct+'</Product><UserId>'+@i_vcUserCode+'</UserId><SelectTag></SelectTag><SelectUsers></SelectUsers><OutputType>G</OutputType><SplFilter></SplFilter><RepType>' 
	  + @strReportCodeNew + '</RepType><RepSubType></RepSubType><Option112A>N</Option112A>'
	  EXEC stpr_Rpt_CapitalGainNotional @vcXML, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT
	END
	ELSE IF @strSubReportCode = 'Dividend'
	BEGIN
	  SET @strReportCodeNew = 'Dividend'
	  SET @strReportDuration = 'From ' + CONVERT(VARCHAR, CAST(@dtFromDate AS DATE), 113) + ' to ' + CONVERT(VARCHAR, CAST(@dtToDate AS DATE), 113)
	  SET @vcXML = '<FromDt>'+@dtFromDate+'</FromDt><ToDt>'+@dtToDate+'></ToDt><Product>'+@i_vcProduct+'</Product><UserId>'+@i_vcUserCode+'</UserId><SelectTag></SelectTag><SelectUsers></SelectUsers><OutputType>G</OutputType><SplFilter></SplFilter><ReportName>CapitalGain</ReportName><RepType>' + @strReportCodeNew + '</RepType><RepSubType></RepSubType><Option112A></Option112A>'
	  EXEC stpr_Rpt_OtherReports @vcXML, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT
	END
  END

  IF @i_vcUserCode <> ''
  BEGIN
  
    DECLARE @i_vcReportName VARCHAR(100) = ''

	IF @strSubReportCode <> ''
	BEGIN
	  SET @i_vcReportName = @i_vcReportCode + '|' + @strSubReportCode
	END
	ELSE
	BEGIN
	  SET @i_vcReportName = @i_vcReportCode
	END

    SELECT ReportName, Product, ColumnType, ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, ColumnFormat, DecimalPlace, ColumnTotal,
    OrderBy FROM tbl_ChatbotPDFConfig(NOLOCK) 
    WHERE ReportName = @i_vcReportName and Product = @i_vcMainProduct 
    ORDER BY ColumnType,OrderBy
  
    DECLARE @strPanNo VARCHAR(12)='', @strAddress VARCHAR(500)=''
    Select @strPanNo = cm_Panno, @strAddress = cm_add1+' '+cm_add2+' '+cm_add3
	FROM CLIENT_MASTER(NOLOCK) XM
	WHERE  ((CM_CD = @i_vcUserCode and @i_vcProduct = 'Trading') or 
	(@i_vcMainProduct = 'Demat' and exists(select 1 from Dematact where da_clientcd = XM.CM_CD AND da_defaultyn='Y' AND da_actno = @i_vcUserCode)))
	
	
    SELECT 
	--ReportHeading = REPLACE(REPLACE(Replace(ReportHeading, '<FROMDATE>',convert(varchar,cast(@dtFromDate as date),113)),'<TODATE>',convert(varchar,cast(@dtToDate as date),113)),
	--'<AsOnDate>',convert(varchar,cast(@dtFromDate as date),113)),
	ReportHeading = REPLACE(REPLACE(REPLACE(REPLACE(
        REPLACE(
            REPLACE(ReportHeading, '<FROMDATE>', CONVERT(VARCHAR, CAST(@dtFromDate AS DATE), 113)),
            '<TODATE>', CONVERT(VARCHAR, CAST(@dtToDate AS DATE), 113)
        ),
        '<AsOnDate>', CONVERT(VARCHAR, CAST(@dtFromDate AS DATE), 113)
    ),'<REPORTCHOICE>',@i_vcReportCode),'<SUBREPORTCHOICE>',@strSubReportCode),'<REPORTDURATION>',@strReportDuration),
	PrintAddress, PassWord = @strPanNo, Address = @strAddress, Orientation = ReportOrientation
    FROM tbl_WhatsAppChatbotConfig(NOLOCK) WHERE ReportName = @i_vcReportCode and product = @i_vcMainProduct
  END
END
GO
