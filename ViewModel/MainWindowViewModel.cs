using ShaderBaker.DataAccess;
using ShaderBaker.GlRenderer;
using System.Collections.ObjectModel;

namespace ShaderBaker.ViewModel
{
    class MainWindowViewModel
    {
        private ObservableCollection<ViewModelBase> viewModels;

        public MainWindowViewModel()
        {
            var textViewModel = new ShaderTextViewModel(new ShaderResource());
            var programViewModel = new ProgramRendererViewModel();
            
            ViewModels.Add(textViewModel);
            ViewModels.Add(programViewModel);
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
