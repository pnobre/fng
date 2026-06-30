namespace PNobre.NortonGuides.App.UITests

open Avalonia
open Avalonia.Headless
open Avalonia.Headless.XUnit
open PNobre.NortonGuides.App

/// Boots the real app under Avalonia's headless platform with Skia drawing, so the
/// UI renders to a bitmap with no display or OS screen-capture permissions.
type TestAppBuilder() =
    static member BuildAvaloniaApp() : AppBuilder =
        AppBuilder.Configure<App>().UseSkia().UseHeadless(AvaloniaHeadlessPlatformOptions(UseHeadlessDrawing = false))

module internal AssemblyAttributes =
    [<assembly: AvaloniaTestApplication(typeof<TestAppBuilder>)>]
    do ()
