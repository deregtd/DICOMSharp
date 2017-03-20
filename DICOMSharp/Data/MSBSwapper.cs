
namespace DICOMSharp.Data
{
    internal class MSBSwapper
    {
        private MSBSwapper() { }

        public static ulong SwapL(ulong lng)
        {
            return (ulong)(
                ((lng & 0xFF) << 56) |
                ((lng & 0xFF00) << 40) |
                ((lng & 0xFF0000) << 24) |
                ((lng & 0xFF000000) << 8) |
                ((lng & 0xFF00000000) >> 8) |
                ((lng & 0xFF0000000000) >> 24) |
                ((lng & 0xFF000000000000) >> 40) |
                ((lng & 0xFF00000000000000) >> 56)
                );
        }

        public static long SwapL(long lng)
        {
            return (long) SwapL((ulong) lng);
        }

        public static int SwapDW(int dword)
        {
            return (int)(
                (((uint)dword & 0xFF) << 24) |
                (((uint)dword & 0xFF00) << 8) |
                (((uint)dword & 0xFF0000) >> 8) |
                (((uint)dword & 0xFF000000) >> 24)
                );
        }

        public static uint SwapDW(uint dword)
        {
            return (uint)(
                ((dword & 0xFF) << 24) |
                ((dword & 0xFF00) << 8) |
                ((dword & 0xFF0000) >> 8) |
                ((dword & 0xFF000000) >> 24)
                );
        }

        public static short SwapW(short word)
        {
            //faster way to do this?
            return (short)(((word & 0xFF) << 8) | ((word & 0xFF00) >> 8));
        }

        public static ushort SwapW(ushort word)
        {
            //faster way to do this?
            return (ushort)(((word & 0xFF) << 8) | ((word & 0xFF00) >> 8));
        }
    }
}
