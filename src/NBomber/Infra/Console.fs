module internal NBomber.Infra.Console

open System

open Spectre.Console
open Spectre.Console.Rendering

let render (renderable: IRenderable) =
    AnsiConsole.Render(renderable)

let highlightPrimary (text) =
    $"[lime]{text}[/]"

let highlightSecondary (text) =
    $"[deepskyblue1]{text}[/]"

let highlightSuccess (text) =
    $"[lime]{text}[/]"

let highlightWarning (text) =
    $"[yellow]{text}[/]"

let highlightDanger (text) =
    $"[red]{text}[/]"

let bold (text) =
    $"[bold]{text}[/]"

let escapeMarkup (text) =
    Markup.Escape(text)

let addLine (text) =
    Markup($"{text}{Environment.NewLine}") :> IRenderable

let addLogo (logo) =
    FigletText(logo) :> IRenderable

let addHeader (header) =
    let rule = Rule(header)
    rule.Centered() |> ignore
    rule :> IRenderable

let addList (items: string seq seq) =
    items
    |> Seq.mapi(fun i renderables ->
        let listItems =
            renderables
            |> Seq.map(fun renderable -> [Markup(renderable) :> IRenderable; addLine(String.Empty)])
            |> Seq.concat

        [ if i > 0 then addLine(String.Empty)
          yield! listItems ]
    )
    |> Seq.concat
    |> List.ofSeq

let addTable (headers: string list) (rows: string list list) =
    let table = Table()
    table.Border <- TableBorder.Square

    headers
    |> Seq.iteri(fun i header ->
        let col = TableColumn(header)

        if i = 0 then
            col.RightAligned() |> ignore
        elif i = headers.Length - 1 then
            col.LeftAligned() |> ignore
        else
            col.Centered() |> ignore

        table.AddColumn(col) |> ignore
    )

    rows
    |> Seq.iter(fun row ->
        row
        |> Seq.map(fun col -> Markup(col) :> IRenderable)
        |> table.AddRow
        |> ignore
    )

    table :> IRenderable
