using System.Net.Sockets;
using System.Text;

namespace BtxDecoder;

internal static class Layer2
{
    private const int BSize = 1024 * 1024;
    private static readonly byte[] Buffer = new byte[BSize];

    private static TcpClient? _client;
    private static NetworkStream? _stream;
    private static byte _lastChar;
    private static bool _lastCharBuffered;
    private static int _rPointer;
    private static int _wPointer;

    public static void Connect() => Connect2("localhost", 20000);

    public static void Connect2(string host, int port)
    {
        _client = new TcpClient();
        _client.Connect(host, port);
        _stream = _client.GetStream();
    }

    public static int WriteReadBuffer(byte c)
    {
        int next = (_wPointer + 1) % BSize;
        if (next == _rPointer)
        {
            return -1;
        }

        Buffer[_wPointer] = c;
        _wPointer = next;
        return 0;
    }

    public static int Eof() => _wPointer == _rPointer ? 0 : 1;

    public static int Getc()
    {
        if (_lastCharBuffered)
        {
            _lastCharBuffered = false;
            return _lastChar;
        }

        if (_stream is null)
        {
            if (_rPointer == _wPointer)
            {
                return -1;
            }

            _lastChar = Buffer[_rPointer];
            _rPointer = (_rPointer + 1) % BSize;
            return _lastChar;
        }

        int b = _stream.ReadByte();
        if (b < 0)
        {
            Thread.Sleep(100);
            return -1;
        }

        _lastChar = (byte)b;
        return _lastChar;
    }

    public static void Ungetc() => _lastCharBuffered = true;

    public static void Write(byte[] data, int len)
    {
        if (_stream is null)
        {
            return;
        }

        _stream.Write(data, 0, len);
    }

    public static void Write(string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        Write(bytes, bytes.Length);
    }
}
