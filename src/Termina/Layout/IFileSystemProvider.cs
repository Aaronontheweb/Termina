// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Abstracts filesystem access for <see cref="FilePickerNode"/> to enable testability.
/// </summary>
public interface IFileSystemProvider
{
    /// <summary>
    /// Returns all file and directory entries in the given directory, sorted with
    /// directories first (alphabetical) then files (alphabetical).
    /// </summary>
    IReadOnlyList<FileSystemEntry> GetEntries(string directoryPath);

    /// <summary>
    /// Returns true if the given directory exists and is accessible.
    /// </summary>
    bool DirectoryExists(string path);

    /// <summary>
    /// Returns the parent directory path, or null if already at a root.
    /// </summary>
    string? GetParentDirectory(string path);
}

/// <summary>
/// Default <see cref="IFileSystemProvider"/> backed by <see cref="System.IO"/>.
/// </summary>
public sealed class DefaultFileSystemProvider : IFileSystemProvider
{
    public static readonly DefaultFileSystemProvider Instance = new();

    public IReadOnlyList<FileSystemEntry> GetEntries(string directoryPath)
    {
        var dir = new DirectoryInfo(directoryPath);
        if (!dir.Exists)
            return [];

        var entries = new List<FileSystemEntry>();

        try
        {
            foreach (var d in dir.EnumerateDirectories())
            {
                try
                {
                    entries.Add(new FileSystemEntry(d.Name, d.FullName, IsDirectory: true,
                        LastModified: d.LastWriteTimeUtc));
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }

        try
        {
            foreach (var f in dir.EnumerateFiles())
            {
                try
                {
                    entries.Add(new FileSystemEntry(f.Name, f.FullName, IsDirectory: false,
                        Size: f.Length, LastModified: f.LastWriteTimeUtc));
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }

        entries.Sort((a, b) =>
        {
            if (a.IsDirectory != b.IsDirectory)
                return a.IsDirectory ? -1 : 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        return entries;
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public string? GetParentDirectory(string path)
    {
        var parent = Directory.GetParent(path);
        return parent?.FullName;
    }
}
