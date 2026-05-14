using System.Text;
using System.Text.RegularExpressions;

namespace Squash.Lib;

public sealed class FilePath : IEquatable<FilePath>, IComparable<FilePath>
{
    private readonly string _path;
 
    public FilePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        
        _path = Normalize(path);
    }
 
    public FilePath(params string[] segments) : this(Path.Combine(segments)) { }
 
    /// <summary>Returns the current working directory (like Path.cwd()).</summary>
    public static FilePath Cwd() => new(Directory.GetCurrentDirectory());
 
    /// <summary>Returns the current user's home directory (like Path.home()).</summary>
    public static FilePath Home() => new(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    public static FilePath From(string s) => new(s);

    /// <summary>Join paths with the / operator, mirroring Python's Path / "child".</summary>
    public static FilePath operator /(FilePath left, FilePath right) => left.JoinPath(right);
    public static FilePath operator /(FilePath left, string   right) => left.JoinPath(new FilePath(right));
    public static FilePath operator /(string   left, FilePath right) => new FilePath(left).JoinPath(right);
 
    /// <summary>Implicit conversion from string for ergonomic use.</summary>
    public static implicit operator FilePath(string s) => new(s);
 
    /// <summary>Explicit conversion back to string.</summary>
    public static explicit operator string(FilePath p) => p._path;
 
    /// <summary>Full normalized path string.</summary>
    public string FullPath => _path;
 
    /// <summary>
    /// All components of the path as an array, similar to Python's Path.parts.
    /// E.g. "/usr/local/bin" → ["/", "usr", "local", "bin"]
    /// </summary>
    public string[] Parts
    {
        get
        {
            var parts  = new List<string>();
            var anchor = Anchor;
            if (!string.IsNullOrEmpty(anchor))
            {
                parts.Add(anchor);
            }
            
            var rel = _path[anchor.Length..].Trim(Path.DirectorySeparatorChar);
            if (!string.IsNullOrEmpty(rel))
            {
                parts.AddRange(rel.Split(Path.DirectorySeparatorChar));
            }
            
            return parts.ToArray();
        }
    }
 
    /// <summary>Drive letter or UNC host (empty on Unix).</summary>
    public string Drive => Path.GetPathRoot(_path) is { } r
        ? r is [_, ':', ..] ? r[..2] : ""
        : "";
 
    /// <summary>Root separator, e.g. "/" or "\".</summary>
    public string Root
    {
        get
        {
            var r = Path.GetPathRoot(_path) ?? "";
            if (r is [_, ':', ..])
            {
                return r.Length > 2 ? r[2..] : "";
            }
            
            return r.Length > 0 ? r[..1] : "";
        }
    }
 
    /// <summary>Drive + Root concatenated (like Python's Path.anchor).</summary>
    public string Anchor => Drive + Root;
 
    /// <summary>Immediate parent directory.</summary>
    public FilePath Parent
    {
        get
        {
            var dir = System.IO.Path.GetDirectoryName(_path);
            return string.IsNullOrEmpty(dir) ? new FilePath(".") : new FilePath(dir);
        }
    }
 
    /// <summary>
    /// Sequence of logical ancestors, closest first.
    /// E.g. "/a/b/c" → ["/a/b", "/a", "/"]
    /// </summary>
    public IEnumerable<FilePath> Parents
    {
        get
        {
            var current = Parent;
            while (current._path != "." && current._path != current.Parent._path)
            {
                yield return current;
                
                current = current.Parent;
            }
            
            yield return current;
        }
    }
 
    /// <summary>Final component (file or directory name).</summary>
    public string Name => Path.GetFileName(_path);
 
    /// <summary>Name without the last extension.</summary>
    public string Stem => Path.GetFileNameWithoutExtension(_path);
 
    /// <summary>Last extension including the dot (e.g. ".txt"), or empty string.</summary>
    public string Suffix => Path.GetExtension(_path) ?? "";
 
    /// <summary>All extensions in order (e.g. ".tar.gz" → [".tar", ".gz"]).</summary>
    public string[] Suffixes
    {
        get
        {
            var name      = Name;
            var exts      = new List<string>();
            var remainder = name;
            
            string ext;
            while (!string.IsNullOrEmpty(ext = Path.GetExtension(remainder)))
            {
                exts.Insert(0, ext);
                remainder = Path.GetFileNameWithoutExtension(remainder);
            }
            
            return exts.ToArray();
        }
    }
 
    /// <summary>True if the path points to an existing file or directory.</summary>
    public bool Exists() => File.Exists(_path) || Directory.Exists(_path);
 
    /// <summary>True if the path is an existing regular file.</summary>
    public bool IsFile() => File.Exists(_path);
 
    /// <summary>True if the path is an existing directory.</summary>
    public bool IsDir() => Directory.Exists(_path);
 
    /// <summary>True if the path is an absolute path.</summary>
    public bool IsAbsolute() => Path.IsPathRooted(_path);
 
    /// <summary>Join this path with one or more child segments.</summary>
    public FilePath JoinPath(params string[] others)
    {
        var all = new[] { _path }.Concat(others).ToArray();
        
        return new FilePath(Path.Combine(all));
    }
 
    public FilePath JoinPath(params FilePath[] others) => JoinPath(others.Select(p => p._path).ToArray());
 
    /// <summary>Return a new path with the name changed.</summary>
    public FilePath WithName(string newName)
    {
        if (string.IsNullOrEmpty(Name))
        {
            throw new InvalidOperationException("Path has no name component.");
        }
        
        return Parent / newName;
    }
 
    /// <summary>Return a new path with the stem changed (extension preserved).</summary>
    public FilePath WithStem(string newStem) => WithName(newStem + Suffix);
 
    /// <summary>Return a new path with the suffix changed (or removed if empty).</summary>
    public FilePath WithSuffix(string newSuffix)
    {
        if (!string.IsNullOrEmpty(newSuffix) && newSuffix[0] != '.')
        {
            throw new ArgumentException("Suffix must start with '.'.", nameof(newSuffix));
        }
        
        return WithName(Stem + newSuffix);
    }
 
    /// <summary>
    /// Make this path absolute. Resolves symlinks and ".." segments
    /// (like Python's Path.resolve()).
    /// </summary>
    public FilePath Resolve()
    {
        var full = System.IO.Path.GetFullPath(_path);
        // Optionally resolve symlinks on platforms that support it.
        try
        {
            full = new FileInfo(full).FullName;
        } catch { }
        
        return new FilePath(full);
    }
 
    /// <summary>
    /// Make this path relative to <paramref name="other"/>
    /// (like Python's Path.relative_to()).
    /// </summary>
    public FilePath RelativeTo(FilePath other)
    {
        var abs      = Resolve()._path;
        var otherAbs = other.Resolve()._path;
        if (!abs.StartsWith(otherAbs, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"'{abs}' is not relative to '{otherAbs}'.");
        }
        
        var rel = abs[otherAbs.Length..].TrimStart(System.IO.Path.DirectorySeparatorChar);
        
        return new FilePath(rel.Length > 0 ? rel : ".");
    }
 
    /// <summary>True if this path is relative to <paramref name="other"/>.</summary>
    public bool IsRelativeTo(FilePath other)
    {
        try
        {
            RelativeTo(other);
            return true;
        }
        catch
        {
            return false;
        }
    }
 
    /// <summary>
    /// Glob the given relative pattern, yielding all matching paths.
    /// Supports * and ** (recursive) like Python's Path.glob().
    /// </summary>
    public IEnumerable<FilePath> Glob(string pattern)
    {
        var recursive = pattern.Contains("**");
        var option    = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var regexPattern = GlobToRegex(pattern);
        var regex        = new Regex(regexPattern, RegexOptions.IgnoreCase);
 
        var baseDir = IsDir() ? _path : Parent._path;
        if (!Directory.Exists(baseDir))
        {
            yield break;
        }
 
        foreach (string entry in Directory.EnumerateFileSystemEntries(baseDir, "*", option))
        {
            var rel = entry.Substring(baseDir.Length).TrimStart(System.IO.Path.DirectorySeparatorChar);
            if (regex.IsMatch(rel))
            {
                yield return new FilePath(entry);
            }
        }
    }
 
    /// <summary>Recursively glob, equivalent to Python's Path.rglob(pattern).</summary>
    public IEnumerable<FilePath> RGlob(string pattern) => Glob("**/" + pattern);
 
    private static string GlobToRegex(string glob)
    {
        var sb = new StringBuilder("^");
        int i = 0;
        while (i < glob.Length)
        {
            switch (glob[i])
            {
                case '*' when i + 1 < glob.Length && glob[i + 1] == '*':
                {
                    sb.Append(".*");
                    i += 2;
                    if (i < glob.Length && (glob[i] == '/' || glob[i] == '\\')) i++;
                    break;
                }
                case '*':
                    sb.Append(@"[^/\\]*"); i++;
                    break;
                case '?':
                    sb.Append(@"[^/\\]"); i++;
                    break;
                case '/':
                    sb.Append(@"[\\/]"); i++;
                    break;
                default:
                    sb.Append(Regex.Escape(glob[i].ToString())); i++;
                    break;
            }
        }
        
        sb.Append('$');
        
        return sb.ToString();
    }
 
    /// <summary>
    /// Yield immediate children of this directory (like Python's Path.iterdir()).
    /// </summary>
    public IEnumerable<FilePath> IterDir()
    {
        if (!IsDir())
        {
            throw new DirectoryNotFoundException($"Not a directory: {_path}");
        }
        
        return Directory.EnumerateFileSystemEntries(_path).Select(e => new FilePath(e));
    }
 
    /// <summary>Create this directory (and optionally parents). Like Python's Path.mkdir().</summary>
    public void Mkdir(bool parents = false, bool existOk = false)
    {
        if (Directory.Exists(_path))
        {
            if (!existOk) throw new IOException($"Directory already exists: {_path}");
            return;
        }

        Directory.CreateDirectory(_path);

        if (!parents && !Directory.Exists(Parent._path))
        {
            throw new DirectoryNotFoundException($"Parent directory does not exist: {Parent._path}");
        }
    }
 
    /// <summary>Remove this directory (must be empty). Like Python's Path.rmdir().</summary>
    public void Rmdir() => Directory.Delete(_path, recursive: false);
 
    /// <summary>Remove this file. Like Python's Path.unlink().</summary>
    public void Unlink(bool missingOk = false)
    {
        if (!Exists())
        {
            if (!missingOk)
            {
                throw new FileNotFoundException($"File not found: {_path}");
            }
            return;
        }
        
        File.Delete(_path);
    }
 
    /// <summary>
    /// Rename (move) the path. Returns a Path pointing to the new location.
    /// Like Python's Path.rename().
    /// </summary>
    public FilePath Rename(FilePath target)
    {
        if (IsDir())
        {
            Directory.Move(_path, target._path);
        }
        else
        {
            File.Move(_path, target._path);
        }
        
        return target;
    }
 
    public FilePath Rename(string target) => Rename(new FilePath(target));
 
    /// <summary>
    /// Rename, overwriting the destination if it exists.
    /// Like Python's Path.replace().
    /// </summary>
    public FilePath Replace(FilePath target)
    {
        if (target.Exists())
        {
            target.Unlink(missingOk: true);
        }
        
        return Rename(target);
    }
 
    public FilePath Replace(string target) => Replace(new FilePath(target));
 
    /// <summary>
    /// Create an empty file or update its timestamp. Like Python's Path.touch().
    /// </summary>
    public void Touch(bool existOk = true)
    {
        if (File.Exists(_path))
        {
            if (!existOk)
            {
                throw new IOException($"File already exists: {_path}");
            }
            
            File.SetLastWriteTimeUtc(_path, DateTime.UtcNow);
        }
        else
        {
            File.WriteAllBytes(_path, Array.Empty<byte>());
        }
    }

    public FileInfo FileInfo() => new(_path);

    /// <summary>Read all text. Like Python's Path.read_text().</summary>
    public string ReadText(Encoding? encoding = null) => File.ReadAllText(_path, encoding ?? Encoding.UTF8);
 
    /// <summary>Write text, overwriting if exists. Like Python's Path.write_text().</summary>
    public void WriteText(string content, Encoding? encoding = null) => File.WriteAllText(_path, content, encoding ?? Encoding.UTF8);
 
    /// <summary>Read all bytes. Like Python's Path.read_bytes().</summary>
    public byte[] ReadBytes() => File.ReadAllBytes(_path);
 
    /// <summary>Write bytes, overwriting if exists. Like Python's Path.write_bytes().</summary>
    public void WriteBytes(byte[] data) => File.WriteAllBytes(_path, data);
 
    /// <summary>Open a StreamReader. Like Python's Path.open() for reading text.</summary>
    public StreamReader OpenText(Encoding? encoding = null) => new(_path, encoding ?? Encoding.UTF8);
 
    /// <summary>Open a StreamWriter. Like Python's Path.open() for writing text.</summary>
    public StreamWriter OpenWrite(bool append = false, Encoding? encoding = null) => new(_path, append, encoding ?? Encoding.UTF8);
 
    /// <summary>Open a raw FileStream.</summary>
    public FileStream Open(FileMode mode, FileAccess access = FileAccess.ReadWrite) => new(_path, mode, access);
    public FileStream Open(FileMode mode, FileAccess access, FileShare fileShare, int bufferSize, bool useAsync)
        => new(_path, mode, access, fileShare, bufferSize, useAsync);
 
    /// <summary>
    /// Returns file/directory info. Like Python's Path.stat().
    /// Use .Length, .LastWriteTime, .Attributes, etc. on the result.
    /// </summary>
    public FileSystemInfo Stat()
    {
        if (IsDir())
        {
            return new DirectoryInfo(_path);
        }
        
        return new FileInfo(_path);
    }
 
    public bool Equals(FilePath? other)
    {
        if (other is null)
        {
            return false;
        }
        
        return string.Equals(
            Resolve()._path, other.Resolve()._path,
            Environment.OSVersion.Platform == PlatformID.Win32NT
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
    }
 
    public override bool Equals(object? obj) => obj is FilePath p && Equals(p);
 
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Resolve()._path);
 
    public int CompareTo(FilePath? other) => string.Compare(_path, other?._path, StringComparison.OrdinalIgnoreCase);
    
    public static FilePath TempDir() => new(Path.GetTempPath());

    public static FilePath TempFile() => new(Path.GetTempFileName());
 
    public static bool operator ==(FilePath? a, FilePath? b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(FilePath? a, FilePath? b) => !(a == b);
    public static bool operator <(FilePath?  a, FilePath? b) => a.CompareTo(b) < 0;
    public static bool operator >(FilePath?  a, FilePath? b) => a.CompareTo(b) > 0;
    public static bool operator <=(FilePath? a, FilePath? b) => a.CompareTo(b) <= 0;
    public static bool operator >=(FilePath? a, FilePath? b) => a.CompareTo(b) >= 0;
 
    /// <summary>The normalized path string (same as FullPath).</summary>
    public override string ToString() => _path;
 
    /// <summary>POSIX-style representation using forward slashes.</summary>
    public string AsPosix() => _path.Replace('\\', '/');
    
    private static string Normalize(string raw)
    {
        // Collapse redundant separators; keep trailing sep only for root.
        var combined = raw.Replace('/', Path.DirectorySeparatorChar);
        // Preserve UNC prefix on Windows
        var isUnc  = combined.StartsWith(@"\\");
        var parts  = combined.Split([Path.DirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        var joined = string.Join(Path.DirectorySeparatorChar.ToString(), parts);
        if (isUnc)
        {
            joined = @"\\" + joined;
        }
        else if (combined.Length > 0 && combined[0] == System.IO.Path.DirectorySeparatorChar)
        {
            joined = Path.DirectorySeparatorChar + joined;
        }
        
        return joined;
    }
}
