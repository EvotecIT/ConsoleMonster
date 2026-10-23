using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Interactive capture temporarily redirects process streams and temp roots, so it must run alone.</summary>
    [CollectionDefinition("Interactive process defaults", DisableParallelization = true)]
    public sealed class InteractiveProcessDefaultsCollection { }
}
