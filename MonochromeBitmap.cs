using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace ConsoleApplication3
{
    class MonochromeBitmap
    {
        //Creates a monochromatic image from the kinect (Could be used to take depth image and remove background)
        static unsafe byte getpixel(KinectBuffer buffer, int point)
        {
            byte* frameData = (byte*)buffer.UnderlyingBuffer;
            return (byte)(frameData[point] );
        }

        static void Main(string[] args)
        {
            KinectSensor sensor = KinectSensor.GetDefault();
            ColorFrameReader reader = sensor.ColorFrameSource.OpenReader();
            FrameDescription description = sensor.ColorFrameSource.FrameDescription;
            sensor.Open();
            while (!sensor.IsAvailable)
            {
                Console.WriteLine("Waiting...");
                System.Threading.Thread.Sleep(1000);
            }
            Console.WriteLine("Ready");
            ColorFrame colorframe = null;
            while (colorframe == null)
            {
                colorframe = reader.AcquireLatestFrame();
            }
            KinectBuffer buffer = colorframe.LockRawImageBuffer();
            uint size = buffer.Size;
            byte[] pixels = new byte[size];
            Console.WriteLine(description.BytesPerPixel + " Bytes per pixal");
            Console.WriteLine(buffer.Size + "pixals");
            for (int i = 0; i < size; i += 2)
            {
                pixels.SetValue(getpixel(buffer, i), i/2);
            }

            Console.WriteLine("Entering editing");


            byte[] newPixels = new byte[size];
            Console.WriteLine("Entering editing");
            for (int i = 0; i < size; i++)
            {

                if (pixels[i] < 100)
                {
                    newPixels[i] = 0;
                }
                else newPixels[i] = 255;
            }
            WriteableBitmap image = new WriteableBitmap(description.Width, description.Height, 96.0, 96.0, PixelFormats.Gray8, null);
            image.WritePixels(new Int32Rect(0, 0, image.PixelWidth, image.PixelHeight), newPixels, image.PixelWidth, 0);
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            string savePath = "C:\\Users\\Tom\\Documents\\University\\Second Year\\COMP2013\\KinectRedReduced.png";
            try
            {
                using (FileStream fs = new FileStream(savePath, FileMode.Create))
                {
                    encoder.Save(fs);
                }
                Console.WriteLine("Saved");
            }
            catch (IOException)
            {
                Console.WriteLine("Did not save");
            }
            Console.ReadLine();


        }
    }
}
