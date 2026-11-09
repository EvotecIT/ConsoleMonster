using System;

namespace ConsoleMonster.Styling {
    [Flags]
    internal enum SchemeOverrideFields : byte { None = 0, Foreground = 1, Background = 2, Decoration = 4, All = 7 }

    public sealed partial class Theme {
        private SchemeOverrideFields[]? _schemeOverrides;

        internal SchemeOverrideFields SchemeOverrides(ThemeToken token) => _schemeOverrides?[(int)token] ?? SchemeOverrideFields.None;

        internal void SetSchemeOverride(ThemeToken token, SchemeOverrideFields fields) {
            _schemeOverrides = _schemeOverrides == null ? new SchemeOverrideFields[TokenCount] : (SchemeOverrideFields[])_schemeOverrides.Clone();
            _schemeOverrides[(int)token] = fields;
        }

        private void ClearSchemeOverrides() => _schemeOverrides = null;

        private Theme WithAllSchemeOverrides() {
            _schemeOverrides = new SchemeOverrideFields[TokenCount];
            for (int i = 0; i < TokenCount; i++) { _schemeOverrides[i] = SchemeOverrideFields.All; }
            return this;
        }
    }
}
