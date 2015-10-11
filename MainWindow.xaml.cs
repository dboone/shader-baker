using ShaderBaker.ViewModel;
using System.Windows;
using System.Windows.Input;

namespace ShaderBaker
{

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ShaderListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ((ObjectRepositoryViewModel) ShaderTabTextView.DataContext).OpenSelectedShader();
    }
}

}