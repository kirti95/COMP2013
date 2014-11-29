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

namespace RemoveRedKinect
{
    class RemoveRedKinect
    {
        
        //removes all the 'red' objects in the image and leaves just the non red (ie the tool)
        static void Main(string[] args)
        {

            KinectSensor sensor = KinectSensor.GetDefault();
            ColorFrameReader reader = sensor.ColorFrameSource.OpenReader();
            sensor.Open();
            while (!sensor.IsAvailable)
            {
                Console.WriteLine("Waiting...");
                System.Threading.Thread.Sleep(1000);
            }
            Console.WriteLine("Ready");
            ColorFrame colorframe = null;
            FrameDescription description = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            WriteableBitmap nonremoved = new WriteableBitmap(description.Width, description.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            Boolean done = false;
            Byte[] pixels = null;
            uint size=0;
            while (!done)
            {
                while (colorframe == null)
                {
                    colorframe = reader.AcquireLatestFrame();
                }
                Console.WriteLine("Recieved image");
                size = 4 * (uint)description.Width * (uint)description.Height;
                pixels = new byte[size];
                using (KinectBuffer buffer = colorframe.LockRawImageBuffer())
                {
                    nonremoved.Lock();
                    if ((description.Width == nonremoved.PixelWidth) && (description.Height == nonremoved.PixelHeight))
                    {

                        colorframe.CopyConvertedFrameDataToIntPtr(nonremoved.BackBuffer, size, ColorImageFormat.Bgra);
                        nonremoved.AddDirtyRect(new Int32Rect(0, 0, nonremoved.PixelWidth, nonremoved.PixelHeight));
                        done = true;

                    }
                    nonremoved.Unlock();

                }
            }
            byte[] newPixels = new byte[size];
            nonremoved.CopyPixels(pixels, nonremoved.PixelWidth*4,0);
            Console.WriteLine("removing red");
            removeRed(pixels, newPixels, size);
            Console.WriteLine("Red removed");
            WriteableBitmap image = new WriteableBitmap(description.Width, description.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            
            image.WritePixels(new Int32Rect(0, 0, image.PixelWidth, image.PixelHeight), newPixels, image.PixelWidth*4, 0);
            //saving bitmaps
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
            encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(nonremoved));
            savePath = "C:\\Users\\Tom\\Documents\\University\\Second Year\\COMP2013\\Kinect.png";
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

        private static void removeRed(byte[] pixels, byte[] newPixals, uint size)
        {
            for (int i = 2; i < size; i = i + 4)
            {
                //Console.WriteLine(pixels[i]);
                if (pixels[i] < 70)
                {
                    int loc = i;
                    newPixals[loc - 2] = 255;
                    newPixals[loc - 1] = 255;
                    newPixals[loc] = 255;
                    newPixals[loc + 1] = 0;
                }
            }
        }
    }
}
