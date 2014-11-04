using System.Windows.Input;

namespace CommentClassifier
{
    public class MainViewModel: ViewModel
    {
        private string _path;
        
        public string Path
        {
            get { return _path; }
            set
            {
                _path = value;
                RaisePropertyChanged();
            }
        }

        public ICommand Scan { get { return new DelegatingCommand(DoScan); } }

        private void DoScan()
        {
        }
    }
}
