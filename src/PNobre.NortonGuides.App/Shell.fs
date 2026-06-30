module PNobre.NortonGuides.App.Shell

open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Primitives
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

/// A view position: the short entry whose lines fill the middle pane, and the
/// entry being read in the content pane.
type Nav =
    { List: Entry option
      Content: Entry option }

type Model =
    { State: GuideState
      Source: string option
      Nav: Nav
      Back: Nav list
      Forward: Nav list }

type Msg =
    | OpenRequested
    | OpenCancelled
    | Opened of name: string * result: Result<Guide, string>
    | Navigate of offset: int
    | NavPrevious
    | NavNext
    | NavParent
    | GoBack
    | GoForward

let private emptyNav = { List = None; Content = None }

let init () =
    { State = Empty
      Source = None
      Nav = emptyNav
      Back = []
      Forward = [] },
    Cmd.none

/// Move to the entry at `offset`, pushing the current position onto the back stack.
let private navigateTo (offset: int) (model: Model) =
    match model.State with
    | Loaded guide ->
        match guide |> Guide.entryAt offset with
        | Some entry ->
            let nav =
                match entry.Body with
                | Short _ -> { List = Some entry; Content = None }
                | Long _ -> { model.Nav with Content = Some entry }

            { model with
                Nav = nav
                Back = model.Nav :: model.Back
                Forward = [] }
        | None -> model
    | _ -> model

/// The offset reached by following a link on the focused entry (the one being read,
/// else the current list), or None if there is none. Used to enable/run Prev/Next/Up.
let focusedTarget (select: Entry -> int) (model: Model) : int option =
    model.Nav.Content
    |> Option.orElse model.Nav.List
    |> Option.map select
    |> Option.filter (fun offset -> offset > 0)

let private navByLink select model =
    match focusedTarget select model with
    | Some offset -> navigateTo offset model, Cmd.none
    | None -> model, Cmd.none

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
        match result with
        | Ok guide ->
            { model with
                State = Loaded guide
                Source = Some name
                Nav = emptyNav
                Back = []
                Forward = [] },
            Cmd.none
        | Error e ->
            { model with
                State = Failed e
                Source = Some name },
            Cmd.none
    | Navigate offset -> navigateTo offset model, Cmd.none
    | NavPrevious -> navByLink (fun e -> e.Links.Previous) model
    | NavNext -> navByLink (fun e -> e.Links.Next) model
    | NavParent -> navByLink (fun e -> e.Links.ParentOffset) model
    | GoBack ->
        match model.Back with
        | prev :: rest ->
            { model with
                Nav = prev
                Back = rest
                Forward = model.Nav :: model.Forward },
            Cmd.none
        | [] -> model, Cmd.none
    | GoForward ->
        match model.Forward with
        | next :: rest ->
            { model with
                Nav = next
                Forward = rest
                Back = model.Nav :: model.Back },
            Cmd.none
        | [] -> model, Cmd.none

let private monospace = FontFamily("Menlo, Consolas, Courier New, monospace")

// The DOS 16-colour (CGA) palette, indexed by attribute nibble.
let private dosPalette =
    [| "#000000"
       "#0000AA"
       "#00AA00"
       "#00AAAA"
       "#AA0000"
       "#AA00AA"
       "#AA5500"
       "#AAAAAA"
       "#555555"
       "#5555FF"
       "#55FF55"
       "#55FFFF"
       "#FF5555"
       "#FF55FF"
       "#FFFF55"
       "#FFFFFF" |]
    |> Array.map (fun hex -> SolidColorBrush(Color.Parse hex) :> IBrush)

// Entry text renders on a dark "terminal" background, where the DOS colours read true.
let private contentBackground = SolidColorBrush(Color.Parse "#1E1E1E") :> IBrush
let private defaultForeground = SolidColorBrush(Color.Parse "#D4D4D4") :> IBrush

let private brushes (style: Style) =
    let fg, bg =
        match style.Colour with
        | Some c -> dosPalette[c.Foreground &&& 15], dosPalette[c.Background &&& 15]
        | None -> defaultForeground, contentBackground

    if style.Reverse then bg, fg else fg, bg

let private spanBlock (span: Span) : IView =
    let fg, bg = brushes span.Style

    TextBlock.create
        [ TextBlock.text span.Text
          TextBlock.fontFamily monospace
          TextBlock.foreground fg
          TextBlock.background bg
          if span.Style.Bold then
              TextBlock.fontWeight FontWeight.Bold
          if span.Style.Underline then
              TextBlock.textDecorations TextDecorations.Underline ]

let private listButton dispatch (link: Link) : IView =
    Button.create
        [ Button.content (Text.plain link.Text)
          Button.background Brushes.Transparent
          Button.horizontalAlignment HorizontalAlignment.Stretch
          Button.horizontalContentAlignment HorizontalAlignment.Left
          Button.padding (Thickness(8.0, 4.0))
          Button.onClick (fun _ -> dispatch (Navigate link.Offset)) ]

let private hint (text: string) : IView =
    TextBlock.create
        [ TextBlock.text text
          TextBlock.foreground Brushes.Gray
          TextBlock.margin (Thickness 12.0) ]

let private scroll (children: IView list) : IView =
    ScrollViewer.create [ ScrollViewer.content (StackPanel.create [ StackPanel.children children ]) ]

let private menuPane dispatch (guide: Guide) : IView =
    scroll
        [ for menu in guide.Header.Menus do
              TextBlock.create
                  [ TextBlock.text menu.Title
                    TextBlock.fontWeight FontWeight.Bold
                    TextBlock.margin (Thickness(8.0, 10.0, 8.0, 2.0)) ]

              yield! menu.Prompts |> List.map (listButton dispatch) ]

let private listPane dispatch (entry: Entry option) : IView =
    match entry with
    | Some { Body = Short lines } -> scroll (lines |> List.map (listButton dispatch))
    | _ -> hint "Pick a menu entry."

let private contentLine (line: string) : IView =
    let blocks =
        match Text.decode line with
        | [] -> [ TextBlock.create [ TextBlock.text " "; TextBlock.fontFamily monospace ] :> IView ] // keep blank lines tall
        | spans -> spans |> List.map spanBlock

    StackPanel.create [ StackPanel.orientation Orientation.Horizontal; StackPanel.children blocks ]

let private seeAlsoLink dispatch (link: Link) : IView =
    Button.create
        [ Button.content (Text.plain link.Text)
          Button.background Brushes.Transparent
          Button.foreground (SolidColorBrush(Color.Parse "#5599FF"))
          Button.horizontalAlignment HorizontalAlignment.Left
          Button.padding (Thickness(0.0, 2.0))
          Button.onClick (fun _ -> dispatch (Navigate link.Offset)) ]

let private contentPane dispatch (entry: Entry option) : IView =
    let inner =
        match entry with
        | None -> hint "Select an entry to read."
        | Some entry ->
            let lines, seeAlso =
                match entry.Body with
                | Long(text, sa) -> text, sa
                | Short links -> (links |> List.map _.Text), []

            let seeAlsoViews =
                if List.isEmpty seeAlso then
                    []
                else
                    (TextBlock.create
                        [ TextBlock.text "See also:"
                          TextBlock.foreground defaultForeground
                          TextBlock.fontWeight FontWeight.Bold
                          TextBlock.margin (Thickness(0.0, 12.0, 0.0, 4.0)) ]
                    :> IView)
                    :: (seeAlso |> List.map (seeAlsoLink dispatch))

            ScrollViewer.create
                [ ScrollViewer.horizontalScrollBarVisibility ScrollBarVisibility.Auto
                  ScrollViewer.content (
                      StackPanel.create [ StackPanel.children ((lines |> List.map contentLine) @ seeAlsoViews) ]
                  ) ]

    Border.create [ Border.background contentBackground; Border.child inner ]

let private pane column (child: IView) : IView =
    Border.create [ Grid.column column; Border.child child ]

let private threePane dispatch (model: Model) (guide: Guide) : IView =
    Grid.create
        [ Grid.columnDefinitions "260,4,300,4,*"
          Grid.children
              [ pane 0 (menuPane dispatch guide)
                GridSplitter.create [ Grid.column 1; GridSplitter.background Brushes.DimGray ]
                pane 2 (listPane dispatch model.Nav.List)
                GridSplitter.create [ Grid.column 3; GridSplitter.background Brushes.DimGray ]
                pane 4 (contentPane dispatch model.Nav.Content) ] ]

let private body (model: Model) (dispatch: Msg -> unit) : IView =
    match model.State with
    | Empty -> hint "No guide loaded — click Open to load a .NG file."
    | Failed e ->
        TextBlock.create
            [ TextBlock.text $"Could not open guide: {e}"
              TextBlock.foreground Brushes.IndianRed
              TextBlock.textWrapping TextWrapping.Wrap
              TextBlock.margin (Thickness 12.0) ]
    | Loaded guide -> threePane dispatch model guide

let private actionButton dispatch (label: string) enabled msg : IView =
    Button.create
        [ Button.content label
          Button.isEnabled enabled
          Button.onClick (fun _ -> dispatch msg) ]

let view (model: Model) (dispatch: Msg -> unit) =
    let title =
        match model.State with
        | Loaded guide -> guide.Header.Title
        | _ -> model.Source |> Option.defaultValue ""

    let canFollow select = (focusedTarget select model).IsSome

    let toolbar =
        Border.create
            [ Border.dock Dock.Top
              Border.background (SolidColorBrush(Color.Parse "#2D2D30"))
              Border.padding (Thickness 8.0)
              Border.child (
                  StackPanel.create
                      [ StackPanel.orientation Orientation.Horizontal
                        StackPanel.spacing 6.0
                        StackPanel.children
                            [ Button.create [ Button.content "Open…"; Button.onClick (fun _ -> dispatch OpenRequested) ]
                              actionButton dispatch "◀" (not (List.isEmpty model.Back)) GoBack
                              actionButton dispatch "▶" (not (List.isEmpty model.Forward)) GoForward
                              actionButton dispatch "Prev" (canFollow (fun e -> e.Links.Previous)) NavPrevious
                              actionButton dispatch "Next" (canFollow (fun e -> e.Links.Next)) NavNext
                              actionButton dispatch "Up" (canFollow (fun e -> e.Links.ParentOffset)) NavParent
                              TextBlock.create
                                  [ TextBlock.text title
                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                    TextBlock.margin (Thickness(8.0, 0.0, 0.0, 0.0))
                                    TextBlock.foreground Brushes.LightGray ] ] ]
              ) ]

    DockPanel.create [ DockPanel.children [ toolbar; body model dispatch ] ]
