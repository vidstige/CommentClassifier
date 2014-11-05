using System.Windows;

namespace CommentClassifier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(this);
        }

        public void ScrollIntoView(object item)
        {
            _fileContents.ScrollIntoView(item);
        }
    }
}
