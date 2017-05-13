using System.Drawing;
using System.Linq;

namespace LolAutoAccept
{
	public static class ExtensionMethods
	{
		public static Bitmap RecreateBitmap(this LockBitmap.LockBitmap lockBitmap)
		{
			var bitmap = new Bitmap(lockBitmap.Width, lockBitmap.Height);
			for (int x = 0; x < lockBitmap.Width; x++)
			for (int y = 0; y < lockBitmap.Height; y++)
			{
				bitmap.SetPixel(x, y, lockBitmap.GetPixel(x, y));
			}
			return bitmap;
		}

		public static Bitmap RecreateBitmap(this Patterns.MatchPoint[] points)
		{
			var rect = Rectangle.FromLTRB(
				points.Min(p => p.X), points.Min(p => p.Y),
				points.Max(p => p.X), points.Max(p => p.Y));
			var bitmap = new Bitmap(rect.Width+1, rect.Height+1);
			foreach (Patterns.MatchPoint p in points)
			{
				bitmap.SetPixel(p.X - rect.Left, p.Y - rect.Top, p.Color);
			}
			return bitmap;
		}


		//#if DEBUG
		//		/// <summary>
		//		/// For debugging purpose only
		//		/// </summary>
		//		/// <returns></returns>
		//		public Bitmap RecreateBitmap()
		//		{
		//			var bitmap = new Bitmap(Width, Height);
		//			for (int x = 0; x < Width; x++)
		//			for (int y = 0; y < Height; y++)
		//			{
		//				bitmap.SetPixel(x, y, GetPixel(x, y));
		//			}
		//			return bitmap;
		//		}
		//#endif


//		public class MatchSample
//		{
//			private readonly MatchPoint[] points;

//			public MatchSample(MatchPoint[] points)
//			{
//				this.points = points;
//			}

//			public MatchPoint this[int index] => points[index];

//#if DEBUG
//			/// <summary>
//			/// For debugging purpose only
//			/// </summary>
//			/// <returns></returns>
//			public Bitmap RecreateBitmap()
//			{
//				var rect = Rectangle.FromLTRB(
//					points.Min(p => p.X), points.Min(p => p.Y),
//					points.Max(p => p.X), points.Max(p => p.Y));
//				var bitmap = new Bitmap(rect.Width, rect.Height);
//				foreach (MatchPoint p in points)
//				{
//					bitmap.SetPixel(p.X - rect.Left, p.Y - rect.Top, p.Color);
//				}
//				return bitmap;
//			}
//#endif
//		}

	}
}