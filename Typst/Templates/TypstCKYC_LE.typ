//#import "@preview/muchpdf:0.1.0": muchpdf
#let muchpdf = {

let muchpdf-plugin =plugin("muchpdf.wasm")

let encode-pages(pages) = {
    // Convert to array if necessary.
    let pages = pages
    if type(pages) == int {
      pages = ((start: pages, end: pages),)
    } else if type(pages) == dictionary {
      pages = (pages,)
    } else if type(pages) != array {
      panic("expected pages to be `int`, `dictionary`, or `array`, but got " + type(pages))
    }

    // Convert to array of ranges.
    let ranges = ()
    for page in pages {
      if type(page) == int {
        ranges.push((start: page, end: page, step: 1))
      } else if type(page) == dictionary {
        ranges.push((
          start: page.remove("start", default: 0),
          end: page.remove("end", default: none),
          step: page.remove("step", default: 1),
        ))
        if page.len() != 0 {
          panic("page contains attributes other than `start`, `end`, and `step`")
        }
      } else {
        panic("pages array contained value of type that is neither `int` nor `dictionary`, got " + type(page))
      }
    }

    // Encode ranges to flat byte array.
    let encoded = bytes(())
    for range in ranges {
      if range.start < 0 or (range.end != none and range.end < 0) {
        panic("pages must not be negative")
      }
      if range.step < 1 {
        panic("step must be positive")
      }

      let encode-int = int.to-bytes.with(endian: "little", size: 4)
      encoded += encode-int(range.start)
      encoded += encode-int(if range.end == none { -1 } else { range.end })
      encoded += encode-int(range.step)
    }
    encoded
  }

  let muchpdf(
    data,
    scale: 2.0,
    pages: (:),
    ..args,
  ) = {
    assert.eq(type(data), bytes)

    let rendered-pages = muchpdf-plugin.render(
      data,
      float.to-bytes(scale),
      encode-pages(pages),
    )
    let cursor = 0
    while cursor < rendered-pages.len() {
      let page-len = int.from-bytes(
        endian: "little",
        signed: false,
        rendered-pages.slice(cursor, count: 8),
      )
      cursor += 8
      let page = rendered-pages.slice(cursor, count: page-len)
      cursor += page-len

      image.decode(page, format: "svg", ..args)
    }
  }

  muchpdf
}

////////
//////// ************ Report Code Start ************
////////
// Load JSON

#let data = json(sys.inputs.at("file_path"))
#let ReKycDetails = data.at(0)
#let Attachment = ReKycDetails.Attachments

// Page setup
#set page(
  width: 210mm,
  height: 430mm,
  margin: 5mm //(top: 1cm, bottom: 1cm, x: 1cm)
)
#set text(size: 8pt,  weight: "regular")

/*
 ///// ***  Uses of All variables and keywords
 
      gutter: 2pt,   //  Adds space between elements (e.g., columns, grid cells)
	  align: left,
	  inset: 2pt,    //  Adds space inside elements (like padding) for padding (spacing)
	  stroke: black/2pt/none, // for adding border
	    //#muchpdf(data, width: 10cm,height:20cm, scale: 1.5, pages: (0, 2, 4))
		//#let data = read("../Assets/NomineeAttach", encoding: none)
		//#muchpdf(data,  pages:(0,2))
*/

//// *** Declare global variable
#let commonSpace = 90pt
#let photoPath =    ""
#let signPath = ""

#for item in Attachment {
	if item.base64Image !="" {
	    if item.ImageType == "Photo_Img"  {
		   photoPath = "../Assets/" + item.ImageType + "_" + ReKycDetails.ClientCode + item.base64Image
		 }
        else if item.ImageType == "Signature_Img" {
		   signPath = "../Assets/" + item.ImageType + "_" + ReKycDetails.ClientCode + item.base64Image
		 }		 
	}
}

// ==== KYC Box Layout ====
#box(
  stroke: 0pt,
  inset: 0pt,
  [
     #grid(
      columns: (6fr),
      gutter: 4pt,
	  align: left,
	  inset: 4pt,
	  fill: rgb(149, 206, 216),
      [
	  	#text(size: 8.5pt, weight: "bold", "CENTRAL KYC REGISTRY | Know Your Customer(KYC) Application Form | Legal Entity/Other than Individual")
	  ]
	)
	#text("Important Insturctions: ", weight: "bold")
    #linebreak()	
    #grid(
      columns: (4.6fr, 5.7fr, 1.6fr),
      gutter: 0pt,
	  inset: 5pt,
	  stroke: none,	  
      [
	  // === Left side: Instructions box
         #grid(
         columns: (6fr),
         gutter: 6pt,
          [
            #text("A) Fields markded with '*' are mandatory fields.")
            #linebreak()
            #text("B) Tick '✔' wherever applicable.")
            #linebreak()
            #text("C) Please fill the date in DD-MM-YYYY format.")
			#linebreak()
            #text("D) Please fill the form in English and in BLOCK letters. ")
			#linebreak()
            #text("E) KYC Number of applicant is mandatory for update application. ")
          ]
        )
      ],
      // === Right side: Logo + App Info
      [
              #grid(
                columns: (6fr),
                gutter: 6pt,
                [
                    #text("F) List of State / U.T code as per Indian Motor Vehicle Act, 1988 is available at the end.")
					#linebreak()
					#text("G) List of two character ISO 3166 country codes is available at the end.")
					#linebreak()
					#text("H) Please read section wise detailed guidlines/instructions at the end.")
					#linebreak()
					#text("I) For particular section update, please tick() in the box available before the section number and strike off the sections not required to be update.") 
                ]
				
           )          
      ],
	  grid.cell(image("../Assets/CompanyLogo.png", width: 100%))
    )

    #v(1pt)
////// For Identity column box
  #box(
          fill: gray.lighten(70%),		  
          inset: 2pt,
          [
		    #grid(
				columns: (3fr, 2fr, 7fr),
				  [ 
					#h(8pt) #text("For office use only", weight: "bold")
					
					#h(8pt) #text("(To be filled by financial institution)")
				 ],
				 [
					#h(8pt) #text("Application Type*")
					
					#h(8pt) #text("KYC Number* ")
					
				],
				 [
					 #text((if ReKycDetails.ApplicationType == "New" { "☑" } else { "☐" }) + " New")
				     #text((if ReKycDetails.ApplicationType == "Update" { "☑" } else { "☐" }) + " Update")
					
					 #h(7pt) #text(ReKycDetails.KYCNumber) #text("   (Mandatory for KYC update request)")
					
				 ]
			)
          ]
        )
	 
    #grid(
      columns: (6fr),
      gutter: 4pt,
	  align: left,
	  inset: 4pt,
	  fill: rgb(185, 221, 235),
      [
	  	#text(size: 8.5pt, weight: "bold", "1. ENTITY DETAILS (Please refer instruction A at the end)")
	  ]
	)      	 
#v(5pt)
	
	 #grid(
      columns: (4fr, 8fr),
      gutter: 2pt,
	  align: left,
	  inset: 0pt,
	  stroke: none, // for adding border
	  grid[
			#h(8pt)#text("Name*  :")
	 
			#h(8pt)#text("Entity Constitution Type* :")
	
			#h(8pt)#text("Date of Incorporation / Formation* :")
	
			#h(8pt)#text("Date of Commencement of Bussiness : ")
   
			#h(8pt) #text("Place of Incorporation / Formation* :")
		  
			#h(8pt) #text("Country of Incorporation / Formation* : ")
			
			#h(8pt) #text("TIN or Equivalent Issuing Country : ")
			
			#h(8pt) #text("PAN* : ")

			#h(8pt) #text("TIN / GST Registration Number : ")

		],
		 grid[
			 #text( ReKycDetails.EntityName) #v(0pt)
			 #text(ReKycDetails.EntityConstType ) #text("   ")  #text("  (Please refer Institution B at the end)") #v(0pt)
			 
			 #let dateOfIncor = if "DateOfIncorp" in ReKycDetails and str(ReKycDetails.DateOfIncorp).len() == 8 {
			  let d = str(ReKycDetails.DateOfIncorp)
			  d.slice(6,8) + " - " + d.slice(4,6) + " - " + d.slice(0,4)
			 } else { "--" }
		     #text(dateOfIncor) #v(0pt)
		   
		    #let dateOfCommc = if "DateOfComnc" in ReKycDetails and str(ReKycDetails.DateOfComnc).len() == 8 {
			  let d = str(ReKycDetails.DateOfComnc)
			  d.slice(6,8) + " - " + d.slice(4,6) + " - " + d.slice(0,4)
			 } else { "--" }
		     #text(dateOfCommc) #v(0pt)
			 
			 #text(ReKycDetails.PlaceOfIncorp ) #v(0pt) 
			 #text(ReKycDetails.CountryOfIncorp ) #v(0pt) 
			 #text(ReKycDetails.TINCountry ) #v(0pt) 
     
             #text(ReKycDetails.PAN) #v(0pt)
		     #text(ReKycDetails.TINGst ) #v(0pt) 

		]
	 )
	
	///// 2
	#grid(
		columns: (6fr),
		gutter: 4pt,
		align: left,
	    inset: 4pt,
		fill: rgb(185, 221, 235),
         [
			#text(size: 8.5pt, weight: "bold", "2.PROOF OF IDENTITY (POI) * (Please refer instruction B at the end)")
		 ]
	)

	#grid(
		columns: (10fr),
		gutter: 5pt,
		align: left,
		inset: 5pt,
		grid[
		
				#h(8pt) #text(if ReKycDetails.POI_OffValDoc == "Y" { "☑" } else { "☐" }) #text(" Official valid document(s) in respect of person authorised to transact ")
			
				#h(8pt) #text(if ReKycDetails.POI_CertIncorp == "Y" { "☑" } else { "☐" }) #text("  Certificate of Incorporation / Formation ") #h(40pt) #text(weight:"bold", ReKycDetails.POI_CertIncorpValue)
			
				#h(8pt) #text(if ReKycDetails.POI_RegCert == "Y" { "☑" } else { "☐" }) #text("  Registration Certificate ") #h(40pt) #text(weight:"bold", ReKycDetails.POI_RegCertValue)
			
				#h(8pt) #text(if ReKycDetails.POI_Memornd == "Y" { "☑" } else { "☐" }) #text("  Memorandum and Articles of Association ")
			
				#h(8pt) #text(if ReKycDetails.POI_PartDeed == "Y" { "☑" } else { "☐" }) #text("  Partership Deed ")
				
				#h(8pt) #text(if ReKycDetails.POI_TrustDeed == "Y" { "☑" } else { "☐" }) #text("  Trust Deed ")
				
				#h(8pt) #text(if ReKycDetails.POI_ResolBoard == "Y" { "☑" } else { "☐" }) #text(" Resolution of Board / Managing Committee ")
				
				#h(8pt) #text(if ReKycDetails.POI_POA == "Y" { "☑" } else { "☐" }) #text(" Power of Attorney granted to its manager, officer ors employees to transact on its behalf")
				
				#h(8pt) #text(if ReKycDetails.POI_ActProof1 == "Y" { "☑" } else { "☐" }) #text(" Activity Proof - 1 (For Sale Proprietorship Only) ")
				
				#h(8pt) #text(if ReKycDetails.POI_ActProof2 == "Y" { "☑" } else { "☐" }) #text(" Activity Proof - 2 (For Sale Proprietorship Only) ")	
			]
	)

		///// 3
	#grid(
		columns: (6fr),
		gutter: 4pt,
		align: left,
	    inset: 4pt,
		fill: rgb(185, 221, 235),
         [
			#text(size: 8.5pt, weight: "bold", "2. ADDRESS * (Please see instruction C at the end)")
		 ]
	)
	#v(-2pt)
	   #box(
          fill: gray.lighten(70%),		  
          inset: 2pt,
          [
		    #grid(
				 columns: (6fr),
				 [
				     #text("3.1 Registered Office Address / Place of Bussiness")
				 ]
			)
          ]
        )
	#grid(
      columns: (3fr, 5fr, 2.2fr, 2fr),
      gutter: 2pt,
	  align: left,
	  inset: 1pt,
		  grid[
      	      #text("Proof of Address*") 
		  ],
		  grid[
      	      #text(if ReKycDetails.POA_CertIncop == "Y" { "☑" } else { "☐" }) #text("Certificate of Incorporation / Formation") 
		  ],
		  grid[
      	      #text(if ReKycDetails.POA_RegCert == "Y" { "☑" } else { "☐" }) #text("Registration Certificate") 
		  ],
		  grid[
      	      #text(if ReKycDetails.POA_OthrDoc == "Y" { "☑" } else { "☐" }) #text("Other Document") 
		  ]
	  )	
		
	#grid(
      columns: (2fr, 10fr),
      gutter: 2pt,
	  align: left,
	  inset: 1pt,
	  grid[
      	   #h(8pt)#text("Line 1* :") 

		   #h(8pt)#text("Line 2 :") 

		   #h(8pt)#text("Line 3 :") 
		   
		   #h(8pt) #text("City/Town/Village* :")
			
		   #h(8pt) #text("Pin/Post Code* :")
 
		   #h(8pt)#text("District* :") 

		   #h(8pt) #text("State/U.T.Code* :")

		   #h(8pt) #text("ISO 3166 Country Code* :")
		],
		 grid[
			  #h(98pt) #text(ReKycDetails.RegAddress1) 
										  
			  #h(98pt) #text(ReKycDetails.RegAddress2)
										  
			  #h(98pt) #text(ReKycDetails.RegAddress3)  
										  
			  #h(97pt) #text(ReKycDetails.RegCity)  
										  
			  #h(97pt) #text(ReKycDetails.RegDistrict)
										  
			  #h(96pt) #text(ReKycDetails.RegPincode) 
										  
			  #h(96pt) #text(ReKycDetails.RegState)
										  
			  #h(96pt)#text(ReKycDetails.RegCountry)
			]
	)
	#v(-2pt)
	   #box(
          fill: gray.lighten(70%),		  
          inset: 2pt,
          [
		    #grid(
				 columns: (6fr),
				 [
				     #text("3.2 Local Address in India (If different from Above)*")
				 ]
			)
          ]
        )
		
		#grid(
      columns: (2fr, 10fr),
      gutter: 2pt,
	  align: left,
	  inset: 1pt,
	  grid[
      	   #h(8pt)#text("Line 1* :") 

		   #h(8pt)#text("Line 2 :") 

		   #h(8pt)#text("Line 3 :") 
		   
		   #h(8pt) #text("City/Town/Village* :")
			
		   #h(8pt) #text("Pin/Post Code* :")
 
		   #h(8pt)#text("District* :") 

		   #h(8pt) #text("State/U.T.Code* :")

		   #h(8pt) #text("ISO 3166 Country Code* :")
		],
		 grid[
			  #h(98pt) #text(ReKycDetails.CorrAddress1) 

			  #h(98pt) #text(ReKycDetails.CorrAddress2)

			  #h(98pt) #text(ReKycDetails.CorrAddress3)  
			 
			  #h(97pt) #text(ReKycDetails.CorrCity)  

			  #h(97pt) #text(ReKycDetails.CorrDistrict)

			  #h(96pt) #text(ReKycDetails.CorrPincode) 
			 
			  #h(96pt) #text(ReKycDetails.CorrState)

			  #h(96pt)#text(ReKycDetails.CorrCountry)
			]
			)
	]
 )
	
#pagebreak()


////   CKYC Second Page

// === Email & Mobile Grid ===
	#box(
	  stroke: 0pt,
	  inset: 0pt,
		  [
			#grid(
				  columns: (6fr),
				  gutter: 4pt,
				  align: left,
				  inset: 4pt,
				  fill: rgb(185, 221, 235),
				  [
					#text(size: 8.5pt, weight: "bold"," 4. CONTACT DETAILS (All communications will be sent to Mobile number/Email-ID Provided) (Please refer instruction D at the end)")
				  ]			
				)

			#h(8pt)#text("Mobile : ") 
			#text(
			if ReKycDetails.Mobile != none and ReKycDetails.Mobile.trim() != "" {
				ReKycDetails.Mobile
			} else {
				"NA"
			}
			)
			#v(8pt)
			
			
			#h(8pt)#text("Email : ") 
			#text(
			if ReKycDetails.Email != none and ReKycDetails.Email.trim() != "" {
				ReKycDetails.Email
			} else {
				"NA"
			}
			)
			#v(8pt)

		
		//5 th point


			#grid(
				  columns: (6fr),
				  gutter: 4pt,
				  align: left,
				  inset: 4pt,
				  fill: rgb(185, 221, 235),
					  [
						#text(size: 8.5pt, weight: "bold"," 5. NUMBER OF RELATED PERSON   (Please refer Instruction E at the end)")
					  ]
			  )
	
				#h(8pt)
		
			#v(30pt)
			
			
			//Remarks


			#grid(
				  columns: (6fr),
				  gutter: 4pt,
				  align: left,
				  inset: 4pt,
				  fill: rgb(185, 221, 235),
					  [
						#text(size: 8.5pt, weight: "bold"," 6. Remarks (If any)")
					  ]
			  )
	
				#h(8pt)
				// #text(if ReKycDetails.Remark != none and ReKycDetails.Remark.trim() != "" { ReKycDetails.Remark } else { "NA" })

			#v(30pt)
			

			//Applicaton Declaration

				#grid(
				  columns: (6fr),
				  gutter: 4pt,
				  align: left,
				  inset: 4pt,
				  fill: rgb(185, 221, 235),
					  [
						#text(size: 8.5pt, weight: "bold"," 7. Application Declaration (Please refer Instruction G at the end)")
					  ]
				)
			
			#grid(
                  columns: (3fr, 1fr),
                  gutter: 0pt,
			[

				#box(
						inset: 2pt,
				[
				#h(8pt) #text(
					"I hereby declare that the details furnished above are true and correct to the best of my knowledge and belief and I undertake to inform you of any changes there in immediately. In case any of the above information is found to be false or uture or misleading or misrepressing. Im aware that i may be held liable for it. 
					 I/We heared by consent to receiving information from Central KYC registry through SMS/Email on the above registered number/email Address."
					)
				])

			],
				[
			
			#box(
			  inset: 4pt,
			   [
			 #h(5pt)
		     #h(5pt)
			  #box(
				width: 100%,
				height: 1.5cm,
				stroke: 1pt + black,
				inset: 4pt,
				fill: gray.lighten(70%),
				  [
					#align(bottom + right)[
					#if signPath != "" {
							image(signPath, width: 100%, height: 100%)
						}
				  ]
				]
				)
					#align(center, text("Signature / Thumb Impression of Applicant", size: 8pt))
			  ])

		])


			#h(8pt)#text("Date : ")
			//#text(if ReKycDetails.DeclarationDate != none and ReKycDetails.DeclarationDate.trim() != "" { ReKycDetails.DeclarationDate } else { "NA" })
		#text(
              if ReKycDetails.DeclarationDate != none 
                and ReKycDetails.DeclarationDate.trim() != "" 
                and ReKycDetails.DeclarationDate.len() == 8 {
                let d = ReKycDetails.DeclarationDate
                d.slice(6,8) + "/" + d.slice(4,6) + "/" + d.slice(0,4)
              } else {
                "NA"
              }   
          )
			#h(30pt)
			#h(8pt)#text("Place : ")
			#text(if ReKycDetails.DeclarationPlace != none and ReKycDetails.DeclarationPlace.trim() != "" { ReKycDetails.DeclarationPlace } else { "NA" })
			
			#h(30pt)
			#v(2pt)

			//Attestation / For Office use only

			#grid(
				  columns: (6fr),
				  gutter: 4pt,
				  align: left,
				  inset: 4pt,
				  fill: rgb(185, 221, 235),
				   [
					  #text(size: 8.5pt, weight: "bold"," 8. Attestation / For Office use only", fill: black)
				   ]
			)

			 #box(
          fill: gray.lighten(70%),		  
          inset: 2pt,
				[
			

      #v(10pt)
	  #block(spacing: 8pt)[
		  #h(4pt)#text("Document Received", weight: "bold")#h(15pt)
		  //#text((if ReKycDetails.DocumentReceived == "CertifiedCopies" { "☑" } else { "☐" }) + "Certified Copies")
		  #text( "☐  Certified Copies")
		  #h(15pt)
		  //#text((if ReKycDetails.DocumentReceived == "EequivalentDoc" { "☑" } else { "☐" }) + "Equivalent e-document")
		  #text("☐  Equivalent e-document")
		 
		]
			
	   #v(10pt)
			
			#grid(
				columns: (6fr, 6fr),
				gutter: 0pt,
			[

				#box(
					inset: 2pt,
					fill: rgb("#d3d3d3"), 
				[
						#h(8pt) #text("KYC VERIFICATION CARRIED OUT BY ",weight: "bold", fill: black)
				])



				    #v(8pt)#text("Date : ")
					//#text(if ReKycDetails.EMPDate != none and ReKycDetails.EMPDate.trim() != "" { ReKycDetails.EMPDate } else { "NA" })
				    #h(8pt)
					#v(8pt)#text("EMP. Name : ")
					#h(8pt)
					#v(8pt)#text("EMP. Code : ")
					#h(8pt)
					#v(8pt)#text("EMP. Designation : ")
					#h(8pt)
					#v(8pt)#text("EMP. Branch : ")
					#h(8pt)
					#v(4pt)
					#box(
						width: 100%,
						height: 1.5cm,
						stroke: 1pt + black,
						inset: 4pt,
						[
						#align(center)[
						#text("[Employee Signature]",size: 8pt, fill: gray.lighten(40%))
						]
					]
					)
					#v(8pt)

			],
			[

				#box(
					inset: 2pt,
					fill: rgb("#d3d3d3"), 
					[
						#text("INSTITUTION DETAILS",weight: "bold", fill: black)
					])


					#v(8pt)#text("Name : ")
					#h(8pt)
					#v(8pt)#text("Code : ")
					
					#v(73pt)
						#box(
						width: 100%,
						height: 1.5cm,
						stroke: 1pt + black,
						inset: 4pt,
						[
						#align(center)[
							#text("[Institution Stamp]",  fill: gray.lighten(40%))
						]
						]
					)
					#v(8pt)
			])
		  ])

])

#pagebreak()


				#grid(
				  columns: (6fr),
				  gutter: 4pt,
				  align: left,
				  inset: 4pt,
				  fill: rgb(185, 221, 235),
					  [
						#text(size: 8.5pt, weight: "bold"," CENTRAL KYC REGISTRY | Instructions / Check list / Guidelines for filling Legal Entity / Other than Individuals KYC Application Form ")
					  ]
				)

				#h(4pt)#text("A", fill: black) #h(8pt) #text("Clarification / Guidelines on filling 'Entity Details Section' ", fill: black)#v(3pt)
				    #h(8pt)#text(" 1", fill: black) #h(8pt) #text("Entity Constitution Type", fill: black)#v(2pt)
                   
					#grid(
						  columns: (6fr, 6fr),
						  gutter: 4pt,
						  align: left,
						  inset: 4pt,
						 	  [
							  #grid(
								 columns: (6fr),
								 gutter: 6pt,
								  [
									#h(8pt)#text("   A - Sole Proprietorship") #linebreak()
									#h(8pt)#text("   B - Partnership Firm")#linebreak()
									#h(8pt)#text("   C - HUF")#linebreak()
									#h(8pt)#text("   D - Private Limited Company")#linebreak()
									#h(8pt)#text("   E - Public Limited Company")#linebreak()
									#h(8pt)#text("   F - Society")#linebreak()
									#h(8pt)#text("   G - Association of Person(AOP)/Body of Individuals (BOI)")#linebreak()
									#h(8pt)#text("   H - Trust")#linebreak()
									#h(8pt)#text("   I - Liquidator")#linebreak()
									#h(8pt)#text("   J - Limited Liability Partnership")
								  ]
								  )
							  ],
							    [
								#grid(
								 columns: (6fr),
								 gutter: 6pt,
								  [
									#h(8pt)#text(" K - Artificial Liability Partnership")#linebreak()
									#h(8pt)#text(" L - Public Sector Bank")#linebreak()
									#h(8pt)#text(" M - Central/State Government Department or Agency")#linebreak()
									#h(8pt)#text(" N - Section 8 Companies()Companies Act,2013")#linebreak()
									#h(8pt)#text(" O - Artificial Jurisdical Person")#linebreak()
									#h(8pt)#text(" P - Internation Organisation or Agency/Foreign Embassy or Consular Office etc.")#linebreak()
									#h(8pt)#text(" Q - Not Categorized")#linebreak()
									#h(8pt)#text(" R - Other")#linebreak()
									#h(8pt)#text(" S - Foreign Portfolio Investors")
								  ]
								  )
							  ]
						)

				   #h(8pt)#text(" 2", fill: black) #h(8pt) #text("In case of Companies or partnership, PAN of the entity is mandatory. In case of other entities, FORM 60 may be obtained if PAN is not available.", fill: black)

				#h(4pt)#text("B", fill: black) #h(8pt) #text("Clarification / Guidelines for filling 'Proof of Identity[Pol]' section ", fill: black)#v(3pt)
					#h(8pt)#text(" 1", fill: black) #h(8pt) #text("Activity Proof - 1 and Activity Proof - 2 are applicable for accounts in case of proprietorship firms. Please refer to relevant instructions issued by the Reserve Bank of India in this regard.", fill: black)#v(2pt)
					#h(8pt)#text(" 2", fill: black) #h(8pt) #text("Please refer to the relevant instructions issued by the regulator regarding applicable documents for the legal entity.", fill: black)#v(2pt)
					#h(8pt)#text(" 3", fill: black) #h(8pt) #text("Certified copy of document or equivalent e-document or OVD obtained through Digital KYC process to be submitted.", fill: black)#v(2pt)
					#h(8pt)#text(" 4", fill: black) #h(8pt) #text("'Equivalent e-document' means an electronic equivalent of a document, issued by the issuing authority of such document with its valid digital signature including
documents issued to the digital locker account of the client as per rule 9 of the Information Technology (Preservation and Retention of Information by Intermediaries Providing Digital Locker Facilities) Rules, 2016.", fill: black)#v(2pt)
					#h(8pt)#text(" 5", fill: black) #h(8pt) #text("'Digital KYC process' has to be carried out as stipulated in the PML Rules, 2005.", fill: black)#v(2pt)
					#h(8pt)#text(" 6", fill: black) #h(8pt) #text("KYC requirements for Foreign Portfolio Investors (FPIs) will be as specified by the concerned regulator from time to time.", fill: black)#v(2pt)
					
				#h(4pt)#text("C", fill: black) #h(8pt) #text("Clarification / Guidelines for filling 'Proof of Address [POA] section", fill: black)#v(3pt)
				   #h(8pt)#text(" 1", fill: black) #h(8pt) #text("State / U.T Code and Pin / Post Code will not be mandatory for Overseas addresses.", fill: black)#v(2pt)
				   #h(8pt)#text(" 2", fill: black) #h(8pt) #text("Certified copy of document or equivalent e-document to be submitted.", fill: black)#v(2pt)

				#h(4pt)#text("D", fill: black) #h(8pt) #text("Clarification / Guidelines for filling 'Contact Details' section", fill: black)#v(3pt)
				   #h(8pt)#text(" 1", fill: black) #h(8pt) #text("Please mention two-digit country code and 10 digit mobile number (e.g. for Indian mobile number mention 91-9999999999).", fill: black)#v(2pt)   
				   #h(8pt)#text(" 2", fill: black) #h(8pt) #text("Do not add '0' in the beginning of Mobile number.", fill: black)#v(2pt)   

				#h(4pt)#text("E", fill: black) #h(8pt) #text("Clarification / Guidelines for filling 'Related Person Details' section", fill: black)#v(3pt)
				   #h(8pt)#text(" 1", fill: black) #h(8pt) #text("Personal Details", fill: black)#v(2pt)
				   #h(8pt)#text(" ", fill: black) #h(8pt) #text(" • The name should match the name as mentioned in the Proof of Identity submitted failing which the application is liable to be rejected. ", fill: black)#v(2pt)
				   #h(8pt)#text(" 2", fill: black) #h(8pt) #text("Proof of Address [POA]", fill: black)#v(2pt)
				   #h(8pt)#text(" ", fill: black) #h(8pt) #text(" • PoA to be submitted only if the submitted Pol does not have an address or address as per Pol is invalid or not in force. ", fill: black)#v(2pt)
				   #h(8pt)#text(" ", fill: black) #h(8pt) #text(" • State / U.T Code and Pin / Post Code will not be mandatory for Overseas addresses. ", fill: black)#v(2pt)
				   #h(8pt)#text(" ", fill: black) #h(8pt) #text(" • In case of deemed POA such as utility bill, the document need not be uploaded on CKYCR ", fill: black)#v(2pt)
				   #h(8pt)#text(" ", fill: black) #h(8pt) #text(" • REs may use the Self Declaration check box where Aadhaar authentication has been carried out successfully for a client and client wants to provide a current address, different from the address as per the identity information available in the Central Identities Data Repository. ", fill: black)#v(2pt)
				#h(8pt)#text(" 3", fill: black) #h(8pt) #text("If KYC number of Related Person is available, no other details except 'Person Type' and 'Name of the Related Person' are required.", fill: black)#v(2pt)
				#h(8pt)#text(" 4", fill: black) #h(8pt) #text("Regulated Entity (RE) shall redact (first 8 digits) of the Aadhaar number from Aadhaar related data and documents such as proof of possession of Aadhaar, while uploading on CKYCR.", fill: black)#v(2pt)

#h(4pt)#text("F", fill: black) #h(8pt) #text("Provision for capturing signature of multiple authorised persons is to be made by the RE. ", fill: black)#v(3pt)
				

#pagebreak()

 //state 


		// Define Colors
		#let header-color = rgb("#5B9BD5") // Blue heading background
		#let header-text-color = white


	#grid(
				  columns: (6fr),
				  gutter: 4pt,
				  align: left,
				  inset: 4pt,
				  fill: rgb(185, 221, 235),
					  [
						#text( " List of two digit state / U.T. codes as per Indian Motor Vehicle Act, 1988")
					  ]
		)


			#let header-color = rgb("#B7D4E7") // Header background color
			#let header-text-color = black
			#let row-bg-light = rgb("#F5F5F5") // Alternate row light gray
			#let row-bg-white = white           // White row background
			#let gap = 1pt                       // Gap between group pairs
			#let txtSize = 6.5pt  
			#let bgColor = 5pt  
			#let bgStColor = 5pt  
			#let bgCntColor = 4.7pt  

	#table(
		columns: (1.5fr, 0.6fr, gap, 1.5fr, 0.6fr, gap, 1.5fr, 0.6fr),
		stroke: none,
		align: center,

		// ===== HEADER ROW =====
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize,  fill: header-text-color)[State / U.T.]]
		],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize,  fill: header-text-color)[Code]]
		],
		[],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize,  fill: header-text-color)[State / U.T.]]
		],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize,  fill: header-text-color)[Code]]
		],
		[],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize,  fill: header-text-color)[State / U.T.]]
		],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize,  fill: header-text-color)[Code]]
		],

		// ===== DATA ROWS =====
		// Row 1
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Andaman & Nicobar]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[AN]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Himachal Pradesh]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[HP]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Pondicherry]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[PY]]],

		// Row 2
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Andhra Pradesh]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[AP]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Jammu & Kashmir]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[JK]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Punjab]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[PB]]],

		// Row 3
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Arunachal Pradesh]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[AR]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Jharkhand]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[JH]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Rajasthan]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[RJ]]],

		// Row 4
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Assam]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[AS]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Karnataka]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[KA]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Sikkim]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[SK]]],

		// Row 5
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Bihar]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[BR]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Kerala]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[KL]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Tamil Nadu]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[TN]]],

		// Row 6
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Chandigarh]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[CH]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Lakshadweep]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[LD]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Telangana]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[TS]]],

		// Row 7
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Chhattisgarh]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[CG]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Madhya Pradesh]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[MP]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Tripura]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[TR]]],

		// Row 8
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Dadra & Nagar Haveli and Daman & Diu]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[DD]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Maharashtra]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[MH]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Uttar Pradesh]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[UP]]],

		// Row 9
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Delhi]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[DL]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Manipur]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[MN]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Uttarakhand]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[UA]]],

		// Row 10
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Goa]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[GA]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Meghalaya]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[ML]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[West Bengal]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[WB]]],

		// Row 11
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Gujarat]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[GJ]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Mizoram]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[MZ]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[Other]]], [#box(fill: row-bg-light, width: 100%, height: bgStColor)[#text(size: txtSize)[XX]]],

		// Row 12
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Haryana]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[HR]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Nagaland]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[NL]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Odisha]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[OR]]]
     )


   //Cuntery

	#grid(
				  columns: (6fr),
				  gutter: 4pt,
				  align: left,
				  inset: 4pt,
				  fill: rgb(185, 221, 235),
					  [
						#text( )[List of ISO 3166 two digit Country Code]
					  ]
		)



			#let header-color = rgb("#B7D4E7") // Header background color
			#let header-text-color = black
			#let row-bg-light = rgb("#F5F5F5") // Alternate row light gray
			#let row-bg-white = white           // White row background
			#let gap = 1pt                     // Gap between group pairs

#table(
		columns: (1.5fr, 0.6fr, gap, 1.5fr, 0.6fr, gap, 1.5fr, 0.6fr, gap, 1.5fr, 0.6fr ),
		stroke: none,
		align: center,

		// ===== HEADER ROW =====
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize,  fill: header-text-color)[Country]]
		],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize,  fill: header-text-color)[Code]]
		],
		[],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize,  fill: header-text-color)[Country]]
		],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize,  fill: header-text-color)[Code]]
		],
		[],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize,  fill: header-text-color)[Country]]
		],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize,  fill: header-text-color)[Code]]
		],
		[],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize,  fill: header-text-color)[Country]]
		],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize,  fill: header-text-color)[Code]]
		],

		// ===== DATA ROWS =====
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Afghanistan]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[AF]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Åland Islands]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[AX]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Albania]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[AL]]],[],
	    [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Andorra]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[AD]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Algeria]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[DZ]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[American Samoa]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[AS]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Angola]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[AO]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Anguilla]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[AI]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Antarctica]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[AQ]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Antigua and Barbuda]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[AG]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Argentina]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[AR]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Armenia]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[AM]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Aruba]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[AW]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Australia]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[AU]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Austria]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[AT]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Azerbaijan]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[AZ]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Bahamas]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[BS]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Bahrain]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[BH]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Bangladesh]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[BD]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Barbados]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[BB]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Belarus]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[BY]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Belgium]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[BE]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Belize]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[BZ]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Benin]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[BJ]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Bermuda]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[BM]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Bhutan]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[BT]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Bolivia]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[BO]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Bonaire, Sint Eustatius and Saba]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[BQ]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Bosnia and Herzegovina]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[BA]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Botswana]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[BW]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Bouvet Island]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[BV]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Brazil]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[BR]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[British Indian Ocean Territory]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[IO]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Brunei Darussalam]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[BN]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Bulgaria]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[BG]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Burkina Faso]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[BF]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Burundi]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[BI]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Cabo Verde]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[CV]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Cambodia]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[KH]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Cameroon]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[CM]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Canada]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[CA]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Cayman Islands]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[KY]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Central African Republic]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[CF]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Chad]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[TD]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Chile]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[CL]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[China]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[CN]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Christmas Island]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[CX]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Cocos (Keeling) Islands]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[CC]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Colombia]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[CO]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Comoros]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[KM]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Congo (Democratic Republic)]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[CD]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Congo]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[CG]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Cook Islands]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[CK]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Costa Rica]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[CR]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Côte d'Ivoire]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[CI]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Croatia]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[HR]]],
		
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Cuba]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[CU]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Curaçao]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[CW]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Cyprus]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[CY]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Czechia]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[CZ]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Denmark]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[DK]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Djibouti]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[DJ]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Dominica]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[DM]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Dominican Republic]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[DO]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Ecuador]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[EC]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Egypt]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[EG]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[El Salvador]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[SV]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Equatorial Guinea]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[GQ]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Eritrea]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[ER]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Estonia]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[EE]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Eswatini]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[SZ]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Ethiopia]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[ET]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Falkland Islands (Malvinas)]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[FK]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Faroe Islands]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[FO]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Fiji]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[FJ]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Finland]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[FI]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[France]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[FR]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[French Guiana]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[GF]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[French Polynesia]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[PF]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[French Southern Territories]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[TF]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Gabon]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[GA]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Gambia]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[GM]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Georgia]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[GE]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Germany]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[DE]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Ghana]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[GH]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Gibraltar]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[GI]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Greece]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[GR]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Greenland]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[GL]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Grenada]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[GD]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Guadeloupe]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[GP]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Guam]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[GU]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Guatemala]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[GT]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Guernsey]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[GG]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Guinea]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[GN]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Guinea-Bissau]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[GW]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Guyana]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[GY]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Haiti]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[HT]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Heard Island and McDonald Islands]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[HM]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Holy See]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[VA]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Honduras]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[HN]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Hong Kong]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[HK]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Hungary]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[HU]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Iceland]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[IS]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[India]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[IN]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Indonesia]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[ID]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Iran]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[IR]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Iraq]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[IQ]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Ireland]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[IE]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Isle of Man]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[IM]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Israel]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[IL]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Italy]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[IT]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Jamaica]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[JM]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Japan]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[JP]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Jersey]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[JE]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Jordan]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[JO]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Kazakhstan]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[KZ]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Kenya]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[KE]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Kiribati]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[KI]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Korea (North)]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[KP]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Korea (South)]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[KR]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Kuwait]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[KW]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Kyrgyzstan]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[KG]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Lao People's Democratic Republic]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[LA]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Latvia]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[LV]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Lebanon]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[LB]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Lesotho]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[LS]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Liberia]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[LR]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Libya]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[LY]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Liechtenstein]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[LI]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Lithuania]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[LT]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Luxembourg]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[LU]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Macao]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[MO]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Madagascar]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[MG]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Malawi]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[MW]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Malaysia]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[MY]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Maldives]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[MV]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Mali]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[ML]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Malta]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[MT]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Marshall Islands]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[MH]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Martinique]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[MQ]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Mauritania]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[MR]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Mauritius]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[MU]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Mayotte]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[YT]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Mexico]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[MX]]],


		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Micronesia (Federated States of)]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[FM]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Moldova (Republic of)]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[MD]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Monaco]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[MC]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Mongolia]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[MN]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Montenegro]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[ME]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Montserrat]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[MS]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Morocco]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[MA]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Mozambique]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[MZ]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Myanmar]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[MM]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Namibia]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[NA]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Nauru]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[NR]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Nepal]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[NP]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Netherlands]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[NL]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[New Caledonia]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[NC]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[New Zealand]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[NZ]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Nicaragua]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[NI]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Niger]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[NE]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Nigeria]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[NG]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Niue]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[NU]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Norfolk Island]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[NF]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Northern Mariana Islands]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[MP]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Norway]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[NO]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Oman]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[OM]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Pakistan]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[PK]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Palau]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[PW]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Palestine, State of]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[PS]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Panama]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[PA]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Papua New Guinea]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[PG]]], 

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Paraguay]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[PY]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Peru]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[PE]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Philippines]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[PH]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Pitcairn]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[PN]]], 

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Poland]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[PL]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Portugal]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[PT]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Puerto Rico]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[PR]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Qatar]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[QA]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Réunion]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[RE]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Romania]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[RO]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Russian Federation]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[RU]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Rwanda]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[RW]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Saint Barthélemy]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[BL]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Saint Helena, Ascension and Tristan da Cunha]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[SH]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Saint Kitts and Nevis]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[KN]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Saint Lucia]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[LC]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Saint Martin (French part)]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[MF]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Saint Pierre and Miquelon]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[PM]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Saint Vincent and the Grenadines]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[VC]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Samoa]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[WS]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[San Marino]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[SM]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Sao Tome and Principe]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[ST]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Saudi Arabia]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[SA]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Senegal]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[SN]]], 

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Serbia]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[RS]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Seychelles]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[SC]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Sierra Leone]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[SL]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Singapore]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[SG]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Sint Maarten (Dutch part)]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[SX]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Slovakia]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[SK]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Slovenia]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[SI]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Solomon Islands]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[SB]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Somalia]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[SO]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[South Africa]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[ZA]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[South Georgia and the South Sandwich Islands]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[GS]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[South Sudan]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[SS]]], 

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Spain]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[ES]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Sri Lanka]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[LK]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Sudan]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[SD]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Suriname]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[SR]]], 

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Svalbard and Jan Mayen]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[SJ]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Sweden]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[SE]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Switzerland]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[CH]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Syrian Arab Republic]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[SY]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Taiwan, Province of China]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[TW]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Tajikistan]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[TJ]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Tanzania, United Republic of]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[TZ]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Thailand]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[TH]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Timor-Leste]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[TL]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Togo]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[TG]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Tokelau]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[TK]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Tonga]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[TO]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Trinidad and Tobago]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[TT]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Tunisia]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[TN]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Turkey]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[TR]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Turkmenistan]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[TM]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Tuvalu]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[TV]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Uganda]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[UG]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Ukraine]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[UA]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[United Arab Emirates]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[AE]]],

		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[United Kingdom of Great Britain and Northern Ireland]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[GB]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[United States of America]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[US]]],[],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Uruguay]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[UY]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Uzbekistan]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[UZ]]], 

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Vanuatu]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[VU]]],[],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Venezuela (Bolivarian Republic of)]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[VE]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Viet Nam]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[VN]]], [],
		[#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[Western Sahara]]], [#box(fill: row-bg-white, width: 100%)[#text(size: txtSize)[EH]]],

		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Yemen]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[YE]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Zambia]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[ZM]]], [],
		[#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[Zimbabwe]]], [#box(fill: row-bg-light, width: 100%, height: bgCntColor)[#text(size: txtSize)[ZW]]], []

)






