using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The base of every prompt. <see cref="Show"/> picks the interaction mode: a rich inline prompt (or an overlay over a
    /// running live dashboard), a line-based question, an accessible numbered question, or, when nobody can answer, the
    /// default or a clear <see cref="PromptUnavailableException"/>. It never waits for input that cannot come.
    /// </summary>
    /// <typeparam name="T">The answer type.</typeparam>
    public abstract class Prompt<T> {
        private T _default = default!;

        /// <summary>Creates a prompt.</summary>
        /// <param name="title">The question.</param>
        protected Prompt(string title) {
            Title = title ?? throw new ArgumentNullException(nameof(title));
        }

        /// <summary>The question.</summary>
        public string Title { get; set; }

        /// <summary>Help text under the question.</summary>
        public string? Description { get; set; }

        /// <summary>
        /// The parameter that supplies this value non-interactively (<c>-Domain</c>, <c>--domain</c>); named in the
        /// non-interactive error message.
        /// </summary>
        public string? ParameterName { get; set; }

        /// <summary>Whether a default was set.</summary>
        public bool HasDefault { get; private set; }

        /// <summary>The default answer: pre-selected, accepted with Enter, and returned in non-interactive sessions.</summary>
        public T Default {
            get => _default;
            set {
                _default = value;
                HasDefault = true;
            }
        }

        /// <summary>Builds the initialized rich prompt as a widget without reading input or producing an answer.</summary>
        /// <param name="options">Profile, theme, labels and inline height; null uses the interactive defaults. Input and mode are not used.</param>
        /// <returns>The prompt's initial view, suitable for ordinary terminal output or a documentation layout.</returns>
        public IWidget Snapshot(InteractiveOptions? options = null) {
            InteractiveOptions settings = options ?? InteractiveOptions.Default;
            Capabilities.TerminalProfile profile = settings.ResolveProfile();
            RenderContext context = Terminal.CreateContext(profile, settings.ResolveTheme());
            int height = Math.Max(3, Math.Min(profile.Height, settings.MaxInlineHeight ?? profile.Height));
            PromptView<T> view = CreateView(settings.ResolveLabels());
            IWidget widget = view.BuildFrame(new ViewFrame(context, profile.Width, height, true, TimeSpan.Zero, view.Caret));
            int rows = Math.Max(1, Math.Min(height, widget.Measure(new Constraints(profile.Width, height), context).Height));
            return new Rows().Add(widget, LayoutSize.Fixed(rows));
        }

        /// <summary>Asks the question and returns the answer.</summary>
        /// <param name="options">Options; null uses <see cref="InteractiveOptions.Default"/>.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>The answer.</returns>
        /// <exception cref="PromptCancelledException">The user cancelled.</exception>
        /// <exception cref="PromptUnavailableException">Nobody can answer and there is no default.</exception>
        public T Show(InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            InteractiveOptions o = options ?? InteractiveOptions.Default;
            PromptLabels labels = o.ResolveLabels();
            switch (o.ResolveMode()) {
                case InteractionMode.None:
                    if (HasDefault) {
                        return Default;
                    }
                    throw Unavailable(labels);
                case InteractionMode.Line:
                case InteractionMode.Accessible:
                    return AskLines(new LinePrompter(o.ResolveLineConsole(), labels, o.ResolveMode() == InteractionMode.Accessible), cancellationToken);
            }
            return ShowRich(o, labels, cancellationToken);
        }

        /// <summary>Asks the question on a worker thread.</summary>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>The answer.</returns>
        public Task<T> ShowAsync(InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            return Task.Factory.StartNew(() => Show(options, cancellationToken), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>Asks the question; returns false instead of throwing when the user cancels.</summary>
        /// <param name="value">The answer.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>Whether an answer was given.</returns>
        public bool TryShow(out T value, InteractiveOptions? options = null, CancellationToken cancellationToken = default) {
            try {
                value = Show(options, cancellationToken);
                return true;
            } catch (PromptCancelledException) {
                value = default!;
                return false;
            }
        }

        internal PromptUnavailableException Unavailable(PromptLabels labels) {
            string message = string.IsNullOrEmpty(ParameterName)
                ? PromptLabels.Format(labels.NonInteractiveNeedsDefault, Title)
                : PromptLabels.Format(labels.NonInteractiveNeedsValue, Title, ParameterName);
            return new PromptUnavailableException(message) { Prompt = Title, Parameter = ParameterName };
        }

        private T ShowRich(InteractiveOptions options, PromptLabels labels, CancellationToken cancellationToken) {
            using (InputLease lease = ConsoleInput.Acquire(options.Input)) {
                Capabilities.TerminalProfile profile = options.ResolveProfile();
                RenderContext context = Terminal.CreateContext(profile, options.ResolveTheme());
                IPromptSurface? surface = options.Surface;
                bool owns = false;
                // A skipped-delay recording captures the real live overlay, including its checkpoint backdrop.
                // A separately supplied surface/capture keeps its usual standalone prompt behavior.
                if (options.ResolveTarget() is RecordingTarget recording && recording.SkipDelays &&
                    ReferenceEquals(surface, recording.Screens.Surface)) {
                    IPromptSurface? overlay = LivePromptSurface.TryCreate(context, recording);
                    if (overlay != null) {
                        surface = overlay;
                        owns = true;
                    }
                }
                if (surface == null) {
                    surface = PromptHost.Resolve(context) ?? new InlineSurface(profile, null, options.MaxInlineHeight);
                    owns = true;
                }
                try {
                    PromptView<T> view = CreateView(labels);
                    try {
                        InteractionLoop.Run(view, lease.Source, surface, context, cancellationToken);
                    } catch {
                        surface.Complete(new[] { CancelledLine(context, labels) });
                        throw;
                    }
                    surface.Complete(view.Summary(context));
                    if (view.IsCancelled) {
                        throw new PromptCancelledException(Title);
                    }
                    return view.Result;
                } finally {
                    if (owns) {
                        surface.Dispose();
                    }
                }
            }
        }

        internal StyledLine CancelledLine(RenderContext context, PromptLabels labels) {
            return new StyledLine(new[] {
                new TextSpan(context.Glyphs.Get(Glyphs.Glyph.Cross) + " ", context.Style(ThemeToken.Muted)),
                new TextSpan(Title, context.Style(ThemeToken.Muted)),
                new TextSpan("  " + labels.Cancelled, context.Style(ThemeToken.Muted).With(TextDecoration.Italic))
            });
        }

        internal abstract PromptView<T> CreateView(PromptLabels labels);

        internal abstract T AskLines(LinePrompter lines, CancellationToken cancellationToken);

        /// <summary>The answer as text, for the summary line left in the scrollback.</summary>
        /// <param name="value">The answer.</param>
        /// <returns>The text.</returns>
        public abstract string Describe(T value);
    }

    /// <summary>The view of a prompt: chrome (question, messages, footer) around an editor, plus the result.</summary>
    /// <typeparam name="T">The answer type.</typeparam>
    internal abstract class PromptView<T> : InteractiveView {
        protected PromptView(Prompt<T> prompt, PromptLabels labels) : base(labels) {
            Prompt = prompt;
        }

        public Prompt<T> Prompt { get; }

        public T Result { get; protected set; } = default!;

        /// <summary>A validation error under the editor, or null.</summary>
        public string? Error { get; set; }

        protected void Accept(T value) {
            Result = value;
            IsDone = true;
        }

        protected void Decline() {
            IsCancelled = true;
            IsDone = true;
        }

        public override IReadOnlyList<StyledLine>? Summary(RenderContext context) {
            if (IsCancelled) {
                return new[] { Prompt.CancelledLine(context, Labels) };
            }
            return new[] {
                new StyledLine(new[] {
                    new TextSpan(context.Glyphs.Get(Glyphs.Glyph.Check) + " ", context.Style(ThemeToken.Pass)),
                    new TextSpan(Prompt.Title, context.Style(ThemeToken.Title)),
                    new TextSpan("  " + Prompt.Describe(Result), context.Style(ThemeToken.Accent))
                })
            };
        }

        /// <summary>The question line: an accent mark, the title and a muted hint.</summary>
        protected UiLine Header(string? hint, ThemeToken mark = ThemeToken.Accent, string? markText = null) {
            return new UiLine(context => {
                InteractiveGlyphs g = InteractiveGlyphs.For(context);
                List<TextSpan> spans = new List<TextSpan> {
                    new TextSpan((markText ?? g.Question) + " ", context.Style(mark).With(TextDecoration.Bold)),
                    new TextSpan(Prompt.Title, context.Style(ThemeToken.Title))
                };
                if (!string.IsNullOrEmpty(hint)) {
                    spans.Add(new TextSpan("  " + hint, context.Style(ThemeToken.Muted)));
                }
                return new StyledLine(spans);
            });
        }

        /// <summary>The error line (validation) or an informational line.</summary>
        protected UiLine MessageLine(string? error, string? info = null) {
            return new UiLine(context => {
                if (!string.IsNullOrEmpty(error)) {
                    return new StyledLine(new[] {
                        new TextSpan(context.Glyphs.Get(Glyphs.Glyph.Cross) + " ", context.Style(ThemeToken.Fail)),
                        new TextSpan(error!, context.Style(ThemeToken.Fail))
                    });
                }
                return string.IsNullOrEmpty(info) ? StyledLine.Empty : StyledLine.From(info!, context.Style(ThemeToken.Muted));
            }) { Indent = 2 };
        }

        protected KeyFooter Footer(ViewFrame frame) {
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, frame);
            return new KeyFooter(bindings) { Indent = 2, Toast = CurrentToast };
        }

        /// <summary>The filter or input row prefix: <c>› </c> in accent.</summary>
        protected static TextSpan PromptMark(RenderContext context) => new TextSpan(InteractiveGlyphs.For(context).Pointer, context.Style(ThemeToken.Accent));
    }
}
