﻿using Opc.Ua.Client;
using Opc.Ua;

namespace OPC_UA.Client
{
    public static class ClientUtils
    {
        public static EndpointDescription SelectEndpoint(Uri discoveryUrl, bool useSecurity)
        {
            var configuration = EndpointConfiguration.Create();
            configuration.OperationTimeout = 5000;
            EndpointDescription endpointDescription1 = null;
            using (var discoveryClient = DiscoveryClient.Create(discoveryUrl, configuration))
            {
                var endpoints = discoveryClient.GetEndpoints(null);
                foreach (var endpointDescription2 in endpoints.Where(endpointDescription2 => endpointDescription2.EndpointUrl.StartsWith(discoveryUrl.Scheme)))
                {
                    if (useSecurity)
                    {
                        if (endpointDescription2.SecurityMode == MessageSecurityMode.None)
                            continue;
                    }
                    else if (endpointDescription2.SecurityMode != MessageSecurityMode.None)
                        continue;
                    if (endpointDescription1 == null)
                        endpointDescription1 = endpointDescription2;
                    if (endpointDescription2.SecurityLevel > endpointDescription1.SecurityLevel)
                        endpointDescription1 = endpointDescription2;
                }
                if (endpointDescription1 == null)
                {
                    if (endpoints.Count > 0)
                        endpointDescription1 = endpoints[0];
                }
            }
            var uri = Utils.ParseUri(endpointDescription1.EndpointUrl);
            if (uri != null && uri.Scheme == discoveryUrl.Scheme)
                endpointDescription1.EndpointUrl = new UriBuilder(uri)
                {
                    Host = discoveryUrl.DnsSafeHost,
                    Port = discoveryUrl.Port
                }.ToString();
            return endpointDescription1;
        }

        public static ReferenceDescriptionCollection Browse(Session session, NodeId nodeId)
        {
            var desc = new BrowseDescription
            {
                NodeId = nodeId,
                BrowseDirection = BrowseDirection.Forward,
                IncludeSubtypes = true,
                NodeClassMask = 0U,
                ResultMask = 63U,
            };
            return Browse(session, desc, true);
        }

        public static ReferenceDescriptionCollection Browse(Session session, BrowseDescription nodeToBrowse, bool throwOnError)
        {
            try
            {
                var descriptionCollection = new ReferenceDescriptionCollection();
                var nodesToBrowse = new BrowseDescriptionCollection { nodeToBrowse };
                BrowseResultCollection results;
                DiagnosticInfoCollection diagnosticInfos;
                session.Browse(null, null, 0U, nodesToBrowse, out results, out diagnosticInfos);
                ClientBase.ValidateResponse(results, nodesToBrowse);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToBrowse);
                while (!StatusCode.IsBad(results[0].StatusCode))
                {
                    for (var index = 0; index < results[0].References.Count; ++index)
                        descriptionCollection.Add(results[0].References[index]);
                    if (results[0].References.Count == 0 || results[0].ContinuationPoint == null)
                        return descriptionCollection;
                    var continuationPoints = new ByteStringCollection();
                    continuationPoints.Add(results[0].ContinuationPoint);
                    session.BrowseNext(null, false, continuationPoints, out results, out diagnosticInfos);
                    ClientBase.ValidateResponse(results, continuationPoints);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, continuationPoints);
                }
                throw new ServiceResultException(results[0].StatusCode);
            }
            catch (Exception ex)
            {
                if (throwOnError)
                    throw new ServiceResultException(ex, 2147549184U);
                return null;
            }
        }
    }
}
