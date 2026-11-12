using System;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Graphics {
    /// <summary>Composes decoded images through the same content builder as core widgets.</summary>
    public static class ImageComposition {
        /// <summary>Reads an image and adds it to a layout.</summary>
        /// <param name="content">The destination content builder.</param>
        /// <param name="path">The encoded image file.</param>
        /// <param name="configure">Optional typed image configuration.</param><returns>The same content builder.</returns>
        public static ContentBuilder Image(this ContentBuilder content, string path, Action<ConsoleImage>? configure = null) {
            var image = ConsoleImage.FromFile(path);
            configure?.Invoke(image);
            return content.Add(image);
        }

        /// <summary>Adds an existing image without decoding it again.</summary>
        /// <param name="content">The destination content builder.</param>
        /// <param name="image">The image to compose.</param><returns>The same content builder.</returns>
        public static ContentBuilder Image(this ContentBuilder content, ConsoleImage image) => content.Add(image);
    }
}
