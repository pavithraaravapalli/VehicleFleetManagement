using System;
using System.Collections.Generic;

namespace VehicleFleetManagement.DTOs
{
    /// <summary>
    /// Unified JSON response wrapper for all RESTful API endpoints.
    /// Provides consistent status, message, payload, validation errors, and UTC timestamp.
    /// </summary>
    /// <typeparam name="T">The type of payload data returned.</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> Ok(T? data, string message = "Operation completed successfully.")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Created(T? data, string message = "Resource created successfully.")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Fail(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }
}
