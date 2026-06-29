//#import "@preview/muchpdf:0.1.0": muchpdf


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
#let ClosureDetails = data




// Page setup
#set page(
  width: 210mm,
  height: 300mm,
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




// ==== KYC Box Layout ====


			// Company Name centered
	#v(8pt)
		#align(center)[#text(ClosureDetails.CompanyName, weight: "bold", size: 14pt)]      
		#align(center)[#text(ClosureDetails.CompanyAddress,size: 12pt)]
		#v(8pt)
        #align(center)[#underline[#text(ClosureDetails.FormHeading, size: 12pt, weight: "bold")]]

	#v(10pt)

	
            // First table

	#table(
			columns: (auto),
				rows: (auto,auto),
					//stroke: (bottom: none),  // Remove bottom border to prevent double line

							[
									#h(8pt)#text("Application No. :", weight: "bold", size: 11pt)
									#h(1pt)#text(if ClosureDetails.ApplicationNumber != none and ClosureDetails.ApplicationNumber.trim() != "" {
													ClosureDetails.ApplicationNumber
													} else {
															"NA" 
														}
												)
									#h(100pt)#text("UCC Code :", weight: "bold", size: 11pt)
									#h(1pt)#text(if ClosureDetails.UCC != none and ClosureDetails.UCC.trim() != "" {
										ClosureDetails.UCC
										}else {
											"NA"
											})
									#h(100pt)#text("Date :", weight: "bold", size: 11pt)
									#h(1pt)#text(if ClosureDetails.TodayDate != none and ClosureDetails.TodayDate.trim() != "" {
										ClosureDetails.TodayDate}
										else {
											"NA"})
							],			
					        [
	                       
								#h(8pt)#text("Closure Inititated By  :", weight: "bold", size: 11pt)#h(20pt) 
								#text(if ClosureDetails.ClosurIntiated == "BO" { "☑ BO" } else { "☐ BO" }) #h(25pt) 
								#text(if ClosureDetails.ClosurIntiated == "DP" { "☑ DP" } else { "☐ DP" }) #h(25pt) 
								#text(if ClosureDetails.ClosurIntiated == "CDSL" { "☑ CDSL" } else { "☐ CDSL" }) #h(25pt) 
					        ]

	)

	   #h(8pt)#text("(To be filled by the BO.(in case of initiated closure). please fill the details in block letter in English)",  size: 11pt)
	#v(10pt)
	   #h(8pt)#text("Dear Sir/Madam,",  weight: "bold", size: 11pt)
	#v(2pt)   
    #h(8pt)
		#block[
		#text("I/ ", size: 11pt)
		#strike("We")
		#text(" the sole holder/ ", size: 11pt)
		#strike("Joint Holders / Guardian (in case of minor) / Cleaning Member")
		#text(" request you to close my / ", size: 11pt)
		#strike("our")
		#text(" account with you from the date of this application. The details of my / ", size: 11pt)
		#strike("our")
		#text(" account are given below.", size: 11pt)
		]
	#v(10pt)


	       //Second table

   #table(
			columns: (auto),
				rows: (auto,auto),
					//stroke: (bottom: none),  // Remove bottom border to prevent double line

                        //First Row
							[

									#box(
											fill: rgb("#d3d3d3"), // Light pink background
											stroke: 0pt,
											inset: 0pt,
											[
												#grid(
												columns: (auto, 1fr),
												gutter: 0pt,
												[
													#v(8pt)
													// Heading text in white
												#text(" Account Holder Details", weight: "bold", fill: black)
													#v(8pt)
											],
											
												)
											]
										)
	
							],	

						//Second Row  ClientID & DPID	

					        [

										#h(8pt)#text("DP ID :", weight: "bold", size: 11pt)
									#h(1pt)#text(if ClosureDetails.DPID != none and ClosureDetails.DPID.trim() != "" {
													ClosureDetails.DPID
													} else {
															"NA" 
														}
												)
									#h(100pt)#text("Client ID :", weight: "bold", size: 11pt)
									#h(1pt)#text(if ClosureDetails.ClientID != none and ClosureDetails.ClientID.trim() != "" {
										ClosureDetails.ClientID
										}else {
											"NA"
											})
					        ],

						//Third Row	name of first holder

						     [

										#h(8pt)#text("Name of the First Holder : ", weight: "bold", size: 11pt)
									    #h(10pt)#text(if ClosureDetails.FirstHolder != none and ClosureDetails.FirstHolder.trim() != "" {
													ClosureDetails.FirstHolder
													} else {
															"NA" 
														}
												)
					        ],
						//Foruth row	
							[

										#h(8pt)#text("Name of the Second Holder : ", weight: "bold", size: 11pt)
									    #h(10pt)#text(if ClosureDetails.SecondHolder != none and ClosureDetails.SecondHolder.trim() != "" {
													ClosureDetails.SecondHolder
													} else {
															"NA" 
														}
												)
					        ],
						//Five Row	
							[

										#h(8pt)#text("Name of the Third Holder : ", weight: "bold", size: 11pt)
									    #h(10pt)#text(if ClosureDetails.ThirdHolder != none and ClosureDetails.ThirdHolder.trim() != "" {
													ClosureDetails.ThirdHolder
													} else {
															"NA" 
														}
												)
					        ],
						//Six Row	 Address

						[

							#let fullCorrAddress =(if ClosureDetails.CorrAddress1 != none and ClosureDetails.CorrAddress1.trim() != "" { ClosureDetails.CorrAddress1 } else { "--" }) +  (if ClosureDetails.CorrAddress2 != none and ClosureDetails.CorrAddress2.trim() != "" { ClosureDetails.CorrAddress2 } else { "--" }) +  (if ClosureDetails.CorrAddress3 != none and ClosureDetails.CorrAddress3.trim() != "" { ClosureDetails.CorrAddress3 } else { "--" })
										
                              #text(" Address: ", weight: "bold", size: 11pt)
                              #text(fullCorrAddress, size: 11pt)
									
						],
						//seven row 
						[
									#h(8pt)#text("City :", weight: "bold", size: 11pt)
									#h(1pt)#text(if ClosureDetails.CorrCity != none and ClosureDetails.CorrCity.trim() != "" {
													ClosureDetails.CorrCity
													} else {
															"NA" 
														}
												)
									#h(100pt)#text("State :", weight: "bold", size: 11pt)
									#h(1pt)#text(if ClosureDetails.CorrState != none and ClosureDetails.CorrState.trim() != "" {
										ClosureDetails.CorrState
										}else {
											"NA"
											})
									#h(100pt)#text("Pincode :", weight: "bold", size: 11pt)
									#h(1pt)#text(if ClosureDetails.CorrPincode != none and ClosureDetails.CorrPincode.trim() != "" {
										ClosureDetails.CorrPincode}
										else {
											"NA"})
							],

						 // Eight row Second Heading
						 	[

									#box(
											fill: rgb("#d3d3d3"), // Light pink background
											stroke: 0pt,
											inset: 0pt,
											[
												#grid(
												columns: (auto, 1fr),
												gutter: 0pt,
												[
													#v(8pt)
													// Heading text in white
												#text(" Details of remaining security balances in the account(if any)", weight: "bold", fill: black)
													#v(8pt)
											],
											
												)
											]
										)
	
							],
							//Nine row
							[

										#h(8pt)#text("Reason for the closing the account  : ", weight: "bold", size: 11pt)
									    #h(10pt)#text(if ClosureDetails.ClosingReason != none and ClosureDetails.ClosingReason.trim() != "" {
													ClosureDetails.ClosingReason
													} else {
															"NA" 
														}
												)
					        ],
							//tenth row
							[

										#h(8pt)#text("Balance remaining in the account(if any to be) : ", weight: "bold", size: 11pt)
									    #h(10pt)#text(if ClosureDetails.RemainBalance != none and ClosureDetails.RemainBalance.trim() != "" {
													ClosureDetails.RemainBalance
													} else {
															"NA" 
														}
												)
					        ],
							//elevan row
								[

										#h(8pt)#text("0 partly rematerialised and partly transferred : ", weight: "bold", size: 11pt)
									    #h(10pt)#text(if ClosureDetails.PartlyTransfer != none and ClosureDetails.PartlyTransfer.trim() != "" {
													ClosureDetails.PartlyTransfer
													} else {
															"NA" 
														}
												)
					            ],
								//12 row
								[
   
									#h(8pt)#text(if ClosureDetails.TransferMode == "Transfered to another account" { "☑  Transfer to another account(Number given below)" } else { "☐ Transfer to another account(Number given below)" })
									#h(200pt)#text(if ClosureDetails.TransferMode == "Not Applicable" { "☑ Not Applicable" } else { "☐ Not Applicable" })
					            ],

								//13 row Transfer ClientID &DPID

								 [

										#h(8pt)#text("DP ID :", weight: "bold", size: 11pt)
									#h(1pt)#text(if ClosureDetails.TransferDPID != none and ClosureDetails.TransferDPID.trim() != "" {
													ClosureDetails.TransferDPID
													} else {
															"NA" 
														}
												)
									#h(100pt)#text("Client ID :", weight: "bold", size: 11pt)
									#h(1pt)#text(if ClosureDetails.TransferClientID != none and ClosureDetails.TransferClientID.trim() != "" {
										ClosureDetails.TransferClientID
										}else {
											"NA"
											})
					            ],

								//14row  some info in static
								[
										#h(8pt)#text("Balance Present in a/c for", weight: "bold", size: 12pt)#h(2pt)#text("(To be filled by DP, if applicable) : ",size: 11pt)#v(0pt)
									    #h(15pt)#text("0 ER - marked 0 Pledged. ", weight: "bold", size: 11pt)#v(0pt)
										#h(15pt)#text("0 Pending for Dematerialisation 0 Frozen. ", weight: "bold", size: 11pt)#v(0pt)
										#h(15pt)#text("0 Pending for Rematerialisation 0 Lock-in. ", weight: "bold", size: 11pt)#v(0pt)
										
										
								],

								//15 row Declaration
								[
									   #h(8pt)#text("DECLARATION (In case of Account closure due to SHIFTING ACCOUNT) : ", weight: "bold", size: 12pt)
									 #h(8pt)
									 #block[
										#text("I/ ", size: 12pt)
										#strike("We")
										#text("  declare and confirm that all the transaction in my/ ", size: 12pt)
										#strike("our")
										#text(" Demat account are true/authetic", size: 12pt)
										]

								]
	)

#v(0pt)


//Third Table singature & holders

	    #table(
			columns: (auto,auto,auto,auto),
				rows: (auto,auto,auto),
					//stroke: (bottom: none),  // Remove bottom border to prevent double line


					//First Row
				[
						
				],
				[
					#h(8pt)#text("First Holder ", weight: "bold", size: 11pt)#h(80pt)
				],
				[
					#h(8pt)#text("Second Holder ", weight: "bold", size: 11pt)#h(78pt)
				],
				[ 
					#h(8pt)#text("Third Holder ", weight: "bold", size: 11pt)#h(80pt)
				],


				//Second Row
				[
						#h(8pt)#text("Name ", weight: "bold", size: 11pt)
				],
				[
					    #h(8pt)#text(ClosureDetails.FirstHolder , size: 11pt)
				],
				[
					
				],
				[ 
					
				],


				//Third Row
				[
						#h(8pt)#text("Signature	", weight: "bold", size: 11pt)
				],
				[
					
				],
				[
					
				],
				[ 
					
				],
		    )

#if ClosureDetails.CMRAttachment != "" {
	  pagebreak()
	  let imgPath = "../Assets/CMRAttachment_" + ClosureDetails.UCC + ClosureDetails.CMRAttachment
	  if ClosureDetails.CMRAttachment == ".pdf" {
		   let data = read(imgPath, encoding: none)
		   muchpdf(data)
	  }
	  else {
		   image(imgPath, width: 100%, height: 100%)
		}
  }