// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Lykke.Service.Operations.Client.AutorestClient
{
    using Lykke.Service;
    using Lykke.Service.Operations;
    using Lykke.Service.Operations.Client;
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
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<IsAliveResponse> IsAliveAsync(this IOperationsAPI operations, CancellationToken cancellationToken = default(CancellationToken))
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
            /// 'Canceled', 'Failed', 'Corrupted'
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
            /// <param name='command'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<System.Guid?> ApiOperationsCashoutByIdSwiftPostAsync(this IOperationsAPI operations, System.Guid id, CreateSwiftCashoutCommand command = default(CreateSwiftCashoutCommand), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiOperationsCashoutByIdSwiftPostWithHttpMessagesAsync(id, command, null, cancellationToken).ConfigureAwait(false))
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
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<System.Guid?> ApiOperationsCashoutByIdPostAsync(this IOperationsAPI operations, System.Guid id, CreateCashoutCommand command = default(CreateCashoutCommand), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.ApiOperationsCashoutByIdPostWithHttpMessagesAsync(id, command, null, cancellationToken).ConfigureAwait(false))
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
            /// <param name='cmd'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task ApiOperationsConfirmByIdPostAsync(this IOperationsAPI operations, System.Guid id, ConfirmCommand cmd = default(ConfirmCommand), CancellationToken cancellationToken = default(CancellationToken))
            {
                (await operations.ApiOperationsConfirmByIdPostWithHttpMessagesAsync(id, cmd, null, cancellationToken).ConfigureAwait(false)).Dispose();
            }

    }
}
