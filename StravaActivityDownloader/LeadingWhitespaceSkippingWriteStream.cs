using System;
using System.IO;

namespace StravaExporter
{
    public class LeadingWhitespaceSkippingWriteStream : Stream
    {
        private Stream stream;
        private bool skipping = true;
        public LeadingWhitespaceSkippingWriteStream(Stream s) { stream = s; }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => stream.Length;
        public override long Position { get => stream.Position; set => throw new InvalidOperationException(); }
        public override void Flush() { stream.Flush(); }
        public override long Seek(long offset, SeekOrigin origin) { throw new InvalidOperationException(); }
        public override void SetLength(long value) { stream.SetLength(value); }
        public override int Read(byte[] buffer, int offset, int count) { throw new InvalidOperationException(); }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (skipping)
            {
                int idx = FindFirstNonWhitespace(buffer, offset, count);
                if (idx < 0)
                    return; // all whitespace, we are still in skipping mode

                // else write eveything from buffer[idx] onwards
                stream.Write(buffer, idx, count - idx + offset);
                skipping = false;
                return;
            }
            stream.Write(buffer, offset, count);
        }
        private int FindFirstNonWhitespace(byte[] buffer, int offset, int count)
        {
            int i;
            for (i = offset; i < offset + count; ++i)
            {
                if (!char.IsWhiteSpace((char)buffer[i]))
                    return i;
            }
            return -1;
        }
    }
}
