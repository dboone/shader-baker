﻿using ShaderBaker.GlRenderer;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ShaderBaker.View
{

public partial class ShaderListItemView : UserControl
{
    public const string KEY_VERTEX_SHADER_ICON = "VertexShaderIcon";
    public const string KEY_GEOMETRY_SHADER_ICON = "GeometryShaderIcon";
    public const string KEY_FRAGMENT_SHADER_ICON = "FragmentShaderIcon";

    public ProgramStage ShaderStage
    {
        get { return (ProgramStage) GetValue(ShaderStageProperty); }
        set { SetValue(ShaderStageProperty, value); }
    }

    private static readonly DependencyProperty ShaderStageProperty =
        DependencyProperty.Register(
            "ShaderStage",
            typeof(ProgramStage),
            typeof(ShaderListItemView),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = false,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

    public string ShaderName
    {
        get { return (string) GetValue(ShaderNameProperty); }
        set { SetValue(ShaderNameProperty, value); }
    }

    private static readonly DependencyProperty ShaderNameProperty =
        DependencyProperty.Register(
            "ShaderName",
            typeof(string),
            typeof(ShaderListItemView),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

    public Validity ShaderSourceValidity
    {
        get { return (Validity) GetValue(ShaderSourceValidityProperty); }
        set { SetValue(ShaderSourceValidityProperty, value); }
    }

    private static readonly DependencyProperty ShaderSourceValidityProperty =
        DependencyProperty.Register(
            "ShaderSourceValidity",
            typeof(Validity),
            typeof(ShaderListItemView),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = false,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

    public bool Renaming
    {
        get { return (bool) GetValue(RenamingProperty); }
        set { SetValue(RenamingProperty, value); }
    }

    private static readonly DependencyProperty RenamingProperty =
        DependencyProperty.Register(
            "Renaming",
            typeof(bool),
            typeof(ShaderListItemView),
            new FrameworkPropertyMetadata
            {
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                DefaultValue = false,
                PropertyChangedCallback = (d, e) => (d as ShaderListItemView).onRenamingChanged((bool) e.NewValue)
            });

    public ShaderListItemView()
    {
        InitializeComponent();
        LayoutRoot.DataContext = this;
    }

    private void onRenamingChanged(bool renaming)
    {
        if (renaming)
        {
            ShaderNameTextBlock.Visibility = Visibility.Hidden;
            ShaderNameTextBox.Text = ShaderName;
            ShaderNameTextBox.SelectAll();
            ShaderNameTextBox.Visibility = Visibility.Visible;
            ShaderNameTextBox.Focus();
        } else
        {
            ShaderNameTextBox.Visibility = Visibility.Hidden;
            ShaderNameTextBlock.Visibility = Visibility.Visible;
        }
    }

    private void stopRenaming()
    {
        Renaming = false;
    }

    private void confirmRename()
    {
        ShaderName = ShaderNameTextBox.Text;
        stopRenaming();
    }

    private void cancelRename()
    {
        stopRenaming();
    }

    private void ShaderNameTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (Renaming)
        {
            confirmRename();
        }
    }

    private void ShaderNameTextBox_KeyDown(object sender, KeyEventArgs e)
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

class ShaderStageToIconConverter : NoConvertBackValueConverter
{
    public override object Convert(
        object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!(value is ProgramStage))
        {
            return null;
        }

        ProgramStage stage = (ProgramStage) value;
        string resourceName;
        switch (stage)
        {
        case ProgramStage.Vertex:
            resourceName = ShaderListItemView.KEY_VERTEX_SHADER_ICON;
            break;
        case ProgramStage.Geometry:
            resourceName = ShaderListItemView.KEY_GEOMETRY_SHADER_ICON;
            break;
        case ProgramStage.Fragment:
            resourceName = ShaderListItemView.KEY_FRAGMENT_SHADER_ICON;
            break;
        default:
            throw new NotImplementedException(
                "Missing switch branch for shader stage: " + stage.ToString());
        }

        return Application.Current.FindResource(resourceName);
    }
}

}
