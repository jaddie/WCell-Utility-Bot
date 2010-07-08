using System;
using System.IO;
using System.Text;

namespace Squishy.Network
{
	/// <summary>
	/// Summary description for IndexedBuffer.
	/// </summary>
	public class ByteBuffer
	{
		internal byte[] bytes;
		internal int length;
		internal int offset;

		/// <summary>
		/// Creates an empty ByteBuffer with the given size, positioned at zero.
		/// </summary>
		public ByteBuffer(int size) : this(0, size, new byte[size])
		{
		}

		public ByteBuffer(int offset, int length, byte[] bytes)
		{
			if (offset + length > bytes.Length || offset < 0 || length < 0)
				throw new ArgumentException(
					"Offset and length of a ByteBuffer must be positve and not exceed the length of the data array.");
			this.offset = offset;
			this.length = length;
			this.bytes = bytes;
		}

		/// <summary>
		/// The offset position within the internal byte array.
		/// </summary>
		public int Offset
		{
			get { return offset; }
		}

		/// <summary>
		/// The count of buf between offset and the end within the underlying byte array.
		/// </summary>
		public int Length
		{
			get { return length; }
		}

		/// <summary>
		/// The underlying byte array.
		/// </summary>
		public byte[] Data
		{
			get { return bytes; }
		}

		public bool Empty
		{
			get { return length == 0; }
		}

		/// <summary>
		/// Writes <code>length</code> buf from the underlying byte array to the given stream, 
		/// starting at <code>offset</code>.
		/// </summary>
		public void Write(Stream s)
		{
			s.Write(bytes, offset, length);
			s.Flush();
		}

		public void Write(Stream s, long startPos)
		{
			// TODO: deprecate
			s.SetLength(startPos + length);
			s.Seek(startPos, SeekOrigin.Begin);
			s.Write(bytes, offset, length);
			s.Flush();
		}

		public void Write(TextWriter w, Encoding e)
		{
			w.Write(e.GetString(bytes, offset, length));
		}

		public string GetString(Encoding encoding)
		{
			return encoding.GetString(bytes, offset, length);
		}
	}
}