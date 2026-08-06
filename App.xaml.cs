using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Limpador_USB
{
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash(e.Exception);
            MessageBox.Show($"Ocorreu um erro inesperado na aplicação:\n{e.Exception.Message}\n\nDetalhes salvos na pasta TEMP.", "Erro na Aplicação", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogCrash(ex);
            }
        }

        private static void LogCrash(Exception ex)
        {
            try
            {
                string logDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP") ?? ".", "Limpador_USB_logs");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                File.WriteAllText(logPath, ex.ToString());
            }
            catch { }
        }
    }
}
