using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ShaderBaker.View
{
    public partial class ShaderTextView : UserControl
    {
        public string ShaderText
        {
            get { return (string)GetValue(TheEditableTextProperty); }
            set { SetValue(TheEditableTextProperty, value); }
        }

        public static readonly DependencyProperty TheEditableTextProperty =
            DependencyProperty.Register(
                "ShaderText",
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
