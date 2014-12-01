using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace ConsoleApplication8
{
    class DepthBitmap
    {
        //Creates a depth bitmap from a still capture from the kinect

        static unsafe ushort getDepths(KinectBuffer depthBuffer, int point)
        {
            ushort* frameData = (ushort*)depthBuffer.UnderlyingBuffer;
            return (ushort)(frameData[point]*16);
        }

        static void findClosestDistance(ushort[] pixel, int size, int diff){
            for (int i=0;i< size-1;i++){
                pixel[i] = (ushort)(Math.Abs((decimal)pixel[i] - (decimal)pixel[i + 1]));
            }
        }

        private static int k;
        private static DepthFrame depthFrame = null;
        private static FrameDescription depthFrameDescription = null;

        static void Main(string[] args)
        {
            KinectSensor kinectSensor = KinectSensor.GetDefault();
            DepthFrameReader dfr = kinectSensor.DepthFrameSource.OpenReader();
            depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            kinectSensor.Open();



            while (!kinectSensor.IsAvailable)
            {
                Console.WriteLine("waiting...");
            }
            Console.WriteLine("Ready");
            
            while(true)
            {
                dfr.FrameArrived += Reader_DepthFrameArrived;
            }

            Console.ReadLine();


        }

        private static void Reader_DepthFrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            depthFrame = e.FrameReference.AcquireFrame();
            KinectBuffer depthBuffer = depthFrame.LockImageBuffer();
            int size = (int)depthBuffer.Size / 2;
            ushort[] pixels = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];
            for (int i = 0; i < size; i++)
            {
                pixels.SetValue(getDepths(depthBuffer, i), i);
            }
            findClosestDistance(pixels, size, 50);
            WriteableBitmap image = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray16, null);
            image.WritePixels(new Int32Rect(0, 0, image.PixelWidth, image.PixelHeight), pixels, image.PixelWidth * 2, 0);
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string path = Path.Combine(myPhotos + "/KinectPics", "KinectScreenshot-Depth-" + k + ".png");
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
}
