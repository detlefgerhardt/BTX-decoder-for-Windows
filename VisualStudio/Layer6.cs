namespace BtxDecoder;

internal static class Layer6
{
    public static int Tia = 0;
    public static int Reveal = 0;
    public static int Dirty;

    private sealed class TerminalState
    {
        public int CursorX;
        public int CursorY;
        public int CursorOn;
        public int SerialMode;
        public int Wrap;
        public int ParAttr;
        public int ParFg;
        public int ParBg;
        public int[] G0123L = new int[5];
        public int[] LeftRight = new int[2];
        public int LastChar;
        public int SShift;
        public int SaveLeft;
        public int Prim;
        public int Supp;
        public int ServiceBreak;
        public int DrcsBits;
        public int DrcsStep;
        public int DrcsW;
        public int DrcsH;
        public int ColModMap;
        public int Clut;
        public int ScrollUpper;
        public int ScrollLower;
        public int ScrollImpl;
        public int ScrollArea;
        public int HoldMosaic;
        public byte BlinkMode;
        public byte BlinkRate;

        public TerminalState Clone()
        {
            return new TerminalState
            {
                CursorX = CursorX,
                CursorY = CursorY,
                CursorOn = CursorOn,
                SerialMode = SerialMode,
                Wrap = Wrap,
                ParAttr = ParAttr,
                ParFg = ParFg,
                ParBg = ParBg,
                G0123L = (int[])G0123L.Clone(),
                LeftRight = (int[])LeftRight.Clone(),
                LastChar = LastChar,
                SShift = SShift,
                SaveLeft = SaveLeft,
                Prim = Prim,
                Supp = Supp,
                ServiceBreak = ServiceBreak,
                DrcsBits = DrcsBits,
                DrcsStep = DrcsStep,
                DrcsW = DrcsW,
                DrcsH = DrcsH,
                ColModMap = ColModMap,
                Clut = Clut,
                ScrollUpper = ScrollUpper,
                ScrollLower = ScrollLower,
                ScrollImpl = ScrollImpl,
                ScrollArea = ScrollArea,
                HoldMosaic = HoldMosaic,
                BlinkMode = BlinkMode,
                BlinkRate = BlinkRate,
            };
        }

        public void CopyFrom(TerminalState other)
        {
            CursorX = other.CursorX;
            CursorY = other.CursorY;
            CursorOn = other.CursorOn;
            SerialMode = other.SerialMode;
            Wrap = other.Wrap;
            ParAttr = other.ParAttr;
            ParFg = other.ParFg;
            ParBg = other.ParBg;
            Array.Copy(other.G0123L, G0123L, 5);
            Array.Copy(other.LeftRight, LeftRight, 2);
            LastChar = other.LastChar;
            SShift = other.SShift;
            SaveLeft = other.SaveLeft;
            Prim = other.Prim;
            Supp = other.Supp;
            ServiceBreak = other.ServiceBreak;
            DrcsBits = other.DrcsBits;
            DrcsStep = other.DrcsStep;
            DrcsW = other.DrcsW;
            DrcsH = other.DrcsH;
            ColModMap = other.ColModMap;
            Clut = other.Clut;
            ScrollUpper = other.ScrollUpper;
            ScrollLower = other.ScrollLower;
            ScrollImpl = other.ScrollImpl;
            ScrollArea = other.ScrollArea;
            HoldMosaic = other.HoldMosaic;
            BlinkMode = other.BlinkMode;
            BlinkRate = other.BlinkRate;
        }
    }

    private static readonly TerminalState T = new();
    private static TerminalState Backup = new();
    private static readonly ScreenCell[,] Screen = new ScreenCell[24, 40];
    private static readonly byte[][] DrcsData = Enumerable.Range(0, 16).Select(_ => new byte[2 * Font.FONT_HEIGHT]).ToArray();

    public static int Rows = 24;
    public static int FontHeight = 10;

    public static void InitLayer6()
    {
        FontHeight = 10;
        Rows = 24;
        T.CursorX = T.CursorY = 1;
        T.Wrap = 1;
        T.ServiceBreak = 0;
        T.ScrollArea = 0;
        T.ScrollImpl = 1;
        T.CursorOn = 0;
        T.ParAttr = 0;
        T.ParFg = Attrib.WHITE;
        T.ParBg = Attrib.TRANSPARENT;
        T.BlinkMode = 0;
        T.BlinkRate = 0;

        for (int y = 0; y < 24; y++)
        {
            XFont.DefineFullrowBg(y, Attrib.BLACK);
        }

        ClearScreen();
        DefaultSets();
    }

    public static int ProcessBtxData()
    {
        int c1 = Layer2.Getc();
        if (c1 < 0)
        {
            return -1;
        }

        if (c1 >= 0x00 && c1 <= 0x1F)
        {
            return PrimaryControlC0(c1);
        }

        if (c1 >= 0x80 && c1 <= 0x9F)
        {
            SupplementaryControlC1(c1, 0);
            return 0;
        }

        int set;
        if (T.SShift != 0)
        {
            set = T.G0123L[T.SShift];
        }
        else
        {
            set = T.G0123L[T.LeftRight[(c1 & 0x80) >> 7]];
        }

        int outc = c1;
        if (set == Font.SUPP && (c1 & 0x70) == 0x40)
        {
            int c2 = Layer2.Getc();
            if (c2 < 0)
            {
                return -1;
            }

            if ((c2 & 0x60) != 0)
            {
                outc = (c1 << 8) | c2;
            }

            T.SShift = 0;
        }

        Output(outc);
        T.LastChar = outc;
        return 0;
    }

    public static void UpdateBlinkClock(long elapsedMs)
    {
        // Original C decoder keeps flashing attributes effectively steady.
        // Keep this hook as a no-op for behavior parity.
    }

    private static bool AttribAt(int y, int x, int a) => (Screen[y - 1, x - 1].Attr & a) != 0;
    private static bool RealAttribAt(int y, int x, int a) => (Screen[y - 1, x - 1].Real & a) != 0;
    private static int AttrFg(int y, int x) => Screen[y - 1, x - 1].Fg;
    private static int AttrBg(int y, int x) => Screen[y - 1, x - 1].Bg;

    private static void DefaultSets()
    {
        T.G0123L[Font.G0] = Font.PRIM;
        T.G0123L[Font.G1] = Font.SUP2;
        T.G0123L[Font.G2] = Font.SUPP;
        T.G0123L[Font.G3] = Font.SUP3;
        T.G0123L[Font.L] = Font.L;
        T.LeftRight[0] = Font.G0;
        T.LeftRight[1] = Font.G2;
        T.SShift = 0;
        T.SaveLeft = Font.G0;
        T.Prim = Font.G0;
        T.Supp = Font.G2;
    }

    private static void InvertCursor(int x, int y)
    {
        XFont.Xcursor(x, y, FontHeight);
        Dirty = 1;
    }

    private static void MoveCursor(int cmd, int y, int x)
    {
        int up = 0;
        int down = 0;

        if (T.CursorOn != 0)
        {
            InvertCursor(T.CursorX - 1, T.CursorY - 1);
        }

        switch (cmd)
        {
            case Control.APF:
                if (++T.CursorX > 40)
                {
                    if (T.Wrap != 0)
                    {
                        T.CursorX -= 40;
                        down = 1;
                    }
                    else
                    {
                        T.CursorX = 40;
                    }
                }
                break;
            case Control.APB:
                if (--T.CursorX < 1)
                {
                    if (T.Wrap != 0)
                    {
                        T.CursorX += 40;
                        up = 1;
                    }
                    else
                    {
                        T.CursorX = 1;
                    }
                }
                break;
            case Control.APU:
                up = 1;
                break;
            case Control.APD:
                down = 1;
                break;
            case Control.APR:
                T.CursorX = 1;
                break;
            case Control.APA:
                T.HoldMosaic = 0;
                if (T.Wrap != 0)
                {
                    if (x < 1)
                    {
                        x += 40;
                        y--;
                    }

                    if (x > 40)
                    {
                        x -= 40;
                        y++;
                    }

                    if (y < 1)
                    {
                        y += Rows;
                    }

                    if (y > Rows)
                    {
                        y -= Rows;
                    }
                }
                else
                {
                    if (x < 1) x = 1;
                    if (x > 40) x = 40;
                    if (y < 1) y = 1;
                    if (y > Rows) y = Rows;
                }

                T.CursorX = x;
                T.CursorY = y;
                break;
        }

        if (up != 0)
        {
            T.HoldMosaic = 0;
            if (T.ScrollArea != 0 && T.ScrollImpl != 0 && T.CursorY == T.ScrollUpper)
            {
                Scroll(0);
            }
            else if (--T.CursorY < 1)
            {
                T.CursorY = T.Wrap != 0 ? T.CursorY + Rows : 1;
            }
        }

        if (down != 0)
        {
            T.HoldMosaic = 0;
            if (T.ScrollArea != 0 && T.ScrollImpl != 0 && T.CursorY == T.ScrollLower)
            {
                Scroll(1);
            }
            else if (++T.CursorY > Rows)
            {
                T.CursorY = T.Wrap != 0 ? T.CursorY - Rows : Rows;
            }
        }

        if (T.CursorOn != 0)
        {
            InvertCursor(T.CursorX - 1, T.CursorY - 1);
        }
    }

    private static int PrimaryControlC0(int c1)
    {
        switch (c1)
        {
            case Control.APB:
            case Control.APF:
            case Control.APD:
            case Control.APU:
                MoveCursor(c1, -1, -1);
                break;
            case Control.CS:
                T.LeftRight[0] = T.SaveLeft;
                ClearScreen();
                break;
            case Control.APR:
                MoveCursor(Control.APR, -1, -1);
                break;
            case Control.LS1:
            case Control.LS0:
            {
                int c2 = c1 == Control.LS1 ? 1 : 0;
                T.LeftRight[0] = c2;
                T.SaveLeft = c2;
                break;
            }
            case Control.CON:
                if (T.CursorOn == 0)
                {
                    T.CursorOn = 1;
                    InvertCursor(T.CursorX - 1, T.CursorY - 1);
                }
                break;
            case Control.RPT:
            {
                int c2 = Layer2.Getc() & 0x3F;
                while (c2-- > 0)
                {
                    Output(T.LastChar);
                }
                break;
            }
            case Control.COF:
                if (T.CursorOn != 0)
                {
                    T.CursorOn = 0;
                    InvertCursor(T.CursorX - 1, T.CursorY - 1);
                }
                break;
            case Control.CAN:
            {
                int y = T.CursorY - 1;
                Screen[y, T.CursorX - 1].Chr = (uint)' ';
                Screen[y, T.CursorX - 1].Set = Font.PRIM;
                for (int x = T.CursorX; x < 40; x++)
                {
                    Screen[y, x].Chr = (uint)' ';
                    Screen[y, x].Set = Font.PRIM;
                    Screen[y, x].Mark = 0;
                    Screen[y, x].Attr = Screen[y, T.CursorX - 1].Attr;
                    Screen[y, x].Fg = Screen[y, T.CursorX - 1].Fg;
                    Screen[y, x].Bg = Screen[y, T.CursorX - 1].Bg;
                }

                for (int x = T.CursorX - 1; x < 40; x++)
                {
                    Redrawc(x + 1, y + 1);
                }

                break;
            }
            case Control.SS2:
                T.SShift = Font.G2;
                break;
            case Control.ESC:
                DoEsc();
                break;
            case Control.SS3:
                T.SShift = Font.G3;
                break;
            case Control.APH:
                MoveCursor(Control.APA, 1, 1);
                T.ParAttr = 0;
                T.ParFg = Attrib.WHITE;
                T.ParBg = Attrib.TRANSPARENT;
                break;
            case Control.US:
                DoUs();
                break;
            default:
                if (c1 == Control.DCT)
                {
                    return 1;
                }
                break;
        }

        return 0;
    }

    private static void SupplementaryControlC1(int c1, int fullrow)
    {
        int adv = 0;
        int mode = fullrow != 0 ? 2 : T.SerialMode;

        switch (c1)
        {
            case >= 0x80 and <= 0x87:
                SetAttr(Attrib.FOREGROUND, 1, T.Clut * 8 + c1 - 0x80, mode);
                if (mode == 1)
                {
                    T.LeftRight[0] = Font.G0;
                    T.HoldMosaic = 0;
                    T.LastChar = ' ';
                }
                break;
            case Control.FSH:
                // Original C decoder ignores flashing begin for rendering parity.
                break;
            case Control.STD:
                // Original C decoder ignores flashing end for rendering parity.
                break;
            case Control.NSZ:
                SetAttr(Attrib.NODOUBLE, 1, 0, mode);
                break;
            case Control.DBH:
                SetAttr(Attrib.YDOUBLE, 1, 0, mode);
                break;
            case Control.DBW:
                SetAttr(Attrib.XDOUBLE, 1, 0, mode);
                break;
            case Control.DBS:
                SetAttr(Attrib.XYDOUBLE, 1, 0, mode);
                break;
            case >= 0x90 and <= 0x97:
                if (mode == 1)
                {
                    SetAttr(Attrib.FOREGROUND, 1, T.Clut * 8 + c1 - 0x90, 1);
                    T.SaveLeft = T.LeftRight[0];
                    if (T.LeftRight[0] != Font.L)
                    {
                        T.LastChar = ' ';
                    }
                    T.LeftRight[0] = Font.L;
                }
                else
                {
                    SetAttr(Attrib.BACKGROUND, 1, T.Clut * 8 + c1 - 0x90, mode);
                }
                break;
            case Control.CDY:
                SetAttr(Attrib.CONCEALED, 1, 0, mode);
                break;
            case Control.SPL:
                SetAttr(Attrib.UNDERLINE, 0, 0, mode);
                break;
            case Control.STL:
                SetAttr(Attrib.UNDERLINE, 1, 0, mode);
                break;
            case Control.CSI:
                adv = DoCsi();
                break;
            case 0x9C:
                if (mode == 1)
                {
                    SetAttr(Attrib.BACKGROUND, 1, T.Clut * 8 + Attrib.BLACK, 1);
                }
                else
                {
                    SetAttr(Attrib.INVERTED, 0, 0, mode);
                }
                break;
            case 0x9D:
                if (mode == 1)
                {
                    SetAttr(Attrib.BACKGROUND, 1, Screen[T.CursorY - 1, T.CursorX - 1].Fg, 1);
                }
                else
                {
                    SetAttr(Attrib.INVERTED, 1, 0, mode);
                }
                break;
            case 0x9E:
                if (mode == 1)
                {
                    T.HoldMosaic = 1;
                }
                else
                {
                    SetAttr(Attrib.BACKGROUND, 1, Attrib.TRANSPARENT, mode);
                }
                break;
            case 0x9F:
                if (mode == 1)
                {
                    T.LastChar = ' ';
                    T.HoldMosaic = 0;
                }
                else
                {
                    SetAttr(Attrib.CONCEALED, 0, 0, mode);
                }
                break;
        }

        if (mode == 1 && (c1 != Control.CSI || adv != 0))
        {
            if (T.HoldMosaic != 0)
            {
                Output(T.LastChar);
            }
            else
            {
                MoveCursor(Control.APF, -1, -1);
            }
        }
    }

    private static void DoUs()
    {
        byte[] tfi = { (byte)Control.SOH, (byte)Control.US, 0x20, 0x7F, 0x40, (byte)Control.ETB };
        int alphamosaic = 0;

        if (T.ServiceBreak != 0)
        {
            T.CopyFrom(Backup);
            MoveCursor(Control.APA, T.CursorY, T.CursorX);
        }

        int c2 = Layer2.Getc();
        if (c2 < 0) return;

        switch (c2)
        {
            case 0x20:
            {
                int c3 = Layer2.Getc();
                if (c3 < 0) return;
                if (c3 == 0x40)
                {
                    Layer2.Write(tfi, 6);
                }
                else
                {
                    do
                    {
                        c3 = Layer2.Getc();
                        if (c3 < 0) return;
                    }
                    while ((c3 & 0x20) != 0);
                }
                break;
            }
            case 0x23:
                DoDrcs();
                break;
            case 0x26:
                DoDefColor();
                break;
            case 0x2D:
                DoDefFormat();
                break;
            case 0x2F:
                DoReset();
                alphamosaic = 1;
                break;
            case 0x3E:
                break;
            default:
                if (c2 >= 0x40)
                {
                    alphamosaic = 1;
                    int c3 = Layer2.Getc();
                    if (c3 < 0) return;
                    MoveCursor(Control.APA, c2 & 0x3F, c3 & 0x3F);
                    T.ParAttr = 0;
                    T.ParFg = Attrib.WHITE;
                    T.ParBg = Attrib.TRANSPARENT;
                }
                break;
        }

        if (alphamosaic == 0)
        {
            while ((c2 = Layer2.Getc()) != Control.US)
            {
                if (c2 < 0) return;
            }
            Layer2.Ungetc();
        }
    }

    private static void DoEsc()
    {
        int c2 = Layer2.Getc();
        if (c2 < 0) return;

        switch (c2)
        {
            case 0x22:
            {
                int c3 = Layer2.Getc();
                if (c3 < 0) return;
                if (c3 == 0x40)
                {
                    T.SerialMode = 1;
                }
                else
                {
                    T.SerialMode = 0;
                    T.LeftRight[0] = T.SaveLeft;
                }
                break;
            }
            case 0x23:
            {
                int c3 = Layer2.Getc();
                if (c3 < 0) return;
                if (c3 == 0x20)
                {
                    int c4 = Layer2.Getc();
                    if (c4 < 0) return;
                    for (int y = 0; y < 24; y++)
                    {
                        XFont.DefineFullrowBg(y, c4 == 0x5E ? Attrib.TRANSPARENT : T.Clut * 8 + c4 - 0x50);
                    }
                    RedrawScreenRect(0, 0, 39, Rows - 1);
                }
                else if (c3 == 0x21)
                {
                    int c4 = Layer2.Getc();
                    if (c4 < 0) return;
                    SupplementaryControlC1(c4 + 0x40, 1);
                }
                break;
            }
            case 0x28:
            case 0x29:
            case 0x2A:
            case 0x2B:
            {
                int c3 = Layer2.Getc();
                if (c3 < 0) return;
                int i = c2 - 0x28;
                switch (c3)
                {
                    case 0x40:
                        T.G0123L[i] = Font.PRIM;
                        T.Prim = i;
                        break;
                    case 0x62:
                        T.G0123L[i] = Font.SUPP;
                        T.Supp = i;
                        break;
                    case 0x63:
                        T.G0123L[i] = Font.SUP2;
                        break;
                    case 0x64:
                        T.G0123L[i] = Font.SUP3;
                        break;
                    case 0x20:
                    {
                        int c4 = Layer2.Getc();
                        if (c4 < 0) return;
                        T.G0123L[i] = Font.DRCS;
                        break;
                    }
                }
                break;
            }
            case 0x6E:
                T.LeftRight[0] = Font.G2;
                T.SaveLeft = Font.G2;
                break;
            case 0x6F:
                T.LeftRight[0] = Font.G3;
                T.SaveLeft = Font.G3;
                break;
            case 0x7C:
                T.LeftRight[1] = Font.G3;
                break;
            case 0x7D:
                T.LeftRight[1] = Font.G2;
                break;
            case 0x7E:
                T.LeftRight[1] = Font.G1;
                break;
        }
    }

    private static int DoCsi()
    {
        int c2 = Layer2.Getc();
        if (c2 < 0) return -1;
        if (c2 == 0x42)
        {
            SetAttr(Attrib.CONCEALED, 0, 0, T.SerialMode);
            return 0;
        }

        int c3 = Layer2.Getc();
        if (c3 < 0) return -1;

        if (c2 == 0x31 && c3 == 0x50)
        {
            SetAttr(Attrib.PROTECTED, 1, 0, 2);
            return 0;
        }
        if (c2 == 0x31 && c3 == 0x51)
        {
            SetAttr(Attrib.PROTECTED, 0, 0, 2);
            return 0;
        }
        if (c2 == 0x32 && (c3 == 0x53 || c3 == 0x54))
        {
            return 0;
        }

        switch (c3)
        {
            case 0x40:
                T.Clut = c2 - 0x30;
                return 0;
            case 0x41:
                switch (c2)
                {
                    case 0x30: // IVF
                        return 1;
                    case 0x31: // RIF
                        return 1;
                    case 0x32: // FF1
                        return 1;
                    case 0x33: // FF2
                        return 1;
                    case 0x34: // FF3
                        return 1;
                    case 0x35: // ICF
                        return 1;
                    case 0x36: // DCF
                        return 1;
                }
                return 1;
            case 0x60:
                switch (c2)
                {
                    case 0x30:
                        if (T.ScrollArea != 0) Scroll(1);
                        break;
                    case 0x31:
                        if (T.ScrollArea != 0) Scroll(0);
                        break;
                    case 0x32:
                        T.ScrollImpl = 1;
                        break;
                    case 0x33:
                        T.ScrollImpl = 0;
                        break;
                }
                return 0;
            default:
            {
                int upper = c2 & 0x0F;
                if (c3 >= 0x30 && c3 <= 0x39)
                {
                    upper = upper * 10 + (c3 & 0x0F);
                    c3 = Layer2.Getc();
                    if (c3 < 0) return -1;
                }

                int lower = Layer2.Getc() & 0x0F;
                c3 = Layer2.Getc();
                if (c3 >= 0x30 && c3 <= 0x39)
                {
                    lower = lower * 10 + (c3 & 0x0F);
                    c3 = Layer2.Getc();
                    if (c3 < 0) return -1;
                }

                if (c3 == 0x55)
                {
                    if (upper >= 2 && lower < Rows && lower >= upper)
                    {
                        T.ScrollUpper = upper;
                        T.ScrollLower = lower;
                        T.ScrollArea = 1;
                    }
                }
                if (c3 == 0x56)
                {
                    T.ScrollArea = 0;
                }
                return 0;
            }
        }
    }

    private static void DoDrcs()
    {
        int c3 = Layer2.Getc();
        if (c3 < 0) return;

        if (c3 == 0x20)
        {
            int c4 = Layer2.Getc();
            if (c4 < 0) return;
            int c5;
            if (c4 == 0x20 || c4 == 0x28)
            {
                if (c4 == 0x28)
                {
                    XFont.FreeDrcs();
                }
                c5 = Layer2.Getc();
                if (c5 < 0) return;
            }
            else
            {
                c5 = c4;
            }

            int c6;
            if (c5 == 0x20)
            {
                c6 = Layer2.Getc();
                if (c6 < 0) return;
            }
            else
            {
                c6 = c5;
            }

            int c7;
            if (c6 == 0x40)
            {
                c7 = Layer2.Getc();
                if (c7 < 0) return;
            }
            else
            {
                c7 = c6;
            }

            switch (c7 & 0xF)
            {
                case 6:
                    T.DrcsW = 12; T.DrcsH = 12; break;
                case 7:
                    T.DrcsW = 12; T.DrcsH = 10; break;
                case 10:
                    T.DrcsW = 6; T.DrcsH = 12; break;
                case 11:
                    T.DrcsW = 6; T.DrcsH = 10; break;
                case 12:
                    T.DrcsW = 6; T.DrcsH = 5; break;
                case 15:
                    T.DrcsW = 6; T.DrcsH = 6; break;
            }

            int c8 = Layer2.Getc();
            if (c8 < 0) return;
            T.DrcsBits = c8 & 0xF;
            T.DrcsStep = (T.DrcsH >= 10 && T.DrcsW * T.DrcsBits == 24) ? 2 : 1;
        }
        else
        {
            DoDrcsData(c3);
        }
    }

    private static void DoDrcsData(int c)
    {
        int maxbytes = 2 * T.DrcsH;
        if (T.DrcsH < 10)
        {
            maxbytes *= 2;
        }

        int start = c;
        byte[][] data = DrcsData;
        int planes = 0;
        int planemask = 0;
        int b = 0;
        int c4;

        do
        {
            c4 = Layer2.Getc();
            if (c4 < 0) return;

            switch (c4)
            {
                case 0x20:
                case 0x2F:
                    for (; b < maxbytes; b++)
                    {
                        for (int n = 0; n < 4; n++)
                        {
                            if ((planemask & (1 << n)) != 0)
                            {
                                data[n][b] = c4 == 0x20 ? (byte)0x00 : (byte)0xFF;
                            }
                        }
                    }
                    break;
                case 0x21:
                case 0x22:
                case 0x23:
                case 0x24:
                case 0x25:
                case 0x26:
                case 0x27:
                case 0x28:
                case 0x29:
                case 0x2A:
                    if ((b & 1) != 0)
                    {
                        b++; // pad to full row
                    }
                    for (int i = 0; i < (c4 & 0x0F); i++)
                    {
                        for (int n = 0; n < 4; n++)
                        {
                            if ((planemask & (1 << n)) == 0)
                            {
                                continue;
                            }

                            data[n][b] = b != 0 ? data[n][b - 2] : (byte)0;
                            data[n][b + 1] = b != 0 ? data[n][b - 1] : (byte)0;
                            if (T.DrcsH < 10)
                            {
                                data[n][b + 2] = b != 0 ? data[n][b - 2] : (byte)0;
                                data[n][b + 3] = b != 0 ? data[n][b - 1] : (byte)0;
                            }
                        }

                        b += 2;
                        if (T.DrcsH < 10)
                        {
                            b += 2;
                        }
                    }
                    break;
                case 0x2C:
                case 0x2D:
                    if ((b & 1) != 0)
                    {
                        b++; // pad to full row
                    }
                    for (int n = 0; n < 4; n++)
                    {
                        if ((planemask & (1 << n)) == 0)
                        {
                            continue;
                        }

                        byte rowVal = c4 == 0x2C ? (byte)0x00 : (byte)0xFF;
                        data[n][b] = rowVal;
                        data[n][b + 1] = rowVal;
                        if (T.DrcsH < 10)
                        {
                            data[n][b + 2] = rowVal;
                            data[n][b + 3] = rowVal;
                        }
                    }
                    b += 2;
                    if (T.DrcsH < 10)
                    {
                        b += 2;
                    }
                    break;
                case 0x2E:
                    if ((b & 1) != 0)
                    {
                        b++; // pad to full row
                    }
                    while (b < maxbytes)
                    {
                        for (int n = 0; n < 4; n++)
                        {
                            if ((planemask & (1 << n)) == 0)
                            {
                                continue;
                            }

                            data[n][b] = b >= 2 ? data[n][b - 2] : (byte)0;
                            data[n][b + 1] = b >= 2 ? data[n][b - 1] : (byte)0;
                        }
                        b += 2;
                    }
                    break;
                case 0x30:
                case 0x31:
                case 0x32:
                case 0x33:
                    if (b != 0)
                    {
                        b = maxbytes;
                        Layer2.Ungetc();
                    }
                    else
                    {
                        for (int i = 0; i < 2 * Font.FONT_HEIGHT; i++)
                        {
                            data[c4 & 0xF][i] = 0;
                        }
                        planemask |= 1 << (c4 & 0xF);
                    }
                    break;
                default:
                    if (c4 < 0x20 || c4 > 0x7F)
                    {
                        Layer2.Ungetc();
                        if (b != 0)
                        {
                            b = maxbytes;
                        }
                    }
                    else
                    {
                        if (T.DrcsW == 6)
                        {
                            for (int n = 0; n < 4; n++)
                            {
                                if ((planemask & (1 << n)) != 0)
                                {
                                    data[n][b] = (byte)((c4 & 32) != 0 ? 0x30 : 0);
                                    data[n][b] |= (byte)((c4 & 16) != 0 ? 0x0C : 0);
                                    data[n][b] |= (byte)((c4 & 8) != 0 ? 0x03 : 0);
                                    data[n][b + 1] = (byte)((c4 & 4) != 0 ? 0x30 : 0);
                                    data[n][b + 1] |= (byte)((c4 & 2) != 0 ? 0x0C : 0);
                                    data[n][b + 1] |= (byte)((c4 & 1) != 0 ? 0x03 : 0);
                                }
                            }
                            b += 2;
                        }
                        else
                        {
                            for (int n = 0; n < 4; n++)
                            {
                                if ((planemask & (1 << n)) != 0)
                                {
                                    data[n][b] = (byte)(c4 & 0x3F);
                                }
                            }
                            b++;
                        }

                        if ((b & 1) == 0 && T.DrcsH < 10)
                        {
                            for (int n = 0; n < 4; n++)
                            {
                                if ((planemask & (1 << n)) != 0)
                                {
                                    data[n][b] = data[n][b - 2];
                                    data[n][b + 1] = data[n][b - 1];
                                }
                            }
                            b += 2;
                        }
                    }
                    break;
            }

            if (b == maxbytes)
            {
                for (int n = 0; n < 4; n++)
                {
                    if ((planemask & (1 << n)) != 0)
                    {
                        planes++;
                    }
                }
                planemask = 0;
                b = 0;
                if (planes == T.DrcsBits)
                {
                    byte[] packed = new byte[2 * Font.FONT_HEIGHT * T.DrcsBits];
                    for (int n = 0; n < T.DrcsBits; n++)
                    {
                        Array.Copy(data[n], 0, packed, n * 2 * Font.FONT_HEIGHT, 2 * Font.FONT_HEIGHT);
                    }
                    XFont.DefineRawDrc(c, packed, T.DrcsBits);
                    planes = 0;
                    c += T.DrcsStep;
                }
            }
        }
        while (c4 >= 0x20 && c4 <= 0x7F);

        UpdateDrcsDisplay(start, c, T.DrcsStep);
    }

    private static void DoDefColor()
    {
        int c3 = Layer2.Getc();
        if (c3 < 0) return;
        switch (c3)
        {
            case 0x20:
            {
                // Color header unit: defaults to colormap loading.
                T.ColModMap = 1;

                int c4 = Layer2.Getc();
                if (c4 < 0) return;

                int c5;
                if ((c4 & 0xF0) == 0x20)
                {
                    // ICT: 0x20 -> colormap, 0x22 -> DCLUT
                    T.ColModMap = c4 == 0x20 ? 1 : 0;
                    c5 = Layer2.Getc();
                    if (c5 < 0) return;
                }
                else
                {
                    c5 = c4;
                }

                int c6;
                if ((c5 & 0xF0) == 0x20)
                {
                    // Optional ICT unit selector (only unit 0 used)
                    c6 = Layer2.Getc();
                    if (c6 < 0) return;
                }
                else
                {
                    c6 = c5;
                }

                int c7;
                if ((c6 & 0xF0) == 0x30)
                {
                    // Optional SUR
                    c7 = Layer2.Getc();
                    if (c7 < 0) return;
                }
                else
                {
                    c7 = c6;
                }

                // Optional SCM
                if ((c7 & 0xF0) != 0x40)
                {
                    Layer2.Ungetc();
                }
                return;
            }
            case 0x21:
                XFont.DefaultColors();
                return;
            default:
            {
                int index = c3 & 0x0F;
                int c4 = Layer2.Getc();
                if (c4 < 0) return;

                int c5;
                if ((c4 & 0xF0) == 0x30)
                {
                    index = (c3 & 0x0F) * 10 + (c4 & 0x0F);
                    c5 = Layer2.Getc();
                    if (c5 < 0) return;
                }
                else
                {
                    c5 = c4;
                }

                if (T.ColModMap != 0)
                {
                    // Colormap transfer (indices 16..31).
                    while (c5 >= 0x40 && c5 <= 0x7F)
                    {
                        int c6 = Layer2.Getc();
                        if (c6 < 0) return;
                        int r = ((c5 & 0x20) >> 2) | (c5 & 0x04) | ((c6 & 0x20) >> 4) | ((c6 & 0x04) >> 2);
                        int g = ((c5 & 0x10) >> 1) | ((c5 & 0x02) << 1) | ((c6 & 0x10) >> 3) | ((c6 & 0x02) >> 1);
                        int b = (c5 & 0x08) | ((c5 & 0x01) << 2) | ((c6 & 0x08) >> 2) | (c6 & 0x01);
                        if (index >= 16 && index <= 31)
                        {
                            XFont.DefineColor((uint)index++, (uint)r, (uint)g, (uint)b);
                        }
                        c5 = Layer2.Getc();
                        if (c5 < 0) return;
                    }
                }
                else
                {
                    // DCLUT transfer (indices 0..3).
                    while (c5 >= 0x40 && c5 <= 0x7F)
                    {
                        if (index >= 0 && index <= 3)
                        {
                            XFont.DefineDclut(index++, c5 & 0x1F);
                        }
                        c5 = Layer2.Getc();
                        if (c5 < 0) return;
                    }
                }

                Layer2.Ungetc();
                return;
            }
        }
    }

    private static void DoDefFormat()
    {
        Rows = 24;
        FontHeight = 10;
        T.Wrap = 1;

        int c3 = Layer2.Getc();
        if (c3 < 0) return;
        int c4;
        if ((c3 & 0xF0) == 0x40)
        {
            if (c3 == 0x42)
            {
                Rows = 20;
                FontHeight = 12;
            }
            c4 = Layer2.Getc();
            if (c4 < 0) return;
        }
        else
        {
            c4 = c3;
        }

        if ((c4 & 0xF0) == 0x70)
        {
            T.Wrap = c3 == 0x70 ? 1 : 0;
        }
        else
        {
            Layer2.Ungetc();
        }
    }

    private static void DoReset()
    {
        int c3 = Layer2.Getc();
        if (c3 < 0) return;

        switch (c3)
        {
            case 0x40:
            {
                int c4 = Layer2.Getc();
                if (c4 < 0) return;
                Backup = T.Clone();
                T.LeftRight[0] = T.Prim;
                T.LeftRight[1] = T.Supp;
                T.SaveLeft = T.Prim;
                T.Wrap = 0;
                T.CursorOn = 0;
                MoveCursor(Control.APA, c4 & 0x3F, 1);
                T.SerialMode = 1;
                T.Clut = 0;
                T.ServiceBreak = 1;
                break;
            }
            case 0x41:
            case 0x42:
                DefaultSets();
                T.SerialMode = c3 & 1;
                T.Wrap = 1;
                T.CursorOn = 0;
                Rows = 24;
                FontHeight = 10;
                for (int y = 0; y < 24; y++)
                {
                    XFont.DefineFullrowBg(y, Attrib.BLACK);
                }
                ClearScreen();
                break;
            case 0x43:
            case 0x44:
                DefaultSets();
                T.SerialMode = c3 & 1;
                break;
            case 0x4F:
                T.CopyFrom(Backup);
                MoveCursor(Control.APA, T.CursorY, T.CursorX);
                break;
        }
    }

    private static void SetAttr(int a, int set, int col, int mode)
    {
        int y = T.CursorY - 1;

        if (mode == 2 && a == Attrib.BACKGROUND)
        {
            XFont.DefineFullrowBg(y, col);
            RedrawScreenRect(0, y, 39, y);
            return;
        }

        int mattr = a;
        if ((a & Attrib.ANYSIZE) != 0)
        {
            mattr = Attrib.SIZE;
        }

        if (mode == 1)
        {
            Screen[y, T.CursorX - 1].Mark |= (uint)mattr;
        }

        if (mode != 0)
        {
            if ((T.ServiceBreak != 0 || (Screen[y, 0].Attr & Attrib.PROTECTED) == 0)
                && (a == Attrib.YDOUBLE || a == Attrib.XYDOUBLE)
                && T.ScrollArea != 0 && T.CursorY == T.ScrollLower)
            {
                Scroll(1);
                MoveCursor(Control.APU, -1, -1);
                y = T.CursorY - 1;
            }

            int x = mode == 2 ? 0 : T.CursorX - 1;
            do
            {
                int refresh = 0;

                if (mode == 2)
                {
                    Screen[y, x].Mark &= ~(uint)mattr;
                }

                if (T.ServiceBreak != 0 || (Screen[y, x].Attr & Attrib.PROTECTED) == 0 || (a == Attrib.PROTECTED && set == 0))
                {
                    switch (a)
                    {
                        case Attrib.NODOUBLE:
                            if ((Screen[y, x].Attr & (Attrib.XDOUBLE | Attrib.YDOUBLE)) != 0) refresh = 1;
                            Screen[y, x].Attr &= unchecked((uint)~(Attrib.XDOUBLE | Attrib.YDOUBLE));
                            break;
                        case Attrib.XYDOUBLE:
                            if ((Screen[y, x].Attr & (Attrib.XDOUBLE | Attrib.YDOUBLE)) != (Attrib.XDOUBLE | Attrib.YDOUBLE)) refresh = 1;
                            Screen[y, x].Attr |= (uint)(Attrib.XDOUBLE | Attrib.YDOUBLE);
                            break;
                        case Attrib.XDOUBLE:
                            if ((Screen[y, x].Attr & (Attrib.XDOUBLE | Attrib.YDOUBLE)) != Attrib.XDOUBLE) refresh = 1;
                            Screen[y, x].Attr &= unchecked((uint)~Attrib.YDOUBLE);
                            Screen[y, x].Attr |= Attrib.XDOUBLE;
                            break;
                        case Attrib.YDOUBLE:
                            if ((Screen[y, x].Attr & (Attrib.XDOUBLE | Attrib.YDOUBLE)) != Attrib.YDOUBLE) refresh = 1;
                            Screen[y, x].Attr &= unchecked((uint)~Attrib.XDOUBLE);
                            Screen[y, x].Attr |= Attrib.YDOUBLE;
                            break;
                        case Attrib.FOREGROUND:
                            if (Screen[y, x].Fg != col && Screen[y, x].Chr != ' ') refresh = 1;
                            Screen[y, x].Fg = (byte)col;
                            break;
                        case Attrib.BACKGROUND:
                            if (Screen[y, x].Bg != col) refresh = 1;
                            Screen[y, x].Bg = (byte)col;
                            break;
                        default:
                            if (((Screen[y, x].Attr & (uint)a) != 0) != (set != 0)) refresh = 1;
                            if (set != 0) Screen[y, x].Attr |= (uint)a;
                            else Screen[y, x].Attr &= unchecked((uint)~a);
                            break;
                    }

                    if (refresh != 0)
                    {
                        Redrawc(x + 1, y + 1);
                    }

                    if (a == Attrib.PROTECTED && y > 0 && RealAttribAt(y, x + 1, Attrib.YDOUBLE)
                        && (AttribAt(y, x + 1, a) != AttribAt(y + 1, x + 1, a)))
                    {
                        Redrawc(x + 1, y);
                    }
                }

                x++;
            }
            while (x < 40 && (mode != 1 || (Screen[y, x].Mark & (uint)mattr) == 0));
        }
        else
        {
            switch (a)
            {
                case Attrib.NODOUBLE:
                    T.ParAttr &= ~(Attrib.XDOUBLE | Attrib.YDOUBLE);
                    break;
                case Attrib.XYDOUBLE:
                    T.ParAttr |= Attrib.XDOUBLE | Attrib.YDOUBLE;
                    break;
                case Attrib.XDOUBLE:
                    T.ParAttr &= ~Attrib.YDOUBLE;
                    T.ParAttr |= Attrib.XDOUBLE;
                    break;
                case Attrib.YDOUBLE:
                    T.ParAttr &= ~Attrib.XDOUBLE;
                    T.ParAttr |= Attrib.YDOUBLE;
                    break;
                case Attrib.FOREGROUND:
                    T.ParFg = col;
                    break;
                case Attrib.BACKGROUND:
                    T.ParBg = col;
                    break;
                default:
                    if (set != 0) T.ParAttr |= a;
                    else T.ParAttr &= ~a;
                    break;
            }
        }
    }

    private static void Output(int c)
    {
        int x = T.CursorX;
        int y = T.CursorY;
        int set = T.SShift != 0 ? T.G0123L[T.SShift] : T.G0123L[T.LeftRight[(c & 0x80) >> 7]];

        int xd;
        int yd;

        if (T.SerialMode != 0)
        {
            xd = AttribAt(y, x, Attrib.XDOUBLE) ? 1 : 0;
            yd = AttribAt(y, x, Attrib.YDOUBLE) ? 1 : 0;

            if (T.ServiceBreak != 0 || !AttribAt(y, x, Attrib.PROTECTED))
            {
                Screen[y - 1, x - 1].Chr = (uint)(c & ~0x80);
                Screen[y - 1, x - 1].Set = (byte)set;
                Redrawc(x, y);
            }
        }
        else
        {
            xd = (T.ParAttr & Attrib.XDOUBLE) != 0 ? 1 : 0;
            yd = (T.ParAttr & Attrib.YDOUBLE) != 0 ? 1 : 0;
            if (y < 2) yd = 0;

            if (T.ServiceBreak != 0 || !AttribAt(y, x, Attrib.PROTECTED))
            {
                if (yd != 0 && T.ScrollArea != 0 && y == T.ScrollUpper)
                {
                    Scroll(0);
                    MoveCursor(Control.APD, -1, -1);
                    y = T.CursorY;
                }

                if (!(yd != 0 && T.ServiceBreak == 0 && AttribAt(y - 1, x, Attrib.PROTECTED))
                    && !(yd != 0 && T.ScrollArea != 0 && y == T.ScrollLower + 1))
                {
                    if (yd != 0) y--;

                    Screen[y - 1, x - 1].Chr = (uint)(c & ~0x80);
                    Screen[y - 1, x - 1].Set = (byte)set;
                    Screen[y - 1, x - 1].Fg = (byte)T.ParFg;
                    Screen[y - 1, x - 1].Bg = (byte)T.ParBg;
                    Screen[y - 1, x - 1].Attr = (uint)T.ParAttr;

                    if (xd != 0 && x < 40)
                    {
                        Screen[y - 1, x].Attr = (uint)T.ParAttr;
                    }

                    int mattr = T.ParAttr & ~Attrib.ANYSIZE;
                    if ((T.ParAttr & Attrib.ANYSIZE) != 0)
                    {
                        mattr |= Attrib.SIZE;
                    }

                    if (x > 1)
                    {
                        Screen[y - 1, x - 1].Mark = (uint)(Screen[y - 1, x - 2].Attr ^ mattr);
                        Screen[y - 1, x - 1].Mark &= unchecked((uint)~(Attrib.FOREGROUND | Attrib.BACKGROUND));
                        if (Screen[y - 1, x - 2].Fg != T.ParFg) Screen[y - 1, x - 1].Mark |= Attrib.FOREGROUND;
                        if (Screen[y - 1, x - 2].Bg != T.ParBg) Screen[y - 1, x - 1].Mark |= Attrib.BACKGROUND;
                    }
                    if (x < 40)
                    {
                        Screen[y - 1, x].Mark = (uint)(mattr ^ Screen[y - 1, x].Attr);
                        Screen[y - 1, x].Mark &= unchecked((uint)~(Attrib.FOREGROUND | Attrib.BACKGROUND));
                        if (Screen[y - 1, x].Fg != T.ParFg) Screen[y - 1, x].Mark |= Attrib.FOREGROUND;
                        if (Screen[y - 1, x].Bg != T.ParBg) Screen[y - 1, x].Mark |= Attrib.BACKGROUND;
                    }

                    Redrawc(x, y);
                }
            }
        }

        if (xd != 0 && T.CursorX < 40)
        {
            MoveCursor(Control.APF, -1, -1);
        }
        MoveCursor(Control.APF, -1, -1);
        T.SShift = 0;
    }

    private static void Redrawc(int x, int y)
    {
        Dirty = 1;

        if (Tia != 0)
        {
            XFont.Xputc((int)Screen[y - 1, x - 1].Chr & 0x7F, Screen[y - 1, x - 1].Set, x - 1, y - 1, 0, 0, 0,
                ((int)Screen[y - 1, x - 1].Chr >> 8) & 0x7F, Attrib.WHITE, Attrib.BLACK, FontHeight, Rows);
            if (T.CursorOn != 0 && x == T.CursorX && y == T.CursorY)
            {
                InvertCursor(x - 1, y - 1);
            }
            return;
        }

        int xd = AttribAt(y, x, Attrib.XDOUBLE) ? 1 : 0;
        int yd = AttribAt(y, x, Attrib.YDOUBLE) ? 1 : 0;
        if (x >= 40) xd = 0;
        if (y >= Rows) yd = 0;

        if ((y > 1 && RealAttribAt(y - 1, x, Attrib.YDOUBLE))
            || (x > 1 && RealAttribAt(y, x - 1, Attrib.XDOUBLE))
            || (y > 1 && x > 1 && RealAttribAt(y - 1, x - 1, Attrib.XDOUBLE) && RealAttribAt(y - 1, x - 1, Attrib.YDOUBLE)))
        {
            Screen[y - 1, x - 1].Real = 0;
            return;
        }

        if (xd != 0 && y > 1 && x < 40 && RealAttribAt(y - 1, x + 1, Attrib.YDOUBLE)) xd = 0;

        if (yd != 0 && T.ScrollArea != 0)
        {
            bool upIn = y >= T.ScrollUpper && y <= T.ScrollLower;
            bool dnIn = y + 1 >= T.ScrollUpper && y + 1 <= T.ScrollLower;
            if (upIn != dnIn) yd = 0;
        }

        if (yd != 0 && (AttribAt(y, x, Attrib.PROTECTED) != AttribAt(y + 1, x, Attrib.PROTECTED))) yd = 0;

        int c;
        int set;
        if (Reveal == 0 && AttribAt(y, x, Attrib.CONCEALED))
        {
            c = ' ';
            set = Font.PRIM;
        }
        else
        {
            c = (int)Screen[y - 1, x - 1].Chr & 0x7F;
            set = Screen[y - 1, x - 1].Set;
        }

        bool inverted = AttribAt(y, x, Attrib.INVERTED);
        int fg = AttrFg(y, x);
        int bg = AttrBg(y, x);

        XFont.Xputc(c, set, x - 1, y - 1, xd, yd,
            AttribAt(y, x, Attrib.UNDERLINE) ? 1 : 0,
            ((int)Screen[y - 1, x - 1].Chr >> 8) & 0x7F,
            inverted ? bg : fg,
            inverted ? fg : bg,
            FontHeight, Rows);

        if (T.CursorOn != 0 && x == T.CursorX && y == T.CursorY)
        {
            InvertCursor(x - 1, y - 1);
        }

        uint real = Screen[y - 1, x - 1].Real;
        Screen[y - 1, x - 1].Real = Screen[y - 1, x - 1].Attr & unchecked((uint)~Attrib.ANYSIZE);
        uint xreal = 0, yreal = 0, xyreal = 0;
        if (xd != 0)
        {
            Screen[y - 1, x - 1].Real |= Attrib.XDOUBLE;
            xreal = Screen[y - 1, x].Real;
            Screen[y - 1, x].Real = 0;
        }
        if (yd != 0)
        {
            Screen[y - 1, x - 1].Real |= Attrib.YDOUBLE;
            yreal = Screen[y, x - 1].Real;
            Screen[y, x - 1].Real = 0;
        }
        if (xd != 0 && yd != 0)
        {
            xyreal = Screen[y, x].Real;
            Screen[y, x].Real = 0;
        }

        Updatec(real, x, y);
        if (xd != 0) Updatec(xreal, x + 1, y);
        if (yd != 0) Updatec(yreal, x, y + 1);
        if (xd != 0 && yd != 0) Updatec(xyreal, x + 1, y + 1);

        if (yd != 0 && x > 1 && RealAttribAt(y + 1, x - 1, Attrib.XDOUBLE)) Redrawc(x - 1, y + 1);
    }

    private static void Updatec(uint real, int x, int y)
    {
        if ((real & Attrib.XDOUBLE) != 0) Redrawc(x + 1, y);
        if ((real & Attrib.YDOUBLE) != 0) Redrawc(x, y + 1);
        if ((real & Attrib.XDOUBLE) != 0 && (real & Attrib.YDOUBLE) != 0) Redrawc(x + 1, y + 1);
    }

    public static void RedrawScreenRect(int x1, int y1, int x2, int y2)
    {
        for (int y = y1; y <= y2; y++)
        {
            for (int x = x1; x <= x2; x++)
            {
                Redrawc(x + 1, y + 1);
            }
        }
    }

    private static void UpdateDrcsDisplay(int start, int stop, int step)
    {
        for (int y = 0; y < 24; y++)
        {
            for (int x = 0; x < 40; x++)
            {
                if (Screen[y, x].Set == Font.DRCS)
                {
                    for (int c = start; c < stop; c += step)
                    {
                        if (Screen[y, x].Chr == c)
                        {
                            Redrawc(x + 1, y + 1);
                        }
                    }
                }
            }
        }
    }

    private static void ClearScreen()
    {
        T.Clut = 0;
        T.ParAttr = 0;
        T.ParFg = Attrib.WHITE;
        T.ParBg = Attrib.TRANSPARENT;
        T.ScrollArea = 0;
        T.ScrollImpl = 1;
        T.CursorX = T.CursorY = 1;

        for (int y = 0; y < 24; y++)
        {
            for (int x = 0; x < 40; x++)
            {
                Screen[y, x].Chr = (uint)' ';
                Screen[y, x].Set = Font.PRIM;
                Screen[y, x].Attr = 0;
                Screen[y, x].Real = 0;
                Screen[y, x].Mark = 0;
                Screen[y, x].Fg = Attrib.WHITE;
                Screen[y, x].Bg = Attrib.TRANSPARENT;
                Screen[y, x].BlinkMode = 0;
            }
        }

        RedrawScreenRect(0, 0, 39, Rows - 1);
        if (T.CursorOn != 0) InvertCursor(0, 0);
    }

    private static void Scroll(int up)
    {
        int y;

        if (up != 0)
        {
            for (y = T.ScrollUpper - 1; y < T.ScrollLower - 1; y++)
            {
                for (int x = 0; x < 40; x++)
                {
                    Screen[y, x] = Screen[y + 1, x];
                }
            }
        }
        else
        {
            for (y = T.ScrollLower - 1; y > T.ScrollUpper - 1; y--)
            {
                for (int x = 0; x < 40; x++)
                {
                    Screen[y, x] = Screen[y - 1, x];
                }
            }
        }

        for (int x = 0; x < 40; x++)
        {
            Screen[y, x].Chr = (uint)' ';
            Screen[y, x].Set = Font.PRIM;
            Screen[y, x].Attr = 0;
            Screen[y, x].Real = 0;
            Screen[y, x].Mark = 0;
            Screen[y, x].Fg = Attrib.WHITE;
            Screen[y, x].Bg = Attrib.TRANSPARENT;
        }

        if (T.CursorOn != 0 && T.CursorY >= T.ScrollUpper && T.CursorY <= T.ScrollLower)
            InvertCursor(T.CursorX - 1, T.CursorY - 1);

        for (y = T.ScrollUpper; y <= T.ScrollLower; y++)
        {
            for (int x = 1; x <= 40; x++)
            {
                Redrawc(x, y);
            }
        }

        if (T.CursorOn != 0 && T.CursorY >= T.ScrollUpper && T.CursorY <= T.ScrollLower)
            InvertCursor(T.CursorX - 1, T.CursorY - 1);
    }

    public static int GetScreenCharacter(int x, int y) => (int)Screen[y, x].Chr;
}
