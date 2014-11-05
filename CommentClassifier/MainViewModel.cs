using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace CommentClassifier
{
    class SourceCodeFile
    {
        private readonly FileInfo _file;
        private readonly Dictionary<int, string> _commentCategories;

        public SourceCodeFile(FileInfo file, Dictionary<int, string> commentLines)
        {
            _file = file;
            _commentCategories = commentLines;
        }

        public Dictionary<int, string> Categories { get { return _commentCategories; } }

        public FileInfo File { get { return _file; } }
    }

    public class LineViewModel: ViewModel
    {
        private readonly string _text;
        private readonly Brush _background;

        public LineViewModel(string text, Brush background)
        {
            _text = text;
            _background = background;
        }

        public string Text { get { return _text; } }
        public Brush Background { get { return _background; } }
    }

    public class MainViewModel: ViewModel
    {
        private string _folderPath;
        private List<SourceCodeFile> _sourceCode;

        private int _currrentSourceCodeFileIndex = -1;
        private string[] _currentFileContents;
        private int _currentLine;
        
        public string FolderPath
        {
            get { return _folderPath; }
            set
            {
                _folderPath = value;
                RaisePropertyChanged();
            }
        }

        public string CurrentLine
        {
            get
            {
                if (_currentFileContents == null) return string.Empty;
                return _currentFileContents[_currentLine];
            }
        }

        private List<SourceCodeFile> SourceCode
        {
            set
            {
                _sourceCode = value;
                RaisePropertyChanged("TotalComments");
                RaisePropertyChanged("CommentsClassified");
            }
        }

        private IEnumerable<string> AllCatagories
        {
            get
            {
                if (_sourceCode == null) return Enumerable.Empty<string>();
                return _sourceCode.SelectMany(scf => scf.Categories).Select(e => e.Value);
            }
        }

        public int CommentsClassified
        {
            get { return AllCatagories.Count(c => c != null); }
        }

        public int TotalComments
        {
            get { return AllCatagories.Count(); }
        }

        private Brush BrushForLineNumber(int lineNumber)
        {
            if (lineNumber == _currentLine) return Brushes.CornflowerBlue;
            return Brushes.LightGray;
        }

        public LineViewModel[] CurrentFileContents
        {
            get
            {
                if (_currentFileContents == null) return new LineViewModel[0];
                return _currentFileContents.Select((v, i) => new LineViewModel(v, BrushForLineNumber(i))).ToArray();
            }
        }

        public ICommand Scan { get { return new DelegatingCommand(DoScan); } }

        public ICommand Save { get { return new DelegatingCommand(DoSave); } }

        public ICommand Load { get { return new DelegatingCommand(DoLoad); } }

        public ICommand ClassifyAs { get { return new DelegatingCommand(Classify); } }
        public ICommand SkipLine { get { return new DelegatingCommand(x => NextComment()); } }

        private void Classify(object parameter)
        {
            var category = parameter as string;
            if (_sourceCode[_currrentSourceCodeFileIndex].Categories.ContainsKey(_currentLine))
            {
                _sourceCode[_currrentSourceCodeFileIndex].Categories[_currentLine] = category;
            }
            else
            {
                System.Console.WriteLine("That was not a comment...?");
            }

            RaisePropertyChanged("CommentsClassified");

            NextComment();
        }

        private void DoSave(object unused)
        {
            var file = new FileInfo(Path.Combine(FolderPath, "comments.txt"));
            using (var writer = new StreamWriter(file.OpenWrite()))
            {
                writer.WriteLine("# comment types");
                foreach (var sourceCodeFile in _sourceCode)
                {
                    writer.WriteLine("{0}", sourceCodeFile.File.FullName);

                    foreach (var entry in sourceCodeFile.Categories)
                    {
                        var lineNumber = entry.Key;
                        var category = entry.Value;
                        writer.WriteLine("{0}={1}", lineNumber, category);
                    }
                }
            }
        }

        private void DoLoad(object unused)
        {
            var file = new FileInfo(Path.Combine(FolderPath, "comments.txt"));
            var result = new List<SourceCodeFile>();
            using (var reader = file.OpenText())
            {
                string filename = null;
                var dict = new Dictionary<int, string>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;
                    
                    var parts = line.Split('=');
                    if (parts.Length == 1)
                    {
                        if (filename != null)
                        {
                            result.Add(new SourceCodeFile(new FileInfo(filename), dict));
                            dict = new Dictionary<int, string>();
                        }
                        filename = line;
                    }
                    if (parts.Length == 2)
                    {
                        var category = parts[1];
                        if (string.IsNullOrEmpty(category)) category = null;
                        dict[int.Parse(parts[0])] = category;
                    }
                    
                }
                if (filename != null)
                {
                    result.Add(new SourceCodeFile(new FileInfo(filename), dict));
                }
            }
            
            SourceCode = result;

            // Go to next comment
            NextComment();
        }

        private void NextComment()
        {
            if (_currrentSourceCodeFileIndex < 0)
            {
                _currrentSourceCodeFileIndex = 0;
                _currentFileContents = File.ReadAllLines(_sourceCode[_currrentSourceCodeFileIndex].File.FullName);

                _currentLine = 0;
            }
            else
            {
                var lines = _sourceCode[_currrentSourceCodeFileIndex].Categories.Keys.ToList();
                lines.Sort();
                var old = _currentLine;
                foreach (var lineNumber in lines)
                {
                    if (lineNumber > old && _sourceCode[_currrentSourceCodeFileIndex].Categories[lineNumber] == null)
                    {
                        _currentLine = lineNumber;
                        break;
                    }
                }

                if (_currentLine == old)
                {
                    // Out of comments, go to next file
                    _currrentSourceCodeFileIndex++;
                    _currentFileContents = File.ReadAllLines(_sourceCode[_currrentSourceCodeFileIndex].File.FullName);
                    //RaisePropertyChanged("CurrentFileContents");
                    _currentLine = 0;
                }
            }


            RaisePropertyChanged("CurrentFileContents");
            RaisePropertyChanged("CurrentLine");
        }

        private IEnumerable<FileInfo> Search(DirectoryInfo root, string[] extensions)
        {
            var directories = new Stack<DirectoryInfo>(new[] { root });
            while (directories.Any())
            {
                DirectoryInfo dir = directories.Pop();
                foreach (var file in dir.EnumerateFiles())
                {
                    if (extensions.Contains(file.Extension))
                        yield return file;
                }
                foreach (var d in dir.EnumerateDirectories()) directories.Push(d);
            }
        }

        private void DoScan(object unused)
        {
            var sourceCode = new List<SourceCodeFile>();
            foreach (var f in Search(new DirectoryInfo(_folderPath), new[] { ".cpp", ".hpp", ".h" }))
            {
                var comments = new List<int>();
                bool inComment = false;
                int lineNumber = 0;
                using (var s = f.OpenText())
                {
                    while (!s.EndOfStream)
                    {
                        var line = s.ReadLine();
                        if (line.Contains("/*"))
                            inComment = true;

                        if (inComment || line.Contains("//"))
                            comments.Add(lineNumber);

                        if (line.Contains("/*"))
                            inComment = false;

                        lineNumber++;
                    }
                }

                var tmp = new Dictionary<int, string>(comments.Count);
                foreach (var commentLine in comments)
                    tmp.Add(commentLine, null);

                sourceCode.Add(new SourceCodeFile(f, tmp));

            }
            SourceCode = sourceCode;
        }
    }
}
