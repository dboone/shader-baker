using ShaderBaker.ViewModel;
using System.Windows;

namespace ShaderBaker
{

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        MainWindow window = new MainWindow();
        window.Show();

        if (e.Args.Length == 1)
        {
            ((ObjectRepositoryViewModel) window.DataContext).OpenProjectFromFile(e.Args[0]);
        }
    }
}

}
