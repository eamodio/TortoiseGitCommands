using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.VisualStudio.Shell;
using TortoiseGitCommands.Polyfills;

namespace TortoiseGitCommands.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PackageCommands
    {
        [Guid(PackageGuids.CmdSetGuidString)]
        private enum CommandIds
        {
            TortoiseGitCommands = 0x1020
        }

        [Guid(PackageGuids.GitCmdSetGuidString)]
        enum GitCommandIds
        {
            Status = 0x0100,
            Log = 0x0200,
            Commit = 0x0300,
        }

        [Guid(PackageGuids.GitFileCmdSetGuidString)]
        enum GitFileCommandIds
        {
            LogFile = 0x0100,
            BlameFile = 0x0200,
            DiffFile = 0x0300,
            RevertFile = 0x0400,
        }

        private static readonly Dictionary<int, string> CommandTextMap = new Dictionary<int, string>
        {
            {(int)GitFileCommandIds.LogFile, "Log {0}..."},
            {(int)GitFileCommandIds.BlameFile, "Blame {0}..."},
            {(int)GitFileCommandIds.DiffFile, "Diff {0}..."},
            {(int)GitFileCommandIds.RevertFile, "Revert {0}..."}
        };

        private TortoiseGitCommandsPackage Package { get; }
        private IServiceProvider ServiceProvider => Package;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PackageCommands Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(TortoiseGitCommandsPackage package)
        {
            Instance = new PackageCommands(package);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageCommands"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private PackageCommands(TortoiseGitCommandsPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            Package = package;

            var commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                SetupCommands(commandService);
            }
        }

        private void SetupCommands(OleMenuCommandService commandService)
        {
            var guid = typeof(CommandIds).GUID;

            var commandId = new CommandID(guid, (int)CommandIds.TortoiseGitCommands);
            var command = new OleMenuCommand(null, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            SetupGitCommands(commandService);
        }

        private void SetupGitCommands(OleMenuCommandService commandService)
        {
            var guid = typeof(GitCommandIds).GUID;

            var commandId = new CommandID(guid, (int)GitCommandIds.Status);
            var command = new OleMenuCommand((s, e) => Package.Runner.ExecuteCommand(TortoiseGitCommandScope.Repo, TortoiseGitCommands.Status), commandId);
            commandService.AddCommand(command);

            Package.Environment.SetKeyBindings(command, "Global::Ctrl+G,S", "Text Editor::Ctrl+G,S");
            Package.Environment.SetKeyBindings(command, "Global::Ctrl+G,Ctrl+S", "Text Editor::Ctrl+G,Ctrl+S");

            commandId = new CommandID(guid, (int)GitCommandIds.Log);
            command = new OleMenuCommand((s, e) => Package.Runner.ExecuteCommand(TortoiseGitCommandScope.Repo, TortoiseGitCommands.Log), commandId);
            commandService.AddCommand(command);

            commandId = new CommandID(guid, (int)GitCommandIds.Commit);
            command = new OleMenuCommand((s, e) => Package.Runner.ExecuteCommand(TortoiseGitCommandScope.Repo, TortoiseGitCommands.Commit), commandId);
            commandService.AddCommand(command);

            Package.Environment.SetKeyBindings(command, "Global::Ctrl+G,C", "Text Editor::Ctrl+G,C");
            Package.Environment.SetKeyBindings(command, "Global::Ctrl+G,Ctrl+C", "Text Editor::Ctrl+G,Ctrl+C");

            guid = typeof(GitFileCommandIds).GUID;

            commandId = new CommandID(guid, (int)GitFileCommandIds.LogFile);
            command = new OleMenuCommand((s, e) => Package.Runner.ExecuteCommand(TortoiseGitCommandScope.File, TortoiseGitCommands.Log), commandId);
            command.BeforeQueryStatus += GitFileCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            Package.Environment.SetKeyBindings(command, "Global::Ctrl+G,L", "Text Editor::Ctrl+G,L");
            Package.Environment.SetKeyBindings(command, "Global::Ctrl+G,Ctrl+L", "Text Editor::Ctrl+G,Ctrl+L");

            commandId = new CommandID(guid, (int)GitFileCommandIds.BlameFile);
            command = new OleMenuCommand((s, e) => Package.Runner.ExecuteCommand(TortoiseGitCommandScope.File, TortoiseGitCommands.Blame), commandId);
            command.BeforeQueryStatus += GitFileCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            Package.Environment.SetKeyBindings(command, "Global::Ctrl+G,B", "Text Editor::Ctrl+G,B");
            Package.Environment.SetKeyBindings(command, "Global::Ctrl+G,Ctrl+B", "Text Editor::Ctrl+G,Ctrl+B");

            commandId = new CommandID(guid, (int)GitFileCommandIds.DiffFile);
            command = new OleMenuCommand((s, e) => Package.Runner.ExecuteCommand(TortoiseGitCommandScope.File, TortoiseGitCommands.Diff), commandId);
            command.BeforeQueryStatus += GitFileCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            Package.Environment.SetKeyBindings(command, "Global::Ctrl+G,D", $"Text Editor::Ctrl+G,D");
            Package.Environment.SetKeyBindings(command, "Global::Ctrl+G,Ctrl+D", $"Text Editor::Ctrl+G,Ctrl+D");

            commandId = new CommandID(guid, (int)GitFileCommandIds.RevertFile);
            command = new OleMenuCommand((s, e) => Package.Runner.ExecuteCommand(TortoiseGitCommandScope.File, TortoiseGitCommands.Revert), commandId);
            command.BeforeQueryStatus += GitFileCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);
        }

        private bool HasRepository => Package.Paths.GitRepoPath != null;

        private void CommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            command.Visible = HasRepository;
        }

        private void GitFileCommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            command.Visible = HasRepository;
            if (!command.Visible)
            {
                return;
            }

            var filePath = Package.Paths.ActiveFilePath;
            string file;
            if (!String.IsNullOrEmpty(filePath))
            {
                command.Enabled = true;
                file = Path.GetFileName(filePath);
            }
            else
            {
                command.Enabled = false;
                file = "<file>";
            }

            string textFormat;
            if (!CommandTextMap.TryGetValue(command.CommandID.ID, out textFormat))
            {
                return;
            }

            command.Text = String.Format(textFormat, file);
        }
    }
}
