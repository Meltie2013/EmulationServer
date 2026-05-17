
namespace EmulationServer.Tools.Extraction.Validation;

public sealed class MapValidationResult
{
    private readonly List<ValidationMessage> _messages = [];

    public IReadOnlyList<ValidationMessage> Messages => _messages;

    public bool IsValid => _messages.All(message => message.Severity != ValidationSeverity.Error);

    public void Add(ValidationSeverity severity, string message)
    {
        _messages.Add(new ValidationMessage(severity, message));
    }

    public void AddInfo(string message)
    {
        Add(ValidationSeverity.Info, message);
    }

    public void AddWarning(string message)
    {
        Add(ValidationSeverity.Warning, message);
    }

    public void AddError(string message)
    {
        Add(ValidationSeverity.Error, message);
    }
}
