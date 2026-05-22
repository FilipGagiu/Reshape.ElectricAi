using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Domain;

public interface ICategorizable
{
    IReadOnlyCollection<Category> Categories { get; }
}
