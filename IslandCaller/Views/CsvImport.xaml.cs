using System.Windows;
using System.Windows.Controls;

namespace IslandCaller.Views;

public partial class CsvImport : Window
{
    public int NameRow { get; private set; }

    public CsvImport()
    {
        InitializeComponent();
    }

    private void NumberBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var tb = (TextBox)sender;
        if (!string.IsNullOrEmpty(tb.Text) && !int.TryParse(tb.Text, out int value))
        {
            tb.Text = string.Empty;
        }
        else if (tb.Text.StartsWith("0"))
        {
            tb.Text = tb.Text.TrimStart('0');
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        NameRow = Convert.ToInt32(name_row?.Text ?? "1") - 1;
        DialogResult = true;
        Close();
    }
}
