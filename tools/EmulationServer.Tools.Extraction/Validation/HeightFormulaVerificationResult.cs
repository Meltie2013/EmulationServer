
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
    public bool IsValid =>
        UInt8MaximumObservedError <= UInt8ExpectedMaximumError + 0.0001f &&
        UInt16MaximumObservedError <= UInt16ExpectedMaximumError + 0.0001f;
}
