using System;
using System.Diagnostics;
using System.IO;
using EnvDTE80;
using Process = System.Diagnostics.Process;

namespace TortoiseGitCommands
{
    public enum TortoiseGitCommands
    {
        Status,
        Log,
        Commit,
        Blame,
        Diff,
        Revert
    }

    public enum TortoiseGitCommandScope
    {
        Repo,
        Solution,
        File
    }

    public interface ITortoiseGitRunner
    {
        void ExecuteCommand(TortoiseGitCommandScope scope, TortoiseGitCommands command);
    }

    public class TortoiseGitRunner : ITortoiseGitRunner
    {
        private const string TortoiseGitProcx64 = @"C:\Program Files\TortoiseGit\bin\TortoiseGitProc.exe";
        private const string TortoiseGitProcx86 = @"C:\Program Files (x86)\TortoiseGit\bin\TortoiseGitProc.exe";

        public static string TortoiseGitPath => _tortoiseGitPath.Value;
        private static readonly Lazy<string> _tortoiseGitPath = new Lazy<string>(() =>
        {
            if (File.Exists(TortoiseGitProcx64))
            {
                return TortoiseGitProcx64;
            }
            return File.Exists(TortoiseGitProcx86)
                       ? TortoiseGitProcx86
                       : null;
        });

        private DTE2 Environment { get; }
        private IPaths Paths { get; }

        public TortoiseGitRunner(DTE2 environment, IPaths paths)
        {
            Environment = environment;
            Paths = paths;
        }

        private const string CommandArgsFormat = "/command:{0} /path:\"{1}\"";
        private const string BlameCommandArgsFormat = "/command:{0} /path:\"{1}\" /line:{2}";

        public void ExecuteCommand(TortoiseGitCommandScope scope, TortoiseGitCommands command)
        {
            var path = GetScopedPath(scope);
            if (String.IsNullOrEmpty(path))
            {
                return;
            }

            string args = null;
            switch (command)
            {
                case TortoiseGitCommands.Status:
                    args = String.Format(CommandArgsFormat, "repostatus", path);
                    break;
                case TortoiseGitCommands.Log:
                    args = String.Format(CommandArgsFormat, "log", path);
                    break;
                case TortoiseGitCommands.Commit:
                    args = String.Format(CommandArgsFormat, "commit", path);
                    break;
                case TortoiseGitCommands.Diff:
                    if (scope != TortoiseGitCommandScope.File)
                    {
                        throw new NotSupportedException();
                    }
                    args = String.Format(CommandArgsFormat, "diff", path);
                    break;
                case TortoiseGitCommands.Revert:
                    if (scope != TortoiseGitCommandScope.File)
                    {
                        throw new NotSupportedException();
                    }
                    args = String.Format(CommandArgsFormat, "revert", path);
                    break;
                case TortoiseGitCommands.Blame:
                    if (scope != TortoiseGitCommandScope.File)
                    {
                        throw new NotSupportedException();
                    }
                    args = String.Format(BlameCommandArgsFormat, "blame", path, GetActiveFileCurrentLine(Environment));
                    break;
            }

            if (args == null)
            {
                return;
            }

            var working = Paths.SolutionPath;
            Process.Start(new ProcessStartInfo
            {
                FileName = TortoiseGitPath,
                Arguments = args,
                UseShellExecute = String.IsNullOrEmpty(working),
                WorkingDirectory = working ?? String.Empty
            });
        }

        private string GetScopedPath(TortoiseGitCommandScope scope)
        {
            switch (scope)
            {
                case TortoiseGitCommandScope.Repo:
                    return Paths.GitRepoPath;
                case TortoiseGitCommandScope.Solution:
                    return Paths.SolutionPath;
                case TortoiseGitCommandScope.File:
                    return Paths.ActiveFilePath;
                default:
                    return null;
            }
        }

        private static int GetActiveFileCurrentLine(DTE2 environment)
        {
            dynamic selection = environment.ActiveDocument?.Selection;
            return selection != null ? selection.CurrentLine : 0;
        }
    }
}
