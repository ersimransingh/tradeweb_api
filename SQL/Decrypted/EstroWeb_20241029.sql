SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE  PROCEDURE [dbo].[SP_UserProfile]  @vcXML AS VARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(500) OUTPUT
WITH ENCRYPTION
AS
BEGIN

  DECLARE @XMLData XML
  SET @XMLData = CAST(@vcXML AS XML)
  DECLARE @strClientCode VARCHAR(10) =  Isnull(@XMLData.value('(ClientCode)[1]', 'VARCHAR(8)'),''),
		  @strDPID VARCHAR(20) =  Isnull(@XMLData.value('(DPID)[1]', 'VARCHAR(20)'),'')

			If  exists (select count(*) from sysobjects where name='Client_AdditionalDetail' having count(*) > 0)
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

CREATE  PROCEDURE [dbo].[SP_Ledger]  @vcXML AS VARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(500) OUTPUT
WITH ENCRYPTION
AS
BEGIN

  DECLARE @XMLData XML
  SET @XMLData = CAST(@vcXML AS XML)
  DECLARE @strClientCode VARCHAR(10) =  Isnull(@XMLData.value('(ClientCode)[1]', 'VARCHAR(8)'),''),
		  @strDPID VARCHAR(20) =  Isnull(@XMLData.value('(DPID)[1]', 'VARCHAR(20)'),''),
		  @strFromDt VARCHAR(8) =  Isnull(@XMLData.value('(FromDate)[1]', 'VARCHAR(8)'),''),
		  @strToDt   VARCHAR(8) =  Isnull(@XMLData.value('(ToDate)[1]', 'VARCHAR(8)'),'')

 
		 Select ld_clientcd ClientCode,ld_dt Date,ld_chequeno ChequeNo,ld_particular Particular,Debit ,Credit ,
				Balance = sum(Balance) OVER (PARTITION BY ld_clientcd ORDER BY Ord,ld_clientcd,Type,ld_dt1,  SerialNo) 
				----Balance = sum(Balance)  over(order by Ord,ld_clientcd,Type,ld_dt1 rows unbounded preceding)
			From (
				select '0' as Ord ,ld_clientcd,  convert(char,convert(datetime,@strFromDt),103)ld_dt,'' ld_chequeno,'Opening Balance' ld_particular,
					0 Debit, 0 Credit,
					CASE When sum(ld_amount)  > 0 Then cast((sum(ld_amount)) as decimal (15,2))  ELSE cast((sum(ld_amount)) as decimal (15,2)) * (- 1) END Balance, 
					SerialNo = 0, 0 Type,convert(datetime,'01 Apr 2011') ld_dt1
				From Ledger with (nolock) ,ClienT_master with (nolock) 
				Where ld_clientcd = cm_Cd and cm_Cd = @strClientCode and ld_dpid = @strDPID and ld_dt < @strFromDt
				Group By ld_clientcd,cm_name -- Having sum(ld_amount) <> 0

				union all 

				select '1' as Ord ,ld_clientcd,convert(char,convert(datetime,ld_dt),103)ld_dt, Case when ld_chequeno ='0' then '' else ld_chequeno end as ld_chequeno,ltrim(rtrim(ld_particular)) as ld_particular,
					Case When ld_amount > 0 Then cast((ld_amount) as decimal (15,2)) else 0 end Debit,
					Case When ld_amount < 0 Then cast((abs(ld_amount)) as decimal (15,2)) else 0 end Credit,
					CASE When ld_amount  > 0 Then cast((abs(ld_amount)) as decimal (15,2))  ELSE cast((abs(ld_amount)) as decimal (15,2)) * (- 1) END Balance,
					SerialNo = ROW_NUMBER() OVER (ORDER BY ld_clientcd, ld_dt,ld_particular,Case When ld_amount < 0 Then 'C' Else 'D' End),1 Type,ld_dt ld_dt1
				From Ledger with (nolock) ,ClienT_master with (nolock) 
				Where ld_clientcd = cm_Cd  And cm_cd = @strClientCode and ld_dpid = @strDPID and ld_dt between @strFromDt and @strToDt
			) As A
            Order by ld_clientcd,Type,ld_dt1

END
GO

CREATE PROCEDURE [dbo].[SP_Holding]  @vcXML AS VARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(500) OUTPUT
WITH ENCRYPTION
AS
BEGIN

  DECLARE @XMLData XML
  SET @XMLData = CAST(@vcXML AS XML)
  DECLARE @strClientCode VARCHAR(10) =  Isnull(@XMLData.value('(ClientCode)[1]', 'VARCHAR(8)'),''),
		  @strDPID VARCHAR(20) =  Isnull(@XMLData.value('(DPID)[1]', 'VARCHAR(20)'),'')

 
		    Declare @holdDate varchar(50)= '[Holding as on ' + Rtrim((select convert(char,convert(datetime,max(hld_hold_date)),103) as datef from holding)) + ' ISIN Name]'

			Declare @HoldQry varchar(max) =  'Select    hld_isin_code as ISIN, rtrim(ltrim(sc_company_name)) + '' ('' + rtrim(ltrim(sc_isinname)) + '')'' as '+ @holdDate + ', 
						bt_description as [Balance Type], Cast(hld_ac_pos as Decimal(10,3)) as Quantity, convert(decimal(15,2),sc_security_rate) as  Rate ,  
						Cast((hld_ac_pos * sc_security_rate) as Decimal(15,2)) as Value
            from Holding with(NoLock), Beneficiary_Type with(NoLock), Security with(NoLock), Client_master  with(NoLock) 
            Where hld_ac_code = cm_cd and bt_code = hld_ac_type and hld_isin_code = sc_isincode and hld_ac_code = '''+ @strClientCode +'''  and hld_dpid = '''+ @strDPID+'''  
			Order by   ' +@holdDate

		  Exec (@HoldQry)

END
GO

CREATE  PROCEDURE [dbo].[SP_Transaction]  @vcXML AS VARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(500) OUTPUT
WITH ENCRYPTION
AS
BEGIN

  DECLARE @XMLData XML
  SET @XMLData = CAST(@vcXML AS XML)
  DECLARE @strClientCode VARCHAR(10) =  Isnull(@XMLData.value('(ClientCode)[1]', 'VARCHAR(8)'),''),
		  @strDPID VARCHAR(20) =  Isnull(@XMLData.value('(DPID)[1]', 'VARCHAR(20)'),''),
		  @strFromDt VARCHAR(8) =  Isnull(@XMLData.value('(FromDate)[1]', 'VARCHAR(8)'),''),
		  @strToDt   VARCHAR(8) =  Isnull(@XMLData.value('(ToDate)[1]', 'VARCHAR(8)'),'')


		SELECT td_trxdate Date,OrdDate,td_reference TrxNo,td_text Particular ,ISINName ISINCode,acdesc AcType, Debit,Credit,
		  Balance  = sum(Balance) OVER (PARTITION BY td_ac_code, ISINName, td_ac_type ORDER BY td_ac_code,ISINName,td_ac_type,OrdDate,td_debit_credit, td_text, tag, SerialNo)
		--Balance = sum(Balance)  over(order by td_ac_code,sc_company_name, td_isin_code, td_ac_type, tag, SerialNo rows unbounded preceding)
								
				FROM (
					  SELECT  '1' Tag, convert (char,convert(datetime,@strFromDt),103) as  td_trxdate,'' td_reference,'Opening Balance'  td_text, 
							  td_isin_code, sc_company_name +' '+ sc_isinname + ' (' + td_isin_code + ')' ISINName, 0 Debit, 0 Credit ,
							  sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE td_qty * (- 1) END) Balance, SerialNo = 0,
							  0 td_qty,td_ac_type,bt_description acdesc,sc_company_name,'' td_debit_credit,td_ac_code ,'' as OrdDate
						FROM  Trxweb a with (nolock), Security with (nolock), Client_master with (nolock), Beneficiary_type with (nolock)
						WHERE td_ac_code = cm_cd  and td_isin_code = sc_isincode  And td_ac_type = bt_code  
							 and td_trxdate < @strFromDt   and td_ac_code= @strClientCode  and td_dpid = @strDPID 
							  and exists(Select 1 from Trxweb(nolock) where td_ac_code = a.td_ac_code and td_isin_code = a.td_isin_code
							 and td_ac_type = a.td_ac_type and td_trxdate between @strFromDt and  @strToDt )
						GROUP BY td_ac_code, td_isin_code, sc_company_name, td_ac_type,  bt_description,sc_isinname 
							HAVING sum(CASE td_debit_credit WHEN 'C' THEN td_qty ELSE td_qty * (- 1) END) <> 0
			UNION ALL

					    SELECT  '2' Tag,convert (char,convert(datetime,td_trxdate),103) td_trxdate,td_reference,td_text, td_isin_code, 
							sc_company_name +' '+ sc_isinname + ' (' + td_isin_code + ')' ISINName,
							  Case td_debit_credit  when 'D' then cast((td_qty)as decimal(15,3)) else 0 end  'Debit', 
							  Case td_debit_credit  when 'C' then cast((td_qty)as decimal(15,3)) else 0 end  'Credit',
							  (CASE td_debit_credit WHEN 'C' THEN td_qty ELSE td_qty * (- 1) END) AS  Balance, 
							  SerialNo = ROW_NUMBER() OVER (ORDER BY td_ac_code, td_isin_code,td_ac_type,td_trxdate,td_reference, td_text,  td_debit_credit),
							  td_qty,td_ac_type,bt_description acdesc,sc_company_name,td_debit_credit,td_ac_code ,td_trxdate as OrdDate
						FROM trxweb a with (nolock), Security with (nolock), Client_master with (nolock), Beneficiary_type with (nolock)
						WHERE td_ac_code = cm_cd  and td_isin_code = sc_isincode  And td_ac_type = bt_code  
							 and td_trxdate between @strFromDt and  @strToDt  and td_ac_code = @strClientCode and td_dpid = @strDPID   

			)  A 
                 	
END
GO

CREATE  PROCEDURE [dbo].[SP_SecurityListing]  @vcXML AS VARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(500) OUTPUT
WITH ENCRYPTION
AS
BEGIN

  DECLARE @XMLData XML
  SET @XMLData = CAST(@vcXML AS XML)
  DECLARE @strClientCode VARCHAR(10) =  Isnull(@XMLData.value('(ClientCode)[1]', 'VARCHAR(8)'),''),
		  @strDPID VARCHAR(20) =  Isnull(@XMLData.value('(DPID)[1]', 'VARCHAR(20)'),''),
		  @strFromDt VARCHAR(8) =  Isnull(@XMLData.value('(FromDate)[1]', 'VARCHAR(8)'),''),
		  @strToDt   VARCHAR(8) =  Isnull(@XMLData.value('(ToDate)[1]', 'VARCHAR(8)'),''),
		  @searchText VARCHAR(100) =  Isnull(@XMLData.value('(SearchText)[1]', 'VARCHAR(100)'),''),
		  @searchBy   VARCHAR(1) =  Isnull(@XMLData.value('(SearchBy)[1]', 'VARCHAR(1)'),''),
		  @status  Bit =  Isnull(@XMLData.value('(Status)[1]', 'bit'),0)
		  
		  Declare  @WhereQry  Varchar(50) = '';
		    
			 Set @WhereQry = Case When @status=1 Then '  and sc_security_status = ''01''' Else '' End
				    Declare @Qry Varchar(500)= ''

				   IF(@searchBy='C')
				      Begin
							Set @Qry = 'Select ''Security Listing'' as Heading,sc_isincode ISINcode,sc_isinname ISINname, sc_company_name CompanyName, cast(sc_security_rate as decimal(15,2)) as Rate 
										From Security where sc_company_name like ''%' + @searchText + '%''' +   @WhereQry
						    Exec (@Qry)							
					  End
				   Else
				      Begin
						 Set @Qry  = 'Select ''Security Listing'' as Heading, sc_isincode ISINcode,sc_isinname ISINname, sc_company_name CompanyName, cast(sc_security_rate as decimal(15,2)) as Rate 
									  From Security where sc_isincode = ''' + @searchText + '''' +  @WhereQry
						 Exec (@Qry)
					  End

END
GO

CREATE PROCEDURE [dbo].[sp_GetReport] @vcFunctionName AS VARCHAR(50), @vcXML AS VARCHAR(MAX), @o_vcErrorFlag VARCHAR(1) OUTPUT, @o_vcErrorMessage VARCHAR(500) OUTPUT
WITH ENCRYPTION
AS
BEGIN

IF @vcXML = ''
  BEGIN
    SET @o_vcErrorFlag  = 'E'
    SET @o_vcErrorMessage = 'Please Send Input Parameter(s)'
    RETURN 1
END 

  DECLARE @XMLData XML
  SET @XMLData = CAST(@vcXML AS XML)
  DECLARE @strClientCode VARCHAR(10) =  Isnull(@XMLData.value('(ClientCode)[1]', 'VARCHAR(8)'),''),
		  @strFromDt VARCHAR(8) =  Isnull(@XMLData.value('(FromDate)[1]', 'VARCHAR(8)'),''),
		  @strToDt   VARCHAR(8) =  Isnull(@XMLData.value('(ToDate)[1]', 'VARCHAR(8)'),'')

	
	IF (@vcFunctionName = 'UserProfileHome')
		BEGIN
			EXEC SP_UserProfile    @vcXML , @o_vcErrorFlag , @o_vcErrorMessage 
			RETURN 1
		END

	ELSE IF (@vcFunctionName = 'Ledger')
		BEGIN
			EXEC SP_Ledger    @vcXML , @o_vcErrorFlag , @o_vcErrorMessage 
			RETURN 1
		END

	ELSE IF (@vcFunctionName = 'Holding')
		BEGIN
			EXEC SP_Holding    @vcXML , @o_vcErrorFlag , @o_vcErrorMessage 
			RETURN 1
		END

	ELSE IF (@vcFunctionName = 'Transaction')
		BEGIN
			EXEC SP_Transaction    @vcXML , @o_vcErrorFlag , @o_vcErrorMessage 
			RETURN 1
		END

	ELSE IF (@vcFunctionName = 'SecurityListing')
		BEGIN
			EXEC SP_SecurityListing    @vcXML , @o_vcErrorFlag , @o_vcErrorMessage 
			RETURN 1
		END

END
GO