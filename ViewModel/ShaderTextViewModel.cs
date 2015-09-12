using ShaderBaker.DataAccess;
using System.ComponentModel;

namespace ShaderBaker.ViewModel
{
    class ShaderTextViewModel : ViewModelBase
    {
        private string _text;
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                OnPropertyChanged("Text");
            }
        }

        public ShaderTextViewModel(ShaderResource resource)
        {
            _text = resource.GetText();
        }
    }
}
