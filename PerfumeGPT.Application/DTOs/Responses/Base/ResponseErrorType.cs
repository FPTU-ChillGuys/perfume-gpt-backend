namespace PerfumeGPT.Application.DTOs.Responses.Base
{
    public enum ResponseErrorType
    {
        None = 200,
        NotFound = 404,       // 404
        BadRequest = 400,     // 400 (Lỗi validation)
        Conflict = 409,       // 409 (Lỗi nghiệp vụ, ví dụ: "đã tồn tại")
        Unauthorized = 401,   // 401 (Chưa đăng nhập)
        Forbidden = 403,      // 403 (Không có quyền)
        InternalError = 500   // 500 (Lỗi server)
    }
}
