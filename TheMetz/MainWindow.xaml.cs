using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using TheMetz.Services;

namespace TheMetz;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private AdoDataService _dataService;

    public MainWindow()
    {
        InitializeComponent();
        
        const string orgUrl = "https://tfs.clarkinc.biz/DefaultCollection";

        const string personalAccessToken = "j3dcue4ijcpz6qxmdzb6uvdm6t6bgxaw2d3tcn6ymswbvuv7y7ra";
            
        var connection =
            new VssConnection(new Uri(orgUrl), new VssBasicCredential(string.Empty, personalAccessToken));
        _dataService = new AdoDataService(connection);
        
        ResultsList.Items.Add("Devs with PR comments in last 2 weeks");
    }

    private async Task LoadData()
    {
        ResultsList.Items.Clear();

        ResultsList.Items.Add("Loading...");

        IEnumerable<KeyValuePair<string, int>> test = await _dataService.ShowCommentCounts();

        ResultsList.Items.Clear();

        foreach (KeyValuePair<string, int> keyValuePair in test)
        {
            ResultsList.Items.Add($"{keyValuePair.Key}: {keyValuePair.Value}");
        }

    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem == null)
        {
            return;
        }
        
        PRResults.Document.Blocks.Clear();
            
        var selectedAuthor = ResultsList.SelectedItem.ToString();
            
        var authorParagraph = new Paragraph(new Run(selectedAuthor));
        PRResults.Document.Blocks.Add(authorParagraph);

        string authorName = selectedAuthor![..selectedAuthor.IndexOf(':')];
        List<(string Title, string Url)> commentLinks = _dataService.GetDeveloperCommentLinks(authorName);

        foreach ((string Title, string Url) linkInfo in commentLinks)
        {
            var paragraph = new Paragraph();
            var hyperlink = new Hyperlink(new Run(linkInfo.Title))
            {
                NavigateUri = new Uri(linkInfo.Url),
                Cursor = Cursors.Hand
            };

            hyperlink.PreviewMouseLeftButtonDown += (s, args) =>
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = linkInfo.Url,
                    UseShellExecute = true
                });
            };

            paragraph.Inlines.Add(hyperlink);
            PRResults.Document.Blocks.Add(paragraph);
        }
        ResultsList.SelectedItem = null;
    }
    
    private async void FetchButtonClicked(object sender, RoutedEventArgs e)
    {
        await LoadData();
    }
}