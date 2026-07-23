using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonOperations.Constants.Common
{
    public static class ResponseCode
    {
        public const int Success = 200;
        public const int BadRequest = 400;
        public const int NotFound = 404;
        public const int InternalServerError = 500;
        public const int Unauthorized = 401;
        public const int Forbidden = 403;
        public const int Conflict = 409;
    }
}