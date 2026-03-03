using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using BtxDecoder;

internal static class Program
{
    private static volatile bool _quit = true;
    private static volatile int _keyboardMode;
    private static readonly object _keyLogLock = new();
    private static readonly string _keyLogPath = System.IO.Path.Combine(AppContext.BaseDirectory, "btx_keydown.log");

    private const int KmNormal = 0;
    private const int KmFColor = 1;
    private const int KmBColor = 2;
    private const int KmSize = 3;
    private const int KmAttr = 3;
    private const int KmGShift = 4;

    private const string Version = "v1.1";

    private static readonly (int Mode, string Input, byte[] Output)[] Translations =
    {
        (KmNormal, "Ä", "\x19HA"u8.ToArray()),
        (KmNormal, "Ö", "\x19HO"u8.ToArray()),
        (KmNormal, "Ü", "\x19HU"u8.ToArray()),
        (KmNormal, "ä", "\x19Ha"u8.ToArray()),
        (KmNormal, "ö", "\x19Ho"u8.ToArray()),
        (KmNormal, "ü", "\x19Hu"u8.ToArray()),
        (KmNormal, "ß", "\x19\x7b"u8.ToArray()),
        (KmNormal, "¡", "\x19\x21"u8.ToArray()),
        (KmNormal, "¢", "\x19\x22"u8.ToArray()),
        (KmNormal, "£", "\x19\x23"u8.ToArray()),
        (KmNormal, "$", "\x19\x24"u8.ToArray()),
        (KmNormal, "¥", "\x19\x25"u8.ToArray()),
        (KmNormal, "#", "\x19\x26"u8.ToArray()),
        (KmNormal, "§", "\x19\x27"u8.ToArray()),
        (KmNormal, "¤", "\x19\x28"u8.ToArray()),
        (KmNormal, "‘", "\x19\x29"u8.ToArray()),
        (KmNormal, "“", "\x19\x2A"u8.ToArray()),
        (KmFColor, "0", "\x80"u8.ToArray()),
        (KmFColor, "1", "\x81"u8.ToArray()),
        (KmFColor, "2", "\x82"u8.ToArray()),
        (KmFColor, "3", "\x83"u8.ToArray()),
        (KmFColor, "4", "\x84"u8.ToArray()),
        (KmFColor, "5", "\x85"u8.ToArray()),
        (KmFColor, "6", "\x86"u8.ToArray()),
        (KmFColor, "7", "\x87"u8.ToArray()),
        (KmFColor, "a", "\x9B\x30\x40"u8.ToArray()),
        (KmFColor, "b", "\x9B\x31\x40"u8.ToArray()),
        (KmFColor, "c", "\x9B\x32\x40"u8.ToArray()),
        (KmFColor, "d", "\x9B\x33\x40"u8.ToArray()),
        (KmBColor, "0", "\x90"u8.ToArray()),
        (KmBColor, "1", "\x91"u8.ToArray()),
        (KmBColor, "2", "\x92"u8.ToArray()),
        (KmBColor, "3", "\x93"u8.ToArray()),
        (KmBColor, "4", "\x94"u8.ToArray()),
        (KmBColor, "5", "\x95"u8.ToArray()),
        (KmBColor, "6", "\x96"u8.ToArray()),
        (KmBColor, "7", "\x97"u8.ToArray()),
        (KmBColor, "t", "\x9E"u8.ToArray()),
        (KmSize, "0", "\x8C"u8.ToArray()),
        (KmSize, "1", "\x8D"u8.ToArray()),
        (KmSize, "2", "\x8E"u8.ToArray()),
        (KmSize, "3", "\x8F"u8.ToArray()),
        (KmAttr, "L", "\x9A"u8.ToArray()),
        (KmAttr, "l", "\x99"u8.ToArray()),
        (KmAttr, "I", "\x9D"u8.ToArray()),
        (KmAttr, "i", "\x9C"u8.ToArray()),
        (KmGShift, "0", "\x0F"u8.ToArray()),
        (KmGShift, "1", "\x0E"u8.ToArray()),
        (KmGShift, "2", "\x19"u8.ToArray()),
        (KmGShift, "3", "\x1D"u8.ToArray()),
    };

    private static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: btx_decoder_csharp <host>:<port>");
            Console.WriteLine("Example: btx_decoder_csharp 195.201.94.166:20000");
            return 1;
        }

        string[] hp = args[0].Split(':', 2);
        string host = hp[0];
        int port = hp.Length > 1 && int.TryParse(hp[1], out int p) ? p : 20000;

        XFont.InitXfont();
        Layer6.InitLayer6();
        Layer6.Dirty = 1;

        Thread decoder = new(() => DecoderThread(host, port));
        decoder.IsBackground = true;
        decoder.Start();

        int renderWidth = 624 * 2;
        int renderHeight = 288 * 3;
        int windowWidth = renderWidth / 2;
        int windowHeight = renderHeight / 2;

        Sdl3Native.InitNativeResolver();
        if (!Sdl3Native.SDL_Init(Sdl3Native.SDL_INIT_VIDEO))
        {
            Console.Error.WriteLine($"SDL_Init failed: {Sdl3Native.GetErrorString()}");
            _quit = false;
            return 1;
        }

        IntPtr window = Sdl3Native.SDL_CreateWindow(
            $"BTX-Decoder  {Version}  [connected to {host}:{port}]",
            windowWidth,
            windowHeight,
            Sdl3Native.SDL_WINDOW_RESIZABLE);
        if (window == IntPtr.Zero)
        {
            Console.Error.WriteLine($"SDL_CreateWindow failed: {Sdl3Native.GetErrorString()}");
            Sdl3Native.SDL_Quit();
            _quit = false;
            return 1;
        }

        IntPtr renderer = Sdl3Native.SDL_CreateRenderer(window, null);
        if (renderer == IntPtr.Zero)
        {
            Console.Error.WriteLine($"SDL_CreateRenderer failed: {Sdl3Native.GetErrorString()}");
            Sdl3Native.SDL_DestroyWindow(window);
            Sdl3Native.SDL_Quit();
            _quit = false;
            return 1;
        }

        if (!Sdl3Native.SDL_SetRenderLogicalPresentation(
            renderer,
            renderWidth,
            renderHeight,
            Sdl3Native.SDL_LOGICAL_PRESENTATION_LETTERBOX))
        {
            Console.Error.WriteLine($"SDL_SetRenderLogicalPresentation failed: {Sdl3Native.GetErrorString()}");
        }

        IntPtr texture = Sdl3Native.SDL_CreateTexture(
            renderer,
            Sdl3Native.SDL_PIXELFORMAT_ARGB8888,
            Sdl3Native.SDL_TEXTUREACCESS_STATIC,
            renderWidth,
            renderHeight);
        if (texture == IntPtr.Zero)
        {
            Console.Error.WriteLine($"SDL_CreateTexture failed: {Sdl3Native.GetErrorString()}");
            Sdl3Native.SDL_DestroyRenderer(renderer);
            Sdl3Native.SDL_DestroyWindow(window);
            Sdl3Native.SDL_Quit();
            _quit = false;
            return 1;
        }

        uint[] pixels = new uint[renderWidth * renderHeight];
        Stopwatch blinkClock = Stopwatch.StartNew();
        if (!Sdl3Native.SDL_StartTextInput(window))
        {
            Console.Error.WriteLine($"SDL_StartTextInput failed: {Sdl3Native.GetErrorString()}");
        }

        try
        {
            while (_quit)
            {
                int draw = 0;
                if (Layer6.Dirty != 0)
                {
                    Layer6.Dirty = 0;
                    draw = 1;
                }

                int eventTimeout = draw != 0 ? 80 : 80;
                if (Sdl3Native.SDL_WaitEventTimeout(out Sdl3Native.SDL_Event ev, eventTimeout))
                {
                    switch (ev.type)
                    {
                        case Sdl3Native.SDL_TEXTINPUT:
                            HandleTextInput(DecodeText(ev.text.text));
                            break;
                        case Sdl3Native.SDL_KEYDOWN:
                            HandleKeyDown(ev.key);
                            break;
                        case Sdl3Native.SDL_QUIT:
                            _quit = false;
                            break;
                    }
                }

                Layer6.UpdateBlinkClock(blinkClock.ElapsedMilliseconds);
                UpdatePixels(pixels, renderWidth, renderHeight);
                Sdl3Native.SDL_UpdateTexture(texture, IntPtr.Zero, pixels, renderWidth * sizeof(uint));

                Sdl3Native.SDL_RenderClear(renderer);
                Sdl3Native.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
                Sdl3Native.SDL_RenderPresent(renderer);
            }
        }
        finally
        {
            Sdl3Native.SDL_StopTextInput(window);
            Sdl3Native.SDL_DestroyTexture(texture);
            Sdl3Native.SDL_DestroyRenderer(renderer);
            Sdl3Native.SDL_DestroyWindow(window);
            Sdl3Native.SDL_Quit();
        }

        return 0;
    }

    private static void DecoderThread(string host, int port)
    {
        try
        {
            Layer2.Connect2(host, port);
            while (_quit)
            {
                Layer6.ProcessBtxData();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Decoder thread stopped: {ex.Message}");
            _quit = false;
        }
    }

    private static void UpdatePixels(uint[]? pixels, int w, int h)
    {
        if (w <= 0 || h <= 0)
        {
            return;
        }

        if (pixels is null)
        {
            return;
        }

        int expected = w * h;
        if (pixels.Length < expected)
        {
            return;
        }

        int sy = Math.Max(1, h / 240);
        int sx = Math.Max(1, w / 480);
        int wx = sx * 480;
        int wy = sy * 240;
        int ox = (w - wx) / 2;
        int oy = (h - wy) / 2;

        byte[]? mem = XFont.MemImage;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int p = y * w + x;
                if ((uint)p >= (uint)pixels.Length)
                {
                    continue;
                }

                int r = 0;
                int g = 0;
                int b = 0;
                if (y < oy || y >= wy + oy)
                {
                    int c = (y - oy) / sy / 10;
                    XFont.GetColumnColour(c, out r, out g, out b);
                }
                else if (x < ox || x >= wx + ox)
                {
                    int c = (y - oy) / sy / 10;
                    XFont.GetColumnColour(c, out r, out g, out b);
                }
                else
                {
                    int miX = Math.Clamp((x - ox) / sx, 0, 479);
                    int miY = Math.Clamp((y - oy) / sy, 0, 239);
                    int miP = (miY * 480 + miX) * 3;
                    if (mem is not null && miP >= 0 && (miP + 2) < mem.Length)
                    {
                        r = mem[miP + 0];
                        g = mem[miP + 1];
                        b = mem[miP + 2];
                    }
                }
                pixels[p] = (uint)((0xFF << 24) | (r << 16) | (g << 8) | b);
            }
        }
    }

    private static string DecodeText(IntPtr utf8)
    {
        if (utf8 == IntPtr.Zero)
        {
            return string.Empty;
        }
        return Marshal.PtrToStringUTF8(utf8) ?? string.Empty;
    }

    private static void HandleTextInput(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return;
        }

        if (s.Length > 31)
        {
            s = s[..31];
        }

        byte[]? output = null;
        foreach (var t in Translations)
        {
            if (t.Mode == _keyboardMode && t.Input == s)
            {
                output = t.Output;
                break;
            }
        }

        _keyboardMode = KmNormal;
        if (output is null)
        {
            output = Encoding.UTF8.GetBytes(s);
        }

        Layer2.Write(output, output.Length);
    }

    private static void SendBytes(params byte[] bytes) => Layer2.Write(bytes, bytes.Length);

    private static void HandleKeyDown(Sdl3Native.SDL_KeyboardEvent key)
    {
        int k = key.key;
        ushort mod = key.mod;
        bool hasShift = (mod & Sdl3Native.KMOD_SHIFT) != 0;
        bool hasAlt = (mod & Sdl3Native.KMOD_ALT) != 0;
        //LogKeyDown(k, mod);

        if (k == Sdl3Native.SDLK_F1) SendBytes(0x13);
        if (k == Sdl3Native.SDLK_F2) SendBytes(0x1C);
        if (k == Sdl3Native.SDLK_F3)
        {
            if (!hasShift && !hasAlt) _keyboardMode = KmFColor;
            if (hasShift) _keyboardMode = KmBColor;
        }
        if (k == Sdl3Native.SDLK_F4) _keyboardMode = KmSize;
        if (k == Sdl3Native.SDLK_F5) _keyboardMode = KmGShift;
        if (k == Sdl3Native.SDLK_F12) SendBytes(0x1A);
        if (k == Sdl3Native.SDLK_LEFT) SendBytes(0x08);
        if (k == Sdl3Native.SDLK_RIGHT) SendBytes(0x09);
        if (k == Sdl3Native.SDLK_UP) SendBytes(0x0B);
        if (k == Sdl3Native.SDLK_DOWN) SendBytes(0x0A);

        if (k == Sdl3Native.SDLK_RETURN || k == Sdl3Native.SDLK_KP_ENTER)
        {
            if (!hasShift && !hasAlt) SendBytes(0x0D, 0x0A);
            if (hasAlt) SendBytes(0x0D);
            if (hasShift) SendBytes(0x18);
        }

        if (k == Sdl3Native.SDLK_HOME)
        {
            if (!hasShift && !hasAlt) SendBytes(0x1E);
            if (hasShift) SendBytes(0x0C);
        }

        if (k == Sdl3Native.SDLK_BACKSPACE) SendBytes(0x08, 0x20, 0x08);
    }

    private static void LogKeyDown(int keyCode, ushort mod)
    {
        try
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} key={GetKeyName(keyCode)} code={keyCode} mod={GetModName(mod)}{Environment.NewLine}";
            lock (_keyLogLock)
            {
                System.IO.File.AppendAllText(_keyLogPath, line);
            }
        }
        catch
        {
            // ignore logging errors to avoid impacting input handling
        }
    }

    private static string GetKeyName(int keyCode)
    {
        IntPtr namePtr = Sdl3Native.SDL_GetKeyName(keyCode);
        if (namePtr == IntPtr.Zero)
        {
            return $"KEY_{keyCode}";
        }

        string? name = Marshal.PtrToStringUTF8(namePtr);
        return string.IsNullOrWhiteSpace(name) ? $"KEY_{keyCode}" : name;
    }

    private static string GetModName(ushort mod)
    {
        if (mod == Sdl3Native.KMOD_NONE)
        {
            return "NONE";
        }

        List<string> parts = new();
        if ((mod & Sdl3Native.KMOD_SHIFT) != 0) parts.Add("SHIFT");
        if ((mod & Sdl3Native.KMOD_ALT) != 0) parts.Add("ALT");

        return parts.Count > 0 ? string.Join('|', parts) : $"0x{mod:X4}";
    }
}
