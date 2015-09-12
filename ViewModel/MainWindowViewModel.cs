using ShaderBaker.DataAccess;
using System.Collections.ObjectModel;

namespace ShaderBaker.ViewModel
{
    class MainWindowViewModel
    {
        private ObservableCollection<ViewModelBase> viewModels;

        public MainWindowViewModel()
        {
            ShaderResource resource = new ShaderResource();
            ShaderTextViewModel textViewModel = new ShaderTextViewModel(resource);
            ShaderRenderViewModel renderViewModel = new ShaderRenderViewModel();
            ViewModels.Add(textViewModel);
            ViewModels.Add(renderViewModel);
        }

        public ObservableCollection<ViewModelBase> ViewModels
        {
            get
            {
                if (viewModels == null)
                {
                    viewModels = new ObservableCollection<ViewModelBase>();
                }

                return viewModels;
            }
        }
    }
}
