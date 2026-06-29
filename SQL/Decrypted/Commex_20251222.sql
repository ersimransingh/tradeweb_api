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

CREATE PROCEDURE stpr_Rpt_BillReport @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, 
@o_vcErrorMessage VARCHAR(MAX) OUTPUT 
WITH ENCRYPTION
AS
BEGIN
 /*
 ///////////////////////////////////////////////////////////////////////////////////////////
 // Create By     : VAIBHAV GARG
 // Created Date  : 13-DEC-2023
 // Description   : 
 // Reviewed By   : 
 // Review Date   : 
 //////////////////////////////////////////////////////////////////////////////////////////
*/
 --- Parameter Declaration
 
  DECLARE @dtFromDate VARCHAR(8), @dtToDt VARCHAR(8), @strUserId VARCHAR(500) = '', @strExchSeg VARCHAR(100),
  @XMLData XML, @strAccountType VARCHAR(500)='', @strTable VARCHAR(50)='', @strString VARCHAR(MAX) = '',
  @blnTplusCommex BIT, @StrCommexConn VARCHAR(MAX) = '', @strCommTable VARCHAR(100)='', @strCommClientMaster VARCHAR(100)='',
  @strCommCompanyExchange VARCHAR(100)='', @strsql1 VARCHAR(500)='', @strsqlstart VARCHAR(MAX)='', @strsqlLast VARCHAR(500)='',
  @strsqlHeader VARCHAR(MAX)='', @strSqlMain VARCHAR(MAX)='', @strSqlExecute VARCHAR(MAX)='', @strOutputType VARCHAR(1), 
  @strProduct VARCHAR(50)='', @strSelectTag VARCHAR(1)='', @strSelectUsers VARCHAR(500)='', @ExchSeg VARCHAR(50)='', 
  @strStringMin VARCHAR(MAX)='', @strSplFilter VARCHAR(MAX)='', @strClearingExchange VARCHAR(1) ='' , 
  @strConsiderOptionBF VARCHAR(1)='N'  , @strReportType VARCHAR(50)='', @strSettType VARCHAR(1)='', @strCompanyCode VARCHAR(1)='A',
  @strRepSubType VARCHAR(MAX)='', @strSettNo VARCHAR(20)='', @strLookUp VARCHAR(100)='',@StrBillOpen VARCHAR(100)='', @XMLDATA1 XML;
  
  IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
  END 
  SET @o_vcErrorFlag = 'S'
  SET @XMLData = CAST('<root>'+@vcXML+'</root>' AS XML)
  
  SELECT @dtFromDate = ISNULL(x.value('(FromDt)[1]', 'VARCHAR(8)'),''),
  @strProduct = ISNULL(x.value('(Product)[1]', 'VARCHAR(50)'),''),
  @dtToDt = ISNULL(x.value('(ToDt)[1]', 'VARCHAR(8)'),''),
  @strUserId = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'),''),
  @ExchSeg = ISNULL(x.value('(ExchSeg)[1]', 'VARCHAR(500)'),''),
  @strSelectTag = ISNULL(x.value('(SelectTag)[1]', 'VARCHAR(1)'),''),
  @strSelectUsers = ISNULL(x.value('(SelectUsers)[1]', 'VARCHAR(500)'),''),
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'),''),
  @strSplFilter = ISNULL(x.value('(SplFilter)[1]', 'VARCHAR(MAX)'),''),
  @strCompanyCode = ISNULL(x.value('(CompanyCode)[1]', 'VARCHAR(1)'),''),
  @strReportType = ISNULL(x.value('(RepType)[1]', 'VARCHAR(100)'),''),
  @strRepSubType = ISNULL(x.value('(RepSubType)[1]', 'VARCHAR(100)'),''),
  @strSettType = ISNULL(x.value('(SettType)[1]', 'VARCHAR(1)'),''),
  @strSettNo = ISNULL(x.value('(SettNo)[1]', 'VARCHAR(20)'),''),
  @strLookUp = ISNULL(x.value('(LookUp)[1]', 'VARCHAR(100)'),'')
  FROM @XMLData.nodes('/root') AS XTbl(x) 
  

  IF ISNULL(@strLookUp,'') <> ''
  BEGIN  
    IF CHARINDEX('/',@strLookUp) > 3
	BEGIN
	  SET @strUserId = SUBSTRING(@strLookUp,1,CHARINDEX('/',@strLookUp)-1)
	  SET @strLookUp = SUBSTRING(@strLookUp,CHARINDEX('/',@strLookUp)+1,LEN(@strLookUp))
	  SET @strSelectTag = ''
	  SET @strSelectUsers = ''
	END
--SELECT @strLookUp
  
    if SUBSTRING(@strLookUp,3,1)  ='C'
	BEGIN
	  SET @strProduct = 'CASH'
	  SELECT @dtFromDate = se_stdt, @strSettNo = se_stlmnt FROM Settlements(NOLOCK) 
      WHERE se_exchange+'/'+'C'+'/'+se_stlmnt+'/'+se_payoutdt = @strLookUp
	  SET @ExchSeg = ''
	  SET @dtToDt = @dtFromDate
	END
	ELSE IF SUBSTRING(@strLookUp,3,1)  ='F'
	BEGIN
      SET @strProduct = 'DERV'	  
	  SET @dtFromDate = SUBSTRING(@strLookUp,5,8)
	  SET @dtToDt = @dtFromDate
	END
  END
  
  --SELECT @dtFromDate, @strSettNo, @strProduct
  IF ISNULL(@strCompanyCode,'')=''
  BEGIN
    SET @strCompanyCode = 'A'
  END
  	
  SET @strStringMin  = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50)) '
    +' INSERT INTO @tbl_UserList(Client_Code) '
    +' SELECT '''+@strUserId+'''' 
  
  BEGIN TRY
    CREATE TABLE #tbl_ClientPLDETAIL (BillNo Numeric, BillDate VARCHAR(8), CompanyCode VARCHAR(10), Exchange VARCHAR(10), Segment VARCHAR(10),  ClientCode VARCHAR(50), 
	ClientName VARCHAR(100), 
    Symbol VARCHAR(50), seriesid INT, ExpiryDate VARCHAR(8), Scrip VARCHAR(100), Multiplier INT, 
	TrxnDate VARCHAR(8), OrderNo VARCHAR(50), TradeNo  VARCHAR(50), TradeTIME  VARCHAR(15), 
	BuyQty MONEY, SellQty Money, MarketRate MONEY, BuyValue MONEY, SellValue MONEY, NetValue MONEY)
	
	
	CREATE TABLE #tbl_ClientMainPL (SerialNo INT IDENTITY(1,1), BillNo Numeric, BillDate VARCHAR(8),CompanyCode VARCHAR(10), Exchange VARCHAR(10), Segment VARCHAR(10),  ClientCode VARCHAR(50), 
	ClientName VARCHAR(100), 
    Symbol VARCHAR(50), seriesid INT, ExpiryDate VARCHAR(8), Scrip VARCHAR(100), Multiplier INT, 
	TrxnDate VARCHAR(8), OrderNo VARCHAR(50), TradeNo  VARCHAR(50), TradeTIME  VARCHAR(15), 
	BuyQty MONEY, SellQty Money, MarketRate MONEY, BuyValue MONEY, SellValue MONEY, NetValue MONEY)
	
	CREATE INDEX indx_ClientPL ON #tbl_ClientPLDETAIL (seriesid)

    DECLARE @tbl_BSClient TABLE(CompanyCode VARCHAR(10), Exchange VARCHAR(10), Segment VARCHAR(10), ClientCode VARCHAR(50), ClientName VARCHAR(100), 
    Symbol VARCHAR(50), seriesid INT, ExpiryDate VARCHAR(8), Scrip VARCHAR(100), Multiplier INT, TrxnDate VARCHAR(8), BOT_QTY MONEY, BOT_VALUE MONEY, 
    SOLD_QTY MONEY, SOLD_VALUE MONEY)

    DECLARE @tbl_CLClient TABLE(CompanyCode VARCHAR(10), Exchange VARCHAR(10), Segment VARCHAR(10),  ClientCode VARCHAR(50), ClientName VARCHAR(100), 
    Symbol VARCHAR(50), seriesid INT, ExpiryDate VARCHAR(8), Scrip VARCHAR(100), Multiplier INT, EX_QTY MONEY, EX_VALUE MONEY, AS_QTY MONEY, AS_VALUE MONEY)
	
	
	 
    DECLARE @tbl_billCharges TABLE(CompanyCode VARCHAR(10), Exchange VARCHAR(10), Segment VARCHAR(10), 
    ClientCode VARCHAR(10) NOT NULL,
    ChargesDescp [char] (40) NOT NULL,
    [bc_amount] [money] NOT NULL)
  
  
    DECLARE @tbl_DervBill TABLE(BillNo Numeric, BillDate VARCHAR(8), Tag INT, Exchange VARCHAR(10),
	ClientCode VARCHAR(10), ClientName VARCHAR(100), seriesid VARCHAR(20), ExpiryDate VARCHAR(8), Scrip VARCHAR(100), Multiplier INT, BF_QTY MONEY, 
	BF_CloseRate MONEY, BF_VALUE MONEY, BOT_QTY MONEY, BOT_VALUE MONEY, 
    SOLD_QTY MONEY,SOLD_VALUE MONEY, EX_QTY MONEY, EX_VALUE MONEY, AS_QTY MONEY, AS_VALUE MONEY, 
    NET_QTY MONEY, CMP MONEY, NET_VALUE MONEY, MTM MONEY)
    
	DECLARE @tbl_BillDate TABLE(BillDate VARCHAR(8))

    INSERT INTO @tbl_BillDate(BillDate) 
	SELECT DISTINCT fb_billdt FROM FBILLS(NOLOCK)
	WHERE fb_billdt>=@dtFromDate AND fb_billdt <= @dtToDt
	
    DECLARE @strBillDate varchar(8)=''
    DECLARE db_CursorBillDate CURSOR FOR         
    SELECT distinct BillDate
    FROM @tbl_BillDate X  
	OPEN db_CursorBillDate       
    FETCH NEXT FROM db_CursorBillDate INTO @strBillDate
    WHILE @@FETCH_STATUS = 0     
    BEGIN
	  DELETE FROM #tbl_ClientPLDETAIL
	  	
	  SET @strString  = @strStringMin+' SELECT  td_companycode, TD_Exchange, TD_Segment = '''', td_clientcd, sm_sname,sm_underlying, td_seriesid,sm_expirydt, '
      +' sm_multiplier, trxnDate = ''B/F'', OrderNo = '''', TradeNo = '''', TradeTIME = '''', '
	  +' CASE WHEN SUM(td_bqty - td_sqty) >= 0 THEN SUM(td_bqty - td_sqty) ELSE 0 END AS BuyQty, '
	  +' ABS(CASE WHEN SUM(td_bqty - td_sqty) < 0 THEN SUM(td_bqty - td_sqty) ELSE 0 END) AS SaleQty, '
	  +' MarketRate = (CASE WHEN sum(td_bqty- td_sqty) <> 0 THEN SUM(CASE WHEN TD_BSFlag=''B'' THEN td_bqty*td_rate ELSE td_sqty*td_rate*-1 END)/sum(td_bqty- td_sqty) ELSE 0 END), '
      +' BuyValue = (CASE WHEN SUM(td_bqty - td_sqty) >= 0 THEN SUM(CASE WHEN TD_BSFlag=''B'' THEN td_bqty*td_rate*-1 ELSE td_sqty*td_rate END) ELSE 0 END),  '
	  +' SaleValue = (CASE WHEN SUM(td_bqty - td_sqty) < 0 THEN abs(SUM(CASE WHEN TD_BSFlag=''B'' THEN td_bqty*td_rate*-1 ELSE td_sqty*td_rate END)) ELSE 0 END),  '
	  +' NetValue = SUM(CASE WHEN TD_BSFlag=''B'' THEN td_bqty*td_rate*-1 ELSE td_sqty*td_rate END)'
	  
	  SET @strString  = @strString +' FROM Trades (NOLOCK), Series_master(NOLOCK), @tbl_UserList X  '
	  
      SET @strString  = @strString +' WHERE  td_dt < '''+@strBillDate+''' '
      +' AND td_clientcd = X.Client_Code '
	  +' AND td_exchange = sm_exchange '--and td_segment = sm_segment '
      +' and td_seriesid = sm_seriesid '
      +' and sm_expirydt >= '''+@strBillDate +''''
      +' AND (('''+@strConsiderOptionBF+''' = ''N'' AND sm_prodtype NOT IN(''EO'',''CO'',''IO'')) OR '''+@strConsiderOptionBF+''' = ''Y'')'
      +' AND ltrim(rtrim(td_groupid)) <> ''B'' and td_companycode = '''+@strCompanyCode+''' '
	  
	  IF ISNULL(@ExchSeg,'') <> ''
      BEGIN
        SET @strString =   @strString+'  AND td_companycode+td_exchange+td_segment IN(SELECT VALUE FROM  ReturnTable('''+@ExchSeg+''','',''))'
      END	
	
      SET @strString =   @strString+' GROUP BY td_companycode, TD_Exchange, td_clientcd, sm_sname,sm_underlying, td_seriesid, sm_expirydt, sm_multiplier '
      +' HAVING sum(td_bqty - td_sqty) <> 0 '
	  BEGIN TRY
        INSERT INTO #tbl_ClientPLDETAIL(CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, Multiplier, 
		TrxnDate, OrderNo, TradeNo, TradeTIME, BuyQty, SellQty, MarketRate, BuyValue, SellValue, NetValue)
	    EXEC(@strString)
	  END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag  = 'E'
        SET @o_vcErrorMessage = ERROR_MESSAGE()
        RETURN 1
	  END CATCH
      
      UPDATE A SET A.MarketRate = ms_prcloseprice, a.BuyValue = (a.BuyQty*a.Multiplier*ms_prcloseprice),
	  A.SellValue = (a.SellQty*a.Multiplier*ms_prcloseprice), 
	  A.NetValue = (a.SellQty*a.Multiplier*ms_prcloseprice)- (a.BuyQty*a.Multiplier*ms_prcloseprice)
      FROM #tbl_ClientPLDETAIL A, Market_summary(NOLOCK), Series_master SR  
      WHERE ms_seriesid = seriesid 
      and ms_exchange = exchange --and ms_segment = Segment
      and ms_dt = @strBillDate
	  AND A.seriesid = SR.sm_seriesid
	  

	  IF @strReportType = 'Detail'
	  BEGIN
	    SET @strString  = @strStringMin+' SELECT td_companycode, TD_Exchange, TD_Segment = '', td_clientcd, sm_sname, sm_underlying, td_seriesid, '
	    +' sm_expirydt, sm_multiplier, trxnDate = '''+@strBillDate+''', OrderNo = td_orderid, TradeNo = td_tradeid, TradeTIME = td_time, td_bqty As BuyQty,'
	    +' td_Sqty As SellQty, td_rate As MarketRate,  BuyValue = td_Bqty * td_rate*sm_multiplier, '
	    +' SellValue = td_sqty * td_rate*sm_multiplier, NetValue = (td_sqty -td_bqty) * td_rate*sm_multiplier '
        +' FROM Trades (NOLOCK), Series_master(NOLOCK) , @tbl_UserList X  '
        +' WHERE  td_dt = '''+@strBillDate+''' '
        +' AND td_clientcd = X.Client_Code '
        +' AND sm_expirydt >= '''+@strBillDate+''' '
        +' AND td_exchange = sm_exchange '-- and td_segment = sm_segment  '
        +' and td_seriesid = sm_seriesid and td_companycode = '''+@strCompanyCode+''''
        +' AND ltrim(rtrim(td_groupid)) <> ''B'' '
	   
	    IF ISNULL(@ExchSeg,'') <> ''
        BEGIN
          SET @strString =   @strString+'  AND td_companycode+td_exchange+td_segment IN(SELECT VALUE FROM  ReturnTable('''+@ExchSeg+''','',''))'
        END	
	  END
	  ELSE IF @strReportType = 'Summary'
	  BEGIN
	    SET @strString  = @strStringMin+' SELECT td_companycode, TD_Exchange, TD_Segment = '''', td_clientcd, sm_sname, sm_underlying, td_seriesid, '
	    +' sm_expirydt, sm_multiplier, trxnDate = '''+@strBillDate+''', OrderNo = '''', TradeNo = '''', TradeTIME = '''', SUM(td_bqty) As BuyQty,'
	    +' SUM(td_Sqty) As SellQty,  (CASE WHEN (SUM(td_bqty)-SUM(td_Sqty))<>0 '
		+' THEN ROUND(ABS((sum(td_bqty * td_rate*sm_multiplier)- sum(td_Sqty * td_rate*sm_multiplier))/(SUM(td_bqty)-SUM(td_Sqty))),2) ELSE 0 END) '
		+' As MarketRate,  BuyValue = sum(td_bqty * td_rate*sm_multiplier), '
	    +' SellValue = sum(td_sqty * td_rate*sm_multiplier), NetValue = SUM((td_sqty -td_bqty) * td_rate*sm_multiplier) '
        +' FROM Trades (NOLOCK), Series_master(NOLOCK) , @tbl_UserList X  '
        +' WHERE  td_dt = '''+@strBillDate+''' '
        +' AND td_clientcd = X.Client_Code '
        +' AND sm_expirydt >= '''+@strBillDate+''' '
        +' AND td_exchange = sm_exchange '--and td_segment = sm_segment  '
        +' and td_seriesid = sm_seriesid '
        +' AND ltrim(rtrim(td_groupid)) <> ''B'' and td_companycode = '''+@strCompanyCode+''''
	   
	    IF ISNULL(@ExchSeg,'') <> ''
        BEGIN
          SET @strString =   @strString+'  AND td_companycode+td_exchange+td_segment IN(SELECT VALUE FROM  ReturnTable('''+@ExchSeg+''','',''))'
        END	
		
        SET @strString =   @strString+' GROUP BY td_companycode, TD_Exchange, td_clientcd, sm_sname,sm_underlying, td_seriesid,sm_expirydt, '
	    +' sm_multiplier '
	  END
	  
	  BEGIN TRY
	    --SELECT @strString
        INSERT INTO #tbl_ClientPLDETAIL(CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, Multiplier, 
		TrxnDate, OrderNo, TradeNo, TradeTIME, BuyQty, SellQty, MarketRate, BuyValue, SellValue, NetValue)
	    EXEC(@strString)
	  END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag  = 'E'
        SET @o_vcErrorMessage = ERROR_MESSAGE()
        RETURN 1
	  END CATCH
    
	  SET @strString  = @strStringMin+'SELECT ex_companycode, ex_Exchange, ex_segment = '''', ex_clientcd, sm_sname, sm_underlying, ex_seriesid, '
      +'	sm_expirydt, sm_multiplier, trxnDate = CASE WHEN SUM(ex_eqty)>0 THEN ''Exerc.'' ELSE ''Assin'' END, OrderNo = '''', TradeNo = '''', TradeTIME = '''', '
	  +' BuyQty = sum(ex_eqty+ex_aqty), SellQty = 0, MarketRate = ex_diffbrokrate, '
	  +' BuyValue = sum((ex_eqty+ex_aqty)*ex_diffbrokrate*sm_multiplier), SellValue = 0, '
      +' NetValue = sum((ex_eqty+ex_aqty)*ex_diffbrokrate*sm_multiplier) '
      +' From Exercise(NOLOCK), Series_master(NOLOCK), @tbl_UserList X  '
      +' WHERE ex_dt = '''+@strBillDate+''' '
      +' and ex_clientcd = X.Client_Code '
      +' and sm_expirydt >= '''+@strBillDate+''' ' 
      +' and ex_exchange = sm_exchange '
      --+' and ex_segment = sm_segment 
	  +' And ex_seriesid = sm_seriesid and ex_companycode = '''+@strCompanyCode+''''
	  
	  IF ISNULL(@ExchSeg,'') <> ''
      BEGIN
        SET @strString =   @strString+'  AND ex_companycode+ex_exchange+ex_segment IN(SELECT VALUE FROM  ReturnTable('''+@ExchSeg+''','',''))'
      END	
      SET @strString =   @strString+' GROUP BY ex_companycode, ex_Exchange,  ex_clientcd, sm_sname, sm_underlying, ex_seriesid, sm_expirydt'
	  +' , sm_multiplier, ex_diffbrokrate '
	  
    
	  BEGIN TRY
        INSERT INTO #tbl_ClientPLDETAIL(CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, Multiplier, 
		TrxnDate, OrderNo, TradeNo, TradeTIME, BuyQty, SellQty, MarketRate, BuyValue, SellValue, NetValue)
	    EXEC(@strString)
	  END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag  = 'E'
        SET @o_vcErrorMessage = ERROR_MESSAGE()
        RETURN 1
	  END CATCH
	  
	  INSERT INTO #tbl_ClientPLDETAIL(CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, Multiplier, 
	  TrxnDate, OrderNo, TradeNo, TradeTIME, BuyQty, SellQty, MarketRate, BuyValue, SellValue, NetValue)
	  SELECT CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, Multiplier, 
	  TrxnDate = 'C/F', OrderNo = '', TradeNo = '', TradeTIME = '',
	  BuyQty = CASE WHEN SUM(BuyQty-SellQty)<0 THEN SUM(SellQty-BuyQty) ELSE 0 END,
	  SellQty = ABS(CASE WHEN SUM(BuyQty-SellQty)>=0 THEN SUM(BuyQty-SellQty) ELSE 0 END),
	  MarketRate = 0, BuyValue = 0 , SellValue = 0, NetValue = 0
      FROM #tbl_ClientPLDETAIL A, Series_master SR   where A.seriesid = SR.sm_seriesid
	  AND sm_prodtype NOT IN('EO','CO','IO')
      GROUP BY  CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, Multiplier
      HAVING SUM(BuyQty-SellQty) <> 0

	  UPDATE A SET A.BuyValue = ROUND(BuyQty*ms_lastprice*Multiplier,2),
	  A.SellValue = ROUND(SellQty*ms_lastprice*Multiplier,2),
	  A.NetValue = ROUND((SellQty-BuyQty)*ms_lastprice*Multiplier,2),
      MarketRate = ms_lastprice
      FROM #tbl_ClientPLDETAIL A, Market_summary(NOLOCK) X
      WHERE ms_seriesid = seriesid  AND TrxnDate = 'C/F'
      and ms_exchange = exchange --and ms_segment = Segment 
      and ms_dt  = (select  max(ms_dt) from Market_summary(NOLOCK)  
	  WHERE ms_exchange = X.ms_exchange --and ms_segment = X.ms_segment 
	  and ms_dt<=@strBillDate)
	
     
      INSERT INTO #tbl_ClientPLDETAIL(CompanyCode, Exchange, Segment, ClientCode, Scrip, NetValue, TrxnDate)
	  SELECT fc_companycode, fc_Exchange, fc_segment ='', fc_clientcd, fc_desc, round(sum(fc_amount),2)*-1, TrxnDate = 'Chrg'  
      FROM Fspecialcharges(NOLOCK), (SELECT DISTINCT Client_Code = ClientCode  FROM #tbl_ClientPLDETAIL) X
      where fc_clientcd = x.Client_COde and fc_dt = @strBillDate 
      GROUP BY fc_clientcd, fc_desc,fc_companycode, fc_Exchange--, fc_segment
      HAVING ROUND(SUM(fc_amount),2) <> 0
 
      INSERT INTO #tbl_ClientPLDETAIL(CompanyCode, Exchange, Segment, ClientCode, Scrip, NetValue, TrxnDate)
      SELECT fc_companycode, fc_Exchange, fc_segment = '', fc_clientcd,'SERVICE TAX', round(sum(fc_servicetax),2)*-1, TrxnDate = 'Chrg'
      FROM Fspecialcharges(NOLOCK), (SELECT DISTINCT Client_Code =ClientCode FROM #tbl_ClientPLDETAIL) X
      WHERE fc_clientcd = x.Client_COde
      AND fc_dt= @strBillDate 
      GROUP BY fc_clientcd,fc_desc,fc_companycode, fc_Exchange--, fc_segment 
      HAVING ROUND(SUM(fc_servicetax),2) <> 0
	
	  UPDATE A SET BillNo = fb_billno
      FROM #tbl_ClientPLDETAIL A , Fbills WITH (NOLOCK)
      WHERE fb_clientcd = ClientCode
	  --AND fb_Segment = 'F'
	  AND fb_billdt = @strBillDate
	  
	  INSERT INTO #tbl_ClientMainPL(BillNo, BillDate, CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, Multiplier, 
	  TrxnDate, OrderNo, TradeNo, TradeTIME, BuyQty, SellQty, MarketRate, BuyValue, SellValue, NetValue)
	  SELECT BillNo,@strBillDate, CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, Multiplier, 
	  TrxnDate, OrderNo, TradeNo, TradeTIME, BuyQty, SellQty, MarketRate, BuyValue, SellValue, NetValue
      FROM #tbl_ClientPLDETAIL order by CompanyCode, Exchange, Segment, ClientCode, 
	  CAST((CASE WHEN TrxnDate='Chrg' THEN 9 ELSE 1 END) AS INT) , 
	  Scrip, Symbol, seriesid, ExpiryDate,
	  CAST((CASE WHEN TrxnDate='b/f' then 1 when TrxnDate='C/F' then 8 
	  when TrxnDate IN('Exerc.','Assin') then 7 else 2 end) as int)
	  
	  FETCH NEXT FROM db_CursorBillDate INTO @strBillDate
    END  
    CLOSE db_CursorBillDate        
    DEALLOCATE db_CursorBillDate	
	
	UPDATE A SET A.ClientName = CM.cm_name
    FROM #tbl_ClientMainPL A, Client_Master CM
    WHERE A.ClientCode = CM.cm_cd
	IF @strReportType = 'Detail'
	BEGIN
	  SELECT CompanyCode, Exchange, Segment, ClientCode, ClientName, BillNo, BillDate, Scrip = ISNULL(Scrip,''), Symbol = isnull(Symbol,''), 
	  seriesid = ISNULL(seriesid,''), ExpiryDate = ISNULL(ExpiryDate,''), 	  TrxnDate, OrderNo = ISNULL(OrderNo,''), TradeNo  =ISNULL(TradeNo,''), TradeTIME = ISNULL(TradeTIME,''), 
	  BuyQty = ISNULL(BuyQty,0), SellQty = ISNULL(SellQty,0), MarketRate = ISNULL(MarketRate,0), 
	  BuyValue = ISNULL(BuyValue,0), SellValue = ISNULL(SellValue,0), NetValue 
	  FROM #tbl_ClientMainPL 
	  ORDER BY SerialNo
	END
	ELSE IF @strReportType = 'SUMMARY'
	BEGIN
	  IF @strOutputType = 'X'
	  BEGIN
	    SET @XMLDATA1 = (SELECT CompanyCode, Exchange, Segment, LTRIM(RTRIM(CES_Exchange))+'-'+LTRIM(RTRIM(CES_SEGMENT)) AS ExchangeSegment, 
		ClientCode, ClientName, BillNo, BillDate, Scrip = ISNULL(Scrip,''), Symbol = isnull(Symbol,''), 
	    seriesid = ISNULL(seriesid,''), ExpiryDate = ISNULL(ExpiryDate,''), TrxnDate, 
	    BuyQty = ISNULL(BuyQty,0), SellQty = ISNULL(SellQty,0), MarketRate = ISNULL(MarketRate,0), 
	    BuyValue = ISNULL(BuyValue,0), SellValue = ISNULL(SellValue,0), NetValue 
	    FROM #tbl_ClientMainPL , CompanyExchangeSegments(NOLOCK)
		WHERE CES_CD = CompanyCode+Exchange+'F'
	    ORDER BY SerialNo FOR XML PATH('Bill'))
	    SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	   -- RETURN 1
	  END
	  ELSE
	  BEGIN
        SELECT CompanyCode, Exchange, Segment, ClientCode, ClientName, BillNo, BillDate, Scrip = ISNULL(Scrip,''), Symbol = isnull(Symbol,''), 
	    seriesid = ISNULL(seriesid,''), ExpiryDate = ISNULL(ExpiryDate,''), TrxnDate, 
	    BuyQty = ISNULL(BuyQty,0), SellQty = ISNULL(SellQty,0), MarketRate = ISNULL(MarketRate,0), 
	    BuyValue = ISNULL(BuyValue,0), SellValue = ISNULL(SellValue,0), NetValue 
	    FROM #tbl_ClientMainPL 
	    ORDER BY SerialNo
	    --RETURN 1
	  END  
	END
	
    DROP TABLE #tbl_ClientMainPL
	DROP TABLE #tbl_ClientPLDETAIL
	
  
  END TRY
  BEGIN CATCH
    CLOSE db_CursorBillDate        
    DEALLOCATE db_CursorBillDate
	SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = ERROR_MESSAGE()
	RETURN 1
  END CATCH
    
  SET @o_vcErrorFlag  = 'S'
  --SET @o_vcErrorMessage = 'Process Completed'
  RETURN 1
END
GO

CREATE PROCEDURE stpr_Rpt_ProfitLossNewDerv @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT 
WITH ENCRYPTION
AS
BEGIN
 /*
 ///////////////////////////////////////////////////////////////////////////////////////////
 // Create By     : VAIBHAV GARG
 // Created Date  : 05-DEC-2023
 // Description   : 
 // Reviewed By   : 
 // Review Date   : 
 //////////////////////////////////////////////////////////////////////////////////////////
*/
 --- Parameter Declaration
 
  DECLARE @dtFromDate VARCHAR(8), @dtToDt VARCHAR(8), @strUserId VARCHAR(500) = '', @strExchSeg VARCHAR(100),
  @XMLData XML, @strAccountType VARCHAR(500)='', @strTable VARCHAR(50)='', @strString VARCHAR(MAX) = '',
  @blnTplusCommex BIT, @StrCommexConn VARCHAR(MAX) = '', @strCommTable VARCHAR(100)='', @strCommClientMaster VARCHAR(100)='',
  @strCommCompanyExchange VARCHAR(100)='', @strsql1 VARCHAR(500)='', @strsqlstart VARCHAR(MAX)='', @strsqlLast VARCHAR(500)='',
  @strsqlHeader VARCHAR(MAX)='', @strSqlMain VARCHAR(MAX)='', @strSqlExecute VARCHAR(MAX)='', @strOutputType VARCHAR(1), 
  @strProduct VARCHAR(50)='', @strSelectTag VARCHAR(1)='', @strSelectUsers VARCHAR(500)='', @ExchSeg VARCHAR(50)='', 
  @strStringMin VARCHAR(MAX)='', @strSplFilter VARCHAR(MAX)='', @strReportType VARCHAR(50)='', @strRateType VARCHAR(50)='',
  @strConsiderOptionBF VARCHAR(1)='Y', @strStringMinC VARCHAR(MAX)='', @strFIFO VARCHAR(1)='N', @strRequestFrom VARCHAR(1)='W',
  @StrLookup VARCHAR(50)='', @StrSummary VARCHAR(1)='';
  
  DECLARE @SehmentCH_ClgHs VARCHAR(1)='', @ch_EffDt VARCHAR(8)=''
  
  IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
  END 

  SET @XMLData = CAST('<root>'+@vcXML+'</root>' AS XML)
  BEGIN TRY
  
  SELECT @dtFromDate = ISNULL(x.value('(FromDt)[1]', 'VARCHAR(8)'),''),
  @strProduct = ISNULL(x.value('(Product)[1]', 'VARCHAR(50)'),'') ,
  @dtToDt =    ISNULL(x.value('(ToDt)[1]', 'VARCHAR(8)'),''),
  @strUserId =  ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'),''),
  @ExchSeg =  ISNULL(x.value('(ExchSeg)[1]', 'VARCHAR(500)'),''),
  @strSelectTag = ISNULL(x.value('(SelectTag)[1]', 'VARCHAR(1)'),''),
  @strSelectUsers = ISNULL(x.value('(SelectUsers)[1]', 'VARCHAR(500)'),''),
  @strOutputType = ISNULL(x.value('(OutputType)[1]', 'VARCHAR(1)'),''),
  @strSplFilter = ISNULL(x.value('(SplFilter)[1]', 'VARCHAR(MAX)'),''),
  @strReportType = ISNULL(x.value('(RepType)[1]', 'VARCHAR(100)'),''),
  @strRateType = ISNULL(x.value('(RepSubType)[1]', 'VARCHAR(100)'),''),
  @strConsiderOptionBF = ISNULL(x.value('(OptionBF)[1]', 'VARCHAR(100)'),''),
  @strRequestFrom = ISNULL(x.value('(RequestFrom)[1]', 'VARCHAR(1)'),''),
  @StrLookup = ISNULL(x.value('(ScripCode)[1]', 'VARCHAR(50)'),''),
  @StrSummary = ISNULL(x.value('(RequestSummary)[1]', 'VARCHAR(1)'),'')
  FROM @XMLData.nodes('/root') AS XTbl(x) 
  
  IF LEN(ISNULL(@StrLookup,'')) <= 2  
  BEGIN
    SET @StrLookup = ''
  END
  
  if ISNULL(@StrSummary,'') =''
  Begin
    SET @StrSummary = 'N'
  END
  
  IF ISNULL(@strRequestFrom,'') =''
  BEGIN
    SET @strRequestFrom = 'W'
  END
  
  SET @strFIFO = 'N'
  
  if ISNULL(@strConsiderOptionBF,'')=''
  BEGIN
    SET @strConsiderOptionBF = 'Y'
  END
  
  IF @strRateType = ''
  BEGIN
    SET @strRateType = 'Market Rate'
  END
  
  DECLARE @tbl_Exchange TABLE(ExchangeCode VARCHAR(20))
 -- INSERT INTO @tbl_Exchange
  -- SELECT CES_CD FROM CompanyExchangeSegments(NOLOCK) 

  IF ISNULL(@ExchSeg,'') <> ''
  BEGIN
    INSERT INTO @tbl_Exchange 
	SELECT CES_CD from CompanyExchangeSegments(NOLOCK) 
    WHERE CES_CD IN(SELECT VALUE FROM ReturnTable(@ExchSeg,','))  
  END
  
  SELECT @ExchSeg = @ExchSeg+','+ExchangeCode from @tbl_Exchange   
  
  CREATE TABLE #tbl_ClientPL (CompanyCode VARCHAR(10), Exchange VARCHAR(10), Segment VARCHAR(10),  ClientCode VARCHAR(50), ClientName VARCHAR(100), 
  Symbol VARCHAR(50), seriesid INT, ExpiryDate VARCHAR(8), Scrip VARCHAR(100), Multiplier INT, TrxnDate VARCHAR(8), 
  BF_QTY numeric, BF_CloseRate MONEY, BF_VALUE MONEY, BOT_QTY numeric, BOT_VALUE MONEY, 
  SOLD_QTY numeric,SOLD_VALUE MONEY, EX_QTY numeric, EX_VALUE MONEY, AS_QTY numeric, AS_VALUE MONEY, MTM MONEY,
  NET_QTY numeric, CMP MONEY, NET_VALUE MONEY, AccountType VARCHAR(1))
  
  CREATE INDEX indx_ClientPL ON #tbl_ClientPL (seriesid)
    

  DECLARE @tbl_BSClient TABLE(CompanyCode VARCHAR(10), Exchange VARCHAR(10), Segment VARCHAR(10), ClientCode VARCHAR(50), ClientName VARCHAR(100), 
  Symbol VARCHAR(50), seriesid INT, ExpiryDate VARCHAR(8), Scrip VARCHAR(100), Multiplier INT, TrxnDate VARCHAR(8), BOT_QTY numeric, BOT_VALUE MONEY, 
  SOLD_QTY numeric, SOLD_VALUE MONEY)

  DECLARE @tbl_CLClient TABLE(CompanyCode VARCHAR(10), Exchange VARCHAR(10), Segment VARCHAR(10),  ClientCode VARCHAR(50), ClientName VARCHAR(100), 
  Symbol VARCHAR(50), seriesid INT, ExpiryDate VARCHAR(8), Scrip VARCHAR(100), Multiplier INT, TrxnDate VARCHAR(8), EX_QTY numeric, EX_VALUE MONEY, AS_QTY numeric, AS_VALUE MONEY)
  

  SET @strStringMin  = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50)) '
  +' INSERT INTO @tbl_UserList(Client_Code) '
  +' SELECT '''+@strUserId+''' '
  
 
 
  IF @strProduct = 'DERV'
  BEGIN 
    
	IF @strSplFilter = ''
	BEGIN
	  SET @strStringMin  = 'DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50)) '
      +' INSERT INTO @tbl_UserList(Client_Code) '
      +' SELECT '''+@strUserId+''' '
	END  
	ELSE
    IF @strSplFilter <> ''
    BEGIN
      SET @strStringMinC  = ' DECLARE @tbl_UserList TABLE (Client_Code VARCHAR(10)) '
	                     +' INSERT INTO @tbl_UserList(Client_Code) SELECT '''+@strUserId+''''
      SET @strStringMin = @strStringMinC					 
    END	

    IF ISNULL(@strFIFO,'N') = 'N'
	BEGIN
	  SET @strString  = @strStringMin+' SELECT  td_companycode, TD_Exchange, TD_Segment =''F'', td_clientcd, SM_DESC, sm_underlying, td_seriesid,sm_expirydt, '
      +' sm_multiplier, td_dt = '''+@dtFromDate+''', SUM(td_bqty - td_sqty) AS BFQty, '
	  +' BF_CloseRate = (CASE WHEN sum(td_bqty- td_sqty) <> 0 THEN SUM(CASE WHEN TD_BSFlag=''B'' THEN td_bqty*CAST(td_rate AS numeric(19,6)) '
	  +' ELSE td_sqty*CAST(td_rate AS numeric(19,6))*-1 END)'
	  +' /sum(td_bqty- td_sqty) ELSE 0 END), '
      +' BF_VALUE = SUM(CASE WHEN TD_BSFlag=''B'' THEN td_bqty*CAST(td_rate AS numeric(19,6))*-1 ELSE td_sqty*CAST(td_rate AS numeric(19,6)) END) '
	  +' FROM Trades (NOLOCK), Series_master(NOLOCK), @tbl_UserList X  '
      +' WHERE  td_dt < '''+@dtFromDate+''' '
      +' AND td_clientcd = X.Client_Code '
      +' AND sm_expirydt >= '''+@dtFromDate+''' '
      +' AND td_exchange = sm_exchange ' -- and td_segment = sm_segment '
      +' and td_seriesid = sm_seriesid '
      +' and sm_expirydt >= '''+@dtFromDate +''''
      +' AND (('''+@strConsiderOptionBF+''' = ''N'' AND sm_prodtype NOT IN(''EO'',''CO'',''IO'')) OR '''+@strConsiderOptionBF+''' = ''Y'')'
      +' AND ltrim(rtrim(td_groupid)) <> ''B'' '
	
	  IF ISNULL(@ExchSeg,'') <> ''
      BEGIN
        SET @strString =   @strString+'  AND td_companycode+td_exchange IN(SELECT VALUE FROM  ReturnTable('''+@ExchSeg+''','',''))'
      END	
	  
	  IF ISNULL(@StrLookup,'') <> ''
      BEGIN
        SET @strString =   @strString+'  AND TD_Exchange+cast(td_seriesid as varchar)  = '''+@StrLookup+''''
      END	
	  
	
      SET @strString =   @strString+' GROUP BY td_companycode, TD_Exchange,  td_clientcd, SM_DESC,sm_underlying, td_seriesid, sm_expirydt, sm_multiplier '
      +' HAVING sum(td_bqty - td_sqty) <> 0 '
	  BEGIN TRY
        INSERT INTO #tbl_ClientPL(CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, Multiplier, TrxnDate,  BF_QTY, BF_CloseRate, BF_VALUE)
	    EXEC(@strString)
	  END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag  = 'E'
        SET @o_vcErrorMessage = ERROR_MESSAGE()
        RETURN 1
	  END CATCH
	  
	  UPDATE A SET A.BF_CloseRate = ms_prcloseprice, a.BF_VALUE = (a.BF_QTY*a.Multiplier*ms_prcloseprice)*-1
      FROM #tbl_ClientPL A, Market_summary(NOLOCK) X, Series_master SR  
      WHERE ms_seriesid = seriesid 
      and ms_exchange = exchange --and ms_segment = Segment
      and ms_dt = (select  max(ms_dt) from Market_summary(NOLOCK)  
	  WHERE ms_exchange = X.ms_exchange --and ms_segment = X.ms_segment 
	  and ms_dt <= @dtFromDate)
	  AND A.exchange = SR.sm_exchange
	 -- AND A.Segment  = sm_Segment
	  AND A.seriesid = SR.sm_seriesid
	  AND ((sm_prodtype NOT IN('EO','CO','IO') AND @strRateType in('Underlying Close Price')) 
	  OR @strRateType not in('Underlying Close Price'))
	
	
      UPDATE A SET A.BF_CloseRate = ms_prcloseprice, a.BF_VALUE = 0
      FROM #tbl_ClientPL A, Market_summary(NOLOCK) X, Series_master SR  
      WHERE ms_seriesid = seriesid 
      and ms_exchange = exchange --and ms_segment = Segment
      and ms_dt = (select  max(ms_dt) from Market_summary(NOLOCK)  
	  WHERE ms_exchange = X.ms_exchange --and ms_segment = X.ms_segment 
	  and ms_dt <= @dtFromDate)
	  AND A.exchange = SR.sm_exchange
	  --AND A.Segment  = sm_Segment
	  AND A.seriesid = SR.sm_seriesid
	  AND sm_prodtype IN('EO','CO','IO') 
	  AND @strRateType = 'Do not Valuate' 
	
	  IF @strRateType = 'Underlying Close Price'
	  BEGIN
	    declare @BFseriesid INT, @spotCloseRate MONEY=0, @strExchange VARCHAR(1)='',
		@strSegment VARCHAR(1)=''
	    DECLARE db_CursorRateBF CURSOR FOR         
        SELECT distinct seriesid, Exchange,  Segment
        FROM #tbl_ClientPL A 
	    where BF_QTY <> 0 and EXISTS(SELECT 1 FROM Series_master(NOLOCK)
	    WHERE --sm_Segment =  A.Segment AND 
		sm_exchange = A.Exchange AND sm_seriesid = A.seriesid   
		AND sm_productcd = 'OPTSTK')
        OPEN db_CursorRateBF       
        FETCH NEXT FROM db_CursorRateBF INTO @BFseriesid, @strExchange, @strSegment
        WHILE @@FETCH_STATUS = 0     
        BEGIN
	      SELECT TOP 1 @spotCloseRate = mk_closerate FROM Market_Rates(NOLOCK) X WHERE mk_scripcd IN(
          SELECT SS_cD FROM Securities WHERE ss_bsymbol IN(
          SELECT sm_symbol FROM Series_master(NOLOCK) WHERE sm_exchange = @strExchange 
		  --AND sm_Segment = @strSegment 
		  AND sm_seriesid = @BFseriesid))
          AND mk_dt   IN(SELECT MAX(mk_dt) FROM Market_Rates(NOLOCK) WHERE mk_dt < @dtFromDate)
          ORDER BY CASE WHEN mk_exchange='N' THEN 1 ELSE 2 END
	  
	      UPDATE A SET A.BF_CloseRate = @spotCloseRate, a.BF_VALUE =(a.BF_QTY*@spotCloseRate)*-1
	      FROM #tbl_ClientPL A
	      WHERE A.seriesid = @BFseriesid
		  AND A.Exchange = @strExchange
		  AND A.Segment = @strSegment
		  
	      FETCH NEXT FROM db_CursorRateBF INTO @BFseriesid, @strExchange, @strSegment
        END        
        CLOSE db_CursorRateBF        
        DEALLOCATE db_CursorRateBF	
	  END 
	  
	  SET @strString  = @strStringMin+' SELECT td_companycode, TD_Exchange, TD_Segment=''F'', td_clientcd, SM_DESC, sm_underlying, td_seriesid, '
	  +' sm_expirydt, sm_multiplier, TrxnDate = CASE WHEN ISNULL('''+@strReportType+''','''')  = ''Series Wise Detail'' THEN  td_dt ELSE '''+@dtFromDate+''' END,'
      +' SUM(td_bqty) BQty, '
      +' sum(td_bqty * cast(td_rate as money)*sm_multiplier) BuyValue, '
      +' SUM(td_Sqty) SQty, '
      +' sum(td_sqty * cast(td_rate as money)*sm_multiplier) SaleValue '
      +' FROM Trades (NOLOCK), Series_master(NOLOCK) , @tbl_UserList X  '
      +' WHERE  td_dt >= '''+@dtFromDate+''' '
      +' and td_dt <= '''+@dtToDt+''' '
      +' AND td_clientcd = X.Client_Code '
      +' AND sm_expirydt >= '''+@dtFromDate+''' '
      +' AND td_exchange = sm_exchange ' --and td_segment = sm_segment  '
      +' and td_seriesid = sm_seriesid '
	  
	  /*
      +' AND ltrim(rtrim(td_groupid)) <> ''B'' '
 	  */
      IF ISNULL(@ExchSeg,'') <> ''
      BEGIN
        SET @strString =   @strString+'  AND td_companycode+td_exchange  IN(SELECT VALUE FROM  ReturnTable('''+@ExchSeg+''','',''))'
      END	
	  
	  IF ISNULL(@StrLookup,'') <> ''
      BEGIN
        SET @strString =   @strString+'  AND TD_Exchange+cast(td_seriesid as varchar)  = '''+@StrLookup+''''
      END	
	
      SET @strString =   @strString+' GROUP BY td_companycode, TD_Exchange,  td_clientcd, SM_DESC,sm_underlying, td_seriesid,sm_expirydt, sm_multiplier'
	  +',  CASE WHEN ISNULL('''+@strReportType+''','''') = ''Series Wise Detail'' THEN  td_dt ELSE  '''+@dtFromDate+''' END '
      
	  BEGIN TRY
        INSERT INTO @tbl_BSClient (CompanyCode, Exchange, Segment,  ClientCode, Scrip, Symbol, seriesid, 
        ExpiryDate, Multiplier, TrxnDate, BOT_QTY, BOT_VALUE, SOLD_QTY,SOLD_VALUE)
	    EXEC(@strString)
	  END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag  = 'E'
        SET @o_vcErrorMessage = ERROR_MESSAGE()
        RETURN 1
	  END CATCH
      

      SET @strString  = @strStringMin+'SELECT ex_companycode, ex_Exchange, sm_Segment =''F''  , ex_clientcd, SM_DESC, sm_underlying, ex_seriesid, '
       +'	sm_expirydt, sm_multiplier, CASE WHEN ISNULL('''+@strReportType+''','''') = ''Series Wise Detail'' THEN  ex_dt ELSE '''+@dtFromDate+''' END'
      +'	 ,  ex_eqty = sum(ex_eqty) , '
      +' EX_VALUE = sum((ex_eqty)*ex_diffbrokrate*sm_multiplier*-1), ex_aqty = sum(ex_aqty), '
      +' AS_VALUE = sum((ex_aqty)*ex_diffbrokrate*sm_multiplier*-1) '
      +' From Exercise(NOLOCK), Series_master(NOLOCK), @tbl_UserList X  '
      +' WHERE ex_dt between '''+@dtFromDate+''' and '''+@dtToDt+''' '
      +' and ex_clientcd = X.Client_Code '
      +' and sm_expirydt >= '''+@dtFromDate+''' ' 
      +' and ex_exchange = sm_exchange '
      +' And ex_seriesid = sm_seriesid '
	
	
      IF ISNULL(@ExchSeg,'') <> ''
      BEGIN
        SET @strString =   @strString+'  AND ex_companycode+ex_exchange  IN(SELECT VALUE FROM  ReturnTable('''+@ExchSeg+''','',''))'
		
      END	
	    
	  IF ISNULL(@StrLookup,'') <> ''
      BEGIN
        SET @strString =   @strString+'  AND ex_Exchange+cast(ex_seriesid as varchar)  = '''+@StrLookup+''''
      END	
	  
      SET @strString =   @strString+' GROUP BY ex_companycode, ex_Exchange,  ex_clientcd, SM_DESC, sm_underlying, ex_seriesid, sm_expirydt, sm_multiplier '
	  +', CASE WHEN ISNULL('''+@strReportType+''','''') = ''Series Wise Detail'' THEN ex_dt ELSE '''+@dtFromDate+''' END '
    
	  BEGIN TRY
        INSERT INTO @tbl_CLClient(CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, Multiplier, TrxnDate, EX_QTY, EX_VALUE,
        AS_QTY, AS_VALUE)
	    EXEC(@strString)
	  END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag  = 'E'
        SET @o_vcErrorMessage = ERROR_MESSAGE()
        RETURN 1
	  END CATCH
	
      UPDATE A SET A.BOT_QTY = B.BOT_QTY, A.BOT_VALUE = B.BOT_VALUE, A.SOLD_QTY = B.SOLD_QTY,
      A.SOLD_VALUE = B.SOLD_VALUE
      FROM #tbl_ClientPL A, @tbl_BSClient B
      WHERE A.ClientCode = B.ClientCode
      AND A.Scrip = B.Scrip
      AND A.Exchange = B.Exchange
      AND A.seriesid = B.seriesid
	  AND A.Segment = B.Segment
	  AND CASE WHEN A.TrxnDate='' THEN @dtFromDate ELSE A.TrxnDate END = 
	  CASE WHEN B.TrxnDate='' THEN @dtFromDate ELSE B.TrxnDate END

      INSERT INTO #tbl_ClientPL(CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, Multiplier, TrxnDate, BF_QTY, BF_CloseRate, BF_VALUE,
      BOT_QTY, BOT_VALUE, SOLD_QTY, SOLD_VALUE)
      SELECT CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, Multiplier, TrxnDate, BF_QTY = 0, BF_CloseRate= 0, BF_VALUE = 0,
      BOT_QTY, BOT_VALUE, SOLD_QTY, SOLD_VALUE
      FROM @tbl_BSClient B
      WHERE NOT EXISTS(SELECT 1 FROM #tbl_ClientPL A WHERE A.ClientCode = B.ClientCode
      AND A.Scrip = B.Scrip
      AND A.Exchange = B.Exchange
      AND A.seriesid = B.seriesid
	  AND A.Segment = B.Segment
	  and CASE WHEN A.TrxnDate='' THEN @dtFromDate ELSE A.TrxnDate END 
	  = CASE WHEN B.TrxnDate='' THEN @dtFromDate ELSE B.TrxnDate END)
	  
	  
      UPDATE A SET A.EX_QTY = B.EX_QTY, A.EX_VALUE = B.EX_VALUE, A.AS_QTY = B.AS_QTY,
      A.AS_VALUE = B.AS_VALUE
      FROM #tbl_ClientPL A, @tbl_CLClient B
      WHERE A.ClientCode = B.ClientCode
      AND A.Scrip = B.Scrip
      AND A.Exchange = B.Exchange
      AND A.seriesid = B.seriesid
	  AND A.Segment = B.Segment
	  and CASE WHEN A.TrxnDate='' THEN @dtFromDate ELSE A.TrxnDate END = 
	  CASE WHEN B.TrxnDate='' THEN @dtFromDate ELSE B.TrxnDate END
	  
	  INSERT INTO #tbl_ClientPL(CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, Multiplier, TrxnDate, BF_QTY, BF_CloseRate, BF_VALUE,
      BOT_QTY, BOT_VALUE, SOLD_QTY, SOLD_VALUE, EX_QTY, EX_VALUE, AS_QTY, AS_VALUE)
      SELECT CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, Multiplier, 
	  TrxnDate, BF_QTY = 0, BF_CloseRate= 0, BF_VALUE = 0,
      BOT_QTY = 0, BOT_VALUE = 0, SOLD_QTY = 0, SOLD_VALUE = 0, EX_QTY, EX_VALUE, AS_QTY, AS_VALUE
      FROM @tbl_CLClient B
      WHERE NOT EXISTS(SELECT 1 FROM #tbl_ClientPL A WHERE A.ClientCode = B.ClientCode
      AND A.Scrip = B.Scrip
      AND A.Exchange = B.Exchange
      AND A.seriesid = B.seriesid
	  AND A.Segment = B.Segment
	  and CASE WHEN A.TrxnDate='' THEN @dtFromDate ELSE A.TrxnDate END 
	  = CASE WHEN B.TrxnDate='' THEN @dtFromDate ELSE B.TrxnDate END)
  
    END
	
	ELSE IF ISNULL(@strFIFO,'N') = 'Y'
	BEGIN
	  IF OBJECT_ID('tempdb..#tbl_ClientPLFIFO') IS NOT NULL
      DROP TABLE #tbl_ClientPLFIFO

      CREATE TABLE #tbl_ClientPLFIFO (CompanyCode VARCHAR(10), Exchange VARCHAR(10), Segment VARCHAR(10),  ClientCode VARCHAR(50), 
      ClientName VARCHAR(100), 
      Symbol VARCHAR(50), seriesid INT, ExpiryDate VARCHAR(8), Scrip VARCHAR(100), Multiplier INT, TrxnDate VARCHAR(8), 
      td_SRNO INT,  td_bsflag VARCHAR(1), Qty_NS MONEY, VALUES_NS MONEY, td_Rate MONEY)
    
	  CREATE INDEX indx_ClientPLFIFO ON #tbl_ClientPLFIFO (seriesid)

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
      IF OBJECT_ID('tempdb..#tbl_DelvTrxnN') IS NOT NULL
        DROP TABLE #tbl_DelvTrxnN
	  IF OBJECT_ID('tempdb..#tbl_DelvTrxnN1') IS NOT NULL
        DROP TABLE #tbl_DelvTrxnN1
	  IF OBJECT_ID('tempdb..#tbl_DelvTrxnN2') IS NOT NULL
        DROP TABLE #tbl_DelvTrxnN2
      IF OBJECT_ID('tempdb..#tbl_Rep') IS NOT NULL
        DROP TABLE #tbl_Rep
	
	  DECLARE @tbl_UserList TABLE(Client_Code VARCHAR(50))
	  IF @strSplFilter = ''
	  BEGIN
	    INSERT INTO @tbl_UserList(Client_Code) 
        SELECT @strUserId
	  END  
	  ELSE
      IF @strSplFilter <> ''
      BEGIN
        SET @strStringMinC  = 'SELECT distinct CM_CD FROM Client_master(NOLOCK) WHERE 1 = 1  AND '+@strSplFilter
		INSERT INTO @tbl_UserList(Client_Code) 
		EXEC(@strStringMinC)
      END	
  
      CREATE TABLE #tbl_DelvTrxn (
      SerialNo int identity(1,1), td_companycode VARCHAR(1), TD_Exchange VARCHAR(1), 
	  TD_Segment VARCHAR(1), td_SRNO INT, td_dt VARCHAR(8), td_clientcd VARCHAR(20), 
	  td_scripcd VARCHAR(20), td_expirydt VARCHAR(8), SM_DESC VARCHAR(500), sm_underlying VARCHAR(50),
      sm_multiplier INT, td_bsflag VARCHAR(1), Qty_NS MONEY, VALUES_NS MONEY, td_Rate MONEY, 
	  XTAG11 INT, LONG_TAG VARCHAR(1), SQR_TAG VARCHAR(1), Tmp_RefNo numeric, td_Filler1 VARCHAR(8))

      INSERT INTO #tbl_DelvTrxn(td_SRNO, td_companycode, TD_Exchange, TD_Segment, 
	  td_dt, td_clientcd, td_scripcd, td_expirydt, SM_DESC, sm_underlying,
	  sm_multiplier, td_bsflag, Qty_NS, VALUES_NS, td_Rate)
      SELECT  td_SRNO, td_companycode, TD_Exchange, 'C', td_dt, td_clientcd, td_seriesid,  sm_expirydt, SM_DESC, sm_underlying,
	  sm_multiplier, td_bsflag, Qty_NS = ISNULL(td_bqty,0)+ISNULL(td_sqty,0), 
      VALUES_NS = (ISNULL(td_bqty,0)+ISNULL(td_sqty,0))*td_rate*sm_multiplier, td_rate
      FROM Trades (NOLOCK), Series_master(NOLOCK), @tbl_UserList X
      WHERE  td_clientcd = X.Client_Code
      AND td_dt < @dtFromDate
      AND sm_expirydt >= @dtFromDate
      AND td_exchange = sm_exchange --and td_segment = sm_segment 
      AND td_seriesid = sm_seriesid 
      AND ltrim(rtrim(td_groupid)) <> 'B'
      ORDER BY td_seriesid, td_dt

      CREATE TABLE #TrxSummaryDLV1 (clientcd VARCHAR(8) NOT NULL, scripcd VARCHAR(6) NOT NULL, td_expirydt VARCHAR(8), 
	  Buys MONEY, Sells MONEY)
  
      INSERT INTO #TrxSummaryDLV1
      SELECT td_clientcd, td_scripcd, td_expirydt, Buys, Sells
      FROM (SELECT td_clientcd, td_scripcd, td_expirydt, sum(CASE WHEN td_bsflag='B' THEN Qty_ns ELSE 0 END) AS 'Buys', 
      SUM(CASE WHEN td_bsflag='S' THEN Qty_ns ELSE 0 END) 'Sells', 
      SUM(CASE WHEN td_bsflag='B' THEN Qty_ns ELSE 0 END) - sum(CASE WHEN td_bsflag='S' THEN Qty_ns ELSE 0 END) AS 'Delivery', 
      COUNT(*) Totalrec
      FROM #tbl_DelvTrxn
      GROUP BY td_clientcd, td_scripcd, td_expirydt
      HAVING sum(CASE WHEN td_bsflag='B' THEN Qty_ns ELSE 0 END) - sum(CASE WHEN td_bsflag='S' THEN Qty_ns ELSE 0 END) <> 0) a

      CREATE TABLE #TrxDLV1 (SrNo NUMERIC, Qty NUMERIC, FinalQty NUMERIC)
      CREATE INDEX indx_SrNo ON #TrxDLV1 (SrNo) 

      INSERT INTO #TrxDLV1
      SELECT SerialNo, Qty, CASE WHEN NetQty >= Running THEN Qty ELSE NetQty - isNull(PrevRunning, 0) END FinalQty
      FROM (
      SELECT SerialNo, td_clientcd, td_scripcd, td_marketrate,  NetQty, Qty, Running, LAG(Running) OVER (
      PARTITION BY td_clientcd, td_scripcd ORDER BY td_dt DESC,  SerialNo DESC) PrevRunning
      FROM (SELECT SerialNo, td_dt, td_clientcd, td_scripcd, Qty_ns Qty, td_marketrate = td_Rate,  abs(Buys - Sells) NetQty, Sum(Qty_ns) OVER (
			PARTITION BY td_clientcd, td_scripcd ORDER BY td_dt DESC,  SerialNo DESC) Running
      FROM #tbl_DelvTrxn,  #TrxSummaryDLV1 
      WHERE td_clientcd = clientcd AND td_scripcd = scripcd AND td_bsflag = CASE WHEN Buys > Sells THEN 'B' ELSE 'S' END) a) b
      WHERE CASE WHEN NetQty >= Running THEN Qty ELSE NetQty - isNull(PrevRunning, 0) END > 0


      INSERT INTO #tbl_DelvTrxn(td_SRNO, td_companycode, TD_Exchange, TD_Segment, td_dt, td_clientcd, td_scripcd, td_expirydt,  
	  SM_DESC, sm_underlying, 
	  sm_multiplier, td_bsflag, Qty_NS, VALUES_NS, td_Rate)
      SELECT td_SRNO, td_companycode, TD_Exchange, TD_Segment, td_dt, td_clientcd, 
	  td_scripcd, td_expirydt, SM_DESC, sm_underlying, sm_multiplier, td_bsflag,
      Qty - FinalQty, VALUES_NS = (Qty - FinalQty)*td_Rate, td_Rate
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

	  INSERT INTO #tbl_ClientPLFIFO(CompanyCode, Exchange, Segment, ClientCode, Symbol, 
	  seriesid, ExpiryDate, Scrip, Multiplier, TrxnDate,  td_SRNO,  td_bsflag, Qty_NS, VALUES_NS, td_Rate)
      SELECT td_companycode, TD_Exchange, TD_Segment, td_clientcd, sm_underlying ,td_scripcd, td_expirydt, SM_DESC, sm_multiplier, td_dt,
	  td_SRNO,  td_bsflag, Qty_NS, VALUES_NS, td_Rate
	  FROM #tbl_DelvTrxn WHERE ISNULL(SQR_TAG,'N') = 'Y'
      ORDER BY td_scripcd, td_expirydt
  
      CREATE TABLE #tbl_DelvTrxnN (CompanyCode VARCHAR(10), Exchange VARCHAR(10), Segment VARCHAR(10),
	  td_SRNO INT, td_dt VARCHAR(8), td_clientcd VARCHAR(20), td_seriesid VARCHAR(20), 
	  td_expirydt VARCHAR(8), td_bsflag VARCHAR(1), Qty_NS MONEY, VALUES_NS MONEY, td_Rate MONEY, FIFONo INT, 
	  XTAG11 INT, LONG_TAG VARCHAR(1), SQR_TAG VARCHAR(1))

	  CREATE TABLE #tbl_DelvTrxnN1 (CompanyCode VARCHAR(10), Exchange VARCHAR(10), Segment VARCHAR(10),
	  td_SRNO INT, td_dt VARCHAR(8), td_clientcd VARCHAR(20), td_seriesid VARCHAR(20), 
	  td_expirydt VARCHAR(8), td_bsflag VARCHAR(1), Qty_NS MONEY, VALUES_NS MONEY, td_Rate MONEY, FIFONo INT, 
	  XTAG11 INT, LONG_TAG VARCHAR(1), SQR_TAG VARCHAR(1))

	  CREATE TABLE #tbl_DelvTrxnN2 (CompanyCode VARCHAR(10), Exchange VARCHAR(10), Segment VARCHAR(10),
	  td_SRNO INT, td_dt VARCHAR(8), td_clientcd VARCHAR(20), td_seriesid VARCHAR(20), 
	  td_expirydt VARCHAR(8), td_bsflag VARCHAR(1), Qty_NS MONEY, VALUES_NS MONEY, td_Rate MONEY, FIFONo INT, 
	  XTAG11 INT, LONG_TAG VARCHAR(1), SQR_TAG VARCHAR(1))

	 
	  CREATE INDEX indx_DelvTrxnN1 ON #tbl_DelvTrxnN1 (td_clientcd, td_seriesid, td_SRNO, FIFONO)
      CREATE INDEX indx_DelvTrxnN ON #tbl_DelvTrxnN (td_clientcd, td_seriesid, td_SRNO, FIFONO)
	  CREATE INDEX indx_DelvTrxnN2 ON #tbl_DelvTrxnN2 (td_clientcd, td_seriesid, td_SRNO, FIFONO)
      CREATE INDEX indx_DelvTrxn1N ON #tbl_DelvTrxnN (td_clientcd, FIFONO)

      INSERT INTO #tbl_DelvTrxnN (CompanyCode, Exchange,  Segment, td_SRNO, td_dt,  
	  td_clientcd, td_seriesid, td_bsflag, Qty_NS, VALUES_NS, td_Rate, FIFONO)

      SELECT X.*, FIFONO = ROW_NUMBER() OVER (
		PARTITION BY td_clientcd ORDER BY td_seriesid, TD_DT, td_SRNO)
      FROM (
	  
	  SELECT CompanyCode, Exchange, Segment, 
	  td_SRNO, td_dt = TrxnDate, td_clientcd =  ClientCode, td_seriesid = seriesid,  td_bsflag, 
      Qty_NS  , VALUES_NS, td_Rate
	  FROM #tbl_ClientPLFIFO
	  UNION ALL
	  SELECT td_companycode as CompanyCode, TD_Exchange as Exchange, 'C' as Segment, 
	  td_SRNO, td_dt, td_clientcd, td_seriesid, td_bsflag, 
      Qty_NS = ABS(td_bqty - td_Sqty),
	  VALUES_NS = td_Rate * ABS(td_bqty - td_Sqty), td_Rate
	  FROM Trades(NOLOCK), Series_master(NOLOCK), @tbl_UserList X
      WHERE  td_clientcd = x.Client_Code
      AND td_dt >= @dtFromDate
      AND td_dt <= @dtToDt
      AND td_exchange = sm_exchange --and td_segment = sm_segment 
      AND td_seriesid = sm_seriesid 
      AND ltrim(rtrim(td_groupid)) <> 'B' ) X --WHERE X.td_seriesid='219504'


	  DECLARE @SQR_QTY MONEY = 0, @SQR_QTY1 MONEY = 0, @td_clientcdC1 VARCHAR(10) = '', @td_scripcdC1 VARCHAR(10) = '', 
	  @td_BuyQtyC1 MONEY = 0, @td_SaleQtyC1 MONEY = 0, @COUNTER INT = 0, @td_SRNOB INT, 
	  @td_dtB VARCHAR(8),  @td_clientcdB VARCHAR(50), @td_scripcdB VARCHAR(50), 
	  @td_bsflagB VARCHAR(1), @QTY_NSB MONEY, @td_RateB MONEY, @VALUES_NSB MONEY, 
	  @td_SRNOS INT, @td_dtS VARCHAR(8), @td_clientcdS VARCHAR(50), @td_scripcdS 
	  VARCHAR(50), @td_bsflagS VARCHAR(1), @QTY_NSS MONEY, @td_RateS MONEY, @buyQty MONEY = 0, @SaleQty MONEY = 0, 
	  @BQTY MONEY = 0, @SQTY MONEY = 0, @VALUES_NSS MONEY, @FIFONOB INT, @FIFONOS INT, @SQ MONEY = 0, @TAG VARCHAR(1) = 'C', 
	  @strFlag VARCHAR(1)='N', @strsaleflag VARCHAR(1)='', @TD_STTB MONEY, 
	  @TD_STTS MONEY, @BDATE varchar(8), @SDATE VARCHAR(8), @CompanyCodeb VARCHAR(10), @Exchangeb VARCHAR(10), 
	  @Segmentb VARCHAR(10), @CompanyCodeS VARCHAR(10), @ExchangeS VARCHAR(10), 
	  @SegmentS VARCHAR(10)

	
      DECLARE CursorC1Main CURSOR
      FOR
      SELECT td_clientcd, td_seriesid, SUM(CASE WHEN td_bsflag = 'B' THEN Qty_NS ELSE 0 END) BuyQty, 
      SUM(CASE WHEN td_bsflag = 'S' THEN Qty_NS ELSE 0 END) SaleQty
      FROM #tbl_DelvTrxnN x
      GROUP BY td_clientcd, td_seriesid
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
	
	
	    DECLARE CursorBMain CURSOR
	    FOR SELECT CompanyCode, Exchange,  Segment, td_SRNO, td_dt, td_clientcd, td_seriesid, td_bsflag, 
		Qty_NS, td_Rate, VALUES_NS, FIFONO
	    FROM #tbl_DelvTrxnN
	    WHERE td_clientcd = @td_clientcdC1 AND td_seriesid = @td_scripcdC1 AND td_bsflag = 'B' AND Qty_NS <> 0
	    ORDER BY td_clientcd, td_seriesid, FIFONO, td_SRNO
	
	    DECLARE CursorSMain CURSOR
	    FOR
	    SELECT CompanyCode, Exchange,  Segment, td_SRNO, td_dt, td_clientcd, td_seriesid, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO
	    FROM #tbl_DelvTrxnN
	    WHERE td_clientcd = @td_clientcdC1 AND td_seriesid = @td_scripcdC1 AND td_bsflag = 'S' AND Qty_NS <> 0
	    ORDER BY td_clientcd, td_seriesid, FIFONO, td_SRNO
	    OPEN CursorBMain
	    OPEN CursorSMain
	    WHILE @@FETCH_STATUS = 0
	    BEGIN
	      SET @COUNTER = @COUNTER + 1
          IF @SQR_QTY <> 0
          BEGIN
            IF @BQTY = 0
            BEGIN
              FETCH NEXT FROM CursorBMain
			  INTO @CompanyCodeb, @Exchangeb, @Segmentb, @td_SRNOB, @td_dtB,  @td_clientcdB, @td_scripcdB, @td_bsflagB, @Qty_NSB, @td_RateB, 
					@VALUES_NSB, @FIFONOB
              SET @BQTY = @QTY_NSB
              SET @BDATE = @td_dtB
            END
          END
          IF @SQR_QTY1 <> 0
          BEGIN
            IF @SQTY = 0
            BEGIN
              FETCH NEXT FROM CursorSMain 
			  INTO @CompanyCodes, @Exchanges, @SegmentS, @td_SRNOS, @td_dtS, @td_clientcdS, @td_scripcdS, @td_bsflagS, @Qty_NSS, @td_RateS, 
			       @VALUES_NSS, @FIFONOS
              SET @SQTY = @QTY_NSS
              SET @SDATE  =  @td_dtS
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
		  INSERT INTO #tbl_DelvTrxnN2 (
			CompanyCode, Exchange,  Segment, td_SRNO, td_dt, td_clientcd, td_seriesid, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO, XTAG11, 
			LONG_TAG, SQR_TAG
			)
		  VALUES (
			@CompanyCodeb, @Exchangeb, @Segmentb, @td_SRNOB, @td_dtB, @td_clientcdB, @td_scripcdB, @td_bsflagB, @SQ, @td_RateB, @SQ * @td_RateB, 
			@FIFONOB, @COUNTER, (CASE WHEN SIGN(ABS(DATEDIFF(DAY, CAST(@td_dtB AS DATE), CAST(@td_dtS AS DATE))) - 365) = 1 THEN 'L' ELSE '' END
				), 'Y')

		  SET @BQTY = @BQTY - @SQ
     
         		
		  INSERT INTO #tbl_DelvTrxnN2 (
			CompanyCode, Exchange,  Segment, td_SRNO, td_dt, td_clientcd, td_seriesid, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO, XTAG11, 
			LONG_TAG, SQR_TAG
			)
		  VALUES (
			@CompanyCodes, @Exchanges, @Segments, @td_SRNOS, @td_dtS, @td_clientcdS, @td_scripcdS, @td_bsflagS, @SQ, @td_RateS, @SQ * @td_RateS, 
			@FIFONOS, @COUNTER, (CASE WHEN SIGN(ABS(DATEDIFF(DAY, CAST(@td_dtB AS DATE), CAST(@td_dtS AS DATE))) - 365) = 1 THEN 'L' ELSE '' END
				), 'Y'
			)
		  SET @SQTY = @SQTY - @SQ
    	  IF @BQTY = 0
		  BEGIN
			UPDATE #tbl_DelvTrxnN
			SET SQR_TAG = @TAG
			WHERE td_clientcd = @td_clientcdB AND FIFONO = @FIFONOB
		  END
		
		  IF @SQTY = 0
		  BEGIN
			UPDATE #tbl_DelvTrxnn
			SET SQR_TAG = @TAG
			WHERE td_clientcd = @td_clientcdS AND FIFONO = @FIFONOS
		  END

		  SET @SQR_QTY1 = @SQR_QTY1 - @SQ 
		  SET @SQR_QTY = @SQR_QTY - @SQ
		  SET @strFlag = 'N'

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
		  INSERT INTO #tbl_DelvTrxnN2 (
			CompanyCode, Exchange,  Segment, td_SRNO, td_dt, td_clientcd, td_seriesid, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO, XTAG11, 
			LONG_TAG, SQR_TAG
			)
		  VALUES (
			@CompanyCodeb, @Exchangeb, @Segmentb, @td_SRNOB, @td_dtB,  @td_clientcdB, @td_scripcdB, @td_bsflagB, @BQTY, @td_RateB, @BQTY * 
			@td_RateB, @FIFONOB, @COUNTER, '', ''
			)
		  UPDATE #tbl_DelvTrxnn
		  SET SQR_TAG = @TAG
		  WHERE td_clientcd = @td_clientcdB AND FIFONO = @FIFONOB
	    END
	
	    IF @SQTY <> 0
	    BEGIN
		  INSERT INTO #tbl_DelvTrxnN2 (
			CompanyCode, Exchange,  Segment, td_SRNO, td_dt, td_clientcd, td_seriesid, td_bsflag, Qty_NS, td_Rate, VALUES_NS, FIFONO, XTAG11, 
			LONG_TAG, SQR_TAG
			)
		  VALUES (
			@CompanyCodeb, @Exchangeb, @Segmentb, @td_SRNOS, @td_dtS, @td_clientcdS, @td_scripcdS, @td_bsflagS, @SQTY, @td_RateS, @SQTY * 
			@td_RateB, @FIFONOS, @COUNTER, '', ''
			)
		  UPDATE #tbl_DelvTrxnN
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
	
      DELETE FROM #tbl_DelvTrxnN WHERE SQR_TAG = 'C'
  
      INSERT INTO #tbl_DelvTrxnN 
      SELECT * FROM #tbl_DelvTrxnN2 WHERE isnull(Qty_NS,0) <> 0 

      DECLARE @tbl_CLClientAS TABLE(CompanyCode VARCHAR(10), Exchange VARCHAR(10), Segment VARCHAR(10),  ClientCode VARCHAR(50), ClientName VARCHAR(100), 
      Symbol VARCHAR(50), seriesid INT, ExpiryDate VARCHAR(8), Scrip VARCHAR(100), Multiplier INT, TrxnDate VARCHAR(8), EX_QTY MONEY, EX_VALUE MONEY, AS_QTY MONEY, AS_VALUE MONEY)
  
      INSERT INTO @tbl_CLClientAS(CompanyCode, Exchange, Segment, ClientCode, Scrip, Symbol, seriesid, ExpiryDate, 
	  Multiplier, TrxnDate, EX_QTY, EX_VALUE,
      AS_QTY, AS_VALUE)

      SELECT ex_companycode, ex_Exchange, sm_Segment='C', ex_clientcd, SM_DESC, sm_underlying, ex_seriesid, 
      sm_expirydt, sm_multiplier,  ex_dt,  ex_eqty = sum(ex_eqty) ,
      EX_VALUE = sum((ex_eqty)*ex_diffbrokrate*sm_multiplier*-1), ex_aqty = sum(ex_aqty), 
      AS_VALUE = sum((ex_aqty)*ex_diffbrokrate*sm_multiplier*-1) 
      FROM Exercise(NOLOCK), Series_master(NOLOCK), @tbl_UserList X  
      WHERE ex_dt between @dtFromDate and @dtToDt 
      AND ex_clientcd = X.Client_Code 
      and sm_expirydt >= @dtFromDate
      and ex_exchange = sm_exchange 
	  And ex_seriesid = sm_seriesid 
	  GROUP BY ex_companycode, ex_Exchange,  ex_clientcd, SM_DESC, sm_underlying, ex_seriesid, 
      sm_expirydt, sm_multiplier,  ex_dt

      
      INSERT INTO #tbl_ClientPL(CompanyCode, Exchange, Segment, ClientCode, seriesid, Scrip, 
	  ExpiryDate, Symbol, Multiplier, TrxnDate, BF_QTY, BF_CloseRate, BF_VALUE,
      BOT_QTY, BOT_VALUE, SOLD_QTY, SOLD_VALUE, EX_QTY, EX_VALUE,
      AS_QTY, AS_VALUE, AccountType)
      SELECT CompanyCode, Exchange,  Segment, td_clientcd, td_seriesid, ScripName, ExpriryDate, sm_underlying,
	  sm_multiplier, td_dt,
      BF_Qty, BF_RATE = ABS(CASE WHEN BF_Qty <> 0 THEN ROUND(BF_Value/BF_Qty,2) ELSE 0 END),
      BF_Value,
      BuyQty, BuyValue, SaleQty, SaleValue, 
      EX_QTY = ISNULL(ex_eqty,0),
      EX_VALUE = ISNULL(EX_VALUE,0),
      AS_QTY = ISNULL(ex_aqty,0),
      AS_VALUE = ISNULL(AS_VALUE,0), SQR_TAG
      FROM(
      SELECT CompanyCode, Exchange,  Segment, td_clientcd, td_seriesid, ScripName = SR.SM_DESC,  
	  ExpriryDate =   SR.sm_expirydt, sm_underlying, sm_multiplier, 
	  td_dt = CASE WHEN ISNULL(@strReportType,'')  = 'Series Wise Detail' THEN  td_dt ELSE @dtFromDate END,
      BF_Qty = SUM(CASE WHEN td_dt < @dtFromDate THEN (CASE WHEN td_bsflag = 'B' THEN  
      Qty_NS ELSE Qty_NS*-1 END) else 0 end), 
      BF_Value = ROUND(SUM(CASE WHEN td_dt < @dtFromDate AND Qty_NS <> 0 THEN (CASE WHEN td_bsflag = 'B' THEN  
      (Qty_NS*TD_RATE)*-1 ELSE (Qty_NS*TD_RATE) END) else 0 end),2), 
      BuyQty = SUM(CASE WHEN td_dt >= @dtFromDate THEN (CASE WHEN td_bsflag = 'B' THEN  
      Qty_NS ELSE 0 END) else 0 end),
      BuyValue = SUM(CASE WHEN td_dt >= @dtFromDate THEN (CASE WHEN td_bsflag = 'B' THEN  
      Qty_NS*td_rate ELSE 0 END) else 0 end),
      SaleQty = SUM(CASE WHEN td_dt >= @dtFromDate THEN (CASE WHEN td_bsflag = 'S' THEN  
      Qty_NS ELSE 0 END) else 0 end),
      SaleValue = SUM(CASE WHEN td_dt >= @dtFromDate THEN (CASE WHEN td_bsflag = 'S' THEN  
      Qty_NS*td_rate ELSE 0 END) else 0 end), SQR_TAG = ISNULL(SQR_TAG,'')
      FROM #tbl_DelvTrxnN X, Series_master(NOLOCK) SR
      where x.td_seriesid = sr.sm_seriesid
	  AND x.exchange = SR.sm_exchange
	  AND X.Segment  = SR.sm_Segment
      Group By CompanyCode, Exchange,  Segment, td_clientcd, td_seriesid, SR.SM_DESC, SR.sm_expirydt, ISNULL(SQR_TAG,''), 
	  sm_multiplier, sm_underlying, CASE WHEN ISNULL(@strReportType,'')  = 'Series Wise Detail' THEN  td_dt ELSE @dtFromDate END) X123 LEFT OUTER JOIN ( select ClientCode, seriesid, 
	  TrxnDate = CASE WHEN ISNULL(@strReportType,'')  = 'Series Wise Detail' THEN  TrxnDate ELSE @dtFromDate END, 
	  ex_eqty = sum(EX_QTY) ,
      EX_VALUE = sum(EX_VALUE), ex_aqty = sum(AS_QTY), 
      AS_VALUE = sum(AS_VALUE) 
      FROM @tbl_CLClientAS
      GROUP BY ClientCode, seriesid, CASE WHEN ISNULL(@strReportType,'')  = 'Series Wise Detail' 
	  THEN  TrxnDate ELSE @dtFromDate END) EXAS 
	  ON( td_clientcd = EXAS.ClientCode AND td_seriesid = EXAS.seriesid and ISNULL(SQR_TAG,'') = ''
      AND td_dt = TrxnDate)
      ORDER BY ScripName
    END
	
	
	
	IF @strRateType NOT IN('Underlying Close Price','Do not Valuate')
	BEGIN
	  ---@@VAIBHAV/03-JUL-2024 START
	  if @strRateType = 'Average price' 
	  BEGIN
	    
		UPDATE A SET A.NET_QTY = (ISNULL(A.BF_QTY,0)+ISNULL(A.BOT_QTY,0))-ISNULL(A.SOLD_QTY,0)-ISNULL(A.AS_QTY,0)- ISNULL(A.EX_QTY,0),
        A.NET_VALUE = ROUND(((ISNULL(A.BF_QTY,0)+ISNULL(A.BOT_QTY,0))-ISNULL(A.SOLD_QTY,0)-ISNULL(A.AS_QTY,0)- ISNULL(A.EX_QTY,0))*ms_lastprice*isnull(Multiplier,1),2),
        cmp = ms_lastprice
        FROM #tbl_ClientPL A , Market_summary(NOLOCK) X
        WHERE ms_seriesid = seriesid 
        and ms_exchange = exchange --and ms_segment = Segment 
        and ms_dt  = (select  max(ms_dt) from Market_summary(NOLOCK)  
	    WHERE ms_exchange = X.ms_exchange --and ms_segment = X.ms_segment 
		and ms_dt<=@dtToDt)
		
		UPDATE A SET A.MTM = ((ISNULL(A.SOLD_VALUE,0)-isnull(A.BOT_VALUE,0))+ISNULL(A.BF_VALUE,0)-ISNULL(A.AS_VALUE,0)-ISNULL(A.EX_VALUE,0))+isnull(NET_VALUE,0)
        FROM #tbl_ClientPL A	  
	  		
	  	UPDATE A SET A.MTM =((ISNULL(A.SOLD_VALUE,0)-isnull(A.BOT_VALUE,0))+ISNULL(A.BF_VALUE,0)-ISNULL(A.AS_VALUE,0)-ISNULL(A.EX_VALUE,0)),
	    A.NET_VALUE = 0 
	    FROM #tbl_ClientPL A
	    WHERE  EXISTS(SELECT 1 FROM Series_master(NOLOCK)
	    WHERE --sm_Segment =  A.Segment AND 
		sm_exchange = A.Exchange AND sm_seriesid = A.seriesid   
		AND sm_prodtype IN('EO','CO','IO'))
	    AND A.NET_QTY <> 0	
	    AND ExpiryDate <= @dtToDt
	  
	    UPDATE A SET A.MTM = case WHEN NET_QTY > 0 THEN 
		((ABS(ISNULL(A.BOT_QTY,0)*ROUND((ISNULL(A.BOT_VALUE,0)/ISNULL(A.BOT_QTY,0)),2))-isnull(A.BOT_VALUE,0))+ISNULL(A.BF_VALUE,0)-ISNULL(A.AS_VALUE,0)-ISNULL(A.EX_VALUE,0))
		ELSE ((ISNULL(A.SOLD_VALUE,0)-ABS(ISNULL(A.SOLD_QTY,0)*ROUND((ISNULL(A.SOLD_VALUE,0)/ISNULL(A.SOLD_QTY,0)),2)))+ISNULL(A.BF_VALUE,0)-ISNULL(A.AS_VALUE,0)-ISNULL(A.EX_VALUE,0)) END,
	    A.NET_VALUE = 0 
	    FROM #tbl_ClientPL A
	    WHERE EXISTS(SELECT 1 FROM Series_master(NOLOCK)
	    WHERE --sm_Segment =  A.Segment AND 
		sm_exchange = A.Exchange AND sm_seriesid = A.seriesid   
		AND sm_prodtype IN('EO','CO','IO'))
		AND A.NET_QTY <> 0	
	    AND ExpiryDate > @dtToDt
			
      END
	  ELSE
	  BEGIN
	     
	    UPDATE A SET A.NET_QTY = (ISNULL(A.BF_QTY,0)+ISNULL(A.BOT_QTY,0))-ISNULL(A.SOLD_QTY,0)-ISNULL(A.AS_QTY,0)- ISNULL(A.EX_QTY,0),
        A.NET_VALUE = ROUND(((ISNULL(A.BF_QTY,0)+ISNULL(A.BOT_QTY,0))-ISNULL(A.SOLD_QTY,0)-ISNULL(A.AS_QTY,0)- ISNULL(A.EX_QTY,0))*ms_lastprice*isnull(Multiplier,1),2),
        cmp = ms_lastprice
        FROM #tbl_ClientPL A , Market_summary(NOLOCK) X
        WHERE ms_seriesid = seriesid 
        and ms_exchange = exchange --and ms_segment = Segment 
        and ms_dt  = (select  max(ms_dt) from Market_summary(NOLOCK)  
	    WHERE ms_exchange = X.ms_exchange --and ms_segment = X.ms_segment 
		and ms_dt<=@dtToDt )
	  
  	    UPDATE A SET A.MTM = ((ISNULL(A.SOLD_VALUE,0)-isnull(A.BOT_VALUE,0))+ISNULL(A.BF_VALUE,0)-ISNULL(A.AS_VALUE,0)-ISNULL(A.EX_VALUE,0))+isnull(NET_VALUE,0)
        FROM #tbl_ClientPL A	  
	  
	    UPDATE A SET A.MTM = ((ISNULL(A.SOLD_VALUE,0)-isnull(A.BOT_VALUE,0))+ISNULL(A.BF_VALUE,0)-ISNULL(A.AS_VALUE,0)-ISNULL(A.EX_VALUE,0)),
	    A.NET_VALUE = 0 
	    FROM #tbl_ClientPL A
	    WHERE  EXISTS(SELECT 1 FROM Series_master(NOLOCK)
	    WHERE --sm_Segment =  A.Segment AND 
		sm_exchange = A.Exchange AND sm_seriesid = A.seriesid   
		AND sm_prodtype IN('EO','CO','IO'))
	    AND A.NET_QTY <> 0	
	    AND ExpiryDate <= @dtToDt
		
		
	  END
      ---@@VAIBHAV/03-JUL-2024 END	  
	END  
	IF @strRateType = 'Do not Valuate'
	BEGIN
	  UPDATE A SET A.NET_QTY = (ISNULL(A.BF_QTY,0)+ISNULL(A.BOT_QTY,0))-ISNULL(A.SOLD_QTY,0)-ISNULL(A.AS_QTY,0)- ISNULL(A.EX_QTY,0),
      A.NET_VALUE = ROUND(((ISNULL(A.BF_QTY,0)+ISNULL(A.BOT_QTY,0))-ISNULL(A.SOLD_QTY,0)-ISNULL(A.AS_QTY,0)- ISNULL(A.EX_QTY,0))*ms_lastprice*Multiplier,2),
      cmp = ms_lastprice
      FROM #tbl_ClientPL A, Market_summary(NOLOCK) X
      WHERE ms_seriesid = seriesid 
      and ms_exchange = exchange --and ms_segment = Segment 
      and ms_dt  = (select  max(ms_dt) from Market_summary(NOLOCK)  
	  WHERE ms_exchange = X.ms_exchange --and ms_segment = X.ms_segment 
	  and ms_dt<=@dtToDt )
	  
	  UPDATE A SET A.NET_VALUE = 0 
	  FROM #tbl_ClientPL A
	  WHERE EXISTS(SELECT 1
	  FROM Series_master(NOLOCK) WHERE --sm_Segment =  A.Segment AND 
	  sm_exchange = A.Exchange 
	  AND sm_seriesid = A.seriesid  AND sm_prodtype IN('EO','CO','IO')) 
	  AND A.NET_QTY <> 0
	  
	  UPDATE A SET A.MTM = ((ISNULL(A.SOLD_VALUE,0)-isnull(A.BOT_VALUE,0))+ISNULL(A.BF_VALUE,0)-ISNULL(A.AS_VALUE,0)-ISNULL(A.EX_VALUE,0))+isnull(NET_VALUE,0)
      FROM #tbl_ClientPL A
	  
	END
	IF @strRateType = 'Underlying Close Price'
	BEGIN
	  UPDATE A SET A.NET_QTY = (ISNULL(A.BF_QTY,0)+ISNULL(A.BOT_QTY,0))-ISNULL(A.SOLD_QTY,0)-ISNULL(A.AS_QTY,0)- ISNULL(A.EX_QTY,0),
      A.NET_VALUE = ROUND(((ISNULL(A.BF_QTY,0)+ISNULL(A.BOT_QTY,0))-ISNULL(A.SOLD_QTY,0)-ISNULL(A.AS_QTY,0)- ISNULL(A.EX_QTY,0))*ms_lastprice*Multiplier,2),
      cmp = ms_lastprice
      FROM #tbl_ClientPL A, Market_summary(NOLOCK) X
      WHERE ms_seriesid = seriesid 
      and ms_exchange = exchange --and ms_segment = Segment 
      and ms_dt  = (select  max(ms_dt) from Market_summary(NOLOCK)  
	  WHERE ms_exchange = X.ms_exchange --and ms_segment = X.ms_segment 
	  and ms_dt<=@dtToDt )
	  AND NOT EXISTS(SELECT 1
	  FROM Series_master(NOLOCK) WHERE --sm_Segment =  A.Segment AND 
	  sm_exchange = A.Exchange 
	  AND sm_seriesid = A.seriesid  AND sm_prodtype IN('EO','CO','IO'))  

	  SET @BFseriesid = 0 
	  SET @spotCloseRate =0
	  SET @strSegment = ''
	  SET @strExchange = ''

	  DECLARE db_CursorRateCF CURSOR FOR         
      SELECT distinct seriesid, Exchange, Segment = 'C'
      FROM #tbl_ClientPL A 
	  where NET_QTY <> 0 and EXISTS(SELECT 1 FROM Series_master(NOLOCK)
	  WHERE --sm_Segment =  A.Segment AND 
	  sm_exchange = A.Exchange  AND sm_seriesid = A.seriesid AND sm_productcd in('OPTSTK'))
      OPEN db_CursorRateCF       
      FETCH NEXT FROM db_CursorRateCF INTO @BFseriesid, @strExchange, @strSegment
      WHILE @@FETCH_STATUS = 0     
      BEGIN
	    SELECT TOP 1 @spotCloseRate = mk_closerate FROM Market_Rates(NOLOCK) X WHERE mk_scripcd IN(
        SELECT SS_cD FROM Securities WHERE ss_bsymbol IN(
        SELECT sm_symbol FROM Series_master(NOLOCK) WHERE --sm_Segment = @strSegment and 
		sm_exchange = @strExchange 
		AND sm_seriesid = @BFseriesid))
        AND mk_dt   IN(SELECT MAX(mk_dt) FROM Market_Rates(NOLOCK) WHERE mk_dt <= @dtToDt)
        ORDER BY CASE WHEN mk_exchange='N' THEN 1 ELSE 2 END
	  
	    UPDATE A SET A.cmp = @spotCloseRate, a.NET_VALUE =(a.NET_QTY*@spotCloseRate)*-1
	    FROM #tbl_ClientPL A
	    WHERE A.seriesid = @BFseriesid
		AND A.Exchange = @strExchange
		AND A.Segment = @strSegment
	   FETCH NEXT FROM db_CursorRateCF INTO @BFseriesid, @strExchange, @strSegment
      END        
      CLOSE db_CursorRateCF        
      DEALLOCATE db_CursorRateCF	

	  UPDATE A SET A.MTM = ((ISNULL(A.SOLD_VALUE,0)-isnull(A.BOT_VALUE,0))+ISNULL(A.BF_VALUE,0)-ISNULL(A.AS_VALUE,0)-ISNULL(A.EX_VALUE,0))+isnull(NET_VALUE,0)
      FROM #tbl_ClientPL A
	  
	  UPDATE A SET A.MTM = ((ISNULL(A.SOLD_VALUE,0)-isnull(A.BOT_VALUE,0))+ISNULL(A.BF_VALUE,0)-ISNULL(A.AS_VALUE,0)-ISNULL(A.EX_VALUE,0)),
	  A.NET_VALUE = 0 
	  FROM #tbl_ClientPL A
	  WHERE  EXISTS(SELECT 1 FROM Series_master(NOLOCK)
	  WHERE --sm_Segment =  A.Segment AND 
	  sm_exchange = A.Exchange AND sm_seriesid = A.seriesid   
	  AND sm_prodtype IN('EO','CO','IO'))
	  AND A.NET_QTY <> 0	
	  AND ExpiryDate <= @dtToDt
	  
	  /*(UPDATE A SET A.MTM = ((ISNULL(A.SOLD_VALUE,0)-isnull(A.BOT_VALUE,0))+ISNULL(A.BF_VALUE,0)-ISNULL(A.AS_VALUE,0)-ISNULL(A.EX_VALUE,0)),
	  A.NET_VALUE = 0 
	  FROM #tbl_ClientPL A
	  WHERE EXISTS(SELECT 1
	  FROM Series_master(NOLOCK) WHERE sm_seriesid = A.seriesid  AND sm_prodtype IN('EO','CO','IO')) 
	  AND A.NET_QTY > 0	
	  */
	END    
	
    UPDATE A SET A.ClientName = CM.cm_name
    FROM #tbl_ClientPL A, Client_Master CM
    WHERE A.ClientCode = CM.cm_cd

  
    declare @tbl_billCharges TABLE(CompanyCode VARCHAR(10), Exchange VARCHAR(10), Segment VARCHAR(10), 
    ClientCode VARCHAR(10) NOT NULL,
    ChargesDescp [char] (40) NOT NULL,
    [bc_amount] [money] NOT NULL)
  
    INSERT INTO @tbl_billCharges (CompanyCode, Exchange, Segment, ClientCode, ChargesDescp, [bc_amount])
    SELECT fc_companycode, fc_Exchange, '', fc_clientcd, fc_desc, round(sum(fc_amount),2)  
    FROM Fspecialcharges(NOLOCK), (SELECT DISTINCT Client_Code = ClientCode  FROM #tbl_ClientPL) X
    where fc_clientcd = x.Client_COde and fc_dt >= @dtFromDate and fc_dt<= @dtToDt 
    AND ((fc_companycode+fc_exchange IN(SELECT ExchangeCode FROM @tbl_Exchange) and @ExchSeg <> '') OR @ExchSeg = '')
    GROUP BY fc_clientcd, fc_desc,fc_companycode, fc_Exchange
    HAVING ROUND(SUM(fc_amount),2) <> 0
 
    INSERT INTO @tbl_billCharges (CompanyCode, Exchange, Segment, ClientCode, ChargesDescp, [bc_amount])
    SELECT fc_companycode, fc_Exchange, '', fc_clientcd,'SERVICE TAX', round(sum(fc_servicetax),2)
    FROM Fspecialcharges(NOLOCK), (SELECT DISTINCT Client_Code =ClientCode FROM #tbl_ClientPL) X
    WHERE fc_clientcd = x.Client_COde
    AND fc_dt >= @dtFromDate and fc_dt<= @dtToDt 
    AND ((fc_companycode+fc_exchange IN(SELECT ExchangeCode FROM @tbl_Exchange) and @ExchSeg <> '') OR @ExchSeg = '')
    GROUP BY fc_clientcd,fc_desc,fc_companycode, fc_Exchange  
    HAVING ROUND(SUM(fc_servicetax),2) <> 0
    DECLARE @XMLDATA1 XML=''
	IF ISNULL(@strReportType,'') in('Series Wise','')
	BEGIN
	  IF @strOutputType = 'X'
	  BEGIN
	    IF ISNULL(@StrSummary,'N') = 'N'
		BEGIN
	      SET @XMLDATA1 = (SELECT * FROM (
	      SELECT TAG = '1', Exchange, Segment, ClientCode, ClientName, 
          seriesid, ExpiryDate, Scrip, Multiplier,  BF_QTY = ISNULL(BF_QTY,0), BF_CloseRate = ISNULL(BF_CloseRate,0), 
          BF_VALUE = ISNULL(BF_VALUE,0), BOT_QTY = ISNULL(BOT_QTY,0), 
	      BOT_RATE = CAST(round((CASE WHEN ISNULL(BOT_QTY,0) <> 0 THEN ISNULL(BOT_VALUE,0)/ISNULL(BOT_QTY,0) ELSE 0 END),4) AS MONEY),
	      BOT_VALUE = ISNULL(BOT_VALUE,0), 
          SOLD_QTY = ISNULL(SOLD_QTY,0), 
	      SOLD_RATE = CAST(round((CASE WHEN ISNULL(SOLD_QTY,0) <> 0 THEN ISNULL(SOLD_VALUE,0)/ISNULL(SOLD_QTY,0) ELSE 0 END),4) AS MONEY),
	      SOLD_VALUE = ISNULL(SOLD_VALUE,0), EX_QTY = ISNULL(EX_QTY,0), EX_VALUE = ISNULL(EX_VALUE,0), 
	      AS_QTY = ISNULL(AS_QTY,0), 
          AS_VALUE = ISNULL(AS_VALUE,0), NET_QTY = ISNULL(NET_QTY,0),
          CMP =  ISNULL(CMP,0), NET_VALUE = ISNULL(NET_VALUE,0), MTM = ISNULL(MTM ,0), 
	      AccountType = ISNULL(AccountType,'N'),
          RelPL = CASE WHEN (ISNULL(AccountType,'N') ='Y' OR ISNULL(NET_QTY,0) = 0) 
	      THEN ISNULL(MTM ,0) ELSE 0 END,
	      UnRelPL = CASE WHEN ISNULL(AccountType,'') ='' AND ISNULL(NET_QTY,0) <> 0 THEN  ISNULL(MTM ,0) ELSE 0 END,
		  Lookup = Exchange+Segment+CAST(seriesid AS VARCHAR), Charges = 0
          FROM #tbl_ClientPL
          UNION ALL
          SELECT TAG = 2, Exchange, Segment, ClientCode, ClientName = '', seriesid = '', ExpiryDate= '', Scrip = ChargesDescp, 
	      Multiplier = 0,BF_QTY = 0, BF_CloseRate = 0, BF_VALUE = 0, BOT_QTY = 0, BOT_RATE = 0, BOT_VALUE = 0,
	      SOLD_QTY = 0, SOLD_RATE = 0, SOLD_VALUE = 0, EX_QTY = 0, EX_VALUE = 0, AS_QTY = 0, AS_VALUE = 0, NET_QTY = 0, CMP = 0, NET_VALUE = 0, 
	      MTM = (bc_amount)*-1, AccountType = '', RelPL = (ISNULL(bc_amount,0))*-1, UnRelPL = 0, Lookup = Exchange+Segment, Charges = ISNULL(bc_amount,0)*-1
	      FROM @tbl_billCharges) X1 --WHERE ((@strRequestFrom = 'M' AND TAG <> 2) OR  @strRequestFrom <> 'M')
		  WHERE ((seriesid <> '' and @StrLookup <> '') or isnull(@StrLookup,'') = '')
		  ORDER BY ClientCode, TAG, Scrip FOR XML PATH('ProfitLoss'))
	      SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	    END
	    ELSE
	    IF ISNULL(@StrSummary,'N') = 'Y'
		BEGIN
		  SET @XMLDATA1 = (SELECT TotalCharges = SUM(CASE WHEN ISNULL(seriesid,'') = '' THEN round(isnull(MTM,0),2) ELSE 0 END),
		  RelPl = SUM(CASE WHEN ISNULL(seriesid,'') <> '' THEN round(isnull(RelPl,0),2) ELSE 0 END), 
		  UnRelPL = SUM(CASE WHEN seriesid <> '' THEN round(UnRelPL,2) ELSE 0 END)  FROM (
	      SELECT TAG = '1', Exchange, Segment, ClientCode, ClientName, 
          seriesid, ExpiryDate, Scrip, Multiplier,  BF_QTY = ISNULL(BF_QTY,0), BF_CloseRate = ISNULL(BF_CloseRate,0), 
          BF_VALUE = ISNULL(BF_VALUE,0), BOT_QTY = ISNULL(BOT_QTY,0), 
	      BOT_RATE = CAST(round((CASE WHEN ISNULL(BOT_QTY,0) <> 0 THEN ISNULL(BOT_VALUE,0)/ISNULL(BOT_QTY,0) ELSE 0 END),4) AS MONEY),
	      BOT_VALUE = ISNULL(BOT_VALUE,0), 
          SOLD_QTY = ISNULL(SOLD_QTY,0), 
	      SOLD_RATE = CAST(round((CASE WHEN ISNULL(SOLD_QTY,0) <> 0 THEN ISNULL(SOLD_VALUE,0)/ISNULL(SOLD_QTY,0) ELSE 0 END),4) AS MONEY),
	      SOLD_VALUE = ISNULL(SOLD_VALUE,0), EX_QTY = ISNULL(EX_QTY,0), EX_VALUE = ISNULL(EX_VALUE,0), 
	      AS_QTY = ISNULL(AS_QTY,0), 
          AS_VALUE = ISNULL(AS_VALUE,0), NET_QTY = ISNULL(NET_QTY,0),
          CMP =  ISNULL(CMP,0), NET_VALUE = ISNULL(NET_VALUE,0), MTM = ISNULL(MTM ,0), 
	      AccountType = ISNULL(AccountType,'N'),
          RelPL = CASE WHEN (ISNULL(AccountType,'N') ='Y' OR ISNULL(NET_QTY,0) = 0) 
	      THEN ISNULL(MTM ,0) ELSE 0 END,
	      UnRelPL = CASE WHEN ISNULL(AccountType,'') ='' AND ISNULL(NET_QTY,0) <> 0 THEN  ISNULL(MTM ,0) ELSE 0 END,
		  Lookup = Exchange+Segment+CAST(seriesid AS VARCHAR)
          FROM #tbl_ClientPL
          UNION ALL
          SELECT TAG = 2, Exchange, Segment, ClientCode, ClientName = '', seriesid = '', ExpiryDate= '', Scrip = ChargesDescp, 
	      Multiplier = 0,BF_QTY = 0, BF_CloseRate = 0, BF_VALUE = 0, BOT_QTY = 0, BOT_RATE = 0, BOT_VALUE = 0,
	      SOLD_QTY = 0, SOLD_RATE = 0, SOLD_VALUE = 0, EX_QTY = 0, EX_VALUE = 0, AS_QTY = 0, AS_VALUE = 0, NET_QTY = 0, CMP = 0, NET_VALUE = 0, 
	      MTM = (bc_amount)*-1, AccountType = '', RelPL = (ISNULL(bc_amount,0))*-1, UnRelPL = 0, Lookup = Exchange+Segment
	      FROM @tbl_billCharges) X1 
		  FOR XML PATH('ProfitLoss'))
	      SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	    END
      END		
	  ELSE
	  BEGIN
		SELECT * FROM (
	    SELECT TAG = '1', Exchange, ClientCode, ClientName, 
        seriesid, ExpiryDate, Scrip, Multiplier,  BF_QTY = ISNULL(BF_QTY,0), BF_CloseRate = ISNULL(BF_CloseRate,0), 
        BF_VALUE = ISNULL(BF_VALUE,0), BOT_QTY = ISNULL(BOT_QTY,0), 
	    BOT_RATE = CAST(round((CASE WHEN ISNULL(BOT_QTY,0) <> 0 THEN ISNULL(BOT_VALUE,0)/ISNULL(BOT_QTY,0) ELSE 0 END),4) AS MONEY),
	    BOT_VALUE = ISNULL(BOT_VALUE,0), 
        SOLD_QTY = ISNULL(SOLD_QTY,0), 
	    SOLD_RATE = CAST(round((CASE WHEN ISNULL(SOLD_QTY,0) <> 0 THEN ISNULL(SOLD_VALUE,0)/ISNULL(SOLD_QTY,0) ELSE 0 END),4) AS MONEY),
	    SOLD_VALUE = ISNULL(SOLD_VALUE,0), EX_QTY = ISNULL(EX_QTY,0), EX_VALUE = ISNULL(EX_VALUE,0), 
	    AS_QTY = ISNULL(AS_QTY,0), 
        AS_VALUE = ISNULL(AS_VALUE,0), NET_QTY = ISNULL(NET_QTY,0),
        CMP =  ISNULL(CMP,0), NET_VALUE = ISNULL(NET_VALUE,0), MTM = ISNULL(MTM ,0), 
	    AccountType = ISNULL(AccountType,'N'),
        RelPL = CASE WHEN (ISNULL(AccountType,'N') ='Y' OR ISNULL(NET_QTY,0) = 0) 
	    THEN ISNULL(MTM ,0) ELSE 0 END,
	    UnRelPL = CASE WHEN ISNULL(AccountType,'') ='' AND ISNULL(NET_QTY,0) <> 0 THEN  ISNULL(MTM ,0) ELSE 0 END,
		Lookup = CAST(seriesid AS VARCHAR)
        FROM #tbl_ClientPL
        UNION ALL
        SELECT TAG = 2, Exchange, ClientCode, ClientName = '', seriesid = '', ExpiryDate= '', Scrip = ChargesDescp, 
	    Multiplier = 0,BF_QTY = 0, BF_CloseRate = 0, BF_VALUE = 0, BOT_QTY = 0, BOT_RATE = 0, BOT_VALUE = 0,
	    SOLD_QTY = 0, SOLD_RATE = 0, SOLD_VALUE = 0, EX_QTY = 0, EX_VALUE = 0, AS_QTY = 0, AS_VALUE = 0, NET_QTY = 0, CMP = 0, NET_VALUE = 0, 
	    MTM = (bc_amount)*-1, AccountType = '', RelPL = (ISNULL(bc_amount,0))*-1, UnRelPL = 0, Lookup = Exchange+Segment
	    FROM @tbl_billCharges) X1 ORDER BY ClientCode, TAG, Scrip
		
	  END  
	  
	END
    ELSE IF ISNULL(@strReportType,'') in('Underlying wise')	
	BEGIN
	  SELECT * FROM (
	  select TAG = '1', Exchange, ClientCode, ClientName, Symbol, BOT_QTY = sum(BOT_QTY), 
	  SOLD_QTY = sum(SOLD_QTY), 
	  NET_VALUE = Sum(NET_VALUE), MTM = sum(MTM)
	  from(
	  SELECT Exchange, ClientCode, ClientName, 
      Symbol, seriesid, ExpiryDate, Scrip, Multiplier,  BF_QTY = ISNULL(BF_QTY,0), BF_CloseRate = ISNULL(BF_CloseRate,0), 
      BF_VALUE = ISNULL(BF_VALUE,0), BOT_QTY = ISNULL(BOT_QTY,0), BOT_VALUE = ISNULL(BOT_VALUE,0), 
      SOLD_QTY = ISNULL(SOLD_QTY,0), 
	  SOLD_VALUE = ISNULL(SOLD_VALUE,0), EX_QTY = ISNULL(EX_QTY,0), EX_VALUE = ISNULL(EX_VALUE,0), AS_QTY = ISNULL(AS_QTY,0), 
      AS_VALUE = ISNULL(AS_VALUE,0), NET_QTY = ISNULL(NET_QTY,0),
      CMP =  ISNULL(CMP,0), NET_VALUE = ISNULL(NET_VALUE,0), MTM = ISNULL(MTM ,0)
      FROM #tbl_ClientPL X) x12 GROUP BY Exchange, ClientCode, ClientName, Symbol
      UNION ALL
      SELECT TAG = 2, Exchange, ClientCode, ClientName = '', Symbol = ChargesDescp, BOT_QTY = 0, SOLD_QTY = 0,
	  NET_VALUE = 0, 
	  MTM = (ISNULL(bc_amount,0))*-1
	  FROM @tbl_billCharges) X1 ORDER BY ClientCode, TAG, Symbol
	END
	ELSE IF ISNULL(@strReportType,'') in('Series Wise Detail','')
	BEGIN
	  IF @strOutputType <> 'X'
	  BEGIN
	    SELECT * FROM (
	    SELECT TAG = '1', Exchange, ClientCode, ClientName, 
        seriesid, ExpiryDate, Scrip, Multiplier,  TrxnDate, BF_QTY = ISNULL(BF_QTY,0), BF_CloseRate = ISNULL(BF_CloseRate,0), 
        BF_VALUE = ISNULL(BF_VALUE,0), BOT_QTY = ISNULL(BOT_QTY,0), 
	    BOT_RATE = CAST(ROUND((CASE WHEN ISNULL(BOT_QTY,0) <> 0 THEN ISNULL(BOT_VALUE,0)/ISNULL(BOT_QTY,0) ELSE 0 END),4) AS MONEY),
	    BOT_VALUE = ISNULL(BOT_VALUE,0), 
        SOLD_QTY = ISNULL(SOLD_QTY,0), 
	    SOLD_RATE = CAST(ROUND((CASE WHEN ISNULL(SOLD_QTY,0) <> 0 THEN ISNULL(SOLD_VALUE,0)/ISNULL(SOLD_QTY,0) ELSE 0 END),4) AS MONEY),
	    SOLD_VALUE = ISNULL(SOLD_VALUE,0), EX_QTY = ISNULL(EX_QTY,0), EX_VALUE = ISNULL(EX_VALUE,0), AS_QTY = ISNULL(AS_QTY,0), 
        AS_VALUE = ISNULL(AS_VALUE,0), NET_QTY = ISNULL(NET_QTY,0),
        CMP =  ISNULL(CMP,0), NET_VALUE = ISNULL(NET_VALUE,0), MTM = ISNULL(MTM ,0), 
	    AccountType = ISNULL(AccountType,'N'),
        RelPL = CASE WHEN (ISNULL(AccountType,'N') ='Y' OR ISNULL(NET_QTY,0) = 0) 
	    THEN ISNULL(MTM ,0) ELSE 0 END,
	    UnRelPL = CASE WHEN ISNULL(AccountType,'') ='' AND ISNULL(NET_QTY,0) <> 0 THEN  ISNULL(MTM ,0) ELSE 0 END
        FROM #tbl_ClientPL
        UNION ALL
        SELECT TAG = 2, Exchange, ClientCode, ClientName = '', seriesid = '', ExpiryDate= '', Scrip = ChargesDescp, 
	    Multiplier = 0,TrxnDate = @dtToDt , BF_QTY = 0, BF_CloseRate = 0, BF_VALUE = 0, BOT_QTY = 0, BOT_RATE = 0, 
	    BOT_VALUE = 0,
	    SOLD_QTY = 0, SOLD_RATE = 0, SOLD_VALUE = 0, EX_QTY = 0, EX_VALUE = 0, AS_QTY = 0, AS_VALUE = 0, NET_QTY = 0, CMP = 0, NET_VALUE = 0, 
	    MTM = (bc_amount)*-1, AccountType = '', RelPL = (ISNULL(bc_amount,0))*-1, UnRelPL = 0
	    FROM @tbl_billCharges) X1 ORDER BY ClientCode, TAG, Scrip, TrxnDate
	  END
	  ELSE IF @strOutputType = 'X'
	  BEGIN
		SET @XMLDATA1 = (SELECT * FROM (
	    SELECT TAG = '1', Exchange, ClientCode, ClientName, 
        seriesid, ExpiryDate, Scrip, Multiplier,  TrxnDate, BF_QTY = ISNULL(BF_QTY,0), BF_CloseRate = ISNULL(BF_CloseRate,0), 
        BF_VALUE = ISNULL(BF_VALUE,0), BOT_QTY = ISNULL(BOT_QTY,0), 
	    BOT_RATE = CAST(ROUND((CASE WHEN ISNULL(BOT_QTY,0) <> 0 THEN ISNULL(BOT_VALUE,0)/ISNULL(BOT_QTY,0) ELSE 0 END),4) AS MONEY),
	    BOT_VALUE = ISNULL(BOT_VALUE,0), 
        SOLD_QTY = ISNULL(SOLD_QTY,0), 
	    SOLD_RATE = CAST(ROUND((CASE WHEN ISNULL(SOLD_QTY,0) <> 0 THEN ISNULL(SOLD_VALUE,0)/ISNULL(SOLD_QTY,0) ELSE 0 END),4) AS MONEY),
	    SOLD_VALUE = ISNULL(SOLD_VALUE,0), EX_QTY = ISNULL(EX_QTY,0), EX_VALUE = ISNULL(EX_VALUE,0), AS_QTY = ISNULL(AS_QTY,0), 
        AS_VALUE = ISNULL(AS_VALUE,0), NET_QTY = ISNULL(NET_QTY,0),
        CMP =  ISNULL(CMP,0), NET_VALUE = ISNULL(NET_VALUE,0), MTM = ISNULL(MTM ,0), 
	    AccountType = ISNULL(AccountType,'N'),
        RelPL = CASE WHEN (ISNULL(AccountType,'N') ='Y' OR ISNULL(NET_QTY,0) = 0) 
	    THEN ISNULL(MTM ,0) ELSE 0 END,
	    UnRelPL = CASE WHEN ISNULL(AccountType,'') ='' AND ISNULL(NET_QTY,0) <> 0 THEN  ISNULL(MTM ,0) ELSE 0 END
        FROM #tbl_ClientPL
        UNION ALL
        SELECT TAG = 2, Exchange, ClientCode, ClientName = '', seriesid = '', ExpiryDate= '', Scrip = ChargesDescp, 
	    Multiplier = 0,TrxnDate = @dtToDt , BF_QTY = 0, BF_CloseRate = 0, BF_VALUE = 0, BOT_QTY = 0, BOT_RATE = 0, 
	    BOT_VALUE = 0,
	    SOLD_QTY = 0, SOLD_RATE = 0, SOLD_VALUE = 0, EX_QTY = 0, EX_VALUE = 0, AS_QTY = 0, AS_VALUE = 0, NET_QTY = 0, CMP = 0, NET_VALUE = 0, 
	    MTM = (bc_amount)*-1, AccountType = '', RelPL = (ISNULL(bc_amount,0))*-1, UnRelPL = 0
	    FROM @tbl_billCharges) X1 
		WHERE ((seriesid <> '' and @StrLookup <> '') or isnull(@StrLookup,'') = '')
		ORDER BY ClientCode, TAG, Scrip, TrxnDate 
		FOR XML PATH('ProfitLoss'))
	    SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	  END
	END
  END
  DROP TABLE #tbl_ClientPL
  END TRY
  BEGIN CATCH
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = ERROR_MESSAGE()
    RETURN 1
  END CATCH
  SET @o_vcErrorFlag  = 'S'
  --SET @o_vcErrorMessage = 'Process Completed'
  RETURN 1
END  
GO
