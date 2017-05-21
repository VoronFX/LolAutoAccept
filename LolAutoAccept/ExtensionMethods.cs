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
				(int)Math.Round(rectangle.X * xKoef),
				(int)Math.Round(rectangle.Y * yKoef),
				(int)Math.Round(rectangle.Width * xKoef),
				(int)Math.Round(rectangle.Height * yKoef));


		public static Bitmap Croped(this Bitmap bitmap, int crop) => crop == 0 ? bitmap :
			bitmap.Clone(new Rectangle(crop, crop, bitmap.Width - crop*2, bitmap.Height - crop * 2), bitmap.PixelFormat);
	}
}