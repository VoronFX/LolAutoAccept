using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;

namespace LolAutoAccept
{
	public class Patterns
	{
		public static Size[] SupportedResolutions { get; }
			= { new Size(1024, 576), new Size(1280, 720), new Size(1600, 900) };

		public static Size NativeResolution { get; }
			= new Size(1280, 720);

		public class MatchPoint
		{
			public readonly Color Color;
			public readonly int X;
			public readonly int Y;

			public MatchPoint(Color color, int x, int y)
			{
				this.Color = color;
				this.X = x;
				this.Y = y;
			}
		}

		private Rectangle[] BanRects { get; }
		private Rectangle[] SummonerNameRects { get; }
		private Rectangle[] AllieSummonerPickRects { get; }
		private Rectangle[] EnemySummonerPickRects { get; }

		public Lazy<MatchPoint[]> AcceptMatchButtonSample { get; }
		public Lazy<MatchPoint[]> AcceptMatchButtonHoverSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectBanButtonSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectBanButtonHoverSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectBanLockButtonDisabledSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectLockButtonSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectLockButtonHoverSample { get; }

		public Size Resolution { get; }
		public InterpolationMode InterpolationMode { get; }

		public Patterns(Size resolution, InterpolationMode interpolationMode = InterpolationMode.NearestNeighbor)
		{
			if (!SupportedResolutions.Contains(resolution))
				throw new ArgumentException($"Resoultion {resolution} is not supported");

			Resolution = resolution;
			InterpolationMode = interpolationMode;

			var samplesNamespace = $"{nameof(LolAutoAccept)}.Samples.";
			AcceptMatchButtonSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace + "AcceptMatchButton.png"), false);
			AcceptMatchButtonHoverSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace + "AcceptMatchButtonHover.png"), false);
			ChampionSelectSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace + "ChampionSelect.png"), false);
			ChampionSelectBanButtonSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace + "ChampionSelectBanButton.png"), false);
			ChampionSelectBanButtonHoverSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace + "ChampionSelectBanButtonHover.png"), false);
			ChampionSelectBanLockButtonDisabledSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace + "ChampionSelectBanLockButtonDisabled.png"), false);
			ChampionSelectLockButtonSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace + "ChampionSelectLockButton.png"), false);
			ChampionSelectLockButtonHoverSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace + "ChampionSelectLockButtonHover.png"), false);

			BanRects = Enumerable.Range(0, 6)
				.Select(i => Scale(new Rectangle(200 + 38 * i + (i < 3 ? 0 : 661), 10, 30, 30))).ToArray();
			SummonerNameRects = Enumerable.Range(0, 4)
				.Select(i => Scale(new Rectangle(128, 126 + i * 80, 6, 22))).ToArray();
			AllieSummonerPickRects = Enumerable.Range(0, 4)
				.Select(i => Scale(new Rectangle(45, 104 + i * 80, 62, 62))).ToArray();
			EnemySummonerPickRects = Enumerable.Range(0, 4)
				.Select(i => Scale(new Rectangle(1172, 104 + i * 80, 62, 62))).ToArray();
		}

		private MatchPoint[] PrepareSample(string resourceName)
		{
			var checkPoints = new List<MatchPoint>();
			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
			using (var bitmap = new Bitmap(stream))
			using (var scaledBitmap = Scale(bitmap, Resolution))
			using (var lockBitmap = new LockBitmap.LockBitmap(scaledBitmap))
			{
				for (int x = 0; x < lockBitmap.Width; x++)
					for (int y = 0; y < lockBitmap.Height; y++)
					{
						var color = lockBitmap.GetPixel(x, y);
						if (color.A == 0xff)
						{
							checkPoints.Add(new MatchPoint(color, x, y));
						}
					}
				return checkPoints.ToArray();
			}
		}

		private Rectangle Scale(Rectangle rectangle)
		{
			if (Resolution == NativeResolution)
				return rectangle;

			var xK = Resolution.Width / (double) NativeResolution.Width;
			var yK = Resolution.Height / (double) NativeResolution.Height;
			return new Rectangle((int) (rectangle.X * xK), (int) (rectangle.Y * yK),
				(int) (rectangle.Width * xK), (int) (rectangle.Height * yK));
		}

		private Bitmap Scale(Bitmap source, Size targetSize)
		{
			if (source.Width == targetSize.Width && source.Height == targetSize.Height)
				return source;
			var bitmap = new Bitmap(targetSize.Width, targetSize.Height);
			using (var g = Graphics.FromImage(bitmap))
			{
				g.InterpolationMode = InterpolationMode;
				g.DrawImage(source, 0, 0, Resolution.Width, bitmap.Height);
				return bitmap;
			}
		}

		private const double BaseTreshold = 0.06835;

		public bool IsAcceptMatchButton(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, AcceptMatchButtonSample.Value, BaseTreshold)
			   || IsMatch(screenshot, AcceptMatchButtonHoverSample.Value, BaseTreshold);

		public bool IsChampionSelect(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, ChampionSelectSample.Value, BaseTreshold);

		public bool HasBanLockButtonDisabled(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, ChampionSelectBanLockButtonDisabledSample.Value, BaseTreshold);

		public bool IsBanButton(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, ChampionSelectBanButtonSample.Value, BaseTreshold)
			   || IsMatch(screenshot, ChampionSelectBanButtonHoverSample.Value, BaseTreshold);

		public bool IsLockButton(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, ChampionSelectLockButtonSample.Value, BaseTreshold)
			   || IsMatch(screenshot, ChampionSelectLockButtonHoverSample.Value, BaseTreshold);

		private static bool IsMatch(LockBitmap.LockBitmap screenshot, MatchPoint[] sample, double threshold)
		{
			int diff = 0;
			int diffTreshold = (int)(sample.Length * 255 * 3 * threshold);
			//int colordiff = 0;
			//int colorTreshold = (int)(sample.Length * 255 * threshold);
			foreach (var pixel in sample)
			{
				var targetPixel = screenshot.GetPixel(pixel.X, pixel.Y);

				diff += Math.Abs(targetPixel.R - pixel.Color.R)
						+ Math.Abs(targetPixel.G - pixel.Color.G)
						+ Math.Abs(targetPixel.B - pixel.Color.B);

				//colordiff += Math.Max(Math.Max(
				//		Math.Abs(targetPixel.R - pixel.Color.R), 
				//		Math.Abs(targetPixel.G - pixel.Color.G)),
				//		Math.Abs(targetPixel.B - pixel.Color.B));

				if (diff > diffTreshold)
					return false;
			}
			//Console.WriteLine($"colordiff:{(double)colordiff / (sample.Length * 255)} threshold: {threshold} colorTreshold: {colorTreshold} colordiff: {colordiff}");
			return true;
		}

		public enum CompareAlgorithm
		{
			Plain,
			ColorPriority
		}

		public static double IsMatchTest(LockBitmap.LockBitmap screenshot, MatchPoint[] sample, CompareAlgorithm algorithm)
		{
			int diff = 0;
			int colordiff = 0;
			foreach (var pixel in sample)
			{
				var targetPixel = screenshot.GetPixel(pixel.X, pixel.Y);

				diff += Math.Abs(targetPixel.R - pixel.Color.R)
						+ Math.Abs(targetPixel.G - pixel.Color.G)
						+ Math.Abs(targetPixel.B - pixel.Color.B);


				colordiff += Math.Max(Math.Max(
						Math.Abs(targetPixel.R - pixel.Color.R),
						Math.Abs(targetPixel.G - pixel.Color.G)),
						Math.Abs(targetPixel.B - pixel.Color.B));
			}

			switch (algorithm)
			{
				case CompareAlgorithm.Plain:
					return (double)diff / (sample.Length * 255 * 3);
				case CompareAlgorithm.ColorPriority:
					return (double)colordiff / (sample.Length * 255);
				default:
					throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
			}
		}

		public int DetectOurPickPosition(LockBitmap.LockBitmap screenshot) =>
			SummonerNameRects.Select(rectangle =>
			{
				int match = 0;
				for (int x = rectangle.Left; x < rectangle.Right; x++)
					for (int y = rectangle.Top; y < rectangle.Bottom; y++)
					{
						var pixel = screenshot.GetPixel(x, y);
						if (pixel.R < 150 || pixel.G < 100 || pixel.B > 50)
							continue;

						match += pixel.R + pixel.G - pixel.B;
					}
				return match;
			})
			.Select((x, i) => (x, i))
			.Aggregate((int.MinValue, 0), (seed, x) => x.Item1 > seed.Item1 ? x : seed).Item2;

	}
}