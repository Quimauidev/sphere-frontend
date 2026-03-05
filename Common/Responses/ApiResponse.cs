using Sphere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Common.Responses
{
    public class ApiResponse<T>
    {
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<ErrorDetail> Errors { get; set; } = [];
        public bool IsSuccess => Errors.Count == 0;

        public ApiResponse() { }

        public ApiResponse(string? message, T? data, List<ErrorDetail>? errors)
        {
            Message = message;
            Data = data;
            Errors = errors ?? [];
        }

        public static ApiResponse<T> Success(string? message, T? data)
            => new(message, data, []);

        public static ApiResponse<T> Fail(string? message, List<ErrorDetail>? errors)
            => new(message, default, errors ?? []);
        public static ApiResponse<T> Fail(string? message, string code, string description)
            => new(message, default, [new ErrorDetail { Code = code, Description = description }]);


    }

    public class ErrorDetail
    {
        public string? Code { get; set; }

        public string? Description { get; set; }
    }
}