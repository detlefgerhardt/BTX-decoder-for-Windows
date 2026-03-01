using System.Runtime.InteropServices;
using System.Reflection;

namespace BtxDecoder;

internal static class Sdl2Native
{
    private static bool _resolverInitialized;

    public const uint SDL_INIT_VIDEO = 0x00000020;

    public const uint SDL_QUIT = 0x100;
    public const uint SDL_KEYDOWN = 0x300;
    public const uint SDL_TEXTINPUT = 0x303;
    public const uint SDL_MOUSEMOTION = 0x400;
    public const uint SDL_MOUSEBUTTONDOWN = 0x401;
    public const uint SDL_MOUSEBUTTONUP = 0x402;

    public const int SDL_WINDOWPOS_UNDEFINED = 0x1FFF0000;
    public const uint SDL_PIXELFORMAT_ARGB8888 = 372645892;
    public const int SDL_TEXTUREACCESS_STATIC = 0;

    public const int SDLK_RETURN = 13;
    public const int SDLK_BACKSPACE = 8;
    public const int SDLK_HOME = 1073741898;
    public const int SDLK_LEFT = 1073741904;
    public const int SDLK_RIGHT = 1073741903;
    public const int SDLK_UP = 1073741906;
    public const int SDLK_DOWN = 1073741905;
    public const int SDLK_F1 = 1073741882;
    public const int SDLK_F2 = 1073741883;
    public const int SDLK_F3 = 1073741884;
    public const int SDLK_F4 = 1073741885;
    public const int SDLK_F5 = 1073741886;
    public const int SDLK_F12 = 1073741893;

    public const ushort KMOD_NONE = 0x0000;
    public const ushort KMOD_SHIFT = 0x0003;
    public const ushort KMOD_ALT = 0x0300;

    public static void InitNativeResolver()
    {
        if (_resolverInitialized)
        {
            return;
        }

        NativeLibrary.SetDllImportResolver(
            Assembly.GetExecutingAssembly(),
            static (libraryName, assembly, searchPath) =>
            {
                if (!string.Equals(libraryName, "SDL2", StringComparison.Ordinal))
                {
                    return IntPtr.Zero;
                }

                string baseDir = AppContext.BaseDirectory;
                string[] candidates =
                {
                    Path.Combine(baseDir, "native", "win-x64", "SDL2.dll"),
                    Path.Combine(baseDir, "SDL2.dll"),
                    Path.Combine(baseDir, "native", "libSDL2.so"),
                    Path.Combine(baseDir, "native", "libSDL2-2.0.so.0"),
                    Path.Combine(baseDir, "libSDL2.so"),
                    Path.Combine(baseDir, "libSDL2-2.0.so.0"),
                };

                foreach (string candidate in candidates)
                {
                    if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out IntPtr handle))
                    {
                        return handle;
                    }
                }

                return IntPtr.Zero;
            });

        _resolverInitialized = true;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SDL_Keysym
    {
        public int scancode;
        public int sym;
        public ushort mod;
        public uint unused;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SDL_KeyboardEvent
    {
        public uint type;
        public uint timestamp;
        public uint windowID;
        public byte state;
        public byte repeat;
        public byte padding2;
        public byte padding3;
        public SDL_Keysym keysym;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SDL_TextInputEvent
    {
        public uint type;
        public uint timestamp;
        public uint windowID;
        public byte text0;
        public byte text1;
        public byte text2;
        public byte text3;
        public byte text4;
        public byte text5;
        public byte text6;
        public byte text7;
        public byte text8;
        public byte text9;
        public byte text10;
        public byte text11;
        public byte text12;
        public byte text13;
        public byte text14;
        public byte text15;
        public byte text16;
        public byte text17;
        public byte text18;
        public byte text19;
        public byte text20;
        public byte text21;
        public byte text22;
        public byte text23;
        public byte text24;
        public byte text25;
        public byte text26;
        public byte text27;
        public byte text28;
        public byte text29;
        public byte text30;
        public byte text31;

        public byte[] GetTextBytes()
        {
            return
            [
                text0, text1, text2, text3, text4, text5, text6, text7,
                text8, text9, text10, text11, text12, text13, text14, text15,
                text16, text17, text18, text19, text20, text21, text22, text23,
                text24, text25, text26, text27, text28, text29, text30, text31
            ];
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct SDL_Event
    {
        [FieldOffset(0)]
        public uint type;

        [FieldOffset(0)]
        public SDL_KeyboardEvent key;

        [FieldOffset(0)]
        public SDL_TextInputEvent text;
    }

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_Init(uint flags);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_CreateWindow(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string title,
        int x,
        int y,
        int w,
        int h,
        uint flags);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_CreateRenderer(IntPtr window, int index, uint flags);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_CreateTexture(IntPtr renderer, uint format, int access, int w, int h);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_UpdateTexture(IntPtr texture, IntPtr rect, IntPtr pixels, int pitch);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_RenderClear(IntPtr renderer);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_RenderCopy(IntPtr renderer, IntPtr texture, IntPtr srcrect, IntPtr dstrect);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_RenderPresent(IntPtr renderer);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_WaitEventTimeout(out SDL_Event sdlEvent, int timeout);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_StartTextInput();

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_StopTextInput();

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_DestroyTexture(IntPtr texture);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_DestroyRenderer(IntPtr renderer);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_DestroyWindow(IntPtr window);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_Quit();

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_GetError();

    public static string GetErrorString()
    {
        IntPtr ptr = SDL_GetError();
        return ptr == IntPtr.Zero ? "unknown SDL error" : Marshal.PtrToStringUTF8(ptr) ?? "unknown SDL error";
    }
}
