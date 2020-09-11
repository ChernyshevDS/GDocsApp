using GalaSoft.MvvmLight.Command;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public MainWindow()
        {
            InitializeComponent();
        }
    }

    public interface IAuthorizationService
    {
        Task<UserCredential> GetUserCredentialAsync();
    }

    public interface IDriveServiceProvider
    {
        Task<DriveService> GetDriveServiceAsync();
    }

    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<T> valueFactory) :
            base(() => Task.Factory.StartNew(valueFactory))
        { }

        public AsyncLazy(Func<Task<T>> taskFactory) :
            base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap())
        { }

        public TaskAwaiter<T> GetAwaiter() => Value.GetAwaiter();
    }

    public class GoogleAuthorizationService : IAuthorizationService
    {
        private static readonly string CREDENTIALS_FILE_PATH = "credentials.json";

        private readonly AsyncLazy<UserCredential> UserCredential =
            new AsyncLazy<UserCredential>(CreateUserCredential);

        private static async Task<UserCredential> CreateUserCredential()
        {
            using var stream = new FileStream(CREDENTIALS_FILE_PATH, FileMode.Open, FileAccess.Read);
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                new[] { Google.Apis.Drive.v3.DriveService.Scope.DriveReadonly },
                "user", CancellationToken.None, new Google.Apis.Util.Store.FileDataStore("Drive.ListMyFiles"));
            return credential;
        }

        public async Task<UserCredential> GetUserCredentialAsync()
        {
            return await UserCredential;
        }
    }

    public class GoogleDriveServiceProvider : IDriveServiceProvider
    {
        private static readonly string APPLICATION_NAME = "Google Drive API Quickstart";

        private readonly AsyncLazy<DriveService> DriveService;
        private readonly IAuthorizationService authorizationService;

        private async Task<DriveService> CreateDriveService()
        {
            var credential = await authorizationService.GetUserCredentialAsync();
            var service = new DriveService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = APPLICATION_NAME
            });
            return service;
        }

        public GoogleDriveServiceProvider(IAuthorizationService authorizationService)
        {
            this.authorizationService = authorizationService;
            DriveService = new AsyncLazy<DriveService>(CreateDriveService);
        }
        
        public async Task<DriveService> GetDriveServiceAsync()
        {
            return await DriveService;
        }
    }

    public class MainVM : GalaSoft.MvvmLight.ViewModelBase
    {
        private readonly AsyncLazy<DriveService> DriveService;

        private RelayCommand getFilesCmd;

        public ICommand GetFilesCommand => getFilesCmd ??= new RelayCommand(HandleGetFiles);

        public ObservableCollection<DriveEntityVM> Files { get; } = new ObservableCollection<DriveEntityVM>();

        public MainVM(IDriveServiceProvider driveServiceProvider)
        {
            DriveService = new AsyncLazy<DriveService>(driveServiceProvider.GetDriveServiceAsync);
        }

        private async void HandleGetFiles()
        {
            await LoadFolder("root");
        }

        private async Task LoadFolder(string folderID)
        {
            var service = await DriveService;
            var filesRequest = service.Files.List();
            filesRequest.Q = $"'{folderID}' in parents";
            filesRequest.Fields = "nextPageToken, files(id, name, mimeType)";
            filesRequest.Spaces = "drive";
            var result = await filesRequest.ExecuteAsync();
            foreach (var item in result.Files)
            {
                //System.Diagnostics.Debug.WriteLine($"{item.Id}: {item.Name}");
                Files.Add(new DriveEntityVM() { ID = item.Id, Name = item.Name, MimeType = item.MimeType });
            }
            
        }
    }

    public class FileTreeItemVM : GalaSoft.MvvmLight.ViewModelBase
    {
        private bool isSelected = false;
        private bool isExpanded = false;
        private Image icon;
        private DriveEntityVM driveEntity;
        private bool isLoaded = false;

        public bool IsSelected { get => isSelected; set => Set(ref isSelected, value); }
        public bool IsExpanded
        {
            get => isExpanded; 
            set
            {
                if (Set(ref isExpanded, value))
                {
                    if (isExpanded == true && IsLoaded == false)
                    {
                        LoadChildren();
                    }
                }
            }
        }

        public bool IsLoaded { get => isLoaded; private set => Set(ref isLoaded, value); }
        public Image Icon { get => icon; set => Set(ref icon, value); }
        public DriveEntityVM DriveEntity { get => driveEntity; set => Set(ref driveEntity, value); }

        public ObservableCollection<FileTreeItemVM> Children { get; }

        public FileTreeItemVM(DriveEntityVM driveEntity)
        {
            DriveEntity = driveEntity;
            Children = new ObservableCollection<FileTreeItemVM>();
        }

        private void LoadChildren()
        {
            if(DriveEntity is FolderEntityVM vm)
            {
                MessengerInstance.Send(new MessageLoadChildren(vm));
            }
        }
    }

    public class MessageLoadChildren : GalaSoft.MvvmLight.Messaging.MessageBase
    { 
        public DriveEntityVM DriveEntity { get; private set; }

        public MessageLoadChildren(DriveEntityVM driveEntity)
        {
            DriveEntity = driveEntity;
        }
    }

    public class DriveEntityVM : GalaSoft.MvvmLight.ViewModelBase
    {
        private string iD;
        private string name;
        private string mimeType;

        public string ID { get => iD; set => Set(ref iD, value); }
        public string Name { get => name; set => Set(ref name, value); }
        public string MimeType { get => mimeType; set => Set(ref mimeType, value); }
    }

    public class FolderEntityVM : DriveEntityVM
    { 
        
    }
}
