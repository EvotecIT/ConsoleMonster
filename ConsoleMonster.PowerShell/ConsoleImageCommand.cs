using System;
using System.IO;
using System.Management.Automation;
using ConsoleMonster.Graphics;

namespace ConsoleMonster.PowerShell {
    /// <summary>The image source and cell size shared by Show-ConsoleImage and New-ConsoleImage.</summary>
    public abstract class ConsoleImageCommand : PSCmdlet {
        /// <summary><para type="description">A PNG, JPEG, GIF, BMP, TIFF or PPM file. Files from Get-ChildItem and path strings can also be piped in.</para></summary>
        [Parameter(Position = 0, ParameterSetName = "Path")]
        public string Path { get; set; } = string.Empty;

        /// <summary><para type="description">Files from Get-ChildItem or Resolve-Path, or path strings.</para></summary>
        [Parameter(ValueFromPipeline = true, ParameterSetName = "Path", DontShow = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Encoded image bytes, for example a chart rendered to PNG in memory.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Bytes")]
        public byte[]? Bytes { get; set; }

        /// <summary><para type="description">The text in the placeholder that redirected and CI output get, [image: label WxH]. Defaults to the file name.</para></summary>
        [Parameter]
        public string? Label { get; set; }

        /// <summary><para type="description">Width in cells. Without -Width or -Height the image takes one cell per 10 pixels, up to the window width.</para></summary>
        [Parameter, ValidateRange(0, 1000)]
        public int Width { get; set; }

        /// <summary><para type="description">Height in cells. With -Width as well, the image fits inside that box; the aspect ratio is always kept.</para></summary>
        [Parameter, ValidateRange(0, 1000)]
        public int Height { get; set; }

        /// <summary>Reads the image for this record, or writes a non-terminating error and returns null.</summary>
        /// <returns>The image, or null.</returns>
        protected ConsoleImage? CreateImage() {
            if (ParameterSetName == "Bytes") {
                return Decode(Bytes ?? Array.Empty<byte>(), "bytes", Label);
            }
            string path = Path;
            if (InputObject != null) {
                object value = InputObject.BaseObject;
                path = value is FileInfo file ? file.FullName : value is PathInfo resolved ? resolved.ProviderPath : value as string
                    ?? throw Reject(value);
            }
            if (path.Length == 0) {
                ThrowTerminatingError(new ErrorRecord(new ArgumentException(MyInvocation.MyCommand.Name + " needs -Path or -Bytes."), "ConsoleImageSourceMissing", ErrorCategory.InvalidArgument, null));
            }
            string source = path;
            byte[] data;
            try {
                source = SessionState.Path.GetUnresolvedProviderPathFromPSPath(path);
                data = File.ReadAllBytes(source);
            } catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is NotSupportedException
                || error is ArgumentException || error is SessionStateException) {
                WriteError(new ErrorRecord(error, "ConsoleImageUnreadable", ErrorCategory.ReadError, source));
                return null;
            }
            return Decode(data, source, Label ?? System.IO.Path.GetFileName(source));
        }

        // The failures ChartForgeX reports for damaged or truncated images: one bad file is an error, not the end of a pipeline.
        private ConsoleImage? Decode(byte[] data, string source, string? name) {
            try {
                return new ConsoleImage(data, name) { Width = Width, Height = Height };
            } catch (Exception error) when (error is IOException || error is NotSupportedException || error is ArgumentException
                || error is ArithmeticException || error is IndexOutOfRangeException) {
                WriteError(new ErrorRecord(new InvalidDataException("'" + source + "' cannot be read as an image. " + error.Message, error),
                    "ConsoleImageInvalid", ErrorCategory.InvalidData, source));
                return null;
            }
        }

        // Anything other than a file or a path is a mistake in the pipeline: stop once instead of one error per object.
        private PipelineStoppedException Reject(object value) {
            ThrowTerminatingError(new ErrorRecord(new ArgumentException(MyInvocation.MyCommand.Name + " accepts image files from Get-ChildItem or Resolve-Path, or path strings, from the pipeline."),
                "UnexpectedPipelineInput", ErrorCategory.InvalidArgument, value));
            return new PipelineStoppedException();
        }
    }
}
