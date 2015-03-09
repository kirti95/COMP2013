//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.DepthBasics
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
    /// INotifyPropertyChanged - This is an interface implemented in the example, its only
    /// use is setting the text on the Window that says whether there is a Kinect currently
    /// avaliable.
    public partial class MainWindow : Window 
    {
        private const int MapDepthToByte = 8000 / 256;

        private KinectSensor kinectSensor = null;

        private DepthFrameReader depthFrameReader = null;

        private FrameDescription depthFrameDescription = null;

        private WriteableBitmap depthBitmap = null;

        //Intermediate storage for frame data converted to colour?
        private byte[] depthPixels = null;

        public MainWindow()
        {
            kinectSensor = KinectSensor.GetDefault();

            depthFrameReader = kinectSensor.DepthFrameSource.OpenReader();

            depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            depthPixels = new byte[depthFrameDescription.Width * depthFrameDescription.Height];

            depthBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            depthFrameReader.FrameArrived += depthFrameReader_FrameArrived;

            kinectSensor.Open();

            DataContext = this;

            //InitializeComponent();
        }

        public ImageSource ImageSource
        {
            get
            {
                return depthBitmap;
            }
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (this.depthFrameReader != null)
            {
                // DepthFrameReader is IDisposable
                this.depthFrameReader.Dispose();
                this.depthFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }
    }
}
