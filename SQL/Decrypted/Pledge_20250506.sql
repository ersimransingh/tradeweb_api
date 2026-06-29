CREATE FUNCTION dbo.fn_SplitString (@input NVARCHAR(MAX), @delimiter CHAR(1))
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

CREATE PROCEDURE stpr_GetAPIHoldingDetail @i_vcXML VARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, 
@o_vcErrorMessage VARCHAR(MAX) OUTPUT WITH ENCRYPTION AS
BEGIN
  DECLARE @XMLData xml = '', @strClientCode VARCHAR(50)='', @StrString VARCHAR(MAX)='', 
  @dtAsOnDate VARCHAR(20) = CONVERT(VARCHAR,GETDATE(),112), @strCompanyCode VARCHAR(1)='A', @strDPCode VARCHAR(16),
  @strApprovedSec VARCHAR(1)='N'
  IF @i_vcXML = ''
  BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
  END 
  
  SET @XMLData = CAST('<root>'+@i_vcXML+'</root>' AS XML)
  
  SELECT @strClientCode  = ISNULL(x.value('(UserId)[1]', 'VARCHAR(500)'),''),
  @strCompanyCode  = ISNULL(x.value('(CompanyCode)[1]', 'VARCHAR(1)'),''),
  @strDPCode  = ISNULL(x.value('(DPCode)[1]', 'VARCHAR(16)'),''),
  @strApprovedSec = ISNULL(x.value('(ApprovedSecurities)[1]', 'VARCHAR(1)'),'')
  FROM @XMLData.nodes('/root') AS XTbl(x) 
  
  IF ISNULL(@strApprovedSec,'')=''
  BEGIN
    SET @strApprovedSec = 'N'
  END
  
  DECLARE @tbl_HoldingDate TABLE(HoldingDate VARCHAR(8))
  
  CREATE TABLE #tbl_HoldingRep (ClientCode VARCHAR(50), ClientName VARCHAR(100),
  BranchCode VARCHAR(50), Product VARCHAR(50), ScripCode VARCHAR(15),
  ScripName VARCHAR(100), ISIN VARCHAR(20),Qty MONEY, ClosingPrice MONEY,
  MarketValue MONEY, Haircut MONEY, NetValue MONEY)
  
  DECLARE @CrossDB VARCHAR(100)='', @CrossOwner VARCHAR(50)='', @OP_Product VARCHAR(50)=''
  
  SELECT @CrossDB = LTRIM(RTRIM(OP_DataBase)),  @CrossOwner = LTRIM(RTRIM(OP_Owner)), @OP_Product = LTRIM(RTRIM(OP_Product))
  FROM Other_Products(NOLOCK) WHERE OP_Product = 'CROSS'
  AND OP_STATUS = 'A'  
  
  IF ISNULL(@strCompanyCode,'') =''
  BEGIN
    SET @strCompanyCode = 'A'
  END
  	
  DECLARE @dp_EstroServer VARCHAR(50)= '', 
  @dp_EstroDatabase VARCHAR(100)='', @dp_EstroOwner VARCHAR(50)=''
	
  SELECT @dp_EstroServer = LTRIM(RTRIM(OP_Server)), @dp_EstroDatabase = LTRIM(RTRIM(OP_DataBase)),
  @dp_EstroOwner = LTRIM(RTRIM(OP_Owner)) FROM Other_Products(NOLOCK) 
  WHERE OP_Product = 'Estro'
  AND op_Status = 'A'
  
  
  DECLARE @defaultDPIds VARCHAR(100)='', @dpdaactno VARCHAR(20)='', @dpType VARCHAR(10)=''
  
  SELECT @defaultDPIds = sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd = 'POADPIDS'
  DECLARE Cur2dp
  CURSOR FOR SELECT da_actno, dpType = iif(substring(da_dpid,1,2)='IN','NSDL','CDSL') 
  FROM Dematact(NOLOCK) WHERE da_clientcd = @strClientCode AND da_status = 'A' 
  AND da_dpid IN(SELECT VALUE FROM dbo.returntable(@defaultDPIds,','))
  AND (CASE WHEN substring(da_dpid,1,2)='IN' THEN da_dpid+da_actno ELSE da_actno END)  = @strDPCode
  OPEN Cur2dp 
  FETCH NEXT FROM Cur2dp INTO @dpdaactno, @dpType
  WHILE @@FETCH_STATUS = 0
  BEGIN  
   
    IF @CrossDB  <> '' AND @dpdaactno <> '' AND @dpType = 'CDSL'
	BEGIN
	  SET @StrString = ' SELECT '''+@strClientCode+''' As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode, DType As Product, '
	  +' SSCD as ScripCode, ScripName , im_isin As ISIN, hld_ac_pos As Qty, 0, 0, 100, 0 '
      +' FROM (SELECT '''+@dpdaactno+''' as CmCd,  im_scripcd sscd, SS_Name As ScripName, ''DP'' Dtype, hld_ac_pos As hld_ac_pos, im_isin '
      +' FROM '+@CrossDB+'.[dbo].Holding, Isin(NOLOCK), Securities(NOLOCK)  '
      +' where hld_ac_type = ''11'' '
	  +' and hld_isin_code = im_isin  '
      +' AND ss_cd = im_scripcd '
	  +' AND hld_ac_code = '''+@dpdaactno+'''  and  im_priority = (Select min(im_priority) from ISIN(NOLOCK) Where im_scripcd = ss_cd)) A, '
	  + ' '+@CrossDB+'.dbo.client_master(NOLOCK) '
      +' WHERE cm_schedule = (select sp_sysvalue from Sysparameter(NOLOCK) where sp_parmcd = ''cmschedule'') '
      +' AND CMCD = cm_cd  and cm_active = ''01'' and FLOOR(hld_ac_pos) > 0 '
	  +' AND EXISTS(SELECT * FROM '+@CrossDB+'.DBO.Client_UCC_Details '
      +' WHERE cud_boid = '''+@dpdaactno+''' and cud_UCC = '''+@strClientCode+''' '
      +' and CAST(cud_tmid AS INT) IN( '
      +' SELECT DISTINCT CAST(clearingno AS INT) FROM( '
      +' select em_bclearingno clearingno  from Entity_master(NOLOCK) WHERE em_cd = '''+@strCompanyCode+''' '
      +' UNION ALL '
      +' select em_nclearingno clearingno  from Entity_master(NOLOCK)  WHERE em_cd = '''+@strCompanyCode+''') C1)) '
	  
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
	

	
    IF @dp_EstroServer  <> '' AND @dpdaactno <> '' AND @dpType = 'NSDL'
	BEGIN 
	  SET @StrString = @StrString + ' SELECT '''+@strClientCode+''' As ClientCode, cm_name As ClientName, cm_brboffcode As BranchCode, DType As Product, '
	    +' SSCD as ScripCode, ScripName , im_isin As ISIN, hld_ac_pos As Qty, 0, 0, 100, 0 '
        +' FROM (SELECT '''+@dpdaactno+''' AS cmcd, im_scripcd sscd, SS_Name As ScripName, ''DP'' Dtype, hld_ac_pos As hld_ac_pos, im_isin '
        +' FROM '+@dp_EstroDatabase+'.[dbo].Holding, Isin(NOLOCK), Securities(NOLOCK)   '
        +' where hld_ac_type = ''22'' '
	    +' and hld_isin_code = im_isin  '
        +' AND ss_cd = im_scripcd '
	    +' AND hld_ac_code = '''+@dpdaactno+''') A, '+@dp_EstroDatabase+'.[dbo].client_master(NOLOCK) '
        +' WHERE cm_schedule = (select sp_sysvalue from Sysparameter(NOLOCK) where sp_parmcd = ''cmschedule'') '
        +' AND CMCD = cm_cd  and cm_active = ''01'' and FLOOR(hld_ac_pos) > 0  '
	    +' AND EXISTS(SELECT * FROM '+@dp_EstroDatabase+'.DBO.Client_UCC_Details '
        +' WHERE cud_clientID = '''+@dpdaactno+''' and cud_UCC = '''+@strClientCode+''' '
        +' and CAST(cud_tmid AS INT) IN( '
        +' SELECT DISTINCT CAST(clearingno AS INT) FROM( '
        +' select em_bclearingno clearingno  from Entity_master(NOLOCK)  WHERE em_cd = '''+@strCompanyCode+''' '
        +' UNION ALL '
        +' select em_nclearingno clearingno  from Entity_master(NOLOCK)  WHERE em_cd = '''+@strCompanyCode+''') C1)) '
     
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
    FETCH NEXT FROM Cur2dp INTO  @dpdaactno, @dpType
  END
  CLOSE Cur2dp
  DEALLOCATE Cur2dp
  
  DECLARE @strPermissibleSec VARCHAR(1)='N'
  SELECT @strPermissibleSec = sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd='PRMISECURITY'
  
  IF ISNULL(@strPermissibleSec,'') ='Y'
  BEGIN
    DELETE FROM #tbl_HoldingRep FROM securities Where ScripCode = ss_cd and ss_Permscm <> 'Y'
  END
  
  IF ISNULL(@strApprovedSec,'') ='Y'
  BEGIN
    DELETE #tbl_HoldingRep 
    WHERE not exists (select MPS_scripcd from MrgPledge_Securities(NOLOCK) 
    WHERE MPS_scripcd = ScripCode and MPS_Dt = (select max(MPS_Dt) from MrgPledge_Securities(NOLOCK) Where MPS_Dt <= CONVERT(VARCHAR,GETDATE(),112)))
  END	
  
  
  UPDATE #tbl_HoldingRep set Haircut = Case When vm_exchange = 'N' Or vm_exchange = 'Z' 
  then vm_applicable_var 
  ELSE vm_margin_rate END  FROM VarMargin(NOLOCK) WHERE vm_scripcd = ScripCode and vm_Exchange = 'B'  
  AND vm_dt = (select max(vm_dt) FROM VarMargin(NOLOCK) where vm_scripcd = ScripCode and vm_exchange = 'B'  
  and vm_dt >= DATEADD(DAY,-180,CAST(@dtAsOnDate AS DATE)) 
  and vm_dt  <=  @dtAsOnDate)

  UPDATE #tbl_HoldingRep SET Haircut = Case When vm_exchange = 'N' Or vm_exchange = 'Z' then vm_applicable_var 
  ELSE vm_margin_rate END  
  FROM VarMargin(NOLOCK) WHERE vm_scripcd = ScripCode and vm_Exchange = 'N'  
  AND vm_dt =(select max(vm_dt) from VarMargin(NOLOCK) 
  WHERE vm_scripcd = ScripCode and vm_exchange = 'N'  
  and vm_dt >= DATEADD(DAY,-180,CAST(@dtAsOnDate AS DATE)) 
  and vm_dt  <=  @dtAsOnDate)
  
  --AND Haircut = 100
  
  
  -- REMOVE ALREADY PLEDGE
  
  UPDATE A SET A.QTY = ISNULL(A.QTY,0) - ISNULL(B.QTY,0)
  FROM #tbl_HoldingRep A, (SELECT Rq_Clientcd, Rq_Scripcd, QTY = SUM(Rq_Qty) 
  FROM PledgeRequest(NOLOCK) 
  WHERE Rq_Date = CONVERT(VARCHAR,GETDATE(),112) 
  AND Rq_Status3 = 'S' AND Rq_Status2 = (CASE WHEN substring(Rq_DematActNo,1,2)='IN' THEN 'W' ELSE 'S' END)  
  AND Rq_DematActNo = @strDPCode
  GROUP BY Rq_Clientcd, Rq_Scripcd) B 
  WHERE ClientCode = Rq_Clientcd 
  AND ScripCode = Rq_Scripcd
  	
  UPDATE #tbl_HoldingRep set ClosingPrice = mk_closerate 
  FROM Market_rates(NOLOCK) 
  WHERE mk_scripcd = ScripCode and mk_exchange ='B' 
  AND mk_dt = (select max(mk_dt) from Market_rates(NOLOCK) where mk_exchange = 'B' and mk_scripcd = ScripCode  
  and mk_dt >= DATEADD(DAY,-180,CAST(@dtAsOnDate AS DATE)) 
  and mk_dt  <= @dtAsOnDate )
	
  
  UPDATE #tbl_HoldingRep set ClosingPrice = mk_closerate 
  FROM Market_rates(NOLOCK) 
  WHERE mk_scripcd = ScripCode and mk_exchange ='N' 
  AND mk_dt = (select max(mk_dt) from Market_rates(NOLOCK) where mk_exchange = 'N' and mk_scripcd = ScripCode  
  and mk_dt >= DATEADD(DAY,-180,CAST(@dtAsOnDate AS DATE)) 
  and mk_dt  <= @dtAsOnDate )
  --AND ISNULL(ClosingPrice,0) = 0 
  
  SET @o_vcErrorMessage = (SELECT ClientCode, ClientName, BranchCode, ScripCode, ScripName, ISIN,  
  SUM(Qty) TotalQty, ClosingPrice, MarketValue = SUM(ROUND(Qty* ClosingPrice,2)), 
	   Haircut, NetValue = SUM(round(((Qty* ClosingPrice)*(100- Haircut))/100,2)),
	   Retain = isNull((select sum(Rq_Qty) From PledgeRequest(NOLOCK) 
	   WHERE  Rq_Date = CONVERT(VARCHAR,GETDATE(),112)  and Rq_Clientcd = ClientCode and Rq_Scripcd = ScripCode and Rq_Status3 = 'P'),0),
	   ReqValue = isNull((select sum(Rq_Qty) From PledgeRequest(NOLOCK) 
	   WHERE Rq_Date = CONVERT(VARCHAR,GETDATE(),112)  and Rq_Clientcd = ClientCode 
	   and Rq_Scripcd = ScripCode and Rq_Status3 = 'P'),0)*ClosingPrice
  FROM #tbl_HoldingRep
  GROUP BY ClientCode, ClientName, BranchCode, ScripCode, ScripName, ISIN, ClosingPrice, Haircut
  HAVING SUM(Qty) > 0
  ORDER BY ClientCode, ScripCode FOR JSON PATH)
  
  DROP TABLE #tbl_HoldingRep
  SET @o_vcErrorFlag  = 'S'
  
  --SET @o_vcErrorMessage = 'Process Completed'
  
  RETURN 1
END
GO

CREATE   PROCEDURE [stpr_CallThirdPartyAPI] @i_vcProjectname VARCHAR(50), 
@i_vcAPIVendor VARCHAR(50), @i_vcAPIName VARCHAR(50), @i_vcCallBackTag VARCHAR(1), 
@i_vcInputJson VARCHAR(MAX), @i_vcRequestid VARCHAR(MAX), @i_vcAPICallingUrl VARCHAR(500), @i_vcUserCode VARCHAR(50), 
@o_vcOutputJson NVARCHAR(MAX) OUTPUT, @strHeaderVal VARCHAR(MAX)='', @strTokenVal VARCHAR(MAX)='' 
WITH ENCRYPTION AS
BEGIN
  set @o_vcOutputJson = ''
  DECLARE @strAPIContantType VARCHAR(100)='', @RequestJSON VARCHAR(MAX)='', @strFile NVARCHAR(MAX)='',
  @strAPIURL VARCHAR(MAX) = '',  @strAPIType VARCHAR(50)='', @strAPICallingType VARCHAR(50)='',
  @strAPIAuthToken VARCHAR(MAX) = '', @strFieldsForHeader VARCHAR(MAX)='', @strCallBackUrl VARCHAR(MAX)='',
  @strFileName VARCHAR(200)='', @strFilePassword VARCHAR(50)='', @StrFileFormat VARCHAR(10)='',
  @CallingAPIURLMain VARCHAR(MAX)='', @strString NVARCHAR(MAX)='', @o_vcsegmentString NVARCHAR(MAX)='', @xmlStr XML,
  @strtradeplustempdb VARCHAR(50)=''
  
  SELECT @strAPIContantType = APIContantType, @strAPIURL = APIURL, 
  @strAPIType = APIType, @strAPICallingType = APICallingType, 
  @strAPIAuthToken = APIAuthToken, @strFieldsForHeader = FieldsForHeader, 
  @strCallBackUrl = CallBackUrl FROM tbl_VendorAPISetting(NOLOCK) 
  WHERE APIVendorName = @i_vcAPIVendor AND APINAME =  @i_vcAPIName 
  
  IF ISNULL(@strFieldsForHeader,'') = ''
  BEGIN
    SET @strFieldsForHeader = ''
  END
  
  IF @strHeaderVal <> ''
  BEGIN
    SET @strFieldsForHeader = @strHeaderVal
  END
  
  IF @strTokenVal <> ''
  BEGIN
    SET @strAPIAuthToken = @strTokenVal
  END
  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB'
  
  IF @i_vcCallBackTag = 'Y'
  BEGIN 
    SET @strAPIURL = @strCallBackUrl
	SET @strAPICallingType = 'GET'
  END
  IF @i_vcAPIVendor in('SARAL','ODIN')
  BEGIN
    SET @RequestJSON = '{"Request": ##JsonRequest##,
	    "ContentType": "##VarContentType##",
	    "ProjectName": "##ProjectName##",
		"APIVendorName":"##APIVendorName##",
	    "APIURL": "##URL##",
	    "APIType": "##APIType##",
	    "APICallingType": "##APICallingType##",
	    "AuthKey": "##APIAuthToken##",
		"OutputType":"",
		"FileType": "##FileType##",
        "File": "##FILE##",
        "FileName":"##FILENAME##",
        "FilePassword":"##FILEPASSWORD##",
        "FileFormat":"##FileFormat##",
	    "FieldsForHeader": ##FieldsForHeader##}'   
  END
  ELSE
  BEGIN
    SET @RequestJSON = '{"Request": ##JsonRequest##,
	    "ContentType": "##VarContentType##",
	    "ProjectName": "##ProjectName##",
		"APIVendorName":"##APIVendorName##",
	    "APIURL": "##URL##",
	    "APIType": "##APIType##",
	    "APICallingType": "##APICallingType##",
	    "AuthKey": "##APIAuthToken##",
		"OutputType":"Json",
		"FileType": "##FileType##",
        "File": "##FILE##",
        "FileName":"##FILENAME##",
        "FilePassword":"##FILEPASSWORD##",
        "FileFormat":"##FileFormat##",
	    "FieldsForHeader": ##FieldsForHeader##}'  
  END
  IF @strAPIContantType <> 'multipart/form-data' 
  BEGIN
    SET @RequestJSON = REPLACE(@RequestJSON,'##JsonRequest##',@i_vcInputJson)
	SET @CallingAPIURLMain = @i_vcAPICallingUrl+'/api/ThirdPartyService/RequestService'
  END
  ELSE
  BEGIN
    SET @RequestJSON = REPLACE(@RequestJSON,'##JsonRequest##','{}')
	
	SET @strString = 'EXEC '+@strtradeplustempdb+'.DBO.sp_ProcessRekyc ''' + @i_vcInputJson + ''', ''thirdPartyAPI'', @o_vcsegmentString OUTPUT ';
	
	EXEC sp_executesql @strString, N'@o_vcsegmentString NVARCHAR(MAX) OUTPUT',  @o_vcsegmentString OUTPUT;
	
	
    SET @xmlStr = CAST(@o_vcsegmentString AS xml)
    
	SELECT @strFile = c.value('(base64)[1]', 'NVARCHAR(MAX)'),
    @strFileName = c.value('(FileName)[1]', 'VARCHAR(200)'),
    @strFilePassword = c.value('(FilePassword)[1]', 'VARCHAR(50)')   	
	FROM  @xmlStr.nodes('//row') AS t(c);
	
	/*SELECT @strFile = [base64], @strFileName = [FileName], @strFilePassword = [FilePassword]  
	FROM OPENJSON(@i_vcInputJson) WITH ([base64] NVARCHAR(MAX),
	[FileName] VARCHAR(200), [FilePassword] VARCHAR(50))*/
	SET @StrFileFormat = 'BASE64'
	SET @CallingAPIURLMain = @i_vcAPICallingUrl+'/api/ThirdPartyService/UploadMultiPartImage'
  END
  --SELECT @CallingAPIURLMain

  IF CHARINDEX('<<Requestid>>',@strAPIURL) > 0
  BEGIN
    SET @strAPIURL = REPLACE(@strAPIURL,'<<Requestid>>',@i_vcRequestid)    
  END
  
  SET @RequestJSON = REPLACE(@RequestJSON,'##VarContentType##',@strAPIContantType)  
  SET @RequestJSON = REPLACE(@RequestJSON,'##ProjectName##',@i_vcProjectname)  
  SET @RequestJSON = REPLACE(@RequestJSON,'##APIVendorName##',@i_vcAPIVendor+'-'+@i_vcAPIName)
  SET @RequestJSON = REPLACE(@RequestJSON,'##URL##',@strAPIURL)  
  SET @RequestJSON = REPLACE(@RequestJSON,'##APIType##',@strAPIType)  
  SET @RequestJSON = REPLACE(@RequestJSON,'##APICallingType##',@strAPICallingType)
  SET @RequestJSON = REPLACE(@RequestJSON,'##APIAuthToken##',@strAPIAuthToken)  
  IF @i_vcAPIName = 'detectliveness'
  BEGIN
    SET @RequestJSON = REPLACE(@RequestJSON,'##FileType##','videoimg')
  END
  ELSE
  BEGIN
    SET @RequestJSON = REPLACE(@RequestJSON,'##FileType##','PDF')
  END  
  
  SET @RequestJSON = REPLACE(@RequestJSON,'##FILE##',ISNULL(@strFile,''))
  SET @RequestJSON = REPLACE(@RequestJSON,'##FILENAME##',ISNULL(@strFileName,''))  
  SET @RequestJSON = REPLACE(@RequestJSON,'##FILEPASSWORD##',ISNULL(@strFilePassword,'')) 
  SET @RequestJSON = REPLACE(@RequestJSON,'##FileFormat##',ISNULL(@StrFileFormat,'')) 
  IF @strFieldsForHeader <> ''
  BEGIN
  SET @RequestJSON = REPLACE(@RequestJSON,'##FieldsForHeader##',@strFieldsForHeader) 
  END
  ELSE   
  BEGIN
    SET @RequestJSON = REPLACE(@RequestJSON,'##FieldsForHeader##','[]') 
  END
  
  DECLARE @Object AS INT;  
  DECLARE @ResponseText AS VARCHAR(8000)='';  
  Declare @tbl_OutputResponse as table(Json_Table nvarchar(max))
  DECLARE @VCOUTPUT VARCHAR(MAX)=''  
  EXEC sp_OACreate 'MSXML2.XMLHTTP', @Object OUT;  

  EXEC sp_OAMethod @Object, 'open', NULL, 'post',@CallingAPIURLMain, 'false' 
  EXEC sp_OAMethod @Object, 'setRequestHeader', null, 'Content-Type', 'application/json'  
  EXEC sp_OAMethod @Object, 'send', null, @RequestJSON  
  INSERT INTO @tbl_OutputResponse (Json_Table) EXEC sp_OAMethod @Object, 'responseText'
  SELECT @VCOUTPUT = Json_Table FROM @tbl_OutputResponse
  SET @o_vcOutputJson = @VCOUTPUT  
  --SELECT @VCOUTPUT
  EXEC sp_OADestroy @Object  
  
  RETURN 1
END
GO

CREATE PROCEDURE [dbo].[stpr_GetCDSLMarginEncryDecry] @i_vcInputJson NVARCHAR(MAX), @i_vcType VARCHAR(50), 
@CallingAPIURLMain VARCHAR(MAX), @o_vcOutputJson NVARCHAR(MAX) OUTPUT 
WITH ENCRYPTION
AS
BEGIN

  DECLARE @i_vcInputJson1 NVARCHAR(MAX)=''
  SET @o_vcOutputJson = ''
  
  
  IF @i_vcType = 'Encrypt'
  BEGIN
    SET @CallingAPIURLMain = @CallingAPIURLMain+'/api/ThirdPartyService/EncryptCDSL'
  END
  ELSE IF @i_vcType = 'Decrypt'
  BEGIN
    SET @CallingAPIURLMain = @CallingAPIURLMain+'/api/ThirdPartyService/DecryptCDSL'
  END
 
  DECLARE @Object AS INT;  
  DECLARE @ResponseText AS VARCHAR(8000)='';  
  DECLARE @tbl_OutputResponse as table(Json_Table nvarchar(max))
  DECLARE @VCOUTPUT VARCHAR(MAX)=''  
  EXEC sp_OACreate 'MSXML2.XMLHTTP', @Object OUT;  
  
  EXEC sp_OAMethod @Object, 'open', NULL, 'post',@CallingAPIURLMain, 'false' 
  EXEC sp_OAMethod @Object, 'setRequestHeader', null, 'Content-Type', 'application/json'  
  EXEC sp_OAMethod @Object, 'send', null, @i_vcInputJson  
  INSERT INTO @tbl_OutputResponse (Json_Table) EXEC sp_OAMethod @Object, 'responseText'
  SELECT @VCOUTPUT = Json_Table FROM @tbl_OutputResponse
  SET @o_vcOutputJson = @VCOUTPUT  
  EXEC sp_OADestroy @Object  
  RETURN 
END
GO

CREATE PROCEDURE stpr_APICDSLINQUERYCALL @strRequestid VARCHAR(MAX), @strClientCode VARCHAR(20), @o_vcOutPutJSON VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN

  --- INQUERY CALL
  
  DECLARE @tbl_jsonoutput TABLE(SerialNo INT, ColumnName VARCHAR(50), ColumnValue VARCHAR(MAX), 
  MasterTag VARCHAR(100), JSONLEVEL INT, MASTERLEVEL INT)
  
  DECLARE @strtradeplustempdb VARCHAR(30)='', @string NVARCHAR(MAX), @strbrokerOrderNo VARCHAR(20)='',
  @strUccCode VARCHAR(20)='', @iJsonLevel INT = 0, @strStatus VARCHAR(20)='', @strReqid VARCHAR(20)='',
  @strISIN VARCHAR(50)='', @strReqid1 VARCHAR(50) = ''

  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB'
  DECLARE @strThirdPartyURL VARCHAR(MAX)='', @o_vcOutputJsonapi NVARCHAR(MAX)='', @encrypttransdtl NVARCHAR(MAX)=''
  SELECT @strThirdPartyURL = sp_sysvalue 
  FROM Sysparameter(NOLOCK) WHERE sp_parmcd = 'ThirdParty'
  
  DECLARE @strAPIRequest VARCHAR(MAX)='', 
  @strHeder VARCHAR(MAX)='', @strRequest VARCHAR(MAX)='', @strEnDnRequest NVARCHAR(MAX)='',
  @strCRandomNoFinal VARCHAR(24)='', @strDPID VARCHAR(10)='', @strASEKEY nvarchar(100)=''
  
  DECLARE @jsonCutterOutput VARCHAR(MAX)='', @strDanSql NVARCHAR(MAX)='', @strpsnNO VARCHAR(50)=''
  
  SELECT @strAPIRequest = RequestJson 
  FROM DBO.tbl_VendorAPISetting(NOLOCK) 
  WHERE APIVendorName = 'CDSL' AND APINAME = 'PLEDGEENQ'
  --SET @strHeder = '[{"dpid":"##dpid##"},{"reqid":"##reqid##"},{"version":"1.0"}]'
  SET @strHeder ='[{"Key":"dpid","Value":"##dpid##"}, {"Key":"reqid","Value":"##reqid##"}, {"Key":"version","Value":"1.0"}]'
  
  
  DECLARE @PledgeFormNo VARCHAR(16)='', @PledgeIntRefNo VARCHAR(16)='', 
  @ISINReqId VARCHAR(24)='', @ISINResId VARCHAR(24)='', @strRefNo VARCHAR(20)='',
  @curClientCode VARCHAR(20)=''
  
  SET @strDPID = ISNULL((SELECT VALUE FROM(SELECT *
  FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 1), '')
  
  SET @strASEKEY = ISNULL((SELECT VALUE FROM(SELECT *
  FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 2), '')
  
  DECLARE @JsonCutterXML XML
  
  IF @strASEKEY = ''
  BEGIN
    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":"##DATA##"}'                             
    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','AESKey Not Found In Api Setting')  
	SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##DATA##','[]')  
    RETURN 1 	
  END
  
  DECLARE CURSOR_inq_CDSL CURSOR FOR      
  SELECT DISTINCT RefNo = CDSLReqNo, ClientCode
  FROM TBL_ClientMarginPledgeDtl(NOLOCK)
  WHERE RequestDate = CONVERT(VARCHAR,GETDATE(),112)
  AND ISNULL(PSNNo,'') = ''
  AND ((ClientCode = @strClientCode and @strClientCode <> '') OR @strClientCode = '')
  AND ((CDSLReqNo = @strRequestid AND @strRequestid <> '') OR @strRequestid = '')
  AND ISNULL(ResponseCode,'') = 'S'
  
  
  OPEN CURSOR_inq_CDSL       
  FETCH NEXT FROM CURSOR_inq_CDSL INTO @strRefNo, @curClientCode
  WHILE @@FETCH_STATUS = 0       
  BEGIN  
 
    SET @strHeder = REPLACE(@strHeder,'##dpid##', SUBSTRING(@strDPID,4,5))
    SET @strHeder = REPLACE(@strHeder,'##reqid##', @strRefNo )
    SET @strRequest =  '{"reqtype":"MP","reqtime":"##reqtime##"}'
    SET @strRequest = REPLACE(@strRequest,'##reqtime##',CAST(FORMAT(GETDATE(), 'ddMMyyyyHHmmss') AS VARCHAR))
	
	--- ADD ENCRYPATION
	
	SET @strEnDnRequest = '{"aesKey":"##aesKey##","data":##data##}'
	SET @strEnDnRequest = REPLACE(@strEnDnRequest,'##aesKey##',@strASEKEY)
	SET @strEnDnRequest = REPLACE(@strEnDnRequest,'##data##',@strRequest)
	SELECT @strEnDnRequest
	EXEC [dbo].[stpr_GetCDSLMarginEncryDecry] @strEnDnRequest, 'Encrypt', @strThirdPartyURL, @o_vcOutputJsonapi OUTPUT
	SELECT @o_vcOutputJsonapi
	DELETE FROM @tbl_jsonoutput
	BEGIN TRY
		
        SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
        INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue, MasterTag, JsonLevel, MasterLevel)
		SELECT X1.* FROM(
        SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
	    JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
        JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
		JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
	    JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
	    JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
        FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1
	END TRY

	BEGIN CATCH
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":"##DATA##"}'                             
       SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE())  
	   SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##DATA##',@o_vcOutputJsonapi)  
	   CLOSE CURSOR_inq_CDSL       
       DEALLOCATE CURSOR_inq_CDSL
       RETURN 1 
	END CATCH

	IF EXISTS (SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'Data' AND ColumnValue <> '')
	BEGIN
	  SELECT @encrypttransdtl = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'Data'
	  SET @strRequest = '{"encrypttransdtl":"##encrypttransdtl##"}'
	  SET @strRequest = REPLACE(@strRequest,'##encrypttransdtl##',@encrypttransdtl)
	
      EXEC stpr_CallThirdPartyAPI 'CDSL-ENQ','CDSL', 'PLEDGEENQ', '',@strRequest,'',@strThirdPartyURL, 
	  'API', @o_vcOutputJsonapi OUTPUT, @strHeder	
	  
	  SET @strEnDnRequest = '{"aesKey":"##aesKey##","data":"##data##"}'
	  SET @strEnDnRequest = REPLACE(@strEnDnRequest,'##aesKey##',@strASEKEY)
	  SET @strEnDnRequest = REPLACE(@strEnDnRequest,'##data##',@o_vcOutputJsonapi)
	  
	  EXEC [dbo].[stpr_GetCDSLMarginEncryDecry] @strEnDnRequest, 'Decrypt', @strThirdPartyURL, @o_vcOutputJsonapi OUTPUT
	  SET @o_vcOutputJsonapi = REPLACE(@o_vcOutputJsonapi,'\"','"')
	  SET @o_vcOutputJsonapi = REPLACE(@o_vcOutputJsonapi,'}]}}"}','}]}}}')
	  SET @o_vcOutputJsonapi = REPLACE(@o_vcOutputJsonapi,'{"data":"{','{"data":{')
	  
	
	  DELETE FROM @tbl_jsonoutput
	  BEGIN TRY
        SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
		
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        SET @JsonCutterXML= CAST(@jsonCutterOutput AS XML)
        INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue, MasterTag, JsonLevel, MasterLevel)
		SELECT X1.* FROM(
        SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
	    JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
        JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
		JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
	    JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
	    JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
        FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1
	  END TRY

	  BEGIN CATCH
	    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":"##DATA##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE())  
	    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##DATA##',@o_vcOutputJsonapi)  
		CLOSE CURSOR_inq_CDSL       
        DEALLOCATE CURSOR_inq_CDSL
        RETURN 1 
	  END CATCH
	  
      IF EXISTS (SELECT 1 FROM @tbl_jsonoutput)
	  BEGIN
       /* DELETE FROM @tbl_jsonoutput
	    SELECT @encrypttransdtl = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'Data'
	    BEGIN TRY
         SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@encrypttransdtl+''' , @jsonCutterOutput OUTPUT';
		
         EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
         SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
         INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue, MasterTag, JsonLevel, MasterLevel)
		 SELECT X1.* FROM(
         SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
	     JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
         JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
		 JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
	     JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
	     JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
         FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1
	    END TRY

	    BEGIN CATCH
	     SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":"##DATA##"}'                             
         SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE())  
	     SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##DATA##',@encrypttransdtl)  
		 CLOSE CURSOR_inq_CDSL       
         DEALLOCATE CURSOR_inq_CDSL
         RETURN 1 
	    END CATCH
	  */
	  
	    SELECT @strbrokerOrderNo = ColumnValue FROM @tbl_jsonoutput 
	    WHERE MasterTag = 'pledgeresdtls' and ColumnName = 'reqid'
	
	    SELECT @strUccCode = ColumnValue 
	    FROM @tbl_jsonoutput WHERE ColumnName = 'uccid' AND MasterTag='pledgeresdtls'
	
	    IF (@strbrokerOrderNo <> @strRefNo) OR (@strUccCode <> @curClientCode)
	    BEGIN
	      SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":""}'                             
          SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Not Found '+@strbrokerOrderNo+'/'+@strUccCode)  
	      SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY('[]'))
	      CLOSE CURSOR_inq_CDSL       
          DEALLOCATE CURSOR_inq_CDSL
          RETURN 1 
	    END
	
	    IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'reqstatus' 
	    AND MasterTag='pledgeresdtls' AND ColumnValue = 'Completed')
	    BEGIN
	      DECLARE CURSOR_CDSLINQ CURSOR FOR      
          SELECT DISTINCT JSONLEVEL  
          FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls'
       
	      OPEN CURSOR_CDSLINQ       
          FETCH NEXT FROM CURSOR_CDSLINQ INTO @iJsonLevel      
          WHILE @@FETCH_STATUS = 0       
          BEGIN  
	        SELECT @strStatus = ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	        AND JSONLEVEL = @iJsonLevel AND ColumnName = 'reqstatus'
	        SET @strpsnNO = ''
		    SELECT @strpsnNO = ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'psn'
		 
		    SELECT @strReqid = ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	        AND JSONLEVEL = @iJsonLevel AND ColumnName = 'prfnumber'
 		
		    SELECT @strISIN = ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	        AND JSONLEVEL = @iJsonLevel AND ColumnName = 'isin'
		
		    SELECT @strReqid1 = ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	        AND JSONLEVEL = @iJsonLevel AND ColumnName = 'isinreqid'
		
	        IF ISNULL(@strStatus,'') <> '0'
		    BEGIN
              UPDATE A SET A.Rq_Note = SUBSTRING(ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'errorcode'),'')+'|'+
							   ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'errormsg'),''),1,50)
	          FROM PledgeRequest A 
		      WHERE Rq_Date = CONVERT(VARCHAR,GETDATE(),112)
		      AND Rq_Clientcd = @strUccCode
		      AND Rq_Status1 = 'S'
		      AND Rq_Status2 = 'P'
	          AND Rq_IpAddress LIKE '%|'+@strbrokerOrderNo+'|'+@strISIN 
		      AND Rq_Clientcd+'|'+Rq_Scripcd = @strReqid1
		      AND SUBSTRING(Rq_DematActNo,1,2) <> 'IN'
		   
		      UPDATE A SET A.ISINResid = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'isinresid'),''),
		      A.ResponseCode = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'errorcode'),''),
              A.ResponseMessage = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'errormsg'),''), UpdateTimeStamp = GETDATE() 	 							   
		      FROM TBL_ClientMarginPledgeDtl A
		      WHERE ClientCode = @strUccCode
		      AND RequestDate = CONVERT(VARCHAR,GETDATE(),112)
		      AND CDSLReqNo = @strbrokerOrderNo
		      AND ISINReqId = @strReqid1
		      AND ISNULL(ResponseCode,'') = 'S'
		      AND ISIN = @strISIN
		  
	        END
	        ELSE IF ISNULL(@strStatus,'') = '0' AND ISNULL(@strpsnNO,'') <> ''
	        BEGIN
	          UPDATE A SET A.Rq_Status2 = 'S', A.Rq_Note =  SUBSTRING(ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'psn'),'')+'|'+
							   ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'psnstatus'),'')+'|'+
							   ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'parentpsn'),'')
							   ,1,50)
	          FROM PledgeRequest A 
	          WHERE Rq_Date = CONVERT(VARCHAR,GETDATE(),112)
		      AND Rq_Clientcd = @strUccCode
		      AND Rq_Status1 = 'S'
		      AND Rq_Status2 = 'P'
	          AND Rq_IpAddress LIKE '%|'+@strbrokerOrderNo+'|'+@strISIN 
		      AND Rq_Clientcd+'|'+Rq_Scripcd = @strReqid1
		      AND SUBSTRING(Rq_DematActNo,1,2) <> 'IN'
		  
		      UPDATE A SET A.ISINResid = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'isinresid'),''),
              A.PSNNo = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'psn'),''),
              A.PSNStatus = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'psnstatus'),''),
              A.ParentPSNNO = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'parentpsn'),''),							    
		      A.ResponseCode = 'X', A.ResponseMessage = '', UpdateTimeStamp = GETDATE() 	  							   
		      FROM TBL_ClientMarginPledgeDtl A
		      WHERE ClientCode = @strUccCode
		      AND RequestDate = CONVERT(VARCHAR,GETDATE(),112)
		      AND CDSLReqNo = @strbrokerOrderNo
		      AND ISINReqId = @strReqid1
		      AND ISNULL(ResponseCode,'') = 'S'
		      AND ISIN = @strISIN
		  
	        END
            FETCH NEXT FROM CURSOR_CDSLINQ INTO @iJsonLevel      
          END       
          CLOSE CURSOR_CDSLINQ       
          DEALLOCATE CURSOR_CDSLINQ 
		END
	  END	
	  ELSE
	  BEGIN
	    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":"##DATA##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Issue in Decrypt')  
	    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##DATA##',@o_vcOutputJsonapi)  
		CLOSE CURSOR_inq_CDSL       
        DEALLOCATE CURSOR_inq_CDSL 
        RETURN 1 
	  END	
	END
	ELSE
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":"##DATA##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Issue in Encrypt')  
	  SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##DATA##',@o_vcOutputJsonapi)  
	  CLOSE CURSOR_inq_CDSL       
      DEALLOCATE CURSOR_inq_CDSL 
      RETURN 1 
	END	
	FETCH NEXT FROM CURSOR_inq_CDSL INTO @strRefNo, @curClientCode
  END 
  CLOSE CURSOR_inq_CDSL       
  DEALLOCATE CURSOR_inq_CDSL   
  SET @o_vcOutPutJSON ='{"ResponseFlag":"S","ResponseMessage":"##ErrorMessage##","DATA":""}'                             
  SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Process Executed')  
  --SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY('[]'))
  RETURN 1 	
END
GO

CREATE PROCEDURE stpr_APICDSLRepledgeCALL @strRequestid VARCHAR(MAX), @strClientCode VARCHAR(20), 
@o_vcOutPutJSON VARCHAR(MAX) OUTPUT 
WITH ENCRYPTION AS
BEGIN
  --- REPLEDGE CALL

  DECLARE @strASEKEY NVARCHAR(MAX)=''
  
  DECLARE @tbl_jsonoutput TABLE(SerialNo INT, ColumnName VARCHAR(50), ColumnValue VARCHAR(MAX), 
  MasterTag VARCHAR(100), JSONLEVEL INT, MASTERLEVEL INT)
  
  DECLARE @strtradeplustempdb VARCHAR(30)='', @string NVARCHAR(MAX), @strbrokerOrderNo VARCHAR(20)='',
  @strUccCode VARCHAR(20)='', @iJsonLevel INT = 0, @strStatus VARCHAR(20)='', @strReqid VARCHAR(20)='',
  @strISIN VARCHAR(50)='', @strReqid1 VARCHAR(50) = '', @strEnDnRequest VARCHAR(MAX)='', 
  @encrypttransdtl VARCHAR(MAX)=''

  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB'
  DECLARE @strThirdPartyURL VARCHAR(MAX)='', @o_vcOutputJsonapi VARCHAR(MAX)=''
  SELECT @strThirdPartyURL = sp_sysvalue 
  FROM Sysparameter(NOLOCK) WHERE sp_parmcd = 'ThirdParty'
  
  DECLARE @strAPIRequest VARCHAR(500)='', 
  @strHeder VARCHAR(MAX)='', @strRequest VARCHAR(MAX)='', 
  @strCRandomNoFinal VARCHAR(24)='', @strjsondata VARCHAR(MAX)='', @strAgrmntNo VARCHAR(20)=''
  
  DECLARE @jsonCutterOutput VARCHAR(MAX)='', @strDanSql NVARCHAR(MAX)=''
  
  SELECT @strAPIRequest = RequestJson 
  FROM DBO.tbl_VendorAPISetting(NOLOCK) WHERE APIVendorName = 'CDSL' AND APINAME = 'MARGINREPLEDGE'
  --SET @strHeder = '[{"dpid":"##dpid##"},{"reqid":"##reqid##"}]'
  SET @strHeder ='[{"Key":"dpid","Value":"##dpid##"}, {"Key":"reqid","Value":"##reqid##"}]'
  
  DECLARE @JsonCutterXML XML
  
  DECLARE @strdpid VARCHAR(10)='', @strReasonCode VARCHAR(20)='', @strCMID VARCHAR(20)=''
  
  SET @strdpid = ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 1), '')
  
  SET @strReasonCode = ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 2), '')
				
  SET @strCMID = ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 3), '')
   
  SET @strAgrmntNo = ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 4), '')
				
  SET @strASEKEY = ISNULL((SELECT VALUE FROM(SELECT *
  FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 5), '')
	
  DECLARE @idRepledge VARCHAR(20)= CAST(FORMAT(GETDATE(), 'ddMMyyhhmmss') AS VARCHAR)+RIGHT(REPLICATE('0', '4') + CAST(ABS(CHECKSUM(NEWID())) % 10000 AS VARCHAR), '4')	
	
  DECLARE @PledgeFormNo VARCHAR(16)='', @PledgeIntRefNo VARCHAR(16)='', 
  @ISINReqId VARCHAR(24)='', @ISINResId VARCHAR(24)='', @strRefNo VARCHAR(20)='',
  @curClientCode VARCHAR(20)=''
  
  DECLARE CURSOR_inq_CDSL CURSOR FOR      
  SELECT DISTINCT RefNo = CDSLReqNo, ClientCode
  FROM TBL_ClientMarginPledgeDtl(NOLOCK)
  WHERE RequestDate = CONVERT(VARCHAR,GETDATE(),112)
  AND ISNULL(PSNNo,'') <> ''
  AND ((ClientCode = @strClientCode and @strClientCode <> '') OR @strClientCode = '')
  AND ((CDSLReqNo = @strRequestid AND @strRequestid <> '') OR @strRequestid = '')
  AND ISNULL(ResponseCode,'') = 'X'
    
  OPEN CURSOR_inq_CDSL       
  FETCH NEXT FROM CURSOR_inq_CDSL INTO @strRefNo, @curClientCode
  WHILE @@FETCH_STATUS = 0       
  BEGIN  
    SET @strHeder = REPLACE(@strHeder,'##dpid##',SUBSTRING(@strdpid,4,5))
    SET @strHeder = REPLACE(@strHeder,'##reqid##',@idRepledge)	

	SET @strRequest = '{"dpid": "##dpid##","reqdatetime": "##reqtime##","setupdtls": [##DATA##]}'
    SET @strRequest = REPLACE(@strRequest,'##reqtime##',CAST(FORMAT(GETDATE(), 'ddMMyyyyHHmmss') AS VARCHAR))
	SET @strRequest = REPLACE(@strRequest,'##dpid##',ISNULL(SUBSTRING(@strdpid,3,6),''))
    
    UPDATE A SET A.pledgeeboreqid = CAST(FORMAT(GETDATE(), 'ddMMyyhhmmss') AS VARCHAR)+RIGHT(REPLICATE('0', '4') + CAST(ABS(CHECKSUM(NEWID())) % 10000 AS VARCHAR), '4'),
	A.pldgrdpintrefno = CAST(FORMAT(GETDATE(), 'ddMMyyhhmmss') AS VARCHAR)+RIGHT(REPLICATE('0', '4') + CAST(ABS(CHECKSUM(NEWID())) % 10000 AS VARCHAR), '4'),
	A.AgrmntNo = @strAgrmntNo, RepledgeReqid  = @idRepledge
	FROM TBL_ClientMarginPledgeDtl A
	WHERE RequestDate = CONVERT(VARCHAR,GETDATE(),112)
	  AND ClientCode = @curClientCode
	  AND CDSLReqNo = @strRefNo
	  AND ISNULL(ResponseCode,'') = 'X'
	  AND ISNULL(PSNNo,'') <> ''
	  --AND A.pledgeeboreqid = ''
	
	  
	SET @strjsondata = (SELECT  pledgeeboreqid, [pledgeeboid] = @strdpid, [pledgeseqno] = PSNNo, [prfno] = PledgeFormNo, [pledgetype] = 'MP',
	                    [pledgequantity] = CAST(QTY AS INT), [pledgeexpdate] = CAST(FORMAT(GETDATE(), 'ddMMyyyy') AS VARCHAR),
	                    [pledgevalue] = CASE WHEN ISNULL(CAST(StockValue AS INT),0) = 0 THEN  CAST(QTY AS INT) ELSE ISNULL(CAST(StockValue AS INT),0) END, 
						agrmntno, pldgrdpintrefno, reasoncode = @strReasonCode, cmid = @strCMID
	                    FROM TBL_ClientMarginPledgeDtl(NOLOCK) 
	                    WHERE RequestDate = CONVERT(VARCHAR,GETDATE(),112)
	                    AND ClientCode = @curClientCode
	                    AND CDSLReqNo = @strRefNo
					    AND ISNULL(ResponseCode,'') = 'X'
						AND ISNULL(PSNNo,'') <> ''  FOR JSON PATH)
	
	SELECT @strJsonData = SUBSTRING(@strJsonData, 2, LEN(@strJsonData))
    SELECT @strJsonData = SUBSTRING(@strJsonData, 1, LEN(@strJsonData) - 1)
	SET @strRequest = REPLACE(@strRequest,'##DATA##',ISNULL(@strjsondata,''))  

	--- ADD ENCRYPATION
	
    SET @strEnDnRequest = '{"aesKey":"##aesKey##","data":##data##}'
	SET @strEnDnRequest = REPLACE(@strEnDnRequest,'##aesKey##',@strASEKEY)
	SET @strEnDnRequest = REPLACE(@strEnDnRequest,'##data##',@strRequest)
	EXEC [dbo].[stpr_GetCDSLMarginEncryDecry] @strEnDnRequest, 'Encrypt', @strThirdPartyURL, @o_vcOutputJsonapi OUTPUT
	
	DELETE FROM @tbl_jsonoutput
	BEGIN TRY
		
        SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
        INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue, MasterTag, JsonLevel, MasterLevel)
		SELECT X1.* FROM(
        SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
	    JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
        JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
		JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
	    JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
	    JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
        FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1
	END TRY

	BEGIN CATCH
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":"##DATA##"}'                             
       SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE())  
	   SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##DATA##',@o_vcOutputJsonapi)  
       RETURN 1 
	END CATCH

	IF EXISTS (SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'Data' AND ColumnValue <> '')
	BEGIN
	  SELECT @encrypttransdtl = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'Data'
	  SET @strRequest = '{"mrgnrepldgdtls":"##encrypttransdtl##"}'
	  SET @strRequest = REPLACE(@strRequest,'##encrypttransdtl##',@encrypttransdtl)
	  
      EXEC stpr_CallThirdPartyAPI 'CDSL-REPLEDGE','CDSL', 'MARGINREPLEDGE', '',@strRequest,'',@strThirdPartyURL, 
	  'API', @o_vcOutputJsonapi OUTPUT, @strHeder
	  
	  IF CHARINDEX('Error_Code', @o_vcOutputJsonapi) >0
      BEGIN
	    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":##DATA##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','CDSL-REPLEDGE')  
	    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##DATA##',@o_vcOutputJsonapi)  
        RETURN 1 
      END 	  
	  
    --- ADD DECRIPTION
	
	  DELETE FROM @tbl_jsonoutput
	  
	  
      SET @strEnDnRequest = '{"aesKey":"##aesKey##","data":"##data##"}'
	  SET @strEnDnRequest = REPLACE(@strEnDnRequest,'##aesKey##',@strASEKEY)
	  SET @strEnDnRequest = REPLACE(@strEnDnRequest,'##data##',@o_vcOutputJsonapi)
	
	  EXEC [dbo].[stpr_GetCDSLMarginEncryDecry] @strEnDnRequest, 'Decrypt', @strThirdPartyURL, @o_vcOutputJsonapi OUTPUT
	  
	  SET @o_vcOutputJsonapi = REPLACE(@o_vcOutputJsonapi,'\"','"')
	  SET @o_vcOutputJsonapi = REPLACE(@o_vcOutputJsonapi,'}]}"}','}]}}')
	  SET @o_vcOutputJsonapi = REPLACE(@o_vcOutputJsonapi,'{"data":"{','{"data":{')
      
	  DELETE FROM @tbl_jsonoutput
	  BEGIN TRY
        SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
		
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
        INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue, MasterTag, JsonLevel, MasterLevel)
		SELECT X1.* FROM(
        SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
	    JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
        JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
		JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
	    JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
	    JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
        FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1
	  END TRY

	  BEGIN CATCH
	    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":"##DATA##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE())  
	    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##DATA##',@o_vcOutputJsonapi)  
        RETURN 1 
	  END CATCH
	  
      IF EXISTS (SELECT 1 FROM @tbl_jsonoutput)
	  BEGIN
        SELECT @strbrokerOrderNo = ColumnValue FROM @tbl_jsonoutput 
	    WHERE ColumnName = 'reqid'
	
	    IF @strbrokerOrderNo <> @idRepledge
	    BEGIN
	      SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":""}'                             
          SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Not Found '+@strbrokerOrderNo)  
	      SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY('[]'))
	      CLOSE CURSOR_inq_CDSL       
          DEALLOCATE CURSOR_inq_CDSL
          RETURN 1 
	    END
	   
	    IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'resstatus' 
	    AND ColumnValue = '0')
	    BEGIN
	      DECLARE CURSOR_CDSLINQ CURSOR FOR      
          SELECT DISTINCT JSONLEVEL  
          FROM @tbl_jsonoutput WHERE MasterTag = 'resdtls'
      
	      OPEN CURSOR_CDSLINQ       
          FETCH NEXT FROM CURSOR_CDSLINQ INTO @iJsonLevel      
          WHILE @@FETCH_STATUS = 0       
          BEGIN  
	        SELECT @strStatus = ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'resdtls' 
	        AND JSONLEVEL = @iJsonLevel AND ColumnName = 'boreqstatus'
	    
		    SELECT @strReqid = ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'resdtls' 
	        AND JSONLEVEL = @iJsonLevel AND ColumnName = 'prfno'
		
		    SELECT @strReqid1 = ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'resdtls' 
	        AND JSONLEVEL = @iJsonLevel AND ColumnName = 'pledgeeboreqid'
			
	        IF ISNULL(@strStatus,'') <> '0'
	        BEGIN
		      UPDATE  A SET A.ResponseCode = 'R' , A.ResponseMessage = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'resdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'reserror'),'')+'|'+
							   ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'resdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'reserrmsg'),'')
		      FROM TBL_ClientMarginPledgeDtl A
	          WHERE RequestDate = CONVERT(VARCHAR,GETDATE(),112)
	          AND ClientCode = @curClientCode
	          AND CDSLReqNo = @strRefNo
	          AND ISNULL(ResponseCode,'') = 'X'
	          AND pledgeeboreqid  = @strReqid1
		      AND PledgeFormNo = @strReqid
			  AND RepledgeReqid = @idRepledge
		  
	        END
		
	        ELSE IF ISNULL(@strStatus,'') = '0'
	        BEGIN
		      UPDATE  A SET A.ResponseCode = 'S' , A.ResponseMessage = 'Repledge Confirmed by NSDL'
		      FROM TBL_ClientMarginPledgeDtl A
	          WHERE RequestDate = CONVERT(VARCHAR,GETDATE(),112)
	          AND ClientCode = @curClientCode
	          AND CDSLReqNo = @strRefNo
	          AND ISNULL(ResponseCode,'') = 'X'
	          AND pledgeeboreqid  = @strReqid1
		      AND PledgeFormNo = @strReqid
			  AND RepledgeReqid = @idRepledge
		    END
		
		    UPDATE A SET A.Rq_Status2 = B.ResponseCode, A.Rq_Note = SUBSTRING(B.ResponseMessage,1,50)
		    FROM PledgeRequest A, TBL_ClientMarginPledgeDtl B
		    WHERE A.Rq_Date = B.RequestDate
		    AND A.Rq_Clientcd = B.ClientCode
		    AND A.Rq_Scripcd = B.ScripCode
            AND ClientCode = @curClientCode
		    AND Rq_IpAddress LIKE '%|'+@strRefNo+'|%'
	        AND CDSLReqNo = @strRefNo
	        AND pledgeeboreqid  = @strReqid1
		    AND PledgeFormNo = @strReqid
		    AND Rq_Status1 = 'S'
		    AND Rq_Status2 = 'P' 
		    AND RepledgeReqid = @idRepledge
            FETCH NEXT FROM CURSOR_CDSLINQ INTO @iJsonLevel      
          END       
          CLOSE CURSOR_CDSLINQ       
          DEALLOCATE CURSOR_CDSLINQ  
	    END
	  END
	  ELSE
	  BEGIN
	    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":##DATA##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Issue in Decrypt')  
	    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##DATA##',@o_vcOutputJsonapi)  
		CLOSE CURSOR_CDSLINQ       
        DEALLOCATE CURSOR_CDSLINQ  
        RETURN 1 
	  END
    END	
	ELSE
    BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":##DATA##}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Issue in Decrypt')  
	  SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##DATA##',@o_vcOutputJsonapi)  
	  CLOSE CURSOR_CDSLINQ       
      DEALLOCATE CURSOR_CDSLINQ  
      RETURN 1 
	END	
	FETCH NEXT FROM CURSOR_inq_CDSL INTO @strRefNo, @curClientCode
  END       
  CLOSE CURSOR_inq_CDSL       
  DEALLOCATE CURSOR_inq_CDSL
  SET @o_vcOutPutJSON ='{"ResponseFlag":"S","ResponseMessage":"##ErrorMessage##","DATA":""}'                             
  SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Process Executed')  
  --SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY('[]'))
  RETURN 1 	
END
GO

CREATE PROCEDURE [dbo].[stpr_APIMARGINPLEDGE]
  @i_vcProjectName VARCHAR(50), @i_vcModuleName VARCHAR(100),                                   
  @i_vcFunctionName VARCHAR(100), @i_vcSource VARCHAR(1),                                 
  @i_vcUniqueID VARCHAR(50), @i_vcUserID VARCHAR(50),                                   
  @i_vcInputJSON NVARCHAR(MAX) , @o_vcOutPutJSON NVARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN
  DECLARE @iCount INT = 0, @strtradeplustempdb varchar(50)='',
  @o_vcErrorFlag VARCHAR(1)='', @o_vcErrorMessage VARCHAR(MAX)='', @string VARCHAR(MAX)='', 
  @strDPAccountNo VARCHAR(16)='', @strISIN VARCHAR(20)=''
  
  DECLARE @strUccCode VARCHAR(50)='', @strbrokerOrderNo VARCHAR(16)=''
  
  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB'
  
  DECLARE @tbl_jsonoutput TABLE(SerialNo INT, ColumnName VARCHAR(50), ColumnValue VARCHAR(MAX), 
  MasterTag VARCHAR(100), JSONLEVEL INT, MASTERLEVEL INT)
  
  DECLARE @strClientCode VARCHAR(50)=''

  DECLARE @tbl_InputJSONTable TABLE (SerialNo INT, ColumnName VARCHAR(100), ColumnValue NVARCHAR(MAX),                             
   ValueTypeColumn INT, ImageFlag VARCHAR(1)) 
  
  SELECT @iCount = ISNULL(COUNT(*),0) FROM tbl_GenericAPIDefinition(NOLOCK)                                 
  WHERE ProjectName = @i_vcProjectName AND ModuleName = @i_vcModuleName                             
  AND FunctionName = @i_vcFunctionName                                
  
  IF @iCount <> 1                            
  BEGIN                            
    SET @o_vcOutPutJSON ='[{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}]'                                  
    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Invalid Request Definition')                                  
    RETURN 1                                   
  END
  
  SET @i_vcInputJSON  = REPLACE(REPLACE(REPLACE(@i_vcInputJSON,'/','//'),CHAR(13),''),CHAR(10),'')


  
  DECLARE @iJsonLevel INT = 0, @strReqid VARCHAR(50)='', @isinresid VARCHAR(10)='', @strStatus VARCHAR(2)='',
  @Strerrorcode VARCHAR(20), @StrerrorMessage VARCHAR(200)
      
  
  BEGIN TRY
    SET @string = 'SELECT SerialNo, ColumnName, ColumnValue, ValueTypeColumn, ImageFlag = ''N'' FROM '+@strtradeplustempdb+'.DBO.fn_JsonCutter('''+@i_vcInputJSON+''') '                      
	INSERT INTO @tbl_InputJSONTable                                   
	EXEC(@string)
  END  TRY                
  BEGIN CATCH    
    SET @o_vcOutPutJSON ='[{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}]'                             
    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Input JSON '+ERROR_MESSAGE())                                  
    RETURN 1                                   
  END CATCH
  
  IF @i_vcProjectName = 'TradeWebAPI' AND @i_vcModuleName in('MARGINPLEDGENSDL','MARGINPLEDGECDSL')  
  AND @i_vcFunctionName = 'GetRepledge'
  BEGIN
	SELECT @strClientCode = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
	
	SET @o_vcErrorMessage = (SELECT ScripCode, ISIN, SS_NAME, Qty, StockValue, CDSLReqNo, PSNNo, PSNStatus
    FROM TBL_ClientMarginPledgeDtl(NOLOCK) X, Securities(NOLOCK)
    WHERE RequestDate = CONVERT(VARCHAR,GETDATE(),112)
    AND (ISNULL(PSNNo,'') = '' OR (ISNULL(ResponseCode,'') = 'X' AND ISNULL(PSNNo,'') <> ''))
    AND X.ClientCode = @strClientCode
    AND X.ScripCode = ss_cd FOR JSON PATH)
	SET @o_vcErrorFlag = 'S'
	IF @o_vcErrorFlag = 'S'
	BEGIN
      SET @o_vcOutPutJSON ='[{"ResponseFlag":"##ResponseFlag##","ResponseMessage":"##ErrorMessage##","DATA":""}]'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Process Executed')                                  
	  SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ResponseFlag##',@o_vcErrorFlag)
      SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY(@o_vcErrorMessage))
	END
	ELSE
	BEGIN
      SET @o_vcOutPutJSON ='[{"ResponseFlag":"##ResponseFlag##","ResponseMessage":"##ErrorMessage##","DATA":""}]'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcErrorMessage)                                  
	  SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ResponseFlag##',@o_vcErrorFlag)                                  
      SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY('[]'))
	END
	RETURN 1                                   
  END
  ELSE IF @i_vcProjectName = 'TradeWebAPI' AND @i_vcModuleName in('MARGINPLEDGENSDL','MARGINPLEDGECDSL')  
  AND @i_vcFunctionName = 'GetHolding'
  BEGIN
    DECLARE @i_vxXML VARCHAR(MAX), @strCompanyCode VARCHAR(1), @strApprovedSecurities VARCHAR(1)
	SELECT @strClientCode = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
	SELECT @strCompanyCode = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='CompanyCode'
	SELECT @strDPAccountNo = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='DematActNo'
	SELECT @strApprovedSecurities = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='ApprovedSecurities'
	SET @i_vxXML = '<UserId>'+@strClientCode+'</UserId><CompanyCode>'+@strCompanyCode+'</CompanyCode><DPCode>'+@strDPAccountNo+'</DPCode><ApprovedSecurities>'+@strApprovedSecurities+'</ApprovedSecurities>'
	EXEC stpr_GetAPIHoldingDetail @i_vxXML, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT
	IF @o_vcErrorFlag = 'S'
	BEGIN
      SET @o_vcOutPutJSON ='[{"ResponseFlag":"##ResponseFlag##","ResponseMessage":"##ErrorMessage##","DATA":""}]'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Process Executed')                                  
	  SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ResponseFlag##',@o_vcErrorFlag)
      SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY(@o_vcErrorMessage))
	END
	ELSE
	BEGIN
      SET @o_vcOutPutJSON ='[{"ResponseFlag":"##ResponseFlag##","ResponseMessage":"##ErrorMessage##","DATA":""}]'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcErrorMessage)                                  
	  SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ResponseFlag##',@o_vcErrorFlag)                                  
      SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY('[]'))
	END
	RETURN 1                                   
  END
  ELSE IF @i_vcProjectName = 'TradeWebAPI' AND @i_vcModuleName in('MARGINPLEDGENSDL','MARGINPLEDGECDSL') 
  AND @i_vcFunctionName = 'POSTDATA'
  BEGIN
    DECLARE @xmlData VARCHAR(MAX)=''
	DECLARE @o_vcJsonOutput VARCHAR(MAX)='', @o_vcParam VARCHAR(MAX)=''
    SELECT @xmlData = REPLACE(ColumnValue,'//','/') FROM @tbl_InputJSONTable WHERE ColumnName='XMLVALUE'
    IF ISNULL(@xmlData,'') <> '' 
	BEGIN
      EXEC stpr_APINSDLHOLDING @xmlData, @o_vcErrorFlag  OUTPUT, @o_vcErrorMessage OUTPUT, 
      @o_vcJsonOutput OUTPUT, @o_vcParam OUTPUT
	  SET @o_vcJsonOutput = N'{"JsonOutput": ' + JSON_QUERY(@o_vcJsonOutput) + 
	  N', "Param": ' + JSON_QUERY(@o_vcParam) + 
      N'}'
	  SET @o_vcOutPutJSON ='[{"ResponseFlag":"##ResponseFlag##","ResponseMessage":"##ErrorMessage##","DATA":""}]'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Process Executed')                                  
	  SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ResponseFlag##',@o_vcErrorFlag)
      SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY(@o_vcJsonOutput))
	END
	ELSE
	BEGIN
	  SET @o_vcOutPutJSON ='[{"ResponseFlag":"##ResponseFlag##","ResponseMessage":"##ErrorMessage##","DATA":""}]'     
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','XML CAN NOT BE BLANK')                                  
	  SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ResponseFlag##','E')                                  
	  SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY('[]'))
	END
  END
  ELSE IF @i_vcProjectName = 'TradeWebAPI' AND @i_vcModuleName in('MARGINPLEDGECDSL')  
  AND @i_vcFunctionName = 'GETRESPONSECDSL'
  BEGIN
    BEGIN TRY
      SET @String = 'SELECT SerialNo, ColumnName, ColumnValue, MasterTag,JSONLEVEL, MASTERLEVEL FROM '+@strtradeplustempdb+'.DBO.FN_JSONCUTTER('''+@i_vcInputJSON+''')'
      INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
      EXEC(@String)
    END TRY
    BEGIN CATCH
      SET @o_vcOutPutJSON ='[{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":""}]'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE()+@i_vcInputJSON)  
	  SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY('[]'))
      RETURN 1 	
    END CATCH
	
	SELECT @strbrokerOrderNo = ColumnValue FROM @tbl_jsonoutput 
	WHERE MasterTag = 'pledgeresdtls' and ColumnName = 'reqid'
	
	SELECT @StrerrorMessage = ISNULL((SELECT TOP 1 ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'pledgeresdtls' 
	                           AND ColumnName = 'reserror'),'')+'|'+
							   ISNULL((SELECT TOP 1 ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'pledgeresdtls' 
	                           AND ColumnName = 'reserrmsg'),'')
	
    IF EXISTS (SELECT 1 FROM @tbl_jsonoutput WHERE MasterTag = 'pledgeresdtls' and ColumnName = 'resstatus' and ColumnValue = '0')
    BEGIN
      DECLARE CURSOR_Output CURSOR FOR      
      SELECT DISTINCT JSONLEVEL  
      FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls'
      
	  OPEN CURSOR_Output       
      FETCH NEXT FROM CURSOR_Output INTO @iJsonLevel      
      WHILE @@FETCH_STATUS = 0       
      BEGIN  
	    SELECT @strStatus = ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	    AND JSONLEVEL = @iJsonLevel AND ColumnName = 'status'
	    
		SELECT @strReqid = ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	    AND JSONLEVEL = @iJsonLevel AND ColumnName = 'isinreqid'
		
	    IF ISNULL(@strStatus,'') <> '0'
	    BEGIN
           UPDATE A SET A.Rq_Note = SUBSTRING(ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'errorcode'),'')+'|'+
							   ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'errormsg'),''),1,50),
		   A.Rq_Status3 = 'R'					   
	       FROM PledgeRequest A WHERE Rq_Status3 = 'P' 
	       AND Rq_Clientcd+Rq_Scripcd = @strReqid
		   AND Rq_IpAddress LIKE '%|'+@strbrokerOrderNo+'|%'
		   AND Rq_Date = CONVERT(VARCHAR,GETDATE(),112)
		   AND SUBSTRING(Rq_DematActNo,1,2) <> 'IN'
		   
		   UPDATE A SET A.ISINResid = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'isinresid'),''),
		   A.ResponseCode = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'errorcode'),''),
           A.ResponseMessage = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'errormsg'),''), UpdateTimeStamp = GETDATE() 	 							   
		   FROM TBL_ClientMarginPledgeDtl A
		   WHERE RequestDate = CONVERT(VARCHAR,GETDATE(),112)
		   AND CDSLReqNo = @strbrokerOrderNo
		   AND ISINReqId = @strReqid
		   AND ISNULL(ResponseCode,'') = ''
		   
	    END
	    ELSE IF isnull(@strStatus,'') = '0'
	    BEGIN
	      UPDATE A SET A.Rq_Status3 = 'S'
	      FROM PledgeRequest A WHERE Rq_Status3 = 'P'
	      AND  Rq_Clientcd+Rq_Scripcd = @strReqid
		  AND Rq_IpAddress LIKE '%|'+@strbrokerOrderNo+'|%'
		  AND Rq_Date = CONVERT(VARCHAR,GETDATE(),112)
		  AND SUBSTRING(Rq_DematActNo,1,2) <> 'IN'
		  
		  UPDATE A SET A.ISINResid = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'isinresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'isinresid'),''),
		  A.ResponseCode = 'S',
          A.ResponseMessage = '', UpdateTimeStamp = GETDATE() 							   							   
		  FROM TBL_ClientMarginPledgeDtl A
		  WHERE RequestDate = CONVERT(VARCHAR,GETDATE(),112)
		  AND CDSLReqNo = @strbrokerOrderNo
		  AND ISINReqId = @strReqid
		  AND ISNULL(ResponseCode,'') = ''
	    END
        FETCH NEXT FROM CURSOR_Output INTO @iJsonLevel      
      END       
      CLOSE CURSOR_Output       
      DEALLOCATE CURSOR_Output 
	  
	  -- CALL CDSL INQUERY
	  
	  EXEC stpr_APICDSLINQUERYCALL @strbrokerOrderNo, '', @o_vcOutPutJSON OUTPUT
	  
	  -- CALL REPLEDGE 
	  
	  EXEC stpr_APICDSLRepledgeCALL @strbrokerOrderNo, '', @o_vcOutPutJSON OUTPUT
	  
	  SET @o_vcOutPutJSON ='[{"ResponseFlag":"S","ResponseMessage":"##ErrorMessage##","DATA":""}]'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Process Executed')  
	  SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY('[]'))
      RETURN 1 	
	  
	END
    ELSE	
	BEGIN 
	  UPDATE A SET A.Rq_Note = SUBSTRING(@StrerrorMessage,1,50), A.Rq_Status3 = 'R'
	  FROM PledgeRequest A 
	  WHERE Rq_Status3 = 'P' 
	  AND Rq_IpAddress LIKE '%|'+@strbrokerOrderNo+'|%'
	  AND Rq_Date = CONVERT(VARCHAR,GETDATE(),112)
	  AND SUBSTRING(Rq_DematActNo,1,2) <> 'IN'
	  
	  UPDATE A SET A.ResponseCode = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'pledgeresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'reserror'),''),
      A.ResponseMessage = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'pledgeresdtls' 
	                           and JSONLEVEL = @iJsonLevel AND ColumnName = 'reserrmsg'),''),
      UpdateTimeStamp = GETDATE() 							   
	  FROM TBL_ClientMarginPledgeDtl A
	  WHERE RequestDate = CONVERT(VARCHAR,GETDATE(),112)
	  AND CDSLReqNo = @strbrokerOrderNo
	  AND ISNULL(ResponseCode,'') = ''
	  
	  SET @o_vcOutPutJSON ='[{"ResponseFlag":"S","ResponseMessage":"##ErrorMessage##","DATA":""}]'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@StrerrorMessage)  
	  SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY('[]'))
      RETURN 1 	
	END
  END
  ELSE IF @i_vcProjectName = 'TradeWebAPI' AND @i_vcModuleName ='MARGINPLEDGENSDL' AND @i_vcFunctionName = 'GETRESPONSENSDL'
  BEGIN
  
    BEGIN TRY
      SET @String = 'SELECT SerialNo, ColumnName, ColumnValue, MasterTag,JSONLEVEL, MASTERLEVEL FROM '+@strtradeplustempdb+'.DBO.FN_JSONCUTTER('''+@i_vcInputJSON+''')'
      INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
      EXEC(@String)
    END TRY
    BEGIN CATCH
      SET @o_vcOutPutJSON ='[{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":""}]'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE()+@i_vcInputJSON)  
	  SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY('[]'))
      RETURN 1 	
    END CATCH
	
	
	SELECT @strUccCode = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'pledgorUCC' AND MasterTag='pledgeDtls'
	SELECT @strbrokerOrderNo = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'brokerOrderNo' AND MasterTag='orderDtls'
    SET @StrerrorMessage = '' 
	
	SELECT @StrerrorMessage = SUBSTRING(ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = '' 
	and ColumnName = 'status'),'')+'|'+ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput 
	WHERE MasterTag = ''  AND ColumnName = 'description'),''),1,50)

	IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'status' and MasterTag = '' and ColumnValue = '00')
	BEGIN 
	  DECLARE CURSOR_Output_N CURSOR FOR      
      SELECT DISTINCT JSONLEVEL  
      FROM @tbl_jsonoutput WHERE MasterTag = 'secDtls'
      
	  OPEN CURSOR_Output_N       
      FETCH NEXT FROM CURSOR_Output_N INTO @iJsonLevel      
      WHILE @@FETCH_STATUS = 0       
      BEGIN  
	    SELECT @strStatus = ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'secDtls' 
	    AND JSONLEVEL = @iJsonLevel AND ColumnName = 'pledgeStatus'
	    
		SELECT @strReqid = ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'secDtls' 
	    AND JSONLEVEL = @iJsonLevel AND ColumnName = 'seqNo'

		SELECT @strISIN = ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'secDtls' 
	    AND JSONLEVEL = @iJsonLevel AND ColumnName = 'isin'
        set @StrerrorMessage = ''
		SELECT @StrerrorMessage =  SUBSTRING(ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE MasterTag = 'secDtls' 
	    AND JSONLEVEL = @iJsonLevel and ColumnName = 'pledgeStatus'),'')+'|'+ISNULL((SELECT TOP 1 ColumnValue FROM @tbl_jsonoutput 
	    WHERE MasterTag = ''  AND ColumnName = 'description'),''),1,50)
	    
		UPDATE A SET A.Rq_Note = @StrerrorMessage,
        A.Rq_Status3 = 'S', A.Rq_Status2 = 'O'							   
	    FROM PledgeRequest A 
		WHERE Rq_Clientcd = @strUccCode
		AND Rq_IpAddress = @strReqid+'|'+@strbrokerOrderNo+'|'+@strISIN
		AND Rq_Status3 = 'P' 
        FETCH NEXT FROM CURSOR_Output_N INTO @iJsonLevel      
      END       
      CLOSE CURSOR_Output_N       
      DEALLOCATE CURSOR_Output_N  
	  
	  SET @o_vcOutPutJSON ='[{"ResponseFlag":"S","ResponseMessage":"##ErrorMessage##","DATA":""}]'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@StrerrorMessage)  
	  SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY('[]'))
      RETURN 1 	
	  
	END
	ELSE
	BEGIN
	  UPDATE A SET A.Rq_Note = @StrerrorMessage, A.Rq_Status3 = 'R'							   
	  FROM PledgeRequest A 
	  WHERE Rq_Clientcd = @strUccCode
		AND Rq_IpAddress like '%'+@strbrokerOrderNo+'%'
		AND Rq_Status3 = 'P' 
      
	  SET @o_vcOutPutJSON ='[{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##","DATA":""}]'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@StrerrorMessage)  
	  SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY('[]'))
      RETURN 1 	
	END
  END
    
  ELSE
  BEGIN
    SET @o_vcOutPutJSON ='[{"ResponseFlag":"##ResponseFlag##","ResponseMessage":"##ErrorMessage##","DATA":""}]'     
    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','API NOT DEFINE')                                  
	SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ResponseFlag##','E')                                  
	SET @o_vcOutPutJSON = JSON_MODIFY(@o_vcOutPutJSON, '$[0].DATA', JSON_QUERY('[]'))
    RETURN 1                                   
  END
END
GO


CREATE PROCEDURE [dbo].[stpr_APINSDLHOLDING] @i_vcXML VARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(500) OUTPUT, 
@o_vcJsonOutput VARCHAR(MAX) OUTPUT, @o_vcParam VARCHAR(MAX) OUTPUT WITH ENCRYPTION AS
BEGIN
  
  DECLARE @xmldata XML = CAST(@i_vcXML AS XML), 
  @strRequestString VARCHAR(MAX)='', @strAPIRequest VARCHAR(MAX)='', @strJsonData VARCHAR(MAX)='', @strAPIUrl VARCHAR(200)='',
  @strCallBackUrl VARCHAR(200)=''
  
  DECLARE @tbl_NSDLPLEDGE TABLE(SerialNo int identity(1,1),
  ClientCode VARCHAR(50), ISIN VARCHAR(50), ISINName VARCHAR(200), Qty MONEY, DPAccountNo VARCHAR(16), ScripCode VARCHAR(20), StockValue MONEY)
  
  SET @o_vcJsonOutput = '{}'
  SET @o_vcParam = '{}'
  
  INSERT INTO @tbl_NSDLPLEDGE(ClientCode, ScripCode, ISIN, ISINName, Qty, DPAccountNo, StockValue)
  SELECT X1.* FROM(
  SELECT HOLDING.value('(ClientCode)[1]', 'VARCHAR(50)') AS Cm_cd ,
  HOLDING.value('(ScripCode)[1]', 'VARCHAR(50)') AS ScripCode ,
  HOLDING.value('(ISIN)[1]', 'VARCHAR(50)') AS ISIN ,
  HOLDING.value('(ISINName)[1]', 'VARCHAR(200)') AS ISINName ,
  HOLDING.value('(Qty)[1]', 'MONEY') AS Qty,
  HOLDING.value('(DPAccountNo)[1]', 'VARCHAR(16)') AS DPAccountNo,
  HOLDING.value('(Value)[1]', 'MONEY') AS Value
  FROM @xmldata.nodes('/HOLDING') AS XTbl(HOLDING)) X1
  
  IF NOT EXISTS(sELECT 1 FROM @tbl_NSDLPLEDGE)
  BEGIN
    SET @o_vcErrorFlag = 'E'
    SET @o_vcErrorMessage = 'Data not Avaiable'
	RETURN 1
  END
  
  DELETE FROM PledgeRequest WHERE Rq_Clientcd IN(SELECT ClientCode FROM @tbl_NSDLPLEDGE)
  AND Rq_Status1 = 'S' AND Rq_Status2 = 'O' AND Rq_Status3 = 'P' 
  
  DECLARE @strClientDP VARCHAR(16)=''
  SELECT TOP 1 @strClientDP = DPAccountNo FROM @tbl_NSDLPLEDGE
    
  DECLARE @strnumOfSecurities VARCHAR(10)='', @strpledgorDpId VARCHAR(8)='', @strpledgorClientId VARCHAR(8)='', @strClientCode VARCHAR(20)=''
  SELECT @strnumOfSecurities = CAST(COUNT(*) AS VARCHAR) FROM @tbl_NSDLPLEDGE
  
  SELECT @strpledgorDpId = SUBSTRING(DPAccountNo,1,8), @strpledgorClientId = SUBSTRING(DPAccountNo,9,8), @strClientCode  =  ClientCode
  FROM @tbl_NSDLPLEDGE
  
  DECLARE @strRandomNoFinal VARCHAR(30)='', @strCRandomNoFinal VARCHAR(20)=''
   
  IF SUBSTRING(@strClientDP,1,2) = 'IN'
  BEGIN
    SELECT @strAPIRequest = RequestJson, @strAPIUrl =  APIUrl
    FROM DBO.tbl_VendorAPISetting(NOLOCK) WHERE APIVendorName = 'NSDL' AND APINAME = 'MARGINPLEDGE'
  
    DECLARE @strRequestor VARCHAR(MAX)=ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 1), ''), 
    @strRequestorId VARCHAR(50) = ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 2), ''), 
    @strsegment VARCHAR(50)= ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 3), ''), 
    @strExchangeCd VARCHAR(50)= ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 4), ''),
    @strPledgeeDpId VARCHAR(8)= ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 5), ''),  
    @strpledgeeClientId VARCHAR(8)=ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 6), ''), 
    @strtmId VARCHAR(10)=ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 7), ''), 
    @strcmId VARCHAR(10)=ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 8), ''),
    @strTrxType VARCHAR(10)=ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 9), ''),
    @strChannel VARCHAR(10)=ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 10), '')
  
    IF ISNULL(@strRequestor,'') = '' OR ISNULL(@strRequestorId,'') = '' OR ISNULL(@strExchangeCd,'') = '' OR ISNULL(@strsegment,'') = ''
    OR ISNULL(@strPledgeeDpId,'') = '' OR ISNULL(@strpledgeeClientId,'') = '' OR ISNULL(@strtmId,'') = '' OR ISNULL(@strcmId,'') = '' 
    OR ISNULL(@strTrxType,'') = '' OR ISNULL(@strChannel,'') = ''
    BEGIN
      SET @o_vcErrorFlag = 'E'
      SET @o_vcErrorMessage = 'Parameter Value Should be "requestor|requestorId|segment|exchangeCd|pledgeeDpId|pledgeeClientId|tmId|cmId|transactionType|channel"'
	  RETURN 1
    END
  
    IF (SELECT COUNT(DISTINCT CLIENTCODE) FROM @tbl_NSDLPLEDGE) > 1
    BEGIN
      SET @o_vcErrorFlag = 'E'
      SET @o_vcErrorMessage = 'Wrong Request Only One Client Allow at one time'
	  RETURN 1
    END
  
    DECLARE @strRandomNo INT = ABS(CHECKSUM(NEWID())) % 10000
    SELECT @strRandomNoFinal = CAST(FORMAT(GETDATE(), 'ddMMyyhhmmss') AS VARCHAR)+RIGHT(REPLICATE('0', '4') + CAST(@strRandomNo AS VARCHAR), '4')
  
    SET @strRequestString = '{"orderDtls": {"brokerOrderNo": "##REFNO##","exchangeCd": "##exchangeCd##","segment": "##segment##","numOfSecurities": "##numOfSecurities##",
    "secDtls": [##DATA##]},"pledgeDtls": {"pledgorDpId": "##pledgorDpId##","pledgorClientId": "##pledgorClientId##","pledgorUCC": "##pledgorUCC##",
    "pledgeeDpId": "##pledgeeDpId##","pledgeeClientId": "##pledgeeClientId##","tmId": "##tmId##","cmId": "##cmId##","execDt": "##execDt##"}}'
    SET @strJsonData = (SELECT [seqNo] = CAST(SerialNo AS VARCHAR), [isin] = ISIN, [isinName] = ISINName, [quantity] = CAST(CAST(Qty*1000 AS INT) AS VARCHAR),
    [lockInReasonCode] = '', [lockInReleaseDate] = '' 
    FROM @tbl_NSDLPLEDGE ORDER BY SerialNo FOR JSON PATH)
  
    SELECT @strJsonData = SUBSTRING(@strJsonData, 2, LEN(@strJsonData))
    SELECT @strJsonData = SUBSTRING(@strJsonData, 1, LEN(@strJsonData) - 1)
    SET @strRequestString  = REPLACE(@strRequestString, '##REFNO##',@strRandomNoFinal)
    SET @strRequestString  = REPLACE(@strRequestString, '##exchangeCd##',@strExchangeCd)
    SET @strRequestString  = REPLACE(@strRequestString, '##segment##',@strsegment)
    SET @strRequestString  = REPLACE(@strRequestString, '##numOfSecurities##',@strnumOfSecurities)
    SET @strRequestString  = REPLACE(@strRequestString, '##DATA##',@strJsonData)
    SET @strRequestString  = REPLACE(@strRequestString, '##pledgorDpId##',@strpledgorDpId)
    SET @strRequestString  = REPLACE(@strRequestString, '##pledgorClientId##',@strpledgorClientId)
    SET @strRequestString  = REPLACE(@strRequestString, '##pledgorUCC##',@strClientCode)
    SET @strRequestString  = REPLACE(@strRequestString, '##pledgeeDpId##',@strPledgeeDpId)
    SET @strRequestString  = REPLACE(@strRequestString, '##pledgeeClientId##',@strpledgeeClientId)
    SET @strRequestString  = REPLACE(@strRequestString, '##tmId##',@strtmId)
    SET @strRequestString  = REPLACE(@strRequestString, '##cmId##',@strcmId)
    SET @strRequestString  = REPLACE(@strRequestString, '##execDt##',CAST(FORMAT(GETDATE(), 'dd-MM-yyyy') AS VARCHAR))
  
    SET @o_vcJsonOutput = @strRequestString
  
    SET @o_vcParam = (SELECT [TransactionType] = @strTrxType, [Requestor] = @strRequestor, [RequestorId] = @strRequestorId, 
    [Channel] = @strChannel, [APIUrl] = @strAPIUrl FOR JSON PATH)
  END
  ELSE IF SUBSTRING(@strClientDP,1,2) <> 'IN' AND @strClientDP <> ''
  BEGIN
  
    DELETE FROM TBL_ClientMarginPledgeDtl
	WHERE RequestDate =  CONVERT(VARCHAR,GETDATE(),112)
	AND ClientCode IN(SELECT ClientCode FROM @tbl_NSDLPLEDGE)
	AND ISNULL(ResponseCode,'') = ''
  
    SELECT @strAPIRequest = RequestJson, @strAPIUrl =  APIUrl, @strCallBackUrl = CallBackUrl
    FROM DBO.tbl_VendorAPISetting(NOLOCK) WHERE APIVendorName = 'CDSL' AND APINAME = 'MARGINPLEDGE'
	
	DECLARE @strPledgeidentifier VARCHAR(MAX)=ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 1), ''), 
    @strpledgeeboid VARCHAR(50) = ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 2), ''), 
    @strexid VARCHAR(50)= ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 3), ''), 
	@strEntityidentifier VARCHAR(50)= ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 4), ''),
    @strCTMID VARCHAR(8)= ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 5), ''),  
    @strCSegmentid VARCHAR(8)=ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 6), ''), 
    @strCCmId VARCHAR(10)=ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 7), ''), 
    @strReasonCode VARCHAR(10)=ISNULL((SELECT VALUE FROM ( SELECT *
				FROM DBO.fn_SplitString(@strAPIRequest, '|')) X1 WHERE X1.Position = 8), '')
	
    DECLARE @strCRandomNo INT = ABS(CHECKSUM(NEWID())) % 10000
	SELECT @strCRandomNoFinal = CAST(FORMAT(GETDATE(), 'ddMMyyhhmmss') AS VARCHAR)+RIGHT(REPLICATE('0', '4') + CAST(@strCRandomNo AS VARCHAR), '4')
	
    SET @strRequestString = '{"pledgeidentifier": "##pledgeidentifier##","ReqTime": "##ReqTime##","ReturnURL": "##ReturnURL##",
	"pledgorboid": "##pledgorboid##","pledgeeboid": "##pledgeeboid##","uccid": "##uccid##","exid": "##exid##",
	"entityidentifier": "##entityidentifier##","TMID": "##TMID##","remarks": "##remarks##","executiondate": "##executiondate##",
	"expirydate": "##expirydate##","isindtls":[##DATA##]}'
    
	
    
	INSERT INTO TBL_ClientMarginPledgeDtl(ClientCode, ScripCode, Isin, DPAccountNo, RequestDate, CDSLReqNo, PledgeFormNo, 
	PledgeIntRefNo, ISINReqId, Qty, StockValue)
	SELECT ClientCode, ScripCode, ISIN, DPAccountNo, 	CONVERT(VARCHAR,GETDATE(),112),  @strCRandomNoFinal,	
	CAST(FORMAT(GETDATE(), 'ddMMyyhhmmss') AS VARCHAR)+RIGHT(REPLICATE('0', '4') + CAST(ABS(CHECKSUM(NEWID())) % 10000 AS VARCHAR), '4'),
	CAST(FORMAT(GETDATE(), 'ddMMyyhhmmss') AS VARCHAR)+RIGHT(REPLICATE('0', '4') + CAST(ABS(CHECKSUM(NEWID())) % 10000 AS VARCHAR), '4'),
	cast(ClientCode as varchar)+CAST(ScripCode AS VARCHAR), Qty, StockValue
    FROM @tbl_NSDLPLEDGE 	
	  
	SET @strJsonData = (SELECT [prfnumber] = X.PledgeFormNo, 
	[pledgorintref] = X.PledgeIntRefNo, 
	[isinreqid] = X.ISINReqId,
	[isin] = X.ISIN, [quantity] = CAST(CAST(X.Qty AS INT) AS VARCHAR),
    [value] = CAST(CASE WHEN ISNULL(CAST(X.StockValue AS INT),0)  = 0  THEN CAST(X.Qty AS INT) ELSE ISNULL(CAST(X.StockValue AS INT),0) END AS VARCHAR), 
	[segmentid] = @strCSegmentid , [cmid] = @strCCmId, [ReasonCode] = @strReasonCode
    FROM TBL_ClientMarginPledgeDtl(NOLOCK) X, @tbl_NSDLPLEDGE  Y
	WHERE X.ClientCode = Y.ClientCode
	AND X.ScripCode = Y.ScripCode
	AND X.RequestDate =  CONVERT(VARCHAR,GETDATE(),112)
	AND ISNULL(X.ResponseCode,'') = ''
	AND CDSLReqNo = @strCRandomNoFinal 
	ORDER BY X.SerialNo FOR JSON PATH)
	
	SELECT @strJsonData = SUBSTRING(@strJsonData, 2, LEN(@strJsonData))
    SELECT @strJsonData = SUBSTRING(@strJsonData, 1, LEN(@strJsonData) - 1)
	
	SET @strRequestString  = REPLACE(@strRequestString, '##pledgeidentifier##',@strPledgeidentifier)
	SET @strRequestString  = REPLACE(@strRequestString, '##ReqTime##',CAST(FORMAT(GETDATE(), 'ddMMyyyyHHmmss') AS VARCHAR))
	SET @strRequestString  = REPLACE(@strRequestString, '##ReturnURL##',@strCallBackUrl)
	SET @strRequestString  = REPLACE(@strRequestString, '##pledgorboid##',@strClientDP)
	SET @strRequestString  = REPLACE(@strRequestString, '##pledgeeboid##',@strpledgeeboid)
	SET @strRequestString  = REPLACE(@strRequestString, '##uccid##',@strClientCode)
	SET @strRequestString  = REPLACE(@strRequestString, '##exid##',@strexid)
	SET @strRequestString  = REPLACE(@strRequestString, '##entityidentifier##',@strEntityidentifier)
	SET @strRequestString  = REPLACE(@strRequestString, '##TMID##',@strCTMID)
	SET @strRequestString  = REPLACE(@strRequestString, '##remarks##','MARGINPLEDGE')
	SET @strRequestString  = REPLACE(@strRequestString, '##executiondate##',CAST(FORMAT(GETDATE(), 'ddMMyyyy')  AS VARCHAR))
	SET @strRequestString  = REPLACE(@strRequestString, '##expirydate##',CAST(FORMAT(GETDATE()+1, 'ddMMyyyy')  AS VARCHAR))
	SET @strRequestString  = REPLACE(@strRequestString, '##DATA##',@strJsonData)
	SET @o_vcJsonOutput = @strRequestString
	SET @o_vcParam = (SELECT [APIUrl] = @strAPIUrl, DPId  = SUBSTRING(@strpledgeeboid,4,5),
    [ReqId] =  @strCRandomNoFinal, [Version] = '1.0'
	FOR JSON PATH)
  END 
  
  INSERT INTO PledgeRequest(Rq_Clientcd, Rq_DematActNo, Rq_Scripcd, Rq_Qty, Rq_IpAddress, 
  Rq_Date, Rq_Time, Rq_Status1, Rq_Status2, Rq_Status3, Rq_Status4, Rq_Note)
  SELECT ClientCode, Rq_DematActNo = DPAccountNo, Rq_Scripcd = ScripCode, Rq_Qty = Qty, 
  Rq_IpAddress = CAST(SerialNo AS VARCHAR)+'|'+(CASE WHEN SUBSTRING(@strClientDP,1,2) = 'IN' THEN CAST(@strRandomNoFinal as VARCHAR) ELSE
  CAST(@strCRandomNoFinal AS VARCHAR) END)+'|'+ISIN,
  Rq_Date  = CONVERT(VARCHAR,GETDATE(),112), Rq_Time =  REPLACE(CONVERT(VARCHAR,GETDATE(),108),':',''),
  Rq_Status1 = 'S', Rq_Status2 = 'O', Rq_Status3 = 'P', Rq_Status4 = '',Rq_Note = ''
  FROM @tbl_NSDLPLEDGE
   
  SELECT @o_vcParam = SUBSTRING(@o_vcParam, 2, LEN(@o_vcParam))
  SELECT @o_vcParam = SUBSTRING(@o_vcParam, 1, LEN(@o_vcParam) - 1)
  SET @o_vcErrorFlag = 'S'
  SET @o_vcErrorMessage = 'Process Executed'
  RETURN 1
END
GO