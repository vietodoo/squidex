﻿// ==========================================================================
//  ProjectionHelper.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;

namespace Squidex.Infrastructure.EventSourcing
{
    public static class ProjectionHelper
    {
        private const string ProjectionName = "by-{0}-{1}";
        private static readonly ConcurrentDictionary<string, bool> SubscriptionsCreated = new ConcurrentDictionary<string, bool>();

        private static string ParseFilter(string prefix, string filter)
        {
            return string.Format(CultureInfo.InvariantCulture, ProjectionName, prefix.Simplify(), filter.Simplify());
        }

        public static async Task<string> CreateProjectionAsync(this IEventStoreConnection connection, string projectionHost, string prefix, string streamFilter = null)
        {
            var streamName = ParseFilter(prefix, streamFilter);

            if (SubscriptionsCreated.TryAdd(streamName, true))
            {
                var projectsManager = await ConnectToProjections(connection, projectionHost);

                var projectionConfig =
                    $@"fromAll()
                        .when({{
                            $any: function (s, e) {{
                                if (e.streamId.indexOf('{prefix}') === 0 && /{streamFilter}/.test(e.streamId.substring({prefix.Length + 1}))) {{
                                    linkTo('{streamName}', e);
                                }}
                            }}
                        }});";

                try
                {
                    var credentials = connection.Settings.DefaultUserCredentials;

                    await projectsManager.CreateContinuousAsync($"${streamName}", projectionConfig, credentials);
                }
                catch (Exception ex)
                {
                    if (!ex.Is<ProjectionCommandConflictException>())
                    {
                        throw;
                    }
                }
            }

            return streamName;
        }

        private static async Task<ProjectionsManager> ConnectToProjections(IEventStoreConnection connection, string projectionHost)
        {
            var addressParts = projectionHost.Split(':');

            if (addressParts.Length < 2 || !int.TryParse(addressParts[1], out var port))
            {
                port = 2113;
            }

            var endpoints = await Dns.GetHostAddressesAsync(addressParts[0]);
            var endpoint = new IPEndPoint(endpoints.First(x => x.AddressFamily == AddressFamily.InterNetwork), port);

            var projectionsManager =
                new ProjectionsManager(
                    connection.Settings.Log, endpoint,
                    connection.Settings.OperationTimeout);

            return projectionsManager;
        }
    }
}
