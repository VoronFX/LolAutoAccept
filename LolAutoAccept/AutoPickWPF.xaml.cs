using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LolAutoAccept.Properties;

namespace LolAutoAccept
{
	/// <summary>
	/// Interaction logic for AutoPickWPF.xaml
	/// </summary>
	public partial class AutoPickWPF : Window
	{
		public AutoPickWPF()
		{
			InitializeComponent();
			try
			{
				this.DataContext = Settings.Default.AutoPickData ?? new AutoPickModel();
			}
			catch (Exception e)
			{
				this.DataContext = new AutoPickModel();
			}
		}

		public string[] Champions { get; }
			= Samples.Champions.Select(x => x.Name).ToArray();

		public static string NoChangeStub { get; } = "No change";
		public static string AnyRoleStub { get; } = "Any";

		public string[] SummonerSpells { get; }
			= new[] { NoChangeStub }.Concat(Samples.SummonerSpells.Select(x => x.Name)).ToArray();

		protected override void OnClosed(EventArgs e)
		{
			Settings.Default.AutoPickData = (AutoPickModel)DataContext;
			Settings.Default.Save();
			base.OnClosed(e);
		}

		private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
		{
			e.NewItem = new PickItem()
			{
				ChampionName =  Champions.First(),
				SummonerSpell1 = NoChangeStub,
				SummonerSpell2 = NoChangeStub,
				Runes = NoChangeStub,
				Masteries = NoChangeStub,
				Role = AnyRoleStub
			};
		}
	}

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

	//LolAutoAccept.AutoPickModel
	[SettingsSerializeAs(SettingsSerializeAs.Xml)]
	public sealed class AutoPickModel
	{
		public bool PostChatMessage { get; set; }
		public string Message { get; set; }

		public bool EnableAutoPick { get; set; }

		public ObservableCollection<PickItem> Picks { get; }
			= new ObservableCollection<PickItem>();

		public bool EnableAutoBan { get; set; }

		public ObservableCollection<BanItem> Bans { get; }
			= new ObservableCollection<BanItem>();
	}

	public class ChampionToIconConverter : IValueConverter
	{
		private static readonly Lazy<(string Name, Bitmap Sample)[]> Champions
			= new Lazy<(string Name, Bitmap Sample)[]>(() => Samples.Champions.ToArray());

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var name = value as string;
			var bitmap = Champions.Value.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase)).Sample;
			if (bitmap == null) return null;

			return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
				bitmap.GetHbitmap(),
				IntPtr.Zero,
				Int32Rect.Empty,
				BitmapSizeOptions.FromEmptyOptions());
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	public class SummonerSpellToIconConverter : IValueConverter
	{
		private static readonly Lazy<(string Name, Bitmap Sample)[]> SummonerSpells
			= new Lazy<(string Name, Bitmap Sample)[]>(() => Samples.SummonerSpells.ToArray());

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var name = value as string;
			var bitmap = SummonerSpells.Value.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase)).Sample;
			if (string.Equals(name, AutoPickWPF.NoChangeStub, StringComparison.InvariantCultureIgnoreCase))
				bitmap = Resources.Symbols_Equals_32xLG;
			if (bitmap == null) return null;

			return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
				bitmap.GetHbitmap(),
				IntPtr.Zero,
				Int32Rect.Empty,
				BitmapSizeOptions.FromEmptyOptions());
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	public class NullVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null ? Visibility.Hidden : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
