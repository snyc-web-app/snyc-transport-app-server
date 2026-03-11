using System.Globalization;
using Libsql.Client;
using SNYC_Transport.Models;

namespace SNYC_Transport.Services;

public class TursoTransportRequestService : ITransportRequestService
{
    private readonly Task<IDatabaseClient> clientTask;

    public TursoTransportRequestService(IConfiguration configuration)
    {
        var url = (configuration["Turso:Url"] ?? throw new InvalidOperationException("Turso:Url is required.")).Trim();
        var authToken = (configuration["Turso:AuthToken"] ?? throw new InvalidOperationException("Turso:AuthToken is required.")).Trim();
        var replicaPath = configuration["Turso:ReplicaPath"]?.Trim();

        clientTask = InitializeClientAsync(url, authToken, replicaPath);
    }

    public async Task<IReadOnlyList<TransportRequests>> GetAllAsync()
    {
        var client = await clientTask;
        await client.Sync();

        var result = await client.Execute(
            "SELECT id, user_id, destination, passenger_count, status, compliance_status, compliance_reviewed_by, compliance_reviewed_at, admin_status, admin_reviewed_by, admin_reviewed_at, created_at_utc, updated_at_utc FROM transport_requests ORDER BY updated_at_utc DESC");

        var requests = new List<TransportRequests>();
        var columns = result.Columns.ToList();

        foreach (var row in result.Rows)
        {
            var values = row.ToList();
            var valueMap = new Dictionary<string, Value>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < columns.Count && i < values.Count; i++)
            {
                valueMap[columns[i]] = values[i];
            }

            requests.Add(new TransportRequests
            {
                Id = Guid.TryParse(GetText(valueMap, "id"), out var id) ? id : Guid.NewGuid(),
                UserId = Guid.TryParse(GetText(valueMap, "user_id"), out var userId) ? userId : Guid.Empty,
                Destination = GetText(valueMap, "destination"),
                passengerCount = GetInteger(valueMap, "passenger_count"),
                status = GetText(valueMap, "status"),
                complianceStatus = GetText(valueMap, "compliance_status"),
                complianceReviewedBy = GetText(valueMap, "compliance_reviewed_by"),
                complianceReviewedAt = GetText(valueMap, "compliance_reviewed_at"),
                adminStatus = GetText(valueMap, "admin_status"),
                adminReviewedBy = GetText(valueMap, "admin_reviewed_by"),
                adminReviewedAt = GetText(valueMap, "admin_reviewed_at"),
                CreatedAtUtc = DateTime.TryParse(
                    GetText(valueMap, "created_at_utc"),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var createdAtUtc)
                    ? createdAtUtc
                    : DateTime.UtcNow,
                UpdatedAtUtc = DateTime.TryParse(
                    GetText(valueMap, "updated_at_utc"),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var updatedAtUtc)
                    ? updatedAtUtc
                    : DateTime.UtcNow
            });
        }

        return requests;
    }

    public async Task<TransportRequests> CreateAsync(TransportRequestInput input)
    {
        var request = new TransportRequests
        {
            UserId = input.UserId ?? Guid.Empty,
            Destination = input.Destination.Trim(),
            passengerCount = input.PassengerCount ?? 1,
            status = "Pending",
            complianceStatus = "Pending",
            adminStatus = "Pending",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var client = await clientTask;

        await client.Execute(
            "INSERT INTO transport_requests (id, user_id, destination, passenger_count, status, compliance_status, compliance_reviewed_by, compliance_reviewed_at, admin_status, admin_reviewed_by, admin_reviewed_at, created_at_utc, updated_at_utc) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
            new object[]
            {
                request.Id.ToString(),
                request.UserId.ToString(),
                request.Destination,
                request.passengerCount,
                request.status,
                request.complianceStatus,
                request.complianceReviewedBy,
                request.complianceReviewedAt,
                request.adminStatus,
                request.adminReviewedBy,
                request.adminReviewedAt,
                request.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                request.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture)
            });

        await client.Sync();
        return request;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        var client = await clientTask;
        var result = await client.Execute(
            "UPDATE transport_requests SET status = ?, admin_status = ?, updated_at_utc = ? WHERE id = ?",
            new object[] { status, status, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), id.ToString() });

        await client.Sync();
        return result.RowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var client = await clientTask;
        var result = await client.Execute(
            "DELETE FROM transport_requests WHERE id = ?",
            new object[] { id.ToString() });

        await client.Sync();
        return result.RowsAffected > 0;
    }

    private static async Task<IDatabaseClient> InitializeClientAsync(string url, string authToken, string? replicaPath)
    {
        var normalizedUrl = NormalizeConnectionUrl(url);

        var client = await DatabaseClient.Create(options =>
        {
            options.Url = normalizedUrl;
            options.AuthToken = authToken;
            options.UseHttps = normalizedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(replicaPath))
            {
                options.ReplicaPath = replicaPath;
            }
        });

        await client.Execute(@"
            CREATE TABLE IF NOT EXISTS transport_requests (
                id TEXT PRIMARY KEY,
                user_id TEXT NOT NULL,
                destination TEXT NOT NULL,
                passenger_count INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL,
                status TEXT NOT NULL,
                compliance_status TEXT NOT NULL,
                compliance_reviewed_by TEXT NOT NULL,
                compliance_reviewed_at TEXT NOT NULL,
                admin_status TEXT NOT NULL,
                admin_reviewed_by TEXT NOT NULL,
                admin_reviewed_at TEXT NOT NULL
            );
        ");

        await EnsureTransportRequestsSchemaAsync(client);

        await client.Sync();
        return client;
    }

    private static string NormalizeConnectionUrl(string url)
    {
        if (url.StartsWith("libsql://", StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + url["libsql://".Length..];
        }

        return url;
    }

    private static async Task EnsureTransportRequestsSchemaAsync(IDatabaseClient client)
    {
        var schemaResult = await client.Execute("SELECT name FROM pragma_table_info('transport_requests')");
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in schemaResult.Rows)
        {
            var values = row.ToList();
            if (values.Count == 0)
            {
                continue;
            }

            var nameValue = values[0];
            if (nameValue is Text text && !string.IsNullOrWhiteSpace(text.Value))
            {
                existingColumns.Add(text.Value);
            }
        }

        await AddMissingColumnAsync(client, existingColumns, "user_id", "TEXT NOT NULL DEFAULT ''");
        await AddMissingColumnAsync(client, existingColumns, "destination", "TEXT NOT NULL DEFAULT ''");
        await AddMissingColumnAsync(client, existingColumns, "passenger_count", "INTEGER NOT NULL DEFAULT 1");
        await AddMissingColumnAsync(client, existingColumns, "status", "TEXT NOT NULL DEFAULT 'Pending'");
        await AddMissingColumnAsync(client, existingColumns, "compliance_status", "TEXT NOT NULL DEFAULT 'Pending'");
        await AddMissingColumnAsync(client, existingColumns, "compliance_reviewed_by", "TEXT NOT NULL DEFAULT ''");
        await AddMissingColumnAsync(client, existingColumns, "compliance_reviewed_at", "TEXT NOT NULL DEFAULT ''");
        await AddMissingColumnAsync(client, existingColumns, "admin_status", "TEXT NOT NULL DEFAULT 'Pending'");
        await AddMissingColumnAsync(client, existingColumns, "admin_reviewed_by", "TEXT NOT NULL DEFAULT ''");
        await AddMissingColumnAsync(client, existingColumns, "admin_reviewed_at", "TEXT NOT NULL DEFAULT ''");
        await AddMissingColumnAsync(client, existingColumns, "created_at_utc", "TEXT NOT NULL DEFAULT ''");
        await AddMissingColumnAsync(client, existingColumns, "updated_at_utc", "TEXT NOT NULL DEFAULT ''");
    }

    private static async Task AddMissingColumnAsync(
        IDatabaseClient client,
        ISet<string> existingColumns,
        string columnName,
        string columnDefinition)
    {
        if (existingColumns.Contains(columnName))
        {
            return;
        }

        await client.Execute($"ALTER TABLE transport_requests ADD COLUMN {columnName} {columnDefinition}");
        existingColumns.Add(columnName);
    }

    private static string GetText(IReadOnlyDictionary<string, Value> values, string key)
    {
        if (!values.TryGetValue(key, out var value))
        {
            return string.Empty;
        }

        return value switch
        {
            Text text => text.Value,
            Integer integer => integer.Value.ToString(CultureInfo.InvariantCulture),
            Real real => real.Value.ToString(CultureInfo.InvariantCulture),
            _ => string.Empty
        };
    }

    private static int GetInteger(IReadOnlyDictionary<string, Value> values, string key)
    {
        if (!values.TryGetValue(key, out var value))
        {
            return 0;
        }

        return value switch
        {
            Integer integer => integer.Value,
            Text text when int.TryParse(text.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            Real real => (int)real.Value,
            _ => 0
        };
    }
}
