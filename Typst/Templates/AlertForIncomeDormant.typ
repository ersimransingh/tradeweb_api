
////////
//////// ************ Report Code Start ************
////////


#let data = json(sys.inputs.at("file_path"))



////// ***** extract-and-generate *** Main function to call sub function and bind report


#let extract-and-generate(json_data) = {

   //// Extract company settings from rs0
  let companyJson = json_data.at("rs0").at(0)

  let page_size = companyJson.at("PageSize", default: "A4").trim()

  let rs1Dat1 = json_data.at("rs1").at(0)
 
 set page(
	  paper: lower(page_size),
	  flipped: false,
	  margin: 1cm,
      footer: context [
		  #set align(left)
		  Print Date : #datetime.today().display("[day]/[month]/[year]")
		  #h(1fr)
		  #companyJson.FooterModuleName : [Page  #numbering("1", ..counter(page).at(here()).map(x => x))]
		],
	)

    ////// Logic for bind Report Header

      grid(
        columns: (1fr, 4fr, 1fr),
        rows: 1,
        gutter: 1pt,
        align: center,
        grid.cell(image("../Assets/CompanyLogo.png", width: 50%)),
      
        grid[
          #text(companyJson.CompanyName, weight: "bold", size: 4mm) \
          #companyJson.Address1 \
          #companyJson.Address2 \
          #companyJson.Address3 \
		  CIN No. :  #companyJson.CINNo \
		  #companyJson.ReportName
        ],
        grid[],
      )
      
	  v(20pt)
	  
	  grid(
        columns: (8fr),
        rows: 1,
        gutter: 1pt,
        align: left,
        grid[
		   To, \
          #companyJson.Client [#companyJson.ClientCode] \
          #companyJson.Address1 \
          #companyJson.Address2 \
          #companyJson.Address3 \
          Pin: #companyJson.Clientpin \
          Mobile: #companyJson.Telephone \   \
		 \
        ],
		
      )
	  
	  v(10pt)
	  //// Subject line
	  text(rs1Dat1.Subject)
	  
	  v(13pt)
	  /////// Main text 
	  text(rs1Dat1.HeaderText)
	  
	   //v(10pt)
	   
	   //text(rs1Dat1.AuthSign)
	   
	   v(10pt)
	   
	   
	   text(rs1Dat1.BodyText)
	  
	   v(10pt)
	   
	     


	   v(15pt)
	   
	   text(rs1Dat1.FooterText)
	   
	  	   
  }
 


#set text(8pt, font: "Arial")



#extract-and-generate(data)

