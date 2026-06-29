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