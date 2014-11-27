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


namespace ConsoleApplication2
{
    class RemoveRed
    {
        //removes all the 'red' objects in the image and leaves just the non red (ie the tool)
        static void Main(string[] args)
        {
                        string path = "C:\\Users\\Tom\\Documents\\University\\Second Year\\COMP2013\\RedReduction.png";
            UriBuilder urib = new UriBuilder(path);
            BitmapImage bmi = new BitmapImage(urib.Uri);
            Console.WriteLine("loaded");
            byte[] pixels = new byte[4*bmi.PixelHeight*bmi.PixelWidth];
            bmi.CopyPixels(pixels, 4*bmi.PixelWidth, 0);
            Console.WriteLine(bmi.PixelWidth + " " + bmi.PixelHeight);
            Console.ReadLine();
            byte[] newPixals = new byte[bmi.PixelWidth * bmi.PixelHeight];
            for (int i = 2; i < 4*bmi.PixelWidth*bmi.PixelHeight; i=i+4)
            {
                //Console.WriteLine(pixels[i]);
                if (pixels[i] <200){
                    int loc = (i-2)/4;
                    int x = loc % bmi.PixelWidth;
                    int y = loc / bmi.PixelWidth;
                    newPixals[loc] = 255;
                    Console.WriteLine("x=" + x + " , y=" + y);
                }
            }
            WriteableBitmap image = new WriteableBitmap(bmi.PixelWidth, bmi.PixelHeight, bmi.DpiX, bmi.DpiY, PixelFormats.Gray8, null);
            image.WritePixels(new Int32Rect(0, 0, image.PixelWidth, image.PixelHeight), newPixals, image.PixelWidth, 0);
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            string savePath = "C:\\Users\\Tom\\Documents\\University\\Second Year\\COMP2013\\RedReduced.png";
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
        
