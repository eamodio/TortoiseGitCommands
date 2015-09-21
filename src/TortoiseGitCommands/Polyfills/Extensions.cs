﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace TortoiseGitCommands.Polyfills
{
    internal static class Extensions
    {
        public static Document GetActiveDocument(this DTE2 environment)
        {
            return environment.ActiveDocument;
        }

        public static IEnumerable<string> GetDocumentFiles(this DTE2 environment)
        {
            return from d in environment.GetDocuments() select GetExactPathName(d.FullName);
        }

        public static IEnumerable<Document> GetDocuments(this DTE2 environment)
        {
            return from w in environment.GetDocumentWindows() where w.Document != null select w.Document;
        }

        public static IEnumerable<Window> GetDocumentWindows(this DTE2 environment)
        {
            return environment.Windows
                              .Cast<Window>()
                              .Where(w =>
                                     {
                                         try
                                         {
                                             return !w.Linkable;
                                         }
                                         catch (ObjectDisposedException)
                                         {
                                             return false;
                                         }
                                     });
        }

        public static IEnumerable<Breakpoint> GetBreakpoints(this DTE2 environment)
        {
            return environment.Debugger.Breakpoints.Cast<Breakpoint>();
        }

        public static IEnumerable<Breakpoint> GetMatchingBreakpoints(this DTE2 environment, HashSet<string> files)
        {
            return environment.Debugger.Breakpoints.Cast<Breakpoint>().Where(bp => files.Contains(bp.File));
        }

        public static void CloseAll(this IEnumerable<Window> windows, vsSaveChanges saveChanges = vsSaveChanges.vsSaveChangesPrompt)
        {
            foreach (var w in windows)
            {
                w.Close(saveChanges);
            }
        }

        public static void CloseAll(this IEnumerable<Document> documents, vsSaveChanges saveChanges = vsSaveChanges.vsSaveChangesPrompt)
        {
            foreach (var d in documents)
            {
                d.Close(saveChanges);
            }
        }

        public static Command GetCommand(this DTE2 environment, OleMenuCommand command)
        {
            return environment.Commands.Item(command.CommandID.Guid, command.CommandID.ID);
        }

        public static object[] GetKeyBindings(this DTE2 environment, OleMenuCommand command)
        {
            return environment.GetCommand(command)?.Bindings as object[];
        }

        public static void SetKeyBindings(this DTE2 environment, OleMenuCommand command, params object[] bindings)
        {
            var dteCommand = environment.GetCommand(command);
            if (dteCommand == null)
            {
                return;
            }

            dteCommand.Bindings = bindings;
        }

        public static void SetKeyBindings(this DTE2 environment, OleMenuCommand command, IEnumerable<object> bindings)
        {
            environment.SetKeyBindings(command, bindings.ToArray());
        }

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
                       : di.Name;
        }

        private static string GetExactPathNameCore(DirectoryInfo di)
        {
            return di.Parent != null
                       ? Path.Combine(GetExactPathNameCore(di.Parent),
                                      di.Parent.EnumerateFileSystemInfos(di.Name).First().Name)
                       : di.Name;
        }
    }
}