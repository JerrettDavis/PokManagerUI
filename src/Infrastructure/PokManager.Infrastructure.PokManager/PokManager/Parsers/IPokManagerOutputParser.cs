using PokManager.Domain.Common;

namespace PokManager.Infrastructure.PokManager.PokManager.Parsers;

/// <summary>
/// Defines a parser that converts POK Manager text output into strongly-typed DTOs.
/// </summary>
/// <typeparam name="T">The type of DTO to parse into.</typeparam>
public interface IPokManagerOutputParser<T>
{
    /// <summary>
    /// Parses POK Manager text output into a strongly-typed result.
    /// </summary>
    /// <param name="output">The raw text output from POK Manager.</param>
    /// <returns>A Result containing the parsed DTO or an error message.</returns>
    Result<T> Parse(string output);
}
