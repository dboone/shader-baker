using ShaderBaker.ViewModel;
using System.Windows;

namespace ShaderBaker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MainWindow window = new MainWindow();
            var viewModel = new ProgramRendererViewModel();
            window.DataContext = viewModel;
            window.Show();
        }
    }
}
