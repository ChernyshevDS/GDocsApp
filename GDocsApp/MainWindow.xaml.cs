using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GDocsApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string APPLICATION_NAME = "Google Drive API Quickstart";
        private static readonly string CREDENTIALS_FILE_PATH = "credentials.json";

        public MainWindow()
        {
            InitializeComponent();
        }

        private async Task LoadFilesList()
        {
            Google.Apis.Auth.OAuth2.UserCredential credential;
            using (var stream = new FileStream(CREDENTIALS_FILE_PATH, FileMode.Open, FileAccess.Read))
            {
                credential = await Google.Apis.Auth.OAuth2.GoogleWebAuthorizationBroker.AuthorizeAsync(
                    Google.Apis.Auth.OAuth2.GoogleClientSecrets.Load(stream).Secrets,
                    new[] { Google.Apis.Drive.v3.DriveService.Scope.DriveReadonly },
                    "user", CancellationToken.None, new Google.Apis.Util.Store.FileDataStore("Drive.ListMyFiles"));
            }

            var service = new Google.Apis.Drive.v3.DriveService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = APPLICATION_NAME
            });

            var filesRequest = service.Files.List();
            filesRequest.PageSize = 10;
            filesRequest.Fields = "nextPageToken, files(id, name)";
            var result = await filesRequest.ExecuteAsync();
            foreach (var item in result.Files)
            {
                System.Diagnostics.Debug.WriteLine($"{item.Id}: {item.Name}");
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await LoadFilesList();
        }
    }
}
