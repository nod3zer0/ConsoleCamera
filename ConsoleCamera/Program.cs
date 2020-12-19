using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AForge.Video;
using AForge.Video.DirectShow;

namespace ConsoleCamera
{



    public static class ConsoleHelper
    {
        private const int FixedWidthTrueType = 54;
        private const int StandardOutputHandle = -11;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool MaximumWindow, ref FontInfo ConsoleCurrentFontEx);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool GetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool MaximumWindow, ref FontInfo ConsoleCurrentFontEx);


        private static readonly IntPtr ConsoleOutputHandle = GetStdHandle(StandardOutputHandle);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct FontInfo
        {
            internal int cbSize;
            internal int FontIndex;
            internal short FontWidth;
            public short FontSize;
            public int FontFamily;
            public int FontWeight;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            //[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.wc, SizeConst = 32)]
            public string FontName;
        }

        public static FontInfo[] SetCurrentFont(string font, short fontSize = 0)
        {
            Console.WriteLine("Set Current Font: " + font);

            FontInfo before = new FontInfo
            {
                cbSize = Marshal.SizeOf<FontInfo>()
            };

            if (GetCurrentConsoleFontEx(ConsoleOutputHandle, false, ref before))
            {

                FontInfo set = new FontInfo
                {
                    cbSize = Marshal.SizeOf<FontInfo>(),
                    FontIndex = 0,
                    FontFamily = FixedWidthTrueType,
                    FontName = font,
                    FontWeight = 400,
                    FontSize = fontSize > 0 ? fontSize : before.FontSize
                };

                // Get some settings from current font.
                if (!SetCurrentConsoleFontEx(ConsoleOutputHandle, false, ref set))
                {
                    var ex = Marshal.GetLastWin32Error();
                    Console.WriteLine("Set error " + ex);
                    throw new System.ComponentModel.Win32Exception(ex);
                }

                FontInfo after = new FontInfo
                {
                    cbSize = Marshal.SizeOf<FontInfo>()
                };
                GetCurrentConsoleFontEx(ConsoleOutputHandle, false, ref after);

                return new[] { before, set, after };
            }
            else
            {
                var er = Marshal.GetLastWin32Error();
                Console.WriteLine("Get error " + er);
                throw new System.ComponentModel.Win32Exception(er);
            }
        }
    }



    class Program
    {
        Bitmap newFrame = new Bitmap(1, 1);

        void videoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            //Cast the frame as Bitmap object and don't forget to use ".Clone()" otherwise
            //you'll probably get access violation exceptions
            newFrame = (Bitmap)eventArgs.Frame.Clone();
        }


        static void Main(string[] args)
        {

         

            Program prog = new Program();
            ConsoleHelper.SetCurrentFont("Consolas", 5);
            VideoCaptureDevice videoSource;
            FilterInfoCollection videosources = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            //Check if atleast one video source is available
            if (videosources != null)
            {
                //For example use first video device. You may check if this is your webcam.
                videoSource = new VideoCaptureDevice(videosources[3].MonikerString);

                try
                {
                    //Check if the video device provides a list of supported resolutions
                    if (videoSource.VideoCapabilities.Length > 0)
                    {
                        string highestSolution = "300;150";
                        //Search for the highest resolution
                        for (int i = 0; i < videoSource.VideoCapabilities.Length; i++)
                        {
                            if (videoSource.VideoCapabilities[i].FrameSize.Width > Convert.ToInt32(highestSolution.Split(';')[0]))
                                highestSolution = videoSource.VideoCapabilities[i].FrameSize.Width.ToString() + ";" + i.ToString();
                        }
                        //Set the highest resolution as active
                        videoSource.VideoResolution = videoSource.VideoCapabilities[Convert.ToInt32(highestSolution.Split(';')[1])];
                    }
                }
                catch { }

                //Create NewFrame event handler
                //(This one triggers every time a new frame/image is captured
                videoSource.NewFrame += new AForge.Video.NewFrameEventHandler(prog.videoSource_NewFrame);

                //Start recording
                videoSource.Start();
            }
            while (true)
            {

                //Bitmap bitmap = new Bitmap(@"C:\Users\game1\source\repos\ConsoleCamera\ConsoleCamera\Anonymous_emblem.svg.png");
                DrawImage(prog.newFrame);

            }
            while (true) { }
            //Console.ReadKey();
            //resized.Save("DSC_0002_thumb.jpg");

        }

        public static void DrawImage(Bitmap bitmap)
        {
            char[,] arr = new char[400, 100];
            string[] arr2 = new string[200];
            //bitmap= ConvertToGrayScale(bitmap);

            double scale = /*0.25f*/ Math.Min(200f / bitmap.Width, 200f / bitmap.Height);
            Bitmap resized = new Bitmap(bitmap, new Size((int)(bitmap.Width * scale), (int)(bitmap.Height * scale)));



            for (int y = 0; y < arr.GetLength(1); y++)
            {
                for (int x = 0; x < arr.GetLength(0); x += 2)
                {
                    //░▒▓█

                    if (y >= resized.Height)
                    {
                        break;
                    }
                    float brightness = resized.GetPixel(x / 2, y).GetBrightness();

                    if (1f >= brightness && brightness >= 0.8f)
                    {
                        arr[x, y] = '█';
                        arr[x + 1, y] = '█';
                        arr2[y] += "██";
                    }
                    else if (0.8f > brightness && brightness >= 0.6f)
                    {
                        arr[x, y] = '▓';
                        arr[x + 1, y] = '▓';
                        arr2[y] += "▓▓";
                    }
                    else if (0.6f > brightness && brightness >= 0.4f)
                    {
                        arr[x, y] = '▒';
                        arr[x + 1, y] = '▒';
                        arr2[y] += "▒▒";
                    }
                    else if (0.4f > brightness && brightness >= 0.2f)
                    {
                        arr[x, y] = '░';
                        arr[x + 1, y] = '░';
                        arr2[y] += "░░";
                    }
                    else if (0.2f > brightness)
                    {
                        arr[x, y] = ' ';
                        arr[x + 1, y] = ' ';
                        arr2[y] += "  ";
                    }


                }
            }






            for (int y = 0; y < arr.GetLength(1); y++)
            {
                Console.WriteLine(arr2[y]);
            }

            //var fonts = ConsoleHelper.ConsoleFonts;
            //for (int f = 0; f < fonts.Length; f++)
            //    Console.WriteLine("{0}: X={1}, Y={2}",
            //       fonts[f].Index, fonts[f].SizeX, fonts[f].SizeY);

            //ConsoleHelper.SetConsoleFont(5);
            //ConsoleHelper.SetConsoleIcon(SystemIcons.Information);
            Console.SetCursorPosition(0, 0);

        }

        public static Bitmap ConvertToGrayScale(Bitmap c)
        {

            Bitmap d;
            int x, y;

            // Loop through the images pixels to reset color.
            for (x = 0; x < c.Width; x++)
            {
                for (y = 0; y < c.Height; y++)
                {
                    Color pixelColor = c.GetPixel(x, y);
                    Color newColor = Color.FromArgb(pixelColor.R, 0, 0);
                    c.SetPixel(x, y, newColor); // Now greyscale
                }
            }
            return c;
        }
    }
}
