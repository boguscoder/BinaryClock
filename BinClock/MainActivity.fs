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
    let mutable scaleDetector:ScaleGestureDetector = null
    let mutable scaleFactor = 1.0f
    let scaleBasis = 1.0f

    interface ScaleGestureDetector.IOnScaleGestureListener with
        member this.OnScale detector = 
            if scaleBasis <> detector.ScaleFactor then
                let deltaScale = detector.ScaleFactor - scaleBasis

                Log.Debug(typeof<MainActivity>.ToString(), String.Format("Scaling with factor {0}", scaleFactor + deltaScale)) |> ignore

                let initialSize = this.Resources.GetDimensionPixelSize(Resource_Dimension.bullet_size)
                let scaledSize = int(float32(initialSize) * scaleFactor)
     
                let rescale (item:ImageView) = 
                    item.LayoutParameters.Height <- scaledSize
                    item.LayoutParameters.Width <- scaledSize
                    item.RequestLayout()
     
                this.forEachItem rescale

                scaleFactor <- scaleFactor + deltaScale

            false
        member this.OnScaleBegin detector = true
        member this.OnScaleEnd detector = ()

    member this.forEachColumn f = 
        ColumnSpec |> List.iteri f

    member this.forEachItem f = 
        let columnF idx (_, max) = 
            let columnView = gridRoot.GetChildAt(idx) :?> ViewGroup

            for i in 0..max - 1 do
                let bullet = (columnView.GetChildAt(max - 1 - i)) :?> ImageView
                bullet |> f
            
        this.forEachColumn columnF

    member this.createGrid = 
        let columnContainer = new LinearLayout(this)
        columnContainer.Orientation <- Orientation.Horizontal
        columnContainer.SetGravity GravityFlags.Bottom

        this.forEachColumn (fun _ (_, size) -> this.createColumn size |> columnContainer.AddView)

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

        this.forEachColumn (fun idx (col, size) -> getColumnValueFromDate col now |> this.setColumnValue idx size)
        ()

    override this.OnTouchEvent(ev) = 
        scaleDetector.OnTouchEvent(ev) |> ignore
        true

    override this.OnCreate (bundle) =
        base.OnCreate(bundle)
        let root = this.createGrid
       
        gridRoot <- root.GetChildAt(0) :?> ViewGroup
        scaleDetector <- new ScaleGestureDetector(this, this)

        let timeLoop = async {
            while true do
                this.RunOnUiThread (fun _ -> this.updateDate)
                do! Async.Sleep 1000  
        }

        this.SetContentView(root)
        Async.Start timeLoop
