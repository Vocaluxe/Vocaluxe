#region license
// This file is part of Vocaluxe.
//
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Api
{
    /// Turns unhandled exceptions into ProblemDetails responses.
    sealed class CExceptionFilter : IExceptionFilter
    {
        private readonly Action<Exception> _OnError;

        public CExceptionFilter(Action<Exception> onError)
        {
            _OnError = onError;
        }

        public void OnException(ExceptionContext context)
        {
            ProblemDetails problem;
            if (context.Exception is TimeoutException)
            {
                problem = new ProblemDetails { Status = StatusCodes.Status503ServiceUnavailable, Title = "Vocaluxe is busy, please try again." };
            }
            else if (context.Exception is CQueueService.CForbiddenException)
            {
                problem = new ProblemDetails { Status = StatusCodes.Status403Forbidden, Title = "You are not allowed to do this." };
            }
            else if (context.Exception is ArgumentOutOfRangeException || context.Exception is KeyNotFoundException)
            {
                problem = new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Not found." };
            }
            else
            {
                _OnError?.Invoke(context.Exception);
                problem = new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "Internal server error." };
            }

            context.Result = new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { "application/problem+json" } };
            context.ExceptionHandled = true;
        }
    }
}
