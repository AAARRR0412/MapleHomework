using System;

namespace MapleHomework.Services
{
    /// <summary>
    /// 작업 결과를 나타내는 클래스 (값 없음)
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string? ErrorMessage { get; }
        public Exception? Exception { get; }

        protected Result(bool isSuccess, string? errorMessage = null, Exception? exception = null)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public static Result Success() => new(true);
        
        public static Result Failure(string errorMessage) => new(false, errorMessage);
        
        public static Result Failure(Exception exception) => new(false, exception.Message, exception);
        
        public static Result Failure(string errorMessage, Exception exception) => new(false, errorMessage, exception);

        public static Result<T> Success<T>(T value) => Result<T>.Success(value);
        
        public static Result<T> Failure<T>(string errorMessage) => Result<T>.Failure(errorMessage);
        
        public static Result<T> Failure<T>(string errorMessage, Exception exception) => Result<T>.Failure(errorMessage, exception);
        
        public static Result<T> Failure<T>(Exception exception) => Result<T>.Failure(exception);
    }

    /// <summary>
    /// 작업 결과를 나타내는 클래스 (값 있음)
    /// </summary>
    public class Result<T> : Result
    {
        public T? Value { get; }

        private Result(bool isSuccess, T? value, string? errorMessage = null, Exception? exception = null)
            : base(isSuccess, errorMessage, exception)
        {
            Value = value;
        }

        public static Result<T> Success(T value) => new(true, value);
        
        public new static Result<T> Failure(string errorMessage) => new(false, default, errorMessage);
        
        public new static Result<T> Failure(Exception exception) => new(false, default, exception.Message, exception);
        
        public new static Result<T> Failure(string errorMessage, Exception exception) => new(false, default, errorMessage, exception);

        /// <summary>
        /// 값을 가져오거나 기본값 반환
        /// </summary>
        public T GetValueOrDefault(T defaultValue) => IsSuccess && Value != null ? Value : defaultValue;
    }
}
