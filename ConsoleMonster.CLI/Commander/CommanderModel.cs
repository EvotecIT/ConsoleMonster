using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConsoleMonster.FileSystem;

namespace ConsoleMonster.Apps.Commander {
    /// <summary>
    /// The two panels of ConsoleMonster Commander and the operations between them: switching, swapping, showing one
    /// folder in both, and planning copies, moves, deletes and new folders from the active panel's targets.
    /// </summary>
    /// <remarks>Planning reads the panels on the app thread and returns a <see cref="FileOperation"/> that can run anywhere.</remarks>
    public sealed class CommanderModel : IDisposable {
        /// <summary>Opens both panels from remembered state.</summary>
        /// <param name="state">The state, such as <see cref="CommanderState.Load"/> returns; defaults open the home folder twice.</param>
        public CommanderModel(CommanderState? state = null) {
            state ??= new CommanderState();
            Left = new CommanderPanel(state.Left ?? new PanelState());
            Right = new CommanderPanel(state.Right ?? new PanelState());
            ActiveSide = state.ActivePanel;
        }

        /// <summary>Opens two panels on given folders.</summary>
        /// <param name="left">The left folder.</param>
        /// <param name="right">The right folder.</param>
        public CommanderModel(string left, string right) : this(new CommanderState { Left = new PanelState { Folder = left }, Right = new PanelState { Folder = right } }) { }

        /// <summary>The left panel.</summary>
        public CommanderPanel Left { get; private set; }

        /// <summary>The right panel.</summary>
        public CommanderPanel Right { get; private set; }

        /// <summary>Which panel has focus.</summary>
        public CommanderSide ActiveSide { get; set; }

        /// <summary>The panel with focus.</summary>
        public CommanderPanel Active => ActiveSide == CommanderSide.Left ? Left : Right;

        /// <summary>The other panel: the default destination of copies and moves.</summary>
        public CommanderPanel Other => ActiveSide == CommanderSide.Left ? Right : Left;

        /// <summary>A panel by side.</summary>
        /// <param name="side">The side.</param>
        /// <returns>The panel.</returns>
        public CommanderPanel Panel(CommanderSide side) => side == CommanderSide.Left ? Left : Right;

        /// <summary>The state to remember on exit.</summary>
        public CommanderState State => new CommanderState { Left = Left.State, Right = Right.State, ActivePanel = ActiveSide };

        /// <summary>Moves focus to the other panel (Tab).</summary>
        public void SwitchPanels() => ActiveSide = ActiveSide == CommanderSide.Left ? CommanderSide.Right : CommanderSide.Left;

        /// <summary>Exchanges the panels' places; focus stays with the same panel, now on the other side.</summary>
        public void SwapPanels() {
            CommanderPanel left = Left;
            Left = Right;
            Right = left;
            SwitchPanels();
        }

        /// <summary>Opens the active panel's folder in the other panel too.</summary>
        /// <returns>Whether the other panel could open it.</returns>
        public bool SameFolder() => Other.Navigate(Active.Folder);

        /// <summary>Lists both panels again, keeping cursors and marks.</summary>
        public void RefreshAll() {
            Left.Refresh();
            if (!ReferenceEquals(Left, Right)) { Right.Refresh(); }
        }

        /// <summary>Plans copying the active panel's targets into the other panel's folder (F5).</summary>
        /// <param name="options">Conflict policy, timestamps and progress.</param>
        /// <param name="destination">A destination other than the other panel's folder, such as one typed into the copy dialog.</param>
        /// <returns>The operation, or null when nothing is selected.</returns>
        public FileOperation? PlanCopy(FileOperationOptions? options = null, string? destination = null) {
            IReadOnlyList<string> sources = TargetPaths();
            return sources.Count == 0 ? null : FileOperation.Copy(sources, Destination(destination), options);
        }

        /// <summary>Plans moving the active panel's targets into the other panel's folder, or renaming one item (F6).</summary>
        /// <param name="options">Conflict policy, timestamps and progress.</param>
        /// <param name="destination">A destination other than the other panel's folder; a plain name renames in place.</param>
        /// <returns>The operation, or null when nothing is selected.</returns>
        public FileOperation? PlanMove(FileOperationOptions? options = null, string? destination = null) {
            IReadOnlyList<string> sources = TargetPaths();
            return sources.Count == 0 ? null : FileOperation.Move(sources, Destination(destination), options);
        }

        /// <summary>Plans deleting the active panel's targets: to the trash (F8), or permanently (Shift+F8).</summary>
        /// <param name="permanently">Whether to bypass the trash.</param>
        /// <param name="options">Trash and progress.</param>
        /// <returns>The operation, or null when nothing is selected.</returns>
        public FileOperation? PlanDelete(bool permanently = false, FileOperationOptions? options = null) {
            IReadOnlyList<string> sources = TargetPaths();
            if (sources.Count == 0) { return null; }
            return permanently ? FileOperation.DeletePermanently(sources, options) : FileOperation.Delete(sources, options);
        }

        /// <summary>Plans a new folder in the active panel's folder (F7).</summary>
        /// <param name="name">The name, which may include subfolders (<c>docs/2026</c>).</param>
        /// <param name="options">Progress.</param>
        /// <returns>The operation.</returns>
        public FileOperation PlanCreateFolder(string name, FileOperationOptions? options = null) {
            if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("A folder name is required.", nameof(name)); }
            return FileOperation.CreateFolder(Path.Combine(Active.Folder, name.Trim()), options);
        }

        /// <summary>Lists both panels after an operation and puts the active cursor on what it created, when it's in view.</summary>
        /// <param name="operation">The finished operation.</param>
        public void AfterOperation(FileOperation operation) {
            if (operation == null) { throw new ArgumentNullException(nameof(operation)); }
            RefreshAll();
            if (operation.Kind == FileOperationKind.CreateFolder && operation.Sources.Count == 1) {
                string created = operation.Sources[0];
                string? parent = Path.GetDirectoryName(created);
                if (parent != null && FileOperation.SamePath(parent, Active.Folder)) { Active.MoveTo(Path.Combine(Active.Folder, Path.GetFileName(created))); }
            }
            if (operation.Kind == FileOperationKind.Move && operation.Sources.Count == 1 && operation.Targets[0] is string renamed) {
                string? parent = Path.GetDirectoryName(renamed);
                if (parent != null && FileOperation.SamePath(parent, Active.Folder)) { Active.MoveTo(renamed); }
            }
        }

        /// <summary>Stops both panels' background work.</summary>
        public void Dispose() {
            Left.Dispose();
            Right.Dispose();
        }

        private IReadOnlyList<string> TargetPaths() => Active.Targets.Select(entry => entry.FullPath).ToArray();

        // The trailing separator makes the other panel's folder a folder destination even for one item.
        private string Destination(string? destination) {
            if (!string.IsNullOrWhiteSpace(destination)) { return destination!; }
            string folder = Other.Folder;
            return folder.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? folder : folder + Path.DirectorySeparatorChar;
        }
    }
}
