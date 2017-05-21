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
	public partial class AutoPickForm : Form
	{
		private IEnumerable<(string Name, Bitmap Sample)> Champions = Samples.Champions;

		public class BanItem
		{
			public string ChampionName { get; set; }
		}

		public sealed class PickItem : BanItem
		{
			public string Runes { get; set; }
			public string Masteries { get; set; }
			public string SummonerSpell1 { get; set; }
			public string SummonerSpell2 { get; set; }
			public string Role { get; set; }
		}

		private readonly BindingList<PickItem> pickList
			= new BindingList<PickItem>();


		public AutoPickForm()
		{
			InitializeComponent();
			flowLayoutPanel1.Controls.AddRange(Champions.Select(x => new PictureBox() { Image = x.Sample }).ToArray());
			this.ChampionName.Items.AddRange(Champions.Select(x => x.Name).ToArray());
			this.PickList.CurrentCellDirtyStateChanged += (sender, args) =>
			{
				var dg = (DataGridView)sender;
				dg.CommitEdit(DataGridViewDataErrorContexts.Commit);
				if (dg.CurrentCell == null)
					return;
				if (dg.CurrentCell.EditType != typeof(DataGridViewTextBoxEditingControl))
					return;
				dg.BeginEdit(false);
				var textBox = (TextBox)dg.EditingControl;
				textBox.SelectionStart = textBox.Text.Length;
			};
			this.PickList.AutoGenerateColumns = false;
			this.PickList.DataSource = pickList;
			this.PickList.CellValueChanged += (sender, args) =>
			{
				if (args.ColumnIndex == ChampionName.Index)
				{

					((DataGridView)sender).Rows[args.RowIndex].Cells[ChampionIcon.Index].Value
				= Champions.FirstOrDefault(x => x.Name == (string)((DataGridView)sender).Rows[args.RowIndex]
											   .Cells[ChampionName.Index].Value).Sample;
				}
			};
		}
	}
}
