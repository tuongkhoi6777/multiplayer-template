using System.Text;

namespace FPS
{
    public static class CRC32
    {
        private static readonly uint[] Crc32Table;

        static CRC32()
        {
            Crc32Table = new uint[256];
            const uint polynomial = 0xedb88320; // Polynomial used for CRC32

            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (uint j = 8; j > 0; j--)
                {
                    if ((crc & 1) == 1)
                        crc = (crc >> 1) ^ polynomial;
                    else
                        crc >>= 1;
                }
                Crc32Table[i] = crc;
            }
        }

        public static uint CalculateCRC(string input)
        {
            return CalculateCRC(Encoding.UTF8.GetBytes(input));
        }

        public static uint CalculateCRC(byte[] input)
        {
            uint crc = 0xffffffff;
            foreach (byte b in input)
            {
                byte tableIndex = (byte)(((crc) & 0xff) ^ b);
                crc = (crc >> 8) ^ Crc32Table[tableIndex];
            }
            return ~crc;
        }
    }
}