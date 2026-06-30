module PNobre.NortonGuides.App.UITests.ScreenshotTests

open System
open System.IO
open Avalonia.Headless
open Avalonia.Headless.XUnit
open Avalonia.Threading
open Xunit
open PNobre.NortonGuides.App

// When FNG_UI_SHOTS points at a directory, captured frames are written there as PNGs;
// otherwise the tests just assert that the UI renders.
let private dumpDir = Environment.GetEnvironmentVariable "FNG_UI_SHOTS"

let private capture (window: Avalonia.Controls.Window) (name: string) =
    window.Show()
    Dispatcher.UIThread.RunJobs()
    let frame = window.CaptureRenderedFrame()
    Assert.True(frame.Size.Width > 0.0 && frame.Size.Height > 0.0)

    if not (String.IsNullOrEmpty dumpDir) then
        Directory.CreateDirectory dumpDir |> ignore
        frame.Save(Path.Combine(dumpDir, name))

[<AvaloniaFact>]
let ``Main window renders`` () =
    capture (MainWindow()) "main-window.png"
