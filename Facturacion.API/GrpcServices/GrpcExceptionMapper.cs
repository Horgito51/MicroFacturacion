using Facturacion.Business.Exceptions;
using Grpc.Core;

namespace Facturacion.API.GrpcServices;

internal static class GrpcExceptionMapper
{
    public static RpcException Map(Exception exception)
    {
        return exception switch
        {
            NotFoundException ex => new RpcException(new Status(StatusCode.NotFound, ex.Message)),
            ConflictException ex => new RpcException(new Status(StatusCode.AlreadyExists, ex.Message)),
            ValidationException ex => new RpcException(new Status(StatusCode.InvalidArgument, ex.Message)),
            _ => new RpcException(new Status(StatusCode.Internal, "Error interno en servicio gRPC de Facturacion."))
        };
    }
}
