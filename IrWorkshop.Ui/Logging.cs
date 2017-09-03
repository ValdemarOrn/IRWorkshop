using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LowProfile.Core.Extensions;

namespace IrWorkshop.Ui
{
	public enum LogType
	{
		Information,
		Warning,
		Error,
	}

	public class Logging
	{
		public static void SetupLogging()
		{
			if (Application.Current != null)
			{
				Application.Current.DispatcherUnhandledException += (s, e) =>
				{
					Exception("An unhandled exception has occurred.\r\n" + e.Exception.Message, e.Exception.GetTrace());
					e.Handled = true;
				};
			}

			// this may not work...
			AppDomain.CurrentDomain.UnhandledException += (s, e) =>
			{
				var ex = e.ExceptionObject as Exception;
				File.WriteAllText(AppDomainErrorLog, ex.GetTrace());
				Exception("An unhandled exception killed the process.\r\n" + ex.Message, ex.GetTrace());
			};
		}

		public static readonly string AppDomainErrorLog = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Exception.txt");

		public static void Exception(string message, string traceMessage)
		{
			ExceptionDialog.ShowDialog(message, traceMessage);
		}

		public static void ShowMessage(string message, LogType type, bool showImmediate = false)
		{
			MessageBoxImage img = MessageBoxImage.None;
			if (type == LogType.Information) img = MessageBoxImage.Information;
			if (type == LogType.Warning) img = MessageBoxImage.Exclamation;
			if (type == LogType.Error) img = MessageBoxImage.Error;

			if (showImmediate)
				MessageBox.Show(message, type.ToString(), MessageBoxButton.OK, img);
			else
				Task.Delay(200).ContinueWith(_ => MessageBox.Show(message, type.ToString(), MessageBoxButton.OK, img)); // prevents some weirdness in wpf
		}
	}
}
