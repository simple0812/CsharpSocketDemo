using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectiveServer.libs.Helper
{
    public static class DirectiveHelper
    {
        public static byte[] ParseNumberTo2Bytes(double number)
        {
            uint data = Convert.ToUInt32(number);

            var totalms = new byte[]
            {
                (byte) ((data & 0xFF00) >> 8),
                (byte) (data & 0xFF)
            };

            return totalms;
        }

        public static byte[] ParseNumberTo4Bytes(double number)
        {
            uint data = Convert.ToUInt32(number);

            var totalms = new byte[]
            {
                (byte)((data>>24) & 0xFF),
                (byte)((data>>16) & 0xFF),
                (byte)((data>>8) & 0xFF),
                (byte)((data>>0) & 0xFF)
            };

            return totalms;
        }

        public static double Parse2BytesToNumber(byte[] bytes)
        {
            if (bytes.Length != 2) return 0;
            return (bytes[0] << 8) + bytes[1];
        }

        public static double Parse4BytesToNumber(byte[] bytes)
        {
            if (bytes.Length != 4) return 0;
            return (bytes[0] << 24) + (bytes[1] << 16) + (bytes[2] << 8) + bytes[3];
        }

        public static DateTime? Parse4BytesToTime(byte[] bytes)
        {
            if (bytes.Length != 4) return null;
            var p = Parse4BytesToNumber(bytes);
            if ((int)p == 0) return null;
            return  new DateTime(1970, 1, 1).AddSeconds(p);
        }

        public static byte[] GenerateCheckCode(byte[] data)
        {
            return GenerateCheckCode(data, (byte)data.Length);
        }

        
        public static byte[] GenerateCheckCode(byte[] dataBuff, byte dataLen)
        {
            byte CRC16High = 0;
            byte CRC16Low = 0;

            int CRCResult = 0xFFFF;
            for (int i = 0; i < dataLen; i++)
            {
                CRCResult = CRCResult ^ dataBuff[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((CRCResult & 1) == 1)
                        CRCResult = (CRCResult >> 1) ^ 0xA001;
                    else
                        CRCResult >>= 1;
                }
            }
            CRC16High = Convert.ToByte(CRCResult & 0xff);
            CRC16Low = Convert.ToByte(CRCResult >> 8);
            //return ((CRCResult >> 8) + ((CRCResult & 0xff) << 8)); 

            return new byte[] { CRC16High , CRC16Low};
        }
    }
}
