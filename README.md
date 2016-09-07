# AAG
Ascii Art Generator

This application converts images to ascii art.
It's as simple as that.

# Usage

Here is the command line usage:

    AAG.exe [/W:size] [/H:size] [/C] <input> [oputput | -]

    Ascii Art Generator - Converts Images to text files

    /W:size - Downscale image to the specified width.
              Should not be larger than 999 if you plan to view this in Notepad
    /H:size - Downscale image to the specified height.
    /C        Crop blank lines from image (white or transparent lines)
    input   - Source image file
    output  - Destination file. if not given, uses same name as image but with txt
              File extension. Destination is overwritten if it exists.
    -       - Write to stdout instead of to a file.

## Note

When resizing, the aspect ratio of the image is always kept.
Resizing a 1000x1000 image to W:900 and H:800 will result in an 800x800 image.
In other words, the arguments act as maximum size.
