using System.Globalization;
using System.Text;
using Budget.Web.Data;
using Budget.Web.Domain.Merchants;
using Microsoft.EntityFrameworkCore;

namespace Budget.Web.Domain.Transactions;

/// <summary>Imports Rabobank CSV files into the database, skipping rows that already exist for the user.</summary>
public sealed class RabobankCsvImporter(BudgetDbContext db)
{
    /// <summary>
    /// Parses the uploaded Rabobank CSV and inserts the rows that are not already present for the user.
    /// </summary>
    /// <param name="fileStream">A latin-1 encoded stream of the uploaded CSV file.</param>
    /// <param name="userId">The id of the user the transactions belong to.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The most recent transaction date in the file, or <see cref="DateOnly.MinValue"/> when the file has no rows.</returns>
    public async Task<DateOnly> ProcessAsync(Stream fileStream, string userId, CancellationToken cancellationToken)
    {
        var rows = Parse(fileStream, userId);
        var maxDate = rows.Count == 0 ? DateOnly.MinValue : rows.Max(t => t.Date);

        var existing = await db.Transactions
            .Where(t => t.UserId == userId)
            .Select(t => new { t.Iban, t.FollowNumber }) // TODO: Maybe distinct it?
            .ToListAsync(cancellationToken);
        var existingKeys = existing.Select(x => (x.Iban, x.FollowNumber)).ToHashSet();

        var toAdd = rows.Where(t => !existingKeys.Contains((t.Iban, t.FollowNumber))).ToList();
        if (toAdd.Count > 0)
        {
            db.Transactions.AddRange(toAdd); // TODO: insert per record? Bulk insert? Or is it because of the stream? What is better?
            await db.SaveChangesAsync(cancellationToken);
        }

        return maxDate;
    }

    /// <summary>Parses a latin-1 encoded Rabobank CSV stream into transactions for the given user, without persisting them.</summary>
    /// <param name="fileStream">A latin-1 encoded stream of the CSV file.</param>
    /// <param name="userId">The id of the user the transactions belong to.</param>
    /// <returns>The parsed transactions; an empty list when the file has only a header or is empty.</returns>
    public static IReadOnlyList<Transaction> Parse(Stream fileStream, string userId)
    {
        using var reader = new StreamReader(fileStream, Encoding.Latin1);
        var rows = ParseCsv(reader);
        if (rows.Count == 0)
            return [];

        var header = rows[0];
        var columnIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < header.Count; i++)
            columnIndex[header[i]] = i;

        var transactions = new List<Transaction>(rows.Count - 1);
        foreach (var dataRow in rows.Skip(1))
        {
            var get = (string column) => dataRow[columnIndex[column]];
            var date = DateOnly.ParseExact(get("Datum"), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var amount = decimal.Parse(get("Bedrag").Replace(",", "."), CultureInfo.InvariantCulture);

            transactions.Add(new Transaction
            {
                Date = date,
                UserId = userId,
                Amount = amount,
                Currency = get("Munt"),
                Description = (get("Omschrijving-1") + get("Omschrijving-2") + get("Omschrijving-3")).Trim(),
                FollowNumber = int.Parse(get("Volgnr"), CultureInfo.InvariantCulture),
                Code = get("Code"),
                Iban = get("IBAN/BBAN"),
                IbanOtherParty = get("Tegenrekening IBAN/BBAN"),
                NameOtherParty = get("Naam tegenpartij"),
                NameOtherPartyNormalized = MerchantNameNormalizer.Normalize(get("Naam tegenpartij")),
            });
        }

        return transactions;
    }

    private static List<List<string>> ParseCsv(TextReader reader)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        while (reader.Peek() >= 0)
        {
            var ch = (char)reader.Read();

            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (reader.Peek() == '"')
                    {
                        field.Append('"');
                        reader.Read();
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(ch);
                }
            }
            else
            {
                switch (ch)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        row.Add(field.ToString());
                        field.Clear();
                        AddRow(rows, row);
                        row = [];
                        break;
                    default:
                        field.Append(ch);
                        break;
                }
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            AddRow(rows, row);
        }

        return rows;
    }

    private static void AddRow(List<List<string>> rows, List<string> row)
    {
        if (row.Count > 1 || row is [{ Length: > 0 }])
            rows.Add(row);
    }
}
