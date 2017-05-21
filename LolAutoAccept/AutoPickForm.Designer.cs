namespace LolAutoAccept
{
	partial class AutoPickForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.checkBox1 = new System.Windows.Forms.CheckBox();
			this.dataGridViewImageColumn1 = new System.Windows.Forms.DataGridViewImageColumn();
			this.dataGridViewComboBoxColumn1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.dataGridViewComboBoxColumn2 = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.dataGridViewComboBoxColumn3 = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.dataGridViewComboBoxColumn4 = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.dataGridViewComboBoxColumn5 = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.dataGridViewButtonColumn1 = new System.Windows.Forms.DataGridViewButtonColumn();
			this.dataGridViewButtonColumn2 = new System.Windows.Forms.DataGridViewButtonColumn();
			this.dataGridViewButtonColumn3 = new System.Windows.Forms.DataGridViewButtonColumn();
			this.PickList = new System.Windows.Forms.DataGridView();
			this.ChampionIcon = new System.Windows.Forms.DataGridViewImageColumn();
			this.ChampionName = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.Runes = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.Masteries = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.SummonerSpell1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.SummonerSpell2 = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.UpButton = new System.Windows.Forms.DataGridViewButtonColumn();
			this.DownButton = new System.Windows.Forms.DataGridViewButtonColumn();
			this.DeleteButton = new System.Windows.Forms.DataGridViewButtonColumn();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			((System.ComponentModel.ISupportInitialize)(this.PickList)).BeginInit();
			this.SuspendLayout();
			// 
			// textBox1
			// 
			this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBox1.Location = new System.Drawing.Point(12, 35);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(694, 20);
			this.textBox1.TabIndex = 0;
			// 
			// checkBox1
			// 
			this.checkBox1.AutoSize = true;
			this.checkBox1.Location = new System.Drawing.Point(12, 12);
			this.checkBox1.Name = "checkBox1";
			this.checkBox1.Size = new System.Drawing.Size(127, 17);
			this.checkBox1.TabIndex = 1;
			this.checkBox1.Text = "Post message in chat";
			this.checkBox1.UseVisualStyleBackColor = true;
			// 
			// dataGridViewImageColumn1
			// 
			this.dataGridViewImageColumn1.HeaderText = "ChampionIcon";
			this.dataGridViewImageColumn1.Name = "dataGridViewImageColumn1";
			// 
			// dataGridViewComboBoxColumn1
			// 
			this.dataGridViewComboBoxColumn1.HeaderText = "ChampionName";
			this.dataGridViewComboBoxColumn1.Name = "dataGridViewComboBoxColumn1";
			// 
			// dataGridViewComboBoxColumn2
			// 
			this.dataGridViewComboBoxColumn2.HeaderText = "Runes";
			this.dataGridViewComboBoxColumn2.Name = "dataGridViewComboBoxColumn2";
			// 
			// dataGridViewComboBoxColumn3
			// 
			this.dataGridViewComboBoxColumn3.HeaderText = "Masteries";
			this.dataGridViewComboBoxColumn3.Name = "dataGridViewComboBoxColumn3";
			// 
			// dataGridViewComboBoxColumn4
			// 
			this.dataGridViewComboBoxColumn4.HeaderText = "SummonerSpell1";
			this.dataGridViewComboBoxColumn4.Name = "dataGridViewComboBoxColumn4";
			// 
			// dataGridViewComboBoxColumn5
			// 
			this.dataGridViewComboBoxColumn5.HeaderText = "SummonerSpell2";
			this.dataGridViewComboBoxColumn5.Name = "dataGridViewComboBoxColumn5";
			// 
			// dataGridViewButtonColumn1
			// 
			this.dataGridViewButtonColumn1.HeaderText = "";
			this.dataGridViewButtonColumn1.Name = "dataGridViewButtonColumn1";
			this.dataGridViewButtonColumn1.Text = "Up";
			this.dataGridViewButtonColumn1.UseColumnTextForButtonValue = true;
			// 
			// dataGridViewButtonColumn2
			// 
			this.dataGridViewButtonColumn2.HeaderText = "DownButton";
			this.dataGridViewButtonColumn2.Name = "dataGridViewButtonColumn2";
			// 
			// dataGridViewButtonColumn3
			// 
			this.dataGridViewButtonColumn3.HeaderText = "DeleteButton";
			this.dataGridViewButtonColumn3.Name = "dataGridViewButtonColumn3";
			// 
			// PickList
			// 
			this.PickList.AllowUserToResizeRows = false;
			this.PickList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.PickList.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.PickList.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.PickList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.PickList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ChampionIcon,
            this.ChampionName,
            this.Runes,
            this.Masteries,
            this.SummonerSpell1,
            this.SummonerSpell2,
            this.UpButton,
            this.DownButton,
            this.DeleteButton});
			this.PickList.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this.PickList.Location = new System.Drawing.Point(12, 211);
			this.PickList.Name = "PickList";
			this.PickList.RowHeadersVisible = false;
			this.PickList.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.PickList.Size = new System.Drawing.Size(694, 186);
			this.PickList.TabIndex = 3;
			// 
			// ChampionIcon
			// 
			this.ChampionIcon.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.ChampionIcon.HeaderText = "ChampionIcon";
			this.ChampionIcon.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
			this.ChampionIcon.MinimumWidth = 32;
			this.ChampionIcon.Name = "ChampionIcon";
			this.ChampionIcon.Width = 32;
			// 
			// ChampionName
			// 
			this.ChampionName.HeaderText = "ChampionName";
			this.ChampionName.Name = "ChampionName";
			// 
			// Runes
			// 
			this.Runes.HeaderText = "Runes";
			this.Runes.Name = "Runes";
			// 
			// Masteries
			// 
			this.Masteries.HeaderText = "Masteries";
			this.Masteries.Name = "Masteries";
			// 
			// SummonerSpell1
			// 
			this.SummonerSpell1.HeaderText = "SummonerSpell1";
			this.SummonerSpell1.Name = "SummonerSpell1";
			// 
			// SummonerSpell2
			// 
			this.SummonerSpell2.HeaderText = "SummonerSpell2";
			this.SummonerSpell2.Name = "SummonerSpell2";
			// 
			// UpButton
			// 
			this.UpButton.HeaderText = "Up";
			this.UpButton.Name = "UpButton";
			this.UpButton.Text = "Up";
			this.UpButton.UseColumnTextForButtonValue = true;
			// 
			// DownButton
			// 
			this.DownButton.HeaderText = "Down";
			this.DownButton.Name = "DownButton";
			this.DownButton.Text = "Down";
			this.DownButton.UseColumnTextForButtonValue = true;
			// 
			// DeleteButton
			// 
			this.DeleteButton.HeaderText = "Delete";
			this.DeleteButton.Name = "DeleteButton";
			this.DeleteButton.Text = "Delete";
			this.DeleteButton.UseColumnTextForButtonValue = true;
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(27, 82);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(667, 31);
			this.flowLayoutPanel1.TabIndex = 4;
			this.flowLayoutPanel1.WrapContents = false;
			// 
			// AutoPickForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(718, 409);
			this.Controls.Add(this.flowLayoutPanel1);
			this.Controls.Add(this.PickList);
			this.Controls.Add(this.checkBox1);
			this.Controls.Add(this.textBox1);
			this.Name = "AutoPickForm";
			this.Text = "AutoPickForm";
			((System.ComponentModel.ISupportInitialize)(this.PickList)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.CheckBox checkBox1;
		private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn1;
		private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn1;
		private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn2;
		private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn3;
		private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn4;
		private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumn5;
		private System.Windows.Forms.DataGridViewButtonColumn dataGridViewButtonColumn1;
		private System.Windows.Forms.DataGridViewButtonColumn dataGridViewButtonColumn2;
		private System.Windows.Forms.DataGridViewButtonColumn dataGridViewButtonColumn3;
		private System.Windows.Forms.DataGridView PickList;
		private System.Windows.Forms.DataGridViewImageColumn ChampionIcon;
		private System.Windows.Forms.DataGridViewComboBoxColumn ChampionName;
		private System.Windows.Forms.DataGridViewComboBoxColumn Runes;
		private System.Windows.Forms.DataGridViewComboBoxColumn Masteries;
		private System.Windows.Forms.DataGridViewComboBoxColumn SummonerSpell1;
		private System.Windows.Forms.DataGridViewComboBoxColumn SummonerSpell2;
		private System.Windows.Forms.DataGridViewButtonColumn UpButton;
		private System.Windows.Forms.DataGridViewButtonColumn DownButton;
		private System.Windows.Forms.DataGridViewButtonColumn DeleteButton;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
	}
}