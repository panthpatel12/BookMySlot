namespace BookMySlot.Services;

public class BookingOperationResult<T>
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public T? Data { get; }

    private BookingOperationResult(bool isSuccess, string message, T? data)
    {
        IsSuccess = isSuccess;
        Message = message;
        Data = data;
    }

    public static BookingOperationResult<T> Success(T data, string message = "Operation completed successfully.")
        => new(true, message, data);

    public static BookingOperationResult<T> Failure(string message)
        => new(false, message, default);
}

public class BookingOperationResult
{
    public bool IsSuccess { get; }
    public string Message { get; }

    private BookingOperationResult(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    public static BookingOperationResult Success(string message = "Operation completed successfully.")
        => new(true, message);

    public static BookingOperationResult Failure(string message)
        => new(false, message);
}
