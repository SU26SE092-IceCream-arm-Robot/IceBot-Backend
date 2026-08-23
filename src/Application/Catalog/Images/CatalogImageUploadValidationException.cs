using Application.Shared.Exceptions;

namespace Application.Catalog.Images;

public sealed class CatalogImageUploadValidationException : AppException
{
    public CatalogImageUploadValidationException(string message)
        : base(message, 400)
    {
    }
}
