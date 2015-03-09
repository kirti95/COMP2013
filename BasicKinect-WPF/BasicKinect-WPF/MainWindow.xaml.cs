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

namespace BasicKinect_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /* KINECT SENSOR */
        private KinectSensor kinectSensor = null;
        
        /* RELATED TO DEPTH */
        private const int MapDepthToByte = 8000 / 256;

        private DepthFrameReader depthFrameReader = null;

        private FrameDescription depthFrameDescription = null;

        private WriteableBitmap depthBitmap = null;

        //Intermediate storage for frame data converted to colour?
        private byte[] depthPixels = null;

        /* RELATED TO COLOUR */
        private ColorFrameReader colorFrameReader = null;

        private WriteableBitmap colorBitmap = null;

        public MainWindow()
        {
            kinectSensor = KinectSensor.GetDefault();

            /* COLOUR RELATED */
            colorFrameReader = kinectSensor.ColorFrameSource.OpenReader();

            FrameDescription colorFrameDescription = kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;

            /* DEPTH RELATED */
            depthFrameReader = kinectSensor.DepthFrameSource.OpenReader();

            depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            depthPixels = new byte[depthFrameDescription.Width * depthFrameDescription.Height];

            depthBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            depthFrameReader.FrameArrived += depthFrameReader_FrameArrived;

            /* OPEN KINECT SENSOR */
            kinectSensor.Open();

            //NECESSARY TO DISPLAY THE IMAGE
            DataContext = this;

            //InitializeComponent();
        }

        void depthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        if (((depthFrameDescription.Width * depthFrameDescription.Height) == (depthBuffer.Size / depthFrameDescription.BytesPerPixel)) &&
                            (depthFrameDescription.Width == depthBitmap.PixelWidth) && (depthFrameDescription.Height == depthBitmap.PixelHeight))
                        {
                            ushort maxDepth = ushort.MaxValue;

                            //Maximum reliable distance
                            //maxDepth = depthFrame.DepthMaxReliableDistance;

                            ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                            depthFrameProcessed = true;
                        }
                    }
                }
            }

            if (depthFrameProcessed)
            {
                RenderDepthPixels();
            }
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

        private void RenderDepthPixels()
        {
            depthBitmap.WritePixels(
                new Int32Rect(0, 0, depthBitmap.PixelWidth, depthBitmap.PixelHeight),
                depthPixels,
                depthBitmap.PixelWidth,
                0);
        }

        //NECESSARY TO DISPLAY THE IMAGE
        //This line is in the XAML: Source="{Binding ImageSource}", relates to this method.
        public ImageSource ImageSource
        {
            get
            {
                return colorBitmap;
            }
        }

        public ImageSource ImageSourceTwo
        {
            get
            {
                return depthBitmap;
            }
        }

        void colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {

                //Not sure why this conditional is necessary, nothing changes if removed.
                if (colorFrame != null)
                {
                    //NECESSARY - simply provides Width and Height information of the current frame.
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        //NECESSARY - allows the image to be processed.
                        colorBitmap.Lock();

                        //Not sure why this conditional is necessary, nothing changes if it is removed.
                        if ((colorFrameDescription.Width == colorBitmap.PixelWidth) && (colorFrameDescription.Height == colorBitmap.PixelHeight))
                        {
                            //NECESSARY - for allowing the image to be converted into a suitable form to be displayed.
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            //This actually displays the image - NECESSARY.
                            colorBitmap.AddDirtyRect(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight));
                        }

                        //NECESSARY - allows the image to be processed.
                        colorBitmap.Unlock();
                    }
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (colorFrameReader != null)
            {
                colorFrameReader.Dispose();
                colorFrameReader = null;
            }

            if (kinectSensor != null)
            {
                kinectSensor.Close();
                kinectSensor = null;
            }
        }
    }
}
