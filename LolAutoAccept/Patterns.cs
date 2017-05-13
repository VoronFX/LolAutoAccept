using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace LolAutoAccept
{
	public class Patterns
	{
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
		public Lazy<MatchPoint[]> AcceptMatchButtonSample { get; }
		public Lazy<MatchPoint[]> AcceptMatchButtonHoverSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectBanButtonSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectBanButtonHoverSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectBanLockButtonDisabledSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectLockButtonSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectLockButtonHoverSample { get; }

		public int TargetWidth { get; }
		public int TargetHeight { get; }
		public InterpolationMode InterpolationMode { get; }

		public Patterns(int targetWidth, int targetHeight,
			InterpolationMode interpolationMode = InterpolationMode.NearestNeighbor)
		{
			TargetWidth = targetWidth;
			TargetHeight = targetHeight;
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
		}

		private MatchPoint[] PrepareSample(string resourceName)
		{
			var checkPoints = new List<MatchPoint>();
			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
			using (var bitmap = new Bitmap(stream))
			using (var scaledBitmap = Scale(bitmap, TargetWidth, TargetHeight))
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

		private Bitmap Scale(Bitmap source, int targetWidth, int targetHeight)
		{
			if (source.Width == targetWidth && source.Height == targetHeight)
				return source;
			var bitmap = new Bitmap(targetWidth, targetHeight);
			using (var g = Graphics.FromImage(bitmap))
			{
				g.InterpolationMode = InterpolationMode;
				g.DrawImage(source, 0, 0, bitmap.Width, bitmap.Height);
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


			}
			//Console.WriteLine($"colordiff:{(double)colordiff / (sample.Length * 255)} threshold: {threshold} colorTreshold: {colorTreshold} colordiff: {colordiff}");
			return diff <= diffTreshold;
		}

		public enum CompareAlgorithm
		{
			Plain, ColorPriority
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
	}
}