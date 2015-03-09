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
using System.Reflection;
using System.Threading;
using System.Diagnostics;

namespace MultiSourceReader_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int count = 0;

        private const int OpaquePixel = -1;

        private readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        private KinectSensor kinectSensor = null;

        private CoordinateMapper coordinateMapper = null;

        private MultiSourceFrameReader multiFrameSourceReader = null;

        private ushort[] depthFrameData = null;

        private byte[] colorFrameData = null;

        private ColorSpacePoint[] colorPoints = null;

        private CameraSpacePoint[] cameraPoints = null;

        private WriteableBitmap bitmap;

        FrameDescription depthFrameDescription = null;

        FrameDescription colorFrameDescription = null;


        public MainWindow()
        {
            kinectSensor = KinectSensor.GetDefault();

            multiFrameSourceReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color);

            multiFrameSourceReader.MultiSourceFrameArrived += multiFrameSourceReader_MultiSourceFrameArrived;

            coordinateMapper = kinectSensor.CoordinateMapper;

            depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            int depthWidth = depthFrameDescription.Width;
            int depthHeight = depthFrameDescription.Height;

            depthFrameData = new ushort[depthWidth * depthHeight];
            colorPoints = new ColorSpacePoint[depthWidth * depthHeight];

            colorFrameDescription = kinectSensor.ColorFrameSource.FrameDescription;

            int colorWidth = colorFrameDescription.Width;
            int colorHeight = colorFrameDescription.Height;

            colorFrameData = new byte[colorWidth * colorHeight * bytesPerPixel];

            bitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            kinectSensor.Open();
        }

        public ImageSource ImageSource
        {
            get
            {
                return bitmap;
            }
        }

        void multiFrameSourceReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference;

            MultiSourceFrame multiSourceFrame = reference.AcquireFrame();
            ColorFrame colorFrame = null;
            DepthFrame depthFrame = null;

            if (multiSourceFrame != null)
            {
                using (depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                {
                    using (colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                    {
                        bitmap.Lock();
                        if (colorFrame == null || depthFrame == null)
                        {
                            Debug.Print("here");
                            return;
                        }

                        var colorDesc = colorFrame.FrameDescription;
                        int colorWidth = colorDesc.Width;
                        int colorHeight = colorDesc.Height;

                        if (colorFrameData == null)
                        {
                            int size = colorDesc.Width * colorDesc.Height;
                            colorFrameData = new byte[size * bytesPerPixel];
                        }

                        var depthDesc = depthFrame.FrameDescription;

                        if (depthFrameData == null)
                        {
                            uint depthSize = depthDesc.LengthInPixels;
                            depthFrameData = new ushort[depthSize];
                            colorPoints = new ColorSpacePoint[depthSize];
                        }

                        colorFrame.CopyConvertedFrameDataToArray(colorFrameData, ColorImageFormat.Bgra);

                        depthFrame.CopyFrameDataToArray(depthFrameData);

                        kinectSensor.CoordinateMapper.MapDepthFrameToColorSpace(depthFrameData, colorPoints);

                        bitmap.Unlock();

                        bitmap.WritePixels(
                            new Int32Rect(0, 0, colorFrameDescription.Width, colorFrameDescription.Height),
                            colorFrameData,
                            colorFrameDescription.Width * bytesPerPixel,
                            0);
                    }
                }
            }

            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
