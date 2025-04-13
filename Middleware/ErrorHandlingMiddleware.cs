using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using FitnessApp.API.Exceptions;
using FitnessApp.API.Models;

namespace FitnessApp.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var result = JsonSerializer.Serialize(new ApiResponse<object>
        {
            Success = false,
            Message = ex.Message,
            Data = null
        });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = ex switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return context.Response.WriteAsync(result);
    }
} 