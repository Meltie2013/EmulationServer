namespace EmulationServer.Tools.Extraction.Validation;

public sealed record HeightFormulaVerificationResult(
    int Samples,
    float GridHeight,
    float GridMaxHeight,
    float UInt8MaximumObservedError,
    float UInt8ExpectedMaximumError,
    float UInt16MaximumObservedError,
    float UInt16ExpectedMaximumError)
{
    // The formula gives the ideal half-step quantization error. The implementation
    // intentionally uses float math because the generated map data is float-based,
    // so allow a small tolerance for single-precision rounding during encode/decode.
    public float FloatingPointTolerance => Math.Max(0.00025f, Math.Abs(GridMaxHeight - GridHeight) * 0.0000001f);

    public float UInt8AllowedMaximumError => UInt8ExpectedMaximumError + FloatingPointTolerance;

    public float UInt16AllowedMaximumError => UInt16ExpectedMaximumError + FloatingPointTolerance;

    public bool IsValid =>
        UInt8MaximumObservedError <= UInt8AllowedMaximumError &&
        UInt16MaximumObservedError <= UInt16AllowedMaximumError;
}
