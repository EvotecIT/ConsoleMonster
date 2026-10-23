using System;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Threading;

namespace ConsoleMonsterErrorShapes {
    // Synthetic compiled producers: the engine, rather than a hand-built InvocationInfo, supplies the source extent.
    [Cmdlet(VerbsDiagnostic.Test, "SyntheticErrorShape")]
    public sealed class TestSyntheticErrorShapeCommand : PSCmdlet {
        [Parameter(Mandatory = true)]
        public string Shape { get; set; }

        protected override void ProcessRecord() {
            if (Shape == "mixed-output") {
                WriteObject("before error");
                WriteError(new ErrorRecord(new IOException("First synthetic failure."), "FirstFailure", ErrorCategory.ReadError, null));
                WriteObject("between errors");
                WriteError(new ErrorRecord(new IOException("Second synthetic failure."), "SecondFailure", ErrorCategory.ReadError, null));
                WriteObject("after errors");
                return;
            }
            throw ErrorShapeFactory.CreateException(Shape);
        }
    }

    public static class ErrorShapeFactory {
        public static Exception CreateException(string shape) {
            switch (shape) {
                case "splat-command":
                    return new CommandNotFoundException("The term 'Get-Command' is not recognized as a name of a cmdlet, function, script file, or executable program. Check the spelling of the name, or if a path was included, verify that the path is correct and try again.");
                case "duplicate-inner":
                    NullReferenceException cause = new NullReferenceException("Object reference not set to an instance of an object");
                    return new CmdletInvocationException(cause.Message, cause);
                case "distinct-cause":
                    return new InvalidOperationException("An error occurred while creating the pipeline.", new IOException("The synthetic inventory cache is locked by another writer."));
                case "embedded-stack":
                    StringBuilder message = new StringBuilder("Runspace execution failed.");
                    for (int i = 1; i <= 20; i++) { message.Append("\n   at System.Synthetic.Worker.Step").Append(i).Append("()"); }
                    return new InvalidOperationException(message.ToString());
                default:
                    return new IOException("A detached worker could not read the synthetic inventory.");
            }
        }

        public static ErrorRecord Detached() {
            ErrorRecord result = null;
            Thread worker = new Thread(() => result = new ErrorRecord(CreateException("detached-record"), "DetachedFailure", ErrorCategory.ReadError, null));
            worker.Start();
            worker.Join();
            return result;
        }
    }
}
