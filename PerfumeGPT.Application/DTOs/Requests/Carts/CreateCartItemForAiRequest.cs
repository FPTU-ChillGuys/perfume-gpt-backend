using System;
using System.Collections.Generic;
using System.Text;

namespace PerfumeGPT.Application.DTOs.Requests.Carts
{
    public class CreateCartItemForAiRequest : CreateCartItemRequest
    {
        public Guid UserId { get; set; }
    }
}
