CREATE PROCEDURE sp_ProcessRekyc @i_vcString VARCHAR(MAX), @i_vcType VARCHAR(20), @o_vcString VARCHAR(MAX) OUTPUT 
WITH ENCRYPTION
AS
BEGIN
  DECLARE @XMLSTR XML
  IF @i_vcType = 'Getsegment'
  BEGIN
    DECLARE @tbl_Segment TABLE (Segment VARCHAR(50), ce_companycode VARCHAR(50), ExchVal VARCHAR(50), Exchange VARCHAR(100), MainExchCode VARCHAR(100))
	INSERT INTO @tbl_Segment(Segment, ce_companycode, ExchVal, Exchange, MainExchCode)
	SELECT * FROM OPENJSON(@i_vcString) WITH (Segment  VARCHAR(50), ce_companycode VARCHAR(50), ExchVal VARCHAR(50), Exchange VARCHAR(100),
	MainExchCode VARCHAR(100))
    SET @XMLSTR = (SELECT * FROM @tbl_Segment X1  FOR XML PATH)
	SET @o_vcString = CAST(@XMLSTR AS VARCHAR(MAX))
  END
  ELSE IF @i_vcType = 'thirdPartyAPI'
  BEGIN
    DECLARE @strFile NVARCHAR(MAX), @strFileName VARCHAR(200), @strFilePassword  VARCHAR(50)
	
  	SELECT @strFile = [base64], @strFileName = [FileName], @strFilePassword = [FilePassword]  
	FROM OPENJSON(@i_vcString) WITH ([base64] NVARCHAR(MAX),
	[FileName] VARCHAR(200), [FilePassword] VARCHAR(50))
	
	SET @XMLSTR = (SELECT [base64] = @strFile, [FileName] = @strFileName, [FilePassword] = @strFilePassword  FOR XML PATH)
	SET @o_vcString = CAST(@XMLSTR AS VARCHAR(MAX))
  END	
  RETURN 1
END
GO

CREATE   FUNCTION [dbo].[fn_ParentJSONSplit] (@i_vcJsonString NVARCHAR(MAX))     
 RETURNS @o_tbOutPutTable TABLE (SerialNo INT IDENTITY(1,1), ColumnName VARCHAR(100),       
ColumnValue NVARCHAR(MAX), ValueTypeColumn SMALLINT, strImageFlag VARCHAR(1))       
AS       
BEGIN      
/*            
///////////////////////////////////////////////////////////////////////////////////////////            
// Create By     : Vaibhav Garg    
// Created Date  : 21-JAN-2021           
// CCT NO        :           
// Description   : This Function is used to spilt JSON String into a Output table     
//                 which contains Root data and child data     
// Reviewed By   :  Shyam M             
// Review Date   :  28-Mar-2022             
//////////////////////////////////////////////////////////////////////////////////////////            
  */      
    
--Details of ValueTypeColumn Values     
--Value of the Type column = 0 -> "null" - JSON Data Type    
--Value of the Type column = 1 -> "string"- JSON Data Type    
--Value of the Type column = 2 -> "number"- JSON Data Type    
--Value of the Type column = 3 -> "true/false"- JSON Data Type    
--Value of the Type column = 4 -> "array"- JSON Data Type    
--Value of the Type column = 5 -> "object"- JSON Data Type    
    
    
  DECLARE @strdetailjson NVARCHAR(MAX) ='', @strColumnValue NVARCHAR(MAX)=''      
    
  DECLARE @tbMainTable TABLE(    
    SerialNo INT IDENTITY(1,1),     
    ColumnName VARCHAR(100),      
    ColumnValue NVARCHAR(MAX),     
    ValueTypeColumn INT,     
    strImageFlag VARCHAR(1)    
    )    
  ---This table is for Dynamic data of Child Data / Data    
   DECLARE @tbDynamicData TABLE(    
    SerialNo INT IDENTITY(1,1),     
    ColumnName VARCHAR(100),      
    ColumnValue NVARCHAR(MAX),     
    ValueTypeColumn INT,     
   strImageFlag VARCHAR(1)    
    )    
  --In this Table @tbMainTable, We are storing all Complete JSON File into Table. This Data    
  -- Is divided into multiple parts, In the main JSON Root Data will come into the table Data    
  --and Images and DocList and Data will come in one Key and the Images JSON data, Doc List JSON Data,    
  -- Normal Data will come in this.     
    
  INSERT INTO @tbMainTable(ColumnName, ColumnValue, ValueTypeColumn)      
  SELECT * FROM OPENJSON(@i_vcJsonString);      
    
  --Now we are going to take the dynamic Data of JSON KeyValue = 'Data' or 'ChildData'      
  -- into @strdetailjson String    
    
  SELECT @strdetailjson = ColumnValue FROM @tbMainTable WHERE ColumnName IN('ChildData','Data')      
  IF @strdetailjson <> ''      
  BEGIN      
    --Here We are inserting every data record into this @tbMainTable     
    --From the JSON String - @strdetailjson. This data is unformatted now.     
    --This data Value Type Columns will be 5 , which is an object data    
    INSERT INTO @tbDynamicData (ColumnName, ColumnValue, ValueTypeColumn)      
    SELECT * FROM OPENJSON(@strdetailjson)      
    --In this Cursor we are breaking the Dynamic data of ChildData and Data JSON into main table    
    DECLARE CURSOR_JSON CURSOR FOR      
    --ValueTypeColumn = 5 meams JSON Data Type is "Object"    
    SELECT ColumnValue from @tbDynamicData where ValueTypeColumn = 5      
    ORDER BY SerialNo      
    OPEN CURSOR_JSON       
    FETCH NEXT FROM CURSOR_JSON INTO @strColumnValue      
    WHILE @@FETCH_STATUS = 0       
    BEGIN      
      INSERT INTO @tbMainTable(ColumnName, ColumnValue, strImageFlag)      
      SELECT * FROM OPENJSON(@strColumnValue) WITH ([Key] VARCHAR(100),     
     Value NVARCHAR(MAX), ImageFlag VARCHAR(1))      
    FETCH NEXT FROM CURSOR_JSON INTO @strColumnValue      
    END       
    CLOSE CURSOR_JSON       
    DEALLOCATE CURSOR_JSON        
  END         
  --We are inserting the into OutPut table, where we are ignoring ChildData/Data Complete JSON    
  INSERT INTO @o_tbOutPutTable(ColumnName, ColumnValue, ValueTypeColumn, strImageFlag)     
  SELECT ColumnName, ColumnValue = ISNULL(ColumnValue,''),     
  ValueTypeColumn = ISNULL(ValueTypeColumn,10),     
  strImageFlag  = ISNULL(strImageFlag,'')     
  FROM @tbMainTable WHERE ColumnName NOT IN ('Data','ChildData')      
  ORDER BY SerialNo      
  RETURN       
END 


CREATE PROCEDURE [dbo].[stpr_GetAdditionalDetailRekyc] @strClientCode VARCHAR(20), @o_vcOutput VARCHAR(MAX) OUTPUT 
WITH ENCRYPTION
AS
BEGIN
  IF EXISTS(sELECT 1 FROM client_master(NOLOCK) WHERE CM_CD =  @strClientCode)
  BEGIN
    DECLARE @companyName varchar(100)=''
	
	IF NOT EXISTS (select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME=N'Entity_Master') 
	BEGIN  
	  SELECT @companyName=ltrim(rtrim(sp_sysvalue)) from sysparameter with (nolock) where sp_parmcd = 'NAME' 
	END 
	ELSE 
	BEGIN  
      SELECT @companyName=ltrim(rtrim(em_Name))   from Entity_master where em_cd = (select min(em_cd) from Entity_master)  
    END 
	
    SELECT PerAddress1 = cm_padd1, PerAddress2 = cm_padd2, PerAddress3 = cm_padd3, PerCity = cm_padd4, PerState = cm_pstate, 
	PerCountry = cm_pcountry, PerPincode = cm_ppincode,
	CKYCNumber = ISNULL(Ck_Nfiller1, 0), CKYCDate = ISNULL(ckyc.mkrdt, ''), CKYCReffNo = ISNULL(Ck_Reference, ''), 
    KRAStatus = ISNULL((SELECT ISNULL(cn_KRAStatus, '') FROM Client_Nominee(NOLOCK) WHERE cn_cd = X.CM_cD),''),
    [KYCVerificatioName] = UM.um_user_name, 
    [KYCVerificationDesig] = um_designation,
    [KYCVerificationBranch] = ISNULL((SELECT sp_sysvalue FROM Sysparameter WHERE sp_parmcd='CKYCBRANCHCD'),''),
    [KYCVerificationEmpCode] = um_empCode, 
    [KRACOMPNAME] = ISNULL((SELECT sp_sysvalue FROM Sysparameter WHERE sp_parmcd='CKYCCOMPNAME'),''),
    [OrganisationCode] = ISNULL((SELECT sp_sysvalue FROM Sysparameter WHERE sp_parmcd='CKYCFINO'),''),
    [PlaceOfDeclaration] ='HeadOffice',(Select Top 1 img_logo From Images where img_desc='Company Logo') as CompanyLogo,@companyName as CompanyName
    FROM client_master(NOLOCK) X 
    LEFT OUTER JOIN (SELECT * FROM Client_CKYC(NOLOCK) CXXX WHERE CK_SRNO IN(SELECT MAX(CK_SRNO)
    FROM Client_CKYC WHERE CK_Panno = CXXX.CK_Panno)) ckyc 
    ON (x.cm_panno = ckyc.CK_Panno) 
    , CLIENT_INFO(NOLOCK) y, (Select um_user_name, um_designation, um_empCode From User_master 
    Where um_user_id IN(SELECT sp_sysvalue FROM Sysparameter WHERE sp_parmcd='CKYCVERIFYBY')) UM 
    WHERE x.cm_cd = y.cm2_cd
    AND X.CM_CD = @strClientCode 
    RETURN 1 
  END
END
GO

CREATE PROCEDURE sp_JsonCutter @strJsonValue NVARCHAR(MAX), @xmlCutterVal VARCHAR(MAX) OUTPUT 
WITH ENCRYPTION
AS
BEGIN
  DECLARE @tbl_Jsontable TABLE (
    SerialNo INT IDENTITY(1, 1),
    ColumnName VARCHAR(100),
    ColumnValue NVARCHAR(MAX),
    xtype INT,
    updateFlag VARCHAR(1),
    MasterTag VARCHAR(100),
    JsonLevel INT, 
	MasterLevel int);

  INSERT INTO @tbl_Jsontable (ColumnName, ColumnValue, xtype)
  SELECT * FROM OPENJSON(@strJsonValue);

  DECLARE @ColumnName VARCHAR(100),
        @ColumnValue NVARCHAR(MAX) = '',
        @tableName NVARCHAR(50) = '',
        @sql NVARCHAR(MAX),
        @iSerialNo INT = 0,
        @icounter INT = 0,
        @currentJsonLevel INT = 1, @i int= 1 -- Added variable to track current JSON level
		DECLARE @jsonPath NVARCHAR(4000) ='', @icounter1 int = 0

   DECLARE CurSettlement CURSOR FOR
   SELECT SerialNo, ColumnName, ColumnValue = IIF(xtype = '5', '[' + ColumnValue + ']', ColumnValue)
   FROM @tbl_Jsontable
   WHERE xtype >= 4
   AND ISNULL(ColumnValue, '') <> ''
   ORDER BY SerialNo;

   OPEN CurSettlement;
   FETCH NEXT FROM CurSettlement INTO @iSerialNo, @ColumnName, @ColumnValue;

   WHILE @@FETCH_STATUS = 0
   BEGIN
     set @icounter = @icounter+1
	 set @i = 1
     IF @ColumnValue <> ''
     BEGIN
	    SET @icounter1 = 0
	    WHILE @i < 1000 
		begin
          SET @icounter1 = @icounter1 + 1; 
		  set @jsonPath = CONCAT('$[', @i - 1, ']');
		  
          SET @sql = 'SELECT [key], value, type, ''' + @ColumnName + ''', ' + CAST(@icounter1 AS NVARCHAR(10)) + ', ' + CAST(@icounter AS NVARCHAR(10)) + '
                    FROM OPENJSON(@ColumnValue, ''' + @jsonPath + ''')
                    WHERE [key] <> ''''';
          INSERT INTO @tbl_Jsontable (ColumnName, ColumnValue, xtype, MasterTag, JsonLevel, MasterLevel)
          EXEC sp_executesql @sql, N'@ColumnValue NVARCHAR(MAX)', @ColumnValue;
		  /*INSERT INTO @tbl_Jsontable (ColumnName, ColumnValue, xtype, MasterTag, JsonLevel, MasterLevel)
          SELECT X1.*, @ColumnName, @icounter1, @icounter
          FROM (
              SELECT * FROM OPENJSON(@ColumnValue, @jsonPath)
          ) X1 where [key] <> '';*/
		  
		  SET @i = @i + 1
		END
        UPDATE @tbl_Jsontable SET updateFlag = 'Y' WHERE SerialNo = @iSerialNo;
      END
      FETCH NEXT FROM CurSettlement INTO @iSerialNo, @ColumnName, @ColumnValue;
    END;

    CLOSE CurSettlement;
    DEALLOCATE CurSettlement;

    declare @iMasterLevel int = 0
    DECLARE CurSettlement1 CURSOR FOR
    SELECT SerialNo, ColumnName, ColumnValue = IIF(xtype = '5', '[' + ColumnValue + ']', ColumnValue), JsonLevel
    FROM @tbl_Jsontable
    WHERE xtype >= 4
     AND ISNULL(ColumnValue, '') <> '' and ISNULL(updateFlag,'N') <> 'Y'  and Mastertag <> 'SegmentDetails'
    ORDER BY SerialNo;

    OPEN CurSettlement1;
    FETCH NEXT FROM CurSettlement1 INTO @iSerialNo, @ColumnName, @ColumnValue, @iMasterLevel

    WHILE @@FETCH_STATUS = 0
    BEGIN
      set @i = 1
      IF @ColumnValue <> ''
      BEGIN
	    set @icounter1 = 0
	    while @i < 1000 
		begin
          SET @icounter1 = @icounter1 + 1; 
		  set @jsonPath = CONCAT('$[', @i - 1, ']');
		  
          SET @sql = '
                    SELECT [key], value, type, ''' + @ColumnName + ''', ' + CAST(@icounter1 AS NVARCHAR(10)) + ', ' + CAST(@iMasterLevel AS NVARCHAR(10)) + '
                    FROM OPENJSON(@ColumnValue, ''' + @jsonPath + ''')
                    WHERE [key] <> ''''';
          INSERT INTO @tbl_Jsontable (ColumnName, ColumnValue, xtype, MasterTag, JsonLevel, MasterLevel)
          EXEC sp_executesql @sql, N'@ColumnValue NVARCHAR(MAX)', @ColumnValue;
		  
		  /*INSERT INTO @tbl_Jsontable (ColumnName, ColumnValue, xtype, MasterTag, JsonLevel, MasterLevel)
          SELECT X1.*, @ColumnName, @icounter1, @iMasterLevel
          FROM (
              SELECT * FROM OPENJSON(@ColumnValue, @jsonPath)
          ) X1 where [key] <> '';*/
		  
		 -- select * from OPENJSON(@ColumnValue, @jsonPath)
		  set @i = @i + 1
		end
        UPDATE @tbl_Jsontable SET updateFlag = 'Y' WHERE SerialNo = @iSerialNo;
      END
      FETCH NEXT FROM CurSettlement1 INTO @iSerialNo, @ColumnName, @ColumnValue, @iMasterLevel
    END;

    CLOSE CurSettlement1;
    DEALLOCATE CurSettlement1;

	SET @iMasterLevel = 0
    DECLARE CurSettlement2 CURSOR FOR
    SELECT SerialNo, ColumnName, ColumnValue = IIF(xtype = '5', '[' + ColumnValue + ']', ColumnValue), JsonLevel
    FROM @tbl_Jsontable
    WHERE xtype >= 4
     AND ISNULL(ColumnValue, '') <> '' and ISNULL(updateFlag,'N') <> 'Y'  and Mastertag <> 'SegmentDetails'
    ORDER BY SerialNo;

    OPEN CurSettlement2;
    FETCH NEXT FROM CurSettlement2 INTO @iSerialNo, @ColumnName, @ColumnValue, @iMasterLevel

    WHILE @@FETCH_STATUS = 0
    BEGIN
      set @i = 1
      IF @ColumnValue <> ''
      BEGIN
	    set @icounter1 = 0
	    while @i < 1000 
		begin
          SET @icounter1 = @icounter1 + 1; 
		  set @jsonPath = CONCAT('$[', @i - 1, ']');
		   SET @sql = '
                    SELECT [key], value, type, ''' + @ColumnName + ''', ' + CAST(@icounter1 AS NVARCHAR(10)) + ', ' + CAST(@iMasterLevel AS NVARCHAR(10)) + '
                    FROM OPENJSON(@ColumnValue, ''' + @jsonPath + ''')
                    WHERE [key] <> ''''';
          INSERT INTO @tbl_Jsontable (ColumnName, ColumnValue, xtype, MasterTag, JsonLevel, MasterLevel)
          EXEC sp_executesql @sql, N'@ColumnValue NVARCHAR(MAX)', @ColumnValue;
		  
          /*INSERT INTO @tbl_Jsontable (ColumnName, ColumnValue, xtype, MasterTag, JsonLevel, MasterLevel)
          SELECT X1.*, @ColumnName, @icounter1, @iMasterLevel
          FROM (
              SELECT * FROM OPENJSON(@ColumnValue, @jsonPath)
          ) X1 where [key] <> '';*/
		 -- select * from OPENJSON(@ColumnValue, @jsonPath)
		  set @i = @i + 1
		end
        UPDATE @tbl_Jsontable SET updateFlag = 'Y' WHERE SerialNo = @iSerialNo;
      END
      FETCH NEXT FROM CurSettlement2 INTO @iSerialNo, @ColumnName, @ColumnValue, @iMasterLevel
    END;

    CLOSE CurSettlement2;
    DEALLOCATE CurSettlement2;
    
	DECLARE @XMLDATA1 XML
    SET @XMLDATA1 = (SELECT SerialNo, ColumnName, ColumnValue, XTYPE, UpdateFlag = ISNULL(UpdateFlag,'N'), MasterTag =  ISNULL(MasterTag,''), 
	JsonLevel = isnull(JsonLevel,0), MasterLevel  = isnull(MasterLevel  ,0)
    FROM @tbl_Jsontable where   ISNULL(updateFlag,'N') <> 'Y' FOR XML PATH('JsonCutter'))
	SET @xmlCutterVal = CAST(@XMLDATA1 AS VARCHAR(MAX))
	RETURN 1
END
GO
CREATE FUNCTION fn_JsonCutter(@strJsonValue NVARCHAR(MAX)) RETURNS @o_tbOutPutTable TABLE (SerialNo INT, ColumnName VARCHAR(100),       
ColumnValue NVARCHAR(MAX), ValueTypeColumn SMALLINT, UpdateFlag VARCHAR(1), MasterTag VARCHAR(100), JsonLevel INT, MasterLevel INT) 
BEGIN
  DECLARE @tbl_Jsontable TABLE (
    SerialNo INT IDENTITY(1, 1),
    ColumnName VARCHAR(100),
    ColumnValue NVARCHAR(MAX),
    xtype INT,
    updateFlag VARCHAR(1),
    MasterTag VARCHAR(100),
    JsonLevel INT, 
	MasterLevel int);

  INSERT INTO @tbl_Jsontable (ColumnName, ColumnValue, xtype)
  SELECT * FROM OPENJSON(@strJsonValue);

  DECLARE @ColumnName VARCHAR(100),
        @ColumnValue NVARCHAR(MAX) = '',
        @tableName NVARCHAR(50) = '',
        @sql NVARCHAR(MAX),
        @iSerialNo INT = 0,
        @icounter INT = 0,
        @currentJsonLevel INT = 1, @i int= 1 -- Added variable to track current JSON level
		DECLARE @jsonPath NVARCHAR(4000) ='', @icounter1 int = 0

   DECLARE CurSettlement CURSOR FOR
   SELECT SerialNo, ColumnName, ColumnValue = IIF(xtype = '5', '[' + ColumnValue + ']', ColumnValue)
   FROM @tbl_Jsontable
   WHERE xtype >= 4
   AND ISNULL(ColumnValue, '') <> ''
   ORDER BY SerialNo;

   OPEN CurSettlement;
   FETCH NEXT FROM CurSettlement INTO @iSerialNo, @ColumnName, @ColumnValue;

   WHILE @@FETCH_STATUS = 0
   BEGIN
     set @icounter = @icounter+1
	 set @i = 1
     IF @ColumnValue <> ''
     BEGIN
	    SET @icounter1 = 0
	    WHILE @i < 10000 
		begin
          SET @icounter1 = @icounter1 + 1; 
		  --set @jsonPath = CONCAT('$[', @i - 1, ']');
		  
          INSERT INTO @tbl_Jsontable (ColumnName, ColumnValue, xtype, MasterTag, JsonLevel, MasterLevel)
          SELECT X1.*, @ColumnName, @icounter1, @icounter
          FROM (
              SELECT * FROM OPENJSON(@ColumnValue,  CONCAT('$[', @i - 1, ']'))
          ) X1 where [key] <> '';
		  SET @i = @i + 1
		END
        UPDATE @tbl_Jsontable SET updateFlag = 'Y' WHERE SerialNo = @iSerialNo;
      END
      FETCH NEXT FROM CurSettlement INTO @iSerialNo, @ColumnName, @ColumnValue;
    END;

    CLOSE CurSettlement;
    DEALLOCATE CurSettlement;

    declare @iMasterLevel int = 0
    DECLARE CurSettlement1 CURSOR FOR
    SELECT SerialNo, ColumnName, ColumnValue = IIF(xtype = '5', '[' + ColumnValue + ']', ColumnValue), JsonLevel
    FROM @tbl_Jsontable
    WHERE xtype >= 4
     AND ISNULL(ColumnValue, '') <> '' and ISNULL(updateFlag,'N') <> 'Y'  and Mastertag <> 'SegmentDetails'
    ORDER BY SerialNo;

    OPEN CurSettlement1;
    FETCH NEXT FROM CurSettlement1 INTO @iSerialNo, @ColumnName, @ColumnValue, @iMasterLevel

    WHILE @@FETCH_STATUS = 0
    BEGIN
      set @i = 1
      IF @ColumnValue <> ''
      BEGIN
	    set @icounter1 = 0
	    while @i < 10000 
		begin
          SET @icounter1 = @icounter1 + 1; 
		  --set @jsonPath = CONCAT('$[', @i - 1, ']');
		  
          INSERT INTO @tbl_Jsontable (ColumnName, ColumnValue, xtype, MasterTag, JsonLevel, MasterLevel)
          SELECT X1.*, @ColumnName, @icounter1, @iMasterLevel
          FROM (
              SELECT * FROM OPENJSON(@ColumnValue, CONCAT('$[', @i - 1, ']'))
          ) X1 where [key] <> '';
		 -- select * from OPENJSON(@ColumnValue, @jsonPath)
		  set @i = @i + 1
		end
        UPDATE @tbl_Jsontable SET updateFlag = 'Y' WHERE SerialNo = @iSerialNo;
      END
      FETCH NEXT FROM CurSettlement1 INTO @iSerialNo, @ColumnName, @ColumnValue, @iMasterLevel
    END;

    CLOSE CurSettlement1;
    DEALLOCATE CurSettlement1;

	SET @iMasterLevel = 0
    DECLARE CurSettlement2 CURSOR FOR
    SELECT SerialNo, ColumnName, ColumnValue = IIF(xtype = '5', '[' + ColumnValue + ']', ColumnValue), JsonLevel
    FROM @tbl_Jsontable
    WHERE xtype >= 4
     AND ISNULL(ColumnValue, '') <> '' and ISNULL(updateFlag,'N') <> 'Y'  and Mastertag <> 'SegmentDetails'
    ORDER BY SerialNo;

    OPEN CurSettlement2;
    FETCH NEXT FROM CurSettlement2 INTO @iSerialNo, @ColumnName, @ColumnValue, @iMasterLevel

    WHILE @@FETCH_STATUS = 0
    BEGIN
      set @i = 1
      IF @ColumnValue <> ''
      BEGIN
	    set @icounter1 = 0
	    while @i < 10000 
		begin
          SET @icounter1 = @icounter1 + 1; 
		  --set @jsonPath = CONCAT('$[', @i - 1, ']');
		  
          INSERT INTO @tbl_Jsontable (ColumnName, ColumnValue, xtype, MasterTag, JsonLevel, MasterLevel)
          SELECT X1.*, @ColumnName, @icounter1, @iMasterLevel
          FROM (
              SELECT * FROM OPENJSON(@ColumnValue, CONCAT('$[', @i - 1, ']'))
          ) X1 where [key] <> '';
		 -- select * from OPENJSON(@ColumnValue, @jsonPath)
		  set @i = @i + 1
		end
        UPDATE @tbl_Jsontable SET updateFlag = 'Y' WHERE SerialNo = @iSerialNo;
      END
      FETCH NEXT FROM CurSettlement2 INTO @iSerialNo, @ColumnName, @ColumnValue, @iMasterLevel
    END;

    CLOSE CurSettlement2;
    DEALLOCATE CurSettlement2;


    INSERT INTO @o_tbOutPutTable
    SELECT SerialNo, ColumnName, ColumnValue, XTYPE, ISNULL(UpdateFlag,'N'), MasterTag =  ISNULL(MasterTag,''), JsonLevel = isnull(JsonLevel,0), 
    MasterLevel  = isnull(MasterLevel  ,0)
    FROM @tbl_Jsontable where   ISNULL(updateFlag,'N') <> 'Y'--  and Mastertag <> 'SegmentDetails'
    RETURN
END



CREATE OR ALTER PROCEDURE SP_JSON_MERGE @i_vcFirstJson NVARCHAR(MAX), 
 @i_vdSecondJson NVARCHAR(MAX), @o_jsonoutput NVARCHAR(MAX) OUTPUT
AS  
BEGIN  
  DECLARE @str VARCHAR(MAX)=''
  DECLARE @TBL_TABLE1 TABLE(DATA1 varchar(max), DATA2 VARCHAR(MAX))
  IF LEFT(@i_vcFirstJson, 1) = '{' AND LEFT(@i_vdSecondJson, 1) = '{' 
  BEGIN  
    SET @str = 'DECLARE @ModifiedJson NVARCHAR(MAX) = ''' + @i_vcFirstJson + ''';
    SELECT @ModifiedJson = 
    CASE 
        WHEN d.[type] IN (4, 5) 
        THEN json_modify(@ModifiedJson, concat(''$.'', d.[key]), json_query(d.[value])) 
        ELSE json_modify(@ModifiedJson, concat(''$.'', d.[key]), d.[value])
    END
    FROM OPENJSON(''' + @i_vdSecondJson + ''') AS d;

    SELECT @ModifiedJson AS MergedJson;';
    INSERT INTO @TBL_TABLE1(DATA1)
    EXEC(@str)
	SELECT @i_vcFirstJson = DATA1  from @TBL_TABLE1
	--SELECT case when TYPE1 in (4,5) then   FROM @TBL_TABLE1
  END 
  ELSE IF LEFT(@i_vcFirstJson, 1) = '[' AND LEFT(@i_vdSecondJson, 1) = '{' 
  BEGIN  
    SET @str = 'SELECT json_modify('''+@i_vcFirstJson+''', ''append $'', json_query('''+@i_vdSecondJson+'''));  '
	INSERT INTO @TBL_TABLE1(DATA1)
    EXEC(@str)
    select @i_vcFirstJson = DATA1 from @TBL_TABLE1
  END ELSE 
  BEGIN  
    SET @str =' SELECT  CONCAT(''['', '''+@i_vcFirstJson+''', '','', '
	 +' RIGHT('''+@i_vdSecondJson+''', LEN('''+@i_vdSecondJson+''') - 1));  '
	INSERT INTO @TBL_TABLE1(DATA1)
    EXEC(@str)
    select @i_vcFirstJson = DATA1,  @i_vcFirstJson = DATA2 from @TBL_TABLE1
  END  
  SET @o_jsonoutput = @i_vcFirstJson;  
  RETURN 
END  
GO



CREATE  FUNCTION [dbo].[fn_json_merge] (@i_vcFirstJson NVARCHAR(MAX), 
 @i_vdSecondJson NVARCHAR(MAX))  
RETURNS NVARCHAR(MAX)  
AS  
BEGIN  
/*            
///////////////////////////////////////////////////////////////////////////////////////////            
// Create By     : Vaibhav Garg    
// Created Date  : 21-JAN-2021           
// CCT NO        :           
// Description   : This Function is used to merge to JSON String into a single JSON.     
// Reviewed By   :  Shyam M             
// Review Date   :  28-Mar-2022             
//////////////////////////////////////////////////////////////////////////////////////////            
  */      

  IF LEFT(@i_vcFirstJson, 1) = '{' AND LEFT(@i_vdSecondJson, 1) = '{' 
  BEGIN  
    SELECT @i_vcFirstJson = CASE WHEN d.[type] in (4,5) THEN 
	  json_modify(@i_vcFirstJson, concat('$.',d.[key]), json_query(d.[value])) 
	  ELSE @i_vcFirstJson END,  
    @i_vcFirstJson = CASE WHEN d.[type] NOT IN (4,5) THEN 
	  json_modify(@i_vcFirstJson, concat('$.',d.[key]), d.[value]) ELSE @i_vcFirstJson END  
        FROM OPENJSON(@i_vdSecondJson) AS d;  
  END 
  ELSE IF LEFT(@i_vcFirstJson, 1) = '[' AND LEFT(@i_vdSecondJson, 1) = '{' 
  BEGIN  
    SELECT @i_vcFirstJson = json_modify(@i_vcFirstJson, 'append $', json_query(@i_vdSecondJson));  
  END ELSE 
  BEGIN  
    SELECT @i_vcFirstJson = CONCAT('[', @i_vcFirstJson, ',', 
	 RIGHT(@i_vdSecondJson, LEN(@i_vdSecondJson) - 1));  
  END  
  RETURN @i_vcFirstJson;  
END  
GO
