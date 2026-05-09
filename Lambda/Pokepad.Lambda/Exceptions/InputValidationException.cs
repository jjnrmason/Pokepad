namespace Pokepad.Lambda.Exceptions;

public sealed class InputValidationException(string message) : Exception(message);
