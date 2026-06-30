namespace PNobre.NortonGuides.App

open System.IO
open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Platform.Storage
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.Elmish

module private FilePicker =

    /// Show the open-file dialog on `window` and read the chosen guide's bytes.
    let pickAndRead (window: Window) : Shell.OpenFile =
        fun () ->
            async {
                let options =
                    FilePickerOpenOptions(
                        Title = "Open a Norton Guide",
                        AllowMultiple = false,
                        FileTypeFilter = [| FilePickerFileType("Norton Guide", Patterns = [| "*.ng"; "*.NG" |]) |]
                    )

                let! files = window.StorageProvider.OpenFilePickerAsync options |> Async.AwaitTask

                match Seq.tryHead files with
                | Some file ->
                    use! stream = file.OpenReadAsync() |> Async.AwaitTask
                    use buffer = new MemoryStream()
                    do! stream.CopyToAsync buffer |> Async.AwaitTask
                    return Some(file.Name, buffer.ToArray())
                | None -> return None
            }

type MainWindow() as this =
    inherit HostWindow()

    do
        base.Title <- "fng — Norton Guides"
        base.Width <- 1000.0
        base.Height <- 700.0

        Program.mkProgram Shell.init (Shell.update (FilePicker.pickAndRead this)) Shell.view
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
