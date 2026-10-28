using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.FileSystem {
    public sealed partial class FileOperation {
        /// <summary>
        /// The operation as an app action, so it runs on the app's action runner with its cancel key and output log:
        /// each failed item and the final summary are written to the log.
        /// </summary>
        /// <param name="key">The command key.</param>
        /// <param name="label">The label shown while it runs; the operation's kind when null.</param>
        /// <param name="completed">Called with the result on the worker, before the action completes; post UI work back to the app thread.</param>
        /// <returns>The command.</returns>
        public AppCommand ToAppCommand(string key, string? label = null, Action<FileOperationResult>? completed = null) {
            return new AppCommand(key, label ?? Kind.ToString(), async (context, token) => {
                FileOperationResult result = await RunAsync(token).ConfigureAwait(false);
                foreach (FileOperationError error in result.Errors) { context.Write(error.ToString(), "err"); }
                string bytes = result.BytesTransferred > 0 ? " · " + result.BytesTransferred.ToString("N0", CultureInfo.InvariantCulture) + " bytes" : string.Empty;
                context.Write(result + bytes, result.Succeeded ? "out" : "warn");
                completed?.Invoke(result);
                // A Cancel answer to a conflict is a normal ending; only the cancel key reports the action as cancelled.
                token.ThrowIfCancellationRequested();
                if (result.Errors.Count != 0) { throw new FileOperationException(result); }
            });
        }
    }

    /// <summary>Reports that some items of an operation failed; the result lists them and can retry them.</summary>
    public sealed class FileOperationException : Exception {
        /// <summary>Creates the exception.</summary>
        /// <param name="result">The result.</param>
        public FileOperationException(FileOperationResult result) : base((result ?? throw new ArgumentNullException(nameof(result))).Errors.Count + " of the items failed.") { Result = result; }

        /// <summary>The result, with the failed items.</summary>
        public FileOperationResult Result { get; }
    }
}
