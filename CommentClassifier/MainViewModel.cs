using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;

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

    public class MainViewModel: ViewModel
    {
        private string _folderPath;

        private List<SourceCodeFile> _sourceCode;
        
        public string FolderPath
        {
            get { return _folderPath; }
            set
            {
                _folderPath = value;
                RaisePropertyChanged();
            }
        }

        public ICommand Scan { get { return new DelegatingCommand(DoScan); } }

        public ICommand Save { get { return new DelegatingCommand(DoSave); } }

        public ICommand Load { get { return new DelegatingCommand(DoLoad); } }

        private void DoSave()
        {
            var file = new FileInfo(Path.Combine(FolderPath, "comments.txt"));
            using (var writer = new StreamWriter(file.OpenWrite()))
            {
                writer.WriteLine("# comment types");
                foreach (var sourceCodeFile in _sourceCode)
                {
                    writer.WriteLine("{0}", sourceCodeFile.File);

                    foreach (var entry in sourceCodeFile.Categories)
                    {
                        var lineNumber = entry.Key;
                        var category = entry.Value;
                        writer.WriteLine("{0}={1}", lineNumber, category);
                    }
                }
            }
        }

        private void DoLoad()
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
                    if (!line.StartsWith("#"))
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 1)
                        {
                            if (filename != null)
                            {
                                result.Add(new SourceCodeFile(new FileInfo(filename), dict));
                            }
                            filename = line;
                        }
                        if (parts.Length == 2)
                        {
                            dict[int.Parse(parts[0])] = parts[1];
                        }
                    }
                }
                if (filename != null)
                {
                    result.Add(new SourceCodeFile(new FileInfo(filename), dict));
                }
            }
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

        private void DoScan()
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
            _sourceCode = sourceCode;
            
        }
    }
}
