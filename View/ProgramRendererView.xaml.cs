using ShaderBaker.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace ShaderBaker.View
{
    public partial class ProgramRendererView : UserControl
    {
        public ProgramRendererView()
        {
            InitializeComponent();

            Application.Current.Exit += (sender, e) => ((ProgramRendererViewModel) DataContext).Shutdown();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ((ProgramRendererViewModel) DataContext).Initialize();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ((ProgramRendererViewModel) DataContext).OnResize(e.NewSize);
        }
    }
}
