using ShaderBaker.Utilities;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ShaderBaker.View
{
    public partial class StatusMessageView : UserControl
    {
        public StatusMessageView()
        {
            InitializeComponent();
            LayoutRoot.DataContext = this;
        }

        public Option<string> StatusMessage
        {
            get { return (Option<string>) GetValue(StatusMessageProperty); }
            set { SetValue(StatusMessageProperty, value); }
        }

        private static readonly DependencyProperty StatusMessageProperty =
            DependencyProperty.Register(
                "StatusMessage",
                typeof(Option<string>),
                typeof(StatusMessageView),
                new FrameworkPropertyMetadata
                {
                    BindsTwoWayByDefault = false,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
    }

    class StatusMessageColorConverter : NoConvertBackValueConverter
    {
        public override object Convert(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Option<string>))
            {
                return "#DEADBEEF";
            }

            Option<string> option = (Option<string>) value;
            if (option.hasValue())
            {
                return "#FFFF0000";
            } else 
            {
                return "#FF00FF00";
            }
        }
    }

    class StatusMessageTextConverter : NoConvertBackValueConverter
    {
        public override object Convert(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Option<string>))
            {
                return "";
            }

            Option<string> option = (Option<string>) value;
            if (option.hasValue())
            {
                return "Error:\n" + option.get();
            } else 
            {
                return "Status: OK";
            }
        }
    }
}
