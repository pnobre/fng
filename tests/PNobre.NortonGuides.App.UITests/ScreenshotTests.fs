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

let private stateWindow (state: Shell.GuideState) (source: string option) =
    let view = Shell.view { State = state; Source = source } ignore
    Window(Width = 1000.0, Height = 700.0, Content = VirtualDom.create (view :> IView))

[<AvaloniaFact>]
let ``Main window boots`` () =
    capture (MainWindow(fun () -> async { return None })) "main-window.png"

[<AvaloniaFact>]
let ``Loaded guide renders title and credits`` () =
    capture (stateWindow (Shell.Loaded mouseGuide) (Some "MOUSE.NG")) "loaded.png"

[<AvaloniaFact>]
let ``Failed open renders an error`` () =
    capture (stateWindow (Shell.Failed "not a Norton Guide file (magic: # )") (Some "README.md")) "failed.png"
