//-----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Rhemyst and Rymix">
//     Open Source. Do with this as you will. Include this statement or 
//     don't - whatever you like.
//
//     No warranty or support given. No guarantees this will work or meet
//     your needs. Some elements of this project have been tailored to
//     the authors' needs and therefore don't necessarily follow best
//     practice. Subsequent releases of this project will (probably) not
//     be compatible with different versions, so whatever you do, don't
//     overwrite your implementation with any new releases of this
//     project!
//
//     Enjoy working with Kinect!
// </copyright>
//-----------------------------------------------------------------------

namespace KinectGetData
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Windows.Controls;
    using System.ComponentModel;
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // We want to control how depth data gets converted into false-color data for more intuitive visualization, 
        // so we keep 32-bit color frame buffer versions of these, to be updated whenever we receive and process a 16-bit frame.
        // 我们希望控制深度数据如何转换为伪彩色数据以实现更直观的可视化，因此我们保留32位彩色帧缓冲版本，每当我们接收和处理16位帧时都要更新。

        /// <summary>/// The red index 红色指数/// </summary>
        private const int RedIdx = 2;

        /// <summary>/// The green index 绿色/// </summary>
        private const int GreenIdx = 1;

        /// <summary>/// The blue index 蓝色/// </summary>
        private const int BlueIdx = 0;

        /// <summary>/// How many skeleton frames to ignore (_flipFlop) 要忽略多少个骨架
        /// 1 = capture every frame, 2 = capture every second frame etc./// </summary>
        private const int Ignore = 2;

        /// <summary>/// How many skeleton frames to store in the _video buffer  要在_video缓冲区中存储多少个骨架帧/// </summary>
        private const int BufferSize = 32;

        /// <summary>/// 在我们尝试开始匹配手势之前，视频缓冲区中的最小帧数(默认是6帧 可以将其增加到12帧)/// </summary>
        private const int MinimumFrames = 12;

        /// <summary>/// The minumum number of frames in the _video buffer before we attempt to start matching gestures 在我们尝试开始匹配手势之前_video缓冲区中的最小帧数
        /// </summary>
        private const int CaptureCountdownSeconds = 3;

        /// <summary>
        /// Where we will save our gestures to. The app will append a data/time and .txt to this string 我们将把手势保存到哪里。 该应用程序会将数据/时间和.txt附加到此字符串
        /// </summary>
        // private const string GestureSaveFileLocation = @"H:\My Dropbox\Dropbox\Microsoft Kinect SDK Beta\DTWGestureRecognition\DTWGestureRecognition\";
        //private const string GestureSaveFileLocation = @"C:\Users\joshu\Desktop\kinectdtw-1.0\trunk\DTWGestureRecognition\";
        private const string GestureSaveFileLocation = @"D:\DTWGestureRecognition\";


        /// <summary>/// Where we will save our gestures to. The app will append a data/time and .txt to this string/// </summary>
        private const string GestureSaveFileNamePrefix = @"RecordedGestures";

        /// <summary>/// Width of output drawing/// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>/// Height of our output drawing/// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>/// Thickness of drawn joint lines 绘制的关节线的厚度/// </summary>
        private const double JointThickness = 3;

        /// <summary>/// Thickness of body center ellipse 体心椭圆的厚度/// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>/// Thickness of clip edge rectangles 夹边缘矩形的厚度/// </summary>
        private const double ClipBoundsThickness = 10; 

        /// <summary>/// Brush used to draw skeleton center point 画笔用于绘制骨架中心点/// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>/// Brush used for drawing joints that are currently tracked 用于绘制当前跟踪的关节的画笔/// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>/// Brush used for drawing joints that are currently inferred 用于绘制当前推断的关节的画笔/// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>/// Pen used for drawing bones that are currently tracked 用于绘制当前跟踪的骨骼的笔/// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>/// Pen used for drawing bones that are currently inferred  用于绘制当前推断的骨骼的笔/// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>/// Drawing group for skeleton rendering output 骨架渲染输出的绘图组/// </summary>
        private DrawingGroup skelDrawingGroup;

        /// <summary>/// Drawing image that we will display 绘制我们将要显示的图像/// </summary>
        private DrawingImage skelImageSource;

        /// <summary>  Flag to show whether or not the gesture recogniser is capturing a new pose /// </summary>
        private bool _capturing;

        /// <summary>/// Dynamic Time Warping object/// </summary>
        private DtwGestureRecognizer _dtw;

        /// <summary>/// How many frames occurred 'last time'. Used for calculating frames per second “上次”发生了多少帧。 用于计算每秒帧数/// </summary>
        private int _lastFrames;

        /// <summary>/// The 'last time' DateTime. Used for calculating frames per second  '上次'日期时间。 用于计算每秒帧数/// </summary>
        private DateTime _lastTime = DateTime.MaxValue;

        /// <summary>The Natural User Interface runtime 自然用户界面运行时</summary>
        private KinectSensor sensor;

        /// <summary>Total number of framed that have occurred. Used for calculating frames per second  已发生的框架总数。 用于计算每秒帧数</summary>
        private int _totalFrames;

        /// <summary> Switch used to ignore certain skeleton frames  切换用于忽略某些骨架框架</summary>
        private int _flipFlop;

        /// <summary>ArrayList of coordinates which are recorded in sequence to define one gesture  按顺序记录的坐标的ArrayList，用于定义一个手势</summary>
        private ArrayList _video;

        /// <summary>ArrayList of coordinates which are recorded in sequence to define one gesture  </summary>
        private DateTime _captureCountdown = DateTime.Now;

        /// <summary>        /// ArrayList of coordinates which are recorded in sequence to define one gesture</summary>
        private Timer _captureCountdownTimer;

        /// <summary>Coordinate Mapper</summary>
        private CoordinateMapper _coordinatemapper;

        /// <summary>Bitmap that will hold color information 位图将保存颜色信息 </summary>
        private WriteableBitmap colorBitmap;

        /// <summary> Intermediate storage for the color data received from the camera 中间存储从相机接收的颜色数据 </summary>
        private byte[] colorPixels;

        /// <summary>Bitmap that will hold depth information 将保存深度信息的位图</summary>
        private WriteableBitmap depthBitmap;

        /// <summary>
        /// Intermediate storage for the depth data received from the camera
        /// DepthImagePixell类型暂时修改为short类型
        /// </summary>
        private short[] depthPixels;

        /// <summary>
        /// Intermediate storage for the depth data to be shown as color pixels received from the camera 深度数据的中间存储将显示为从相机接收的彩色像素
        /// </summary>
        private byte[] depthcolorPixels;

        /// <summary>(新添加)To display whether application is running or kinect is not connected </summary>
        private string statusText = null;

        ///新添加↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓
        ///      ↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓
        ///     ↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓

      

        /// <summary> Reader for color frames </summary>
        private ColorFrameReader colorFrameReader = null;

        /// <summary>录制次数 /// </summary>
        public static int capturetime = 0;

        /// <summary>The datareader for the body frames </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary> SDK 2.0 can track up to six people simultaneously, bodies array stores the body joints from Kinect</summary>
        private Body[] bodies = null;

        /// <summary> This object draws the Kinectbodies in the presentation layer</summary>
        private BodyView kinectBodyView = null;
     
        private ArrayList trainDatavideo;
        private bool trainDatacapturing;
        /// <summary>ArrayList of coordinates which are recorded in sequence to define one gesture  </summary>
        private DateTime trainDatacaptureCountdown = DateTime.Now;
        /// <summary> 获取训练数据的/// </summary>
        private bool getTrainBodyData = false;
        /// <summary>/// 是否存储训练数据 /// </summary>
        private bool storeBodyData = false;
        private ArrayList bodyDateTemp = new ArrayList();
        /// <summary>
        /// 识别模式，如果是2d识别模式则为true，否则为false
        /// </summary>
        private bool reg2DMode = true; 


        /// <summary>        /// ArrayList of coordinates which are recorded in sequence to define one gesture</summary>
        private Timer trainDatacaptureCountdownTimer;
        ///新添加↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
        ///新添加↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑

        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Opens the sent text file and creates a _dtw recorded gesture sequence
        /// Currently not very flexible and totally intolerant of errors.将发送的文本文件作为笔并创建_dtw记录的手势序列当前不是非常灵活且完全不能容忍错误。
        /// </summary>
        /// <param name="fileLocation">Full path to the gesture file</param>
        public void LoadGesturesFromFile(string fileLocation)
        {
            int itemCount = 0;
            string line;
            string gestureName = String.Empty;

            // TODO I'm defaulting this to 12 here for now as it meets my current need but I need to cater for variable lengths in the future
            // 我现在将这个默认为12，因为它符合我目前的需要，但我需要在将来满足不同的长度需求
            ArrayList frames = new ArrayList();
            double[] items = new double[12];

            // Read the file and display it line by line.
            System.IO.StreamReader file = new System.IO.StreamReader(fileLocation);
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("@"))
                {
                    gestureName = line;
                    continue;
                }

                if (line.StartsWith("~"))
                {
                    frames.Add(items);
                    itemCount = 0;
                    items = new double[12];
                    continue;
                }

                if (!line.StartsWith("----"))
                {
                    items[itemCount] = Double.Parse(line);
                }

                itemCount++;

                if (line.StartsWith("----"))
                {
                    _dtw.AddOrUpdate(frames, gestureName);
                    frames = new ArrayList();
                    gestureName = String.Empty;
                    itemCount = 0;
                }
            }

            file.Close();
        }

        /// <summary>
        /// 3D姿势的识别
        /// 将发送的文本文件作为笔并创建_dtw记录的手势序列当前不是非常灵活且完全不能容忍错误。
        /// </summary>
        /// <param name="fileLocation">Full path to the gesture file</param>
        public void Load3DGesturesFromFile(string fileLocation)
        {
            int itemCount = 0;
            string line;
            string gestureName = String.Empty;

            // TODO I'm defaulting this to 12 here for now as it meets my current need but I need to cater for variable lengths in the future
            // 我现在将这个默认为12，因为它符合我目前的需要，但我需要在将来满足不同的长度需求
            ArrayList frames = new ArrayList();
            //由12改为18
            double[] items = new double[18];

            // Read the file and display it line by line.
            System.IO.StreamReader file = new System.IO.StreamReader(fileLocation);
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("@"))
                {
                    gestureName = line;
                    continue;
                }

                if (line.StartsWith("~"))
                {
                    frames.Add(items);
                    itemCount = 0;
                    //由12改为18
                    items = new double[18];
                    continue;
                }

                if (!line.StartsWith("----"))
                {
                    items[itemCount] = Double.Parse(line);
                }

                itemCount++;

                if (line.StartsWith("----"))
                {
                    _dtw.AddOrUpdate(frames, gestureName);
                    frames = new ArrayList();
                    gestureName = String.Empty;
                    itemCount = 0;
                }
            }

            file.Close();
        }


        # region 弃用代码

        /// <summary>
        /// Called each time a skeleton frame is ready. Passes skeletal data to the DTW processor 每次骨架框准备好时调用。 将骨架数据传递给DTW处理器
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Skeleton Frame Ready Event Args</param>
        //private static void SkeletonExtractSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        //{
        //    SkeletonFrame skeletonFrame = e.OpenSkeletonFrame();
        //    if (skeletonFrame != null)
        //    {
        //        Skeleton[] skeleton_array = new Skeleton[skeletonFrame.SkeletonArrayLength];
        //        skeletonFrame.CopySkeletonDataTo(skeleton_array);
        //        foreach (Skeleton data in skeleton_array)
        //        {
        //            if (data != null)
        //            {
        //                Skeleton2DDataExtract.ProcessData(data);
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// Called when each depth frame is ready
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Depth Image Frame Ready Event Args</param>
        //private void NuiDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        //{
        //    using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
        //    {
        //        if (depthFrame != null)
        //        {
        //            // Copy the pixel data from the image to a temporary array
        //            depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);

        //            // Get the min and max reliable depth for the current frame
        //            int minDepth = depthFrame.MinDepth;
        //            int maxDepth = depthFrame.MaxDepth;

        //            // Convert the depth to RGB
        //            int colorPixelIndex = 0;
        //            for (int i = 0; i < this.depthPixels.Length; ++i)
        //            {
        //                // Get the depth for this pixel
        //                short depth = depthPixels[i].Depth;

        //                // To convert to a byte, we're discarding the most-significant
        //                // rather than least-significant bits.
        //                // We're preserving detail, although the intensity will "wrap."
        //                // Values outside the reliable depth range are mapped to 0 (black).
        //                //要转换为一个字节，我们要丢弃最重要的而不是最不重要的位。我们保留细节，尽管强度将“包裹”。 超出可靠深度范围的值将映射为0（黑色）。
        //                // Note: Using conditionals in this loop could degrade performance.
        //                // Consider using a lookup table instead when writing production code.
        //                // See the KinectDepthViewer class used by the KinectExplorer sample
        //                // for a lookup table example.
        //                // 注意：在此循环中使用条件可能会降低性能。在编写生产代码时，请考虑使用查找表。请参阅KinectExplorer示例用于查找表示例的KinectDepthViewer类。
        //                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

        //                // Write out blue byte
        //                this.depthcolorPixels[colorPixelIndex++] = intensity;

        //                // Write out green byte
        //                this.depthcolorPixels[colorPixelIndex++] = intensity;

        //                // Write out red byte                        
        //                this.depthcolorPixels[colorPixelIndex++] = intensity;

        //                // We're outputting BGR, the last byte in the 32 bits is unused so skip it
        //                // If we were outputting BGRA, we would write alpha here.
        //                ++colorPixelIndex;
        //            }

        //            // Write the pixel data into our bitmap
        //            this.depthBitmap.WritePixels(
        //                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
        //                this.depthcolorPixels,
        //                this.depthBitmap.PixelWidth * sizeof(int),
        //                0);
        //        }
        //    }

        //    ++_totalFrames;

        //    DateTime cur = DateTime.Now;
        //    if (cur.Subtract(_lastTime) > TimeSpan.FromSeconds(1))
        //    {
        //        int frameDiff = _totalFrames - _lastFrames;
        //        _lastFrames = _totalFrames;
        //        _lastTime = cur;
        //        frameRate.Text = frameDiff + " fps";
        //    }
        //}

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data 绘制指示器以显示哪些边是剪切骨架数据
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        //private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        //{
        //    if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
        //    {
        //        drawingContext.DrawRectangle(
        //            Brushes.Red,
        //            null,
        //            new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
        //    }

        //    if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
        //    {
        //        drawingContext.DrawRectangle(
        //            Brushes.Red,
        //            null,
        //            new Rect(0, 0, RenderWidth, ClipBoundsThickness));
        //    }

        //    if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
        //    {
        //        drawingContext.DrawRectangle(
        //            Brushes.Red,
        //            null,
        //            new Rect(0, 0, ClipBoundsThickness, RenderHeight));
        //    }

        //    if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
        //    {
        //        drawingContext.DrawRectangle(
        //            Brushes.Red,
        //            null,
        //            new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
        //    }
        //}

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event Kinect传感器的SkeletonFrameReady事件的事件处理程序
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        //private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        //{
        //    Skeleton[] skeletons = new Skeleton[0];

        //    using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
        //    {
        //        if (skeletonFrame != null)
        //        {
        //            skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
        //            skeletonFrame.CopySkeletonDataTo(skeletons);
        //        }
        //    }

        //    using (DrawingContext dc = this.skelDrawingGroup.Open())
        //    {
        //        // Draw a transparent background to set the render size
        //        dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

        //        if (skeletons.Length != 0)
        //        {
        //            foreach (Skeleton skel in skeletons)
        //            {
        //                RenderClippedEdges(skel, dc);

        //                if (skel.TrackingState == SkeletonTrackingState.Tracked)
        //                {
        //                    this.DrawBonesAndJoints(skel, dc);
        //                }
        //                else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
        //                {
        //                    dc.DrawEllipse(
        //                    this.centerPointBrush,
        //                    null,
        //                    this.SkeletonPointToScreen(skel.Position),
        //                    BodyCenterThickness,
        //                    BodyCenterThickness);
        //                }
        //            }
        //        }

        //        // prevent drawing outside of our render area
        //        this.skelDrawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
        //    }
        //}

        /// <summary>
        /// Draws a skeleton's bones and joints 绘制骨架的骨骼和关节
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        //private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        //{
        //    // Render Torso
        //    this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
        //    this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
        //    this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
        //    this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
        //    this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
        //    this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
        //    this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

        //    // Left Arm
        //    this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
        //    this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
        //    this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

        //    // Right Arm
        //    this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
        //    this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
        //    this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

        //    // Left Leg
        //    this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
        //    this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
        //    this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

        //    // Right Leg
        //    this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
        //    this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
        //    this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

        //    // Render Joints
        //    foreach (Joint joint in skeleton.Joints)
        //    {
        //        Brush drawBrush = null;

        //        if (joint.TrackingState == JointTrackingState.Tracked)
        //        {
        //            drawBrush = this.trackedJointBrush;
        //        }
        //        else if (joint.TrackingState == JointTrackingState.Inferred)
        //        {
        //            drawBrush = this.inferredJointBrush;
        //        }

        //        if (drawBrush != null)
        //        {
        //            drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
        //        }
        //    }
        //}

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point 将SkeletonPoint映射到我们的渲染空间内并转换为Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        //private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        //{
        //    // Convert point to depth space.   将点转换为深度空间。
        //    // We are not using depth directly, but we do want the points in our 640x480 output resolution.
        //    // 我们没有直接使用深度，但我们确实需要640x480输出分辨率的点数。
        //    DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
        //    return new Point(depthPoint.X, depthPoint.Y);
        //}

        /// <summary>
        /// Draws a bone line between two joints 在两个关节之间绘制骨骼线
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        //private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        //{
        //    Joint joint0 = skeleton.Joints[jointType0];
        //    Joint joint1 = skeleton.Joints[jointType1];

        //    // If we can't find either of these joints, exit
        //    if (joint0.TrackingState == JointTrackingState.NotTracked ||
        //        joint1.TrackingState == JointTrackingState.NotTracked)
        //    {
        //        return;
        //    }

        //    // Don't draw if both points are inferred
        //    if (joint0.TrackingState == JointTrackingState.Inferred &&
        //        joint1.TrackingState == JointTrackingState.Inferred)
        //    {
        //        return;
        //    }

        //    // We assume all drawn bones are inferred unless BOTH joints are tracked
        //    Pen drawPen = this.inferredBonePen;
        //    if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
        //    {
        //        drawPen = this.trackedBonePen;
        //    }

        //    drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        //}

        /// <summary>
        /// Called every time a video (RGB) frame is ready  每次视频（RGB）帧准备就绪时调用
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Image Frame Ready Event Args</param>
        //private void NuiColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        //{
        //    // 32-bit per pixel, RGBA image
        //    /*
        //    ColorImageFrame image = e.OpenColorImageFrame();
        //    byte[] convertedImageFrame=new byte[image.PixelDataLength];
        //    image.CopyPixelDataTo(convertedImageFrame);
        //    videoImage.Source = BitmapSource.Create(
        //        image.Width, image.Height, 96, 96, PixelFormats.Bgr32, null, convertedImageFrame, image.Width * image.BytesPerPixel);
        //    */
        //    using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
        //    {
        //        if (colorFrame != null)
        //        {
        //            // Copy the pixel data from the image to a temporary array
        //            colorFrame.CopyPixelDataTo(this.colorPixels);

        //            // Write the pixel data into our bitmap
        //            // public void WritePixels(Int32Rect sourceRect, IntPtr sourceBuffer, int sourceBufferSize, int sourceBufferStride, int destinationX, int destinationY);
        //            this.colorBitmap.WritePixels(
        //                new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
        //                this.colorPixels,
        //                this.colorBitmap.PixelWidth * sizeof(int),
        //                0);
        //        }
        //    }
        //}
        # endregion

        /// <summary>
        /// Runs after the window is loaded 窗口加载后运行
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.sensor = KinectSensor.GetDefault();
            //This event handler is set to know if the Kinect sensor status is changed
            this.sensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // To open the sensor to detect the bodies
            this.sensor.Open();

            // The status text to find the status of Kinect
            this.StatusText = this.sensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;
            //TODO   _coordinatemapper在源程序中需要用到 现在暂时弃用
            // _coordinatemapper = new CoordinateMapper(potentialSensor);

            if (null != this.sensor)
            {
                /// 新代码 用于处理彩色图像的操作
                // open the reader for the color frames
                this.colorFrameReader = this.sensor.ColorFrameSource.OpenReader();
                FrameDescription colorFrameDescription = this.sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
                this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.videoImage.Source = this.colorBitmap;
                // wire handler for frame arrival
                this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
                // create the colorFrameDescription from the ColorFrameSource using Bgra format
               
                ///新代码 用于处理骨骼图像的操作
                // open the body frame reader to read the data frames from Kinect
                this.bodyFrameReader = this.sensor.BodyFrameSource.OpenReader();
                // This event notifier is used to identify if the body frame has arrived

                // It initializes the bodyviewer object to display tracked bodies in Presentation Layer   
                this.kinectBodyView = new BodyView(this.sensor);
                // set our data context objects for display in UI
                this.DataContext = this;
                this.kinectBodyViewbox.DataContext = this.kinectBodyView;

                this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;
                this.bodyFrameReader.FrameArrived += SkeletonExtractSkeletonFrameReady;
                // 采用2D识别
                //if (reg2DMode)
                //{
                    Skeleton2DDataExtract.Skeleton2DdataCoordReady += this.NuiSkeleton2DdataCoordReady;
                //}
                //else //采用3D识别
                //{
                    Skeleton3DDataExtract.Skeleton3DdataCoordReady += this.NuiSkeleton3DdataCoordReady;
                //}

               
                #region
                ////TODO 源代码 用于处理骨骼图像的操作
                //this.sensor.SkeletonStream.Enable();
                //// Create the drawing group we'll use for drawing  创建我们将用于绘图的绘图组
                //this.skelDrawingGroup = new DrawingGroup();

                //// Create an image source that we can use in our image control  创建一个我们可以在图像控件中使用的图像源
                //this.skelImageSource = new DrawingImage(this.skelDrawingGroup);

                //// Display the drawing using our image control  使用我们的图像控件显示图形
                //skeletonImage.Source = this.skelImageSource;
                //// Track Seated User (Change to SkeletonTrackingMode.Default if not seated)
                //this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                //this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;
                //this.sensor.SkeletonFrameReady += SkeletonExtractSkeletonFrameReady;
                //Skeleton2DDataExtract.Skeleton2DdataCoordReady += this.NuiSkeleton2DdataCoordReady;
                #endregion

                try
                {
                    this.sensor.Open();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            _lastTime = DateTime.Now;

            _dtw = new DtwGestureRecognizer(12, 0.6, 2, 2, 10);
            _video = new ArrayList();

            // Update the debug window with Sequences information 使用序列信息更新调试窗口
            dtwTextOutput.Text = _dtw.RetrieveText();

            Debug.WriteLine("Finished Window Loading");
        }





        /// <summary>
        /// Runs some tidy-up code when the window is closed. This is especially important for our NUI instance because the Kinect SDK is very picky about this having been disposed of nicely.
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Event Args</param>
        private void WindowClosed(object sender, EventArgs e)
        {
            if (null != this.sensor)
            {
                Debug.WriteLine("Stopping NUI");
                this.sensor.Close();
                Debug.WriteLine("NUI stopped");
            }
            Environment.Exit(0);
        }

        /// <summary>
        /// Runs every time our 2D coordinates are ready.  每次我们的2D坐标准备就运行。
        /// </summary> 
        /// <param name="sender">The sender object</param>
        /// <param name="a">Skeleton 2Ddata Coord Event Args</param>
        private void NuiSkeleton2DdataCoordReady(object sender, Skeleton2DdataCoordEventArgs a)
        {
            tiaoshi.Text = "2D识别";
            currentBufferFrame.Text = _video.Count.ToString();
            // 在我们开始尝试将手势与记忆序列匹配之前，我们需要合理数量的帧
            // We need a sensible number of frames before we start attempting to match gestures against remembered sequences
            if (_video.Count > MinimumFrames && _capturing == false)
            {
                ////Debug.WriteLine("Reading and video.Count=" + video.Count);
                string s = _dtw.Recognize(_video);
                results.Text = "Recognised as: " + s;
                if (!s.Contains("__UNKNOWN"))
                {
                    //TODO 将识别结果加入到TextBox中
                    RegResult.AppendText("Recognised as: " + s);
                }
                
                if (!s.Contains("__UNKNOWN"))
                {
                    // There was no match so reset the buffer  没有匹配，所以重置缓冲区
                    _video = new ArrayList();
                }
            }

            // Ensures that we remember only the last x frames  确保我们只记住最后的x帧
            if (_video.Count > BufferSize)
            {
                // If we are currently capturing and we reach the maximum buffer size then automatically store
                // 如果我们正在捕获并且达到最大缓冲区大小，则自动存储
                if (_capturing)
                {
                    DtwStoreClick(null, null);
                }
                else
                {
                    // Remove the first frame in the buffer  删除缓冲区中的第一帧
                    _video.RemoveAt(0);
                }
            }

            // Decide which skeleton frames to capture. Only do so if the frames actually returned a number. 
            // For some reason my Kinect/PC setup didn't always return a double in range (i.e. infinity) even when standing completely within the frame.
            // TODO Weird. Need to investigate this
            // 确定要捕获的骨架帧。 只有在帧实际返回一个数字时才这样做。
            // 出于某种原因，即使完全站在画面内，我的Kinect / PC设置并不总是返回双倍范围（即无限远）。
            // 需要对此进行调查

            if (!double.IsNaN(a.GetPoint(0).X))
            {
                // Optionally register only 1 frame out of every n  可选择每n只注册1帧
                _flipFlop = (_flipFlop + 1) % Ignore;
                if (_flipFlop == 0)
                {
                    _video.Add(a.GetCoords());
                }
            }

            // Update the debug window with Sequences information  使用序列信息更新调试窗口
            //dtwTextOutput.Text = _dtw.RetrieveText();
        }

        /// <summary>
        /// 每次我们的3D坐标准备就运行。使用3D模式进行识别
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        private void NuiSkeleton3DdataCoordReady(object sender, Skeleton3DdataCoordEventArgs a)
        {
            //Debug.WriteLine("Finished Window Loading");
            //如果是3D识别模式的话，更改DTW的参数 
            //_dimension  默认12   _globalThreshold 默认0.6 _maxSlope  _firstThreshold  默认2
            if (_dtw != null)
            {
                if (_dtw._firstThreshold == 2)
                {
                    _dtw._firstThreshold = 2.2;
                }
                if (_dtw._globalThreshold == 0.6)
                {
                    _dtw._globalThreshold = 0.7;
                }
            }

            // TODO 在状态栏增加3D识别的标志
            tiaoshi.Text = "3D识别" + "  " + "_firstThreshold:"+_dtw._firstThreshold.ToString();
            currentBufferFrame.Text = _video.Count.ToString();
            // 在我们开始尝试将手势与记忆序列匹配之前，我们需要合理数量的帧
            // We need a sensible number of frames before we start attempting to match gestures against remembered sequences
            if (_video.Count > MinimumFrames && _capturing == false)
            {
                ////Debug.WriteLine("Reading and video.Count=" + video.Count);
                //string s = _dtw.Recognize(_video);
                // 更改为3D
                string s = _dtw.RecognizeUse3D(_video);
                results.Text = "Recognised as: " + s;
                if (!s.Contains("__UNKNOWN"))
                {
                    //TODO 将识别结果加入到TextBox中
                    //TODO 识别效果，将录制的第几次给去掉
                    s = s.Replace("_","");
                    RegResult.AppendText("Recognised as: " + s);
                }
                if (!s.Contains("__UNKNOWN"))
                {
                    // There was no match so reset the buffer  没有匹配，所以重置缓冲区
                    _video = new ArrayList();
                }
            }

            // Ensures that we remember only the last x frames  确保我们只记住最后的x帧
            if (_video.Count > BufferSize)
            {
                // If we are currently capturing and we reach the maximum buffer size then automatically store
                // 如果我们正在捕获并且达到最大缓冲区大小，则自动存储
                if (_capturing)
                {
                    DtwStoreClick(null, null);
                }
                else
                {
                    // Remove the first frame in the buffer  删除缓冲区中的第一帧
                    _video.RemoveAt(0);
                }
            }

            // Decide which skeleton frames to capture. Only do so if the frames actually returned a number. 
            // For some reason my Kinect/PC setup didn't always return a double in range (i.e. infinity) even when standing completely within the frame.
            // TODO Weird. Need to investigate this
            // 确定要捕获的骨架帧。 只有在帧实际返回一个数字时才这样做。
            // 出于某种原因，即使完全站在画面内，我的Kinect / PC设置并不总是返回双倍范围（即无限远）。
            // 需要对此进行调查

            if (!double.IsNaN(a.GetPoint(0).X))
            {
                // Optionally register only 1 frame out of every n  可选择每n只注册1帧
                _flipFlop = (_flipFlop + 1) % Ignore;
                if (_flipFlop == 0)
                {
                    _video.Add(a.GetCoords());
                }
            }

            // Update the debug window with Sequences information  使用序列信息更新调试窗口
            //dtwTextOutput.Text = _dtw.RetrieveText();
        }

        /// <summary>
        /// Read mode. Sets our control variables and button enabled states  读模式。 设置我们的控制变量和按钮启用状态
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
        private void DtwReadClick(object sender, RoutedEventArgs e)
        {
            // Set the buttons enabled state

                    
                     dtwRead.IsEnabled = false;
                     dtwCapture.IsEnabled = true;
                     dtwStore.IsEnabled = false;
                     // Set the capturing? flag
                     _capturing = false;
                     // Update the status display
                     status.Text = "Reading";
                 
            //}
           
        }

        /// <summary>
        /// Starts a countdown timer to enable the player to get in position to record gestures  启动倒数计时器以使玩家能够到位以记录手势
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
        private void DtwCaptureClick(object sender, RoutedEventArgs e)
        {
      
                           //TODO 连续录制三次
           // for (int i = 0; i < 3; i++)
           //{
                 capturetime = (capturetime + 1)%4;
                
                 if (capturetime != 0)
                 {
                     Capturetime.Text = "这是第" + capturetime + "次录制";
                     _captureCountdown = DateTime.Now.AddSeconds(CaptureCountdownSeconds);
                     // _captureCountdown = DateTime.Now.AddSeconds(5);
                     _captureCountdownTimer = new Timer();
                     _captureCountdownTimer.Interval = 50;
                     _captureCountdownTimer.Start();
                     _captureCountdownTimer.Tick += CaptureCountdown;
                 }
        }

        /// <summary>
        /// The method fired by the countdown timer. Either updates the countdown or fires the StartCapture method if the timer expires
        /// 倒数计时器触发的方法。 如果计时器到期，则更新倒计时或触发StartCapture方法
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Event Args</param>
        private void CaptureCountdown(object sender, EventArgs e)
        {
            if (sender == _captureCountdownTimer)
            {
                if (DateTime.Now < _captureCountdown)
                {
                    status.Text = "Wait " + ((_captureCountdown - DateTime.Now).Seconds + 1) + " seconds";
                }
                else
                {
                    _captureCountdownTimer.Stop();
                    status.Text = "Recording gesture";
                    StartCapture();
                }
            }
        } 

        /// <summary>
        /// Capture mode. Sets our control variables and button enabled states
        /// 捕捉模式。 设置我们的控制变量和按钮启用状态
        /// </summary>
        private void StartCapture()
        {
            // Set the buttons enabled state
            dtwRead.IsEnabled = false;
            dtwCapture.IsEnabled = false;
            dtwStore.IsEnabled = true;

            // Set the capturing? flag
            _capturing = true;

            ////_captureCountdownTimer.Dispose();

            status.Text = "Recording gesture" + gestureList.Text;
            Capturetime.Text = "";

            // Clear the _video buffer and start from the beginning
            _video = new ArrayList();
        }

        /// <summary>
        /// Stores our gesture to the DTW sequences list
        /// 将我们的手势存储到DTW序列列表中
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
        private void DtwStoreClick(object sender, RoutedEventArgs e)
        {
            // Set the buttons enabled state
            dtwRead.IsEnabled = false;
            dtwCapture.IsEnabled = true;
            dtwStore.IsEnabled = false;
            
            // Set the capturing? flag
            _capturing = false;

            status.Text = "Remembering " + gestureList.Text;

            // Add the current video buffer to the dtw sequences list
            //TODO 处理重复录制
            if (capturetime > 0)
            {
                if(capturetime == 1)
                {
                    _dtw.AddOrUpdate(_video, gestureList.Text);
                }
                else
                {
                    string temp = gestureList.Text;
                    string str = "";
                    for (int i = 0; i < capturetime-1;i++ )
                    {
                        str += "_";
                    }
                    temp = str + temp;
                    _dtw.AddOrUpdate(_video, temp);
                }
            }
            //_dtw.AddOrUpdate(_video, gestureList.Text);
            results.Text = "Gesture " + gestureList.Text + "added";

            // Scratch the _video buffer
            _video = new ArrayList();

            // Switch back to Read mode
            DtwReadClick(null, null);
        }

        /// <summary>
        /// Stores our gesture to the DTW sequences list
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
        private void DtwSaveToFile(object sender, RoutedEventArgs e)
        {
            //TODO 区分3D识别和2D识别
            //如果是2D识别
            if (reg2DMode)
            {
                string fileName = GestureSaveFileNamePrefix + DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + ".txt";
                System.IO.File.WriteAllText(GestureSaveFileLocation + fileName, _dtw.RetrieveText());
                status.Text = "Saved to " + fileName;
            }
            else
            {
                string fileName = GestureSaveFileNamePrefix +"3DReg_" +DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + ".txt";
                System.IO.File.WriteAllText(GestureSaveFileLocation + fileName, _dtw.RetrieveText3D());
                status.Text = "Saved to " + fileName;
            }
            
        }

        /// <summary>
        /// Loads the user's selected gesture file 加载用户选择的手势文件
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
        private void DtwLoadFile(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 设置文件扩展名和默认文件扩展名的过滤器
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text documents (.txt)|*.txt";

            dlg.InitialDirectory = GestureSaveFileLocation;

            // Display OpenFileDialog by calling ShowDialog method 通过调用ShowDialog方法显示OpenFileDialog
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 获取所选文件名并显示在TextBox中
            if (result == true)
            {
                // Open document
                LoadGesturesFromFile(dlg.FileName);
                dtwTextOutput.Text = _dtw.RetrieveText();
                status.Text = "Gestures loaded!";
            } 
        }

        /// <summary>
        /// Stores our gesture to the DTW sequences list 将我们的手势存储到DTW序列列表中
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Routed Event Args</param>
        private void DtwShowGestureText(object sender, RoutedEventArgs e)
        {
            dtwTextOutput.Text = _dtw.RetrieveText();
        }

        ///新添加↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓
        ///↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓
        ///↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓

        /// <summary>       
        /// Allows to bind the data when the change occurs
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///  Event handler when the sensor becomes unavailable
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.sensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        /// <summary>
        /// get or set to display the status text
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();
                    }
                }
            }
        }


        /// <summary>
        /// 可视化骨骼节点的处理方法
        /// Handles the body frame data arriving from the sensor and updates the associated gesture detector object for each body.
        /// On every frame received processes if the last detected gesture is recognised.
        /// Gets the gesture name of the last detected gesture.
        /// </summary>
        /// <param name="sender">is an object for sending elements</param>
        /// <param name="e">event arguments for an object</param>
        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {

                    if (this.bodies == null)
                    {
                        // creates an array of 6 bodies, which is the max number of bodies that Kinect can track simultaneously
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    //Adds each body in an array
                    //These objects are used unless they are either set to null or empty 
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                //Is called when a valid frame is received i.e it consists of all 25 joints in 3D   
                this.kinectBodyView.UpdateBodyFrame(this.bodies);
            }
        }
        /// <summary>
        /// 处理骨骼节点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SkeletonExtractSkeletonFrameReady(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;
            Body[] skeleton_array = null;
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {

                    if (skeleton_array == null)
                    {
                        // creates an array of 6 bodies, which is the max number of bodies that Kinect can track simultaneously
                        skeleton_array = new Body[bodyFrame.BodyCount]; 
                    }
                    bodyFrame.GetAndRefreshBodyData(skeleton_array);
                    dataReceived = true;
                }
            }
            if (dataReceived)
            {
                foreach (Body data in skeleton_array)
                {
                    if (data != null)
                    {
                        if (data.IsTracked)
                        {
                            //TODO 采用何种模式进行识别
                            if (reg2DMode)
                            {
                                Skeleton2DDataExtract.ProcessData(data);
                            }
                            else
                            {
                                Skeleton3DDataExtract.ProcessData(data);
                            }
                        }                   
                    }

                    // TODO 新添加方法  仅仅用来获取一副身体的数据数据 可以通过多次点击获取数据
                    if (data != null && getTrainBodyData)
                    {
                        bool isZero = chargeZero(data);
                        if (!isZero)
                        {
                            if (bodyDateTemp == null)
                            {
                                bodyDateTemp = new ArrayList();
                            }       
                            foreach (Joint j in data.Joints.Values)
                            {
                                bodyDateTemp.Add(j.Position.X);
                                bodyDateTemp.Add(j.Position.Y);
                                bodyDateTemp.Add(j.Position.Z);
                                // TODO 添加谷歌节点的下标
                                // bodyDateTemp.Add((int)j.JointType);
                                // int i = (int)j.JointType;
                            }
                            bodyDateTemp.Add(1111.0);
                            getTrainBodyData = false;  
                        }
                    }

                    //  是否存储训练数据数据
                    if (storeBodyData)
                    {
                        string fileName = "Bodydata" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + ".txt";
                        System.IO.File.WriteAllText(GestureSaveFileLocation + fileName, returnBodyData(bodyDateTemp));
                        status.Text = "Saved to " + fileName;
                        storeBodyData = false;
                    }


                    //TODO  新添加的处理方法，用来获取训练数据
                    if(data!=null && trainDatacapturing)//只用到一个骨骼节点
                    {
                        currentBufferFrame.Text = trainDatavideo.Count.ToString();
                        processTrainData(data);
                        // Ensures that we remember only the last x frames  确保我们只记住最后的x帧
                        if (trainDatavideo!= null && trainDatavideo.Count > BufferSize)
                        {
                            // If we are currently capturing and we reach the maximum buffer size then automatically store
                            // 如果我们正在捕获并且达到最大缓冲区大小，则自动存储
                            if (trainDatacapturing)
                            {
                                trainDatacapturing = false;
                            }
                            else
                            {
                                // Remove the first frame in the buffer  删除缓冲区中的第一帧
                                trainDatavideo.RemoveAt(0);
                            }

                        }
                    }


                }
            }
        }

        private string returnBodyData(ArrayList arr)
        {
            String str = String.Empty;
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i] != null)
                {
                    str += arr[i] + "\r";
                }
                else
                {
                    str += "null" + "\r";
                }
            }
            return str;
        }

        private bool chargeZero(Body data)
        {
            bool isZero = false;
            foreach (Joint j in data.Joints.Values)
            {
                if (j.JointType.Equals(JointType.HandLeft))
                {
                    if (j.Position.X == 0 && j.Position.Y == 0 && j.Position.Z == 0)
                    {
                        isZero = true;
                        break;
                    }
                }
            }
            return isZero;
        }
        /// <summary>
        /// 新添加的
        /// </summary>
        /// <param name="data"></param>
        private void processTrainData(Body data)
        {
            // Extract the coordinates of the points.  提取点的坐标。
            // foreach (Joint j in data.Joints)
            double[] arr = new double[18];
             bool inZero  = true;


             foreach (Joint j in data.Joints.Values)
             {
                 if (j.JointType.Equals(JointType.HandLeft))
                 {
                     if (j.Position.X == 0 && j.Position.Y == 0 && j.Position.Z == 0)
                     {
                         inZero = false;
                         break;
                     }
                 }
             }
           
            if(inZero)
            {
                foreach (Joint j in data.Joints.Values)
                {
                    switch (j.JointType)
                    {
                        case JointType.HandLeft:
                            arr[0] = j.Position.X; arr[1] = j.Position.Y; arr[2] = j.Position.Z;
                            break;
                        case JointType.WristLeft:
                            arr[3] = j.Position.X; arr[4] = j.Position.Y; arr[5] = j.Position.Z;
                            break;
                        case JointType.ElbowLeft:
                            arr[6] = j.Position.X; arr[7] = j.Position.Y; arr[8] = j.Position.Z;
                            break;
                        case JointType.ElbowRight:
                            arr[9] = j.Position.X; arr[10] = j.Position.Y; arr[11] = j.Position.Z;
                            break;
                        case JointType.WristRight:
                            arr[12] = j.Position.X; arr[13] = j.Position.Y; arr[14] = j.Position.Z;
                            break;
                        case JointType.HandRight:
                            arr[15] = j.Position.X; arr[16] = j.Position.Y; arr[17] = j.Position.Z;
                            break;

                    }
                }

                // Centre the data  使数据居中
                if (trainDatavideo != null)
                {
                    if (!double.IsNaN(arr[0]))
                    {
                         // Optionally register only 1 frame out of every n  可选择每n只注册1帧
                        _flipFlop = (_flipFlop + 1) % Ignore;
                        if (_flipFlop == 0)
                        {
                            _video.Add(arr);
                        }
                    }
                    trainDatavideo.Add(arr);
                }
                else
                {
                    trainDatavideo = new ArrayList();
                }
            }
            
           
        }



        private void saveDataToFile_Click(object sender, RoutedEventArgs e)
        {
            ////TODO 区分3d识别和2D识别
            ////如果是2D
            //if (reg2DMode)
            //{
                string fileName = "Traindata" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + ".txt";
                System.IO.File.WriteAllText(GestureSaveFileLocation + fileName, captureTrainDataVideo(trainDatavideo));
                status.Text = "Saved to " + fileName;
            //}
            //else
            //{
            //    string fileName = "3DTraindata" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + ".txt";
            //    System.IO.File.WriteAllText(GestureSaveFileLocation + fileName, captureTrainDataVideo(trainDatavideo));
            //    status.Text = "Saved to " + fileName;
            //}
            
        }
        /// <summary>
        /// 新添加用于将数据写入文件中
        /// </summary>
        /// <param name="trainDatavideo"></param>
        /// <returns></returns>
        private string captureTrainDataVideo(ArrayList trainDatavideo)
        {
            String str = "";
       
                    foreach (double[] frame in trainDatavideo)
                    {
                        // Echo the label
                        str += "gestrue" + "\r\n";
                        //Iterate through each frame of this gesture
                        // Extract each double
                        foreach (double dub in (double[])frame)
                        {
                            str += dub + "\r\n";
                        }

                        // Signifies end of this double
                        str += "~\r\n";
                    }

                    // Signifies end of this gesture
                    str += "----";
            return str;

        }


        private void getDataCaptrue_Click(object sender, RoutedEventArgs e)
        {
            trainDatacaptureCountdown = DateTime.Now.AddSeconds(CaptureCountdownSeconds);
            // _captureCountdown = DateTime.Now.AddSeconds(5);

            trainDatacaptureCountdownTimer = new Timer();
            trainDatacaptureCountdownTimer.Interval = 50;
            trainDatacaptureCountdownTimer.Start();
            trainDatacaptureCountdownTimer.Tick += trainDataCaptureCountdown;
        }

        /// <summary>
        /// The method fired by the countdown timer. Either updates the countdown or fires the StartCapture method if the timer expires
        /// 倒数计时器触发的方法。 如果计时器到期，则更新倒计时或触发StartCapture方法
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">Event Args</param>
        private void trainDataCaptureCountdown(object sender, EventArgs e)
        {
            if (sender == trainDatacaptureCountdownTimer)
            {
                if (DateTime.Now < trainDatacaptureCountdown)
                {
                    status.Text = "Wait " + ((trainDatacaptureCountdown - DateTime.Now).Seconds + 1) + " seconds";
                }
                else
                {
                    trainDatacaptureCountdownTimer.Stop();
                    status.Text = "Recording gesture";
                    StartTrainDataCapture();
                }
            }
        }

        private void StartTrainDataCapture()
        {
           trainDatacapturing = true;
           trainDatavideo = new ArrayList();
        }

        private void getBodyData_Click(object sender, RoutedEventArgs e)
        {
            getTrainBodyData = true;
        }

        private void storeBodyData_Click(object sender, RoutedEventArgs e)
        {
            storeBodyData = true;
        }

        private void choseMode_Click(object sender, RoutedEventArgs e)
        {
            reg2DMode = !reg2DMode;
            if (reg2DMode)
            {
                choseMode.Content = "2D识别";
            }
            else
            {
                choseMode.Content = "3D识别";
            }
        } 

    }
}