using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace CommentClassifier
{
    class SourceCodeFile
    {
        private readonly Dictionary<int, string> _commentCategories;

        public SourceCodeFile(List<int> commentLines)
        {
            _commentCategories = new Dictionary<int, string>(commentLines.Count);
            foreach (var commentLine in commentLines)
                _commentCategories.Add(commentLine, null);
        }
    }

    public class MainViewModel: ViewModel
    {
        private string _path;

        private List<SourceCodeFile> _sourceCode;
        
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

        private void DoScan()
        {
            var sourceCode = new List<SourceCodeFile>();
            foreach (var f in Search(new DirectoryInfo(Path), new[] { ".cpp", ".hpp", ".h" }))
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
                sourceCode.Add(new SourceCodeFile(comments));

            }
            _sourceCode = sourceCode;
            
        }
    }
}
