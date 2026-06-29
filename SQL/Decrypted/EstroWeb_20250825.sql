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

CREATE FUNCTION [dbo].[fn_EncryptStringTW](@strInput NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
WITH ENCRYPTION
AS
BEGIN
    DECLARE @m INT = 0
    DECLARE @strEncKey NVARCHAR(MAX) = 'ASHOKKHE'
    DECLARE @gsEcDc NVARCHAR(1)
    DECLARE @gsFinal NVARCHAR(MAX) = ''
    DECLARE @gsCompare NVARCHAR(1)
    DECLARE @glNumber INT = LEN(LTRIM(RTRIM(@strInput)))
    DECLARE @sb NVARCHAR(MAX) = 'ASHOKKHE'
    WHILE (LEN(@sb) < @glNumber)
    BEGIN
        SET @sb = @sb + 'ASHOKKHE'
    END
    SET @strEncKey = LEFT(@sb, @glNumber)
    SET @m = 1
    WHILE @m <= @glNumber
    BEGIN
        SET @gsEcDc = SUBSTRING(@strInput, @m, 1)
        SET @gsCompare = SUBSTRING(@strEncKey, @m, 1)
        SET @gsFinal = @gsFinal + CHAR(ASCII(@gsEcDc) + ASCII(@gsCompare) + 13)
        SET @m = @m + 1
    END
    RETURN @gsFinal
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

CREATE PROCEDURE [dbo].[stpr_InsertUpdateXMLDropDown] @dsXml XML 
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
  @o_vcFlag VARCHAR(1)='S', @o_vcMessage VARCHAR(MAX)=''
  SELECT @strModuleName = ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'ActionName'
  SELECT @strOption = ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'Option'
  IF ISNULL(@strModuleName,'') <> ''
  BEGIN
    
    SELECT @strQuery = DBQuery, @strQueryType = DBQueryType 
	FROM tbl_InsertUpdateXMLDropDownQuery(NOLOCK) 
	WHERE ModuleName = @strModuleName AND ColumnName = @strOption
	
	IF ISNULL(@strQuery,'') <> '' AND ISNULL(@strQueryType,'Q') = 'Q'
	BEGIN
	  IF @strUserid <> '' AND UPPER(@strQuery) LIKE '%CLIENT_MASTER%'
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
		SET @strString = REPLACE(@strString, '<<FILTER>>', (SELECT TOP 1 ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'X_Filter' and HeaderName = 'X_Filter'))
	  END
	  --END REPLACE CHANGES

	  EXEC(@strString)
	END
	ELSE IF ISNULL(@strQuery,'') <> '' AND ISNULL(@strQueryType,'Q') = 'P'
	BEGIN
	   SET @strString = 'EXEC DBO.' + @strQuery + ' ''' + @strxml + ''', @o_vcFlag OUTPUT, @o_vcMessage OUTPUT';
	   BEGIN TRY
	     EXEC sp_executesql @strString, N'@o_vcFlag VARCHAR(1) OUTPUT, @o_vcMessage VARCHAR(500) OUTPUT', @o_vcFlag OUTPUT, @o_vcMessage OUTPUT;
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

CREATE PROCEDURE [dbo].[SecurityListingSearch] @i_vcinput XML, @o_vcFlag VARCHAR(1) OUTPUT, @o_vcMessage VARCHAR(MAX) OUTPUT
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

  DECLARE @strType VARCHAR(50) = ''
  SELECT @strType = ParameterValue FROM @tbl_InputJSONTable where ParameterName = 'X_Filter'

  IF @strType = 'I'
  BEGIN
  SELECT DISTINCT TOP 100 sc_isincode as [Value] , sc_isinname + ' [' + sc_isincode + ']'  as [DisplayName] FROM Security with (noLock)
  END
  ELSE IF @strType = 'C'
  BEGIN
  select distinct TOP 100 sc_company_name as [Value],sc_company_name as [DisplayName] from Security with (noLock)
  END

END

GO

CREATE PROCEDURE [dbo].[SP_CompanyCodeDPID]  @dsXml AS XML
WITH ENCRYPTION
AS
BEGIN
 	
		Select distinct STUFF((SELECT Distinct ',' + cm_companycode
               FROM Client_master  (nolock) Where isNull(cm_companycode,'') <> '' and Left(cm_companycode,2) in ('IN') 
               FOR XML PATH('')), 1, 2, '') DPID
		,  Case when (Select Count(*) from INFORMATION_SCHEMA.TABLES where TABLE_NAME=N'Entity_Master') > 0 Then 
														(Case When (Select Count(*) from Entity_master) > 0 Then (Select em_Name From Entity_master (nolock) where em_cd = (select min(em_cd) from Entity_master (nolock))) Else (Select Cast(sp_sysvalue as varchar(100)) from sysparameter with(nolock) where sp_parmcd = 'NAME') end)
												Else (Select Cast(sp_sysvalue as varchar(100)) from sysparameter with(nolock) where sp_parmcd = 'NAME') End as CompanyName

END

GO

CREATE PROCEDURE [dbo].[SP_InitializeLogin]  @dsXml AS XML
WITH ENCRYPTION
AS
BEGIN
 	
	DECLARE @DPID varchar(100)=''
	SET @DPID = (Select distinct STUFF((SELECT Distinct ',' + cm_companycode
               FROM Client_master  (nolock) Where isNull(cm_companycode,'') <> '' and Left(cm_companycode,2) in ('IN') 
               FOR XML PATH('')), 1, 1, ''))
		
	Declare @CompanyName Varchar(50) = (Select Cast(sp_sysvalue as varchar(100)) from sysparameter with(nolock) where sp_parmcd = 'NAME') 


    Declare @PasswordMaxLength Int = (SELECT character_maximum_length as 'Max Length'  FROM information_schema.columns
											WHERE table_name = 'Client_Master' and COLUMN_NAME='CM_PWD')


Select @DPID as DPID, @CompanyName as CompanyName, @PasswordMaxLength as PasswordMaxLength

END

GO

CREATE  PROCEDURE [dbo].[sp_ChangePassword]  @dsXml AS XML
WITH ENCRYPTION
AS
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
  
   DECLARE @ClientCode Varchar(20)='', @OldPassword NVARCHAR(50) = ''  , @NewPassword NVARCHAR(50)=''
  
   SELECT @ClientCode = ParameterValue  From @tb_ParamListDetail  WHERE ParameterName = 'ClientCode' AND HeaderName = 'X_Data'  
   SELECT @OldPassword = ParameterValue  From @tb_ParamListDetail  WHERE ParameterName = 'OldPassword' AND HeaderName = 'X_Data'  
   SELECT @NewPassword = ParameterValue  From @tb_ParamListDetail  WHERE ParameterName = 'NewPassword' AND HeaderName = 'X_Data'  
   
   DECLARE @strEncPwd VARCHAR(10) = (Select ISNULL(sp_sysvalue,0) from WebParameter where sp_parmcd = 'TWEBENCPWD')

   If @strEncPwd = 'Y'
   BEGIN
     SET @OldPassword = dbo.fn_EncryptStringTW(@OldPassword)
	 SET @NewPassword = dbo.fn_EncryptStringTW(@NewPassword)
   END

   If Exists(Select * From Client_master With(NoLock) Where cm_cd = @ClientCode And cm_pwd = @OldPassword)
	  Begin
		Update Client_master Set cm_pwd = @NewPassword  Where cm_cd = @ClientCode And cm_pwd = @OldPassword
		Select 'Password changed successfully!'
		Return 1
	  End
	
   Else
		Begin
			Select 'Old password not matched.'
			Return 1
		End

END

GO

CREATE  PROCEDURE [dbo].[SP_Transaction]  @dsXml AS XML
WITH ENCRYPTION
AS
BEGIN
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
  
   DECLARE @strClientCode VARCHAR(10) = ''  
   DECLARE @strCompanyCode VARCHAR(50) = ''  , @strFromDt VARCHAR(8)='', @strToDt   VARCHAR(8)=''
  
   SELECT @strClientCode = ParameterValue  From @tb_ParamListDetail    WHERE ParameterName = 'ClientCode' AND HeaderName = 'X_Filter'  
   SELECT @strCompanyCode = ParameterValue  From @tb_ParamListDetail   WHERE ParameterName = 'CompanyCode' AND HeaderName = 'X_Filter'  
   SELECT @strFromDt = ParameterValue  From @tb_ParamListDetail    WHERE ParameterName = 'FromDate' AND HeaderName = 'X_Filter'  
   SELECT @strToDt = ParameterValue  From @tb_ParamListDetail    WHERE ParameterName = 'ToDate' AND HeaderName = 'X_Filter'  


		SELECT CompanyCode, ClientCode, td_trxdate Date,OrdDate,td_reference TrxNo,td_text Particular, td_isin_code ISINCode ,ISINName ISINName,acdesc AcType, Debit,Credit,
		  Balance  = sum(Balance) OVER (PARTITION BY td_ac_code, ISINName, td_ac_type ORDER BY td_ac_code,ISINName,td_ac_type,OrdDate,td_debit_credit, td_text, tag, SerialNo)						
				FROM (
					  SELECT  '1' Tag, cm_companycode CompanyCode, cm_cd ClientCode, convert (char,convert(datetime,@strFromDt),103) as  td_trxdate,'' td_reference,'Opening Balance'  td_text, 
							  td_isin_code, sc_company_name +' '+ sc_isinname + ' (' + td_isin_code + ')' ISINName, 0 Debit, 0 Credit ,
							  sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE td_qty * (- 1) END) Balance, SerialNo = 0,
							  0 td_qty,td_ac_type,bt_description acdesc,sc_company_name,'' td_debit_credit,td_ac_code ,'' as OrdDate
						FROM  Trxweb a with (nolock), Security with (nolock), Client_master with (nolock), Beneficiary_type with (nolock)
						WHERE td_ac_code = cm_cd  and td_isin_code = sc_isincode  And td_ac_type = bt_code  
							 and td_trxdate < @strFromDt   and td_ac_code= @strClientCode  and td_dpid = @strCompanyCode 
						GROUP BY td_ac_code, td_isin_code, sc_company_name, td_ac_type,  bt_description,sc_isinname, cm_companycode , cm_cd
							HAVING sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE td_qty * (- 1) END) <> 0
			UNION ALL

					    SELECT  '2' Tag,cm_companycode CompanyCode, cm_cd ClientCode, convert (char,convert(datetime,td_trxdate),103) td_trxdate,td_reference,td_text, td_isin_code, 
							sc_company_name +' '+ sc_isinname + ' (' + td_isin_code + ')' ISINName,
							  Case td_debit_credit  when 'D' then cast((td_qty)as decimal(15,3)) else 0 end  'Debit', 
							  Case td_debit_credit  when 'C' then cast((td_qty)as decimal(15,3)) else 0 end  'Credit',
							  (CASE td_debit_credit WHEN 'C' THEN td_qty ELSE td_qty * (- 1) END) AS  Balance, 
							  SerialNo = ROW_NUMBER() OVER (ORDER BY td_ac_code, td_isin_code,td_ac_type,td_trxdate,td_reference, td_text,  td_debit_credit),
							  td_qty,td_ac_type,bt_description acdesc,sc_company_name,td_debit_credit,td_ac_code ,td_trxdate as OrdDate
						FROM trxweb a with (nolock), Security with (nolock), Client_master with (nolock), Beneficiary_type with (nolock)
						WHERE td_ac_code = cm_cd  and td_isin_code = sc_isincode  And td_ac_type = bt_code  
							 and td_trxdate between @strFromDt and  @strToDt  and td_ac_code = @strClientCode and td_dpid = @strCompanyCode   

			)  A 

             ------------- For Second table property ---------------
		     SELECT '<XmlData>
						<TotalList>Balance</TotalList>
						<RightList>Balance</RightList>
						<HideList>CompanyCode,ClientCode</HideList>
						<DateFormat></DateFormat>
						<DateFormatList></DateFormatList>
						<Dec2List>Balance</Dec2List>
						<Dec4List></Dec4List>
						<DrCRColorList></DrCRColorList>
						<PnLColorList></PnLColorList>
						<PrimaryKey></PrimaryKey>
					</XmlData>' 
   AS Settings      	
END

GO

CREATE  PROCEDURE [dbo].[SP_SecurityListing]  @dsXml AS XML
WITH ENCRYPTION
AS
BEGIN
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
  
   DECLARE @strClientCode VARCHAR(10) = ''  
   DECLARE @strDPID VARCHAR(50) = ''  , @strFromDt VARCHAR(8)='', @strToDt   VARCHAR(8)='',@searchText VARCHAR(100) ='',
		   @searchBy   VARCHAR(1) = '', @status  Bit = 0
  
   SELECT @strClientCode = ParameterValue  From @tb_ParamListDetail  WHERE ParameterName = 'ClientCode' AND HeaderName = 'X_Filter'  
   SELECT @strDPID = ParameterValue  From @tb_ParamListDetail  WHERE ParameterName = 'CompanyCode' AND HeaderName = 'X_Filter'  
   SELECT @searchText = ParameterValue  From @tb_ParamListDetail   WHERE ParameterName = 'SearchText' AND HeaderName = 'X_Filter'  
   SELECT @searchBy = ParameterValue  From @tb_ParamListDetail   WHERE ParameterName = 'SearchBy' AND HeaderName = 'X_Filter'  
   SELECT @status = ParameterValue  From @tb_ParamListDetail   WHERE ParameterName = 'Status' AND HeaderName = 'X_Filter'  

		  Declare  @WhereQry  Varchar(50) = '';
		    
			 Set @WhereQry = Case When @status=1 Then '  and sc_security_status = ''01''' Else '' End
				    Declare @Qry Varchar(500)= ''

				   IF(@searchBy='C')
				      Begin
					    set @searchText = (Select Case when isnull(@searchText,'')='' then '' else ' And sc_company_name like ''%' + @searchText + '%''' End)
							Set @Qry = 'Select sc_isincode [ISIN Code],sc_isinname [ISIN Name], sc_company_name [Company Name], cast(sc_security_rate as decimal(15,2)) as Rate 
										From Security where 1 = 1  ' + @searchText +   @WhereQry
						    Exec (@Qry)							
					  End
				   Else
				      Begin
					  set @searchText = (Select Case when isnull(@searchText,'')='' then '' else ' And sc_isincode = ''' + @searchText + '''' End)
						 Set @Qry  = 'Select  sc_isincode [ISIN Code],sc_isinname [ISIN Name], sc_company_name [Company Name], cast(sc_security_rate as decimal(15,2)) as Rate 
									  From Security where 1 = 1 ' + @searchText +  @WhereQry
						 Exec (@Qry)
					  End
              
	 ------------- For Second table property ---------------
		     SELECT '<XmlData>
						<TotalList>Balance</TotalList>
						<RightList>Balance</RightList>
						<HideList></HideList>
						<DateFormat></DateFormat>
						<DateFormatList></DateFormatList>
						<Dec2List>Balance</Dec2List>
						<Dec4List></Dec4List>
						<DrCRColorList></DrCRColorList>
						<PnLColorList></PnLColorList>
						<PrimaryKey></PrimaryKey>
					</XmlData>' 
   AS Settings            	

END

GO

CREATE PROCEDURE [dbo].[SP_UserProfile]  @dsXml AS XML
WITH ENCRYPTION
AS
BEGIN
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
  
   DECLARE @strClientCode VARCHAR(10) = ''  , @strDPID VARCHAR(50) = ''  

   SELECT @strClientCode = ParameterValue  From @tb_ParamListDetail   WHERE ParameterName = 'ClientCode' AND HeaderName = 'X_Filter'  
   SELECT @strDPID = ParameterValue  From @tb_ParamListDetail    WHERE ParameterName = 'CompanyCode' AND HeaderName = 'X_Filter'  
  
			If  exists (select count(*) from sysobjects where name = 'Client_AdditionalDetail' having count(*) > 0)
				begin
					Select  cm_cd as ClientCode, cm_name ClientName, cm_add1 Address1, cm_add2 Address2, cm_add3 Address3, cm_add4 Address4, cm_pincode Pincode  
					    , Isnull(cm_tele1,'--') TelePhone1,Isnull(cm_tele2,'--') TelePhone2,Isnull(cm_mobile,'--') MobileNo,Isnull(cm_email,'--') Email  
                        , Isnull(cm_panno,'--') PanNo, Isnull(ca_state,'') State, Isnull(ca_country,'') Country , Isnull(bc_description,'--') Category  
                        , Isnull(ca_nominee,'--') Nominee, Isnull(ca_blsavingcd,'--') TradingCd, Isnull(ca_chgsscheme,'--') Scheme   
                        , Isnull((Case when Isnull(ca_thih_name,'') ='' Then ca_sech_name Else ca_sech_name +',' + ca_thih_name End),'--') Joints, Isnull(cm_groupname,'--') Groups  
                        , Isnull(cm_familyname,'--') Family , Isnull(ca_branchname,'--') Branch , Isnull(bt_description,'--') SubType , Isnull(bs_description,'--') Status  
                        , Isnull(ca_divbankacno,'--') BankAcNo, Isnull(ca_divbankcode,'--') BankCode, Isnull(ca_divbankname,'--') BankName, Isnull(ca_divifsc,'--') IFSC  
                        , Case when cm_divacctype=10 then 'Saving Account' when cm_divacctype=11 then 'Current Account' when cm_divacctype=13 then 'Cash Credit' Else '--' End AcType  
                     From client_master with(nolock)  left outer join Client_AdditionalDetail with(nolock) on  cm_companycode + cm_cd = ca_companycode + ca_cmcd  
                        left outer join Beneficiary_category with(nolock) on  bc_code = cm_acctype left outer join Beneficiary_type with (nolock) on  bt_code = ca_clienttype  
                        left outer join Beneficiary_status with(nolock) on  bs_code = ca_active  
                     Where cm_companycode = @strDPID   And  cm_cd = @strClientCode
				End
             Else 
				Begin
					Select  cm_cd as ClientCode, cm_name ClientName, cm_add1 Address1, cm_add2 Address2, cm_add3 Address3, cm_add4 Address4, cm_pincode Pincode 
                            , Isnull(cm_tele1, ' --') TelePhone1,Isnull(cm_tele2, ' --') TelePhone2,Isnull(cm_mobile, ' --') MobileNo,Isnull(cm_email, ' --') Email 
                            , Isnull(cm_panno, ' --') PanNo, '' State, '' Country , '--' Category, '--' Nominee, '--' TradingCd, '--' Scheme , '--' Joints, Isnull(cm_groupname, '--') Groups 
                            , Isnull(cm_familyname, ' --') Family , '--' Branch , '--' SubType , '--' Status, '--' BankAcNo, '--' BankCode, '--' BankName, '--' IFSC, '--' AcType 
                      From client_master with (nolock) Where cm_companycode =  @strDPID   And  cm_cd = @strClientCode
				End
      	

END

GO

CREATE  PROCEDURE [dbo].[SP_Ledger]  @dsXml AS XML
WITH ENCRYPTION
AS
BEGIN
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
  
   DECLARE @strClientCode VARCHAR(10) = ''  
   DECLARE @strDPID VARCHAR(50) = ''  , @strFromDt VARCHAR(8)='', @strToDt   VARCHAR(8)=''
  
   SELECT @strClientCode = ParameterValue  From @tb_ParamListDetail   WHERE ParameterName = 'ClientCode' AND HeaderName = 'X_Filter'  
   SELECT @strDPID = ParameterValue  From @tb_ParamListDetail   WHERE ParameterName = 'CompanyCode' AND HeaderName = 'X_Filter'  
   SELECT @strFromDt = ParameterValue  From @tb_ParamListDetail   WHERE ParameterName = 'FromDate' AND HeaderName = 'X_Filter'  
   SELECT @strToDt = ParameterValue  From @tb_ParamListDetail   WHERE ParameterName = 'ToDate' AND HeaderName = 'X_Filter'  

 
		 Select  CompanyCode,  ClientCode, ld_dt Date,ld_chequeno ChequeNo,ld_particular Particular,Debit ,Credit ,
				Balance = sum(Balance) OVER (PARTITION BY ld_clientcd ORDER BY Ord,ld_clientcd,Type,ld_dt1,  SerialNo) 
				----Balance = sum(Balance)  over(order by Ord,ld_clientcd,Type,ld_dt1 rows unbounded preceding)
			From (
				select '0' as Ord ,cm_companycode CompanyCode, ld_clientcd ClientCode,  ld_clientcd,  convert(char,convert(datetime,@strFromDt),103)ld_dt,'' ld_chequeno,'Opening Balance' ld_particular,
					0 Debit, 0 Credit,
					CASE When sum(ld_amount)  > 0 Then cast((sum(ld_amount)) as decimal (15,2))  ELSE cast((sum(ld_amount)) as decimal (15,2)) * (- 1) END Balance, 
					SerialNo = 0, 0 Type,convert(datetime,'01 Apr 2011') ld_dt1
				From Ledger with (nolock) ,ClienT_master with (nolock) 
				Where ld_clientcd = cm_Cd and cm_Cd = @strClientCode and ld_dpid = @strDPID and ld_dt < @strFromDt
				Group By ld_clientcd,cm_name,cm_companycode  Having sum(ld_amount) <> 0

				union all 

				select '1' as Ord ,cm_companycode CompanyCode, cm_cd ClientCode,  ld_clientcd,convert(char,convert(datetime,ld_dt),103)ld_dt, Case when ld_chequeno ='0' then '' else ld_chequeno end as ld_chequeno,ltrim(rtrim(ld_particular)) as ld_particular,
					Case When ld_amount > 0 Then cast((ld_amount) as decimal (15,2)) else 0 end Debit,
					Case When ld_amount < 0 Then cast((abs(ld_amount)) as decimal (15,2)) else 0 end Credit,
					CASE When ld_amount  > 0 Then cast((abs(ld_amount)) as decimal (15,2))  ELSE cast((abs(ld_amount)) as decimal (15,2)) * (- 1) END Balance,
					SerialNo = ROW_NUMBER() OVER (ORDER BY ld_clientcd, ld_dt,ld_particular,Case When ld_amount < 0 Then 'C' Else 'D' End),1 Type,ld_dt ld_dt1
				From Ledger with (nolock) ,ClienT_master with (nolock) 
				Where ld_clientcd = cm_Cd  And cm_cd = @strClientCode and ld_dpid = @strDPID and ld_dt between @strFromDt and @strToDt
			) As A
            Order by ld_clientcd,Type,ld_dt1

			  ------------- For Second table property ---------------
		     SELECT '<XmlData>
						<TotalList>Balance</TotalList>
						<RightList>Balance</RightList>
						<HideList>CompanyCode,ClientCode</HideList>
						<DateFormat></DateFormat>
						<DateFormatList></DateFormatList>
						<Dec2List>Balance</Dec2List>
						<Dec4List></Dec4List>
						<DrCRColorList></DrCRColorList>
						<PnLColorList></PnLColorList>
						<PrimaryKey></PrimaryKey>
					</XmlData>' 
   AS Settings
END

GO

CREATE PROCEDURE [dbo].[SP_Holding]  @dsXml AS XML
WITH ENCRYPTION
AS
BEGIN
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
  
   DECLARE @strClientCode VARCHAR(10) = ''  
   DECLARE @strDPID VARCHAR(50) = ''  
  
   SELECT @strClientCode = ParameterValue  From @tb_ParamListDetail   WHERE ParameterName = 'ClientCode' AND HeaderName = 'X_Filter'  
   SELECT @strDPID = ParameterValue  From @tb_ParamListDetail    WHERE ParameterName = 'CompanyCode' AND HeaderName = 'X_Filter'  

		    Declare @holdDate varchar(50)= '[Holding as on ' + Rtrim((select convert(char,convert(datetime,max(hld_hold_date)),103) as datef from holding)) + ' ISIN Name]'

			Declare @HoldQry varchar(max) =  'Select  cm_companycode CompanyCode, cm_cd ClientCode,   hld_isin_code as ISIN, rtrim(ltrim(sc_company_name)) + '' ('' + rtrim(ltrim(sc_isinname)) + '')'' as '+ @holdDate + ', 
						bt_description as [Balance Type], Cast(hld_ac_pos as Decimal(10,3)) as Quantity, convert(decimal(15,2),sc_security_rate) as  Rate ,  
						Cast((hld_ac_pos * sc_security_rate) as Decimal(15,2)) as Value
            from Holding with(NoLock), Beneficiary_Type with(NoLock), Security with(NoLock), Client_master  with(NoLock) 
            Where hld_ac_code = cm_cd and bt_code = hld_ac_type and hld_isin_code = sc_isincode and hld_ac_code = '''+ @strClientCode +''' -- and hld_dpid = '''+ @strDPID+'''  
			Order by   ' +@holdDate

		  Exec (@HoldQry)

		  ------------- For Second table property ---------------
		     SELECT '<XmlData>
						<TotalList>Balance</TotalList>
						<RightList>Balance</RightList>
						<HideList>CompanyCode,ClientCode</HideList>
						<DateFormat></DateFormat>
						<DateFormatList></DateFormatList>
						<Dec2List>Balance</Dec2List>
						<Dec4List></Dec4List>
						<DrCRColorList></DrCRColorList>
						<PnLColorList></PnLColorList>
						<PrimaryKey></PrimaryKey>
					</XmlData>' 
   AS Settings
END

GO

CREATE   PROCEDURE [dbo].[stpr_EstroWebMenu] @dsXml AS XML = NULL 
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
   
     INSERT INTO @tb_ParamListDetail (ParameterName,  ParameterValue, HeaderName)
     SELECT Parameter.value('(ColumnName)[1]', 'VARCHAR(MAX)') AS Client_Code ,
     Parameter.value('(ColumnValue)[1]', 'VARCHAR(MAX)') AS ColumnValue,
	 Parameter.value('(MasterTag)[1]', 'VARCHAR(MAX)') AS MasterTag
     FROM @o_ParameterListxml.nodes('/Parameter') AS XTbl(Parameter)
   END 

   DECLARE @strUserid VARCHAR(500)='', @strLevel INT = 0, @dtFromDate DATE, @dtToDate DATE,
   @StrClientCode VARCHAR(50)='', @strOption VARCHAR(50) = ''
   SELECT @strUserid = ParameterValue From @tb_ParamListDetail where ParameterName = 'UserId'
   SELECT @strOption = ParameterValue From @tb_ParamListDetail where ParameterName = 'Option'

   IF @strOption = 'AccessRights'
   BEGIN
   
   Select ModuleId = ModuleCode, MenuName = MenuName, MenuCode = MenuCode, MenuTag = MenuTag, DenyRights = DenyRights, TargetForm = TargetForm, [Path] = [Path],
   [Enable] = CASE [Enable] WHEN 'Y' then 'true' else 'false' end, 
   [Add] = CASE SUBSTRING(Rights, 1, 1) WHEN 'Y' then 'true' else 'false' end, 
   [Edit] = CASE SUBSTRING(Rights, 2, 1) WHEN 'Y' then 'true' else 'false' end, 
   [Delete] = CASE SUBSTRING(Rights, 3, 1) WHEN 'Y' then 'true' else 'false' end, 
   [View] = CASE SUBSTRING(Rights, 4, 1) WHEN 'Y' then 'true' else 'false' end
   from tbl_EstroWebMenu where MenuType = 'C'
   return
   END
   ELSE IF @strOption = 'Routes'
   BEGIN
   Select [name] = MenuName, [path] = [Path], element = MenuTag from tbl_EstroWebMenu Order By MenuCode
   return
   END
   ELSE IF @strOption = 'List'
   BEGIN
   select ModuleCode as 'id', MenuName as 'title', Path as 'path', '' as 'icon',
   submenu=Isnull((Select REPLACE((select ModuleCode as 'id', MenuName as 'title', [Path] as 'path', '' as 'icon' from tbl_EstroWebMenu b where b.ParentMenu=a.MenuName  order by MenuCode FOR JSON PATH),'\\','')),'')
   from tbl_EstroWebMenu a where a.MenuType='P'
   order by MenuCode
   END
END