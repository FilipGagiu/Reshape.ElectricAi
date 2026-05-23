namespace Reshape.ElectricAi.Core.Domain.Exceptions;

public sealed class LlmSchemaException(string missingOrInvalidField)
    : LlmException("llm-malformed-response", $"LLM response failed schema validation: {missingOrInvalidField}.")
{
    public string MissingOrInvalidField { get; } = missingOrInvalidField;
}
