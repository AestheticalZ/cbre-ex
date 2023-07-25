using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CBRE.Common;

namespace CBRE.Packages.Vpk
{
    internal class VpkPackageStreamSource : IPackageStreamSource
    {
        private readonly VpkDirectory _directory;
        private readonly Dictionary<string, VpkEntry> _entries;
        private readonly Dictionary<ushort, Stream> _streams;
        private readonly Dictionary<string, HashSet<string>> _folders;
        private readonly Dictionary<string, HashSet<string>> _files;

        public VpkPackageStreamSource(VpkDirectory directory)
        {
            _directory = directory;
            _entries = new Dictionary<string, VpkEntry>();
            _streams = new Dictionary<ushort, Stream>();
            _folders = new Dictionary<string, HashSet<string>>();
            _files = new Dictionary<string, HashSet<string>>();
            foreach (VpkEntry entry in directory.GetEntries().OfType<VpkEntry>())
            {
                string fn = entry.FullName;
                _entries.Add(fn, entry);
                string[] split = fn.Split('/');
                string joined = "";
                for (int i = 0; i < split.Length; i++)
                {
                    string sub = split[i];
                    string name = joined.Length == 0 ? sub : joined + '/' + sub;
                    if (i == split.Length - 1)
                    {
                        // File name
                        if (!_files.ContainsKey(joined)) _files.Add(joined, new HashSet<string>());
                        _files[joined].Add(name);
                    }
                    else
                    {
                        // Folder name
                        if (!_folders.ContainsKey(joined)) _folders.Add(joined, new HashSet<string>());
                        if (!_folders[joined].Contains(sub)) _folders[joined].Add(name);
                    }
                    joined = joined.Length == 0 ? sub : joined + '/' + sub;
                }
            }
        }

        private string GetName(string path)
        {
            int idx = path.LastIndexOf('/');
            if (idx < 0) return path;
            return path.Substring(idx + 1);
        }

        private string GetParent(string path)
        {
            int idx = path.LastIndexOf('/');
            if (idx < 0) return "";
            return path.Substring(0, idx);
        }

        public bool HasDirectory(string path)
        {
            return _folders.ContainsKey(path);
        }

        public bool HasFile(string path)
        {
            path = path.ToLowerInvariant();
            int idx = path.LastIndexOf('/');
            string fol = idx >= 0 ? path.Substring(0, idx) : "";
            return _files.ContainsKey(fol) && _files[fol].Contains(path);
        }

        public IEnumerable<string> GetDirectories()
        {
            return _files.Keys;
        }

        public IEnumerable<string> GetFiles()
        {
            return _files.Values.SelectMany(x => x);
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            if (!_folders.ContainsKey(path)) return new string[0];
            return _folders[path].Where(x => x.Length > 0);
        }

        public IEnumerable<string> GetFiles(string path)
        {
            if (!_files.ContainsKey(path)) return new string[0];
            return _files[path];
        }

        public IEnumerable<string> SearchDirectories(string path, string regex, bool recursive)
        {
            IEnumerable<string> files = recursive ? CollectDirectories(path) : GetDirectories(path);
            return files.Where(x => Regex.IsMatch(GetName(x), regex, RegexOptions.IgnoreCase));
        }

        public IEnumerable<string> SearchFiles(string path, string regex, bool recursive)
        {
            IEnumerable<string> files = recursive ? CollectFiles(path) : GetFiles(path);
            return files.Where(x => Regex.IsMatch(GetName(x), regex, RegexOptions.IgnoreCase));
        }

        private IEnumerable<string> CollectDirectories(string path)
        {
            List<string> files = new List<string>();
            if (_folders.ContainsKey(path))
            {
                files.AddRange(_folders[path].Where(x => x.Length > 0));
                files.AddRange(_folders[path].SelectMany(CollectDirectories));
            }
            return files;
        }

        private IEnumerable<string> CollectFiles(string path)
        {
            List<string> files = new List<string>();
            if (_folders.ContainsKey(path))
            {
                files.AddRange(_folders[path].SelectMany(CollectFiles));
            }
            if (_files.ContainsKey(path))
            {
                files.AddRange(_files[path]);
            }
            return files;
        }

        private VpkEntry GetEntry(string path)
        {
            path = path.ToLowerInvariant();
            return _entries.ContainsKey(path) ? _entries[path] : null;
        }

        public Stream OpenFile(string path)
        {
            VpkEntry entry = GetEntry(path);
            if (entry == null) throw new FileNotFoundException();

            lock (this)
            {
                if (!_streams.ContainsKey(entry.ArchiveIndex))
                {
                    FileInfo file = _directory.Chunks[entry.ArchiveIndex];
                    Stream stream = _directory.OpenFile(file);
                    _streams.Add(entry.ArchiveIndex, stream);
                }
            }

            uint offset = entry.ArchiveIndex == VpkDirectory.DirectoryIndex ? _directory.HeaderLength + _directory.TreeLength + entry.EntryOffset : entry.EntryOffset;
            SubStream sub = new SubStream(_streams[entry.ArchiveIndex], offset, entry.EntryLength);
            return new BufferedStream(new VpkEntryStream(entry, sub));
        }

        public void Dispose()
        {
            foreach (KeyValuePair<ushort, Stream> stream in _streams)
            {
                stream.Value.Dispose();
            }
            _streams.Clear();
        }
    }
}