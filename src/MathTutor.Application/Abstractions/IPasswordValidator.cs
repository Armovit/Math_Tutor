using MathTutor.Application.Common;

namespace MathTutor.Application.Abstractions;

public interface IPasswordValidator
{
    OperationResult Validate(string password);
}
