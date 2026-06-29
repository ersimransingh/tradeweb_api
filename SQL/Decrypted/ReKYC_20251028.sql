CREATE FUNCTION [dbo].FN_base64toBinary(@bin NVARCHAR(MAX))
RETURNS VARBINARY(MAX)
AS
BEGIN
    DECLARE @Base64 VARBINARY(MAX)
    SET @Base64 = CAST('' AS XML).value('xs:base64Binary(sql:variable("@bin"))', 'VARBINARY(MAX)')
    RETURN @Base64
END
GO

CREATE FUNCTION [ReturnTable]
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

CREATE Procedure [dbo].[SP_GetMasterData] @masterName VARCHAR(50)
WITH ENCRYPTION
AS
BEGIN
	IF (@masterName = 'State')
	BEGIN
		SELECT st_State Code, st_State Name
		FROM State_Master WITH (NOLOCK)
		ORDER BY st_State
	END
	ELSE IF (@masterName = 'Relation')
	BEGIN
		DECLARE @CrossDB VARCHAR(100) = '', @CrossOwner VARCHAR(50) = '', @string VARCHAR(500) = ''
		SELECT @CrossDB = LTRIM(RTRIM(OP_DataBase)), @CrossOwner = LTRIM(RTRIM(OP_Owner))
		FROM Other_Products(NOLOCK)
		WHERE OP_Product = 'Cross' AND OP_STATUS = 'A'
		IF @CrossDB <> ''
		BEGIN
			SET @string = ' Select rtrim(ltrim(cs_code)) Code, rtrim(ltrim(cs_desc)) Name  From  ' + @CrossDB + '.' + @CrossOwner + 
				'.' + 'Clientsub_master (NoLock) where cs_module = ''CS19'''
			EXEC (@string)
		END
		ELSE 
		BEGIN
		  SELECT @CrossDB = LTRIM(RTRIM(OP_DataBase)), @CrossOwner = LTRIM(RTRIM(OP_Owner))
		  FROM Other_Products(NOLOCK)
		  WHERE OP_Product = 'Estro' AND OP_STATUS = 'A'
		  IF @CrossDB <> ''
		  BEGIN
			SET @string = ' SELECT RTRIM(LTRIM(cs_code)) Code, RTRIM(LTRIM(cs_desc)) Name  FROM  ' + @CrossDB + '.' + @CrossOwner + 
				'.' + 'Clientsub_master(NoLock) where cs_module = ''CS27'''
			EXEC (@string)
		  END
		END
	END
END
GO

CREATE Procedure [SP_GetSegmentUserwise] @i_vcClientCode VARCHAR(20), @i_vcCondition VARCHAR(500) , @i_oJsonValue VARCHAR(MAX) OUTPUT 
WITH ENCRYPTION
AS  
BEGIN  
/*            
///////////////////////////////////////////////////////////////////////////////////////////            
// Create By     : Hemant Jhala
// Created Date  : 15-JAN-2024           
// CCT NO        :           
// Description   : This Procedure is used to get segment value user wise with segment value or all.     
// Reviewed By   :  
// Review Date   :  
//////////////////////////////////////////////////////////////////////////////////////////            
  */      
  Declare @tbl_SegmentTemp TABLE (Segment VARCHAR(50), ce_companycode VARCHAR(50), ExchVal VARCHAR(50), Exchange VARCHAR(100),
  MainExchCode VARCHAR(50)) 
  DECLARE @strCommexConn VARCHAR(50)='', @string VARCHAR(MAX)=''  
  
  Declare @WhereCondition varchar(100)=''
  Set @WhereCondition = IIF(@i_vcCondition='','AND 1 = 1 ','And ce_companycode in (''' + REPLACE(@i_vcCondition, ',', ''',''') + ''')')
  
  SELECT @strCommexConn = LTRIM(RTRIM(OP_DataBase)) 
  FROM Other_Products(NOLOCK) WHERE OP_Product = 'Commex' and ISNULL(OP_Status,'') = 'A'
  
  IF ISNULL(@strCommexConn,'')=''
  BEGIN
    SET @strCommexConn = ''
  END
 
 SET @string = ' SELECT * FROM (SELECT ''Equity'' AS tag, ce_companycode = ISNULL(LTRIM(RTRIM(ce_companycode)),''''), '
  +' CES_Segment = REPLACE(LTRIM(RTRIM(SEG.CES_Segment)),''&'','''') '
  +' ,CES_Exchange = LTRIM(RTRIM(CES_Exchange)), MainExchCode =  ISNULL(LTRIM(RTRIM(CES_Cd)),'''') '
  +' FROM  CompanyExchangeSegments(NOLOCK) SEG LEFT OUTER JOIN Client_details(NOLOCK) C  '
  +' ON(C.ce_companycode = SEG.CES_Cd AND ce_clientcd = '''+ @i_vcClientCode +''' AND ce_regDt <> '''')    '
  +' UNION ALL '
  +' SELECT ''Equity'' AS tag, ce_companycode = sp_sysvalue, CES_Segment = ''SLBM'' ,CES_Exchange = ''NSE'', MainExchCode = sp_sysvalue '
  +' FROM Sysparameter(NOLOCK) C  WHERE sp_parmcd = ''SLBMexchange'''
  IF @strCommexConn <> ''
  BEGIN
    SET @string = @string+ ' UNION ALL '
    +' SELECT ''Commodity'' AS tag, ce_companycode = ISNULL(LTRIM(RTRIM(ce_companycode)),''''), CES_Segment = REPLACE(LTRIM(RTRIM(SEG.CES_Segment)),''&'','''') '
    +' ,CES_Exchange = LTRIM(RTRIM(CES_Exchange)), MainExchCode =  ISNULL(LTRIM(RTRIM(CES_Cd)),'''') '
    +' FROM '+@strCommexConn+'.DBO.CompanyExchangeSegments(NOLOCK) SEG LEFT OUTER JOIN '+@strCommexConn+'.DBO.Client_details(NOLOCK) C '
    +' ON(C.ce_companycode = SEG.CES_Cd AND ce_clientcd = '''+ @i_vcClientCode +''' AND ce_regDt <> '''')    '
    +' ) X12 '
    +' WHERE 1 = 1 ' + @WhereCondition 
  END
  ELSE
  BEGIN
    SET @string = @string+ ' ) X12 '
    +' WHERE 1 = 1 ' + @WhereCondition 
  END  
  INSERT INTO @tbl_SegmentTemp
  EXEC(@string)

  
  SET @i_oJsonValue = (Select * from @tbl_SegmentTemp FOR JSON PATH)
  RETURN  
END  
GO

CREATE PROCEDURE [dbo].[stpr_APISendEmail] @i_vcInputJson VARCHAR(MAX), @i_OTPType VARCHAR(20) ='EMAIL',
@CallingAPIURLMain VARCHAR(MAX),
@o_vcOutputJson NVARCHAR(MAX) OUTPUT WITH ENCRYPTION AS
BEGIN
  SET @o_vcOutputJson = ''
  IF ISNULL(@i_OTPType,'') = 'EMAIL'
  BEGIN
    SET @CallingAPIURLMain = @CallingAPIURLMain+'/api/Main/SendEmail'  
  END
  ELSE IF ISNULL(@i_OTPType,'') = 'OTP'
  BEGIN 
    SET @CallingAPIURLMain = @CallingAPIURLMain+'/api/ThirdPartyService/SendOTP'  
  END
  ELSE IF ISNULL(@i_OTPType,'') = 'OTPValidate'
  BEGIN 
    SET @CallingAPIURLMain = @CallingAPIURLMain+'/api/ThirdPartyService/VerifyOTP'  
  END
  --SELECT @CallingAPIURLMain
  DECLARE @Object AS INT;  
  DECLARE @ResponseText AS VARCHAR(8000)='';  
  Declare @tbl_OutputResponse as table(Json_Table nvarchar(max))
  DECLARE @VCOUTPUT VARCHAR(MAX)=''  
  EXEC sp_OACreate 'MSXML2.XMLHTTP', @Object OUT;  
  EXEC sp_OAMethod @Object, 'open', NULL, 'post',@CallingAPIURLMain, 'false' 
  EXEC sp_OAMethod @Object, 'setRequestHeader', null, 'Content-Type', 'application/json'  
  EXEC sp_OAMethod @Object, 'send', null, @i_vcInputJson  
  INSERT INTO @tbl_OutputResponse (Json_Table) EXEC sp_OAMethod @Object, 'responseText'
  SELECT @VCOUTPUT = Json_Table FROM @tbl_OutputResponse
  SET @o_vcOutputJson = @VCOUTPUT  
  --SELECT @o_vcOutputJson
  EXEC sp_OADestroy @Object  
  
  RETURN 1
END
GO

CREATE PROCEDURE [SP_ReKyc_CheckerApprove] @i_vcClient_Code VARCHAR(20), @i_vcRefNo numeric(10), @i_vcApprovalFlag VARCHAR(1),
@i_vcApprovalReason VARCHAR(500), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(500) OUTPUT, @o_vcJsonOutput VARCHAR(MAX) OUTPUT,
@i_vcTemplateCode VARCHAR(20) = 'Template1' WITH ENCRYPTION  AS
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
--@@VAIBHAV/18-DEC-2023/
  SET @o_vcErrorFlag = 'S'
  SET @o_vcErrorMessage = 'Success' --'Process Completed'
  set @o_vcJsonOutput = '{}'
  
  DECLARE @i_vcInputJsonEMAIL VARCHAR(MAX) = '',@o_vcOutputJsonEMAIL VARCHAR(MAX) = ''
  DECLARE @strBodyText NVARCHAR(MAX)='', @strCCEMailid VARCHAR(100)='', @strBCCEMailid VARCHAR(100)='', 
  @strToEmailid VARCHAR(100)='',@strSubject VARCHAR(MAX)='', @strCompamnyName VARCHAR(100)='', 
  @strThirdPartyURL VARCHAR(MAX)=''
  
  
  IF NOT EXISTS(select 1 from Client_ReKycMain(nolock) where rm_cmcd = @i_vcClient_Code 
  and  rm_refno = @i_vcRefNo and rm_status = 'Pending'
  and rm_rekyc = 'N') AND @i_vcTemplateCode = 'Template1'
  BEGIN
    SET @o_vcErrorFlag = 'E'
    SET @o_vcErrorMessage = 'Invalid Request'
    RETURN 1
  END
 
  SELECT @strThirdPartyURL = sp_sysvalue 
  FROM Sysparameter(NOLOCK) WHERE sp_parmcd = 'ThirdParty'
  
  DECLARE @StrCompanyCount INT = 0
  
 ------------ ******  Reject Logic Start ***********
 
  IF @i_vcApprovalFlag = 'R'
  BEGIN
    DELETE FROM Client_ModifyAttach WHERE ma_cmcd = @i_vcClient_Code  AND  ma_refno = @i_vcRefNo
	AND ma_filename IN ('EsignRequest','EsignRequestKRA','UnSignedKRApdf','UnSignedPdf','SignedKRAPdf','SignedPdf')
	  
	UPDATE A SET A.rm_status = 'N',
	A.rm_Desc = @i_vcApprovalReason , A.rm_rekyc = 'N', A.rm_step = 1,
	A.mkrdt = convert(varchar,getdate(),112), A.mkrtm = CONVERT(VARCHAR,GETDATE(),108)
	FROM Client_ReKycMain A 
	WHERE rm_cmcd = @i_vcClient_Code  AND  rm_refno = @i_vcRefNo
	  
	UPDATE A SET A.ca_Tplus = 'N'
	FROM Client_ModifyAPI A 
	where ca_cmcd = @i_vcClient_Code  AND  ca_Nfiller3 = @i_vcRefNo
	
	UPDATE A SET A.ma_status = 'N'
	FROM Client_ModifyAttach A 
	where ma_cmcd = @i_vcClient_Code  AND  ma_refno = @i_vcRefNo
	
	--- EMAIL SEND
	
	IF @i_vcTemplateCode = 'Template1' 		
	BEGIN
	  SET @i_vcInputJsonEMAIL = '{"reqestName": "Email","requestObject": {"ToEmailId": "##ToEmailId##","CCEmailId": "##CCEmailId##","BCCEmailId": "##BCCEmailId##",
      "Subject": "##Subject##","Body": "##Body##","Attachment":[]}}'
      SET @o_vcOutputJsonEMAIL = ''
      
	  SELECT @strBodyText = BodyText, @strCCEMailid = CCEmail, @strBCCEMailid = BCCEmailid , 
	  @strSubject = EmailSubject
      FROM tbl_EmailTemplate(NOLOCK) WHERE RefName = 'RekycCheckerReject'

      SELECT @strToEmailid = cm_email 
      FROM Client_Master(NOLOCK) WHERE CM_cD = @i_vcClient_Code
          
	  --SET @strToEmailid = 'Vaibhavgarg2005@gmail.com'
           
      SELECT TOP 1 @strCompamnyName = LTRIM(RTRIM(EM_NAME)) from Entity_master(NOLOCK) 
	  WHERE em_cd =(select MIN(em_cd) from Entity_master(NOLOCK))
	  
	  
	  
	  SELECT @StrCompanyCount = ISNULL(SUM(ISNULL(cnt,0)),0) 
      FROM ( SELECT COUNT(0) Cnt From Entity_master(NOLOCK) 
      WHERE em_bse <> 'N' and isNull(em_bclearingno,'') in ('189') 
      UNION ALL 
      select count(0) Cnt From Entity_master(NOLOCK) 
      Where em_nse <> 'N' and isNull(em_nclearingno,'') in ('07277')) a
	
	  IF @StrCompanyCount > 0
	  BEGIN
	    SELECT @strCompamnyName= LTRIM(RTRIM(em_Name)) FROM Entity_master(NOLOCK)  
	    WHERE em_cd  ='B'
	  END
	  
      SET @strBodyText = REPLACE(REPLACE(@strBodyText,'<<CompanyName>>',@strCompamnyName),'<<Reason>>',@i_vcApprovalReason)
      IF @strToEmailid <> ''
      BEGIN 
 	    SET  @i_vcInputJsonEMAIL = REPLACE(@i_vcInputJsonEMAIL,'##ToEmailId##',@strToEmailid)
        SET  @i_vcInputJsonEMAIL = REPLACE(@i_vcInputJsonEMAIL,'##CCEmailId##',@strCCEMailid)
        SET  @i_vcInputJsonEMAIL = REPLACE(@i_vcInputJsonEMAIL,'##BCCEmailId##',@strBCCEMailid)
        SET  @i_vcInputJsonEMAIL = REPLACE(@i_vcInputJsonEMAIL,'##Subject##',@strSubject)
        SET  @i_vcInputJsonEMAIL = REPLACE(@i_vcInputJsonEMAIL,'##Body##',@strBodyText)
  
	    SET @o_vcOutputJsonEMAIL = ''
	    EXEC stpr_APISendEmail @i_vcInputJsonEMAIL, 'EMAIL', @strThirdPartyURL, @o_vcOutputJsonEMAIL OUTPUT
        INSERT INTO tbl_EMailLog(Code, ToMailid, CCMailid, BCCMailid, EmailSubject, EmailBodyText,
	    AttachmentFile1Name, AttachmentFile1, SendDate,Response,RefName)
	    VALUES(@i_vcClient_Code,@strToEmailid, @strCCEMailid, @strBCCEMailid, @strSubject, @strBodyText, '',
	    '', GETDATE(), isnull(@o_vcOutputJsonEMAIL,0), 'RekycCheckerReject')
	  END
	END
  END
  ------------ ******  Reject Logic End ***********

  ------------ ******  Approve Logic Start ***********
  ELSE IF @i_vcApprovalFlag = 'A'
  BEGIN
    BEGIN TRY
      DECLARE  @TradePlusTableName VARCHAR(100), 
	 @ComexTableName VARCHAR(100), @ca_field VARCHAR(100), @ca_oldValue VARCHAR(MAX), @ca_newValue VARCHAR(MAX),
	 @strClientMasterString VARCHAR(MAX)='', @strClient_InfoString VARCHAR(MAX)='', 
	 @strSegmentOldValue VARCHAR(100), @strSegmentNewValue VARCHAR(100)='',
	 @strDematactColumnString VARCHAR(MAX)='', @strDematactvalueString VARCHAR(MAX)='', @strBankactColumnString VARCHAR(MAX)='',
	 @strBankactvalueString VARCHAR(MAX)='', @NomineeTag VARCHAR(1) = 'N', @strClient_NomineeColumnString VARCHAR(MAX)='',
	 @strClient_NomineevalueString VARCHAR(MAX)='', @strClientCode VARCHAR(20)= @i_vcClient_Code, 
	 @MainTradePlusTableName VARCHAR(100), @ca_Nfiller1 INT, @C1ca_Nfiller1 INT = 0, @string VARCHAR(MAX)='', 
	 @strAuditString VARCHAR(MAX)='',
	 @ca_keyfield VARCHAR(50)='', @ca_computername VARCHAR(50)='', @mkrdt VARCHAR(8)='', @ca_Time varchar(20)='', 
	 @strAuditStringDetail VARCHAR(MAX)='', @strPermanentAddressFlag VARCHAR(1)='N', @strFieldLeg INT =0
    
	 IF EXISTS(SELECT 1 FROM Client_Nominee(NOLOCK) WHERE cn_cd = @i_vcClient_Code)
	 BEGIN
	   set @NomineeTag ='Y'
	 END
	 
	 SET @strAuditString = 'INSERT INTO Common_audit( ca_table ,ca_dpid, ca_keyfield ,ca_keyvalue, ca_field, ca_fielddescription, '
	 +' ca_oldvalue, ca_newvalue, ca_computername, mkrid, mkrdt, mkrtm,  ca_master,ca_keyname, mkridold, mkrdtold, mkrtmold  ) VALUES ( '
	 
	 DECLARE @ca_KeyFieldDescp VARCHAR(200)='', @StrBOID VARCHAR(16)=''
	 DECLARE @tbl_ExecuteCommand TABLE(SerialNo int identity(1,1), TableName VARCHAR(100), QueryExecute VARCHAR(MAX), PrimaryKey VARCHAR(50))
	 
	 SELECT @strPermanentAddressFlag = ISNULL(ca_newvalue,'N')
	 FROM Client_ModifyAPI(nolock) 
	 WHERE ca_cmcd = @strClientCode 
	 AND ca_Nfiller3 = @i_vcRefNo AND ca_field = 'PermanentAddressFlag'
	 
	 
	 
	 DECLARE @STRClosureType varchar(1)='', @StrClosureReason VARCHAR(200)='', @strca_computername1 VARCHAR(100)=''
	
     if @i_vcTemplateCode = 'ONLYCLOSURE'
     BEGIN	 
	   SELECT @STRClosureType = ca_newvalue FROM Client_ModifyAPI(nolock) 
	   WHERE ca_cmcd = @strClientCode AND ca_Nfiller3 = @i_vcRefNo AND ca_field = 'ClosureType'
	   
	   SELECT @StrBOID = ca_newvalue FROM Client_ModifyAPI(nolock) 
	   WHERE ca_cmcd = @strClientCode AND ca_Nfiller3 = @i_vcRefNo AND ca_field = 'BOID'
			  	 
	   SELECT @StrClosureReason = ca_newvalue, @strca_computername1 = ca_computername 
	   FROM Client_ModifyAPI(nolock) 
	   WHERE ca_cmcd = @strClientCode AND ca_Nfiller3 = @i_vcRefNo AND ca_field = 'cm_freezereason'
	 END    
	 
     DECLARE Cur1 CURSOR FOR 
     SELECT x1.*, 
	 ca_keyfield =  (case when TradePlusTableName = 'Client_master' then 'cm_cd'
	 when TradePlusTableName = 'Client_NomineeDetails' then 'cn_Cmcd'
	 when TradePlusTableName = 'Common_Contacts' then 'cc_Client'
	 when TradePlusTableName = 'Dematact' then 'da_clientcd'
	 when TradePlusTableName = 'Client_details' then 'ce_clientcd'
	 when TradePlusTableName = 'Bankact' then 'ba_clientcd' else 'cm_cd' end)
	 FROM (SELECT N.tablename as TradePlusTableName, 
	 '' as ComexTableName,
	 ca_field, ca_oldValue, ca_newValue, ca_computername  , ca_date, ca_Time,  
	 FieldDescp = IIF(N.FieldDescp='', ca_field,N.FieldDescp)     
	 FROM Client_ModifyAPI(NOLOCK) X, tbl_ReKycAuditColumnMapping_new(NOLOCK) N  
	 WHERE ca_cmcd = @i_vcClient_Code AND ca_Nfiller3 = @i_vcRefNo
	 AND X.ca_field = N.FieldName AND X.ca_filler1 = N.MasterJsonTag and DefaultValuetag in('B','U')
	 and TemplateCode = @i_vcTemplateCode and N.FieldName <> '') x1
	 where x1.TradePlusTableName in('Client_master','Client_details','Client_info') AND ca_field <> 'cm_cd'
	 ORDER BY TradePlusTableName
     OPEN Cur1 
     FETCH NEXT FROM Cur1 INTO @TradePlusTableName, @ComexTableName, @ca_field, @ca_oldValue, @ca_newValue, @ca_computername, @mkrdt,
	 @ca_Time, @ca_KeyFieldDescp, @ca_keyfield
     WHILE @@FETCH_STATUS = 0
     BEGIN 
	    SET @strAuditStringDetail = ''
	    IF @TradePlusTableName = 'Client_master'
		AND EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TradePlusTableName and COLUMN_NAME = @ca_field)
		BEGIN
		  SELECT @strFieldLeg = CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TradePlusTableName 
		  AND COLUMN_NAME = @ca_field
		  SET @strClientMasterString = @strClientMasterString +' , '+@ca_field+' = RIGHT('''+@ca_newValue+''','+CAST(@strFieldLeg AS VARCHAR)+') ' 
        END
        IF @TradePlusTableName = 'Client_info'
		AND EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Client_Info' and COLUMN_NAME = @ca_field)
		BEGIN
		  SET @strClient_InfoString = @strClient_InfoString +' , '+@ca_field+' = '''+@ca_newValue+'''' 
        end
		IF @TradePlusTableName = 'Client_details'
		AND EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Client_details' and COLUMN_NAME = @ca_field)
		BEGIN
		  SET @strSegmentOldValue =  @ca_oldValue
		  set @strSegmentNewValue = @ca_newValue
        END
		
		SET @strAuditStringDetail = ''''+@TradePlusTableName+''', '' '' ,'''+@ca_keyfield+''','''+@i_vcClient_Code+''','''+@ca_field+''','''+@ca_KeyFieldDescp+''','''+@ca_oldValue+''','''+@ca_newvalue+''','''+@ca_computername
		+''','''+@i_vcClient_Code+''','''+@mkrdt+''','''+@ca_Time+''',replace('''+@TradePlusTableName+''',''_'','' ''),''Client Code'','''','''','''' ) '
		INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		VALUES('Common_audit',@strAuditString+' '+@strAuditStringDetail, '')
	    FETCH NEXT FROM Cur1 INTO @TradePlusTableName, @ComexTableName, @ca_field, @ca_oldValue, @ca_newValue,  @ca_computername, @mkrdt, @ca_Time, @ca_KeyFieldDescp,
		@ca_keyfield
    END 
    CLOSE Cur1 
    DEALLOCATE Cur1 
    
    IF @strClientMasterString <> ''
	BEGIN
	  SET @strClientMasterString = SUBSTRING(@strClientMasterString,3,LEN(@strClientMasterString))
	  SET @strClientMasterString = 'UPDATE Client_master SET '+@strClientMasterString+' WHERE cm_cd = '''+@strClientCode+''''
	  BEGIN TRY
	    IF ((@STRClosureType IN('T','B') AND @i_vcTemplateCode = 'ONLYCLOSURE') OR @i_vcTemplateCode <> 'ONLYCLOSURE')
		BEGIN
	      INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		  VALUES('Client_master', @strClientMasterString, @strClientCode)
		END   
      END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'ClientMaster ' + ERROR_MESSAGE()
		RETURN 1
	  END CATCH
    END
	
	IF @strClient_InfoString <> ''
	BEGIN
	  SET @strClient_InfoString = SUBSTRING(@strClient_InfoString,3,LEN(@strClient_InfoString))
	  SET @strClient_InfoString = 'UPDATE Client_Info SET '+@strClient_InfoString+' WHERE cm2_cd = '''+@strClientCode+''''
	  BEGIN TRY
	    INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		VALUES('Client_master',@strClient_InfoString, @strClientCode)
      END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Client_Info '+ERROR_MESSAGE()
		RETURN 1
	  END CATCH
    END
	
	--- COMMON CONTACT
	
	BEGIN TRY
    DECLARE @strNewMobileNo VARCHAR(20)='', @strEmailid VARCHAR(100)='', @strMobileRelation VARCHAR(50)
	SELECT @strNewMobileNo = ca_newValue FROM Client_ModifyAPI(NOLOCK)  
	WHERE ca_cmcd = @i_vcClient_Code AND ca_Nfiller3 = @i_vcRefNo
	AND ca_field = 'cm_mobile' AND ca_oldValue <> ca_newValue
	
	SELECT @strMobileRelation = ca_newValue FROM Client_ModifyAPI(NOLOCK)  
	WHERE ca_cmcd = @i_vcClient_Code AND ca_Nfiller3 = @i_vcRefNo
	AND ca_field = 'cc_Relationmobile' AND ca_oldValue <> ca_newValue

	IF @strNewMobileNo <> ''
	BEGIN
	  IF EXISTS(SELECT 1 FROM Common_Contacts(NOLOCK) WHERE cc_Client = @i_vcClient_Code AND cc_type = 'M')
	  BEGIN
		SET @String = ' UPDATE A SET A.cc_Contact = '''+@strNewMobileNo+''', A.cc_Date = CONVERT(VARCHAR,GETDATE(),112), '
		+' cc_Relation = '''+@strMobileRelation +''''
		+' FROM Common_Contacts A '
		+' WHERE cc_Client = '''+@i_vcClient_Code+''' AND cc_type = ''M'' '
	  BEGIN TRY
	    INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		VALUES('Common_Contacts',@String, @i_vcClient_Code+'|'+'M')
      END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Client_Info '+ERROR_MESSAGE()
		RETURN 1
	  END CATCH
	  END
	  ELSE
	  BEGIN
		SET @String = ' INSERT INTO Common_Contacts(cc_type, cc_Contact, cc_Client, cc_Date, cc_Relation, cc_mkdt, cc_RefNo, '
		+' Cc_Modification, cc_Filler1, cc_Filler2, cc_Filler3, cc_Filler4, cc_mkrid) '
		+' VALUES(''M'','''+@strNewMobileNo+''', '''+@i_vcClient_Code+''', CONVERT(VARCHAR,GETDATE(),112), '''+@strMobileRelation+''',  '
		+' CONVERT(VARCHAR,GETDATE(),112),'''','''','''','''','''','''','''+@i_vcClient_Code+''') '
	  BEGIN TRY
	    INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		VALUES('Common_Contacts',@String, @i_vcClient_Code+'|'+'M')
      END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Client_Info '+ERROR_MESSAGE()
		RETURN 1
	  END CATCH
	  END
    END 	  
	DECLARE @strEmailRelation VARCHAR(50)=''
	SELECT @strEmailid = ca_newValue FROM Client_ModifyAPI(NOLOCK)  
	WHERE ca_cmcd = @i_vcClient_Code AND ca_Nfiller3 = @i_vcRefNo
	AND ca_field = 'cm_email' AND ca_oldValue <> ca_newValue
	
    SELECT @strEmailRelation = ca_newValue FROM Client_ModifyAPI(NOLOCK)  
	WHERE ca_cmcd = @i_vcClient_Code AND ca_Nfiller3 = @i_vcRefNo
	AND ca_field = 'cc_RelationEmail' AND ca_oldValue <> ca_newValue
	
	IF @strEmailid <> ''
	BEGIN
	  IF EXISTS(SELECT 1 FROM Common_Contacts(NOLOCK) WHERE cc_Client = @i_vcClient_Code AND cc_type = 'E')
	  BEGIN
		SET @String = ' UPDATE A SET A.cc_Contact = '''+@strEmailid+''', A.cc_Date = CONVERT(VARCHAR,GETDATE(),112), '
		+' cc_Relation = '''+@strEmailRelation+''' '
		+' FROM Common_Contacts A '
		+' WHERE cc_Client = '''+@i_vcClient_Code+''' AND cc_type = ''E'' '
	  BEGIN TRY
	    INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute,PrimaryKey)
		VALUES('Common_Contacts',@String, @i_vcClient_Code+'|'+'E')
      END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Client_Info '+ERROR_MESSAGE()
		RETURN 1
	  END CATCH
	  END
	  ELSE
	  BEGIN
		SET @String = ' INSERT INTO Common_Contacts(cc_type, cc_Contact, cc_Client, cc_Date, cc_Relation, cc_mkdt, cc_RefNo,  '
		+' Cc_Modification, cc_Filler1, cc_Filler2, cc_Filler3, cc_Filler4, cc_mkrid) '
		+' VALUES(''E'','''+@strEmailid+''', '''+@i_vcClient_Code+''', CONVERT(VARCHAR,GETDATE(),112), '''+@strEmailRelation+''',  '
		+' CONVERT(VARCHAR,GETDATE(),112),'''','''','''','''','''','''','''+@i_vcClient_Code+''') '
	  BEGIN TRY
	    INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		VALUES('Common_Contacts',@String, @i_vcClient_Code+'|'+'E')
      END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Client_Info '+ERROR_MESSAGE()
		RETURN 1
	  END CATCH	
	  END
	END
	END TRY
	BEGIN CATCH
	  SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = 'Common Contacts '+ERROR_MESSAGE()
	  RETURN 1
	END CATCH

	---- SEGMENT UPDATE
	
	DECLARE @ReKYCFlag VARCHAR(1)=''
	SELECT @ReKYCFlag = ca_newValue FROM Client_ModifyAPI(NOLOCK)  
	WHERE ca_cmcd = @i_vcClient_Code AND ca_Nfiller3 = @i_vcRefNo
	AND ca_field = 'cm_ReKYC' AND ca_oldValue <> ca_newValue
	
	IF @strSegmentNewValue <> '' 
	AND ((@ReKYCFlag = 'Y' AND @i_vcTemplateCode = 'Template2') or @i_vcTemplateCode <> 'Template2')
	BEGIN
	  SET @string = ' UPDATE a set a.ce_regDt = convert(varchar,getdate(),112)'
	  +' FROM Client_details a, (SELECT VALUE AS Segment FROM dbo.returntable('''+@strSegmentNewValue+''','','')) b '
	  +' where a.ce_clientcd = '''+@i_vcClient_Code+''' '
	  +' and a.ce_companycode = b.Segment '

	  +' INSERT INTO Client_details(ce_companycode, ce_clientcd, ce_regDt, ce_remissier2scheme, ce_brkscheme) '
	  +' SELECT Segment, '''+@i_vcClient_Code+''', convert(varchar,getdate(),112), ce_remissier2scheme ='''', ce_brkscheme='''' '
	  +' FROM(SELECT VALUE AS Segment FROM dbo.returntable('''+@strSegmentNewValue+''','','')) X1 '
	  +' where x1.Segment not in(Select ce_companycode from Client_details(NOLOCK) '
	  +' WHERE ce_clientcd = '''+@i_vcClient_Code+''' ) '
	  +' AND ((EXISTS(SELECT 1 FROM CompanyExchangeSegments(NOLOCK)  '
	  +' WHERE CES_CD = X1.Segment) AND   X1.Segment <> ''BNS'') OR X1.Segment = ''BNS'')'
	  BEGIN TRY
	    INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		VALUES('Client_details',@String,'')
	    --SELECT (@string)
      END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Segment in Commex DB '+ERROR_MESSAGE()
		RETURN 1
	  END CATCH 

      SET @string = ' UPDATE a set a.ce_regDt = '''''
	  +' FROM Client_details a ,(SELECT SEGMENT FROM( '
	  +' SELECT VALUE AS Segment FROM dbo.returntable('''+@strSegmentOldValue+''','','')) OLD1  '
	  +' WHERE NOT EXISTS(SELECT 1 FROM( '
	  +' SELECT VALUE AS Segment FROM dbo.returntable('''+@strSegmentNewValue+''','','')) X11 WHERE X11.Segment = OLD1.Segment) ) B '
	  +' WHERE a.ce_clientcd = '''+@i_vcClient_Code+''''
	  +' AND  a.ce_companycode = B.SEGMENT '
	  BEGIN TRY
	    INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		VALUES('Client_details',@String,'')
	    --SELECT (@string)
      END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Segment in Commex DB '+ERROR_MESSAGE()
		RETURN 1
	  END CATCH 
	  
	END
    
	
    DECLARE @comexDB VARCHAR(100)='', @comexOwner VARCHAR(50)=''
	SELECT @comexDB = LTRIM(RTRIM(OP_DataBase)),  @comexOwner = LTRIM(RTRIM(OP_Owner))
    FROM Other_Products(NOLOCK) WHERE OP_Product = 'Commex' AND ISNULL(OP_Status,'')='A'
	
	IF ISNULL(@comexDB,'') <> '' AND  @strSegmentNewValue <> ''
	AND ((@ReKYCFlag = 'Y' AND @i_vcTemplateCode = 'Template2') or @i_vcTemplateCode <> 'Template2')
	BEGIN
	  SET @string = ' UPDATE a set a.ce_regDt = convert(varchar,getdate(),112)
	  FROM '+@comexDB+'.'+@comexOwner+'.Client_details a, (SELECT VALUE AS Segment FROM dbo.returntable('''+@strSegmentNewValue+''','','')) b
	  where a.ce_clientcd = '''+@i_vcClient_Code+'''
	  and a.ce_companycode = b.Segment

	  INSERT INTO '+@comexDB+'.'+@comexOwner+'.Client_details(ce_companycode, ce_clientcd, ce_regDt, ce_remissierscheme, ce_brkscheme)
	  SELECT Segment, '''+@i_vcClient_Code+''', convert(varchar,getdate(),112),ce_remissier2scheme ='''', ce_brkscheme ='''' 
	  FROM(SELECT VALUE AS Segment FROM dbo.returntable('''+@strSegmentNewValue+''','','')) X1
	  where x1.Segment not in(Select ce_companycode from '+@comexDB+'.'+@comexOwner+'.Client_details(NOLOCK)
	  WHERE ce_clientcd = '''+@i_vcClient_Code+''') 
	  AND EXISTS(SELECT 1 FROM '+@comexDB+'.'+@comexOwner+'.CompanyExchangeSegments(NOLOCK) 
	  WHERE CES_CD =  X1.Segment) '
	  BEGIN TRY
	    INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		VALUES(@comexDB+'.'+@comexOwner+'.'+'Client_details',@String, '')
      END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Segment in Commex DB '+ERROR_MESSAGE()
		RETURN 1
	  END CATCH 
	  SET @string = ' UPDATE a set a.ce_regDt = '''''
	  +' FROM '+@comexDB+'.'+@comexOwner+'.Client_details a ,(SELECT SEGMENT FROM( '
	  +' SELECT VALUE AS Segment FROM dbo.returntable('''+@strSegmentOldValue+''','','')) OLD1  '
	  +' WHERE NOT EXISTS(SELECT 1 FROM( '
	  +' SELECT VALUE AS Segment FROM dbo.returntable('''+@strSegmentNewValue+''','','')) X11 WHERE X11.Segment = OLD1.Segment) ) B '
	  +' WHERE a.ce_clientcd = '''+@i_vcClient_Code+''''
	  +' AND  a.ce_companycode = B.SEGMENT '
	  BEGIN TRY
	    INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		VALUES(@comexDB+'.'+@comexOwner+'.Client_details',@String, '')
	    --SELECT (@string)
      END TRY
	  BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Segment in Commex DB '+ERROR_MESSAGE()
		RETURN 1
	  END CATCH 
	END  
	
	
	IF EXISTS(SELECT 1 FROM Client_ModifyAPI(NOLOCK) WHERE CA_CMCD = @i_vcClient_Code AND ca_Nfiller3 = @i_vcRefNo
        AND ca_filler1 in('NomineeDetails','GuardianDetails'))
	BEGIN
	  INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute)
	  VALUES('Client_NomineeDetails','DELETE FROM Client_NomineeDetails WHERE cn_Cmcd = '''+@i_vcClient_Code+'''')	 
    END	
	SET @ca_KeyFieldDescp = ''
	declare @StrUniqueBankKey VARCHAR(MAX)='', @StrUniqueDematKey VARCHAR(MAX)='',
	@strDematactUpdateString VARCHAR(MAX)='', @strBankactUpdateString  VARCHAR(MAX)='', @StrUniqueBankKey1 varchar(max)='',
	@StrUniqueDematKey1 VARCHAR(MAX)='', @ca_filler3 varchar(1), @strDPName VARCHAR(100)='', @NomName VARCHAR(100) = '',
	@StrUniqueBankKeyX VARCHAR(MAX)='', @StrUniqueDPKeyX VARCHAR(MAX)='', @strMTFColumnString VARCHAR(MAX)='',
	@strMTFvalueString VARCHAR(MAX)=''
	
	DECLARE Cur2Main
	 CURSOR FOR 
	 SELECT * FROM (select distinct TableName as TradePlusTableName , ca_Nfiller1, ca_filler3
	 FROM Client_ModifyAPI(NOLOCK) x, tbl_ReKycAuditColumnMapping_new(nolock) N  
	 WHERE ca_cmcd = @i_vcClient_Code AND ca_Nfiller3 = @i_vcRefNo
	 AND X.ca_field = N.FieldName AND X.ca_filler1 = N.MasterJsonTag and TemplateCode = @i_vcTemplateCode
	 ) X11
	 WHERE TradePlusTableName NOT in('Client_master','Client_details','Client_info')
	 
	 OPEN Cur2Main 
     FETCH NEXT FROM Cur2Main INTO @MainTradePlusTableName, @ca_Nfiller1, @ca_filler3
     WHILE @@FETCH_STATUS = 0
     BEGIN 
	   SET @strDematactColumnString = ''
	   SET @strDematactvalueString = ''
	   SET @strBankactColumnString = ''
	   SET @strBankactvalueString = ''
	   SET @strClient_NomineeColumnString = ''
	   SET @strClient_NomineevalueString = ''
	   set @StrUniqueDematKey = ''
	   set @StrUniqueBankKey = ''
	   set @strDematactUpdateString = ''
	   set @strBankactUpdateString = ''
	   set @StrUniqueBankKey1 = ''
	   set @StrUniqueDematKey1 = ''
	   set @StrUniqueBankKeyX = ''
	   set @StrUniqueDPKeyX = ''
       DECLARE Cur2 CURSOR FOR 
        select X1.*, ca_keyfield =  (case when TradePlusTableName = 'Client_master' then 'cm_cd'
	    when TradePlusTableName = 'Client_NomineeDetails' then 'cn_Cmcd'
	    when TradePlusTableName = 'Common_Contacts' then 'cc_Client'
	    when TradePlusTableName = 'Dematact' then 'da_clientcd'
	    when TradePlusTableName = 'Client_details' then 'ce_clientcd'
	    when TradePlusTableName = 'Bankact' then 'ba_clientcd' 
		when TradePlusTableName = 'MrgTdgFin_Clients' then 'MTFC_CMCD' else 'cm_cd' end)  from (
	    SELECT tablename as TradePlusTableName, 
	    '' as ComexTableName,
	    ca_field, ca_oldValue, ca_newValue , ca_Nfiller1, ca_computername  , ca_date, ca_Time ,
        FieldDescp = IIF(N.FieldDescp='', ca_field,N.FieldDescp)   		
	    FROM Client_ModifyAPI(NOLOCK) x, tbl_ReKycAuditColumnMapping_new(nolock) n 
		WHERE ca_cmcd = @i_vcClient_Code AND ca_Nfiller3 = @i_vcRefNo
		AND X.ca_field = N.FieldName AND X.ca_filler1 = N.MasterJsonTag AND X.ca_filler3 = @ca_filler3 and DefaultValuetag in('B','U')
		and (((TemplateCode = 'Template2' and (FieldDescp = 'NomineeDetails('+cast(@ca_Nfiller1 as varchar)+')'
		or FieldDescp = 'GuardianDetails('+cast(@ca_Nfiller1 as varchar)+')') and n.TableName = 'Client_NomineeDetails')
		or n.TableName <> 'Client_NomineeDetails')
		or TemplateCode <> 'Template2')
		and TemplateCode = @i_vcTemplateCode  and FieldName <> '') x1
	    where x1.TradePlusTableName = @MainTradePlusTableName
		AND X1.ca_Nfiller1 = @ca_Nfiller1
	    ORDER BY TradePlusTableName
        OPEN Cur2 
        FETCH NEXT FROM Cur2 INTO @TradePlusTableName, @ComexTableName, @ca_field, @ca_oldValue, @ca_newValue, @C1ca_Nfiller1,  @ca_computername, @mkrdt, @ca_Time, @ca_KeyFieldDescp, @ca_keyfield
		
        WHILE @@FETCH_STATUS = 0
        BEGIN 
		   SET @strAuditStringDetail = ''
	       IF @TradePlusTableName = 'Dematact' 
		   AND EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TradePlusTableName and COLUMN_NAME = @ca_field) 
		  BEGIN
		    IF @ca_oldValue = ''
			begin
		      SET @strDematactColumnString = @strDematactColumnString +', '+@ca_field
		      SET @strDematactvalueString = @strDematactvalueString +' , '''+@ca_newValue+'''' 
			END
			ELSE
		    IF @ca_oldValue <> ''
			BEGIN
			  IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Dematact' and COLUMN_NAME = @ca_field)
			  and @ca_newValue <> @ca_oldValue
		      BEGIN
		        SET @strDematactUpdateString = @strDematactUpdateString +' , '+@ca_field+' = '''+@ca_newValue+'''' 
              end
			  
			  IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Dematact' and COLUMN_NAME = @ca_field)
			  AND  @i_vcTemplateCode = 'Template2'
			  BEGIN
			    SET @strDematactColumnString = @strDematactColumnString +', '+@ca_field
		        SET @strDematactvalueString = @strDematactvalueString +' , '''+@ca_newValue+'''' 
			  END  
			END
			IF @ca_field IN('da_dpid','da_actno')
			BEGIN
			  IF @ca_field = 'da_dpid'
			  BEGIN
			    set @strDPName = ''
			    SELECT @strDPName = DP_NAME FROM DPS WHERE dp_dpid = @ca_newValue
			  END
			  
			  SET @StrUniqueDematKey = @StrUniqueDematKey +'|'+@ca_oldValue
			  SET @StrUniqueDematKey1 = @StrUniqueDematKey1+'+'+'''|'''+'+LTRIM(RTRIM('+@ca_field+'))'
			  SET @StrUniqueDpKeyx = @StrUniqueDpKeyx +'|'+@ca_NewValue
			END  
          END
		  IF @TradePlusTableName = 'Bankact' 
		  AND EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TradePlusTableName and COLUMN_NAME = @ca_field)
		  BEGIN
		    IF @ca_oldValue = ''
			BEGIN
		      SET @strBankactColumnString = @strBankactColumnString +', '+@ca_field
		      SET @strBankactvalueString = @strBankactvalueString +' , '''+@ca_newValue+'''' 
			END
            ELSE IF @ca_oldValue <> ''
            BEGIN			
			  IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Bankact' and COLUMN_NAME = @ca_field)
			  AND @ca_newValue <> @ca_oldValue
		      BEGIN
			    SET @strBankactUpdateString = @strBankactUpdateString +' , '+@ca_field+' = '''+@ca_newValue+'''' 
			  END 	
			  
			  IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Bankact' and COLUMN_NAME = @ca_field)
			  AND  @i_vcTemplateCode = 'Template2'
			   BEGIN
				SET @strBankactColumnString = @strBankactColumnString +', '+@ca_field
		        SET @strBankactvalueString = @strBankactvalueString +' , '''+@ca_newValue+'''' 
			  END
			END
			
			IF @ca_field IN('ba_micr', 'ba_acttype', 'ba_actno', 'ba_ifsccode')
			BEGIN
			  SET @StrUniqueBankKey = @StrUniqueBankKey +'|'+@ca_oldValue
			  SET @StrUniqueBankKey1 = @StrUniqueBankKey1+'+'+'''|'''+'+LTRIM(RTRIM('+@ca_field+'))'
			  SET @StrUniqueBankKeyX = @StrUniqueBankKeyX +'|'+@ca_NewValue
			END  
          END
		  
		  IF @TradePlusTableName = 'Client_NomineeDetails'
		  AND EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TradePlusTableName and COLUMN_NAME = @ca_field)
		  BEGIN
		    IF @ca_field = 'cn_Relation'
			BEGIN
			  SELECT @ca_newValue = (CASE WHEN @ca_newValue = '01' THEN 'Spouse' 
			  WHEN @ca_newValue = '02' THEN 'Son'
			  WHEN @ca_newValue = '03' THEN 'Daughter'
			  WHEN @ca_newValue = '04' THEN 'Father'
			  WHEN @ca_newValue = '05' THEN 'Mother'
			  WHEN @ca_newValue = '06' THEN 'Brother'
			  WHEN @ca_newValue = '07' THEN 'Sister'
			  WHEN @ca_newValue = '08' THEN 'Grandson'
			  WHEN @ca_newValue = '09' THEN 'Granddaughter'
			  WHEN @ca_newValue = '10' THEN 'Grandfather'
			  WHEN @ca_newValue = '11' THEN 'Grandmother'
			  WHEN @ca_newValue = '12' THEN 'Not Provided'
			  WHEN @ca_newValue = '13' THEN 'Others' ELSE @ca_newValue END)
			END
		    SET @strClient_NomineeColumnString = @strClient_NomineeColumnString +', '+@ca_field
		    SET @strClient_NomineevalueString = @strClient_NomineevalueString +' , '''+@ca_newValue+'''' 
		  END
          
		  IF @TradePlusTableName = 'MrgTdgFin_Clients'
		  AND EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TradePlusTableName and COLUMN_NAME = @ca_field)
		  BEGIN		
		    IF @ca_field = 'MTFC_IntRate' AND CAST(ISNULL(@ca_newValue,'0.00') AS MONEY) = 0
			BEGIN
			  SELECT @ca_newValue = sp_sysvalue 
			  FROM Sysparameter(NOLOCK) WHERE sp_parmcd = 'MTFP_RTOFINT'
		    END
			IF @ca_field = 'MTFC_Frequency' AND ISNULL(@ca_newValue,'') = ''
			BEGIN
			  SELECT @ca_newValue = sp_sysvalue 
			  FROM Sysparameter(NOLOCK) WHERE sp_parmcd = 'MTFP_INTFRQ'
		    END
		    SET @strMTFColumnString = @strMTFColumnString +', '+@ca_field
		    SET @strMTFvalueString = @strMTFvalueString +' , '''+@ca_newValue+''''  
		  END
		
		  IF @ca_newValue <> @ca_oldValue
		  BEGIN
		    SET @strAuditStringDetail = ''''+SUBSTRING(@TradePlusTableName,1,20)+''', '' '' ,'''+@ca_keyfield+''','''+@i_vcClient_Code+''','''+@ca_field+''','''+@ca_KeyFieldDescp+''','''+@ca_oldValue+''','''+@ca_newvalue+''','''+@ca_computername
 		    +''','''+@i_vcClient_Code+''','''+@mkrdt+''','''+@ca_Time+''',replace('''+SUBSTRING(@TradePlusTableName,1,20)+''',''_'','' ''),''Client Code'','''','''','''' ) '
		    INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute)
		    VALUES('Common_audit',@strAuditString+' '+@strAuditStringDetail)
		  END	
	    
		  FETCH NEXT FROM Cur2 INTO @TradePlusTableName, @ComexTableName, @ca_field, @ca_oldValue, @ca_newValue, @C1ca_Nfiller1,  @ca_computername, @mkrdt, @ca_Time, @ca_KeyFieldDescp, @ca_keyfield
        END
       CLOSE Cur2
       DEALLOCATE Cur2
	   
	   DECLARE @MTFString VARCHAR(MAX)=''
	   SET @MTFString = 'SELECT COUNT(*) FROM MrgTdgFin_Clients(NOLOCK) WHERE MTFC_CMCD = '''+@strClientCode+''''
	   DECLARE @tbl_MrgTdgFin_Clients TABLE(Clientexists INT)
	   BEGIN TRY
	     INSERT INTO @tbl_MrgTdgFin_Clients(Clientexists)
	     EXEC(@MTFString) 
	   END TRY
       BEGIN CATCH
	     INSERT INTO @tbl_MrgTdgFin_Clients(Clientexists) VALUES(0)
       END CATCH 	   
	   
	   IF @strMTFvalueString <> '' AND NOT EXISTS(SELECT 1 FROM @tbl_MrgTdgFin_Clients WHERE ISNULL(Clientexists,0) > 0)
       BEGIN
	     DECLARE @mtfCustomerName VARCHAR(100)='', @mtfColumnPrefix VARCHAR(1)='L'
	     SELECT @mtfCustomerName = cm_Name From Client_Master(NOLOCK) WHERE CM_cD = @strClientCode
	     
		 SELECT @mtfColumnPrefix = sp_sysvalue FROM Sysparameter WHERE sp_parmcd = 'MTFP_DRINTTO'
		 
	     SET @strMTFColumnString = SUBSTRING(@strMTFColumnString,3,LEN(@strMTFColumnString))
	     SET @strMTFvalueString = SUBSTRING(@strMTFvalueString,3,LEN(@strMTFvalueString))
		 SET @strMTFColumnString = @strMTFColumnString+', '
		 +'MTFC_CMCD, MTFC_CMName, MTFC_RegDt, MTFC_FillerA, MTFC_FillerB, MTFC_FillerC, MTFC_FillerD, MTFC_FillerE, MTFC_Filler1, 
		 MTFC_Filler2, MTFC_Filler3, MTFC_Filler4, MTFC_Filler5, MTFC_MKRDT, MTFC_MKRID '
		 SET @strMTFvalueString = @strMTFvalueString+', '''+@strClientCode+''', '''+@mtfCustomerName+''', CONVERT(VARCHAR,GETDATE(),112), '''', '''+@strClientCode+@mtfColumnPrefix+''',
         '''','''','''',0,0,0,0,0,CONVERT(VARCHAR,GETDATE(),112), '''+@strClientCode+''' '
		 
	     SET @strMTFvalueString = 'INSERT INTO '+@MainTradePlusTableName+'  ('+ @strMTFColumnString +' ) VALUES (' +@strMTFvalueString+' )'	 
		 
	     BEGIN TRY
	       INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		   VALUES('MTF',@strMTFvalueString, @StrUniqueDematKey)
         END TRY
	     BEGIN CATCH
	        SET @o_vcErrorFlag = 'E'
	        SET @o_vcErrorMessage = 'MTF '+ERROR_MESSAGE()
		    RETURN 1
	     END CATCH
       END	   
	   
       IF @strDematactvalueString <> ''
	   BEGIN
	     
         SET @strDematactvalueString = SUBSTRING(@strDematactvalueString,3,LEN(@strDematactvalueString))
	     SET @strDematactColumnString = SUBSTRING(@strDematactColumnString,3,LEN(@strDematactColumnString))
	     SET @strDematactColumnString = @strDematactColumnString+', '+'da_clientcd, Mkrdt, Mkrid, da_proof, da_status, da_name  '
		 IF CHARINDEX('da_defaultyn',@strDematactColumnString) = 0
		 BEGIN
		   SET @strDematactColumnString = @strDematactColumnString+','+'da_defaultyn '
		   set @strDematactvalueString = @strDematactvalueString+', '''+@strClientCode+''','''+convert(varchar,getdate(),112)+''', '''+@strClientCode+''',''Y'',''A'','''+@strDPName+''',''Y'' '
		 END
		 ELSE
		 BEGIN
	       SET @strDematactvalueString = @strDematactvalueString+', '''+@strClientCode+''','''+convert(varchar,getdate(),112)+''', '''+@strClientCode+''',''Y'',''A'','''+@strDPName+''' '
		 END
		 
	     SET @strDematactvalueString = 'INSERT INTO '+@MainTradePlusTableName+'  ('+ @strDematactColumnString +' ) VALUES (' +@strDematactvalueString+' )'	 
		 
	     BEGIN TRY
	       INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		   VALUES('Dematact',@strDematactvalueString, @StrUniqueDematKey)
         END TRY
	     BEGIN CATCH
	        SET @o_vcErrorFlag = 'E'
	        SET @o_vcErrorMessage = 'Dematact '+ERROR_MESSAGE()
		    RETURN 1
	     END CATCH
       END
	   
	   IF @strDematactUpdateString <> ''
	   BEGIN
	    SET @strDematactUpdateString = SUBSTRING(@strDematactUpdateString,3,LEN(@strDematactUpdateString))
		 SET @StrUniqueDematKey = SUBSTRING(@StrUniqueDematKey,2,LEN(@StrUniqueDematKey))
		 SET @StrUniqueDematKey1 = SUBSTRING(@StrUniqueDematKey1,6,LEN(@StrUniqueDematKey1))
		 IF @i_vcTemplateCode <> 'Template2'
		 BEGIN
	       SET @strDematactUpdateString = 'UPDATE '+@MainTradePlusTableName+' SET '+@strDematactUpdateString+' WHERE da_clientcd = '''+@strClientCode+''''
           +'and '+@StrUniqueDematKey1+' = '''+@StrUniqueDematKey+''''
		 END
         ELSE
		 BEGIN
		   SET @strDematactUpdateString = 'UPDATE '+@MainTradePlusTableName+' SET da_defaultyn = ''N'' WHERE da_clientcd = '''+@strClientCode+''''
           +'and '+@StrUniqueDematKey1+' = '''+@StrUniqueDematKey+''''
		 END
		 
	     BEGIN TRY
	      INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		  VALUES('Dematact',@strDematactUpdateString, @StrUniqueDematKey)
         END TRY
	     BEGIN CATCH
	      SET @o_vcErrorFlag = 'E'
	      SET @o_vcErrorMessage = 'Demat Update '+ERROR_MESSAGE()
		  RETURN 1
	     END CATCH
	   END
	   
	   IF @strBankactvalueString <> ''
	   BEGIN
	     SET @strBankactvalueString = SUBSTRING(@strBankactvalueString,3,LEN(@strBankactvalueString))
	     SET @strBankactColumnString = SUBSTRING(@strBankactColumnString,3,LEN(@strBankactColumnString))
	     SET @strBankactColumnString = @strBankactColumnString+', '+'ba_clientcd, Mkrdt, Mkrid, ba_proof '
		 
		 IF CHARINDEX('ba_default',@strBankactColumnString) = 0 
		 BEGIN
		   SET @strBankactColumnString = @strBankactColumnString+', '+'ba_default '  
		   set @strBankactvalueString = @strBankactvalueString+', '''+@strClientCode+''','''+convert(varchar,getdate(),112)+''', '''+@strClientCode+''',''Y'',''Y'''
		 END
		 ELSE
		 BEGIN
		   SET @strBankactvalueString = @strBankactvalueString+', '''+@strClientCode+''','''+convert(varchar,getdate(),112)+''', '''+@strClientCode+''',''Y'''
		 END
	     SET @strBankactvalueString = 'INSERT INTO  '+@MainTradePlusTableName+' ('+ @strBankactColumnString +' ) VALUES (' +@strBankactvalueString+' )'	 
	     BEGIN TRY
	        INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		    VALUES('Bankact',@strBankactvalueString, @StrUniqueBankKey)
         END TRY
	     BEGIN CATCH
	       SET @o_vcErrorFlag = 'E'
	       SET @o_vcErrorMessage = 'Bankact '+ERROR_MESSAGE()
		   RETURN 1
	     END CATCH
      END
	  
      IF @strBankactUpdateString <> ''
	   BEGIN
	     SET @strBankactUpdateString = SUBSTRING(@strBankactUpdateString,3,LEN(@strBankactUpdateString))
		 SET @StrUniqueBankKey = SUBSTRING(@StrUniqueBankKey,2,LEN(@StrUniqueBankKey))
		 SET @StrUniqueBankKey1 = SUBSTRING(@StrUniqueBankKey1,6,LEN(@StrUniqueBankKey1))
		 
		 IF @i_vcTemplateCode <> 'Template2'
		 BEGIN
		   SET @strBankactUpdateString = 'UPDATE '+@MainTradePlusTableName+' SET '+@strBankactUpdateString+' WHERE ba_clientcd = '''+@strClientCode+''''
           +'and '+@StrUniqueBankKey1+' = '''+@StrUniqueBankKey+'''' 
		 END
		 ELSE
		 BEGIN
	       SET @strBankactUpdateString = 'UPDATE '+@MainTradePlusTableName+' SET ba_default = ''N'' WHERE ba_clientcd = '''+@strClientCode+''''
           +'and '+@StrUniqueBankKey1+' = '''+@StrUniqueBankKey+'''' 
		 END  
	     BEGIN TRY
	      INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute, PrimaryKey)
		  VALUES('Bankact',@strBankactUpdateString, @StrUniqueBankKey)
         END TRY
	     BEGIN CATCH
	      SET @o_vcErrorFlag = 'E'
	      SET @o_vcErrorMessage = 'Bank Update '+ERROR_MESSAGE()
		  RETURN 1
	     END CATCH
	   END	  
      
	  IF @strClient_NomineevalueString <> ''
	  BEGIN
	    SET @strClient_NomineevalueString = SUBSTRING(@strClient_NomineevalueString,3,LEN(@strClient_NomineevalueString))
	    SET @strClient_NomineeColumnString = SUBSTRING(@strClient_NomineeColumnString,3,LEN(@strClient_NomineeColumnString))
		
		SET @NomName = ''
        SELECT @NomName = ca_newValue 
        FROM Client_ModifyAPI(NOLOCK) WHERE CA_CMCD = @i_vcClient_Code AND ca_Nfiller3 = @i_vcRefNo  AND ca_filler3 = @ca_filler3
        AND ca_field LIKE  'cn_firstname%'
          
		SELECT @NomName = @NomName+' '+ca_newValue 
        FROM Client_ModifyAPI(NOLOCK) WHERE CA_CMCD = @i_vcClient_Code AND ca_Nfiller3 = @i_vcRefNo 
        and ca_filler3 = @ca_filler3
        and ca_field LIKE 'cn_middlename%'

        SELECT @NomName = @NomName+' '+ca_newValue 
        FROM Client_ModifyAPI(NOLOCK) WHERE CA_CMCD= @i_vcClient_Code AND ca_Nfiller3 = @i_vcRefNo 
        and ca_filler3  = @ca_filler3
        and ca_field LIKE 'cn_lastname%'
		
		declare @sColumn_Name VARCHAR(100)='', @sdata_type VARCHAR(20)=''
		DECLARE Cur31Main
        CURSOR FOR SELECT Column_Name, data_type FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Client_NomineeDetails'
        and column_name not in(select ca_field from Client_ModifyAPI(NOLOCK) WHERE CA_CMCD = @i_vcClient_Code AND ca_Nfiller3 = @i_vcRefNo  
		AND ca_Nfiller1 = @ca_Nfiller1 AND ca_filler3 = @ca_filler3)
        and column_name not in('cn_Cmcd', 'cn_MkrDt', 'cn_MkrId', 'cn_Srno', 'cn_Name')
        OPEN Cur31Main 
        FETCH NEXT FROM Cur31Main INTO @sColumn_Name, @sdata_type  
        WHILE @@FETCH_STATUS = 0
        BEGIN 
         IF EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Client_NomineeDetails' and COLUMN_NAME = @sColumn_Name) 
         BEGIN
           SET @strClient_NomineeColumnString = @strClient_NomineeColumnString +', '+@sColumn_Name
           SET @strClient_NomineevalueString = @strClient_NomineevalueString +' , '''+CAST((CASE WHEN @sdata_type IN('int','money','numeric') THEN '0' ELSE '' END) AS VARCHAR)+'''' 
         END
        FETCH NEXT FROM Cur31Main INTO @sColumn_Name, @sdata_type  
        END
        CLOSE Cur31Main
        DEALLOCATE Cur31Main
		
		SET @strClient_NomineeColumnString = @strClient_NomineeColumnString+', '+'cn_Cmcd, cn_MkrDt, cn_MkrId, cn_Srno, cn_Name '
	    set @strClient_NomineevalueString = @strClient_NomineevalueString+', '''+@strClientCode+''','''+convert(varchar,getdate(),112)+''', '''+@strClientCode+''','''+@ca_filler3+''','''+@NomName+''' '
		SET @strClient_NomineevalueString = 'INSERT INTO  '+@MainTradePlusTableName+' ('+ @strClient_NomineeColumnString +' ) VALUES (' +@strClient_NomineevalueString+' )'	 
	    
		BEGIN TRY
          INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute)
		  VALUES('Client_NomineeDetails',@strClient_NomineevalueString)	       
        END TRY
	    BEGIN CATCH
	      SET @o_vcErrorFlag = 'E'
	      SET @o_vcErrorMessage = 'Client_NomineeDetails '+ERROR_MESSAGE()
		  RETURN 1
	    END CATCH
	  END
	  
	  FETCH NEXT FROM Cur2Main INTO @MainTradePlusTableName, @ca_Nfiller1, @ca_filler3
    END
	CLOSE Cur2Main
    DEALLOCATE Cur2Main
	
		 
	
    DECLARE @strClosuredpString VARCHAR(MAX)='', @string1 NVARCHAR(MAX)='', @string2 NVARCHAR(MAX)=''	
	IF NOT EXISTS(SELECT 1 FROM Client_ModifyAPI(NOLOCK) WHERE ca_cmcd = @i_vcClient_Code AND ca_Nfiller3 = @i_vcRefNo
	AND ca_field = 'cm_UpdateCDSLNSDL' and ca_newValue ='N') 
	BEGIN
    DECLARE @defaultDPIds VARCHAR(100)='', @dpdaactno VARCHAR(20)='', @dpType VARCHAR(10)=''
	DECLARE @CrossDB VARCHAR(100)='', @CrossOwner VARCHAR(50)='', @OP_Product VARCHAR(50)=''
    SELECT @defaultDPIds = sp_sysvalue FROM Sysparameter WHERE sp_parmcd = 'POADPIDS'
    DECLARE Cur2dp
	CURSOR FOR SELECT da_actno, dpType = iif(substring(da_dpid,1,2)='IN','NSDL','CDSL') 
    FROM Dematact(NOLOCK) WHERE da_clientcd = @strClientCode AND da_status = 'A' 
    AND da_dpid IN(SELECT VALUE FROM dbo.returntable(@defaultDPIds,','))
	OPEN Cur2dp 
    FETCH NEXT FROM Cur2dp INTO @dpdaactno, @dpType
    WHILE @@FETCH_STATUS = 0
    BEGIN  
	  SET @CrossDB = ''
	  SET @CrossOwner = ''
	  SET @OP_Product = ''
   	  SELECT @CrossDB = LTRIM(RTRIM(OP_DataBase)),  @CrossOwner = LTRIM(RTRIM(OP_Owner)), @OP_Product = LTRIM(RTRIM(OP_Product))
      FROM Other_Products(NOLOCK) WHERE OP_Product = IIF(@dpType = 'CDSL','Cross','Estro')
	  AND OP_STATUS = 'A'
    
      IF @CrossDB  <> '' AND @dpdaactno <> ''
	  BEGIN
	    IF @i_vcTemplateCode = 'ONLYCLOSURE'
		BEGIN
		  SET @string = ' INSERT INTO  '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification (ca_cmcd, ca_field, ca_oldvalue, ca_newvalue, '
            +' mkrid, mkrdt, ca_computername, ca_time, ca_allow, ca_brcode, ca_authid, ca_authdt, ca_batchno, ca_fdesc, ca_refno, ca_refdt, ca_inwardno, '
            +' ca_flag, ca_trxtype, ca_closure, ca_closurereason, ca_rembal, ca_newboid, ca_remarks, ca_reqintref, ca_destroyslip, ca_check, ca_RecTime) '
            +' values ('''+@dpdaactno+''','''','''',CONVERT(VARCHAR,GETDATE(), 108),'''+@strClientCode+''',' 
		    +' CONVERT(VARCHAR,GETDATE(), 112),'''+@strca_computername1+''',CONVERT(VARCHAR,GETDATE(), 108), '
            +' ''N'','''','''',Null,0,''Close Account'','''+CAST(@i_vcRefNo AS VARCHAR)+''', CONVERT(VARCHAR,GETDATE(), 112),'''',''C'','
			+' (CASE WHEN '''+@StrBOID+''' <> '''' THEN ''T'' ELSE ''S'' END),(CASE WHEN '''+@StrBOID+''' <> '''' THEN ''2'' ELSE ''1'' END),'
			+' (CASE WHEN '''+@StrBOID+''' <> '''' THEN ''4'' ELSE ''99'' END)'
			+' ,''N'','''+@StrBOID+''','
            +' '''+@StrClosureReason+''','''+CAST(@i_vcRefNo AS VARCHAR)+''','''','''',CONVERT(VARCHAR,GETDATE(), 108)) '
		  SET @strClosuredpString = ' UPDATE A SET A.da_status = ''I'' FROM Dematact A WHERE A.da_clientcd = '''+@strClientCode+''' AND '
		  +' A.da_actno = '''+@dpdaactno+''' AND  da_status = ''A'' '
		
        END
		ELSE
	    IF  @dpType = 'CDSL' AND @i_vcTemplateCode <>'ONLYCLOSURE'
		BEGIN
	      
		  SET @string = ' INSERT INTO  '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification(ca_cmcd, ca_field, ca_oldValue,ca_newValue,mkrid,mkrdt,ca_computername,ca_time, '
          +' ca_allow,ca_brcode,ca_batchno,ca_fdesc,ca_refno,ca_refdt,ca_flag,CA_RECTIME, ca_trxType, ca_Closure,  ca_authid, ca_inwardno, ca_closurereason, '
          +' ca_newboid, ca_remarks, ca_reqintref, ca_destroyslip , ca_check) '
          +' SELECT ca_cmcd = '''+@dpdaactno+''', TableColumn, ca_oldValue = (CASE WHEN TableColumn = ''cm_divbranchno'' '
		  +' THEN (CASE WHEN X11.ca_oldValue = ''CA'' THEN ''11'' '
          +' WHEN X11.ca_oldValue = ''SB'' THEN ''10'' ELSE ''13'' END) WHEN  TableColumn = ''cb_annualincome'' '
		  +' THEN (CASE WHEN X11.ca_oldValue = ''2'' THEN ''6'' WHEN X11.ca_oldValue = ''3'' THEN ''7'' WHEN X11.ca_oldValue = ''4'' THEN ''8'' '
		  +' WHEN X11.ca_oldValue in(''5'',''6'') THEN ''9'' ELSE  X11.ca_oldValue END) '
		  +' ELSE X11.ca_oldValue END), '
          +' ca_newValue = (CASE WHEN (TableColumn IN(''cn_NomUID'',''cb_UID1'') AND  LEN(X11.ca_newValue) = 12)  '
		  +' THEN CONCAT(REPLICATE(''X'', 8),RIGHT(X11.ca_newValue, 4)) '
		  +' WHEN TableColumn = ''cm_divbranchno'' THEN (CASE WHEN X11.ca_newValue = ''CA'' THEN ''11'' '
          +' WHEN X11.ca_newValue = ''SB'' THEN ''10'' ELSE ''13'' END) WHEN  TableColumn = ''cb_annualincome'' '
		  +' THEN (CASE WHEN X11.ca_newValue = ''2'' THEN ''6'' WHEN X11.ca_newValue = ''3'' THEN ''7'' WHEN X11.ca_newValue = ''4'' THEN ''8'' '
		  +' WHEN X11.ca_newValue in(''5'',''6'') THEN ''9'' ELSE  X11.ca_newValue END) '
		  +' ELSE X11.ca_newValue END), mkrid = '''+@strClientCode+''', '
          +' mkrdt = convert(varchar,getdate(),112), ca_computername , ca_time, ca_allow =''N'', ca_brcode = ''1'',ca_batchno = 0, '
          +' IIF(TradePlusTableName=''Client_NomineeDetails'',iif(ca_filler3 in(''1'',''3'',''5''),''Nominee '',''Guardian '')+''(''+CAST(ca_Nfiller1 AS VARCHAR)+(CASE WHEN ca_Nfiller1 = 1 THEN ''st)'' WHEN ca_Nfiller1 = 2 then  ''nd)'''
          +' WHEN ca_Nfiller1 = 3 then  ''rd'' else ''st)'' end) ,ColumnDesc) AS ColumnDesc, ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+''',  ca_refdt = ca_date, ca_flag = ''M'', '
	      +' CA_RECTIME = convert(time,getdate()) , '
          +' (case when (TradePlusTableName=''Client_NomineeDetails'' and   (ca_filler3 in(''1'',''3'',''5''))) then ''6'' '
		  +'  when (TradePlusTableName=''Client_NomineeDetails'' and   (ca_filler3 in(''2'',''4'',''6''))) then ''8'' ELSE '''' END) ca_trxnType'
		  +' , IIF(TradePlusTableName = ''Bankact'',0,ca_Nfiller1) as ca_Closure ,ca_authid = '''',' 
		  +' ca_inwardno = '''', ca_closurereason = '''', ca_newboid ='''', ca_remarks = '''', ca_reqintref = '''', ca_destroyslip ='''' , ca_check = '''' '
          +' FROM (select distinct tablename as TradePlusTableName ,  ca_field, ca_oldValue, ca_newValue , ca_Nfiller1, ca_computername, ca_date, ca_time, ca_filler3 '
          +' FROM Client_ModifyAPI(NOLOCK) X , tbl_ReKycAuditColumnMapping_new(nolock) n  '
		  +' WHERE ca_cmcd = '''+@i_vcClient_Code+''' AND ca_Nfiller3 = '''+CAST(@i_vcRefNo AS VARCHAR)+''''
		  +' and X.ca_field = N.FieldName AND X.ca_filler1 = N.MasterJsonTag and DefaultValuetag in(''B'',''U'') and TemplateCode = '''+@i_vcTemplateCode+''' ) X11, tbl_ReKycDPColumnMapping(NOLOCK) MP '
          +' WHERE REPLACE(REPLACE(ReKycColumn,CHAR(10),''''),CHAR(13),'''')  =  REPLACE(REPLACE(X11.ca_field,CHAR(10),''''),CHAR(13),'''') AND MP.ReKycColumn <> '''''  
          +' and ( '''+@i_vcTemplateCode+''' = ''Template1'' and ((TradePlusTableName = ''Bankact'' '
	      +' and exists(Select 1 from client_ModifyAPI(NOLOCK) X WHERE ca_cmcd = '''+@i_vcClient_Code+''' AND ca_Nfiller3 = '''+CAST(@i_vcRefNo AS VARCHAR)+''' '
          +' and ca_field= ''ba_default'' AND ca_newValue =''Y'' AND ca_Nfiller1 = X11.ca_Nfiller1)) OR TradePlusTableName <> ''Bankact'') or '''+@i_vcTemplateCode+''' <> ''Template1'')  AND '''+@dpdaactno+''' <> '''' '
	      +' AND MP.Product = '''+@OP_Product+''' '
          +' ORDER BY ca_Nfiller1,ColumnDesc  '
     		
			
		  SET @string1 = ' INSERT INTO  '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification(ca_cmcd, ca_field, ca_oldValue,ca_newValue,mkrid,mkrdt,ca_computername,ca_time, '
          +' ca_allow,ca_brcode,ca_batchno,ca_fdesc,ca_refno,ca_refdt,ca_flag,CA_RECTIME, ca_trxType, ca_Closure,  ca_authid, ca_inwardno, ca_closurereason, '
          +' ca_newboid, ca_remarks, ca_reqintref, ca_destroyslip , ca_check) '
          +' SELECT ca_cmcd = '''+@dpdaactno+''' , TableColumn = ''cb_MobileISD'', '
          +' ca_oldValue ='''', ca_newValue = ''91'', mkrid =  '''+@strClientCode+''', '
          +' mkrdt = convert(varchar,getdate(),112), ca_computername , ca_time, ca_allow =''N'', ca_brcode = ''1'',ca_batchno = 0,' 
          +' ''Primary Mobile ISD No'' AS ColumnDesc, ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+''',  ca_refdt = ca_date, ca_flag = ''M'', '
          +' CA_RECTIME = convert(time,getdate()) , '
          +' '''' ca_trxnType, ca_Nfiller1 as ca_Closure ,ca_authid = '''', '
          +' ca_inwardno = '''', ca_closurereason = '''', ca_newboid ='''', ca_remarks = '''', ca_reqintref = '''', ca_destroyslip ='''' , ca_check = '''' '
          +' FROM (select distinct tablename as TradePlusTableName ,  ca_field, ca_oldValue, ca_newValue , ca_Nfiller1, ca_computername, ca_date, ca_time, ca_filler3 '
          +' FROM Client_ModifyAPI(NOLOCK) X , tbl_ReKycAuditColumnMapping_new(nolock) n  '
          +' WHERE ca_cmcd = '''+@i_vcClient_Code+''' AND ca_Nfiller3 = '''+CAST(@i_vcRefNo AS VARCHAR)+''''
          +' and X.ca_field = N.FieldName AND X.ca_filler1 = N.MasterJsonTag and DefaultValuetag in(''B'',''U'') '
          +' and TemplateCode = '''+@i_vcTemplateCode+''' AND ca_field = ''cm_mobile'' AND ca_oldValue <> ca_newValue '
          +' and NOT EXISTS(SELECT 1 FROM Client_ModifyAPI WHERE ca_cmcd = X.ca_cmcd AND ca_Nfiller3 = X.ca_Nfiller3 AND ca_field = ''cb_MobileISD'')) X11, '
          +' tbl_ReKycDPColumnMapping(NOLOCK) MP  '
          +' WHERE REPLACE(REPLACE(ReKycColumn,CHAR(10),''''),CHAR(13),'''')  =  REPLACE(REPLACE(X11.ca_field,CHAR(10),''''),CHAR(13),'''') '
          +' AND MP.ReKycColumn <> '''' '  
          +' AND MP.Product = ''CROSS'' ' 
          +' ORDER BY ca_Nfiller1,ColumnDesc  '
		  
		  SET @string2 = ' INSERT INTO '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification(ca_cmcd, ca_field, ca_oldValue,ca_newValue,mkrid,mkrdt,ca_computername,ca_time, '
          +' ca_allow,ca_brcode,ca_batchno,ca_fdesc,ca_refno,ca_refdt,ca_flag,CA_RECTIME, ca_trxType, ca_Closure,  ca_authid, ca_inwardno, ca_closurereason, '
          +' ca_newboid, ca_remarks, ca_reqintref, ca_destroyslip , ca_check) '
		  +' SELECT ca_cmcd = '''+@dpdaactno+''' , TableColumn = ''cm_clienttype'', ca_oldValue =''2169'', ca_newValue = ''2103'', '
		  +' mkrid =  '''+@strClientCode+''', mkrdt = convert(varchar,getdate(),112), ca_computername , ca_time, '
		  +' ca_allow =''N'', ca_brcode = ''1'',ca_batchno = 0, '
          +' ''BO Sub Status'' AS ColumnDesc, ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+''',  ca_refdt = ca_date, ca_flag = ''M'',  '
          +' CA_RECTIME = convert(time,getdate()) ,  '
          +' '''' ca_trxnType, ca_Nfiller1 as ca_Closure ,ca_authid = '''', '
          +' ca_inwardno = '''', ca_closurereason = '''', ca_newboid ='''', ca_remarks = '''', ca_reqintref = '''', ca_destroyslip ='''' , ca_check = '''' ' 
          +' FROM (select top 1 tablename as TradePlusTableName ,  ca_field, ca_oldValue, ca_newValue , ca_Nfiller1, ca_computername, ca_date, ca_time, ca_filler3 '
          +' FROM Client_ModifyAPI(NOLOCK) X , tbl_ReKycAuditColumnMapping_new(nolock) n   '
          +' WHERE ca_cmcd = '''+@i_vcClient_Code+''' AND ca_Nfiller3 = '''+CAST(@i_vcRefNo AS VARCHAR)+''''
          +' and X.ca_field = N.FieldName AND X.ca_filler1 = N.MasterJsonTag and DefaultValuetag in(''B'',''U'')  '
          +' and TemplateCode = '''+@i_vcTemplateCode+''' AND ca_desc like ''Nominee%'' AND ca_oldValue = '''' '
          +' and EXISTS(SELECT 1 FROM '+@CrossDB+'.'+@CrossOwner+'.'+'CLIENT_MASTER WHERE CM_CD = '''+@dpdaactno+''' AND cm_clienttype=''2169'')) X11 '
          +' ORDER BY ca_Nfiller1,ColumnDesc '

		END
	    IF  @dpType = 'NSDL' AND @i_vcTemplateCode <>'ONLYCLOSURE'
		BEGIN
		  SET @string = ' INSERT INTO  '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification(ca_cmcd, ca_field, ca_oldValue, ca_newValue, mkrid, mkrdt, ca_computername, ca_time, ca_recordorder, ca_allow ,ca_brcode, '
          +' ca_authid ,ca_authdt, ca_batchno, ca_fdesc, ca_holderind, ca_flag, ca_refdt, CA_RECTIME, ca_refno, ca_inwardno) '
          
		  +' SELECT ca_cmcd = '''+@dpdaactno+''', TableColumn, ca_oldValue = '
		  +' (CASE WHEN TableColumn = ''cx_StateCdPer'' THEN (select CS_cODE from '+@CrossDB+'.'+@CrossOwner+'.'+'Clientsub_master where cs_module = ''CS22'' AND cs_desc = X11.ca_oldValue)     '
		  +' ELSE (CASE WHEN TableColumn = ''cm_divbranchno'' '
		  +' THEN (CASE WHEN X11.ca_oldValue = ''CA'' THEN ''11'' '
          +' WHEN X11.ca_oldValue = ''SB'' THEN ''10'' ELSE ''13'' END) WHEN  TableColumn = ''cb_annualincome'' '
		  +' THEN (CASE WHEN X11.ca_oldValue = ''2'' THEN ''6'' WHEN X11.ca_oldValue = ''3'' THEN ''7'' WHEN X11.ca_oldValue = ''4'' THEN ''8'' '
		  +' WHEN X11.ca_oldValue in(''5'',''6'') THEN ''9'' ELSE  X11.ca_oldValue END) '
		  +' ELSE X11.ca_oldValue END) END), '
          +' ca_newValue = (CASE WHEN TableColumn = ''cx_StateCdPer'' THEN (select CS_cODE from '+@CrossDB+'.'+@CrossOwner+'.'+'Clientsub_master where cs_module = ''CS22'' AND cs_desc = X11.ca_newValue)     '
		  +' ELSE (CASE WHEN TableColumn = ''cm_divbranchno'' THEN (CASE WHEN X11.ca_newValue = ''CA'' THEN ''11'' '
          +' WHEN X11.ca_newValue = ''SB'' THEN ''10'' ELSE ''13'' END) WHEN  TableColumn = ''cb_annualincome'' '
		  +' THEN (CASE WHEN X11.ca_newValue = ''2'' THEN ''6'' WHEN X11.ca_newValue = ''3'' THEN ''7'' WHEN X11.ca_newValue = ''4'' THEN ''8'' '
		  +' WHEN X11.ca_newValue in(''5'',''6'') THEN ''9'' ELSE  X11.ca_newValue END) '
		  +' ELSE X11.ca_newValue END) END), mkrid = '''+@strClientCode+''', '
          +' mkrdt = convert(varchar,getdate(),112), ca_computername , ca_time, '
		  +' ca_recordorder = IIF(TradePlusTableName=''Client_NomineeDetails'', (CASE WHEN ca_filler3 = ''1'' THEN ''3'' WHEN ca_filler3 = ''3'' THEN ''7'' WHEN ca_filler3 = ''5'' THEN ''9'' '
		  +' WHEN ca_filler3 = ''2'' THEN ''6'' WHEN ca_filler3 = ''4'' THEN ''8'' WHEN ca_filler3 = ''6'' THEN ''10'' ELSE '''' END),MP.GurKycColumn) '
		  +' ,ca_allow =''S'', ca_brcode = ''N'',ca_authid ='''',ca_authdt = '''', ca_batchno = 0, '
          +' IIF(TradePlusTableName=''Client_NomineeDetails'',CAST(ca_Nfiller1 AS VARCHAR)+(CASE WHEN ca_Nfiller1 = 1 THEN ''st'' WHEN ca_Nfiller1 = 2 then  ''nd'''
          +' WHEN ca_Nfiller1 = 3 then  ''rd'' else ''st'' end)+'' ''+iif(ca_filler3 in(''1'',''3'',''5''),''Nominee'',''Guardian'') ,ColumnDesc) AS ColumnDesc, ca_holderind = '''',  ca_flag = ''M'', ca_refdt = ca_date, CA_RECTIME = convert(time,getdate()), '
		  +' ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+''', ca_inwardno = '''''
          +' FROM (select distinct tablename as TradePlusTableName ,  ca_field, ca_oldValue, ca_newValue , ca_Nfiller1, ca_computername, ca_date, ca_time, ca_filler3 '
          +' FROM Client_ModifyAPI(NOLOCK) X , tbl_ReKycAuditColumnMapping_new(nolock) n  '
		  +' WHERE ca_cmcd = '''+@i_vcClient_Code+''' AND ca_Nfiller3 = '''+CAST(@i_vcRefNo AS VARCHAR)+''''
		  +' and X.ca_field = N.FieldName AND X.ca_filler1 = N.MasterJsonTag and DefaultValuetag in(''B'',''U'') and TemplateCode = '''+@i_vcTemplateCode+''' ) X11, tbl_ReKycDPColumnMapping(NOLOCK) MP '
          +' WHERE REPLACE(REPLACE(ReKycColumn,CHAR(10),''''),CHAR(13),'''')  =  REPLACE(REPLACE(X11.ca_field,CHAR(10),''''),CHAR(13),'''') AND MP.ReKycColumn <> '''''  
          +' and ( '''+@i_vcTemplateCode+''' = ''Template1'' and ((TradePlusTableName = ''Bankact'' '
	      +' and exists(Select 1 from client_ModifyAPI(NOLOCK) X WHERE ca_cmcd = '''+@i_vcClient_Code+''' AND ca_Nfiller3 = '''+CAST(@i_vcRefNo AS VARCHAR)+''' '
          +' and ca_field= ''ba_default'' AND ca_newValue =''Y'' AND ca_Nfiller1 = X11.ca_Nfiller1)) OR TradePlusTableName <> ''Bankact'') or '''+@i_vcTemplateCode+''' <> ''Template1'')  AND '''+@dpdaactno+''' <> '''' '
	      +' AND MP.Product = '''+@OP_Product+''' '
          +' ORDER BY ca_Nfiller1, ColumnDesc  '
		END
        BEGIN TRY
		  IF ((@STRClosureType IN('B','D') AND @i_vcTemplateCode = 'ONLYCLOSURE') OR @i_vcTemplateCode <> 'ONLYCLOSURE')
		  BEGIN
	        INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute)
		    VALUES(@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification',@String)
			IF ISNULL(@String1,'') <> ''
			BEGIN
			  INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute)
		      VALUES(@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification',@String1)
			END  
			IF ISNULL(@String2,'') <> ''
			BEGIN
			  INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute)
		      VALUES(@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification',@String2)
			END  
			IF ISNULL(@strClosuredpString,'') <> ''
			BEGIN
			  INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute)
		      VALUES('Dematact',@strClosuredpString)
			END
		  END	
        END TRY
	    BEGIN CATCH
	      SET @o_vcErrorFlag = 'E'
	      SET @o_vcErrorMessage = 'CROSS DB '+ERROR_MESSAGE()
		  RETURN 1
	    END CATCH 
	  END
	  
	  /*
	  SET @string = ' UPDATE X SET X.ca_check = ''M'' '
	  +' FROM  '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification X, '+@CrossDB+'.'+@CrossOwner+'.'+'Client_NomineeDetails A '
	  +' WHERE CA_CMCD ='''+@dpdaactno+''' AND ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+''''
      +' AND (ca_fdesc LIKE ''Nominee%'' OR ca_fdesc LIKE ''Guardian%'') and  A.Cn_Cmcd = X.ca_cmcd AND A.cn_PurposeCd = X.ca_trxtype '
      +' AND CHARINDEX(cast(A.cn_NomSrno as varchar), X.ca_fdesc) > 0 '
      */
	  
	  IF  @dpType = 'CDSL' AND @i_vcTemplateCode <>'ONLYCLOSURE'
	  BEGIN
	    SET @string = ' DECLARE @ca_fdesc VARCHAR(50) ='''', @ca_trxtype INT = 0 '
        +' DECLARE @Xmldata_BasicInfo XML; '
        +' DECLARE @tbl_NomUpdated TABLE(ColumnName VARCHAR(50), ColumnValue VARCHAR(MAX), ca_fdesc VARCHAR(50), ca_trxtype INT) '
        +' DECLARE CURSOR_NomUpdate  CURSOR FOR '
        +' SELECT DISTINCT  X.ca_fdesc, X.ca_trxtype '
        +' FROM  '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification X, '+@CrossDB+'.'+@CrossOwner+'.'+'Client_NomineeDetails A  '
        +' WHERE CA_CMCD = '''+@dpdaactno+'''  AND ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+''' '
        +' AND (ca_fdesc LIKE ''Nominee%'' OR ca_fdesc LIKE ''Guardian%'') and  A.Cn_Cmcd = X.ca_cmcd AND A.cn_PurposeCd = X.ca_trxtype  '
        +' AND CHARINDEX(cast(A.cn_NomSrno as varchar), X.ca_fdesc) > 0 '
        +' OPEN CURSOR_NomUpdate       '
        +' FETCH NEXT FROM CURSOR_NomUpdate INTO @ca_fdesc, @ca_trxtype  '
        +' WHILE @@FETCH_STATUS = 0            '
        +' BEGIN  '
        +'   SET @Xmldata_BasicInfo = (SELECT DISTINCT A.* '
        +'   FROM  '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification X, '+@CrossDB+'.'+@CrossOwner+'.'+'Client_NomineeDetails A  '
        +'   WHERE CA_CMCD = '''+@dpdaactno+'''  AND ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+''''
        +'   AND (ca_fdesc LIKE ''Nominee%'' OR ca_fdesc LIKE ''Guardian%'') and  A.Cn_Cmcd = X.ca_cmcd AND A.cn_PurposeCd = X.ca_trxtype  '
        +'   AND CHARINDEX(cast(A.cn_NomSrno as varchar), X.ca_fdesc) > 0  FOR XML PATH(''Response'')) '
        +' INSERT INTO @tbl_NomUpdated( ColumnName, ColumnValue, ca_fdesc, ca_trxtype) '
        +' SELECT ColumnName = ISNULL(ColumnName,''''), ColumnValue = ISNULL(ColumnValue,''''), @ca_fdesc, @ca_trxtype  '
        +' FROM(SELECT  c.value(''local-name(.)'', ''NVARCHAR(MAX)'') AS ColumnName, '
        +' c.value(''(./text())[1]'', ''NVARCHAR(MAX)'') AS ColumnValue '
        +' FROM @Xmldata_BasicInfo.nodes(''/Response//*'') AS t(c)) X12 '
        +' FETCH NEXT FROM CURSOR_NomUpdate INTO  @ca_fdesc, @ca_trxtype      '
        +' END  CLOSE CURSOR_NomUpdate DEALLOCATE CURSOR_NomUpdate '
        +' UPDATE A SET A.ca_check = ''M'', A.ca_oldvalue = B.ColumnValue '
        +' FROM  '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification A, @tbl_NomUpdated B '
        +' WHERE A.CA_CMCD = '''+@dpdaactno+'''  AND ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+''' '
        +' AND A.ca_fdesc = B.ca_fdesc  '
        +' AND A.ca_trxtype = B.ca_trxtype '
        +' AND A.ca_field = B.ColumnName '

	    BEGIN TRY
	      INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute)
		  VALUES(@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification',@String)
        END TRY
	    BEGIN CATCH
	      SET @o_vcErrorFlag = 'E'
	      SET @o_vcErrorMessage = 'CROSS DB '+ERROR_MESSAGE()
		  RETURN 1
	    END CATCH 
	  
        SET @string = ' UPDATE X SET X.ca_check = ''S''
	    FROM '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification X WHERE CA_CMCD ='''+@dpdaactno+''' AND ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+'''
        AND (ca_fdesc LIKE ''Nominee%'' OR ca_fdesc LIKE ''Guardian%'') 
        AND NOT EXISTS(SELECT 1 FROM '+@CrossDB+'.'+@CrossOwner+'.'+'Client_NomineeDetails WHERE  cn_Cmcd = X.ca_cmcd AND cn_PurposeCd = X.ca_trxtype
        AND CHARINDEX(cast(cn_NomSrno as varchar), X.ca_fdesc) > 0 ) '
	  
	    BEGIN TRY
	      INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute)
		  VALUES(@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification',@String)
        END TRY
	    BEGIN CATCH
	      SET @o_vcErrorFlag = 'E'
	      SET @o_vcErrorMessage = 'CROSS DB '+ERROR_MESSAGE()
		  RETURN 1
	    END CATCH 
  	  END  	 
	  FETCH NEXT FROM Cur2dp INTO  @dpdaactno, @dpType
    END
	CLOSE Cur2dp
    DEALLOCATE Cur2dp
	END

	DECLARE @StrClientStatus VARCHAR(1)=''
	SELECT @StrClientStatus = cm_freezeyn FROM CLIENT_MASTER(NOLOCK) WHERE CM_CD = @i_vcClient_Code
	
	IF @StrClientStatus NOT IN('N','A','B') AND @i_vcTemplateCode <> 'ONLYCLOSURE' 
	BEGIN
	  INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute)
	  VALUES('Client_Master','update Client_Master set cm_freezeyn = ''N'' WHERE CM_CD = '''+@i_vcClient_Code+'''')
	  
	  SET @strAuditStringDetail = ''''+SUBSTRING('Client_Master',1,20)+''', '' '' ,''cm_freezeyn'','''+@i_vcClient_Code+''',''cm_freezeyn'','
	  +' ''Freezeyn'','''+@StrClientStatus+''',''N'','''+@ca_computername
 		    +''','''+@i_vcClient_Code+''','''+@mkrdt+''','''+@ca_Time+''',replace('''+SUBSTRING('Client_Master',1,20)+''',''_'','' ''),''Client Code'','''','''','''' ) '
	  INSERT INTO @tbl_ExecuteCommand(TableName, QueryExecute)
	  VALUES('Common_audit',@strAuditString+' '+@strAuditStringDetail)
	END
	BEGIN TRAN
	 declare @strTableName VARCHAR(100), @StrQuery VARCHAR(MAX)=''
	 DECLARE CurExecute
	 CURSOR FOR SELECT TableName, QueryExecute 
	 FROM @tbl_ExecuteCommand order by IIF(TABLENAME='Common_audit',10,1), SerialNo
	 OPEN CurExecute 
     FETCH NEXT FROM CurExecute INTO @strTableName, @StrQuery
     WHILE @@FETCH_STATUS = 0
     BEGIN 
	   SET @String =  @StrQuery
	   BEGIN TRY
	     EXEC(@String)
	   END TRY
       BEGIN CATCH
	     SET @o_vcErrorFlag = 'E'
	     SET @o_vcErrorMessage = @strTableName+' '+ERROR_MESSAGE()
		 CLOSE CurExecute
         DEALLOCATE CurExecute
		 ROLLBACK;
         RETURN 1
       END CATCH	   
       FETCH NEXT FROM CurExecute INTO @strTableName, @StrQuery		
	 END
	CLOSE CurExecute
    DEALLOCATE CurExecute
	
	IF ISNULL(@strPermanentAddressFlag,'N') = 'Y'
	BEGIN
	  UPDATE A SET A.cm_padd1 = B.cm_add1, A.cm_padd2 = B.cm_add2, 
	  A.cm_padd3 = B.cm_add3, A.cm_padd4 = B.cm_add4,
      A.cm_ppincode = B.cm_pincode, A.cm_pcountry = B.cm_bankactno
      FROM client_info A, CLIENT_MASTER B
	  WHERE A.cm2_cd = B.CM_CD 
	  AND B.CM_CD = @i_vcClient_Code
	  
	  UPDATE A SET A.cm_pstate = A.cm_state
      FROM client_info A
	  WHERE A.cm2_cd = @i_vcClient_Code
	  
	END   
	
	
	
    UPDATE A SET A.rm_status = iif(@i_vcApprovalFlag='R','Reject','Approve'),
	A.rm_Desc = @i_vcApprovalReason , A.rm_rekyc = iif(@i_vcApprovalFlag='R','R','Y'), A.rm_step = 6,
	A.mkrdt = convert(varchar,getdate(),112), A.mkrtm = CONVERT(VARCHAR,GETDATE(), 108)
	FROM Client_ReKycMain A 
	where rm_cmcd = @i_vcClient_Code  AND  rm_refno = @i_vcRefNo
	
	UPDATE A SET A.ca_Tplus = iif(@i_vcApprovalFlag='R','R','Y') , A.ca_Cross = 'Y'
	FROM Client_ModifyAPI A 
	where ca_cmcd = @i_vcClient_Code  AND  ca_Nfiller3 = @i_vcRefNo

	UPDATE A SET A.ma_status = iif(@i_vcApprovalFlag='R','R','Y') 
	FROM Client_ModifyAttach A 
	where ma_cmcd = @i_vcClient_Code  AND  ma_refno = @i_vcRefNo

	
	COMMIT;	
	
	DECLARE @dpca_computername VARCHAR(50)='', @dpca_time VARCHAR(20)='', @dpca_date VARCHAR(8)=''
	
	SET @dpdaactno = ''
    SET @dpType =''
    DECLARE Cur3dp
	CURSOR FOR SELECT da_actno, dpType = iif(substring(da_dpid,1,2)='IN','NSDL','CDSL') 
    FROM Dematact(NOLOCK) WHERE da_clientcd = @strClientCode AND da_status = 'A'
    AND da_dpid IN(SELECT VALUE FROM dbo.returntable(@defaultDPIds,','))
	OPEN Cur3dp 
    FETCH NEXT FROM Cur3dp INTO @dpdaactno, @dpType
    WHILE @@FETCH_STATUS = 0
    BEGIN  
	  SET @CrossDB = ''
	  SET @CrossOwner = ''
	  SET @OP_Product = ''
   	  SELECT @CrossDB = LTRIM(RTRIM(OP_DataBase)),  @CrossOwner = LTRIM(RTRIM(OP_Owner)), @OP_Product = LTRIM(RTRIM(OP_Product))
      FROM Other_Products(NOLOCK) WHERE OP_Product = IIF(@dpType = 'CDSL','Cross','Estro')
	  AND OP_STATUS = 'A' AND OP_Product = 'CROSS'
    
      IF @CrossDB  <> '' AND @dpdaactno <> '' AND @i_vcTemplateCode <> 'ONLYCLOSURE' 
	  BEGIN
	    IF @dpType = 'CDSL'
		BEGIN
          SELECT TOP 1 @dpca_computername = ca_computername, @dpca_time = ca_time,
          @dpca_date = 	ca_date	
		  from Client_ModifyAPI(NOLOCK) A 
	      where ca_cmcd = @i_vcClient_Code  AND  ca_Nfiller3 = @i_vcRefNo
		
		  SET @String = 'DECLARE @Xmldata_BasicInfo XML; '
                      +' declare @cn_PurposeCd VARCHAR(10), @cn_NomSrno VARCHAR(10) '
                      +' DECLARE CURSOR_NomDelete  CURSOR FOR '         
                      +' SELECT DISTINCT cn_PurposeCd, cn_NomSrno FROM '+@CrossDB+'.'+@CrossOwner+'.'+'Client_NomineeDetails X WHERE cn_Cmcd = '''+@dpdaactno+''''
          +' AND NOT EXISTS(sELECT 1 FROM '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification WHERE ca_cmcd = X.cn_Cmcd '
          +' AND ca_trxtype = X.cn_PurposeCd AND ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+''' AND (ca_fdesc LIKE ''Nominee%'' OR ca_fdesc LIKE ''Guardian%'') '
          +' AND CHARINDEX(cast(X.cn_NomSrno as varchar), ca_fdesc) > 0) '
          +' AND EXISTS(SELECT 1 FROM '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification WHERE ca_cmcd = X.cn_Cmcd '
          +' AND ca_fdesc LIKE ''Nominee%'' OR ca_fdesc LIKE ''Guardian%'' AND ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+''') '
          +' OPEN CURSOR_NomDelete        '    
          +' FETCH NEXT FROM CURSOR_NomDelete INTO @cn_PurposeCd, @cn_NomSrno  '        
          +' WHILE @@FETCH_STATUS = 0   '         
          +' BEGIN  '
          +' SET @Xmldata_BasicInfo = (SELECT cn_NomName, cn_NomMidNm, cn_NomlastNm, cn_NomAdd1, cn_NomAdd2, cn_NomAdd3, cn_City, cn_State, cn_Country, '
          +' cn_NomPin, cn_Relation , cn_NomPershare , cn_ResidualFlag , cn_NomDOB, cn_NomTitle, cn_NomSuffix, cn_FathHusbnm, cn_PH1INDC, cn_PH1, '
          +' cn_NomPAN,cn_NomUID, cn_NomEmail, cn_NomUIDVerifyFlag, cn_NomMobileISD '
		  +' FROM '+@CrossDB+'.'+@CrossOwner+'.'+'Client_NomineeDetails X WHERE cn_Cmcd = '''+@dpdaactno+''' '
          +' AND CAST(cn_PurposeCd AS VARCHAR) = cast(@cn_PurposeCd as varchar) AND CAST(cn_NomSrno AS VARCHAR) = cast(@cn_NomSrno  as varchar) '
          +' AND NOT EXISTS(sELECT 1 FROM '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification WHERE ca_cmcd = X.cn_Cmcd '
          +' AND ca_trxtype = X.cn_PurposeCd AND ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+''' AND (ca_fdesc LIKE ''Nominee%'' OR ca_fdesc LIKE ''Guardian%'') '
          +' AND CHARINDEX(cast(X.cn_NomSrno as varchar), ca_fdesc) > 0) '
          +' AND EXISTS(SELECT 1 FROM '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification WHERE ca_cmcd = X.cn_Cmcd AND ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+'''  '
          +' AND ca_fdesc LIKE ''Nominee%'' OR ca_fdesc LIKE ''Guardian%'') FOR XML PATH(''''))' 
          +' INSERT INTO  '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification(ca_cmcd, ca_field, ca_oldValue,ca_newValue,mkrid,mkrdt,ca_computername,ca_time, '
          +' ca_allow,ca_brcode,ca_batchno,ca_fdesc,ca_refno,ca_refdt,ca_flag,CA_RECTIME, ca_trxType, ca_Closure,  ca_authid, ca_inwardno, ca_closurereason, '
          +' ca_newboid, ca_remarks, ca_reqintref, ca_destroyslip , ca_check) '
          +' SELECT ca_cmcd = '''+@dpdaactno+''', TableColumn = KeyName, ca_oldValue = KeyValue,  ca_newValue = KeyValue,   '
          +' mkrid = '''+@strClientCode+''', mkrdt = convert(varchar,getdate(),112), '
	      +' ca_computername = '''+@dpca_computername+''', ca_time = '''+@dpca_time+''', ca_allow =''N'', ca_brcode = '''',ca_batchno = 0, '
          +' IIF( @cn_PurposeCd = ''6'', ''Nominee '',''Guardian '')+''(''+@cn_NomSrno+(CASE WHEN @cn_NomSrno = 1 THEN ''st)'''
          +'  WHEN @cn_NomSrno = 2 then  ''nd)'' '
          +' WHEN @cn_NomSrno = 3 then  ''rd'' else ''st)'' end) AS ColumnDesc, ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+''','
          +'  ca_refdt = '''+@dpca_date+''', ca_flag = ''M'', CA_RECTIME = convert(time,getdate()) ,  '
		  +' @cn_PurposeCd AS ca_trxnType, @cn_NomSrno as ca_Closure ,ca_authid = '''','
		  +' ca_inwardno = '''', ca_closurereason = '''', ca_newboid ='''', ca_remarks = '''', '
		  +' ca_reqintref = '''', ca_destroyslip ='''' , ca_check = ''D'' FROM (      '
          +' SELECT i.value(''local-name(.)'',''varchar(100)'') KeyName,      '
          +' i.value(''.'',''varchar(500)'') KeyValue     '
          +' FROM @Xmldata_BasicInfo.nodes(''//*[text()]'') x(i) ) tmp '
	    
          +' FETCH NEXT FROM CURSOR_NomDelete INTO @cn_PurposeCd, @cn_NomSrno   END '          
          +' CLOSE CURSOR_NomDelete DEALLOCATE CURSOR_NomDelete '
		  EXEC(@String)
		  
		  IF ISNULL(@strPermanentAddressFlag,'N') = 'Y'
		  BEGIN
		    SET @string = ' INSERT INTO  '+@CrossDB+'.'+@CrossOwner+'.'+'Client_Modification(ca_cmcd, ca_field, '
		     +' ca_oldValue,ca_newValue, mkrid, mkrdt, ca_computername, ca_time, '
             +' ca_allow,ca_brcode,ca_batchno,ca_fdesc,ca_refno,ca_refdt,ca_flag,CA_RECTIME, ca_trxType, ca_Closure,  '
		     +' ca_authid, ca_inwardno, ca_closurereason, '
             +' ca_newboid, ca_remarks, ca_reqintref, ca_destroyslip , ca_check) '
             +' SELECT ca_cmcd ='''+@dpdaactno+''', TableColumn, ca_oldValue, ca_newValue, mkrid = '''+@strClientCode+''', '
             +' mkrdt = convert(varchar,getdate(),112), ca_computername , ca_time, ca_allow =''N'', ca_brcode = ''1'',ca_batchno = 0, '
             +' ColumnDesc, ca_refno = '''+CAST(@i_vcRefNo AS VARCHAR)+''',  ca_refdt = ca_date, ca_flag = ''M'', '
		     +' CA_RECTIME = convert(time,getdate()) , '''' ca_trxnType, '
             +' ca_Nfiller1 as ca_Closure ,ca_authid = '''', ca_inwardno = '''', ca_closurereason = '''', '
		     +' ca_newboid ='''', ca_remarks = '''', ca_reqintref = '''', ca_destroyslip ='''' , '
		     +' ca_check = '''' FROM( '
             +' SELECT ca_field = case when ca_field = ''cm_add1'' then ''cm_padd1'''
             +' when ca_field = ''cm_add2'' then ''cm_padd2'' '
             +' when ca_field = ''cm_add3'' then ''cm_padd3'' '
             +' when ca_field = ''cm_add4'' then ''cm_padd4'' '
             +' when ca_field = ''cm_pincode'' then ''cm_ppincode'' '
		     +' when ca_field = ''cm_state'' then ''cm_pstate''  when ca_field = ''cm_bankactno'' then ''cm_pcountry'' else ca_field end, '
             +' ca_oldValue, ca_newValue , ca_Nfiller1, ca_computername, ca_date, ca_time, ca_filler3   '
             +' FROM Client_ModifyAPI X , tbl_ReKycAuditColumnMapping_new(nolock) n  WHERE ca_cmcd='''+@strClientCode+''' '
             +' AND ca_Nfiller3 = '''+CAST(@i_vcRefNo AS VARCHAR)+''' AND ca_desc LIKE ''Address%'' '
             +' AND X.ca_field = N.FieldName AND X.ca_filler1 = N.MasterJsonTag and '
		     +'   DefaultValuetag in(''B'',''U'') and TemplateCode = '''+@i_vcTemplateCode+''') X11, tbl_ReKycDPColumnMapping(NOLOCK) MP '
             +' WHERE REPLACE(REPLACE(ReKycColumn,CHAR(10),''''),CHAR(13),'''')  =  REPLACE(REPLACE(X11.ca_field,CHAR(10),''''),CHAR(13),'''') AND MP.ReKycColumn <> '''''  
             EXEC(@String)
          END 			 
		END
	  END	
	  FETCH NEXT FROM Cur3dp INTO  @dpdaactno, @dpType
	END	--SELECT @String
	
    CLOSE Cur3dp
    DEALLOCATE Cur3dp  
    
	
	IF EXISTS(sELECT 1 FROM Client_NomineeDetails(NOLOCK) WHERE cn_Cmcd = @i_vcClient_Code)
	BEGIN
	  IF NOT EXISTS(SELECT 1 FROM Client_Nominee(NOLOCK) WHERE cn_cd = @i_vcClient_Code) 
	  begin
	    INSERT INTO Client_Nominee(cn_cd, cn_ucc, cn_polexp, cn_polexpval, cn_pastreg, cn_pastrerval, mkrdt, mkrid, cn_KRAStatus, cn_KRADate, cn_smsalert,
        cn_emailalert, cn_CIN, cn_filler5, cn_filler6, cn_filler7, cn_filler8, cn_filler9, cn_fillerN0, cn_fillerN1, cn_fillerN2,
        cn_fillerN3, cn_fillerN4, cn_fillerN5, cn_fillerN6, cn_fillerN7, cn_fillerN8, cn_fillerN9, cn_name, cn_add1, cn_add2,
	    cn_city, cn_state, cn_pin, cn_tel, cn_pan, cn_regdt, cn_dob, cn_gname, cn_gadd1, cn_gadd2, cn_gcity,
	    cn_gstate, cn_gpin, cn_gtel, cn_gpan)
	    VALUES(@i_vcClient_Code,'','','','','',CONVERT(VARCHAR,GETDATE(),112),'REKYC','','','','','','','','','','',0,0,0,0,0,1,0,0,0,
		0,'','','','','','','','','', '','','','','','','','','')
		
		UPDATE A SET cn_polexpval = CONVERT(VARCHAR,GETDATE(),112)
	    FROM Client_Nominee A
	    WHERE A.Cn_CD = @i_vcClient_Code 
		and @i_vcTemplateCode = 'ONLYCLOSURE' 
		and @STRClosureType IN('B','T')
		
      END
	  ELSE
	  BEGIN
	    UPDATE A SET cn_fillerN5 = 1
	    FROM Client_Nominee A
	    WHERE A.Cn_CD = @i_vcClient_Code 
		
		UPDATE A SET cn_polexpval = CONVERT(VARCHAR,GETDATE(),112)
	    FROM Client_Nominee A
	    WHERE A.Cn_CD = @i_vcClient_Code 
		and @i_vcTemplateCode = 'ONLYCLOSURE' 
		and @STRClosureType IN('B','T')
		
 	  END
	END
    ELSE
	BEGIN
      UPDATE A SET cn_polexpval = CONVERT(VARCHAR,GETDATE(),112)
	  FROM Client_Nominee A
	  WHERE A.Cn_CD = @i_vcClient_Code 
	  and @i_vcTemplateCode = 'ONLYCLOSURE' 
	  and @STRClosureType IN('B','T')
    END	  
	  		
	
	BEGIN TRY
	--IF EXISTS(SELECT 1 FROM Client_ModifyAPI(NOLOCK) WHERE ca_cmcd = @i_vcClient_Code  AND  ca_Nfiller3 = @i_vcRefNo
	--AND ca_field IN('cm_add1','cm_add2','cm_add3','cm_add4','cm_mobile','cm_email','cm_pincode','cm_state') 
	--AND ca_oldValue <> ca_newValue) 
	--BEGIN
	  
	  DECLARE @IserialNo INT = 0, @strCK_Reference VARCHAR(50)=''
	  
	  SELECT @IserialNo = isnull(MAX(CK_SRNO),0)+ 1 
	  FROM Client_CKYC(NOLOCK)
	  
	  SELECT TOP 1 @strCK_Reference = CK_Reference FROM Client_CKYC(NOLOCK), 
	  CLIENT_MASTER(NOLOCK) CM  WHERE CM.CM_CD = @i_vcClient_Code AND CK_Panno = CM.cm_panno AND CK_Reference <> ''
	  AND CK_SRNO IN(SELECT MAX(CK_SRNO) FROM Client_CKYC(NOLOCK) WHERE CK_Panno = CM.cm_panno)
	  
	  INSERT INTO Client_CKYC(CK_SRNO, CK_Panno, CK_MaidenPrefix, CK_MaidenFname, CK_MaidenMName, 
	  CK_MaidenLname, CK_FatherPrefix, CK_FatherFname, CK_FatherMname, CK_FatherLname,
      CK_MotherPrefix, CK_Motherfname, CK_MotherMname, CK_MotherLname, CK_fatherspouseflag, CK_citizenship, 
	  CK_Resistatus, CK_ResiTaxPurpose, CK_IdentityProof,
      CK_IdentityProofID, CK_IdentityProofExpDt, CK_AddrProof, CK_AddrProofID, CK_AddrProofExpDt, CK_PermAddrProof, CK_PermAddrProofID, CK_PermAddrProofExpDt,
      CK_CityOfBirth, CK_CountyOfBirth, CK_verifyBy, CK_Status, CK_batchno, CK_batchDt, CK_Reference, mkrId, mkrDt, CK_ActType, 
	  Ck_RespType, Ck_AppType,
      Ck_Filler1, Ck_Filler2, Ck_Filler3, Ck_NFiller1, Ck_Nfiller2, Ck_NFiller3)
	  SELECT @IserialNo, CK_Panno = cm_panno, CK_MaidenPrefix = '', CK_MaidenFname = '', CK_MaidenMName = '', 
	  CK_MaidenLname = '', CK_FatherPrefix = 'Mr', 
	  CK_FatherFname = ltrim(rtrim((CASE WHEN CHARINDEX(' ', cm_faherhusguar) > 0 THEN SUBSTRING(cm_faherhusguar, 1, CHARINDEX(' ', cm_faherhusguar) - 1) 
	  ELSE cm_faherhusguar END))), 
	  CK_FatherMname = ltrim(rtrim((CASE WHEN CHARINDEX(' ', cm_faherhusguar) = 0 THEN '' ELSE RTRIM(LTRIM(REPLACE(REPLACE(cm_faherhusguar, SUBSTRING(cm_faherhusguar
												, 1, CHARINDEX(' ', cm_faherhusguar) - 1), ''), REVERSE(LEFT(REVERSE(cm_faherhusguar), CHARINDEX(' ', REVERSE(
														cm_faherhusguar)) - 1)), ''))) END))), 
	  CK_FatherLname = ltrim(rtrim((CASE WHEN CHARINDEX(' ', cm_faherhusguar) > 0 
	  THEN REVERSE(LEFT(REVERSE(cm_faherhusguar), CHARINDEX(' ', REVERSE(cm_faherhusguar)) - 1)) ELSE '' END))),
      CK_MotherPrefix = '', 
	  CK_Motherfname ='', CK_MotherMname = '', CK_MotherLname = '', CK_fatherspouseflag = 'F', CK_citizenship = 'I', 
	  CK_Resistatus ='01', CK_ResiTaxPurpose = '', CK_IdentityProof = 'E',
      CK_IdentityProofID = cm_uid, CK_IdentityProofExpDt = '', 
	  CK_AddrProof = '01', 
	  CK_AddrProofID = cm_uid, CK_AddrProofExpDt = '', CK_PermAddrProof = '01', CK_PermAddrProofID = cm_uid, 
	  CK_PermAddrProofExpDt = '',
      CK_CityOfBirth = cm_add4, CK_CountyOfBirth = '', 
	  CK_verifyBy = '', CK_Status = 'Y', CK_batchno = 0, CK_batchDt = '', CK_Reference = @strCK_Reference, mkrId = @i_vcClient_Code, 
	  mkrDt = CONVERT(VARCHAR,GETDATE(),112), CK_ActType = '01', Ck_RespType = '', 
	  Ck_AppType = (CASE WHEN ISNULL(@strCK_Reference,'') <> '' THEN '03' ELSE '01' END),
      Ck_Filler1 = '', Ck_Filler2 = '', Ck_Filler3 = '', Ck_NFiller1 = '', Ck_Nfiller2 = 0, Ck_NFiller3  = 0
      FROM CLIENT_MASTER(NOLOCK) CM, CLIENT_INFO(NOLOCK) CM2 
      WHERE CM_CD = @i_vcClient_Code
      AND CM.cm_cd = CM2.cm2_cd
      AND NOT EXISTS(SELECT 1 FROM Client_CKYC(NOLOCK) WHERE CK_Panno = CM.cm_panno and CK_SRNO = @IserialNo)
	  	  
	  INSERT INTO  Client_CKYCImages (CI_SRNO, CI_Panno, CI_Type, CI_Image, mkrId, mkrDt, CI_ContentType)
	  SELECT DISTINCT CI_SRNO = @IserialNo, CI_Panno = CM.cm_panno, CI_Type, 
	  CI_Image = DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(REPLACE(ma_proof,'data:image/jpeg;base64,',''),'data:image/png;base64,','')) AS NVARCHAR(MAX))),
      mkrId = @i_vcClient_Code, mkrDt = CONVERT(VARCHAR,GETDATE(),112), CI_ContentType FROM(
      SELECT X.*, CI_Type = (CASE WHEN ma_field  in('AadhaarAttachment') THEN 'AD'
      WHEN ma_field  IN('IDAttachment') THEN 'ID'
      WHEN ma_field  = 'PhotoAttachment' THEN 'PH'
      WHEN ma_field  = 'SignAttachment' THEN 'SG' ELSE '' END), CI_ContentType = iif(ma_filename in('SIGN','Photo','Aadhaar'), 'jpg','pdf')
      FROM Client_ModifyAttach(NOLOCK) X WHERE MA_CMCD = @i_vcClient_Code AND ma_refno = @i_vcRefNo
      AND ma_field IN('IDAttachment','PhotoAttachment','SignAttachment','AadhaarAttachment')) X1, 
	  CLIENT_MASTER(NOLOCK) CM
      WHERE X1.ma_cmcd = CM.cm_cd
	  and NOT EXISTS(SELECT 1 FROM Client_CKYCImages(NOLOCK) WHERE  CI_Panno = CM.cm_panno
	  AND CI_Type = X1.CI_Type  and CI_SRNO = @IserialNo)
	  AND ma_proof IS NOT NULL
	   	  
	  INSERT INTO  Client_CKYCImages (CI_SRNO, CI_Panno, CI_Type, CI_Image, mkrId, mkrDt, CI_ContentType)
	  SELECT DISTINCT CI_SRNO = @IserialNo, CI_Panno = CM.cm_panno, CI_Type, 
	  CI_Image = DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX))),
      mkrId = @i_vcClient_Code, mkrDt = CONVERT(VARCHAR,GETDATE(),112), CI_ContentType FROM(
      SELECT X.*, CI_Type = (CASE WHEN ma_field  in('AadhaarAttachment') THEN 'ID' 
	  ELSE '' END), CI_ContentType = iif(ma_filename in('SIGN','Photo','Aadhaar'), 'jpg','pdf')
      FROM Client_ModifyAttach(NOLOCK) X WHERE MA_CMCD = @i_vcClient_Code AND ma_refno = @i_vcRefNo
      AND ma_field IN('AadhaarAttachment')) X1, CLIENT_MASTER(NOLOCK) CM
      WHERE X1.ma_cmcd = CM.cm_cd
	  and NOT EXISTS(SELECT 1 FROM Client_CKYCImages(NOLOCK) WHERE  CI_Panno = CM.cm_panno
	  AND CI_Type = 'ID'  and CI_SRNO = @IserialNo)
	  AND ma_proof IS NOT NULL
	  
	  /*INSERT INTO  Client_CKYCImages (CI_SRNO, CI_Panno, CI_Type, CI_Image, mkrId, mkrDt, CI_ContentType)
	  SELECT DISTINCT CI_SRNO = @IserialNo, CI_Panno = CM.cm_panno, CI_Type, 
	  CI_Image = DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX))),
      mkrId = @i_vcClient_Code, mkrDt = CONVERT(VARCHAR,GETDATE(),112), CI_ContentType FROM(
      SELECT X.*, CI_Type = 'AD', CI_ContentType = iif(ma_filename in('SIGN','Photo','Aadhaar'), 'jpg','pdf')
      FROM Client_ModifyAttach(NOLOCK) X WHERE MA_CMCD = @i_vcClient_Code AND ma_refno = @i_vcRefNo
      AND ma_field IN('AddressAttachment')) X1, CLIENT_MASTER(NOLOCK) CM
      WHERE X1.ma_cmcd = CM.cm_cd
	  and NOT EXISTS(SELECT 1 FROM Client_CKYCImages(NOLOCK) WHERE  CI_Panno = CM.cm_panno
	  AND CI_Type = 'AD'  and CI_SRNO = @IserialNo)*/
    
	--END
	END TRY
	BEGIN CATCH
	  SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = ERROR_MESSAGE()+' '+'CKYC'
	  RETURN 1
	END CATCH
	
	SET @MTFString = ' IF EXISTS (SELECT 1 FROM MrgTdgFin_Clients(NOLOCK) WHERE MTFC_CMCD = '''+@i_vcClient_Code+''') '
	+' AND NOT EXISTS(sELECT 1 from client_master(nolock) where cm_Cd in(SELECT MTFC_FillerB FROM MrgTdgFin_Clients(NOLOCK) WHERE MTFC_CMCD = '''+@i_vcClient_Code+'''))'
	+' BEGIN '
	+'  INSERT INTO [Client_master] ([cm_cd], [cm_name], [cm_prefix], [cm_add1], [cm_add2], [cm_add3], [cm_std], [cm_tele1], [cm_tele2], [cm_fax], [cm_mobile], '
    +'  [cm_email], [cm_panno], [cm_sex], [cm_occup], [cm_dob], [cm_introducer], [cm_subbroker], [cm_groupcd], [cm_familycd], [cm_cfstdratesyn], [cm_marginyn], [cm_margintype], '
      +'  [cm_billflag], [cm_contractflag], [cm_contractprint], [cm_dpid], [cm_dpactno], [cm_type], [cm_brkggroup], [cm_openingbal], [cm_updatebal], [cm_haircut], [cm_bankname], '
      +'  [cm_bankbranch], [cm_bankrbi], [cm_bankacttype], [cm_bankactno], [cm_confirmemailyn], [cm_confirmwebyn], [cm_confirmfaxyn], [cm_confirmtelyn], [cm_billroff], [cm_netrate], '
      +'  [cm_marketrate], [cm_brokerage], [cm_currencycd], [cm_schedule], [cm_servicetax], [cm_turnovertax], [cm_gainloss], [cm_hisshare], [cm_opendt], [cm_pwd], [mkrid], [mkrdt], '
      +'  [cm_jobloss], [cm_directpayout], [cm_transfertoac], [cm_specialyn], [cm_freezeyn], [cm_freezedt], [cm_freezereason], [cm_stampdutyyn], [cm_dematchargeyn], [cm_brboffcode], '
      +'  [cm_poa], [cm_sebino], [cm_digitalcontract], [cm_digitaldispatch], [cm_focashmargin], [cm_fosharemargin], [cm_custodiancd], [cm_stpexport], [cm_filler3], [cm_filler4], '
      +'  [cm_sttprintYn]) '
      +'  SELECT [cm_cd] = MTFC_FillerB , [cm_name], [cm_prefix] = '''', [cm_add1] = '''', [cm_add2] = '''', [cm_add3] = '''', '
      +' [cm_std] = '''', [cm_tele1] = '''', [cm_tele2] = '''', [cm_fax] = '''', [cm_mobile] = '''',  '
      +' [cm_email] = '''', [cm_panno] = '''', [cm_sex] = '''', [cm_occup] = '''', [cm_dob] = '''', [cm_introducer] = '''', '
      +' [cm_subbroker] = '''', [cm_groupcd], [cm_familycd], [cm_cfstdratesyn] = '''', [cm_marginyn] = '''', [cm_margintype] = '''', '
      +' [cm_billflag], [cm_contractflag], [cm_contractprint], [cm_dpid], [cm_dpactno], [cm_type], [cm_brkggroup], [cm_openingbal], [cm_updatebal], [cm_haircut], [cm_bankname], '
      +' [cm_bankbranch], [cm_bankrbi], [cm_bankacttype], [cm_bankactno], [cm_confirmemailyn], [cm_confirmwebyn], [cm_confirmfaxyn], [cm_confirmtelyn], [cm_billroff], [cm_netrate], '
      +' [cm_marketrate], [cm_brokerage], [cm_currencycd], '
      +' [cm_schedule] = (SELECT sp_sysvalue FROM Sysparameter WHERE sp_parmcd = ''MTFP_SCHDL''),  '
      +' [cm_servicetax], [cm_turnovertax], [cm_gainloss], [cm_hisshare], [cm_opendt], [cm_pwd], [mkrid] = ''API'', '
      +' [mkrdt] = CONVERT(VARCHAR,GETDATE(),112), '
      +' [cm_jobloss], [cm_directpayout], [cm_transfertoac], [cm_specialyn], '
      +' [cm_freezeyn], [cm_freezedt], [cm_freezereason], [cm_stampdutyyn], [cm_dematchargeyn], [cm_brboffcode], '
      +' [cm_poa], [cm_sebino], [cm_digitalcontract], [cm_digitaldispatch], [cm_focashmargin], [cm_fosharemargin], [cm_custodiancd], [cm_stpexport], [cm_filler3], [cm_filler4], '
      +' [cm_sttprintYn] '
      +' FROM CLIENT_MASTER(NOLOCK), MrgTdgFin_Clients(NOLOCK) '
      +' WHERE CM_CD = '''+@i_vcClient_Code+''''
      +' AND CM_CD = MTFC_CMCD '
	  
	  +' INSERT INTO BankAct(ba_clientcd, ba_micr, ba_acttype, ba_actno, Mkrid, Mkrdt, ba_default, ba_proof, ba_ifsccode) '
	  +' SELECT ba_clientcd = MTFC_FillerB, ba_micr, ba_acttype, ba_actno, Mkrid = ''API'', Mkrdt =CONVERT(VARCHAR,GETDATE(),112) , '
	  +' ba_default, ba_proof, ba_ifsccode '
	  +' FROM BankAct(NOLOCK) , MrgTdgFin_Clients(NOLOCK) '
	  +' WHERE ba_clientcd = MTFC_CMCD '
	  +' and ba_default = ''Y'' '
	  +' AND ba_clientcd = '''+@i_vcClient_Code+'''' 
	+' END '
	BEGIN TRY
	  EXEC(@MTFString)
	END TRY
	BEGIN CATCH
	  SET @o_vcErrorFlag = 'S'
	END CATCH
	
	--- EMAIL SEND
	
	IF @i_vcTemplateCode = 'Template1' 		
	BEGIN
	  SET @i_vcInputJsonEMAIL = '{"reqestName": "Email","requestObject": {"ToEmailId": "##ToEmailId##","CCEmailId": "##CCEmailId##","BCCEmailId": "##BCCEmailId##",
      "Subject": "##Subject##","Body": "##Body##","Attachment":[]}}'
      SET @o_vcOutputJsonEMAIL = ''
      
	  SELECT @strBodyText = BodyText, @strCCEMailid = CCEmail, @strBCCEMailid = BCCEmailid , 
	  @strSubject = EmailSubject
      FROM tbl_EmailTemplate(NOLOCK) WHERE RefName = 'RekycCheckerAccept'

      SELECT @strToEmailid = cm_email 
      FROM Client_Master(NOLOCK) WHERE CM_cD = @i_vcClient_Code
          
	  --SET @strToEmailid = 'Vaibhavgarg2005@gmail.com'
           
      SELECT TOP 1 @strCompamnyName = LTRIM(RTRIM(EM_NAME)) from Entity_master(NOLOCK) 
	  WHERE em_cd =(select min(em_cd) from Entity_master(NOLOCK))
	  
	    
	  SELECT @StrCompanyCount = ISNULL(SUM(ISNULL(cnt,0)),0) 
      FROM ( SELECT COUNT(0) Cnt From Entity_master(NOLOCK) 
      WHERE em_bse <> 'N' and isNull(em_bclearingno,'') in ('189') 
      UNION ALL 
      select count(0) Cnt From Entity_master(NOLOCK) 
      Where em_nse <> 'N' and isNull(em_nclearingno,'') in ('07277')) a
	
	  IF @StrCompanyCount > 0
	  BEGIN
	    SELECT @strCompamnyName= LTRIM(RTRIM(em_Name)) FROM Entity_master(NOLOCK)  
	    WHERE em_cd = 'B'
	  END
	  
	  
      SET @strBodyText = REPLACE(REPLACE(@strBodyText,'<<CompanyName>>',@strCompamnyName),'<<Reason>>',@i_vcApprovalReason)
      IF @strToEmailid <> ''
      BEGIN 
 	    SET  @i_vcInputJsonEMAIL = REPLACE(@i_vcInputJsonEMAIL,'##ToEmailId##',@strToEmailid)
        SET  @i_vcInputJsonEMAIL = REPLACE(@i_vcInputJsonEMAIL,'##CCEmailId##',@strCCEMailid)
        SET  @i_vcInputJsonEMAIL = REPLACE(@i_vcInputJsonEMAIL,'##BCCEmailId##',@strBCCEMailid)
        SET  @i_vcInputJsonEMAIL = REPLACE(@i_vcInputJsonEMAIL,'##Subject##',@strSubject)
        SET  @i_vcInputJsonEMAIL = REPLACE(@i_vcInputJsonEMAIL,'##Body##',@strBodyText)
    
	    SET @o_vcOutputJsonEMAIL = ''
	    EXEC stpr_APISendEmail @i_vcInputJsonEMAIL, 'EMAIL', @strThirdPartyURL, @o_vcOutputJsonEMAIL OUTPUT

        INSERT INTO tbl_EMailLog(Code, ToMailid, CCMailid, BCCMailid, EmailSubject, EmailBodyText,
	    AttachmentFile1Name, AttachmentFile1, SendDate,Response,RefName)
	    VALUES(@i_vcClient_Code,@strToEmailid, @strCCEMailid, @strBCCEMailid, @strSubject, @strBodyText, '',
	    '', GETDATE(), isnull(@o_vcOutputJsonEMAIL,0), 'RekycCheckerAccept')
	  END
	END

	
	IF EXISTS(SELECT 1 FROM @tbl_ExecuteCommand WHERE TableName LIKE '%Client_Modification')
	BEGIN
	  set @o_vcErrorMessage = 'Data Updated in Cross/Data Updated in Tradeplus'
	END
	ELSE
	BEGIN
	  SET @o_vcErrorMessage = 'Data Updated in Tradeplus Only'
	END	
	END TRY
    BEGIN CATCH
      SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = ERROR_MESSAGE()
	  --ROLLBACK;
      RETURN 1
    END CATCH  
  END

  ------------ ******  Approve Logic End ***********
END
GO

CREATE PROCEDURE [dbo].[SP_RekycGetCheckerSummary] @i_vcFromDate VARCHAR(10)='', @i_vcToDate VARCHAR(10)='',     
 @i_vcStatus VARCHAR(20) = 'Pending', @o_JsonOutput VARCHAR(MAX) OUTPUT WITH ENCRYPTION  
AS    
BEGIN    
 DECLARE @Status VARCHAR(10) = (SELECT CASE @i_vcStatus WHEN 'Approved' THEN 'Y'     
 WHEN 'Rejected' THEN 'R' ELSE 'N' END)    
    
 BEGIN    
   SET @o_JsonOutput = (SELECT RefNo AS ReqRefNo, ca_cmcd ClientCode, Rtrim(LTrim(cm_name)) ClientName, Replace(convert(VARCHAR(12    
       ), cast(ca_date AS DATE), 106), ' ', '-') DATE, CASE WHEN Sum(TotalPersonal) > 0 THEN     
        'Yes' ELSE 'No' END PersonalDetails, CASE WHEN SUM(TotalAddress) > 0 THEN 'Yes'     
      ELSE 'No' END PersonalAddress, CASE WHEN SUM(TotalIncome) > 0 THEN 'Yes' ELSE 'No' END     
     PersonalIncome, CASE WHEN Sum(TotalEmail) > 0 THEN 'Yes' ELSE 'No' END PersonalEmail, CASE     
      WHEN Sum(TotalMobile) > 0 THEN 'Yes' ELSE 'No' END PersonalMobile, CASE WHEN Sum(    
        TotalNominee) > 0 THEN 'Yes' ELSE 'No' END NomineeDetails, CASE WHEN Sum(    
        TotalBank) > 0 THEN 'Yes' ELSE 'No' END BankDetails, CASE WHEN Sum(TotalDemat) >     
       0 THEN 'Yes' ELSE 'No' END DematDetails, CASE WHEN Sum(TotalSegment) > 0 THEN 'Yes'     
      ELSE 'No' END SegmentDetails  , @i_vcStatus as Status  , Replace(convert(VARCHAR(12),cast(CheckerDate AS DATE), 106), ' ', '-') as 'Checker Date',
	  [RejectionReason]
    FROM (    
     SELECT Isnull(Max(ca_Nfiller3), 1000) AS RefNo, ca_cmcd, cm_name, ca_date, C.Mkrdt as CheckerDate, (    
       CASE WHEN (    
          SUBSTRING(ltrim(ca_field), 1, 2) = 'ck' OR SUBSTRING(ltrim(    
            ca_field), 1, 2) = 'cm'    
          ) THEN 1 ELSE 0 END    
       ) AS TotalPersonal, (    
       CASE WHEN (    
          SUBSTRING(ltrim(ca_field), 1, 3) = 'cm_' AND Left(ca_desc, 7) =     
          'Address'    
          ) THEN 1 ELSE 0 END    
       ) AS TotalAddress, (    
       CASE WHEN (SUBSTRING(ltrim(ca_field), 1, 7) = 'cm_gros'    
          ) THEN 1 ELSE 0 END    
       ) AS TotalIncome, (    
       CASE WHEN (SUBSTRING(ltrim(ca_field), 1, 7) = 'cm_emai'    
          ) THEN 1 ELSE 0 END    
       ) AS TotalEmail, (    
       CASE WHEN (SUBSTRING(ltrim(ca_field), 1, 7) = 'cm_mobi'    
          ) THEN 1 ELSE 0 END    
       ) AS TotalMobile, (    
       CASE WHEN (SUBSTRING(ltrim(ca_field), 1, 2) = 'cn'    
          ) THEN 1 ELSE 0 END    
       ) AS TotalNominee, (    
       CASE WHEN (SUBSTRING(ltrim(ca_field), 1, 2) = 'ba'    
          ) THEN 1 ELSE 0 END    
       ) AS TotalBank, (    
       CASE WHEN (SUBSTRING(ltrim(ca_field), 1, 2) = 'da'    
          ) THEN 1 ELSE 0 END    
       ) AS TotalDemat, (    
       CASE WHEN (SUBSTRING(ltrim(ca_field), 1, 2) = 'ce'    
          ) THEN 1 ELSE 0 END    
       ) AS TotalSegment, [RejectionReason] = rm_desc
     FROM Client_ModifyAPI(NOLOCK) A, Client_master(NOLOCK) B, Client_ReKycMain(NOLOCK) C    
     WHERE A.ca_cmcd = B.cm_cd AND ca_cmcd = rm_cmcd AND ca_Nfiller3 = rm_refno AND ca_Tplus =     
      @status AND ca_field NOT IN ('cm_country', 'cm_sex')     
      AND ((@status = 'N' AND rm_step = 5) OR @status <> 'N')     
      AND ((@status <> 'N' AND (C.mkrdt BETWEEN cast(@i_vcFromDate as date) AND cast(@i_vcToDate as date))    
        ) OR @status = 'N')    And C.rm_RequestType = 'ReKYC'
     GROUP BY ca_cmcd, cm_name, ca_date, SUBSTRING(ltrim(ca_field), 1, 2), SUBSTRING(ltrim(    
        ca_field), 1, 7), SUBSTRING(ltrim(ca_field), 1, 3), Left(ca_desc, 7)  , C.mkrdt, rm_desc, ca_oldvalue, ca_field
     ) A    
    GROUP BY ca_cmcd, cm_name, ca_date, RefNo, CheckerDate, [RejectionReason]
    ORDER BY ca_date DESC    
    FOR JSON Path)    
 END    
 SELECT @o_JsonOutput = Isnull(@o_JsonOutput, '')    
END    
GO

CREATE PROCEDURE [SP_ReKyc_CheckValidation] @i_vcJsonString NVARCHAR(MAX), @i_vcClientCode VARCHAR(20), 
@i_vcRefNo numeric(10),
@i_vcValidationType VARCHAR(20), @o_vcErrorFlag VARCHAR(1) OUTPUT, 
@o_vcErrorMessage VARCHAR(500) OUTPUT, @o_vcJsonOutput VARCHAR(MAX) OUTPUT, @i_vcRequestMode VARCHAR(1) = 'U',
@i_vcPassingValueType VARCHAR(5) = 'JSON' , @i_vcTemplateCode VARCHAR(20) = 'Template1'
WITH ENCRYPTION
AS 
BEGIN
  BEGIN TRY

  DECLARE @WrongID VARCHAR(100)='', @strtradeplustempdb VARCHAR(50)='', @i_vcOldJsonString VARCHAR(MAX)='', @strString VARCHAR(MAX)=''
  SET @o_vcErrorFlag = 'S'
  SET @o_vcErrorMessage = 'Process Completed'
  SET @o_vcJsonOutput = '{}'
  DECLARE  @TBL_OutputJSON TABLE(ErrorTag VARCHAR(100), ErrorMessage VARCHAR(MAX))
  
  DECLARE @jsonCutterOutput VARCHAR(MAX)='', @strDanSql NVARCHAR(MAX)=''
  DECLARE @JsonCutterXML XML
  
  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB'

  IF @i_vcValidationType = 'MAKER'
  BEGIN
    EXEC [dbo].[SP_ReKyc_GetData] @i_vcClientCode,  @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT, 
	@o_vcJsonOutput OUTPUT, @i_vcTemplateCode, 'X'
  
    IF @o_vcErrorFlag = 'S'
    BEGIN
      SET @i_vcOldJsonString = @o_vcJsonOutput 
	  SET @o_vcJsonOutput = '{}'
    END
    ELSE 
    BEGIN  
      RETURN 1
    END	
  
    IF OBJECT_ID('tempdb..#TBL_OldJson') IS NOT NULL
      DROP TABLE #TBL_OldJson
   
    CREATE TABLE #TBL_OLDJson(SerialNo INT, ColumnName VARCHAR(50), ColumnValue VARCHAR(MAX), ValueTypeColumn INT,
    UpdateFlag VARCHAR(1), MasterTag VARCHAR(50), JsonLevel INT, MasterLevel INT)
    
	IF @i_vcPassingValueType = 'JSON' AND @i_vcTemplateCode = 'Template1'
	BEGIN
      BEGIN TRY
        SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@i_vcOldJsonString+''' , @jsonCutterOutput OUTPUT';
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
        INSERT INTO #TBL_OldJson(SerialNo, ColumnName, ColumnValue, ValueTypeColumn,  MasterTag, JSONLEVEL, MASTERLEVEL)
		SELECT X1.* FROM(
        SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
	    JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
        JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
	    JsonCutter.value('(XTYPE)[1]', 'INT') AS ValueTypeColumn,
		JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
	    JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
	    JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
        FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1
      END TRY
      BEGIN CATCH
        SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'OLD JSON '+ERROR_MESSAGE()
	    RETURN 1
      END CATCH
    END	   
	
	IF @i_vcTemplateCode = 'Template1' AND ISNULL(@i_vcRefNo,0) <> 0
	BEGIN 
	  IF NOT EXISTS(SELECT 1 FROM Client_ModifyAttach(nolock) WHERE ma_cmcd =  @i_vcClientCode 
	  and ma_filename IN('Aadhaar') and ma_refno = @i_vcRefNo)
	  BEGIN
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Aadhaar Not Found '
		set @o_vcJsonOutput = '{"Response":[{"ErrorTag":"E","ErrorMessage":"##MESSAGE##"}]}'
	    set @o_vcJsonOutput = REPLACE(@o_vcJsonOutput,'##MESSAGE##',@o_vcErrorMessage)
	    RETURN 1 
	  END
	  
	  IF NOT EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE  ma_cmcd =  @i_vcClientCode 
	  AND  ma_filename IN('PHOTO') and ma_refno = @i_vcRefNo)
	  BEGIN
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'PHOTO Not Found '
		set @o_vcJsonOutput = '{"Response":[{"ErrorTag":"E","ErrorMessage":"##MESSAGE##"}]}'
	    set @o_vcJsonOutput = REPLACE(@o_vcJsonOutput,'##MESSAGE##',@o_vcErrorMessage)
	    RETURN 1 
	  END
    END
	
  	---Closed on validation -
  
    DECLARE @dtCloseonDate VARCHAR(8)=''
    SELECT @dtCloseonDate = IsNull(cn_polexpval,'') 
    FROM Client_Nominee(NOLOCK) 
    WHERE cn_cd = @i_vcClientCode
  
    IF @dtCloseonDate <> ''  
    BEGIN
      SET @o_vcErrorFlag  = 'E'
	  SET @o_vcErrorMessage = 'Closed on '+replace(convert(VARCHAR,cast(@dtCloseonDate as date),105),'-','/')
	  set @o_vcJsonOutput = '{"Response":[{"ErrorTag":"E","ErrorMessage":"##MESSAGE##"}]}'
	  set @o_vcJsonOutput = REPLACE(@o_vcJsonOutput,'##MESSAGE##',@o_vcErrorMessage)
      RETURN 1	
    END
  
    IF EXISTS(sELECT 1 FROM CLIENT_MASTER(NOLOCK) WHERE CM_CD =  @i_vcClientCode AND cm_freezeyn = 'A')
    BEGIN
      SET @o_vcErrorFlag  = 'E'
	  SET @o_vcErrorMessage = 'Client Status is Freeze for All'
	  set @o_vcJsonOutput = '{"Response":[{"ErrorTag":"E","ErrorMessage":"##MESSAGE##"}]}'
	  set @o_vcJsonOutput = REPLACE(@o_vcJsonOutput,'##MESSAGE##',@o_vcErrorMessage)
      RETURN 1	
    END
  
    DECLARE @StrQuery VARCHAR(MAX)=''
    CREATE TABLE #tbl_JsonFile  (SerialNo INT,ColumnName VARCHAR(50),ColumnValue VARCHAR(MAX), 
    ValueTypeColumn INT,UpdateFlag VARCHAR(1), MasterTag VARCHAR(50),JsonLevel INT,
	MasterLevel INT, MOldValue VARCHAR(MAX) NOT NULL DEFAULT '', MRequireTag VARCHAR(1) NOT NULL DEFAULT '') 
   
	IF @i_vcPassingValueType = 'JSON'
	BEGIN
	  SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@i_vcJsonString+''' , @jsonCutterOutput OUTPUT';
      EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
      SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
      INSERT INTO #tbl_JsonFile(SerialNo, ColumnName, ColumnValue, ValueTypeColumn,  MasterTag, JSONLEVEL, MASTERLEVEL)
	  SELECT X1.* FROM(
      SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
	  JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
      JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
	  JsonCutter.value('(XTYPE)[1]', 'INT') AS ValueTypeColumn,
	  JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
	  JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
	  JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
      FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1
	END  
	ELSE
	BEGIN
	  DECLARE @i_vcPayloadJson XML=CAST(@i_vcJsonString AS XML)
	  
	  DECLARE @tbl_xmlCutter TABLE(SerialNo int identity(1,1), 
      ColumnName VARCHAR(100), ColumnValue VARCHAR(MAX), ValueTypeColumn INT,
      UpdateFlag VARCHAR(1), 
      MasterTag VARCHAR(50),
      JsonLevel INT, MasterLevel INT)

      DECLARE @sql NVARCHAR(MAX), @XCOUNTER1 INT=0, @OLDTAG VARCHAR(50) ='',
      @TagCounter INT = 0, @tabcount1 INT=0
      DECLARE @tabname VARCHAR(MAX)=''
  
      DECLARE db_CursorxmlTag CURSOR FOR         
      SELECT TagName, count(*) as counta From(
      SELECT c.value('local-name(.)', 'NVARCHAR(MAX)') as TagName
      FROM @i_vcPayloadJson.nodes('//*[(*)]') AS t(c)) x1 where TagName  NOT IN('root','GuardianDetails')
      group by TagName
 
      OPEN db_CursorxmlTag       
      FETCH NEXT FROM db_CursorxmlTag INTO @tabname, @tabcount1
      WHILE @@FETCH_STATUS = 0     
      BEGIN
        SET @TagCounter = 1
	    SET @XCOUNTER1 = @XCOUNTER1 + 1
	    WHILE @TagCounter <=@tabcount1
	    BEGIN
	      SET @sql = 'SELECT '''+@tabname+''' as MasterTag,  
          c.value(''local-name(.)'', ''NVARCHAR(MAX)'') AS ColumnName,
          c.value(''(./text())[1]'', ''NVARCHAR(MAX)'') AS ColumnValue, ''1'' AS ValueTypeColumn, UpdateFlag = ''N'', 
	      MasterLevel =  '''+CAST(@XCOUNTER1 AS VARCHAR)+'''
	      , JsonLevel = '''+CAST(@TagCounter AS VARCHAR)+'''
          FROM @i_vcPayloadJson.nodes(''/root/'+@tabname+'['+CAST(@TagCounter AS VARCHAR)+']/*'') AS t(c) '
	     INSERT INTO @tbl_xmlCutter(MasterTag, ColumnName, ColumnValue, ValueTypeColumn, UpdateFlag, MasterLevel, JsonLevel)
	     EXEC sp_executesql @sql, N'@i_vcPayloadJson XML', @i_vcPayloadJson
	     SET @TagCounter = @TagCounter+1
	    END
	    SET @OLDTAG = @tabname
	    FETCH NEXT FROM db_CursorxmlTag INTO @tabname, @tabcount1
      END        
      CLOSE db_CursorxmlTag        
      DEALLOCATE db_CursorxmlTag
   

      DECLARE db_CursorxmlTagDTL CURSOR FOR         
      select TagName, COUNT(*) AS COUNT1  From(
      SELECT c.value('local-name(.)', 'NVARCHAR(MAX)') as TagName
      FROM @i_vcPayloadJson.nodes('//*[(*)]') AS t(c)) x1 where TagName  ='GuardianDetails'
      GROUP BY TagName
 
      OPEN db_CursorxmlTagDTL       
      FETCH NEXT FROM db_CursorxmlTagDTL INTO @tabname , @tabcount1 
      WHILE @@FETCH_STATUS = 0     
      BEGIN
        SET @TagCounter = 1
	    SET @XCOUNTER1 = @XCOUNTER1 + 1
	    WHILE @TagCounter <=@tabcount1
	    BEGIN
	       SET @sql = 'SELECT '''+@tabname+''' as MasterTag,  
           c.value(''local-name(.)'', ''NVARCHAR(MAX)'') AS ColumnName,
           c.value(''(./text())[1]'', ''NVARCHAR(MAX)'') AS ColumnValue, ''1'' AS ValueTypeColumn, UpdateFlag = ''N'', 
	       MasterLevel = '''+CAST(@TagCounter AS VARCHAR)+''' , JsonLevel = ''1''
           FROM @i_vcPayloadJson.nodes(''/root/NomineeDetails['+CAST(@TagCounter AS VARCHAR)+']'+'/'+@tabname+'[1]/*'') AS t(c) '
	       INSERT INTO @tbl_xmlCutter(MasterTag, ColumnName, ColumnValue, ValueTypeColumn, UpdateFlag, MasterLevel, JsonLevel)
	       EXEC sp_executesql @sql, N'@i_vcPayloadJson XML', @i_vcPayloadJson
	       SET @TagCounter = @TagCounter+1
	    END
	    SET @OLDTAG = @tabname
	    FETCH NEXT FROM db_CursorxmlTagDTL INTO @tabname , @tabcount1
      END        
      CLOSE db_CursorxmlTagDTL        
      DEALLOCATE db_CursorxmlTagDTL
	  
	  
	  
      INSERT INTO #tbl_JsonFile( SerialNo,ColumnName,ColumnValue,ValueTypeColumn,UpdateFlag,MasterTag,JsonLevel,MasterLevel )
      SELECT SerialNo,ColumnName,ColumnValue = ISNULL(ColumnValue,''),
      ValueTypeColumn,UpdateFlag,MasterTag,JsonLevel,MasterLevel
      FROM @tbl_xmlCutter
      ORDER BY SerialNo
	END
	
	
	IF OBJECT_ID('tempdb..#tbl_MainJson') IS NOT NULL
    drop table #tbl_MainJson

    -- FOR SINGLE TABLE RECORD
	
	IF @i_vcTemplateCode = 'Template2'
	BEGIN
	  UPDATE A SET MasterTag = B.MasterJsonTag, JsonLevel = 1, MasterLevel = 1
	  FROM #tbl_JsonFile A, tbl_ReKycAuditColumnMapping_new B 
	  WHERE B.JsonKey = A.ColumnName
	  AND A.MasterTag = ''
	  and TemplateCode = @i_vcTemplateCode
	  
      UPDATE A SET JsonLevel = 2, MasterLevel = 1
      FROM #tbl_JsonFile A, tbl_ReKycAuditColumnMapping_new B
      WHERE A.ColumnName = B.JsonKey
      AND B.FieldDescp like '%NomineeDetails(2)%'
      and TemplateCode = @i_vcTemplateCode

      UPDATE A SET JsonLevel = 3, MasterLevel = 1
      FROM #tbl_JsonFile A, tbl_ReKycAuditColumnMapping_new B
      WHERE A.ColumnName = B.JsonKey
      AND B.FieldDescp LIKE '%NomineeDetails(3)%'
	  and TemplateCode = @i_vcTemplateCode

      UPDATE A SET JsonLevel = 1, MasterLevel = 2
      FROM #tbl_JsonFile A, tbl_ReKycAuditColumnMapping_new B
      WHERE A.ColumnName = B.JsonKey
      AND B.FieldDescp LIKE '%GuardianDetails(2)%'
	  and TemplateCode = @i_vcTemplateCode

      UPDATE A SET JsonLevel = 1, MasterLevel = 3
      FROM #tbl_JsonFile A, tbl_ReKycAuditColumnMapping_new B
      WHERE A.ColumnName = B.JsonKey
      AND B.FieldDescp LIKE '%GuardianDetails(3)%'
	  and TemplateCode = @i_vcTemplateCode
  
	END
    ELSE if @i_vcTemplateCode = 'ONLYCLOSURE'
	begin
	  UPDATE A SET MasterTag = B.MasterJsonTag, JsonLevel = 1, MasterLevel = 1
	  FROM #tbl_JsonFile A, tbl_ReKycAuditColumnMapping_new B 
	  WHERE B.JsonKey = A.ColumnName
	  AND A.MasterTag = ''
	  and TemplateCode = @i_vcTemplateCode
	END
	
	
	
	UPDATE Y SET Y.MOldValue = X.ColumnValue
	FROM #TBL_OldJson x, #tbl_JsonFile Y
	WHERE X.ColumnName = Y.ColumnName
	--AND X.ColumnValue = Y.ColumnValue
	AND Y.MasterTag = 'PersonalDetails'
	
	DECLARE @isNomineeModified VARCHAR(20)='False', @IsBankModified VARCHAR(20)='False', @IsDematModified VARCHAR(20)='False'
    SELECT @isNomineeModified = ColumnValue FROM #tbl_JsonFile where columnname = 'isNomineeModified'
    SELECT @IsBankModified = ColumnValue FROM #tbl_JsonFile where columnname = 'IsBankModified'
    SELECT @IsDematModified = ColumnValue FROM #tbl_JsonFile where columnname = 'IsDematModified'	
	
  
    SELECT MasterJsonTag, M.JsonKey, M.FieldName, M.TableName, M.RecordType, M.DefaultValueTag,	
    DefaultValue = IIF(M.DefaultValue='CURRDATE',CONVERT(VARCHAR,GETDATE(),112),DefaultValue),	
    M.MandatoryTag, ColumnName = ISNULL(X.ColumnName,''), ColumnValue  =ISNULL(X.ColumnValue,''), 
	JsonLevel = ISNULL(JsonLevel,0), MasterLevel = ISNULL(MasterLevel,0),
    m.ValidationValue , m.ValidationMessage, MOldValue = ISNULL(X.MOldValue,'')
    INTO #tbl_MainJson
    FROM tbl_ReKycAuditColumnMapping_new(NOLOCK) M LEFT OUTER JOIN #tbl_JsonFile x 
	ON(M.MasterJsonTag = IIF(X.MasterTag='',M.MasterJsonTag,X.MasterTag)
    AND M.JsonKey = X.ColumnName) 
    WHERE  ((DefaultValueTag IN('B','I') AND @i_vcRequestMode = 'I') OR 
	(DefaultValueTag IN('B','U') AND @i_vcRequestMode = 'U'))
	AND TemplateCode  = @i_vcTemplateCode
    ORDER BY (CASE WHEN TABLENAME = 'Client_master' 
    THEN 1 WHEN TABLENAME='client_info' THEN 2 ELSE 3 END), M.SERIALNO
    
	--SELECT * FROM #tbl_MainJson
	
    IF isnull(@isNomineeModified,'') = ''
    BEGIN
      SELECT @isNomineeModified = ColumnValue FROM #tbl_JsonFile where MasterTag IN('NomineeDetails','GuardianDetails') 
	  AND ColumnName = 'IsInserted' AND ColumnValue = 'true'  
	
	  IF isnull(@isNomineeModified,'') = ''
	  BEGIN
	    SELECT @isNomineeModified = ColumnValue FROM #tbl_JsonFile where MasterTag IN('NomineeDetails','GuardianDetails') 
	    AND ColumnName = 'IsModified' AND ColumnValue = 'true'  
	  END
    END
  
    IF isnull(@IsBankModified,'') = ''
    BEGIN
      SELECT @IsBankModified = ColumnValue FROM #tbl_JsonFile where MasterTag IN('BankDetails') 
	  AND ColumnName = 'IsInserted' AND ColumnValue = 'true'  
    END
	
	IF isnull(@IsDematModified,'') = ''
    BEGIN
      SELECT @IsDematModified = ColumnValue FROM #tbl_JsonFile where MasterTag IN('DematDetails') 
	  AND ColumnName = 'IsInserted' AND ColumnValue = 'true'  
    END
	
	IF ISNULL(@isNomineeModified,'') = ''
	BEGIN
	  SET @isNomineeModified ='false'
	END 
	
	
	if isnull(@IsBankModified,'') = ''
	begin
	  set @IsBankModified ='false'
	end 
	
	IF ISNULL(@IsDematModified,'') = ''
	BEGIN
	  SET @IsDematModified ='false'
	END 
	
	--SELECT @isNomineeModified, @IsDematModified, @IsBankModified

    UPDATE A SET A.ValidationValue = CASE WHEN A.MasterJsonTag = 'BankDetails' and @IsBankModified = 'False' then ''
	WHEN A.MasterJsonTag in('NomineeDetails','GuardianDetails') and @isNomineeModified = 'False' then ''
	WHEN A.MasterJsonTag = 'DematDetails' and @IsDematModified = 'False' then '' else A.ValidationValue end
	FROM #tbl_MainJson A
	
	
    DECLARE @strColumnValue VARCHAR(MAX)='', @strColumnName VARCHAR(100)='', @strValidationValue VARCHAR(500), 
	@strValidationmessage VARCHAR(MAX)=''
    DECLARE CurValidation CURSOR FOR
    SELECT ColumnValue = (case when ColumnValue = 'True' THEN 'Y'
	when ColumnValue = 'False' THEN 'N' ELSE ColumnValue END), ColumnName, ValidationValue, ValidationMessage 
    FROM #tbl_MainJson m where ValidationValue <> '' AND ColumnValue <> ''

    OPEN CurValidation;
    FETCH NEXT FROM CurValidation INTO @strColumnValue, @strColumnName, @strValidationValue, @strValidationmessage
    WHILE @@FETCH_STATUS = 0
    BEGIN
      IF NOT EXISTS(select 1 from(
      SELECT VALUE AS ColumnValue FROM dbo.ReturnTable(@strValidationValue,'|')) x1 
	  WHERE x1.ColumnValue = @strColumnValue)
      BEGIN
        INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	    VALUES('E',ISNULL(@strColumnName,'')+' Value Should be '+iif(@strValidationmessage='',@strValidationValue,@strValidationmessage))
      END

      FETCH NEXT FROM CurValidation INTO  @strColumnValue, @strColumnName, @strValidationValue, @strValidationmessage
    END;
    CLOSE CurValidation;
    DEALLOCATE CurValidation;

  --- VALIDATION QUERY

  IF EXISTS(sELECT 1 FROM CLIENT_MASTER(NOLOCK) WHERE CM_CD = @i_vcClientCode) 
  AND @i_vcRequestMode = 'I'
  BEGIN
    INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	VALUES('E','Client Already Available - '+@i_vcClientCode)
  END
  
  

  DECLARE @tbl_Resultoutput TABLE(Scount INT)
  DECLARE @SQueryFieldName VARCHAR(100)='',  @SQueryColumnValue VARCHAR(MAX)='', 
  @SQueryValidationQuery VARCHAR(MAX), @SQueryJsonLevel INT, @SQueryMasterJsonTag VARCHAR(50)
  DECLARE @SQuerydtlFieldName VARCHAR(100)= '',  @SQuerydtlcolumnvalue VARCHAR(MAX)='', @Qcount INT=0
  	
  DECLARE CurqueryValidation CURSOR FOR
  SELECT FieldName,  ColumnValue, ValidationQuery, 
  JsonLevel, MasterJsonTag
  FROM tbl_ReKycAuditColumnMapping_new(NOLOCK) M LEFT OUTER JOIN #tbl_JsonFile x 
  ON(M.MasterJsonTag = X.MasterTag
  AND M.JsonKey = X.ColumnName) 
  WHERE (CASE WHEN MasterJsonTag = 'BankDetails' 
  and @IsBankModified = 'False' then ''
  WHEN MasterJsonTag in('NomineeDetails','GuardianDetails') and @isNomineeModified = 'False' then ''
  WHEN MasterJsonTag = 'DematDetails' and @IsDematModified = 'False' then '' else ValidationQuery end) <> '' 
  AND ISNULL(ColumnValue,'')<> ''
  AND ((DefaultValueTag IN('B','I') AND @i_vcRequestMode = 'I') OR 
	(DefaultValueTag IN('B','U') AND @i_vcRequestMode = 'U'))
  AND TemplateCode  = @i_vcTemplateCode	
  OPEN CurqueryValidation
  FETCH NEXT FROM CurqueryValidation INTO @SQueryFieldName,  @SQueryColumnValue, @SQueryValidationQuery, 
  @SQueryJsonLevel, @SQueryMasterJsonTag
  WHILE @@FETCH_STATUS = 0
  BEGIN
     SET @SQuerydtlFieldName = ''
     SET @SQuerydtlcolumnvalue = ''
	
     DECLARE CurqueryValidationdtl CURSOR FOR
     select FieldName, columnvalue  from #tbl_MainJson 
     where MasterJsonTag = @SQueryMasterJsonTag and jsonlevel = @SQueryJsonLevel
     OPEN CurqueryValidationdtl;
     FETCH NEXT FROM CurqueryValidationdtl INTO @SQuerydtlFieldName, @SQuerydtlcolumnvalue
     WHILE @@FETCH_STATUS = 0
     BEGIN

       SET @SQueryValidationQuery = REPLACE(@SQueryValidationQuery,LTRIM(RTRIM('<<'+LTRIM(RTRIM(@SQuerydtlFieldName))+'>>')),''+@SQuerydtlcolumnvalue+'')
       FETCH NEXT FROM CurqueryValidationdtl INTO @SQuerydtlFieldName, @SQuerydtlcolumnvalue
     END
     CLOSE CurqueryValidationdtl;
     DEALLOCATE CurqueryValidationdtl
	 SET @Qcount = 0
	 DELETE FROM @tbl_Resultoutput
	 BEGIN TRY
	   INSERT INTO @tbl_Resultoutput(SCOUNT)
	   EXEC(@SQueryValidationQuery)
	 END TRY
     BEGIN CATCH
	   INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	   VALUES('E','Issue in Query '+@SQueryFieldName+' '+@SQueryValidationQuery)
     END CATCH
	 SELECT @Qcount = SCOUNT FROM @tbl_Resultoutput
     IF @Qcount = 0
	 BEGIN
	   INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	   VALUES('E',ISNULL(@SQueryFieldName,'')+'( '+ISNULL(@SQueryColumnValue,'')+' ) Value Not Found')
	 END
     FETCH NEXT FROM CurqueryValidation INTO @SQueryFieldName,  @SQueryColumnValue, 
	 @SQueryValidationQuery, @SQueryJsonLevel, @SQueryMasterJsonTag  
   END;
   CLOSE CurqueryValidation;
   DEALLOCATE CurqueryValidation;

    
 
   DECLARE @strdtColumnName VARCHAR(100) = '',  @strdtColumnvALUE varchar(max)=''
   DECLARE DATEValidation CURSOR FOR
   SELECT COLUMNNAME, COLUMNVALUE FROM #tbl_JsonFile X WHERE EXISTS(
   SELECT 1 FROM tbl_ReKycAuditColumnMapping_new(NOLOCK) WHERE (JsonKey like '%date%' or JsonKey like '%dob%')
   AND JsonKey <> 'UpdateCDSLNSDL'
   and FieldName <> '' and TemplateCode = @i_vcTemplateCode	
   AND MasterJsonTag = X.MasterTag
   AND JsonKey = X.ColumnName AND ((DefaultValueTag IN('B','I') 
   AND @i_vcRequestMode = 'I') 
   OR (DefaultValueTag IN('B','U') AND @i_vcRequestMode = 'U')))
   AND ISNULL(ColumnValue,'')<> ''
   
   OPEN DATEValidation
   FETCH NEXT FROM DATEValidation INTO @strdtColumnName,  @strdtColumnvALUE
   WHILE @@FETCH_STATUS = 0
   BEGIN
    IF ISDATE(@strdtColumnvALUE) = 0
	BEGIN
	  INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	  VALUES('INVALID DATE',@strdtColumnName+' Invalid Date')
	END
	
    FETCH NEXT FROM DATEValidation INTO @strdtColumnName,  @strdtColumnvALUE 
   END;
   CLOSE DATEValidation;
   DEALLOCATE DATEValidation;
  
    ---- CLIENT CODE CHECK
  
    IF NOT EXISTS (select 1 from client_master where cm_cd in(
    SELECT ColumnValue FROM #tbl_JsonFile 
	WHERE MasterTag = 'PersonalDetails' AND ColumnName = 'ClientCode')) AND @i_vcRequestMode <> 'I'
    BEGIN
	  INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	  VALUES('PersonalDetails','Client not found in Client master table')
    END
	
	
	IF OBJECT_ID('tempdb..#tbl_MainJson') IS NOT NULL
    DROP TABLE #tbl_MainJson


	IF EXISTS(SELECT 1 FROM #tbl_JsonFile X WHERE MasterTag = 'BankDetails' AND ColumnName = 'IsDefaultNew'
	AND columnvalue = 'True' and EXISTS(SELECT 1 FROM #tbl_JsonFile X1
      WHERE MasterTag = 'BankDetails' and ColumnName = 'IsDefault' and JSONLEVEL = X.JSONLEVEL
	  AND ColumnValue = 'True'))
    BEGIN
	  UPDATE X SET X.ColumnValue = 'False' 
      FROM #tbl_JsonFile X
	  WHERE X.MasterTag = 'BankDetails' and ColumnName = 'IsDefault' AND ColumnValue = 'True'
	  AND NOT EXISTS(SELECT 1 FROM #tbl_JsonFile X1 WHERE MasterTag = 'BankDetails' AND ColumnName = 'IsDefaultNew'
	  AND JSONLEVEL = X.JSONLEVEL AND  columnvalue = 'True')
    END	
	
    IF EXISTS(SELECT 1 FROM #tbl_JsonFile X WHERE MasterTag = 'DematDetails' AND ColumnName = 'IsDefaultNew'
	AND columnvalue = 'True' and EXISTS(SELECT 1 FROM #tbl_JsonFile X1
      WHERE MasterTag = 'DematDetails' and ColumnName = 'IsDefault' and JSONLEVEL = X.JSONLEVEL
	  AND ColumnValue = 'True'))
    BEGIN
	  UPDATE X SET X.ColumnValue = 'False' 
      FROM #tbl_JsonFile X
	  WHERE X.MasterTag = 'DematDetails' and ColumnName = 'IsDefault' AND ColumnValue = 'True'
	  AND NOT EXISTS(SELECT 1 FROM #tbl_JsonFile X1 WHERE MasterTag = 'DematDetails' AND ColumnName = 'IsDefaultNew'
	  AND JSONLEVEL = X.JSONLEVEL AND  columnvalue = 'True')
    END	
	
	--- Invalid Country

    IF NOT EXISTS (SELECT * FROM Country_master(NOLOCK)  WHERE CT_NAME in(
    SELECT ColumnValue FROM #tbl_JsonFile
    WHERE MasterTag = 'PersonalDetails' AND ColumnName = 'CorrCountry')) 
	AND @i_vcTemplateCode <> 'ONLYCLOSURE' 
    BEGIN
	  IF ((EXISTS(SELECT 1 FROM #tbl_JsonFile
         WHERE MasterTag = 'PersonalDetails' 
	     AND MOldValue <> ColumnValue 
		 AND ColumnName IN(SELECT JSONKEY FROM tbl_ReKycAuditColumnMapping_new(NOLOCK)
		 WHERE RequireTag = 1 AND TEMPLATECODE = @i_vcTemplateCode)) 
		 and @i_vcTemplateCode = 'Template1' and @i_vcRequestMode = 'U') OR @i_vcRequestMode = 'I')
	  BEGIN
	    INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	    VALUES('PersonalDetails','Country not found')
	  END	
    END

    --- invalid state

    IF NOT EXISTS (SELECT * FROM State_Master (NOLOCK)  WHERE st_State in(
    SELECT ColumnValue FROM #tbl_JsonFile
    WHERE MasterTag = 'PersonalDetails' AND ColumnName = 'CorrState')) 
	AND @i_vcTemplateCode <> 'ONLYCLOSURE'
    BEGIN
	 IF ((EXISTS(SELECT 1 FROM #tbl_JsonFile
         WHERE MasterTag = 'PersonalDetails' 
	     AND MOldValue <> ColumnValue 
		 AND ColumnName IN(SELECT JSONKEY FROM tbl_ReKycAuditColumnMapping_new(NOLOCK)
		 WHERE RequireTag = 1 AND TEMPLATECODE = @i_vcTemplateCode)) 
		 and @i_vcTemplateCode = 'Template1' and @i_vcRequestMode = 'U') OR @i_vcRequestMode = 'I')
	  BEGIN	 
        INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	    VALUES('PersonalDetails','State not found')
	  END	
    END
	 
	--- invalid Email id 

    if EXISTS(SELECT  1 FROM #tbl_JsonFile
    WHERE MasterTag = 'PersonalDetails' AND ColumnName = 'Email' 
	and IIF((ColumnValue LIKE '%_@_%._%'),'Valid','Invalid') = 'Invalid' 
	AND ((MOldValue <> ColumnValue and @i_vcRequestMode = 'U') 
	OR  @i_vcRequestMode = 'I'))
    BEGIN
	  INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	  VALUES('PersonalDetails','Invalid Email id')
    END
	
	
	IF EXISTS(SELECT  1 FROM #tbl_JsonFile xx
    WHERE MasterTag = 'PersonalDetails' AND ColumnName LIKE '%PAN%'
	and IIF((ColumnValue LIKE '[A-Z][A-Z][A-Z][A-Z][A-Z][0-9][0-9][0-9][0-9][A-Z]' 
	AND LEN(ColumnValue) = 10),'Valid','Invalid') = 'Invalid'
	and ColumnValue <> '' 
	AND ((MOldValue <> ColumnValue and @i_vcRequestMode = 'U') 
	OR  @i_vcRequestMode = 'I'))
    BEGIN
      INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	  VALUES('PersonalDetails','Invalid PAN No')
    END
	
	if EXISTS(SELECT  1 FROM #tbl_JsonFile
    WHERE MasterTag = 'PersonalDetails' AND ColumnName = 'Mobile' and len(ColumnValue)<> 10  
    AND ((MOldValue <> ColumnValue and @i_vcRequestMode = 'U') 
	OR  @i_vcRequestMode = 'I'))
    BEGIN
	  INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	  VALUES('PersonalDetails','Invalid Mobile No')
    END
    --- Invalid Nom Guardian PAN  
    
	IF ((@isNomineeModified = 'true' and @i_vcRequestMode = 'U' AND @i_vcTemplateCode = 'Template1')
	OR (@isNomineeModified = 'False' and @i_vcRequestMode <> 'U' AND @i_vcTemplateCode <> 'Template1') 
	OR @i_vcRequestMode = 'I')
    BEGIN	
      
	  IF EXISTS(SELECT  1 FROM #tbl_JsonFile xx
      WHERE MasterTag = 'GuardianDetails' 
	  AND (ColumnName = 'NomGuardianPAN' OR ColumnName LIKE '%PAN%')
	  and IIF((ColumnValue LIKE '[A-Z][A-Z][A-Z][A-Z][A-Z][0-9][0-9][0-9][0-9][A-Z]' 
	  AND LEN(ColumnValue) = 10),'Valid','Invalid') = 'Invalid'
	  and ColumnValue <> '') 
      BEGIN
        INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	    VALUES('GuardianDetails','Invalid Nom Guardian PAN No')
      END
	
	  IF EXISTS(SELECT  1 FROM #tbl_JsonFile xx
      WHERE MasterTag = 'NomineeDetails' AND (ColumnName = 'NomineePAN' OR ColumnName LIKE '%PAN%')
	  and IIF((ColumnValue LIKE '[A-Z][A-Z][A-Z][A-Z][A-Z][0-9][0-9][0-9][0-9][A-Z]' 
	  AND LEN(ColumnValue) = 10),'Valid','Invalid') = 'Invalid'
	  AND ColumnValue <> '')
      BEGIN
        INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	    VALUES('NomineeDetails','Invalid Nom PAN No')
      END
	
		
	  --- Invalid Nom Guardian PAN  
	
	  IF EXISTS(SELECT 1 FROM #tbl_JsonFile xx
      WHERE MasterTag = 'GuardianDetails' AND (ColumnName = 'NomGuardianUID' OR ColumnName LIKE '%UID%')
	  and LEN(ColumnValue) <> 12 AND ColumnValue <> '')
      BEGIN
        INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	    VALUES('GuardianDetails','Invalid Nom Guardian UID')
      END
	
	
	  IF EXISTS(SELECT 1 FROM #tbl_JsonFile xx
      WHERE MasterTag = 'NomineeDetails' AND (ColumnName = 'NomineeUID' OR ColumnName LIKE '%UID%')
	  and LEN(ColumnValue) <> 12 AND ColumnValue <> '')
      BEGIN
        INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	    VALUES('NomineeDetails','Invalid Nom UID')
      END
	

	  IF NOT EXISTS(SELECT 1 FROM #tbl_JsonFile xx
      WHERE MasterTag = 'NomineeDetails' AND ColumnName = 'NomineeResidualSecurities'
	  and ColumnValue = 'Y' AND EXISTS(sELECT 1 FROM #tbl_JsonFile X1
      WHERE MasterTag = 'NomineeDetails' and ColumnName in('NomFirstName')
      AND JSONLEVEL = X1.JSONLEVEL AND ColumnValue <> '')) AND @i_vcTemplateCode = 'Template2'
	  AND EXISTS(SELECT 1 FROM #tbl_JsonFile 
	  WHERE MasterTag ='NomineeDetails' AND ColumnName LIKE '%FirstName' AND ColumnValue <> '')
      BEGIN
        INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	    VALUES('NomineeDetails','Nominee Residual Flag Must be Y')
      END
	
	
	
      --- in Case of Minar Nominee Should Have Guardian

      IF EXISTS(select 1 FROM #tbl_JsonFile x
      WHERE MasterTag = 'NomineeDetails' and (ColumnName = 'NomineeDOB' OR ColumnName LIKE  '%DOB%')
      and DATEDIFF(YEAR, cast(ColumnValue as date), GETDATE()) < 18
      and  NOT EXISTS(sELECT 1 FROM #tbl_JsonFile X1
      WHERE MasterTag = 'GuardianDetails' and ColumnName in('NomGuardianFirstName' ,'NomGuardianName','SecondNomGuardianFirstName','ThirdNomGuardianFirstName')
      AND MASTERLEVEL = X.JSONLEVEL AND ColumnValue <> ''))
      BEGIN
	    INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	    VALUES('NomineeDetails','Nominee Guardian Detail is Missing For Minar Nominee')
      END
	
	  IF EXISTS(SELECT 1 FROM #tbl_JsonFile x
      WHERE MasterTag ='NomineeDetails' and ColumnName LIKE 'NomFirstName'
      and COLUMNVALUE <> '' AND EXISTS(SELECT 1 FROM #tbl_JsonFile X1
      WHERE MasterTag = 'NomineeDetails' 
	  and (ColumnName = 'NomLastName' OR ColumnName = 'NomAddress1' OR ColumnName = 'NomAddressCity'
	  OR ColumnName = 'NomAddressState' OR ColumnName = 'NomAddressCountry'  OR ColumnName = 'NomAddressPin'
	  OR ColumnName = 'NomineeRelation')
      AND JSONLEVEL = X.JSONLEVEL AND ColumnValue = ''))
	  BEGIN
	    INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	    VALUES('NomineeDetails','NomLastName/NomAddress1/NomAddressCity/NomAddressState/NomAddressCountry/NomAddressPin/NomineeRelation can not be blank')
	  END
	
      IF EXISTS(SELECT 1 FROM #tbl_JsonFile x
      WHERE MasterTag ='GuardianDetails' and ColumnName LIKE 'NomGuardianFirstName'
      and COLUMNVALUE <> '' AND EXISTS(SELECT 1 FROM #tbl_JsonFile X1
      WHERE MasterTag = 'GuardianDetails' 
	  and (ColumnName = 'NomGuardianLastName' OR ColumnName = 'NomGuardianAddress1' OR ColumnName = 'NomGuardianAddressCity'
	  OR ColumnName = 'NomGuardianAddressState' OR ColumnName = 'NomGuardianAddressCountry'  OR ColumnName = 'NomGuardianAddressPin')
      AND JSONLEVEL = X.JSONLEVEL AND ColumnValue = ''))
	  BEGIN
	    INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	    VALUES('GuardianDetails','NomGuardianLastName/NomGuardianAddress1/NomGuardianAddressCity/NomGuardianAddressState/NomGuardianAddressCountry/NomGuardianAddressPin can not be blank')
	  END
	
	  --- In Case of Nominee Having 100 % for Sharing 
    
      DECLARE @NomSharingPer MONEY = 0
      SELECT @NomSharingPer = sum(cast(columnValue as money)) FROM #tbl_JsonFile 
	  WHERE MasterTag ='NomineeDetails' and (columnName = 'NomPercentage' OR columnName LIKE '%SharePercentage%')
	
	
      IF ISNULL(@NomSharingPer,0) <> 100 AND EXISTS(SELECT 1 FROM #tbl_JsonFile 
	  WHERE MasterTag ='NomineeDetails' AND ColumnName LIKE '%FirstName' AND ColumnValue <> '')
      BEGIN
	    INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	    VALUES('NomineeDetails','Nominee Share must Equall to 100 %')
      END
	END
	
	
    IF EXISTS(SELECT 1 FROM #tbl_JsonFile xx
    WHERE MasterTag = 'PersonalDetails' AND (ColumnName = 'UID' OR ColumnName LIKE '%UID%')
	and LEN(ColumnValue) <> 12 AND ColumnValue <> '')
    BEGIN
      INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	  VALUES('PersonalDetails','Invalid UID')
    END
	

    --- DEFAULT CHECK DEMAT	

	IF ((@IsDematModified = 'true' and @i_vcRequestMode = 'U' AND @i_vcTemplateCode = 'Template1')
	OR (@IsDematModified = 'False' and @i_vcRequestMode <> 'U' AND @i_vcTemplateCode <> 'Template1') 
	OR @i_vcRequestMode = 'I')
	BEGIN
	  DECLARE @tbl_dpDefault TABLE(UniqueNo INT, Dpid VARCHAR(50), dpaccountNo VARCHAR(20), DefaultTag VARCHAR(1))
      DECLARE @tbl_dpDefaultMain TABLE(Dpid VARCHAR(50), dpaccountNo VARCHAR(20), DefaultTag VARCHAR(1))


      INSERT INTO @tbl_dpDefault(UniqueNo, Dpid)
      SELECT JSONlEVEL, COLUMNVALUE FROM #tbl_JsonFile WHERE MasterTag = 'DematDetails' AND COLUMNNAME ='DPID'

      UPDATE A SET A.dpaccountNo = B.COLUMNVALUE
      FROM @tbl_dpDefault A,
      (SELECT COLUMNVALUE, JSONlEVEL FROM #tbl_JsonFile WHERE MasterTag = 'DematDetails' AND COLUMNNAME ='DPAcNo') B
      WHERE A.UniqueNo = B.JSONlEVEL

      UPDATE A SET A.DefaultTag = B.COLUMNVALUE
      FROM @tbl_dpDefault A,
      (SELECT IIF(COLUMNVALUE IN('false','true'),IIF(COLUMNVALUE='false','N','Y') ,COLUMNVALUE)
	  AS COLUMNVALUE, JSONlEVEL FROM #tbl_JsonFile 
	  WHERE MasterTag = 'DematDetails' AND COLUMNNAME ='IsDefault') B
      WHERE A.UniqueNo = B.JSONlEVEL


      INSERT INTO @tbl_dpDefaultMain(DPID, dpaccountNo, DefaultTag)
      SELECT da_dpid, da_actno, da_defaultyn FROM Dematact(NOLOCK) WHERE da_clientcd = @i_vcClientCode AND da_status = 'A'

      UPDATE A SET a.DefaultTag = b.DefaultTag
      FROM @tbl_dpDefaultMain a,  @tbl_dpDefault b
      WHERE a.Dpid = b.Dpid
      AND a.dpaccountNo = b.dpaccountNo

      INSERT INTO @tbl_dpDefaultMain(DPID, dpaccountNo, DefaultTag)
      SELECT DPID, dpaccountNo, DefaultTag from @tbl_dpDefault x
      WHERE NOT EXISTS(SELECT 1 from @tbl_dpDefaultMain where Dpid = x.Dpid and dpaccountNo = x.dpaccountNo)


      DECLARE @idpCount INT = 0
      SELECT @idpCount = count(*) from(
      SELECT DISTINCT DPID, dpaccountNo from @tbl_dpDefault WHERE DefaultTag = 'Y') x1 

      IF @idpCount > 1
      BEGIN
	    INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	    VALUES('DematDetails','Multiple Default DP account Available')
      END
	
	
	
	  SET @WrongID = ''
      SELECT @WrongID = @WrongID+'|'+DPID+','+dpaccountNo FROM @tbl_dpDefaultMain 
	  WHERE  (case when SUBSTRING(DPID,1,2)='IN' then  LEN(DPID+dpaccountNo)
	  ELSE LEN(dpaccountNo) end) <> 16

	  IF @WrongID <> ''
	  BEGIN
	   INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	   VALUES('DematDetails','Wrong DP Account No and ID length not match' + ' ' + @WrongID )
	  END
	END
	
	IF ((@IsBankModified = 'true' and @i_vcRequestMode = 'U' AND @i_vcTemplateCode = 'Template1')
	OR (@IsBankModified = 'False' and @i_vcRequestMode <> 'U' AND @i_vcTemplateCode <> 'Template1') 
	OR @i_vcRequestMode = 'I')
	
	BEGIN
	  DECLARE @tbl_BankDefault TABLE(UniqueNo INT, ba_micr VARCHAR(10), ba_acttype VARCHAR(50), ba_actno VARCHAR(20), ba_ifsccode VARCHAR(15), DefaultTag VARCHAR(1))
	  DECLARE @tbl_BankDefaultMain 
	  TABLE(ba_micr VARCHAR(10), ba_acttype VARCHAR(50), ba_actno VARCHAR(20), ba_ifsccode VARCHAR(15), DefaultTag VARCHAR(1))
	
	  INSERT INTO @tbl_BankDefault(UniqueNo, ba_micr)
      SELECT JSONlEVEL, COLUMNVALUE FROM #tbl_JsonFile WHERE MasterTag = 'BankDetails' AND COLUMNNAME ='BankMICR'

      UPDATE A SET A.ba_acttype = B.COLUMNVALUE
      FROM @tbl_BankDefault A,
      (SELECT COLUMNVALUE, JSONlEVEL FROM #tbl_JsonFile WHERE MasterTag = 'BankDetails' AND COLUMNNAME ='AccountType') B
      WHERE A.UniqueNo = B.JSONlEVEL

      UPDATE A SET A.DefaultTag = B.COLUMNVALUE
      FROM @tbl_BankDefault A,
      (SELECT IIF(COLUMNVALUE IN('false','true'),IIF(COLUMNVALUE='false','N','Y') ,COLUMNVALUE) AS COLUMNVALUE, JSONlEVEL FROM #tbl_JsonFile 
	  WHERE MasterTag = 'BankDetails' AND COLUMNNAME ='IsDefault') B
       WHERE A.UniqueNo = B.JSONlEVEL
	 
	  UPDATE A SET A.ba_actno = B.COLUMNVALUE
      FROM @tbl_BankDefault A,
      (SELECT COLUMNVALUE, JSONlEVEL FROM #tbl_JsonFile WHERE MasterTag = 'BankDetails' AND COLUMNNAME ='BankAccNo') B
       WHERE A.UniqueNo = B.JSONlEVEL

      UPDATE A SET A.ba_ifsccode = B.COLUMNVALUE
      FROM @tbl_BankDefault A,
      (SELECT COLUMNVALUE, JSONlEVEL FROM #tbl_JsonFile WHERE MasterTag = 'BankDetails' AND COLUMNNAME ='BankIFSC') B
       WHERE A.UniqueNo = B.JSONlEVEL
	 
	
      INSERT INTO @tbl_BankDefaultMain(ba_micr, ba_acttype, ba_actno, ba_ifsccode , DefaultTag )
      SELECT ba_micr, ba_acttype, ba_actno, ba_ifsccode , ba_default  
	  FROM BankAct(NOLOCK) WHERE BA_clientcd = @i_vcClientCode

      UPDATE A SET a.DefaultTag = b.DefaultTag
      FROM @tbl_BankDefaultMain a,  @tbl_BankDefault b
      WHERE a.ba_micr = b.ba_micr
      AND a.ba_acttype = b.ba_acttype
	  and a.ba_actno = b.ba_actno
	  and a.ba_ifsccode = b.ba_ifsccode

      INSERT INTO @tbl_BankDefaultMain(ba_micr, ba_acttype, ba_actno, ba_ifsccode , DefaultTag )
      SELECT ba_micr, ba_acttype, ba_actno, ba_ifsccode , DefaultTag  from @tbl_BankDefault b
      WHERE NOT EXISTS(SELECT 1 from @tbl_BankDefaultMain a where a.ba_micr = b.ba_micr
      AND a.ba_acttype = b.ba_acttype
	  and a.ba_actno = b.ba_actno
	  and a.ba_ifsccode = b.ba_ifsccode)

      set @idpCount= 0
      SELECT @idpCount = count(*) from(
      SELECT DISTINCT ba_micr, ba_acttype, ba_actno, ba_ifsccode  from @tbl_BankDefaultMain WHERE DefaultTag = 'Y') x1
    
      IF @idpCount > 1
      BEGIN
	   INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	   VALUES('BankDetails','Multiple Default Bank account Available')
      END
	END   
    DROP TABLE #tbl_JsonFile
  END
  ELSE IF  @i_vcValidationType = 'CHECKER'  
  BEGIN
    Declare @Msg_dpid varchar(30) = ''

	DECLARE @ca_Nfiller1 INT, @strca_ifsccode VARCHAR(20)='', @SSbk_micr VARCHAR(50)=''
	
	DECLARE CURSOR_DetailAuto CURSOR FOR
	SELECT ca_Nfiller1 FROM Client_ModifyAPI(NOLOCK) X1  
	where ca_cmcd = @i_vcClientCode  AND ca_Nfiller3 = @i_vcRefNo
    AND ca_filler1 = 'BankDetails'
    AND ca_field  = 'ba_ifsccode'
	AND ISNULL(ca_NewValue,'') <> ''
	AND NOT EXISTS(SELECT 1 FROM Client_ModifyAPI(NOLOCK) 
	where ca_cmcd = @i_vcClientCode  AND ca_Nfiller3 = @i_vcRefNo
    AND ca_filler1 = 'BankDetails'
    AND ca_field  = 'ba_micr' and ISNULL(ca_NewValue,'') <> ''
	and ca_Nfiller1 = x1.ca_Nfiller1)
     
	OPEN CURSOR_DetailAuto
    FETCH NEXT FROM CURSOR_DetailAuto
	INTO @ca_Nfiller1

	WHILE @@FETCH_STATUS = 0
	BEGIN
	  SELECT @strca_ifsccode = ISNULL(ca_NewValue,'') 
	  FROM Client_ModifyAPI(NOLOCK) 
	  where ca_cmcd = @i_vcClientCode  AND ca_Nfiller3 = @i_vcRefNo
      AND ca_filler1 = 'BankDetails'
      AND ca_field = 'ba_ifsccode'
	  AND ca_Nfiller1 = @ca_Nfiller1
	  
	  SELECT TOP 1 @SSbk_micr = bk_micr FROM Bank_master(NOLOCK) WHERE bk_IFCCode = @strca_ifsccode
	  
	  IF EXISTS(SELECT 1 FROM Client_ModifyAPI(NOLOCK) 
	   where ca_cmcd = @i_vcClientCode  AND ca_Nfiller3 = @i_vcRefNo
       AND ca_filler1 = 'BankDetails'
       AND ca_field  = 'ba_micr' and ISNULL(ca_NewValue,'') = '' and ca_Nfiller1 = @ca_Nfiller1)
	  BEGIN  
	    UPDATE A SET A.ca_NewValue = @SSbk_micr
	    FROM Client_ModifyAPI A
		where ca_cmcd = @i_vcClientCode  AND ca_Nfiller3 = @i_vcRefNo
        AND ca_filler1 = 'BankDetails'
        AND ca_field  = 'ba_micr' and ISNULL(ca_NewValue,'') = '' and ca_Nfiller1 = @ca_Nfiller1
	  END
      ELSE
	  BEGIN
	    INSERT INTO Client_ModifyAPI
		SELECT ca_cmcd,ca_field = 'ba_micr', ca_desc = 'MICR', ca_oldValue = '', ca_newValue = @SSbk_micr,
		ca_date,ca_time,ca_computername,ca_Tplus,ca_Cross,ca_Estro,ca_Dematacno,ca_filler1,ca_filler2,ca_filler3,ca_Nfiller1,ca_Nfiller2,ca_Nfiller3
		FROM Client_ModifyAPI 
		WHERE ca_cmcd = @i_vcClientCode  AND ca_Nfiller3 = @i_vcRefNo
        AND ca_filler1 = 'BankDetails'
        AND ca_field  = 'ba_ifsccode' and ISNULL(ca_NewValue,'') <> '' and ca_Nfiller1 = @ca_Nfiller1
	  END
      	  
	  FETCH NEXT FROM CURSOR_DetailAuto INTO @ca_Nfiller1
     END
     CLOSE CURSOR_DetailAuto
     DEALLOCATE CURSOR_DetailAuto

    SELECT @WrongID = @WrongID+'|'+ca_newValue , @Msg_dpid = ca_newValue from client_ModifyAPI(NOLOCK) X  WHERE ca_cmcd  = @i_vcClientCode 
	AND ca_Nfiller3 = @i_vcRefNo
	and ca_field like 'da_dpid' 
	AND ca_newValue NOT IN(SELECT dp_dpid FROM DPS(NOLOCK))
  
    IF @WrongID <> ''
	BEGIN
	  INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	 -- VALUES('DematDetails','DP ID NOT OPEN WITH US '+@WrongID)
	  VALUES('DematDetails','DP ID ' + @Msg_dpid + ' NOT found in Demat Master')
	END
	
	SET @WrongID = ''
	Declare @Msg_micr varchar(20)='', @Msg_ifsc varchar(20)=''
    DECLARE @tbl_Bankmicr TABLE(UniqueNo INT, ba_micr VARCHAR(10), ba_ifsccode VARCHAR(15))
    INSERT INTO @tbl_Bankmicr(UniqueNo, ba_micr)
	SELECT ca_Nfiller1, ca_newValue from client_ModifyAPI(NOLOCK) X  WHERE ca_cmcd  = @i_vcClientCode AND ca_Nfiller3 = @i_vcRefNo
	and ca_field ='ba_micr'
	
	update A SET A.ba_ifsccode = B.ca_newValue
	from @tbl_Bankmicr A, (SELECT ca_Nfiller1, ca_newValue from client_ModifyAPI(NOLOCK) X  WHERE ca_cmcd  = @i_vcClientCode AND ca_Nfiller3 = @i_vcRefNo
	and ca_field ='ba_ifsccode') B
	WHERE A.UniqueNo = B.ca_Nfiller1
	
	SELECT @WrongID = @WrongID+'|'+ba_micr+','+ba_ifsccode, @Msg_micr = ba_micr , @Msg_ifsc = ba_ifsccode FROM @tbl_Bankmicr  X
	WHERE NOT EXISTS(SELECT 1 FROM Bank_master(NOLOCK) WHERE bk_micr = X.ba_micr AND bk_IFCCode = X.ba_ifsccode)
    
	IF @WrongID <> ''
	BEGIN
	  INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	  --VALUES('BankDetails','Bank Micr and Ifsc NOT OPEN WITH US '+@WrongID)
	  VALUES('BankDetails','MICR ' + @Msg_micr + ' and IFSC code ' + @Msg_ifsc + ' Combination not found in Bank Master')
	END
	
	DECLARE @tbl_BankAccIFSC TABLE(UniqueNo INT, ifsccode VARCHAR(15), bankAccountNo VARCHAR(25))
	
	INSERT INTO @tbl_BankAccIFSC(UniqueNo, bankAccountNo)
	SELECT ca_Nfiller1, ca_newValue from client_ModifyAPI(NOLOCK) X  WHERE ca_cmcd  = @i_vcClientCode AND ca_Nfiller3 = @i_vcRefNo
	AND ca_field ='ba_actno' and ISNULL(ca_oldValue,'') = ''
	
	update A SET A.ifsccode = B.ca_newValue
	from @tbl_BankAccIFSC A, (SELECT ca_Nfiller1, ca_newValue from client_ModifyAPI(NOLOCK) X  
	WHERE ca_cmcd  = @i_vcClientCode AND ca_Nfiller3 = @i_vcRefNo
	and ca_field ='ba_ifsccode' and ISNULL(ca_oldValue,'') = '' ) B
	WHERE A.UniqueNo = B.ca_Nfiller1
	
	DECLARE @strBankAccountNo VARCHAR(25)='', @StrBankIfscCode VARCHAR(20)=''
	SELECT TOP 1 @strBankAccountNo = ba_actno, @StrBankIfscCode = ba_ifsccode 
	FROM Bankact(nolock) X where ba_clientcd =  @i_vcClientCode 
	AND EXISTS(SELECT 1 FROM @tbl_BankAccIFSC WHERE ifsccode = x.ba_ifsccode and bankAccountNo = x.ba_actno)
	
	IF ISNULL(@strBankAccountNo,'') <> ''
	BEGIN
	   INSERT INTO @TBL_OutputJSON(ErrorTag, ErrorMessage)
	   VALUES('BankDetails','Bank Detail Already Exists in Client Bank '+@strBankAccountNo+' - '+@StrBankIfscCode)
	END
  END
  
  IF @i_vcPassingValueType = 'JSON'
  BEGIN
    IF EXISTS(sELECT 1 FROM @TBL_OutputJSON)
    BEGIN
      SET @o_vcJsonOutput = (SELECT * FROM @TBL_OutputJSON ORDER BY ErrorTag FOR JSON PATH, ROOT ('Response'))
	  SET @o_vcErrorMessage = 'Error Message'
	  SET @o_vcErrorFlag = 'E'
	  RETURN 1   
    END
    ELSE
    BEGIN
      SET @o_vcJsonOutput = (SELECT ErrorTag = 'S', ErrorMessage = 'Process Executed' FOR JSON PATH, ROOT ('Response'))
	  SET @o_vcErrorMessage = 'Process Executed'
	  SET @o_vcErrorFlag = 'S'
	  RETURN 1   
    END
  END	
  ELSE
  BEGIN
    IF EXISTS(sELECT 1 FROM @TBL_OutputJSON)
    BEGIN
      SET @o_vcJsonOutput = (SELECT * FROM @TBL_OutputJSON ORDER BY ErrorTag FOR XML PATH('Response'))
	  SET @o_vcErrorMessage = 'Error Message'
	  SET @o_vcErrorFlag = 'E'
	  RETURN 1   
    END
    ELSE
    BEGIN
      SET @o_vcJsonOutput = (SELECT ErrorTag = 'S', ErrorMessage = 'Process Executed' FOR XML PATH('Response'))
	  SET @o_vcErrorMessage = 'Process Executed'
	  SET @o_vcErrorFlag = 'S'
	  RETURN 1   
    END
  END
  END TRY
  BEGIN CATCH
    IF @i_vcPassingValueType = 'JSON'
    BEGIN
      SET @o_vcJsonOutput = (SELECT ErrorTag = 'E', ErrorMessage = ERROR_MESSAGE() FOR JSON PATH, ROOT ('Response'))
	  SET @o_vcErrorMessage = error_message()
	  SET @o_vcErrorFlag = 'E'
	  RETURN 1   
	END
    ELSE	
	BEGIN
      SET @o_vcJsonOutput = (SELECT ErrorTag = 'E', ErrorMessage = ERROR_MESSAGE() FOR XML PATH('Response'))
	  SET @o_vcErrorMessage = error_message()
	  SET @o_vcErrorFlag = 'E'
	  RETURN 1   
	END
  END CATCH  
END   
GO

CREATE PROCEDURE [sp_ReKyc_CorssAPI] @i_vcJsonString NVARCHAR(MAX), @i_vcClientCode VARCHAR(20), 
@i_vcRefNo numeric(10), @i_vcComputerName VARCHAR(50), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT, 
@o_vcJsonOutput  VARCHAR(MAX) OUTPUT, @vcTemplateCode VARCHAR(30)='Template2' WITH ENCRYPTION AS
BEGIN
  IF @i_vcJsonString = ''
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Payload is blank'
	SET @o_vcJsonOutput = '{}'
	RETURN 1
  END
  IF EXISTS(SELECT 1 FROM Client_ModifyAPI WHERE ca_cmcd = @i_vcClientCode AND ca_Nfiller3 = @i_vcRefNo)
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Duplicate Ref No '
	SET @o_vcJsonOutput = '{}'
	RETURN 1
  END
  
  --- VALIDATION PROCEDURE CALL
  
  BEGIN TRY
  EXEC [dbo].[SP_ReKyc_CheckValidation] @i_vcJsonString, @i_vcClientCode, 0, 'MAKER', @o_vcErrorFlag OUTPUT, 
  @o_vcErrorMessage OUTPUT, @o_vcJsonOutput OUTPUT,'U', 'JSON', @vcTemplateCode
  
 
  IF @o_vcErrorFlag = 'E'
  BEGIN
    RETURN 1
  END
  END TRY
  BEGIN CATCH
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = ERROR_MESSAGE()
	SET @o_vcJsonOutput = '{}'
  END CATCH
  SET @o_vcErrorFlag = ''
  SET @o_vcErrorMessage = ''
  -- 
  --- MAKER POST
  BEGIN TRY
  EXEC [dbo].[SP_ReKyc_MakerPost] '', @i_vcJsonString, 
  @i_vcClientCode, @i_vcRefNo, @i_vcComputerName, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT, @o_vcJsonOutput OUTPUT, @vcTemplateCode
  
  IF @o_vcErrorFlag = 'E'
  BEGIN
    DELETE FROM Client_ModifyAPI WHERE ca_cmcd = @i_vcClientCode AND ca_Nfiller3 = @i_vcRefNo
    RETURN 1
  END   
  
  END TRY
  BEGIN CATCH
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = ERROR_MESSAGE()
	SET @o_vcJsonOutput = '{}'
  END CATCH
  
  
  BEGIN TRY
  EXEC [dbo].[SP_ReKyc_CheckerApprove] @i_vcClientCode, @i_vcRefNo, 'A','AUTO Approve', 
  @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT, @o_vcJsonOutput OUTPUT, @vcTemplateCode 
  
  IF  @o_vcErrorFlag = 'E'
  BEGIN
    DELETE FROM Client_ModifyAPI WHERE ca_cmcd = @i_vcClientCode AND ca_Nfiller3 = @i_vcRefNo
  END	
  RETURN 1
  END TRY
  BEGIN CATCH
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = ERROR_MESSAGE()
	SET @o_vcJsonOutput = '{}'
  END CATCH
END
GO

CREATE Procedure [SP_ReKyc_GetData] @i_vcClientCode VARCHAR(20),  @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(500) OUTPUT, 
@o_vcJsonOutput VARCHAR(MAX) OUTPUT, @i_vcTemplateCode VARCHAR(20) = 'Template1', @i_vcProcessTag VARCHAR(1)='G', @strFormNo INT = 0 WITH ENCRYPTION  AS
BEGIN
  set @o_vcErrorFlag = 'S'
  SET @o_vcErrorMessage = 'Process Executed'
  SET @o_vcJsonOutput = ''
  DECLARE @json VARCHAR(MAX)='', @string VARCHAR(MAX)='', @strCommexConn VARCHAR(100)='', @dpconn VARCHAR(100)='',
  @xmlStr XML, @o_vcsegmentString VARCHAR(MAX)='', @strtradeplustempdb VARCHAR(50)='', @strString NVARCHAR(MAX)=''

  DECLARE @vcClientCode VARCHAR(10) = @i_vcClientCode
  
  SELECT @strCommexConn = LTRIM(RTRIM(OP_DataBase)) 
  FROM Other_Products(NOLOCK) WHERE OP_Product = 'Commex' AND OP_Status ='A'
  
  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB'
  
  SELECT @dpconn = LTRIM(RTRIM(OP_DataBase))
  FROM Other_Products(NOLOCK) WHERE OP_Product = 'Estro' AND OP_Status ='A'
  
  
  
  
  IF @dpconn = ''
  BEGIN
    SELECT @dpconn = LTRIM(RTRIM(OP_DataBase))
    FROM Other_Products(NOLOCK) WHERE OP_Product = 'cross' AND OP_Status ='A'
  END
  
  IF @i_vcTemplateCode = 'Template1'
  BEGIN
  
    DECLARE @i_vcRefNo INT = 0
    SELECT @i_vcRefNo = rm_refno FROM Client_ReKycMain(NOLOCK) 
	WHERE rm_cmcd = @vcClientCode AND rm_RequestType = 'REKYC'
    AND ISNULL(rm_rekyc,'N') = 'N' AND rm_srno IN( SELECT MAX(ISNULL(rm_srno, 0)) 
	FROM Client_ReKycMain(NOLOCK) WHERE rm_cmcd = @vcClientCode AND rm_RequestType = 'REKYC'
	AND ISNULL(rm_rekyc,'N') = 'N')
	
	IF @strFormNo > 0
	BEGIN
	  SET @i_vcRefNo = @strFormNo
	END
  
    DECLARE @tbl_Segment TABLE (Segment VARCHAR(50), ce_companycode VARCHAR(50), ExchVal varchar(50), Exchange VARCHAR(100), 
	MainExchCode VARCHAR(100))
   -- DECLARE @tbl_Segment1 TABLE (Segment VARCHAR(50), SegmentExch VARCHAR(100), ce_companycode VARCHAR(50), ce_ExchangeDescp VARCHAR(100))

------Add New segment checkbox list concept
	DECLARE @tbl_Segment2 TABLE (ExchSegment VARCHAR(100), SegmentValue VARCHAR(50), IsSelect bit)

    ---- For Segment details
	
    DECLARE @outputSp VARCHAR(MAX)=''
    EXEC SP_GetSegmentUserwise @vcClientCode, '',@outputSp Output

    SET @strString = 'EXEC '+@strtradeplustempdb+'.DBO.sp_ProcessRekyc ''' + @outputSp + ''', ''Getsegment'', @o_vcsegmentString OUTPUT ';
	
	EXEC sp_executesql @strString, N'@o_vcsegmentString VARCHAR(MAX) OUTPUT',  @o_vcsegmentString OUTPUT;
	
    SET @xmlStr = CAST(@o_vcsegmentString AS xml)
    INSERT INTO @tbl_Segment (Segment, ce_companycode, ExchVal, Exchange, MainExchCode)
	SELECT c.value('(Segment)[1]', 'VARCHAR(100)') AS Segment,
    c.value('(ce_companycode)[1]', 'VARCHAR(100)') AS ce_companycode,
	c.value('(ExchVal)[1]', 'VARCHAR(100)') AS ExchVal,
    c.value('(Exchange)[1]', 'VARCHAR(100)') AS Exchange,
    c.value('(MainExchCode)[1]', 'VARCHAR(100)') AS MainExchCode FROM 
    @xmlStr.nodes('//row') AS t(c);
	
   
    /*INSERT INTO @tbl_Segment1
    SELECT Segment, ExchVal, STUFF((
			SELECT ', ' + ce_companycode
			FROM @tbl_Segment
			WHERE ExchVal = t.ExchVal
			AND Segment = T.Segment
			FOR XML PATH('')
			), 1, 2, '') AS Exhange,
			STUFF((
			SELECT ', ' + MainExchCode+'/'+Exchange
			FROM @tbl_Segment
			WHERE ExchVal = t.ExchVal
			AND Segment = T.Segment
			FOR XML PATH('')
			), 1, 2, '') AS Seg_Exhange
    FROM @tbl_Segment t
    GROUP BY Segment, ExchVal
	*/

	
	------Add New segment checkbox list concept
	INSERT INTO @tbl_Segment2
    SELECT Exchange+'/'+ExchVal As ExchSegment, MainExchCode AS SegmentValue, 0
    FROM @tbl_Segment t
    GROUP BY Exchange, ExchVal, MainExchCode
 
    SET @json = (SELECT ReKycDetails = (SELECT KycNumber = (select isNull(Ck_Nfiller1,0) as ckycnumber From Client_CKYC(NOLOCK) CXXX    
	WHERE ck_panno = (Select cm_panno From Client_master(NOLOCK) 
	WHERE cm_cd = @i_vcClientCode) 
	AND CK_SRNO IN(SELECT MAX(CK_SRNO) FROM Client_CKYC WHERE CK_Panno = CXXX.CK_Panno))
		, KycMode = '',[NomineeOpt]='',[IsNomineeModified]='false',
		[IsBankModified]='false', [IsDematModified]='false', [ReturnUrlEsignSetu]='',[ReqIdSetu]='',
		[ApiCallBank] = (Select SUBSTRING(sp_sysvalue,CHARINDEX('/', sp_sysvalue) + 1,len(sp_sysvalue) - 1) as Addrs 
		FROM WebParameter(NOLOCK) where sp_parmcd='SetuSetting'),
		[ApiCallAddress] = (Select SUBSTRING(sp_sysvalue,1,CHARINDEX('/', sp_sysvalue) - 1) as Addrs 
		From WebParameter(NOLOCK) where sp_parmcd='SetuSetting'),
		[FnoActiveFlag]= (Case when ISNULL((SELECT count(1) FROM Client_details(NOLOCK) WHERE ce_clientcd = @i_vcClientCode
        AND right(ce_companycode,1) in('F','X','K') AND ce_regDt <> ''),0) > 0 then 'Y' ELSE 'N' END),
		[CheckerMessage] = (Select rm_desc From Client_ReKycMain(NOLOCK) where  rm_cmcd = @i_vcClientCode and rm_rekyc = 'R' 
		And rm_refno >= (Select Isnull(Max(rm_refno),0) From Client_ReKycMain(NOLOCK) where rm_cmcd = @i_vcClientCode)),
		[OldEmailMob] = (Select sp_sysvalue From WebParameter(NOLOCK) where sp_parmcd='OldEmailMobOTP'),
		[IPV] = (Select sp_sysvalue From WebParameter(NOLOCK) where sp_parmcd='RKYCVIDEO') FOR JSON PATH),
	    ---- FOR ReKyc HEADER
		---- For Personal details

		PersonalDetails = (
				SELECT ClientCode = ltrim(rtrim(Cm_cd)), 
					ClientsNamePrefix = ltrim(rtrim(cm_prefix)), 
					FirstName = ltrim(rtrim((
								CASE WHEN CHARINDEX(' ', LTRIM(RTRIM(cm_name))) > 0 THEN 
											SUBSTRING(LTRIM(RTRIM(cm_name)), 1, CHARINDEX(' ', 
													LTRIM(RTRIM(cm_name))) - 1) ELSE LTRIM(RTRIM(cm_name)) END
								))), MiddleName = ltrim(rtrim((
								CASE WHEN CHARINDEX(' ', LTRIM(RTRIM(cm_name))) = 0 THEN '' ELSE 
										RTRIM(LTRIM(REPLACE(REPLACE(LTRIM(RTRIM(cm_name)), 
														SUBSTRING(LTRIM(RTRIM(cm_name)), 1, CHARINDEX(' ', 
																LTRIM(RTRIM(cm_name))) - 1), ''), REVERSE(LEFT(
															REVERSE(LTRIM(RTRIM(cm_name))), CHARINDEX(' ', 
																REVERSE(LTRIM(RTRIM(cm_name)))) - 1)), ''))) END
								))), LastName = ltrim(rtrim((
								CASE WHEN CHARINDEX(' ', LTRIM(RTRIM(cm_name))) > 0 THEN 
											REVERSE(LEFT(REVERSE(LTRIM(RTRIM(cm_name))), CHARINDEX
													(' ', REVERSE(LTRIM(RTRIM(cm_name)))) - 1)) ELSE '' END
								))), CorrAddress1 = ltrim(rtrim(cm_add1)), 
					CorrAddress2 = ltrim(rtrim(cm_add2)), 
					CorrAddress3 = ltrim(rtrim(cm_add3)), CorrCity = 
					ltrim(rtrim(cm_add4)), CorrCountry = ltrim(rtrim
						(cm_bankactno)), CorrState = ltrim(rtrim(
							cm_state)), CorrPincode = ltrim(rtrim(cm.
							cm_pincode)), AddressType = '', Email = ltrim(
						rtrim(cm_email)), EmailRelation = CASE WHEN 
							ISNULL((
									SELECT cc_Relation
									FROM Common_Contacts(NOLOCK)
									WHERE cc_Client = CM.CM_CD AND cc_type = 'E'
									), '') = 'Self' THEN '0' WHEN ISNULL((
									SELECT cc_Relation
									FROM Common_Contacts(NOLOCK)
									WHERE cc_Client = CM.CM_CD AND cc_type = 'E'
									), '') = 'Spouse' THEN '1' WHEN ISNULL((
									SELECT cc_Relation
									FROM Common_Contacts(NOLOCK)
									WHERE cc_Client = CM.CM_CD AND cc_type = 'E'
									), '') = 'Dependent Children' THEN '2' WHEN 
							ISNULL((
									SELECT cc_Relation
									FROM Common_Contacts(NOLOCK)
									WHERE cc_Client = CM.CM_CD AND cc_type = 'E'
									), '') = 'Dependent Parent' THEN '3' ELSE ISNULL
							((
									SELECT cc_Relation
									FROM Common_Contacts(NOLOCK)
									WHERE cc_Client = CM.CM_CD AND cc_type = 'E'
									), '') END, Mobile = CM_MOBILE, MobileRelation = 
					CASE WHEN ISNULL((
									SELECT cc_Relation
									FROM Common_Contacts(NOLOCK)
									WHERE cc_Client = CM.CM_CD AND cc_type = 'M'
									), '') = 'Self' THEN '0' WHEN ISNULL((
									SELECT cc_Relation
									FROM Common_Contacts(NOLOCK)
									WHERE cc_Client = CM.CM_CD AND cc_type = 'M'
									), '') = 'Spouse' THEN '1' WHEN ISNULL((
									SELECT cc_Relation
									FROM Common_Contacts(NOLOCK)
									WHERE cc_Client = CM.CM_CD AND cc_type = 'M'
									), '') = 'Dependent Children' THEN '2' WHEN 
							ISNULL((
									SELECT cc_Relation
									FROM Common_Contacts(NOLOCK)
									WHERE cc_Client = CM.CM_CD AND cc_type = 'M'
									), '') = 'Dependent Parent' THEN '3' ELSE ISNULL
							((
									SELECT cc_Relation
									FROM Common_Contacts(NOLOCK)
									WHERE cc_Client = CM.CM_CD AND cc_type = 'M'
									), '') END, FatherName = ltrim(rtrim(INFO.
							cm_faherhusguar)), [UID] = ltrim(rtrim(INFO.
							cm_uid)), [PAN] = ltrim(rtrim(cm_panno)), 
					Gender = CASE WHEN cm_sex = 'M' THEN 'Male' WHEN cm_sex 
							= 'F' THEN 'Female' WHEN cm_sex = 'O' THEN 'Other' 
						END, [DateOfBirth] = cm_dob, [MaritalStatus] = iif
					(info.cm_maritalstatus = 'M', 'Married', 'Single'
					), [Income] = CASE WHEN info.cm_grossincome = '1' 
							THEN 'Below Rs. 1  Lac' WHEN info.
							cm_grossincome = '2' THEN 
								'Btw Rs. 1 to Rs. 5 Lacs' WHEN info.
							cm_grossincome = '3' THEN 
								'Btw Rs. 5 to Rs. 10 Lacs' WHEN info.
							cm_grossincome = '4' THEN 
								'Btw Rs. 10 to Rs. 25 Lacs' WHEN info.
							cm_grossincome = '5' THEN 
								'Btw Rs. 25 Lacs to Rs. 1 Crore' WHEN info.
							cm_grossincome = '6' THEN 
								'More than Rs. 1 Crore' ELSE info.
							cm_grossincome END, [IncomeValue] = info.
					cm_grossincome, [IncomeDate] =LTRIM(RTRIM(INFO.cm_grossincomedt)), [PermanentAddressFlag] = 'N'
				FROM client_master(NOLOCK) cm
				LEFT OUTER JOIN client_info(NOLOCK) INFO
					ON (cm_cd = cm2_Cd)
				WHERE cm_cd = @vcClientCode
				FOR JSON PATH
				),
				---- For Nominee details
				[NomineeDetails] = (
				SELECT NomSerial = cn_Srno, NomFirstName = (
						CASE WHEN CHARINDEX(' ', cn_name) > 0 THEN 
									SUBSTRING(cn_name, 1, CHARINDEX(' ', cn_name
										) - 1) ELSE cn_name END
						), NomMiddleName = (
						CASE WHEN CHARINDEX(' ', cn_name) = 0 THEN '' ELSE 
								RTRIM(LTRIM(REPLACE(REPLACE(cn_name, 
												SUBSTRING(cn_name, 1, CHARINDEX(' ', 
														cn_name) - 1), ''), REVERSE(LEFT(REVERSE
													(cn_name), CHARINDEX(' ', REVERSE(
															cn_name)) - 1)), ''))) END
						), NomLastName = (
						CASE WHEN CHARINDEX(' ', cn_name) > 0 THEN REVERSE(
										LEFT(REVERSE(cn_name), CHARINDEX(' ', 
												REVERSE(cn_name)) - 1)) ELSE '' END
						), NomAddressCountry = ltrim(rtrim(cn_Country))
					, NomAddressState = ltrim(rtrim(cn_State)), 
					NomAddressCity = ltrim(rtrim(cn_City)), 
					NomAddress1 = ltrim(rtrim(cn_Add1)), NomAddress2 
					= ltrim(rtrim(cn_Add2)), NomAddress3 = ltrim(
						rtrim(cn_Add3)), NomineePAN = ltrim(rtrim(
							cn_PAN)), NomPincode = ltrim(rtrim(cn_Pin)), 
					NomineeUID = cn_UID, 
					NomRelation = (CASE WHEN ISNULL(cn_Relation, '') ='Spouse' THEN '01'  
		            WHEN ISNULL(cn_Relation, '') ='Son' THEN '02'  
		            WHEN ISNULL(cn_Relation, '') ='Daughter' THEN '03'  
		            WHEN ISNULL(cn_Relation, '') ='Father' THEN '04'  
		            WHEN ISNULL(cn_Relation, '') ='Mother' THEN '05'  
		            WHEN ISNULL(cn_Relation, '') ='Brother' THEN '06'  
		            WHEN ISNULL(cn_Relation, '') ='Sister' THEN '07'  
		            WHEN ISNULL(cn_Relation, '') ='Grandson' THEN '08'  
		            WHEN ISNULL(cn_Relation, '') ='Granddaughter' THEN '09'  
		            WHEN ISNULL(cn_Relation, '') ='Grandfather' THEN '10'  
		            WHEN ISNULL(cn_Relation, '') ='Grandmother' THEN '11'  
		            WHEN ISNULL(cn_Relation, '') ='Not Provided' THEN '12'  
		            WHEN ISNULL(cn_Relation, '') ='Others' THEN '13' ELSE  ISNULL(cn_Relation, '') END), 
					NomPercentage = cn_NomPershare, NomineeDOB = cn_DOB,
					NomMobile = cn_Tel, NomineeId = 1000 + cn_Srno, 
					NomineeResidualSecurities =(CASE WHEN ISNULL(cn_ResidualFlag, 'N') = 'Y' THEN 'true' else 'false' end),
					IsNomineeDeleted = 'false',  [NomineeAttachment] = '',
					---- For Gaurdian details
					[GuardianDetails] =  (
						SELECT NomGuardianSerial = cn_Srno, NomGuardianFirstName = (
								CASE WHEN CHARINDEX(' ', cn_name) > 0 THEN 
											SUBSTRING(cn_name, 1, CHARINDEX(' ', 
													cn_name) - 1) ELSE cn_name END
								), NomGuardianMiddleName = (
								CASE WHEN CHARINDEX(' ', cn_name) = 0 THEN '' ELSE 
										RTRIM(LTRIM(REPLACE(REPLACE(cn_name, 
														SUBSTRING(cn_name, 1, CHARINDEX(' ', 
																cn_name) - 1), ''), REVERSE(LEFT(
															REVERSE(cn_name), CHARINDEX(' ', 
																REVERSE(cn_name)) - 1)), ''))) END
								), NomGuardianLastName = (
								CASE WHEN CHARINDEX(' ', cn_name) > 0 THEN 
											REVERSE(LEFT(REVERSE(cn_name), CHARINDEX
													(' ', REVERSE(cn_name)) - 1)) ELSE '' END
								), NomGuardianCountry = ltrim(rtrim
								(cn_Country)), NomGuardianState = ltrim(rtrim
								(cn_State)), NomGuardianCity = ltrim(rtrim
								(cn_City)), NomGuardianAddress1 = ltrim(rtrim
								(cn_Add1)), NomGuardianAddress2 = ltrim(rtrim
								(cn_Add2)), NomGuardianAddress3 = ltrim(rtrim
								(cn_Add3)), NomGuardianPAN = ltrim(rtrim
								(cn_PAN)), NomGuardianPincode = ltrim(rtrim
								(cn_Pin)), 
							NomGuardianUID = cn_UID, 
							NomGuardianRelation = (CASE WHEN ISNULL(cn_Relation, '') ='Spouse' THEN '01'  
		                    WHEN ISNULL(cn_Relation, '') ='Son' THEN '02'  
		                    WHEN ISNULL(cn_Relation, '') ='Daughter' THEN '03'  
		                    WHEN ISNULL(cn_Relation, '') ='Father' THEN '04'  
		                    WHEN ISNULL(cn_Relation, '') ='Mother' THEN '05'  
		                    WHEN ISNULL(cn_Relation, '') ='Brother' THEN '06'  
		                    WHEN ISNULL(cn_Relation, '') ='Sister' THEN '07'  
		                    WHEN ISNULL(cn_Relation, '') ='Grandson' THEN '08'  
		                    WHEN ISNULL(cn_Relation, '') ='Granddaughter' THEN '09'  
		                    WHEN ISNULL(cn_Relation, '') ='Grandfather' THEN '10'  
		                    WHEN ISNULL(cn_Relation, '') ='Grandmother' THEN '11'  
		                    WHEN ISNULL(cn_Relation, '') ='Not Provided' THEN '12'  
		                    WHEN ISNULL(cn_Relation, '') ='Others' THEN '13' ELSE  ISNULL(cn_Relation, '') END), 
							NomGuardianDOB = cn_DOB, NomGuardianMobile = ltrim(rtrim(cn_Tel)), NomGuardianEmail = ltrim(rtrim(cn_Email)),
							NomGuardianId = 100 + cn_Srno , NomineeId = 1000 + (cn_Srno - 1)
						FROM Client_NomineeDetails(NOLOCK)
						WHERE cn_Cmcd = @vcClientCode AND cn_Srno = xmainn.cn_Srno + 1
						FOR JSON PATH  -- , WITHOUT_ARRAY_WRAPPER
						)  
				FROM Client_NomineeDetails(NOLOCK) xmainn
				WHERE cn_Cmcd = @vcClientCode AND cn_Srno IN (1, 3, 5)
				FOR JSON PATH
				),
				
				---- For Bank details
				[BankDetails] = (
				SELECT Top 3 [BankAccNo] = ltrim(rtrim(ba_actno)), 
					[BankName] = ltrim(rtrim(mas.bk_name)), 
					[BankIFSC] = ltrim(rtrim(ba_ifsccode)), BankMICR 
					= ltrim(rtrim(ba_micr)), IsDefault = iif(
						ba_default = 'Y', 'true', 'false'), [AccountType] 
					= ltrim(rtrim(ba_acttype)), [BankID] = (ROW_NUMBER() OVER(ORDER BY ba_actno ASC) + 1000),  
					[IsBankDeleted] = '', BankAttachment = '', NameAsPerBank = '' , IsDefaultNew = 'false'
				FROM Bankact x, Bank_master mas
				WHERE ba_clientcd = @vcClientCode AND x.ba_micr = mas.bk_micr AND x.ba_ifsccode = mas.bk_IFCCode
					--AND ltrim(rtrim(ba_ifsccode)) <> '' AND ltrim(rtrim(ba_micr)) <> ''
				Order By ba_default desc
				FOR JSON PATH
				),
				
				---- For Demat details
				[DematDetails] = (
				SELECT TOP 3 [DematId] = (ROW_NUMBER() OVER(ORDER BY da_clientcd ASC) + 1000), [DPAcNo] = ltrim(rtrim(
							da_actno)), [DPID] = ltrim(rtrim(da_dpid)), 
					DematAccountType = IIF(SUBSTRING(da_dpid, 1, 2) = 
						'IN', 'NSDL', 'CDSL'), [IsDefault] = iif(
						da_defaultyn = 'Y', 'true', 'false'), 
					[IsDematDeleted] = 'false', DematAttachment = '' , IsDefaultNew = 'false'
				FROM Dematact(NOLOCK)
				WHERE da_clientcd = @vcClientCode AND da_status = 'A' Order By da_defaultyn desc
				FOR JSON PATH
				),
				---- For Dormant details
				[DormantDetails] = (select DormantIsActive ='false', 
				DormantStatus = (CASE WHEN cm_freezeyn in('','N') THEN 'Active'  When cm_freezeyn='Y' Then 'Freeze for Trades' ELSE '' END)  
                FROM client_master(NOLOCK) WHERE CM_CD = @vcClientCode  FOR JSON PATH),
				[Attachments] = (SELECT AddressAttachment = '', 
				AddressProofType = '',
				SegmentProofType ='',SegmentAttachment = '',
				IncomeAttachment = ISNULL((SELECT CASE WHEN CAST(ma_proof  AS VARCHAR(MAX)) <> ''
                then CAST(ma_proof  AS VARCHAR(MAX)) else '' end FROM  Client_ModifyAttach(NOLOCK) WHERE ma_cmcd =  @vcClientCode AND ma_refno = @i_vcRefNo 
				AND ma_field ='IncomeAttachment'),''), 
				IncomeProofType = ISNULL((SELECT ma_filename FROM  Client_ModifyAttach(NOLOCK) WHERE ma_cmcd =  @vcClientCode 
				AND ma_refno = @i_vcRefNo AND ma_field ='IncomeAttachment'),''), 
				
				Nominee1ProofType = ISNULL((SELECT ma_filename FROM  Client_ModifyAttach(NOLOCK) WHERE ma_cmcd =  @vcClientCode AND ma_refno = @i_vcRefNo AND ma_field ='Nominee1Attachment'),''), 	
				
				Nominee1Attachment = ISNULL((SELECT CASE WHEN CAST(ma_proof  AS VARCHAR(MAX)) <> '' then CAST(ma_proof  AS VARCHAR(MAX)) else '' end
	            FROM  Client_ModifyAttach(NOLOCK) WHERE ma_cmcd =  @vcClientCode AND ma_refno = @i_vcRefNo AND ma_field ='Nominee1Attachment'),''),
				
				
				Nominee2Attachment = ISNULL((SELECT CASE WHEN CAST(ma_proof  AS VARCHAR(MAX)) <> ''
                then CAST(ma_proof  AS VARCHAR(MAX)) else '' end
	            FROM  Client_ModifyAttach(NOLOCK) WHERE ma_cmcd =  @vcClientCode AND ma_refno = @i_vcRefNo AND ma_field ='Nominee2Attachment'),''),
				Nominee2ProofType = ISNULL((SELECT ma_filename FROM  Client_ModifyAttach(NOLOCK) WHERE ma_cmcd =  @vcClientCode AND ma_refno = @i_vcRefNo AND ma_field ='Nominee2Attachment'),''), 	
				
				Nominee3Attachment = ISNULL((SELECT CASE WHEN CAST(ma_proof  AS VARCHAR(MAX)) <> ''
                then CAST(ma_proof  AS VARCHAR(MAX)) else '' end
	            FROM  Client_ModifyAttach(NOLOCK) WHERE ma_cmcd =  @vcClientCode AND ma_refno = @i_vcRefNo AND ma_field ='Nominee3Attachment'),''),
				Nominee3ProofType = ISNULL((SELECT ma_filename FROM  Client_ModifyAttach(NOLOCK) WHERE ma_cmcd =  @vcClientCode AND ma_refno = @i_vcRefNo AND ma_field ='Nominee3Attachment'),''), 	
				
				SignatureAttachment= ISNULL((SELECT CASE WHEN CAST(ma_proof  AS VARCHAR(MAX)) <> ''
                then CAST(ma_proof  AS VARCHAR(MAX)) else '' end
	            FROM  Client_ModifyAttach(NOLOCK) WHERE ma_cmcd =  @vcClientCode AND ma_refno = @i_vcRefNo AND ma_field ='SignAttachment'),'')
        FOR JSON PATH),
		/*[SegmentDetails] = (SELECT  Segment, SegmentExch = [SegmentExch], [SegmentValue] =  ISNULL(REPLACE(ce_companycode,'"',''),''), 
		[SegmentDescp] = ISNULL(REPLACE(ce_ExchangeDescp,'"',''),'') FROM @tbl_Segment1  FOR JSON PATH)*/
		[SegmentDetails] = (Select SegmentExch, SegmentValue, IsSelect From
							(SELECT   SegmentExch = [ExchSegment], [SegmentValue] =  SegmentValue, Case when Isnull(CD.ce_regDt,'')<>'' then 'true' Else 'false' End as IsSelect
							   FROM @tbl_Segment2 A Left Join Client_details CD ON A.SegmentValue = CD.ce_companycode 
							   And CD.ce_clientcd = @vcClientCode ) A  Order By SegmentExch
							   FOR JSON PATH)
		FROM client_master(NOLOCK) xmain
		WHERE cm_cd = @vcClientCode
		FOR JSON PATH
		)
       
	   IF JSON_VALUE(@json, '$[0].NomineeDetails[0].GuardianDetails[0].NomGuardianSerial') IS NULL  
	   BEGIN
         SET @json = JSON_MODIFY(@json, '$[0].NomineeDetails[0].GuardianDetails', 
         JSON_QUERY('[]'))
	   END	   

	   SET @json = JSON_MODIFY(@json, '$[0].NomineeDetails[1].GuardianDetails[0].NomGuardianSerial', 
       CASE WHEN JSON_VALUE(@json, '$[0].NomineeDetails[1].GuardianDetails') IS NULL THEN JSON_QUERY('[]') 
       ELSE JSON_VALUE(@json, '$[0].NomineeDetails[1].GuardianDetails') END)

	   SET @json = JSON_MODIFY(@json, '$[0].NomineeDetails[2].GuardianDetails[0].NomGuardianSerial', 
       CASE WHEN JSON_VALUE(@json, '$[0].NomineeDetails[2].GuardianDetails') IS NULL THEN JSON_QUERY('[]') 
       ELSE JSON_VALUE(@json, '$[0].NomineeDetails[2].GuardianDetails') END)
 
	  DECLARE @json1 VARCHAR(max) = ''
	  SET @json1 = @json
	  
	  IF CHARINDEX('NomineeDetails',@json1)= 0 
	  BEGIN
	    SET @json1 = JSON_MODIFY(@json1, '$[0].NomineeDetails', JSON_QUERY('[]'))
	  END
  	
	  IF CHARINDEX('BankDetails',@json1)= 0 
	  BEGIN
	    SET @json1 = JSON_MODIFY(@json1, '$[0].BankDetails', JSON_QUERY('[]'))
	  END

	  IF CHARINDEX('DematDetails',@json1)= 0 
	  BEGIN
	    SET @json1 = JSON_MODIFY(@json1, '$[0].DematDetails', JSON_QUERY('[]'))
	  END
  	
	  SET @o_vcJsonOutput = '{	"RekycJson": '+@json1+'}'
      RETURN 
  END
  ELSE
  IF @i_vcTemplateCode = 'Template2'
  BEGIN
	
	---Closed on validation -
  
    DECLARE @dtCloseonDate VARCHAR(8)=''
    SELECT @dtCloseonDate = IsNull(cn_polexpval,'') 
    FROM Client_Nominee(NOLOCK) 
    WHERE cn_cd = @i_vcClientCode
  
    IF @dtCloseonDate <> ''  
    BEGIN
      SET @o_vcErrorFlag  = 'E'
	  SET @o_vcErrorMessage = 'Closed on '+replace(convert(VARCHAR,cast(@dtCloseonDate as date),105),'-','/')
	  set @o_vcJsonOutput = '{"ErrorTag":"E","ErrorMessage":"##MESSAGE##"}'
	  set @o_vcJsonOutput = REPLACE(@o_vcJsonOutput,'##MESSAGE##',@o_vcErrorMessage)
      RETURN 1	
    END
  
    IF EXISTS(sELECT 1 FROM CLIENT_MASTER(NOLOCK) WHERE CM_CD =  @i_vcClientCode AND cm_freezeyn = 'A')
    BEGIN
      SET @o_vcErrorFlag  = 'E'
	  SET @o_vcErrorMessage = 'Client Status is Freeze for All'
	  set @o_vcJsonOutput = '{"ErrorTag":"E","ErrorMessage":"##MESSAGE##"}'
	  set @o_vcJsonOutput = REPLACE(@o_vcJsonOutput,'##MESSAGE##',@o_vcErrorMessage)
      RETURN 1	
    END
	
	declare @dpCount INT = 0
	SELECT @dpCount = count(*) FROM DEMATACT(NOLOCK) WHERE da_clientcd =  @i_vcClientCode 
	AND da_defaultyn = 'Y' AND DA_STATUS = 'A'
  
    IF  @dpCount > 1
	BEGIN
      SET @o_vcErrorFlag  = 'E'
	  SET @o_vcErrorMessage = 'Multiple DP Account Available'
	  set @o_vcJsonOutput = '{}'
      RETURN 1	
    END
	
	SET @dpCount = 0
	SELECT @dpCount = Count(*)
	FROM BANKACT(NOLOCK) BA, BANK_MASTER(NOLOCK) BAM
	WHERE BA.ba_micr = BAM.bk_micr AND BA.ba_ifsccode = BAM.bk_IFCCode AND BA.ba_default = 'Y'
	and BA.ba_clientcd = @i_vcClientCode
	
	IF @dpCount > 1
	BEGIN
      SET @o_vcErrorFlag  = 'E'
	  SET @o_vcErrorMessage = 'Multiple Bank Account Available'
	  set @o_vcJsonOutput = '{}'
      RETURN 1	
    END
	
	
    DECLARE @strSegment VARCHAR(100)=''
    SELECT @strSegment = @strSegment+','+ IIF(@i_vcProcessTag = 'G',LTRIM(RTRIM(CES_Exchange))+'-'+LTRIM(RTRIM(CES_Segment))+' ['+ce_companycode+']',ce_companycode) 
	FROM Client_details(NOLOCK) CD, CompanyExchangeSegments(NOLOCK) EX 
    WHERE ce_clientcd = @vcClientCode AND ce_regDt <> ''
    AND CD.ce_companycode = EX.CES_Cd
    SET @strSegment = SUBSTRING(@strSegment,2,LEN(@strSegment))
	
	DECLARE @tbl_LastTradingDate table (LastTradingDate VARCHAR(8))
	DECLARE @dtLasttradeDate VARCHAR(8)='', @strIsJointAccount VARCHAR(1)='N'
	
	
	SET @string = 'select max(td_dt) from( 
    select isNull(max(td_dt),'''') td_dt from trades where td_clientcd = '''+@vcClientCode+'''
    UNION ALL 
    select isNull(max(td_dt),'''') td_dt from trx where td_clientcd = '''+@vcClientCode+'''
    UNION ALL 
    select isNull(max(td_dt),'''') td_dt from '+ @strCommexConn + '.DBO.trades where td_clientcd='''+ @vcClientCode+''') a '
		
	INSERT INTO @tbl_LastTradingDate(LastTradingDate)
	EXEC(@string)
	
	DECLARE @TBL_ISJOINAC TABLE(IsJointFlag VARCHAR(1))
	
	SET @string = 'select IIF(isNull(cm_sech_name,'''')='''',''N'',''Y'') AS  cm_sech_name '
	+' from '+@dpconn+'.DBO.Client_master where cm_blsavingcd = '''+ @vcClientCode+''''
	
    INSERT INTO @TBL_ISJOINAC(IsJointFlag)
	EXEC(@string)
	
	DECLARE @tbl_MTF TABLE(MTFC_CMCD VARCHAR(8), MTFC_Status VARCHAR(1), MTFC_IntRate numeric(6,2), MTFC_AllowLimit numeric(10,0), 
	MTFC_Frequency VARCHAR(1))
	
	IF EXISTS(SELECT 1 FROM SYS.tables WHERE NAME ='MrgTdgFin_Clients')
	BEGIN
	  SET @string = 'SELECT MTFC_CMCD, MTFC_Status, MTFC_IntRate, MTFC_AllowLimit, MTFC_Frequency  '
	  +' FROM MrgTdgFin_Clients(NOLOCK) WHERE MTFC_CMCD = '''+@vcClientCode+''''
	  INSERT INTO @tbl_MTF  (MTFC_CMCD, MTFC_Status, MTFC_IntRate, MTFC_AllowLimit, MTFC_Frequency)
	  EXEC(@string)
    END
    ELSE
    BEGIN
	  INSERT INTO @tbl_MTF  (MTFC_CMCD, MTFC_Status, MTFC_IntRate, MTFC_AllowLimit, MTFC_Frequency)
	  VALUES(@vcClientCode,'',0,0,'')
    END  	
	  
	
	SELECT @dtLasttradeDate = LastTradingDate FROM @tbl_LastTradingDate
	
	SELECT @strIsJointAccount = IsJointFlag FROM @TBL_ISJOINAC

    SET @o_vcJsonOutput = (SELECT ClientCode = LTRIM(RTRIM(CM_cd)), ClientName = ltrim(rtrim(cm_name)), 
	FirstName = ltrim(rtrim((CASE WHEN CHARINDEX(' ', cm_name) > 0 THEN SUBSTRING(cm_name, 1, CHARINDEX(' ', cm_name) - 1) ELSE cm_name END
				))), MiddleName = ltrim(rtrim((
				CASE WHEN CHARINDEX(' ', cm_name) = 0 THEN '' ELSE RTRIM(LTRIM(REPLACE(REPLACE(cm_name, SUBSTRING(cm_name, 
											1, CHARINDEX(' ', cm_name) - 1), ''), REVERSE(LEFT(REVERSE(cm_name), CHARINDEX(' ', REVERSE(
													cm_name)) - 1)), ''))) END
				))), LastName = ltrim(rtrim((
				CASE WHEN CHARINDEX(' ', cm_name) > 0 THEN REVERSE(LEFT(REVERSE(cm_name), CHARINDEX(' ', REVERSE(cm_name)
									) - 1)) ELSE '' END
				))), Branch = cm_brboffcode /*(SELECT IIF(@i_vcProcessTag = 'G',bm_branchname+' ['+bm_branchcd+']' ,bm_branchcd)
				FROM BRANCH_MASTER(nolock) where bm_branchcd =  cm_brboffcode)*/, 
				BranchName = (SELECT bm_branchname FROM BRANCH_MASTER(nolock) where bm_branchcd =  cm_brboffcode), FatherName = cm_faherhusguar, 
				Nationality = IIF(@i_vcProcessTag = 'G', case when cm_nationalcode = '0' then 'Blank ['+cm_nationalcode+']' 
				when cm_nationalcode = '1' then 'Indian ['+cm_nationalcode+']'
				when cm_nationalcode = '2' then 'Other ['+cm_nationalcode+']' else 'Other ['+cm_nationalcode+']' end, cm_nationalcode) , 
	PAN = cm_panno, UID = cm_uid, Constitution = (SELECT IIF(@i_vcProcessTag = 'G',cc_descrip+' ['+CAST(cc_cd AS varchar)+']',CAST(cc_cd AS varchar)) 
	FROM Client_category(NOLOCK) WHERE CC_CD = cm_constitution), 
	Mobile = LTRIM(RTRIM(cm_mobile)), Email = LTRIM(RTRIM(cm_email)), DateofBirth = 
	cm_dob, Gender = IIF(@i_vcProcessTag = 'G', CASE WHEN cm_sex = 'M' THEN 'Male ['+cm_sex+']'
	WHEN cm_sex = 'F' THEN 'Female ['+cm_sex+']'
	WHEN cm_sex = 'N' THEN 'NA ['+cm_sex+']' ELSE 'NA ['+cm_sex+']' END,cm_sex), 
	MaritalStatus = IIF(@i_vcProcessTag = 'G', CASE WHEN cm_maritalstatus = 'M' THEN 'Married ['+LTRIM(RTRIM(cm_maritalstatus))+']'
	WHEN cm_maritalstatus = 'S' THEN 'Single ['+LTRIM(RTRIM(cm_maritalstatus))+']'
	WHEN cm_maritalstatus = 'W' THEN 'Widow ['+LTRIM(RTRIM(cm_maritalstatus))+']'
	WHEN cm_maritalstatus = 'D' THEN 'Divorce ['+LTRIM(RTRIM(cm_maritalstatus))+']'
	WHEN cm_maritalstatus = 'NA' THEN 'Not Applicable ['+LTRIM(RTRIM(cm_maritalstatus))+']' ELSE 'Not Applicable ['+LTRIM(RTRIM(cm_maritalstatus))+']' END,cm_maritalstatus) , 
	AccountStatus = IIF(@i_vcProcessTag = 'G', CASE WHEN cm_freezeyn = 'N' THEN  'Active ['+cm_freezeyn+']'
	WHEN cm_freezeyn = 'Y' THEN  'Freeze for Trades ['+cm_freezeyn+']'
	WHEN cm_freezeyn = 'B' THEN  'Freeze for Branches ['+cm_freezeyn+']'
	WHEN cm_freezeyn = 'A' THEN  'Freeze for All ['+cm_freezeyn+']' ELSE 'Active ['+cm_freezeyn+']' END, cm_freezeyn)
	, ResidentialStatus =  IIF(@i_vcProcessTag = 'G', CASE WHEN cm_residentialstatus = 'I' THEN 'Indian ['+cm_residentialstatus+']'
	WHEN cm_residentialstatus = 'N' THEN 'NRI ['+cm_residentialstatus+']'
	WHEN cm_residentialstatus = 'F' THEN 'Foreign National ['+cm_residentialstatus+']' ELSE 'Indian ['+cm_residentialstatus+']' END, cm_residentialstatus), 
	ExchangeSegment = @strSegment, CKYCStatus = IIF(@i_vcProcessTag = 'G', case when ISNULL(ck_status, '') = 'Y' THEN 'Yes ['+ISNULL(ck_status, '')
	when ISNULL(ck_status, '') = 'N' THEN 'No ['+ISNULL(ck_status, '')
	else  'Pending' end, ISNULL(ck_status, '')), 
	CKYCRespType = IIF(@i_vcProcessTag = 'G', case when ISNULL(Ck_RespType, '') = '02' then 'Post De-Duplication ['+'02'+']'
	when ISNULL(Ck_RespType, '') = '03' then 'Post FIR ['+'03'+']'
	when ISNULL(Ck_RespType, '') = '04' then 'Post confirmation with ID issuer ['+'04'+']'
	when ISNULL(Ck_RespType, '') = '05' then 'Post KYC Generation ['+'05'+']' else '' end, ISNULL(Ck_RespType, '')), 
	CKYCNumber = ISNULL(Ck_Nfiller1, 0), CKYCDate = ISNULL(ckyc.mkrdt, ''), CKYCReffNo = ISNULL(
		Ck_Reference, ''), KRAStatus = IIF(@i_vcProcessTag = 'G', case when ISNULL(cn_KRAStatus, '') = 'Y' THEN 'Yes ['+ISNULL(cn_KRAStatus, '')+']'
		when ISNULL(cn_KRAStatus, '') = 'N' THEN 'No ['+ISNULL(cn_KRAStatus, '')+']' else '' end, ISNULL(cn_KRAStatus, '')), 
		KRAAddressUpdate = cn_KRADate, 
		FATCAStatus = IIF(@i_vcProcessTag = 'G', case when cn_fillerN0 = 2 then 'Blank [2]'  
		when cn_fillerN0 = 1 then 'Reportable [1]' when cn_fillerN0 = 0 then 'Not Reportable [0]' else '' end,CAST(cn_fillerN0 AS VARCHAR)), 
		FATCADeclaration = cn_fillerN1, FATCADueDiligence = cn_fillerN2, FATCACountry = IIF(cn_filler9 
		<> '', SUBSTRING(cn_filler9, 1, CHARINDEX('~', cn_filler9) - 1), ''), FATCATaxIdentify = IIF(cn_filler9 <> '', 
		SUBSTRING(cn_filler9, CHARINDEX('~', cn_filler9) + 1, CHARINDEX('~', cn_filler9, CHARINDEX('~', cn_filler9
				) + 1) - CHARINDEX('~', cn_filler9) - 1), ''), FATCATypeIdentify = IIF(cn_filler9 <> '', SUBSTRING(cn_filler9, 
			CHARINDEX('~', cn_filler9, CHARINDEX('~', cn_filler9) + 1) + 1, LEN(cn_filler9) - CHARINDEX('~', REVERSE(
					cn_filler9))), ''), BankName = ISNULL(BankName,''), BankAddress1 = ISNULL(BankAddress1,''), 
					BankAddress2 = ISNULL(BankAddress2,''), BankAddress3 = ISNULL(BankAddress3,''), BankAddress4 = ISNULL(BankAddress4,''),  
					BankPincode = ISNULL(BankPincode,''), BankIFSC = ISNULL(LTRIM(RTRIM(BankIFSC)),''), 
					BankAccNo = ISNULL(LTRIM(RTRIM(BankAccNo)),''), BankMICR = ISNULL(LTRIM(RTRIM(BankMICR)),''), 
					BankAccType = ISNULL(BankAccType,''), CorrAddress1 = cm_add1, CorrAddress2 = cm_add2, CorrAddress3 = 
	cm_add3, CorrCity = cm_add4, CorrState = cm_state, CorrCountry = cm_bankactno, CorrPincode = cm_pincode, 
	PerAddress1 = cm_padd1, PerAddress2 = cm_padd2, PerAddress3 = cm_padd3, PerCity = cm_padd4, PerState = cm_pstate, 
	PerCountry = cm_pcountry, PerPincode = cm_ppincode, NetWorth = IIF(@i_vcProcessTag = 'G', case when cm_networth = '1' then  'Below Rs. 1  Lac [1]'
	when cm_networth = '2' then  'Btw Rs. 1 to Rs. 5 Lacs [2]'
	when cm_networth = '3' then  'Btw Rs. 5 to Rs. 10 Lacs [3]'
	when cm_networth = '4' then  'Btw Rs. 10 to Rs. 25 Lacs [4]'
	when cm_networth = '5' then  'Btw Rs. 25 Lacs to Rs. 1 Crore [5]'
	when cm_networth = '6' then  'More than Rs. 1 Crore [6]' else '' end, cm_networth), NetworthDate = cm_networthdt, 
	GrossAnnualIncome = IIF(@i_vcProcessTag = 'G', case when CM_grossincome = '1' then  'Below Rs. 1  Lac [1]'
	when CM_grossincome = '2' then  'Btw Rs. 1 to Rs. 5 Lacs [2]'
	when CM_grossincome = '3' then  'Btw Rs. 5 to Rs. 10 Lacs [3]'
	when CM_grossincome = '4' then  'Btw Rs. 10 to Rs. 25 Lacs [4]'
	when CM_grossincome = '5' then  'Btw Rs. 25 Lacs to Rs. 1 Crore [5]'
	when CM_grossincome = '6' then  'More than Rs. 1 Crore [6]' else '' end, CM_grossincome), 
	GrossAnnualIncomeDate = cm_grossincomedt, RM = cm_dpactno, ReKYC = '', 
	UpdateCDSLNSDL = '', DPType = IIF(SUBSTRING(DA.da_dpid, 1, 2) = 'IN', 'NSDL', 'CDSL'), DPID = ISNULL(DA.da_dpid,''), DPAcno = 
	ISNULL(DA.da_actno,''), Occupation = cm_occup, 
	LastTradingDate = ISNULL(@dtLasttradeDate,''),
	GeneralPOA = IIF(isNull(pa.cn_fillerN4,'0')='0','Y','N'), MarginPledgePOA = IIF(isNull(pa.cn_fillerN5,'0') = '1','Y','N'), 
	DDPIPOA = IIF(isNull(pa.cn_fillerN3,'0')='1','Y','N'), IsJointAccount  = @strIsJointAccount,
	NomUCCCode = ISNULL(NomUCCCode, ''), NomRegDate = ISNULL(NomRegDate, ''), 
	NomFirstName = ISNULL(NomFirstName, ''), NomMiddleName = ISNULL(NomMiddleName, ''), NomLastName = ISNULL(
		NomLastName, ''), NomFatherSpouseName = ISNULL(NomFatherSpouseName, ''), NomAddress1 = ISNULL(NomAddress1, 
		''), NomAddress2 = ISNULL(NomAddress2, ''), NomAddress3 = ISNULL(NomAddress3, ''), NomAddressCity = ISNULL(
		NomAddressCity, ''), NomAddressState = ISNULL(NomAddressState, ''), NomAddressCountry = ISNULL(
		NomAddressCountry, ''), NomMobileCountryCode = ISNULL(NomMobileCountryCode, ''), NomAddressPin = ISNULL(
		NomAddressPin, ''), NomineePAN = ISNULL(NomineePAN, ''), NomineeRegDt = ISNULL(NomineeRegDt, ''), 
	NomineeMobile = ISNULL(NomineeMobile, ''), NomineeEmail = ISNULL(NomineeEmail, ''), NomineeUID = ISNULL(
		NomineeUID, ''), NomineeDOB = ISNULL(NomineeDOB, ''), NomineeRelation = LTRIM(RTRIM(ISNULL(NomineeRelation, ''))), 
	NomineeSharePercentage = ISNULL(NomineeSharePercentage, 0), NomineeResidualSecurities = ISNULL(
		NomineeResidualSecurities, ''), NomGuardianFirstName = ISNULL(NomGuardianFirstName, ''), 
	NomGuardianMiddleName = ISNULL(NomGuardianMiddleName, ''), NomGuardianLastName = ISNULL(
		NomGuardianLastName, ''), NomGuardianFatherSpouseName = ISNULL(NomGuardianFatherSpouseName, ''), 
	NomGuardianAddress1 = ISNULL(NomGuardianAddress1, ''), NomGuardianAddress2 = ISNULL(NomGuardianAddress2, 
		''), NomGuardianAddress3 = ISNULL(NomGuardianAddress3, ''), NomGuardianAddressCity = ISNULL(
		NomGuardianAddressCity, ''), NomGuardianAddressState = ISNULL(NomGuardianAddressState, ''), 
	NomGuardianAddressCountry = ISNULL(NomGuardianAddressCountry, ''), NomGuardianAddressPin = ISNULL(
		NomGuardianAddressPin, ''), NomGuardianMobile = ISNULL(NomGuardianMobile, ''), NomGuardianEmail = ISNULL(
		NomGuardianEmail, ''), NomGuardianUID = ISNULL(NomGuardianUID, ''), NomGuardianPAN = ISNULL(NomGuardianPAN
		, ''), NomGuardianRelation = LTRIM(RTRIM(ISNULL(NomGuardianRelation, ''))), SecondNomFirstName = ISNULL(
		SecondNomFirstName, ''), SecondNomMiddleName = ISNULL(SecondNomMiddleName, ''), SecondNomLastName = ISNULL
	(SecondNomLastName, ''), SecondNomFatherSpouseName = ISNULL(SecondNomFatherSpouseName, ''), 
	SecondNomAddress1 = ISNULL(SecondNomAddress1, ''), SecondNomAddress2 = ISNULL(SecondNomAddress2, ''), 
	SecondNomAddress3 = ISNULL(SecondNomAddress3, ''), SecondNomAddressCity = ISNULL(SecondNomAddressCity, '')
	, SecondNomAddressState = ISNULL(SecondNomAddressState, ''), SecondNomAddressCountry = ISNULL(
		SecondNomAddressCountry, ''), SecondNomMobileCountryCode = ISNULL(SecondNomMobileCountryCode, ''), 
	SecondNomAddressPin = ISNULL(SecondNomAddressPin, ''), SecondNomineePAN = ISNULL(SecondNomineePAN, ''), 
	SecondNomineeRegDt = ISNULL(SecondNomineeRegDt, ''), SecondNomineeMobile = ISNULL(SecondNomineeMobile, '')
	, SecondNomineeEmail = ISNULL(SecondNomineeEmail, ''), SecondNomineeUID = ISNULL(SecondNomineeUID, ''), 
	SecondNomineeDOB = ISNULL(SecondNomineeDOB, ''), SecondNomineeRelation = LTRIM(RTRIM(ISNULL(SecondNomineeRelation, '')))
	, SecondNomineeSharePercentage = ISNULL(SecondNomineeSharePercentage, 0), 
	SecondNomineeResidualSecurities = ISNULL(SecondNomineeResidualSecurities, ''), 
	SecondNomGuardianFirstName = ISNULL(SecondNomGuardianFirstName, ''), SecondNomGuardianMiddleName = ISNULL
	(SecondNomGuardianMiddleName, ''), SecondNomGuardianLastName = ISNULL(SecondNomGuardianLastName, ''), 
	SecondNomGuardianFatherSpouseName = ISNULL(SecondNomGuardianFatherSpouseName, ''), SecondNomGuardianAddress1 = ISNULL(
		SecondNomGuardianAddress1, ''), SecondNomGuardianAddress2 = ISNULL(SecondNomGuardianAddress2, ''), 
	SecondNomGuardianAddress3 = ISNULL(SecondNomGuardianAddress3, ''), SecondNomGuardianAddressCity = ISNULL(
		SecondNomGuardianAddressCity, ''), SecondNomGuardianAddressState = ISNULL(
		SecondNomGuardianAddressState, ''), SecondNomGuardianAddressCountry = ISNULL(
		SecondNomGuardianAddressCountry, ''), SecondNomGuardianMobileCountryCode = ISNULL(
		SecondNomGuardianMobileCountryCode, ''), SecondNomGuardianAddressPin = ISNULL(
		SecondNomGuardianAddressPin, ''), SecondNomGuardianPAN = ISNULL(SecondNomGuardianPAN, ''), 
	SecondNomGuardianMobile = ISNULL(SecondNomGuardianMobile, ''), SecondNomGuardianEmail = ISNULL(
		SecondNomGuardianEmail, ''), SecondNomGuardianUID = ISNULL(SecondNomGuardianUID, ''), 
	SecondNomGuardianRelation = LTRIM(RTRIM(ISNULL(SecondNomGuardianRelation, ''))), ThirdNomFirstName = ISNULL(
		ThirdNomFirstName, ''), ThirdNomMiddleName = ISNULL(ThirdNomMiddleName, ''), ThirdNomLastName = ISNULL(
		ThirdNomLastName, ''), ThirdNomFatherSpouseName = ISNULL(ThirdNomFatherSpouseName, ''), ThirdNomAddress1 
	= ISNULL(ThirdNomAddress1, ''), ThirdNomAddress2 = ISNULL(ThirdNomAddress2, ''), ThirdNomAddress3 = ISNULL(
		ThirdNomAddress3, ''), ThirdNomAddressCity = ISNULL(ThirdNomAddressCity, ''), ThirdNomAddressState = 
	ISNULL(ThirdNomAddressState, ''), ThirdNomAddressCountry = ISNULL(ThirdNomAddressCountry, ''), 
	ThirdNomMobileCountryCode = ISNULL(ThirdNomMobileCountryCode, ''), ThirdNomAddressPin = ISNULL(
		ThirdNomAddressPin, ''), ThirdNomineePAN = ISNULL(ThirdNomineePAN, ''), ThirdNomineeRegDt = ISNULL(
		ThirdNomineeRegDt, ''), ThirdNomineeMobile = ISNULL(ThirdNomineeMobile, ''), ThirdNomineeEmail = ISNULL(
		ThirdNomineeEmail, ''), ThirdNomineeUID = ISNULL(ThirdNomineeUID, ''), ThirdNomineeDOB = ISNULL(
		ThirdNomineeDOB, ''), ThirdNomineeRelation = LTRIM(RTRIM(ISNULL(ThirdNomineeRelation, ''))), 
	ThirdNomineeSharePercentage = ISNULL(ThirdNomineeSharePercentage, 0), ThirdNomineeResidualSecurities = 
	ISNULL(ThirdNomineeResidualSecurities, ''), ThirdNomGuardianFirstName = ISNULL(
		ThirdNomGuardianFirstName, ''), ThirdNomGuardianMiddleName = ISNULL(ThirdNomGuardianMiddleName, ''), 
	ThirdNomGuardianLastName = ISNULL(ThirdNomGuardianLastName, ''), ThirdNomGuardianFatherSpouseName = 
	ISNULL(ThirdNomGuardianFatherSpouseName, ''), ThirdNomGuardianAddress1 = ISNULL(
		ThirdNomGuardianAddress1, ''), ThirdNomGuardianAddress2 = ISNULL(ThirdNomGuardianAddress2, ''), 
	ThirdNomGuardianAddress3 = ISNULL(ThirdNomGuardianAddress3, ''), ThirdNomGuardianAddressCity = ISNULL(
		ThirdNomGuardianAddressCity, ''), ThirdNomGuardianAddressState = ISNULL(ThirdNomGuardianAddressState, 
		''), ThirdNomGuardianAddressCountry = ISNULL(ThirdNomGuardianAddressCountry, ''), 
	ThirdNomGuardianAddressPin = ISNULL(ThirdNomGuardianAddressPin, ''), ThirdNomGuardianPAN = ISNULL(
		ThirdNomGuardianPAN, ''), ThirdNomGuardianMobile = ISNULL(ThirdNomGuardianMobile, ''), 
	ThirdNomGuardianEmail = ISNULL(ThirdNomGuardianEmail, ''), ThirdNomGuardianUID = ISNULL(
		ThirdNomGuardianUID, ''), ThirdNomGuardianRelation = rtrim(ltrim(ISNULL(ThirdNomGuardianRelation, ''))),
	[MTFSegment] = ISNULL(MTFC_Status,''), [MTFInterestRate] = ISNULL(MTFC_IntRate,0), 
	[MTFLimit] = ISNULL(MTFC_AllowLimit,0), [MTFInterestFeq] = ISNULL(MTFC_Frequency,'')
    FROM CLIENT_MASTER(NOLOCK) cm
    LEFT OUTER JOIN (SELECT * FROM Client_CKYC(NOLOCK) CXXX WHERE CK_SRNO IN(SELECT MAX(CK_SRNO)
	FROM Client_CKYC WHERE CK_Panno = CXXX.CK_Panno)) ckyc
	ON (cm.cm_panno = ckyc.CK_Panno)
    LEFT OUTER JOIN Client_Nominee(NOLOCK) pa
	ON (cm.cm_Cd = pa.cn_cd)
    LEFT OUTER JOIN DEMATACT(NOLOCK) DA
	ON (cm.cm_Cd = DA.da_clientcd AND DA.da_defaultyn = 'Y' AND DA.DA_STATUS = 'A')
    LEFT OUTER JOIN (
	SELECT ba_clientcd, BankName = ISNULL(bk_name, ''), BankAddress1 = ISNULL(bk_add1, ''), BankAddress2 = ISNULL(
			bk_add2, ''), BankAddress3 = ISNULL(bk_add3, ''), BankAddress4 = ISNULL(bk_city, ''), BankPincode = ISNULL(
			bk_pin, ''), BankIFSC = ISNULL(ba_ifsccode, ''), BankAccNo = ISNULL(BA.ba_actno, ''), BankMICR = ISNULL(
			ba_micr, ''), BankAccType = ISNULL(BA.ba_acttype, '')
	FROM BANKACT(NOLOCK) BA, BANK_MASTER(NOLOCK) BAM
	WHERE BA.ba_micr = BAM.bk_micr AND BA.ba_ifsccode = BAM.bk_IFCCode AND BA.ba_default = 'Y'
	) BANK
	ON (CM.cm_Cd = BANK.ba_clientcd)
    LEFT OUTER JOIN (
	SELECT cn_Cmcd, NomUCCCode = ISNULL(cn_UCC, ''), NomRegDate = ISNULL(cn_regdt, ''), NomFirstName = ltrim(rtrim(
					(CASE WHEN CHARINDEX(' ', cn_Name) > 0 THEN SUBSTRING(cn_Name, 1, CHARINDEX(' ', cn_Name) - 1) ELSE cn_Name END
					))), NomMiddleName = ltrim(rtrim((
					CASE WHEN CHARINDEX(' ', cn_Name) = 0 THEN '' ELSE RTRIM(LTRIM(REPLACE(REPLACE(cn_Name, SUBSTRING(cn_Name
												, 1, CHARINDEX(' ', cn_Name) - 1), ''), REVERSE(LEFT(REVERSE(cn_Name), CHARINDEX(' ', REVERSE(
														cn_Name)) - 1)), ''))) END
					))), NomLastName = ltrim(rtrim((
					CASE WHEN CHARINDEX(' ', cn_Name) > 0 THEN REVERSE(LEFT(REVERSE(cn_Name), CHARINDEX(' ', REVERSE(cn_Name
											)) - 1)) ELSE '' END
					))), NomFatherSpouseName = ISNULL(cn_FathHusbnm, ''), NomAddress1 = ISNULL(cn_Add1, ''), NomAddress2 = 
		ISNULL(cn_Add2, ''), NomAddress3 = ISNULL(cn_Add3, ''), NomAddressCity = ISNULL(cn_City, ''), 
		NomAddressState = ISNULL(cn_State, ''), NomAddressCountry = ISNULL(cn_Country, ''), NomMobileCountryCode = 
		'', NomAddressPin = ISNULL(cn_Pin, ''), NomineePAN = ISNULL(cn_PAN, ''), NomineeRegDt = ISNULL(cn_regdt, ''), 
		NomineeMobile = ISNULL(cn_Tel, ''), NomineeEmail = ISNULL(cn_Email, ''), NomineeUID = ISNULL(cn_UID, ''), 
		NomineeDOB = ISNULL(cn_DOB, ''), 
		NomineeRelation = CASE WHEN ISNULL(cn_Relation, '') ='Spouse' THEN '01'  
		WHEN ISNULL(cn_Relation, '') ='Son' THEN '02'  
		WHEN ISNULL(cn_Relation, '') ='Daughter' THEN '03'  
		WHEN ISNULL(cn_Relation, '') ='Father' THEN '04'  
		WHEN ISNULL(cn_Relation, '') ='Mother' THEN '05'  
		WHEN ISNULL(cn_Relation, '') ='Brother' THEN '06'  
		WHEN ISNULL(cn_Relation, '') ='Sister' THEN '07'  
		WHEN ISNULL(cn_Relation, '') ='Grandson' THEN '08'  
		WHEN ISNULL(cn_Relation, '') ='Granddaughter' THEN '09'  
		WHEN ISNULL(cn_Relation, '') ='Grandfather' THEN '10'  
		WHEN ISNULL(cn_Relation, '') ='Grandmother' THEN '11'  
		WHEN ISNULL(cn_Relation, '') ='Not Provided' THEN '12'  
		WHEN ISNULL(cn_Relation, '') ='Others' THEN '13' ELSE  ISNULL(cn_Relation, '') END,
		NomineeSharePercentage = ISNULL(
			cn_NomPershare, 0), NomineeResidualSecurities = ISNULL(cn_ResidualFlag, '')
	FROM Client_NomineeDetails(NOLOCK)
	WHERE cn_Cmcd = @vcClientCode AND cn_Srno = '1'
	) NOM1
	ON (CM.cm_Cd = NOM1.cn_Cmcd)
    LEFT OUTER JOIN (
	SELECT cn_Cmcd, NomGuardianFirstName = ltrim(rtrim((CASE WHEN CHARINDEX(' ', cn_Name) > 0 THEN SUBSTRING(cn_Name, 1, CHARINDEX(' ', cn_Name) - 1) ELSE cn_Name END
					))), NomGuardianMiddleName = ltrim(rtrim((
					CASE WHEN CHARINDEX(' ', cn_Name) = 0 THEN '' ELSE RTRIM(LTRIM(REPLACE(REPLACE(cn_Name, SUBSTRING(cn_Name
												, 1, CHARINDEX(' ', cn_Name) - 1), ''), REVERSE(LEFT(REVERSE(cn_Name), CHARINDEX(' ', REVERSE(
														cn_Name)) - 1)), ''))) END
					))), NomGuardianLastName = ltrim(rtrim((
					CASE WHEN CHARINDEX(' ', cn_Name) > 0 THEN REVERSE(LEFT(REVERSE(cn_Name), CHARINDEX(' ', REVERSE(cn_Name
											)) - 1)) ELSE '' END
					))), NomGuardianFatherSpouseName = ISNULL(cn_FathHusbnm, ''), NomGuardianAddress1 = ISNULL(cn_Add1, '')
		, NomGuardianAddress2 = ISNULL(cn_Add2, ''), NomGuardianAddress3 = ISNULL(cn_Add3, ''), 
		NomGuardianAddressCity = ISNULL(cn_City, ''), NomGuardianAddressState = ISNULL(cn_State, ''), 
		NomGuardianAddressCountry = ISNULL(cn_Country, ''), NomGuardianAddressPin = ISNULL(cn_Pin, ''), 
		NomGuardianPAN = ISNULL(cn_PAN, ''), NomGuardianMobile = ISNULL(cn_Tel, ''), NomGuardianEmail = ISNULL(
			cn_Email, ''), NomGuardianUID = ISNULL(cn_UID, ''), 
	    NomGuardianRelation = CASE WHEN ISNULL(cn_Relation, '') ='Spouse' THEN '01'  
		WHEN ISNULL(cn_Relation, '') ='Son' THEN '02'  
		WHEN ISNULL(cn_Relation, '') ='Daughter' THEN '03'  
		WHEN ISNULL(cn_Relation, '') ='Father' THEN '04'  
		WHEN ISNULL(cn_Relation, '') ='Mother' THEN '05'  
		WHEN ISNULL(cn_Relation, '') ='Brother' THEN '06'  
		WHEN ISNULL(cn_Relation, '') ='Sister' THEN '07'  
		WHEN ISNULL(cn_Relation, '') ='Grandson' THEN '08'  
		WHEN ISNULL(cn_Relation, '') ='Granddaughter' THEN '09'  
		WHEN ISNULL(cn_Relation, '') ='Grandfather' THEN '10'  
		WHEN ISNULL(cn_Relation, '') ='Grandmother' THEN '11'  
		WHEN ISNULL(cn_Relation, '') ='Not Provided' THEN '12'  
		WHEN ISNULL(cn_Relation, '') ='Others' THEN '13' ELSE  ISNULL(cn_Relation, '') END
	FROM Client_NomineeDetails(NOLOCK)
	WHERE cn_Cmcd = @vcClientCode AND cn_Srno = '2'
	) GUR1
	ON (CM.cm_Cd = GUR1.cn_Cmcd)
    LEFT OUTER JOIN (
	SELECT cn_Cmcd, SecondNomFirstName = ltrim(rtrim((CASE WHEN CHARINDEX(' ', cn_Name) > 0 THEN SUBSTRING(cn_Name, 1, CHARINDEX(' ', cn_Name) - 1) ELSE cn_Name END
					))), SecondNomMiddleName = ltrim(rtrim((
					CASE WHEN CHARINDEX(' ', cn_Name) = 0 THEN '' ELSE RTRIM(LTRIM(REPLACE(REPLACE(cn_Name, SUBSTRING(cn_Name
												, 1, CHARINDEX(' ', cn_Name) - 1), ''), REVERSE(LEFT(REVERSE(cn_Name), CHARINDEX(' ', REVERSE(
														cn_Name)) - 1)), ''))) END
					))), SecondNomLastName = ltrim(rtrim((
					CASE WHEN CHARINDEX(' ', cn_Name) > 0 THEN REVERSE(LEFT(REVERSE(cn_Name), CHARINDEX(' ', REVERSE(cn_Name
											)) - 1)) ELSE '' END
					))), SecondNomFatherSpouseName = ISNULL(cn_FathHusbnm, ''), SecondNomAddress1 = ISNULL(cn_Add1, ''), 
		SecondNomAddress2 = ISNULL(cn_Add2, ''), SecondNomAddress3 = ISNULL(cn_Add3, ''), SecondNomAddressCity = 
		ISNULL(cn_City, ''), SecondNomAddressState = ISNULL(cn_State, ''), SecondNomAddressCountry = ISNULL(
			cn_Country, ''), SecondNomMobileCountryCode = '', SecondNomAddressPin = ISNULL(cn_Pin, ''), 
		SecondNomineePAN = ISNULL(cn_PAN, ''), SecondNomineeRegDt = ISNULL(cn_regdt, ''), SecondNomineeMobile = 
		ISNULL(cn_Tel, ''), SecondNomineeEmail = ISNULL(cn_Email, ''), SecondNomineeUID = ISNULL(cn_UID, ''), 
		SecondNomineeDOB = ISNULL(cn_DOB, ''), 
		SecondNomineeRelation = CASE WHEN ISNULL(cn_Relation, '') ='Spouse' THEN '01'  
		WHEN ISNULL(cn_Relation, '') ='Son' THEN '02'  
		WHEN ISNULL(cn_Relation, '') ='Daughter' THEN '03'  
		WHEN ISNULL(cn_Relation, '') ='Father' THEN '04'  
		WHEN ISNULL(cn_Relation, '') ='Mother' THEN '05'  
		WHEN ISNULL(cn_Relation, '') ='Brother' THEN '06'  
		WHEN ISNULL(cn_Relation, '') ='Sister' THEN '07'  
		WHEN ISNULL(cn_Relation, '') ='Grandson' THEN '08'  
		WHEN ISNULL(cn_Relation, '') ='Granddaughter' THEN '09'  
		WHEN ISNULL(cn_Relation, '') ='Grandfather' THEN '10'  
		WHEN ISNULL(cn_Relation, '') ='Grandmother' THEN '11'  
		WHEN ISNULL(cn_Relation, '') ='Not Provided' THEN '12'  
		WHEN ISNULL(cn_Relation, '') ='Others' THEN '13' ELSE  ISNULL(cn_Relation, '') END,
		SecondNomineeSharePercentage = ISNULL(cn_NomPershare, ''), SecondNomineeResidualSecurities = ISNULL(
			cn_ResidualFlag, '')
	FROM Client_NomineeDetails(NOLOCK)
	WHERE cn_Cmcd = @vcClientCode AND cn_Srno = '3'
	) NOM2
	ON (CM.cm_Cd = NOM2.cn_Cmcd)
    LEFT OUTER JOIN (
	SELECT cn_Cmcd, SecondNomGuardianFirstName = ltrim(rtrim((CASE WHEN CHARINDEX(' ', cn_Name) > 0 THEN SUBSTRING(cn_Name, 1, CHARINDEX(' ', cn_Name) - 1) ELSE cn_Name END
					))), SecondNomGuardianMiddleName = ltrim(rtrim((
					CASE WHEN CHARINDEX(' ', cn_Name) = 0 THEN '' ELSE RTRIM(LTRIM(REPLACE(REPLACE(cn_Name, SUBSTRING(cn_Name
												, 1, CHARINDEX(' ', cn_Name) - 1), ''), REVERSE(LEFT(REVERSE(cn_Name), CHARINDEX(' ', REVERSE(
														cn_Name)) - 1)), ''))) END
					))), SecondNomGuardianLastName = ltrim(rtrim((
					CASE WHEN CHARINDEX(' ', cn_Name) > 0 THEN REVERSE(LEFT(REVERSE(cn_Name), CHARINDEX(' ', REVERSE(cn_Name
											)) - 1)) ELSE '' END
					))), SecondNomGuardianFatherSpouseName = ISNULL(cn_FathHusbnm, ''), SecondNomGuardianAddress1 = ISNULL
		(cn_Add1, ''), SecondNomGuardianAddress2 = ISNULL(cn_Add2, ''), SecondNomGuardianAddress3 = ISNULL(cn_Add3
			, ''), SecondNomGuardianAddressCity = ISNULL(cn_City, ''), SecondNomGuardianAddressState = ISNULL(
			cn_State, ''), SecondNomGuardianAddressCountry = ISNULL(cn_Country, ''), 
		SecondNomGuardianMobileCountryCode = '', SecondNomGuardianAddressPin = ISNULL(cn_Pin, ''), 
		SecondNomGuardianPAN = ISNULL(cn_PAN, ''), SecondNomGuardianMobile = ISNULL(cn_Tel, ''), 
		SecondNomGuardianEmail = ISNULL(cn_Email, ''), SecondNomGuardianUID = ISNULL(cn_UID, ''), 
		SecondNomGuardianRelation = CASE WHEN ISNULL(cn_Relation, '') ='Spouse' THEN '01'  
		WHEN ISNULL(cn_Relation, '') ='Son' THEN '02'  
		WHEN ISNULL(cn_Relation, '') ='Daughter' THEN '03'  
		WHEN ISNULL(cn_Relation, '') ='Father' THEN '04'  
		WHEN ISNULL(cn_Relation, '') ='Mother' THEN '05'  
		WHEN ISNULL(cn_Relation, '') ='Brother' THEN '06'  
		WHEN ISNULL(cn_Relation, '') ='Sister' THEN '07'  
		WHEN ISNULL(cn_Relation, '') ='Grandson' THEN '08'  
		WHEN ISNULL(cn_Relation, '') ='Granddaughter' THEN '09'  
		WHEN ISNULL(cn_Relation, '') ='Grandfather' THEN '10'  
		WHEN ISNULL(cn_Relation, '') ='Grandmother' THEN '11'  
		WHEN ISNULL(cn_Relation, '') ='Not Provided' THEN '12'  
		WHEN ISNULL(cn_Relation, '') ='Others' THEN '13' ELSE  ISNULL(cn_Relation, '') END
	FROM Client_NomineeDetails(NOLOCK)
	WHERE cn_Cmcd = @vcClientCode AND cn_Srno = '4'
	) GUR2
	ON (CM.cm_Cd = GUR2.cn_Cmcd)
    LEFT OUTER JOIN (
	SELECT cn_Cmcd, ThirdNomFirstName = ltrim(rtrim((CASE WHEN CHARINDEX(' ', cn_Name) > 0 THEN SUBSTRING(cn_Name, 1, CHARINDEX(' ', cn_Name) - 1) ELSE cn_Name END
					))), ThirdNomMiddleName = ltrim(rtrim((
					CASE WHEN CHARINDEX(' ', cn_Name) = 0 THEN '' ELSE RTRIM(LTRIM(REPLACE(REPLACE(cn_Name, SUBSTRING(cn_Name
												, 1, CHARINDEX(' ', cn_Name) - 1), ''), REVERSE(LEFT(REVERSE(cn_Name), CHARINDEX(' ', REVERSE(
														cn_Name)) - 1)), ''))) END
					))), ThirdNomLastName = ltrim(rtrim((
					CASE WHEN CHARINDEX(' ', cn_Name) > 0 THEN REVERSE(LEFT(REVERSE(cn_Name), CHARINDEX(' ', REVERSE(cn_Name
											)) - 1)) ELSE '' END
					))), ThirdNomFatherSpouseName = ISNULL(cn_FathHusbnm, ''), ThirdNomAddress1 = ISNULL(cn_Add1, ''), 
		ThirdNomAddress2 = ISNULL(cn_Add2, ''), ThirdNomAddress3 = ISNULL(cn_Add3, ''), ThirdNomAddressCity = ISNULL
		(cn_City, ''), ThirdNomAddressState = ISNULL(cn_State, ''), ThirdNomAddressCountry = ISNULL(cn_Country, '')
		, ThirdNomMobileCountryCode = '', ThirdNomAddressPin = ISNULL(cn_Pin, ''), ThirdNomineePAN = ISNULL(cn_PAN, 
			''), ThirdNomineeRegDt = ISNULL(cn_regdt, ''), ThirdNomineeMobile = ISNULL(cn_Tel, ''), ThirdNomineeEmail = 
		ISNULL(cn_Email, ''), ThirdNomineeUID = ISNULL(cn_UID, ''), ThirdNomineeDOB = ISNULL(cn_DOB, ''), 
		ThirdNomineeRelation = CASE WHEN ISNULL(cn_Relation, '') ='Spouse' THEN '01'  
		WHEN ISNULL(cn_Relation, '') ='Son' THEN '02'  
		WHEN ISNULL(cn_Relation, '') ='Daughter' THEN '03'  
		WHEN ISNULL(cn_Relation, '') ='Father' THEN '04'  
		WHEN ISNULL(cn_Relation, '') ='Mother' THEN '05'  
		WHEN ISNULL(cn_Relation, '') ='Brother' THEN '06'  
		WHEN ISNULL(cn_Relation, '') ='Sister' THEN '07'  
		WHEN ISNULL(cn_Relation, '') ='Grandson' THEN '08'  
		WHEN ISNULL(cn_Relation, '') ='Granddaughter' THEN '09'  
		WHEN ISNULL(cn_Relation, '') ='Grandfather' THEN '10'  
		WHEN ISNULL(cn_Relation, '') ='Grandmother' THEN '11'  
		WHEN ISNULL(cn_Relation, '') ='Not Provided' THEN '12'  
		WHEN ISNULL(cn_Relation, '') ='Others' THEN '13' ELSE  ISNULL(cn_Relation, '') END, ThirdNomineeSharePercentage = ISNULL(cn_NomPershare, 0), 
		ThirdNomineeResidualSecurities = ISNULL(cn_ResidualFlag, '')
	FROM Client_NomineeDetails(NOLOCK)
	WHERE cn_Cmcd = @vcClientCode AND cn_Srno = '5'
	) NOM3
	ON (CM.cm_Cd = NOM3.cn_Cmcd)
    LEFT OUTER JOIN (
	SELECT cn_Cmcd, ThirdNomGuardianFirstName = ltrim(rtrim((CASE WHEN CHARINDEX(' ', cn_Name) > 0 THEN SUBSTRING(cn_Name, 1, CHARINDEX(' ', cn_Name) - 1) ELSE cn_Name END
					))), ThirdNomGuardianMiddleName = ltrim(rtrim((
					CASE WHEN CHARINDEX(' ', cn_Name) = 0 THEN '' ELSE RTRIM(LTRIM(REPLACE(REPLACE(cn_Name, SUBSTRING(cn_Name
												, 1, CHARINDEX(' ', cn_Name) - 1), ''), REVERSE(LEFT(REVERSE(cn_Name), CHARINDEX(' ', REVERSE(
														cn_Name)) - 1)), ''))) END
					))), ThirdNomGuardianLastName = ltrim(rtrim((
					CASE WHEN CHARINDEX(' ', cn_Name) > 0 THEN REVERSE(LEFT(REVERSE(cn_Name), CHARINDEX(' ', REVERSE(cn_Name
											)) - 1)) ELSE '' END
					))), ThirdNomGuardianFatherSpouseName = cn_FathHusbnm, ThirdNomGuardianAddress1 = cn_Add1, 
		ThirdNomGuardianAddress2 = cn_Add2, ThirdNomGuardianAddress3 = cn_Add3, ThirdNomGuardianAddressCity = 
		cn_City, ThirdNomGuardianAddressState = cn_State, ThirdNomGuardianAddressCountry = cn_Country, 
		ThirdNomGuardianMobileCountryCode = '', ThirdNomGuardianAddressPin = cn_Pin, ThirdNomGuardianPAN = cn_PAN, 
		ThirdNomGuardianMobile = cn_Tel, ThirdNomGuardianEmail = cn_Email, ThirdNomGuardianUID = cn_UID, 
		ThirdNomGuardianRelation = CASE WHEN ISNULL(cn_Relation, '') ='Spouse' THEN '01'  
		WHEN ISNULL(cn_Relation, '') ='Son' THEN '02'  
		WHEN ISNULL(cn_Relation, '') ='Daughter' THEN '03'  
		WHEN ISNULL(cn_Relation, '') ='Father' THEN '04'  
		WHEN ISNULL(cn_Relation, '') ='Mother' THEN '05'  
		WHEN ISNULL(cn_Relation, '') ='Brother' THEN '06'  
		WHEN ISNULL(cn_Relation, '') ='Sister' THEN '07'  
		WHEN ISNULL(cn_Relation, '') ='Grandson' THEN '08'  
		WHEN ISNULL(cn_Relation, '') ='Granddaughter' THEN '09'  
		WHEN ISNULL(cn_Relation, '') ='Grandfather' THEN '10'  
		WHEN ISNULL(cn_Relation, '') ='Grandmother' THEN '11'  
		WHEN ISNULL(cn_Relation, '') ='Not Provided' THEN '12'  
		WHEN ISNULL(cn_Relation, '') ='Others' THEN '13' ELSE  ISNULL(cn_Relation, '') END
	FROM Client_NomineeDetails(NOLOCK)
	WHERE cn_Cmcd = @vcClientCode AND cn_Srno = '6'
	) GUR3
	ON (CM.cm_Cd = GUR3.cn_Cmcd)
	LEFT OUTER JOIN (SELECT MTFC_CMCD, MTFC_Status, MTFC_IntRate, MTFC_AllowLimit, MTFC_Frequency  
	FROM @tbl_MTF WHERE MTFC_CMCD = @vcClientCode) MTF ON(CM.cm_Cd = MTF.MTFC_CMCD), 
	Client_Info(NOLOCK) INFO
    WHERE CM_cD = @vcClientCode AND CM_cD = INFO.cm2_cd 	FOR JSON PATH)
	set @o_vcJsonOutput = substring(@o_vcJsonOutput,2,len(@o_vcJsonOutput))
	set @o_vcJsonOutput = LEFT(@o_vcJsonOutput, LEN(@o_vcJsonOutput) - 1) 
	RETURN 1
  END
  ELSE IF @i_vcTemplateCode	 ='ONLYCLOSURE'
  BEGIN
    EXEC stpr_ReKyc_ClosureGetData @vcClientCode,@o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT, @o_vcJsonOutput OUTPUT 
	RETURN 1
  END
END
GO

CREATE PROCEDURE [SP_ReKyc_MakerPost] @i_vcOldJsonString NVARCHAR(MAX)='', 
@i_vcNewJsonString NVARCHAR(MAX), @i_vcClientCode VARCHAR(20), @i_vcRefNo numeric(10), @i_vccomputername varchar(50),   
@o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(500) OUTPUT, @o_vcJsonOutput VARCHAR(MAX) OUTPUT, 
@i_vcTemplateCode VARCHAR(20) = 'Template1' WITH ENCRYPTION AS
BEGIN
  --- GETTING OLD JSON
  SET @o_vcErrorFlag = 'S'
  SET @o_vcErrorMessage = 'Success'
  set @o_vcJsonOutput = '{}'
  DECLARE @jsonCutterOutput VARCHAR(MAX)='', @strDanSql NVARCHAR(MAX)=''
  
  declare @strtradeplustempdb VARCHAR(50)='', @strString NVARCHAR(MAX)=''
  DECLARE @JsonCutterXML XML
  
  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB'
  
  EXEC [dbo].[SP_ReKyc_GetData] @i_vcClientCode,  @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT, @o_vcJsonOutput OUTPUT, @i_vcTemplateCode, 'X'

  
  IF @o_vcErrorFlag = 'S'
  BEGIN
    SET @i_vcOldJsonString = @o_vcJsonOutput 
	SET @o_vcJsonOutput = '{}'
  END
  ELSE 
  BEGIN  
    RETURN 1
  END	
  
  DECLARE  @TBL_OutputJSON TABLE(ErrorTag VARCHAR(100), ErrorMessage VARCHAR(MAX))  
  
  	
  IF EXISTS(SELECT 1 FROM Client_ReKycMain WHERE rm_cmcd = @i_vcClientCode  
  AND rm_refno = @i_vcRefNo
  AND rm_step <= 2
  AND rm_rekyc = 'N')
  BEGIN
    DELETE from Client_ModifyAPI WHERE ca_cmcd = @i_vcClientCode
    AND ca_Tplus ='N' AND ca_Nfiller3 = @i_vcRefNo
  END
  
  IF EXISTS(select 1 from Client_ModifyAPI(NOLOCK) WHERE ca_cmcd = @i_vcClientCode 
  AND ca_Tplus ='N' AND ca_Nfiller3 = @i_vcRefNo)
  BEGIN
   SET @o_vcErrorFlag = 'E'
   SET @o_vcErrorMessage = 'Verifcation Pending'
   RETURN 1
  END
  
  BEGIN TRY
  IF OBJECT_ID('tempdb..#TBL_NewJson') IS NOT NULL
   drop table #TBL_NewJson
  
  IF OBJECT_ID('tempdb..#TBL_OldJson') IS NOT NULL
   drop table #TBL_OldJson
   
  CREATE TABLE #TBL_NewJson(SerialNo INT, ColumnName VARCHAR(50), ColumnValue VARCHAR(MAX), ValueTypeColumn INT,
  UpdateFlag VARCHAR(1), MasterTag VARCHAR(50), JsonLevel INT, MasterLevel INT)

  CREATE TABLE #TBL_OLDJson(SerialNo INT, ColumnName VARCHAR(50), ColumnValue VARCHAR(MAX), ValueTypeColumn INT,
  UpdateFlag VARCHAR(1), MasterTag VARCHAR(50), JsonLevel INT, MasterLevel INT)

    
  BEGIN TRY
    SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@i_vcNewJsonString+''' , @jsonCutterOutput OUTPUT';
    EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
    SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
    
    INSERT INTO #TBL_NewJson(SerialNo, ColumnName, ColumnValue, MasterTag, JsonLevel, MasterLevel)
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
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'NEW JSON '+ERROR_MESSAGE()
	RETURN 1
  END CATCH
  
  BEGIN TRY
    SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@i_vcOldJsonString+''' , @jsonCutterOutput OUTPUT';
    EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
    SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
    
    INSERT INTO #TBL_OldJson(SerialNo, ColumnName, ColumnValue,  MasterTag, JsonLevel, MasterLevel)
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
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'OLD JSON '+ERROR_MESSAGE()
	 RETURN 1
  END CATCH
  
  
  	IF @i_vcTemplateCode = 'Template2'
	BEGIN
	  UPDATE A SET MasterTag = B.MasterJsonTag, JsonLevel = 1, MasterLevel = 1
	  FROM #TBL_OldJson A, tbl_ReKycAuditColumnMapping_new B 
	  WHERE B.JsonKey = A.ColumnName
	  AND A.MasterTag = ''
	  and TemplateCode = @i_vcTemplateCode
	  
      UPDATE A SET JsonLevel = 2, MasterLevel = 1
      FROM #TBL_OldJson A, tbl_ReKycAuditColumnMapping_new B
      WHERE A.ColumnName = B.JsonKey
      AND B.FieldDescp LIKE  '%NomineeDetails(2)%'
	  and TemplateCode = @i_vcTemplateCode

      UPDATE A SET JsonLevel = 3, MasterLevel = 1
      FROM #TBL_OldJson A, tbl_ReKycAuditColumnMapping_new B
      WHERE A.ColumnName = B.JsonKey
      AND B.FieldDescp LIKE  '%NomineeDetails(3)%'
	  and TemplateCode = @i_vcTemplateCode

      UPDATE A SET JsonLevel = 1, MasterLevel = 2
      FROM #TBL_OldJson A, tbl_ReKycAuditColumnMapping_new B
      WHERE A.ColumnName = B.JsonKey
      AND B.FieldDescp LIKE '%GuardianDetails(2)%'
	  and TemplateCode = @i_vcTemplateCode

      UPDATE A SET JsonLevel = 1, MasterLevel = 3
      FROM #TBL_OldJson A, tbl_ReKycAuditColumnMapping_new B
      WHERE A.ColumnName = B.JsonKey
      AND B.FieldDescp LIKE '%GuardianDetails(3)%'
	  and TemplateCode = @i_vcTemplateCode
	  
	   UPDATE A SET MasterTag = B.MasterJsonTag, JsonLevel = 1, MasterLevel = 1
	  FROM #TBL_NewJson A, tbl_ReKycAuditColumnMapping_new B 
	  WHERE B.JsonKey = A.ColumnName
	  AND A.MasterTag = ''
	  and TemplateCode = @i_vcTemplateCode
	  
      UPDATE A SET JsonLevel = 2, MasterLevel = 1
      FROM #TBL_NewJson A, tbl_ReKycAuditColumnMapping_new B
      WHERE A.ColumnName = B.JsonKey
      AND B.FieldDescp LIKE  '%NomineeDetails(2)%'
	  and TemplateCode = @i_vcTemplateCode

      UPDATE A SET JsonLevel = 3, MasterLevel = 1
      FROM #TBL_NewJson A, tbl_ReKycAuditColumnMapping_new B
      WHERE A.ColumnName = B.JsonKey
      AND B.FieldDescp LIKE  '%NomineeDetails(3)%'
      and TemplateCode = @i_vcTemplateCode

      UPDATE A SET JsonLevel = 1, MasterLevel = 2
      FROM #TBL_NewJson A, tbl_ReKycAuditColumnMapping_new B
      WHERE A.ColumnName = B.JsonKey
      AND B.FieldDescp LIKE '%GuardianDetails(2)%'
	  and TemplateCode = @i_vcTemplateCode

      UPDATE A SET JsonLevel = 1, MasterLevel = 3
      FROM #TBL_NewJson A, tbl_ReKycAuditColumnMapping_new B
      WHERE A.ColumnName = B.JsonKey
      AND B.FieldDescp LIKE '%GuardianDetails(3)%'
      and TemplateCode = @i_vcTemplateCode
	  
	  UPDATE #TBL_NewJson SET ColumnValue = '0' WHERE CAST(ColumnValue AS MONEY) = 0
	  AND COLUMNNAME LIKE '%SharePercentage%'
	  
	  UPDATE #TBL_OldJson SET ColumnValue = '0' WHERE CAST(ColumnValue AS MONEY) = 0
	  AND COLUMNNAME LIKE '%SharePercentage%'
	  
	END
  
  UPDATE A SET A.ColumnValue = 'Active'
  FROM #TBL_NewJson A
  WHERE A.COLUMNNAME = 'DormantStatus'
  
  
  
  IF EXISTS(SELECT 1 FROM #TBL_NewJson X WHERE MasterTag = 'BankDetails' AND ColumnName = 'IsDefaultNew'
  AND columnvalue = 'True' and EXISTS(SELECT 1 FROM #TBL_NewJson X1
  WHERE MasterTag = 'BankDetails' and ColumnName = 'IsDefault' and JSONLEVEL = X.JSONLEVEL
  AND ColumnValue = 'True'))
  BEGIN
	UPDATE X SET X.ColumnValue = 'False' 
    FROM #TBL_NewJson X
	WHERE X.MasterTag = 'BankDetails' and ColumnName = 'IsDefault' AND ColumnValue = 'True'
	AND NOT EXISTS(SELECT 1 FROM #TBL_NewJson X1 WHERE MasterTag = 'BankDetails' AND ColumnName = 'IsDefaultNew'
	AND JSONLEVEL = X.JSONLEVEL AND  columnvalue = 'True')
  END	
  
  IF EXISTS(SELECT 1 FROM #TBL_NewJson X WHERE MasterTag = 'DematDetails' AND ColumnName = 'IsDefaultNew'
  AND columnvalue = 'True' and EXISTS(SELECT 1 FROM #TBL_NewJson X1
  WHERE MasterTag = 'DematDetails' and ColumnName = 'IsDefault' and JSONLEVEL = X.JSONLEVEL
  AND ColumnValue = 'True'))
  BEGIN
	UPDATE X SET X.ColumnValue = 'False' 
    FROM #TBL_NewJson X
	WHERE X.MasterTag = 'DematDetails' and ColumnName = 'IsDefault' AND ColumnValue = 'True'
	AND NOT EXISTS(SELECT 1 FROM #TBL_NewJson X1 WHERE MasterTag = 'DematDetails' AND ColumnName = 'IsDefaultNew'
	AND JSONLEVEL = X.JSONLEVEL AND  columnvalue = 'True')
  END	
  
  
  DECLARE @tbl_Client_ModifyAPI TABLE(ca_cmcd VARCHAR(50),
  FieldName VARCHAR(50), FieldDescp VARCHAR(100),Oldvalue VARCHAR(MAX),
  NewValue VARCHAR(MAX), ca_date VARCHAR(8), ca_time VARCHAR(8),
  ca_computername VARCHAR(50),ca_Tplus VARCHAR(1),ca_Cross VARCHAR(1), 
  ca_Estro VARCHAR(1), ca_Dematacno VARCHAR(50),ca_filler1 VARCHAR(1),
  ca_filler2 VARCHAR(1), ca_filler3 VARCHAR(1) ,ca_Nfiller1 numeric(19,0),
  ca_Nfiller2 numeric(19,0),ca_Nfiller3 numeric(19,0), ca_Mastertag VARCHAR(50))
  
  DECLARE  @dtca_date VARCHAR(8)= CONVERT(VARCHAR,GETDATE(),112), @ca_time VARCHAR(8) =CONVERT(time,GETDATE()),
  @seqNo INT =  @i_vcRefNo
  
  IF  @i_vcTemplateCode <> 'ONLYCLOSURE'
  BEGIN
    INSERT INTO @tbl_Client_ModifyAPI(ca_cmcd, FieldName, FieldDescp, Oldvalue, NewValue, ca_date, ca_time, ca_computername, ca_Tplus,
    ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_filler2, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3, ca_Mastertag)

    SELECT ca_cmcd = @i_vcClientCode,  FieldName, XMAIN.FieldDescp, X1.OldBalue, X1.NewValue, ca_date =   @dtca_date,
    ca_time = @ca_time, ca_computername = @i_vccomputername, ca_Tplus ='N', ca_Cross = 'N',
    ca_Estro = 'N', ca_Dematacno = '', ca_filler1 = 0, ca_filler2 = xmain.RequireTag, ca_filler3 = 0,ca_Nfiller1 = 0, ca_Nfiller2 = 0,
    ca_Nfiller3 = @seqNo, XMAIN.MasterJsonTag
    FROM(SELECT n.ColumnName, OldBalue = ISNULL(o.ColumnValue,''), 
	NewValue = ISNULL((CASE WHEN N.ColumnName ='AccountType' 
	THEN CASE WHEN n.ColumnValue  LIKE 'SAVING%' THEN 'SB' ELSE n.ColumnValue 
	END ELSE n.ColumnValue END),'')
    FROM #TBL_NewJson n LEFT OUTER JOIN #TBL_OldJson o
    ON(n.MasterTag = o.MasterTag anD n.ColumnName=o.ColumnName)) x1 , tbl_ReKycAuditColumnMapping_NEW(NOLOCK) xmain
    WHERE X1.ColumnName = XMAIN.JsonKey and xmain.MasterJsonTag not in('GuardianDetails','ContactDetails','SegmentDetails')
    AND XMAIN.RecordType ='S' AND DefaultValueTag IN('B','U')
    AND OldBalue <> NewValue
    and TemplateCode = @i_vcTemplateCode
  END
  ELSE IF  @i_vcTemplateCode = 'ONLYCLOSURE'
  BEGIN
    
    IF EXISTS(SELECT 1 from #TBL_OLDJson where ColumnName = 'TradingLedgerBalance' AND CAST(ISNULL(ColumnValue,'0') AS MONEY) <> 0)
	AND EXISTS(SELECT 1 from #TBL_NEWJson where ColumnName = 'ClosureType' AND ISNULL(ColumnValue,'D') IN('T','A'))
	BEGIN
	  SET @o_vcErrorFlag = 'E'
      SET @o_vcErrorMessage = 'Trading Ledger Balance Must be Zero'
	  RETURN 1;
    END
	
	IF EXISTS(SELECT 1 from #TBL_OLDJson where ColumnName = 'DPLedgerBalance' AND CAST(ISNULL(ColumnValue,'0') AS MONEY) <> 0)
	AND EXISTS(SELECT 1 from #TBL_NEWJson where ColumnName = 'ClosureType' AND ISNULL(ColumnValue,'D') IN('D','A'))
	BEGIN
	  SET @o_vcErrorFlag = 'E'
      SET @o_vcErrorMessage = 'DP Ledger Balance Must be Zero'
	  RETURN 1;
    END
	
    INSERT INTO @tbl_Client_ModifyAPI(ca_cmcd, FieldName, FieldDescp, Oldvalue, NewValue, ca_date, ca_time, ca_computername, ca_Tplus,
    ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_filler2, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3, ca_Mastertag)

    SELECT ca_cmcd = @i_vcClientCode,  FieldName, XMAIN.FieldDescp, X1.OldBalue, X1.NewValue, ca_date =   @dtca_date,
    ca_time = @ca_time, ca_computername = @i_vccomputername, ca_Tplus ='N', ca_Cross = 'N',
    ca_Estro = 'N', ca_Dematacno = '', ca_filler1 = 0, ca_filler2 = xmain.RequireTag, ca_filler3 = 0,ca_Nfiller1 = 0, ca_Nfiller2 = 0,
    ca_Nfiller3 = @seqNo, XMAIN.MasterJsonTag
    FROM(SELECT n.ColumnName, OldBalue = ISNULL(o.ColumnValue,''), 
	NewValue = n.ColumnValue 
    FROM #TBL_NewJson n LEFT OUTER JOIN #TBL_OldJson o
    ON(n.MasterTag = o.MasterTag anD n.ColumnName=o.ColumnName)) x1 , tbl_ReKycAuditColumnMapping_NEW(NOLOCK) xmain
    WHERE X1.ColumnName = XMAIN.JsonKey and xmain.MasterJsonTag not in('GuardianDetails','ContactDetails','SegmentDetails')
    AND XMAIN.RecordType ='S' AND DefaultValueTag IN('B','U')
    and TemplateCode = @i_vcTemplateCode
	
	INSERT INTO @tbl_Client_ModifyAPI(ca_cmcd, FieldName, FieldDescp, Oldvalue, NewValue, ca_date, ca_time, ca_computername, ca_Tplus,
    ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_filler2, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3, ca_Mastertag)

    SELECT ca_cmcd = @i_vcClientCode,  FieldName, XMAIN.FieldDescp, '', REPLACE(XMAIN.DefaultValue,'CURRDATE',CONVERT(VARCHAR,GETDATE(),112)), 
	ca_date =   @dtca_date,
    ca_time = @ca_time, ca_computername = @i_vccomputername, ca_Tplus ='N', ca_Cross = 'N',
    ca_Estro = 'N', ca_Dematacno = '', ca_filler1 = 0, ca_filler2 = xmain.RequireTag, ca_filler3 = 0,ca_Nfiller1 = 0, ca_Nfiller2 = 0,
    ca_Nfiller3 = @seqNo, XMAIN.MasterJsonTag
    FROM tbl_ReKycAuditColumnMapping_NEW(NOLOCK) xmain
    WHERE TemplateCode = @i_vcTemplateCode
	AND JsonKey = ''
    AND DefaultValue <> ''
	
	IF NOT EXISTS (SELECT * FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @i_vcClientCode AND ma_refno = @seqNo 
	AND ma_filename='CMRAttachment')
	Begin			
	  INSERT INTO Client_ModifyAttach(ma_cmcd,ma_date,ma_filename,ma_field,mkrdt,mkrtm,ma_proof,ma_refno,ma_Nfiller1,ma_status)
	  Select ma_cmcd = @i_vcClientCode, ma_date = convert(varchar(8),cast(getdate() as date),112), 
	  ma_filename = 'CMRAttachment', ma_field = 'CMRAccountClosureAttachment', mkrdt = convert(varchar(8),cast(getdate() as date),112), 
	  mkrtm = convert(varchar, getdate(), 108), ma_proof = CAST(ColumnValue AS xml).value('xs:base64Binary(.)', 'varbinary(max)'), 
	  ma_refno = @seqNo, ma_Nfiller1 = 1, ma_status = 'N'
	  From #TBL_NewJson  Where ColumnName = 'CMRAttachment' and Isnull(ColumnValue,'') <> ''
	END
  END   
  

  INSERT INTO @tbl_Client_ModifyAPI(ca_cmcd, FieldName, FieldDescp, Oldvalue, NewValue, ca_date, ca_time, ca_computername, ca_Tplus,
  ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_filler2, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3, ca_Mastertag)
  SELECT ca_cmcd = @i_vcClientCode,  FieldName, XMAIN.FieldDescp, X1.OldBalue, X1.NewValue, ca_date =   @dtca_date,
  ca_time = @ca_time, ca_computername = @i_vccomputername, ca_Tplus ='N', ca_Cross = 'N',
  ca_Estro = 'N', ca_Dematacno = '', ca_filler1 = 0, ca_filler2 = 0, ca_filler3 = 0,ca_Nfiller1 = 0, ca_Nfiller2 = 0,
  ca_Nfiller3 = @seqNo, XMAIN.MasterJsonTag
  FROM(SELECT n.ColumnName, OldBalue = ISNULL(o.ColumnValue,''), 
  NewValue = ISNULL((CASE WHEN N.ColumnName ='AccountType' 
  THEN CASE WHEN n.ColumnValue   LIKE 'SAVING%' THEN 'SB' ELSE n.ColumnValue 
  END ELSE n.ColumnValue END),'')
  FROM #TBL_NewJson n LEFT OUTER JOIN #TBL_OldJson o
  ON(n.MasterTag = o.MasterTag anD n.ColumnName=o.ColumnName)) x1 , tbl_ReKycAuditColumnMapping_NEW(NOLOCK) xmain
  WHERE X1.ColumnName = XMAIN.JsonKey
  AND XMAIN.RecordType ='S'  AND DefaultValueTag IN('B','U')
  AND EXISTS(SELECT 1 FROM @tbl_Client_ModifyAPI WHERE ca_cmcd = @i_vcClientCode AND ca_Nfiller3 = @seqNo
  AND ca_filler2  = xmain.RequireTag AND xmain.RequireTag <> '' AND ca_Mastertag = XMAIN.MasterJsonTag)
  AND NOT EXISTS(SELECT 1 FROM @tbl_Client_ModifyAPI WHERE ca_cmcd = @i_vcClientCode AND ca_Nfiller3 = @seqNo
  AND FieldName = XMAIN.FIELDNAME  AND ca_Mastertag = XMAIN.MasterJsonTag) 
  and xmain.MasterJsonTag not in('GuardianDetails','ContactDetails','SegmentDetails')
  and TemplateCode = @i_vcTemplateCode
  
 
  
  DECLARE @isNomineeModified VARCHAR(20)=''
  SELECT @isNomineeModified = ColumnValue FROM #TBL_NewJson where columnname = 'isNomineeModified'    
  
  if isnull(@isNomineeModified,'') = ''
  BEGIN
    SELECT @isNomineeModified = ColumnValue FROM #TBL_NewJson where MasterTag IN('NomineeDetails','GuardianDetails') 
	AND ColumnName = 'IsInserted' AND ColumnValue = 'true'  
	
	IF isnull(@isNomineeModified,'') = ''
	BEGIN
	  SELECT @isNomineeModified = ColumnValue FROM #TBL_NewJson where MasterTag IN('NomineeDetails','GuardianDetails') 
	  AND ColumnName = 'IsModified' AND ColumnValue = 'true'  
	END
  END
  

  INSERT INTO @tbl_Client_ModifyAPI(ca_cmcd, FieldName, FieldDescp, Oldvalue, NewValue, ca_date, ca_time, ca_computername, ca_Tplus,
  ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_filler2, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3, ca_Mastertag)

  SELECT ca_cmcd = @i_vcClientCode,  FieldName, XMAIN.FieldDescp, X1.OldBalue, X1.NewValue, ca_date =   @dtca_date,
  ca_time = @ca_time, ca_computername = @i_vccomputername, ca_Tplus ='N', ca_Cross = 'N',
  ca_Estro = 'N', ca_Dematacno = '', ca_filler1 = 0, ca_filler2 = xmain.RequireTag, ca_filler3 = IIF(XMAIN.MasterJsonTag ='NomineeDetails',(case when JSONLEVEL = 1 then 1
  when JSONLEVEL = 2 then 3 when JSONLEVEL = 3 then 5 else JSONLEVEL end),0),
  ca_Nfiller1 = JSONLEVEL, ca_Nfiller2 = 0, ca_Nfiller3 = @seqNo, xmain.MasterJsonTag
  FROM(SELECT N.Mastertag, n.ColumnName, OldBalue = ISNULL(o.ColumnValue,''), 
  NewValue = ISNULL((CASE WHEN N.ColumnName ='AccountType' 
  THEN CASE WHEN n.ColumnValue  LIKE 'SAVING%' THEN 'SB' ELSE n.ColumnValue END ELSE n.ColumnValue END),''), 
  JSONLEVEL = N.JSONLEVEL 
  FROM #TBL_NewJson n LEFT OUTER JOIN #TBL_OldJson o
  ON(n.MasterTag = o.MasterTag anD n.ColumnName=o.ColumnName and N.JsonLevel = O.JsonLevel)) x1 , 
  tbl_ReKycAuditColumnMapping_NEW(NOLOCK) xmain
  WHERE X1.ColumnName = XMAIN.JsonKey
  and mastertag = xmain.MasterJsonTag  AND DefaultValueTag IN('B','U')
  AND XMAIN.RecordType ='M' and xmain.MasterJsonTag not in('GuardianDetails','ContactDetails','SegmentDetails', 'NomineeDetails')
  AND OldBalue <> NewValue 
  and TemplateCode = @i_vcTemplateCode

  INSERT INTO @tbl_Client_ModifyAPI(ca_cmcd, FieldName, FieldDescp, Oldvalue, NewValue, ca_date, ca_time, ca_computername, ca_Tplus,
  ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_filler2, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3, ca_Mastertag)
  SELECT ca_cmcd = @i_vcClientCode,  FieldName, XMAIN.FieldDescp, X1.OldBalue, X1.NewValue, ca_date =   @dtca_date,
  ca_time = @ca_time, ca_computername = @i_vccomputername, ca_Tplus ='N', ca_Cross = 'N',
  ca_Estro = 'N', ca_Dematacno = '', ca_filler1 = 0, ca_filler2 = xmain.RequireTag, 
  ca_filler3 = IIF(XMAIN.MasterJsonTag ='NomineeDetails',(case when JSONLEVEL = 1 then 1
  when JSONLEVEL = 2 then 3 when JSONLEVEL = 3 then 5 else JSONLEVEL end),0),
  ca_Nfiller1 = JSONLEVEL, ca_Nfiller2 = 0, ca_Nfiller3 = @seqNo, xmain.MasterJsonTag
  FROM(SELECT N.Mastertag, n.ColumnName, OldBalue = ISNULL(o.ColumnValue,''), 
  NewValue = ISNULL((CASE WHEN N.ColumnName ='AccountType' 
	THEN CASE WHEN n.ColumnValue  LIKE 'SAVING%' THEN 'SB' ELSE n.ColumnValue 
	END ELSE n.ColumnValue END),''), 
  JSONLEVEL = N.JSONLEVEL 
  FROM #TBL_NewJson n LEFT OUTER JOIN #TBL_OldJson o
  ON(n.MasterTag = o.MasterTag anD n.ColumnName=o.ColumnName and N.JsonLevel = O.JsonLevel )) x1 , 
  tbl_ReKycAuditColumnMapping_NEW(NOLOCK) xmain
  WHERE X1.ColumnName = XMAIN.JsonKey
  and mastertag = xmain.MasterJsonTag  AND DefaultValueTag IN('B','U')
  AND XMAIN.RecordType ='M' and xmain.MasterJsonTag not in('GuardianDetails','ContactDetails','SegmentDetails','NomineeDetails')
  AND EXISTS(SELECT 1 FROM @tbl_Client_ModifyAPI WHERE ca_cmcd = @i_vcClientCode AND ca_Nfiller3 = @seqNo
  AND ca_filler2  = xmain.RequireTag AND xmain.RequireTag <> '' AND ca_Nfiller1 = JSONLEVEL  AND ca_Mastertag = XMAIN.MasterJsonTag)
  AND NOT EXISTS(SELECT 1 FROM @tbl_Client_ModifyAPI WHERE ca_cmcd = @i_vcClientCode AND ca_Nfiller3 = @seqNo
  AND FieldName = XMAIN.FIELDNAME AND ca_Nfiller1 = JSONLEVEL  AND ca_Mastertag = XMAIN.MasterJsonTag)
  and TemplateCode = @i_vcTemplateCode
  
 
  
  IF @isNomineeModified = 'true' AND @i_vcTemplateCode = 'Template1'
  BEGIN
   INSERT INTO @tbl_Client_ModifyAPI(ca_cmcd, FieldName, FieldDescp, Oldvalue, NewValue, ca_date, ca_time, ca_computername, ca_Tplus,
   ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_filler2, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3, ca_Mastertag)

   SELECT ca_cmcd = @i_vcClientCode,  FieldName, XMAIN.FieldDescp, X1.OldBalue, X1.NewValue, ca_date =   @dtca_date,
   ca_time = @ca_time, ca_computername = @i_vccomputername, ca_Tplus ='N', ca_Cross = 'N',
   ca_Estro = 'N', ca_Dematacno = '', ca_filler1 = 0, ca_filler2 = xmain.RequireTag, 
   ca_filler3 = IIF(XMAIN.MasterJsonTag ='NomineeDetails',(case when JSONLEVEL = 1 then 1
   when JSONLEVEL = 2 then 3 when JSONLEVEL = 3 then 5 else JSONLEVEL end),0),
   ca_Nfiller1 = JSONLEVEL, ca_Nfiller2 = 0, ca_Nfiller3 = @seqNo, xmain.MasterJsonTag
   FROM(SELECT N.Mastertag, n.ColumnName, OldBalue = CASE WHEN ISNULL(N.ColumnName,'') = 'NomineeResidualSecurities' 
   THEN CASE WHEN ISNULL(o.ColumnValue,'') = 'true' THEN 'Y' ELSE 'N' End ELSE  ISNULL(o.ColumnValue,'')
   END, NewValue = CASE WHEN ISNULL(N.ColumnName,'') = 'NomineeResidualSecurities' 
   THEN CASE WHEN ISNULL(n.ColumnValue,'') = 'true' THEN 'Y' ELSE 'N' End ELSE  ISNULL(n.ColumnValue,'')
   END, JSONLEVEL = N.JSONLEVEL 
   FROM #TBL_NewJson n LEFT OUTER JOIN #TBL_OldJson o
   ON(n.MasterTag = o.MasterTag anD n.ColumnName=o.ColumnName and N.JsonLevel = O.JsonLevel)) x1 , 
   tbl_ReKycAuditColumnMapping_NEW(NOLOCK) xmain
   WHERE X1.ColumnName = XMAIN.JsonKey
   and mastertag = xmain.MasterJsonTag  AND DefaultValueTag IN('B','U')
   AND XMAIN.RecordType ='M' and xmain.MasterJsonTag = 'NomineeDetails'
   and TemplateCode = @i_vcTemplateCode

  
   INSERT INTO @tbl_Client_ModifyAPI(ca_cmcd, FieldName, FieldDescp, Oldvalue, NewValue, ca_date, ca_time, ca_computername, ca_Tplus,
   ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_filler2, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3, ca_Mastertag)

   SELECT ca_cmcd = @i_vcClientCode,  FieldName, XMAIN.FieldDescp, X1.OldBalue, X1.NewValue, ca_date =   @dtca_date,
   ca_time = @ca_time, ca_computername = @i_vccomputername, ca_Tplus ='N', ca_Cross = 'N',
   ca_Estro = 'N', ca_Dematacno = '', ca_filler1 = 0, ca_filler2 = xmain.RequireTag, ca_filler3 = (case when JSONLEVEL = 1 then 2
   when JSONLEVEL = 2 then 4 when JSONLEVEL = 3 then 6 else JSONLEVEL end),
   ca_Nfiller1 = JSONLEVEL , ca_Nfiller2 = 0, ca_Nfiller3 = @seqNo, xmain.MasterJsonTag 
   FROM(SELECT N.Mastertag, n.ColumnName, OldBalue = ISNULL(o.ColumnValue,''), 
   NewValue = n.ColumnValue, JSONLEVEL = N.masterlevel 
   FROM #TBL_NewJson n LEFT OUTER JOIN #TBL_OldJson o
   ON(n.MasterTag = o.MasterTag anD n.ColumnName = o.ColumnName and N.JsonLevel = O.JsonLevel and n.MasterLevel = O.MasterLevel)) x1 , 
   tbl_ReKycAuditColumnMapping_NEW(NOLOCK) xmain
   WHERE X1.ColumnName = XMAIN.JsonKey
   and mastertag = xmain.MasterJsonTag  AND DefaultValueTag IN('B','U')
   AND XMAIN.RecordType ='M' and xmain.MasterJsonTag = 'GuardianDetails'
   and TemplateCode = @i_vcTemplateCode
   
  END   

  DECLARE @svar INT = 0
   BEGIN TRY
  SELECT @svar = COUNT(*)
   FROM(SELECT N.Mastertag, n.ColumnName, OldBalue = ISNULL(o.ColumnValue,''), NewValue = n.ColumnValue, JSONLEVEL = N.JSONLEVEL 
   FROM #TBL_NewJson n LEFT OUTER JOIN #TBL_OldJson o
   ON(n.MasterTag = o.MasterTag anD n.ColumnName=o.ColumnName and N.JsonLevel = O.JsonLevel)) x1 , 
   tbl_ReKycAuditColumnMapping_NEW(NOLOCK) xmain
   WHERE X1.ColumnName = XMAIN.JsonKey
   and mastertag = xmain.MasterJsonTag  AND DefaultValueTag IN('B','U')
   AND XMAIN.RecordType ='M' and xmain.MasterJsonTag = 'NomineeDetails'
   AND XMAIN.JsonKey <> 'NomineeResidualSecurities'
   AND (ISNUMERIC(NewValue) = 0 AND ISNUMERIC(OldBalue) = 0  AND OldBalue <> NewValue) 
   and TemplateCode = @i_vcTemplateCode
      END TRY
   BEGIN CATCH
     SET @svar = 0
   END CATCH  

   IF @svar = 0
   BEGIN
     SELECT @svar = ISNULL(COUNT(*),0)   
     FROM(SELECT N.Mastertag, n.ColumnName, OldBalue = CAST(ISNULL(o.ColumnValue,'0') AS MONEY), 
     NewValue = CAST(ISNULL(N.ColumnValue,'0') AS MONEY), JSONLEVEL = N.JSONLEVEL 
     FROM #TBL_NewJson n LEFT OUTER JOIN #TBL_OldJson o
     ON(n.MasterTag = o.MasterTag anD n.ColumnName=o.ColumnName and N.JsonLevel = O.JsonLevel)) x1 , 
     tbl_ReKycAuditColumnMapping_NEW(NOLOCK) xmain
     WHERE X1.ColumnName = XMAIN.JsonKey
     and mastertag = xmain.MasterJsonTag  AND DefaultValueTag IN('B','U')
     AND XMAIN.RecordType ='M' and xmain.MasterJsonTag = 'NomineeDetails'
     AND xmain.FieldName = 'cn_NomPershare'  AND OldBalue <> NewValue
     AND TemplateCode = @i_vcTemplateCode
  END	 
  
  IF @i_vcTemplateCode = 'Template2'
  BEGIN
   IF @svar > 0
   BEGIN
     INSERT INTO @tbl_Client_ModifyAPI(ca_cmcd, FieldName, FieldDescp, Oldvalue, NewValue, ca_date, ca_time, ca_computername, ca_Tplus,
     ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_filler2, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3, ca_Mastertag)
     SELECT ca_cmcd = @i_vcClientCode,  FieldName, XMAIN.FieldDescp, X1.OldBalue, X1.NewValue, ca_date =   @dtca_date,
     ca_time = @ca_time, ca_computername = @i_vccomputername, ca_Tplus ='N', ca_Cross = 'N',
     ca_Estro = 'N', ca_Dematacno = '', ca_filler1 = 0, ca_filler2 = xmain.RequireTag, 
     ca_filler3 = IIF(XMAIN.MasterJsonTag ='NomineeDetails',(case when JSONLEVEL = 1 then 1
     when JSONLEVEL = 2 then 3 when JSONLEVEL = 3 then 5 else JSONLEVEL end),0),
     ca_Nfiller1 = JSONLEVEL, ca_Nfiller2 = 0, ca_Nfiller3 = @seqNo, xmain.MasterJsonTag
     FROM(SELECT N.Mastertag, n.ColumnName, OldBalue = ISNULL(o.ColumnValue,''), NewValue = n.ColumnValue, JSONLEVEL = N.JSONLEVEL 
     FROM #TBL_NewJson n LEFT OUTER JOIN #TBL_OldJson o
     ON(n.MasterTag = o.MasterTag anD n.ColumnName=o.ColumnName and N.JsonLevel = O.JsonLevel)
	 WHERE EXISTS(SELECT 1 FROM #TBL_NewJson
	 WHERE MasterTag = N.MasterTag AND JSONLEVEL = N.JSONLEVEL 
	 AND ColumnName like '%FirstName%' and  ColumnValue <> '')) x1 , 
     tbl_ReKycAuditColumnMapping_NEW(NOLOCK) xmain
     WHERE X1.ColumnName = XMAIN.JsonKey
     and mastertag = xmain.MasterJsonTag  AND DefaultValueTag IN('B','U')
     AND XMAIN.RecordType ='M' and xmain.MasterJsonTag = 'NomineeDetails'
     and TemplateCode = @i_vcTemplateCode
	 AND FieldName <> '' 

  
     INSERT INTO @tbl_Client_ModifyAPI(ca_cmcd, FieldName, FieldDescp, Oldvalue, NewValue, ca_date, ca_time, ca_computername, ca_Tplus,
     ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_filler2, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3, ca_Mastertag)

     SELECT ca_cmcd = @i_vcClientCode,  FieldName, XMAIN.FieldDescp, X1.OldBalue, X1.NewValue, ca_date =   @dtca_date,
     ca_time = @ca_time, ca_computername = @i_vccomputername, ca_Tplus ='N', ca_Cross = 'N',
     ca_Estro = 'N', ca_Dematacno = '', ca_filler1 = 0, ca_filler2 = xmain.RequireTag, ca_filler3 = (case when JSONLEVEL = 1 then 2
     when JSONLEVEL = 2 then 4 when JSONLEVEL = 3 then 6 else JSONLEVEL end),
     ca_Nfiller1 = JSONLEVEL , ca_Nfiller2 = 0, ca_Nfiller3 = @seqNo, xmain.MasterJsonTag 
     FROM(SELECT N.Mastertag, n.ColumnName, OldBalue = ISNULL(o.ColumnValue,''), 
     NewValue = n.ColumnValue, JSONLEVEL = N.masterlevel 
     FROM #TBL_NewJson n LEFT OUTER JOIN #TBL_OldJson o
     ON(n.MasterTag = o.MasterTag anD n.ColumnName = o.ColumnName and N.JsonLevel = O.JsonLevel and n.MasterLevel = O.MasterLevel)
	 WHERE EXISTS(SELECT 1 FROM #TBL_NewJson
	 WHERE MasterTag = N.MasterTag AND masterlevel = N.masterlevel 
	 AND ColumnName like '%FirstName%' and  ColumnValue <> '')) x1 , 
     tbl_ReKycAuditColumnMapping_NEW(NOLOCK) xmain
     WHERE X1.ColumnName = XMAIN.JsonKey
     and mastertag = xmain.MasterJsonTag  AND DefaultValueTag IN('B','U')
     AND XMAIN.RecordType ='M' and xmain.MasterJsonTag = 'GuardianDetails'
     and TemplateCode = @i_vcTemplateCode
	 AND FieldName <> ''  
	 
    END
  END
  
 
   --- CONTACT Details
  
   INSERT INTO @tbl_Client_ModifyAPI(ca_cmcd, FieldName, FieldDescp, Oldvalue, NewValue, ca_date, ca_time, ca_computername, ca_Tplus,
   ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_filler2, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3, ca_Mastertag)
   
   SELECT  ca_cmcd = @i_vcClientCode,  FieldName, XMAIN.FieldDescp, X1.OldBalue, X1.NewValue, ca_date =   @dtca_date,
   ca_time = @ca_time, ca_computername = @i_vccomputername, ca_Tplus ='N', ca_Cross = 'N',
   ca_Estro = 'N', ca_Dematacno = '', ca_filler1 = 0, ca_filler2 = 0, ca_filler3 = 0,ca_Nfiller1 = JsonLevel, ca_Nfiller2 = 0,
   ca_Nfiller3 = @seqNo, XMAIN.MasterJsonTag from(
   SELECT  N.SerialNo,n.ColumnName, OldBalue = CASE WHEN ISNULL(o.ColumnValue,'')='0' THEN 'Self'
   WHEN ISNULL(o.ColumnValue,'')='1' then 'Spouse'
   WHEN ISNULL(o.ColumnValue,'')='2' then 'Dependent Children'
   WHEN ISNULL(o.ColumnValue,'')='3' then 'Dependent Parent' else  ISNULL(o.ColumnValue,'') end  , 
   NewValue = CASE WHEN ISNULL(n.ColumnValue,'')='0' THEN 'Self'
   WHEN ISNULL(n.ColumnValue,'')='1' then 'Spouse'
   WHEN ISNULL(n.ColumnValue,'')='2' then 'Dependent Children'
   WHEN ISNULL(n.ColumnValue,'')='3' then 'Dependent Parent' else  ISNULL(n.ColumnValue,'') end, N.JsonLevel FROM #TBL_NewJson n LEFT OUTER JOIN #TBL_OldJson o
   ON(n.MasterTag = o.MasterTag 
   and n.ColumnName=o.ColumnName )
   ) x1, tbl_ReKycAuditColumnMapping_NEW(NOLOCK) xmain
   WHERE   XMAIN.JsonKey = X1.ColumnName 
   AND XMAIN.MasterJsonTag IN('ContactDetails')
   and isnull(X1.NewValue,'') <> ''  AND DefaultValueTag IN('B','U')
   AND X1.OldBalue <> X1.NewValue
   and TemplateCode = @i_vcTemplateCode
   
   DECLARE @strSegmentNew VARCHAR(MAX)='', @strSegmentold VARCHAR(MAX)=''
    /*
  DECLARE @strSegmentNew VARCHAR(MAX)='', @strSegmentold VARCHAR(MAX)=''
  SELECT @strSegmentNew = @strSegmentNew+','+replace(isnull(REPLACE(REPLACE(COLUMNVALUE,'[',''),']',''),''),' ','') 
  FROM #TBL_NewJson WHERE MasterTag ='SegmentDetails' AND VALUETYPECOLUMN = 4
  and isnull(REPLACE(REPLACE(COLUMNVALUE,'[',''),']',''),'') <> ''


  SELECT @strSegmentold = @strSegmentold+','+replace(isnull(REPLACE(REPLACE(COLUMNVALUE,'[',''),']',''),''),' ','') 
  FROM #TBL_oldJson WHERE MasterTag ='SegmentDetails' AND VALUETYPECOLUMN = 4
  and isnull(REPLACE(REPLACE(COLUMNVALUE,'[',''),']',''),'') <> ''
  */
 
  
  /*
  SELECT @strSegmentNew = @strSegmentNew+','+replace(isnull(REPLACE(REPLACE(COLUMNVALUE,'[',''),']',''),''),' ','')
  FROM #TBL_NewJson WHERE MasterTag ='SegmentDetails' AND ColumnName IN('SegmentValue','ce_companycode')
  and isnull(REPLACE(REPLACE(COLUMNVALUE,'[',''),']',''),'') <> ''
  SELECT @strSegmentold = @strSegmentold+','+replace(isnull(REPLACE(REPLACE(COLUMNVALUE,'[',''),']',''),''),' ','')
  FROM #TBL_oldJson WHERE MasterTag ='SegmentDetails' AND ColumnName IN('SegmentValue','ce_companycode')
  and isnull(REPLACE(REPLACE(COLUMNVALUE,'[',''),']',''),'') <> ''
  */
  
 
   SELECT @strSegmentNew = @strSegmentNew +','+    replace(isnull(REPLACE(REPLACE(Exch,'[',''),']',''),''),' ','')
	From (
	SELECT  JsonLevel,MAX(CASE WHEN columnName = 'SegmentValue' THEN  COLUMNVALUE  Else ''  END) AS Exch,
		MAX(CASE WHEN columnName = 'IsSelect' THEN ColumnValue   END) AS SelectSeg
	FROM #TBL_NewJson where MasterTag ='SegmentDetails' AND ColumnName IN('SegmentValue','IsSelect') 
	GROUP BY JsonLevel
	) A Where SelectSeg = 'true'


	SELECT @strSegmentold = @strSegmentold +','+    replace(isnull(REPLACE(REPLACE(Exch,'[',''),']',''),''),' ','')
	From (
	SELECT  JsonLevel,MAX(CASE WHEN columnName = 'SegmentValue' THEN  COLUMNVALUE  Else ''  END) AS Exch,
		MAX(CASE WHEN columnName = 'IsSelect' THEN ColumnValue   END) AS SelectSeg
	FROM #TBL_oldJson where MasterTag ='SegmentDetails' AND ColumnName IN('SegmentValue','IsSelect') 
	GROUP BY JsonLevel
	) A Where SelectSeg = 'true'
  
  
  INSERT INTO @tbl_Client_ModifyAPI(ca_cmcd, FieldName, FieldDescp, Oldvalue, NewValue, ca_date, ca_time, ca_computername, ca_Tplus,
  ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_filler2, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3, ca_Mastertag)
  
  SELECT  ca_cmcd = @i_vcClientCode,  FieldName, XMAIN.FieldDescp, X1.OldBalue, X1.NewValue, ca_date =   @dtca_date,
  ca_time = @ca_time, ca_computername = @i_vccomputername, ca_Tplus ='N', ca_Cross = 'N',
  ca_Estro = 'N', ca_Dematacno = '', ca_filler1 = 0, ca_filler2 = 0, ca_filler3 = 0,ca_Nfiller1 = JsonLevel, ca_Nfiller2 = 0,
  ca_Nfiller3 = @seqNo, XMAIN.MasterJsonTag  from(
  SELECT   OldBalue = REPLACE(REPLACE(@strSegmentold,'"',''),'  ,  ',','), NewValue = REPLACE(REPLACE(@strSegmentnew,'"',''),'  ,  ',','), JsonLevel = 0  
   ) x1, tbl_ReKycAuditColumnMapping_NEW(NOLOCK) xmain
   WHERE   XMAIN.FieldName = 'ce_companycode'   AND DefaultValueTag IN('B','U')
   AND XMAIN.MasterJsonTag IN('SegmentDetails')
   --AND X1.OldBalue <>  X1.NewValue
   and TemplateCode = @i_vcTemplateCode
   
   BEGIN TRY
     DELETE FROM Client_ModifyAPI WHERE ca_cmcd = @i_vcClientCode AND ca_Nfiller3 = @seqNo
     INSERT INTO Client_ModifyAPI(ca_cmcd, ca_field, ca_desc, ca_oldValue, ca_newValue, ca_date, ca_time, ca_computername, ca_Tplus,
     ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_filler2, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3)
     
     SELECT ca_cmcd, FieldName, FieldDescp, Oldvalue = ISNULL((CASE WHEN Oldvalue = 'false' then 'N'
     WHEN Oldvalue = 'TRUE' then 'Y' ELSE Oldvalue END),''), NewValue = ISNULL((CASE WHEN NewValue = 'false' then 'N'
     WHEN NewValue = 'TRUE' then 'Y' ELSE NewValue END),''), ca_date, ca_time, ca_computername, ca_Tplus,
     ca_Cross, ca_Estro, ca_Dematacno, ca_filler1 = ca_Mastertag, ca_filler2 =0, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3 
     FROM @tbl_Client_ModifyAPI ORDER BY ca_Nfiller1,ca_filler2, FieldDescp, FieldName
	 	 
   END TRY
   BEGIN CATCH
     SET @o_vcErrorFlag = 'E'
     SET @o_vcErrorMessage = 'MAIN '+ERROR_MESSAGE()
     DELETE FROM Client_ModifyAPI WHERE ca_cmcd = @i_vcClientCode AND ca_Nfiller3 = @seqNo
     RETURN 1
   END CATCH

  DECLARE @strOldValue VARCHAR(100), @strNewValue VARCHAR(100), @CommexDB VARCHAR(100) ='',
  @string VARCHAR(MAX)=''
  DECLARE @tbl_SegmentAdd TABLE(SegmentAction VARCHAR(50), Segment VARCHAR(20), SegmentName VARCHAR(100))
  
 
  select @strOldValue =  Oldvalue, @strNewValue = newValue 
  from @tbl_Client_ModifyAPI where ca_cmcd = @i_vcClientCode AND ca_Nfiller3 = @seqNo AND FieldName LIKE 'ce_companycode'
 
  SELECT @CommexDB = ltrim(rtrim((OP_DataBase))) FROM Other_Products(NOLOCK) WHERE OP_Product = 'Commex' AND ISNULL(OP_Status,'')='A'
  
 IF @strOldValue <> '' OR @strNewValue <> ''
  BEGIN
    SELECT @string = 'SELECT ''Deleted'' as tag, [DeletedSegment] = SEGMENT, [DeletedExhange] = LTRIM(RTRIM(CES_Exchange))+''/'' + LTRIM(RTRIM(REPLACE(CES_Segment,''&'',''''))) FROM ( '
    +' SELECT VALUE AS Segment FROM DBO.Returntable('''+@strOldValue+''','','')) OLD1  , (SELECT * FROM( '
    +' select CES_Cd,CES_Name,CES_CompanyCd,CES_Exchange,CES_Segment from CompanyExchangeSegments(NOLOCK) '
    +' UNION ALL '
    +' select TOP 1 CES_Cd = ''BNS'' ,CES_Name,CES_CompanyCd,CES_Exchange = ''SLBM'', CES_Segment =''CASH'' '
    +' 	from CompanyExchangeSegments(NOLOCK)) X1) SEG  '
    +' WHERE NOT EXISTS(SELECT 1 FROM( '
    +' SELECT VALUE AS Segment FROM DBO.Returntable('''+@strNewValue+''','','')) X11 WHERE X11.Segment = OLD1.Segment)  '
    +' AND Segment = SEG.CES_cD '
  IF Isnull(@CommexDB,'')<>''
   Begin
	   SELECT @string = @string+'   '
		+' UNION ALL '
		+' SELECT ''Deleted'' as tag,  [DeletedSegment] = SEGMENT, [DeletedExhange] = LTRIM(RTRIM(CES_Exchange))+''/'' + LTRIM(RTRIM(REPLACE(CES_Segment,''&'',''''))) FROM ( '
		+' SELECT VALUE AS Segment FROM DBO.Returntable('''+@strOldValue+''','','')) OLD1  , '+@CommexDB+'.DBO.CompanyExchangeSegments SEG (NOLOCK) '
		+' WHERE NOT EXISTS(SELECT 1 FROM( '
		+  ' SELECT VALUE AS Segment FROM DBO.Returntable('''+@strNewValue+''','','')) X11 WHERE X11.Segment = OLD1.Segment) '
		+' AND Segment = SEG.CES_cD '
   End
    
    INSERT INTO @tbl_SegmentAdd(SegmentAction, Segment, SegmentNaMe)
    EXEC(@string)

    SELECT @string = 'SELECT ''Added'' as tag, [DeletedSegment] = SEGMENT, [DeletedExhange] = LTRIM(RTRIM(CES_Exchange))+''/'' + LTRIM(RTRIM(REPLACE(CES_Segment,''&'','''')))  FROM ( '
    +' SELECT VALUE AS Segment FROM DBO.Returntable('''+@strNewValue+''','','')) OLD1  , (SELECT * FROM( '
    +' select CES_Cd,CES_Name,CES_CompanyCd,CES_Exchange,CES_Segment from CompanyExchangeSegments(NOLOCK) '
    +' UNION ALL '
    +' select TOP 1 CES_Cd = ''BNS'' ,CES_Name,CES_CompanyCd,CES_Exchange = ''SLBM'', CES_Segment =''CASH'' '
    +' 	from CompanyExchangeSegments(NOLOCK)) X1) SEG  '
    +' WHERE NOT EXISTS(SELECT 1 FROM( '
    +' SELECT VALUE AS Segment FROM DBO.Returntable('''+@strOldValue+''','','')) X11 WHERE X11.Segment = OLD1.Segment)  '
    +' AND Segment = SEG.CES_cD '
  IF Isnull(@CommexDB,'')<>''
   Begin
	   SELECT @string = @string +'   '
		+' UNION ALL '
		+' SELECT ''Added'' as tag, [DeletedSegment] = SEGMENT, [DeletedExhange] = LTRIM(RTRIM(CES_Exchange))+''/'' + LTRIM(RTRIM(REPLACE(CES_Segment,''&'','''')))   FROM ( '
		+' SELECT VALUE AS Segment FROM DBO.Returntable('''+@strNewValue+''','','')) OLD1  , '+@CommexDB+'.DBO.CompanyExchangeSegments SEG (NOLOCK) '
		+' WHERE NOT EXISTS(SELECT 1 FROM( '
		+  ' SELECT VALUE AS Segment FROM DBO.Returntable('''+@strOldValue+''','','')) X11 WHERE X11.Segment = OLD1.Segment) '
		+' AND Segment = SEG.CES_cD '
   End
    
    INSERT INTO @tbl_SegmentAdd(SegmentAction, Segment, SegmentNaMe)
    EXEC(@string)
   END
  
   IF EXISTS(SELECT 1 FROM @tbl_SegmentAdd)
   BEGIN
     Declare @NewSegment varchar(500)='', @OldSegment varchar(500)=''
	 
	 SELECT @NewSegment = @NewSegment+','+SegmentNaMe 
     FROM @tbl_SegmentAdd e1
	 Where SegmentAction = 'Added'
	 
	  SELECT @OldSegment = @OldSegment+','+SegmentNaMe 
     FROM @tbl_SegmentAdd e1
	 Where SegmentAction = 'Deleted'
	 
	 /*set @NewSegment = (SELECT STRING_AGG(SegmentNaMe, ', ') 
	 FROM @tbl_SegmentAdd Where SegmentAction = 'Added')
	 
	 SET @OldSegment = (SELECT STRING_AGG(SegmentNaMe, ', ') 
	 FROM @tbl_SegmentAdd Where SegmentAction = 'Deleted')
	 

	 SELECT @NewSegment = ConcatenatedValues FROM(
	 SELECT SegmentNaMe, STUFF((SELECT ', ' + SegmentNaMe FROM @tbl_SegmentAdd e2
        WHERE e1.SegmentNaMe = e2.SegmentNaMe AND SegmentAction = 'Added'
        FOR XML PATH(''), TYPE
       ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS ConcatenatedValues
     FROM @tbl_SegmentAdd e1
	 Where SegmentAction = 'Added'
     GROUP BY SegmentNaMe) X11;

	 SELECT @OldSegment = ConcatenatedValues FROM(
	 SELECT SegmentNaMe, STUFF((SELECT ', ' + SegmentNaMe FROM @tbl_SegmentAdd e2
        WHERE e1.SegmentNaMe = e2.SegmentNaMe AND SegmentAction = 'Deleted'
        FOR XML PATH(''), TYPE
       ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS ConcatenatedValues
     FROM @tbl_SegmentAdd e1
	 WHERE SegmentAction = 'Deleted'
     GROUP BY SegmentNaMe) X11;
     */
	 INSERT INTO Client_ModifyAPI(ca_cmcd, ca_field, ca_desc, ca_oldValue, ca_newValue, ca_date, ca_time, ca_computername, ca_Tplus,
								   ca_Cross, ca_Estro, ca_Dematacno, ca_filler1, ca_filler2, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3)
	 SELECT top 1 ca_cmcd, 'sg_segment', 'segments for display', Oldvalue = Isnull(@OldSegment,' '), 
	 NewValue = Isnull(@NewSegment,' '), ca_date, ca_time, ca_computername, ca_Tplus,
     ca_Cross, ca_Estro, ca_Dematacno, ca_filler1 = 'DisplaySegment' , ca_filler2 =0, ca_filler3, ca_Nfiller1, ca_Nfiller2, ca_Nfiller3
     FROM @tbl_Client_ModifyAPI 

     SET @o_vcJsonOutput = ''
	 RETURN 1
   END 
 END TRY
 BEGIN CATCH
   SET @o_vcErrorFlag = 'E'
   SET @o_vcErrorMessage = 'MAIN '+ERROR_MESSAGE()
   DELETE FROM Client_ModifyAPI WHERE ca_cmcd = @i_vcClientCode AND ca_Nfiller3 = @seqNo
   RETURN 1
 END CATCH
END
GO

CREATE PROCEDURE [stpr_APIReKyc]
  @i_vcProjectName VARCHAR(50), @i_vcModuleName VARCHAR(100),                                   
  @i_vcFunctionName VARCHAR(100), @i_vcSource VARCHAR(1),                                 
  @i_vcUniqueID VARCHAR(50), @i_vcUserID VARCHAR(50),                                   
  @i_vcInputJSON NVARCHAR(MAX) , @o_vcOutPutJSON NVARCHAR(MAX) OUTPUT                                 
WITH ENCRYPTION
AS                                  
BEGIN

  DECLARE @vc_OutputJSON VARCHAR(MAX) = '', @vc_RefNo VARCHAR(25) = ''

  DECLARE @iAPISerialNo INT = 0,  @iCount INT = 0, @strResponseText VARCHAR(MAX)='', 
  @strStatus VARCHAR(1)='N', @string VARCHAR(MAX)='', @strtradeplustempdb varchar(50)=''
  
  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB'

  DECLARE @tbl_InputJSONTable TABLE (SerialNo INT, ColumnName VARCHAR(100), ColumnValue NVARCHAR(MAX),                             
   ValueTypeColumn INT,ImageFlag VARCHAR(1)) 
   
  SELECT @iCount = ISNULL(COUNT(*),0) FROM tbl_GenericAPIDefinition(NOLOCK)                                 
  WHERE ProjectName = @i_vcProjectName AND ModuleName = @i_vcModuleName                             
  AND FunctionName = @i_vcFunctionName                                
  /*
  IF @iCount <> 1                            
  BEGIN                            
    SET @o_vcOutPutJSON ='[{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}]'                                  
    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Invalid Request Definition')                                  
    RETURN 1                                   
  END
  */
  BEGIN TRY
    SET @string = 'SELECT * FROM '+@strtradeplustempdb+'.DBO.fn_ParentJSONSplit('''+@i_vcInputJSON+''') '                      
	INSERT INTO @tbl_InputJSONTable                                   
	EXEC(@string)
  END  TRY                
  BEGIN CATCH                                  
    SET @o_vcOutPutJSON ='[{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}]'                             
    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Input JSON '+ERROR_MESSAGE())                                  
    RETURN 1                                   
  END CATCH
  

  IF @i_vcProjectName = 'TradeWebAPI' AND @i_vcModuleName = 'ReKYC'         
    --AND @i_vcFunctionName = ''
  BEGIN
    DECLARE @strClientCode VARCHAR(8), @strTemplateCode VARCHAR(20), @strProcessTag VARCHAR(1), @strMethod VARCHAR(20), @JsonData VARCHAR(MAX), @o_vcErrorFlag VARCHAR(1), @o_vcErrorMessage VARCHAR(500)
    SELECT @strClientCode = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
    IF @i_vcFunctionName = 'GetClientMaster'
    BEGIN
      SET @strProcessTag = 'G'
    END
    ELSE
    BEGIN
      SET @strProcessTag = ''
    END
    IF @i_vcFunctionName = 'GetClientMaster' OR @i_vcFunctionName = 'GetPostClientMaster'
    BEGIN
      SET @strTemplateCode='Template2'
      EXEC [dbo].[SP_ReKyc_GetData] @strClientCode,  @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT, 
      @o_vcOutPutJSON OUTPUT, @strTemplateCode, @strProcessTag

      IF @o_vcErrorFlag = 'E'
      BEGIN
        SET @o_vcOutPutJSON ='[{"ClientCode":"##ClientCode##","Status":"N","Remark":"##ErrorMessage##"}]'
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ClientCode##', @strClientCode)
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##', @o_vcErrorMessage)
        RETURN 1
      END
      SET @o_vcOutPutJSON = '[' + @o_vcOutPutJSON + ']'
      RETURN 1
    END

    IF @i_vcFunctionName = 'UpdateClientMaster'
    BEGIN
	  SET @strTemplateCode='Template2'
      set @vc_OutputJSON = ''
      set @vc_RefNo  = ISNULL((select MAX(ISNULL(ca_Nfiller3, 0)) + 1 from Client_ModifyAPI(NOLOCK) where ca_cmcd=@strClientCode),1)
      EXEC sp_ReKyc_CorssAPI @i_vcInputJSON, @strClientCode, @vc_RefNo , 'API' , @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT, 
      @o_vcOutPutJSON OUTPUT

      IF @o_vcErrorFlag = 'E'
      BEGIN
        SET @o_vcOutPutJSON ='[{"Status":"N","Remark":"' + @o_vcErrorMessage + '","Data":' + @o_vcOutPutJSON + '}]'
        RETURN 1
      END
      SET @o_vcOutPutJSON='[{"Status":"Y","Remark":"' + @o_vcErrorMessage + '","Data":' + @o_vcOutPutJSON + '}]'
      RETURN 1
--      SET @o_vcOutPutJSON = '[' + @o_vcOutPutJSON + ']'
      --return 1
    END
	
	IF @i_vcFunctionName = 'GetClientClosure'
    BEGIN
      SET @strTemplateCode='ONLYCLOSURE'
      EXEC [dbo].[SP_ReKyc_GetData] @strClientCode,  @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT, 
      @o_vcOutPutJSON OUTPUT, @strTemplateCode, @strProcessTag

      IF @o_vcErrorFlag = 'E'
      BEGIN
	    IF EXISTS(SELECT 1 FROM CLIENT_MASTER(NOLOCK) WHERE CM_cD = @strClientCode AND cm_freezeyn ='A')
		BEGIN
		  SET @strStatus = 'N'
		END
	    ELSE
		BEGIN
		  SET @strStatus = 'Y'
		END
        SET @o_vcOutPutJSON ='[{"ClientCode":"##ClientCode##","Status":"##Status##","Remark":"##ErrorMessage##","Data":' + @o_vcOutPutJSON + '}]'
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ClientCode##', @strClientCode)
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##', @o_vcErrorMessage)
		SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##Status##', @strStatus)
        RETURN 1
      END
      
	  SET @o_vcOutPutJSON ='[{"ClientCode":"##ClientCode##","Status":"##Status##","Remark":"##ErrorMessage##","Data":' + @o_vcOutPutJSON + '}]'
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ClientCode##', @strClientCode)
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##', @o_vcErrorMessage)
	  SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##Status##', 'Y')
	  --SET @o_vcOutPutJSON = '[' + @o_vcOutPutJSON + ']'
      RETURN 1
    END
	
	IF @i_vcFunctionName = 'UpdateClientClosure'
    BEGIN
	  SET @strTemplateCode = 'ONLYCLOSURE'
      set @vc_OutputJSON = ''
      set @vc_RefNo  = ISNULL((select MAX(ISNULL(ca_Nfiller3, 0)) + 1 from Client_ModifyAPI(NOLOCK)),1)
	  IF ISNULL(@vc_RefNo,0) = 0
	  BEGIN
	    SET @vc_RefNo = 1
	  END
      EXEC sp_ReKyc_CorssAPI @i_vcInputJSON, @strClientCode, @vc_RefNo , 'API' , @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT, 
      @o_vcOutPutJSON OUTPUT, @strTemplateCode

      IF @o_vcErrorFlag = 'E'
      BEGIN
        SET @o_vcOutPutJSON ='[{"Status":"N","Remark":"' + @o_vcErrorMessage + '","Data":' + @o_vcOutPutJSON + '}]'
        RETURN 1
      END
      SET @o_vcOutPutJSON='[{"Status":"Y","Remark":"' + @o_vcErrorMessage + '","Data":' + @o_vcOutPutJSON + '}]'
      RETURN 1
--      SET @o_vcOutPutJSON = '[' + @o_vcOutPutJSON + ']'
      --return 1
    END
	
    IF @i_vcFunctionName = 'PostClientClosureMaker'
    BEGIN
	  IF EXISTS(SELECT 1 FROM Client_ReKycMain(NOLOCK) WHERE rm_cmcd = @strClientCode AND rm_RequestType='Account closure' AND rm_status = 'Pending'
	  AND rm_rekyc = 'N')
	  BEGIN
	    SET @o_vcOutPutJSON = '{}'
		SET @o_vcErrorMessage = 'The closure request is currently under process.'
	    SET @o_vcOutPutJSON ='[{"Status":"N","Remark":"' + @o_vcErrorMessage + '","Data":' + @o_vcOutPutJSON + '}]'
		RETURN 1
	  END
	
	  SET @strTemplateCode='ONLYCLOSURE'
      SET @vc_OutputJSON = ''
      SET @vc_RefNo  = ISNULL((select MAX(ISNULL(ca_Nfiller3, 0)) + 1 FROM Client_ModifyAPI(NOLOCK)),1)
	  
	  IF ISNULL(@vc_RefNo,0) = 0
	  BEGIN
	    SET @vc_RefNo = 1
	  END
      
	  
	  BEGIN TRY
        EXEC [dbo].[SP_ReKyc_CheckValidation] @i_vcInputJSON, @strClientCode, 0, 'MAKER', @o_vcErrorFlag OUTPUT, 
        @o_vcErrorMessage OUTPUT, @o_vcOutPutJSON OUTPUT, 'U', 'JSON', @strTemplateCode
        IF @o_vcErrorFlag = 'E'
        BEGIN
          SET @o_vcOutPutJSON ='[{"Status":"N","Remark":"' + @o_vcErrorMessage + '","Data":' + @o_vcOutPutJSON + '}]'
		  DELETE FROM Client_ReKycMain WHERE rm_cmcd = @strClientCode AND rm_refno = @vc_RefNo
		  DELETE FROM Client_ModifyAPI WHERE ca_cmcd = @strClientCode AND ca_Nfiller3 = @vc_RefNo
		  RETURN 1
        END
      END TRY
      BEGIN CATCH
        SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Validation: '+ERROR_MESSAGE()
		DELETE FROM Client_ReKycMain WHERE rm_cmcd = @strClientCode AND rm_refno = @vc_RefNo
		DELETE FROM Client_ModifyAPI WHERE ca_cmcd = @strClientCode AND ca_Nfiller3 = @vc_RefNo
	    SET @o_vcOutPutJSON ='[{"Status":"N","Remark":"' + @o_vcErrorMessage + '","Data":' + @o_vcOutPutJSON + '}]'
		RETURN 1
      END CATCH
      SET @o_vcErrorFlag = ''
      SET @o_vcErrorMessage = ''
      -- 
      --- MAKER POST
	  
      BEGIN TRY
        EXEC [dbo].[SP_ReKyc_MakerPost] '', @i_vcInputJSON, 
        @strClientCode, @vc_RefNo, @i_vcUserID, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT, @o_vcOutPutJSON OUTPUT, @strTemplateCode
  
        IF @o_vcErrorFlag = 'E'
        BEGIN
          DELETE FROM Client_ModifyAPI WHERE ca_cmcd = @strClientCode AND ca_Nfiller3 = @vc_RefNo
		  DELETE FROM Client_ReKycMain WHERE rm_cmcd = @strClientCode AND rm_refno = @vc_RefNo
		  SET @o_vcOutPutJSON ='[{"Status":"N","Remark":"' + @o_vcErrorMessage + '","Data":' + @o_vcOutPutJSON + '}]'
          RETURN 1
        END   
     END TRY
     BEGIN CATCH
       SET @o_vcErrorFlag = 'E'
	   SET @o_vcErrorMessage = 'MakerPost: '+ERROR_MESSAGE()
	   SET @o_vcOutPutJSON ='[{"Status":"N","Remark":"' + @o_vcErrorMessage + '","Data":' + @o_vcOutPutJSON + '}]'
	   DELETE FROM Client_ModifyAPI WHERE ca_cmcd = @strClientCode AND ca_Nfiller3 = @vc_RefNo
	   DELETE FROM Client_ReKycMain WHERE rm_cmcd = @strClientCode AND rm_refno = @vc_RefNo
	   RETURN 1
     END CATCH
	 
	 INSERT INTO Client_ReKycMain(rm_cmcd, rm_cdate, rm_ctime, rm_rekyc, rm_status, rm_desc, mkrdt, mkrtm, rm_step, rm_refno, rm_RequestType)
     VALUES(@strClientCode, CONVERT(VARCHAR,GETDATE(),112),CONVERT(varchar, getdate(), 108),'N','Pending','Only Closure',CONVERT(VARCHAR,GETDATE(),112),'',1,@vc_RefNo, 'Account Closure')
	 SET @o_vcErrorMessage = 'Closure Request Submitted'
     SET @o_vcOutPutJSON='[{"Status":"Y","RefNo":"##RefNo##","Remark":"' + @o_vcErrorMessage + '","Data":' + @o_vcOutPutJSON + '}]'
	 SET @o_vcOutPutJSON=REPLACE(@o_vcOutPutJSON,'##RefNo##',CAST(@vc_RefNo AS VARCHAR))
     RETURN 1
   END
   IF @i_vcFunctionName = 'GetClientClosureChecker'
   BEGIN
	 SET @strTemplateCode='ONLYCLOSURE'
	 SET @o_vcOutPutJSON = ''
	 SET @o_vcErrorMessage = ''
	 DECLARE @strRequestFlag VARCHAR(1)='', @strFromDate VARCHAR(8)='', @strToDate VARCHAR(8)=''
	 SELECT @strRequestFlag = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='RequestFlag'
	 SELECT @strFromDate = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='FromDate'
	 SELECT @strToDate = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='ToDate'
	 IF ISNULL(@strRequestFlag,'') = ''
	 Begin
	   SET @strRequestFlag = 'P'
	 END
     EXEC stpr_GetMarkerDataOnlyClosure @o_vcOutPutJSON OUTPUT, @strRequestFlag, @strFromDate, @strToDate
     SET @o_vcOutPutJSON='[{"Status":"Y","RefNo":"##RefNo##","Remark":"' + @o_vcErrorMessage + '","Data":' + @o_vcOutPutJSON + '}]'
	 SET @o_vcOutPutJSON=REPLACE(@o_vcOutPutJSON,'##RefNo##',CAST(@vc_RefNo AS VARCHAR))
     RETURN 1
   END
   
   IF @i_vcFunctionName = 'PostClientClosureChecker'
   BEGIN
	 SET @strTemplateCode='ONLYCLOSURE'
     SET @vc_OutputJSON = ''
	 SET @o_vcOutPutJSON = '{}'
     
	 DECLARE @strApprovalFlag VARCHAR(1)='', @strApprovalRemarks VARCHAR(200)=''
	 SELECT @strApprovalFlag = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='ApprovalFlag'
	 SELECT @strApprovalRemarks = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='ApprovalRemarks'
	 SELECT @vc_RefNo = CAST(ColumnValue AS INT) FROM @tbl_InputJSONTable WHERE ColumnName='RefNo'
     IF  @strApprovalFlag NOT IN('A','R')
	 BEGIN
	   SET @o_vcOutPutJSON ='[{"Status":"N","Remark":"ApprovalFlag Must be A/R","Data":' + @o_vcOutPutJSON + '}]'
	   RETURN 1
	 END
	 IF @strApprovalFlag = 'R' AND @strApprovalRemarks = ''
	 BEGIN
	   SET @o_vcOutPutJSON ='[{"Status":"N","Remark":"Remarks Require in case of Rejection","Data":' + @o_vcOutPutJSON + '}]'
	   RETURN 1
	 END
	 IF @strClientCode = ''
	 BEGIN
	   SET @o_vcOutPutJSON ='[{"Status":"N","Remark":"ClientCode can not be blank","Data":' + @o_vcOutPutJSON + '}]'
	   RETURN 1
	 END
	 if @vc_RefNo = 0
	 BEGIN
	   SET @o_vcOutPutJSON ='[{"Status":"N","Remark":"RefNo can not be blank","Data":' + @o_vcOutPutJSON + '}]'
	   RETURN 1
	 END
	 
	 BEGIN TRY
       EXEC [dbo].[SP_ReKyc_CheckerApprove] @strClientCode, @vc_RefNo, @strApprovalFlag ,@strApprovalRemarks, 
       @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT, @o_vcOutPutJSON OUTPUT, @strTemplateCode 
  
       IF  @o_vcErrorFlag = 'E'
       BEGIN
         SET @o_vcErrorFlag = 'E'
	     SET @o_vcErrorMessage = 'Checker: '+ERROR_MESSAGE()
	     SET @o_vcOutPutJSON ='[{"Status":"N","Remark":"' + @o_vcErrorMessage + '","Data":' + @o_vcOutPutJSON + '}]'
		 RETURN 1
       END	
     END TRY
     BEGIN CATCH
        SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = ERROR_MESSAGE()
	    SET @o_vcOutPutJSON ='[{"Status":"N","Remark":"' + @o_vcErrorMessage + '","Data":' + @o_vcOutPutJSON + '}]'
		RETURN 1
     END CATCH
     SET @o_vcOutPutJSON='[{"Status":"Y","RefNo":"##RefNo##","Remark":"' + @o_vcErrorMessage + '","Data":' + @o_vcOutPutJSON + '}]'
	 SET @o_vcOutPutJSON=REPLACE(@o_vcOutPutJSON,'##RefNo##',CAST(@vc_RefNo AS VARCHAR))
     RETURN 1
   END
 END
END
GO

CREATE PROCEDURE [stpr_APIReKycThirdParty] @i_vcVendorCode VARCHAR(20), 
  @i_vcFunctionName VARCHAR(100),  @i_vcUserID VARCHAR(50),                              
  @i_vcInputJSON NVARCHAR(MAX) , @i_vcRefNo VARCHAR(20), @o_vcOutPutJSON NVARCHAR(MAX) OUTPUT                                 
WITH ENCRYPTION
AS                                  
BEGIN                                  
/*                                  
///////////////////////////////////////////////////////////////////////////////////////////                                  
// Create By     : Vaibhav                                 
// Created Date  : 02-FEB-2024                                  
// CCT NO        :                                 
// Description   : 
// Reviewed By   :                                   
// Review Date   :                                   
//////////////////////////////////////////////////////////////////////////////////////////                                  
*/
 --- Variable Declaration Start

  DECLARE @iAPISerialNo INT = 0,  @iCount INT = 0, @strResponseText VARCHAR(MAX)=''                        
    
  DECLARE @strCN NVARCHAR(MAX)='', @strCV NVARCHAR(MAX)='',                   
  @str_IU_ColumnName NVARCHAR(MAX)='', @str_IU_ColumnValue NVARCHAR(MAX)='', @strMastertableName VARCHAR(50)='',              
  @o_SerialNo INT= 0, @o_dtlSerialNo INT= 0,        
  @strOutputImageMessage VARCHAR(MAX)='', @vcUserType VARCHAR(2)='' , @dtFromDate DATETIME = 0,            
  @dtToDate DATETIME = 0, @strQuery VARCHAR(MAX)='', @vcFieldName VARCHAR(100) ='',  @vcFieldValue VARCHAR(100)='',
  @o_vcErrorMessage VARCHAR(8000) ='', @o_vcErrorFlag VARCHAR(1)='', @strBrokerCode VARCHAR(30)='',
  @strMobileNo VARCHAR(10)='', @strEmailid VARCHAR(50)='', @strKyc VARCHAR(10)='', @strType VARCHAR(50)='', @strEntity VARCHAR(50)='',
  @strOPTFLAG VARCHAR(10)='', @vcOPT INT = 0,  @strSTAGE VARCHAR(50)='', @strThirdPartyURL VARCHAR(MAX)='', @strTokenid VARCHAR(50)='',
  @o_vcOutputJsonapi VARCHAR(MAX)='',@strTJsonInput VARCHAR(MAX)='', @strredirectUrl VARCHAR(100)='', @strRequestid VARCHAR(500)='',
  @strxmlFile VARCHAR(MAX)='', @stroutpuxmlFile VARCHAR(MAX)='',
  @strurlxml VARCHAR(MAX)='', @strClientCode VARCHAR(20)='', @strRefNo INT=0, @strFileData VARCHAR(MAX)='', @strFileName VARCHAR(500)='',
  @strFilePassword VARCHAR(50)=''
  
  DECLARE @strtradeplustempdb VARCHAR(50)='', @strString VARCHAR(MAX)=''
  
  DECLARE @jsonCutterOutput VARCHAR(MAX)='', @strDanSql NVARCHAR(MAX)=''
  DECLARE @JsonCutterXML XML
  
  DECLARE @strphoto VARCHAR(MAX)='', @strxmlname VARCHAR(100), @strxmlmaskedNumber VARCHAR(20)=''
  
  DECLARE @inputBitarrayString NVARCHAR(MAX), @binaryRepresentation VARBINARY(MAX) = 0x;
  
  DECLARE @indexbitarray INT = 1, @lengthbitarray INT 

  DECLARE @strOTP VARCHAR(20) = ''

  DECLARE @tbl_jsonoutput TABLE(SerialNo INT, ColumnName VARCHAR(50), ColumnValue VARCHAR(MAX), 
  MasterTag VARCHAR(100), JSONLEVEL INT, MASTERLEVEL INT)
    
  
  DECLARE @tbl_InputJSONTable TABLE (SerialNo INT, ColumnName VARCHAR(100), ColumnValue NVARCHAR(MAX),                             
   ValueTypeColumn INT,ImageFlag VARCHAR(1))                                              
  -- Variable Declaration END                                  
                              
  SET @o_vcOutPutJSON = ''     
  
  SELECT @strThirdPartyURL = sp_sysvalue 
  FROM Sysparameter(NOLOCK) WHERE sp_parmcd = 'ThirdParty'
      
  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB' 	  
                      
                                               
  BEGIN TRY           
    SET @strString = 'SELECT * FROM '+@strtradeplustempdb+'.DBO.fn_ParentJSONSplit('''+@i_vcInputJSON+''')'
	INSERT INTO @tbl_InputJSONTable                                   
	EXEC(@strString)
  END  TRY                
  BEGIN CATCH                                  
    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Input JSON '+ERROR_MESSAGE())                                  
    RETURN 1                                   
  END CATCH                            
  
  DECLARE @o_vcOutPutJSON1 VARCHAR(MAX)=''                   
  BEGIN TRY 
   IF @i_vcFunctionName = 'IPV_FINAL'              
   BEGIN
    SELECT @strClientCode = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
    
	SELECT @strRefNo = max(rm_refno) FROM Client_ReKycMain(NOLOCK) WHERE rm_cmcd = @strClientCode AND rm_status='Pending'
	AND rm_RequestType = 'ReKYC'
	
	SELECT @strFileData = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='base64'
		
    IF @strClientCode  = '' OR @strRefNo = ''
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Require Value ClientCode')  
      RETURN 1 
	END

	IF @strFileData <> ''
	BEGIN
	  SET @binaryRepresentation = CAST(CONVERT(VARBINARY(MAX), @strFileData) AS VARBINARY(MAX));

	  IF NOT EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	  AND ma_filename = 'IPV_FINAL')
	  BEGIN
	    INSERT INTO Client_ModifyAttach(ma_cmcd, ma_date, ma_filename, ma_field, mkrdt, mkrtm, ma_proof, 
		ma_refno, ma_Nfiller1, ma_status)
		VALUES(@strClientCode, CONVERT(VARCHAR,GETDATE(),112), 'IPV_FINAL', 'IPV FINAL Image -'+@strOTP,
		CONVERT(VARCHAR,GETDATE(),112), CONVERT(VARCHAR,GETDATE(), 108),@binaryRepresentation, @strRefNo, 1, 'N')
	  END
      ELSE
      BEGIN
	    UPDATE A SET A.ma_proof = @binaryRepresentation, A.mkrdt = CONVERT(VARCHAR,GETDATE(),112), 
		A.mkrtm = CONVERT(VARCHAR,GETDATE(), 108), ma_field = 'IPV FINAL Image -'+@strOTP
		FROM Client_ModifyAttach A
		WHERE Ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	    AND ma_filename = @i_vcFunctionName
      END 
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"S","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Image Saved')  
      RETURN 1 
	END 
	ELSE
    BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Image Not Found')  
      RETURN 1 
	END	
   END	
   ELSE
   IF @i_vcFunctionName = 'IPV'              
   BEGIN
    SELECT @strClientCode = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
    
	SELECT @strRefNo = max(rm_refno) FROM Client_ReKycMain(NOLOCK) WHERE rm_cmcd = @strClientCode AND rm_status='Pending'
	and rm_RequestType = 'ReKYC'
	
	SELECT @strFileData = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='base64'
	SELECT @strOTP = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='OTP'
		
    IF @strClientCode  = '' OR @strRefNo = '' OR ISNULL(@strOTP,'') = ''
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Require Value ClientCode,OTP')  
      RETURN 1 
	END

	IF @strFileData <> ''
	BEGIN
	  SET @strTJsonInput = '{"base64":"##base64##","FileName":"##FileName##","FilePassword": "##FilePassword##"}'
	  SET @strTJsonInput = REPLACE(@strTJsonInput,'##base64##',@strFileData)
	  SET @strTJsonInput = REPLACE(@strTJsonInput,'##FileName##','Video.mp4')
	  SET @strTJsonInput = REPLACE(@strTJsonInput,'##FilePassword##','')
	  
	  EXEC stpr_CallThirdPartyAPI 'SecMark', 'SecMark', 'detectliveness', '',@strTJsonInput, '', @strThirdPartyURL, @i_vcUserID, @o_vcOutputJsonapi OUTPUT
	 
	  IF @o_vcOutputJsonapi <> ''
	  BEGIN
	    DELETE from @tbl_jsonoutput
	    BEGIN TRY
		
          SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
          EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
          SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
          INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
		  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
          SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE()+@o_vcOutputJsonapi)  
          RETURN 1 		
        END CATCH 
	    
		IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'status' AND ColumnValue ='Success')
	    BEGIN
		  SELECT @strFileData  = ColumnValue  FROM @tbl_jsonoutput WHERE ColumnName = 'b64content' AND ColumnValue <> ''
		  SET @binaryRepresentation = CAST(CONVERT(VARBINARY(MAX), @strFileData) AS VARBINARY(MAX));
	       IF NOT EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	       AND ma_filename = 'IPV')
	       BEGIN
	         INSERT INTO Client_ModifyAttach(ma_cmcd, ma_date, ma_filename, ma_field, mkrdt, mkrtm, ma_proof, 
		     ma_refno, ma_Nfiller1, ma_status)
		     VALUES(@strClientCode, CONVERT(VARCHAR,GETDATE(),112), 'IPV', 'IPV Image -'+@strOTP,
		     CONVERT(VARCHAR,GETDATE(),112), CONVERT(VARCHAR,GETDATE(), 108),@binaryRepresentation, @strRefNo, 1, 'N')
	      END
          ELSE
          BEGIN
	        UPDATE A SET A.ma_proof = @binaryRepresentation, A.mkrdt = CONVERT(VARCHAR,GETDATE(),112), 
		    A.mkrtm = CONVERT(VARCHAR,GETDATE(), 108)
		    FROM Client_ModifyAttach A
		    WHERE Ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	        AND ma_filename = 'IPV'
          END  
 		  SET @o_vcOutPutJSON = @o_vcOutputJsonapi
		  RETURN 1
	    END
	    ELSE
	    BEGIN
	      SET @o_vcOutPutJSON =@o_vcOutputJsonapi
          SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
          RETURN 1 
	    END
	  END  
	  ELSE
	  BEGIN
	    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
        RETURN 1 
	  END
	  
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"S","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Image Saved')  
      RETURN 1 
	END 
	ELSE
    BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Image Not Found')  
      RETURN 1 
	END	
  END	
  ELSE  
  IF @i_vcFunctionName IN('DigilockerJSON')
  BEGIN
    
	SELECT @strClientCode = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
    SET @strRefNo = @i_vcRefNo
    SELECT @strRequestid = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='Requestid' 
	
	IF @strRefNo = '' OR @strClientCode  = '' OR @strRequestid = ''
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Require Value redirectUrl,ClientCode,Requestid')  
      RETURN 1 
	END

	SET @strTJsonInput = '{}'
	
	EXEC stpr_CallThirdPartyAPI 'REKYC',@i_vcVendorCode, 'Gete-AadhaarXML', '',@strTJsonInput, @strRequestid,@strThirdPartyURL, @i_vcUserID, @o_vcOutputJsonapi  OUTPUT	
	IF @o_vcOutputJsonapi <> ''
	BEGIN
	  DELETE from @tbl_jsonoutput
	  
	  BEGIN TRY
	    SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
        INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
		SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE()+@o_vcOutputJsonapi)  
        RETURN 1 	
      END CATCH
	  IF NOT EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'status' and ColumnValue = 'complete')
	  BEGIN
		SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
        RETURN 1 
	  END
	  ELSE
	  BEGIN 
	    SET @binaryRepresentation = CAST(CONVERT(VARBINARY(MAX), @o_vcOutputJsonapi) AS VARBINARY(MAX));

		IF EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @strClientCode AND ma_refno = @strRefNo
		AND ma_filename = @i_vcFunctionName)
		BEGIN
		  UPDATE A SET A.ma_proof = @binaryRepresentation, A.mkrdt = CONVERT(VARCHAR,GETDATE(),112), 
		  A.mkrtm = CONVERT(VARCHAR,GETDATE(), 108)
		  FROM Client_ModifyAttach A
		  WHERE Ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	      AND ma_filename = @i_vcFunctionName 
		END
		ELSE
		BEGIN
	      INSERT INTO Client_ModifyAttach(ma_cmcd, ma_date, ma_filename, ma_field, mkrdt, mkrtm, ma_proof, 
		  ma_refno, ma_Nfiller1, ma_status)
		  VALUES(@strClientCode, CONVERT(VARCHAR,GETDATE(),112), @i_vcFunctionName, 'DigiLocker Response full Json with XML',
		  CONVERT(VARCHAR,GETDATE(),112), CONVERT(VARCHAR,GETDATE(), 108),@binaryRepresentation, @strRefNo, 1, 'N')
		END  
		
		--- INSERT PHOTO
		
		SET @binaryRepresentation =  0x;
		SELECT @strphoto = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName IN('photo')
		IF @strphoto <> ''
		BEGIN
		  SET @binaryRepresentation = CAST(CONVERT(VARBINARY(MAX), @strphoto) AS VARBINARY(MAX));

		  IF EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @strClientCode AND ma_refno = @strRefNo
		  AND ma_filename = 'Photo')
		  BEGIN
		    UPDATE A SET A.ma_proof = @binaryRepresentation, A.mkrdt = CONVERT(VARCHAR,GETDATE(),112), 
		    A.mkrtm = CONVERT(VARCHAR,GETDATE(), 108)
		    FROM Client_ModifyAttach A
		    WHERE Ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	        AND ma_filename = 'Photo' 
		  END
		  ELSE
		  BEGIN
		    INSERT INTO Client_ModifyAttach(ma_cmcd, ma_date, ma_filename, ma_field, mkrdt, mkrtm, ma_proof, 
		    ma_refno, ma_Nfiller1, ma_status)
		    VALUES(@strClientCode, CONVERT(VARCHAR,GETDATE(),112), 'Photo', 'PhotoAttachment',
		    CONVERT(VARCHAR,GETDATE(),112), CONVERT(VARCHAR,GETDATE(), 108),@binaryRepresentation, @strRefNo, 1, 'N')
		  END
		END
		
		-- INSERT XML FILE 
		
	    SET @binaryRepresentation =  0x;
		SELECT @strurlxml = ColumnValue  FROM @tbl_jsonoutput WHERE ColumnName IN('fileUrl')
		IF @strurlxml <> ''
		BEGIN
  	      SET @strxmlFile = '{"fileUrl":"##fileUrl##"}'
		  SET @strxmlFile = replace(@strxmlFile,'##fileUrl##',@strurlxml)
		  EXEC stpr_GetURLtoBASE64 @strxmlFile, @strThirdPartyURL, @stroutpuxmlFile OUTPUT
		  
		  DELETE from @tbl_jsonoutput
		  
		  SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@stroutpuxmlFile+''' , @jsonCutterOutput OUTPUT';
          EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
          SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
          INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
		  SELECT X1.* FROM(
          SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
	      JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
          JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
	      JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
	      JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
	      JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
          FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1
          
		  IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'status' and ColumnValue = 'true')
		  BEGIN
		    SELECT @stroutpuxmlFile = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'data'
		  END	

		  IF @stroutpuxmlFile <> ''
		  BEGIN
		    SET @binaryRepresentation = CAST(CONVERT(VARBINARY(MAX), @stroutpuxmlFile) AS VARBINARY(MAX));

		    IF EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @strClientCode AND ma_refno = @strRefNo
		    AND ma_filename = 'DigilockerXML')
		    BEGIN
		      UPDATE A SET A.ma_proof = @binaryRepresentation, A.mkrdt = CONVERT(VARCHAR,GETDATE(),112), 
		      A.mkrtm = CONVERT(VARCHAR,GETDATE(), 108)
		      FROM Client_ModifyAttach A
		      WHERE Ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	          AND ma_filename = 'DigilockerXML' 
		    END
			ELSE
			BEGIN
		      INSERT INTO Client_ModifyAttach(ma_cmcd, ma_date, ma_filename, ma_field, mkrdt, mkrtm, ma_proof, 
		      ma_refno, ma_Nfiller1, ma_status)
		      VALUES(@strClientCode, CONVERT(VARCHAR,GETDATE(),112), 'DigilockerXML', 'DigiLocker Response XML',
		      CONVERT(VARCHAR,GETDATE(),112), CONVERT(VARCHAR,GETDATE(), 108),@binaryRepresentation, @strRefNo, 1, 'N')
			END  
		  END
		END
		
		SET @o_vcOutPutJSON = @o_vcOutputJsonapi
		
		SET @strTJsonInput = '{"docType": "AADHAAR","format": "jpg","consent": "Y"}'
	    SET @o_vcOutputJsonapi = ''
	    EXEC stpr_CallThirdPartyAPI 'REKYC',@i_vcVendorCode, 'documentDigilocker', '',@strTJsonInput,
		@strRequestid,@strThirdPartyURL, @i_vcUserID, @o_vcOutputJsonapi  OUTPUT	
	    IF @o_vcOutputJsonapi <> ''
	    BEGIN
	      DELETE from @tbl_jsonoutput
	      BEGIN TRY
		  	SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
            EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
            SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
            INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
		    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
            SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE()+@o_vcOutputJsonapi)  
            RETURN 1 	
          END CATCH
	  
		  SET @binaryRepresentation =  0x;
		  IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'fileUrl' and ColumnValue <> '')
		  BEGIN
			SELECT @strurlxml = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'fileUrl'
			IF @strurlxml <> ''
		    BEGIN
  	          SET @strxmlFile = '{"fileUrl":"##fileUrl##"}'
		      SET @strxmlFile = replace(@strxmlFile,'##fileUrl##',@strurlxml)
		      EXEC stpr_GetURLtoBASE64 @strxmlFile, @strThirdPartyURL, @stroutpuxmlFile OUTPUT
			  
			  		  
		      DELETE from @tbl_jsonoutput
		  
		  	  SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@stroutpuxmlFile+''' , @jsonCutterOutput OUTPUT';
              EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
              SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
              INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
		      SELECT X1.* FROM(
              SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
	          JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
              JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
	          JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
	          JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
	          JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
              FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1
		  
            
		      IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'status' and ColumnValue = 'true')
		      BEGIN
		        SELECT @stroutpuxmlFile = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'data'
		      END	

			  
		      IF @stroutpuxmlFile <> ''
		      BEGIN
		        SET @binaryRepresentation = CAST(CONVERT(VARBINARY(MAX), @stroutpuxmlFile) AS VARBINARY(MAX));
  
	            IF EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @strClientCode AND ma_refno = @strRefNo
		        AND ma_filename = 'AADHAAR')
		        BEGIN
		          UPDATE A SET A.ma_proof = @binaryRepresentation, A.mkrdt = CONVERT(VARCHAR,GETDATE(),112), 
		          A.mkrtm = CONVERT(VARCHAR,GETDATE(), 108)
		          FROM Client_ModifyAttach A
		          WHERE Ma_cmcd = @strClientCode AND ma_refno = @strRefNo
 	              AND ma_filename = 'AADHAAR' 
		        END
				ELSE
			    BEGIN
                  INSERT INTO Client_ModifyAttach(ma_cmcd, ma_date, ma_filename, ma_field, mkrdt, mkrtm, ma_proof, 
		          ma_refno, ma_Nfiller1, ma_status)
		          VALUES(@strClientCode, CONVERT(VARCHAR,GETDATE(),112), 'Aadhaar', 'AadhaarAttachment',
		          CONVERT(VARCHAR,GETDATE(),112), CONVERT(VARCHAR,GETDATE(), 108),@binaryRepresentation, @strRefNo, 1, 'N')
				END   
			  END	
			END 
			ELSE 
			BEGIN
			  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
              SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','GET AADHAAR DOCUMENT ' +@o_vcOutputJsonapi)  
              RETURN 1  
			END
		  END	
		END	
		--- DOWNLOAD PAN CARD
		
		SET @strTJsonInput = ''
		SET @strTJsonInput = '{"docType": "PANCR","format": "pdf","consent": "Y"}'
	    SET @o_vcOutputJsonapi = ''
	    EXEC stpr_CallThirdPartyAPI 'REKYC',@i_vcVendorCode, 'documentDigilocker', '',@strTJsonInput,
		@strRequestid,@strThirdPartyURL, @i_vcUserID, @o_vcOutputJsonapi  OUTPUT	
	    IF @o_vcOutputJsonapi <> ''
	    BEGIN
	      DELETE from @tbl_jsonoutput
	      BEGIN TRY
            SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
            EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
            SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
            INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
		    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
            SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE()+@o_vcOutputJsonapi)  
            RETURN 1 	
          END CATCH
	  
		  SET @binaryRepresentation =  0x;
		  IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'fileUrl' and ColumnValue <> '')
		  BEGIN
			SELECT @strurlxml = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'fileUrl'
			IF @strurlxml <> ''
		    BEGIN
  	          SET @strxmlFile = '{"fileUrl":"##fileUrl##"}'
		      SET @strxmlFile = replace(@strxmlFile,'##fileUrl##',@strurlxml)
		      EXEC stpr_GetURLtoBASE64 @strxmlFile, @strThirdPartyURL, @stroutpuxmlFile OUTPUT
			  
			  		  
		      DELETE from @tbl_jsonoutput
		  
		      SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@stroutpuxmlFile+''' , @jsonCutterOutput OUTPUT';
              EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
              SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
              INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
		      SELECT X1.* FROM(
              SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
	          JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
              JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
	          JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
	          JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
	          JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
              FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1
		    
		      IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'status' and ColumnValue = 'true')
		      BEGIN
		        SELECT @stroutpuxmlFile = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'data'
		      END	

			  
		      IF @stroutpuxmlFile <> ''
		      BEGIN
		        SET @binaryRepresentation = CAST(CONVERT(VARBINARY(MAX), @stroutpuxmlFile) AS VARBINARY(MAX));
  
	            IF EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @strClientCode AND ma_refno = @strRefNo
		        AND ma_filename = 'PANCARD')
		        BEGIN
		          UPDATE A SET A.ma_proof = @binaryRepresentation, A.mkrdt = CONVERT(VARCHAR,GETDATE(),112), 
		          A.mkrtm = CONVERT(VARCHAR,GETDATE(), 108)
		          FROM Client_ModifyAttach A
		          WHERE Ma_cmcd = @strClientCode AND ma_refno = @strRefNo
 	              AND ma_filename = 'PANCARD' 
		        END
				ELSE
			    BEGIN
                  INSERT INTO Client_ModifyAttach(ma_cmcd, ma_date, ma_filename, ma_field, mkrdt, mkrtm, ma_proof, 
		          ma_refno, ma_Nfiller1, ma_status)
		          VALUES(@strClientCode, CONVERT(VARCHAR,GETDATE(),112), 'PANCARD', 'PANAttachment',
		          CONVERT(VARCHAR,GETDATE(),112), CONVERT(VARCHAR,GETDATE(), 108),@binaryRepresentation, @strRefNo, 1, 'N')
				END   
			  END	
			END 
			ELSE 
			BEGIN
			  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
              SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','GET AADHAAR DOCUMENT ' +@o_vcOutputJsonapi)  
              RETURN 1  
			END
		  END	
		END	
	  END	
    END  
	ELSE
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
      RETURN 1  
	END
  END	
  
  ELSE
  
  IF @i_vcFunctionName IN('EsignRequest','EsignRequestKRA')                            
  BEGIN
    SELECT @strClientCode = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
    SET @strRefNo = @i_vcRefNo
	SELECT @strFileData = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='base64'
	SELECT @strFileName = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='FileName'
	SELECT @strFilePassword = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='FilePassword'
    IF @strClientCode  = '' OR @strRefNo = ''
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Require Value ClientCode,RefNo')  
      RETURN 1 
	END

	IF @strFileData <> ''
	BEGIN
	  SET @binaryRepresentation = CAST(CONVERT(VARBINARY(MAX), @strFileData) AS VARBINARY(MAX));

	  IF NOT EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	  AND ((ma_filename = 'UnSignedPdf' and @i_vcFunctionName = 'EsignRequest')
	  OR (ma_filename = 'UnSignedKRAPdf' and @i_vcFunctionName = 'EsignRequestKRA')))
	  BEGIN
	    INSERT INTO Client_ModifyAttach(ma_cmcd, ma_date, ma_filename, ma_field, mkrdt, mkrtm, ma_proof, 
		ma_refno, ma_Nfiller1, ma_status)
		VALUES(@strClientCode, CONVERT(VARCHAR,GETDATE(),112),  (CASE WHEN  @i_vcFunctionName = 'EsignRequest' THEN 
		'UnSignedPdf' ELSE 'UnSignedKRAPdf' END), (CASE WHEN  @i_vcFunctionName = 'EsignRequest' THEN 
		'ReKyc UnSigned Pdf' ELSE 'ReKyc KRA UnSigned Pdf' END), 
		CONVERT(VARCHAR,GETDATE(),112), CONVERT(VARCHAR,GETDATE(), 108),@binaryRepresentation, @strRefNo, 1, 'N')
	  END
      ELSE
      BEGIN
	    UPDATE A SET A.ma_proof = @binaryRepresentation, A.mkrdt = CONVERT(VARCHAR,GETDATE(),112), 
		A.mkrtm = CONVERT(VARCHAR,GETDATE(), 108)
		FROM Client_ModifyAttach A
		WHERE Ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	    AND ma_filename = (CASE WHEN  @i_vcFunctionName = 'EsignRequest' THEN 
		'UnSignedPdf' ELSE 'UnSignedKRAPdf' END)
      END 
      SET @strTJsonInput = '{"base64":"##base64##","FileName":"##FileName##","FilePassword": "##FilePassword##"}'
	  SET @strTJsonInput = REPLACE(@strTJsonInput,'##base64##',@strFileData)
	  SET @strTJsonInput = REPLACE(@strTJsonInput,'##FileName##',@strFileName)
	  SET @strTJsonInput = REPLACE(@strTJsonInput,'##FilePassword##',@strFilePassword)
	 -- SELECT @strTJsonInput
	  EXEC stpr_CallThirdPartyAPI 'REKYC', @i_vcVendorCode, 'EsignUploadDocument', '',@strTJsonInput, '', @strThirdPartyURL, @i_vcUserID, @o_vcOutputJsonapi OUTPUT
	  IF @o_vcOutputJsonapi <> ''
	  BEGIN
	    DELETE from @tbl_jsonoutput
	    BEGIN TRY
		
          SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
          EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
          SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
          INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
		  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
          SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE()+@o_vcOutputJsonapi)  
          RETURN 1 		
        END CATCH 
	    IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'id' AND ColumnValue <> '')
	    BEGIN
	      SET @o_vcOutPutJSON = @o_vcOutputJsonapi
		  RETURN 1
	    END
	    ELSE
	    BEGIN
	      SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
          SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
          RETURN 1 
	    END
	  END  
	  ELSE
	  BEGIN
	    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
        RETURN 1 
	  END
	END  
  END	
  ELSE
  IF @i_vcFunctionName IN('CreateSign','CreateSignKRA')               
  BEGIN
    
	DECLARE @stridentifier VARCHAR(20)='', @strdisplayName VARCHAR(100)='', @strbirthYear VARCHAR(20)='',
	@strheight VARCHAR(10)='', @stronPages VARCHAR(20)='', @strposition VARCHAR(30),@strwidth VARCHAR(20)=''
    SELECT @strClientCode = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
    SET @strRefNo = @i_vcRefNo
	SELECT @strRequestid = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='Requestid'
	SELECT @strredirectUrl = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='redirectUrl'
	SELECT @stridentifier = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='identifier'
	SELECT @strdisplayName = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='displayName'
	SELECT @strbirthYear = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='birthYear'
	SELECT @strheight = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='height'
	SELECT @stronPages = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='onPages'
	SELECT @strposition = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='position'
	SELECT @strwidth = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='width'
	
	IF @strClientCode  = '' OR @strRefNo = '' OR @strRequestid = '' OR @stridentifier = ''
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Require Value ClientCode,RefNo,Requestid,MobileNo')  
      RETURN 1 
	END
	
	DECLARE @iPagecounter INT = 1, @ipage INT = @stronPages, @strPagestring VARCHAR(MAX)=''
    WHILE @ipage >= @iPagecounter
    BEGIN
      SET @strPagestring = @strPagestring+','+'"'+CAST(@iPagecounter AS VARCHAR)+'"'
      SET @iPagecounter = @iPagecounter+1
    END
    SELECT @strPagestring = SUBSTRING(@strPagestring,2,LEN(@strPagestring))
    SET @strTJsonInput = '{"documentId": "##Requestid##",  "redirectUrl": "##redirectUrl##",   
    "signers": [{"identifier": "##identifier##",          
    "displayName": "##displayName##",        
    "birthYear": "##birthYear##","signature": {"height": ##height##,"onPages": [ ##onPages##],            
    "position": "##position##",             
    "width": ##width## }}]}'
	SET @strTJsonInput =  REPLACE(@strTJsonInput,'##Requestid##',@strRequestid)
	SET @strTJsonInput =  REPLACE(@strTJsonInput,'##redirectUrl##',@strredirectUrl)
	SET @strTJsonInput =  REPLACE(@strTJsonInput,'##identifier##',@stridentifier)
	SET @strTJsonInput =  REPLACE(@strTJsonInput,'##displayName##',@strdisplayName)
	SET @strTJsonInput =  REPLACE(@strTJsonInput,'##birthYear##',@strbirthYear)
	SET @strTJsonInput =  REPLACE(@strTJsonInput,'##height##',@strheight)
	SET @strTJsonInput =  REPLACE(@strTJsonInput,'##onPages##',@strPagestring)
	SET @strTJsonInput =  REPLACE(@strTJsonInput,'##position##',@strposition)
	SET @strTJsonInput =  REPLACE(@strTJsonInput,'##width##',@strwidth)
	EXEC stpr_CallThirdPartyAPI 'REKYC', @i_vcVendorCode, 'CreateSign', '',@strTJsonInput, '', @strThirdPartyURL, @i_vcUserID, @o_vcOutputJsonapi OUTPUT
	
	IF @o_vcOutputJsonapi <> ''
	BEGIN
	  DELETE from @tbl_jsonoutput
	  BEGIN TRY
        
		SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
        INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE()+@o_vcOutputJsonapi)  
        RETURN 1 		
      END CATCH 
	  IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'url' AND ColumnValue <> '')
	  BEGIN
	    SET @o_vcOutPutJSON = @o_vcOutputJsonapi
		RETURN 1
	  END
	  ELSE
	  BEGIN
	    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
        RETURN 1 
	  END
	END  
	ELSE
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
      RETURN 1 
	END
  END	
  ELSE
  IF @i_vcFunctionName IN('GetEsignDocument','GetEsignDocumentKRA')              
  BEGIN
    SELECT @strClientCode = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
    SET @strRefNo = @i_vcRefNo
	SELECT @strRequestid = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='Requestid'
	
	IF @strClientCode  = '' OR @strRefNo = '' OR @strRequestid = ''
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Require Value ClientCode,RefNo,Requestid')  
      RETURN 1 
	END
	SET @strTJsonInput = '{}'
	EXEC stpr_CallThirdPartyAPI 'REKYC', @i_vcVendorCode, 'GetSignDownload', '',@strTJsonInput, @strRequestid, 
	@strThirdPartyURL, @i_vcUserID, @o_vcOutputJsonapi OUTPUT
	IF @o_vcOutputJsonapi <> ''
	BEGIN
	  DELETE from @tbl_jsonoutput
	  BEGIN TRY
        SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
        INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE()+@o_vcOutputJsonapi)  
        RETURN 1 		
      END CATCH 
	  IF NOT EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'downloadUrl' AND ColumnValue <> '')
	  BEGIN
	    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
        RETURN 1 
	  END
	  ELSE
	  BEGIN
	    DECLARE @strESignDocumenturl VARCHAR(MAX)=''
		select @strESignDocumenturl = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'downloadUrl'
	    
		SET @strxmlFile = '{"fileUrl":"##fileUrl##"}'
		SET @strxmlFile = replace(@strxmlFile,'##fileUrl##',@strESignDocumenturl)
		EXEC stpr_GetURLtoBASE64 @strxmlFile, @strThirdPartyURL, @stroutpuxmlFile OUTPUT
		
				  
		DELETE from @tbl_jsonoutput
		  
		SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@stroutpuxmlFile+''' , @jsonCutterOutput OUTPUT';
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
        INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
		SELECT X1.* FROM(
        SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
	    JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
        JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
		JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
	    JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
	    JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
        FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1  
		  
		  
		IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'status' and ColumnValue = 'true')
		BEGIN
		  SELECT @stroutpuxmlFile = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'data'
		END	

		IF @stroutpuxmlFile <> ''
		BEGIN
		  SET @binaryRepresentation = CAST(CONVERT(VARBINARY(MAX), @stroutpuxmlFile) AS VARBINARY(MAX));

		  IF EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @strClientCode AND ma_refno = @strRefNo
		  AND ((ma_filename = 'SignedPdf' and @i_vcFunctionName = 'GetEsignDocument')
	      OR (ma_filename = 'SignedKRAPdf' and @i_vcFunctionName = 'GetEsignDocumentKRA')))
		  
		  BEGIN
		    UPDATE A SET A.ma_proof = @binaryRepresentation, A.mkrdt = CONVERT(VARCHAR,GETDATE(),112), 
		    A.mkrtm = CONVERT(VARCHAR,GETDATE(), 108)
		    FROM Client_ModifyAttach A
		    WHERE Ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	        AND ma_filename = (CASE WHEN  @i_vcFunctionName = 'GetEsignDocument' 
			THEN 'SignedPdf' ELSE 'SignedKRAPdf' END)
		  END
		  ELSE
		  BEGIN
		    INSERT INTO Client_ModifyAttach(ma_cmcd, ma_date, ma_filename, ma_field, mkrdt, mkrtm, ma_proof, 
		    ma_refno, ma_Nfiller1, ma_status)
		    VALUES(@strClientCode, CONVERT(VARCHAR,GETDATE(),112), (CASE WHEN  @i_vcFunctionName = 'GetEsignDocument' 
			THEN 'SignedPdf' ELSE 'SignedKRAPdf' END), 'Esign Sign PDF Document Output for Request Id',
		    CONVERT(VARCHAR,GETDATE(),112), CONVERT(VARCHAR,GETDATE(), 108),@binaryRepresentation, @strRefNo, 1, 'N')
			
			--- EMAIL SEND
			BEGIN TRY
			DECLARE  @i_vcInputJsonPDF VARCHAR(MAX) = '{"reqestName": "Email","requestObject": {"ToEmailId": "##ToEmailId##","CCEmailId": "##CCEmailId##","BCCEmailId": "##BCCEmailId##",
            "Subject": "##Subject##","Body": "##Body##","Attachment":[{"FileName":"##FileName1##","Base64":"##Base641##"}]}}',
            @o_vcOutputJsonPDF VARCHAR(MAX) = '', @strCLientEmail VARCHAR(10) = @strClientCode

            DECLARE @strBodyText NVARCHAR(MAX)='', @strCCEMailid VARCHAR(100)='', @strBCCEMailid VARCHAR(100)='', 
			@strToEmailid VARCHAR(100)='',@strSubject VARCHAR(MAX)='', @strCompamnyName VARCHAR(100)='', 
			@strFileName1 VARCHAR(100)=''
            SELECT @strBodyText = BodyText, @strCCEMailid = CCEmail, @strBCCEMailid = BCCEmailid , @strSubject = EmailSubject
            FROM tbl_EmailTemplate(NOLOCK) WHERE RefName = 'RekycMakerPDF'

           SELECT @strToEmailid = cm_email 
           FROM Client_Master(NOLOCK) WHERE CM_cD = @strCLientEmail
          
		   --SET @strToEmailid = 'Vaibhavgarg2005@gmail.com'
           
           SET @strFileName1 = @strCLientEmail+'_ReKYC_ApplicationForm.pdf'
           SELECT TOP 1 @strCompamnyName = LTRIM(RTRIM(EM_NAME)) from Entity_master(NOLOCK) 
		   WHERE em_cd =(select min(em_cd) from Entity_master(NOLOCK))
		   
		   DECLARE @StrCompanyCount INT = 0
		   
		   SELECT @StrCompanyCount = ISNULL(SUM(ISNULL(cnt,0)),0) 
           FROM ( SELECT COUNT(0) Cnt From Entity_master(NOLOCK) 
           WHERE em_bse <> 'N' and isNull(em_bclearingno,'') in ('189') 
           UNION ALL 
           SELECT count(0) Cnt From Entity_master(NOLOCK) 
           WHERE em_nse <> 'N' and isNull(em_nclearingno,'') in ('07277')) a
	
	       IF @StrCompanyCount > 0
	       BEGIN
	         SELECT @strCompamnyName= LTRIM(RTRIM(em_Name)) FROM Entity_master(NOLOCK)  
	         WHERE em_cd = 'B'
	       END
		   
           SET @strBodyText = REPLACE(@strBodyText,'<<CompanyName>>',@strCompamnyName)
           IF @strToEmailid <> '' AND @i_vcFunctionName = 'GetEsignDocument' 
           BEGIN 
		   
             SET  @i_vcInputJsonPDF = REPLACE(@i_vcInputJsonPDF,'##ToEmailId##',@strToEmailid)
             SET  @i_vcInputJsonPDF = REPLACE(@i_vcInputJsonPDF,'##CCEmailId##',@strCCEMailid)
             SET  @i_vcInputJsonPDF = REPLACE(@i_vcInputJsonPDF,'##BCCEmailId##',@strBCCEMailid)
             SET  @i_vcInputJsonPDF = REPLACE(@i_vcInputJsonPDF,'##BCCEmailId##',@strBCCEMailid)
             SET  @i_vcInputJsonPDF = REPLACE(@i_vcInputJsonPDF,'##Subject##',@strSubject)
             SET  @i_vcInputJsonPDF = REPLACE(@i_vcInputJsonPDF,'##Body##',@strBodyText)
             SET  @i_vcInputJsonPDF = REPLACE(@i_vcInputJsonPDF,'##FileName1##',@strFileName1)
             SET  @i_vcInputJsonPDF = REPLACE(@i_vcInputJsonPDF,'##Base641##',@stroutpuxmlFile)
			 set @o_vcOutputJsonPDF = ''
			 EXEC stpr_APISendEmail @i_vcInputJsonPDF, 'EMAIL', @strThirdPartyURL, @o_vcOutputJsonPDF OUTPUT

             INSERT INTO tbl_EMailLog(Code, ToMailid, CCMailid, BCCMailid, EmailSubject, EmailBodyText,
			 AttachmentFile1Name, AttachmentFile1, SendDate,Response,RefName)
			 VALUES(@strCLientEmail,@strToEmailid, @strCCEMailid, @strBCCEMailid, @strSubject, @strBodyText, @strFileName1,
			 @stroutpuxmlFile, GETDATE(), isnull(@o_vcOutputJsonPDF,0), 'RekycMakerPDF')
		  end
          end try
		  begin catch
		    SET @o_vcOutPutJSON = error_message()
		  end catch
		  END	
		  SET @o_vcOutPutJSON = @o_vcOutputJsonapi
		  RETURN 1
		END
		ELSE
		BEGIN
		  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
          SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
          RETURN 1 
		END  
	  END  
	END
	ELSE
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
      RETURN 1
	END
  END	
  ELSE
  IF @i_vcFunctionName = 'RPD_Called'              
  BEGIN
    SELECT @strClientCode = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
    SET @strRefNo = @i_vcRefNo	
    IF @strClientCode  = '' OR @strRefNo = ''
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Require Value ClientCode,RefNo')  
      RETURN 1 
	END
	SET @strTJsonInput = '{}'
	EXEC stpr_CallThirdPartyAPI 'REKYC',@i_vcVendorCode, 'CreateRPD', '',@strTJsonInput,'',@strThirdPartyURL, 
	@i_vcUserID, @o_vcOutputJsonapi  OUTPUT	
	IF @o_vcOutputJsonapi <> ''
	BEGIN
	  DELETE from @tbl_jsonoutput
	  BEGIN TRY
        
		SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
        INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue, MasterTag, JSONLEVEL, MASTERLEVEL)
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
		SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE()+@o_vcOutputJsonapi)  
        RETURN 1 		
      END CATCH 
	  IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'shortUrl' AND ColumnValue <> '')
	  BEGIN
	    SET @binaryRepresentation = CAST(CONVERT(VARBINARY(MAX), @o_vcOutputJsonapi) AS VARBINARY(MAX));
		IF EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @strClientCode AND ma_refno = @strRefNo
		AND ma_filename = @i_vcFunctionName)
		BEGIN
		  UPDATE A SET A.ma_proof = @binaryRepresentation, A.mkrdt = CONVERT(VARCHAR,GETDATE(),112), 
		  A.mkrtm = CONVERT(VARCHAR,GETDATE(), 108)
		  FROM Client_ModifyAttach A
		  WHERE Ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	      AND ma_filename = @i_vcFunctionName
		END
		ELSE
		BEGIN
	      INSERT INTO Client_ModifyAttach(ma_cmcd, ma_date, ma_filename, ma_field, mkrdt, mkrtm, ma_proof, 
		  ma_refno, ma_Nfiller1, ma_status)
		  VALUES(@strClientCode, CONVERT(VARCHAR,GETDATE(),112), @i_vcFunctionName, 'Reverse Penny Drop verification data',
		  CONVERT(VARCHAR,GETDATE(),112), CONVERT(VARCHAR,GETDATE(), 108),@binaryRepresentation, @strRefNo, 1, 'N')
		END  
	    SET @o_vcOutPutJSON = @o_vcOutputJsonapi
		RETURN 1
	  END
	  ELSE
	  BEGIN
	    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
        RETURN 1 
	  END
	END  
	ELSE
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
      RETURN 1 
	END
  END	
  ELSE
  IF @i_vcFunctionName = 'RPD_GetResponse'              
  BEGIN
    SET @strRequestid = ''
    SELECT @strClientCode = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
    SET @strRefNo = @i_vcRefNo	
	SELECT @strRequestid = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='Requestid' 
	
    IF @strClientCode  = '' OR @strRefNo = '' OR @strRequestid = ''
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Require Value ClientCode,RefNo,Requestid')  
      RETURN 1 
	END
	
	--- MOCK PAYMENT NEED TO BE COMMENT
    /*  
	SET @strTJsonInput = '{"paymentStatus": "successful"}'
	
	EXEC stpr_CallThirdPartyAPI 'REKYC',@i_vcVendorCode, 'MockPayment', '',@strTJsonInput,@strRequestid,@strThirdPartyURL, 
	@i_vcUserID, @o_vcOutputJsonapi  OUTPUT	
	
	DELETE from @tbl_jsonoutput
	BEGIN TRY
	  SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
      EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
      SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
      INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE()+@o_vcOutputJsonapi)  
      RETURN 1 		
    END CATCH 
    
	--- END MOCK PAYMENT 
	
	--IF EXISTS(SELECT 1 FROM @tbl_InputJSONTable WHERE ColumnName = 'success' AND ColumnValue ='true') 
    */
	
	SET @strTJsonInput = '{}'
	IF @strRequestid <> ''
	BEGIN
	  EXEC stpr_CallThirdPartyAPI 'REKYC',@i_vcVendorCode, 'CreateRPD', 'Y',@strTJsonInput,@strRequestid,@strThirdPartyURL, 
	  @i_vcUserID, @o_vcOutputJsonapi  OUTPUT	
	  IF @o_vcOutputJsonapi <> ''
	  BEGIN
	    DELETE from @tbl_jsonoutput
	    BEGIN TRY
		  
		  SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
          EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
          SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
          INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
		  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
          SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE()+@o_vcOutputJsonapi)  
          RETURN 1 		
        END CATCH 
	    IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'shortUrl' AND ColumnValue <> '')
	    BEGIN
		  DECLARE @strRPDCall1 VARCHAR(MAX)='' 	   
		  
	      SET @strRPDCall1 = (SELECT [ColumnDesc] = case when ColumnName = 'accountType' then '[Account Type]'
	      when ColumnName = 'bankAccountIfsc' then '[Customer Ifsc Code]'
	      when ColumnName = 'bankAccountName' then '[Customer Name]'
	      when ColumnName = 'bankAccountNumber' then '[Customer Bank Account No]'
	      when ColumnName = 'bankAccountType' then '[Customer Bank Account Type]'
	      when ColumnName = 'address' then '[Bank Address]'
	      when ColumnName = 'name' then '[Bank Name]' else ColumnName end, ColumnValue,
		  ColumnMatch = (case when ColumnName = 'bankAccountName' then case when ColumnValue <> 
		  (select cm_name from client_master(nolock) where cm_cd = @strClientCode)
		  THEN 'Mismatch'
		  ELSE 'Matched' END else '' end)
	      FROM @tbl_jsonoutput
		  WHERE ColumnName in('accountType','bankAccountIfsc',
		  'bankAccountName','bankAccountNumber','bankAccountType','address','name') FOR JSON PATH)
		  
		  SET @binaryRepresentation = CAST(CONVERT(VARBINARY(MAX), @strRPDCall1) AS VARBINARY(MAX));
	   
		  IF EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @strClientCode AND ma_refno = @strRefNo
		  AND ma_filename = @i_vcFunctionName)
		  BEGIN
		    UPDATE A SET A.ma_proof = @binaryRepresentation, A.mkrdt = CONVERT(VARCHAR,GETDATE(),112), 
		    A.mkrtm = CONVERT(VARCHAR,GETDATE(), 108)
		    FROM Client_ModifyAttach A
		    WHERE Ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	        AND ma_filename = @i_vcFunctionName
		  END
		  ELSE
		  BEGIN
		    INSERT INTO Client_ModifyAttach(ma_cmcd, ma_date, ma_filename, ma_field, mkrdt, mkrtm, ma_proof, 
		    ma_refno, ma_Nfiller1, ma_status)
		    VALUES(@strClientCode, CONVERT(VARCHAR,GETDATE(),112), @i_vcFunctionName, 'Reverse Penny Drop get response with bank details',
		    CONVERT(VARCHAR,GETDATE(),112), CONVERT(VARCHAR,GETDATE(), 108),@binaryRepresentation, @strRefNo, 1, 'N')
		  END  
	    
		  SET @o_vcOutPutJSON = @o_vcOutputJsonapi
		  RETURN 1
	    END
	    ELSE
	    BEGIN
	      SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
          SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
          RETURN 1 
	    END
	  END  
	  ELSE
	  BEGIN
	    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
        RETURN 1 
	  END
	END
    ELSE
    BEGIN
	 SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
     SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
     RETURN 1 
    END	
  END
  ELSE  
  IF @i_vcFunctionName = 'PENNY-DROP'              
  BEGIN
    DECLARE @strPennyIFSC VARCHAR(20)='', @strPennyAccountNo VARCHAR(50)='', @strNameasPerBank VARCHAR(135)='',
	@strActualClientName VARCHAR(135)=''
    SET @strRequestid = ''
    SELECT @strClientCode = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
	SELECT @strPennyIFSC = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='ifscCode'
	SELECT @strPennyAccountNo = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='AccountNumber'
    SET @strRefNo = @i_vcRefNo	
    
	IF @strClientCode  = '' OR @strRefNo = ''
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Require Value ClientCode,RefNo')  
      RETURN 1 
	END
	
	SET @strTJsonInput = '{"ifsc": "##IFSC##","accountNumber": "##AccountNumber##"}' 
	SET @strTJsonInput = REPLACE(@strTJsonInput,'##IFSC##',@strPennyIFSC)
	SET @strTJsonInput = REPLACE(@strTJsonInput,'##AccountNumber##',@strPennyAccountNo)
	set @o_vcOutputJsonapi = ''
	EXEC stpr_CallThirdPartyAPI 'REKYC',@i_vcVendorCode, 'PENNY-DROP', '',@strTJsonInput,@strRequestid,@strThirdPartyURL, 
	@i_vcUserID, @o_vcOutputJsonapi  OUTPUT	
	
	DELETE from @tbl_jsonoutput
	BEGIN TRY
	  SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
      EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
      SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
      INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE()+@o_vcOutputJsonapi)  
      RETURN 1 		
    END CATCH 
	
	
	IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'verification' and ColumnValue in('success'))
    BEGIN 
	  SELECT @strNameasPerBank = ColumnValue FROM @tbl_jsonoutput WHERE COLUMNNAME = 'name' 
	  SELECT @strActualClientName = cm_name FROM Client_master(NOLOCK) WHERE CM_cD = @strClientCode
	  
	  
	  /*IF @strActualClientName <>  @strNameasPerBank
	  BEGIN
	    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Name Mismatch With Bank, Customer Name in Bank is :- '+@strNameasPerBank)  
        RETURN 1   
	  END
	  */
	  
	  SET @binaryRepresentation = CAST(CONVERT(VARBINARY(MAX), @o_vcOutputJsonapi) AS VARBINARY(MAX)); 
	  IF EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	  AND ma_filename = @i_vcFunctionName)
	  BEGIN
		UPDATE A SET A.ma_proof = @binaryRepresentation, A.mkrdt = CONVERT(VARCHAR,GETDATE(),112), 
		A.mkrtm = CONVERT(VARCHAR,GETDATE(), 108)
		FROM Client_ModifyAttach A
		WHERE Ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	    AND ma_filename = @i_vcFunctionName
	  END
	  ELSE
	  BEGIN
		INSERT INTO Client_ModifyAttach(ma_cmcd, ma_date, ma_filename, ma_field, mkrdt, mkrtm, ma_proof, 
		ma_refno, ma_Nfiller1, ma_status)
		VALUES(@strClientCode, CONVERT(VARCHAR,GETDATE(),112), @i_vcFunctionName, 'Penny Drop get response with bank details',
		CONVERT(VARCHAR,GETDATE(),112), CONVERT(VARCHAR,GETDATE(), 108),@binaryRepresentation, @strRefNo, 1, 'N')
	  END
	  DECLARE @strPennyDropMessage VARCHAR(500)='', @micr VARCHAR(20)=''
	  SELECT @strPennyDropMessage = COLUMNVALUE FROM @tbl_jsonoutput WHERE ColumnName = 'message'
	  SELECT TOP 1 @micr = bk_micr FROM Bank_master(NOLOCK) WHERE  bk_IFCCode = @strPennyIFSC
	  SET @o_vcOutputJsonapi = '{"message":"##MESSAGE##","NameAsPerBank":"##Name##","MICR":"##MICR##"}'
	  SET @o_vcOutputJsonapi = REPLACE(@o_vcOutputJsonapi,'##MESSAGE##',@strPennyDropMessage)
	  SET @o_vcOutputJsonapi = REPLACE(@o_vcOutputJsonapi,'##Name##',@strNameasPerBank)
	  SET @o_vcOutputJsonapi = REPLACE(@o_vcOutputJsonapi,'##MICR##',@micr)
	  
      SET @o_vcOutPutJSON = @o_vcOutputJsonapi	  
	  RETURN 1
	END
	ELSE
    BEGIN
	 SELECT @o_vcOutputJsonapi = COLUMNVALUE FROM @tbl_jsonoutput WHERE ColumnName = 'message' 
	 SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
     SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
     RETURN 1 
    END	
  END	
  ELSE
  IF @i_vcFunctionName = 'DigilockerRequest'              
  BEGIN
    SELECT @strClientCode = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
    SET @strRefNo = @i_vcRefNo
    SELECT @strredirectUrl = Columnvalue FROM @tbl_InputJSONTable WHERE ColumnName='redirectUrl' 
	IF @strredirectUrl = '' OR @strClientCode  = '' OR @strRefNo = ''
	BEGIN
	  SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
      SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Require Value redirectUrl,ClientCode,RefNo')  
      RETURN 1 
	END

	IF @i_vcVendorCode = 'SETU'
	BEGIN
	  SET @strTJsonInput = '{"redirectUrl": "##redirectUrl##"}'
      SET @strTJsonInput = REPLACE(@strTJsonInput,'##redirectUrl##',@strredirectUrl)
	END  
	
	EXEC stpr_CallThirdPartyAPI 'REKYC',@i_vcVendorCode, 'DigilockerRequest', '',@strTJsonInput,'',@strThirdPartyURL, 
	@i_vcUserID, @o_vcOutputJsonapi  OUTPUT	

	IF @o_vcOutputJsonapi <> ''
	BEGIN
	  DELETE from @tbl_jsonoutput
	  BEGIN TRY
        SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcOutputJsonapi+''' , @jsonCutterOutput OUTPUT';
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
        INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
		SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
        SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE()+@o_vcOutputJsonapi)  
        RETURN 1 		
      END CATCH
	  
	  IF @i_vcVendorCode = 'SETU'
	  BEGIN
	    SELECT @strRequestid = ColumnValue FROM @tbl_jsonoutput WHERE COLUMNNAME = 'id' 
	    
	    IF @strRequestid = ''
	    BEGIN
	      SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                             
          SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',@o_vcOutputJsonapi)  
          RETURN 1 		
	    END
	  
	    SET @binaryRepresentation = CAST(CONVERT(VARBINARY(MAX), @o_vcOutputJsonapi) AS VARBINARY(MAX));

		 IF EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @strClientCode AND ma_refno = @strRefNo
		 AND ma_filename = @i_vcFunctionName)
		 BEGIN
		   UPDATE A SET A.ma_proof = @binaryRepresentation, A.mkrdt = CONVERT(VARCHAR,GETDATE(),112), 
		   A.mkrtm = CONVERT(VARCHAR,GETDATE(), 108)
		   FROM Client_ModifyAttach A
		   WHERE Ma_cmcd = @strClientCode AND ma_refno = @strRefNo
	       AND ma_filename = @i_vcFunctionName
		 END
		 ELSE
		 BEGIN
	       INSERT INTO Client_ModifyAttach(ma_cmcd, ma_date, ma_filename, ma_field, mkrdt, mkrtm, ma_proof, 
		   ma_refno, ma_Nfiller1, ma_status)
		   VALUES(@strClientCode, CONVERT(VARCHAR,GETDATE(),112), @i_vcFunctionName, 'DigiLocker Request Output for Request Id',
		   CONVERT(VARCHAR,GETDATE(),112), CONVERT(VARCHAR,GETDATE(), 108),@binaryRepresentation, @strRefNo, 1, 'N')
		 END    
		SET @o_vcOutPutJSON = @o_vcOutputJsonapi
	  END
	END
  END
  ELSE
  BEGIN
    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                                  
    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','No API Found')                           
    RETURN 1
  END
  END TRY                            
  BEGIN CATCH 
    SET @o_vcOutPutJSON ='{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}'                                  
    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##',ERROR_MESSAGE())                           
  END CATCH                                  
END
GO

CREATE PROCEDURE [stpr_CallThirdPartyAPI] @i_vcProjectname VARCHAR(50), 
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
  IF @i_vcAPIVendor in('SARAL','ODIN','CDSL')
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
  --SELECT @RequestJSON, @CallingAPIURLMain
  EXEC sp_OAMethod @Object, 'open', NULL, 'post',@CallingAPIURLMain, 'false' 
  EXEC sp_OAMethod @Object, 'setRequestHeader', null, 'Content-Type', 'application/json'  
  EXEC sp_OAMethod @Object, 'send', null, @RequestJSON  
  INSERT INTO @tbl_OutputResponse (Json_Table) EXEC sp_OAMethod @Object, 'responseText'
  SELECT @VCOUTPUT = Json_Table FROM @tbl_OutputResponse
  SET @o_vcOutputJson = @VCOUTPUT  
 -- SELECT @VCOUTPUT
  EXEC sp_OADestroy @Object  
  
  RETURN 1
END
GO

CREATE PROCEDURE stpr_GetMarkerDataOnlyClosure(@o_vcJsonOutput VARCHAR(MAX) OUTPUT, @i_vcRequestFlag VARCHAR(1) = 'P', @strFromDate VARCHAR(8)='', 
@strToDate VARCHAR(8)= '') 
WITH ENCRYPTION
AS
BEGIN
  DECLARE  @o_vcErrorFlag VARCHAR(1)='', @o_vcErrorMessage VARCHAR(MAX)=''
  
  CREATE TABLE #TBL_ClosureData(ClientCode VARCHAR(20), ClientName VARCHAR(200), LastTradingDate VARCHAR(50),
  TradingLedgerBalance  money, DPLedgerBalance money, DPHoldingValue money, RequestDate VARCHAR(20), RequestTime VARCHAR(20),
  RequestType VARCHAR(50), RequestReason VARCHAR(500), rm_refno numeric, BOID VARCHAR(16), RejectedDate VARCHAR(8))
  
  IF @i_vcRequestFlag = 'P'
  BEGIN
    INSERT INTO #TBL_ClosureData(ClientCode, ClientName, LastTradingDate,
    TradingLedgerBalance, DPLedgerBalance, DPHoldingValue, RequestDate, RequestTime, 
	RequestType, RequestReason, rm_refno, RejectedDate)
	
	SELECT ClientCode = rm_cmcd,   ClientName = cm_name, LastTradingDate = '', 
    TradingLedgerBalance = 0,
    DPLedgerBalance = 0, DPHoldingValue = 0, RequestDate = rm_cdate, RequestTime = rm_ctime,
    RequestType = '', RequestReason = '', rm_refno, RejectedDate = ''
    FROM Client_ReKycMain(NOLOCK) cm, Client_master(NOLOCK) where rm_rekyc = 'N'
    AND rm_status = 'Pending' And rm_RequestType = 'Account Closure' And rm_step IN(5,4) AND 
     rm_cmcd = CM_cD AND EXISTS(SELECT 1 FROM Client_ModifyAPI(NOLOCK)
    WHERE ca_cmcd = CM.rm_cmcd AND ca_Nfiller3 = CM.rm_refno AND ca_field = 'ClosureType'
    AND ca_newValue <> '')
	AND rm_Desc LIKE '%Esign done: pdf docs done with Esign%'
    ORDER BY cm.mkrdt, rm_ctime
	
  END
  ELSE IF @i_vcRequestFlag = 'R'
  BEGIN
    INSERT INTO #TBL_ClosureData(ClientCode, ClientName, LastTradingDate,
    TradingLedgerBalance, DPLedgerBalance, DPHoldingValue, RequestDate, RequestTime, 
	RequestType, RequestReason, rm_refno, RejectedDate)
  
    SELECT ClientCode = rm_cmcd,   ClientName = cm_name, LastTradingDate = '', 
    TradingLedgerBalance = 0,
    DPLedgerBalance = 0, DPHoldingValue = 0, RequestDate = rm_cdate, RequestTime = rm_ctime,
    RequestType ='', RequestReason = rm_desc, rm_refno, RejectedDate =   cm.mkrdt
    FROM Client_ReKycMain(NOLOCK) cm, Client_master(NOLOCK) where rm_rekyc = 'R'
    AND rm_status = 'Reject'
    AND rm_cmcd = CM_cD AND EXISTS(SELECT 1 FROM Client_ModifyAPI(NOLOCK)
    WHERE ca_cmcd = CM.rm_cmcd AND ca_Nfiller3 = CM.rm_refno AND ca_field = 'ClosureType'
    AND ca_newValue <> '') AND rm_cdate >= @strFromDate AND rm_cdate <= @strToDate
  END
  ELSE IF @i_vcRequestFlag = 'A'
  BEGIN
    INSERT INTO #TBL_ClosureData(ClientCode, ClientName, LastTradingDate,
    TradingLedgerBalance, DPLedgerBalance, DPHoldingValue, RequestDate, RequestTime, 
	RequestType, RequestReason, rm_refno, RejectedDate)
  
    SELECT ClientCode = rm_cmcd,   ClientName = cm_name, LastTradingDate ='', 
    TradingLedgerBalance = 0,
    DPLedgerBalance = 0, DPHoldingValue =0, RequestDate = rm_cdate, RequestTime = rm_ctime,
    RequestType = '', RequestReason = rm_desc, rm_refno, RejectedDate = ''
    FROM Client_ReKycMain(NOLOCK) cm, Client_master(NOLOCK) where rm_rekyc = 'Y'
    AND rm_status = 'Approve'
    AND rm_cmcd = CM_cD AND EXISTS(SELECT 1 FROM Client_ModifyAPI(NOLOCK)
    WHERE ca_cmcd = CM.rm_cmcd AND ca_Nfiller3 = CM.rm_refno AND ca_field = 'ClosureType'
    AND ca_newValue <> '')  AND rm_cdate >= @strFromDate AND rm_cdate <= @strToDate
  END
  
   
  SET @o_vcJsonOutput = ''

 
  CREATE TABLE #TBL_JsonCutter(SerialNo INT, ColumnName VARCHAR(50), ColumnValue VARCHAR(MAX), ValueTypeColumn INT,
  UpdateFlag VARCHAR(1), MasterTag VARCHAR(50), JsonLevel INT, MasterLevel INT)

  DECLARE @strtradeplustempdb VARCHAR(50)='', @strString VARCHAR(MAX)=''
  
  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB'

  DECLARE @strClient VARCHAR(50)='', @irefno INT
  DECLARE Cur1 CURSOR FOR 
  SELECT ClientCode, rm_refno from #TBL_ClosureData
  OPEN Cur1 
  FETCH NEXT FROM Cur1 INTO @strClient, @irefno
  WHILE @@FETCH_STATUS = 0
  BEGIN 
     EXEC stpr_ReKyc_ClosureGetData @strClient, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT, @o_vcJsonOutput OUTPUT 
	 IF @o_vcErrorFlag = 'S'
	 BEGIN
	    DELETE FROM #TBL_JsonCutter
		
		DECLARE @jsonCutterOutput VARCHAR(MAX)='', @strDanSql NVARCHAR(MAX)=''
        SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_vcJsonOutput+''' , @jsonCutterOutput OUTPUT';
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        DECLARE @JsonCutterXML XML = CAST(@jsonCutterOutput AS XML)
        INSERT INTO #TBL_JsonCutter(SerialNo, ColumnName, ColumnValue, ValueTypeColumn, MasterTag, JsonLevel, MasterLevel)
		SELECT X1.* FROM(
        SELECT JsonCutter.value('(SerialNo)[1]', 'int') AS SerialNo ,
	    JsonCutter.value('(ColumnName)[1]', 'VARCHAR(1000)') AS ColumnName ,
        JsonCutter.value('(ColumnValue)[1]', 'VARCHAR(max)') AS ColumnValue,
	    JsonCutter.value('(XTYPE)[1]', 'INT') AS ValueTypeColumn,
		JsonCutter.value('(MasterTag)[1]', 'VARCHAR(50)') AS MasterTag,
	    JsonCutter.value('(JsonLevel)[1]', 'INT') AS JsonLevel,
	    JsonCutter.value('(MasterLevel)[1]', 'INT') AS MasterLevel
        FROM @JsonCutterXML.nodes('/JsonCutter') AS XTbl(JsonCutter)) X1
	 END
	 UPDATE A SET A.LastTradingDate = B.ColumnValue
		FROM #TBL_ClosureData A, #TBL_JsonCutter B
		WHERE A.ClientCode = @strClient
		AND ColumnName = 'LastTradedDate'

		UPDATE A SET A.TradingLedgerBalance = CAST(B.ColumnValue AS MONEY)
		FROM #TBL_ClosureData A, #TBL_JsonCutter B
		WHERE A.ClientCode = @strClient
		AND ColumnName = 'TradingLedgerBalance'

		UPDATE A SET A.DPLedgerBalance = CAST(B.ColumnValue AS MONEY)
		FROM #TBL_ClosureData A, #TBL_JsonCutter B
		WHERE A.ClientCode = @strClient
		AND ColumnName = 'DPLedgerBalance'

	    UPDATE A SET A.DPHoldingValue = CAST(B.ColumnValue AS MONEY)
		FROM #TBL_ClosureData A, #TBL_JsonCutter B
		WHERE A.ClientCode = @strClient
		AND ColumnName = 'DPHoldingValue'

		UPDATE A SET A.RequestType = ca_newValue
		FROM #TBL_ClosureData A, Client_ModifyAPI B
		WHERE A.ClientCode = @strClient
		AND A.ClientCode = B.ca_cmcd
		AND B.ca_Nfiller3 = @irefno
		AND B.ca_field = 'ClosureType'
       
	   IF @i_vcRequestFlag IN('P','A')
	   BEGIN
	    UPDATE A SET A.RequestReason = ca_newValue
		FROM #TBL_ClosureData A, Client_ModifyAPI B
		WHERE A.ClientCode = @strClient
		AND A.ClientCode = B.ca_cmcd
		AND B.ca_Nfiller3 = @irefno
		AND B.ca_field = 'cm_freezereason'
       END
	   
		UPDATE A SET A.BOID = ca_newValue
		FROM #TBL_ClosureData A, Client_ModifyAPI B
		WHERE A.ClientCode = @strClient
		AND A.ClientCode = B.ca_cmcd
		AND B.ca_Nfiller3 = @irefno
		AND B.ca_field = 'BOID'

	 FETCH NEXT FROM Cur1 INTO @strClient, @irefno
  END 
  CLOSE Cur1 
  DEALLOCATE Cur1 
  IF EXISTS(SELECT 1 FROM #TBL_ClosureData)
  BEGIN
    SET @o_vcJsonOutput = (SELECT ClientCode, ClientName, LastTradingDate, TradingLedgerBalance, DPLedgerBalance, DPHoldingValue, 
	RequestDate =  REPLACE(CONVERT(VARCHAR,cast(RequestDate as date),106),' ','-'), RequestTime, 
    RequestType = CASE WHEN RequestType = 'T' THEN 'Trading' WHEN RequestType = 'D' THEN 'Demat' 
    WHEN RequestType = 'B' THEN 'Trading+Demat' else '' end, RequestReason, rm_refno, BOID, 
	RejectedDate = case when RejectedDate = '' then '' else REPLACE(CONVERT(VARCHAR,cast(RejectedDate as date),106),' ','-') END 
	FROM #TBL_ClosureData 
    ORDER BY ClientCode FOR JSON PATH)
  END
  ELSE
  BEGIN
    SET @o_vcJsonOutput = (SELECT ClientCode = '', ClientName = '', LastTradingDate = '', TradingLedgerBalance = 0, 
	DPLedgerBalance = 0, DPHoldingValue = 0, RequestDate = '', RequestTime = '',
	RequestType = '', RequestReason = '', rm_refno = '', BOID = '', RejectedDate = '' FOR JSON PATH)
  End
  
  DROP TABLE #TBL_ClosureData
  DROP TABLE #TBL_JsonCutter
END
GO

CREATE   PROCEDURE [stpr_GetURLtoBASE64] @i_vcInputJson VARCHAR(MAX), @CallingAPIURLMain VARCHAR(MAX),
@o_vcOutputJson NVARCHAR(MAX) OUTPUT WITH ENCRYPTION AS
BEGIN
  set @o_vcOutputJson = ''
  
  SET @CallingAPIURLMain = @CallingAPIURLMain+'/api/ThirdPartyService/Base64FromUrl'
  DECLARE @Object AS INT;  
  DECLARE @ResponseText AS VARCHAR(8000)='';  
  Declare @tbl_OutputResponse as table(Json_Table nvarchar(max))
  DECLARE @VCOUTPUT VARCHAR(MAX)=''  
  EXEC sp_OACreate 'MSXML2.XMLHTTP', @Object OUT;  
  
  EXEC sp_OAMethod @Object, 'open', NULL, 'post',@CallingAPIURLMain, 'false' 
  EXEC sp_OAMethod @Object, 'setRequestHeader', null, 'Content-Type', 'application/json'  
  EXEC sp_OAMethod @Object, 'send', null, @i_vcInputJson  
  INSERT INTO @tbl_OutputResponse (Json_Table) EXEC sp_OAMethod @Object, 'responseText'
  SELECT @VCOUTPUT = Json_Table FROM @tbl_OutputResponse
  SET @o_vcOutputJson = @VCOUTPUT  
  EXEC sp_OADestroy @Object  
  
  RETURN 1
END
GO

CREATE PROCEDURE [stpr_ReKyc_ClosureGetData] @i_vcUserCode VARCHAR(20),  @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT,
@o_strJson VARCHAR(MAX) OUTPUT WITH ENCRYPTION AS
BEGIN
  DECLARE @dtFromDate VARCHAR(8)= CONVERT(VARCHAR,GETDATE(),112), @ExchSeg VARCHAR(40)='', 
  @StrCompanyCode VARCHAR(1)= ISNULL((SELECT top 1 LTRIM(RTRIM(em_cd)) from Entity_master(NOLOCK) 
		   WHERE em_cd =(select min(em_cd) from Entity_master(NOLOCK))),'A'), @vcXML nVARCHAR(MAX), @strString Nvarchar(max)='', @xmldata XML , 
  @strLedgerJson VARCHAR(MAX)='', @strCommexConn VARCHAR(MAX)='', @strtradeplustempdb VARCHAR(50)=''
 
  DECLARE @StrCompanyCount INT = 0;
  SELECT @StrCompanyCount = ISNULL(SUM(ISNULL(cnt,0)),0) 
  FROM ( SELECT COUNT(0) Cnt From Entity_master(NOLOCK) 
  WHERE em_bse <> 'N' and isNull(em_bclearingno,'') in ('189') 
  UNION ALL 
  SELECT count(0) Cnt From Entity_master(NOLOCK) 
  WHERE em_nse <> 'N' and isNull(em_nclearingno,'') in ('07277')) a
  IF @StrCompanyCount > 0
  BEGIN
	SET @StrCompanyCode = 'B'
  END
  
  DECLARE @strCMRAttachmentAttach VARBINARY(MAX)
 
  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB'

  SET @vcXML ='<AsOnDate>'+@dtFromDate+'</AsOnDate><ExchSeg>'+@ExchSeg+'</ExchSeg><UserId>'+@i_vcUserCode+'</UserId><AccountType>EM,MTF,CX,CM</AccountType><SelectTag></SelectTag><SelectUsers></SelectUsers><OutputType>X</OutputType><SplFilter></SplFilter><CompanyCode>'+@StrCompanyCode+'</CompanyCode>'

  EXEC DBO.sp_LedgerBalance  @vcXML , @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT
  IF @o_vcErrorFlag = 'E'
  BEGIN
    RETURN 1
  END
  
  SET @xmldata = CAST(@o_vcErrorMessage AS XML)

  DECLARE @i_vcRefNo INT = 0, @strCount1 INT = 0, @strCount2 INT = 0
  SELECT @i_vcRefNo = rm_refno FROM Client_ReKycMain(NOLOCK) 
  WHERE rm_cmcd = @i_vcUserCode AND rm_RequestType = 'Account Closure'
  AND ISNULL(rm_rekyc,'N') = 'N' AND rm_srno IN( SELECT MAX(ISNULL(rm_srno, 0)) 
  FROM Client_ReKycMain(NOLOCK) WHERE rm_cmcd = @i_vcUserCode AND rm_RequestType = 'Account Closure'
  AND ISNULL(rm_rekyc,'N') = 'N')
  
  
  SELECT @strCount1 = COUNT(*) 
  FROM client_ModifyAPI(NOLOCK) WHERE CA_CMCD = @i_vcUserCode AND ca_Nfiller3 = @i_vcRefNo
  AND ISNULL(ca_Tplus,'N') = 'N'
  
  DECLARE @strNewBOID VARCHAR(16)='', @strNewClosureType VARCHAR(1)='', @strNewfreezereason VARCHAR(500)=''
  
  SELECT @strNewBOID = ca_newValue
  FROM client_ModifyAPI(NOLOCK) WHERE CA_CMCD = @i_vcUserCode AND ca_Nfiller3 = @i_vcRefNo
  AND ISNULL(ca_Tplus,'N') = 'N' and ca_field = 'BOID'
  
  SELECT @strNewClosureType = ca_newValue
  FROM client_ModifyAPI(NOLOCK) WHERE CA_CMCD = @i_vcUserCode AND ca_Nfiller3 = @i_vcRefNo
  AND ISNULL(ca_Tplus,'N') = 'N' and ca_field = 'ClosureType'
  
  SELECT @strNewfreezereason = ca_newValue
  FROM client_ModifyAPI(NOLOCK) WHERE CA_CMCD = @i_vcUserCode AND ca_Nfiller3 = @i_vcRefNo
  AND ISNULL(ca_Tplus,'N') = 'N' and ca_field = 'cm_freezereason'
  
  
  SELECT @strCount2 = COUNT(*) FROM Client_ModifyAttach(NOLOCK) 
  WHERE ma_cmcd= @i_vcUserCode and ma_refno =  @i_vcRefNo AND ma_filename ='SignedPdf'
  AND ma_proof IS NOT NULL
  
  SET @o_strJson = (SELECT ClientCode = @i_vcUserCode, ClientName = LTRIM(RTRIM(cm_name)),
  Mobile = LTRIM(RTRIM(cm_Mobile)), Email = LTRIM(RTRIM(cm_Email)),
  [ViewFlag] =  CASE WHEN @strCount1 > 0  THEN 'true' else 'false' end,
  [FINALPDFFlag] =  CASE WHEN @strCount2 > 0  THEN 'true' else 'false' end ,
  [BOID] = ISNULL(@strNewBOID,''),
  [ClosureType] = ISNULL(@strNewClosureType,''),
  [ClosureReason] = ISNULL(@strNewfreezereason,'')
  FROM CLIENT_MASTER(NOLOCK) WHERE CM_CD = @i_vcUserCode FOR JSON PATH )
  DECLARE @tbl_LedgerBalance TABLE(Segment VARCHAR(20), ClientCode VARCHAR(50), LedgerBalance MONEY)
  BEGIN TRY
    INSERT INTO @tbl_LedgerBalance(Segment, ClientCode, LedgerBalance)
    SELECT Segment = 'Trading', X1.* FROM(
    SELECT LedgerBalance.value('(Client_Code)[1]', 'VARCHAR(50)') AS Client_Code ,
    LedgerBalance.value('(LedgerBalance)[1]', 'MONEY') AS LedgerBalance
    FROM @xmldata.nodes('/LedgerBalance') AS XTbl(LedgerBalance)) X1
  END TRY
  BEGIN CATCH
    SET @o_strJson = '{}'
	RETURN 1
  END CATCH

  SELECT @strCommexConn = LTRIM(RTRIM(OP_DataBase)) 
  FROM Other_Products(NOLOCK) WHERE OP_Product = 'Commex' AND OP_Status ='A'
  
  DECLARE @tbl_LastTradingDate table (LastTradingDate VARCHAR(8))
  DECLARE @dtLasttradeDate VARCHAR(8)=''
	

  SET @strString = 'select max(td_dt) from( 
  select isNull(max(td_dt),'''') td_dt from trades where td_clientcd = '''+@i_vcUserCode+'''
  UNION ALL 
  select isNull(max(td_dt),'''') td_dt from trx where td_clientcd = '''+@i_vcUserCode+'''
  UNION ALL 
  select isNull(max(td_dt),'''') td_dt from '+ @strCommexConn + '.DBO.trades where td_clientcd='''+ @i_vcUserCode+''') A '
		
  INSERT INTO @tbl_LastTradingDate(LastTradingDate)
  EXEC(@strString)
  
  DECLARE @defaultDPIds VARCHAR(100)='', @dpdaactno VARCHAR(20)='', @dpType VARCHAR(10)=''
  DECLARE @CrossDB VARCHAR(100)='', @CrossOwner VARCHAR(50)='', @OP_Product VARCHAR(50)=''
  SELECT @defaultDPIds = sp_sysvalue FROM Sysparameter WHERE sp_parmcd = 'POADPIDS'

  DECLARE @TBL_Holding TABLE(ClientCode VARCHAR(20), ClientName VARCHAR(100), ScripName VARCHAR(100), ISIN VARCHAR(20), 
  AccountType VARCHAR(50), Holding MONEY, CloseRate MONEY, HoldingValue MONEY)

  DECLARE Cur3dp
  CURSOR FOR SELECT da_actno, dpType = iif(substring(da_dpid,1,2)='IN','NSDL','CDSL') 
  FROM Dematact(NOLOCK) WHERE da_clientcd = @i_vcUserCode AND da_status = 'A' AND da_defaultyn = 'Y'
  AND da_dpid IN(SELECT VALUE FROM dbo.returntable(@defaultDPIds,','))
  OPEN Cur3dp 
  FETCH NEXT FROM Cur3dp INTO @dpdaactno, @dpType
  WHILE @@FETCH_STATUS = 0
  BEGIN  
    SET @CrossDB = ''
	SET @CrossOwner = ''
	SET @OP_Product = ''
   	SELECT @CrossDB = LTRIM(RTRIM(OP_DataBase)),  @CrossOwner = LTRIM(RTRIM(OP_Owner)), @OP_Product = LTRIM(RTRIM(OP_Product))
    FROM Other_Products(NOLOCK) WHERE OP_Product = IIF(@dpType = 'CDSL','Cross','Estro')
	AND OP_STATUS = 'A'
    
    IF @CrossDB  <> '' AND @dpdaactno <> ''
	BEGIN
	   
	   SET @strString = 'SELECT ld_clientcd, SUM(LD_AMOUNT) As LedgerBalance FROM '+@CrossDB+'.DBO.LEDGER(NOLOCK) WHERE ld_clientcd = '''+@dpdaactno+''''
	   +' GROUP BY ld_clientcd '
	   
	   INSERT INTO @tbl_LedgerBalance(ClientCode, LedgerBalance)
	   EXEC(@strString)
	   UPDATE A SET A.Segment = 'DP'
	   FROM @tbl_LedgerBalance A WHERE CLIENTCODE = @dpdaactno
       
	   SET @vcXML ='<AsOnDate>'+@dtFromDate+'</AsOnDate><UserId>'+@dpdaactno+'</UserId><Product>DP</Product><SelectTag></SelectTag><SelectUsers></SelectUsers><OutputType>X</OutputType><SplFilter></SplFilter><CompanyCode>'+@StrCompanyCode+'</CompanyCode>'
       EXEC stpr_Rpt_HoldingNew @vcXML, @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT 
	   
       SET @xmldata = CAST(@o_vcErrorMessage AS XML)
	   INSERT INTO @TBL_Holding(ClientCode, ClientName, ScripName, ISIN, AccountType, Holding, CloseRate, HoldingValue)
	   SELECT X1.* FROM(
       SELECT DPHolding.value('(ClientCode)[1]', 'VARCHAR(50)') AS ClientCode ,
	   DPHolding.value('(ClientName)[1]', 'VARCHAR(100)') AS ClientName ,
       DPHolding.value('(ScripName)[1]', 'VARCHAR(100)') AS ScripName,
	   DPHolding.value('(ISIN)[1]', 'VARCHAR(100)') AS ISIN,
	   DPHolding.value('(AccountType)[1]', 'VARCHAR(100)') AS AccountType,
	   DPHolding.value('(Holding)[1]', 'MONEY') AS Holding,
	   DPHolding.value('(ClosingPrice)[1]', 'MONEY') AS ClosingPrice,
	   DPHolding.value('(MarketValue)[1]', 'MONEY') AS MarketValue
       FROM @xmldata.nodes('/DPHolding') AS XTbl(DPHolding)) X1

    END
	FETCH NEXT FROM Cur3dp INTO  @dpdaactno, @dpType
  END
  CLOSE Cur3dp
  DEALLOCATE Cur3dp

  SET @o_strJson = REPLACE(REPLACE(@o_strJson,'[',''),']','')  

  DECLARE @strExists INT = 0
  SELECT @strExists = COUNT(*) FROM @tbl_LedgerBalance
  
  IF @strExists > 0
  BEGIN
    SET @strLedgerJson = (SELECT [TradingLedgerBalance] = SUM(IIF(Segment='Trading',LedgerBalance,0)),
    [DPLedgerBalance] = SUM(IIF(Segment='DP',LedgerBalance,0)) FROM @tbl_LedgerBalance  FOR JSON PATH )
  END
  ELSE
  BEGIN
    SET @strLedgerJson = (SELECT [TradingLedgerBalance] = 0,
    [DPLedgerBalance] = 0   FOR JSON PATH )
  END
 
  DECLARE @tbl_fn_json_merge TABLE(JsonValue VARCHAR(MAX))
  SET @strLedgerJson = REPLACE(REPLACE(@strLedgerJson,'[',''),']','')  	
  
    
  /*SET @strString = 'SELECT '+@strtradeplustempdb+'.DBO.FN_json_merge('''+@o_strJson+''','''+@strLedgerJson+''') '
  INSERT INTO @tbl_fn_json_merge(JsonValue)
  EXEC(@strString)
  
  SELECT @o_strJson = JsonValue FROM @tbl_fn_json_merge

  DELETE FROM @tbl_fn_json_merge
  */

  SET @strString = 'EXEC '+@strtradeplustempdb+'.DBO.' + 'SP_json_merge' + ' ''' + @o_strJson + ''', '''+@strLedgerJson+''', @o_jsonoutput OUTPUT';
  EXEC sp_executesql @strString, N'@o_jsonoutput VARCHAR(MAX) OUTPUT', @o_strJson OUTPUT;


  IF EXISTS(SELECT 1 FROM @tbl_LastTradingDate WHERE ISNULL(LastTradingDate,'') <> '')
  BEGIN
    SET @strLedgerJson = (SELECT [LastTradedDate] = ISNULL(LastTradingDate,'') FROM @tbl_LastTradingDate  FOR JSON PATH )
  END
  ELSE
  BEGIN
    SET @strLedgerJson = (SELECT [LastTradedDate] = ''  FOR JSON PATH )
  END
  SET @strLedgerJson = REPLACE(REPLACE(@strLedgerJson,'[',''),']','')  	
   
  /*SET @strString = 'SELECT '+@strtradeplustempdb+'.DBO.fn_json_merge('''+@o_strJson+''','''+@strLedgerJson+''') '

  INSERT INTO @tbl_fn_json_merge(JsonValue)
  EXEC(@strString)
  
  SELECT @o_strJson = JsonValue FROM @tbl_fn_json_merge

  DELETE FROM @tbl_fn_json_merge
  */
  SET @strString = 'EXEC '+@strtradeplustempdb+'.DBO.' + 'SP_json_merge' + ' ''' + @o_strJson + ''', '''+@strLedgerJson+''', @o_jsonoutput OUTPUT';
  EXEC sp_executesql @strString, N'@o_jsonoutput VARCHAR(MAX) OUTPUT', @o_strJson OUTPUT;

  IF EXISTS(SELECT 1 FROM @TBL_Holding)
  BEGIN
    SET @strLedgerJson = (SELECT [DPAcno] = ClientCode, [DPHoldingValue] = SUM(HoldingValue) 
	FROM @TBL_Holding GROUP BY ClientCode ORDER BY ClientCode  FOR JSON PATH )
  END
  ELSE
  BEGIN
    DECLARE @dpno1 VARCHAR(20)=''
    SELECT top 1 @dpno1 = da_actno
    FROM Dematact(NOLOCK) WHERE da_clientcd = @i_vcUserCode AND da_status = 'A' AND da_defaultyn = 'Y'
    AND da_dpid IN(SELECT VALUE FROM dbo.returntable(@defaultDPIds,','))
  
    SET @strLedgerJson = (SELECT DISTINCT [DPAcno] = @dpno1,[DPHoldingValue] = 0.0000  FOR JSON PATH )
	
  END
  SET @strLedgerJson = REPLACE(REPLACE(@strLedgerJson,'[',''),']','')  	
  
  
   /*SET @strString = 'SELECT '+@strtradeplustempdb+'.DBO.fn_json_merge('''+@o_strJson+''','''+@strLedgerJson+''') '

  INSERT INTO @tbl_fn_json_merge(JsonValue)
  EXEC(@strString)
  
  SELECT @o_strJson = JsonValue FROM @tbl_fn_json_merge

  DELETE FROM @tbl_fn_json_merge
  */
  
  SET @strString = 'EXEC '+@strtradeplustempdb+'.DBO.' + 'SP_json_merge' + ' ''' + @o_strJson + ''', '''+@strLedgerJson+''', @o_jsonoutput OUTPUT';
  EXEC sp_executesql @strString, N'@o_jsonoutput VARCHAR(MAX) OUTPUT', @o_strJson OUTPUT;
  PRINT '2'
  
  IF EXISTS(SELECT 1 FROM @TBL_Holding)
  BEGIN
  
    SET @strLedgerJson = (SELECT [ISINCode] = ISIN, [ISINName] = ScripName, [Quantity] = Holding, [Type] = AccountType 
    FROM @TBL_Holding ORDER BY ClientCode  FOR JSON PATH, ROOT('DPHolding') )
  END
  ELSE
  BEGIN
    SET @strLedgerJson = (SELECT [ISINCode] = '', [ISINName] = '', [Quantity] = 0, [Type] = ''  FOR JSON PATH, ROOT('DPHolding') )
  END
  
  /*SET @strString = 'SELECT '+@strtradeplustempdb+'.DBO.fn_json_merge('''+@o_strJson+''','''+@strLedgerJson+''') '
  INSERT INTO @tbl_fn_json_merge(JsonValue)
  EXEC(@strString)
  
  SELECT @o_strJson = JsonValue FROM @tbl_fn_json_merge

  DELETE FROM @tbl_fn_json_merge
  */
  --SELECT @o_strJson, @strLedgerJson
  SET @strString = 'EXEC '+@strtradeplustempdb+'.DBO.' + 'SP_json_merge' + ' ''' + @o_strJson + ''', '''+@strLedgerJson+''', @o_jsonoutput OUTPUT';
  EXEC sp_executesql @strString, N'@o_jsonoutput VARCHAR(MAX) OUTPUT', @o_strJson OUTPUT;
  
  
  
  IF EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) 
  WHERE ma_filename='CMRAttachment' and ma_cmcd = @i_vcUserCode )--and ma_status ='N')
  BEGIN
  
    SELECT TOP 1 @strCMRAttachmentAttach = ma_proof
	FROM  Client_ModifyAttach(NOLOCK) 
    WHERE ma_filename='CMRAttachment' and ma_cmcd = @i_vcUserCode
    /*
    SET @strLedgerJson = (SELECT TOP 1 [Attachment] =
	case when isnull(dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX)))),'') = ''
    then dbo.fnBinaryToBase64(ma_proof) else dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX)))) end
	FROM  Client_ModifyAttach(NOLOCK) 
    WHERE ma_filename='CMRAttachment' and ma_cmcd = @i_vcUserCode --and ma_status ='N'
    ORDER BY ma_srno	DESC
	FOR JSON PATH, ROOT('CMRAttachment'))
	*/
	SET @strLedgerJson = (SELECT TOP 1 [Attachment] = replace(CAST(CAST('' AS XML).value('xs:base64Binary(sql:variable("@strCMRAttachmentAttach"))', 'VARBINARY(MAX)'
      ) AS NVARCHAR(MAX)),'data:application/pdf;base64,','')
	  FROM  Client_ModifyAttach(NOLOCK) 
    WHERE ma_filename='CMRAttachment' and ma_cmcd = @i_vcUserCode --and ma_status ='N'
    ORDER BY ma_srno DESC
	FOR JSON PATH, ROOT('CMRAttachment'))
  END
  ELSE
  BEGIN
    SET @strLedgerJson = (SELECT [Attachment] ='' FOR JSON PATH, ROOT('CMRAttachment'))
  END
  
  SET @strString = 'EXEC '+@strtradeplustempdb+'.DBO.' + 'SP_json_merge' + ' ''' + @o_strJson + ''', '''+@strLedgerJson+''', @o_jsonoutput OUTPUT';
  EXEC sp_executesql @strString, N'@o_jsonoutput VARCHAR(MAX) OUTPUT', @o_strJson OUTPUT;
  
  SET @strLedgerJson = ''
  IF EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) 
  WHERE ma_filename='SignedPdf' and ma_cmcd = @i_vcUserCode )--and ma_status ='N')
  BEGIN
    SET @strLedgerJson = (SELECT TOP 1 [Attachment] =
	case when isnull(dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX)))),'') = ''
    then dbo.fnBinaryToBase64(ma_proof) else dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX)))) end
	FROM  Client_ModifyAttach(NOLOCK) 
    WHERE ma_filename='SignedPdf' and ma_cmcd = @i_vcUserCode --and ma_status ='N'  
	ORDER BY ma_srno	DESC
	FOR JSON PATH, ROOT('SignedPdf'))
  END
  ELSE
  BEGIN
    SET @strLedgerJson = (SELECT [Attachment] ='' FOR JSON PATH, ROOT('SignedPdf'))
  END
  
  SET @strString = 'EXEC '+@strtradeplustempdb+'.DBO.' + 'SP_json_merge' + ' ''' + @o_strJson + ''', '''+@strLedgerJson+''', @o_jsonoutput OUTPUT';
  EXEC sp_executesql @strString, N'@o_jsonoutput VARCHAR(MAX) OUTPUT', @o_strJson OUTPUT;
  
  IF EXISTS(SELECT 1 FROM CLIENT_MASTER(NOLOCK) WHERE CM_cD = @i_vcUserCode AND cm_freezeyn ='A')
  BEGIN
   -- SET @o_strJson = '{}'
	SET @o_vcErrorMessage = 'Client Already freeze for All'
	SET @o_vcErrorFlag = 'E'
	RETURN 1
  END
  SET @o_vcErrorMessage = 'Process Executed'
  SET @o_vcErrorFlag = 'S'
  RETURN 1
END
GO

CREATE PROCEDURE stpr_RekycGetMakerData @i_vcClientCode VARCHAR(20), @i_vcRefNo INT, @o_JsonOutput VARCHAR(MAX) OUTPUT WITH ENCRYPTION AS
BEGIN
  DECLARE @tbl_JsonMatch TABLE(SerialNo INT IDENTITY(1,1), 
  ClientCode VARCHAR(20), ClientName VARCHAR(100), RefNo INT,
  JsonTag VARCHAR(50), JsonKey VARCHAR(50), OldValue VARCHAR(MAX), NewValue VARCHAR(MAX), ca_Nfiller1 INT, 
  ca_filler3 INT, FieldDescp VARCHAR(200))
  set @o_JsonOutput = ''
  
  declare @strtradeplustempdb VARCHAR(50)='', @strString Nvarchar(max)='', @o_JsonOutput1 VARCHAR(MAX)=''
  
  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB'
  
  INSERT INTO @tbl_JsonMatch(ClientCode,  RefNo, JsonTag, JsonKey, OldValue, NewValue, ca_Nfiller1, ca_filler3, FieldDescp)
  SELECT X.ca_cmcd,  X.ca_Nfiller3, ca_filler1 as JsonTag, X1.JsonKey, CASE WHEN x1.FieldName like '%dob' THEN Replace(CONVERT(VARCHAR,CAST(ca_oldValue AS DATE),103),'01/01/1900','')
  ELSE ca_oldValue END, CASE WHEN x1.FieldName like '%dob' THEN Replace(CONVERT(VARCHAR,CAST(ca_newValue AS DATE),103),'01/01/1900','')
  ELSE ca_newValue END, ca_Nfiller1, ca_filler3, x1.FieldDescp
  FROM Client_ModifyAPI(NOLOCK) X left outer join tbl_ReKycAuditColumnMapping_NEW(NOLOCK) X1 on(X.ca_field = X1.FieldName
  AND X.ca_filler1 = X1.MasterJsonTag
  AND TemplateCode = 'Template1')
  WHERE ca_cmcd = @i_vcClientCode and ca_Nfiller3 = @i_vcRefNo
  ORDER BY ca_filler1, ca_Nfiller1, ca_Srno

  UPDATE A SET A.ClientName = CM.cm_name
  FROM @tbl_JsonMatch a, Client_master(nolock) cm
  where a.ClientCode = cm.cm_Cd

  SET @o_JsonOutput1 = (select distinct JSONTAG ,
  [Detail]= (select distinct ca_Nfiller1 as [Rowid] ,
  [Data] = (SELECT ColumnName, OldValue = (CASE WHEN ColumnName IN('Nominee Relation','Nominee Guardian Relation') 
  THEN (CASE WHEN ISNULL(OldValue, '') ='01'  THEN 'Spouse'
		            WHEN ISNULL(OldValue, '') = '02' THEN 'Son'   
		            WHEN ISNULL(OldValue, '') = '03'  THEN 'Daughter' 
		            WHEN ISNULL(OldValue, '') = '04' THEN 'Father'
		            WHEN ISNULL(OldValue, '') = '05' THEN 'Mother'
		            WHEN ISNULL(OldValue, '') = '06' THEN 'Brother'
		            WHEN ISNULL(OldValue, '') = '07' THEN 'Sister'
		            WHEN ISNULL(OldValue, '') = '08' THEN 'Grandson'
		            WHEN ISNULL(OldValue, '') = '09' THEN 'Granddaughter'
		            WHEN ISNULL(OldValue, '') = '10' THEN 'Grandfather'
		            WHEN ISNULL(OldValue, '') = '11' THEN 'Grandmother'
		            WHEN ISNULL(OldValue, '') = '12' THEN 'Not Provided'
		            WHEN ISNULL(OldValue, '') ='13' THEN 'Others' ELSE  ISNULL(OldValue, '') END) 
					WHEN ColumnName IN('Income') then
					(CASE WHEN OldValue = '1' THEN 'Below Rs. 1  Lac' 
					     WHEN OldValue = '2' THEN 'Btw Rs. 1 to Rs. 5 Lacs' 
						 WHEN OldValue = '3' THEN 'Btw Rs. 5 to Rs. 10 Lacs' 
						 WHEN OldValue = '4' THEN 'Btw Rs. 10 to Rs. 25 Lacs' 
						 WHEN OldValue = '5' THEN 'Btw Rs. 25 Lacs to Rs. 1 Crore' 
						 WHEN OldValue = '6' THEN 'More than Rs. 1 Crore' 
						 ELSE OldValue END)   ELSE  OldValue END),
  
  NewValue = (CASE WHEN ColumnName IN('Nominee Relation','Nominee Guardian Relation') 
  THEN (CASE WHEN ISNULL(NewValue, '') ='01'  THEN 'Spouse'
		            WHEN ISNULL(NewValue, '') = '02' THEN 'Son'   
		            WHEN ISNULL(NewValue, '') = '03'  THEN 'Daughter' 
		            WHEN ISNULL(NewValue, '') = '04' THEN 'Father'
		            WHEN ISNULL(NewValue, '') = '05' THEN 'Mother'
		            WHEN ISNULL(NewValue, '') = '06' THEN 'Brother'
		            WHEN ISNULL(NewValue, '') = '07' THEN 'Sister'
		            WHEN ISNULL(NewValue, '') = '08' THEN 'Grandson'
		            WHEN ISNULL(NewValue, '') = '09' THEN 'Granddaughter'
		            WHEN ISNULL(NewValue, '') = '10' THEN 'Grandfather'
		            WHEN ISNULL(NewValue, '') = '11' THEN 'Grandmother'
		            WHEN ISNULL(NewValue, '') = '12' THEN 'Not Provided'
		            WHEN ISNULL(NewValue, '') ='13' THEN 'Others'ELSE  ISNULL(NewValue, '') END) 	
					WHEN ColumnName IN('Income') then
					(CASE WHEN NewValue = '1' THEN 'Below Rs. 1  Lac' 
					     WHEN NewValue = '2' THEN 'Btw Rs. 1 to Rs. 5 Lacs' 
						 WHEN NewValue = '3' THEN 'Btw Rs. 5 to Rs. 10 Lacs' 
						 WHEN NewValue = '4' THEN 'Btw Rs. 10 to Rs. 25 Lacs' 
						 WHEN NewValue = '5' THEN 'Btw Rs. 25 Lacs to Rs. 1 Crore' 
						 WHEN NewValue = '6' THEN 'More than Rs. 1 Crore' 
						 ELSE NewValue END) ELSE  NewValue END)
  FROM(SELECT DISTINCT IIF(FieldDescp='',JsonKey, FieldDescp) As ColumnName, OldValue=IIF(IIF(FieldDescp='',JsonKey, FieldDescp) = 'Segment Details',
  STUFF(replace(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(OldValue,'ABC','BSE-CASH'),'ANC','NSE-CASH'),
  'ANF','NSE-F&O'),'ANK','NSE-FX'),'ABM','BSE-MF'),'ANM','NSE-MF'),'AMF','MCX-Comm'),'ANS','NSE-SLBM'),'BNS','NSE-SLBM'),',,',','),',,',','), 1, 1, ''),
  OldValue),
  NewValue = IIF(IIF(FieldDescp='',JsonKey, FieldDescp) = 'Segment Details',
  STUFF(replace(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(NewValue,'ABC','BSE-CASH'),'ANC','NSE-CASH'),
  'ANF','NSE-F&O'),'ANK','NSE-FX'),'ABM','BSE-MF'),'ANM','NSE-MF'),'AMF','MCX-Comm'),'ANS','NSE-SLBM'),'BNS','NSE-SLBM'),',,',','),',,',','), 1, 1, ''),
  NewValue)
  FROM @tbl_JsonMatch WHERE JsonTag = xdetail.JsonTag and  ca_Nfiller1 = xdetail.ca_Nfiller1 ) XM123
  FOR JSON PATH),
  [Attachment] = (select ma_srno, ma_field = (CASE WHEN ma_field = 'AadhaarAttachment' THEN 'Aadhaar Attachment'
  WHEN ma_field = 'AddressAttachment' THEN 'Address Attachment'
  WHEN ma_field = 'BankAttachment' THEN 'Bank Attachment'
  WHEN ma_field = 'DematAttachment' THEN 'Demat Attachment'
  WHEN ma_field = 'IncomeAttachment' THEN 'Income Attachment'
  WHEN ma_field = 'NomineeAttachment' THEN 'Nominee Attachment'
  WHEN ma_field = 'SegmentAttachment' THEN 'Segment Attachment' ELSE ma_field END),  ma_filename = @i_vcClientCode+'_'+ma_filename, 
  DocFileName = @i_vcClientCode+'_'+ma_filename/*,
  ma_proof = dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX))))*/
  FROM  Client_ModifyAttach(NOLOCK) WHERE  ma_cmcd = @i_vcClientCode AND ma_refno = @i_vcRefNo
  and ma_field like '%Attachment' 
  AND ((ma_Nfiller1 = xdetail.ca_Nfiller1  and XMAIN.JsonTag not in('PersonalDetails','SegmentDetails','DisplaySegment','BankAttachment')) or  
  XMAIN.JsonTag in('PersonalDetails','SegmentDetails','DisplaySegment','BankAttachment'))
  AND ((ma_field IN('NomineeAttachment') and XMAIN.JsonTag = 'NomineeDetails' )
  or (XMAIN.JsonTag = 'PersonalDetails'  and ma_field in('AddressAttachment','IdAttachment','IncomeAttachment'))
  or (XMAIN.JsonTag = 'SegmentDetails' and ma_field in('SegmentAttachment'))
  or (XMAIN.JsonTag = 'DematDetails' and ma_field in('DematAttachment'))
  or (XMAIN.JsonTag = 'BankDetails' and ma_field in('BankAttachment'))) FOR JSON PATH)
  FROM @tbl_JsonMatch  xdetail where JsonTag =  XMAIN.JsonTag FOR JSON PATH)
  FROM @tbl_JsonMatch XMAIN  FOR JSON PATH)
  
 
  DECLARE @mainjson NVARCHAR(MAX)= ''
  
  SET @mainjson = (SELECT DISTINCT ClientCode, ClientName, RefNo From @tbl_JsonMatch FOR JSON PATH)
  
  SET @mainjson = substring(@mainjson, 2, len(@mainjson) - 2)
  
  
  SET @strString = 'EXEC '+@strtradeplustempdb+'.DBO.' + 'SP_json_merge' + ' ''' + @o_JsonOutput1 + ''', '''+@mainjson+''', @o_jsonoutput OUTPUT';
  EXEC sp_executesql @strString, N'@o_jsonoutput VARCHAR(MAX) OUTPUT', @mainjson OUTPUT;
  
   
  DECLARE @Attch NVARCHAR(MAX)=''
  IF EXISTS(SELECT 1 FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @i_vcClientCode AND ma_refno = @i_vcRefNo 
  and ma_filename in('Sign','RPD_GetResponse','SignedPdf','IPV_FINAL','SignedKRAPdf') )
  BEGIN
    SET @Attch = (SELECT ma_srno, FileDescp = case when ma_filename = 'RPD_GetResponse' then 'BankDetailFile'
    when ma_filename = 'SignedPdf' then 'Final Signed Document'
    when ma_filename = 'UnSignedPdf' then 'Customer UnSign PDF' 
    when ma_filename = 'Sign' then 'Signature Proof' when ma_filename = 'IPV_FINAL' then 'IPV Image' ELSE ma_filename END,  
	DocFileName = case when ma_filename = 'RPD_GetResponse' then @i_vcClientCode+'_'+'BankDetailFile'
    when ma_filename = 'SignedPdf' then @i_vcClientCode+'_'+'Final Signed Document'
    when ma_filename = 'UnSignedPdf' then @i_vcClientCode+'_'+'Customer UnSign PDF' 
    when ma_filename = 'Sign' then @i_vcClientCode+'_'+'Signature Proof'  
	when ma_filename = 'IPV_FINAL' then @i_vcClientCode+'_'+'IPV Image' 
	when ma_filename = 'SignedKRAPdf' then @i_vcClientCode+'_'+'SignedKRAPdf' ELSE @i_vcClientCode+'_'+ma_filename END/*,
    ma_proof = case when isnull(dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX)))),'') = ''
    then dbo.fnBinaryToBase64(ma_proof) else dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX)))) end */
    FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @i_vcClientCode AND ma_refno = @i_vcRefNo 
    and ma_filename in('Sign','RPD_GetResponse','SignedPdf','IPV_FINAL','SignedKRAPdf') 
    FOR JSON PATH, root('Attachment2'))
  End
  ELSE
  BEGIN
    SET @Attch = (SELECT FileDescp = '', ma_proof = ''  FOR JSON PATH, root('Attachment2'))
  END
  
  --SET @Attch = '['+@Attch+']'
  --SELECT @mainjson, @Attch
  SET @strString = 'EXEC '+@strtradeplustempdb+'.DBO.' + 'SP_json_merge' + ' ''' + @mainjson + ''', '''+@Attch+''', @o_jsonoutput OUTPUT';
  --SELECT @strString
  EXEC sp_executesql @strString, N'@o_jsonoutput VARCHAR(MAX) OUTPUT', @mainjson OUTPUT;
  
  SET @o_jsonoutput = @mainjson
  RETURN 1
END
GO

CREATE PROCEDURE [dbo].[stpr_GetAdditionalDetailRekyc] @strClientCode VARCHAR(20), @o_vcOutput VARCHAR(MAX) OUTPUT WITH ENCRYPTION AS
BEGIN
  IF EXISTS(sELECT 1 FROM client_master(NOLOCK) WHERE CM_CD =  @strClientCode)
  BEGIN
    DECLARE @companyName varchar(100)='', @CompanyAddress varchar(100)='', @DPID varchar(8)='', @ClientId varchar(8)=''
	
	IF NOT EXISTS (select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME=N'Entity_Master') 
	BEGIN  
	  SELECT @companyName = ltrim(rtrim(sp_sysvalue)) from sysparameter with (nolock) where sp_parmcd = 'NAME' 
	END 
	ELSE 
	BEGIN  
      SELECT @companyName = ltrim(rtrim(em_Name))   from Entity_master where em_cd = (select min(em_cd) from Entity_master)  
    END 
	
	DECLARE @StrCompanyCount INT = 0
	SELECT @StrCompanyCount = ISNULL(SUM(ISNULL(cnt,0)),0) 
    FROM ( SELECT COUNT(0) Cnt From Entity_master(NOLOCK) 
    WHERE em_bse <> 'N' and isNull(em_bclearingno,'') in ('189') 
    UNION ALL 
    select count(0) Cnt From Entity_master(NOLOCK) 
    Where em_nse <> 'N' and isNull(em_nclearingno,'') in ('07277')) a
	
	IF @StrCompanyCount > 0
	Begin
	 SELECT @companyName = LTRIM(RTRIM(em_Name)) FROM Entity_master(NOLOCK)  
	 WHERE em_cd  ='B'
	END
	
	Set @CompanyAddress = ''
    Select @DPID = Left(da_dpid,8), @ClientId = RIGHT(da_actno,8)   
	FROM Dematact(NOLOCK) 
	WHERE da_clientcd = @strClientCode 
						and da_status = 'A' and da_defaultyn = 'Y'

    SELECT PerAddress1 = cm_padd1, PerAddress2 = cm_padd2, PerAddress3 = cm_padd3, PerCity = cm_padd4, PerState = cm_pstate, 
	PerCountry = cm_pcountry, PerPincode = cm_ppincode,
	CKYCNumber = ISNULL(Ck_Nfiller1, 0), CKYCDate = ISNULL(ckyc.mkrdt, ''), CKYCReffNo = ISNULL(Ck_Reference, ''), 
    KRAStatus = ISNULL((SELECT ISNULL(cn_KRAStatus, '') FROM Client_Nominee(NOLOCK) WHERE cn_cd = X.CM_cD),''),
    [KYCVerificatioName] = UM.um_user_name, 
    [KYCVerificationDesig] = um_designation,
    [KYCVerificationBranch] = ISNULL((SELECT sp_sysvalue FROM Sysparameter(NOLOCK)  WHERE sp_parmcd='CKYCBRANCHCD'),''),
    [KYCVerificationEmpCode] = um_empCode, 
    [KRACOMPNAME] = ISNULL((SELECT sp_sysvalue FROM Sysparameter(NOLOCK)  WHERE sp_parmcd='CKYCCOMPNAME'),''),
    [OrganisationCode] = ISNULL((SELECT sp_sysvalue FROM Sysparameter(NOLOCK)  WHERE sp_parmcd='CKYCFINO'),''),
    [PlaceOfDeclaration] =ISNULL((SELECT sp_sysvalue FROM Sysparameter(NOLOCK)  WHERE sp_parmcd='PlaceOfKRA'),'HeadOffice'),
	CK_FatherPrefix FatherPrefix, CK_MotherPrefix MotherPrefix, (CK_Motherfname + ' ' + CK_MotherMname + ' '+ CK_MotherLname) MotherFName,
	CK_MaidenPrefix MaidernPrefix, (CK_MaidenFname+ ' '+CK_MaidenMName+' '+CK_MaidenLname) MaidenFname,
	(SELECT Top 1 img_logo From Images where img_desc='Company Logo') as CompanyLogo,@companyName as CompanyName,
	@CompanyAddress as CompanyAddress, @DPID as DPID, @ClientId as ClientId
    FROM client_master(NOLOCK) X 
    LEFT OUTER JOIN (SELECT * FROM Client_CKYC(NOLOCK) CXXX WHERE CK_SRNO IN(SELECT MAX(CK_SRNO)
    FROM Client_CKYC WHERE CK_Panno = CXXX.CK_Panno)) ckyc 
    ON (x.cm_panno = ckyc.CK_Panno) 
    , CLIENT_INFO(NOLOCK) y, (SELECT um_user_name, um_designation, um_empCode FROM User_master(NOLOCK) 
    WHERE um_user_id IN(SELECT sp_sysvalue FROM Sysparameter(NOLOCK)  WHERE sp_parmcd = 'CKYCVERIFYBY')) UM 
    WHERE x.cm_cd = y.cm2_cd
    AND X.CM_CD = @strClientCode 
    RETURN 1 
  END

END
GO

CREATE PROCEDURE [dbo].[SP_AccountClosurePDF]  @dsXml AS XML  WITH ENCRYPTION
AS
BEGIN
 DECLARE @CrossDB VARCHAR(100)='', @CrossOwner VARCHAR(50)='', @OP_Product VARCHAR(50)='' , @strString Nvarchar(max)=''
  /*
   --SET ARITHABORT ON
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
   */
   
   DECLARE @strClientCode VARCHAR(10) = '', @strRefNo VARCHAR(50) = '0',  @strHoldingBal VARCHAR(50) = '0',@strAcType varchar(20)='D', 
			@strFormHeading varchar (100) = ''
  
   SELECT @strClientCode = ISNULL(Parameter.value('(ClientCode)[1]', 'VARCHAR(MAX)'),''),  
   @strAcType = ISNULL(Parameter.value('(AccountType)[1]', 'VARCHAR(MAX)'),'D'),  
   @strHoldingBal = ISNULL(Parameter.value('(HoldingBal)[1]', 'VARCHAR(MAX)'),'0')
   FROM @dsXml.nodes('/dsXml/X_Data') AS XTbl(Parameter)  

  
  -- SELECT @strClientCode = Isnull(ParameterValue,'')  From @tb_ParamListDetail    WHERE ParameterName = 'ClientCode' AND HeaderName = 'X_Data'  
  -- SELECT @strHoldingBal = Isnull(ParameterValue,0)  From @tb_ParamListDetail    WHERE ParameterName = 'HoldingBal' AND HeaderName = 'X_Data' 
  -- SELECT @strAcType = Isnull(ParameterValue,0)  From @tb_ParamListDetail    WHERE ParameterName = 'AccountType' AND HeaderName = 'X_Data' 

   SELECT @CrossDB = LTRIM(RTRIM(OP_DataBase)),  @CrossOwner = LTRIM(RTRIM(OP_Owner)), @OP_Product = LTRIM(RTRIM(OP_Product))  
		FROM Other_Products(NOLOCK) WHERE OP_Product = 'Cross' AND OP_STATUS = 'A'  

   Select @strFormHeading = Case when @strAcType='T' Then 'Account Closure Request Form of Trading Account' 
							  When @strAcType='B' Then 'Account Closure Request Form of Trading and Demat Account' 
												  Else 'Account Closure Request Form of Demat Account' End

   SET @strString  =  ' SELECT cm_name FirstHolderName, '''' SecondHolderName, '''' ThirdHolderName, Left(cm_cd,8) DPID, Right(cm_cd,8) ClientId, 
		(cm_add1 + '' '' + cm_add2 + '' '' + cm_add3) HolderAddress, cm_city City, cm_state State, cm_pin PinCode, RA.ca_newValue ClosingReason, Round((' + @strHoldingBal + '),2)  RemainingBal, 
		(Select sp_sysvalue From '+@CrossDB+'.'+@CrossOwner+'.Sysparameter where sp_parmcd = ''NAME'') as CompanyName, rm_RefNo  RefNo , 
		STUFF((Select '' '' + sp_sysvalue From '+@CrossDB+'.'+@CrossOwner+'.Sysparameter Where sp_parmcd IN (''ADD1'',''ADD2'',''ADD3'') FOR XML PATH ('''')), 1, 1, '''')  as CompanyFullAddress ,
		cm_mobile MobileNo, cm_email Email, cm_name FullName, cm_cd UserId, Left(cm_dateofbirth,4) BirthYear ,
		''' + @strFormHeading + ''' as FormHeading
	From Client_ReKycMain RM Inner Join Client_ModifyAPI RA ON RM.rm_cmcd = RA.ca_cmcd And RM.rm_refno = RA.ca_Nfiller3 
		Inner join '+@CrossDB+'.'+@CrossOwner+'.Client_master CM ON RA.ca_cmcd = CM.cm_blsavingcd 
	Where RM.rm_RequestType = ''Account Closure'' And RM.rm_rekyc = ''N'' And RA.ca_field = ''cm_freezereason'' And RM.rm_cmcd = '''+ @strClientCode +'''  '


   Exec (@strString)
END
GO

CREATE PROCEDURE [stpr_APIReKycExport]
  @i_vcProjectName VARCHAR(50), @i_vcModuleName VARCHAR(100),                                   
  @i_vcFunctionName VARCHAR(100), @i_vcSource VARCHAR(1),                                 
  @i_vcUniqueID VARCHAR(50), @i_vcUserID VARCHAR(50),                                   
  @i_vcInputJSON NVARCHAR(MAX) , @o_vcOutPutJSON NVARCHAR(MAX) OUTPUT                                 
WITH ENCRYPTION
AS                                  
BEGIN

  DECLARE @vc_OutputJSON VARCHAR(MAX) = '', @vc_RefNo VARCHAR(25) = '', @o_vcErrorMessage VARCHAR(MAX)=''

  DECLARE @iAPISerialNo INT = 0,  @iCount INT = 0, @strResponseText VARCHAR(MAX)='', 
  @strStatus VARCHAR(1)='N', @string VARCHAR(MAX)='', @strtradeplustempdb varchar(50)=''
  
  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB'

  DECLARE @tbl_InputJSONTable TABLE (SerialNo INT, ColumnName VARCHAR(100), ColumnValue NVARCHAR(MAX),                             
   ValueTypeColumn INT,ImageFlag VARCHAR(1)) 
   
  SELECT @iCount = ISNULL(COUNT(*),0) FROM tbl_GenericAPIDefinition(NOLOCK)                                 
  WHERE ProjectName = @i_vcProjectName AND ModuleName = @i_vcModuleName                             
  AND FunctionName = @i_vcFunctionName                                
  
  IF @iCount <> 1                            
  BEGIN                            
    SET @o_vcOutPutJSON ='[{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}]'                                  
    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Invalid Request Definition')                                  
    RETURN 1                                   
  END
  
  BEGIN TRY
    SET @string = 'SELECT * FROM '+@strtradeplustempdb+'.DBO.fn_ParentJSONSplit('''+@i_vcInputJSON+''') '                      
	INSERT INTO @tbl_InputJSONTable                                   
	EXEC(@string)
  END  TRY                
  BEGIN CATCH                                  
    SET @o_vcOutPutJSON ='[{"ResponseFlag":"E","ResponseMessage":"##ErrorMessage##"}]'                             
    SET @o_vcOutPutJSON = REPLACE(@o_vcOutPutJSON,'##ErrorMessage##','Input JSON '+ERROR_MESSAGE())                                  
    RETURN 1                                   
  END CATCH
  DECLARE @jsonExport1 VARCHAR(MAX)='', @strClient VARCHAR(500)='', @strOptions1 VARCHAR(MAX)=''
  
  SELECT @strOptions1 = SP_SYSVALUE FROM WebParameter(NOLOCK) 
  WHERE sp_parmcd ='APIEXPORT'

  IF @i_vcProjectName = 'TradeWebAPI' AND @i_vcModuleName = 'ReKYC'         
  BEGIN  
    DECLARE @strExportType VARCHAR(100)=''
    SELECT @strExportType = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='ExportType'
	IF @i_vcFunctionName = 'GetImageFile'
	BEGIN
	  DECLARE @strEClientCode VARCHAR(50)='', @strERefNo INT = 0, @strEma_srno INT 
      SELECT top 1 @strEClientCode = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'  
	  SELECT top 1 @strERefNo = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='RefNo'  
	  SELECT top 1 @strEma_srno = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='ma_srno'  
	  SET @jsonExport1 = (SELECT ma_proof = case when isnull(dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX)))),'') = ''
      then dbo.fnBinaryToBase64(ma_proof) else dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX)))) end
	  FROM Client_ModifyAttach(NOLOCK) WHERE ma_cmcd = @strEClientCode AND ma_refno = @strERefNo AND ma_srno = @strEma_srno
	  FOR JSON Path)
	  SET @o_vcErrorMessage = 'Process Executed'
	  SET @o_vcOutPutJSON='[{"Status":"Y","Remark":"' + @o_vcErrorMessage + '","Data":' + @jsonExport1 + '}]'
      RETURN 1 
    END
	
    IF  @i_vcFunctionName = 'GetExportOptions'
	BEGIN
	   SET @jsonExport1 = (SELECT CASE WHEN APIVendorName = 'CVLKRA' AND APIName = 'InsertUpdateKYCRecord' 
	   THEN 'CVL-KRA (Modification)' WHEN APIVendorName = 'CVLKRA' THEN   'CKYCFileDownload' ELSE APIVendorName+' - '+APIName END
	   as [DisplayValue], APIVendorName+' - '+APIName as [FieldValue] 
	   FROM tbl_VendorAPISetting(NOLOCK)
       WHERE APIVendorName+' - '+APIName IN(SELECT VALUE FROM DBO.ReturnTable(@strOptions1,'|'))
       ORDER BY APIVendorName, APIName  FOR JSON Path)
	   SET @o_vcErrorMessage = 'Process Executed'
	   SET @o_vcOutPutJSON='[{"Status":"Y","Remark":"' + @o_vcErrorMessage + '","Data":' + @jsonExport1 + '}]'
       RETURN 1
	END
	DECLARE @vcXML NVARCHAR(MAX) ='',@o_vcErrorFlag VARCHAR(1) = '', @TotalCount INT = 0, @SucessCount INT = 0, @rejectedCount INT=0
	IF  @i_vcFunctionName = 'UpdateAPI'
	BEGIN
	  SELECT @strClient = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
	  IF  @strExportType = 'CVLKRA - InsertUpdateKYCRecord'
	  BEGIN
	    SET @jsonExport1 = ''
		
		DECLARE @tbl_Clients TABLE(ClientCode VARCHAR(50))
		INSERT INTO @tbl_Clients
		SELECT * FROM DBO.ReturnTable(@strClient,'|')
		DECLARE @ClientCode VARCHAR(50)=''
		DECLARE Cur0 CURSOR FOR 
        SELECT * from @tbl_Clients
        OPEN Cur0
        FETCH NEXT FROM Cur0 INTO @ClientCode
        WHILE @@FETCH_STATUS = 0
        BEGIN
		  SET @TotalCount = @TotalCount+1
		  SET @vcXML ='<root><ClientCode>'+@ClientCode+'</ClientCode></root>'
          EXEC stpr_APICVLKRA @vcXML , @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT
	      IF ISNULL(@o_vcErrorFlag,'') <> 'S'
		  BEGIN
		    SET @rejectedCount = @rejectedCount + 1
		  END
		  ELSE IF ISNULL(@o_vcErrorFlag,'') = 'S'
		  BEGIN
		    SET @SucessCount = @SucessCount + 1
		  END
          FETCH NEXT FROM Cur0 INTO @ClientCode
        END 
        CLOSE Cur0
        DEALLOCATE Cur0
		SET @o_vcErrorMessage ='Total = '+CAST(@TotalCount AS VARCHAR)+'<br> Success = '+CAST(@SucessCount AS VARCHAR)+'<br> Rejected = '+CAST(@rejectedCount AS VARCHAR)
		
        SET @o_vcOutPutJSON='[{"Status":"##STATUS##","Remark":"' + @o_vcErrorMessage + '"}]'
		SET @o_vcOutPutJSON=REPLACE(@o_vcOutPutJSON,'##STATUS##','S')

        RETURN 1 
	  END
	  ELSE 
	  IF  @strExportType = 'CKYC - CKYCFileDownload'
	  BEGIN
	    SET @vcXML ='<root><ClientCode>'+@strClient+'</ClientCode></root>'
        EXEC stpr_APICKYCFILEGEN @vcXML , @o_vcErrorFlag  OUTPUT, @o_vcErrorMessage  OUTPUT
        
		SELECT @TotalCount = COUNT(*) FROM DBO.ReturnTable(@strClient,'|')
		
		/*IF @o_vcErrorFlag = 'S'
		BEGIN
		  SET @o_vcErrorMessage ='Total = '+CAST(@TotalCount AS VARCHAR)+' Success Exported'
		END  */
        SET @o_vcOutPutJSON='[{"Status":"##STATUS##","Remark":[' + @o_vcErrorMessage + ']}]'
		SET @o_vcOutPutJSON=REPLACE(@o_vcOutPutJSON,'##STATUS##',@o_vcErrorFlag)
		--SET @o_vcOutPutJSON = @o_vcErrorMessage
		RETURN 1
	  END
	END
	
	IF  @i_vcFunctionName = 'UpdateReGenerateStatus'
	BEGIN
	  SELECT @strClient = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
	  IF  @strExportType = 'CVLKRA - InsertUpdateKYCRecord'
	  BEGIN
	    SET @jsonExport1 = ''
		DECLARE @strSrNo INT=0, @strCM_PANNO VARCHAR(20)=''
		DECLARE @tbl_Clients1 TABLE(ClientCode VARCHAR(50))
		INSERT INTO @tbl_Clients1
		SELECT * FROM DBO.ReturnTable(@strClient,'|')
		DECLARE @ClientCode1 VARCHAR(50)=''
		DECLARE Cur1 CURSOR FOR 
        SELECT * from @tbl_Clients1
        OPEN Cur1
        FETCH NEXT FROM Cur1 INTO @ClientCode1
        WHILE @@FETCH_STATUS = 0
        BEGIN
		  
          SET @TotalCount = @TotalCount+1
		  SET @vcXML ='<root><ClientCode>'+@ClientCode1+'</ClientCode></root>'
          EXEC stpr_APICVLKRA @vcXML , @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT
	      IF ISNULL(@o_vcErrorFlag,'') <> 'S'
		  BEGIN
		    SET @rejectedCount = @rejectedCount + 1
		  END
		  ELSE IF ISNULL(@o_vcErrorFlag,'') = 'S'
		  BEGIN
		    SET @SucessCount = @SucessCount + 1
		  END
		  SET @strSrNo = 0
		  SET @strCM_PANNO = ''
		  
		  --SELECT @strSrNo = ck_srno, @strCM_PANNO = CM_PANNO
		  --FROM Client_Master, Client_Info, Client_CKYC a
          --WHERE cm_cd = cm2_cd AND cm_panno = CK_Panno 
	      --AND /*isnull(Ck_NFiller2, 0) > 0 
	      --AND */cm_freezeyn = 'N' 
	      --AND Ck_Status = 'S' 
	      --AND CK_SRNO IN(SELECT MAX(CK_SRNO) FROM Client_CKYC WHERE CK_Panno = CM_PANNO)
		  --AND CM_cD = @ClientCode1
		  
		  --UPDATE A SET A.Ck_NFiller2 = 0
          --FROM Client_CKYC A
          --WHERE A.ck_srno = @strSrNo
          --AND A.CK_Panno =  @strCM_PANNO  
		  
          FETCH NEXT FROM Cur1 INTO @ClientCode1
        END 
        CLOSE Cur1
        DEALLOCATE Cur1
		SET @o_vcErrorMessage ='Total = '+CAST(@TotalCount AS VARCHAR)+'<br> Success = '+CAST(@SucessCount AS VARCHAR)+'<br> Rejected = '+CAST(@rejectedCount AS VARCHAR)
		
        SET @o_vcOutPutJSON='[{"Status":"##STATUS##","Remark":"' + @o_vcErrorMessage + '"}]'
		SET @o_vcOutPutJSON=REPLACE(@o_vcOutPutJSON,'##STATUS##','S')
		--SET @o_vcErrorMessage ='Update Status as Pending'
		
        --SET @o_vcOutPutJSON='[{"Status":"##STATUS##","Remark":"' + @o_vcErrorMessage + '"}]'
		--SET @o_vcOutPutJSON=REPLACE(@o_vcOutPutJSON,'##STATUS##','S')

        RETURN 1 
	  END
	  ELSE 
	  IF  @strExportType = 'CKYC - CKYCFileDownload'
	  BEGIN
	    SET @vcXML ='<root><ClientCode>'+@strClient+'</ClientCode></root>'
        EXEC stpr_APICKYCFILEGEN @vcXML , @o_vcErrorFlag  OUTPUT, @o_vcErrorMessage  OUTPUT
        
		SELECT @TotalCount = COUNT(*) FROM DBO.ReturnTable(@strClient,'|')
		
		/*IF @o_vcErrorFlag = 'S'
		BEGIN
		  SET @o_vcErrorMessage ='Total = '+CAST(@TotalCount AS VARCHAR)+' Success Exported'
		END  */
        SET @o_vcOutPutJSON='[{"Status":"##STATUS##","Remark":[' + @o_vcErrorMessage + ']}]'
		SET @o_vcOutPutJSON=REPLACE(@o_vcOutPutJSON,'##STATUS##',@o_vcErrorFlag)
		--SET @o_vcOutPutJSON = @o_vcErrorMessage
		RETURN 1
	  END
	END
	IF  @i_vcFunctionName = 'UpdateSubmittedStatus'
	BEGIN
	  SELECT @strClient = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='ClientCode'
	  
	  IF @strExportType = 'CVLKRA - InsertUpdateKYCRecord'
	  BEGIN
	    SET @jsonExport1 = ''
		set @SucessCount = 0
		set @rejectedCount = 0
	    DECLARE @tbl_ClientSub TABLE(ClientCode VARCHAR(50))
		
		INSERT INTO @tbl_ClientSub
		SELECT * FROM DBO.ReturnTable(@strClient,'|')
		DECLARE @SClientCode VARCHAR(50)=''
		DECLARE Cur0 CURSOR FOR 
        SELECT * from @tbl_ClientSub
        OPEN Cur0
        FETCH NEXT FROM Cur0 INTO @SClientCode
        WHILE @@FETCH_STATUS = 0
        BEGIN
		  SET @TotalCount = @TotalCount+1
		  SET @vcXML ='<root><ClientCode>'+@SClientCode+'</ClientCode></root>'
          EXEC stpr_APICVLKRAStatusUpdate @vcXML , @o_vcErrorFlag OUTPUT, @o_vcErrorMessage OUTPUT
	      IF ISNULL(@o_vcErrorFlag,'') <> 'S'
		  BEGIN
		    SET @rejectedCount = @rejectedCount + 1
		  END
		  ELSE IF ISNULL(@o_vcErrorFlag,'') = 'S'
		  BEGIN
		    SET @SucessCount = @SucessCount + 1
		  END
          FETCH NEXT FROM Cur0 INTO @SClientCode
        END 
        CLOSE Cur0
        DEALLOCATE Cur0
		SET @o_vcErrorMessage ='Total = '+CAST(@TotalCount AS VARCHAR)+'<br> Success = '+CAST(@SucessCount AS VARCHAR)+'<br> Rejected = '+CAST(@rejectedCount AS VARCHAR)
		
        SET @o_vcOutPutJSON='[{"Status":"##STATUS##","Remark":"' + @o_vcErrorMessage + '"}]'
		SET @o_vcOutPutJSON=REPLACE(@o_vcOutPutJSON,'##STATUS##','S')

        RETURN 1 
	  END
	END
	
	IF  @i_vcFunctionName = 'GetExportData'
	BEGIN
	   DECLARE @strOptionType VARCHAR(50)='',
	   @strFromDate VARCHAR(11)='', @strToDate VARCHAR(11)=''
	   
	   SELECT @strOptionType = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='OptionType'
	   SELECT @strFromDate = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='FromDate'
	   SELECT @strToDate = ColumnValue FROM @tbl_InputJSONTable WHERE ColumnName='ToDate'
	 
	   
	  
	   IF  @strExportType = 'CVLKRA - InsertUpdateKYCRecord'
	   BEGIN
	     IF @strOptionType IN('Pending','Rejected')
		 BEGIN
	       SELECT rm_cmcd AS [ClientCode], [ClientName]= LTRIM(RTRIM(cm_name)), 
		   [PANNO] = cm_panno, CONVERT(VARCHAR,CAST(x.rm_cdate AS DATE),106) as [RequestDate], 
		   rm_ctime as [RequestTime],
           ApproveDate = CONVERT(VARCHAR,CAST(x.mkrdt AS DATE),106), ApproveTime = x.mkrtm,
           [Status] = isnull((select ResponseString FROM tbl_GenericAPIDebugLog(NOLOCK) xg WHERE RequestSource = 'CVLKRA' AND RequestUniqueID = X.rm_cmcd
           AND SerialNo in(Select max(SerialNo) from tbl_GenericAPIDebugLog(NOLOCK) WHERE RequestSource = 'CVLKRA'  
		   and RequestUniqueID = xg.RequestUniqueID) AND UpdateTimeStamp >= CAST(X.MKRDT AS DATE)),'Pending')
		   INTO #TBL_PENDINGEXPORT
           FROM Client_ReKycMain(NOLOCK) x, client_master(nolock) cc, Client_CKYC CK
           WHERE rm_RequestType ='ReKYC'
           AND RM_CMCD = CC.CM_CD
		   AND CM_PANNO = CK.CK_Panno
		   AND CK_SRNO IN(SELECT MAX(CK_SRNO) FROM Client_CKYC(NOLOCK) WHERE CK_Panno = CK.CK_Panno)
		   AND EXISTS(SELECT 1 FROM Client_CKYCImages(NOLOCK) WHERE CI_PANNO = CK.CK_Panno AND CI_SRNO = CK_SRNO)
           AND Ck_NFiller2 = 0
           AND rm_rekyc ='Y'
           AND RM_CMCD = cm_cd
           AND x.rm_refno in(select max(rm_refno) from Client_ReKycMain(NOLOCK) where rm_RequestType ='ReKYC'
           AND rm_cmcd = x.rm_cmcd)
		   AND X.mkrdt >= '20250601'
		 END
	     IF @strOptionType = 'Pending'
		 BEGIN
		   SET @jsonExport1 = (SELECT * FROM #TBL_PENDINGEXPORT WHERE Status = 'Pending' FOR JSON Path)
		     
           IF OBJECT_ID('tempdb..#TBL_PENDINGEXPORT') IS NOT NULL
           DROP TABLE #TBL_PENDINGEXPORT
		   
		   SET @o_vcErrorMessage = 'Process Executed'
	       SET @o_vcOutPutJSON='[{"Status":"Y","Remark":"' + @o_vcErrorMessage + '","Data":' + @jsonExport1 + '}]'
           RETURN 1
		 END
		 
		 IF @strOptionType = 'Rejected'
		 BEGIN
		   SET @jsonExport1 = ( SELECT * FROM #TBL_PENDINGEXPORT WHERE Status <> 'Pending' FOR JSON Path)
		   
		   IF OBJECT_ID('tempdb..#TBL_PENDINGEXPORT') IS NOT NULL
           DROP TABLE #TBL_PENDINGEXPORT
		   
		   SET @o_vcErrorMessage = 'Process Executed'
	       SET @o_vcOutPutJSON='[{"Status":"Y","Remark":"' + @o_vcErrorMessage + '","Data":' + @jsonExport1 + '}]'
           RETURN 1
		 END
		 
		 IF @strOptionType IN('Submitted','Re-Generate')
		 BEGIN
		   SELECT CONVERT(VARCHAR,CAST(x.rm_cdate AS DATE),106) as [RequestDate],
		   [Form No] = rm_refno,
		   rm_cmcd AS [ClientCode], [ClientName]= LTRIM(RTRIM(cm_name)), 
		   [PANNO] = cm_panno,   
           [Checker Date] = CONVERT(VARCHAR,cast(x.mkrdt+' '+x.mkrtm as datetime),113),
		   [Upload Date] = convert(VARCHAR,UpdateTimeStamp,113),
		   [Ack No] = '',		   
           [Status] = isnull(ResponseString,'Pending')
		   INTO #TBL_PENDINGEXPORTAPPROVED
           FROM Client_ReKycMain(NOLOCK) x, client_master(nolock) cc 
		   LEFT OUTER JOIN (select ResponseString, RequestUniqueID, UpdateTimeStamp FROM tbl_GenericAPIDebugLog(NOLOCK) xg WHERE RequestSource = 'CVLKRA'
           AND SerialNo in(Select max(SerialNo) from tbl_GenericAPIDebugLog(NOLOCK)
		   WHERE RequestSource = 'CVLKRA'  and RequestUniqueID = xg.RequestUniqueID)) X11 ON
		   (RequestUniqueID = CM_CD)
           WHERE rm_RequestType ='ReKYC'
           AND RM_CMCD IN(SELECT CM_CD FROM CLIENT_MASTER WHERE cm_panno IN(
           SELECT CK_Panno  FROM Client_CKYC(NOLOCK) CM  
           WHERE CK_SRNO IN(SELECT MAX(CK_SRNO) FROM Client_CKYC(NOLOCK) WHERE CK_Panno = CM.CK_Panno)
           AND Ck_NFiller2 > 0))
           AND rm_rekyc ='Y'
           AND RM_CMCD = cm_cd
           AND x.rm_refno in(select max(rm_refno) from Client_ReKycMain(NOLOCK) where rm_RequestType ='ReKYC'
           AND rm_cmcd = x.rm_cmcd)
		   AND ((X.mkrdt >= @strFromDate
		   AND X.mkrdt <= @strToDate AND @strOptionType <> 'Submitted') OR @strOptionType = 'Submitted')
		   AND X.mkrdt >='20250601'
		   AND NOT EXISTS(select 1 from Client_Nominee(NOLOCK) WHERE cn_cd  = RM_CMCD 
		   AND ISNULL(cn_KRAStatus,'') <> '' AND cn_filler5 IN('Modification Validated','KRA Verified','KRA Validated','Existing KYC Verified','Modification Registered')
		   AND cn_KRADate >= rm_cdate)
		   
		   SET @jsonExport1 = ( SELECT * FROM #TBL_PENDINGEXPORTAPPROVED  FOR JSON Path)
		   
		   IF OBJECT_ID('tempdb..#TBL_PENDINGEXPORT') IS NOT NULL
           DROP TABLE #TBL_PENDINGEXPORT
		   
		   SET @o_vcErrorMessage = 'Process Executed'
	       SET @o_vcOutPutJSON='[{"Status":"Y","Remark":"' + @o_vcErrorMessage + '","Data":' + @jsonExport1 + '}]'
           RETURN 1
		   
		 END
		 IF @strOptionType IN('Approved')
		 BEGIN
		   SELECT CONVERT(VARCHAR,CAST(x.rm_cdate AS DATE),106) as [RequestDate],
		   [Form No] = rm_refno,
		   rm_cmcd AS [ClientCode], [ClientName]= LTRIM(RTRIM(cm_name)), 
		   [PANNO] = cm_panno,   
           [Checker Date] = CONVERT(VARCHAR,cast(x.mkrdt+' '+x.mkrtm as datetime),113),
		   [Upload Date] = convert(VARCHAR,UpdateTimeStamp,113),
		   [Ack No] = '',		   
           [Status] = isnull(ExceptionMessage,ResponseString)
		   INTO #TBL_PENDINGEXPORTAPPROVED1
           FROM Client_ReKycMain(NOLOCK) x, client_master(nolock) cc 
		   LEFT OUTER JOIN (select ResponseString, RequestUniqueID, UpdateTimeStamp, ExceptionMessage FROM tbl_GenericAPIDebugLog(NOLOCK) xg WHERE RequestSource = 'CVLKRA'
           AND SerialNo in(Select MAX(SerialNo) from tbl_GenericAPIDebugLog(NOLOCK)
		   WHERE RequestSource = 'CVLKRA' and RequestUniqueID = xg.RequestUniqueID)) X11 ON
		   (RequestUniqueID = CM_CD)
           WHERE rm_RequestType ='ReKYC'
           AND RM_CMCD IN(SELECT CM_CD FROM CLIENT_MASTER WHERE cm_panno IN(
           SELECT CK_Panno  FROM Client_CKYC(NOLOCK) CM  
           WHERE CK_SRNO IN(SELECT MAX(CK_SRNO) FROM Client_CKYC(NOLOCK) WHERE CK_Panno = CM.CK_Panno)
           AND Ck_NFiller2 > 0))
           AND rm_rekyc ='Y'
           AND RM_CMCD = cm_cd
		   AND EXISTS(select 1 from Client_Nominee(NOLOCK) WHERE cn_cd  = RM_CMCD 
		   AND ISNULL(cn_KRAStatus,'') <> '' AND cn_filler5 IN('Modification Validated','KRA Verified','KRA Validated','Existing KYC Verified','Modification Registered')
		   AND cn_KRADate >= rm_cdate)
           AND x.rm_refno in(select max(rm_refno) from Client_ReKycMain(NOLOCK) where rm_RequestType ='ReKYC'
           AND rm_cmcd = x.rm_cmcd)
		   AND X.mkrdt >= @strFromDate
		   AND X.mkrdt <= @strToDate
		   
		   SET @jsonExport1 = ( SELECT * FROM #TBL_PENDINGEXPORTAPPROVED1  FOR JSON Path)
		   
		   IF OBJECT_ID('tempdb..#TBL_PENDINGEXPORTAPPROVED1') IS NOT NULL
           DROP TABLE #TBL_PENDINGEXPORTAPPROVED1
		   
		   SET @o_vcErrorMessage = 'Process Executed'
	       SET @o_vcOutPutJSON='[{"Status":"Y","Remark":"' + @o_vcErrorMessage + '","Data":' + @jsonExport1 + '}]'
           RETURN 1
		 END
	   END
	   ELSE 
	   IF  @strExportType = 'CKYC - CKYCFileDownload'
	   BEGIN
	     IF @strOptionType IN('Pending')
		 BEGIN
	       SELECT rm_cmcd AS [ClientCode], [ClientName]= LTRIM(RTRIM(cm_name)), CONVERT(VARCHAR,CAST(x.rm_cdate AS DATE),106) as [RequestDate], 
		   rm_ctime as [RequestTime],
           ApproveDate = CONVERT(VARCHAR,CAST(x.mkrdt AS DATE),106), ApproveTime = x.mkrtm,
           [Status] = 'Pending'
		   INTO #TBL_CKYCPENDINGEXPORT
           FROM Client_ReKycMain(NOLOCK) x, client_master(nolock) CM , Client_CKYC(NOLOCK) CK 
           WHERE rm_RequestType ='ReKYC'
           --AND Ck_STATUS = 'Y' 
		   AND ISNULL(CK_batchno,0) = 0
		   AND CM.cm_panno =  CK.CK_Panno 
		   AND CK_SRNO IN(SELECT MAX(CK_SRNO) FROM Client_CKYC(NOLOCK) WHERE CK_Panno = CK.CK_Panno)
		   AND RM_CMCD = CM.CM_CD 
           AND rm_rekyc ='Y'
           AND RM_CMCD = cm_cd
           AND x.rm_refno in(select max(rm_refno) from Client_ReKycMain(NOLOCK) 
		   WHERE rm_RequestType ='ReKYC'
           AND rm_cmcd = x.rm_cmcd)

		   
		   SET @jsonExport1 = ( SELECT * FROM #TBL_CKYCPENDINGEXPORT  FOR JSON Path)
		   
		   IF OBJECT_ID('tempdb..#TBL_CKYCPENDINGEXPORT') IS NOT NULL
           DROP TABLE #TBL_CKYCPENDINGEXPORT
		   
		   SET @o_vcErrorMessage = 'Process Executed'
	       SET @o_vcOutPutJSON='[{"Status":"Y","Remark":"' + @o_vcErrorMessage + '","Data":' + @jsonExport1 + '}]'
           RETURN 1
		 END   
		 IF @strOptionType IN('Re-Generate','Submitted')
		 BEGIN
	       SELECT rm_cmcd AS [ClientCode], [ClientName]= LTRIM(RTRIM(cm_name)), CONVERT(VARCHAR,CAST(x.rm_cdate AS DATE),106) as [RequestDate], 
		   rm_ctime as [RequestTime],
           ApproveDate = CONVERT(VARCHAR,CAST(x.mkrdt AS DATE),106), ApproveTime = x.mkrtm,
		   [BatchNo] = ISNULL(CK_batchno,0),
           [BatchDate] = CONVERT(VARCHAR,CK_batchDt,106)
		   INTO #TBL_CKYCREEXPORT
           FROM Client_ReKycMain(NOLOCK) x,  client_master(nolock) CM, Client_CKYC(NOLOCK) CK 
		   WHERE rm_RequestType ='ReKYC'
           AND CM.cm_panno =  CK.CK_Panno AND CK_SRNO IN(SELECT MAX(CK_SRNO) FROM Client_CKYC(NOLOCK) WHERE CK_Panno = CK.CK_Panno)
		   AND RM_CMCD = CM.CM_CD
		   AND Ck_STATUS = 'E' AND ISNULL(CK_batchno,0) <> 0
           AND rm_rekyc ='Y'
           AND RM_CMCD = cm_cd
           AND x.rm_refno in(select max(rm_refno) from Client_ReKycMain(NOLOCK) where rm_RequestType ='ReKYC'
           AND rm_cmcd = x.rm_cmcd)
		   AND X.mkrdt >= @strFromDate
		   AND X.mkrdt <= @strToDate
		   AND X.mkrdt >='20250601'
		 
		 
		   SET @jsonExport1 = ( SELECT * FROM #TBL_CKYCREEXPORT  FOR JSON Path)
		   
		   IF OBJECT_ID('tempdb..#TBL_CKYCREEXPORT') IS NOT NULL
           DROP TABLE #TBL_CKYCREEXPORT
		   
		   SET @o_vcErrorMessage = 'Process Executed'
	       SET @o_vcOutPutJSON='[{"Status":"Y","Remark":"' + @o_vcErrorMessage + '","Data":' + @jsonExport1 + '}]'
           RETURN 1
		 END
		 IF @strOptionType IN('Approved')
		 BEGIN
	       SELECT rm_cmcd AS [ClientCode], [ClientName] = LTRIM(RTRIM(cm_name)), CONVERT(VARCHAR,CAST(x.rm_cdate AS DATE),106) as [RequestDate], 
		   rm_ctime as [RequestTime],
           ApproveDate = CONVERT(VARCHAR,CAST(x.mkrdt AS DATE),106), ApproveTime = x.mkrtm,
		   [BatchNo] = ISNULL(CK_batchno,0),
           [BatchDate] = CONVERT(VARCHAR,CK_batchDt,106)
		   INTO #TBL_CKYCAPPEXPORT
           FROM Client_ReKycMain(NOLOCK) x,  client_master(nolock) CM, Client_CKYC(NOLOCK) CK 
		   WHERE rm_RequestType ='ReKYC'
           AND CM.cm_panno =  CK.CK_Panno AND CK_SRNO IN(SELECT MAX(CK_SRNO) FROM Client_CKYC(NOLOCK) WHERE CK_Panno = CK.CK_Panno)
		   AND RM_CMCD = CM.CM_CD
		   AND Ck_STATUS = 'S' AND ISNULL(CK_batchno,0) <> 0
           AND rm_rekyc ='Y'
           AND RM_CMCD = cm_cd
           AND x.rm_refno in(select max(rm_refno) from Client_ReKycMain(NOLOCK) where rm_RequestType ='ReKYC'
           AND rm_cmcd = x.rm_cmcd)
		   
		   SET @jsonExport1 = ( SELECT * FROM #TBL_CKYCAPPEXPORT  FOR JSON Path)
		   
		   IF OBJECT_ID('tempdb..#TBL_CKYCAPPEXPORT') IS NOT NULL
           DROP TABLE #TBL_CKYCAPPEXPORT
		   
		   SET @o_vcErrorMessage = 'Process Executed'
	       SET @o_vcOutPutJSON='[{"Status":"Y","Remark":"' + @o_vcErrorMessage + '","Data":' + @jsonExport1 + '}]'
           RETURN 1
		 END
	   END
	END
  END
END
GO

CREATE PROCEDURE stpr_APICVLKRAStatusUpdate @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT WITH ENCRYPTION AS
BEGIN
  --SET @vcXML = '<root>'+@vcXML+'</root>' 
  DECLARE @XMLData XML = CAST(@vcXML AS XML), @strClient VARCHAR(20)='', @strThirdPartyURL VARCHAR(200)='',
  @o_vcOutputJsonapi VARCHAR(MAX)='', @strKRAAPIPassword NVARCHAR(MAX)='', @strurl VARCHAR(MAX)='', @StrHeaderString VARCHAR(MAX)='',
  @strRefNo VARCHAR(20)=''
  BEGIN TRY
    SELECT @strClient =  ISNULL(x.value('(ClientCode)[1]', 'VARCHAR(500)'),''),
	@strRefNo = ISNULL(x.value('(RefNo)[1]', 'VARCHAR(50)'),'')
    FROM @XMLData.nodes('/root') AS XTbl(x) 
  END TRY
  BEGIN CATCH
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Error in Parameter '+ ERROR_MESSAGE()
	RETURN 1
  END CATCH  

  IF ISNULL(@strClient,'') = ''
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'CLIENT CODE NOT FOUND'
	RETURN 1
  END 
  DECLARE @strParamString VARCHAR(MAX)='',  @strRequestString VARCHAR(MAX)='', @strtradeplustempdb VARCHAR(50)=''
  SELECT @strParamString = RequestJson, @strurl = APIUrl
  FROM tbl_VendorAPISetting(NOLOCK) where APIVendorName= 'CVLKRA' 
  AND APIName = 'GetToken' AND ISNULL(ISACTIVE,'N') = 'Y' 
  
  DECLARE @strLogSerialNo INT = 0
  
  DECLARE @tbl_ErrorLog TABLE(Process VARCHAR(50), Descp VARCHAR(MAX))
    
  SELECT @strThirdPartyURL = sp_sysvalue 
  FROM Sysparameter(NOLOCK) WHERE sp_parmcd = 'ThirdParty'
  
  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB' 	

  IF ISNULL(@strThirdPartyURL,'') = ''
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Please Define Third Party Calling Local URL in Sysparameter AS sp_parmcd = ''ThirdParty'''
    RETURN 1
  END  
 
  IF ISNULL(@strParamString,'') = ''
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'SETTING NOT DEFINE'
	EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
    RETURN 1
  END
  
  
  DECLARE @tbl_jsonoutput TABLE(SerialNo INT, ColumnName VARCHAR(50), ColumnValue VARCHAR(MAX), 
  MasterTag VARCHAR(100), JSONLEVEL INT, MASTERLEVEL INT)
  
  DECLARE @jsonCutterOutput VARCHAR(MAX)='', @strDanSql NVARCHAR(MAX)=''
  DECLARE @JsonCutterXML XML, 
  @strCmschedule VARCHAR(20) = (SELECT TOP 1 sp_sysvalue FROM sysparameter(NOLOCK) WHERE sp_parmcd = 'CMSCHEDULE')
    
  DECLARE @strposcd VARCHAR(200)='', @strusername VARCHAR(200)='', @strpassword VARCHAR(200)='', @strpasskey VARCHAR(200)='',
  @strEncykey VARCHAR(200)='', @o_DecyText VARCHAR(MAX)='', @strEncryptText NVARCHAR(MAX)='', @Striv NVARCHAR(MAX)='', @strToken NVARCHAR(MAX)='',
  @strPANNO VARCHAR(20)=''
  
  SELECT @strPANNO = CM_PANNO FROM CLIENT_MASTER(NOLOCK) WHERE CM_cD = @strClient
  
  
  SELECT @strposcd = VALUE
  FROM (SELECT * FROM DBO.fn_SplitString(@strParamString, '~')) X1
  WHERE X1.Position = 1 
  
  SELECT @strusername = VALUE
  FROM (SELECT * FROM DBO.fn_SplitString(@strParamString, '~')) X1
  WHERE X1.Position = 2 
  
  SELECT @strpassword = VALUE
  FROM (SELECT * FROM DBO.fn_SplitString(@strParamString, '~')) X1
  WHERE X1.Position = 3

  SELECT @strpasskey = VALUE
  FROM (SELECT * FROM DBO.fn_SplitString(@strParamString, '~')) X1
  WHERE X1.Position = 4  
  
  SELECT @strEncykey = VALUE
  FROM (SELECT * FROM DBO.fn_SplitString(@strParamString, '~')) X1
  WHERE X1.Position = 5
  
  SET @StrHeaderString = '[{"key":"api_key","value":"##api_key##"}]'
  SET @StrHeaderString = REPLACE(@StrHeaderString,'##api_key##',@strpasskey)
  SET @strRequestString = '{"username": "##UserName##","poscode": "##PosCode##","password": "##Password##"}'
  SET @strRequestString = REPLACE(@strRequestString,'##UserName##',@strusername)
  SET @strRequestString = REPLACE(@strRequestString,'##PosCode##',@strposcd)
  SET @strRequestString = REPLACE(@strRequestString,'##Password##',@strpassword)
  
  EXEC [dbo].[stpr_GetKRAEncryDecry] @strRequestString, 'Encrypt', @strThirdPartyURL, @strEncykey, '', @o_DecyText OUTPUT 
  
   
  
  DELETE FROM @tbl_jsonoutput
  BEGIN TRY
	SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_DecyText+''' , @jsonCutterOutput OUTPUT';
    EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
    SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
    INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Encrypt '+@o_DecyText
	EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Encrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
    RETURN 1 		
  END CATCH 
	    
  IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'encryptedData' AND ColumnValue <> '')
  BEGIN
    SELECT @strEncryptText = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'encryptedData'
	SELECT @Striv = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'iv'
  END
  ELSE
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Encrypt '+@o_DecyText
	EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Encrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
    RETURN 1 		
  END 
  
  SET @o_DecyText = '"\"'+@Striv+':'+@strEncryptText+'\""'
  
  EXEC stpr_CallThirdPartyAPI 'CVLKRA-PASSWORD', 'CVLKRA', 'GetToken', '', @o_DecyText, '', 
	@strThirdPartyURL, 'API', @o_vcOutputJsonapi OUTPUT, @StrHeaderString,'' 
  
   
  IF ISNULL(@o_vcOutputJsonapi,'') = ''
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'CVLKRA-Token RESPONSE NOT FOUND'
	EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
    RETURN 1
  END
  ELSE
  BEGIN
    BEGIN TRY
      SELECT @Striv = VALUE
      FROM (SELECT * FROM DBO.fn_SplitString(@o_vcOutputJsonapi, ':')) X1
      WHERE X1.Position = 1
	   
	  SELECT @strEncryptText = VALUE
      FROM (SELECT * FROM DBO.fn_SplitString(@o_vcOutputJsonapi, ':')) X1
      WHERE X1.Position = 2 
	  SET @o_DecyText = ''
  
	  EXEC [dbo].[stpr_GetKRAEncryDecry] @strEncryptText, 'Decrypt', @strThirdPartyURL, @strEncykey, @Striv, @o_DecyText OUTPUT 
	  DELETE FROM @tbl_jsonoutput
	  BEGIN TRY
	    SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_DecyText+''' , @jsonCutterOutput OUTPUT';
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
        INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	   SET @o_vcErrorFlag = 'E'
	   SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Decrypt '+@o_DecyText
	   EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Decrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
       RETURN 1 		
     END CATCH 
	 
     IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'token' AND ISNULL(ColumnValue,'') <> '')
	 BEGIN
	   SET @strToken = ''
	   SELECT @strToken = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'token'
     END	 
	END TRY
    BEGIN CATCH
	  SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Token '+@o_vcOutputJsonapi
	  EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
      RETURN 1
    END CATCH  	
  END
  
  IF ISNULL(@strToken,'') <> ''
  BEGIN
    INSERT INTO tbl_GenericAPIDebugLog(ParentSerialNo,RequestSource,RequestUniqueID,RequestString,ResponseString,ExceptionMessage,UpdateBy,UpdateTimeStamp)
    VALUES(@strRefNo,'CVLKRA',@strClient,@strRequestString,'','','SA',GETDATE())
	 
    SET @strLogSerialNo = IDENT_CURRENT('tbl_GenericAPIDebugLog')   
	
    SET @strRequestString = '{"pan":"##pan##","poscode":"##PosCode##"}'
	SET @strRequestString = REPLACE(@strRequestString,'##pan##',@strPANNO)
	SET @strRequestString = REPLACE(@strRequestString,'##PosCode##',@strposcd)
	 
	EXEC [dbo].[stpr_GetKRAEncryDecry] @strRequestString, 'Encrypt', @strThirdPartyURL, @strEncykey, '', @o_DecyText OUTPUT 
	 
    DELETE FROM @tbl_jsonoutput
    BEGIN TRY
	  SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_DecyText+''' , @jsonCutterOutput OUTPUT';
      EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
      SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
      INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	  SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Encrypt '+@o_DecyText
	  
	  UPDATE A SET A.ResponseString = @o_vcErrorMessage, A.ExceptionMessage = @o_DecyText
      FROM tbl_GenericAPIDebugLog A
      WHERE A.SerialNo =  @strLogSerialNo	
		
	  EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Encrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
      RETURN 1 		
    END CATCH 
	SET @strEncryptText = ''
    SET @Striv = ''	 
	IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'encryptedData' AND ColumnValue <> '')
    BEGIN
      SELECT @strEncryptText = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'encryptedData'
	  SELECT @Striv = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'iv'
    END
    ELSE
    BEGIN
      SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Encrypt '+@o_DecyText
	    
	  UPDATE A SET A.ResponseString = @o_vcErrorMessage, A.ExceptionMessage = @o_DecyText
      FROM tbl_GenericAPIDebugLog A
      WHERE A.SerialNo =  @strLogSerialNo	
	   
	  EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Encrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
      RETURN 1 		
    END 
  
    SET @o_DecyText = '"\"'+@Striv+':'+@strEncryptText+'\""'
	 
	SET @StrHeaderString = '[{"key":"Token","value":"##Token##"}]'
    SET @StrHeaderString = REPLACE(@StrHeaderString,'##Token##',@strToken)
	 
    SET @o_vcOutputJsonapi = ''
	 
	EXEC stpr_CallThirdPartyAPI 'CVLKRA-GetPanStatus', 'CVLKRA', 'GetPanStatus', '', @o_DecyText, '', 
	@strThirdPartyURL, 'API', @o_vcOutputJsonapi OUTPUT, @StrHeaderString, ''
	
	
	SET @strEncryptText = ''
    SET @Striv = ''	
	 
	IF ISNULL(@o_vcOutputJsonapi,'') = ''
    BEGIN
      SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = 'CVLKRA-UPDATEKYC RESPONSE NOT FOUND ' +@o_vcOutputJsonapi
	  
	    
	  UPDATE A SET A.ResponseString = @o_vcErrorMessage, A.ExceptionMessage = @o_vcOutputJsonapi
      FROM tbl_GenericAPIDebugLog A
      WHERE A.SerialNo =  @strLogSerialNo	
	   
	  EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN GetPanStatus ', @o_vcErrorMessage, '',@o_vcErrorMessage
      RETURN 1
    END
    ELSE
    BEGIN
      BEGIN TRY
        SELECT @Striv = VALUE
        FROM (SELECT * FROM DBO.fn_SplitString(@o_vcOutputJsonapi, ':')) X1
        WHERE X1.Position = 1
	   
	    SELECT @strEncryptText = VALUE
        FROM (SELECT * FROM DBO.fn_SplitString(@o_vcOutputJsonapi, ':')) X1
        WHERE X1.Position = 2 
	     
	    SET @o_DecyText = ''
	    EXEC [dbo].[stpr_GetKRAEncryDecry] @strEncryptText, 'Decrypt', @strThirdPartyURL, @strEncykey, @Striv, @o_DecyText OUTPUT 
	
		
	    DELETE FROM @tbl_jsonoutput 
		
	    BEGIN TRY
	      SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_DecyText+''' , @jsonCutterOutput OUTPUT';
          EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
          SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
          INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	       SET @o_vcErrorFlag = 'E'
	       SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Decrypt '+@o_DecyText
		   
		   UPDATE A SET A.ResponseString = @o_vcErrorMessage, A.ExceptionMessage = @o_DecyText
           FROM tbl_GenericAPIDebugLog A
           WHERE A.SerialNo =  @strLogSerialNo	
	   
		   
		   EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Decrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
           RETURN 1 		
        END CATCH 
		
		IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'resdtls' AND ISNULL(ColumnValue,'') <> '')
	    BEGIN
	       --SET @strToken = ''
	      SELECT @o_DecyText = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'resdtls'
		   
		  DELETE FROM @tbl_jsonoutput 
	      BEGIN TRY
	        SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_DecyText+''' , @jsonCutterOutput OUTPUT';
            EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
            SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
            INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	        SET @o_vcErrorFlag = 'E'
	        SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Decrypt '+@o_DecyText
		   
		    UPDATE A SET A.ResponseString = @o_vcErrorMessage, A.ExceptionMessage = @o_DecyText
            FROM tbl_GenericAPIDebugLog A
            WHERE A.SerialNo =  @strLogSerialNo	
	   
		   
		    EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Decrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
            RETURN 1 		
          END CATCH 
		END  
	   END TRY
      BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Token '+@o_vcOutputJsonapi

	    EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN GetPanStatus API ', @o_vcErrorMessage, '',@o_vcErrorMessage
        RETURN 1
      END CATCH  	
    END
	 
	
	DECLARE @StrKRACode VARCHAR(10)='CVLKRA', @StrKRAPANStatus VARCHAR(MAX)='', @StrKRAPANCode VARCHAR(20)='', 
	@APP_MODDT VARCHAR(30)
    SELECT @StrKRACode = ColumnValue FROM @tbl_jsonoutput 
	WHERE COLUMNNAME = 'APP_UPDT_STATUS'
	
	SELECT @APP_MODDT = ColumnValue FROM @tbl_jsonoutput 
	WHERE COLUMNNAME = 'APP_MODDT'
	
	
	SELECT TOP 1 @StrKRAPANStatus = Status_Description, 
	@StrKRAPANCode = CASE WHEN CVLKRA = @StrKRACode THEN 'CVLKRA' 
    WHEN NDML = @StrKRACode THEN 'NDML' 
    WHEN DOTEX = @StrKRACode THEN 'DOTEX' 
    WHEN CAMS = @StrKRACode THEN 'CAMS' 
    WHEN KARVY = @StrKRACode THEN 'KARVY' ELSE '' END
    FROM tbl_KRAStatusCode(NOLOCK) WHERE (CVLKRA = @StrKRACode OR NDML = @StrKRACode OR DOTEX = @StrKRACode  OR CAMS = @StrKRACode
    OR KARVY = @StrKRACode ) 
	AND InsertFlag = 'U'
	 
	
	IF LTRIM(RTRIM(@StrKRAPANStatus)) NOT IN('Existing KYC Verified','KRA Validated','KRA Verified','Modification Validated','Modification Registered') AND @StrKRAPANCode <> 'CVLKRA'
	BEGIN
	  UPDATE A SET A.ResponseString = @StrKRAPANStatus+' WITH '+@StrKRAPANCode, A.ExceptionMessage = @o_DecyText
      FROM tbl_GenericAPIDebugLog A
      WHERE A.SerialNo =  @strLogSerialNo	
	  SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = @StrKRAPANStatus+' WITH '+@StrKRAPANCode
      RETURN 1
	END
	ELSE 
	  
	  UPDATE A SET A.ResponseString = @StrKRAPANStatus, A.ExceptionMessage = @StrKRAPANStatus+' WITH '+@StrKRAPANCode
      FROM tbl_GenericAPIDebugLog A
      WHERE A.SerialNo =  @strLogSerialNo	
	  
	  SET DATEFORMAT DMY
      
	  UPDATE A SET A.cn_KRAStatus = 'Y', cn_filler5= @StrKRAPANStatus, cn_KRADate = CONVERT(VARCHAR,CAST(@APP_MODDT AS DATE),112) 
	  FROM Client_Nominee A 
	  WHERE A.cn_cd = @strClient
	  
	  UPDATE A SET A.Ck_Status = 'S'
	  FROM Client_CKYC A 
	  WHERE A.CK_Panno = @strPANNO
	  AND ck_srno IN(SELECT MAX(ck_srno) FROM Client_CKYC(NOLOCK) WHERE CK_Panno = A.CK_Panno)
	  SET DATEFORMAT YMD
	  SET @o_vcErrorFlag = 'S'
	  SET @o_vcErrorMessage = @StrKRAPANStatus+' WITH '+@StrKRAPANCode
      RETURN 1
  END	
END
GO

CREATE PROCEDURE [dbo].[stpr_GetKRAEncryDecry] @i_vcInputJson NVARCHAR(MAX), @i_vcType VARCHAR(50), 
@CallingAPIURLMain VARCHAR(MAX), @i_aesKey VARCHAR(MAX), @i_IVKey VARCHAR(MAX), @o_vcOutputJson NVARCHAR(MAX) OUTPUT 
WITH ENCRYPTION
AS
BEGIN

  DECLARE @i_vcInputJson1 NVARCHAR(MAX)=''
  SET @o_vcOutputJson = ''
  
  
  IF @i_vcType = 'Encrypt'
  BEGIN
    SET @CallingAPIURLMain = @CallingAPIURLMain+'/api/ThirdPartyService/EncryptKRA'
	SET @i_vcInputJson1 = '{"aesKey":"##AESKEY##","data":"##data##"}'
	SET @i_vcInputJson1 = REPLACE(@i_vcInputJson1,'##AESKEY##',@i_aesKey)
    SET @i_vcInputJson1 = REPLACE(@i_vcInputJson1,'##DATA##',REPLACE(@i_vcInputJson,'"','\"'))
	--SET @i_vcInputJson1 = replace(@i_vcInputJson1,'##DATA##',@i_vcInputJson)
  
  END
  ELSE IF @i_vcType = 'Decrypt'
  BEGIN
    SET @CallingAPIURLMain = @CallingAPIURLMain+'/api/ThirdPartyService/DecryptKRA'
	SET @i_vcInputJson1 = '{"aesKey":"##AESKEY##","encryptedText":"##data##","iv":"##IV##"}'
	SET @i_vcInputJson1 = REPLACE(@i_vcInputJson1,'##AESKEY##',@i_aesKey)
    SET @i_vcInputJson1 = REPLACE(@i_vcInputJson1,'##DATA##',@i_vcInputJson)
    SET @i_vcInputJson1 = REPLACE(@i_vcInputJson1,'##IV##',@i_IVKey)
  END
  ELSE IF @i_vcType = 'Upload'
  BEGIN
    SET @CallingAPIURLMain = @CallingAPIURLMain+'/api/ThirdPartyService/SFTP_upload'
	SET @i_vcInputJson1 = @i_vcInputJson
  END
  ELSE IF @i_vcType = 'Download'
  BEGIN
    SET @CallingAPIURLMain = @CallingAPIURLMain+'/api/ThirdPartyService/CKYC_FileDownload'
	SET @i_vcInputJson1 = @i_vcInputJson
  END
  --SELECT @i_vcInputJson1, @CallingAPIURLMain
  
  DECLARE @Object AS INT;  
  DECLARE @ResponseText AS VARCHAR(8000)='';  
  DECLARE @tbl_OutputResponse as table(Json_Table nvarchar(max))
  DECLARE @VCOUTPUT VARCHAR(MAX)=''  
  EXEC sp_OACreate 'MSXML2.XMLHTTP', @Object OUT;  
  
  EXEC sp_OAMethod @Object, 'open', NULL, 'post',@CallingAPIURLMain, 'false' 
  EXEC sp_OAMethod @Object, 'setRequestHeader', null, 'Content-Type', 'application/json'  
  EXEC sp_OAMethod @Object, 'send', null, @i_vcInputJson1  
  --EXEC sp_OAMethod @Object, 'send', null
  INSERT INTO @tbl_OutputResponse (Json_Table) EXEC sp_OAMethod @Object, 'responseText'
  SELECT @VCOUTPUT = Json_Table FROM @tbl_OutputResponse
  --SELECT @VCOUTPUT, @CallingAPIURLMain
  SET @o_vcOutputJson = @VCOUTPUT  
  EXEC sp_OADestroy @Object  
  RETURN 
END
GO

CREATE PROCEDURE stpr_APICKYCFILEGEN @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage NVARCHAR(MAX) OUTPUT WITH ENCRYPTION AS
BEGIN
  --SET @vcXML = '<root>'+@vcXML+'</root>' 
  DECLARE @XMLData XML = CAST(@vcXML AS XML), @strClient VARCHAR(MAX)='', @strThirdPartyURL VARCHAR(200)='',
  @o_vcOutputJsonapi VARCHAR(MAX)='', @strKRAAPIPassword NVARCHAR(MAX)='', @strurl VARCHAR(MAX)='', @StrHeaderString VARCHAR(MAX)='',
  @strPANNO VARCHAR(20)=''
  BEGIN TRY
    SELECT @strClient =  ISNULL(x.value('(ClientCode)[1]', 'VARCHAR(MAX)'),'')
    FROM @XMLData.nodes('/root') AS XTbl(x) 
  END TRY
  BEGIN CATCH
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Error in Parameter '+ ERROR_MESSAGE()
	RETURN 1
  END CATCH  

  IF ISNULL(@strClient,'') = ''
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'CLIENT CODE NOT FOUND'
	RETURN 1
  END 
  
 SET @o_vcErrorFlag = 'S'
 SET @o_vcErrorMessage = 'Process Completed'
  
  DECLARE @strLogSerialNo INT = 0, @o_DecyText NVARCHAR(MAX)=''
  
  DECLARE @tbl_ErrorLog TABLE(Process VARCHAR(50), Descp VARCHAR(MAX))
    
    
  SELECT @strThirdPartyURL = sp_sysvalue 
  FROM Sysparameter(NOLOCK) WHERE sp_parmcd = 'ThirdParty'
  
  Declare @strBatchNO INT =0, @strFolderName VARCHAR(100)='', @strCKYCFINO VARCHAR(20)='', @strCKYCREGIONCD VARCHAR(20)='',
  @strCKYCUSERID VARCHAR(20)='', @StrTXTFileName VARCHAR(200)='', @strtotalClientCount INT = 0, @str20String VARCHAR(MAX)='',
  @strCKYCCOMPNAME VARCHAR(100)=''
  
  SELECT @strBatchNO = ISNULL(sp_sysvalue,0) + 1 
  FROM Sysparameter(NOLOCK) WHERE sp_parmcd='CKYCBATCHNO'
  
  IF ISNULL(@strBatchNO,0) = 0
  BEGIN
    SET @strBatchNO = 1
  END
  
  SELECT @strCKYCFINO = LTRIM(RTRIM(ISNULL(sp_sysvalue,''))) 
  FROM Sysparameter(NOLOCK) WHERE sp_parmcd='CKYCFINO'
  
  SELECT @strCKYCREGIONCD = LTRIM(RTRIM(ISNULL(sp_sysvalue,''))) 
  FROM Sysparameter(NOLOCK) WHERE sp_parmcd='CKYCREGIONCD'
  
  SELECT @strCKYCUSERID = LTRIM(RTRIM(ISNULL(sp_sysvalue,''))) 
  FROM Sysparameter(NOLOCK) WHERE sp_parmcd='CKYCUSERID'
  
  SELECT @strCKYCCOMPNAME = LTRIM(RTRIM(ISNULL(sp_sysvalue,''))) 
  FROM Sysparameter(NOLOCK) WHERE sp_parmcd='CKYCCOMPNAME'
  
  SET @strFolderName = @strCKYCFINO+'_'+@strCKYCREGIONCD+'_'+REPLACE(CONVERT(VARCHAR,GETDATE(),105),'-','')+'_'+'V1.2'+'_'+@strCKYCUSERID+'_U'+RIGHT(REPLICATE('0', 5) + cast(@strBatchNO AS VARCHAR), 5)
  SET @StrTXTFileName = @strCKYCFINO+'_'+@strCKYCREGIONCD+'_'+REPLACE(CONVERT(VARCHAR,GETDATE(),105),'-','')+'_'+'V1.2'+'_'+@strCKYCUSERID+'_U'+RIGHT(REPLICATE('0', 5) + cast(@strBatchNO AS VARCHAR), 5)+'.txt'
  
  DECLARE @tbl_Client TABLE(ClientCode VARCHAR(MAX))
		
  INSERT INTO @tbl_Client
  SELECT * FROM DBO.ReturnTable(@strClient,'|')
  
  SELECT @strtotalClientCount = COUNT(DISTINCT ClientCode) FROM @tbl_Client
  
  DECLARE @cx_Status VARCHAR(1)='', @cx_Reason VARCHAR(200)='', @cx_srno INT = 0,
	@cx_mrkdt VARCHAR(10) = '', @cx_Panno VARCHAR(12) = '', @cx_sex VARCHAR(20)='', @cm_name VARCHAR(200)='', 
	@cx_name VARCHAR(200)='', @cx_FatherFname VARCHAR(100)='', @cx_FatherMname VARCHAR(100)='', @cx_FatherLname VARCHAR(100)='',
	@cx_dob VARCHAR(20)='', @cx_nationalcode VARCHAR(10)='', @cx_ResiTaxPurpose VARCHAR(100)='', @cx_add1 VARCHAR(200)='',
	@cx_add2 VARCHAR(200)='', @cx_add3 VARCHAR(200)='', @cx_add4 VARCHAR(200)='', @cx_pincode VARCHAR(20)='', @cx_state VARCHAR(100)='',
	@cx_BankActNo VARCHAR(25)='', @cx_stdoffice VARCHAR(20)='', @cx_tele1 VARCHAR(20)='', @cx_std VARCHAR(20)='',
	@cx_tele2 VARCHAR(20)='', @cx_mobile VARCHAR(20)='', @cx_fax VARCHAR(50)='', @cx_email VARCHAR(50)='',
	@cx_IdentityProof VARCHAR(100)='', @cx_IdentityProofID VARCHAR(100)='', @cx_IdentityProofExpDt VARCHAR(20)='',
	@cx_AddrProofID VARCHAR(50)='', @cx_AddrProofExpDt VARCHAR(50)='', @cx_padd1 VARCHAR(100)='',
	@cx_padd2 VARCHAR(100)='', @cx_padd3 VARCHAR(100)='', @cx_padd4 VARCHAR(100)='', @cx_ppincode VARCHAR(20)='',
	@cx_pstate VARCHAR(100)='', @cx_pcountry VARCHAR(100)='', @cx_PermAddrProof VARCHAR(100)='' , @cx_PermAddrProofID VARCHAR(30)='',
	@cx_PermAddrProofExpDt VARCHAR(8)='', @cx_grossincome VARCHAR(20)='', @cx_brboffcode VARCHAR(20)='',
	@cx_maritalstatus VARCHAR(20)='', @cx_networth VARCHAR(50)='', @cx_networthdt VARCHAR(8)='', @cx_VerifyBy VARCHAR(50)='',
	@cx_IdentityProofcd VARCHAR(20)='', @cx_AddrProofCd VARCHAR(20)='', @cx_PermAddrProofCd VARCHAR(20)='', @cx_AddrProof VARCHAR(20)='',
	@Cx_AppType VARCHAR(20)='', @StrCKYCBRANCHCD VARCHAR(20)='', @CK_ActType VARCHAR(20)='',
	@cx_Reference VARCHAR(20)='', @cx_prefix VARCHAR(20)='', @CK_Fname VARCHAR(100)='', @CK_Middlename VARCHAR(50)='',
	@CK_Lname VARCHAR(100)='', @CK_fatherspouseflag VARCHAR(10)='', @CK_FatherPrefix VARCHAR(10)='',
	@CX_Motherfname VARCHAR(100)='', @CX_MotherPrefix VARCHAR(10)='',@CX_MotherMname VARCHAR(100)='', @CX_MotherLname VARCHAR(100)='',
	@cx_pdistrict VARCHAR(100)='', @cx_PStatecd VARCHAR(100)='', @cx_District VARCHAR(100)='', @cx_mkrdt VARCHAR(20)='', 
	@str30String VARCHAR(MAX)='', @str70String VARCHAR(MAX)=''
	
  SELECT @cx_VerifyBy = sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd='CKYCVERIFYBY'
	
  SELECT @StrCKYCBRANCHCD = sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd='CKYCBRANCHCD'
  
  DECLARE @um_user_name VARCHAR(100)='',   @um_designation VARCHAR(100)='',@um_empCode VARCHAR(100)=''
  
  SELECT @um_user_name = LTRIM(RTRIM(um_user_name)), @um_designation = LTRIM(RTRIM(um_designation)), 
  @um_empCode = LTRIM(RTRIM(um_empCode)) FROM User_master(NOLOCK) Where um_user_id = @cx_VerifyBy
    
  SET @strHeaderString = '10|'+RIGHT(REPLICATE('0', 5) + @strBatchNO, 5)+'|'+@strCKYCFINO+'|'+@strCKYCREGIONCD+'|'+CAST(@strtotalClientCount AS VARCHAR)+'|'+CONVERT(VARCHAR,GETDATE(),105)
  SET @strHeaderString = @strHeaderString +'|V1.2|01||||'
        
  DECLARE @StrClientCode VARCHAR(50)='', @strCounter INT = 0
  
  DECLARE Cur0 CURSOR FOR 
  SELECT DISTINCT ClientCode from @tbl_Client
  OPEN Cur0
  FETCH NEXT FROM Cur0 INTO @StrClientCode
  WHILE @@FETCH_STATUS = 0
  BEGIN  
    SET @strCounter = @strCounter + 1
	
	
    SELECT @cx_Status = 'Y' , @cx_Reason = '', @cx_srno = ck_srno, @cx_mrkdt = CONVERT(VARCHAR,CAST(a.mkrdt AS DATE),105), 
    @cx_Panno = A.CK_Panno , @cx_sex = cm_sex , @cm_name = cm_name, 
	@cx_name = cm_name, @cx_FatherFname = CK_FatherFname , @cx_FatherMname = CK_FatherMname , 
	@cx_FatherLname = CK_FatherLname , 
	@CX_MotherPrefix = isnull(Replace(CK_MotherPrefix,'.',''),''),
	@CX_Motherfname = isnull(CK_Motherfname,''),
	@CX_MotherMname =  isnull(CK_MotherMname,''),
	@CX_MotherLname = isnull(CK_MotherLname,''),
	
	@cx_dob = CONVERT(VARCHAR,CAST(cm_dob AS DATE),105), @cx_nationalcode = cm_nationalcode , 
	@cx_ResiTaxPurpose = CK_ResiTaxPurpose , @cx_add1 = LTRIM(RTRIM(cm_add1)) , 
	@cx_add2 = LTRIM(RTRIM(cm_add2)) , @cx_add3 = LTRIM(RTRIM(cm_add3)) , 
	@cx_add4 = LTRIM(RTRIM(cm_add4)) , @cx_pincode = LTRIM(RTRIM(cm_pincode)), 
	@cx_state = ISNULL((SELECT LTRIM(RTRIM(CS_Code)) FROM CKYC_StaticCodes(NOLOCK) Where cs_datatype='State' and cs_description = cm_state),''), 
	@cx_BankActNo = ISNULL((SELECT LTRIM(RTRIM(CS_Code)) FROM CKYC_StaticCodes(NOLOCK) Where cs_datatype='Country' and cs_description = cm_BankActNo),'') , 
	@cx_stdoffice = cm_stdoffice , @cx_tele1 = cm_tele1 , @cx_std = cm_std , @cx_tele2 = cm_tele2, 
	@cx_mobile = cm_mobile, @cx_fax = cm_fax , @cx_email = Replace(cm_email, ',', ';'), 
	@cx_IdentityProof = A.CK_IdentityProof, @cx_IdentityProofID = CK_IdentityProofID, 
	@cx_IdentityProofExpDt = CK_IdentityProofExpDt, @cx_AddrProof = CK_AddrProof, 
	@cx_AddrProofID = CK_AddrProofID , @cx_AddrProofExpDt = CK_AddrProofExpDt , @cx_padd1 = LTRIM(RTRIM(cm_padd1)) , 
	@cx_padd2 = LTRIM(RTRIM(cm_padd2)) , @cx_padd3 = LTRIM(RTRIM(cm_padd3)) , @cx_padd4 = LTRIM(RTRIM(cm_padd4)) , 
	@cx_ppincode = LTRIM(RTRIM(cm_ppincode)) , 
	@cx_pstate = ISNULL((SELECT LTRIM(RTRIM(CS_Code)) FROM CKYC_StaticCodes(NOLOCK) Where cs_datatype='State' and cs_description = cm_pstate),'') , 
	@cx_pcountry = ISNULL((SELECT LTRIM(RTRIM(CS_Code)) FROM CKYC_StaticCodes(NOLOCK) Where cs_datatype='Country' and cs_description = cm_pcountry),''), 
	@cx_PermAddrProof = CK_PermAddrProof , @cx_PermAddrProofID = CK_PermAddrProofID , 
	@cx_PermAddrProofExpDt = CK_PermAddrProofExpDt , @cx_grossincome = cm_grossincome , @cx_brboffcode = cm_brboffcode, 
	@cx_maritalstatus = cm_maritalstatus , @cx_networth = cm_networth , @cx_networthdt = cm_networthdt,
    @Cx_AppType = ISNULL(CK_AppType,''), @CK_ActType = ISNULL(CK_ActType,''), @cx_Reference = isNull(CK_Reference, ''),
	@cx_prefix =  ISNULL(Replace(cm_prefix,'.',''),''), @CK_Fname = CK_Fname, @CK_Middlename = CK_Middlename, @CK_Lname = CK_Lname,
	@CK_fatherspouseflag = (CASE WHEN CK_fatherspouseflag = 'F' THEN '01' WHEN CK_fatherspouseflag = 'S' THEN '02' ELSE '' END),
	@CK_FatherPrefix = ISNULL(REPLACE(CK_FatherPrefix,'.',''),''), @cx_mkrdt = CONVERT(VARCHAR,CAST(A.mkrdt AS DATE),105)
	FROM Client_Master(NOLOCK), Client_Info(NOLOCK), Client_CKYC(NOLOCK) a, CLIENT_KYC(NOLOCK) CK
    WHERE cm_cd = cm2_cd AND cm_panno = A.CK_Panno 
	AND cm_freezeyn = 'N' 
	AND CM_CD = @StrClientCode
    AND ck_srno IN(SELECT MAX(ck_srno) FROM Client_CKYC(NOLOCK) WHERE CK_Panno = A.CK_Panno)	
	AND cm_cd = CK_ClientCd 
	AND cm_panno = CK.CK_PANNo
	
	IF @cx_srno > 0
	BEGIN
	  IF @str20String <> ''
	  BEGIN
	    SET @str20String = @str20String+'\r\n' 
	    SET @str30String = @str30String+'\r\n' 
	    --SET @str70String = @str70String+'\r\n' 
	  END
	
      SET @str20String = @str20String+'20|'+CAST(@strCounter AS VARCHAR)+'|'+LTRIM(RTRIM(@Cx_AppType))+'|'+LTRIM(RTRIM(@StrCKYCBRANCHCD))+'|'
      IF @Cx_AppType = '03'			   
	  BEGIN
	    SET @str20String = @str20String+'01'+'|'+'02'+'|'+'02'+'|'+'02'+'|'+'02'+'|'+'02'+'|'+'02'+'|'+'02'+'|'+'02'+'|'+'02'+'|'+'01'+'|||'+LTRIM(RTRIM(@CK_ActType))+'|'+RTRIM(LTRIM(ISNULL(@cx_Reference,'')))+'|'
 	  END
	  ELSE				   
	  BEGIN
	    SET @str20String = @str20String+'||||||||||'+'01'+'|||'+LTRIM(RTRIM(@CK_ActType))+'|'+RTRIM(LTRIM(CAST(ISNULL(@cx_srno,0) AS VARCHAR)))+'|'
	  END
	  SET @str20String = @str20String+LTRIM(RTRIM(@cx_prefix))+'|'+LTRIM(RTRIM(@CK_Fname))+'|'+LTRIM(RTRIM(@CK_Middlename))+'|'+LTRIM(RTRIM(@CK_Lname))+'|||||||'
	  SET @str20String = @str20String+@CK_fatherspouseflag+'|'+LTRIM(RTRIM(@CK_FatherPrefix))+'|'+LTRIM(RTRIM(@cx_FatherFname))+'|'+LTRIM(RTRIM(@cx_FatherMname))+'|'
	  SET @str20String = @str20String+LTRIM(RTRIM(@cx_FatherLname))+'|'
	
	  IF LTRIM(RTRIM(@cx_FatherFname)) = '' AND LTRIM(RTRIM(@cx_FatherLname)) = '' 
      BEGIN
	    SET @str20String = @str20String+'|'
      END	
      ELSE
	  BEGIN
	    SET @str20String = @str20String+LTRIM(RTRIM(@cx_FatherFname))+' '+LTRIM(RTRIM(@cx_FatherMname))+' '+LTRIM(RTRIM(@cx_FatherLname))+'|'
	  END
	
	  IF LTRIM(RTRIM(@cx_Motherfname)) <> '' 
      BEGIN
	    SET @str20String = @str20String+LTRIM(RTRIM(@cx_motherPrefix))+'|'
	  END
	  ELSE
	  BEGIN
	    SET @str20String = @str20String+'|'
	  END
	  SET @str20String = @str20String+LTRIM(RTRIM(@cx_Motherfname))+'|'+LTRIM(RTRIM(@cx_MotherMname))+'|'+LTRIM(RTRIM(@cx_MotherLname))+'|'
	
	  IF LTRIM(RTRIM(@cx_Motherfname)) = '' AND LTRIM(RTRIM(@cx_MotherLname)) = '' 
      BEGIN
	    SET @str20String = @str20String+'|'
      END	
      ELSE
	  BEGIN
	    SET @str20String = @str20String+LTRIM(RTRIM(@cx_Motherfname))+' '+LTRIM(RTRIM(@cx_MotherMname))+' '+LTRIM(RTRIM(@cx_MotherLname))+'|'
	  END
	  SET @str20String = @str20String+LTRIM(RTRIM(@cx_sex))+'||||'
	  IF ISNULL(@cx_sex,'') = ''
	  BEGIN
	    SET @str20String = @str20String+'|'
	  END
      ELSE
	  BEGIN
	    SET @str20String = @str20String+LTRIM(RTRIM(@cx_dob))+'|'
	  END
      SET @str20String = @str20String+'|||||||'+LTRIM(RTRIM(@cx_Panno))+'||||||||'+@cx_padd1+'|'+@cx_padd2+'|'+@cx_padd3+'|'+@cx_padd4+'|'
	
	  SELECT @cx_pdistrict = LTRIM(RTRIM(cs_description)) FROM CKYC_StaticCodes(NOLOCK) 
	  Where cs_datatype='District' and cs_code=@cx_ppincode

      SET @str20String = @str20String+@cx_pdistrict+'|'
 	
  	  SET @str20String = @str20String+@cx_pstate+'|'+@cx_pcountry+'|'+@cx_ppincode+'|'+@cx_PermAddrProof+'||Y||'+@cx_add1+'|'+@cx_add2+'|'+@cx_add3+'|'+@cx_add4+'|'
	
      SELECT @cx_district = LTRIM(RTRIM(cs_description)) FROM CKYC_StaticCodes(NOLOCK) 
	  Where cs_datatype='District' and cs_code=@cx_pincode
	
	  SET @str20String = @str20String+@cx_district+'|'+@cx_state+'|'+@cx_BankActNo+'|'+@cx_pincode+'|'+@cx_AddrProof+'|||||||||'
		
	  IF RTRIM(LTRIM(@cx_tele2)) <> ''
	  BEGIN
	    SET @str20String = @str20String+LTRIM(RTRIM(@cx_std))+'|'+RTRIM(LTRIM(@cx_tele2))+'|'
	  END
	  ELSE  
	  BEGIN
	    SET @str20String = @str20String+'||'
	  END
	  SET @str20String = @str20String+'||||'
            
	  IF RTRIM(LTRIM(@cx_mobile)) <> ''
	  BEGIN
	    IF LEN(RTRIM(LTRIM(@cx_mobile))) > 10
	    BEGIN
	      SET @str20String = @str20String+'|'+LTRIM(RTRIM(@cx_mobile))+'|'
	    END	
	    ELSE
	    BEGIN
	      SET @str20String = @str20String+'91|'+LTRIM(RTRIM(@cx_mobile))+'|'
	    END	
	  END
	  ELSE  
	  BEGIN
	    SET @str20String = @str20String+'||'
	  END
	  SET @str20String = @str20String+'||'
	  IF ISNULL(@cx_email,'') = ''
	  BEGIN
	    SET @str20String = @str20String+'notprovided@notprovided.com|'
	  END
	  ELSE 
	  BEGIN
	    SET @str20String = @str20String+@cx_email+'|'
	  END
	  SET @str20String = @str20String+'|'+CONVERT(VARCHAR,GETDATE(),105)+'|HeadOffice|'+CONVERT(VARCHAR,GETDATE(),105)+'|01|'+LTRIM(RTRIM(@cx_VerifyBy))+'|'+LTRIM(RTRIM(ISNULL(@um_user_name,'')))+'|'
	  SET @str20String = @str20String+ISNULL(@um_designation,'')+'|'+ISNULL(@um_empCode,'')+'|'+@strCKYCCOMPNAME+'|'+@strCKYCFINO+'|1|0|01||'
	  SET @str20String = @str20String+'3|||||'
	
	  SET @str30String = @str30String +'30|'+CAST(@strCounter AS VARCHAR)+'|'+ISNULL(RTRIM(LTRIM(@cx_IdentityProof)),'')+'|'+ISNULL(RTRIM(LTRIM(@cx_IdentityProofID)),'')+'||||'
	  SET @str30String = @str30String +'02|||||'

	  SELECT @str70String =@str70String+'\r\n'+'70|'+CAST(@strCounter AS VARCHAR)+'|'+RTRIM(LTRIM(CAST(ISNULL(CI_SRNO,0) AS VARCHAR)))+'_'+CI_Type+'.'+CI_ContentType+'|'+
	  CASE WHEN CI_Type = 'ID' THEN '04' WHEN CI_Type = 'SG' THEN '09' WHEN CI_Type = 'PH' THEN '02' ELSE '' END+'||||||'
	  FROM Client_CKYCImages(NOLOCK) 
	  WHERE CI_Panno = @cx_Panno
	  AND CI_SRNO = @cx_srno
	  AND CI_Type IN('SG','ID','PH') 
   END 	
   FETCH NEXT FROM Cur0 INTO @StrClientCode
  END 
  CLOSE Cur0
  DEALLOCATE Cur0
  --SELECT @str20String, @str30String, @str70String
  DECLARE @StrRequestJson VARCHAR(MAX)=''
  
  SET @StrRequestJson = '{"baseFolderName":"##baseFolderName##","baseFileString": "##baseFileString##",
  "baseFileName":"##baseFileName##","subFileDetails":##subFileDetails##}'
  SET  @StrRequestJson  = REPLACE(@StrRequestJson,'##baseFolderName##',@strFolderName)
  SET  @StrRequestJson  = REPLACE(@StrRequestJson,'##baseFileName##',@StrTXTFileName)
  SET  @StrRequestJson  = REPLACE(@StrRequestJson,'##baseFileString##',@strHeaderString+'\r\n'+@str20String+'\r\n'+@str30String+@str70String)
    
  DECLARE @subFileDetails NVARCHAR(MAX)='' 
  SET @subFileDetails=(SELECT [subFolderName] = CAST(CI_SRNO AS VARCHAR), [fileName] =  CAST(CI_SRNO AS VARCHAR)+'_'+CI_Type+'.'+CI_ContentType,
  [fileBase64] =   CAST('' AS XML).value('xs:base64Binary(sql:column("[ImageColumn]"))', 'VARCHAR(MAX)')
  FROM (SELECT CONVERT(VARBINARY(MAX), CI_Image) as [ImageColumn], CI_SRNO , CI_Type, CI_ContentType
  FROM Client_CKYCImages(NOLOCK) XX
  WHERE CI_Panno IN(SELECT CK_Panno FROM Client_CKYC(NOLOCK) 
  WHERE CK_Panno IN(SELECT CM_PANNO FROM CLIENT_MASTER(NOLOCK) 
  WHERE CM_cD IN(SELECT CLIENTCODE FROM @tbl_Client)))
  AND CI_SRNO IN(SELECT MAX(CI_SRNO) FROM Client_CKYCImages(NOLOCK) WHERE CI_Panno = XX.CI_Panno)
  AND CI_Type IN('SG','ID','PH') ) x1 ORDER BY CAST(CI_SRNO AS VARCHAR) FOR JSON PATH)

  SET @StrRequestJson  = REPLACE(@StrRequestJson,'##subFileDetails##',@subFileDetails)
  --SELECT @StrRequestJson
  
  EXEC [dbo].[stpr_GetKRAEncryDecry] @StrRequestJson,'Download', @strThirdPartyURL,'','',  @o_DecyText OUTPUT  
  IF ISNULL(@o_DecyText,'') = ''
  BEGIN
    SET @o_vcErrorFlag = 'E'
    SET @o_vcErrorMessage = 'ISSUE IN FILE DOWNLOAD API'
	RETURN 1
  END
  ELSE
  BEGIN
    SET @o_vcErrorFlag = 'S'
	SET @o_vcErrorMessage = @o_DecyText
	
	UPDATE A SET sp_sysvalue = @strBatchNO
	FROM Sysparameter A 
	WHERE sp_parmcd='CKYCBATCHNO'
	
	UPDATE A SET CK_batchno = @strBatchNO, A.CK_batchDt = CONVERT(VARCHAR,GETDATE(),112), CK_STATUS = 'E'
	FROM Client_CKYC A 
	WHERE A.CK_PANNO IN(SELECT CM_PANNO FROM CLIENT_MASTER(NOLOCK) 
    WHERE CM_cD IN(SELECT CLIENTCODE FROM @tbl_Client))
	AND ck_srno IN(SELECT MAX(ck_srno) FROM Client_CKYC(NOLOCK) WHERE CK_Panno = A.CK_Panno)	
	RETURN 1
  END
END  
GO

CREATE PROCEDURE stpr_APICVLKRA @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(MAX) OUTPUT WITH ENCRYPTION AS
BEGIN
  --SET @vcXML = '<root>'+@vcXML+'</root>' 
  DECLARE @XMLData XML = CAST(@vcXML AS XML), @strClient VARCHAR(20)='', @strThirdPartyURL VARCHAR(200)='',
  @o_vcOutputJsonapi VARCHAR(MAX)='', @strKRAAPIPassword NVARCHAR(MAX)='', @strurl VARCHAR(MAX)='', @StrHeaderString VARCHAR(MAX)='',
  @strRefNo VARCHAR(10)=''
  BEGIN TRY
    SELECT @strClient =  ISNULL(x.value('(ClientCode)[1]', 'VARCHAR(500)'),''),
	@strRefNo =  ISNULL(x.value('(RefNo)[1]', 'VARCHAR(500)'),'')
    FROM @XMLData.nodes('/root') AS XTbl(x) 
  END TRY
  BEGIN CATCH
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Error in Parameter '+ ERROR_MESSAGE()
	RETURN 1
  END CATCH  

  IF ISNULL(@strClient,'') = ''
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'CLIENT CODE NOT FOUND'
	RETURN 1
  END 
  
  IF ISNULL(@strRefNo,'') = ''
    SET @strRefNo = '1'
  
  DECLARE @strParamString VARCHAR(MAX)='',  @strRequestString VARCHAR(MAX)='', @strtradeplustempdb VARCHAR(50)=''
  SELECT @strParamString = RequestJson, @strurl = APIUrl
  FROM tbl_VendorAPISetting(NOLOCK) where APIVendorName= 'CVLKRA' 
  AND APIName = 'GetToken' AND ISNULL(ISACTIVE,'N') = 'Y' 
  
  DECLARE @strLogSerialNo INT = 0
  
  DECLARE @tbl_ErrorLog TABLE(Process VARCHAR(50), Descp VARCHAR(MAX))
    
  SELECT @strThirdPartyURL = sp_sysvalue 
  FROM Sysparameter(NOLOCK) WHERE sp_parmcd = 'ThirdParty'
  
  SELECT @strtradeplustempdb = sp_sysvalue FROM WebParameter(NOLOCK) WHERE sp_parmcd = 'TRADEPLUSTEMPDB' 	

  IF ISNULL(@strThirdPartyURL,'') = ''
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Please Define Third Party Calling Local URL in Sysparameter AS sp_parmcd = ''ThirdParty'''
    RETURN 1
  END  
 
  IF ISNULL(@strParamString,'') = ''
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'SETTING NOT DEFINE'
	EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
    RETURN 1
  END
  
  
  DECLARE @tbl_jsonoutput TABLE(SerialNo INT, ColumnName VARCHAR(50), ColumnValue VARCHAR(MAX), 
  MasterTag VARCHAR(100), JSONLEVEL INT, MASTERLEVEL INT)
  
  DECLARE @jsonCutterOutput VARCHAR(MAX)='', @strDanSql NVARCHAR(MAX)=''
  DECLARE @JsonCutterXML XML, 
  @strCmschedule VARCHAR(20) = (SELECT TOP 1 sp_sysvalue FROM sysparameter(NOLOCK) WHERE sp_parmcd = 'CMSCHEDULE')
    
  
  DECLARE @SQLTEXTE varchar(max),  @OE_Value varchar(max)
  
  SET @SQLTEXTE ='' 
  
  SET @SQLTEXTE =  '<HTML> <BODY style="FONT-FAMILY: ''CALIBRI''">'
   + '<TABLE width="50%" border=0  style="FONT-SIZE: 14px; FONT-FAMILY: ''CALIBRI''">'
          + '<TR><TD VALIGN=TOP >Dear All,</TD></TR>'
	      + '<TR><TD VALIGN=TOP >Please Find CVL KRA ERROR REASON ARE :</TD></TR>'
          + '<TR></TR>'
          + '</table> <TABLE class="table table-striped" width="50%" border=0 ' 
          +'style="table-layout:fixed;FONT-SIZE: 15px; FONT-FAMILY: ''CALIBRI''">'
          + '</TABLE>'
  SET @SQLTEXTE = @SQLTEXTE  + '<TABLE class="table table-striped" style="FONT-SIZE: 14px; FONT-FAMILY: ''CALIBRI''" cellSpacing=0 cellPadding=0 width="80%"; border=1>'
          + '<TR>'
    
  SET  @SQLTEXTE = @SQLTEXTE  +' ##BODYTEXT##  </TR></table> ' 
    
  SET @SQLTEXTE = @SQLTEXTE +   '<TABLE width="100%" border=0  style="FONT-SIZE: 14px; FONT-FAMILY: ''CALIBRI''">' 
          + '<TR></TR>'
          + '<TR><TD VALIGN=TOP >Regards,</TD></TR>'
          + '<TR><TD VALIGN=TOP >RMS TEAM</TD></TR>'
          + '<TR></TR>'
          + '<TR><TD VALIGN=TOP >Note: This mail is auto generated. Please do not reply to this mail.</TD></TR>'
          + '</TABLE> </BODY> </HTML> '
  
  DECLARE @strposcd VARCHAR(200)='', @strusername VARCHAR(200)='', @strpassword VARCHAR(200)='', @strpasskey VARCHAR(200)='',
  @strEncykey VARCHAR(200)='', @o_DecyText VARCHAR(MAX)='', @strEncryptText NVARCHAR(MAX)='', @Striv NVARCHAR(MAX)='', @strToken NVARCHAR(MAX)='',
  @strPANNO VARCHAR(20)=''
  
  SELECT @strPANNO = CM_PANNO FROM CLIENT_MASTER(NOLOCK) WHERE CM_cD = @strClient

  
  DECLARE @strSFTRequesrt VARCHAR(MAX)='{"hostName": "##hostName##","username": "##username##","password": "##password##","port": ##port##,'
  +' "filePath": "##filePath##","files": [{"fileName": "##fileName1##","base64": "##base641##"}]}'
	  
  DECLARE @strAPIKRAUrl VARCHAR(100), @strKRARequest VARCHAR(MAX)='', @strKRAUserName VARCHAR(100),
  @strKRAUserPassword VARCHAR(100), @strPort VARCHAR(40)='', @strfilePath VARCHAR(MAX)='', @strFileName1 VARCHAR(200), @strbase64 NVARCHAR(MAX)
  SELECT @strAPIKRAUrl = APIUrl, @strKRARequest = RequestJson 
  FROM tbl_VendorAPISetting(NOLOCK) WHERE APIVendorName='CVLKRA'
  AND APIName = 'SFTP_UPLOAD'
	  
  IF ISNULL(@strKRARequest,'') <> ''	  
  BEGIN
    SELECT @strKRAUserName = VALUE
    FROM (SELECT * FROM DBO.fn_SplitString(@strKRARequest, '~')) X1
    WHERE X1.Position = 1
	  
    SELECT @strKRAUserPassword = VALUE
    FROM (SELECT * FROM DBO.fn_SplitString(@strKRARequest, '~')) X1
    WHERE X1.Position = 2
  
    SELECT @strPort = VALUE
    FROM (SELECT * FROM DBO.fn_SplitString(@strKRARequest, '~')) X1
    WHERE X1.Position = 3
  
    SELECT @strfilePath = VALUE
    FROM (SELECT * FROM DBO.fn_SplitString(@strKRARequest, '~')) X1
    WHERE X1.Position = 4
  
    SET @strfilePath = CAST(format(getdate(),'ddMMyyyy') AS VARCHAR)+'/'+'CVLKRA'
  
    SELECT @strbase64 = case when isnull(dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX)))),'') = ''
    THEN dbo.fnBinaryToBase64(ma_proof) else dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX)))) end 
    FROM Client_ModifyAttach where MA_cmcd=@strClient AND ma_refno IN(SELECT MAX(ma_refno)
    FROM Client_ModifyAttach where MA_cmcd=@strClient)
    AND ma_filename ='SignedKRAPdf' 
  
    SET @strSFTRequesrt = REPLACE(@strSFTRequesrt,'##hostName##',ISNULL(@strAPIKRAUrl,''))
    SET @strSFTRequesrt = REPLACE(@strSFTRequesrt,'##username##',ISNULL(@strKRAUserName,''))
    SET @strSFTRequesrt = REPLACE(@strSFTRequesrt,'##password##',ISNULL(@strKRAUserPassword,''))
    SET @strSFTRequesrt = REPLACE(@strSFTRequesrt,'##port##',ISNULL(@strPort,''))
    SET @strSFTRequesrt = REPLACE(@strSFTRequesrt,'##filePath##',ISNULL(@strfilePath,''))
    SET @strSFTRequesrt = REPLACE(@strSFTRequesrt,'##fileName1##',ISNULL(@strPANNO,@strClient)+'.PDF')
    SET @strSFTRequesrt = REPLACE(@strSFTRequesrt,'##base641##',ISNULL(@strbase64,''))
  
    SET @o_DecyText = ''
  
    EXEC [dbo].[stpr_GetKRAEncryDecry] @strSFTRequesrt,'Upload', @strThirdPartyURL,'','',  @o_DecyText OUTPUT  
	--SELECT @o_DecyText
  END
  
  IF ISNULL(@o_DecyText,'') <> 'File uploaded successfully.'
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Issue in Response for SFTP '
	EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- SFTP ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
    RETURN 1 
  END
  ELSE
  BEGIN
    SET @strbase64 = 0
	SELECT @strbase64 = case when isnull(dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX)))),'') = ''
    THEN dbo.fnBinaryToBase64(ma_proof) else dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX)))) end 
    FROM Client_ModifyAttach where MA_cmcd=@strClient AND ma_refno IN(SELECT MAX(ma_refno)
    FROM Client_ModifyAttach where MA_cmcd=@strClient)
    AND ma_filename ='DigilockerXML' 
	
	SET @strSFTRequesrt ='{"hostName": "##hostName##","username": "##username##","password": "##password##","port": ##port##,'
    +' "filePath": "##filePath##","files": [{"fileName": "##fileName1##","base64": "##base641##"}]}'
	
	SET @strSFTRequesrt = REPLACE(@strSFTRequesrt,'##hostName##',@strAPIKRAUrl)
    SET @strSFTRequesrt = REPLACE(@strSFTRequesrt,'##username##',@strKRAUserName)
    SET @strSFTRequesrt = REPLACE(@strSFTRequesrt,'##password##',@strKRAUserPassword)
    SET @strSFTRequesrt = REPLACE(@strSFTRequesrt,'##port##',@strPort)
    SET @strSFTRequesrt = REPLACE(@strSFTRequesrt,'##filePath##',@strfilePath)
    SET @strSFTRequesrt = REPLACE(@strSFTRequesrt,'##fileName1##',@strPANNO+'.XML')
    SET @strSFTRequesrt = REPLACE(@strSFTRequesrt,'##base641##',@strbase64)
  
    SET @o_DecyText = ''
    --SELECT @strSFTRequesrt
    EXEC [dbo].[stpr_GetKRAEncryDecry] @strSFTRequesrt,'Upload', @strThirdPartyURL,'','',  @o_DecyText OUTPUT 
	--select @o_DecyText
  END 
  
  IF ISNULL(@o_DecyText,'') <> 'File uploaded successfully.'
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Issue in XML for SFTP '+@o_DecyText
	EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- XML ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
    RETURN 1 
  END
  
  --select @strParamString
  SELECT @strposcd = VALUE
  FROM (SELECT * FROM DBO.fn_SplitString(@strParamString, '~')) X1
  WHERE X1.Position = 1 
  
  SELECT @strusername = VALUE
  FROM (SELECT * FROM DBO.fn_SplitString(@strParamString, '~')) X1
  WHERE X1.Position = 2 
  
  SELECT @strpassword = VALUE
  FROM (SELECT * FROM DBO.fn_SplitString(@strParamString, '~')) X1
  WHERE X1.Position = 3

  SELECT @strpasskey = VALUE
  FROM (SELECT * FROM DBO.fn_SplitString(@strParamString, '~')) X1
  WHERE X1.Position = 4  
  
  SELECT @strEncykey = VALUE
  FROM (SELECT * FROM DBO.fn_SplitString(@strParamString, '~')) X1
  WHERE X1.Position = 5  

  
  SET @StrHeaderString = '[{"key":"api_key","value":"##api_key##"}]'
  SET @StrHeaderString = REPLACE(@StrHeaderString,'##api_key##',@strpasskey)
  SET @strRequestString = '{"username": "##UserName##","poscode": "##PosCode##","password": "##Password##"}'
  SET @strRequestString = REPLACE(@strRequestString,'##UserName##',@strusername)
  SET @strRequestString = REPLACE(@strRequestString,'##PosCode##',@strposcd)
  SET @strRequestString = REPLACE(@strRequestString,'##Password##',@strpassword)
  
  EXEC [dbo].[stpr_GetKRAEncryDecry] @strRequestString, 'Encrypt', @strThirdPartyURL, @strEncykey, '', @o_DecyText OUTPUT 
  
   
  
  DELETE FROM @tbl_jsonoutput
  BEGIN TRY
	SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_DecyText+''' , @jsonCutterOutput OUTPUT';
    EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
    SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
    INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Encrypt '+@o_DecyText
	EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Encrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
    RETURN 1 		
  END CATCH 
	    
  IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'encryptedData' AND ColumnValue <> '')
  BEGIN
    SELECT @strEncryptText = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'encryptedData'
	SELECT @Striv = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'iv'
  END
  ELSE
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Encrypt '+@o_DecyText
	EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Encrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
    RETURN 1 		
  END 
  
  SET @o_DecyText = '"\"'+@Striv+':'+@strEncryptText+'\""'
  
  
  
  EXEC stpr_CallThirdPartyAPI 'CVLKRA-PASSWORD', 'CVLKRA', 'GetToken', '', @o_DecyText, '', 
	@strThirdPartyURL, 'API', @o_vcOutputJsonapi OUTPUT, @StrHeaderString,'' 
  
  IF ISNULL(@o_vcOutputJsonapi,'') = ''
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'CVLKRA-Token RESPONSE NOT FOUND'
	EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
    RETURN 1
  END
  ELSE
  BEGIN
    BEGIN TRY
      SELECT @Striv = VALUE
      FROM (SELECT * FROM DBO.fn_SplitString(@o_vcOutputJsonapi, ':')) X1
      WHERE X1.Position = 1
	   
	  SELECT @strEncryptText = VALUE
      FROM (SELECT * FROM DBO.fn_SplitString(@o_vcOutputJsonapi, ':')) X1
      WHERE X1.Position = 2 
	  SET @o_DecyText = ''
  
	  EXEC [dbo].[stpr_GetKRAEncryDecry] @strEncryptText, 'Decrypt', @strThirdPartyURL, @strEncykey, @Striv, @o_DecyText OUTPUT 
	  DELETE FROM @tbl_jsonoutput
	  BEGIN TRY
	    SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_DecyText+''' , @jsonCutterOutput OUTPUT';
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
        INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	   SET @o_vcErrorFlag = 'E'
	   SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Decrypt '+@o_DecyText
	   EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Decrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
       RETURN 1 		
     END CATCH 
	 
     IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'token' AND ISNULL(ColumnValue,'') <> '')
	 BEGIN
	   SET @strToken = ''
	   SELECT @strToken = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'token'
     END	 
	END TRY
    BEGIN CATCH
	  SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Token '+@o_vcOutputJsonapi
	  EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
      RETURN 1
    END CATCH  	
  END
  
  IF ISNULL(@strToken,'') <> ''
  BEGIN
    
    --- GET PAN STATUS

    INSERT INTO tbl_GenericAPIDebugLog(ParentSerialNo,RequestSource,RequestUniqueID,RequestString,ResponseString,ExceptionMessage,UpdateBy,UpdateTimeStamp)
    VALUES(CAST(@strRefNo AS INT),'CVLKRA',@strClient,@strRequestString,'','','SA',GETDATE())
	 
    SET @strLogSerialNo = IDENT_CURRENT('tbl_GenericAPIDebugLog')   
	
    SET @strRequestString = '{"pan":"##pan##","poscode":"##PosCode##"}'
	SET @strRequestString = REPLACE(@strRequestString,'##pan##',@strPANNO)
	SET @strRequestString = REPLACE(@strRequestString,'##PosCode##',@strposcd)
	 
	EXEC [dbo].[stpr_GetKRAEncryDecry] @strRequestString, 'Encrypt', @strThirdPartyURL, @strEncykey, '', @o_DecyText OUTPUT 
	 
    DELETE FROM @tbl_jsonoutput
    BEGIN TRY
	  SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_DecyText+''' , @jsonCutterOutput OUTPUT';
      EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
      SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
      INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	  SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Encrypt '+@o_DecyText
	  
	  UPDATE A SET A.ResponseString = @o_vcErrorMessage, A.ExceptionMessage = @o_DecyText
      FROM tbl_GenericAPIDebugLog A
      WHERE A.SerialNo =  @strLogSerialNo	
		
	  EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Encrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
      RETURN 1 		
    END CATCH 
	SET @strEncryptText = ''
    SET @Striv = ''	 
	IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'encryptedData' AND ColumnValue <> '')
    BEGIN
      SELECT @strEncryptText = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'encryptedData'
	  SELECT @Striv = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'iv'
    END
    ELSE
    BEGIN
      SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Encrypt '+@o_DecyText
	    
	  UPDATE A SET A.ResponseString = @o_vcErrorMessage, A.ExceptionMessage = @o_DecyText
      FROM tbl_GenericAPIDebugLog A
      WHERE A.SerialNo =  @strLogSerialNo	
	   
	  EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Encrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
      RETURN 1 		
    END 
  
    SET @o_DecyText = '"\"'+@Striv+':'+@strEncryptText+'\""'
	 
	SET @StrHeaderString = '[{"key":"Token","value":"##Token##"}]'
    SET @StrHeaderString = REPLACE(@StrHeaderString,'##Token##',@strToken)
	 
    SET @o_vcOutputJsonapi = ''
	 
	EXEC stpr_CallThirdPartyAPI 'CVLKRA-GetPanStatus', 'CVLKRA', 'GetPanStatus', '', @o_DecyText, '', 
	@strThirdPartyURL, 'API', @o_vcOutputJsonapi OUTPUT, @StrHeaderString, ''
	
	
	SET @strEncryptText = ''
    SET @Striv = ''	
	 
	IF ISNULL(@o_vcOutputJsonapi,'') = ''
    BEGIN
      SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = 'CVLKRA-UPDATEKYC RESPONSE NOT FOUND ' +@o_vcOutputJsonapi
	  
	    
	  UPDATE A SET A.ResponseString = @o_vcErrorMessage, A.ExceptionMessage = @o_vcOutputJsonapi
      FROM tbl_GenericAPIDebugLog A
      WHERE A.SerialNo =  @strLogSerialNo	
	   
	  EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN GetPanStatus ', @o_vcErrorMessage, '',@o_vcErrorMessage
      RETURN 1
    END
    ELSE
    BEGIN
      BEGIN TRY
        SELECT @Striv = VALUE
        FROM (SELECT * FROM DBO.fn_SplitString(@o_vcOutputJsonapi, ':')) X1
        WHERE X1.Position = 1
	   
	    SELECT @strEncryptText = VALUE
        FROM (SELECT * FROM DBO.fn_SplitString(@o_vcOutputJsonapi, ':')) X1
        WHERE X1.Position = 2 
	     
	    SET @o_DecyText = ''
	    EXEC [dbo].[stpr_GetKRAEncryDecry] @strEncryptText, 'Decrypt', @strThirdPartyURL, @strEncykey, @Striv, @o_DecyText OUTPUT 
	
		
	    DELETE FROM @tbl_jsonoutput 
		
	    BEGIN TRY
	      SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_DecyText+''' , @jsonCutterOutput OUTPUT';
          EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
          SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
          INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	       SET @o_vcErrorFlag = 'E'
	       SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Decrypt '+@o_DecyText
		   
		   UPDATE A SET A.ResponseString = @o_vcErrorMessage, A.ExceptionMessage = @o_DecyText
           FROM tbl_GenericAPIDebugLog A
           WHERE A.SerialNo =  @strLogSerialNo	
	   
		   
		   EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Decrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
           RETURN 1 		
        END CATCH 
		
		IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'resdtls' AND ISNULL(ColumnValue,'') <> '')
	    BEGIN
	       --SET @strToken = ''
	      SELECT @o_DecyText = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'resdtls'
		   
		  DELETE FROM @tbl_jsonoutput 
	      BEGIN TRY
	        SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_DecyText+''' , @jsonCutterOutput OUTPUT';
            EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
            SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
            INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	        SET @o_vcErrorFlag = 'E'
	        SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Decrypt '+@o_DecyText
		   
		    UPDATE A SET A.ResponseString = @o_vcErrorMessage, A.ExceptionMessage = @o_DecyText
            FROM tbl_GenericAPIDebugLog A
            WHERE A.SerialNo =  @strLogSerialNo	
	   
		   
		    EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Decrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
            RETURN 1 		
          END CATCH 
		END  
	   END TRY
      BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Token '+@o_vcOutputJsonapi

	    EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN GetPanStatus API ', @o_vcErrorMessage, '',@o_vcErrorMessage
        RETURN 1
      END CATCH  	
    END
	
	 	
    DECLARE @StrKRACode VARCHAR(10)='', @StrKRAPANStatus VARCHAR(MAX)='', @StrKRAPANCode VARCHAR(20)=''
    SELECT @StrKRACode = ColumnValue FROM @tbl_jsonoutput 
	WHERE COLUMNNAME = 'APP_STATUS'
	
	SELECT TOP 1 @StrKRAPANStatus = Status_Description, 
	@StrKRAPANCode = CASE WHEN CVLKRA = @StrKRACode THEN 'CVLKRA' 
    WHEN NDML = @StrKRACode THEN 'NDML' 
    WHEN DOTEX = @StrKRACode THEN 'DOTEX' 
    WHEN CAMS = @StrKRACode THEN 'CAMS' 
    WHEN KARVY = @StrKRACode THEN 'KARVY' ELSE '' END
    FROM tbl_KRAStatusCode(NOLOCK) WHERE (CVLKRA = @StrKRACode OR NDML = @StrKRACode OR DOTEX = @StrKRACode  OR CAMS = @StrKRACode
    OR KARVY = @StrKRACode ) 
	AND InsertFlag = 'I'
   
   -- VAIBHAV HERE   
	
	IF LTRIM(RTRIM(@StrKRAPANStatus)) NOT IN('Existing KYC Verified','KRA Validated','KRA Verified','KYC Registered with CVLMF', 'Existing KYC Hold')
	BEGIN
	  UPDATE A SET A.ResponseString = @StrKRAPANStatus+' WITH '+@StrKRAPANCode, A.ExceptionMessage = @o_DecyText
      FROM tbl_GenericAPIDebugLog A
      WHERE A.SerialNo =  @strLogSerialNo	
	  EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN GetPanStatus API ', @StrKRAPANStatus, '',@o_vcErrorMessage
	  SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = @StrKRAPANStatus+' WITH '+@StrKRAPANCode
      RETURN 1
	END
		
	DECLARE @dtBatchDate VARCHAR(20) = CONVERT(VARCHAR, getdate(), 103), @strUpdateInsertTag VARCHAR(2)=''
    
    DECLARE @cx_Status VARCHAR(1)='', @cx_Reason VARCHAR(200)='', @cx_srno INT = 0,
	@cx_mrkdt VARCHAR(10) = '', @cx_Panno VARCHAR(12) = '', @cx_sex VARCHAR(20)='', @cm_name VARCHAR(200)='', 
	@cx_name VARCHAR(200)='', @cx_FatherFname VARCHAR(100)='', @cx_FatherMname VARCHAR(100)='', @cx_FatherLname VARCHAR(100)='',
	@cx_dob VARCHAR(20)='', @cx_nationalcode VARCHAR(10)='', @cx_ResiTaxPurpose VARCHAR(100)='', @cx_add1 VARCHAR(200)='',
	@cx_add2 VARCHAR(200)='', @cx_add3 VARCHAR(200)='', @cx_add4 VARCHAR(200)='', @cx_pincode VARCHAR(20)='', @cx_state VARCHAR(100)='',
	@cx_BankActNo VARCHAR(25)='', @cx_stdoffice VARCHAR(20)='', @cx_tele1 VARCHAR(20)='', @cx_std VARCHAR(20)='',
	@cx_tele2 VARCHAR(20)='', @cx_mobile VARCHAR(20)='', @cx_fax VARCHAR(50)='', @cx_email VARCHAR(50)='',
	@cx_IdentityProof VARCHAR(100)='', @cx_IdentityProofID VARCHAR(100)='', @cx_IdentityProofExpDt VARCHAR(20)='',
	@cx_AddrProofID VARCHAR(50)='', @cx_AddrProofExpDt VARCHAR(50)='', @cx_padd1 VARCHAR(100)='',
	@cx_padd2 VARCHAR(100)='', @cx_padd3 VARCHAR(100)='', @cx_padd4 VARCHAR(100)='', @cx_ppincode VARCHAR(20)='',
	@cx_pstate VARCHAR(100)='', @cx_pcountry VARCHAR(100)='', @cx_PermAddrProof VARCHAR(100)='' , @cx_PermAddrProofID VARCHAR(30)='',
	@cx_PermAddrProofExpDt VARCHAR(8)='', @cx_grossincome VARCHAR(20)='', @cx_brboffcode VARCHAR(20)='',
	@cx_maritalstatus VARCHAR(20)='', @cx_networth VARCHAR(50)='', @cx_networthdt VARCHAR(8)='', @cx_VerifyBy VARCHAR(50)='',
	@cx_IdentityProofcd VARCHAR(20)='', @cx_AddrProofCd VARCHAR(20)='', @cx_PermAddrProofCd VARCHAR(20)='', @cx_AddrProof VARCHAR(20)=''
	
	SELECT @cx_VerifyBy = sp_sysvalue FROM Sysparameter(NOLOCK) WHERE sp_parmcd='CKYCVERIFYBY'
	
    SELECT @cx_Status = 'Y' , @cx_Reason = '', @cx_srno = ck_srno, @cx_mrkdt = CONVERT(VARCHAR,CAST(a.mkrdt AS DATE),103), 
    @cx_Panno = CK_Panno , @cx_sex = cm_sex , @cm_name = cm_name, 
	@cx_name = cm_name, @cx_FatherFname = CK_FatherFname , @cx_FatherMname = CK_FatherMname , 
	@cx_FatherLname = CK_FatherLname , 
	@cx_dob = CONVERT(VARCHAR,CAST(cm_dob AS DATE),103), @cx_nationalcode = cm_nationalcode , 
	@cx_ResiTaxPurpose = CK_ResiTaxPurpose , @cx_add1 = cm_add1 , 
	@cx_add2 = cm_add2 , @cx_add3 = cm_add3 , @cx_add4 = cm_add4 , @cx_pincode = cm_pincode, 
	@cx_state = ISNULL((SELECT CS_KRACode FROM CKYC_StaticCodes(NOLOCK) Where cs_datatype='State' and cs_description = cm_state),''), 
	@cx_BankActNo = ISNULL((SELECT CS_KRACode FROM CKYC_StaticCodes(NOLOCK) Where cs_datatype='Country' and cs_description = cm_BankActNo),'') , 
	@cx_stdoffice = cm_stdoffice , @cx_tele1 = cm_tele1 , @cx_std = cm_std , @cx_tele2 = cm_tele2, 
	@cx_mobile = cm_mobile, @cx_fax = cm_fax , @cx_email = Replace(cm_email, ',', ';'), 
	@cx_IdentityProof = CK_IdentityProof, @cx_IdentityProofID = CK_IdentityProofID, 
	@cx_IdentityProofExpDt = CK_IdentityProofExpDt, @cx_AddrProof = CK_AddrProof, 
	@cx_AddrProofID = CK_AddrProofID , @cx_AddrProofExpDt = CK_AddrProofExpDt , @cx_padd1 = cm_padd1 , 
	@cx_padd2 = cm_padd2 , @cx_padd3 = cm_padd3 , @cx_padd4 = cm_padd4 , @cx_ppincode = cm_ppincode , 
	@cx_pstate = ISNULL((SELECT CS_KRACode FROM CKYC_StaticCodes(NOLOCK) Where cs_datatype='State' and cs_description = cm_pstate),'') , 
	@cx_pcountry = ISNULL((SELECT CS_KRACode FROM CKYC_StaticCodes(NOLOCK) Where cs_datatype='Country' and cs_description = cm_pcountry),''), 
	@cx_PermAddrProof = CK_PermAddrProof , @cx_PermAddrProofID = CK_PermAddrProofID , 
	@cx_PermAddrProofExpDt = CK_PermAddrProofExpDt , @cx_grossincome = cm_grossincome , @cx_brboffcode = cm_brboffcode 
	, @cx_maritalstatus = cm_maritalstatus , @cx_networth = cm_networth , @cx_networthdt = cm_networthdt 
	--'' cx_Username, '' cx_Designation, '' cx_Statecd, 
	--'' cx_PStatecd, '' cx_CountryCd, '' cx_PCountryCd, '' cx_AddrProofCd, '' cx_PermAddrProofCd, '' cx_IdentityProofCd
    FROM Client_Master, Client_Info, Client_CKYC a
    WHERE cm_cd = cm2_cd AND cm_panno = CK_Panno 
	--AND isnull(Ck_NFiller2, 0) = 0 
	AND cm_freezeyn = 'N' 
	--AND Ck_Status = 'Y' 
    AND cm_schedule =  @strCmschedule
	/*AND cm_panno IN ( SELECT cm_panno FROM CLient_master(NOLOCK), Client_Info(NOLOCK)
		WHERE cm_cd = cm2_cd
		and cm_schedule = @strCmschedule
		GROUP BY cm_panno
		HAVING count(0) = 1) */
	AND CM_CD = @strClient
    AND ck_srno IN(SELECT MAX(ck_srno) FROM Client_CKYC(NOLOCK) WHERE CK_Panno = A.CK_Panno)
 	
	SELECT @cx_AddrProofCd = CS_KRACode FROM CKYC_StaticCodes(NOLOCK) 
	Where cs_datatype = 'AddressProof' and cs_Code = @cx_AddrProof
		
	SELECT @cx_PermAddrProofCd = CS_KRACode FROM CKYC_StaticCodes(NOLOCK) 
	Where cs_datatype = 'AddressProof' and cs_Code = @cx_PermAddrProof
	
    SELECT @cx_IdentityProofCd = CS_KRACode FROM CKYC_StaticCodes(NOLOCK) 
	Where cs_datatype = 'IdentityProof' and cs_Code = @cx_IdentityProof
	
    IF ISNULL(@cx_IdentityProofCd,'') = ''
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Identity Code (Not As per KRA Identity Proof List)')
	END
    
    IF ISNULL(@cx_sex,'') = ''
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Gender Not Found')
	END
    
    IF ISNULL(@cx_FatherFname,'') = ''
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Father name not avaiable')
	END
    
	IF ISNULL(@cx_dob,'') = ''
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Date of Birth not avaiable')
	END
    
	IF ISNULL(@cx_nationalcode,'') in('','0')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Nationality not avaiable')
	END
    
	IF ISNULL(@cx_add1,'') in('')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Correspondence Address Details not avaiable')
	END
    
	IF ISNULL(@cx_add4,'') in('')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Correspondence Address City not avaiable')
	END
    
	IF ISNULL(@cx_add4,'') in('')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Correspondence Address City not avaiable')
	END
    
	IF ISNULL(@cx_pincode,'') in('')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Correspondence Address Pincode not avaiable')
	END
    
	IF ISNULL(@cx_state,'') in('')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Correspondence Address State (Not As per KRA State List) not avaiable')
	END
	
	IF ISNULL(@cx_BankActNo,'') in('')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Correspondence Address Country (Not As per KRA Country List) not avaiable')
	END
	
	IF ISNULL(@cx_AddrProofCd,'') in('')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Correspondence Address Proof (Not As per KRA Address Proof List) not avaiable')
	END
	
	IF ISNULL(@cx_padd1,'') in('')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Permanent Address Details not avaiable')
	END
	
	IF ISNULL(@cx_padd4,'') in('')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Permanent Address City not avaiable')
	END
	
	IF ISNULL(@cx_ppincode,'') in('')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Permanent Address Pincode not avaiable')
	END
	
	IF ISNULL(@cx_PState,'') in('')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Permanent Address State (Not As per KRA State List) not avaiable')
	END
	
	IF ISNULL(@cx_PCountry,'') in('')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Permanent Address Country (Not As per KRA Country List) not avaiable')
	END
	
	IF ISNULL(@cx_PermAddrProofCd,'') in('')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Permanent Address Proof (Not As per KRA Address Proof List) not avaiable')
	END
    
	IF ISNULL(@cx_maritalstatus,'') in('')
	BEGIN
	  INSERT INTO @tbl_ErrorLog(Process, Descp)
	  VALUES('CVL-KRA_'+@strClient,'Marital Status not avaiable')
	END
    
	
	IF EXISTS(SELECT 1 FROM @tbl_ErrorLog)
	BEGIN
	  IF EXISTS (SELECT * FROM tempdb.dbo.sysobjects o WHERE o.xtype IN ('U') 
      AND o.id = object_id(N'tempdb..##tbl_EmailBodyODINPGLimit'))
      BEGIN
        DROP TABLE tempdb..##tbl_EmailBodyODINPGLimit
      END 
	

	  CREATE TABLE ##tbl_EmailBodyODINPGLimit(Process VARCHAR(50), Particular VARCHAR(MAX))
	
	  INSERT INTO ##tbl_EmailBodyODINPGLimit(Process, Particular)
      SELECT Process, Descp
      FROM @tbl_ErrorLog
	  DECLARE @stringtag VARCHAR(MAx)=''
	  SELECT @stringtag = @stringtag+'<br>'+Particular 
	  FROM ##tbl_EmailBodyODINPGLimit
	  
	  UPDATE A SET A.ResponseString = @stringtag
      FROM 	tbl_GenericAPIDebugLog A
	  WHERE SerialNo = @strLogSerialNo
     
      SET @OE_Value  =''
      EXEC Proc_GenerateMailString 'tempdb..##tbl_EmailBodyODINPGLimit', @OE_Value output
      SET @SQLTEXTE = REPLACE(@SQLTEXTE,'##BODYTEXT##',@OE_Value)
      EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- ERROR IN API ', @SQLTEXTE, '',''
	  RETURN 1
	END
	
	
	DECLARE @strKraStatus VARCHAR(1)=''
	SELECT @strKraStatus = cn_KRAStatus FROM Client_Nominee(NOLOCK) WHERE cn_cd =  @strClient
	
	IF LTRIM(RTRIM(@StrKRAPANCode)) = 'CVLKRA'
	BEGIN
	  SET @strUpdateInsertTag = '02'
	END
	ELSE
	BEGIN
	  SET @strUpdateInsertTag = '99'
	END
	
    DECLARE @cx_Username VARCHAR(200)='', @cx_Designation VARCHAR(100)='', @strKraCompanyName VARCHAR(200)=''
    SELECT @cx_Username = um_user_name, 
	@cx_Designation = um_designation From User_master(NOLOCK) 
	WHERE um_user_id = @CX_verifyBy and  Rtrim(Ltrim(@CX_verifyBy)) <> ''
	
	SELECT @strKraCompanyName = sp_sysvalue FROM Sysparameter WHERE sp_parmcd='KRACOMPNAME'
	SET @strurl = ''
	SELECT @strurl = APIUrl
    FROM tbl_VendorAPISetting(NOLOCK) where APIVendorName= 'CVLKRA' 
    AND APIName = 'InsertUpdateKYCRecord' AND ISNULL(ISACTIVE,'N') = 'Y'

    SET @strRequestString = '{"ROOT":{"HEADER":{"COMPANY_CODE":"##POSCODE##","BATCH_DATE":"##BATCH_DATE##"},"KYCDATA":{"APP_UPDTFLG": "##APP_UPDTFLG##",'
	SET @strRequestString = @strRequestString+'"APP_KRA_CODE": "##KRA_CODE##",'
	SET @strRequestString = @strRequestString+'"APP_POS_CODE": "##POSCODE##","APP_TYPE": "I","APP_NO": "##APP_NO##","APP_DATE": "##APP_DATE##","APP_PAN_NO": "##PAN##",'
    SET @strRequestString = @strRequestString+'"APP_PAN_COPY":"##PAN_COPY##","APP_EXMT": "N","APP_EXMT_CAT": "","APP_EXMT_ID_PROOF": "##EXMT_ID_PROOF##","APP_IPV_FLAG": "##IPV_FLAG##",'
	SET @strRequestString = @strRequestString+'"APP_IPV_DATE": "##IPV_DATE##","APP_GEN": "##GEN##","APP_NAME": "##NAME##","APP_F_NAME": "##F_NAME##","APP_REGNO": "",'
    SET @strRequestString = @strRequestString+'"APP_DOB_INCORP": "##DOB_INCORP##","APP_COMMENCE_DT": "","APP_NATIONALITY": "##NATIONALITY##","APP_OTH_NATIONALITY": "",'
    SET @strRequestString = @strRequestString+'"APP_COMP_STATUS": "","APP_OTH_COMP_STATUS": "","APP_RES_STATUS": "R","APP_RES_STATUS_PROOF": "##RES_STATUS_PROOF##",'
	SET @strRequestString = @strRequestString+'"APP_UID_NO": "","APP_COR_ADD1": "##COR_ADD1##","APP_COR_ADD2": "##COR_ADD2##","APP_COR_ADD3": "##COR_ADD3##",'
	SET @strRequestString = @strRequestString+'"APP_COR_CITY": "##COR_CITY##","APP_COR_PINCD": "##COR_PINCD##","APP_COR_STATE": "##COR_STATE##","APP_COR_CTRY": "##COR_CTRY##",'
	SET @strRequestString = @strRequestString+'"APP_OFF_ISD": "","APP_OFF_STD": "##OFF_STD##","APP_OFF_NO": "##OFF_NO##","APP_RES_ISD": "","APP_RES_STD": "##RES_STD##",'
	SET @strRequestString = @strRequestString+'"APP_RES_NO": "##RES_NO##","APP_MOB_ISD": 91,"APP_MOB_NO": "##MOB_NO##","APP_FAX_ISD": "","APP_FAX_STD": "",'
	SET @strRequestString = @strRequestString+'"APP_FAX_NO": "","APP_EMAIL": "##EMAIL##","APP_COR_ADD_PROOF": "##COR_ADD_PROOF##","APP_COR_ADD_REF": "##COR_ADD_REF##",'
	SET @strRequestString = @strRequestString+'"APP_COR_ADD_DT": "##COR_ADD_DT##","APP_PER_ADD_FLAG": "Y","APP_PER_ADD1": "##PER_ADD1##","APP_PER_ADD2": "##PER_ADD2##",'
	SET @strRequestString = @strRequestString+'"APP_PER_ADD3": "##PER_ADD3##","APP_PER_CITY": "##PER_CITY##","APP_PER_PINCD": "##PER_PINCD##","APP_PER_STATE": "##PER_STATE##",'
	SET @strRequestString = @strRequestString+'"APP_PER_CTRY": "##PER_CTRY##","APP_PER_ADD_PROOF": "##PER_ADD_PROOF##","APP_PER_ADD_REF": "##PER_ADD_REF##","APP_PER_ADD_DT": "##PER_ADD_DT##",'
	SET @strRequestString = @strRequestString+'"APP_INCOME": "","APP_OCC": "","APP_OTH_OCC": "","APP_POL_CONN": "","APP_DOC_PROOF": "S","APP_INTERNAL_REF": "##INTERNAL_REF##",'
	SET @strRequestString = @strRequestString+'"APP_BRANCH_CODE": "##BRANCH_CODE##","APP_MAR_STATUS": "##MAR_STATUS##","APP_NETWRTH": "##NETWRTH##","APP_NETWORTH_DT": "##NETWORTH_DT##",'
	SET @strRequestString = @strRequestString+'"APP_INCORP_PLC": "","APP_OTHERINFO": "","APP_FILLER1": "","APP_FILLER2": "","APP_FILLER3": "","APP_IPV_NAME": "##IPV_NAME##",'
	SET @strRequestString = @strRequestString+'"APP_IPV_DESG": "##IPV_DESG##","APP_IPV_ORGAN": "##IPV_ORGAN##","APP_KYC_MODE": 5,"APP_VER_NO": "V29","APP_VID_NO": "",'
	SET @strRequestString = @strRequestString+'"APP_UID_TOKEN": "","APP_AUTH_NAME": "","APP_AUTH_EMAIL": "","APP_AUTH_EMAIL1": "","APP_AUTH_EMAIL2": "",'
	SET @strRequestString = @strRequestString+'"APP_AUTH_MOBILE": "","APP_AUTH_FPICONSENT": "","APP_AUTH_UBOCONSENT": "","APP_FATCA_APPLICABLE_FLAG": "","APP_FATCA_BIRTH_PLACE": "",'
	SET @strRequestString = @strRequestString+'"APP_FATCA_BIRTH_COUNTRY": "","APP_FATCA_COUNTRY_RES": "","APP_FATCA_COUNTRY_CITYZENSHIP": "","APP_FATCA_DATE_DECLARATION": "##APP_FATCA_DATE_DECLARATION##"'
	SET @strRequestString = @strRequestString+'},"FATCA_ADDL_DTLS": {"APP_FATCA_ENTITY_PAN": "","APP_FATCA_COUNTRY_RESIDENCY": "","APP_FATCA_TAX_IDENTIFICATION_NO": "",'
	SET @strRequestString = @strRequestString+'"APP_FATCA_TAX_EXEMPT_FLAG": "","APP_FATCA_TAX_EXEMPT_REASON": ""},"FOOTER": {"NO_OF_KYC_RECORDS": 1,'
	SET @strRequestString = @strRequestString+'"APP_FATCADATA_RECORDS": "0","NO_OF_ADDLDATA_RECORDS": 0}}}'
      
	  set @strRequestString = REPLACE(@strRequestString ,'##KRA_CODE##',LTRIM(RTRIM(@StrKRAPANCode)))
	  set @strRequestString = REPLACE(@strRequestString ,'##POSCODE##',LTRIM(RTRIM(@strposcd)))
      set @strRequestString = REPLACE(@strRequestString ,'##BATCH_DATE##',LTRIM(RTRIM(@dtBatchDate)))
	  set @strRequestString = REPLACE(@strRequestString ,'##APP_UPDTFLG##',LTRIM(RTRIM(@strUpdateInsertTag)))
	  set @strRequestString = REPLACE(@strRequestString ,'##APP_NO##',LTRIM(RTRIM(CAST(@cx_srno AS VARCHAR))))
	  set @strRequestString = REPLACE(@strRequestString ,'##APP_DATE##',LTRIM(RTRIM(CAST(@cx_mrkdt AS VARCHAR))))
	  set @strRequestString = REPLACE(@strRequestString ,'##PAN##',LTRIM(RTRIM(@cx_Panno)))
	  set @strRequestString = REPLACE(@strRequestString ,'##PAN_COPY##',LTRIM(RTRIM((CASE WHEN ISNULL(@cx_Panno,'') <> '' THEN 'Y' ELSE 'N' END))))
	  set @strRequestString = REPLACE(@strRequestString ,'##EXMT_ID_PROOF##',LTRIM(RTRIM(@cx_IdentityProofcd)))
	  set @strRequestString = REPLACE(@strRequestString ,'##IPV_FLAG##','Y')
	  set @strRequestString = REPLACE(@strRequestString ,'##IPV_DATE##',LTRIM(RTRIM(@cx_mrkdt)))
	  set @strRequestString = REPLACE(@strRequestString ,'##GEN##',LTRIM(RTRIM(@cx_sex)))
	  set @strRequestString = REPLACE(@strRequestString ,'##NAME##',LTRIM(RTRIM(@cx_name)))
	  set @strRequestString = REPLACE(@strRequestString ,'##F_NAME##',LTRIM(RTRIM(@cx_FatherFname))+' '+LTRIM(RTRIM(@cx_FatherMname))+' '+LTRIM(RTRIM(@cx_FatherLname)))
	  set @strRequestString = REPLACE(@strRequestString ,'##DOB_INCORP##',LTRIM(RTRIM(@cx_dob)))
	  set @strRequestString = REPLACE(@strRequestString ,'##NATIONALITY##',LTRIM(RTRIM((CASE WHEN @cx_nationalcode IN('1','01') THEN '01' ELSE '02' END))))
	  set @strRequestString = REPLACE(@strRequestString ,'##RES_STATUS_PROOF##',LTRIM(RTRIM(@cx_ResiTaxPurpose)))
	  set @strRequestString = REPLACE(@strRequestString ,'##COR_ADD1##',LTRIM(RTRIM(REPLACE(@cx_add1,'&',''))))
	  set @strRequestString = REPLACE(@strRequestString ,'##COR_ADD2##',LTRIM(RTRIM(REPLACE(@cx_add2,'&',''))))
	  set @strRequestString = REPLACE(@strRequestString ,'##COR_ADD3##',LTRIM(RTRIM(REPLACE(@cx_add3,'&',''))))
	  set @strRequestString = REPLACE(@strRequestString ,'##COR_CITY##',LTRIM(RTRIM(REPLACE(@cx_add4,'&',''))))
	  set @strRequestString = REPLACE(@strRequestString ,'##COR_PINCD##',LTRIM(RTRIM(@cx_pincode)))
	  set @strRequestString = REPLACE(@strRequestString ,'##COR_STATE##',LTRIM(RTRIM(@cx_state)))
	  set @strRequestString = REPLACE(@strRequestString ,'##COR_CTRY##',LTRIM(RTRIM(@cx_BankActNo)))
	  set @strRequestString = REPLACE(@strRequestString ,'##OFF_STD##',LTRIM(RTRIM(@cx_stdoffice)))
	  set @strRequestString = REPLACE(@strRequestString ,'##OFF_NO##',LTRIM(RTRIM(@cx_tele1)))
	  Set @strRequestString = REPLACE(@strRequestString ,'##RES_STD##',LTRIM(RTRIM(@cx_std)))
	  set @strRequestString = REPLACE(@strRequestString ,'##RES_NO##',LTRIM(RTRIM(@cx_tele2)))
	  set @strRequestString = REPLACE(@strRequestString ,'##MOB_NO##',LTRIM(RTRIM(@cx_mobile)))
	  set @strRequestString = REPLACE(@strRequestString ,'##EMAIL##',LTRIM(RTRIM(CASE WHEN ISNULL(@cx_email,'') = '' THEN 'notprovided@notprovided.com' ELSE @cx_email END)))
	  Set @strRequestString = REPLACE(@strRequestString ,'##COR_ADD_PROOF##',LTRIM(RTRIM(@cx_AddrProofcd)))
	  set @strRequestString = REPLACE(@strRequestString ,'##COR_ADD_REF##',LTRIM(RTRIM(@cx_AddrProofID)))
	  set @strRequestString = REPLACE(@strRequestString ,'##COR_ADD_DT##',LTRIM(RTRIM(@cx_AddrProofExpDt)))
	  set @strRequestString = REPLACE(@strRequestString ,'##PER_ADD1##',LTRIM(RTRIM(REPLACE(@cx_padd1,'&',''))))
	  set @strRequestString = REPLACE(@strRequestString ,'##PER_ADD2##',LTRIM(RTRIM(REPLACE(@cx_padd2,'&',''))))
	  set @strRequestString = REPLACE(@strRequestString ,'##PER_ADD3##',LTRIM(RTRIM(REPLACE(@cx_padd3,'&',''))))
	  set @strRequestString = REPLACE(@strRequestString ,'##PER_CITY##',LTRIM(RTRIM(REPLACE(@cx_padd4,'&',''))))
	  set @strRequestString = REPLACE(@strRequestString ,'##PER_PINCD##',LTRIM(RTRIM(@cx_ppincode)))
	  set @strRequestString = REPLACE(@strRequestString ,'##PER_CTRY##',LTRIM(RTRIM(@cx_pcountry)))
	  set @strRequestString = REPLACE(@strRequestString ,'##PER_STATE##',LTRIM(RTRIM(@cx_pstate)))
	  set @strRequestString = REPLACE(@strRequestString ,'##PER_ADD_PROOF##',LTRIM(RTRIM(@cx_PermAddrProofcd)))
	  set @strRequestString = REPLACE(@strRequestString ,'##PER_ADD_REF##',LTRIM(RTRIM(@cx_PermAddrProofID)))
	  set @strRequestString = REPLACE(@strRequestString ,'##PER_ADD_DT##',LTRIM(RTRIM(@cx_PermAddrProofExpDt)))
	  set @strRequestString = REPLACE(@strRequestString ,'##INTERNAL_REF##',LTRIM(RTRIM(CAST(@cx_SRNO AS VARCHAR))))
	  set @strRequestString = REPLACE(@strRequestString ,'##BRANCH_CODE##',LTRIM(RTRIM(@cx_brboffcode)))
	  set @strRequestString = REPLACE(@strRequestString ,'##MAR_STATUS##',LTRIM(RTRIM(CASE WHEN @cx_maritalstatus = 'M' THEN '01' ELSE '02' END)))
	  set @strRequestString = REPLACE(@strRequestString ,'##NETWRTH##',LTRIM(RTRIM(CASE WHEN @cx_networth <> '' AND @cx_networthdt <> '' THEN @cx_networth ELSE '' END)))
	  set @strRequestString = REPLACE(@strRequestString ,'##NETWORTH_DT##',LTRIM(RTRIM(CASE WHEN @cx_networth <> '' AND @cx_networthdt <> '' THEN @cx_networthdt ELSE '' END)))
	  set @strRequestString = REPLACE(@strRequestString ,'##IPV_NAME##',LTRIM(RTRIM(@cx_Username)))
	  set @strRequestString = REPLACE(@strRequestString ,'##IPV_DESG##',LTRIM(RTRIM(@cx_Designation)))
	  set @strRequestString = REPLACE(@strRequestString ,'##IPV_ORGAN##',LTRIM(RTRIM(@strKraCompanyName)))
	  set @strRequestString = REPLACE(@strRequestString ,'##APP_FATCA_DATE_DECLARATION##',cast(FORMAT(GETDATE(),'dd/MM/yyyy') as varchar))
     
	 --set @strLogSerialNo = 0
	 
     UPDATE A SET A.RequestString = @strRequestString
     FROM tbl_GenericAPIDebugLog A
     WHERE A.SerialNo =  @strLogSerialNo	
	 
	 EXEC [dbo].[stpr_GetKRAEncryDecry] @strRequestString, 'Encrypt', @strThirdPartyURL, @strEncykey, '', @o_DecyText OUTPUT 
	 
     DELETE FROM @tbl_jsonoutput
     BEGIN TRY
	    SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_DecyText+''' , @jsonCutterOutput OUTPUT';
        EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
        SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
        INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	 
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Encrypt '+@o_DecyText
		
		UPDATE A SET A.ResponseString = @o_DecyText, A.ExceptionMessage = @o_vcErrorMessage
        FROM tbl_GenericAPIDebugLog A
        WHERE A.SerialNo =  @strLogSerialNo		
		
	    EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Encrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
        RETURN 1 		
     END CATCH 
	 
	 SET @strEncryptText = ''
     SET @Striv = ''	 
	 IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'encryptedData' AND ColumnValue <> '')
     BEGIN
       SELECT @strEncryptText = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'encryptedData'
	   SELECT @Striv = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'iv'
     END
     ELSE
     BEGIN
       SET @o_vcErrorFlag = 'E'
	   SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Encrypt '+@o_DecyText
	   
	   UPDATE A SET A.ResponseString = @o_DecyText, A.ExceptionMessage = @o_vcErrorMessage
       FROM tbl_GenericAPIDebugLog A
       WHERE A.SerialNo =  @strLogSerialNo		

	   
	   EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Encrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
       RETURN 1 		
     END 
  
     SET @o_DecyText = '"\"'+@Striv+':'+@strEncryptText+'\""'
	 
	 SET @StrHeaderString = '[{"key":"Token","value":"##Token##"}]'
     SET @StrHeaderString = REPLACE(@StrHeaderString,'##Token##',@strToken)
	 
     SET @o_vcOutputJsonapi = ''
	 
	 EXEC stpr_CallThirdPartyAPI 'CVLKRA-UPDATEKYC', 'CVLKRA', 'InsertUpdateKYCRecord', '', @o_DecyText, '', 
	 @strThirdPartyURL, 'API', @o_vcOutputJsonapi OUTPUT, @StrHeaderString, ''
	
	 
	 SET @strEncryptText = ''
     SET @Striv = ''	
	 
	 IF ISNULL(@o_vcOutputJsonapi,'') = ''
     BEGIN
       SET @o_vcErrorFlag = 'E'
	   SET @o_vcErrorMessage = 'CVLKRA-UPDATEKYC RESPONSE NOT FOUND ' +@o_vcOutputJsonapi
	   
	   UPDATE A SET A.ResponseString = @o_vcOutputJsonapi, A.ExceptionMessage = @o_vcErrorMessage
       FROM tbl_GenericAPIDebugLog A
       WHERE A.SerialNo =  @strLogSerialNo		
   
	   
	   EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
       RETURN 1
     END
     ELSE
     BEGIN
       BEGIN TRY
         SELECT @Striv = VALUE
         FROM (SELECT * FROM DBO.fn_SplitString(@o_vcOutputJsonapi, ':')) X1
         WHERE X1.Position = 1
	   
	     SELECT @strEncryptText = VALUE
         FROM (SELECT * FROM DBO.fn_SplitString(@o_vcOutputJsonapi, ':')) X1
         WHERE X1.Position = 2 
	     
		 SET @o_DecyText = ''
	     EXEC [dbo].[stpr_GetKRAEncryDecry] @strEncryptText, 'Decrypt', @strThirdPartyURL, @strEncykey, @Striv, @o_DecyText OUTPUT 
	     DELETE FROM @tbl_jsonoutput 
	     BEGIN TRY
	       SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@o_DecyText+''' , @jsonCutterOutput OUTPUT';
           EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
           SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
           INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	       SET @o_vcErrorFlag = 'E'
	       SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Decrypt '+@o_DecyText
		   
		   UPDATE A SET A.ResponseString = @o_DecyText, A.ExceptionMessage = @o_vcErrorMessage
           FROM tbl_GenericAPIDebugLog A
           WHERE A.SerialNo =  @strLogSerialNo		

		   
	       EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Decrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
           RETURN 1 		
         END CATCH 
		 
		 
		 
         IF EXISTS(SELECT 1 FROM @tbl_jsonoutput WHERE ColumnName = 'resdtls' AND ISNULL(ColumnValue,'') <> '')
	     BEGIN
	       SET @strToken = ''
	       SELECT @strToken = ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'resdtls'
		   
		   DELETE FROM @tbl_jsonoutput 
	       BEGIN TRY
	         SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@strToken+''' , @jsonCutterOutput OUTPUT';
             EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
             SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
             INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	         SET @o_vcErrorFlag = 'E'
	         SET @o_vcErrorMessage = 'Issue in Response for CVLKRA '+@strToken
			 
			 UPDATE A SET A.ResponseString = @strToken, A.ExceptionMessage = @o_vcErrorMessage
             FROM tbl_GenericAPIDebugLog A
             WHERE A.SerialNo =  @strLogSerialNo		
			 
	         EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Decrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
             RETURN 1 		
           END CATCH 
         END	 
	  END TRY
      BEGIN CATCH
	    SET @o_vcErrorFlag = 'E'
	    SET @o_vcErrorMessage = 'Issue in Response for CVLKRA-Token '+@o_vcOutputJsonapi
		
 	    UPDATE A SET A.ResponseString = @o_vcOutputJsonapi, A.ExceptionMessage = @o_vcErrorMessage
        FROM tbl_GenericAPIDebugLog A
        WHERE A.SerialNo =  @strLogSerialNo		

		
	    EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
        RETURN 1
      END CATCH  	
    END
	
    declare @strErrorKMessage VARCHAR(MAX)= ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'ERROR_MSG'),'')
	declare @strErrorKCode VARCHAR(MAX)= ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'ERROR_CODE'),'')
	IF ISNULL(@strErrorKMessage,'') <> '' AND @strErrorKCode LIKE 'WEBERR%'
	BEGIN
	  UPDATE A SET A.ResponseString = @strErrorKMessage, A.ExceptionMessage = @strToken
      FROM tbl_GenericAPIDebugLog A
      WHERE A.SerialNo =  @strLogSerialNo

      SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = 'ERROR MESSAGE '+@strErrorKMessage
	  RETURN 1
	END
	
    DECLARE @strOutputPAN VARCHAR(20) = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'resdtls'),''), 
	@strAckNo VARCHAR(20)=ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'APP_MODF_ACK'),''),
	@strAckStatus VARCHAR(20)=ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'APP_STATUS'),'')
		
    SET @strOutputPAN = REPLACE(@strOutputPAN,'\"','"')
	
	  
	DELETE FROM @tbl_jsonoutput 
	BEGIN TRY
	  SET @strDanSql = 'EXEC '+@strtradeplustempdb+'.DBO.sp_JsonCutter'+' '''+@strToken+''' , @jsonCutterOutput OUTPUT';
      EXEC sp_executesql @strDanSql, N'@jsonCutterOutput VARCHAR(MAX) OUTPUT', @jsonCutterOutput OUTPUT;
      SET @JsonCutterXML = CAST(@jsonCutterOutput AS XML)
      INSERT INTO @tbl_jsonoutput(SerialNo, ColumnName, ColumnValue,  MasterTag, JSONLEVEL, MASTERLEVEL)
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
	  SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = 'Issue in Response for CVLKRA '+@strToken
			 
	  UPDATE A SET A.ResponseString = @strOutputPAN, A.ExceptionMessage = 'Error'
      FROM tbl_GenericAPIDebugLog A
      WHERE A.SerialNo =  @strLogSerialNo		
			 
	  EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- Decrypt ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
      RETURN 1 		
    END CATCH 
	  
	DECLARE @strAPP_STATUS VARCHAR(20) = ISNULL((SELECT ColumnValue FROM @tbl_jsonoutput WHERE ColumnName = 'APP_STATUS'),''),
	@strAPP_STATUSText VARCHAR(200)=''
	  
	SELECT TOP 1 @strAPP_STATUSText = CS_Description FROM CKYC_StaticCodes WHERE CS_DataType='AppStatus' AND CS_DataTypeCd = @strAPP_STATUS
		
	IF @strAckStatus NOT LIKE 'ERR-%' AND @strAckNo <> '0' AND @strAckStatus NOT LIKE 'WEBERR-%'
	BEGIN
	  
 	  UPDATE A SET A.ResponseString = @strAPP_STATUSText, A.ExceptionMessage = CASE WHEN ISNULL(@strAPP_STATUSText, '') ='' THEN 
	  @o_DecyText ELSE 'Process Executed' END
      FROM tbl_GenericAPIDebugLog A
      WHERE A.SerialNo =  @strLogSerialNo	

      UPDATE A SET A.Ck_NFiller2 = 1
      FROM Client_CKYC A
      WHERE A.ck_srno = @cx_srno
      AND A.CK_Panno =  @cx_Panno 
	END  
	ELSE
	BEGIN
	  SET @o_vcErrorFlag = 'E'
	  SET @o_vcErrorMessage = 'CVLKRA-UPDATEKYC RESPONSE NOT FOUND '+@strToken
	  
	  UPDATE A SET A.ResponseString = CASE WHEN ISNULL(@strAPP_STATUSText,'') = '' THEN @strAPP_STATUS ELSE ISNULL(@strAPP_STATUSText,'') END, 
	  A.ExceptionMessage = @o_vcErrorMessage
      FROM tbl_GenericAPIDebugLog A
      WHERE A.SerialNo =  @strLogSerialNo		
	  
	  EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
      RETURN 1
    END 
  
	SET @o_vcErrorFlag = 'S'
	SET @o_vcErrorMessage = 'Process Completed'
	RETURN 1
  END
  ELSE
  BEGIN
    SET @o_vcErrorFlag = 'E'
	SET @o_vcErrorMessage = 'CVLKRA-PASSWORD RESPONSE NOT FOUND'
	EXEC stpr_GenerateAPIMailAlerts 'CVLKRA', @strThirdPartyURL, ' :- CVLKRA ERROR IN API ', @o_vcErrorMessage, '',@o_vcErrorMessage
    RETURN 1
  END
END   
GO

CREATE PROCEDURE [dbo].[SP_GetPdfBase64](@userId varchar(20), @fileName varchar(30), @refNo int)
WITH ENCRYPTION
AS
  Begin
	Select case when isnull(dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX), REPLACE(ma_proof,'data:image/jpeg;base64,','')) 
	AS NVARCHAR(MAX)))),'') = ''  THEN dbo.fnBinaryToBase64(ma_proof) else dbo.fnBinaryToBase64(DBO.FN_base64toBinary(CAST(CONVERT(varchar(MAX),
	REPLACE(ma_proof,'data:image/jpeg;base64,','')) AS NVARCHAR(MAX)))) end as pdfFile
	From Client_ModifyAttach (NoLock) 
	where   ma_cmcd = @userId and ma_filename = @fileName and ma_refno = @refNo And ma_status = 'N'
 End
GO 

