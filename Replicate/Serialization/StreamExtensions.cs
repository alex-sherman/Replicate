using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    public static class StreamExtensions
    {
        public static void WriteInt32(this Stream stream, int value)
        {
            stream.WriteByte((byte)(value & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >> 24) & 0xFF));
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
            stream.Write(bytes, 0, bytes.Length);
        }
        public static string ReadChars(this Stream stream, int number)
        {
            var output = new StringBuilder();
            for (int i = 0; i < number; i++)
            {
                output.Append(stream.ReadChar(out var _));
            }
            return output.ToString();
        }
        public static char ReadCharOne(this Stream stream, bool peak = false)
        {
            var result = stream.ReadChar(out var count);
            if (result == null) throw new EndOfStreamException();
            if (peak) stream.Position -= count;
            return result[0];
        }
        private static char[] ReadChar(this Stream stream, out int readBytes)
        {
            readBytes = 0;
            var first = stream.ReadByte();
            if (first == -1) return null;
            byte[] buffer = new byte[] { (byte)first, 0, 0, 0 };
            var count = 0;
            if ((first & 0x80) != 0)
            {
                if ((first & 0xE0) == 0xC0)
                    count = 1;
                else if ((first & 0xF0) == 0xE0)
                    count = 2;
                else if ((first & 0xF8) == 0xF0)
                    count = 3;
            }
            stream.Read(buffer, 1, count);
            readBytes = (count + 1);
            // TODO: This might cause a lot of garbage collection
            return Encoding.UTF8.GetChars(buffer, 0, count + 1);
        }
        public static string ReadAllString(this Stream stream, Func<char, bool> predicate = null)
        {
            StringBuilder output = new StringBuilder();
            char[] current;
            while ((current = stream.ReadChar(out var count)) != null)
            {
                if (!(predicate?.Invoke(current[0]) ?? true))
                {
                    stream.Position -= count;
                    break;
                }
                output.Append(current);
            }
            return output.ToString();
        }
        public static void WriteNString(this Stream stream, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            stream.WriteInt32(bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }
        public static string ReadNString(this Stream stream)
        {
            int count = stream.ReadInt32();
            byte[] buffer = new byte[count];
            stream.Read(buffer, 0, count);
            return Encoding.UTF8.GetString(buffer);
        }

    }
}
