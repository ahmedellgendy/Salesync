namespace Salesync.API.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<String>? Errors { get; set; }
        public int StatusCode { get; set; }

        // success response
        public static ApiResponse<T> SuccessResponse(T data, string message = "Success", int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                StatusCode = statusCode
            };
        }

        // error response
        public static ApiResponse<T> ErrorResponse(string message, int statusCode = 400, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors,
                StatusCode = statusCode
            };
        }

        // validation error response
        public static ApiResponse<T> ValidationErrorResponse(List<string> errors, string message = "Validation Error")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors,
                StatusCode = 400
            };
        }

        // not found response
        public static ApiResponse<T> NotFoundResponse(string message = "Resource Not Found")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                StatusCode = 404
            };
        }

        // conflict response
        public static ApiResponse<T> ConflictResponse(string message = "Resource already exists")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                StatusCode = 409
            };
        }
    }
}