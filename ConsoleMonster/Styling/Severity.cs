namespace ConsoleMonster.Styling;

/// <summary>Assessment outcomes, distinct from inventory health states.</summary>
public enum Severity {
    /// <summary>A critical finding.</summary>
    Critical,
    /// <summary>A high-severity finding.</summary>
    High,
    /// <summary>A medium-severity finding.</summary>
    Medium,
    /// <summary>A low-severity finding.</summary>
    Low,
    /// <summary>An informational result.</summary>
    Info,
    /// <summary>A successful assessment.</summary>
    Passed,
    /// <summary>Evaluation was prevented by missing evidence.</summary>
    CouldNotEvaluate,
    /// <summary>The assessment was not evaluated.</summary>
    NotEvaluated,
    /// <summary>The outcome is unknown.</summary>
    Unknown
}
