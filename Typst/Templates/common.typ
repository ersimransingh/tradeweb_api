//#import "@preview/oxifmt:0.2.1": strfmt

// oxifmt v0.2.1

// For compatibility with pre-0.8.0 Typst types, which were strings
#let _int-type = type(0)
#let _float-type = type(5.5)
#let _str-type = type("")
#let _label-type = type(<hello>)

#let _strfmt_formatparser(s) = {
  if type(s) != _str-type {
    panic("String format parsing internal error: String format parser given non-string.")
  }
  let result = ()
  let codepoints = s.codepoints()

  // -- parsing state --
  let current-fmt-span = none
  let current-fmt-name = none
  // if the last character was an unescaped {
  let last-was-lbracket = false
  // if the last character was an unescaped }
  let last-was-rbracket = false

  // -- procedures --
  let write-format-span(last-i, result, current-fmt-span, current-fmt-name) = {
    current-fmt-span.at(1) = last-i + 1  // end index
    result.push((format: (name: current-fmt-name, span: current-fmt-span)))
    current-fmt-span = none
    current-fmt-name = none
    (result, current-fmt-span, current-fmt-name)
  }

  // -- errors --
  let excessive-lbracket() = {
    panic("String format parsing error: Inserted a second, non-escaped { inside a {format specifier}. Did you forget to insert a } somewhere, or to escape the { with {{?")
  }
  let excessive-rbracket() = {
    panic("String format parsing error: Inserted a stray } (doesn't match any { from before). Did you forget to insert a { somewhere, or to escape the } with }}?")
  }
  let missing-rbracket() = {
    panic("String format parsing error: Reached end of string with an open format specifier {, but without a closing }. Did you forget to insert a right bracket, or to escape the { with {{?")
  }

  // -- parse loop --
  let last-i = none
  let i = 0
  for character in codepoints {
    if character == "{" {
      // double l-bracket = escape
      if last-was-lbracket {
        last-was-lbracket = false  // escape {{
        last-was-rbracket = false
        if current-fmt-span.at(0) == last-i {
          current-fmt-span = none  // cancel this span
          current-fmt-name = none
        }
        if current-fmt-name != none {
          // if in the middle of a larger span ({ ... {{ <-):
          // add the escaped character to the format name
          current-fmt-name += character
        } else {
          // outside a span ({...} {{ <-) => emit an 'escaped' token
          result.push((escape: (escaped: "{", span: (last-i, i + 1))))
        }

        last-i = i
        i += 1  // '{' is ASCII, so 1 byte
        continue
      }
      if last-was-rbracket {
        // { ... }{ <--- ok, close the previous span
        (result, current-fmt-span, current-fmt-name) = write-format-span(last-i, result, current-fmt-span, current-fmt-name)
        last-was-rbracket = false
      }
      if current-fmt-span == none {
        // begin span
        current-fmt-span = (i, none)
        current-fmt-name = ""
      }
      last-was-lbracket = true
    } else if character == "}" {
      last-was-lbracket = false
      if last-was-rbracket {
        last-was-rbracket = false  // escape }}
        if current-fmt-name != none {
          current-fmt-name += character
        } else {
          result.push((escape: (escaped: "}", span: (last-i, i + 1))))
        }

        last-i = i
        i += 1  // '}' is ASCII, so 1 byte
        continue
      }
      // delay closing the span to the next iteration
      // in case this is an escaped }
      last-was-rbracket = true
    } else {
      // { ... {A  <--- non-escaped { inside larger {}
      if last-was-lbracket and (current-fmt-span != none and current-fmt-span.at(0) != last-i) {
        excessive-lbracket()
      }
      if last-was-rbracket {
        if current-fmt-span == none {
          // {...} }A <--- non-escaped } with no matching {
          excessive-rbracket()
        } else {
          // { ... }A <--- ok, close the previous span
          (result, current-fmt-span, current-fmt-name) = write-format-span(last-i, result, current-fmt-span, current-fmt-name)
        }
      }
      // {abc <--- add character to the format name
      if current-fmt-name != none {
        current-fmt-name += character
      }
      last-was-lbracket = false
      last-was-rbracket = false
    }

    last-i = i
    i += character.len() // index must be in bytes, and a UTF-8 codepoint can have more than one byte
  }
  // { ...
  if current-fmt-span != none {
    if last-was-rbracket {
      // ... } <--- ok, close span
      (result, current-fmt-span, current-fmt-name) = write-format-span(last-i, result, current-fmt-span, current-fmt-name)
    } else {
      // {abcd| <--- string ended with unclosed span
      missing-rbracket()
    }
  }

  result
}

#let _strfmt_parse-fmt-name(name) = {
  // {a:b} => separate 'a' from 'b'
  // (also accepts {a}, {}, {0}, {:...})
  let subparts = name.match(regex("^([^:]*)(?::(.*))?$")).captures
  let name = subparts.at(0)
  let extras = subparts.at(1)
  let name = if type(name) != _str-type {
    name
  } else if name == "" {
    none
  } else if regex("^\\d+$") in name {
    int(name)
  } else {
    name
  }
  (name, extras)
}

#let _strfmt_is-numeric-type(obj) = {
  type(obj) in (_int-type, _float-type)
}

#let _strfmt_stringify(obj) = {
  if type(obj) in (_int-type, _float-type) {
    // Fix negative sign not being a hyphen
    // for consistency with our rich formatting output
    str(obj).replace("\u{2212}", "-")
  } else if type(obj) in (_label-type, _str-type) {
    str(obj)
  } else {
    repr(obj)
  }
}

#let _strfmt_display-radix(num, radix, signed: true, lowercase: false) = {
  let num = int(num)
  if type(radix) != _int-type or num == 0 or radix <= 1 {
    return "0"
  }
  let sign = if num < 0 and signed { "-" } else { "" }
  let num = calc.abs(num)
  let radix = calc.min(radix, 16)
  let digits = if lowercase {
    ("0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f")
  } else {
    ("0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F")
  }
  let result = ""

  while (num > 0) {
    let quot = calc.quo(num, radix)
    let rem = calc.floor(calc.rem(num, radix))
    let digit = digits.at(rem)
    result = digit + result
    num = quot
  }

  sign + result
}

#let _strfmt_with-precision(num, precision) = {
  if precision == none {
    return _strfmt_stringify(num)
  }
  let result = _strfmt_stringify(calc.round(float(num), digits: calc.min(50, precision)))
  let digits-match = result.match(regex("^\\d+\\.(\\d+)$"))
  let digits-len-diff = 0
  if digits-match != none and digits-match.captures.len() > 0 {
    // get the digits capture group; its length will be digit amount
    let digits = digits-match.captures.first()
    digits-len-diff = precision - digits.len()
  } else if "." not in result {  // 5.0 or something
    // 0 digits! Difference will be exactly 'precision'
    digits-len-diff = precision
  }

  // add missing zeroes for precision
  if digits-len-diff > 0 {
    if "." not in result {
      result += "."  // decimal separator missing
    }
    result += "0" * digits-len-diff
  }

  result
}

#let _strfmt_exp-format(num, exponent-sign: "e", base: 10, precision: none) = {
  assert(_strfmt_is-numeric-type(num), message: "String formatter internal error: Cannot convert '" + repr(num) + "' to a number to obtain its scientific notation representation.")
  let f = float(num)
  let exponent = if f == 0 { 1 } else { calc.floor(calc.log(calc.abs(f), base: base)) }
  let mantissa = f / calc.pow(10, exponent)
  let mantissa = _strfmt_with-precision(mantissa, precision)

  mantissa + exponent-sign + _strfmt_stringify(exponent)
}

// Parses {format:specslikethis}.
// Rust's format spec grammar:
/*
format_spec := [[fill]align][sign]['#']['0'][width]['.' precision]type
fill := character
align := '<' | '^' | '>'
sign := '+' | '-'
width := count
precision := count | '*'
type := '' | '?' | 'x?' | 'X?' | identifier
count := parameter | integer
parameter := argument '$'
*/
#let _generate-replacement(fullname, extras, replacement, pos-replacements: (), named-replacements: (:), fmt-decimal-separator: auto) = {
  if extras == none {
    if not _strfmt_is-numeric-type(replacement) {
      fmt-decimal-separator = auto
    }
    replacement = _strfmt_stringify(replacement)
    if fmt-decimal-separator not in (auto, none) {
      replacement = replacement.replace(".", _strfmt_stringify(fmt-decimal-separator))
    }
    return replacement
  }
  let extras = _strfmt_stringify(extras)
  // note: usage of [\s\S] in regex to include all characters, incl. newline
  // (dot format ignores newline)
  let extra-parts = extras.match(
    //           fill      align    sign   #   0     width(from param)      width      precision(from param)    precision  spectype
    regex("^(?:([\\s\\S])?([<^>]))?([+-])?(#)?(0)?(?:(?:(\\d+)|([^.$]+))\$|(\\d+))?(?:\\.(?:(?:(\\d+)|([^$]+))\$|(\\d+|\*)))?([^\\s]*)\\s*$")
  )
  if extra-parts == none {
    panic("String formatter error: Invalid format spec '" + extras + "', from '{" + fullname.replace("{", "{{").replace("}", "}}") + "}'. Try escaping the braces { } with {{ }} if you wanted to insert literal braces.")
  }

  let (fill, align, sign, hashtag, zero, width-posarg, width-namedarg, width-lit, precision-posarg, precision-namedarg, precision-lit, spectype) = extra-parts.captures

  // 'count' type parameters in the spec (width, precision) can be either a literal number (123),
  // a number referring to a positional argument (123$), or some text referring to a named argument (abc$).
  // The final $ is mandatory for the last two cases.
  let parse-count(lit, pos, named, spec-part-name: "unknown") = {
    if lit != none {
      int(lit)
    } else if pos != none {
      let i = int(pos)
      assert(
        pos-replacements.len() > 0,
        message: "String formatter error: Attempted to use positional argument " + str(i) + " for " + spec-part-name + ", but no positional arguments were given (from '{" + fullname.replace("{", "{{").replace("}", "}}") + "}')."
      )
      assert(
        i >= 0 and i < pos-replacements.len(),
        message: "String formatter error: Attempted to use positional argument " + str(i) + " for " + spec-part-name + ", but there is no argument at that position (from '{" + fullname.replace("{", "{{").replace("}", "}}") + "}'). Please note that positional arguments start at position 0, and are specified in order after the format string in the 'strfmt' call."
      )
      let arg = pos-replacements.at(i)
      assert(
        type(arg) == _int-type,
        message: "String formatter error: Attempted to use positional argument " + str(i) + " for " + spec-part-name + ", but it was a(n) '" + type(arg) + "', not an integer (from '{" + fullname.replace("{", "{{").replace("}", "}}") + "}')."
      )

      int(arg)
    } else if named != none {
      assert(
        named-replacements.len() > 0,
        message: "String formatter error: Attempted to use named argument '" + named + "' for " + spec-part-name + ", but no named arguments were given (from '{" + fullname.replace("{", "{{").replace("}", "}}") + "}')."
      )
      assert(
        named in named-replacements,
        message: "String formatter error: Attempted to use named argument '" + named + "' for " + spec-part-name + ", but there is no argument associated to that name (from '{" + fullname.replace("{", "{{").replace("}", "}}") + "}'). Ensure you pass that argument in the 'strfmt' call, e.g. strfmt(\"string {:.myarg$}\", 5.823, myarg: 10)."
      )
      let arg = named-replacements.at(named)
      assert(
        type(arg) == _int-type,
        message: "String formatter error: Attempted to use named argument '" + named + "' for " + spec-part-name + ", but it was a(n) '" + type(arg) + "', not an integer (from '{" + fullname.replace("{", "{{").replace("}", "}}") + "}')."
      )

      int(arg)
    } else {
      none
    }
  }

  if precision-lit == "*" {
    panic("String formater error: Precision specification of type `.*` is not supported yet (from '{" + fullname.replace("{", "{{").replace("}", "}}") + "}'). Try specifying your desired precision directly on the format spec, e.g. `.5`, or through some argument, e.g. `.name$` to take it from the 'name' named argument.")
  }

  let align = if align == "" {
    none
  } else if align == "<" {
    left
  } else if align == ">" {
    right
  } else if align == "^" {
    center
  } else if align != none {
    panic("String formatter error: Invalid alignment in the format spec: '" + align + "' (must be either '<', '^' or '>').")
  }
  let width = parse-count(width-lit, width-posarg, width-namedarg, spec-part-name: "width")
  let width = if width == none { 0 } else { int(width) }
  let precision = parse-count(precision-lit, precision-posarg, precision-namedarg, spec-part-name: "precision")
  let hashtag = hashtag == "#"
  let zero = zero == "0"
  let hashtag-prefix = ""

  let valid-specs = ("", "?", "b", "x", "X", "o", "x?", "X?", "e", "E")
  let spec-error() = {
    panic(
      "String formatter error: Unknown spec type '" + spectype + "', from '{" + fullname.replace("{", "{{").replace("}", "}}") + "}'. Valid options include: '" + valid-specs.join("', '") + "'. Maybe you specified some invalid formatting spec syntax (after the ':'), which can also prompt this error. Check the oxifmt docs for more information.")
  }
  if spectype not in valid-specs {
    spec-error()
  }

  let is-numeric = _strfmt_is-numeric-type(replacement)
  if is-numeric {
    if zero {
      // disable fill, we will be prefixing with zeroes if necessary
      fill = none
    } else if fill == none {
      fill = " "
      zero = false
    }
    // default number alignment to right
    if align == none {
      align = right
    }

    // if + is specified, + will appear before all numbers >= 0.
    if sign == "+" and replacement >= 0 {
      sign = "+"
    } else if replacement < 0 {
      sign = "-"
    } else {
      sign = ""
    }

    // we'll add the sign back later!
    replacement = calc.abs(replacement)

    if spectype in ("e", "E") {
      let exponent-sign = if spectype == "E" { "E" } else { "e" }
      replacement = _strfmt_exp-format(calc.abs(replacement), exponent-sign: exponent-sign, precision: precision)
    } else if type(replacement) != _int-type and precision != none {
      replacement = _strfmt_with-precision(replacement, precision)
    } else if type(replacement) == _int-type and spectype in ("x", "X", "b", "o", "x?", "X?") {
      let radix-map = (x: 16, X: 16, "x?": 16, "X?": 16, b: 2, o: 8)
      let radix = radix-map.at(spectype)
      let lowercase = spectype.starts-with("x")
      replacement = _strfmt_stringify(_strfmt_display-radix(replacement, radix, lowercase: lowercase, signed: false))
      if hashtag {
        let hashtag-prefix-map = ("16": "0x", "2": "0b", "8": "0o")
        hashtag-prefix = hashtag-prefix-map.at(str(radix))
      }
    } else {
      precision = none
      replacement = if spectype.ends-with("?") {
        repr(replacement)
      } else {
        _strfmt_stringify(replacement)
      }
    }
    if fmt-decimal-separator not in (auto, none) {
      replacement = replacement.replace(".", _strfmt_stringify(fmt-decimal-separator))
    }
    if zero {
      let width-diff = width - (replacement.len() + sign.len() + hashtag-prefix.len())
      if width-diff > 0 {  // prefix with the appropriate amount of zeroes
        replacement = ("0" * width-diff) + replacement
      }
    }
  } else {
    sign = ""
    hashtag-prefix = ""
    hashtag = false
    zero = false
    replacement = if spectype.ends-with("?") {
      repr(replacement)
    } else {
      _strfmt_stringify(replacement)
    }
    if fill == none {
      fill = " "
    }
    if align == none {
      align = left
    }
    if precision != none and replacement.len() > precision {
      replacement = replacement.slice(0, precision)
    }
  }

  // use number prefixes parsed above
  replacement = sign + hashtag-prefix + replacement

  if fill != none {
    // perform fill/width adjustments: "x" ---> "  x" if width is 4
    let width-diff = width - replacement.len()  // number prefixes are also considered for width
    if width-diff > 0 {
      if align == left {
        replacement = replacement + (fill * width-diff)
      } else if align == right {
        replacement = (fill * width-diff) + replacement
      } else if align == center {
        let width-fill = fill * (calc.ceil(float(width-diff) / 2))
        replacement = width-fill + replacement + width-fill
      }
    }
  }

  replacement
}

#let strfmt(format, ..replacements) = {
  if format == "" { return "" }
  let formats = _strfmt_formatparser(format)
  let num-replacements = replacements.pos()
  let named-replacements = replacements.named()
  let unnamed-format-index = 0
  let fmt-decimal-separator = if "fmt-decimal-separator" in named-replacements { named-replacements.at("fmt-decimal-separator") } else { auto }

  let parts = ()
  let last-span-end = 0
  for f in formats {
    let replace-by = none
    let replace-span = none
    if "escape" in f {
      replace-by = f.escape.escaped
      replace-span = f.escape.span
    } else if "format" in f {
      let f = f.format
      let (name, extras) = _strfmt_parse-fmt-name(f.name)
      if name == none {
        let fmt-index = unnamed-format-index
        let amount-pos-replacements = num-replacements.len()
        if amount-pos-replacements == 0 {
          panic("String formatter error: Specified a {} (or similar) format to extract positional replacements, but none were given. Try specifying them sequentially after the format string, e.g. strfmt(\"{}, {}\", 5, 1+1) would become \"5, 2\".")
        }
        if amount-pos-replacements <= fmt-index {
          let were-was = if amount-pos-replacements == 1 { "was" } else { "were" }
          panic("String formatter error: Specified more {} (or similar) formats than positional replacements (only " + str(amount-pos-replacements) + " of them " + were-was + " given!). Please specify the missing positional arguments sequentially after the format string in the 'strfmt' call.")
        }
        replace-by = num-replacements.at(fmt-index)
        unnamed-format-index += 1
      } else if type(name) == _int-type {
        let fmt-index = name
        let amount-pos-replacements = num-replacements.len()
        if amount-pos-replacements == 0 {
          panic("String formatter error: format key '" + str(name) + "' would attempt to get a positional replacement, but none were given after the string. Try specifying positional replacements after the format string in the 'strfmt' call, e.g. strfmt(\"{1}, {0}\", 2, 3) would become \"3, 2\".")
        }
        if amount-pos-replacements <= fmt-index {
          let were-was = if amount-pos-replacements == 1 { "was" } else { "were" }
          panic("String formatter error: format key '" + str(name) + "', from '{" + f.name.replace("{", "{{").replace("}", "}}") + "}', is not a valid positional replacement position (only " + str(amount-pos-replacements) + " of them " + were-was + " given). Note that the first position is 0. For example, strfmt(\"{1}, {0}\", 2, 3) would become \"3, 2\".")
        }
        replace-by = num-replacements.at(fmt-index)
      } else {  // named replacement
        if name not in named-replacements {
          panic("String formatter error: format key '" + name + "', from '{" + f.name.replace("{", "{{").replace("}", "}}") + "}', does not match any given named replacement. Try specifying it after the format string, e.g. like so: strfmt(\"Test: {myarg}\", myarg: 1 + 1) would become \"Test: 2\".")
        }
        replace-by = named-replacements.at(name)
      }
      replace-by = _generate-replacement(f.name, extras, replace-by, pos-replacements: num-replacements, named-replacements: named-replacements, fmt-decimal-separator: fmt-decimal-separator)
      replace-span = f.span
    } else {
      panic("String formatter error: Internal error (unexpected format received).")
    }
    // {...}ABCABCABC{...}  <--- push ABCABCABC to parts
    parts.push(format.slice(last-span-end, replace-span.at(0)))
    // push the replacement string instead of the {...} at the end
    parts.push(replace-by)
    last-span-end = replace-span.at(1)
  }
  if last-span-end < format.len() {
    parts.push(format.slice(last-span-end, format.len()))
  }

  // join all the string parts (constant parts + formatted parts + escaped parts)
  parts.join()
}


////////
//////// ************ Report Code Start ************
////////


#let data = json(sys.inputs.at("file_path"))
#let companyJson = json(sys.inputs.at("file_path")).at("rs0").at(0)

//#let clientCode = data.rs2.at(0).at("ClientCode", default: "N/A").trim()
//#let clientName = data.rs2.at(0).at("ClientName", default: "N/A").trim()
#let clientCode = if (
  "rs2" in data and
  data.rs2 != none and
  type(data.rs2) == "array" and
  data.rs2.len() > 0 and
  data.rs2.at(0) != none
) {
  data.rs2.at(0).at("ClientCode", default: "N/A").trim()
} else {
   text("No Data Available", weight: "bold", size: 11pt)	 
}

#let clientName = if (
  "rs2" in data and
  data.rs2 != none and
  type(data.rs2) == "array" and
  data.rs2.len() > 0 and
  data.rs2.at(0) != none
) {
  data.rs2.at(0).at("ClientName", default: "N/A").trim()
} else {
   text("No Data Available",weight: "bold", size: 11pt)	 
}

// not used below code
#set page(
  width: 420mm,
  height: 600mm,
  footer: context [
    #set align(left)
    Print Date : #datetime.today().display(("[day]/[month]/[year]")
  ) #h(1fr) #numbering("1", ..counter(page).at(here()).map(x => x))
  ],
)

////// ***** table-client-info *** for Client header
#let table-client-info(data_pass) = {
  let table_data = data_pass

  if table_data == none or type(table_data) != "array" or table_data.len() == 0 {
   return [
     #v(180pt)
	  #align(center)[
       #text("⚠ Client Information not Available", weight: "bold", size: 18pt, fill: rgb(178, 34, 34)) 
	 ]
    ]
  }
  let data = data_pass.at(0)
  table(
    columns: (1fr, 2fr, 1fr, 2fr),
    fill: (x, y) => if y == 0 { },
    align: (right, left, right, left),
    [Client Code:], table.cell(colspan: 3, align: left)[#data.ClientCode],
    [Client Name:], table.cell(colspan: 3, align: left)[#data.Client.trim()],
    [Address :], table.cell(colspan: 3, align: left)[#data.ClientAddress1],
    [], table.cell(colspan: 3, align: left)[#data.ClientAddress2],
    [], table.cell(colspan: 3, align: left)[#data.ClientAddress3],
    [], table.cell(colspan: 3, align: left)[#data.ClientCity],
    [Pin :], table.cell(colspan: 3, align: left)[#data.Clientpin],

  )
}


////// ***** generate-table *** for Report data

#let generate-table(data_pass, label, header) = {
  let table_data = data_pass

  if table_data == none or type(table_data) != "array" or table_data.len() == 0 {
 return [
     #v(180pt)
    #h(350pt)#text("⚠ No Data Available", weight: "bold", size: 18pt, fill: rgb(178, 34, 34)) 
  ]
  }

  let data1 = table_data.at(0)
	let visible_columns = header.filter(h => (h.at("ColumnHide", default: "N") == "N") )

	let keys = visible_columns.map(h => h.ColumnName)
	let new_header = visible_columns.map(h => h.ColumnHeading)
	let column_width = visible_columns.map(h => h.ColumnWidth * 1fr)
	let alignment = visible_columns.map(h => h.ColumnAlignement)
	let column_total = visible_columns.map(h => h.ColumnTotal)
	let DecimalPlace = visible_columns.map(h => h.DecimalPlace)
	let dateFormat = visible_columns.map(h => h.ColumnFormat)

	let group_Cols = header.filter(h => h.at("Columnstyle", default: "N") == "G").map(h => h.ColumnName)
	let firstGroupCol = group_Cols.at(0, default: "")

  [
    #text(weight: "bold", size: 11pt, label)	 
	 
	#let temperature = 25

   //#let filterGroup = table_data.map(row => group_Cols.map(k => row.at(k))) //// for multiple columns
   #let filterGroup = table_data.map(row => row.at(firstGroupCol, default: "")) //// for single columns

   #let filterCols  = filterGroup.fold((), (acc, val) => {
			  if acc.contains(val) {
				acc
			  } else {
				acc + (val,)
			  }
			})

        // handled Date format function
               #let normalize-date(raw_value) = {
                    let clean_value = str.trim(str(raw_value))

                    if str.len(clean_value) < 8 {
                      return "WrongDateFormat"
                    }

                    // Detect separators
                    if str.contains(clean_value, "-") or str.contains(clean_value, "/") or str.contains(clean_value, ".") {
                      let sep = if str.contains(clean_value, "-") { "-" } 
                                else if str.contains(clean_value, "/") { "/" } 
                                else { "." }
                      let parts = clean_value.split(sep)

                      if parts.len() == 3 {
                        let first = parts.at(0)
                        let second = parts.at(1)
                        let third = parts.at(2)

                        if str.len(first) == 4 {
                          // yyyy-MM-dd
                          return third + "/" + second + "/" + first
                        } else {
                          // dd-MM-yyyy
                          return first + "/" + second + "/" + third
                        }
                      }
                        return "wrongDateFormat"
                    }

                    // No separator → yyyyMMdd
                    let year = str.slice(clean_value, 0, 4)
                    let month = str.slice(clean_value, 4, 6)
                    let day = str.slice(clean_value, 6, 8)
                    return day + "/" + month + "/" + year
                  }

            #let wrap-text(v) = {
                let s = str(v).trim()
                // Insert zero-width space between each character
                 let stretched = s.split("").join("\u{200B}")
                text(stretched)
              }

  #for cat in filterCols {
  
		  text(cat, weight: "bold", size: 4mm)  	

          table(
                columns: column_width,
                align: alignment.map(a => if a == "L" { left } else if a == "R" { right } else { left }),	  
                table.header(repeat: true, ..new_header), 
                ..table_data.filter(h => cat == "" or h.at(firstGroupCol) == cat)
                .map(row => {				
                    // keys.map(key => str(row.at(key, default: "[N/A]")))
                keys.enumerate().map(((i, key)) => {
                  
                  let value = row.at(key, default: "[N/A]")
				  let fmt = dateFormat.at(i)

                  // Check if format is a date format string like "dd/MM/yyyy"
                  //if fmt != ""  {
                    //fmt.replace("yyyy", str.slice(value, 0, 4)).replace("MM", str.slice(value, 4, 6)).replace("dd", str.slice(value, 6, 8))
                 // }
                   if fmt != "" {
                                        let normalized_value = normalize-date(value) // Normalize date
                                        let final_date = fmt
                                          .replace("dd", str.slice(normalized_value, 0, 2))
                                          .replace("MM", str.slice(normalized_value, 3, 5))
                                          .replace("yyyy", str.slice(normalized_value, 6, 10))
                                           text(final_date)
                                }
                  // Handle numeric formatting with decimal places
                  
                  else	if (type(value) == "float") {
                    let decimals = DecimalPlace.at(i)
                      let fmt = "{0:." + str(decimals) + "}"
                      text(strfmt(fmt, value))
                    }
                              
                  else {
                  //str(value)
                  wrap-text(value)
                    }
                })
                  })
                  .flatten(),
                      table.footer(
                                  repeat: false,
                              text("Total", weight: "bold"),
                                  ..column_total
                                .slice(1)
                                    .enumerate()
                                    .map(((i, val)) => {	 
                                      if val == "Y" {
                                        let key = keys.at(i + 1)
                                        let total = data_pass.filter(h => cat == "" or h.at(firstGroupCol) == cat)
                                  .map(row => {
                                  let value = row.at(key, default: "0")
                                    
                                            if type(value) == "str" {
                                              eval(value)
                                            } else {
                                               value
                                            }
                                          })
                                .fold(0, (acc, val) => acc + val)

                                        let decimals = DecimalPlace.at(i + 1)
                                        let fmt = "{0:." + str(decimals) + "}"
                                        text(strfmt(fmt, total), weight: "bold")
                                      } 
                                else {
                                        text("", weight: "bold")
                                      }
                                    }),
                                )
                   )
	
	        }
	
    #v(0.5cm)
  ]
}

////// ***** extract-and-generate *** Main function to call sub function and bind report

#let extract-and-generate(json_data) = {

   //// Extract company settings from rs0
  let companyJson = json_data.at("rs0").at(0)

  //// === Step 1: Get orientation and page size from JSON with defaults ===
  let orientation = companyJson.at("PageOrientation", default: "Portrait").trim()
  let page_size = companyJson.at("PageSize", default: "A4").trim()

  //// === Step 2: Determine base width from PageSize ===
  // A4 Portrait = 290mm, A4 Landscape = 350mm
  // A3 Portrait = 350mm, A3 Landscape = 420mm
  let base_width = if page_size == "A3" {
    if orientation == "Landscape" { 420mm } else { 350mm }
  } else {
    if orientation == "Landscape" { 350mm } else { 290mm }
  }

  //// === Step 3: If total columns > 15, override width to 500mm ===
  let total_columns = json_data.at("rs1").len()  // rs1 is assumed to contain header columns
  let final_width = if total_columns > 15 { 500mm } else { base_width }

  //// === Step 4: Set the page with fixed height (310mm) and dynamic width ===
  set page(
    width: final_width,
    height: 310mm,            // Fixed height
    margin: (top: 2cm, x: 2cm),
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
          Billing Address\
          #companyJson.BillingAddress \
        // GST No: Gst No Here \
        \

          #text(companyJson.ReportName, weight: "bold", size: 4mm)  // Hemant
        ],
        grid[],
      )
      
      let rs2 = json_data.at("rs0")
		let UniqueCodes = ()
		for entry in rs2 {
		  UniqueCodes.push(entry.ClientCode)
		}

	  //let uniqueClientCodes = set(codes).to-array()
	  //let uniqueCodes = set(rs2.map((x) => x.ClientCode)).to-array() 
      //let UniqueCodes = json_data.at("rs2").map(entry => entry.ClientCode)
	  
      let grid_keys = json_data.at("rs0").at(0).keys().filter(k => k.starts-with("Grid"))
      let column_keys = grid_keys.map(k => str.replace(k, "Grid", "rs"))
      let keys = json_data.keys().filter(key => key != "rs0" and not (column_keys.contains(key)))

      //let cond = json_data.at("column_keys").map(entry => entry.ColumnHide)
      //UniqueCodes.join(", ")
      ///// column_keys = "rs1"  , UniqueCodes = "clientCode", 
 
      for (index, codes) in UniqueCodes.enumerate() {
        let filtered = json_data.at("rs0").filter(row => row.ClientCode.trim() == codes)
		
        table-client-info(filtered)

        for ((inner_index, value), grid_key) in keys.enumerate().zip(grid_keys) {
      
          let header = json_data.at(column_keys.at(inner_index))
        //let showHead =  header.map(h => h.ColumnHide)
        
          let filtered = json_data.at(value).filter(row => row.ClientCode.trim() == codes)		 
          generate-table(filtered, json_data.at("rs0").at(0).at(grid_key), header)
            
        } 
        if index < (UniqueCodes.len() - 1) {
          pagebreak()
        }
      }
}




#set text(8pt, font: "Arial")
#show table.cell.where(y: 0): set text(weight: "bold")
#set table(
  fill: (x, y) => if y == 0 { gray.lighten(40%) },
  align: right,
)


#extract-and-generate(data)

