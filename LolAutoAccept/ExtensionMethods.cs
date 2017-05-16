using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace LolAutoAccept
{
	public static class ExtensionMethods
	{
		public static Bitmap Scaled(this Bitmap source, Size targetSize, InterpolationMode interpolationMode)
		{
			if (source.Width == targetSize.Width && source.Height == targetSize.Height)
				return source;
			var bitmap = new Bitmap(targetSize.Width, targetSize.Height);

			using (var g = Graphics.FromImage(bitmap))
			{
				g.InterpolationMode = interpolationMode;
				g.DrawImage(source, 0, 0, bitmap.Width, bitmap.Height);
				return bitmap;
			}
		}

		public static Rectangle Scale(this Rectangle rectangle, double xKoef, double yKoef) =>
			new Rectangle(
				(int)(rectangle.X * xKoef),
				(int)(rectangle.Y * yKoef),
				(int)(rectangle.Width * xKoef),
				(int)(rectangle.Height * yKoef));
	}
}