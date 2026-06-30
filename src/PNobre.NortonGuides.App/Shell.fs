module PNobre.NortonGuides.App.Shell

open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Media
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open PNobre.NortonGuides.Core

/// Picks a file and returns its name and bytes, or None if cancelled. The host
/// implements this so file I/O stays out of Core (and tests can inject a stub).
type OpenFile = unit -> Async<(string * byte[]) option>

type GuideState =
    | Empty
    | Loaded of Guide
    | Failed of string

type Model =
    { State: GuideState
      Source: string option }

type Msg =
    | OpenRequested
    | OpenCancelled
    | Opened of name: string * result: Result<Guide, string>

let init () =
    { State = Empty; Source = None }, Cmd.none

let update (openFile: OpenFile) (msg: Msg) (model: Model) =
    match msg with
    | OpenRequested ->
        let pick () =
            async {
                match! openFile () with
                | Some(name, bytes) -> return Opened(name, Guide.parse bytes)
                | None -> return OpenCancelled
            }

        // Run on the dispatching (UI) thread: Cmd.OfAsync hops to a thread pool,
        // where the file dialog and the follow-up dispatch both misbehave.
        let run dispatch =
            Async.StartImmediate(
                async {
                    let! msg = pick ()
                    dispatch msg
                }
            )

        model, [ run ]
    | OpenCancelled -> model, Cmd.none
    | Opened(name, result) ->
        let state =
            match result with
            | Ok guide -> Loaded guide
            | Error e -> Failed e

        { State = state; Source = Some name }, Cmd.none

let private guideInfo (guide: Guide) : IView =
    let title =
        TextBlock.create
            [ TextBlock.text guide.Header.Title
              TextBlock.fontSize 24.0
              TextBlock.fontWeight FontWeight.Bold ]
        :> IView

    let credits =
        guide.Header.Credits
        |> List.filter (fun c -> c.Trim() <> "")
        |> List.map (fun c ->
            TextBlock.create [ TextBlock.text (c.Trim()); TextBlock.foreground Brushes.Gray ] :> IView)

    let summary =
        TextBlock.create
            [ TextBlock.text $"{guide.Entries.Length} entries · {guide.Header.Menus.Length} menus"
              TextBlock.margin (Thickness(0.0, 12.0, 0.0, 0.0)) ]
        :> IView

    StackPanel.create
        [ StackPanel.spacing 6.0
          StackPanel.children ((title :: credits) @ [ summary ]) ]

let private content (model: Model) : IView =
    match model.State with
    | Empty ->
        TextBlock.create
            [ TextBlock.text "No guide loaded — click Open to load a .NG file"
              TextBlock.foreground Brushes.Gray ]
    | Failed e ->
        TextBlock.create
            [ TextBlock.text $"Could not open guide: {e}"
              TextBlock.foreground Brushes.IndianRed
              TextBlock.textWrapping TextWrapping.Wrap ]
    | Loaded guide -> guideInfo guide

let view (model: Model) (dispatch: Msg -> unit) =
    let toolbar =
        Border.create
            [ Border.dock Dock.Top
              Border.background (SolidColorBrush(Color.Parse "#2D2D30"))
              Border.padding (Thickness 8.0)
              Border.child (
                  StackPanel.create
                      [ StackPanel.orientation Orientation.Horizontal
                        StackPanel.spacing 12.0
                        StackPanel.children
                            [ Button.create [ Button.content "Open…"; Button.onClick (fun _ -> dispatch OpenRequested) ]
                              TextBlock.create
                                  [ TextBlock.text (model.Source |> Option.defaultValue "")
                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                    TextBlock.foreground Brushes.LightGray ] ] ]
              ) ]

    DockPanel.create
        [ DockPanel.children
              [ toolbar
                Border.create [ Border.padding (Thickness 24.0); Border.child (content model) ] ] ]
