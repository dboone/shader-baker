using ShaderBaker.DataAccess;

namespace ShaderBaker.ViewModel
{
    class ShaderTextViewModel : ViewModelBase
    {
        private string _text;
        public string Text
        {
            get { return _text; }
            set { _text = value; NotifyPropertyChanged("Text"); }
        }

        public ShaderTextViewModel(ShaderResource resource)
        {
            _text = resource.GetText();
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            // Not sure about this part yet...
        }
    }
}
