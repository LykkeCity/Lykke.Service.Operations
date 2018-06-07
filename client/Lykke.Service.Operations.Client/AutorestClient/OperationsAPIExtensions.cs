// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Service.Operations.Client.AutorestClient
{
    using Models;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for OperationsAPI.
    /// </summary>
    public static partial class OperationsAPIExtensions
    {
            /// <summary>
            /// Checks service is alive
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            public static object IsAlive(this IOperationsAPI operations)
            {
                return operations.IsAliveAsync().GetAwaiter().GetResult();
            }

            /// <summary>
            /// Checks service is alive
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<object> IsAliveAsync(this IOperationsAPI operations, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.IsAliveWithHttpMessagesAsync(null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            public static OperationModel ApiOperationsByIdGet(this IOperationsAPI operations, System.Guid id)
            {
                return operations.ApiOperationsByIdGetAsync(id).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<OperationModel> ApiOperationsByIdGetAsync(this IOperationsAPI operations, System.Guid id, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiOperationsByIdGetWithHttpMessagesAsync(id, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='clientId'>
            /// </param>
            /// <param name='status'>
            /// Possible values include: 'Created', 'Accepted', 'Confirmed', 'Completed',
            /// 'Canceled', 'Failed'
            /// </param>
            public static IList<OperationModel> ApiOperationsByClientIdListByStatusGet(this IOperationsAPI operations, System.Guid clientId, OperationStatus status)
            {
                return operations.ApiOperationsByClientIdListByStatusGetAsync(clientId, status).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='clientId'>
            /// </param>
            /// <param name='status'>
            /// Possible values include: 'Created', 'Accepted', 'Confirmed', 'Completed',
            /// 'Canceled', 'Failed'
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<IList<OperationModel>> ApiOperationsByClientIdListByStatusGetAsync(this IOperationsAPI operations, System.Guid clientId, OperationStatus status, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiOperationsByClientIdListByStatusGetWithHttpMessagesAsync(clientId, status, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <summary>
            /// Registers a new order with attached client order Id.
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// The order Id
            /// </param>
            /// <param name='cmd'>
            /// Order related information
            /// </param>
            public static System.Guid? ApiOperationsNewOrderByIdPost(this IOperationsAPI operations, System.Guid id, CreateNewOrderCommand cmd = default(CreateNewOrderCommand))
            {
                return operations.ApiOperationsNewOrderByIdPostAsync(id, cmd).GetAwaiter().GetResult();
            }

            /// <summary>
            /// Registers a new order with attached client order Id.
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// The order Id
            /// </param>
            /// <param name='cmd'>
            /// Order related information
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<System.Guid?> ApiOperationsNewOrderByIdPostAsync(this IOperationsAPI operations, System.Guid id, CreateNewOrderCommand cmd = default(CreateNewOrderCommand), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiOperationsNewOrderByIdPostWithHttpMessagesAsync(id, cmd, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='cmd'>
            /// </param>
            public static bool? ApiOperationsPaymentByIdPut(this IOperationsAPI operations, System.Guid id, SetPaymentClientIdCommand cmd = default(SetPaymentClientIdCommand))
            {
                return operations.ApiOperationsPaymentByIdPutAsync(id, cmd).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='cmd'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<bool?> ApiOperationsPaymentByIdPutAsync(this IOperationsAPI operations, System.Guid id, SetPaymentClientIdCommand cmd = default(SetPaymentClientIdCommand), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiOperationsPaymentByIdPutWithHttpMessagesAsync(id, cmd, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='command'>
            /// </param>
            public static System.Guid? ApiOperationsPaymentByIdPost(this IOperationsAPI operations, System.Guid id, CreatePaymentCommand command = default(CreatePaymentCommand))
            {
                return operations.ApiOperationsPaymentByIdPostAsync(id, command).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='command'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<System.Guid?> ApiOperationsPaymentByIdPostAsync(this IOperationsAPI operations, System.Guid id, CreatePaymentCommand command = default(CreatePaymentCommand), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiOperationsPaymentByIdPostWithHttpMessagesAsync(id, command, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='command'>
            /// </param>
            public static System.Guid? ApiOperationsOrderByIdMarketPost(this IOperationsAPI operations, System.Guid id, CreateMarketOrderCommand command = default(CreateMarketOrderCommand))
            {
                return operations.ApiOperationsOrderByIdMarketPostAsync(id, command).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='command'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<System.Guid?> ApiOperationsOrderByIdMarketPostAsync(this IOperationsAPI operations, System.Guid id, CreateMarketOrderCommand command = default(CreateMarketOrderCommand), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiOperationsOrderByIdMarketPostWithHttpMessagesAsync(id, command, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='command'>
            /// </param>
            public static System.Guid? ApiOperationsOrderByIdLimitPost(this IOperationsAPI operations, System.Guid id, CreateLimitOrderCommand command = default(CreateLimitOrderCommand))
            {
                return operations.ApiOperationsOrderByIdLimitPostAsync(id, command).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='command'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<System.Guid?> ApiOperationsOrderByIdLimitPostAsync(this IOperationsAPI operations, System.Guid id, CreateLimitOrderCommand command = default(CreateLimitOrderCommand), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiOperationsOrderByIdLimitPostWithHttpMessagesAsync(id, command, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='cmd'>
            /// </param>
            public static System.Guid? ApiOperationsTransferByIdPost(this IOperationsAPI operations, System.Guid id, CreateTransferCommand cmd = default(CreateTransferCommand))
            {
                return operations.ApiOperationsTransferByIdPostAsync(id, cmd).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='cmd'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<System.Guid?> ApiOperationsTransferByIdPostAsync(this IOperationsAPI operations, System.Guid id, CreateTransferCommand cmd = default(CreateTransferCommand), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiOperationsTransferByIdPostWithHttpMessagesAsync(id, cmd, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            public static void ApiOperationsCancelByIdPost(this IOperationsAPI operations, System.Guid id)
            {
                operations.ApiOperationsCancelByIdPostAsync(id).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task ApiOperationsCancelByIdPostAsync(this IOperationsAPI operations, System.Guid id, CancellationToken cancellationToken = default(CancellationToken))
            {
                (await operations.ApiOperationsCancelByIdPostWithHttpMessagesAsync(id, null, cancellationToken).ConfigureAwait(false)).Dispose();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            public static void ApiOperationsCompleteByIdPost(this IOperationsAPI operations, System.Guid id)
            {
                operations.ApiOperationsCompleteByIdPostAsync(id).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task ApiOperationsCompleteByIdPostAsync(this IOperationsAPI operations, System.Guid id, CancellationToken cancellationToken = default(CancellationToken))
            {
                (await operations.ApiOperationsCompleteByIdPostWithHttpMessagesAsync(id, null, cancellationToken).ConfigureAwait(false)).Dispose();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            public static void ApiOperationsFailByIdPost(this IOperationsAPI operations, System.Guid id)
            {
                operations.ApiOperationsFailByIdPostAsync(id).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task ApiOperationsFailByIdPostAsync(this IOperationsAPI operations, System.Guid id, CancellationToken cancellationToken = default(CancellationToken))
            {
                (await operations.ApiOperationsFailByIdPostWithHttpMessagesAsync(id, null, cancellationToken).ConfigureAwait(false)).Dispose();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            public static void ApiOperationsConfirmByIdPost(this IOperationsAPI operations, System.Guid id)
            {
                operations.ApiOperationsConfirmByIdPostAsync(id).GetAwaiter().GetResult();
            }

            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='id'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task ApiOperationsConfirmByIdPostAsync(this IOperationsAPI operations, System.Guid id, CancellationToken cancellationToken = default(CancellationToken))
            {
                (await operations.ApiOperationsConfirmByIdPostWithHttpMessagesAsync(id, null, cancellationToken).ConfigureAwait(false)).Dispose();
            }

    }
}
