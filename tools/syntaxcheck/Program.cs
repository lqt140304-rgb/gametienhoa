using System; using System.IO; using System.Linq; using Microsoft.CodeAnalysis; using Microsoft.CodeAnalysis.CSharp;
// usage: syntaxcheck <repoRoot> <changedFilesListFile>   (see run.sh)
var root = args[0];
var all = Directory.GetFiles(Path.Combine(root, "Assets/_Project"), "*.cs", SearchOption.AllDirectories).ToArray();
var changed = File.ReadAllLines(args[1]).Select(l => Path.GetFullPath(Path.Combine(root, l.Trim()))).ToHashSet();
var opts = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: new[] { "UNITY_EDITOR", "ENABLE_INPUT_SYSTEM" });
var trees = all.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), opts, f)).ToList();
var refs = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location), MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll")) };
var comp = CSharpCompilation.Create("lint", trees, refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
int syntax = 0;
foreach (var d in comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error && d.Location.SourceTree != null && changed.Contains(Path.GetFullPath(d.Location.SourceTree.FilePath))))
{
    var syn = d.Location.SourceTree.GetDiagnostics().Any(x => x.Id == d.Id && x.Location == d.Location);
    if (syn) syntax++;
    Console.WriteLine((syn ? "SYNTAX " : "SEMANTIC ") + Path.GetRelativePath(root, d.Location.SourceTree.FilePath) + ":" + (d.Location.GetLineSpan().StartLinePosition.Line + 1) + " " + d.Id + " " + d.GetMessage());
}
Console.WriteLine("syntax errors in changed files: " + syntax);
