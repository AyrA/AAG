using System;
using System.IO;
using System.Text;
using System.Drawing;

namespace AAG
{
    /// <summary>
    /// Contains processed brightness data
    /// </summary>
    public struct ImageData
    {
        /// <summary>
        /// Brightness of each pixel.
        /// </summary>
        /// <remarks>Can be from 0.0 to 1.0 (including both)</remarks>
        public float[] Brightnes;
        /// <summary>
        /// Width of the image
        /// </summary>
        public int Width;
        /// <summary>
        /// Height of the image
        /// </summary>
        public int Height
        {
            get
            {
                return Brightnes.Length / Width;
            }
        }

        /// <summary>
        /// Converts the brightnes values to a string using the given charmap
        /// </summary>
        /// <param name="Charmap">Charmap. With darkest color on left and lightest on right</param>
        /// <returns>ASCII Brightnes map</returns>
        public string AsString(string Charmap)
        {
            StringBuilder SB = new StringBuilder();
            char[] Data = Array.ConvertAll(Brightnes, delegate(float f)
            {
                return Charmap[(int)(f * (Charmap.Length - 1))];
            });
            for (var i = 0; i < Height; i++)
            {
                SB.Append(Data, i * Width, Width);
                SB.AppendLine();
            }
            return SB.ToString();
        }
    }

    /// <summary>
    /// Contains processed user arguments
    /// </summary>
    public struct UserArgs
    {
        /// <summary>
        /// Image scale width
        /// </summary>
        public int Width;
        /// <summary>
        /// Image scale height
        /// </summary>
        public int Height;
        /// <summary>
        /// Source file name
        /// </summary>
        public string InFile;
        /// <summary>
        /// Destination file
        /// </summary>
        public string OutFile;
        /// <summary>
        /// Use console for output instead of file
        /// </summary>
        public bool UseConsole;
        /// <summary>
        /// Crop empty areas away
        /// </summary>
        public bool Crop;
        /// <summary>
        /// Show Help
        /// </summary>
        public bool ShowHelp;
        /// <summary>
        /// Argument validity
        /// </summary>
        public bool Valid;
    }

    /// <summary>
    /// Contains error code definitions
    /// </summary>
    public struct ERROR_CODES
    {
        /// <summary>
        /// Processing successful
        /// </summary>
        public const int SUCCESS = 0;
        /// <summary>
        /// input file not found
        /// </summary>
        public const int NOT_FOUND = 1;
        /// <summary>
        /// Input file not a valid image
        /// </summary>
        public const int IMAGE_INVALID = 2;
        /// <summary>
        /// Argument combination is invalid
        /// </summary>
        public const int INVALID_ARGUMENTS = 3;
        /// <summary>
        /// Can't write to output file
        /// </summary>
        public const int CANT_WRITE = 4;
        /// <summary>
        /// Help shown
        /// </summary>
        public const int HELP = 5;
    }
    class Program
    {
        const string BRIGHTNES = "@%#*+=-;:,. ";
        //const string BRIGHTNES = "$@B%8&WM#*oahkbdpqwmZO0QLCJUYXzcvunxrjft/\\|()1{}[]?-_+~<>i!lI;:,\"^`'. ";

        static int Main(string[] args)
        {
#if DEBUG
            Console.Write("Enter image file name/path: ");
            var FI = new FileInfo(Console.ReadLine().Trim('"'));
            var Parsed = ParseArgs(new string[] { "/W:999", "/C", FI.FullName });
#else
            if (args.Length == 0)
            {
                return Help();
            }
            var Parsed = ParseArgs(args);
            if (!Parsed.Valid)
            {
                Help();
                return ERROR_CODES.INVALID_ARGUMENTS;
            }
            var FI = new FileInfo(Parsed.InFile);
#endif
            if (FI.Exists)
            {
                //auto-generate output name if not given
                if (string.IsNullOrEmpty(Parsed.OutFile) && !Parsed.UseConsole)
                {
                    Parsed.OutFile = FI.FullName;

                    if (Path.HasExtension(Parsed.OutFile))
                    {
                        Parsed.OutFile = Parsed.OutFile.Substring(0, Parsed.OutFile.LastIndexOf('.') + 1) + "txt";
                    }
                    else
                    {
                        Parsed.OutFile += ".txt";
                    }
                }

                Console.Error.WriteLine("Reading image...");
                Bitmap B = null;
                Bitmap temp = null;
                try
                {
                    B = (Bitmap)Image.FromFile(FI.FullName);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Can't read image.\r\nError: {0}", ex.Message);
                    return ERROR_CODES.IMAGE_INVALID;
                }

                if (Parsed.Crop)
                {
                    Console.Error.WriteLine("Cropping image...");
                    using (temp = (Bitmap)B.Clone())
                    {
                        B.Dispose();
                        B = Crop(temp);
                    }
                    temp = null;
                }
                if (Parsed.Width > 0 || Parsed.Height > 0)
                {
                    Console.Error.WriteLine("Scaling image...");
                    using (temp = (Bitmap)B.Clone())
                    {
                        B.Dispose();
                        B = Scale(temp, Parsed.Width, Parsed.Height);
                    }
                    temp = null;
                }
                Console.Error.WriteLine("Extracting brightnes map...");
                var Data = Lightness(B).AsString(BRIGHTNES);
                B.Dispose();
                if (Parsed.UseConsole)
                {
                    Console.Error.WriteLine("Dumping to console...");
                    //No need for WL as the return value will already contain one at the end.
                    Console.Write(Data);
                }
                else
                {
                    Console.Error.WriteLine("Writing output...");
                    try
                    {
                        File.WriteAllText(Parsed.OutFile, Data);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Can't write output. Check if file in use or read-only.\nError: {0}", ex.Message);
                        B.Dispose();
                        return ERROR_CODES.CANT_WRITE;
                    }
                }
                B.Dispose();
            }
            else
            {
                Console.Error.WriteLine("Image not found");
                return ERROR_CODES.NOT_FOUND;
            }
#if DEBUG
            Console.Error.WriteLine("#END");
            Console.ReadKey(true);
#endif
            return ERROR_CODES.SUCCESS;
        }

        /// <summary>
        /// processes arguments. Checks if valid and fills a UserArgs Structure
        /// </summary>
        /// <param name="args">Raw arguments</param>
        /// <returns>Processed arguments</returns>
        private static UserArgs ParseArgs(string[] args)
        {
            UserArgs U = new UserArgs();
            U.Valid = true;
            U.Width = -1;
            U.Height = -1;
            U.OutFile = U.InFile = null;
            U.ShowHelp = U.UseConsole = U.Crop = false;

            foreach (string arg in args)
            {
                if (arg.ToLower() == "--help" ||
                    arg == "-?" || arg == "/?")
                {
                    U.ShowHelp = true;
                    U.Valid = true;
                    return U;
                }
                if (arg.Length >= 2 && arg[0] == '/')
                {
                    switch (arg.ToUpper().Substring(0, 2))
                    {
                        case "/C":
                            if (arg.Length == 2)
                            {
                                U.Crop = true;
                            }
                            else
                            {
                                Console.Error.WriteLine("Invalid argument: {0}. Did you mean '/C'?.", arg);
                                U.Valid = false;
                                return U;
                            }
                            break;
                        case "/W":
                            if (arg.Length > 3)
                            {
                                if (int.TryParse(arg.Substring(3), out U.Width))
                                {
                                    if (U.Width < 1)
                                    {
                                        U.Width = -1;
                                    }
                                }
                                else
                                {
                                    Console.Error.WriteLine("Invalid argument: {0}. Use /? for help.", arg);
                                    U.Valid = false;
                                    return U;
                                }
                            }
                            else
                            {
                                Console.Error.WriteLine("Argument missing a value: {0}. Use /? for help.", arg);
                                U.Valid = false;
                                return U;
                            }
                            break;
                        case "/H":
                            if (arg.Length > 3)
                            {
                                if (int.TryParse(arg.Substring(3), out U.Height))
                                {
                                    if (U.Height < 1)
                                    {
                                        U.Height = -1;
                                    }
                                }
                                else
                                {
                                    Console.Error.WriteLine("Invalid argument: {0}. Use /? for help.", arg);
                                    U.Valid = false;
                                    return U;
                                }
                            }
                            else
                            {
                                Console.Error.WriteLine("Argument missing a value: {0}. Use /? for help.", arg);
                                U.Valid = false;
                                return U;
                            }
                            break;
                        default:
                            Console.Error.WriteLine("Unknown argument: {0}. Use /? for help.", arg);
                            U.Valid = false;
                            return U;
                    }
                }
                else if (arg == "-")
                {
                    U.UseConsole = true;
                }
                else
                {
                    //assume file name at this point
                    if (string.IsNullOrEmpty(U.InFile))
                    {
                        U.InFile = arg;
                    }
                    else if (string.IsNullOrEmpty(U.OutFile))
                    {
                        U.OutFile = arg;
                    }
                    else
                    {
                        Console.Error.WriteLine("Unknown argument: {0}. Use /? for help.", arg);
                        U.Valid = false;
                        return U;
                    }
                }
            }
            //can't have both options
            if (!string.IsNullOrEmpty(U.OutFile) && U.UseConsole)
            {
                Console.Error.WriteLine("Conflict. Can't use output file and console output");
                U.Valid = false;
            }
            //missing input file
            if (!string.IsNullOrEmpty(U.OutFile))
            {
                Console.Error.WriteLine("Missing input file argument");
                U.Valid = false;
            }
            return U;
        }

        /// <summary>
        /// Simply prints Help
        /// </summary>
        /// <returns>The help error code</returns>
        private static int Help()
        {
            Console.Error.WriteLine(@"AAG.exe [/W:size] [/H:size] [/C] <input> [oputput | -]

Ascii Art Generator - Converts Images to text files

/W:size - Downscale image to the specified width.
          Should not be larger than 999 if you plan to view this in Notepad
/H:size - Downscale image to the specified height.
/C        Crop blank lines from image (white or transparent lines)
input   - Source image file
output  - Destination file. if not given, uses same name as image but with txt
          File extension. Destination is overwritten if it exists.
-       - Write to stdout instead of to a file.

Note: When resizing, the aspect ratio of the image is always kept.
      Resizing a 1000x1000 image to W:900 and H:800 will result in an
      800x800 image. In other words, the arguments act as maximum size.");
            return ERROR_CODES.HELP;
        }

        /// <summary>
        /// Scales an image down if needed (respects aspect ratio)
        /// </summary>
        /// <param name="Img">Source image</param>
        /// <param name="MaxWidth">Maximum width</param>
        /// <param name="MaxHeight">Maximum height</param>
        /// <remarks>Regardless of the need for scaling, this will not dispose the source image</remarks>
        /// <returns>Scaled image</returns>
        private static Bitmap Scale(Bitmap Img, int MaxWidth, int MaxHeight)
        {
            Bitmap Temp = (Bitmap)Img.Clone();
            Bitmap Dest = null;
            if (MaxWidth == 0 || MaxHeight == 0)
            {
                return Temp;
            }
            if (MaxWidth > 0 && Img.Width > MaxWidth)
            {
                int newHeight = MaxWidth * 100 / Img.Width * Img.Height / 100;
                //if the height is too big, do not rescale on width now, but rather on height later
                if (newHeight <= MaxHeight || MaxHeight < 0)
                {
                    Dest = new Bitmap(MaxWidth, newHeight);
                    using (Graphics G = Graphics.FromImage(Dest))
                    {
                        G.DrawImage(Temp, 0, 0, MaxWidth, newHeight);
                    }
                    Temp.Dispose();
                    Temp = (Bitmap)Dest.Clone();
                }
            }
            if (MaxHeight > 0 && Img.Height > MaxHeight)
            {
                int newWidth = MaxHeight * 100 / Img.Height * Img.Width / 100;
                //if the height is too big, do not rescale on width now, but rather on height later
                if (newWidth <= MaxHeight || MaxWidth < 0)
                {
                    Dest = new Bitmap(newWidth, MaxHeight);
                    using (Graphics G = Graphics.FromImage(Dest))
                    {
                        G.DrawImage(Temp, 0, 0, newWidth, MaxHeight);
                    }
                    Temp.Dispose();
                    Temp = (Bitmap)Dest.Clone();
                }
            }
            if (Dest != null)
            {
                Dest.Dispose();
            }
            return Temp;
        }

        /// <summary>
        /// Gets the lightmap of the specified Image
        /// </summary>
        /// <param name="I">Source Image</param>
        /// <returns>Lightmap</returns>
        static ImageData Lightness(Image I)
        {
            return Lightness((Bitmap)I);
        }

        /// <summary>
        /// Gets the lightmap of the specified Image
        /// </summary>
        /// <param name="I">Source Image</param>
        /// <returns>Lightmap</returns>
        static ImageData Lightness(Bitmap I)
        {
            ImageData ID = new ImageData();
            ID.Brightnes = new float[I.Width * I.Height];
            ID.Width = I.Width;

            for (var y = 0; y < I.Height; y++)
            {
                for (var x = 0; x < I.Width; x++)
                {
                    Color Pixel = I.GetPixel(x, y);
                    ID.Brightnes[I.Width * y + x] = Pixel.A == 0 ? 1.0f : Pixel.GetBrightness();
                }
                if (y % 20 == 0)
                {
                    Console.Error.Write('.');
                }
            }
            Console.Error.WriteLine();
            return ID;
        }

        /// <summary>
        /// Removes whitespace from an image
        /// </summary>
        /// <remarks>http://stackoverflow.com/questions/248141/remove-surrounding-whitespace-from-an-image</remarks>
        /// <param name="bmp">Source image</param>
        /// <returns>Cropped image</returns>
        public static Bitmap Crop(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;

            int topmost = 0;
            for (int row = 0; row < h; ++row)
            {
                if (ShouldCropRow(bmp, row))
                    topmost = row;
                else break;
            }

            int bottommost = 0;
            for (int row = h - 1; row >= 0; --row)
            {
                if (ShouldCropRow(bmp, row))
                    bottommost = row;
                else break;
            }

            int leftmost = 0, rightmost = 0;
            for (int col = 0; col < w; ++col)
            {
                if (ShouldCropCol(bmp, col))
                    leftmost = col;
                else
                    break;
            }

            for (int col = w - 1; col >= 0; --col)
            {
                if (ShouldCropCol(bmp, col))
                    rightmost = col;
                else
                    break;
            }

            if (rightmost == 0) rightmost = w; // As reached left
            if (bottommost == 0) bottommost = h; // As reached top.

            int croppedWidth = rightmost - leftmost;
            int croppedHeight = bottommost - topmost;

            if (croppedWidth == 0) // No border on left or right
            {
                leftmost = 0;
                croppedWidth = w;
            }

            if (croppedHeight == 0) // No border on top or bottom
            {
                topmost = 0;
                croppedHeight = h;
            }

            try
            {
                //don't attempt to crop if none needed.
                if (croppedWidth == bmp.Width && croppedHeight == bmp.Height)
                {
                    return (Bitmap)bmp.Clone();
                }
                var target = new Bitmap(croppedWidth, croppedHeight);
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(bmp,
                      new RectangleF(0, 0, croppedWidth, croppedHeight),
                      new RectangleF(leftmost, topmost, croppedWidth, croppedHeight),
                      GraphicsUnit.Pixel);
                }
                return target;
            }
            catch (Exception ex)
            {
                throw new Exception(
                  string.Format("Values are topmost={0} btm={1} left={2} right={3} croppedWidth={4} croppedHeight={5}", topmost, bottommost, leftmost, rightmost, croppedWidth, croppedHeight),
                  ex);
            }
        }

        /// <summary>
        /// Checks if a row is empty
        /// </summary>
        /// <param name="B">Image</param>
        /// <param name="RowIndex">Row to check</param>
        /// <returns>true, if to crop</returns>
        private static bool ShouldCropRow(Bitmap B, int RowIndex)
        {
            for (var i = 0; i < B.Width; i++)
            {
                if (!CropThis(B.GetPixel(i, RowIndex)))
                {
                    return false;
                }
                /*
                var C = B.GetPixel(i, RowIndex);
                var Col = C.ToArgb() & 0xFFFFFF;
                if (C.A > 0 && Col != 0xFFFFFF)
                {
                    return false;
                }
                //*/
            }
            return true;
        }

        /// <summary>
        /// Checks if a column is empty
        /// </summary>
        /// <param name="B">Image</param>
        /// <param name="ColIndex">Column to check</param>
        /// <returns>true, if to crop</returns>
        private static bool ShouldCropCol(Bitmap B, int ColIndex)
        {
            for (var i = 0; i < B.Height; i++)
            {
                if (!CropThis(B.GetPixel(ColIndex, i)))
                {
                    return false;
                }
                /*
                var C = B.GetPixel(ColIndex, i);
                var Col = C.ToArgb() & 0xFFFFFF;
                if (C.A > 0 && Col != 0xFFFFFF)
                {
                    return false;
                }
                //*/
            }
            return true;
        }

        /// <summary>
        /// Check if this pixel fulfills the crop condition
        /// </summary>
        /// <param name="C">Pixel color</param>
        /// <returns>true, if we should crop this</returns>
        private static bool CropThis(Color C)
        {
            var Col = C.ToArgb() & 0xFFFFFF;
            if (C.A > 0 && Col != 0xFFFFFF)
            {
                return false;
            }
            return true;
        }
    }
}
