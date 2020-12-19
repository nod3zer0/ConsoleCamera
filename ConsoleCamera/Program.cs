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
using CommandLine;
using CommandLine.Text;

namespace ConsoleCamera
{

    //arguments for this console app
    class Options
    {
        

        [Option('h', "height", Required = true,
          HelpText = "height default 160")]
        public int height { get; set; }

        [Option('w', "width", Required = true,
          HelpText = "width default 160")]
        public int width { get; set; }

        [Option('p', "pixelSize", Required = true,
         HelpText = "pixelSize default 5")]
        public short pixelSize { get; set; }

        [Option('i', "cameraInterface", Required = true,
          HelpText = "number of camera interface default 3")]
        public short cameraInterface { get; set; }

        [HelpOption]
        public string GetUsage()
        {
          
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));


        }
    }

    ///copy pasted from stackowerflow from:https://stackoverflow.com/questions/6554536/possible-to-get-set-console-font-size-in-c-sharp-net
    ///it works and writing code for changing font wasnt goal of this project for me
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
        //updates newFrame varriable every time new frame is available
        void videoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            newFrame = (Bitmap)eventArgs.Frame.Clone();
        }

        public static int height = 160;
        public static int width = 160;
        public static short pixelSize = 5;

        static void Main(string[] args)
        {
            Program prog = new Program();
            int webcamera = 3; //webCamInterface (mine is 3)

            var options = new Options();

          //command line arguments
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                webcamera = options.cameraInterface;
                height = options.height;
                width = options.width;
                pixelSize = options.pixelSize;
            }
            else
            {
                 return;
            }




            //sets font size so its possible to show webcamera
            ConsoleHelper.SetCurrentFont("Consolas", pixelSize);
            VideoCaptureDevice videoSource;
            FilterInfoCollection videosources = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            //Check if atleast one video source is available
            if (videosources != null)
            {
                //For example use first video device. You may check if this is your webcam.
                videoSource = new VideoCaptureDevice(videosources[webcamera].MonikerString);

                try
                {
                    //Check if the video device provides a list of supported resolutions
                    if (videoSource.VideoCapabilities.Length > 0)
                    {
                        string highestSolution = "0;0";
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

                DrawImage(prog.newFrame);

            }
            while (true) { }
            //Console.ReadKey();
            //resized.Save("DSC_0002_thumb.jpg");

        }

        public static void DrawImage(Bitmap bitmap)
        {
            

            //lowers the resolution of camera
            double scale = /*0.25f*/ Math.Max(width / (float)bitmap.Width, height / (float)bitmap.Height);
            Bitmap resized = new Bitmap(bitmap, new Size((int)(bitmap.Width * scale), (int)(bitmap.Height * scale)));

            //It could be done with just arr2, and renderArray will be removed in future versions
            char[,] arr = new char[resized.Width, resized.Height];
            string[] renderArray = new string[resized.Height];


            //assing 5 different chars to brightness values
            for (int y = 0; y < arr.GetLength(1); y++)
            {
                for (int x = 0; x < arr.GetLength(0); x++)
                {
                    //░▒▓█ if I forget how its written
                   
                    //for some reason it was crashing, it will be repaired in future versions
                    if (y >= resized.Height)
                    {
                        break;
                    }
                    float brightness = resized.GetPixel(x, y).GetBrightness();


                    //assing 5 different chars to brightness values
                    //it will be optimalized in future versions
                    if (1f >= brightness && brightness >= 0.8f)
                    {
                       
                        renderArray[y] += "██";
                    }
                    else if (0.8f > brightness && brightness >= 0.6f)
                    {
                      
                        renderArray[y] += "▓▓";
                    }
                    else if (0.6f > brightness && brightness >= 0.4f)
                    {
                       
                        renderArray[y] += "▒▒";
                    }
                    else if (0.4f > brightness && brightness >= 0.2f)
                    {
                       
                        renderArray[y] += "░░";
                    }
                    else if (0.2f > brightness)
                    {
                    
                        renderArray[y] += "  ";
                    }


                }
            }





            //drawing final image to console
            for (int y = 0; y < arr.GetLength(1); y++)
            {
                Console.WriteLine(renderArray[y]);
            }

            //sets cursor to top of console because its faster than Console.Clear();
            Console.SetCursorPosition(0, 0);

        }

       
    }
}
