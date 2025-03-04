namespace Public.Api.Tickets
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using Infrastructure;
    using Infrastructure.Swagger;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using RestSharp;
    using StreetNameRegistry.Api.BackOffice.Abstractions.Response;
    using Swashbuckle.AspNetCore.Filters;
    using TicketingService.Abstractions;
    using ProblemDetails = Be.Vlaanderen.Basisregisters.BasicApiProblem.ProblemDetails;

    public partial class TicketingServiceController
    {
        /// <summary>
        /// Vraag een ticket op (v2).
        /// </summary>
        /// <param name="ticketId">Identificator van het ticket.</param>
        /// <param name="actionContextAccessor"></param>
        /// <param name="cancellationToken"></param>
        /// <response code="200">Als het ticket gevonden is.</response>
        /// <response code="400">Als uw verzoek foutieve data bevat.</response>
        /// <response code="429">Als het aantal requests per seconde de limiet overschreven heeft.</response>
        /// <response code="500">Als er een interne fout is opgetreden.</response>
        [HttpGet("tickets/{ticketId}", Name = nameof(GetTicket))]
        [ApiOrder(ApiOrder.TicketingService + 1)]
        [ProducesResponseType(typeof(Ticket), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(TicketExample))]
        [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(BadRequestResponseExamples))]
        [SwaggerResponseExample(StatusCodes.Status429TooManyRequests, typeof(TooManyRequestsResponseExamples))]
        [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExamples))]
        public async Task<IActionResult> GetTicket(
            [FromRoute] Guid ticketId,
            [FromServices] IActionContextAccessor actionContextAccessor,
            CancellationToken cancellationToken = default)
        {
            if (!_ticketingToggle.FeatureEnabled)
            {
                return NotFound();
            }

            if (actionContextAccessor.ActionContext == null)
            {
                return BadRequest();
            }

            var contentFormat = DetermineFormat(actionContextAccessor.ActionContext);

            RestRequest BackendRequest() => CreateBackendGetRequest(ticketId);

            var value = await GetFromBackendAsync(
                contentFormat.ContentType,
                BackendRequest,
                CreateDefaultHandleBadRequest(),
                cancellationToken);

            return new BackendResponseResult(value, BackendResponseResultOptions.ForRead());
        }

        private static RestRequest CreateBackendGetRequest(Guid ticketId)
        {
            var request = new RestRequest("tickets/{ticketId}");
            request.AddParameter("ticketId", ticketId, ParameterType.UrlSegment);
            return request;
        }
    }

    public class TicketExample : IExamplesProvider<Ticket>
    {
        public Ticket GetExamples()
        {
            return new Ticket(
                Guid.NewGuid(),
                TicketStatus.Complete,
                new Dictionary<string, string>
                {
                    { "Action", "ProposeStreetName" },
                    { "ObjectId", "31169" },
                    { "Registry", "StreetNameRegistry" },
                    { "AggregateId", Guid.NewGuid().ToString("D") }
                },
                new TicketResult(
                    new ETagResponse(
                        "https://api.basisregisters.staging-vlaanderen.be/v2/straatnamen/3016611",
                        "D048BFCE9B392B49C352AE518F6F5393096A80ABC9A6B6DABC7CECC609B76A2264A2003CBB9DDEE44F4AB6AD7EC46960F3907171717C930371B11A3E9D01F970")));
        }
    }
}
