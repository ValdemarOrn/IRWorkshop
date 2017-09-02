using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using Image = System.Windows.Controls.Image;

namespace ImpulseHd.Ui
{
	/// <summary>
	/// Interaction logic for ExceptionDialog.xaml
	/// </summary>
	public partial class ExceptionDialog : Window
	{
		public ExceptionDialog()
		{
			InitializeComponent();
		}

		public ExceptionDialog(string message, string stacktrace)
		{
			ErrorMessage = message;
			Stacktrace = stacktrace;
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		public static void ShowDialog(string message, string stacktrace)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				var d = new ExceptionDialog(message, stacktrace);
				d.Owner = Application.Current.MainWindow;
				d.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				d.Topmost = true;
				d.ShowDialog();
			});
		}

		public string Stacktrace { get; set; }

		public string ErrorMessage { get; set; }
	}
}
