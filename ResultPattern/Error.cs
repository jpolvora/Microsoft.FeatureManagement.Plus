using System;

namespace FeatureManagement.ResultPattern
{
    public class Error
    {
        public Error(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public bool IsNone => this == None;

        public string Code { get; set; }
        public string Message { get; set; }


        public static readonly Error None = new Error(string.Empty, string.Empty);

        public static readonly Error NullValue = new Error("Error.NullValue", "The specified result value is null.");

        public static readonly Error ConditionNotMet = new Error("Error.ConditionNotMet", "The specified condition was not met.");

        public static implicit operator Error(Exception ex) => new Error(ex.GetType().Name, ex.Message);

        public static implicit operator Error(Result value) => value.Error;

        public static implicit operator Error(string value) => new Error(null, value);
    }
}