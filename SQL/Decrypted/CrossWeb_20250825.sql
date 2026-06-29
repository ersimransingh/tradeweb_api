CREATE TYPE [dbo].[tb_ParameterXMLLIST] AS TABLE(
	[J_Ui] [varchar](max) NULL,
	[strSql] [varchar](max) NULL,
	[X_Filter] [varchar](max) NULL,
	[X_GFilter] [varchar](max) NULL,
	[J_Api] [varchar](max) NULL
)
GO

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

CREATE   Function [dbo].[fn_ParameterXMLRep] (@vcXML XML)  RETURNS @tbl_ParameterList TABLE(J_Ui VARCHAR(MAX), SSql VARCHAR(MAX), X_Filter VARCHAR(MAX), X_GFilter VARCHAR(MAX),
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

CREATE   FUNCTION [dbo].[fn_SplitString] (@input NVARCHAR(MAX), @delimiter CHAR(1))
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

CREATE   FUNCTION [dbo].[fnStampDutyRate] (@strSecType NVARCHAR(10))
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

CREATE   FUNCTION [dbo].[ReturnTable]
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

CREATE PROCEDURE [dbo].[SP_InitializeLogin] @dsXml AS XML
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
@strReportCode VARCHAR(50), @strReportCategroy VARCHAR(50), @strRequestFrom VARCHAR(1)
WITH ENCRYPTION
AS
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

CREATE PROCEDURE [dbo].[stpr_APIReportGeneration] @dsXml AS XML = NULL
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

CREATE PROCEDURE [dbo].[stpr_BillBreakup] @dsXml AS XML = NULL
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

CREATE PROCEDURE [dbo].[stpr_CrossNetMenu] @dsXml AS XML = NULL
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

CREATE PROCEDURE [dbo].[stpr_CrossNetSearch] @dsXml AS XML = NULL
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

   DECLARE @strUserid VARCHAR(500)='', @strOption VARCHAR(500)=''

   SELECT @strUserid = ParameterValue From @tb_ParamListDetail where ParameterName = 'UserId'

   SELECT @strOption = ParameterValue From @tb_ParamListDetail 
   where ParameterName = 'Option'

   IF @strOption = 'Client'
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

CREATE PROCEDURE [dbo].[stpr_CrosstrxholdbillNew] @dsXml NVARCHAR(MAX) 
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
	SET @o_Message = REPLACE(@o_Message,'##FormatGrid##','Grid1,Grid2,Grid3,Grid4')
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
	/*
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
    */
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
   
  DECLARE @tbl_trxrtpe TABLE(TRXTYPE VARCHAR(10))

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
  
  
  IF ISNULL(@strOutputType,'G') = 'G'
  BEGIN
    SELECT ClientCode, ClientName, TrxnDate = CONVERT(VARCHAR,CAST(TrxnDate AS DATE),106), ISIN, ScripName, TrxnNo, Description, Narration, DebitCredit, DebitQty,
	CreditQty, Runingbalace
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
	  CreditQty = '', Runingbalace = ''
	  FOR XML PATH('Detail'))
    END 
	ELSE
	BEGIN
      SET @XMLDATA1 = 
	  (SELECT ClientCode, ClientName, TrxnDate = CONVERT(VARCHAR,CAST(TrxnDate AS DATE),106), ISIN, ScripName, TrxnNo, Description, Narration, DebitCredit, DebitQty,
	  CreditQty, Runingbalace
	  FROM  @tbl_Transaction ORDER BY SerialNo FOR XML PATH('Detail'))
	END
	SET @o_vcErrorMessage = CAST(@XMLDATA1 AS VARCHAR(MAX))
	SET @o_vcErrorFlag = 'S'
	RETURN 1
END
  
END
GO

CREATE PROCEDURE stpr_HoldingStatement @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, 
	@o_vcErrorMessage VARCHAR(MAX) OUTPUT
WITH ENCRYPTION
AS
BEGIN
	--- Parameter Declaration
  DECLARE @dtFromDate VARCHAR(8), @dtToDt VARCHAR(8), @strUserId VARCHAR(500) = '', @strLevel VARCHAR(1), 
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
	AND td_curdate = @dtFromDate
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

CREATE PROCEDURE stpr_ClientListing  @vcXML NVARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, 
	@o_vcErrorMessage VARCHAR(MAX) OUTPUT AS 
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