module PNobre.NortonGuides.App.Shell

open Elmish
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.FuncUI.DSL

// The MVU shell. For now it is an empty placeholder; opening and viewing a guide
// arrives in #20 onwards.

type Model = { Status: string }

type Msg = NoOp

let init () =
    { Status = "No guide loaded" }, Cmd.none

let update (msg: Msg) (model: Model) =
    match msg with
    | NoOp -> model, Cmd.none

let view (model: Model) (_dispatch: Msg -> unit) =
    DockPanel.create
        [ DockPanel.children
              [ TextBlock.create
                    [ TextBlock.text $"fng — Norton Guides viewer ({model.Status})"
                      TextBlock.horizontalAlignment HorizontalAlignment.Center
                      TextBlock.verticalAlignment VerticalAlignment.Center ] ] ]
