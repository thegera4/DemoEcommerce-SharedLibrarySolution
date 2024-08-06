using Ecommerce.SharedLibrary.Logs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Ecommerce.SharedLibrary.Middleware
{
    /// <summary>
    /// This class is a middleware for handling exceptions globally
    /// </summary>
    public class GlobalException(RequestDelegate next)
    {
        /// <summary>
        /// This method is checking for exceptions and handling them
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Default values
            string message = "An error occurred while processing your request. Please try again later.";
            int statusCode = (int)HttpStatusCode.InternalServerError;
            string title = "Error";

            try 
            {
                await next(context);

                // check if Response is "Too Many Request" (429)
                if(context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
                {
                    message = "Too many requests.";
                    title = "Warning";
                    statusCode = StatusCodes.Status429TooManyRequests;
                    await ModifyHeader(context, title, message, statusCode);
                }

                // check if Response is "Not Authorized" (401)
                if(context.Response.StatusCode == StatusCodes.Status401Unauthorized)
                {
                    message = "You are not authorized to access this resource.";
                    title = "Alert";
                    statusCode = StatusCodes.Status401Unauthorized;
                    await ModifyHeader(context, title, message, statusCode);
                }

                // check if Response is "Forbidden" (403)
                if(context.Response.StatusCode == StatusCodes.Status403Forbidden)
                {
                    message = "You are allowed/required to access this resource.";
                    title = "Out of Access";
                    statusCode = StatusCodes.Status403Forbidden;
                    await ModifyHeader(context, title, message, statusCode);
                }

            }
            catch (Exception ex)
            {
                // Log the exception with our custom logger
                LogException.LogExceptions(ex);

                // check if Exceptions is Timeout or TaskCanceled
                if(ex is TimeoutException || ex is TaskCanceledException)
                {
                    message = "The request has timed out...Please try again";
                    title = "Timeout";
                    statusCode = StatusCodes.Status408RequestTimeout;
                    await ModifyHeader(context, title, message, statusCode);
                }

                // if it is not Timeout or TaskCanceled, then it is a server error (default values)
                await ModifyHeader(context, title, message, statusCode);
            }
        }

        /// <summary>
        /// This method is used to modify the header of the response and displaying the messages to the client
        /// </summary>
        private static async Task ModifyHeader(HttpContext context, string title, string message, int statusCode)
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails()
            {
                Title = title,
                Detail = message,
                Status = statusCode
            }), CancellationToken.None);
            return;
        }
    }
}