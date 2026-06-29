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
      ORDER BY ClientCode, ScripCode

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
  DECLARE @InputDate DATE = CAST(GETDATE() AS DATE), @ExchSeg VARCHAR(50) = '', @StrCompanyCode VARCHAR(1)='A'
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
  @StrCompanyCode =  Isnull(@XMLData.value('(CompanyCode)[1]', 'VARCHAR(1)'),'')
  
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
  IF @i_vcUserCode <> ''
  BEGIN
  
    SELECT ReportName, Product, ColumnType, ColumnName, ColumnHeading, ColumnWidth, ColumnAlignement, ColumnFormat, DecimalPlace, ColumnTotal,
    OrderBy FROM tbl_ChatbotPDFConfig(NOLOCK) 
    WHERE ReportName = @i_vcReportCode and Product = @i_vcMainProduct 
    ORDER BY OrderBy
  
    DECLARE @strPanNo VARCHAR(12)='', @strAddress VARCHAR(500)=''
    Select @strPanNo = cm_Panno, @strAddress = cm_add1+' '+cm_add2+' '+cm_add3
	FROM CLIENT_MASTER(NOLOCK) XM
	WHERE  ((CM_CD = @i_vcUserCode and @i_vcProduct = 'Trading') or 
	(@i_vcMainProduct = 'Demat' and exists(select 1 from Dematact where da_clientcd = XM.CM_CD AND da_defaultyn='Y' AND da_actno = @i_vcUserCode)))
	
	
    SELECT ReportHeading = 
	REPLACE(REPLACE(Replace(ReportHeading, '<FROMDATE>',convert(varchar,cast(@dtFromDate as date),113)),'<TODATE>',convert(varchar,cast(@dtToDate as date),113)),
	'<AsOnDate>',convert(varchar,cast(@dtFromDate as date),113)),
	PrintAddress, PassWord = @strPanNo, Address = @strAddress, Orientation = ReportOrientation
    FROM tbl_WhatsAppChatbotConfig(NOLOCK) WHERE ReportName = @i_vcReportCode and product = @i_vcMainProduct
  END
ENDselect * from
GO
