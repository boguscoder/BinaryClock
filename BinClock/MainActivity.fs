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

type ColumnPos = 
    | HoursFst = 0
    | HoursSnd = 1
    | MinutesFst = 2
    | MinutesSnd = 3
    | SecondsFst = 4
    | SecondsSnd = 5

type Column = { Pos:ColumnPos; Size:int }

[<Activity (Label = "Binary Clock", MainLauncher = true, Theme = "@android:style/Theme.NoTitleBar")>]
type MainActivity() =
    inherit Activity ()

    let ColumnSpec = [  {Pos = ColumnPos.HoursFst;   Size = 2};
                        {Pos = ColumnPos.HoursSnd;   Size = 4};
                        {Pos = ColumnPos.MinutesFst; Size = 3};
                        {Pos = ColumnPos.MinutesSnd; Size = 4};
                        {Pos = ColumnPos.SecondsFst; Size = 3};
                        {Pos = ColumnPos.SecondsSnd; Size = 4} ]

    let mutable gridRoot:ViewGroup = null
    let mutable scaleDetector:ScaleGestureDetector = null
    let mutable scaleFactor = 1.0f
    let mutable initialItemSize = 0

    interface ScaleGestureDetector.IOnScaleGestureListener with
        member this.OnScale detector = 
            let ZOOM_BASE = 1.0f
            let ZOOM_MAX = 1.7f
            let ZOOM_MIN = 0.3f
        
            if ZOOM_BASE <> detector.ScaleFactor then
                let deltaScale = detector.ScaleFactor - ZOOM_BASE

                Log.Debug(typeof<MainActivity>.ToString(), String.Format("Last zoom factor {0}, delta {1}", scaleFactor, deltaScale)) |> ignore

                scaleFactor <- min ZOOM_MAX (max ZOOM_MIN (scaleFactor + deltaScale))

                let scaledSize = int(float32(initialItemSize) * scaleFactor)
     
                let rescale (item:ImageView, idx, column) = 
                    item.LayoutParameters.Height <- scaledSize
                    item.LayoutParameters.Width <- scaledSize
                    item.RequestLayout()
                this.forEachItem rescale

            false

        member this.OnScaleBegin detector = true
        member this.OnScaleEnd detector = ()

    member this.forEachColumn f = 
        ColumnSpec |> List.iter f

    member this.forEachItem f = 
        let columnF column = 
            let columnView = gridRoot.GetChildAt(int column.Pos) :?> ViewGroup
            let max = column.Size
            for i in 0..max - 1 do
                let item = (columnView.GetChildAt(max - 1 - i)) :?> ImageView
                (item, i, column)  |> f
            
        this.forEachColumn columnF

    member this.createGrid = 
        let columnContainer = new LinearLayout(this)
        columnContainer.Orientation <- Orientation.Horizontal
        columnContainer.SetGravity GravityFlags.Bottom

        this.forEachColumn (fun column -> this.createColumn column.Size |> columnContainer.AddView)

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

    member this.setItemValue (bullet:ImageView) value = 
        bullet.SetImageResource(if value then Resource_Drawable.on else Resource_Drawable.off)
        ()

    member this.updateDate = 
        let first value =
            value / 10
       
        let second value = 
            value % 10
            
        let getColumnValueFromDate column (nowDate:DateTime) = 
            match column.Pos with
            | ColumnPos.HoursFst -> first nowDate.Hour
            | ColumnPos.HoursSnd -> second nowDate.Hour 
            | ColumnPos.MinutesFst -> first nowDate.Minute
            | ColumnPos.MinutesSnd -> second nowDate.Minute
            | ColumnPos.SecondsFst -> first nowDate.Second
            | ColumnPos.SecondsSnd -> second nowDate.Second   
            | _ -> 0         

        let testBit bit value = 
            value &&& (1 <<< bit) <> 0

        let now = DateTime.Now

        this.forEachItem (fun (item, idx, column) -> getColumnValueFromDate column now |> testBit idx |> this.setItemValue item )
        ()

    override this.OnTouchEvent(ev) = 
        scaleDetector.OnTouchEvent(ev) |> ignore
        true

    override this.OnCreate (bundle) =
        base.OnCreate(bundle)
        let root = this.createGrid
        initialItemSize <- this.Resources.GetDimensionPixelSize(Resource_Dimension.bullet_size)
        gridRoot <- root.GetChildAt(0) :?> ViewGroup
        scaleDetector <- new ScaleGestureDetector(this, this)

        let timeLoop = async {
            while true do
                this.RunOnUiThread (fun _ -> this.updateDate)
                do! Async.Sleep 1000  
        }

        this.SetContentView(root)
        Async.Start timeLoop
