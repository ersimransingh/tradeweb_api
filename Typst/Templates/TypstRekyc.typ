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
#let nominee = ReKycDetails.NomineeDetails
#let bank = ReKycDetails.BankDetails
#let demat = ReKycDetails.DematDetails
#let Attachment = ReKycDetails.Attachments
#let clean(str) = str.trim().replace(regex("\s+"), " ")


// Page setup
#set page(
  width: 210mm,
  height: 430mm,
  margin: 5mm //(top: 1cm, bottom: 1cm, x: 1cm)
)
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
  stroke: 1pt,
  inset: 0pt,
      [
         #align(center)[
      		// Company Name centered
			#v(8pt)
			 #text(ReKycDetails.CompanyName.trim(), weight: "bold", size: 16pt)
			#v(2pt)     
			#text(clean(ReKycDetails.CompanyAddress), size: 11pt)
      ]

    #grid(
      columns: (4.6fr, 5.7fr, 1.6fr),
      gutter: 0pt,
	  inset: 5pt,
	  stroke: none,	  
      [
	  // === Left side: Instructions box
        #box(
          fill: gray.lighten(60%),		  
          inset: 2pt,
          [
            #text("Know Your Client (KYC)", weight: "bold"),
            #linebreak()
            #text("Application Form (For Individual only)", weight: "bold"),
            #linebreak()
            #text("Please fill the form in English and in BLOCK LETTERS."),
            #linebreak()
            #text("Fields marked with '*' are mandatory fields", fill: red)
          ]
        )
      ],
      // === Right side: Logo + App Info
      [
              #grid(
                columns: (2fr, 6fr),
                gutter: 6pt,
                [
                  #text("Application Type*", size: 12pt) 
				    #linebreak()
				    #linebreak()
				  #text("KYC Mode", size: 12pt)				 
                ],
                grid[   
			       #text(
						  if ReKycDetails.ApplicationType == "New KYC" { "☑ New KYC" } else { "☐ New KYC" }
						 ) #h(20pt)
				   #text(
						if ReKycDetails.ApplicationType == "Modification" { "☑ Update/Modification" } else { "☐ Update/Modification" }
					  )
				 #linebreak()
					 #text(
						if ReKycDetails.ApplicationType == "Modification" { "Kyc Number"  }
					  )
			    #linebreak()
				#linebreak()
			  ☐ Normal/Physical #h(20pt)  ☑ DigiLocker 
			  ]
           )          
      ],
	  grid.cell(image("../Assets/CompanyLogo.png", width: 100%))
    )

    #v(1pt)
////// For Identity column box
     #v(-10pt)
	 
    #grid(
      columns: (6fr),
      gutter: 4pt,
	  align: left,
	  inset: 4pt,
	  fill: gray.lighten(70%),
      [
	  	#text("1. Identity Details (Please refer instruction A)", weight: "bold")
	  ]
	)      	 
#v(5pt)
	
	 #grid(
      columns: (4fr, 7fr, 4fr),
      gutter: 2pt,
	  align: left,
	  inset: 0pt,
	  stroke: none, // for adding border
	  grid[
			#h(8pt)#text("Name* (Same as ID proof) :",  size: 11pt,weight: "bold")
	 
			#h(8pt)#text("Maiden Name (If any) :",  size: 11pt,weight: "bold")
	
			#h(8pt)#text("Father/Spouse Name * :",  size: 11pt,weight: "bold")
	
			#h(8pt)#text("Mother Name : ",  size: 11pt,weight: "bold")
   
			#h(8pt) #text("Date of Birth* :", size: 11pt,weight: "bold")
		  
			#h(8pt) #text("PAN : ", size: 11pt,weight: "bold")

		],
		 grid[
			#text(ReKycDetails.ClientsNamePrefix + " " + ReKycDetails.ClientFullName, size: 11pt) #v(0pt)
			#text(ReKycDetails.MaidenNamePrefix + " " + ReKycDetails.MaidenName,  size: 11pt)#v(0pt)
			#text(ReKycDetails.FatherSpouseNamePrefix + " " + ReKycDetails.FatherSpouseName,  size: 11pt)#v(0pt)
			#text(ReKycDetails.MotherNamePrefix + " " + ReKycDetails.MotherName, size: 11pt)#v(0pt)
      #text(ReKycDetails.DateOfBirth, size: 11pt) #v(0pt)
      //#text(dob, size: 11pt) #v(0pt)
			#text(ReKycDetails.PAN, size: 11pt) #v(0pt)

		],

		grid[

		//align(right)[
		 #box(
			stroke: 1pt,
			width: 4cm,
			inset: 0pt,
			[
			  #block(spacing: 6pt)[
				// Passport Photo
				#box(
				  width: 4cm,
				  height: 3.8cm,
				  inset: 2pt,
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
				#v(-10pt)
			  // Signature box
				#box(
				  width: 4cm,
				  height: 1.4cm,
				  inset: 2pt,
				  stroke: 1pt,
			  [
				#align(bottom + center)[
				//#text("Signature", size: 8pt, fill: gray.lighten(40%))
				   #if signPath != "" {
					    image(signPath, width: 100%, height: 100%)
					}
			  ]
			]
		  )
			  ]
			]
		)
		 //]
	  ]
	 )
#v(-40pt)

#grid(
      columns: (3fr, 5fr, 3.5fr),
      gutter: 2pt,     //  Adds space between elements (e.g., columns, grid cells)
	  align: left,
	  inset: 2pt,    //  Adds space inside elements (like padding) for padding (spacing)
	  stroke: none, // for adding border
	  grid[
 
				#h(7pt) #text("Gender* :", size: 11pt,weight: "bold")#v(0pt)
				#h(7pt) #text("Marital Status* :", size: 11pt,weight: "bold")#v(3pt)
				#h(7pt)#text("Nationality* :", size: 11pt,weight: "bold")#v(4pt)
				#h(7pt)#text("Residential Status* :", size: 11pt,weight: "bold")#v(2pt)
		  ],
		 grid[

				#text((if ReKycDetails.Gender == "Male" { "☑" } else { "☐" }) + " Male")
				#text((if ReKycDetails.Gender == "Female" { "☑" } else { "☐" }) + " Female")
				#text((if ReKycDetails.Gender == "Transgender" { "☑" } else { "☐" }) + " Transgender")
			  #v(0pt)
				#text((if ReKycDetails.MaritalStatus == "Single" { "☑" } else { "☐" }) + " Single")
				#text((if ReKycDetails.MaritalStatus == "Married" { "☑" } else { "☐" }) + " Married")
				#v(4pt)
				#text((if ReKycDetails.Nationality == "Indian" { "☑" } else { "☐" }) + " Indian")
				#text((if ReKycDetails.Nationality == "Other" { "☑" } else { "☐" }) + " Other")
			  #v(0pt)
				#text((if ReKycDetails.Residencial == "Resident Individual" { "☑" } else { "☐" }) + " Resident Individual")
				#text((if ReKycDetails.Residencial == "Non Resident Indian" { "☑" } else { "☐" }) + " Non Resident Indian")
				#text((if ReKycDetails.Residencial == "Foreign National" { "☑" } else { "☐" }) + " Foreign National")
				#text((if ReKycDetails.Residencial == "Person of Indian Origin" { "☑" } else { "☐" }) + " Person of Indian Origin")
	       ]
     )
		
	
	////// For Proof ofIdentity column box 2
  #v(2pt)	 
#grid(
      columns: (6fr),
  gutter: 4pt,
	  align: left,
  inset: 4pt,
	  fill: gray.lighten(70%),
      [
    #text("2. Proof of Identity (POI)* (Please refer instruction B)", weight: "bold")
		]
)
#h(8pt)#text("(Certified copy of any one of the following Proof of identity [POI] needs to be submitted)", size: 10pt)
 #grid(
		columns: (4fr, 4fr,4fr,3fr),
		gutter: 5pt,
		align: left,
		inset: 5pt,
		grid[
				#h(8pt) #text("A - UID (Aadhar card) : ", weight: "bold")
			
				#h(8pt) #text("B - PAN Card : ", weight: "bold")
			
				#h(8pt) #text("C - Passport Number : ", weight: "bold")
			
				#h(8pt) #text("D - Voter ID : ", weight: "bold")
			
				#h(8pt) #text("E - Driving License : ", weight: "bold")
			
				#h(8pt) #text("F - NAREGA Job Card : ", weight: "bold")
			
				#h(8pt) #text("Z - Other : ", weight: "bold")
			],
		grid[
				#text(ReKycDetails.UID)
			
				#text(ReKycDetails.PAN)  	
			],
		grid[
                #text("")
				#linebreak()
				#text("")
				#linebreak()
				#text("")
				#linebreak()
				#text("Passport Expiry Date :",weight: "bold")
				#text("")
	            #linebreak()
			    #text("")
	            #linebreak()
				#text("")
	            #linebreak()
				#text("Driving Expiry Date :",weight: "bold")
				#text("")
				#linebreak()
				#text("")
				#linebreak()
				#text("Identification Number : ",weight: "bold")
		],
		grid[

		]
    )

	////// For Proof Address column box 3
	#v(1pt)
	
	#grid(
      columns: (6fr),
      gutter: 2pt,
	  align: left,
	  inset: 1pt,
	  fill: gray.lighten(70%),
      [
    #text("3. Proof of Address (POA)* (Please refer instruction C)", weight: "bold")
		#linebreak()
		#h(20pt) #text("3.1 Correspondence / Local Address", weight: "bold")
	  ]
	)
	#v(0pt)
	#h(5pt)#text("Address", weight: "bold",size:14pt)
	#v(0pt)
	
	#grid(
      columns: (3fr, 7fr),
      gutter: 2pt,
	  align: left,
	  inset: 1pt,
	  grid[
      	  #h(8pt)#text("Line 1* :", weight: "bold") 

		   #h(8pt)#text("Line 2 :", weight: "bold") 

		   #h(8pt)#text("Line 3 :", weight: "bold") 

		   #h(8pt)#text("City/Town/Village* :", weight: "bold") 

		   #h(8pt)#text("PIN Code :", weight: "bold") 
	
		  #h(8pt) #text("State :", weight: "bold") 

		  #h(8pt) #text("Country :", weight: "bold")

		  #v(2pt)#h(8pt)#text("Address Type* ", weight: "bold", size: 14pt) 
		],
		 grid[
      	  #h(6pt) #text(ReKycDetails.CorrAddress1) 

		  #h(6pt) #text(ReKycDetails.CorrAddress2)

		  #h(8pt)  #text(ReKycDetails.CorrAddress3)

		  #h(8pt) #text(ReKycDetails.CorrCity) 
		 
		  #h(8pt) #text(ReKycDetails.CorrPincode)

		  #h(8pt)#text(ReKycDetails.CorrState) 

		  #h(8pt)#text(ReKycDetails.CorrCountry)

			]
	)
	#v(-5pt)
	#h(8pt)#text("Proof of Address * :", weight: "bold")
	#v(-10pt)

	#grid(
		columns: (4fr, 4fr,4fr,3fr),
		gutter: 5pt,
		align: left,
		inset: 5pt,
		grid[
				#h(8pt) #text("A - UID (Aadhar card) : ", weight: "bold")
			
				#h(8pt) #text("B - PAN Card : ", weight: "bold")
			
				#h(8pt) #text("C - Passport Number : ", weight: "bold")
			
				#h(8pt) #text("D - Voter ID : ", weight: "bold")
			
				#h(8pt) #text("E - Driving License : ", weight: "bold")
			
				#h(8pt) #text("F - NAREGA Job Card : ", weight: "bold")
			
				#h(8pt) #text("Z - Other : ", weight: "bold")
			],
		grid[
				#text(ReKycDetails.UID)
			
				#text(ReKycDetails.PAN)  	
			],
		grid[
                #text("")
				#linebreak()
				#text("")
				#linebreak()
				#text("")
				#linebreak()
				#text("Passport Expiry Date :",weight: "bold")
				#text("")
	            #linebreak()
			    #text("")
	            #linebreak()
				#text("")
	            #linebreak()
				#text("Driving Expiry Date :",weight: "bold")
				#text("")
				#linebreak()
				#text("")
				#linebreak()
				#text("Identification Number : ",weight: "bold")
		],
		grid[

		]
    )

	#v(-10pt)
	

     #grid(
      columns: (6fr),
      gutter: 5pt,
	  align: left,
	  inset: 5pt,
	  fill: gray.lighten(70%),
      [
      	#text("3.2 Permanent Address (If Permanent Address same as Correspondence Address)", weight: "bold")
		]
	)
	#v(-10pt)
	
	#grid(
      columns: (3fr, 7fr),
      gutter: 2pt,
	  align: left,
	  inset: 1pt,
	  grid[
      	#h(8pt)#text("Line 1* :", weight: "bold") 

		   #h(8pt)#text("Line 2 :", weight: "bold") 

		   #h(8pt)#text("Line 3 :", weight: "bold") 

		   #h(8pt)#text("City/Town/Village* :", weight: "bold") 

		   #h(8pt)#text("PIN Code :", weight: "bold") 
	
		  #h(8pt) #text("State :", weight: "bold") 

		  #h(8pt) #text("Country :", weight: "bold")
		],
		 grid[
      	  #h(6pt) #text(ReKycDetails.CorrAddress1) 

		  #h(6pt) #text(ReKycDetails.CorrAddress2)

		  #h(8pt)  #text(ReKycDetails.CorrAddress3)

		  #h(8pt) #text(ReKycDetails.CorrCity) 
		 
		  #h(8pt) #text(ReKycDetails.CorrPincode)

		  #h(8pt)#text(ReKycDetails.CorrState) 

		  #h(8pt)#text(ReKycDetails.CorrCountry)
		 
		 #v(10pt)

			]
	)
	
  ]
)

#pagebreak()
//#embed("../Assets/Test.pdf")
//#image("../Assets/CompanyLogo.bmp", width: 100%)
//#image("../Assets/Test.pdf")
//#pdf("../Assets/Test.pdf")
//grid.cell(image("../Assets/CompanyLogo.bmp", width: 50%))




// Second Page

//Rekyc Second Page

// === Email & Mobile Grid ===

#box(
  stroke: 1pt,
  inset: 0pt,
      [

			#v(8pt)
			#box(
			fill: rgb("#d3d3d3"), // Light pink background
			stroke: 1pt,
			inset: 0pt,
			[
				#grid(
				columns: (auto, 1fr),
				gutter: 0pt,
				[
					#v(8pt)
					// Heading text in white
				#text(" 4. Contact Details", weight: "bold", fill: black)
					#v(8pt)
			],
			
				)
			]
			)

			#h(8pt)#text("Email : ", weight: "bold") 
			#text(
			if ReKycDetails.Email != none and ReKycDetails.Email.trim() != "" {
				ReKycDetails.Email
			} else {
				"NA"
			}
			)
			#v(8pt)

			#h(8pt)#text("Mobile : ", weight: "bold") 
			#text(
			if ReKycDetails.Mobile != none and ReKycDetails.Mobile.trim() != "" {
				ReKycDetails.Mobile
			} else {
				"NA"
			}
			)
			#v(8pt)


			//5.FATCA

			#box(
			fill: rgb("#d3d3d3"), // Light pink background
			stroke: 1pt,
			inset: 0pt,
			[
				#grid(
				columns: (auto, 1fr),
				gutter: 0pt,
					[
					#v(8pt)
					// Heading text in white
				#text(" 5. FATCA/CRS Information (Tick Applicable)Residence for tax purpose in Jurisdiction(s) Outside India(Please refer instruction B at the end.)", weight: "bold", fill: black)
					#v(8pt)
				],

				)
			]
			)
			#v(4pt)

			#h(8pt)#text("Additional Details Required* (Mandatory only if above option (5) is ticked) : ", weight: "bold") 

			#v(4pt)

			#h(8pt)#text("Country of Jurisdiction of Residence*(as per ISO 3166 ) : ", weight: "bold")
			//#text(if ReKycDetails.JuriResidence != none and ReKycDetails.JuriResidence.trim() != "" { ReKycDetails.JuriResidence } else { "NA" })

			#h(8pt)#text("Country code of Jurisdiction of Residence(as per ISO 3166 ) : ", weight: "bold")
			//#text(if ReKycDetails.JuriCode != none and ReKycDetails.JuriCode.trim() != "" { ReKycDetails.JuriCode } else { "NA" })

			#v(4pt)

			#h(8pt)#text("Tax Identification Number or Equivalent* (if issued by Jurisdiction) : ", weight: "bold")
			//#text(if ReKycDetails.TaxIdNumber != none and ReKycDetails.TaxIdNumber.trim() != "" { ReKycDetails.TaxIdNumber } else { "NA" })

			#v(4pt)

			#h(8pt)#text("Place/City of Birth*(as per ISO 3166 ) : ", weight: "bold")
			//#text(if ReKycDetails.CityofBirth != none and ReKycDetails.CityofBirth.trim() != "" { ReKycDetails.CityofBirth } else { "NA" })

			#h(8pt)#text("Country of Birth*(as per ISO 3166 ) : ", weight: "bold")
			//#text(if ReKycDetails.CounteryofBirth != none and ReKycDetails.CounteryofBirth.trim() != "" { ReKycDetails.CounteryofBirth } else { "NA" })

			#v(4pt)

			// Line1
			#h(8pt)#text("Line1* : ", weight: "bold")
			//#text(if ReKycDetails.Address1 != none and ReKycDetails.Address1.trim() != "" { ReKycDetails.Address1 } else { "NA" })

			// Line2
			#h(8pt)#text("Line2 : ", weight: "bold")
			//#text(if ReKycDetails.Address2 != none and ReKycDetails.Address2.trim() != "" { ReKycDetails.Address2 } else { "NA" })

			// Line3
			#h(8pt)#text("Line3 : ", weight: "bold")
			//#text(if ReKycDetails.Address3 != none and ReKycDetails.Address3.trim() != "" { ReKycDetails.Address3 } else { "NA" })


			#h(8pt)#text("City/Town/Village* : ", weight: "bold")
			//#text(if ReKycDetails.City != none and ReKycDetails.City.trim() != "" { ReKycDetails.City } else { "NA" })
			#h(30pt)
			#h(8pt)#text("Pin/Post Code* : ", weight: "bold")
			//#text(if ReKycDetails.Pincode != none and ReKycDetails.Pincode.trim() != "" { ReKycDetails.Pincode } else { "NA" })
			#h(30pt)
			#h(8pt)#text("State : ", weight: "bold")
			//#text(if ReKycDetails.State != none and ReKycDetails.State.trim() != "" { ReKycDetails.State } else { "NA" })
			#h(30pt)
			#h(8pt)#text("Country : ", weight: "bold")
			//#text(if ReKycDetails.Country != none and ReKycDetails.Country.trim() != "" { ReKycDetails.Country } else { "NA" })

			#v(8pt)


			//Details of Related Person
			#box(
			fill: rgb("#d3d3d3"), 
			stroke: 1pt,
			inset: 0pt,
			[
				#grid(
				columns: (auto, 1fr),
				gutter: 0pt,
					[
					#v(8pt)
					// Heading text in white
				#text(" 6. Details of Related Person(Optional)(Please refer instruction G at the end)(in case of addtional related person, please fil 'Annexure of B1')", weight: "bold", fill: black)
					#v(8pt)
				],

				)
			]
			)
				#v(8pt)
				//Related Person 
				#block(spacing: 8pt)[
				//#h(8pt)#text((if ReKycDetails.ModeRelatedPerson == "RelatedPerson" { "?" } else { "?" }) + " Related Person"),
				#h(30pt)
				//#text((if ReKycDetails.ModeRelatedPerson == "DeletionPerson" { "?" } else { "?" }) + " Deletion of Related Person"),
				#h(30pt)
				#h(8pt)#text("KYC Number of Related Person(if available*) : ", weight: "bold")
				//#text(if ReKycDetails.KYCRelatedPerson != none and ReKycDetails.KYCRelatedPerson.trim() != "" { ReKycDetails.KYCRelatedPerson } else { "NA" })
				]
				#v(8pt)
				//Related Person Type
				#block(spacing: 8pt)[
				#h(8pt)#text("Related Person Type* :")  
				#h(6pt)
				//#h(8pt)#text((if ReKycDetails.RelatedPersonType == "GurdianOfMinor" { "?" } else { "?" }) + " Guardian Of Minor"),
				#h(30pt)
				//#text((if ReKycDetails.RelatedPersonType == "Assignee" { "?" } else { "?" }) + " Assignee"),
				#h(30pt)
				//#text((if ReKycDetails.RelatedPersonType == "AuthRepresentive" { "?" } else { "?" }) + " Authorized Representative"),
				#h(30pt)
				]
				#v(8pt)
				#h(8pt)#text("Name*(If name is provided, the details in Section 6 are optional.) : ",weight: "bold")
				//#text(if ReKycDetails.ClientName != none and ReKycDetails.ClientName.trim() != "" { ReKycDetails.ClientName } else { "NA" })

			#v(8pt)


			#v(8pt)


			//POI(Related Person)

			#box(
			fill: rgb("#d3d3d3"), 
			stroke: 1pt,
			inset: 0pt,
			[
				#grid(
				columns: (auto, 1fr),
				gutter: 0pt,
					[
					#v(8pt)
					// Heading text in white
				#text(" Proof of Identity(POI) Related Person*(Please see instruction (H) at the end)", weight: "bold", fill: black)
					#v(8pt)
				],

				)
			]
			)


			#v(5pt)

			//Passport Number
			#h(8pt)#text(" Passport Number : ", weight: "bold")
			// #h(8pt)#text((if ReKycDetails.PassportNumber != "" { "?" } else { "?" }) + " Passport Number : ", weight: "bold")
			// #text(
				//if ReKycDetails.PassportNumber != "" {
				//  ReKycDetails.PassportNumber
				//} else {
				// "NA"
			// }
			//)
			#h(73pt)
			#h(80pt)#text("Expire Date(DD/MM/YYYY) : ", weight: "bold")
			//#text(
				//if ReKycDetails.PassExpireDate != "" {
			//   ReKycDetails.PassExpireDate
				//} else {
			//   "NA"
			//  }
			// )

			#v(3pt)

			//Voter id
			#h(8pt)#text(" Voter ID Card: ", weight: "bold")
			//#h(8pt)#text((if ReKycDetails.VoterID != "" { "?" } else { "?" }) + " Voter ID Card: ", weight: "bold")
			// #text(
			// if ReKycDetails.VoterID != "" {
				//  ReKycDetails.VoterID
			// } else {
				//  "NA"
				//}
			// )

			#v(3pt)

			//Driving Licence
			#h(8pt)#text(" Driving License : ", weight: "bold")
			//#h(8pt)#text((if ReKycDetails.DrivingLicence != "" { "?" } else { "?" }) + " Driving License : ", weight: "bold")
			//#text(
			// if ReKycDetails.DrivingLicence != "" {
				//  ReKycDetails.DrivingLicence
			// } else {
				// "NA"
			// }
			//)
			#h(80pt)
			#h(80pt)#text("Expire Date(DD/MM/YYYY) : ", weight: "bold")
			//#text(
			//  if ReKycDetails.DriveExpireDate != "" {
			//   ReKycDetails.DriveExpireDate
			// } else {
			//   "NA"
			// }
			// )
			#v(3pt)

			// Aadhaar Number
			#h(8pt)#text(" UID (Aadhaar) : ", weight: "bold")
			//#h(8pt)#text((if ReKycDetails.AadhaarNumber != "" { "?" } else { "?" }) + " UID (Aadhaar) : ", weight: "bold")
			//#text(
			//  if ReKycDetails.AadhaarNumber != "" {
			//   ReKycDetails.AadhaarNumber
			// } else {
			//   "NA"
			// }
			// )
			
			#v(3pt)
			//job card
			#h(8pt)#text("  NAREGA Job Card :", weight: "bold")
			//#h(8pt)#text((if ReKycDetails.JobCard != "" { "?" } else { "?" }) + "  NAREGA Job Card :", weight: "bold")
			//#h(8pt)
			// #text(
				//  if ReKycDetails.JobCard != "" {
				// ReKycDetails.JobCard + "                       "  // ? Add more spaces here
				//} else {
				//  "                                      "  // ? Match length for empty case
			// },
			// )
			#v(3pt)

			//Others
			#h(8pt)#text(" Others (Any document notified by Central Government): ", weight: "bold")
			// #h(8pt)#text((if ReKycDetails.Others != "" { "?" } else { "?" }) + " Others (Any document notified by Central Government): ", weight: "bold")
			//#text(
			//  if ReKycDetails.Others != "" {
			//   ReKycDetails.Others
			// } else {
			//   "NA"
			// }
			//)
			#h(60pt)
			#h(8pt)#text("Identification Number: ", weight: "bold")
			//#text(
				//if ReKycDetails.IDNumber != "" {
				//  ReKycDetails.IDNumber
			// } else {
				//  "NA"
			// }
			// )

			#v(8pt)

				
			//Remarks

			#box(
			fill: rgb("#d3d3d3"), 
			stroke: 1pt,
			inset: 0pt,
			[
				#grid(
				columns: (auto, 1fr),
				gutter: 0pt,
					[
					#v(8pt)
					// Heading text in white
				#text(" 7. Remarks (If any)", weight: "bold", fill: black)
					#v(8pt)
				],
			)
			]
			)
				#h(8pt)
				// #text(if ReKycDetails.Remark != none and ReKycDetails.Remark.trim() != "" { ReKycDetails.Remark } else { "NA" })

			#v(30pt)
			

			//Applicaton Declaration
			#box(
			fill: rgb("#d3d3d3"), 
			stroke: 1pt,
			inset: 0pt,
			[
				#grid(
				columns: (auto, 1fr),
				gutter: 0pt,
					[
					#v(8pt)
					// Heading text in white
				#text(" 8.Application Declaration", weight: "bold", fill: black)
					#v(8pt)
				],
				)
			]
			)

			#v(2pt)

			#grid(
				columns: (3fr, 1fr),
				gutter: 0pt,
			[


			#box(
					inset: 2pt,
			[
			#h(8pt) #text(
				"I hereby declare that the details furnished above are true and correct to the best of my knowledge and belief and I undertake to inform you of any changes therein immediately. In case any of the above information is found to be false, misleading or misrepresented. I am aware that i may be held liable for it. I hereby declare that i am not making this application for the purpose of contravention of any act, Rules, Regulations or any statute of legislation or any notification/direction issued by Government or any statutory authority from time to time.
				I hereby consent to receiving information from Central KYC registry through SMS/Email on the above registered number/email Address."
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
				[
					#align(bottom + center)[
					#if signPath != "" {
							image(signPath, width: 100%, height: 100%)
						}
					]
				]
				)
				#align(center, text("Signature / Thumb Impression of Applicant", size: 8pt))
			])

			])

			#h(8pt)#text("Date : ", weight: "bold")
			#text(if ReKycDetails.DeclarationDate != none and ReKycDetails.DeclarationDate.trim() != "" { ReKycDetails.DeclarationDate } else { "NA" })
			#h(30pt)
			#h(8pt)#text("Place : ", weight: "bold")
			#text(if ReKycDetails.DeclarationPlace != none and ReKycDetails.DeclarationPlace.trim() != "" { ReKycDetails.DeclarationPlace } else { "NA" })
			#h(30pt)

			#v(2pt)


			//Attestation / For Office use only
			#box(
			fill: rgb("#d3d3d3"), 
			stroke: 1pt,
			inset: 0pt,
			[
				#grid(
				columns: (auto, 1fr),
				gutter: 0pt,
					[
					#v(8pt)
					// Heading text in white
				#text(" 9. Attestation / For Office use only", weight: "bold", fill: black)
					#v(8pt)
				],
				)
			]
			)

			#v(1pt)

			#h(8pt)#(text("Document Received", weight: "bold", fill: black))
			#h(12pt)
				//#text((if ReKycDetails.CertifiedCopy == "true" { "?" } else { "?" }) + "Certified Copies"),
				#text("☐ Certified Copies"),
				#h(8pt)

			#grid(
				columns: (3fr, 3fr),
				gutter: 0pt,
			[

				#box(
					inset: 2pt,
					fill: rgb("#d3d3d3"), 
				[
						#h(8pt) #text("KYC Verification Carried Out by(Refer Instruction I) ",weight: "bold", fill: black)
				])

					#v(8pt)

					#h(8pt)#text("Date : ", weight: "bold")
					#text(if ReKycDetails.EMPDate != none and ReKycDetails.EMPDate.trim() != "" { ReKycDetails.EMPDate } else { "NA" })
					#h(8pt)#text("EMP. Name : ", weight: "bold")
					#text(if ReKycDetails.EMPName != none and ReKycDetails.EMPName.trim() != "" { ReKycDetails.EMPName } else { "NA" })

					#h(8pt)#text("EMP. Code : ", weight: "bold")
					#text(if ReKycDetails.EMPCode != none and ReKycDetails.EMPCode.trim() != "" { ReKycDetails.EMPCode } else { "NA" })
					#h(8pt)
					#text("EMP. Designation : ", weight: "bold")
					#text(if ReKycDetails.Designation != none and ReKycDetails.Designation.trim() != "" { ReKycDetails.Designation } else { "NA" })
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
						#text("Institution Details",weight: "bold", fill: black)
					])
					#v(8pt)

					#text("Name : ", weight: "bold")
					#text(if ReKycDetails.AdminName != none and ReKycDetails.AdminName.trim() != "" { ReKycDetails.AdminName } else { "NA" })

					#text("Code : ", weight: "bold")
					#text(if ReKycDetails.AdminCode != none and ReKycDetails.AdminCode.trim() != "" { ReKycDetails.AdminCode } else { "NA" })
          #h(8pt)
					#text("EMP. Branch : ", weight: "bold")
					#text(if ReKycDetails.EMPBranch != none and ReKycDetails.EMPBranch.trim() != "" { ReKycDetails.EMPBranch } else { "NA" })
					#v(4pt)
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

#pagebreak()


// Third page
  
  //Nomination Form


// Prepare dynamic values for each nominee
#let n1 = if nominee.len() > 0 {
  let n = nominee.at(0)
  (
    name: (if "NomFirstName" in n { n.NomFirstName } else { "" }) + (if "NomMiddleName" in n and n.NomMiddleName != "" { " " + n.NomMiddleName } else { "" }) + (if "NomLastName" in n and n.NomLastName != "" { " " + n.NomLastName } else { "" }),
    share: if "NomPercentage" in n { n.NomPercentage } else { "" },
    relation: if "NomRelation" in n { n.NomRelation } else { "" },
    address: (
      if "NomAddress1" in n { n.NomAddress1 } else { "" } +
      if "NomAddress2" in n { ", " + n.NomAddress2 } else { "" } +
      if "NomAddress3" in n { ", " + n.NomAddress3 } else { "" } +
      if "NomAddressCity" in n { ", " + n.NomAddressCity } else { "" } +
      if "NomAddressState" in n { ", " + n.NomAddressState } else { "" } +
      if "NomAddressCountry" in n { ", " + n.NomAddressCountry } else { "" }
    ),
    pincode: if "NomPincode" in n { n.NomPincode } else { "" },
    mobile: if "NomMobile" in n { n.NomMobile } else { "" },
    UID: if "NomineeUID" in n { n.NomineeUID } else { "" },
    //dob: if "NomineeDOB" in n { n.NomineeDOB } else { "" }
    dob: if "NomineeDOB" in n and n.NomineeDOB.len() == 8 {
    let d = n.NomineeDOB
    d.slice(6,8) + "/" + d.slice(4,6) + "/" + d.slice(0,4)
    } else { "--" },
  )
} else {
  (name: "", share: "", relation: "", address: "", pincode: "", mobile: "", UID: "", dob: "")
}

#let n2 = if nominee.len() > 1 {
  let n = nominee.at(1)
  (
    name: (if "NomFirstName" in n { n.NomFirstName } else { "" }) + (if "NomMiddleName" in n and n.NomMiddleName != "" { " " + n.NomMiddleName } else { "" }) + (if "NomLastName" in n and n.NomLastName != "" { " " + n.NomLastName } else { "" }),
    share: if "NomPercentage" in n { n.NomPercentage } else { "" },
    relation: if "NomRelation" in n { n.NomRelation } else { "" },
  address: (
      if "NomAddress1" in n { n.NomAddress1 } else { "" } +
      if "NomAddress2" in n { ", " + n.NomAddress2 } else { "" } +
      if "NomAddress3" in n { ", " + n.NomAddress3 } else { "" } +
      if "NomAddressCity" in n { ", " + n.NomAddressCity } else { "" } +
      if "NomAddressState" in n { ", " + n.NomAddressState } else { "" } +
      if "NomAddressCountry" in n { ", " + n.NomAddressCountry } else { "" }
    ),
    pincode: if "NomPincode" in n { n.NomPincode } else { "" },
    mobile: if "NomMobile" in n { n.NomMobile } else { "" },
    UID: if "NomineeUID" in n { n.NomineeUID } else { "" },
    //dob: if "NomineeDOB" in n { n.NomineeDOB } else { "" }
    dob: if "NomineeDOB" in n and n.NomineeDOB.len() == 8 {
    let d = n.NomineeDOB
    d.slice(6,8) + "/" + d.slice(4,6) + "/" + d.slice(0,4)
   } else { "--" },
    
  )
} else {
  (name: "", share: "", relation: "", address: "", pincode: "", mobile: "", UID: "", dob: "")
}

#let n3 = if nominee.len() > 2 {
  let n = nominee.at(2)
  (
    name: (if "NomFirstName" in n { n.NomFirstName } else { "" }) + (if "NomMiddleName" in n and n.NomMiddleName != "" { " " + n.NomMiddleName } else { "" }) + (if "NomLastName" in n and n.NomLastName != "" { " " + n.NomLastName } else { "" }),
    share: if "NomPercentage" in n { n.NomPercentage } else { "" },
    relation: if "NomRelation" in n { n.NomRelation } else { "" },
   address: (
      if "NomAddress1" in n { n.NomAddress1 } else { "" } +
      if "NomAddress2" in n { ", " + n.NomAddress2 } else { "" } +
      if "NomAddress3" in n { ", " + n.NomAddress3 } else { "" } +
      if "NomAddressCity" in n { ", " + n.NomAddressCity } else { "" } +
      if "NomAddressState" in n { ", " + n.NomAddressState } else { "" } +
      if "NomAddressCountry" in n { ", " + n.NomAddressCountry } else { "" }
    ),
    pincode: if "NomPincode" in n { n.NomPincode } else { "" },
    mobile: if "NomMobile" in n { n.NomMobile } else { "" },
    UID: if "NomineeUID" in n { n.NomineeUID } else { "" },
    //dob: if "NomineeDOB" in n { n.NomineeDOB } else { "" }
    dob: if "NomineeDOB" in n and n.NomineeDOB.len() == 8 {
    let d = n.NomineeDOB
    d.slice(6,8) + "/" + d.slice(4,6) + "/" + d.slice(0,4)
   } else { "--" },
  )
} else {
  (name: "", share: "", relation: "", address: "", pincode: "", mobile: "", UID: "", dob: "")
}

//Guardian

#let n1g = if (nominee.len() > 0 and "GuardianDetails" in nominee.at(0) and nominee.at(0).GuardianDetails != none) {
  let g = nominee.at(0).GuardianDetails
  (
   // name: if "NomGuardianFirstName" in g { g.NomGuardianFirstName + " " +g.NomGuardianMiddleName +" "+g.NomGuardianLastName } else { "" },
      name: if "NomGuardianFirstName" in g or "NomGuardianMiddleName" in g or "NomGuardianLastName" in g {
  (
    (if "NomGuardianFirstName" in g { g.NomGuardianFirstName } else { "" }) +
    (if "NomGuardianMiddleName" in g and g.NomGuardianMiddleName != "" { " " + g.NomGuardianMiddleName } else { "" }) +
    (if "NomGuardianLastName" in g { " " + g.NomGuardianLastName } else { "" })
  ).trim()
} else { "--" },

    address: (
      if "NomGuardianAddress1" in g { g.NomGuardianAddress1 } else { "" } +
      if "NomGuardianAddress2" in g { ", " + g.NomGuardianAddress2 } else { "" } +
      if "NomGuardianAddress3" in g { ", " + g.NomGuardianAddress3 } else { "" } +
      if "NomGuardianCity" in g { ", " + g.NomGuardianCity } else { "" } +
      if "NomGuardianState" in g { ", " + g.NomGuardianState } else { "" } +
      if "NomGuardianPincode" in g { ", " + g.NomGuardianPincode } else { "" }
    ),
    mobile: if "NomGuardianMobile" in g { g.NomGuardianMobile } else { "" },
    email: if "NomGuardianEmail" in g { g.NomGuardianEmail } else { "" },
    relation: if "NomGuardianRelation" in g { g.NomGuardianRelation } else { "" },
    pan: if "NomGuardianPAN" in g { g.NomGuardianPAN } else { "" },
    uid: if "NomGuardianUID" in g { g.NomGuardianUID } else { "" },
    //dob: if "NomGuardianDob" in g { g.NomGuardianDob } else { "" }
    dob: if "NomGuardianDob" in g and g.NomGuardianDob.len() == 8 {
      let d = g.NomGuardianDob
      d.slice(6,8) + "/" + d.slice(4,6) + "/" + d.slice(0,4)
    } else { "--" },
  )
} else {
  (name: "", address: "", mobile: "", email: "", relation: "", pan: "", uid: "", dob: "")
}

#let n2g = if (nominee.len() > 1 and "GuardianDetails" in nominee.at(1) and nominee.at(1).GuardianDetails != none) {
  let g = nominee.at(1).GuardianDetails
  (
   // name: if "NomGuardianFirstName" in g { g.NomGuardianFirstName + " " +g.NomGuardianMiddleName +" "+g.NomGuardianLastName  } else { "" },
      name: if "NomGuardianFirstName" in g or "NomGuardianMiddleName" in g or "NomGuardianLastName" in g {
  (
    (if "NomGuardianFirstName" in g { g.NomGuardianFirstName } else { "" }) +
    (if "NomGuardianMiddleName" in g and g.NomGuardianMiddleName != "" { " " + g.NomGuardianMiddleName } else { "" }) +
    (if "NomGuardianLastName" in g { " " + g.NomGuardianLastName } else { "" })
  ).trim()
} else { "--" },

    address: (
      if "NomGuardianAddress1" in g { g.NomGuardianAddress1 } else { "" } +
      if "NomGuardianAddress2" in g { ", " + g.NomGuardianAddress2 } else { "" } +
      if "NomGuardianAddress3" in g { ", " + g.NomGuardianAddress3 } else { "" } +
      if "NomGuardianCity" in g { ", " + g.NomGuardianCity } else { "" } +
      if "NomGuardianState" in g { ", " + g.NomGuardianState } else { "" } +
      if "NomGuardianPincode" in g { ", " + g.NomGuardianPincode } else { "" }
    ),
    mobile: if "NomGuardianMobile" in g { g.NomGuardianMobile } else { "" },
    email: if "NomGuardianEmail" in g { g.NomGuardianEmail } else { "" },
    relation: if "NomGuardianRelation" in g { g.NomGuardianRelation } else { "" },
    pan: if "NomGuardianPAN" in g { g.NomGuardianPAN } else { "" },
    uid: if "NomGuardianUID" in g { g.NomGuardianUID } else { "" },
    //dob: if "NomGuardianDob" in g { g.NomGuardianDob } else { "" }
    dob: if "NomGuardianDob" in g and g.NomGuardianDob.len() == 8 {
      let d = g.NomGuardianDob
      d.slice(6,8) + "/" + d.slice(4,6) + "/" + d.slice(0,4)
    } else { "--" },
  )
} else {
  (name: "", address: "", mobile: "", email: "", relation: "", pan: "", uid: "", dob: "")
}

#let n3g = if (nominee.len() > 2 and "GuardianDetails" in nominee.at(2) and nominee.at(2).GuardianDetails != none) {
  let g = nominee.at(2).GuardianDetails
  (
    //name: if "NomGuardianFirstName" in g { g.NomGuardianFirstName + " " +g.NomGuardianMiddleName +" "+g.NomGuardianLastName  } else { "" },
      name: if "NomGuardianFirstName" in g or "NomGuardianMiddleName" in g or "NomGuardianLastName" in g {
  (
    (if "NomGuardianFirstName" in g { g.NomGuardianFirstName } else { "" }) +
    (if "NomGuardianMiddleName" in g and g.NomGuardianMiddleName != "" { " " + g.NomGuardianMiddleName } else { "" }) +
    (if "NomGuardianLastName" in g { " " + g.NomGuardianLastName } else { "" })
  ).trim()
} else { "--" },

    address: (
      if "NomGuardianAddress1" in g { g.NomGuardianAddress1 } else { "" } +
      if "NomGuardianAddress2" in g { ", " + g.NomGuardianAddress2 } else { "" } +
      if "NomGuardianAddress3" in g { ", " + g.NomGuardianAddress3 } else { "" } +
      if "NomGuardianCity" in g { ", " + g.NomGuardianCity } else { "" } +
      if "NomGuardianState" in g { ", " + g.NomGuardianState } else { "" } +
      if "NomGuardianPincode" in g { ", " + g.NomGuardianPincode } else { "" }
    ),
    mobile: if "NomGuardianMobile" in g { g.NomGuardianMobile } else { "" },
    email: if "NomGuardianEmail" in g { g.NomGuardianEmail } else { "" },
    relation: if "NomGuardianRelation" in g { g.NomGuardianRelation } else { "" },
    pan: if "NomGuardianPAN" in g { g.NomGuardianPAN } else { "" },
    uid: if "NomGuardianUID" in g { g.NomGuardianUID } else { "" },
    //dob: if "NomGuardianDob" in g { g.NomGuardianDob } else { "" }
    dob: if "NomGuardianDob" in g and g.NomGuardianDob.len() == 8 {
      let d = g.NomGuardianDob
      d.slice(6,8) + "/" + d.slice(4,6) + "/" + d.slice(0,4)
   } else { "--" },
  )
} else {
  (name: "", address: "", mobile: "", email: "", relation: "", pan: "", uid: "", dob: "")
}


#v(20pt)
            #align(center)[
             #text("Nomination Form",weight: "bold",size: 14pt)
            ]

#v(8pt)

#v(5pt)

#block(spacing: 0em)[

  //Header

  //Table 1

  #table(
    columns: (1fr, 3fr),
    rows: (1.5cm),
    //stroke: (bottom: none),  // Remove bottom border to prevent double line
    [
       #align(center)[
        #text(weight: "bold")[TM/DP]
        #linebreak()
          #h(8pt) #text(
                  if "CompanyName" in ReKycDetails and ReKycDetails.CompanyName  != "" {
                  ReKycDetails.CompanyName
                  } else {
                   ""
                    },
                  size: 10pt
            )
      ]
      // #line(length: 57mm, stroke: 1pt)
    
    ],
    
     [
      #align(center)[
       #text("Form For Nomination", weight: "bold", size: 13pt)
       #linebreak()
       #text("(To be filled by individual applying singly or jointly)", size: 8pt)
         ]
      //     #line(length: 175mm, stroke: 1pt)

     ],
  )
  
  //Table 2 

  #table(
    columns: (1fr),  // Must match first table's column structure
    rows: (auto, auto, auto, auto),
    //stroke: (top: none),  // Remove top border to prevent double line
    [
     
     //Date
        #h(8pt)#text("Date : ", weight: "bold")
        #text(if ReKycDetails.DeclarationDate != none and ReKycDetails.DeclarationDate.trim() != "" { ReKycDetails.DeclarationDate } else { "NA" })
      //DP ID
       #h(60pt)#text("DPID : ", weight: "bold")
        #text(
          if "DPID" in ReKycDetails and ReKycDetails.DPID != "" {
            ReKycDetails.DPID
          } else {
            "NA"
          }
        )
      //Client ID
       #h(60pt)#text("Client ID : ", weight: "bold")
        #text(
          if "ClientID" in ReKycDetails and ReKycDetails.ClientID != "" {
            ReKycDetails.ClientID
          } else {
            "NA"
          }
        )
        //UCC
         #h(60pt)#text("UCC : ", weight: "bold")
        #text(
          if "UCC" in ReKycDetails and ReKycDetails.UCC != "" {
            ReKycDetails.UCC
          } else {
            "NA"
          }
        )
      
    ],
    [
     #align(center)[
      #text("I/We wish to make Nomination.[As per details given below].",weight: "bold")
      ]
    ],
    [ 
      #align(center)[
      #text("Nomination Details",weight: "bold")
      ]
    ],
    [
      #h(8pt)#text("I/We wish to make Nomination and do here by nominate the following person(s) who shall receive all the assets held in my  /our account in the event of my /our death.", weight: "bold")
    ],
  )

  //Table 3

    #table(
    columns: (2fr, 2fr, 2fr, 2fr, ),
    rows: (auto,auto,auto,auto),
    //stroke: (bottom: none),  // Remove bottom border to prevent double line

    //First row
    [
      #h(8pt)#text("Nomination can be made upto three nominee in the account.",weight: "bold", size: 12pt)
      
    ],
     [
      #align(center)[
       #v(10pt)#text("Details of 1st Nominee", weight: "bold", size: 12pt)
         ]
     ],
      [
      #align(center)[
       #v(10pt)#text("Details of 2nd Nominee", weight: "bold", size: 12pt)
         ]
     ],
      [
      #align(center)[
       #v(10pt)#text("Details of 3rd Nominee", weight: "bold", size: 12pt)
         ]
     ],

       //Second row Name of the nominee
     [
           #h(8pt)#text("Name of the nominees(s)(Mr./Ms.) :",weight: "bold", size: 12pt)
     ],
     [
                #align(center)[
                    #v(10pt)
                                #text(n1.name, size: 10pt)
          ]

     ],
     [

          #align(center)[
                    #v(10pt)
                                #text(n2.name, size: 10pt)
          ]
     ],
     [
          #align(center)[
                    #v(10pt)
                                #text(n3.name, size: 10pt)
          ]
        
     ],

      
   //Third row Percentage of the nominee
     [
           #h(8pt)#text("Share of each Nominee in Equally (If not Equally, please specify Percentage )",weight: "bold", size: 12pt)
     ],
     [
         #align(center)[
                    #v(10pt)
                                #text(n1.share, size: 10pt)
          ]
     ],
     [
       #align(center)[
                    #v(10pt)
                                #text(n2.share, size: 10pt)
          ]
     ],
     [
         #align(center)[
                    #v(10pt)
                                #text(n3.share, size: 10pt)
          ]
     ],


        //Fourth row Relationship of the nominee
     [
           #h(8pt)#text("Relation with the Applicant(If any)",weight: "bold", size: 12pt)
     ],
     [
        #align(center)[
                    #v(10pt)
                                #text(n1.relation, size: 10pt)
          ]
     ],
     [
         #align(center)[
                    #v(10pt)
                                #text(n2.relation, size: 10pt)
          ]
     ],
     [

         #align(center)[
                    #v(10pt)
                                #text(n3.relation, size: 10pt)
          ]
     ],

     //Fifth row Address of the nominee

    [
           #h(8pt)#text("Address of the Nominee",weight: "bold", size: 12pt)
     ],
     [
         #align(center)[
                    #v(10pt)
                                #text(n1.address, size: 10pt)
          ]
     ],
     [
         #align(center)[
                    #v(10pt)
                                #text(n2.address, size: 10pt)
          ]
     ],
     [  
       #align(center)[
                                #v(10pt)
                                #text(n3.address, size: 10pt)
                              ]
     ],


     //Pincode of the nominee

     [
           #h(8pt)#text("Pincode",weight: "bold", size: 12pt)
     ],
     [
          #align(center)[
                    #v(10pt)
                                #text(n1.pincode, size: 10pt)
          ]
     ],
     [
             #align(center)[
                                #v(10pt)
                                #text(n2.pincode, size: 10pt)
                              ]
     ],
     [  
             #align(center)[
                                #v(10pt)
                                #text(n3.pincode, size: 10pt)
     ]
     ],


        //Mobile of the nominee

     [
           #h(8pt)#text("Mobile/Tele. No. of the Nominees(s)",weight: "bold", size: 12pt)
     ],
     [
          #align(center)[
                    #v(10pt)
                                #text(n1.mobile, size: 10pt)
          ]
     ],
     [
          #align(center)[
                    #v(10pt)
                                #text(n2.mobile, size: 10pt)
          ]
     ],
     [  
        #align(center)[
                    #v(10pt)
                                #text(n3.mobile, size: 10pt)
          ]
     ],


    //Identification Detail of the nominee

     [
           #h(8pt)#text("Nominee Identification Details",weight: "bold", size: 12pt)
     ],
     [
        #align(center)[
                    #v(10pt)
                                #text(n1.UID, size: 10pt)
          ]
     ],
     [
         #align(center)[
                    #v(10pt)
                                #text(n2.UID, size: 10pt)
          ]
     ],
     [  
         #align(center)[
                    #v(10pt)
                                #text(n3.UID, size: 10pt)
          ]
     ],
  )]

   #v(8pt)
//Table 4 Gurdain Details
  #box(
  fill: rgb("#d3d3d3"), 
  stroke: 1pt,
  inset: 0pt,
     [
    #grid(
      columns: (auto, 1fr),
      gutter: 0pt,
     [
          #v(8pt)
        // Heading text in white
       #text(" The below Detail should be filled only if nominee(s) is a minor*", weight: "bold", size: 13pt)
          #v(8pt)
      ],
    )
  ]
)

   #table(
    columns: (2fr, 2fr, 2fr, 2fr, ),
    rows: (auto,auto,auto,auto),
    //stroke: (bottom: none),  // Remove bottom border to prevent double line

      //DOB of Nominee(If Minor)

     [
           #h(8pt)#text("Date of Birth(In case of minor nominees(s))  ",weight: "bold", size: 12pt)
     ],
     [
         #align(center)[
                    #v(10pt)
                                #text(n1.dob, size: 10pt)
          ]
     ],
     [
          #align(center)[
                    #v(10pt)
                                #text(n2.dob, size: 10pt)
          ]
     ],
     [  
          #align(center)[
                    #v(10pt)
                                #text(n3.dob, size: 10pt)
          ]
     ],

        //Guardian Names
     [
           #h(8pt)#text("Name of Guardian(Mr./Ms.)",weight: "bold", size: 12pt)
     ],
     [
         #align(center)[
                    #v(10pt)
                                #text(n1g.name, size: 10pt)
          ]
     ],
     [
           #align(center)[
                    #v(10pt)
                                #text(n2g.name, size: 10pt)
          ]
     ],
     [  
          #align(center)[
                    #v(10pt)
                                #text(n3g.name, size: 10pt)
          ]
     ],

        //Guardian Address
        [
           #h(8pt)#text("Address of Guardian",weight: "bold", size: 12pt)
        ],

          [
         #align(center)[
                    #v(10pt)
                                #text(n1g.address, size: 10pt)
          ]
     ],
     [
           #align(center)[
                    #v(10pt)
                                #text(n2g.address, size: 10pt)
          ]
     ],
     [  
          #align(center)[
                    #v(10pt)
                                #text(n3g.address, size: 10pt)
          ]
     ],

      //Guardian Mobile
        [
           #h(8pt)#text("Mobile/Tele.No. of the Guardian",weight: "bold", size: 12pt)
        ],

          [
         #align(center)[
                    #v(10pt)
                                #text(n1g.mobile, size: 10pt)
          ]
     ],
     [
           #align(center)[
                    #v(10pt)
                                #text(n2g.mobile, size: 10pt)
          ]
     ],
     [  
          #align(center)[
                    #v(10pt)
                                #text(n3g.mobile, size: 10pt)
          ]
     ],

       //Guardian Rlation
     [
           #h(8pt)#text("Relation of the Guardian with Nominees",weight: "bold", size: 12pt)
     ],
     [
          #align(center)[
                    #v(10pt)
                                #text(n1g.relation, size: 10pt)
          ]
     ],
     [
         #align(center)[
                    #v(10pt)
                                #text(n2g.relation, size: 10pt)
          ]
     ],
     [  
         #align(center)[
                    #v(10pt)
                                #text(n3g.relation, size: 10pt)
          ]
     ],

        //Guardian Identification
     [
           #h(8pt)#text("Guardian PAN",weight: "bold", size: 12pt)
     ],
     [
          #align(center)[
                    #v(10pt)
                                #text(n1g.pan, size: 10pt)
          ]
     ],
     [
          #align(center)[
                    #v(10pt)
                                #text(n2g.pan, size: 10pt)
          ]
     ],
     [  

          #align(center)[
                    #v(10pt)
                                #text(n3g.pan, size: 10pt)
          ]
     ],
        //Guardian UID
     [
           #h(8pt)#text("Guardian Adhaar",weight: "bold", size: 12pt)
     ],
     [
          #align(center)[
                    #v(10pt)
                                #text(n1g.uid, size: 10pt)
          ]
     ],
     [
         #align(center)[
                    #v(10pt)
                                #text(n2g.uid, size: 10pt)
          ]
     ],
     [  
         #align(center)[
                    #v(10pt)
                                #text(n3g.uid, size: 10pt)
          ]
     ],

        //Guardian DOB
     [
           #h(8pt)#text("Guardian DOB",weight: "bold", size: 12pt)
     ],
     [
         #align(center)[
                    #v(10pt)
                                #text(n1g.dob, size: 10pt)
          ]
     ],
     [
         #align(center)[
                    #v(10pt)
                                #text(n2g.dob, size: 10pt)
          ]
     ],
     [  
         #align(center)[
                    #v(10pt)
                                #text(n3g.dob, size: 10pt)
          ]
     ],
   )


    #h(8pt)#text("",weight: "bold", size: 13pt)

   #table(
    columns: (2fr, 2fr, 2fr),
    rows: (auto,auto,auto,auto),
    //stroke: (bottom: none),  // Remove bottom border to prevent double line

    // Heading 
    [
           #h(8pt)#text(" ",weight: "bold", size: 12pt)
     ],
     [
        #align(center)[
         #v(2pt)#text("Holders Name",weight: "bold", size: 12pt)
        ]
     ],
     [
        #align(center)[
           #v(2pt)#text("Signature Holders*", weight: "bold", size: 12pt)
        ]
     ],

     //  First row

     [
           #h(8pt)#text("Sole/First Holder(Mr./Ms.)",weight: "bold", size: 12pt)
     ],
     [
            #h(8pt)
          #text(
                (
                  if "FirstName" in ReKycDetails { ReKycDetails.FirstName } else { "" } +
                  if "MiddleName" in ReKycDetails and ReKycDetails.MiddleName != "" { " " + ReKycDetails.MiddleName } else { "" } +
                  if "LastName" in ReKycDetails and ReKycDetails.LastName != "" { " " + ReKycDetails.LastName } else { "" }
                ),
                  size: 10pt
            )
     ],
     [
            #box(
            width: 100%,
            height: 1.5cm,
            stroke: 1pt + black,
            inset: 4pt,
             [
			 #align(bottom + center)[
			  #if signPath != "" {
					image(signPath, width: 100%, height: 100%)
				}
			   
			  ]
            ]
          )
     ],

     // Second Row

     [
           #h(8pt)#text("Second Holder(Mr./Ms.)",weight: "bold", size: 12pt)
     ],
     [

            #h(8pt) #text(
                  if "HolderName2" in ReKycDetails and ReKycDetails.HolderName2  != "" {
                  //ReKycDetails.HolderName2
                  } else {
                   ""
                    },
                  size: 10pt
            )
     ],
     [
              #box(
            width: 100%,
            height: 1.5cm,
            stroke: 1pt + black,
            inset: 4pt,
             [
              #align(center)[
                #text("Signature",fill: gray.lighten(40%))
              ]
             ]
          ) 
     ],

     //Third Row
    
     [
           #h(8pt)#text("Third Holder(Mr./Ms.)",weight: "bold", size: 12pt)
     ],
     [
          #h(8pt) #text(
                  if "HolderName3" in ReKycDetails and ReKycDetails.HolderName3  != "" {
                 // ReKycDetails.HolderName3
                  } else {
                   ""
                    },
                  size: 10pt
            )
     ],
     [
            #box(
            width: 100%,
            height: 1.5cm,
            stroke: 1pt + black,
            inset: 4pt,
             [
              #align(center)[
                #text("Signature",fill: gray.lighten(40%))
              ]
             ]
          )
     ],

   )

#pagebreak()

//Fourth Page

//Individual Client Modification

//#box(
 // stroke: 1pt,
  //inset: 0pt,
  //[


				#v(12pt)
						#align(center)[
							#text(" Individual Client Modification Form: ", weight: "bold", fill: black, size:16pt)
						]
				
							#box(
							fill: rgb("#d3d3d3"), 
							stroke: 1pt,
							inset: 0pt,
							[
								#grid(
								columns: (auto, 1fr),
								gutter: 0pt,
									[
									#v(8pt)
									// Heading text in white
								#h(8pt) #text(" Personal Detail: ", weight: "bold", fill: black)
									#v(8pt)
								],
								)
							]
							)

				#v(10pt)

					#h(120pt)#text("New Value", weight:"bold",size:13pt )
					#h(185pt) #text("Old Value", weight:"bold",size:13pt )

				#{
				if "IncomeNew" in ReKycDetails and ReKycDetails.IncomeNew != "" {
					v(4pt)
				} else {
					none
				}
				}
				

				//Income 
				
				#if "IncomeNew" in ReKycDetails and ReKycDetails.IncomeNew  != "" {
					block(spacing: 8pt)[
				#h(8pt) #text(
								if "IncomeNew" in ReKycDetails {
								"Income"
								} else {
								""
									},
								size: 10pt,
								weight:"bold"
							)
				#h(75pt) #text(
								if "IncomeNew" in ReKycDetails and ReKycDetails.IncomeNew  != "" {
								ReKycDetails.IncomeNew
								} else {
								""
									},
								size: 10pt
							)
					#h(185pt) #text(
								if "IncomeOld" in ReKycDetails and ReKycDetails.IncomeOld  != "" {
								ReKycDetails.IncomeOld
								} else {
								""
									},
								size: 10pt
							)
					]
				}else{
				none
				}     
						

				//Income Date    
				#if "IncomeDateNew" in ReKycDetails and ReKycDetails.IncomeDateNew  != "" {
					block(spacing: 8pt)[
						#h(8pt) #text(
								if "IncomeDateNew" in ReKycDetails {
								"Income Date"
								} else {
								""
									},
								size: 10pt,
								weight:"bold"
							)
						#h(50pt) #text(
              if "IncomeDateNew" in ReKycDetails and ReKycDetails.IncomeDateNew.len() == 8 {
                let d = ReKycDetails.IncomeDateNew
                d.slice(6,8) + "/" + d.slice(4,6) + "/" + d.slice(0,4)
              } else {
                ""
              },
              size: 10pt
             )
					#h(195pt) #text(
            if "IncomeDateOld" in ReKycDetails and ReKycDetails.IncomeDateOld.len() == 8 {
              let d = ReKycDetails.IncomeDateOld
              d.slice(6,8) + "/" + d.slice(4,6) + "/" + d.slice(0,4)
            } else {
              ""
            },
            size: 10pt
            )

					]
				}else{
				
				}
				//Income Email    
				#if "EmailNew" in ReKycDetails and ReKycDetails.EmailNew  != "" {
					block(spacing: 8pt)[
						#h(8pt) #text(
								if "EmailNew" in ReKycDetails {
								"Email"
								} else {
								""
									},
								size: 10pt,
								weight:"bold"
							)
						#h(82pt) #text(
								if "EmailNew" in ReKycDetails and ReKycDetails.EmailNew  != "" {
								ReKycDetails.EmailNew
								} else {
								""
									},
								size: 10pt
							)
						#h(120pt) #text(
								if "EmailOld" in ReKycDetails and ReKycDetails.EmailOld  != "" {
								ReKycDetails.EmailOld
								} else {
								""
									},
								size: 10pt
							)
					]
				}else{
				none
				}


				// Mobile  
				#if "MobileNew" in ReKycDetails and ReKycDetails.MobileNew  != "" {
					block(spacing: 8pt)[
						#h(8pt) #text(
								if "MobileNew" in ReKycDetails {
								"Mobile"
								} else {
								""
									},
								size: 10pt,
								weight:"bold"
							)
						#h(78pt) #text(
								if "MobileNew" in ReKycDetails and ReKycDetails.MobileNew  != "" {
								ReKycDetails.MobileNew
								} else {
								""
									},
								size: 10pt
							)
						#h(190pt) #text(
								if "MobileOld" in ReKycDetails and ReKycDetails.MobileOld  != "" {
								ReKycDetails.MobileOld
								} else {
								""
									},
								size: 10pt
							)
				]
				}else{
					none
				}

				#v(10pt)

							#box(
							fill: rgb("#d3d3d3"), 
							stroke: 1pt,
							inset: 0pt,
							[
								#grid(
								columns: (auto, 1fr),
								gutter: 0pt,
									[
									#v(8pt)
									// Heading text in white
								#h(8pt) #text(" Nominee Detail: ", weight: "bold", fill: black)
									#v(8pt)
								],
								)
							]
							)

				#for i in range(0, nominee.len()) [ 

					#h(8pt)
					#align(center)[
							#box(
							inset: 4pt,
							fill: rgb("#f0f0f0"),  // Light gray background (you can change it)
							radius: 2pt
							)[  
							#strong("Nominee Details " + str(i + 1))
							]
					]
					#v(2pt)

					//Added for data exist or not
					#let n =  nominee.at(i)

					#let NomName =(if "NomFirstName" in n { n.NomFirstName } else { "" } +if "NomMiddleName" in n and n.NomMiddleName != "" { " " + n.NomMiddleName } else { "" } +if "NomLastName" in n and n.NomLastName != "" { " " + n.NomLastName } else { "" })
					//#let NomDOB = if "NomineeDOB" in n {n.NomineeDOB} else {"--"}
          #let NomDOB = if "NomineeDOB" in n and n.NomineeDOB.len() == 8 {
            let d = n.NomineeDOB
            d.slice(6,8) + "/" + d.slice(4,6) + "/" + d.slice(0,4)
          } else { "--" }
					#let fullAddress = (if "NomAddress1" in n { n.NomAddress1 } else { "--" }) + (if "NomAddress2" in n { ", " + n.NomAddress2 } else { "" }) +(if "NomAddress3" in n { ", " + n.NomAddress3 } else { "" })
					#let city = if "NomAddressCity" in n {n.NomAddressCity} else {"--"}
					#let pincode  = if "NomPincode" in n {n.NomPincode} else {"--"}
					#let state  = if "NomAddressState" in n {n.NomAddressState} else {"--"}
					#let contry  = if "NomAddressCountry" in n {n.NomAddressCountry} else {"--"}
					#let email = if "NomEmail" in n {n.NomEmail} else {"--"}
					//#let dob  = if "NomineeDOB" in n {n.NomineeDOB} else {"--"}
          #let dob = if "NomineeDOB" in n and n.NomineeDOB.len() == 8 {
            let d = n.NomineeDOB
            d.slice(6,8) + "/" + d.slice(4,6) + "/" + d.slice(0,4)
          } else { "--" }
					#let relation  = if "NomRelation" in n {n.NomRelation} else {"--"}
					#let percentage  = if "NomPercentage" in n {n.NomPercentage} else {"--"}
					#let uid  = if "NomineeUID" in n {n.NomineeUID} else {"--"}
					#let pan  = if "NomineePAN" in n {n.NomineePAN} else {"--"}
					#let mobile  = if "NomMobile" in n {n.NomMobile} else {"--"}
					#let checkbox(checked) = if checked == "true" {
						// Checked box
						raw("[✓]")
								} else {
						// Empty box
						raw("[ ]")
						}

					//Printed from here
					#h(8pt)#text("Name : ",weight:"bold")
					#h(8pt)#text(NomName)
					#v(1pt)
					#h(8pt)#text("Address : ",weight:"bold")
					#h(8pt)#text(fullAddress)
					#v(1pt)
					#h(8pt)#text("City : ",weight:"bold")
					#h(2pt)#text(city)
					#h(20pt)#text("Pincode : ",weight:"bold")
					#h(2pt)#text(pincode)
					#h(20pt)#text("State : ",weight:"bold")
					#h(2pt)#text(state)
					#h(20pt)#text("Country : ",weight:"bold")
					#h(2pt)#text(contry)
					#v(1pt)
					#h(8pt)#text("Date Of Birth : ",weight:"bold")
					#h(2pt)#text(dob)
					#h(20pt)#text("Share: ",weight:"bold")
					#h(2pt)#text(percentage)
					#h(20pt)#text("Relation : ",weight:"bold")
					#h(2pt)#text(relation)
					#h(20pt)#text("Mobile : ",weight:"bold")
					#h(2pt)#text(mobile)
					#v(1pt)
					#h(8pt)#text("UID : ",weight:"bold")
					#h(2pt)#text(uid)
					#h(20pt)#text("PAN : ",weight:"bold")
					#h(2pt)#text(pan)
					#h(20pt) #strong("Residual Securities: ") #checkbox(nominee.at(i).NomineeResidualSecurities)
					#v(1pt)
					#h(8pt)#text("Email : ",weight:"bold")
					#h(2pt)#text(email)

					//Guardian Details
					// Check and show GuardianDetails if exists
				
						#if "GuardianDetails" in nominee.at(i) and nominee.at(i).GuardianDetails != none {
							let g = nominee.at(i).GuardianDetails
									
							//g.NomGuardianFirstName
							//Added for data exist or not
							//let  gurdName = if "NomGuardianFirstName" in g { g.NomGuardianFirstName + " " +g.NomGuardianMiddleName +" "+g.NomGuardianLastName  } else { "--" }
							 
               let gurdName = (if "NomGuardianFirstName" in g or "NomGuardianMiddleName" in g or "NomGuardianLastName" in g {(if "NomGuardianFirstName" in g { g.NomGuardianFirstName } else { "" }) + (if "NomGuardianMiddleName" in g and g.NomGuardianMiddleName != "" { " " + g.NomGuardianMiddleName } else { "" }) + (if "NomGuardianLastName" in g { " " + g.NomGuardianLastName } else { "" })} else {"--"})

              let  fullAddress = (if "NomGuardianAddress1" in g { g.NomGuardianAddress1 } else { "--" }) + (if "NomGuardianAddress2" in g { ", " + g.NomGuardianAddress2 } else { "" }) +(if "NomGuardianAddress3" in g { ", " + g.NomGuardianAddress3 } else { "" })
							let  city = if "NomGuardianCity" in g { g.NomGuardianCity } else { "--" }
							let  pincode = if "NomGuardianPincode" in g { g.NomGuardianPincode } else { "--" }
							let  state = if "NomGuardianState" in g { g.NomGuardianState } else { "--" }
							let  Contry = if "NomGuardianCountry" in g { g.NomGuardianCountry } else { "--" }
							//let  gurdDOB = if "NomGuardianDob" in g { g.NomGuardianDob } else { "--" }
              let gurdDOB = if "NomGuardianDob" in g and g.NomGuardianDob.len() == 8 {
              let d = g.NomGuardianDob
              d.slice(6,8) + "/" + d.slice(4,6) + "/" + d.slice(0,4)
              } else { "--" }
							let  adhar = if "NomGuardianUID" in g { g.NomGuardianUID } else { "--" }
							let  pan = if "NomGuardianPAN" in g { g.NomGuardianPAN } else { "--" }
							let  relation = if "NomGuardianRelation" in g { g.NomGuardianRelation } else { "--" }
							let  email = if "NomGuardianEmail" in g { g.NomGuardianEmail } else { "--" }

							//printed here
							[
							#align(center)[
									#box(
										inset: 4pt,
										fill: rgb("#f0f0f0"),  // Light gray background (you can change it)
										radius: 2pt
									)[  
										#strong("Guardian Details " + str(i + 1))
									]
									]
							]
							
							//text("   Name : ", weight: "bold") + text(gurdName) + linebreak() + v(1pt)
              text("Name : ", weight: "bold") + text(gurdName) + linebreak() + v(1pt)
							text("   Address : ", weight: "bold") + text(fullAddress) + linebreak() + v(1pt)
							text("   City : ", weight: "bold") + text(city) + text("      Pincode: ", weight: "bold") + text(pincode) + linebreak() + v(1pt)
							text("   State : ", weight: "bold") + text(state) + text("      Country: ", weight: "bold") + text(Contry) + linebreak() + v(1pt)
							text("   Date of Birth : ", weight: "bold") + text(gurdDOB) + linebreak() + v(1pt)
							text("   Aadhaar : ", weight: "bold") + text(adhar) + text("      PAN: ", weight: "bold") + text(pan) + linebreak() + v(1pt)
							text("   Email : ", weight: "bold") + text(email)
				

				} 
					#v(10pt)  
				]



				//Bank Details

				#v(10pt)

					#box(
							fill: rgb("#d3d3d3"), 
							stroke: 1pt,
							inset: 0pt,
							[
								#grid(
								columns: (auto, 1fr),
								gutter: 0pt,
									[
									#v(8pt)
									// Heading text in white
								#h(8pt) #text(" Bank Detail: ", weight: "bold", fill: black)
									#v(8pt)
								],
					)
							]
					)


					#for i in range(0, bank.len()) [ 

							#align(center)[
							#box(
							inset: 4pt,
							fill: rgb("#f0f0f0"),  // Light gray background (you can change it)
							radius: 2pt
							)[  
							#strong("Bank Details " + str(i + 1))
							]
						]

						#let b =  bank.at(i)
					#let acc = if "BankAccNo" in b {b.BankAccNo} else {"--"}
					#let name = if "BankName" in b {b.BankName} else {"--"}
					#let ifsc = if "BankIFSC" in b {b.BankIFSC} else {"--"}
					#let micr = if "BankMICR" in b {b.BankMICR} else {"--"}
					#let type  = if "AccountType" in b {b.AccountType} else {"--"}
					#let checkbox(checked) = if checked == "true" {
						// Checked box
						raw("[✓]")
								} else {
						// Empty box
						raw("[ ]")
						}


					//printed here
					#h(8pt)#text("Bank Account : ",weight:"bold")
					#h(4pt)#text(acc)
					#h(35pt)#text("Bank Name : ",weight:"bold")
					#h(4pt)#text(name)
					#h(35pt)#text("Bank IFSC : ",weight:"bold")
					#h(4pt)#text(ifsc)
					#v(4pt)
					#h(8pt)#text("Bank MICR : ",weight:"bold")
					#h(4pt)#text(micr)
					#h(50pt)#text("Bank Type : ",weight:"bold")
					#h(4pt)#text(type)
					#h(50pt) #strong("Default: ") #checkbox(bank.at(i).IsDefault)
					]


				//Demat Details

				#v(10pt)

					#box(
							fill: rgb("#d3d3d3"), 
							stroke: 1pt,
							inset: 0pt,
							[
								#grid(
								columns: (auto, 1fr),
								gutter: 0pt,
									[
									#v(8pt)
									// Heading text in white
								#h(8pt) #text(" Demat Detail: ", weight: "bold", fill: black)
									#v(8pt)
								],
					)
							]
					)


					#for i in range(0, demat.len()) [ 
				
							#align(center)[
							#box(
							inset: 4pt,
							fill: rgb("#f0f0f0"),  // Light gray background (you can change it)
							radius: 2pt
							)[  
							#strong("Demat Details " + str(i + 1))
							]
						]
				
						#let d =  demat.at(i)
					#let acc = if "DPAcNo" in d {d.DPAcNo} else {"--"}
					#let id = if "DPID" in d {d.DPID} else {"--"}
					#let type = if "DematAccountType" in d {d.DematAccountType} else {"--"}
					#let checkbox(checked) = if checked == "true" {
						// Checked box
						raw("[✓]")
						} else {
						// Empty box
						raw("[ ]")
										}

				
					//printed here
					#h(8pt)#text("Demat Account : ",weight:"bold")
					#h(4pt)#text(acc)
					#h(35pt)#text("DPID : ",weight:"bold")
					#h(4pt)#text(id)
					#h(35pt)#text("Demat Type : ",weight:"bold")
					#h(4pt)#text(type)
					#h(35pt) #strong("Default: ") #checkbox(demat.at(i).IsDefault)
					]

					#v(15pt)

   //]
  //)

				//Segment Details

						#box(
							fill: rgb("#d3d3d3"), 
							stroke: 1pt,
							inset: 0pt,
							[
								#grid(
								columns: (auto, 1fr),
								gutter: 0pt,
									[
									#v(8pt)
									// Heading text in white
								#h(8pt) #text(" Segment Details: ", weight: "bold", fill: black)
									#v(8pt)
								],
					)
							]
					)


					#v(10pt)

						//Segment Details
				
							#let SegmentDetails = ReKycDetails.SegmentDetails

							#table(
							columns: (2fr, 1fr),
							align: (left, center),
							stroke: (.5pt, gray),
							inset: 4pt,
							table.header(
								text("Segment Exchange", weight: "bold"),
								text("Checked/Unchecked", weight: "bold")
							),

							//Proper looping with no extra brackets
							..for s in SegmentDetails {
								( text(s.SegmentExch), if s.IsSelect == "true" { text("☑") } else { text("☐") } )
							}
							)


//Attachements

#for item in Attachment {
	if item.base64Image !="" and item.ImageType !="Photo_Img" {
	    if item.base64Image == ".pdf" {
		   let filePath = "../Assets/" + item.ImageType + "_" + ReKycDetails.ClientCode + item.base64Image
		   let data = read(filePath, encoding: none)
		   pagebreak()
		   muchpdf(data)

		 //"pdf " + item.ImageType
		  //line()
	    }
	    else {
		  let filePath = "../Assets/" + item.ImageType + "_" + ReKycDetails.ClientCode + item.base64Image
		  pagebreak()
		  image(filePath, width: 100%)
	    } 
	}
}

  
  //#muchpdf(data, width: 10cm,height:20cm, scale: 1.5, pages: (0, 2, 4))

//#let data = read("../Assets/NomineeAttach", encoding: none)

//#muchpdf(data,  pages:(0,2))