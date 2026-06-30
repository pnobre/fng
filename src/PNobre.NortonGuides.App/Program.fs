module PNobre.NortonGuides.App.Program

open Avalonia

[<EntryPoint>]
let main argv =
    AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().StartWithClassicDesktopLifetime(argv)
