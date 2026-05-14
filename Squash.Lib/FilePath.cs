using System.Text;
using System.Text.RegularExpressions;

namespace Squash.Lib;

public sealed class FilePath : IEquatable<FilePath>, IComparable<FilePath>
{
    private string?   _resolvedPath;
    private string[]? _parts;
    private string[]? _suffixes;
    
    private readonly string _path;

    public FilePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        _path = Normalize(path);
    }

    public FilePath(params string[] segments) : this(Path.Combine(segments)) { }

    public static FilePath Cwd() => new(Directory.GetCurrentDirectory());
    public static FilePath Home() => new(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    public static FilePath From(string s) => new(s);

    public static FilePath operator /(FilePath left, FilePath right) => left.JoinPath(right);
    public static FilePath operator /(FilePath left, string   right) => left.JoinPath(new FilePath(right));
    public static FilePath operator /(string   left, FilePath right) => new FilePath(left).JoinPath(right);

    public static implicit operator FilePath(string s) => new(s);
    public static explicit operator string(FilePath p) => p._path;

    public string FullPath => _path;

    public string[] Parts => _parts ??= CalculateParts();

    private string[] CalculateParts()
    {
        var partsList = new List<string>();
        var anchor    = Anchor;
        if (!string.IsNullOrEmpty(anchor))
        {
            partsList.Add(anchor);
        }

        var rel = _path[anchor.Length..].Trim(Path.DirectorySeparatorChar);
        if (!string.IsNullOrEmpty(rel))
        {
            partsList.AddRange(rel.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries));
        }

        return partsList.ToArray();
    }

    public string Drive => Path.GetPathRoot(_path) is [_, ':', ..] r ? r[..2] : "";

    public string Root
    {
        get
        {
            var r = Path.GetPathRoot(_path) ?? "";
            if (r.Length >= 2 && r[1] == ':')
            {
                return r.Length > 2 ? r[2..] : "";
            }
            return r.Length > 0 ? r[..1] : "";
        }
    }

    public string Anchor => Drive + Root;

    public FilePath Parent
    {
        get
        {
            var dir = Path.GetDirectoryName(_path);
            
            return string.IsNullOrEmpty(dir) ? new FilePath(".") : new FilePath(dir);
        }
    }

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

    public string Name => Path.GetFileName(_path);
    public string Stem => Path.GetFileNameWithoutExtension(_path);
    public string Suffix => Path.GetExtension(_path) ?? "";

    public string[] Suffixes => _suffixes ??= CalculateSuffixes();

    private string[] CalculateSuffixes()
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

    public bool Exists() => File.Exists(_path) || Directory.Exists(_path);

    public bool IsFile() => File.Exists(_path);

    public bool IsDir() => Directory.Exists(_path);

    public bool IsAbsolute() => Path.IsPathRooted(_path);

    public FilePath JoinPath(params string[] others)
    {
        if (others.Length == 0)
        {
            return this;
        }
        
        var combined = Path.Combine([_path, ..others]);
        
        return new FilePath(combined);
    }

    public FilePath JoinPath(params FilePath[] others) => JoinPath(others.Select(p => p._path).ToArray());

    public FilePath WithName(string newName) => Parent / newName;

    public FilePath WithStem(string newStem) => WithName(newStem + Suffix);
    
    public FilePath WithSuffix(string newSuffix)
    {
        if (!string.IsNullOrEmpty(newSuffix) && newSuffix[0] != '.')
        {
            throw new ArgumentException("Suffix must start with '.'.", nameof(newSuffix));
        }
        
        return WithName(Stem + newSuffix);
    }

    public FilePath Resolve() => new(Path.GetFullPath(_path));

    private string GetResolvedPath() => _resolvedPath ??= Path.GetFullPath(_path);

    public FilePath RelativeTo(FilePath other)
    {
        try
        {
            return new FilePath(Path.GetRelativePath(other._path, _path));
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Could not make '{_path}' relative to '{other._path}'.", ex);
        }
    }

    public bool IsRelativeTo(FilePath other)
    {
        var rel = Path.GetRelativePath(other._path, _path);
        
        return !rel.StartsWith("..") && !Path.IsPathRooted(rel);
    }

    public bool Match(string pattern)
    {
        var regexPattern = GlobToRegex(pattern);
        
        return Regex.IsMatch(_path, regexPattern, RegexOptions.IgnoreCase);
    }

    public IEnumerable<FilePath> Glob(string pattern)
    {
        var recursive = pattern.Contains("**");
        var option    = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var regex     = new Regex(GlobToRegex(pattern), RegexOptions.IgnoreCase);

        var baseDir = IsDir() ? _path : Parent.Exists() ? Parent._path : ".";
        if (!Directory.Exists(baseDir))
        {
            yield break;
        }

        foreach (string entry in Directory.EnumerateFileSystemEntries(baseDir, "*", option))
        {
            var rel = Path.GetRelativePath(baseDir, entry);
            if (regex.IsMatch(rel))
            {
                yield return new FilePath(entry);
            }
        }
    }

    public IEnumerable<FilePath> RGlob(string pattern) => Glob("**/" + pattern);

    private static string GlobToRegex(string glob)
    {
        var sb = new StringBuilder("^");
        var i  = 0;
        while (i < glob.Length)
        {
            switch (glob[i])
            {
                case '*' when i + 1 < glob.Length && glob[i + 1] == '*':
                    sb.Append(".*");
                    i += 2;
                    if (i < glob.Length && (glob[i] == '/' || glob[i] == '\\'))
                    {
                        i++;
                    }
                    break;
                case '*':
                    sb.Append(@"[^/\\]*");
                    i++;
                    break;
                case '?':
                    sb.Append(@"[^/\\]");
                    i++;
                    break;
                case '/':
                    sb.Append(@"[\\/]");
                    i++;
                    break;
                default:
                    sb.Append(Regex.Escape(glob[i].ToString()));
                    i++;
                    break;
            }
        }
        sb.Append('$');
        
        return sb.ToString();
    }

    public IEnumerable<FilePath> IterDir()
    {
        if (!IsDir())
        {
            throw new DirectoryNotFoundException($"Not a directory: {_path}");
        }
        
        return Directory.EnumerateFileSystemEntries(_path).Select(e => new FilePath(e));
    }

    public void Mkdir(bool parents = false, bool existOk = false)
    {
        if (Directory.Exists(_path))
        {
            if (!existOk) throw new IOException($"Directory already exists: {_path}");
            return;
        }

        if (parents)
        {
            Directory.CreateDirectory(_path);
        }
        else
        {
            if (!Directory.Exists(Parent._path))
            {
                throw new DirectoryNotFoundException($"Parent directory does not exist: {Parent._path}");
            }
            
            Directory.CreateDirectory(_path);
        }
    }

    public void Rmdir() => Directory.Delete(_path, recursive: false);

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

    public FilePath Replace(FilePath target)
    {
        if (IsDir())
        {
            Directory.Move(_path, target._path);
        }
        else
        {
            File.Move(_path, target._path, overwrite: true);
        }
        
        return target;
    }

    public FilePath Replace(string target) => Replace(new FilePath(target));

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
            File.WriteAllBytes(_path, []);
        }
    }

    public FileInfo FileInfo() => new(_path);
    public FileSystemInfo Stat() => IsDir() ? new DirectoryInfo(_path) : new FileInfo(_path);

    public string ReadText(Encoding? encoding = null) => File.ReadAllText(_path, encoding ?? Encoding.UTF8);
    public Task<string> ReadTextAsync(Encoding? encoding = null, CancellationToken ct = default) => File.ReadAllTextAsync(_path, encoding ?? Encoding.UTF8, ct);
    public void WriteText(string content, Encoding? encoding = null) => File.WriteAllText(_path, content, encoding ?? Encoding.UTF8);
    public Task WriteTextAsync(string content, Encoding? encoding = null, CancellationToken ct = default) => File.WriteAllTextAsync(_path, content, encoding ?? Encoding.UTF8, ct);

    public byte[] ReadBytes() => File.ReadAllBytes(_path);
    public Task<byte[]> ReadBytesAsync(CancellationToken ct = default) => File.ReadAllBytesAsync(_path, ct);
    public void WriteBytes(byte[] data) => File.WriteAllBytes(_path, data);
    public Task WriteBytesAsync(byte[] data, CancellationToken ct = default) => File.WriteAllBytesAsync(_path, data, ct);

    public StreamReader OpenText(Encoding? encoding = null) => new(_path, encoding ?? Encoding.UTF8);
    public StreamWriter OpenWrite(bool append = false, Encoding? encoding = null) => new(_path, append, encoding ?? Encoding.UTF8);
    public FileStream Open(FileMode mode, FileAccess access = FileAccess.ReadWrite) => new(_path, mode, access);
    public FileStream Open(FileMode mode, FileAccess access, FileShare fileShare, int bufferSize, bool useAsync) => new(_path, mode, access, fileShare, bufferSize, useAsync);

    public bool Equals(FilePath? other)
    {
        if (other is null)
        {
            return false;
        }
        var comparison = Environment.OSVersion.Platform == PlatformID.Win32NT ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        
        return string.Equals(GetResolvedPath(), other.GetResolvedPath(), comparison);
    }

    public override bool Equals(object? obj) => obj is FilePath p && Equals(p);
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(GetResolvedPath());
    public int CompareTo(FilePath? other) => string.Compare(_path, other?._path, StringComparison.OrdinalIgnoreCase);

    public static FilePath TempDir() => new(Path.GetTempPath());
    public static FilePath TempFile() => new(Path.GetTempFileName());

    public static bool operator ==(FilePath? a, FilePath? b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(FilePath? a, FilePath? b) => !(a == b);
    public static bool operator <(FilePath?  a, FilePath? b) => a is not null ? a.CompareTo(b) < 0 : b is not null;
    public static bool operator >(FilePath?  a, FilePath? b) => a is not null && a.CompareTo(b) > 0;
    public static bool operator <=(FilePath? a, FilePath? b) => a is null || a.CompareTo(b) <= 0;
    public static bool operator >=(FilePath? a, FilePath? b) => a is null ? b is null : a.CompareTo(b) >= 0;

    public override string ToString() => _path;
    public string AsPosix() => _path.Replace('\\', '/');

    private static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ".";
        }
        
        var replaced = raw.Replace('/', Path.DirectorySeparatorChar);
        if (replaced.Length > 1 && replaced.EndsWith(Path.DirectorySeparatorChar))
        {
            var root = Path.GetPathRoot(replaced);
            if (replaced != root)
            {
                replaced = replaced.TrimEnd(Path.DirectorySeparatorChar);
            }
        }
        
        return replaced;
    }
}
