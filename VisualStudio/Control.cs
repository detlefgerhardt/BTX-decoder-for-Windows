namespace BtxDecoder;

internal static class Control
{
    public const int NUL = 0x00;
    public const int SOH = 0x01;
    public const int STX = 0x02;
    public const int ETX = 0x03;
    public const int EOT = 0x04;
    public const int ENQ = 0x05;
    public const int ACK = 0x06;
    public const int ITB = 0x07;
    public const int APB = 0x08;
    public const int APF = 0x09;
    public const int APD = 0x0A;
    public const int APU = 0x0B;
    public const int CS = 0x0C;
    public const int APR = 0x0D;
    public const int LS1 = 0x0E;
    public const int LS0 = 0x0F;
    public const int DLE = 0x10;
    public const int CON = 0x11;
    public const int RPT = 0x12;
    public const int INI = 0x13;
    public const int COF = 0x14;
    public const int NAK = 0x15;
    public const int SYN = 0x16;
    public const int ETB = 0x17;
    public const int CAN = 0x18;
    public const int SS2 = 0x19;
    public const int DCT = 0x1A;
    public const int ESC = 0x1B;
    public const int TER = 0x1C;
    public const int SS3 = 0x1D;
    public const int APH = 0x1E;
    public const int APA = 0x1F;
    public const int US = 0x1F;

    public const int FSH = 0x88;
    public const int STD = 0x89;
    public const int EBX = 0x8A;
    public const int SBX = 0x8B;
    public const int NSZ = 0x8C;
    public const int DBH = 0x8D;
    public const int DBW = 0x8E;
    public const int DBS = 0x8F;
    public const int CDY = 0x98;
    public const int SPL = 0x99;
    public const int STL = 0x9A;
    public const int CSI = 0x9B;
}
