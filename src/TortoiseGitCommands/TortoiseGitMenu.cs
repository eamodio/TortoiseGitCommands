//------------------------------------------------------------------------------
// <copyright file="TortoiseGitMenu.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using Microsoft.VisualStudio.Shell;

namespace TortoiseGitCommands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TortoiseGitMenu
    {
        enum CommandIds
        {
            Status = 0x0100,
            Log = 0x0200,
            Commit = 0x0300,

            LogFile = 0x0100,
            BlameFile = 0x0200,
            DiffFile = 0x0300,
            RevertFile = 0x0400,
        }

        private static readonly Dictionary<int, string> CommandTextMap = new Dictionary<int, string>
        {
            {(int)CommandIds.LogFile, "Log {0}..."},
            {(int)CommandIds.BlameFile, "Blame {0}..."},
            {(int)CommandIds.DiffFile, "Diff {0}..."},
            {(int)CommandIds.RevertFile, "Revert {0}..."}
        };

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x1020;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid MenuGroup = new Guid("03fd3928-9f87-4d31-9cd4-ccf9af6b4239");
        public static readonly Guid GitMenuGroup = new Guid("211dde17-b062-461e-9628-6c89f81930e2");
        public static readonly Guid GitFileMenuGroup = new Guid("39fce672-099b-43e0-9a91-e5b44018ebb3");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private TortoiseCommandsPackage Package { get; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider => Package;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TortoiseGitMenu Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TortoiseGitMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private TortoiseGitMenu(TortoiseCommandsPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            Package = package;

            var commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var commandId = new CommandID(MenuGroup, CommandId);
                var command = new OleMenuCommand(null, commandId);
                command.BeforeQueryStatus += GitCommandOnBeforeQueryStatus;
                commandService.AddCommand(command);

                SetupCommands(commandService);
            }
        }

        private void SetupCommands(OleMenuCommandService commandService)
        {
            var commandId = new CommandID(GitMenuGroup, (int)CommandIds.Status);
            var command = new OleMenuCommand((s, e) => Package.Runner.ExecuteCommand(TortoiseGitCommandScope.Repo, TortoiseGitCommands.Status), commandId);
            commandService.AddCommand(command);

            commandId = new CommandID(GitMenuGroup, (int)CommandIds.Log);
            command = new OleMenuCommand((s, e) => Package.Runner.ExecuteCommand(TortoiseGitCommandScope.Repo, TortoiseGitCommands.Log), commandId);
            commandService.AddCommand(command);

            commandId = new CommandID(GitMenuGroup, (int)CommandIds.Commit);
            command = new OleMenuCommand((s, e) => Package.Runner.ExecuteCommand(TortoiseGitCommandScope.Repo, TortoiseGitCommands.Commit), commandId);
            commandService.AddCommand(command);

            commandId = new CommandID(GitFileMenuGroup, (int)CommandIds.LogFile);
            command = new OleMenuCommand((s, e) => Package.Runner.ExecuteCommand(TortoiseGitCommandScope.File, TortoiseGitCommands.Log), commandId);
            command.BeforeQueryStatus += GitFileCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(GitFileMenuGroup, (int)CommandIds.BlameFile);
            command = new OleMenuCommand((s, e) => Package.Runner.ExecuteCommand(TortoiseGitCommandScope.File, TortoiseGitCommands.Blame), commandId);
            command.BeforeQueryStatus += GitFileCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(GitFileMenuGroup, (int)CommandIds.DiffFile);
            command = new OleMenuCommand((s, e) => Package.Runner.ExecuteCommand(TortoiseGitCommandScope.File, TortoiseGitCommands.Diff), commandId);
            command.BeforeQueryStatus += GitFileCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(GitFileMenuGroup, (int)CommandIds.RevertFile);
            command = new OleMenuCommand((s, e) => Package.Runner.ExecuteCommand(TortoiseGitCommandScope.File, TortoiseGitCommands.Revert), commandId);
            command.BeforeQueryStatus += GitFileCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);
        }

        private bool HasRepository => Package.Paths.GitRepoPath != null;

        private void GitCommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            var commandOle = sender as OleMenuCommand;
            if (commandOle == null)
            {
                return;
            }

            commandOle.Visible = HasRepository;
        }

        private void GitFileCommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            var commandOle = sender as OleMenuCommand;
            if (commandOle == null)
            {
                return;
            }

            commandOle.Visible = HasRepository;
            if (!commandOle.Visible)
            {
                return;
            }

            var filePath = Package.Paths.ActiveFilePath;
            string file;
            if (!String.IsNullOrEmpty(filePath))
            {
                commandOle.Enabled = true;
                file = Path.GetFileName(filePath);
            }
            else
            {
                commandOle.Enabled = false;
                file = "<file>";
            }

            string textFormat;
            if (!CommandTextMap.TryGetValue(commandOle.CommandID.ID, out textFormat))
            {
                return;
            }

            commandOle.Text = String.Format(textFormat, file);
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(TortoiseCommandsPackage package)
        {
            Instance = new TortoiseGitMenu(package);
        }
    }
}
