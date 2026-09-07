namespace Auction.BLL.Services;

public sealed class ServiceResponse
{
    public bool IsSuccess { get; }

    public string Message { get; }

    public object? Payload { get; }

    private ServiceResponse(bool isSuccess, string message, object? payload)
    {
        IsSuccess = isSuccess;
        Message = message;
        Payload = payload;
    }

    public static ServiceResponse Success(string message, object? payload = null)
    {
        return new ServiceResponse(true, message, payload);
    }

    public static ServiceResponse Error(string message, object? payload = null)
    {
        return new ServiceResponse(false, message, payload);
    }
}
