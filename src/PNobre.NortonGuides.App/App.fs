namespace PNobre.NortonGuides.App

open Elmish
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.Elmish

type MainWindow() as this =
    inherit HostWindow()

    do
        base.Title <- "fng — Norton Guides"
        base.Width <- 1000.0
        base.Height <- 700.0

        Program.mkProgram Shell.init Shell.update Shell.view
        |> Program.withHost this
        |> Program.run

type App() =
    inherit Application()

    override this.Initialize() = this.Styles.Add(FluentTheme())

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop -> desktop.MainWindow <- MainWindow()
        | _ -> ()

        base.OnFrameworkInitializationCompleted()
