CREATE  PROCEDURE sp_ProcessRekyc @i_vcString VARCHAR(MAX), @i_vcType VARCHAR(20), @o_vcString VARCHAR(MAX) OUTPUT WITH ENCRYPTION AS
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