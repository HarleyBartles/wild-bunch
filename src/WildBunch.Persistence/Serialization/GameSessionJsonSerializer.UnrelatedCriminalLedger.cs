using System.Text.Json;
using WildBunch.Domain.Cases;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    public string SerializeUnrelatedCriminalLedger(UnrelatedCriminalLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        return JsonSerializer.Serialize(ledger.ToSnapshot(), Options);
    }

    public UnrelatedCriminalLedger DeserializeUnrelatedCriminalLedger(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = JsonSerializer.Deserialize<UnrelatedCriminalLedgerSnapshot>(json, Options)
            ?? throw new InvalidOperationException("Unable to deserialize UnrelatedCriminalLedgerSnapshot.");
        return UnrelatedCriminalLedger.FromSnapshot(snapshot);
    }
}
