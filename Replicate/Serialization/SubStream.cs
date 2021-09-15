using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.Serialization
{
    public class SubStream : Stream
    {
        private Stream Stream;
        private long Base;
        private long length;
        public SubStream(Stream stream, long length = 0)
        {
            Stream = stream;
            Base = stream.Position;
            this.length = length;
        }

        public override bool CanRead => Stream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => Stream.CanWrite;

        public override long Length => length;

        public override long Position
        {
            get => Stream.Position - Base;
            set => Stream.Position = value + Base;
        }

        public override void Flush() => Stream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = (int)Math.Min(count, length - Position);
            return Stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            length += count;
            Stream.Write(buffer, offset, count);
        }
    }
}
