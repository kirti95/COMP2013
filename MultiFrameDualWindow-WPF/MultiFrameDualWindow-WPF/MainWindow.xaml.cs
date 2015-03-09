using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;
using System.Diagnostics;

namespace MultiFrameDualWindow_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /* KINECT SENSOR */
        private KinectSensor kinectSensor = null;

        /* FRAME READER */
        private MultiSourceFrameReader multiSourceFrameReader = null;

        /* FRAME DESCRIPTIONS */
        private FrameDescription depthFrameDescription = null;

        private FrameDescription colourFrameDescription = null;

        /* DEPTH RELATED */
        private byte[] depthPixels = null;

        private const int MapDepthToByte = 8000 / 256;

        /* BITMAPS TO DISPLAY */
        private WriteableBitmap depthBitmap = null;

        private WriteableBitmap colourBitmap = null;

        /* COORDINATE MAPPING RELATED */
        private ColorSpacePoint[] colourPoints = null;

        private ushort[] depthData = null;

        public MainWindow()
        {
            kinectSensor = KinectSensor.GetDefault();

            multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);

            multiSourceFrameReader.MultiSourceFrameArrived += multiSourceFrameReader_MultiSourceFrameArrived;

            depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            colourFrameDescription = kinectSensor.ColorFrameSource.FrameDescription;

            depthPixels = new byte[depthFrameDescription.Width * depthFrameDescription.Height];

            depthBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            colourBitmap = new WriteableBitmap(colourFrameDescription.Width, colourFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            colourPoints = new ColorSpacePoint[depthFrameDescription.Width * depthFrameDescription.Height];

            depthData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];

            kinectSensor.Open();

            DataContext = this;

            InitializeComponent();
        }

        public ImageSource ImageSource
        {
            get
            {
                return colourBitmap;
            }
        }

        public ImageSource ImageSourceTwo
        {
            get
            {
                return depthBitmap;
            }
        }

        void multiSourceFrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            bool depthFrameProcessed = false;
            bool colourFrameProcessed = false;
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
            {
                using (ColorFrame colourFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                {
                    if (depthFrame != null)
                    {
                        using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                        {
                            if (((depthFrameDescription.Width * depthFrameDescription.Height) == (depthBuffer.Size / depthFrameDescription.BytesPerPixel)) &&
                                (depthFrameDescription.Width == depthBitmap.PixelWidth) && (depthFrameDescription.Height == depthBitmap.PixelHeight))
                            {
                                depthFrame.CopyFrameDataToArray(depthData);
                                ushort maxDepth = ushort.MaxValue;

                                //Maximum reliable distance
                                //maxDepth = depthFrame.DepthMaxReliableDistance;

                                ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                                depthFrameProcessed = true;
                            }
                        }
                    }

                    if (colourFrame != null)
                    {
                        using (KinectBuffer colourBuffer = colourFrame.LockRawImageBuffer())
                        {
                            colourBitmap.Lock();

                            if ((colourFrameDescription.Width == colourBitmap.PixelWidth) && (colourFrameDescription.Height == colourBitmap.PixelHeight))
                            {
                                colourFrame.CopyConvertedFrameDataToIntPtr(
                                colourBitmap.BackBuffer,
                                (uint)(colourFrameDescription.Width * colourFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                                colourBitmap.AddDirtyRect(new Int32Rect(0, 0, colourBitmap.PixelWidth, colourBitmap.PixelHeight));
                            }

                            colourBitmap.Unlock();
                            colourFrameProcessed = true;
                        }
                    }

                }
            }

            kinectSensor.CoordinateMapper.MapDepthFrameToColorSpace(depthData, colourPoints);

            for (int i = 0; i < colourPoints.Length; i++)
            {
                Debug.Print(colourPoints[i].X + " " + colourPoints[i].Y + "");
            }
            
            if (depthFrameProcessed)
            {
                RenderDepthPixels();
            }
        }

        private void RenderDepthPixels()
        {
            depthBitmap.WritePixels(
                new Int32Rect(0, 0, depthBitmap.PixelWidth, depthBitmap.PixelHeight),
                depthPixels,
                depthBitmap.PixelWidth,
                0);
        }

        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            ushort* frameData = (ushort*)depthFrameData;

            for (int i = 0; i < (int)(depthFrameDataSize / depthFrameDescription.BytesPerPixel); ++i)
            {
                ushort depth = frameData[i];

                depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (multiSourceFrameReader != null)
            {
                multiSourceFrameReader.Dispose();
                multiSourceFrameReader = null;
            }

            if (kinectSensor != null)
            {
                kinectSensor.Close();
                kinectSensor = null;
            }
        }
    }
}
