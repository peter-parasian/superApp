using MahApps.Metro.IconPacks;
using Coll = System.Collections.Generic;
using Controls = System.Windows.Controls;
using IO = System.IO;
using Linq = System.Linq;
using Models = MyAPP.Models;
using Services = MyAPP.Services;
using Sys = System;
using Tasks = System.Threading.Tasks;
using Text = System.Text;
using Win = System.Windows;

namespace MyAPP.Views
{
    public partial class DashboardView
    {
        private static readonly Coll.Dictionary<Sys.String, Sys.String> _langMap = new Coll.Dictionary<Sys.String, Sys.String>(Sys.StringComparer.OrdinalIgnoreCase)
        {
            {".py", "python"}, {".ipynb", "python"}, {".js", "javascript"}, {".mjs", "javascript"},
            {".cjs", "javascript"}, {".ts", "typescript"}, {".tsx", "tsx"}, {".jsx", "jsx"},
            {".java", "java"}, {".kt", "kotlin"}, {".kts", "kotlin"}, {".cs", "csharp"},
            {".vb", "vbnet"}, {".rb", "ruby"}, {".php", "php"}, {".go", "go"}, {".rs", "rust"},
            {".swift", "swift"}, {".dart", "dart"}, {".scala", "scala"}, {".lua", "lua"},
            {".pl", "perl"}, {".pm", "perl"}, {".t", "perl"}, {".groovy", "groovy"},
            {".clj", "clojure"}, {".cljs", "clojure"}, {".edn", "clojure"}, {".lisp", "lisp"},
            {".scm", "scheme"}, {".rkt", "racket"}, {".hs", "haskell"}, {".lhs", "haskell"},
            {".ml", "ocaml"}, {".mli", "ocaml"}, {".erl", "erlang"}, {".hrl", "erlang"},
            {".ex", "elixir"}, {".exs", "elixir"}, {".r", "r"}, {".jl", "julia"},
            {".mat", "matlab"}, {".m", "matlab"},
            {".c", "c"}, {".h", "c"}, {".cpp", "cpp"}, {".cc", "cpp"}, {".cxx", "cpp"},
            {".hpp", "cpp"}, {".hh", "cpp"}, {".hxx", "cpp"}, {".ino", "arduino"},
            {".asm", "assembly"}, {".s", "assembly"},
            {".html", "html"}, {".htm", "html"}, {".xhtml", "html"}, {".xml", "xml"},
            {".svg", "xml"}, {".css", "css"}, {".scss", "scss"}, {".sass", "sass"},
            {".less", "less"}, {".vue", "vue"},
            {".md", "markdown"}, {".markdown", "markdown"}, {".rst", "restructuredtext"},
            {".tex", "latex"}, {".bib", "bibtex"}, {".txt", "text"}, {".adoc", "asciidoc"},
            {".json", "json"}, {".jsonc", "jsonc"}, {".yaml", "yaml"}, {".yml", "yaml"},
            {".toml", "toml"}, {".ini", "ini"}, {".cfg", "ini"}, {".conf", "ini"},
            {".env", "env"}, {".properties", "ini"},
            {".sh", "bash"}, {".bash", "bash"}, {".zsh", "bash"}, {".ksh", "bash"},
            {".fish", "fish"}, {".ps1", "powershell"}, {".psm1", "powershell"},
            {".bat", "batch"}, {".cmd", "batch"},
            {".sql", "sql"}, {".sqlite", "sql"}, {".csv", "csv"}, {".tsv", "tsv"},
            {"dockerfile", "docker"}, {".dockerfile", "docker"}, {".cshtml", "razor"},
            {".razor", "razor"}, {".gradle", "gradle"}, {".make", "makefile"},
            {"makefile", "makefile"}, {"cmakelists.txt", "cmake"}, {".cmake", "cmake"},
            {"package.json", "json"}, {"composer.json", "json"}, {"pom.xml", "xml"},
            {".log", "log"}, {".lock", "text"}
        };

        #region Preview Processing (Supporting both {{xxc}} and backtick file listing)

        private void BtnCheckPreview_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (this._selectedTemplate == null)
            {
                return;
            }

            Sys.String processed = this.GenerateProcessedContent(this._selectedTemplate);
            this.TxtProcessedPreview.Text = processed;
            this.TxtProcessedPreview.Visibility = Win.Visibility.Visible;
        }

        private async void BtnCopyResult_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (this._selectedTemplate == null)
            {
                return;
            }

            Sys.String processed = this.GenerateProcessedContent(this._selectedTemplate);

            try
            {
                await TextCopy.ClipboardService.SetTextAsync(processed);

                if (this.ItemsVariables.ItemsSource is Coll.List<VariableViewModel> variables)
                {
                    foreach (VariableViewModel variable in variables)
                    {
                        variable.CurrentValue = Sys.String.Empty;
                    }
                }

                await this.ClearUploadedDataAsync().ConfigureAwait(true);
                this.LoadFileManager();

                this.ShowToast("Tersalin, Variabel & File dibersihkan");
            }
            catch (Sys.Exception ex)
            {
                Sys.Console.WriteLine(Sys.String.Concat("Copy Error: ", ex));
                this.ShowToast(Sys.String.Concat("Gagal menyalin: ", ex.Message), true);
            }
        }

        private Sys.String GenerateProcessedContent(Models.Template template)
        {
            Sys.String content = template.Content ?? "";

            if (content.Contains("{{xxc}}"))
            {
                Text.StringBuilder sb = new Text.StringBuilder();
                if (IO.Directory.Exists(_uploadDir))
                {
                    sb.AppendLine("<folder_tree>");
                    sb.AppendLine(this.GenerateTree(_uploadDir));
                    sb.AppendLine("</folder_tree>\n");
                    sb.AppendLine(this.GetCodeTemplate(_uploadDir));
                }
                else
                {
                    sb.AppendLine("Error: Directory not found.");
                }
                content = content.Replace("{{xxc}}", sb.ToString());
            }

            if (content.Contains("{{file}}"))
            {
                Sys.String fileList = IO.Directory.Exists(_uploadDir)
                    ? this.ListUploadedFiles(_uploadDir)
                    : "(tidak ada file)";

                content = content.Replace("{{file}}", fileList);
            }

            Coll.List<VariableViewModel>? variables = this.ItemsVariables.ItemsSource as Coll.List<VariableViewModel>;

            if (variables == null)
            {
                return content;
            }

            foreach (VariableViewModel variable in variables)
            {
                Sys.String safeName = variable.Name.Replace(@"\", @"\\").Replace("[", @"\[").Replace("]", @"\]");
                Sys.String pattern = Sys.String.Concat(@"\{\{\s*", safeName, @"\s*\}\}");

                Sys.String valueToUse = Sys.String.IsNullOrWhiteSpace(variable.CurrentValue)
                                        ? (variable.OriginalValue ?? "")
                                        : variable.CurrentValue;

                content = Sys.Text.RegularExpressions.Regex.Replace(
                    content,
                    pattern,
                    valueToUse,
                    Sys.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return content;
        }

        private Sys.String GenerateTree(Sys.String dirPath, Sys.String prefix = "")
        {
            Text.StringBuilder treeBuilder = new Text.StringBuilder();
            if (!IO.Directory.Exists(dirPath))
            {
                return "Direktori tidak ditemukan.";
            }

            Coll.List<Sys.String> directories = IO.Directory.EnumerateDirectories(dirPath).OrderBy(d => d).ToList();
            Coll.List<Sys.String> files = IO.Directory.EnumerateFiles(dirPath).OrderBy(f => f).ToList();

            for (Sys.Int32 i = 0; i < directories.Count; i++)
            {
                Sys.String d = directories[i];
                Sys.Boolean isLast = (i == directories.Count - 1) && (files.Count == 0);
                treeBuilder.Append($"{prefix}{(isLast ? "└──" : "├──")} {IO.Path.GetFileName(d)}/\n");
                Sys.String newPrefix = prefix + (isLast ? "    " : "│   ");
                treeBuilder.Append(GenerateTree(d, newPrefix));
            }

            for (Sys.Int32 i = 0; i < files.Count; i++)
            {
                Sys.String f = files[i];
                Sys.Boolean isLast = (i == files.Count - 1);
                treeBuilder.Append($"{prefix}{(isLast ? "└──" : "├──")} {IO.Path.GetFileName(f)}\n");
            }

            return treeBuilder.ToString();
        }

        private Sys.String ListUploadedFiles(Sys.String rootDir)
        {
            if (!IO.Directory.Exists(rootDir))
            {
                return "";
            }

            Coll.IEnumerable<Sys.String> filenames = IO.Directory.EnumerateFiles(rootDir, "*", IO.SearchOption.AllDirectories)
                                        .Select(IO.Path.GetFileName)
                                        .Where(name => name != null)
                                        .OrderBy(name => name);

            return Sys.String.Join(", ", filenames.Select(name => $"`{name}`"));
        }

        private Sys.String GetCodeTemplate(Sys.String rootDir)
        {
            Text.StringBuilder entries = new Text.StringBuilder();
            if (!IO.Directory.Exists(rootDir))
            {
                return "";
            }

            Coll.IEnumerable<Sys.String> allFiles = IO.Directory.EnumerateFiles(rootDir, "*", IO.SearchOption.AllDirectories).OrderBy(f => f);

            foreach (Sys.String fullPath in allFiles)
            {
                try
                {
                    IO.FileInfo fi = new IO.FileInfo(fullPath);
                    if (fi.Length > MAX_FILE_SIZE_BYTES)
                    {
                        continue;
                    }

                    Sys.String fname = IO.Path.GetFileName(fullPath);
                    Sys.String ext = IO.Path.GetExtension(fname).ToLower();
                    Sys.String lang = _langMap.GetValueOrDefault(ext, "text");

                    Sys.String fileContent;
                    using (var stream = new IO.FileStream(
                        fullPath,
                        IO.FileMode.Open,
                        IO.FileAccess.Read,
                        IO.FileShare.Read,
                        FILE_BUFFER_SIZE,
                        IO.FileOptions.SequentialScan))
                    using (var reader = new IO.StreamReader(stream, Sys.Text.Encoding.UTF8))
                    {
                        fileContent = reader.ReadToEnd().TrimEnd();
                    }

                    entries.AppendLine($"<{fname}>\n");
                    entries.AppendLine($"```{lang}\n{fileContent}\n```\n");
                    entries.AppendLine($"</{fname}>\n");
                }
                catch
                {
                    continue;
                }
            }
            return entries.ToString().TrimEnd();
        }

        #endregion
    }
}