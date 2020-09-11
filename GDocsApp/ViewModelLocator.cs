using GalaSoft.MvvmLight.Ioc;

namespace GDocsApp
{
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            SimpleIoc.Default.Register<IDriveServiceProvider, GoogleDriveServiceProvider>();
            SimpleIoc.Default.Register<IAuthorizationService, GoogleAuthorizationService>();
            SimpleIoc.Default.Register<MainVM>();
        }

        public MainVM MainVM => SimpleIoc.Default.GetInstance<MainVM>();
    }
}
