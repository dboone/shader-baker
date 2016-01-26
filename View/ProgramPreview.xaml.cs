using ShaderBaker.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace ShaderBaker.View
{

public partial class ProgramPreview : UserControl
{
    public ProgramPreview()
    {
        InitializeComponent();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ((ObjectRepositoryViewModel) DataContext).OnResize(e.NewSize);
    }
}

}
