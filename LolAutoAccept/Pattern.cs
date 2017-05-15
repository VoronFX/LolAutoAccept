using System;
using System.Drawing;
using System.Linq;

namespace LolAutoAccept
{
	public abstract class Pattern
	{
		public bool IsMatch(CachedBitmapPixels image, Point offset, double threshold)
		{
			return Match(image, offset, threshold, true) > threshold;
		}

		public double Match(CachedBitmapPixels image, Point offset)
			=> Match(image, offset, 0, false);

		public abstract double Match(CachedBitmapPixels image, Point offset, double threshold, bool breakEarly);
	}

	public sealed class DifferencePattern : Pattern
	{
		private readonly CachedBitmapPixels sample;

		public DifferencePattern(Bitmap sample)
		{
			this.sample = new CachedBitmapPixels(sample);
		}

		public override double Match(CachedBitmapPixels image, Point offset, double threshold, bool breakEarly)
		{
			long match = 255 * sample.CacheAll().Length;
			long breakEarlyThreshold = (long) (match * threshold);
			for (int x = 0; x < sample.Width; x++)
			for (int y = 0; y < sample.Height; y++)
			{
				var imagePixel = image[x+offset.X, y+offset.Y];
				var samplePixel = image[x, y];

				match -= Math.Abs(imagePixel.R - samplePixel.R)
					  + Math.Abs(imagePixel.G - samplePixel.G)
				      + Math.Abs(imagePixel.B - samplePixel.B);

				if (breakEarly && match < breakEarlyThreshold)
					break;
			}
			return match / (double) sample.CacheAll().Length;
		}
	}

	public sealed class HueSaturationPattern : Pattern
	{
		private readonly Size size;
		private readonly float targetHue;
		private readonly float hueTolerance;

		public HueSaturationPattern(Size size, float targetHue, float hueTolerance)
		{
			this.size = size;
			this.targetHue = targetHue;
			this.hueTolerance = hueTolerance;
		}

		public override double Match(CachedBitmapPixels image, Point offset, double threshold, bool breakEarly)
		{
			double match = 0;
			double breakEarlyThreshold = size.Width * size.Height * threshold;

			for (int x = offset.X; x < size.Width + offset.X; x++)
				for (int y = offset.Y; y < size.Height + offset.Y; y++)
				{
					var pixel = (Color)image[x, y];
					if (Math.Abs(targetHue - pixel.GetHue()) < hueTolerance)
						match += pixel.GetSaturation();

					if (breakEarly && match > breakEarlyThreshold)
						break;
				}
			return match / size.Width / size.Height;
		}
	}

	public sealed class ContrastMaskPattern : Pattern
	{
		private readonly byte lightHomogeneityThreshold;
		private readonly byte darkHomogeneityThreshold;
		private readonly float targetContrast;
		private readonly Point[] whitePoints;
		private readonly Point[] blackPoints;

		public ContrastMaskPattern(Bitmap sample,
			byte lightHomogeneityThreshold, byte darkHomogeneityThreshold, float targetContrast)
		{
			this.lightHomogeneityThreshold = lightHomogeneityThreshold;
			this.darkHomogeneityThreshold = darkHomogeneityThreshold;
			this.targetContrast = targetContrast;
			var cachedBitmapPixels = new CachedBitmapPixels(sample).CacheAll();

			Point[] ExtractPoints(CachedBitmapPixels.Color color)
				=> cachedBitmapPixels
					.Where(x => x == color)
					.Select((x, i) => new Point(i % sample.Width, i / sample.Width))
					.ToArray();

			whitePoints = ExtractPoints(new CachedBitmapPixels.Color(255, 255, 255));
			blackPoints = ExtractPoints(new CachedBitmapPixels.Color(0, 0, 0));

			if (whitePoints.Length + blackPoints.Length != cachedBitmapPixels.Length)
				throw new ArgumentException("Sample should contain only white and black pixels.");
			if (!whitePoints.Any() || !blackPoints.Any())
				throw new ArgumentException("Sample should contain at least one black and one white pixel.");
		}

		public override double Match(CachedBitmapPixels image, Point offset, double threshold, bool breakEarly)
		{
			Color PixelsAvgColor(Point[] points)
				=> points.Aggregate(new int[3], (seed, point) =>
					{
						var pixel = image[point.X + offset.X, point.Y + offset.Y];
						seed[0] += pixel.R;
						seed[1] += pixel.G;
						seed[2] += pixel.B;
						return seed;
					},
					colorSum => Color.FromArgb(
						colorSum[0] / points.Length,
						colorSum[1] / points.Length,
						colorSum[2] / points.Length)
				);

			double HomogeneityLevel(Point[] points, Color colorSample, byte homogeneityThreshold)
				=> points.Count(point =>
				{
					var pixel = image[point.X + offset.X, point.Y + offset.Y];
					return Math.Abs(pixel.R - colorSample.R) < homogeneityThreshold
						   && Math.Abs(pixel.G - colorSample.G) < homogeneityThreshold
						   && Math.Abs(pixel.B - colorSample.B) < homogeneityThreshold;
				}) / (double)points.Length;

			var darkAvg = PixelsAvgColor(blackPoints);
			var lightAvg = PixelsAvgColor(whitePoints);

			double Normalize(double value) => Math.Min(1, Math.Max(0, value));

			var contrastDifference = Normalize(
				(lightAvg.GetBrightness() - darkAvg.GetBrightness()) / targetContrast);

			return HomogeneityLevel(blackPoints, darkAvg, darkHomogeneityThreshold)
				   + HomogeneityLevel(whitePoints, lightAvg, lightHomogeneityThreshold)
				   * contrastDifference / 2;
		}
	}

}