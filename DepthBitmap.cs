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

namespace ConsoleApplication1
{
    class DepthBitmap
    {
        //Creates a depth bitmap from a still capture from the kinect

        static unsafe byte getDepths(KinectBuffer depthBuffer, int point)
        {
            ushort* frameData = (ushort*)depthBuffer.UnderlyingBuffer;
            return (byte)(frameData[point]/2);
        }

        static void Main(string[] args)
        {   
            KinectSensor kinectSensor = KinectSensor.GetDefault();
            DepthFrameReader dfr = kinectSensor.DepthFrameSource.OpenReader();
            FrameDescription depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;
            
            kinectSensor.Open();
            
            
            
            while (!kinectSensor.IsAvailable)
            {
                Console.WriteLine("waiting...");
            }
            Console.WriteLine("Ready");
            DepthFrame depthFrame = null;
            while (depthFrame == null)
            {
                depthFrame = dfr.AcquireLatestFrame();
            }
            if (depthFrame != null)
            {
                KinectBuffer depthBuffer = depthFrame.LockImageBuffer();
                uint size = depthBuffer.Size ;
                byte[] pixels = new byte[depthFrameDescription.Width*depthFrameDescription.Height];
                for (int i = 0; i < size/2 ; i++)
                {
                    pixels.SetValue(getDepths(depthBuffer,i),i);
                }
                WriteableBitmap image = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);
                image.WritePixels(new Int32Rect(0, 0, image.PixelWidth, image.PixelHeight), pixels, image.PixelWidth, 0);
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
                catch(IOException)
                {
                    Console.WriteLine("Did not save");
                }
            }
            
            Console.ReadLine();


        }
    }
}
