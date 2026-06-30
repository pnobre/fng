namespace PNobre.NortonGuides.App

open System.IO
open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Platform.Storage
open Avalonia.Themes.Fluent
open Avalonia.Threading
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.Elmish

module FilePicker =

    /// Show the open-file dialog and read the chosen guide's bytes. The StorageProvider
    /// must be used on the UI thread, so the whole task is marshalled there (an Elmish
    /// command otherwise runs it on a thread-pool thread and Avalonia throws).
    let read (window: Window) : Async<(string * byte[]) option> =
        Dispatcher.UIThread.Invoke(fun () ->
            task {
                let options =
                    FilePickerOpenOptions(
                        Title = "Open a Norton Guide",
                        AllowMultiple = false,
                        FileTypeFilter = [| FilePickerFileType("Norton Guide", Patterns = [| "*.ng"; "*.NG" |]) |]
                    )

                let! files = window.StorageProvider.OpenFilePickerAsync options

                match Seq.tryHead files with
                | Some file ->
                    use! stream = file.OpenReadAsync()
                    use buffer = new MemoryStream()
                    do! stream.CopyToAsync buffer
                    return Some(file.Name, buffer.ToArray())
                | None -> return None
            })
        |> Async.AwaitTask

/// The main window. `openFile` is injected so the real app uses the StorageProvider
/// while tests can drive the open flow with a stub.
type MainWindow(openFile: Shell.OpenFile) as this =
    inherit HostWindow()

    do
        base.Title <- "fng — Norton Guides"
        base.Width <- 1000.0
        base.Height <- 700.0

        Program.mkProgram Shell.init (Shell.update openFile) Shell.view
        |> Program.withHost this
        |> Program.run

type App() =
    inherit Application()

    override this.Initialize() = this.Styles.Add(FluentTheme())

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            // Resolve the window lazily so the picker runs against the shown main window.
            let openFile: Shell.OpenFile = fun () -> FilePicker.read desktop.MainWindow
            desktop.MainWindow <- MainWindow openFile
        | _ -> ()

        base.OnFrameworkInitializationCompleted()
