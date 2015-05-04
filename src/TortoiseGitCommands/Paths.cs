using System;
using System.IO;
using System.Linq;
using EnvDTE80;

namespace TortoiseGitCommands
{
    public enum RefreshMode
    {
        All = 0,
        Solution = 1,
        ActiveDocument = 2,
    }

    public interface IPaths
    {
        string GitRepoPath { get; }
        string SolutionPath { get; }
        string ActiveFilePath { get; }

        void Refresh(RefreshMode mode = RefreshMode.All);
    }

    public class Paths : IPaths
    {
        public string GitRepoPath => _gitRepoPath.Value;
        private Lazy<string> _gitRepoPath;

        public string SolutionPath => _solutionPath.Value;
        private Lazy<string> _solutionPath;

        public string ActiveFilePath => _activeFilePath.Value;
        private Lazy<string> _activeFilePath;

        private DTE2 Environment { get; }

        public Paths(DTE2 environment)
        {
            Environment = environment;
            Refresh();
        }

        public void Refresh(RefreshMode mode = RefreshMode.All)
        {
            if (_gitRepoPath == null || _gitRepoPath.IsValueCreated)
            {
                _gitRepoPath = new Lazy<string>(() => GetGitRepoPath(Environment));
            }

            if (_solutionPath == null || _solutionPath.IsValueCreated)
            {
                _solutionPath = new Lazy<string>(() => GetSolutionPath(Environment));
            }

            if (_activeFilePath == null || _activeFilePath.IsValueCreated)
            {
                _activeFilePath = new Lazy<string>(() => GetActiveFilePath(Environment));
            }
        }

        public static string GetGitRepoPath(DTE2 environment)
        {
            var path = GetSolutionPath(environment);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var di = new DirectoryInfo(path);
            do
            {
                if (Directory.EnumerateDirectories(di.FullName, ".git").Any())
                {
                    return di.FullName;
                }
                di = di.Parent;
            } while (di != null);

            return null;
        }

        public static string GetSolutionPath(DTE2 environment) => environment.Solution != null &&
                                                                  environment.Solution?.IsOpen == true &&
                                                                  !String.IsNullOrEmpty(environment.Solution?.FullName)
                                                                      ? Path.GetDirectoryName(environment.Solution.FullName)
                                                                      : null;

        public static string GetActiveFilePath(DTE2 environment) => GetExactPathName(environment?.ActiveDocument?.FullName);

        private static string GetExactPathName(string pathName)
        {
            if (String.IsNullOrEmpty(pathName) ||
                (!File.Exists(pathName) && !Directory.Exists(pathName)))
            {
                return pathName;
            }

            var di = new DirectoryInfo(pathName);
            return di.Parent != null
                       ? Path.Combine(GetExactPathNameCore(di.Parent),
                                      di.Parent.EnumerateFileSystemInfos(di.Name).First().Name)
                       : di.Name; //.ToUpperInvariant();
        }

        private static string GetExactPathNameCore(DirectoryInfo di)
        {
            return di.Parent != null
                       ? Path.Combine(GetExactPathNameCore(di.Parent),
                                      di.Parent.EnumerateFileSystemInfos(di.Name).First().Name)
                       : di.Name; //.ToUpperInvariant();
        }
    }
}