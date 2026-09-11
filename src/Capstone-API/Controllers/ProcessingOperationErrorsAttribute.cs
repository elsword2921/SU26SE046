using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Capstone_API.Controllers;

public sealed class ProcessingOperationErrorsAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        if (exception is KeyNotFoundException)
            context.Result = new NotFoundObjectResult(new { message = exception.Message });
        else if (exception is DbUpdateConcurrencyException || sql?.Number is 1205 or 2601 or 2627)
            context.Result = new ConflictObjectResult(new { message = "Dữ liệu vừa được xử lý bởi yêu cầu khác. Vui lòng tải lại trước khi tiếp tục." });
        else return;
        context.ExceptionHandled = true;
    }
}
