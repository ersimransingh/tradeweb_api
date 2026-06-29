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
	  	#text(size: 8.5pt, weight: "bold", "CENTRAL KYC REGISTRY | Know Your Customer(KYC) Application Form | Individual")
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
            #text("B) Tick '*' wherever applicable."),
            #linebreak()
            #text("C) Please fill the form in English and in BLOCK letters."),
			#linebreak()
            #text("D) Please fill the date in DD-MM-YYYY format."),
			#linebreak()
            #text("E) For particular section update, please tick() in the box section number and strike off the sections not required to be updated.")

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
					#linebreak()
					#text("J) The 'OTP based E-KYC' check box is to be checked for accounts opened using OTP based E-KYC in non-face to face mode") 
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
					
					#h(8pt) #text("KYC Number*")
					
					#h(8pt) #text("Account Type*")
				 ],
				 [
					 #text((if ReKycDetails.ApplicationType == "New" { "☑" } else { "☐" }) + " New")
				     #text((if ReKycDetails.ApplicationType == "Update" { "☑" } else { "☐" }) + " Update")
					
					 #h(7pt) #text(ReKycDetails.KYCNumber) #text("   Mandatory for KYC update request")
					
					 #h(7pt) #text((if ReKycDetails.AccountType == "Normal" { "☑" } else { "☐" }) + " Normal ")
							 #text((if ReKycDetails.AccountType == "Minor" { " ☑" } else { " ☐" }) + " Minor ")
							 #text((if ReKycDetails.AccountType == "AadharOTP" { " ☑" } else { " ☐" }) + " Aadhar OTP based E-KYC ")
							 #text(" (in not-face to face mode)")
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
	  	#text(size: 8.5pt, weight: "bold", "1. PERSONAL DETAILS (Please refer instruction A at the end)")
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
			#h(8pt)#text("Name* (Same as ID proof) :")
	 
			#h(8pt)#text("Maiden Name (If any) :")
	
			#h(8pt)#text("Father/Spouse Name * :")
	
			#h(8pt)#text("Mother Name : ")
   
			#h(8pt) #text("Date of Birth* :")
		  
			#h(8pt) #text("Gender* : ")
			
			#h(8pt) #text("PAN* : ")

		],
		 grid[
			 #text(ReKycDetails.ClientsNamePrefix + " " + ReKycDetails.ClientFullName) #v(0pt)
			 #text(ReKycDetails.MaidenNamePrefix + " " + ReKycDetails.MaidenName)  #v(0pt)
			 #text(ReKycDetails.FatherSpouseNamePrefix + " " + ReKycDetails.FatherSpouseName) #v(0pt) 
			 #text(ReKycDetails.MotherNamePrefix + " " + ReKycDetails.MotherName ) #v(0pt) 
     
		  #let dob = if "DateOfBirth" in ReKycDetails and str(ReKycDetails.DateOfBirth).len() == 8 {
			  let d = str(ReKycDetails.DateOfBirth)
			  d.slice(6,8) + " / " + d.slice(4,6) + " / " + d.slice(0,4)
			} else { "--" }
		   #text(dob) #v(0pt)
	
		   #text((if ReKycDetails.Gender == "M" { "☑" } else { "☐" }) + " Male")
				 #text((if ReKycDetails.Gender == "F" { "☑" } else { "☐" }) + " Female")
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
			#text(size: 8.5pt, weight: "bold", "2.PROOF OF IDENTITY AND ADDRESS * (Please refer instruction B at the end)")
		 ]
	)
	#text("I. Certified copy of OVD or equivalent e-document of OVD or OVD obtained through digital KYC process needs to be submitted (anyone of the following OVDs)")
		
	#grid(
		columns: (4fr, 5fr, 3fr),
		gutter: 5pt,
		align: left,
		inset: 5pt,
		grid[
		
				#h(8pt) #text("  A - Passport Number : ")
			
				#h(8pt) #text("  B - Voter ID : ")
			
				#h(8pt) #text("  C - Driving License : ")
			
				#h(8pt) #text("  D - NAREGA Job Card : ")
			
				#h(8pt) #text("  E - National Population Register Letter : ")
				
				#h(8pt) #text("  F - Proof of Possession of Aadhaar : ")
				
				#h(8pt) #text("II  E-KYC Authentication : ")
				
				#h(8pt) #text("III Offline verification of Aadhaar : ")
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
		
				#h(8pt) #text("   A - Passport Number : ")
			
				#h(8pt) #text("   B - Voter ID : ")
			
				#h(8pt) #text("   C - Driving License : ")
			
				#h(8pt) #text("   D - NAREGA Job Card : ")
			
				#h(8pt) #text("   E - National Population Register Letter : ")
				
				#h(8pt) #text("   F - Proof of Possession of Aadhaar : ")
				
				#h(8pt) #text("II  E-KYC Authentication : ")
				
				#h(8pt) #text("III Offline verification of Aadhaar : ")
				
				#h(8pt) #text("IV  Deemed Proof of Address-Document Type code : ")
				
				#h(8pt) #text("V   Self Declaration : ")
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
					#text(size: 8.5pt, weight: "bold"," 4. CONTACT DETAILS (All communications will be sent to Mobile number/Email-ID Provided) (Please refer instruction C at the end)")
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
						#text(size: 8.5pt, weight: "bold"," CENTRAL KYC REGISTRY | Instructions / Check list / Guidelines for filling Individual KYC Application Form")
					  ]
				)

				#h(4pt)#text("A",weight: "bold", fill: black) #h(8pt) #text("Clarification / Guidelines on filling 'Personal Detail Section' ",weight: "bold", fill: black)#v(3pt)
				    #h(8pt)#text("1",weight: "bold", fill: black) #h(8pt) #text("Name: The name should be match the name as mentioned in the proof of identity submitted failing which the application is liable to be rejected.", fill: black)#v(2pt)
                    #h(8pt)#text("2",weight: "bold", fill: black) #h(8pt) #text("One of the following is mandatory : Mother name, Spouse's name, Father name.", fill: black)

				#h(4pt)#text("B",weight: "bold", fill: black) #h(8pt) #text("Clarification / Guidelines on filling 'Current Address Detail Section' ",weight: "bold", fill: black)#v(3pt)
					#h(8pt)#text("1",weight: "bold", fill: black) #h(8pt) #text("In case of deemed PoA such as utility bill, etc. or self declaration, the document need not be uploaded on CKYCR", fill: black)#v(2pt)
					#h(8pt)#text("2",weight: "bold", fill: black) #h(8pt) #text("PoA to be submitted only if the submitted Pol does not have current address or address as per Pol is invalid or not in force.", fill: black)#v(2pt)
					#h(8pt)#text("3",weight: "bold", fill: black) #h(8pt) #text("State / U.T Cade and Pin / Post Code will not be mandatory for Overseas addresses.", fill: black)#v(2pt)
					#h(8pt)#text("4",weight: "bold", fill: black) #h(8pt) #text("In Section 2, one of I, Il, and II| is to be selected. In case of online E-KYC authentication, II is to be selected.", fill: black)#v(2pt)
					#h(8pt)#text("5",weight: "bold", fill: black) #h(8pt) #text("In Section 3, one of I, Il, Ill and IV is to be selected. In case of online E-KYC authentication, II is to be selected.", fill: black)#v(2pt)
					#h(8pt)#text("6",weight: "bold", fill: black) #h(8pt) #text("List of documents for 'Deemed Proof of Address’:", fill: black)#v(2pt)
					#h(8pt)#box(
							inset: 2pt,
							fill: rgb("#d3d3d3"), 
							[
								#text("Document Code Description",weight: "bold", fill: black)
							])
							#v(2pt)

							#h(16pt)#text("01",weight: "bold", fill: black) #h(8pt) #text("Utility bill which is not more than two months old of any service provider (electricity, telephone, post-paid mobile phone, piped gas, water bill).", fill: black)#v(2pt)		
							#h(16pt)#text("02",weight: "bold", fill: black) #h(8pt) #text("Property or Municipal tax receipt.", fill: black)#v(2pt)
							#h(16pt)#text("03",weight: "bold", fill: black) #h(8pt) #text("Pension or family pension payment orders (PPOs) issued to retired employees by Government Departments or Public Sector Undertakings, if they contain the", fill: black)#v(2pt)
							#h(32pt)#text("address", fill: black) #v(2pt)
							#h(16pt)#text("04",weight: "bold", fill: black) #h(8pt) #text("Letter of allotment of accommodation from employer issued by State Government or Central Government Departments, statutory or ", fill: black)#v(2pt)
							#h(32pt)#text("regulatory bodies, public sector undertakings, scheduled commercial banks, financial institutions and listed companies and leave and licence", fill: black) #v(2pt)
							#h(32pt)#text("agreements with such employers allotting official accommodation.", fill: black) #v(2pt)

					#h(8pt)#text("7",weight: "bold", fill: black) #h(8pt) #text("Regulated Entity (RE) shall redact (first 8 digits) of the Aadhaar number from Aadhaar related data and documents such as proof of possession of Aadhaar, while", fill: black)#v(2pt)
					#h(22pt)#text("uploading on CKYCR", fill: black) #v(2pt)
				   	#h(8pt)#text("8",weight: "bold", fill: black) #h(8pt) #text("Equivalent e-document means an electronic equivalent of a dacument, issued by the issuing authority of such document with its valid digital signature including", fill: black)#v(2pt)
					#h(22pt)#text("documents issued to the digital locker account of the client as per rule 9 of the Information Technology Preservation and Retention of Information by", fill: black) #v(2pt)
					#h(22pt)#text("Intermediaries Providing Digital Locker Facilities Rules, 2016.", fill: black) #v(2pt)
					#h(8pt)#text("9",weight: "bold", fill: black) #h(8pt) #text("Digital KYC process has to be carried out as stipulated in the PML Rules, 2005.", fill: black)#v(2pt)
					#h(8pt)#text("10",weight: "bold", fill: black) #h(5pt) #text("REs may use the Self Declaration check box where Aadhaar authentication has been carried out successfully for a client and client wants to provide a current address,", fill: black)#v(2pt)
					#h(22pt)#text("different from the address as per the identity information available in the Central Identities Data Repository", fill: black) #v(2pt)

				#h(4pt)#text("C",weight: "bold", fill: black) #h(8pt) #text("Clarification / Guidelines on filling ‘Contact details’ section",weight: "bold", fill: black)#v(3pt)
				   #h(8pt)#text("1",weight: "bold", fill: black) #h(8pt) #text("Please mention two- digit country code and 10 digit mobile number e.g. for Indian mobile number mention 91-9999999999.", fill: black)#v(2pt)
				   #h(8pt)#text("2",weight: "bold", fill: black) #h(8pt) #text("Do not add '0' in the beginning of Mobile number.", fill: black)#v(2pt)

				#h(4pt)#text("D",weight: "bold", fill: black) #h(8pt) #text("Clarification / Guidelines on filling ‘Related Person details' section",weight: "bold", fill: black)#v(3pt)
				   #h(8pt)#text("1",weight: "bold", fill: black) #h(8pt) #text("Provide KYC number of related person, if available.", fill: black)#v(2pt)   

				#h(4pt)#text("E",weight: "bold", fill: black) #h(8pt) #text("Clarification on Minor",weight: "bold", fill: black)#v(3pt)
				   #h(8pt)#text("1",weight: "bold", fill: black) #h(8pt) #text("Guardian details are optional for minors above 10 years of age for opening of bank account only", fill: black)#v(2pt)
				   #h(8pt)#text("2",weight: "bold", fill: black) #h(8pt) #text("However, in case guardian details are available for minor above 10 years of age, the same (or CKYCR number of guardian) is to be uploaded.", fill: black)#v(2pt)

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
						#text(size: 8.5pt, weight: "bold"," List of two digit state / U.T. codes as per Indian Motor Vehicle Act, 1988")
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
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize, weight: "bold", fill: header-text-color)[State / U.T.]]
		],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize, weight: "bold", fill: header-text-color)[Code]]
		],
		[],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize, weight: "bold", fill: header-text-color)[State / U.T.]]
		],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize, weight: "bold", fill: header-text-color)[Code]]
		],
		[],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize, weight: "bold", fill: header-text-color)[State / U.T.]]
		],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize, weight: "bold", fill: header-text-color)[Code]]
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
						#text(size: 8.5pt, weight: "bold",)[List of ISO 3166 two digit Country Code]
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
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize, weight: "bold", fill: header-text-color)[Country]]
		],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize, weight: "bold", fill: header-text-color)[Code]]
		],
		[],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize, weight: "bold", fill: header-text-color)[Country]]
		],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize, weight: "bold", fill: header-text-color)[Code]]
		],
		[],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize, weight: "bold", fill: header-text-color)[Country]]
		],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize, weight: "bold", fill: header-text-color)[Code]]
		],
		[],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize, weight: "bold", fill: header-text-color)[Country]]
		],
		[
			#box(fill: header-color, width: 100%, height: bgColor)[#text(size: txtSize, weight: "bold", fill: header-text-color)[Code]]
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






