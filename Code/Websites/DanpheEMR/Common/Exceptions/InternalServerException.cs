﻿using System.Collections.Generic;
using System.Net;

namespace Application.Common.Exceptions
{
    public class InternalServerException : CustomException
    {
        public InternalServerException(string message, List<string> errors = default) : base(message, errors, HttpStatusCode.InternalServerError)
        {
        }
    }
}
