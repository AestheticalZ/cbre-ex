﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace CBRE.Editor
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			//TODO: fix this tomfuckery that .NET 6.0 did
			Application.SetDefaultFont(new Font(new FontFamily("Microsoft Sans Serif"), 8f));
			RegisterHandlers();
			SingleInstance.Start(typeof(Editor));
		}

		private static void RegisterHandlers()
		{
			Application.ThreadException += ThreadException;
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
			AppDomain.CurrentDomain.UnhandledException += UnhandledException;
		}

		private static void UnhandledException(object sender, UnhandledExceptionEventArgs args)
		{
			LogException((Exception)args.ExceptionObject);
		}

		private static void ThreadException(object sender, ThreadExceptionEventArgs args)
		{
			LogException(args.Exception);
		}

		private static void LogException(Exception ex)
		{
			StackTrace st = new StackTrace();
			StackFrame[] frames = st.GetFrames() ?? new StackFrame[0];
			string msg = "Unhandled exception";
			foreach (StackFrame frame in frames)
			{
				System.Reflection.MethodBase method = frame.GetMethod();
				msg += "\r\n    " + method.ReflectedType.FullName + "." + method.Name;
			}
			Logging.Logger.ShowException(new Exception(msg, ex), "Unhandled exception");
		}
	}
}
