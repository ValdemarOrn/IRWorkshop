using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ImpulseHd.Ui
{
	public enum LogType
	{
		Information,
		Warning,
		Error,
	}

	public class Logging
	{
		public static void ShowMessage(string message, LogType type)
		{
			MessageBoxImage img = MessageBoxImage.None;
			if (type == LogType.Information) img = MessageBoxImage.Information;
			if (type == LogType.Warning) img = MessageBoxImage.Exclamation;
			if (type == LogType.Error) img = MessageBoxImage.Error;

			Task.Delay(200).ContinueWith(_ => MessageBox.Show(message, type.ToString(), MessageBoxButton.OK, img));
		}
	}
}
