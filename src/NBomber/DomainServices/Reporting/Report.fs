module internal NBomber.DomainServices.Reporting.Report

open System.Collections.Generic
open System.IO

open Serilog
open Spectre.Console.Rendering

open NBomber.Configuration
open NBomber.Contracts
open NBomber.Contracts.Stats
open NBomber.Extensions.InternalExtensions

type ReportsContent = {
    TxtReport: string
    HtmlReport: string
    CsvReport: string
    MdReport: string
    ConsoleReport: IRenderable seq
    SessionFinishedWithErrors: bool
}

let build (sessionResult: NodeSessionResult)
          (simulations: IDictionary<string, LoadSimulation list>) =

    let errorsExist = sessionResult.NodeStats.ScenarioStats |> Array.exists(fun stats -> stats.FailCount > 0)

    { TxtReport = TxtReport.print sessionResult simulations
      HtmlReport = HtmlReport.print sessionResult
      CsvReport = CsvReport.print sessionResult
      MdReport = MdReport.print sessionResult simulations
      ConsoleReport = ConsoleReport.print sessionResult simulations
      SessionFinishedWithErrors = errorsExist }

let save (folder: string, fileName: string, reportFormats: ReportFormat list,
          report: ReportsContent, logger: ILogger, testInfo: TestInfo) =
    try
        let reportsDir = Path.Combine(folder, testInfo.SessionId)
        Directory.CreateDirectory(reportsDir) |> ignore

        let buildReportFile (format: ReportFormat) =
            let fileExt =
                match format with
                | ReportFormat.Txt  -> ".txt"
                | ReportFormat.Html -> ".html"
                | ReportFormat.Csv  -> ".csv"
                | ReportFormat.Md   -> ".md"
                | _                 -> failwith "invalid report format."

            let filePath = Path.Combine(reportsDir, fileName) + fileExt
            { FilePath = filePath; ReportFormat = format }

        let reportFiles = reportFormats |> Seq.map(buildReportFile) |> Seq.toArray

        reportFiles
        |> Seq.map(fun x ->
            match x.ReportFormat with
            | ReportFormat.Txt  -> {| Content = report.TxtReport; FilePath = x.FilePath |}
            | ReportFormat.Html -> {| Content = report.HtmlReport; FilePath = x.FilePath |}
            | ReportFormat.Csv  -> {| Content = report.CsvReport; FilePath = x.FilePath |}
            | ReportFormat.Md   -> {| Content = report.MdReport; FilePath = x.FilePath |}
            | _                 -> failwith "invalid report format."
        )
        |> Seq.iter(fun x -> File.WriteAllText(x.FilePath, x.Content))

        if report.SessionFinishedWithErrors then
            logger.Warning("Test finished with errors, please check the log file.")

        if reportFiles.Length > 0 then
            logger.Information("Reports saved in folder: '{0}'", DirectoryInfo(reportsDir).FullName)

        reportFiles
    with
    | ex -> logger.Error(ex, "Report.save failed")
            Array.empty
