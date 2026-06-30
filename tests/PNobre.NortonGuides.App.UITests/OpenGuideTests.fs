module PNobre.NortonGuides.App.UITests.OpenGuideTests

open System
open System.IO
open System.Threading
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Headless
open Avalonia.Headless.XUnit
open Avalonia.Threading
open Avalonia.VisualTree
open Xunit
open PNobre.NortonGuides.App

let private mouseBytes =
    File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fixtures", "MOUSE.NG"))

/// An OpenFile stub that returns fixed bytes instead of showing a real file dialog.
let private stub name bytes : Shell.OpenFile =
    fun () -> async { return Some(name, bytes) }

let private show (window: #Window) : Window =
    window.Show()
    Dispatcher.UIThread.RunJobs()
    window :> Window

let private firstButton (window: Window) =
    window.GetVisualDescendants()
    |> Seq.pick (function
        | :? Button as b -> Some b
        | _ -> None)

let private buttonWith (window: Window) (text: string) =
    window.GetVisualDescendants()
    |> Seq.tryPick (function
        | :? Button as b ->
            match b.Content with
            | :? string as s when s.Contains(text) -> Some b
            | _ -> None
        | _ -> None)

let private click (window: Window) (button: Button) =
    let centre =
        button.TranslatePoint(Point(button.Bounds.Width / 2.0, button.Bounds.Height / 2.0), window)

    let p = centre.Value
    window.MouseDown(p, MouseButton.Left, RawInputModifiers.None)
    window.MouseUp(p, MouseButton.Left, RawInputModifiers.None)

let private hasText (window: Window) (needle: string) =
    window.GetVisualDescendants()
    |> Seq.exists (function
        | :? TextBlock as tb -> not (isNull tb.Text) && tb.Text.Contains(needle)
        | _ -> false)

/// Pump the dispatcher until the predicate holds (the open flow runs an async command).
let private pumpUntil (predicate: unit -> bool) =
    let mutable ok = false
    let mutable i = 0

    while not ok && i < 100 do
        Dispatcher.UIThread.RunJobs()
        ok <- predicate ()

        if not ok then
            Thread.Sleep 10

        i <- i + 1

    ok

[<AvaloniaFact>]
let ``Clicking Open loads and shows the guide`` () =
    let window = show (MainWindow(stub "MOUSE.NG" mouseBytes))
    click window (firstButton window)
    Assert.True(pumpUntil (fun () -> hasText window "Mouse Services"), "guide title did not appear after clicking Open")

[<AvaloniaFact>]
let ``Clicking Open on a non-guide shows an error`` () =
    let window =
        show (MainWindow(stub "README.md" (Text.Encoding.ASCII.GetBytes "not a guide")))

    click window (firstButton window)
    Assert.True(pumpUntil (fun () -> hasText window "Could not open guide"), "error did not appear after clicking Open")

[<AvaloniaFact>]
let ``Selecting a menu prompt then a list item shows the entry`` () =
    let window = show (MainWindow(stub "MOUSE.NG" mouseBytes))
    click window (firstButton window) // Open
    Assert.True(pumpUntil (fun () -> (buttonWith window "Int 51 Services").IsSome), "menu did not load")

    click window (buttonWith window "Int 51 Services").Value // populate the middle list

    Assert.True(
        pumpUntil (fun () -> (buttonWith window "Reset Driver and Read Status").IsSome),
        "list did not populate"
    )

    click window (buttonWith window "Reset Driver and Read Status").Value // show the entry
    Assert.True(pumpUntil (fun () -> hasText window "INT 33"), "entry content did not appear")
