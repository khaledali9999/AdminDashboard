namespace AdminDashboard.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "") =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string error) =>
        new() { Success = false, Message = error, Errors = new List<string> { error } };

    public static ApiResponse<T> Fail(IEnumerable<string> errors)
    {
        var list = errors.ToList();
        return new() { Success = false, Message = list.FirstOrDefault(), Errors = list };
    }
}

public record PagedResult<T>(List<T> Items, int Total, int Page, int Size)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)Size);
}
