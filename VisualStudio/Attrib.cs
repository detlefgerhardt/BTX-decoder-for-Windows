namespace BtxDecoder;

internal static class Attrib
{
    public const int UNDERLINE = 0x00001;
    public const int FLASH = 0x00002;
    public const int INVERTED = 0x00008;
    public const int WINDOW = 0x00010;
    public const int PROTECTED = 0x00020;
    public const int MARKED = 0x00040;
    public const int CONCEALED = 0x00080;
    public const int YDOUBLE = 0x00100;
    public const int XDOUBLE = 0x00200;

    public const int FOREGROUND = 0x00400;
    public const int BACKGROUND = 0x00800;
    public const int NODOUBLE = 0x01000;
    public const int XYDOUBLE = 0x02000;
    public const int SIZE = 0x04000;
    public const int ANYSIZE = NODOUBLE | XDOUBLE | YDOUBLE | XYDOUBLE;

    public const int BLACK = 0;
    public const int RED = 1;
    public const int GREEN = 2;
    public const int YELLOW = 3;
    public const int BLUE = 4;
    public const int MAGENTA = 5;
    public const int CYAN = 6;
    public const int WHITE = 7;
    public const int TRANSPARENT = 8;
}
