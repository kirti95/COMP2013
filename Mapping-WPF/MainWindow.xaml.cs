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

namespace Mapping_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private KinectSensor kinectSensor = null;

        private MultiSourceFrameReader multiFrameReader = null;

        private WriteableBitmap colorBitmap = null;

        private DepthSpacePoint[] depthMappedToColorPoints = null;

        private ColorSpacePoint[] colorSpacePoints = null;

        private int i = 0;

        private const int MapDepthToByte = 8000 / 256;

        private FrameDescription colorFrameDescription = null;

        private FrameDescription depthFrameDescription = null;

        private int depthWidth = 0;

        private int depthHeight = 0;

        private int colorWidth = 0;

        private int colorHeight = 0;

        private byte[] colorFrameData = null;

        private ushort[] depthPixels = null;

        private byte[] pixels = null;

        private byte[] newPixels = null;

        private CoordinateMapper coordinateMapper = null;

        public MainWindow()
        {
            kinectSensor = KinectSensor.GetDefault();

            multiFrameReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);

            multiFrameReader.MultiSourceFrameArrived += multiFrameReader_MultiSourceFrameArrived;

            FrameDescription depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            FrameDescription colorFrameDescription = kinectSensor.ColorFrameSource.FrameDescription;

            //colorBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            coordinateMapper = kinectSensor.CoordinateMapper;

            kinectSensor.Open();

            DataContext = this;

            InitializeComponent();
        }

        public ImageSource ImageSource
        {
            get
            {
                return colorBitmap;
            }
        }

        void multiFrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
            {
                using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                {
                    if (colorFrame == null || depthFrame == null)
                    {
                        return;
                    }

                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        using (Microsoft.Kinect.KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                        {
                            colorBitmap.Lock();

                            colorFrameDescription = colorFrame.FrameDescription;
                            depthFrameDescription = depthFrame.FrameDescription;
                            colorWidth = colorFrameDescription.Width;
                            colorHeight = colorFrameDescription.Height;

                            if (colorFrameData == null)
                            {
                                int size = (colorFrameDescription.Width * colorFrameDescription.Height * 4);
                                colorFrameData = new byte[size];
                                pixels = new byte[size];
                                newPixels = new byte[size];
                            }

                            if (depthPixels == null)
                            {
                                int depthSize = depthFrameDescription.Width * depthFrameDescription.Height;
                                depthPixels = new ushort[depthSize];
                                colorSpacePoints = new ColorSpacePoint[depthSize];
                                //pixels = new byte[depthSize];
                            }

                            colorFrame.CopyConvertedFrameDataToArray(colorFrameData, ColorImageFormat.Bgra);

                            depthFrame.CopyFrameDataToArray(depthPixels);

                            coordinateMapper.MapDepthFrameToColorSpace(depthPixels, colorSpacePoints);

                            Array.Clear(pixels, 0, pixels.Length);

                            for (int depthIndex = 0; depthIndex < depthPixels.Length; ++depthIndex)
                            {
                                ColorSpacePoint point = colorSpacePoints[depthIndex];

                                int colorX = (int)Math.Floor(point.X + 0.5);
                                int colorY = (int)Math.Floor(point.Y + 0.5);
                                if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight)) 
                                {
                                    int colorImageIndex = ((colorWidth * colorY) + colorX) * 4;
                                    //int depthPixel = depthIndex * 4;

                                    if (isGreen(colorFrameData[colorImageIndex + 1], colorFrameData[colorImageIndex + 2]) )
                                    {
                                        pixels[colorImageIndex] = colorFrameData[colorImageIndex];
                                        //pixels[colorImageIndex + 1] = colorFrameData[colorImageIndex + 1];
                                        pixels[colorImageIndex + 1] = 255;
                                        pixels[colorImageIndex + 2] = colorFrameData[colorImageIndex + 2];
                                        pixels[colorImageIndex + 3] = colorFrameData[colorImageIndex + 3];
                                        
                                    }
                                    else
                                    {
                                        pixels[colorImageIndex] = 0;
                                        pixels[colorImageIndex + 1] = 0;
                                        pixels[colorImageIndex + 2] = 0;
                                        pixels[colorImageIndex + 3] = 0;
                                    }
                                    
                                    
                                    
                                    /*
                                    pixels[depthPixel] = 166;
                                    pixels[depthPixel + 1] = 5;
                                    pixels[depthPixel + 2] = 200;
                                    pixels[depthPixel + 3] = 255;
                                    */
                                }
                                //Console.Out.WriteLine(pixels.Length);
                                //Console.Out.WriteLine("Depth: " + depthPixels.Length);
                            } 

                                /*colorBitmap.WritePixels(
                                    new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight),
                                    colorFrameData,
                                    colorBitmap.PixelWidth,
                                    0);*/

                                colorBitmap.WritePixels(
                                        new Int32Rect(0, 0, colorFrameDescription.Width, colorFrameDescription.Height),
                                        pixels,
                                        colorFrameDescription.Width * 4,
                                        0);

                            colorBitmap.Unlock();
                        }
                    }
                }
            }
        }

        private bool isGreen(byte one, byte two)
        {
            if (one - two > 25)
            {
                return true;
            } 
            else 
            {
                return false;
            }
        }

        private bool isSurroundingGreen(byte[] colorData, int index)
        {
            return true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (multiFrameReader != null)
            {
                multiFrameReader.Dispose();
                multiFrameReader = null;
            }

            if (kinectSensor != null)
            {
                kinectSensor.Close();
                kinectSensor = null;
            }
        }


    }
}
