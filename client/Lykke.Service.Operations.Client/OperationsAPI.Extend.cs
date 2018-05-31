using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Lykke.Service.Operations.Client.AutorestClient
{    

    public partial class OperationsAPI
    {
        /// <summary>
        /// Should be used to prevent memory leak in RetryPolicy
        /// </summary>
        public OperationsAPI(Uri baseUri, HttpClient client) : base(client)
        {
            Initialize();
            BaseUri = baseUri ?? throw new ArgumentNullException("baseUri");
        }
    }
}
