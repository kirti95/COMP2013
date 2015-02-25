//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace Microsoft.Samples.Kinect.ColorBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for color frames
        /// </summary>
        private MultiSourceFrameReader multiFrameReader = null;


        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap colorBitmap = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        private DepthSpacePoint[] depthMappedToColorPoints = null;
        private int i = 0;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // open the reader for the color frames
            this.multiFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);

            // wire handler for frame arrival
            this.multiFrameReader.MultiSourceFrameArrived += this.multiFrameReader_MultiSourceFrameArrived;

            // create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            // create the bitmap to display
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();
        }

        void multiFrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiFrame = e.FrameReference.AcquireFrame();
            Console.Out.WriteLine("Frame Arrived");
            if (multiFrame != null)
            {
                ColorFrame colorFrame = multiFrame.ColorFrameReference.AcquireFrame();
                DepthFrame depthFrame = multiFrame.DepthFrameReference.AcquireFrame();
                FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                FrameDescription depthFrameDescription = depthFrame.FrameDescription;
                int depthWidth = depthFrameDescription.Width;
                int depthHeight = depthFrameDescription.Height;
                byte[] pixels = new byte[colorFrameDescription.Width * colorFrameDescription.Height * 4];
                ushort[] depthPixels = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];
                colorFrame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
                depthFrame.CopyFrameDataToArray(depthPixels);
                byte[] newPixels = new byte[colorFrameDescription.Width * colorFrameDescription.Height * 4];
                byte[] edges = new byte[colorFrameDescription.Width * colorFrameDescription.Height * 4];
                CoordinateMapper coordinateMapper = this.kinectSensor.CoordinateMapper;
                this.depthMappedToColorPoints = new DepthSpacePoint[colorFrameDescription.Width * colorFrameDescription.Height];
                KinectBuffer colorFrameData = colorFrame.LockRawImageBuffer();
                coordinateMapper.MapColorFrameToDepthSpace(depthPixels, depthMappedToColorPoints);
                removeRed(pixels, newPixels, (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4));
                findEdge(newPixels, edges, depthMappedToColorPoints,depthFrameDescription.Width,depthPixels);
                colorBitmap.WritePixels(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight), edges, colorBitmap.PixelWidth * 4, 0);
                for(int i=0;i<depthPixels.Length;i++){
                    depthPixels[i] *= 22;
                }
                //saving images
                WriteableBitmap image = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray16, null);
                image.WritePixels(new Int32Rect(0, 0, image.PixelWidth, image.PixelHeight), depthPixels, image.PixelWidth*2, 0);
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                string time = System.DateTime.UtcNow.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                string path = Path.Combine(myPhotos + "/KinectPics", "KinectScreenshot-Depth-" + time + ".png");
                try
                {
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }
                    Console.WriteLine("Saved");
                }
                catch (IOException)
                {
                    Console.WriteLine("Did not save");
                }
            }
        }
                
    
        

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
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
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.multiFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.multiFrameReader.Dispose();
                this.multiFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }



        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        private static void drawPixel(byte[] edges, int i)
        {
            edges[i] = 255;
            edges[i + 1] = 255;
            edges[i + 2] = 255;
            edges[i + 3] = 255;
        }

        private int findProximity(DepthSpacePoint[] depthMappedToColorPoints, int index, int depthWidth, ushort[] depthPixels)
        {
            int i=1;
            /*while(Double.IsInfinity(depthMappedToColorPoints[index].X)){
                if ((index >= depthMappedToColorPoints.Length)||index ==0 || i>20) return 10000;
                index+=i;
                i++;
                i*=-1;//add 1 then minus 2 (so -1 from original) and so on until closest valid point
            }*/
            if (Double.IsInfinity(depthMappedToColorPoints[index].X)) return 9999;
            int depthIndex = (int)depthMappedToColorPoints[index].X + (int)depthMappedToColorPoints[index].Y * depthWidth;
            int max = Math.Abs(depthPixels[depthIndex] - depthPixels[depthIndex-2]);
            if (Math.Abs(depthPixels[depthIndex] - depthPixels[depthIndex + 2]) > max) max = Math.Abs(depthPixels[depthIndex] - depthPixels[depthIndex + 2]);
            return max;
        }

        private void findEdge(byte[] pixels, byte[] edges, DepthSpacePoint[] depthMappedToColorPoints,int depthWidth, ushort[] depthPixels)
        {
            Boolean redSection = false;
            Console.Out.WriteLine("Finding Edges...");
            int j = 0;
            int mindist = 10000;
            int minI = 0;
            int dist = 0;
            for (int i = 0; i < pixels.Length/4; i ++)
            {
                j = i * 4;
                if (!redSection)
                {
                    if ((pixels[j] == 0) && (pixels[j + 1] == 0) && (pixels[j + 2] == 0) && (pixels[j + 3] == 0))
                    {
                        if (!(i==0)) drawPixel(edges, j - 4);
                        dist = findProximity(depthMappedToColorPoints, i,depthWidth,depthPixels);
                        if (dist < mindist) mindist = dist;
                        redSection = true;
                    }
                }
                else
                {
                    if (!((pixels[j] == 0) && (pixels[j + 1] == 0) && (pixels[j + 2] == 0) && (pixels[j + 3] == 0)))
                    {
                        drawPixel(edges, j);
                        dist = findProximity(depthMappedToColorPoints, i, depthWidth,depthPixels);
                        if (dist < mindist)
                        {
                            mindist = dist;
                            minI = i;
                        }
                        redSection = false;
                    }
                }
            }
            Console.WriteLine(mindist);
            Console.WriteLine(depthMappedToColorPoints[minI].X + " " + depthMappedToColorPoints[minI].Y);
        }

        private void removeRed(byte[] pixels, byte[] newPixals, uint size)
        {
            for (int i = 2; i < size; i = i + 4)
            {

                if ((pixels[i] - pixels[i-1]) < 0)
                {
                    int loc = i;
                    newPixals[loc - 2] = pixels[i - 2];
                    newPixals[loc - 1] = pixels[i - 1];
                    newPixals[loc] = pixels[i];
                    newPixals[loc + 1] = pixels[i + 1];
                }
            }
        }
    }
}