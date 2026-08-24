using System.Collections.ObjectModel;
using System.IO;
using MarkdownEditor.Core.Documents;

namespace MarkdownEditor.App;

public sealed class FolderTreeNode
{
    private bool _childrenLoaded;

    private FolderTreeNode(string fullPath, bool isDirectory, bool isPlaceholder = false)
    {
        FullPath = fullPath;
        IsDirectory = isDirectory;
        IsPlaceholder = isPlaceholder;
        Name = isPlaceholder
            ? string.Empty
            : Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        if (string.IsNullOrEmpty(Name))
        {
            Name = fullPath;
        }

        if (isDirectory && !isPlaceholder)
        {
            Children.Add(new FolderTreeNode(string.Empty, isDirectory: false, isPlaceholder: true));
        }
    }

    public string Name { get; }

    public string FullPath { get; }

    public bool IsDirectory { get; }

    public bool IsPlaceholder { get; }

    public string Icon => IsDirectory ? "▸" : "#";

    public bool IsExpanded { get; set; }

    public ObservableCollection<FolderTreeNode> Children { get; } = [];

    public static FolderTreeNode CreateRoot(string path)
    {
        var root = new FolderTreeNode(Path.GetFullPath(path), isDirectory: true)
        {
            IsExpanded = true,
        };
        root.LoadChildren();
        return root;
    }

    public void LoadChildren()
    {
        if (!IsDirectory || _childrenLoaded)
        {
            return;
        }

        _childrenLoaded = true;
        Children.Clear();

        try
        {
            foreach (var directory in Directory.EnumerateDirectories(FullPath)
                         .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                Children.Add(new FolderTreeNode(directory, isDirectory: true));
            }

            foreach (var file in Directory.EnumerateFiles(FullPath)
                         .Where(MarkdownPath.IsSupported)
                         .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                Children.Add(new FolderTreeNode(file, isDirectory: false));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Children.Clear();
        }
    }

}
