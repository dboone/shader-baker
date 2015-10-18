using ShaderBaker.ViewModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System;

namespace ShaderBaker.View
{

public partial class ProgramTreeView : UserControl
{
    public ObservableCollection<ProgramViewModel> Programs
    {
        get { return (ObservableCollection<ProgramViewModel>) GetValue(ProgramsProperty); }
        set { SetValue(ProgramsProperty, value); }
    }

    private static readonly DependencyProperty ProgramsProperty =
        DependencyProperty.Register(
            "Programs",
            typeof(ObservableCollection<ProgramViewModel>),
            typeof(ProgramTreeView),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = false,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

    public ProgramViewModel SelectedProgram
    {
        get { return (ProgramViewModel) GetValue(SelectedProgramProperty); }
        set { SetValue(SelectedProgramProperty, value); }
    }

    private static readonly DependencyProperty SelectedProgramProperty =
        DependencyProperty.Register(
            "SelectedProgram",
            typeof(ProgramViewModel),
            typeof(ProgramTreeView),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

    public ShaderViewModel SelectedShader
    {
        get { return (ShaderViewModel) GetValue(SelectedShaderProperty); }
        set { SetValue(SelectedShaderProperty, value); }
    }

    private static readonly DependencyProperty SelectedShaderProperty =
        DependencyProperty.Register(
            "SelectedShader",
            typeof(ShaderViewModel),
            typeof(ProgramTreeView),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

    public ProgramViewModel ActiveProgram
    {
        get { return (ProgramViewModel) GetValue(ActiveProgramProperty); }
        set { SetValue(ActiveProgramProperty, value); }
    }

    private static readonly DependencyProperty ActiveProgramProperty =
        DependencyProperty.Register(
            "ActiveProgram",
            typeof(ProgramViewModel),
            typeof(ProgramTreeView),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

    public ProgramTreeView()
    {
        InitializeComponent();
    }

    private void TreeView_SelectedItemChanged<T>(object sender, RoutedPropertyChangedEventArgs<T> e)
    {
        SelectedProgram = e.NewValue as ProgramViewModel;
        SelectedShader = e.NewValue as ShaderViewModel;
    }

    private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SelectedProgram != null)
        {
            ActiveProgram = SelectedProgram;
        }
    }
}
}
