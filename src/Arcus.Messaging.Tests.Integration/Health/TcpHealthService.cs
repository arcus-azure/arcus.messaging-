﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.Messaging.Tests.Integration.Health
{
    /// <summary>
    /// Represents a service to interact with the TCP health probe.
    /// </summary>
    public class TcpHealthService
    {
        private const string LocalAddress = "127.0.0.1";
        
        private readonly int _healthTcpPort;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpHealthService"/> class.
        /// </summary>
        /// <param name="healthTcpPort">The local health TCP port to contact the TCP health probe.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages while interacting with the TCP probe.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="healthTcpPort"/> is not a valid TCP port number.</exception>
        public TcpHealthService(int healthTcpPort, ILogger logger)
        {
            Guard.NotLessThan(healthTcpPort, 0, nameof(healthTcpPort), "Requires a TCP health port number that's above zero");
            _healthTcpPort = healthTcpPort;
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Gets the <see cref="HealthReport"/> from the exposed TCP health probe.
        /// </summary>
        public async Task<HealthReport> GetHealthReportAsync()
        {
            using (var client = new TcpClient())
            {
                _logger.LogTrace("Connecting to the TCP {Address}:{Port}...", LocalAddress, _healthTcpPort);
                await client.ConnectAsync(IPAddress.Parse(LocalAddress), _healthTcpPort);
                _logger.LogTrace("Connected to the TCP {Address}:{Port}", LocalAddress, _healthTcpPort);
                
                _logger.LogTrace("Retrieving health report...");
                using (NetworkStream clientStream = client.GetStream())
                using (var reader = new StreamReader(clientStream))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    JObject json = JObject.Load(jsonReader);

                    if (json.TryGetValue("Entries", out JToken entries)
                        && json.TryGetValue("Status", out JToken status)
                        && json.TryGetValue("TotalDuration", out JToken totalDuration))
                    {
                       IEnumerable<KeyValuePair<string, HealthReportEntry>> reportEntries = entries.Children().Select(CreateHealthReportEntry);

                        var report = new HealthReport(
                            new ReadOnlyDictionary<string, HealthReportEntry>(reportEntries.ToDictionary(entry => entry.Key, entry => entry.Value)),
                            Enum.Parse<HealthStatus>(status.Value<string>()),
                            TimeSpan.Parse(totalDuration.Value<string>()));

                        _logger.LogTrace("Health report retrieved");
                        return report;
                    }

                    return null;
                }
            }
        }

        private static KeyValuePair<string, HealthReportEntry> CreateHealthReportEntry(JToken entry)
        {
            JToken token = entry.First;

            var healthStatus = token["Status"].ToObject<HealthStatus>();
            var description = token["Description"]?.ToObject<string>();
            var duration = token["Duration"].ToObject<TimeSpan>();
            var exception = token["Exception"]?.ToObject<Exception>();
            var data = token["Data"]?.ToObject<Dictionary<string, object>>();
            var readOnlyDictionary = new ReadOnlyDictionary<string, object>(data ?? new Dictionary<string, object>());
            var tags = token["Tags"]?.ToObject<string[]>();

            return new KeyValuePair<string, HealthReportEntry>(
                entry.Path.Replace("Entries.", ""),
                new HealthReportEntry(healthStatus, description, duration, exception, readOnlyDictionary, tags));
        }
    }
}
