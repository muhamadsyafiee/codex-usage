using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using TextBox = System.Windows.Controls.TextBox;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using Orientation = System.Windows.Controls.Orientation;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace CodexUsage;

public sealed class FeedbackWindow : Window
{
    private readonly TextBox feedbackBox;
    private readonly CheckBox deviceInfo;
    private readonly Button submitButton;
    private readonly TextBlock statusText;
    private readonly Brush foreground;
    private readonly Brush background;

    public FeedbackWindow(bool lightTheme)
    {
        Title = "Send feedback";
        Width = 440;
        Height = 390;
        MinWidth = 360;
        MinHeight = 330;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        background = lightTheme ? Brushes.White : Brushes.Black;
        foreground = lightTheme ? Brushes.Black : Brushes.White;
        Background = background;
        Foreground = foreground;
        FontFamily = new FontFamily("Segoe UI");

        var panel = new Grid { Margin = new Thickness(22) };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new TextBlock
        {
            Text = "Help improve Codex Usage Widget",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = foreground,
            Margin = new Thickness(0, 0, 0, 6)
        };
        panel.Children.Add(title);
        var intro = Label("Tell us what happened or what you would like to see next.");
        Grid.SetRow(intro, 1);
        panel.Children.Add(intro);

        var feedbackLabel = new TextBlock { Text = "Your feedback", Foreground = foreground, Margin = new Thickness(0, 12, 0, 5) };
        Grid.SetRow(feedbackLabel, 2);
        panel.Children.Add(feedbackLabel);
        feedbackBox = new TextBox
        {
            MinHeight = 90,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(8),
            Background = background,
            Foreground = foreground,
            BorderBrush = foreground,
            BorderThickness = new Thickness(1)
        };
        AutomationProperties.SetName(feedbackBox, "Feedback message");
        Grid.SetRow(feedbackBox, 3);
        panel.Children.Add(feedbackBox);

        deviceInfo = new CheckBox
        {
            IsChecked = true,
            Content = "Include device info (Windows, architecture, app and runtime)",
            Foreground = foreground,
            Margin = new Thickness(0, 12, 0, 3)
        };
        AutomationProperties.SetName(deviceInfo, "Include device information");
        Grid.SetRow(deviceInfo, 4);
        panel.Children.Add(deviceInfo);
        var privacy = Label("No name, hostname or files are sent.");
        Grid.SetRow(privacy, 5);
        panel.Children.Add(privacy);

        statusText = Label("");
        statusText.Margin = new Thickness(0, 8, 0, 2);
        Grid.SetRow(statusText, 6);
        panel.Children.Add(statusText);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = Button("Cancel");
        cancel.Click += (_, _) => Close();
        submitButton = Button("Submit feedback");
        submitButton.IsDefault = true;
        submitButton.Click += async (_, _) => await Submit();
        buttons.Children.Add(cancel);
        buttons.Children.Add(submitButton);
        Grid.SetRow(buttons, 7);
        panel.Children.Add(buttons);

        Content = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Child = panel
        };
        Loaded += (_, _) => feedbackBox.Focus();
    }

    private TextBlock Label(string text) => new()
    {
        Text = text,
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap,
        Foreground = foreground,
        Margin = new Thickness(0, 0, 0, 4)
    };

    private Button Button(string text) => new()
    {
        Content = text,
        Padding = new Thickness(12, 7, 12, 7),
        Margin = new Thickness(6, 4, 0, 0),
        Background = background,
        Foreground = foreground,
        BorderBrush = foreground,
        BorderThickness = new Thickness(1)
    };

    private async Task Submit()
    {
        if (string.IsNullOrWhiteSpace(feedbackBox.Text))
        {
            statusText.Text = "Write some feedback first.";
            feedbackBox.Focus();
            return;
        }

        submitButton.IsEnabled = false;
        feedbackBox.IsEnabled = false;
        deviceInfo.IsEnabled = false;
        statusText.Text = "Sending feedback…";
        try
        {
            await FeedbackService.SubmitAsync(feedbackBox.Text, deviceInfo.IsChecked == true);
            statusText.Text = "Feedback sent. Thank you.";
        }
        catch
        {
            submitButton.IsEnabled = true;
            feedbackBox.IsEnabled = true;
            deviceInfo.IsEnabled = true;
            statusText.Text = "Could not send feedback. Check your connection and try again.";
        }
    }
}
