using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LolAutoAccept
{
	public partial class SearchMatchingSizeForm : Form
	{
		public SearchMatchingSizeForm()
		{
			InitializeComponent();
		}
		private CancellationTokenSource token;

		private void button1_Click(object sender, EventArgs e)
		{
			LockBitmap.LockBitmap sourceBmp;
			Bitmap searchBmp;
			var dlg = new OpenFileDialog()
			{
				Title = "Select source bitmap"
			};
			//dlg.DefaultExt = "*.png";
			if (dlg.ShowDialog() != DialogResult.OK)
				return;
			try
			{
				sourceBmp = new LockBitmap.LockBitmap(new Bitmap(dlg.FileName));
				sourceBmp.UnlockBits();
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message);
				return;
			}

			dlg = new OpenFileDialog()
			{
				Title = "Select bitmap to search"
			}; ;
			//dlg.DefaultExt = "*.png";
			if (dlg.ShowDialog() != DialogResult.OK)
				return;
			try
			{
				searchBmp = new Bitmap(dlg.FileName);
				if (searchBmp.Width != searchBmp.Height)
					throw new InvalidOperationException("Match bitmap should be square");
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message);
				return;
			}
			int crop;
			if (!int.TryParse(textBox2.Text, out crop))
			{
				MessageBox.Show("Invalid crop value");
				return;
			}
			Task.Run(() =>
			{
				token = new CancellationTokenSource();
				try
				{
					searchBmp = searchBmp.Clone(new Rectangle(crop, crop, searchBmp.Width - crop * 2, searchBmp.Height - crop * 2),
						searchBmp.PixelFormat);
					var searchResults = new List<(int x, int y, int size, double percent, double colorpercent)>();
					var minSize = 20;
					var complete = 0;
					var maxcomplete = 0;
					for (int i = minSize; i < searchBmp.Width; i++)
						maxcomplete += (searchBmp.Width - i) * (searchBmp.Height - i);
					maxcomplete *= searchBmp.Width - minSize;
					Parallel.For(minSize, searchBmp.Width, i =>
					{
						LockBitmap.LockBitmap lockSearchBitmap;
						using (var bitmap = new Bitmap(i, i))
						using (var g = Graphics.FromImage(bitmap))
						{
							g.InterpolationMode = InterpolationMode.NearestNeighbor;
							lock (searchBmp)
							{
								g.DrawImage(searchBmp, 0, 0, bitmap.Width, bitmap.Height);
								//g.DrawImage(searchBmp, new Rectangle(0, 0, i, i), new Rectangle(0, 0, bitmap.Width, bitmap.Height),
								//	GraphicsUnit.Pixel);
								lockSearchBitmap = new LockBitmap.LockBitmap(bitmap);
								lockSearchBitmap.UnlockBits();
							}
						}
						Parallel.For(0, sourceBmp.Width - lockSearchBitmap.Width, x =>
						{
							Invoke((Action)(() => Text = $"{complete / (double)maxcomplete}"));
							Parallel.For(0, sourceBmp.Height - lockSearchBitmap.Height, y =>
							{
								Interlocked.Increment(ref complete);
								token.Token.ThrowIfCancellationRequested();
								double percent = 0;
								double colorpercent = 0;
								for (var x1 = 0; x1 < lockSearchBitmap.Width; x1++)
									for (var y1 = 0; y1 < lockSearchBitmap.Height; y1++)
									{
										var pixelA = sourceBmp.GetPixel(x1 + x, y1 + y);
										var pixelB = lockSearchBitmap.GetPixel(x1, y1);
										percent +=
									((Math.Abs(pixelA.R - pixelB.R)
									  + Math.Abs(pixelA.G - pixelB.G)
									  + Math.Abs(pixelA.B - pixelB.B)));
										colorpercent += Math.Max(Math.Max(Math.Abs(pixelA.R - pixelB.R), Math.Abs(pixelA.G - pixelB.G)),
											Math.Abs(pixelA.B - pixelB.B));
									}
								percent /= 255f * 3 * lockSearchBitmap.Width * lockSearchBitmap.Height;
								colorpercent /= 255f * lockSearchBitmap.Width * lockSearchBitmap.Height;
								lock (searchResults)
									searchResults.Add((x, y, i, percent, colorpercent));
							});
						});
					});
					Invoke((Action)(() =>
					textBox1.Text = string.Join(Environment.NewLine,
						searchResults.OrderBy(x => x.Item4).Select(x => $"x:{x.x} y:{x.y} size:{x.size} percent:{x.percent} colorpercent:{x.colorpercent}"))));
				}
				catch (Exception exception)
				{
					Console.WriteLine(exception);
				}
			});
		}

		LockBitmap.LockBitmap sourceBmp2;
		Bitmap searchBmp2;
		private void button2_Click(object sender, EventArgs e)
		{
			panel1.Visible = true;
			var dlg = new OpenFileDialog()
			{
				Title = "Select source bitmap"
			};
			//dlg.DefaultExt = "*.png";
			if (dlg.ShowDialog() != DialogResult.OK)
				return;
			try
			{
				var b = new Bitmap(dlg.FileName);
				sourceBmp2 = new LockBitmap.LockBitmap(b);
				sourceBmp2.UnlockBits();
				pictureBox1.Image = b;
				trackBar1.Maximum = sourceBmp2.Width;
				trackBar2.Maximum = sourceBmp2.Height;
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message);
				return;
			}

			dlg = new OpenFileDialog()
			{
				Title = "Select bitmap to search"
			}; ;
			//dlg.DefaultExt = "*.png";
			if (dlg.ShowDialog() != DialogResult.OK)
				return;
			try
			{
				searchBmp2 = new Bitmap(dlg.FileName);
				if (searchBmp2.Width != searchBmp2.Height)
					throw new InvalidOperationException("Match bitmap should be square");
				trackBar4.Maximum = searchBmp2.Width;
				trackBar4.Value = searchBmp2.Width;
				trackBar3.Maximum = searchBmp2.Width / 2;
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message);
				return;
			}
			trackBar2_ValueChanged(this, EventArgs.Empty);
		}

		private void trackBar2_ValueChanged(object sender, EventArgs e)
		{
			if (!checkBox1.Checked)
				return;

			int crop = trackBar3.Value;
			var searchBmp = searchBmp2.Clone(new Rectangle(crop, crop, searchBmp2.Width - crop * 2, searchBmp2.Height - crop * 2),
				searchBmp2.PixelFormat);

			LockBitmap.LockBitmap lockSearchBitmap;
			var bitmap = new Bitmap(trackBar4.Value, trackBar4.Value);
			using (var g = Graphics.FromImage(bitmap))
			{
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.DrawImage(searchBmp, 0, 0, bitmap.Width, bitmap.Height);
				//g.DrawImage(searchBmp, 
				//	new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				//	new Rectangle(0, 0, trackBar4.Value, trackBar4.Value),
				//	GraphicsUnit.Pixel);

				lockSearchBitmap = new LockBitmap.LockBitmap(bitmap);
				lockSearchBitmap.UnlockBits();
				pictureBox2.Left = pictureBox1.Left + trackBar1.Value;
				pictureBox2.Top = pictureBox1.Top + trackBar2.Value;
			}
			pictureBox2.Image = bitmap;

			double percent = 0;
			double colorpercent = 0;
			for (var x1 = 0; x1 < lockSearchBitmap.Width; x1++)
				for (var y1 = 0; y1 < lockSearchBitmap.Height; y1++)
				{
					var pixelA = sourceBmp2.GetPixel(x1 + trackBar1.Value, y1 + trackBar2.Value);
					var pixelB = lockSearchBitmap.GetPixel(x1, y1);
					percent +=
					((Math.Abs(pixelA.R - pixelB.R)
					  + Math.Abs(pixelA.G - pixelB.G)
					  + Math.Abs(pixelA.B - pixelB.B)));
					colorpercent += Math.Max(Math.Max(Math.Abs(pixelA.R - pixelB.R), Math.Abs(pixelA.G - pixelB.G)),
						Math.Abs(pixelA.B - pixelB.B));
				}
			percent /= 255f * 3 * lockSearchBitmap.Width * lockSearchBitmap.Height;
			colorpercent /= 255f * lockSearchBitmap.Width * lockSearchBitmap.Height;
			Text = $"x:{trackBar1.Value} y:{trackBar2.Value} size:{trackBar4.Value} crop:{crop} percent:{percent} colorpercent:{colorpercent}";
		}
	}
}
