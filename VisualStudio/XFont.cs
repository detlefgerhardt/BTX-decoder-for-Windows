namespace BtxDecoder;

internal static class XFont
{
    private sealed class BtxChar
    {
        public BtxChar? Link;
        public byte[]? Raw;
        public int Bits;
    }

    private struct Color
    {
        public byte Red;
        public byte Green;
        public byte Blue;
    }

    private static readonly BtxChar[] BtxFont = Enumerable.Range(0, 6 * 96).Select(_ => new BtxChar()).ToArray();
    private static readonly int[] FullrowBg = new int[24];
    private static readonly int[] Dclut = new int[4];
    private static readonly Color[] Colormap = new Color[32 + 4 + 24];

    public static byte[]? MemImage { get; private set; }

    public static void InitXfont()
    {
        MemImage = new byte[480 * 240 * 3];
        InitFonts();
        DefaultColors();
    }

    private static void InitFonts()
    {
        byte[] rawDel = Enumerable.Repeat((byte)0x3F, 24).ToArray();

        for (int n = 0; n < 4 * 96; n++)
        {
            BtxFont[n].Raw = RawFont.Data.Skip(n * 2 * Font.FONT_HEIGHT).Take(2 * Font.FONT_HEIGHT).ToArray();
            BtxFont[n].Bits = 1;
            BtxFont[n].Link = null;
        }

        for (int n = 0; n < 32; n++)
        {
            BtxFont[Font.SUP1 * 96 + n].Link = BtxFont[Font.SUP2 * 96 + n];
            BtxFont[Font.SUP1 * 96 + n + 32].Link = BtxFont[Font.PRIM * 96 + n + 32];
            BtxFont[Font.SUP1 * 96 + n + 64].Link = BtxFont[Font.SUP2 * 96 + n + 64];
        }

        BtxFont[Font.L * 96 + 0x7F - 0x20].Link = null;
        BtxFont[Font.L * 96 + 0x7F - 0x20].Raw = rawDel;
        BtxFont[Font.L * 96 + 0x7F - 0x20].Bits = 1;

        BtxFont[Font.DRCS * 96 + 0].Link = BtxFont[0];
        for (int n = 1; n < 96; n++)
        {
            BtxFont[Font.DRCS * 96 + n].Bits = 0;
            BtxFont[Font.DRCS * 96 + n].Raw = null;
            BtxFont[Font.DRCS * 96 + n].Link = null;
        }
    }

    public static void Xputc(int c, int set, int x, int y, int xdouble, int ydouble, int underline, int diacrit, int fg, int bg, int fontHeight, int rows)
    {
        if (x < 0 || y < 0 || x > 39 || y > rows - 1 || MemImage is null)
        {
            return;
        }

        BtxChar ch = BtxFont[set * 96 + c - 0x20];
        while (ch.Link is not null)
        {
            ch = ch.Link;
        }

        if (ch.Raw is null)
        {
            ch = BtxFont[0];
        }

        if (ch.Bits == 1)
        {
            BtxChar? dia = diacrit != 0 ? BtxFont[Font.SUPP * 96 + diacrit - 0x20] : null;
            XdrawNormalChar(ch, x, y, xdouble, ydouble, underline, dia, fg, bg, fontHeight);
        }
        else
        {
            XdrawMulticolorChar(ch, x, y, xdouble, ydouble, fontHeight);
        }
    }

    private static void SetMem(int x, int y, int col)
    {
        if (MemImage is null)
        {
            return;
        }

        int idx = y * 480 * 3 + x * 3;
        MemImage[idx + 0] = Colormap[col].Red;
        MemImage[idx + 1] = Colormap[col].Green;
        MemImage[idx + 2] = Colormap[col].Blue;
    }

    private static void XdrawNormalChar(BtxChar ch, int x, int y, int xd, int yd, int ul, BtxChar? dia, int fg, int bg, int fontHeight)
    {
        if (ch.Raw is null)
        {
            return;
        }

        if (fg == Attrib.TRANSPARENT)
        {
            fg = 32 + 4 + y;
        }

        if (bg == Attrib.TRANSPARENT)
        {
            bg = 32 + 4 + y;
        }

        int z = y * fontHeight;
        for (int yy = 0; yy < fontHeight; yy++)
        {
            for (int yyy = 0; yyy < (yd + 1); yyy++)
            {
                int s = x * Font.FONT_WIDTH;
                for (int j = 0; j < 2; j++)
                {
                    for (int i = 5; i >= 0; i--)
                    {
                        for (int xxx = 0; xxx < (xd + 1); xxx++, s++)
                        {
                            bool on = (ch.Raw[yy * 2 + j] & (1 << i)) != 0;
                            if (dia is not null && dia.Raw is not null)
                            {
                                on |= (dia.Raw[yy * 2 + j] & (1 << i)) != 0;
                            }

                            SetMem(s, z, on ? fg : bg);
                        }
                    }
                }

                z++;
            }
        }

        if (ul != 0)
        {
            for (int yyy = 0; yyy < (yd + 1); yyy++)
            {
                for (int xxx = 0; xxx < Font.FONT_WIDTH * (xd + 1); xxx++)
                {
                    SetMem(x * Font.FONT_WIDTH + xxx, y * fontHeight + (fontHeight - 1) * (yd + 1) + yyy, fg);
                }
            }
        }
    }

    private static void XdrawMulticolorChar(BtxChar ch, int x, int y, int xd, int yd, int fontHeight)
    {
        if (ch.Raw is null)
        {
            return;
        }

        int z = y * fontHeight;
        for (int yy = 0; yy < fontHeight; yy++)
        {
            for (int yyy = 0; yyy < (yd + 1); yyy++)
            {
                int s = x * Font.FONT_WIDTH;
                for (int j = 0; j < 2; j++)
                {
                    for (int i = 5; i >= 0; i--)
                    {
                        int c = 0;
                        for (int p = 0; p < ch.Bits; p++)
                        {
                            if ((ch.Raw[p * 2 * Font.FONT_HEIGHT + yy * 2 + j] & (1 << i)) != 0)
                            {
                                c |= 1 << p;
                            }
                        }

                        if (ch.Bits == 2)
                        {
                            c = Dclut[c];
                            if (c == Attrib.TRANSPARENT)
                            {
                                c = 32 + 4 + y;
                            }
                        }
                        else
                        {
                            c += 16;
                        }

                        for (int xxx = 0; xxx < (xd + 1); xxx++, s++)
                        {
                            SetMem(s, z, c);
                        }
                    }
                }

                z++;
            }
        }
    }

    public static void Xcursor(int x, int y, int fontHeight)
    {
        if (MemImage is null)
        {
            return;
        }

        for (int yy = y * fontHeight; yy < y * fontHeight + fontHeight; yy++)
        {
            for (int xx = x * 12; xx < x * 12 + 12; xx++)
            {
                int idx = yy * 480 * 3 + xx * 3;
                MemImage[idx + 0] ^= 0xFF;
                MemImage[idx + 1] ^= 0xFF;
                MemImage[idx + 2] ^= 0xFF;
            }
        }
    }

    public static void DefineRawDrc(int c, byte[] data, int bits)
    {
        BtxChar ch = BtxFont[Font.DRCS * 96 + c - 0x20];
        ch.Raw = data.Take(2 * Font.FONT_HEIGHT * bits).ToArray();
        ch.Bits = bits;
        ch.Link = null;
    }

    public static void FreeDrcs()
    {
        BtxFont[Font.DRCS * 96 + 0].Link = BtxFont[0];
        for (int n = Font.DRCS * 96 + 1; n < (Font.DRCS + 1) * 96; n++)
        {
            BtxFont[n].Raw = null;
            BtxFont[n].Link = null;
            BtxFont[n].Bits = 0;
        }
    }

    public static void DefaultColors()
    {
        for (int n = 0; n < 16; n++)
        {
            if (n == 8)
            {
                Colormap[n] = Colormap[0];
                continue;
            }

            Colormap[n].Red = (byte)(((n & 1) > 0) ? (((n & 8) == 0) ? 0xff : 0x7f) : 0);
            Colormap[n].Green = (byte)(((n & 2) > 0) ? (((n & 8) == 0) ? 0xff : 0x7f) : 0);
            Colormap[n].Blue = (byte)(((n & 4) > 0) ? (((n & 8) == 0) ? 0xff : 0x7f) : 0);
        }

        for (int n = 0; n < 8; n++)
        {
            Colormap[n + 16].Red = Colormap[n + 24].Red = (byte)((n & 1) != 0 ? 0xff : 0);
            Colormap[n + 16].Green = Colormap[n + 24].Green = (byte)((n & 2) != 0 ? 0xff : 0);
            Colormap[n + 16].Blue = Colormap[n + 24].Blue = (byte)((n & 4) != 0 ? 0xff : 0);
        }

        for (int n = 0; n < 4; n++)
        {
            Colormap[32 + n].Red = Colormap[n].Red;
            Colormap[32 + n].Green = Colormap[n].Green;
            Colormap[32 + n].Blue = Colormap[n].Blue;
            Dclut[n] = n;
        }

        for (int n = 0; n < 24; n++)
        {
            Colormap[32 + 4 + n].Red = Colormap[0].Red;
            Colormap[32 + 4 + n].Green = Colormap[0].Green;
            Colormap[32 + 4 + n].Blue = Colormap[0].Blue;
            FullrowBg[n] = 0;
        }
    }

    public static void DefineColor(uint index, uint r, uint g, uint b)
    {
        Colormap[index].Red = (byte)(r << 4);
        Colormap[index].Green = (byte)(g << 4);
        Colormap[index].Blue = (byte)(b << 4);

        for (int i = 0; i < 4; i++)
        {
            if (Dclut[i] == index)
            {
                DefineDclut(i, (int)index);
            }
        }

        for (int i = 0; i < 24; i++)
        {
            if (FullrowBg[i] == index)
            {
                DefineFullrowBg(i, (int)index);
            }
        }
    }

    public static void DefineDclut(int entry, int index)
    {
        Dclut[entry] = index;
        Colormap[32 + entry].Red = Colormap[index].Red;
        Colormap[32 + entry].Green = Colormap[index].Green;
        Colormap[32 + entry].Blue = Colormap[index].Blue;
    }

    public static void DefineFullrowBg(int row, int index)
    {
        FullrowBg[row] = index;
        Colormap[32 + 4 + row].Red = Colormap[index].Red;
        Colormap[32 + 4 + row].Green = Colormap[index].Green;
        Colormap[32 + 4 + row].Blue = Colormap[index].Blue;
    }

    public static void GetColumnColour(int column, out int r, out int g, out int b)
    {
        int c = column;
        if (c < 0)
        {
            c = 0;
        }

        if (c >= 24)
        {
            c = 23;
        }

        r = Colormap[32 + 4 + c].Red;
        g = Colormap[32 + 4 + c].Green;
        b = Colormap[32 + 4 + c].Blue;
    }
}
