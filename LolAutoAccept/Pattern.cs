using System;
using System.Collections.Generic;
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
#if DEBUG
		public Bitmap Sample { get; }
#endif
		private readonly (CachedBitmapPixels.Color Color, Point Coord)[] points;

		public DifferencePattern(Bitmap sample)
		{
#if DEBUG
			this.Sample = sample;
#endif
			var cachedBitmap = new CachedBitmapPixels(sample);
			var checkPoints = new List<(CachedBitmapPixels.Color Color, Point Point)>();

			for (int x = 0; x < sample.Width; x++)
				for (int y = 0; y < sample.Height; y++)
				{
					if (cachedBitmap[x, y].A == 0xFF)
						checkPoints.Add((cachedBitmap[x, y], new Point(x, y)));
				}
			points = checkPoints.ToArray();
		}

		public override double Match(CachedBitmapPixels image, Point offset, double threshold, bool breakEarly)
		{
			long match = 255 * 3 * points.Length;
			long breakEarlyThreshold = (long)(match * threshold);
			for (int i = 0; i < points.Length; i++)
			{
				var point = points[i];
				var imagePixel = image[point.Coord.X + offset.X, point.Coord.Y + offset.Y];

				match -= Math.Abs(imagePixel.R - point.Color.R)
						 + Math.Abs(imagePixel.G - point.Color.G)
						 + Math.Abs(imagePixel.B - point.Color.B);

				if (breakEarly && (match < breakEarlyThreshold
					|| match - breakEarlyThreshold > (points.Length - i) * 255 * 3))
					break;
			}

			return match / (double)(255 * 3 * points.Length);
		}
	}

	public sealed class HueSaturationPattern : Pattern
	{
		private readonly Point[] whitePoints;
		private readonly Size size;
		private readonly float targetHue;
		private readonly float hueTolerance;

		public HueSaturationPattern(Bitmap mask, float targetHue, float hueTolerance)
			: this(mask.Size, targetHue, hueTolerance)
		{
			whitePoints = new CachedBitmapPixels(mask).CacheAll()
				.Select((x, i) => (x, new Point(i % mask.Width, i / mask.Width)))
				.Where(x => x.Item1 == new CachedBitmapPixels.Color(255, 255, 255))
				.Select(x => x.Item2)
				.ToArray();
		}

		public HueSaturationPattern(Size size, float targetHue, float hueTolerance)
		{
			this.size = size;
			this.targetHue = targetHue;
			this.hueTolerance = hueTolerance;
		}

		public override double Match(CachedBitmapPixels image, Point offset, double threshold, bool breakEarly)
		{
			double match = 0;

			if (whitePoints != null)
			{
				double breakEarlyThreshold = whitePoints.Length * threshold;

				foreach (Point point in whitePoints)
				{
					var pixel = (Color) image[point.X + offset.X, point.Y + offset.Y];

					if (Math.Abs(targetHue - pixel.GetHue()) < hueTolerance)
						match += pixel.GetSaturation();

					if (breakEarly && match > breakEarlyThreshold)
						break;
				}
				return match / whitePoints.Length;
			}
			else
			{
				double breakEarlyThreshold = size.Width * size.Height * threshold;

				for (int x = offset.X; x < size.Width + offset.X; x++)
				for (int y = offset.Y; y < size.Height + offset.Y; y++)
				{
					var pixel = (Color) image[x, y];
					if (Math.Abs(targetHue - pixel.GetHue()) < hueTolerance)
						match += pixel.GetSaturation();

					if (breakEarly && match > breakEarlyThreshold)
						break;
				}
				return match / size.Width / size.Height;
			}
		}
	}

	public sealed class ContrastMaskPattern : Pattern
	{
#if DEBUG
		public Bitmap Sample { get; }
#endif
		private readonly byte lightHomogeneityThreshold;
		private readonly byte darkHomogeneityThreshold;
		private readonly float targetContrast;
		private readonly Point[] whitePoints;
		private readonly Point[] blackPoints;

		public ContrastMaskPattern(Bitmap sample,
			byte lightHomogeneityThreshold, byte darkHomogeneityThreshold, float targetContrast)
		{
#if DEBUG
			this.Sample = sample;
#endif
			this.lightHomogeneityThreshold = lightHomogeneityThreshold;
			this.darkHomogeneityThreshold = darkHomogeneityThreshold;
			this.targetContrast = targetContrast;
			var cachedBitmapPixels = new CachedBitmapPixels(sample).CacheAll();

			Point[] ExtractPoints(CachedBitmapPixels.Color color)
				=> cachedBitmapPixels
					.Select((x, i) => (x, new Point(i % sample.Width, i / sample.Width)))
					.Where(x => x.Item1 == color)
					.Select(x => x.Item2)
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

			return (HomogeneityLevel(blackPoints, darkAvg, darkHomogeneityThreshold)
				   + HomogeneityLevel(whitePoints, lightAvg, lightHomogeneityThreshold))
				   * contrastDifference / 2;
		}
	}

}