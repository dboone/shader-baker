using ShaderBaker.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System;

namespace ShaderBaker
{

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private ObjectRepositoryViewModel getDataContext()
    {
        return (ObjectRepositoryViewModel) DataContext;
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        getDataContext().GlContextManager.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        getDataContext().GlContextManager.StopAndWait();
    }
    
    private void ShaderListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
       getDataContext().OpenSelectedShader();
    }

    private void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        ShaderViewModel shader = (ShaderViewModel) ((Control) sender).DataContext;
        ((ObjectRepositoryViewModel) ShaderTabTextView.DataContext).CloseShaderTab(shader);
    }
}

}