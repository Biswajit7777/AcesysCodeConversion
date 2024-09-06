using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Fls.AcesysConversion.Common.Logging;
using Fls.AcesysConversion.UI.Messages;
using Fls.AcesysConversion.UI.Services;
using Fls.AcesysConversion.UI.Services.Interface;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Windows;

namespace Fls.AcesysConversion.UI
{
    public partial class App : Application
    {
        public ILogger Logger;
        public App()
        {
            Logger = LogHelper.Instance.Logger;
            Logger.Information("App started..");

            Dispatcher.UnhandledException += OnDispatcherUnhandledException;

            try
            {
                Ioc.Default.ConfigureServices(
                    new ServiceCollection()
                    .AddSingleton((IOpenFileDlgVM)new OpenFileDlgVM())
                    .AddSingleton<IMsgBoxService, MsgBoxService>()
                    .AddSingleton((ISaveFileDlgVM)new SaveFileDlgVM())
                    .AddSingleton<MessageBroker>()
                    .AddSingleton<WeakReferenceMessenger>()
                    .AddSingleton<IMessenger, WeakReferenceMessenger>(provider => provider.GetRequiredService<WeakReferenceMessenger>())
                    .BuildServiceProvider()
                );
            }
            catch (Exception ex)
            {
                Log.Error($"{DateTime.Now} - {ex} - \n\n{ex.StackTrace}");
                IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();
                _ = (msgBoxService?.Show("Unexpected error: \\n\\n" + ex.ToString(), img: MessageBoxImage.Error));
            }
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow wnd = new();

            wnd.Show();

            Log.CloseAndFlush();
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = string.Format("An unhandled exception occurred: {0}", e.Exception.Message);
            _ = MessageBox.Show(errorMessage, "Unknown Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Log.Error(errorMessage);
            Log.CloseAndFlush();
            e.Handled = true;
        }
    }
}
