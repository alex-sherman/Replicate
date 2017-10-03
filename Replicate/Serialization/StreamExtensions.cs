using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    static class StreamExtensions
    {
        public static void WriteInt32(this Stream stream, int value)
        {
            stream.WriteByte((byte)(value & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >> 24) & 0xFF));
            //stream.Write(BitConverter.GetBytes(value), 0, 4);
        }
        public static int ReadInt32(this Stream stream)
        {
            byte[] buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }
        public static void WriteSingle(this Stream stream, float value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, 4);
        }
        public static float ReadSingle(this Stream stream)
        {
            byte[] buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            return BitConverter.ToSingle(buffer, 0);
        }
        public static void WriteString(this Stream stream, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            stream.WriteInt32(bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }
        public static string ReadString(this Stream stream)
        {
            int count = stream.ReadInt32();
            byte[] buffer = new byte[count];
            stream.Read(buffer, 0, count);
            return Encoding.UTF8.GetString(buffer);
        }

    }
}
