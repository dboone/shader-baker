using ShaderBaker.GlRenderer;
using ShaderBaker.ViewModel;
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

    private static readonly DependencyProperty RenamingProgramProperty =
        DependencyProperty.Register(
            "RenamingProgram",
            typeof(ProgramViewModel),
            typeof(ProgramView),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                PropertyChangedCallback = (d, e) => (d as ProgramView).onRenamingProgramChanged((ProgramViewModel) e.NewValue)
            });

    public ProgramViewModel RenamingProgram
    {
        get { return (ProgramViewModel) GetValue(RenamingProgramProperty); }
        set { SetValue(RenamingProgramProperty, value); }
    }
    
    private static readonly DependencyProperty ActiveProgramProperty =
        DependencyProperty.Register(
            "ActiveProgram",
            typeof(ProgramViewModel),
            typeof(ProgramView),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = false,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                PropertyChangedCallback = (d, e) => (d as ProgramView).onActiveProgramChanged((ProgramViewModel) e.NewValue)
            });

    public ProgramViewModel ActiveProgram
    {
        get { return (ProgramViewModel) GetValue(ActiveProgramProperty); }
        set { SetValue(ActiveProgramProperty, value); }
    }

    private static readonly DependencyProperty IsActiveProgramProperty =
        DependencyProperty.Register(
            "IsActiveProgram",
            typeof(bool),
            typeof(ProgramView),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = false,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                DefaultValue = false
            });

    public bool IsActiveProgram
    {
        get { return (bool) GetValue(IsActiveProgramProperty); }
        private set { SetValue(IsActiveProgramProperty, value); }
    }

    public ProgramView()
    {
        InitializeComponent();
        LayoutRoot.DataContext = this;
    }

    private void onRenamingProgramChanged(ProgramViewModel newValue)
    {
        if (newValue == DataContext)
        {
            startRenaming();
        } else if (newValue == null)
        {
            stopRenaming();
        }
    }

    private void startRenaming()
    {
        ProgramNameTextBlock.Visibility = Visibility.Hidden;
        ProgramNameTextBox.Text = ProgramName;
        ProgramNameTextBox.SelectAll();
        ProgramNameTextBox.Visibility = Visibility.Visible;
        ProgramNameTextBox.Focus();
    }

    private void stopRenaming()
    {
        ProgramNameTextBox.Visibility = Visibility.Hidden;
        ProgramNameTextBlock.Visibility = Visibility.Visible;
        RenamingProgram = null;
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
        // The text box can lose focus if either the user clicks away from it, or it gets hidden.
        // The text box gets hidden either when the user confirms a rename or cancels it. This
        // check prevents confirmRename() from being called when a rename is canceled.
        if (RenamingProgram == DataContext)
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

    private void onActiveProgramChanged(ProgramViewModel newValue)
    {
        IsActiveProgram = newValue == DataContext;
    }
}

}
