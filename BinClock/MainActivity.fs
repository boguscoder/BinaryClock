namespace BinClock

open System
open System.Threading

open Android.App
open Android.Content
open Android.OS
open Android.Util
open Android.Runtime
open Android.Views
open Android.Widget

type Column = 
    | HoursFst
    | HoursSnd
    | MinutesFst 
    | MinutesSnd 
    | SecondsFst 
    | SecondsSnd


[<Activity (Label = "Binary Clock", MainLauncher = true, Theme = "@android:style/Theme.NoTitleBar")>]
type MainActivity () =
    inherit Activity ()

    let ColumnSpec = [  (HoursFst, 2);
                        (HoursSnd, 4);
                        (MinutesFst, 3);
                        (MinutesSnd, 4);
                        (SecondsFst, 3);
                        (SecondsSnd, 4) ]

    let mutable gridRoot:ViewGroup = null

    member this.createGrid = 
        let columnContainer = new LinearLayout(this)
        columnContainer.Orientation <- Orientation.Horizontal
        columnContainer.SetGravity GravityFlags.Bottom

        ColumnSpec |> List.iter (fun (_, size) -> columnContainer.AddView (this.createColumn size))

        let root = this.LayoutInflater.Inflate(Resource_Layout.Main, null) :?> ViewGroup
        root.AddView(columnContainer)
        root

    member this.createColumn size = 
        let columnLayout = new LinearLayout(this)
        columnLayout.Orientation <- Orientation.Vertical
   
        let bulletSize = this.Resources.GetDimensionPixelSize(Resource_Dimension.bullet_size)

        for i in 0..size - 1 do
            let bulletImg = new ImageView(this)
            columnLayout.AddView(bulletImg, bulletSize, bulletSize)
            
        columnLayout

    member this.setColumnValue idx max value = 
        let columnView = gridRoot.GetChildAt(idx) :?> ViewGroup

        let testBit bit value = 
            value &&& (1 <<< bit) <> 0

        for i in 0..max - 1 do
            let bullet = (columnView.GetChildAt(max - 1 - i)) :?> ImageView
            bullet.SetImageResource(if (testBit i value) then Resource_Drawable.on else Resource_Drawable.off)
        ()

    member this.updateDate = 
        let first value =
            value / 10
       
        let second value = 
            value % 10
            
        let getColumnValueFromDate (column:Column) (nowDate:DateTime) = 
            match column with
            | HoursFst -> first nowDate.Hour
            | HoursSnd -> second nowDate.Hour 
            | MinutesFst -> first nowDate.Minute
            | MinutesSnd -> second nowDate.Minute
            | SecondsFst -> first nowDate.Second
            | SecondsSnd -> second nowDate.Second            

        let now = DateTime.Now

        ColumnSpec |> List.iteri (fun idx col -> getColumnValueFromDate (fst col) now |> this.setColumnValue idx (snd col))
        ()

    override this.OnCreate (bundle) =
        base.OnCreate(bundle)
        let root = this.createGrid
        gridRoot <- root.GetChildAt(0) :?> ViewGroup
        this.SetContentView(root)

        Log.Debug(typeof<MainActivity>.ToString(), String.Format("Main TID {0}", Thread.CurrentThread.ManagedThreadId)) |> ignore

        let timeLoop = async {
            while true do
                this.RunOnUiThread (fun _ -> this.updateDate)
                do! Async.Sleep 1000  
        }

        Async.Start timeLoop
