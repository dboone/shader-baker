using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ShaderBaker.View
{
    /// <summary>
    /// Interaction logic for ShaderTextView.xaml
    /// </summary>
    public partial class ShaderTextView : UserControl
    {
        public string TheEditableText
        {
            get { return (string)GetValue(TheEditableTextProperty); }
            set { SetValue(TheEditableTextProperty, value); }
        }

        public static readonly DependencyProperty TheEditableTextProperty =
            DependencyProperty.Register(
                "TheEditableText",
                typeof(string),
                typeof(ShaderTextView),
                new FrameworkPropertyMetadata
                {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

        public ShaderTextView()
        {
            InitializeComponent();
            LayoutRoot.DataContext = this;
        }
    }
}
