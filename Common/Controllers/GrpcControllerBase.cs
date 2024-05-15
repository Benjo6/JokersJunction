using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace JokersJunction.Common.Controllers;

public class GrpcControllerBase<T> : ControllerBase where T : ClientBase<T>
{
    protected T Service => HttpContext.RequestServices.GetService(typeof(T)) as T ?? throw new InvalidOperationException();
}