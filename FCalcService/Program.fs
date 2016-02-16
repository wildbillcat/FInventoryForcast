// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

//#r "FSharp.Configuration"
//#r "System.Configuration"

open System.Configuration
open System.IO
open FSharp.Configuration

type Settings = AppSettings<"app.config">

let removeSeasonality (x:double) (y:int) = 
    match y with
    // The following line contains literal patterns combined with an OR pattern.
    1 -> x / Settings.JanuarySeasonality
    | 2 -> x / Settings.FebruarySeasonality
    | 3 -> x / Settings.MarchSeasonality
    | 4 -> x / Settings.AprilSeasonality
    | 5 -> x / Settings.MaySeasonality
    | 6 -> x / Settings.JuneSeasonality
    | 7 -> x / Settings.JulySeasonality
    | 8 -> x / Settings.AugustSeasonality
    | 9 -> x / Settings.SeptemberSeasonality
    | 10 -> x / Settings.OctoberSeasonality
    | 11 -> x / Settings.NovemberSeasonality
    | 12 -> x / Settings.DecemberSeasonality
    | _ -> x

//let csv = 
//    let titanic1 = CsvProvider<"../data/Titanic.csv", Schema=",,Passenger Class,,,float">.GetSample()
//    for row in titanic1.Rows do
//      printfn "%s Class = %d Fare = %g" row.Name row.``Passenger Class`` row.Fare



[<EntryPoint>]
let main argv = 
    let dir = new DirectoryInfo(Settings.InputFiles)
    printfn "%A" argv
    0 // return an integer exit code
