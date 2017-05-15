using System;
using System.Diagnostics;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LolAutoAccept.Tests
{
	[TestClass()]
	public class CachedBitmapPixelsTests
	{

		[TestMethod()]
		public void CachedBitmapPixelsSpeedTest()
		{
			var stopWatch = new Stopwatch();
			const int mulTimes = 100;
			int dummySum = 0;

			var bitmap = new Bitmap(500, 500);
			stopWatch.Start();
			for (int mul = 0; mul < mulTimes; mul++)
			for (int x = 0; x < bitmap.Width; x++)
			for (int y = 0; y < bitmap.Height; y++)
			{
				var pixel = bitmap.GetPixel(x, y);
				dummySum += pixel.A + pixel.R + pixel.G + pixel.B;
			}
			stopWatch.Stop();

			var bitmapTime = stopWatch.Elapsed;
			using (var lockBitmap = new LockBitmap.LockBitmap(bitmap))
			{
				stopWatch.Restart();
				for (int mul = 0; mul < mulTimes; mul++)
				for (int x = 0; x < lockBitmap.Width; x++)
				for (int y = 0; y < lockBitmap.Height; y++)
				{
					var pixel = lockBitmap.GetPixel(x, y);
					dummySum += pixel.A + pixel.R + pixel.G + pixel.B;
				}
				stopWatch.Stop();
			}
			var lockBitmapTime = stopWatch.Elapsed;

			var cachedBitmap = new CachedBitmapPixels(bitmap);
			stopWatch.Restart();
			for (int mul = 0; mul < mulTimes; mul++)
			for (int x = 0; x < cachedBitmap.Width; x++)
			for (int y = 0; y < cachedBitmap.Height; y++)
			{
				var pixel = cachedBitmap[x, y];
				dummySum += pixel.A + pixel.R + pixel.G + pixel.B;
			}
			stopWatch.Stop();
			var cachedBitmapTime = stopWatch.Elapsed;

			var cachedBitmapArray = new CachedBitmapPixels(bitmap);
			stopWatch.Restart();
			for (int mul = 0; mul < mulTimes; mul++)
			{
				var pixels = cachedBitmapArray.CacheAll();
				for (int x = 0; x < cachedBitmapArray.Width; x++)
				for (int y = 0; y < cachedBitmapArray.Height; y++)
				{
					var pixel = pixels[x + y * cachedBitmapArray.Width];
					dummySum += pixel.A + pixel.R + pixel.G + pixel.B;
				}
			}
			stopWatch.Stop();
			var cachedBitmapArrayTime = stopWatch.Elapsed;
			Console.WriteLine($"Bitmap time {bitmapTime}");
			Console.WriteLine($"LockBitmap time {lockBitmapTime}");
			Console.WriteLine($"CachedBitmapTime time {cachedBitmapTime}");
			Console.WriteLine($"CachedBitmapArrayTime time {cachedBitmapArrayTime}");

			Assert.IsTrue(cachedBitmapTime < bitmapTime && cachedBitmapTime < lockBitmapTime);
			Assert.IsTrue(cachedBitmapArrayTime < bitmapTime && cachedBitmapArrayTime < lockBitmapTime);
		}
	}
}