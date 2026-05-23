using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Domain;

public interface ICategorizable
{
    IReadOnlyDictionary<Category, IReadOnlyList<string>> CategoryValues { get; }
}
