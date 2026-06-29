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
#let photoPath = ""
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
	  fill: rgb(254, 245, 197),
      [
	  	#text(size: 8pt, weight: "bold", "Annexure A1")
	  ]
	)
	 #v(-10pt)
	#grid(
      columns: (6fr),
      gutter: 4pt,
	  align: left,
	  inset: 4pt,
	  fill: rgb(149, 206, 216),
      [
	  	#text(size: 8.5pt, weight: "bold", "CENTRAL KYC REGISTRY | Know Your Customer(KYC) Application Form | Related Person")
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
            #text("A) Fields markded with '*' are mandatory fields."),
            #linebreak()
            #text("B) Tick '✔' wherever applicable."),
            #linebreak()
            #text("C) Please fill the form in English and in BLOCK letters."),
			#linebreak()
            #text("D) Please fill the date in DD-MM-YYYY format."),
			#linebreak()
            #text("E) For particular section update, please tick() in the box section number and strike off the sections not required to be updated.")
//#sym.check
//#sym.checkmark
//#sym.heavy-check-mark

//#text(20pt, fill: green)[#sym.check]
          ]
        )
      ],
      // === Right side: Logo + App Info
      [
              #grid(
                columns: (6fr),
                gutter: 6pt,
                [
                    #text("F) Please read section wise details guidelines/instruction at the end."),
					#linebreak()
					#text("G) List of State / U.T code as per Indian Motor Vehicle Act, 1988 is available at the end."),
					#linebreak()
					#text("H) List of two character ISO 3166 country codes is available at the end."),
					#linebreak()
					#text("I) KYC number of applicant is mandatory for update application."),
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
				columns: (3fr, 2fr, 4fr, 3fr),
				  [ 
					#h(8pt) #text("For office use only", weight: "bold")
					
					#h(8pt) #text("(To be filled by financial institution)")
				 ],
				 [
					#h(8pt) #text("Application Type*")
					
					#h(8pt) #text("KYC Number*")
					
				 ],
				 [
					#text((if ReKycDetails.ApplicationType == "New" { "☑" } else { "☐" }) + " New ")
					#text((if ReKycDetails.ApplicationType == "Update" { "☑" } else { "☐" }) + " Update ")
					#text((if ReKycDetails.ApplicationType == "Delete" { "☑" } else { "☐" }) + " Delete")
					
					#h(7pt) #text(ReKycDetails.KYCNumber) 
				 ],
				 [
					#h(8pt) #text(" ")
					
					#h(8pt) #text(" ") #text("Mandatory for KYC update request")					
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
	  	#text(size: 8.5pt, weight: "bold", "1.  DETAILS OF RELATED PERSON(Please refer instruction D & E at the end)")
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
				     #text((if ReKycDetails.DetailRelPerson == "Addition" { "☑" } else { "☐" }) + " Addition of Related Person ")
					 #text((if ReKycDetails.DetailRelPerson == "Deletion" { " ☑" } else { " ☐" }) + " Deletion of Related Person ")
					 #text((if ReKycDetails.DetailRelPerson == "Updation" { " ☑" } else { " ☐" }) + " Updation ")
					 #text(" KYC Number of Releted Person (if available*)")
				 ]
			)
          ]
        )	
	#v(2pt)
	
	 #grid(
      columns: (4fr, 8fr),
      gutter: 2pt,
	  align: left,
	  inset: 0pt,
	  stroke: none, // for adding border
	  grid[
			#h(8pt)#text("Related Person Type* :")
			
			#h(8pt)#text("Name* (Same as ID proof) :")
			
			#h(8pt)#text("  ")
	 
			#h(8pt)#text("Maiden Name (If any) :")
	
			#h(8pt)#text("Father/Spouse Name * :")
	
			#h(8pt)#text("Mother Name : ")
   
			#h(8pt)#text("Date of Birth* :")
		  
			#h(8pt)#text("Gender* : ")
			
			#h(8pt)#text("PAN* : ")
		],
		 grid[
			 #text((if ReKycDetails.RelatedPersonType == "GrdMinor" { "☑" } else { "☐" }) + " Guardian of Minor ")
			 #text((if ReKycDetails.RelatedPersonType == "Assignee" { "☑" } else { "☐" }) + " Assignee ")
			 #text((if ReKycDetails.RelatedPersonType == "AuthRepresntative" { "☑" } else { "☐" }) + " Authorized Representative") #v(0pt)
			 
			 #text(ReKycDetails.ClientsNamePrefix + " " + ReKycDetails.ClientFullName) #v(-2.5pt)
			 #text(size: 6.5pt,"  (If KYC number and name are provided, below details are optional) ")#v(0pt)
			 #text(ReKycDetails.MaidenNamePrefix + " " + ReKycDetails.MaidenName)  #v(0pt)
			 #text(ReKycDetails.FatherSpouseNamePrefix + " " + ReKycDetails.FatherSpouseName) #v(0pt) 
			 #text(ReKycDetails.MotherNamePrefix + " " + ReKycDetails.MotherName ) #v(0pt) 
     
		   #let dob = if "DateOfBirth" in ReKycDetails and str(ReKycDetails.DateOfBirth).len() == 8 {
			  let d = str(ReKycDetails.DateOfBirth)
			  d.slice(6,8) + "/" + d.slice(4,6) + "/" + d.slice(0,4)
			} else { "--" }
		   #text(dob) #v(0pt)
	
		   #text((if ReKycDetails.Gender == "M" { "☑" } else { "☐" }) + " Male ")
		   #text((if ReKycDetails.Gender == "F" { "☑" } else { "☐" }) + " Female ")
		   #text((if ReKycDetails.Gender == "T" { "☑" } else { "☐" }) + " Transgender") #v(0pt)
				
           #text(ReKycDetails.PAN) #v(0pt)

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
			#text(size: 8.5pt, weight: "bold", "2.PROOF OF IDENTITY AND ADDRESS* ")
		 ]
	)
	#text("I. Certified copy of OVD or equivalent e-document of OVD or OVD obtained through digital KYC process needs to be submitted (anyone of the following OVDs)")
		
	#grid(
		columns: (4fr, 5fr, 3fr),
		gutter: 5pt,
		align: left,
		inset: 5pt,
		grid[
		
				#h(8pt) #text("A - Passport Number : ")
			
				#h(8pt) #text("B - Voter ID : ")
			
				#h(8pt) #text("C - Driving License : ")
			
				#h(8pt) #text("D - NAREGA Job Card : ")
			
				#h(8pt) #text("E - National Population Register Letter : ")
				
				#h(8pt) #text("F - Proof of Possession of Aadhaar : ")
				
				#h(8pt) #text("E-KYC Authentication : ")
				
				#h(8pt) #text("Offline verification of Aadhaar : ")
			],
		grid[
					 #text(ReKycDetails.PassportNumber) #v(0pt)
			         #text(ReKycDetails.VoterID)  #v(0pt)
					 #text(ReKycDetails.DrivingLicence) #v(0pt)
			         #text(ReKycDetails.NAEGIAJobCard)  #v(0pt)
					 #text(ReKycDetails.NationalPopulationLetter) #v(0pt)
			         #text(ReKycDetails.PossessionAdhar)  #v(0pt)
					 #text(ReKycDetails.EKYCAuthentication) #v(0pt)
			         #text(ReKycDetails.OfflineVerificationAdhar)  #v(0pt)
			],
			
		grid[
			#align(center)[
              #text("Photo*",size: 8pt, weight: "bold", fill: rgb(185, 221, 235))
			]
		 #box(
			stroke: 1pt,
			width: 3cm,
			inset: 0pt,
			[
			  #block(spacing: 6pt)[
				// Passport Photo
				#box(
				  width: 3cm,
				  height: 3cm,
				  inset: 1pt,
				  stroke: 1pt,
				  [
					#align(center)[
					 // #text("Passport\nPhoto",size: 8pt, fill: gray.lighten(40%))
					 #if photoPath != "" {
					      image(photoPath, width: 100%, height: 100%)
					  }
					]
				  ]
				)		
			  ]
		   ]
		)
	  ]
	)

	#h(5pt)#text(size: 8.5pt, weight: "bold", "Address")
	
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
 
		   #h(8pt)#text("District* :") 
		   
		   #h(8pt) #text("Pin/Post Code* :")

		   #h(8pt) #text("State/U.T.Code* :")

		   #h(8pt) #text("ISO 3166 Country Code* :")
		],
		 grid[
			  #h(98pt) #text(ReKycDetails.CorrAddress1) 

			  #h(98pt) #text(ReKycDetails.CorrAddress2)

			  #h(98pt) #text(ReKycDetails.CorrAddress3)  
			 
			  #h(97pt) #text(ReKycDetails.CorrCity)  
			  
			  #h(96pt) #text(ReKycDetails.CorrDistrict) 
			  
			  #h(96pt) #text(ReKycDetails.CorrPincode)

			  #h(97pt) #text(ReKycDetails.CorrState)

			  #h(96pt)#text(ReKycDetails.CorrCountry)
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
			   #text(size: 8.5pt, weight: "bold", "3.CURRENT ADDRESS DETAILS * (Please refer instruction B at the end)")
			]
	)
	#text(if ReKycDetails.IsAddressSame == "Y" { "☑" } else { "☐" })#text(" Same as above mentioned address (In such cases address details as below need not be provided) ")
	
	#text("I. Certified copy of OVD or equivalent e-document of OVD or OVD obtained through digital KYC process needs to be submitted (anyone of the following OVDs)")
		
	#grid(
		columns: (4fr, 8fr),
		gutter: 5pt,
		align: left,
		inset: 5pt,
		grid[
		
				#h(8pt) #text("A - Passport Number : ")
			
				#h(8pt) #text("B - Voter ID : ")
			
				#h(8pt) #text("C - Driving License : ")
			
				#h(8pt) #text("D - NAREGA Job Card : ")
			
				#h(8pt) #text("E - National Population Register Letter : ")
				
				#h(8pt) #text("F - Proof of Possession of Aadhaar : ")
				
				#h(8pt) #text("E-KYC Authentication : ")
				
				#h(8pt) #text("Offline verification of Aadhaar : ")
				
				#h(8pt) #text("Deemed Proof of Address-Document Type code : ")
				
				#h(8pt) #text("Self Declaration : ")
			],
		grid[	
				 #text(ReKycDetails.PassportNumber) #v(0pt)
				 #text(ReKycDetails.VoterID)  #v(0pt)
				 #text(ReKycDetails.DrivingLicence) #v(0pt)
				 #text(ReKycDetails.NAEGIAJobCard)  #v(0pt)
				 #text(ReKycDetails.NationalPopulationLetter) #v(0pt)
				 #text(ReKycDetails.PossessionAdhar)  #v(0pt)
				 #text(ReKycDetails.EKYCAuthentication) #v(0pt)
				 #text(ReKycDetails.OfflineVerificationAdhar)  #v(0pt)	
			]
	)

	#h(5pt)#text(size: 8.5pt, weight: "bold", "Address")
	
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
 
		   #h(8pt)#text("District* :") 
		   
		   #h(8pt) #text("Pin/Post Code* :")

		   #h(8pt) #text("State/U.T.Code* :") 

		   #h(8pt) #text("ISO 3166 Country Code* :")
		],
		 grid[
			  #h(98pt) #text(ReKycDetails.CorrAddress1) 

			  #h(98pt) #text(ReKycDetails.CorrAddress2)

			  #h(98pt) #text(ReKycDetails.CorrAddress3)  

			  #h(97pt) #text(ReKycDetails.CorrCity)  

			  #h(96pt) #text(ReKycDetails.CorrDistrict) 
			 
			  #h(96pt) #text(ReKycDetails.CorrPincode)
			  
			  #h(97pt) #text(ReKycDetails.CorrState)

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
					#text(size: 8.5pt, weight: "bold"," 4. CONTACT DETAILS ")
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

		
			//Remarks


			#grid(
				  columns: (6fr),
				  gutter: 4pt,
				  align: left,
				  inset: 4pt,
				  fill: rgb(185, 221, 235),
					  [
						#text(size: 8.5pt, weight: "bold"," 5. Remarks (If any)")
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
						#text(size: 8.5pt, weight: "bold"," 6. Application Declaration")
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
					"I hereby declare that the details furnished above are true and correct to the best of my knowledge and belief and I undertake to inform you of any changes there in immediately. In case any of the above information is found to be false or future or misleading or misrepressing. Im aware that i may be held liable for it. I heared by declare that im not making this application for the purpose of contravention of any act, Rules, Regulations or any statute of legislation or any notification/direction issued by govermenta or any statutory authority time to time.
					 I heared by consent to receiving information from Central KYC registry through SMS/Email on the above registered number/email Address."
					)
				])

			],
				[
			
			#box(
			  inset: 4pt,
			   [
				#h(5pt)
					#h(5pt)#box(
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
					  #text(size: 8.5pt, weight: "bold"," 7. Attestation / For Office use only", fill: black)
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
		  #h(15pt)
			  //#text((if ReKycDetails.DocumentReceived == "DataReceivedUIDAI" { "☑" } else { "☐" }) + "E-KYC data received by UIDAI")
			  #text("☐  E-KYC data received by UIDAI")
		  #h(15pt)
			  //#text((if ReKycDetails.DocumentReceived == "VideoKYC" { "☑" } else { "☐" }) + "Video based KYC")
			  #text("☐  Video based KYC")
		  #v(2pt)
			  //#h(93pt)#text((if ReKycDetails.DocumentReceived == "DataReceivedOffline" { "☑" } else { "☐" }) + "Data received from Offline verification")
			  #h(93pt)#text("☐  Data received from Offline verification")
		  #h(8pt)
			  #text("☐  Digital KYC Process")
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
					  ]
				   )

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





#for item in Attachment {
	if item.base64Image !="" and item.ImageType !="Photo_Img" {
	    if item.base64Image == ".pdf" {
		   let filePath = "../Assets/" + item.ImageType + "_" + ReKycDetails.ClientCode + item.base64Image
		   let data = read(filePath, encoding: none)
		   pagebreak()
		   muchpdf(data)
	    }
	    else {
		  let filePath = "../Assets/" + item.ImageType + "_" + ReKycDetails.ClientCode + item.base64Image
		  pagebreak()
		  image(filePath, width: 100%)
	    } 
	}
}
