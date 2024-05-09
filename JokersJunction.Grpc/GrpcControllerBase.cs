using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace JokersJunction.Grpc;

public class GrpcControllerBase<T> : ControllerBase where T : ClientBase<T>
{
    protected T Service => HttpContext.RequestServices.GetService(typeof(T)) as T ?? throw new InvalidOperationException();
}