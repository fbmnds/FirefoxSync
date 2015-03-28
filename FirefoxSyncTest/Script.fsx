#r @"..\FirefoxSync\bin\Release\FirefoxSync.dll"
#r @"..\packages\FSharp.Data.2.0.8\lib\net40\FSharp.Data.dll"
#r @"System.dll"
#r @"System.Core.dll"
#r @"System.Numerics.dll"
#r @"C:\Windows\assembly\GAC_MSIL\Microsoft.Office.Interop.Excel\15.0.0.0__71e9bce111e9429c\Microsoft.Office.Interop.Excel.dll"

open System
open FSharp.Data
open Microsoft.Office.Interop.Excel
open FirefoxSync


[<Literal>]
let passwordItem = """
 {
  "hostname" : "http://www.site.de",
  "formSubmitURL" : "http://www.site.de",
  "httpRealm" : "***",
  "username" : "***",
  "password" : "***",
  "usernameField" : "***",
  "passwordField" : "***"
}
"""


type Password = JsonProvider<passwordItem, RootName = "pw">


let secrets = 
    SecretStore.setSecretByDefaultFile()
    |> Results.setOrFail

let uriToString uri = uri |> sprintf "%A" 

let passwords =    
    CryptoKey.getCryptokeysFromFile secrets None 
    |> Results.setOrFail
    |> Collections.getPasswords secrets
    |> Results.setOrFail


let passwordFile =
    Environment.GetEnvironmentVariable("SECRETS") + @"\FirefoxPasswords.json"

passwords
|> fun x -> [| for p in x do 
                  yield Password.Pw((p.hostname |> uriToString),
                                    (p.formSubmitURL |> uriToString),
                                    p.httpRealm,
                                    p.username,
                                    p.password,
                                    p.usernameField,
                                    p.passwordField).ToString() |]
|> String.concat ","
|> sprintf("{ \"passwords\":[%s] }")
|> Utilities.writeStringToFile false passwordFile


let passwordFileXlsx =
    Environment.GetEnvironmentVariable("SECRETS") + @"\FirefoxPasswords.xlsx"


let app = new ApplicationClass(Visible = true) 

let workbook = app.Workbooks.Add(XlWBATemplate.xlWBATWorksheet) 
let worksheet = (workbook.Worksheets.[1] :?> Worksheet)

let titles = 
    [| "hostname"
       "formSubmitURL" 
       "httpRealm"
       "username"
       "password"
       "usernameField"
       "passwordField" |]

worksheet.Range("B2", "H2").Value2 <- titles

for i = 0 to (passwords.Length - 1) do
  let p = passwords.[i]
  let data = 
      [| (p.hostname |> uriToString)
         (p.formSubmitURL |> uriToString)
         p.httpRealm
         p.username
         p.password
         p.usernameField
         p.passwordField |]
  worksheet.Range((sprintf "B%i" (i+3)), (sprintf "H%i" (i+3))).Value2 <- data

worksheet.SaveAs passwordFileXlsx
