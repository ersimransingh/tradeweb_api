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
#let company = json(sys.inputs.at("file_path"))


// Page setup
#set page(
  width: 240mm,
  height: 400mm,
  margin: (top: 1cm, bottom: 2cm, x: 2cm)
)


// ==== KYC Box Layout ====
#box(
  stroke: 1pt,
  inset: 0pt,
  [

    #grid(
      columns: (1.2fr, 2fr),
      gutter: 0pt,

      // === Left side: Instructions
      [
        #box(
          fill: gray.lighten(60%),
          inset: 6pt,
          [
            #text("Know Your Client (KYC)", weight: "bold"),
            #linebreak()
            #text("Application Form (For Individual only)", weight: "bold"),
            #linebreak()
            #text("Please fill the form in English and in BLOCK LETTERS."),
            #linebreak()
            #text("Fields marked with '*' are mandatory.", fill: red)
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
                  #text(company.CompanyName, weight: "bold", size: 12pt)
                ]
             )
              // App No.
              #text("Application No.: " + company.ApplicationNumber, weight: "bold")

              // App Type
              #text("Application Type:", weight: "bold")
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

        //Kyc Mode
      #text(" KYC Mode* (Please tick ✔)", weight: "bold")
      #v(3pt)
     #block(spacing: 8pt)[
      #text((if company.KYCMode == "Normal" { "☑" } else { "☐" }) + "Normal"),
      #h(8pt)
      #text((if company.KYCMode == "eKYC OTP" { "☑" } else { "☐" }) + " eKYC OTP"),
      #h(8pt)
      #text((if company.KYCMode == "eKYC Biometric" { "☑" } else { "☐" }) + "eKYC Biometric"),
      #h(8pt)
      #text((if company.KYCMode == "Online KYC" { "☑" } else { "☐" }) + " Online KYC"),
      #h(8pt)
      #text((if company.KYCMode == "Offline KYC" { "☑" } else { "☐" }) + "Offline KYC"),
      #h(8pt)
      #text((if company.KYCMode == "Digilocker" { "☑" } else { "☐" }) + " Digilocker")
      ]
        #v(8pt)

#box(
  fill: rgb("#f8d7da"), // Light pink background
  stroke: 1pt,
  inset: 0pt,
  [
    #grid(
      columns: (auto, 1fr),
      gutter: 0pt,
          [
          #v(8pt)
        // Heading text in white
        #text(" 1.Identity Details (Please refer gudelines overleaf)", weight: "bold", fill: black)
          #v(8pt)
      ],

    )
  ]
)
        #v(8pt)

#let indent = 8pt
#let label_width = 80pt
#let panSpace = 101pt
#let nameSpace = 8pt
#let maidenSpace = 61pt
#let fatherSpace = 18pt
#let dobSpace = 68pt  
#let genderSpace = 90pt
#let marriedSpace = 57pt
#let nationSpace = 68pt
#let residentSpace = 35pt
#let origninSpace =  14pt

#h(8pt)#text("PAN*:", weight: "bold")
#h(panSpace)
  #h(8pt)#underline([
    #text(
       if company.PAN != "" {
      company.PAN + "                                                            "  // ← Add more spaces here
    } else {
      "                                                                                "  // ← Match length for empty case
    },

    )
  ])
#v(4pt)

#h(8pt)#text("Name (Same as ID proof)*: ", weight: "bold")
#h(nameSpace)
 #underline([
    #text(
       if company.ClientName != "" {
      company.ClientName + "                                                        "  // ← Add more spaces here
    } else {
      "                                                                              "  // ← Match length for empty case
    },
    )
  ])
#v(4pt)


#h(8pt)#text("Maiden Name: ", weight: "bold")
#h(maidenSpace)
 #underline([
    #text(
       if company.MaidenName != "" {
      company.MaidenName + "                                                        "  // ← Add more spaces here
    } else {
      "                                                                              "  // ← Match length for empty case
    },
    )
  ])
#v(4pt)


#h(8pt)#text("Father/Spouse Name*: *: ", weight: "bold")
#h(fatherSpace)
 #underline([
    #text(
       if company.FatherSpouseName != "" {
      company.FatherSpouseName + "                                                   "  // ← Add more spaces here
    } else {
      "                                                                              "  // ← Match length for empty case
    },
    )
  ])
#v(4pt)


#h(8pt)#text("Date of Birth:*", weight: "bold")
#h(dobSpace)
 #underline([
    #text(
       if company.DOB != "" {
      company.DOB + "                                                "  // ← Add more spaces here
    } else {
      "                                                              "  // ← Match length for empty case
    },
    )
  ])
#v(4pt)



            #h(8pt)#text("Gender*: ", weight: "bold")  #h(genderSpace)
            #text((if company.Gender == "Male" { "☑" } else { "☐" }) + " Male")
            #h(10pt)#text((if company.Gender == "Female" { "☑" } else { "☐" }) + " Female")
            #h(10pt)#text((if company.Gender == "Transgender" { "☑" } else { "☐" }) + " Transgender")

          #h(8pt)#text("Marital Status*: ", weight: "bold") #h(marriedSpace)
          #text((if company.MaritalStatus == "Single" { "☑" } else { "☐" }) + " Single")
          #h(10pt)#text((if company.MaritalStatus == "Married" { "☑" } else { "☐" }) + " Married")

          #h(8pt)#text("Nationality*: ", weight: "bold")#h(nationSpace)
          #text((if company.Nationality == "Indian" { "☑" } else { "☐" }) + " Indian")
          #h(10pt)#text((if company.Nationality == "Other" { "☑" } else { "☐" }) + " Other")

          #h(8pt)#text("Residential Status*: ", weight: "bold")#h(residentSpace)
          #text((if company.ResidentialStatus == "Resident Individual" { "☑" } else { "☐" }) + " Resident Individual")
          #h(3pt)#text((if company.ResidentialStatus == "Non Resident Indian" { "☑" } else { "☐" }) + " Non Resident Indian")

          #h(8pt)#text("Origin (Please tick ✔)*: ", weight: "bold")#h(origninSpace)
          #text((if company.Origin == "Foreign National" { "☑" } else { "☐" }) + " Foreign National")
          #h(3pt)#text((if company.Origin == "Person of Indian Origin" { "☑" } else { "☐" }) + " Person of Indian Origin")
   


#align(top)[
    #align(right)[
  #v(-140pt) // aligns with DOB row
  #box(
    stroke: 1pt,
    width: 4cm,
    inset: 0pt,
    [
      #block(spacing: 6pt)[
        // Passport Photo
        #box(
          width: 4cm,
          height: 3cm,
          inset: 4pt,
          stroke: none,
          [
            #align(center)[
              //#text("Passport\nPhoto",size: 8pt, fill: gray.lighten(40%))
			  #if company.PhotoImg != "" {
				  let photoPath = "../Assets/Photo_Img_" + company.PAN + company.PhotoImg
				  image(photoPath, width: 100%, height: 100%)
			  }
            ]
          ]
        )

        // Horizontal line
        #rect(width: 4cm, height: 0.5pt, fill: black)

      // Signature box
        #box(
          width: 4cm,
          height: 1.5cm,
          inset: 4pt,
          stroke: none,
      [
        #align(bottom + center)[
        //#text("Signature", size: 8pt, fill: gray.lighten(40%))
		#if company.SignatureIMG != "" {
			  let imgPath = "../Assets/Sign_Img_" + company.PAN + company.SignatureIMG
			  image(imgPath, width: 100%, height: 100%)
		  }
      ]
    ]
  )
      ]
    ]
  )
    ]
]


#v(10pt)
#h(8pt)#text("Proof of Identity(POI) submitted for PAN exempted cases(plesae tick)", weight: "bold")
#v(5pt)
// Aadhaar Number
  #h(8pt)#text((if company.AadhaarNumber != "" { "☑" } else { "☐" }) + " A - Aadhaar Card   ", weight: "bold")
  #underline([
    #text(
       if company.AadhaarNumber != "" {
      company.AadhaarNumber + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])
#v(3pt)

//Passport Number
  #h(8pt)#text((if company.PassportNumber != "" { "☑" } else { "☐" }) + " B - Passport Number   ", weight: "bold")
  #underline([
    #text(
       if company.PassportNumber != "" {
      company.PassportNumber + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])#h(24pt)#text("Expire Date",weight: "bold")
    #underline([
    #text(
       if company.PassExpireDate != "" {
      company.PassExpireDate + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])
#v(3pt)

//Voter ID Card
  #h(8pt)#text((if company.VoterID != "" { "☑" } else { "☐" }) + " C - Voter ID Card   ", weight: "bold")
  #underline([
    #text(
       if company.VoterID != "" {
      company.VoterID + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])
#v(3pt)

//Driving License
  #h(8pt)#text((if company.DrivingLicence != "" { "☑" } else { "☐" }) + " D - Driving License   ", weight: "bold")
  #underline([
    #text(
       if company.DrivingLicence != "" {
      company.DrivingLicence + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])#h(24pt)#text("Expire Date",weight: "bold")
    #underline([
    #text(
       if company.DriveExpireDate != "" {
      company.DriveExpireDate + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])
#v(3pt)
//job card
  #h(8pt)#text((if company.JobCard != "" { "☑" } else { "☐" }) + "  E - NREGA Job Card", weight: "bold")
   #h(8pt)#underline([
    #text(
       if company.JobCard != "" {
      company.JobCard + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])
#v(3pt)
//others
  #h(8pt)#text((if company.Others != "" { "☑" } else { "☐" }) + " Z - Others", weight: "bold")
    #h(8pt)#underline([
    #text(
       if company.Others != "" {
      company.Others + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])#h(24pt)#text("(Any document notified by Central goverment)",weight: "bold")
#v(3pt)

 #h(8pt)#text(" Identification Number", weight: "bold")
    #h(8pt)#underline([
    #text(
       if company.IDNumber != "" {
      company.IDNumber + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])
#v(5pt)


//Address Details:- 

#box(
  fill: rgb("#f8d7da"), // Light pink background
  stroke: 1pt,
  inset: 0pt,
  [
    #grid(
      columns: (auto, 1fr),
      gutter: 0pt,
          [
          #v(8pt)
        // Heading text in white
        #text(" 2.Address Details (Please refer gudelines overleaf)", weight: "bold", fill: black)
          #v(8pt)
      ],

    )
  ]
)
#v(3pt)
#h(8pt)#text(" A.Correspondence/ Local Address*",weight: "bold")

 #h(8pt)#text(" Line1*", weight: "bold")
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

   #h(8pt)#text(" Line2*", weight: "bold")
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

   #h(8pt)#text(" Line3*", weight: "bold")
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


 #h(8pt)#text("City/Town/Village", weight: "bold")
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

 #h(8pt)#text("State*", weight: "bold")
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
      #h(8pt)#text("Address Type*", weight: "bold")#h(5pt)
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
  columns: (1fr, auto), // line on left, heading on right
  gutter: 0pt,
  [
    // Line spanning to the left of the heading
    #line(length: 190mm, stroke: 1pt)
  ],
  [
    #box(
      fill: rgb("#f8d7da"),
      inset: 6pt,
      [
        #text("Applicant e-Sign", weight: "bold", size: 11pt)
      ]
    )
  ]
)

#v(58pt)

  ]
)


//Second Page


#pagebreak()
#box(
    stroke: 1pt,
  inset: 0pt,
  [
    #v(8pt)
#h(0pt)#text(" B.Permanant Residence address of applicant, if different of above A / Oversease Address*(Mandatory for NRI Applicant)",weight: "bold")
#v(6pt)
 #h(8pt)#text(" Line1*", weight: "bold")
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

   #h(8pt)#text(" Line2*", weight: "bold")
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

   #h(8pt)#text(" Line3*", weight: "bold")
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


 #h(8pt)#text("City/Town/Village", weight: "bold")
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

 #h(8pt)#text("State*", weight: "bold")
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
      #h(8pt)#text("Address Type*", weight: "bold")#h(5pt)
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

    #line(length: 200mm, stroke: 1pt)
      #v(2pt)


  //BreakDown page

  #h(8pt)#text("Proof of Address* (Affected copy if abt 1 PDA for correnspondence and Permanant address to be submitted)", weight: "bold")
  #v(6pt)

  // Aadhaar Number
  #h(8pt)#text((if company.AadhaarNumber != "" { "☑" } else { "☐" }) + " A - Aadhaar Card   ", weight: "bold")
  #underline([
    #text(
       if company.AadhaarNumber != "" {
      company.AadhaarNumber + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])
#v(3pt)

//Passport Number
  #h(8pt)#text((if company.PassportNumber != "" { "☑" } else { "☐" }) + " B - Passport Number   ", weight: "bold")
  #underline([
    #text(
       if company.PassportNumber != "" {
      company.PassportNumber + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])#h(24pt)#text("Expire Date",weight: "bold")
    #underline([
    #text(
       if company.PassExpireDate != "" {
      company.PassExpireDate + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])
#v(3pt)

//Voter ID Card
  #h(8pt)#text((if company.VoterID != "" { "☑" } else { "☐" }) + " C - Voter ID Card   ", weight: "bold")
  #underline([
    #text(
       if company.VoterID != "" {
      company.VoterID + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])
#v(3pt)

//Driving License
  #h(8pt)#text((if company.DrivingLicence != "" { "☑" } else { "☐" }) + " D - Driving License   ", weight: "bold")
  #underline([
    #text(
       if company.DrivingLicence != "" {
      company.DrivingLicence + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])#h(24pt)#text("Expire Date",weight: "bold")
    #underline([
    #text(
       if company.DriveExpireDate != "" {
      company.DriveExpireDate + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])
#v(3pt)
//job card
  #h(8pt)#text((if company.JobCard != "" { "☑" } else { "☐" }) + "  E - NREGA Job Card", weight: "bold")
   #h(8pt)#underline([
    #text(
       if company.JobCard != "" {
      company.JobCard + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])
#v(3pt)
//others
  #h(8pt)#text((if company.Others != "" { "☑" } else { "☐" }) + " Z - Others", weight: "bold")
    #h(8pt)#underline([
    #text(
       if company.Others != "" {
      company.Others + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])#h(24pt)#text("(Any document notified by Central goverment)",weight: "bold")
#v(3pt)

 #h(8pt)#text(" Identification Number", weight: "bold")
    #h(8pt)#underline([
    #text(
       if company.IDNumber != "" {
      company.IDNumber + "                       "  // ← Add more spaces here
    } else {
      "                                      "  // ← Match length for empty case
    },
    )
  ])
#v(5pt)


//Contact Details

#box(
  fill: rgb("#f8d7da"), // Light pink background
  stroke: 1pt,
  inset: 0pt,
  [
    #grid(
      columns: (auto, 1fr),
      gutter: 0pt,
          [
          #v(8pt)
        // Heading text in white
        #text(" 3.Contact Details(in captial)", weight: "bold", fill: black)
          #v(8pt)
      ],
    )
  ]
)
#v(8pt)

#h(8pt)#text("Email*: ", weight: "bold")
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

#h(8pt)#text("Mobile Number*: ", weight: "bold")
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


 #h(8pt)#text("Tele(off)", weight: "bold")
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
  fill: rgb("#f8d7da"),
  stroke: 1pt,
  inset: 0pt,
  [
    #grid(
      columns: (auto, 1fr),
      gutter: 0pt,
      [
        #v(8pt)
        #text(" 3.Applicant Declaration          ", weight: "bold", fill: black)
        #v(8pt)
      ]
    )
  ]
)
#v(4pt)

// Three-column layout with vertical lines
#grid(
  columns: (1fr, 1pt, 1fr, 1pt, 1fr), // Added 1pt columns for vertical lines
  gutter: 0pt,

  // First column
  [
    #box(
      inset: 6pt,
      [
        #align(center)[
          #text("Declaration", weight: "bold")
        ]
        #linebreak()
        #text("I hereby declare that the information provided is true and correct to the best of my knowledge.")

        #h(8pt)#text("Date: ", weight: "bold")
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

        #h(8pt)#text("Place: ", weight: "bold")
        #underline([
          #text(
            if company.Branch != "" {
              company.Branch + "                         "
            } else {
              "                                          "
            }
          )
        ])
        #v(6pt)
      ]
    )
  ],
  
  // First vertical line
  [
    #line(
      start: (0pt, 0pt),
      end: (0pt, 170pt),
      stroke: 0.5pt + black
    )
  ],
  
  // Second column
  [
    #box(
      inset: 6pt,
      [
        #align(center)[
          #h(32pt)
          #text("Applicant Esign", weight: "bold")
        ]
        #text("im in Applicant Esign", weight: "bold")
      ]
    )
  ],
  
  // Second vertical line
  [
    #line(
      start: (0pt, 0pt),
      end: (0pt, 170pt),
      stroke: 0.5pt + black
    )
  ],
  
  // Third column
  [
    #box(
	  width: 4cm,
	  height: 1.4cm,
	  inset: 2pt,
	  stroke: 1pt,
      [
        #align(center)[
          #h(32pt)
          #text("Applicant Wet Esign", weight: "bold")
        ]
        //#text("im in Applicant Wet Esign", weight: "bold")
		#if company.SignatureIMG != "" {
			  let imgPath = "../Assets/Sign_Img_" + company.PAN + company.SignatureIMG
			  image(imgPath, width: 100%, height: 100%)
		  }
      ]
    )
  ]
)
  #line(length: 200mm, stroke: 1pt)

//For office use only

#grid(
  columns: (auto, 1pt, auto),
  gutter: 0pt,
  
  // ==== Left Column: IPV Details ====
  [
    #box(
      inset: 2pt,
      [
        #align(center)[
          #h(16pt)
          #text("In-Person Verification (IPV) carried out by:*", weight: "bold")
        ]
        #v(4pt)

        // IPV Date
        #h(8pt)#text(" IPV Date:", weight: "bold")
        #h(8pt)#underline([
          #text(if company.IPVDate != "" { company.IPVDate } else { " " })
        ])
        #v(5pt)

        // EMP Name
        #h(8pt)#text(" EMP Name:", weight: "bold")
        #h(5pt)#underline([
          #text(if company.EMPAdmin != "" { company.EMPAdmin } else { " " })
        ])

        // EMP Code
        #h(8pt)#text(" EMP Code:", weight: "bold")
        #h(8pt)#underline([
          #text(if company.EMPCode != "" { company.EMPCode } else { " " })
        ])

        // Designation
        #h(8pt)#text(" Designation:", weight: "bold")
        #h(8pt)#underline([
          #text(if company.Designation != "" { company.Designation } else { " " })
        ])
      ]
    )
  ],
  
  // ==== Vertical Line (fixed syntax) ====
  [
    #line(
      start: (0pt, 0pt),
      end: (0pt, 155pt),  // Simple percentage works for content height
      stroke: 0.5pt + black,
    )
  ],
  
  // ==== Right Column: Intermediary Details ====
  [
    #box(
      inset: 2pt,
      [
        #align(center)[
          #text("Intermediary Details*", weight: "bold")
        ]
        #v(4pt)

        // Self Certified
        #h(8pt)#text((if company.intermedetaryDetail == "OVD" { "☑" } else { "☐" }) + " Self Certified document copies received(OVD)", weight: "bold")
        #v(6pt)

        // True Copies
        #h(8pt)#text((if company.intermedetaryDetail == "Attested" { "☑" } else { "☐" }) + " True copies of documents received(Attested)", weight: "bold")
        #v(8pt)

        // AMC/Intermediary Name
        #h(8pt)#text("AMC/Intermediary Name:", weight: "bold")
        #v(4pt)

        // Highlighted Box
        #h(8pt)#box(
          stroke: 1pt,
          inset: 6pt,
          [
            #text(company.CompanyName, weight: "bold", size: 13pt)
          ]
        )
      ]
    )
  ]
)

// Applicant e-Sign section
#grid(
  columns: (1fr, auto),
  gutter: 0pt,
  [
    #line(length: 200mm, stroke: 1pt)
  ],
  [
    #box(
      fill: rgb("#f8d7da"),
      inset: 6pt,
      [
        #text("Applicant e-Sign", weight: "bold", size: 11pt)
      ]
    )
  ]
)
#v(60pt)

  ])

#if company.panAtt != "" {
	  pagebreak()
	  let imgPath = "../Assets/PAN_Img_" + company.PAN + company.panAtt
	  if company.panAtt == ".pdf" {
		   let data = read(imgPath, encoding: none)
		   muchpdf(data)
	  }
	  else {
		   image(imgPath, width: 100%, height: 100%)
		}
  }


#if company.AadhaarAtt != "" {
	  pagebreak()
	  let imgPath = "../Assets/Aadhar_Img_" + company.PAN + company.AadhaarAtt
	  if company.AadhaarAtt == ".pdf" {
		   let data = read(imgPath, encoding: none)
		   muchpdf(data)
	  }
	  else {
		   image(imgPath, width: 100%, height: 100%)
		}
  }