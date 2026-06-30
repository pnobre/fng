module PNobre.NortonGuides.App.UITests.ScreenshotTests

open System
open System.IO
open Avalonia.Controls
open Avalonia.Headless
open Avalonia.Headless.XUnit
open Avalonia.Threading
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.VirtualDom
open Xunit
open PNobre.NortonGuides.Core
open PNobre.NortonGuides.App

// When FNG_UI_SHOTS points at a directory, captured frames are written there as PNGs;
// otherwise the tests just assert that the UI renders.
let private dumpDir = Environment.GetEnvironmentVariable "FNG_UI_SHOTS"

let private mouseGuide =
    match Guide.parse (File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fixtures", "MOUSE.NG"))) with
    | Ok g -> g
    | Error e -> failwith e

let private capture (window: Window) (name: string) =
    window.Show()
    Dispatcher.UIThread.RunJobs()
    let frame = window.CaptureRenderedFrame()
    Assert.True(frame.Size.Width > 0.0 && frame.Size.Height > 0.0)

    if not (String.IsNullOrEmpty dumpDir) then
        Directory.CreateDirectory dumpDir |> ignore
        frame.Save(Path.Combine(dumpDir, name))

let private modelWindow (model: Shell.Model) =
    Window(Width = 1000.0, Height = 700.0, Content = VirtualDom.create (Shell.view model ignore :> IView))

let private state s source : Shell.Model =
    { State = s
      Source = source
      Nav = { List = None; Content = None }
      Back = []
      Forward = [] }

[<AvaloniaFact>]
let ``Main window boots`` () =
    capture (MainWindow(fun () -> async { return None })) "main-window.png"

[<AvaloniaFact>]
let ``Loaded guide shows the three-pane layout`` () =
    let populated =
        { state (Shell.Loaded mouseGuide) (Some "MOUSE.NG") with
            Nav =
                { List = Guide.entryAt 488 mouseGuide
                  Content = Guide.entryAt 1335 mouseGuide } }

    capture (modelWindow populated) "three-pane.png"

[<AvaloniaFact>]
let ``Content renders DOS colours`` () =
    let links =
        { Previous = 0
          Next = 0
          ParentOffset = 0
          ParentLine = 0
          ParentMenu = 0
          ParentPrompt = 0 }

    // ^A1E: yellow (E) on blue (1); ^A4F: white (F) on red (4).
    let entry =
        { Offset = 0
          Links = links
          Body = Long([ "^A1E Yellow on blue   ^A4F White on red " ], []) }

    let model =
        { state (Shell.Loaded mouseGuide) (Some "demo") with
            Nav = { List = None; Content = Some entry } }

    capture (modelWindow model) "colour.png"

[<AvaloniaFact>]
let ``Content shows see-also links`` () =
    let links =
        { Previous = 0
          Next = 0
          ParentOffset = 0
          ParentLine = 0
          ParentMenu = 0
          ParentPrompt = 0 }

    let entry =
        { Offset = 0
          Links = links
          Body =
            Long(
                [ " ^bMouse Reset^b"; ""; " Resets the driver." ],
                [ { Text = "Show Mouse Cursor"
                    Offset = 1650 }
                  { Text = "Hide Mouse Cursor"
                    Offset = 1795 } ]
            ) }

    let model =
        { state (Shell.Loaded mouseGuide) (Some "demo") with
            Nav = { List = None; Content = Some entry } }

    capture (modelWindow model) "see-also.png"

[<AvaloniaFact>]
let ``Failed open renders an error`` () =
    capture (modelWindow (state (Shell.Failed "not a Norton Guide file (magic: # )") (Some "README.md"))) "failed.png"
