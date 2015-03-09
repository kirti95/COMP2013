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
    /// INotifyPropertyChanged
    public partial class MainWindow : Window
    {
        private KinectSensor kinectSensor = null;

        private ColorFrameReader colorFrameReader = null;

        private WriteableBitmap colorBitmap = null;

        public MainWindow()
        {
            kinectSensor = KinectSensor.GetDefault();

            colorFrameReader = kinectSensor.ColorFrameSource.OpenReader();

            FrameDescription colorFrameDescription = kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;

            kinectSensor.Open();

            //NECESSARY TO DISPLAY THE IMAGE
            DataContext = this;

            //InitializeComponent();
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

        private void Window_Closing(object sender, CancelEventArgs e)
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
