namespace Application.Exceptions
{
    public class AppException : Exception
    {
        public int StatusCode { get; }
        public Dictionary<string, string>? Errors { get; }

        public AppException(string message, int statusCode = 400)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public AppException(string message, Dictionary<string, string> errors, int statusCode = 400)
            : base(message)
        {
            StatusCode = statusCode;
            Errors = errors;
        }
    }

    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message = "Unauthorized")
            : base(message, 401)
        {
        }
    }

    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message = "Forbidden")
            : base(message, 403)
        {
        }
    }

    public class NotFoundException : AppException
    {
        public NotFoundException(string message = "Resource not found")
            : base(message, 404)
        {
        }
    }

    public class ConflictException : AppException
    {
        public ConflictException(string message = "Conflict")
            : base(message, 409)
        {
        }
    }

    public class ValidationException : AppException
    {
        public ValidationException(Dictionary<string, string> errors)
            : base("Validation failed", errors, 400)
        {
        }
    }
}

