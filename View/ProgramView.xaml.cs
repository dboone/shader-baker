using ShaderBaker.GlRenderer;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ShaderBaker.View
{

public partial class ProgramView : UserControl
{
    private static readonly DependencyProperty ProgramNameProperty =
        DependencyProperty.Register(
            "ProgramName",
            typeof(string),
            typeof(ProgramView),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

    public string ProgramName
    {
        get { return (string) GetValue(ProgramNameProperty); }
        set { SetValue(ProgramNameProperty, value); }
    }

    private static readonly DependencyProperty ProgramLinkValidityProperty =
        DependencyProperty.Register(
            "ProgramLinkValidity",
            typeof(Validity),
            typeof(ProgramView),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = false,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
    
    public Validity ProgramLinkValidity
    {
        get { return (Validity) GetValue(ProgramLinkValidityProperty); }
        set { SetValue(ProgramLinkValidityProperty, value); }
    }

    private static readonly DependencyProperty RenamingProperty =
        DependencyProperty.Register(
            "Renaming",
            typeof(bool),
            typeof(ProgramView),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                DefaultValue = false,
                PropertyChangedCallback = (d, e) => (d as ProgramView).onRenamingChanged((bool) e.NewValue)
            });

    public bool Renaming
    {
        get { return (bool) GetValue(RenamingProperty); }
        set { SetValue(RenamingProperty, value); }
    }

    public ProgramView()
    {
        InitializeComponent();
        LayoutRoot.DataContext = this;
    }

    private void onRenamingChanged(bool renaming)
    {
        if (renaming)
        {
            ProgramNameTextBlock.Visibility = Visibility.Hidden;
            ProgramNameTextBox.Text = ProgramName;
            ProgramNameTextBox.SelectAll();
            ProgramNameTextBox.Visibility = Visibility.Visible;
            ProgramNameTextBox.Focus();
        } else
        {
            ProgramNameTextBox.Visibility = Visibility.Hidden;
            ProgramNameTextBlock.Visibility = Visibility.Visible;
        }
    }

    private void stopRenaming()
    {
        Renaming = false;
    }

    private void confirmRename()
    {
        ProgramName = ProgramNameTextBox.Text;
        stopRenaming();
    }

    private void cancelRename()
    {
        stopRenaming();
    }

    private void ProgramNameTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (Renaming)
        {
            confirmRename();
        }
    }

    private void ProgramNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
        case Key.Return:
            confirmRename();
            break;
        case Key.Escape:
            cancelRename();
            break;
        }
    }
}

}
