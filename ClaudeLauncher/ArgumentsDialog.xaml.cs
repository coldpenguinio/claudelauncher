using System.Windows;

namespace ClaudeLauncher;

public partial class ArgumentsDialog : Window
{
    public string Arguments => ArgumentsBox.Text;

    public ArgumentsDialog(string currentArguments)
    {
        InitializeComponent();
        ArgumentsBox.Text = currentArguments;

        Loaded += (s, e) =>
        {
            ArgumentsBox.Focus();
            ArgumentsBox.SelectAll();
        };
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Launch_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
