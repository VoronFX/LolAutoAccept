using System;
using System.Drawing;
using System.Linq;

namespace LolAutoAccept
{
	public sealed class CachedBitmapPixels
	{
#if DEBUG
		public Bitmap Bitmap { get; }
#endif
		private Color?[] cachedPixels;
		private byte[] rawPixels;
		private Color[] pixels;

		/// <summary>
		/// Caches all pixels into array and uses this array for all other time.
		/// This method is not thread safe. Call it at least once before performing multithreaded operations.
		/// </summary>
		public Color[] CacheAll()
		{
			if (pixels != null)
				return pixels;
			pixels = Enumerable
				.Range(0, Width * Height)
				.Select(index => GetPixel(index))
				.ToArray();
			rawPixels = null;
			cachedPixels = null;
			return pixels;
		}

		public int Depth { get; }
		public int Width { get; }
		public int Height { get; }

		public CachedBitmapPixels(Bitmap bitmap)
		{
#if DEBUG
			Bitmap = bitmap;
#endif
			using (var lockBitmap = new LockBitmap.LockBitmap(bitmap))
			{
				Width = lockBitmap.Width;
				Height = lockBitmap.Height;
				Depth = lockBitmap.Depth;
				rawPixels = lockBitmap.Pixels;
			}
			this.cachedPixels = new Color?[Width * Height];
		}

		public Color this[int x, int y] => GetPixel(x, y);
		public Color this[int index] => GetPixel(index);

		private Color GetPixel(int index)
		{
			if (pixels != null)
				return pixels[index];

			var cached = cachedPixels[index];
			if (cached.HasValue)
				return cached.Value;

			// Get color components count
			var cCount = Depth / 8;
			var rawIndex = index * cCount;

			if (rawIndex > rawPixels.Length - cCount)
				throw new IndexOutOfRangeException();

			if (Depth == 32) // For 32 BPP get Red, Green, Blue and Alpha
			{
				var b = rawPixels[rawIndex];
				var g = rawPixels[rawIndex + 1];
				var r = rawPixels[rawIndex + 2];
				var a = rawPixels[rawIndex + 3]; // a
				cached = new Color(a, r, g, b);
			}

			if (Depth == 24) // For 24 BPP get Red, Green and Blue
			{
				var b = rawPixels[rawIndex];
				var g = rawPixels[rawIndex + 1];
				var r = rawPixels[rawIndex + 2];
				cached = new Color(r, g, b);
			}

			// For 8 BPP get color value (Red, Green and Blue values are the same)
			if (Depth == 8)
			{
				var c = rawPixels[rawIndex];
				cached = new Color(c, c, c);
			}

			if (cached.HasValue)
			{
				cachedPixels[index] = cached.Value;
				return cached.Value;
			}
			throw new NotSupportedException($"Depth {Depth} not supported");
		}

		private Color GetPixel(int x, int y)
		{
			// Get start index of the specified pixel
			var index = ((y * Width) + x);

			return GetPixel(index);
		}

		public struct Color
		{
			public readonly byte A;
			public readonly byte R;
			public readonly byte G;
			public readonly byte B;

			public Color(byte a, byte r, byte g, byte b)
			{
				A = a;
				R = r;
				G = g;
				B = b;
			}

			public Color(byte r, byte g, byte b)
				: this(255, r, g, b) { }

			public static bool operator ==(Color a, Color b)
				=> a.R == b.R && a.G == b.G && a.B == b.B && a.R == b.R;

			public static bool operator !=(Color a, Color b)
				=> !(a == b);

			public static bool operator ==(System.Drawing.Color a, Color b)
				=> a.R == b.R && a.G == b.G && a.B == b.B && a.R == b.R;

			public static bool operator !=(System.Drawing.Color a, Color b)
				=> !(a == b);

			public static explicit operator System.Drawing.Color(Color c)
				=> System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);

			public static explicit operator Color(System.Drawing.Color c)
				=> new Color(c.A, c.R, c.G, c.B);

			public bool Equals(Color other)
				=> A == other.A && R == other.R && G == other.G && B == other.B;

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return obj is Color && Equals((Color)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					int hashCode = A.GetHashCode();
					hashCode = (hashCode * 397) ^ R.GetHashCode();
					hashCode = (hashCode * 397) ^ G.GetHashCode();
					hashCode = (hashCode * 397) ^ B.GetHashCode();
					return hashCode;
				}
			}
		}
	}
}