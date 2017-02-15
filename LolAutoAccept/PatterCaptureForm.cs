using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LolAutoAccept
{
	public partial class PatterCaptureForm : Form
	{
		public PatterCaptureForm()
		{
			InitializeComponent();
		}

		public void AddBitmap(Bitmap bitmap)
		{
			Bitmaps.Add(new Bitmap(bitmap));
			Invoke((Action)(() => trackBar1.Maximum = Bitmaps.Count - 1));
		}

		private readonly List<Bitmap> Bitmaps = new List<Bitmap>();

		private void trackBar1_ValueChanged(object sender, EventArgs e)
		{
			if (trackBar1.Value < Bitmaps.Count)
				pictureBox1.Image = Bitmaps[trackBar1.Value];
		}

		private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
		{
			toolStripStatusLabel1.Text = $"X: {e.X} Y: {e.Y}";
			var bitmap = pictureBox1.Image as Bitmap;
			if (bitmap != null && e.X < bitmap.Width && e.Y < bitmap.Height)
			{
				toolStripStatusLabel1.Text += $" Color: {bitmap.GetPixel(e.X, e.Y)}";
			}
		}

		private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
		{
			var bitmap = pictureBox1.Image as Bitmap;
			if (bitmap != null && e.X < bitmap.Width && e.Y < bitmap.Height)
			{
				var pixel = bitmap.GetPixel(e.X, e.Y);
				textBox1.AppendText($"new Tuple<Point, Color>(new Point({e.X}, {e.Y}), Color.FromArgb({pixel.R}, {pixel.G}, {pixel.B})),{Environment.NewLine}");
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			var dlg = new OpenFileDialog();
			//dlg.DefaultExt = "*.png";
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				try
				{
					pictureBox1.Load(dlg.FileName);
				}
				catch (Exception exception)
				{
					MessageBox.Show(exception.Message);
				}
			}
		}
	}
}
