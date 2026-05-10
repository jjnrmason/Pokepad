namespace Pokepad.Gold.Api.Exceptions;

public sealed class InputValidationException(string message) : Exception(message);
