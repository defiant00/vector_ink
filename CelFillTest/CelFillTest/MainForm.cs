using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CelFillTest
{
	public partial class MainForm : Form
	{
		private Bitmap _image;

		public MainForm()
		{
			InitializeComponent();
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				_image = new Bitmap(openFileDialog.FileName);
				BackgroundImage = _image;
				Width = _image.Width + 20;
				Height = _image.Height + 50;
			}
		}

		private void MainForm_MouseDown(object sender, MouseEventArgs e)
		{
			/* Calculate a "distance" array - so all contiguous clicked on are 0, any of the transition colors add 1 to the count
			 */
			CelFill(e.X, e.Y);
			//DepthFill(e.X, e.Y);

			Invalidate();
		}

		class PointDepth
		{
			public int X, Y, Depth, Previous;
		}

		private void DepthFill(int x_pos, int y_pos)
		{
			int[,] depths = new int[_image.Width, _image.Height];
			for (int x = 0; x < _image.Width; x++)
			{
				for (int y = 0; y < _image.Height; y++) { depths[x, y] = Int32.MaxValue; }
			}

			int c_initial = _image.GetPixel(x_pos, y_pos).ToArgb();
			var points = new Queue<PointDepth>();
			points.Enqueue(new PointDepth { X = x_pos, Y = y_pos, Depth = 0, Previous = c_initial });

			DepthFill(c_initial, points, depths);

			for (int x = 0; x < _image.Width; x++)
			{
				for (int y = 0; y < _image.Height; y++)
				{
					if (depths[x, y] < Int32.MaxValue)
					{
						int v = 255 - 30 * depths[x, y];
						_image.SetPixel(x, y, Color.FromArgb(v, 0, v));
					}
				}
			}
		}

		private void DepthFill(int c_initial, Queue<PointDepth> points, int[,] depths)
		{
			var colors = new HashSet<int> { c_initial, Color.Red.ToArgb(), Color.Blue.ToArgb() };
			while (points.Count > 0)
			{
				var p = points.Dequeue();
				if (p.X > -1 && p.Y > -1 && p.X < _image.Width && p.Y < _image.Height)
				{
					var c = _image.GetPixel(p.X, p.Y).ToArgb();
					if (colors.Contains(c))
					{
						int d = (c == p.Previous) ? p.Depth : p.Depth + 1;
						if (d < depths[p.X, p.Y])
						{
							depths[p.X, p.Y] = d;
							points.Enqueue(new PointDepth { X = p.X - 1, Y = p.Y, Depth = d, Previous = c });
							points.Enqueue(new PointDepth { X = p.X + 1, Y = p.Y, Depth = d, Previous = c });
							points.Enqueue(new PointDepth { X = p.X, Y = p.Y - 1, Depth = d, Previous = c });
							points.Enqueue(new PointDepth { X = p.X, Y = p.Y + 1, Depth = d, Previous = c });
						}
					}
				}
			}
		}

		private void CelFill(int x, int y)
		{
			bool[,] updated = new bool[_image.Width, _image.Height];

			int c_initial = _image.GetPixel(x, y).ToArgb();

			var c_main = Color.FromArgb(255, 147, 187);
			var c_hi = Color.FromArgb(255, 213, 206);
			var c_shadow = Color.FromArgb(165, 49, 134);

			var colors = new Dictionary<int, Color>
			{
				{c_initial, c_main },
				{Color.Red.ToArgb(), c_hi },
				{Color.Blue.ToArgb(), c_shadow },
			};

			var points = new Dictionary<int, Queue<Point>>();
			foreach (int c in colors.Keys) { points[c] = new Queue<Point>(); }
			CelFillQueuePoint(c_initial, c_initial, x, y, points, updated);

			do
			{
				CelFill(c_initial, c_initial, colors, points, updated);
				foreach (int c in colors.Keys) { CelFill(c_initial, c, colors, points, updated); }
			} while (points.Keys.Sum(k => points[k].Count) > 0);

		}

		private void CelFill(int c_initial, int key, Dictionary<int, Color> colors, Dictionary<int, Queue<Point>> points, bool[,] updated)
		{
			var cur_points = points[key];
			while (cur_points.Count > 0)
			{
				var p = cur_points.Dequeue();

				if (!updated[p.X, p.Y])
				{
					var c = _image.GetPixel(p.X, p.Y).ToArgb();
					if (c == c_initial || c == key)
					{
						updated[p.X, p.Y] = true;
						_image.SetPixel(p.X, p.Y, colors[key]);
						CelFillQueuePoint(c_initial, key, p.X - 1, p.Y, points, updated);
						CelFillQueuePoint(c_initial, key, p.X + 1, p.Y, points, updated);
						CelFillQueuePoint(c_initial, key, p.X, p.Y - 1, points, updated);
						CelFillQueuePoint(c_initial, key, p.X, p.Y + 1, points, updated);
					}
				}
			}
		}

		private void CelFillQueuePoint(int c_initial, int key, int x, int y, Dictionary<int, Queue<Point>> points, bool[,] updated)
		{
			if (x > -1 && y > -1 && x < _image.Width && y < _image.Height && !updated[x, y])
			{
				int c = _image.GetPixel(x, y).ToArgb();
				if (c == c_initial) { points[key].Enqueue(new Point(x, y)); }
				else if (points.ContainsKey(c)) { points[c].Enqueue(new Point(x, y)); }
			}
		}
	}
}
