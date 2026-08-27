using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Tax;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Infrastructure.Services
{
    /// <summary>
    /// Tax report service with Indian Income Tax computation (FY 2025-26).
    /// Supports both Old and New tax regimes with correct slab rates,
    /// rebate u/s 87A, surcharge, and 4% Health &amp; Education Cess.
    /// Capital gains: STCG 20%, LTCG 12.5% (above ₹1.25L exemption).
    /// </summary>
    public class TaxReportService : ITaxReportService
    {
        private readonly AppDbContext _db;

        public TaxReportService(AppDbContext db)
        {
            _db = db;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CRUD
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<TaxEntryDto> CreateAsync(Guid userId, CreateTaxEntryDto dto)
        {
            var validTypes = new[] { "income", "deduction", "capital_gain" };
            if (!validTypes.Contains(dto.EntryType))
                throw new ArgumentException("EntryType must be 'income', 'deduction', or 'capital_gain'.");

            var entry = new TaxEntry
            {
                UserId = userId,
                FinancialYear = dto.FinancialYear,
                Category = dto.Category,
                Description = dto.Description,
                Amount = dto.Amount,
                EntryType = dto.EntryType,
                Section = dto.Section,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.TaxEntries.Add(entry);
            await _db.SaveChangesAsync();
            return MapToDto(entry);
        }

        public async Task<List<TaxEntryDto>> GetAllAsync(Guid userId, string financialYear)
        {
            var entries = await _db.TaxEntries
                .AsNoTracking()
                .Where(e => e.UserId == userId && e.FinancialYear == financialYear)
                .OrderBy(e => e.EntryType)
                .ThenBy(e => e.Category)
                .ToListAsync();

            return entries.Select(MapToDto).ToList();
        }

        public async Task<TaxEntryDto> UpdateAsync(Guid userId, Guid entryId, UpdateTaxEntryDto dto)
        {
            var entry = await _db.TaxEntries
                .FirstOrDefaultAsync(e => e.TaxEntryId == entryId && e.UserId == userId)
                ?? throw new KeyNotFoundException("Tax entry not found.");

            if (dto.Category != null) entry.Category = dto.Category;
            if (dto.Description != null) entry.Description = dto.Description;
            if (dto.Amount.HasValue) entry.Amount = dto.Amount.Value;
            if (dto.EntryType != null)
            {
                var validTypes = new[] { "income", "deduction", "capital_gain" };
                if (!validTypes.Contains(dto.EntryType))
                    throw new ArgumentException("EntryType must be 'income', 'deduction', or 'capital_gain'.");
                entry.EntryType = dto.EntryType;
            }
            if (dto.Section != null) entry.Section = dto.Section;
            entry.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return MapToDto(entry);
        }

        public async Task<bool> DeleteAsync(Guid userId, Guid entryId)
        {
            var entry = await _db.TaxEntries
                .FirstOrDefaultAsync(e => e.TaxEntryId == entryId && e.UserId == userId);
            if (entry == null) return false;

            _db.TaxEntries.Remove(entry);
            await _db.SaveChangesAsync();
            return true;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // TAX COMPUTATION — Indian Income Tax Act (FY 2025-26 / AY 2026-27)
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<TaxComputationDto> ComputeTaxAsync(Guid userId, string financialYear)
        {
            var entries = await _db.TaxEntries
                .AsNoTracking()
                .Where(e => e.UserId == userId && e.FinancialYear == financialYear)
                .ToListAsync();

            var entryDtos = entries.Select(MapToDto).ToList();

            // ── Classify entries ──
            var grossIncome = entries
                .Where(e => e.EntryType == "income")
                .Sum(e => e.Amount);

            var stcg = entries
                .Where(e => e.EntryType == "capital_gain" &&
                       (e.Category.Contains("Short", StringComparison.OrdinalIgnoreCase) ||
                        e.Category.Contains("STCG", StringComparison.OrdinalIgnoreCase)))
                .Sum(e => e.Amount);

            var ltcg = entries
                .Where(e => e.EntryType == "capital_gain" &&
                       (e.Category.Contains("Long", StringComparison.OrdinalIgnoreCase) ||
                        e.Category.Contains("LTCG", StringComparison.OrdinalIgnoreCase)))
                .Sum(e => e.Amount);

            var totalCapitalGains = stcg + ltcg;

            var allDeductions = entries
                .Where(e => e.EntryType == "deduction")
                .ToList();

            var totalDeductions = allDeductions.Sum(e => e.Amount);

            // ── Compute both regimes ──
            var newRegime = ComputeNewRegime(grossIncome, stcg, ltcg, allDeductions);
            var oldRegime = ComputeOldRegime(grossIncome, stcg, ltcg, allDeductions);

            var recommended = newRegime.TotalTax <= oldRegime.TotalTax ? "new" : "old";
            var taxSaved = Math.Abs(oldRegime.TotalTax - newRegime.TotalTax);

            return new TaxComputationDto
            {
                FinancialYear = financialYear,
                GrossIncome = grossIncome,
                CapitalGains = totalCapitalGains,
                ShortTermCapitalGains = stcg,
                LongTermCapitalGains = ltcg,
                TotalDeductions = totalDeductions,
                Entries = entryDtos,
                NewRegime = newRegime,
                OldRegime = oldRegime,
                RecommendedRegime = recommended,
                TaxSaved = taxSaved
            };
        }

        // ── NEW REGIME (default from FY 2023-24, updated FY 2024-25) ─────────

        private TaxRegimeResult ComputeNewRegime(decimal grossIncome, decimal stcg, decimal ltcg, List<TaxEntry> deductions)
        {
            // New regime: only standard deduction allowed (₹75,000 from FY 2024-25)
            const decimal standardDeduction = 75000m;

            var taxableIncome = Math.Max(0, grossIncome - standardDeduction);
            var incomeTax = ComputeNewRegimeSlabs(taxableIncome, out var slabs);

            // Rebate u/s 87A: Full rebate if total income ≤ ₹7,00,000 (New Regime)
            var rebate87A = 0m;
            if (taxableIncome <= 700000m)
            {
                rebate87A = incomeTax;
                incomeTax = 0;
            }

            // Capital gains tax (separate from slab, no rebate applicable)
            var stcgTax = stcg * 0.20m;                              // STCG 20% (Finance Act 2024)
            var ltcgExemption = Math.Min(ltcg, 125000m);             // ₹1.25L exemption
            var ltcgTax = Math.Max(0, ltcg - ltcgExemption) * 0.125m; // LTCG 12.5% (Finance Act 2024)

            var totalBeforeSurcharge = incomeTax + stcgTax + ltcgTax;

            // Surcharge (on income tax, applicable if total income > ₹50L)
            var totalIncomeForSurcharge = taxableIncome + stcg + ltcg;
            var surcharge = ComputeSurchargeNewRegime(totalBeforeSurcharge, totalIncomeForSurcharge);

            // 4% Health & Education Cess
            var cess = (totalBeforeSurcharge + surcharge) * 0.04m;

            var totalTax = totalBeforeSurcharge + surcharge + cess;

            return new TaxRegimeResult
            {
                RegimeName = "New Regime (FY 2025-26)",
                StandardDeduction = standardDeduction,
                TotalDeductions = standardDeduction,
                TaxableIncome = taxableIncome,
                IncomeTax = incomeTax,
                StcgTax = Math.Round(stcgTax, 0),
                LtcgTax = Math.Round(ltcgTax, 0),
                Surcharge = Math.Round(surcharge, 0),
                HealthEducationCess = Math.Round(cess, 0),
                Rebate87A = Math.Round(rebate87A, 0),
                TotalTax = Math.Round(totalTax, 0),
                SlabBreakdown = slabs
            };
        }

        private decimal ComputeNewRegimeSlabs(decimal taxableIncome, out List<TaxSlabDetailDto> slabs)
        {
            slabs = new List<TaxSlabDetailDto>();
            decimal tax = 0;
            decimal remaining = taxableIncome;

            // New Regime slabs (FY 2025-26)
            var brackets = new (decimal limit, decimal rate, string label)[]
            {
                (300000m,  0.00m, "Up to ₹3,00,000"),
                (400000m,  0.05m, "₹3,00,001 – ₹7,00,000"),
                (300000m,  0.10m, "₹7,00,001 – ₹10,00,000"),
                (200000m,  0.15m, "₹10,00,001 – ₹12,00,000"),
                (300000m,  0.20m, "₹12,00,001 – ₹15,00,000"),
                (decimal.MaxValue, 0.30m, "Above ₹15,00,000"),
            };

            foreach (var (limit, rate, label) in brackets)
            {
                if (remaining <= 0) break;
                var taxable = Math.Min(remaining, limit);
                var slabTax = Math.Round(taxable * rate, 0);
                slabs.Add(new TaxSlabDetailDto
                {
                    Slab = label,
                    Rate = rate * 100,
                    TaxableAmount = taxable,
                    Tax = slabTax
                });
                tax += slabTax;
                remaining -= taxable;
            }

            return Math.Round(tax, 0);
        }

        private decimal ComputeSurchargeNewRegime(decimal incomeTax, decimal totalIncome)
        {
            // New regime surcharge rates (FY 2025-26)
            // Marginal relief applies but simplified here
            if (totalIncome <= 5000000m) return 0;
            if (totalIncome <= 10000000m) return incomeTax * 0.10m;
            if (totalIncome <= 20000000m) return incomeTax * 0.15m;
            // Max surcharge rate capped at 25% in new regime
            return incomeTax * 0.25m;
        }

        // ── OLD REGIME ───────────────────────────────────────────────────────

        private TaxRegimeResult ComputeOldRegime(decimal grossIncome, decimal stcg, decimal ltcg, List<TaxEntry> deductions)
        {
            // Old regime: standard deduction ₹50,000
            const decimal standardDeduction = 50000m;

            // Apply all deductions (80C max 1.5L, 80D max 25K/50K etc.)
            var deductionTotal = deductions.Sum(d => d.Amount);
            var totalDeductions = standardDeduction + deductionTotal;

            var taxableIncome = Math.Max(0, grossIncome - totalDeductions);
            var incomeTax = ComputeOldRegimeSlabs(taxableIncome, out var slabs);

            // Rebate u/s 87A: ₹12,500 if total income ≤ ₹5,00,000 (Old Regime)
            var rebate87A = 0m;
            if (taxableIncome <= 500000m)
            {
                rebate87A = Math.Min(incomeTax, 12500m);
                incomeTax = Math.Max(0, incomeTax - rebate87A);
            }

            // Capital gains (same rates as new regime)
            var stcgTax = stcg * 0.20m;
            var ltcgExemption = Math.Min(ltcg, 125000m);
            var ltcgTax = Math.Max(0, ltcg - ltcgExemption) * 0.125m;

            var totalBeforeSurcharge = incomeTax + stcgTax + ltcgTax;

            // Surcharge
            var totalIncomeForSurcharge = taxableIncome + stcg + ltcg;
            var surcharge = ComputeSurchargeOldRegime(totalBeforeSurcharge, totalIncomeForSurcharge);

            // 4% Health & Education Cess
            var cess = (totalBeforeSurcharge + surcharge) * 0.04m;

            var totalTax = totalBeforeSurcharge + surcharge + cess;

            return new TaxRegimeResult
            {
                RegimeName = "Old Regime (FY 2025-26)",
                StandardDeduction = standardDeduction,
                TotalDeductions = totalDeductions,
                TaxableIncome = taxableIncome,
                IncomeTax = incomeTax,
                StcgTax = Math.Round(stcgTax, 0),
                LtcgTax = Math.Round(ltcgTax, 0),
                Surcharge = Math.Round(surcharge, 0),
                HealthEducationCess = Math.Round(cess, 0),
                Rebate87A = Math.Round(rebate87A, 0),
                TotalTax = Math.Round(totalTax, 0),
                SlabBreakdown = slabs
            };
        }

        private decimal ComputeOldRegimeSlabs(decimal taxableIncome, out List<TaxSlabDetailDto> slabs)
        {
            slabs = new List<TaxSlabDetailDto>();
            decimal tax = 0;
            decimal remaining = taxableIncome;

            // Old Regime slabs (FY 2025-26) — unchanged from FY 2014-15
            var brackets = new (decimal limit, decimal rate, string label)[]
            {
                (250000m,  0.00m, "Up to ₹2,50,000"),
                (250000m,  0.05m, "₹2,50,001 – ₹5,00,000"),
                (500000m,  0.20m, "₹5,00,001 – ₹10,00,000"),
                (decimal.MaxValue, 0.30m, "Above ₹10,00,000"),
            };

            foreach (var (limit, rate, label) in brackets)
            {
                if (remaining <= 0) break;
                var taxable = Math.Min(remaining, limit);
                var slabTax = Math.Round(taxable * rate, 0);
                slabs.Add(new TaxSlabDetailDto
                {
                    Slab = label,
                    Rate = rate * 100,
                    TaxableAmount = taxable,
                    Tax = slabTax
                });
                tax += slabTax;
                remaining -= taxable;
            }

            return Math.Round(tax, 0);
        }

        private decimal ComputeSurchargeOldRegime(decimal incomeTax, decimal totalIncome)
        {
            // Old regime surcharge rates
            if (totalIncome <= 5000000m) return 0;
            if (totalIncome <= 10000000m) return incomeTax * 0.10m;
            if (totalIncome <= 20000000m) return incomeTax * 0.15m;
            if (totalIncome <= 50000000m) return incomeTax * 0.25m;
            return incomeTax * 0.37m;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // PDF REPORT GENERATION (plain-text format as PDF-ready content)
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<byte[]> GenerateReportPdfAsync(Guid userId, string financialYear)
        {
            var computation = await ComputeTaxAsync(userId, financialYear);
            var sb = new StringBuilder();

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"         INCOME TAX COMPUTATION REPORT — FY {computation.FinancialYear}");
            sb.AppendLine($"         Assessment Year: 20{computation.FinancialYear.Split('-')[1]}-{int.Parse("20" + computation.FinancialYear.Split('-')[1]) + 1}");
            sb.AppendLine($"         Generated: {DateTime.Now:dd MMM yyyy, hh:mm tt}");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();

            // ── Income Summary ──
            sb.AppendLine("━━━ INCOME SUMMARY ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            foreach (var entry in computation.Entries.Where(e => e.EntryType == "income"))
                sb.AppendLine($"  {entry.Category,-35} ₹{entry.Amount,15:N0}");
            sb.AppendLine($"  {"Gross Income",-35} ₹{computation.GrossIncome,15:N0}");
            sb.AppendLine();

            // ── Capital Gains ──
            if (computation.CapitalGains > 0)
            {
                sb.AppendLine("━━━ CAPITAL GAINS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                foreach (var entry in computation.Entries.Where(e => e.EntryType == "capital_gain"))
                    sb.AppendLine($"  {entry.Category,-35} ₹{entry.Amount,15:N0}");
                sb.AppendLine($"  {"Total Capital Gains",-35} ₹{computation.CapitalGains,15:N0}");
                sb.AppendLine($"    STCG (taxed at 20%)              ₹{computation.ShortTermCapitalGains,15:N0}");
                sb.AppendLine($"    LTCG (12.5% above ₹1.25L)        ₹{computation.LongTermCapitalGains,15:N0}");
                sb.AppendLine();
            }

            // ── Deductions ──
            var deductionEntries = computation.Entries.Where(e => e.EntryType == "deduction").ToList();
            if (deductionEntries.Any())
            {
                sb.AppendLine("━━━ DEDUCTIONS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                foreach (var entry in deductionEntries)
                {
                    var section = string.IsNullOrEmpty(entry.Section) ? "" : $" (Sec {entry.Section})";
                    sb.AppendLine($"  {entry.Category + section,-35} ₹{entry.Amount,15:N0}");
                }
                sb.AppendLine($"  {"Total Deductions",-35} ₹{computation.TotalDeductions,15:N0}");
                sb.AppendLine();
            }

            // ── New Regime ──
            AppendRegimeDetails(sb, computation.NewRegime);

            // ── Old Regime ──
            AppendRegimeDetails(sb, computation.OldRegime);

            // ── Recommendation ──
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  RECOMMENDED REGIME: {computation.RecommendedRegime.ToUpper()} REGIME");
            sb.AppendLine($"  Tax Saved:         ₹{computation.TaxSaved:N0}");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("  Disclaimer: This is an estimate based on the data provided.");
            sb.AppendLine("  Consult a qualified CA for final tax filing.");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private void AppendRegimeDetails(StringBuilder sb, TaxRegimeResult regime)
        {
            sb.AppendLine($"━━━ {regime.RegimeName.ToUpper()} ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"  Standard Deduction                 ₹{regime.StandardDeduction,15:N0}");
            sb.AppendLine($"  Total Deductions                   ₹{regime.TotalDeductions,15:N0}");
            sb.AppendLine($"  Taxable Income                     ₹{regime.TaxableIncome,15:N0}");
            sb.AppendLine();
            sb.AppendLine("  Slab Breakdown:");
            foreach (var slab in regime.SlabBreakdown)
                sb.AppendLine($"    {slab.Slab,-30} {slab.Rate,5:N0}%   ₹{slab.TaxableAmount,12:N0}  →  ₹{slab.Tax,10:N0}");
            sb.AppendLine();
            sb.AppendLine($"  Income Tax (on slabs)              ₹{regime.IncomeTax,15:N0}");
            if (regime.Rebate87A > 0)
                sb.AppendLine($"  Less: Rebate u/s 87A              (₹{regime.Rebate87A,14:N0})");
            sb.AppendLine($"  STCG Tax (20%)                     ₹{regime.StcgTax,15:N0}");
            sb.AppendLine($"  LTCG Tax (12.5%)                   ₹{regime.LtcgTax,15:N0}");
            sb.AppendLine($"  Surcharge                          ₹{regime.Surcharge,15:N0}");
            sb.AppendLine($"  Health & Education Cess (4%)       ₹{regime.HealthEducationCess,15:N0}");
            sb.AppendLine($"  ─────────────────────────────────────────────────────────");
            sb.AppendLine($"  TOTAL TAX                          ₹{regime.TotalTax,15:N0}");
            sb.AppendLine();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // MAPPING
        // ═══════════════════════════════════════════════════════════════════════

        private static TaxEntryDto MapToDto(TaxEntry e) => new()
        {
            TaxEntryId = e.TaxEntryId,
            FinancialYear = e.FinancialYear,
            Category = e.Category,
            Description = e.Description,
            Amount = e.Amount,
            EntryType = e.EntryType,
            Section = e.Section,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
