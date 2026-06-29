CREATE OR ALTER PROCEDURE [dbo].[SP_Ledger]  @dsXml AS XML
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

 
		 Select  CompanyCode [DPID],  ClientCode, ld_dt Date,ld_chequeno ChequeNo,ld_particular Particular,Debit ,Credit ,
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

			DECLARE @CompanyName VARCHAR(100) = Isnull((select top 1 sp_sysvalue from Sysparameter where sp_parmcd='NAME'),'')
			DECLARE @CompanyAdd1 VARCHAR(1000) = '', @CompanyAdd2 VARCHAR(1000) = '', @CompanyAdd3 VARCHAR(1000) = ''

			SELECT @CompanyAdd1 = Isnull((select top 1 sp_sysvalue from Sysparameter where sp_parmcd='ADD1'),'')
			SELECT @CompanyAdd2 = Isnull((select top 1 sp_sysvalue from Sysparameter where sp_parmcd='ADD2'),'')
			SELECT @CompanyAdd3 = Isnull((select top 1 sp_sysvalue from Sysparameter where sp_parmcd='ADD3'),'')
			DECLARE @ClientName VARCHAR(135) = (select cm_name from Client_master where cm_cd=@strClientCode)

			  ------------- For Second table property ---------------
		     SELECT '<XmlData>
						<TotalList>Debit,Credit</TotalList>
						<RightList>Debit,Credit,Balance</RightList>
						<HideList>DPID,ClientCode</HideList>
						<DateFormat></DateFormat>
						<DateFormatList></DateFormatList>
						<Dec2List>Debit,Credit,Balance</Dec2List>
						<Dec4List></Dec4List>
						<DrCRColorList></DrCRColorList>
						<PnLColorList></PnLColorList>
						<PrimaryKey></PrimaryKey>
						<CompanyName>' + @CompanyName + '</CompanyName>
						<CompanyAdd1>' + @CompanyAdd1 + '</CompanyAdd1>
						<CompanyAdd2>' + @CompanyAdd2 + '</CompanyAdd2>
						<CompanyAdd3>' + @CompanyAdd3 + '</CompanyAdd3>
						<ReportHeader>Ledger Report from ' + CONVERT(VARCHAR(10), CAST(@strFromDt AS DATE), 103) + ' to ' + CONVERT(VARCHAR(10), CAST(@strToDt AS DATE), 103) + ' \n ' + RTRIM(@ClientName) + ' (' + RTRIM(@strClientCode) + ')</ReportHeader>
						<PDFWidth>230</PDFWidth>
						<PDFHeight>297</PDFHeight>
					</XmlData>' 
   AS Settings
END


