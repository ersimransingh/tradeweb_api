#let muchpdf = {
  let muchpdf-plugin = plugin("muchpdf.wasm")

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
#let company = data.at(0)
#let Attachment = company.Attachments

// Page setup
#set page(
  width: 240mm,
  height: 400mm,
  margin: (top: 1cm, bottom: 2cm, x: 2cm)
)


#set text(
  size: 9pt,
  font: "ArialMT",
  weight: "regular",
  style: "normal"
)

#let photoPath = ""
#let signPath = ""

#for item in Attachment {
	if item.base64Image !="" {
	    if item.ImageType == "Photo_Img"  {
		   photoPath = "../Assets/" + item.ImageType + "_" + company.ClientCode + item.base64Image
		 }
        else if item.ImageType == "Signature_Img" {
		   signPath = "../Assets/" + item.ImageType + "_" + company.ClientCode + item.base64Image
		 }		 
	}
}

// ==== KYC Box Layout ====
#box(
  stroke: 1pt,
  inset: 0pt,
  [

    #grid(
      columns: (3.6fr, 6fr),
      gutter: 0pt,

      // === Left side: Instructions
      [
        #box(
          fill: rgb(246, 160, 160),
          inset: 6pt,
          [
            #text(weight:"bold", "Know Your Client (KYC)")
            #linebreak()
			#linebreak()
            #text(weight:"bold", "Application Form (For Individual only)")
            #linebreak()
            #linebreak()
			#line(length: 72mm, stroke: 1pt)
            #text(size: 6.5pt, "Please fill the form in ENGLISH and in BLOCK letters")
            #linebreak()
            #text(size: 6.5pt, "Fields marked with '*' are mandatory")
			#linebreak()
            #text(size: 6.5pt, "Fields marked '*' are pertaining to CKYC and mandatory only if processing CKYC also")
          ]
        )
      ],

      // === Right side: Logo + App Info
      [
        #box(
          inset: 10pt,
          [
            #block(spacing: 6pt)[
              // Logo & Company Name
              #grid(
                columns: (1fr, 3fr),
                gutter: 6pt,
                [
                  //#text("[Logo]", size: 12pt)
				  #image("../Assets/CompanyLogo.png", width: 100%)
                ],
                [
                  #text(company.CompanyName, size: 12pt)
                ]
             )
              // App No.
              #text("Application Number: " + company.ApplicationNumber)

              // App Type
              #text("Application Type:")
              #text(
                if company.ApplicationType == "New KYC" { "☑ New KYC" } else { "☐ New KYC" }
              )
              #text(
                if company.ApplicationType == "Modification" { "☑ Modification" } else { "☐ Modification" }
              )
            ]
          ]
        )
      ]
    )
	 #v(-10pt)
 #line(length: 200mm, stroke: 1pt)
        //Kyc Mode
      #text(weight:"bold", " KYC Mode*:") #text(size: 6.5pt, " (Please tick ✔)")  
      #v(1pt)
     #block(spacing: 10pt)[
		  #text((if company.KYCMode == "Normal" { " ☑" } else { " ☐" }) + " Normal ")
		  #h(8pt)
		  #text((if company.KYCMode == "EKYC OTP" { " ☑" } else { " ☐" }) + " EKYC OTP ")
		  #h(8pt)
		  #text((if company.KYCMode == "EKYC Biometric" { " ☑" } else { " ☐" }) + " EKYC Biometric ")
		  #h(8pt)
		  #text((if company.KYCMode == "Online KYC" { " ☑" } else { " ☐" }) + " Online KYC ")
		  #h(8pt)
		  #text((if company.KYCMode == "Offline KYC" { " ☑" } else { " ☐" }) + " Offline KYC ")
		  #h(8pt)
		  #text((if company.KYCMode == "Digilocker" { " ☑" } else { " ☐" }) + " Digilocker")
      ]
        #v(8pt)

#box(
  fill: rgb(246, 160, 160),
  stroke: 1pt,
  inset: 0pt,
  [
    #grid(
      columns: (auto, 1fr),
      gutter: 0pt,
          [
          #v(8pt)
          #text(weight:"bold", " 1.Identity Details ") #text(size:7pt, "(Please refer gudelines overleaf)", fill: black)
          #v(8pt)
		  ],
		)
	  ]
	)
  

  #grid(
	columns: (3.5fr, 6fr),
	gutter: 3pt,
	[
	#h(8pt)#text("PAN*:")

	#h(8pt)#text("Name (Same as ID proof)*: ")

	#h(8pt)#text("Maiden Name: ")

	#h(8pt)#text("Father/Spouse Name*: *: ")

	#h(8pt)#text("Date of Birth:*")

	#h(8pt)#text("Gender*: ")  
	
    #h(8pt)#text("Marital Status*: ")  

    #h(8pt)#text("Nationality*: ") 
	
    #h(8pt)#text("Residential Status*: ")
  
    #h(8pt)#text(size:7pt, "Please tick ✔)*: ")


],
[
////////// ***********  Data Values ********
  #underline([
    #text(company.PAN)
	])
	#text(size:7pt, "                   Please enclose a duly attested copy of your PAN card")
	
  	#underline([
		#text(company.ClientName) #text("              ")
	  ])
  
	 #underline([
		#text(company.MaidenName) #text("                                         ")
	  ])
	
	 #underline([
		#text(company.FatherSpouseName) #text("               ")
	  ])

	 #underline([
		#text(company.DOB ) #text("                   ")
	  ])

		  #text((if company.Gender == "Male" { "☑" } else { "☐" }) + " Male")
		  #h(10pt)#text((if company.Gender == "Female" { "☑" } else { "☐" }) + " Female")
		  #h(10pt)#text((if company.Gender == "Transgender" { "☑" } else { "☐" }) + " Transgender")


		  #text((if company.MaritalStatus == "Single" { "☑" } else { "☐" }) + " Single")
		  #h(10pt)#text((if company.MaritalStatus == "Married" { "☑" } else { "☐" }) + " Married")


		  #text((if company.Nationality == "Indian" { "☑" } else { "☐" }) + " Indian")
		  #h(10pt)#text((if company.Nationality == "Other" { "☑" } else { "☐" }) + " Other")


		  #text((if company.ResidentialStatus == "Resident Individual" { "☑" } else { "☐" }) + " Resident Individual")
		  #h(3pt)#text((if company.ResidentialStatus == "Non Resident Indian" { "☑" } else { "☐" }) + " Non Resident Indian")

		  #text((if company.ResidentialStatus == "Foreign National" { "☑" } else { "☐" }) + " Foreign National")
		  #h(3pt)#text((if company.ResidentialStatus == "Person of Indian Origin" { "☑" } else { "☐" }) + " Person of Indian Origin")
   
#align(top)[
		#align(right)[
	  #v(-140pt) // aligns with DOB row
	  #box(
		stroke: 1pt,
		width: 4cm,
		inset: 0pt,
		[
			#box(
			  width: 3.9cm,
			  height: 4.5cm,
			  inset: 2pt,
			  stroke: none,
			  [
				#align(center)[
				   #if photoPath != "" {
					  image(photoPath, width: 100%, height: 100%)
				    }
				  ]
			    ]
			  )			  
			]
		  )
		  #v(5pt)
		  #text(size:7pt, "Cross Signature across photograph  ")
		]
	  ]

]
)

#v(10pt)
#h(8pt)#text(" Proof of Identity(POI) submitted for PAN exempted cases (plesae tick)")
#v(5pt)

  #grid(
	columns: (3.5fr, 4fr, 4fr),
	gutter: 3pt,
	[
	  #h(8pt)#text((if company.AadhaarNumber != "" { "☑" } else { "☐" }) + " A - Aadhaar Card   ")

	  #h(8pt)#text((if company.PassportNumber != "" { "☑" } else { "☐" }) + " B - Passport Number   ")
	 
	  #h(8pt)#text((if company.VoterID != "" { "☑" } else { "☐" }) + " C - Voter ID Card   ")

	  #h(8pt)#text((if company.DrivingLicence != "" { "☑" } else { "☐" }) + " D - Driving License   ")

	  #h(8pt)#text((if company.JobCard != "" { "☑" } else { "☐" }) + "  E - NREGA Job Card")
	  
	  #h(8pt)#text((if company.NPR != "" { "☑" } else { "☐" }) + "  F - NPR ")

	  #h(8pt)#text((if company.Others != "" { "☑" } else { "☐" }) + " Z - Others")

	  #h(8pt)#text("         Identification Number")
    
  ],
  [
  #h(8pt)  #underline([
    #text(
       if company.AadhaarNumber != "" {
          company.AadhaarNumber + "                       "  // ← Add more spaces here
		} else {
		  "                           		           "  // ← Match length for empty case
		},
		)
	  ])
  
 #h(8pt) #underline([
    #text(
       if company.PassportNumber != "" {
		  company.PassportNumber + "                       "  // ← Add more spaces here
		} else {
		  "                 		                     "  // ← Match length for empty case
		},
		)
	  ])

 #h(8pt) #underline([
    #text(
       if company.VoterID != "" {
		  company.VoterID + "                       "  // ← Add more spaces here
		} else {
		  "              		                        "  // ← Match length for empty case
		},
		)
	  ])

 #h(8pt) #underline([
    #text(
       if company.DrivingLicence != "" {
		  company.DrivingLicence + "                       "  // ← Add more spaces here
		} else {
		  "                   		                   "  // ← Match length for empty case
		},
		)
	  ])
  
    #h(8pt)#underline([
    #text(
       if company.JobCard != "" {
		  company.JobCard + "                       "  // ← Add more spaces here
		} else {
		  "                                      "  // ← Match length for empty case
		},
		)
	  ])

    #h(8pt)#underline([
    #text(
       if company.NPR != "" {
		  company.NPR + "                       "  // ← Add more spaces here
		} else {
		  "                                      "  // ← Match length for empty case
		},
		)
	  ])
  
    #h(8pt)#underline([
    #text(
       if company.Others != "" {
		  company.Others + "                       "  // ← Add more spaces here
		} else {
		  "                                      "  // ← Match length for empty case
		},
		)
	  ])
	  
     #h(8pt)#underline([
     #text(
       if company.IDNumber != "" {
		  company.IDNumber + "                       "  // ← Add more spaces here
		} else {
		  "                                      "  // ← Match length for empty case
		},
		)
	  ])
  ],
  [
	  #h(8pt) #text(" 									    ")
	  #h(8pt) #text(size:7.5pt, " (Expire Date) ")
    #underline([
    #text(
       if company.PassExpireDate != "" {
		  company.PassExpireDate + "                       "  // ← Add more spaces here
		} else {
		  "                                      "  // ← Match length for empty case
		},
		)
	  ])

	#h(8pt) #text("    										 ")
	#h(8pt) #text(size:7.5pt, " (Expire Date) ")
    #underline([
    #text(
       if company.DriveExpireDate != "" {
		  company.DriveExpireDate + "                       "  // ← Add more spaces here
		} else {
		  "                                      "  // ← Match length for empty case
		},
		)
	  ])
	#h(8pt) #text("   										  ")
	#h(8pt) #text(" 								   	      ")
	#h(8pt) #text("    										  ")
	#h(8pt) #text("   										  ")
	#h(8pt) #text("   										  ")
	#h(16pt) #text(size:7pt, "(Any document notified by Central goverment)")

  ]
  )
#v(4pt)

//Address Details:- 

#box(
  fill: rgb(246, 160, 160),
  stroke: 1pt,
  inset: 0pt,
  [
    #grid(
      columns: (auto, 1fr),
      gutter: 0pt,
          [
          #v(8pt)
        // Heading text in white
        #text(" 2.Address Details (Please refer gudelines overleaf)", fill: black, weight: "bold")
          #v(8pt)
      ],

    )
  ]
)
#v(1pt)
#h(2pt)#text(" A.Correspondence/ Local Address*", weight: "bold")
#v(5pt)
 #h(8pt)#text(" Line1*")
  #h(8pt)#underline([
    #text(
       if company.Address1 != "" {
      company.Address1 + "                                                            "  // ← Add more spaces here
    } else {
      "                                                                                "  // ← Match length for empty case
    },

    )
  ])
#v(6pt)

   #h(8pt)#text(" Line2*")
  #h(8pt)#underline([
    #text(
       if company.Address2 != "" {
      company.Address2 + "                                                            "  // ← Add more spaces here
    } else {
      "                                                                                "  // ← Match length for empty case
    },

    )
  ])
#v(6pt)

   #h(8pt)#text(" Line3*")
  #h(8pt)#underline([
    #text(
       if company.Address3 != "" {
      company.Address3 + "                                                            "  // ← Add more spaces here
    } else {
      "                                                                                "  // ← Match length for empty case
    },

    )
  ])
#v(6pt)


 #h(8pt)#text("City/Town/Village")
  #h(8pt)#underline([
    #text(
       if company.City != "" {
      company.City + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },

    )
  ])#h(24pt)#text("Pincode",weight: "bold")
    #h(8pt)#underline([
    #text(
       if company.Pincode != "" {
      company.Pincode + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])

#v(8pt)

 #h(8pt)#text("State*")
  #h(8pt)#underline([
    #text(
       if company.State != "" {
      company.State + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },

    )
  ])#h(24pt)#text("Country",weight: "bold")
    #h(8pt)#underline([
    #text(
       if company.Country != "" {
      company.Country + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])

#v(8pt)


  //Adderess Type
    
     #block(spacing: 8pt)[
      #h(8pt)#text("Address Type*")#h(5pt)
      #text((if company.AddressType == "ResiBusi" { "☑" } else { "☐" }) + " Residential/Business"),
      #h(8pt)
      #text((if company.AddressType == "Residential" { "☑" } else { "☐" }) + " Residential"),
      #h(8pt)
      #text((if company.AddressType == "Business" { "☑" } else { "☐" }) + " Business"),
      #h(8pt)
      #text((if company.AddressType == "RegiOffice" { "☑" } else { "☐" }) + " Registered Office"),
      #h(8pt)
      #text((if company.AddressType == "Unspecified" { "☑" } else { "☐" }) + " Unspecified")
      #h(8pt)
      ]
        #v(8pt)

  //Applicant esign
#grid(
	  columns: (6fr, .2fr, 3fr), // line on left, heading on right
	  gutter: 0pt,
	  [
		// Line spanning to the left of the heading
		#line(length: 200mm, stroke: 1pt)

	  ],
	  [
		#v(3.5pt)
		#rotate(90deg)[
			  #line(length: 3cm, stroke: 1pt)
			]
	  ],
	  [
		#box(
		  fill: rgb(246, 160, 160),
		  inset: 4.5pt,
		  width: 185pt,
		  [
			#text(" Applicant e-SIGN ", size: 10pt)
		  ]
		)
	  ]
	)
	#v(65pt)
  ]
)


//Second Page


#pagebreak()
#box(
    stroke: 1pt,
  inset: 0pt,
  [
    #v(8pt)
#h(0pt)#text(" B. Permanant Residence address of applicant, if different of above A / Oversease Address*(Mandatory for NRI Applicant)",weight: "bold")
#v(6pt)
 #h(8pt)#text(" Line1*")
  #h(8pt)#underline([
    #text(
       if company.Address1 != "" {
      company.Address1 + "                                                            "  // ← Add more spaces here
    } else {
      "                                                                                "  // ← Match length for empty case
    },

    )
  ])
#v(6pt)

   #h(8pt)#text(" Line2*")
  #h(8pt)#underline([
    #text(
       if company.Address2 != "" {
      company.Address2 + "                                                            "  // ← Add more spaces here
    } else {
      "                                                                                "  // ← Match length for empty case
    },

    )
  ])
#v(6pt)

   #h(8pt)#text(" Line3*")
  #h(8pt)#underline([
    #text(
       if company.Address3 != "" {
      company.Address3 + "                                                            "  // ← Add more spaces here
    } else {
      "                                                                                "  // ← Match length for empty case
    },

    )
  ])
#v(6pt)


 #h(8pt)#text("City/Town/Village")
  #h(8pt)#underline([
    #text(
       if company.City != "" {
      company.City + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },

    )
  ])#h(24pt)#text("Pincode",weight: "bold")
    #h(8pt)#underline([
    #text(
       if company.Pincode != "" {
      company.Pincode + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])

#v(8pt)

 #h(8pt)#text("State*")
  #h(8pt)#underline([
    #text(
       if company.State != "" {
      company.State + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },

    )
  ])#h(24pt)#text("Country",weight: "bold")
    #h(8pt)#underline([
    #text(
       if company.Country != "" {
      company.Country + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])

#v(8pt)

  //Adderess Type
     #block(spacing: 8pt)[
      #h(8pt)#text("Address Type*")#h(5pt)
      #text((if company.AddressType == "ResiBusi" { "☑" } else { "☐" }) + " Residential/Business"),
      #h(8pt)
      #text((if company.AddressType == "Residential" { "☑" } else { "☐" }) + " Residential"),
      #h(8pt)
      #text((if company.AddressType == "Business" { "☑" } else { "☐" }) + " Business"),
      #h(8pt)
      #text((if company.AddressType == "RegiOffice" { "☑" } else { "☐" }) + " Registered Office"),
      #h(8pt)
      #text((if company.AddressType == "Unspecified" { "☑" } else { "☐" }) + " Unspecified")
      #h(8pt)
      ]
        #v(8pt)

// Line spanning to the left of the heading

    #line(length: 200mm, stroke: .5pt)
      #v(2pt)


  //BreakDown page

#h(8pt)#text(weight:"bold", "Proof of Address*") #text(size: 7pt, " (attested copy of any 1 POA for correspondence and permanent address each to be submitted)")
  #v(6pt)

  #grid(
	columns: (3.5fr, 4fr, 4fr),
	gutter: 3pt,
	[
	  #h(8pt)#text((if company.AadhaarNumber != "" { "☑" } else { "☐" }) + " A - Aadhaar Card   ")

	  #h(8pt)#text((if company.PassportNumber != "" { "☑" } else { "☐" }) + " B - Passport Number   ")
	 
	  #h(8pt)#text((if company.VoterID != "" { "☑" } else { "☐" }) + " C - Voter ID Card   ")

	  #h(8pt)#text((if company.DrivingLicence != "" { "☑" } else { "☐" }) + " D - Driving License   ")

	  #h(8pt)#text((if company.JobCard != "" { "☑" } else { "☐" }) + "  E - NREGA Job Card")
	  
	  #h(8pt)#text((if company.NPR != "" { "☑" } else { "☐" }) + "  F - NPR ")

	  #h(8pt)#text((if company.Others != "" { "☑" } else { "☐" }) + " Z - Others")

	  #h(8pt)#text("         Identification Number")
    
  ],
  [
  #h(8pt)  #underline([
    #text(
       if company.AadhaarNumber != "" {
      company.AadhaarNumber + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])

 #h(8pt) #underline([
    #text(
       if company.PassportNumber != "" {
      company.PassportNumber + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])

 #h(8pt) #underline([
    #text(
       if company.VoterID != "" {
      company.VoterID + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])

 #h(8pt) #underline([
    #text(
       if company.DrivingLicence != "" {
      company.DrivingLicence + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
	  ])
  
    #h(8pt)#underline([
    #text(
       if company.JobCard != "" {
		  company.JobCard + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])

   #h(8pt)#underline([
    #text(
       if company.NPR != "" {
		  company.NPR + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])
  
    #h(8pt)#underline([
    #text(
       if company.Others != "" {
      company.Others + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
	  ])

    #h(8pt)#underline([
    #text(
       if company.IDNumber != "" {
      company.IDNumber + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])
  ],
  [
	  #h(8pt) #text("     ")
	  #h(16pt) #text(size:7.5pt, " Expire Date ")
    #underline([
    #text(
       if company.PassExpireDate != "" {
		  company.PassExpireDate + "                       "  // ← Add more spaces here
		} else {
		  "                                      "  // ← Match length for empty case
		},
		)
	  ])

	#h(8pt) #text("     ")
	#h(16pt) #text(size:7.5pt, " Expire Date ")
    #underline([
    #text(
       if company.DriveExpireDate != "" {
		  company.DriveExpireDate + "                       "  // ← Add more spaces here
		} else {
		  "                                      "  // ← Match length for empty case
		},
		)
	  ])
	#h(8pt) #text("     ")
	#h(8pt) #text("     ")
	#h(16pt) #text(size:7pt, "(Any document notified by Central goverment)")

  ]
  )

//Contact Details

#box(
  fill: rgb(246, 160, 160),
  stroke: 1pt,
  inset: 0pt,
  [
    #grid(
      columns: (auto, 1fr),
      gutter: 0pt,
          [
          #v(8pt)
        // Heading text in white
        #text(" 3.Contact Details(in captial)", fill: black)
          #v(8pt)
      ],
    )
  ]
)
#v(8pt)

#h(8pt)#text("Email*: ")
 #underline([
    #text(
       if company.Email != "" {
      company.Email + "                                            "  // ← Add more spaces here
    } else {
      "                                                              "  // ← Match length for empty case
    },
    )
  ])
#v(6pt)

#h(8pt)#text("Mobile Number*: ")
 #underline([
    #text(
       if company.Mobile != "" {
      company.Mobile + "                                            "  // ← Add more spaces here
    } else {
      "                                                              "  // ← Match length for empty case
    },
    )
  ])
#v(6pt)


 #h(8pt)#text("Tele(off)")
  #underline([
    #text(
       if company.TelephoneOff != "" {
      company.TelephoneOff + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])#h(24pt)#text("Tell(On)",weight: "bold")
    #underline([
    #text(
       if company.TelephoneOn != "" {
      company.TelephoneOn + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])


#v(8pt)

//Applicant Declaration

#box(
  fill: rgb(246, 160, 160),
  stroke: 1pt,
  inset: 0pt,
  [
    #grid(
      columns: (auto, 1fr),
      gutter: 0pt,
      [
        #v(8pt)
        #text(" 4. Applicant Declaration          ", fill: black)
        #v(8pt)
      ]
    )
  ]
)
 #v(-10pt)
// Three-column layout with vertical lines
#grid(
  columns: (1.5fr, 1pt, 1fr, 1pt, 1fr), // Added 1pt columns for vertical lines
  gutter: 0pt,
  // First column
  [
    #box(
      inset: 6pt,
      [
        #text(size:6.5pt, "I/We hereby declare that the KYC details furnished by me are true and correct to the best of my/our knowledge and belief and I/we under-take to inform you of any changes therein, immediately. In case any of the above information is found to be false or untrue or misleading or misrepresenting, I am/We are aware that I/We may be held liable for it.")
		#text(size:6.5pt, "\nI/We hereby consent to receiving information from CVL KRA through SMS/Email on the above registered number/Email address.")
		#text(size:6.5pt, "\nI am/We are also aware that for Aadhaar OVD based KYC, my KYC request shall be validated against Aadhaar details. I/We hereby consent to sharing my/our masked Aadhaar card with readable QR code or my Aadhaar XML/Digilocker XML file, along with passcode and as applicable, with KRA and other Intermediaries with whom I have a business relationship for KYC purposes only.")      
	      
		#v(6pt)
		#h(8pt)
		#text(" Date: ")
        #underline([
          #text(
            if company.TodayDate != "" {
              company.TodayDate + "                      "
            } else {
              "                                          "
            }
          )
        ])
        #v(6pt)
        #h(8pt)
		#text(" Place: ")
        #underline([
          #text(
            if company.Branch != "" {
              company.Branch + "                         "
            } else {
              "                                          "
            }
          )
        ])
      ]
    )
	 #v(-12pt)
  ],
  
  // First vertical line
  [
    #line(
      start: (0pt, 0pt),
      end: (0pt, 185pt),
      stroke: 0.5pt + black
    )
  ],

  // Second column
  [
  
    #box(
	  fill: rgb(243, 243, 243),
	  inset: 6pt,
      [
        #align(center)[
          #h(50mm)
          #text("Applicant e-SIGN")
        ]
       
      ]
    )
	#v(-10pt)
	#line(length: 100mm, stroke: .5pt)
  ],
  
  // Second vertical line
  [
    #line(
      start: (0pt, 0pt),
      end: (0pt, 185pt),
      stroke: 0.5pt + black
    )
  ],
  
  // Third column
  [
    #box(
	  fill: rgb(243, 243, 243),
	  inset: 6pt,
	  [
        #align(center)[
          #h(50mm)
          #text("Applicant Wet Esign")
        ]
      ]
    )
	#v(10pt)#h(10pt)
	#box(
	  width: 4cm,
	  height: 1.4cm,
	  inset: 2pt,
		  [
			 #if signPath != "" {
					    image(signPath, width: 100%, height: 100%)
					}
		  ]
       )
   ]
)

//For office use only
#box(
  fill: rgb(246, 160, 160),
  stroke: 1pt,
  inset: 0pt,
  [
    #grid(
      columns: (auto, 1fr),
      gutter: 0pt,
      [
        #v(8pt)
        #text(weight:"bold", " 5. For Office Use Only          ", fill: black)
        #v(8pt)
      ]
    )
  ]
)
  #v(-10pt)
  #grid(
  columns: (3fr, 1pt, 3fr), // Added 1pt columns for vertical lines
  gutter: 0pt,
  // First column
   [
    #box(
	  fill: rgb(246, 160, 160),
	  stroke: 0pt,
      inset: 6pt,
      [
        #align(center)[
           #text(" In-Person Verification (IPV) carried out by* ")
        ]    
      ]
    )
	#v(-10pt)
	#line(length: 200mm, stroke: .5pt)
	#v(10pt)
	// IPV Date
        #h(8pt)#text(" IPV Date:")
        #h(8pt)#underline([
          #text(if company.IPVDate != "" { company.IPVDate } else { " " })
        ])
        #v(5pt)

        // EMP Name
        #h(8pt)#text(" EMP Name:")
        #h(5pt)#underline([
          #text(if company.EMPAdmin != "" { company.EMPAdmin } else { "          " })
        ])

        // EMP Code
        #h(8pt)#text(" EMP Code:")
        #h(8pt)#underline([
          #text(if company.EMPCode != "" { company.EMPCode } else { "             " })
        ])

        // Designation
        #h(8pt)#text(" Designation:")
        #h(8pt)#underline([
          #text(if company.Designation != "" { company.Designation } else { "           " })
        ])
		#v(30pt)
		#line(length: 200mm, stroke: .5pt)
		#v(10pt)
		#box(
		  inset: 6pt,
		  [
			#align(center)[
			  #h(50pt)
			   #text(" Employee Signature and Stamp ")
			]
		   
		  ]
		)
  ],
  
  // Second vertical line
  [
    #line(
      start: (0pt, 0pt),
      end: (0pt, 180pt),
      stroke: 0.5pt + black
    )
  ],
  
  // Third column
  [
    #box(
	  fill: rgb(246, 160, 160),
	  inset: 6pt,
	  stroke: 0pt,
      [
        #align(center)[
           #text(" Intermediary Details* ")
        ]
      ]
    )
 #v(10pt)
        // Self Certified
        #h(8pt)#text((if company.intermedetaryDetail == "OVD" { "☑" } else { "☐" }) + " Self Certified document copies received(OVD)")
        #v(6pt)

        // True Copies
        #h(8pt)#text((if company.intermedetaryDetail == "Attested" { "☑" } else { "☐" }) + " True copies of documents received(Attested)")
        #v(8pt)

        // AMC/Intermediary Name
        #h(8pt)#text("AMC/Intermediary Name:")
        #v(4pt)

        // Highlighted Box
        #h(8pt)#box(
          stroke: .5pt,
          inset: 6pt,
          [
		    #h(80mm)
            #text(company.AMCInterName)
          ]
        )
		//#line(length: 200mm, stroke: .5pt)
		#v(10pt)
		#box(
		  inset: 6pt,
		  [
			#align(center)[
			   #h(50pt)
			   #text(" Institution Name and Stamp ")
			]	   
		  ]
		)
   ]
)
  ]
    )



#pagebreak()

#box(
  fill: rgb(246, 160, 160),
  stroke: 1pt,
  inset: 0pt,
  [
    #grid(
      columns: (auto, 1fr),
      gutter: 0pt,
      [
	  #align(center)[
        #v(8pt)
        #text(weight:"bold", " Instructions/Guidelines for filling Individual KYC Application Form ", fill: black)
        #v(4pt)
		]
		#line(length: 200mm, stroke: .5pt)
		#text( "  A. General Instructions: ", fill: black)
        #v(8pt)
      ]
    )
  ]
)



///// **** Added Attachments Logic ****

/*
#for item in Attachment {
	if item.base64Image !="" and item.ImageType !="Photo_Img" {
	    if item.base64Image == ".pdf" {
		   let filePath = "../Assets/" + item.ImageType + "_" + company.ClientCode + item.base64Image
		   let data = read(filePath, encoding: none)
		   pagebreak()
		   muchpdf(data)
	    }
	    else {
		  let filePath = "../Assets/" + item.ImageType + "_" + company.ClientCode + item.base64Image
		  pagebreak()
		  image(filePath, width: 100%)
	    } 
	}
}
*/