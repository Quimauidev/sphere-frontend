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
        public List<ErrorDetail>? Errors { get; set; }
        public bool IsSuccess => Errors == null || Errors.Count == 0;

        public ApiResponse() => Errors = [];

        public ApiResponse(string? message, T? data = default, List<ErrorDetail>? errors = null)
        {
            Message = message;
            Data = data;
            Errors = errors ?? [];
        }

        public static ApiResponse<T> Success(string message, T data)
            => new(message, data, null);

        public static ApiResponse<T> Fail(string message, List<ErrorDetail> errors)
            => new(message, default, errors);

        public static implicit operator ApiResponse<T>(ApiResponse<UserProfileModel> v)
        {
            throw new NotImplementedException();
        }
    }

    public class ErrorDetail
    {
        public string? Code { get; set; }

        public string? Description { get; set; }
    }
}