
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
    public class TaxReportService : ITaxReportService
    {
        private readonly AppDbContext _db;

        public TaxReportService(AppDbContext db)
        {
            _db = db;
        }

        // ================================================================
        // CREATE
        // ================================================================

        public async Task<TaxEntryDto> CreateAsync(
            Guid userId,
            CreateTaxEntryDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.Amount < 0)
                throw new ArgumentException(
                    "Amount cannot be negative.");

            if (string.IsNullOrWhiteSpace(dto.FinancialYear))
                throw new ArgumentException(
                    "Financial year is required.");

            var validTypes = new[]
            {
                "income",
                "deduction",
                "capital_gain"
            };

            var entryType =
                dto.EntryType?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(entryType) ||
                !validTypes.Contains(entryType))
            {
                throw new ArgumentException(
                    "EntryType must be income, deduction, or capital_gain.");
            }

            var entry = new TaxEntry
            {
                UserId = userId,
                FinancialYear = dto.FinancialYear.Trim(),
                Category = dto.Category?.Trim() ?? string.Empty,
                Description = dto.Description?.Trim() ?? string.Empty,
                Amount = dto.Amount,
                EntryType = entryType,
                Section = dto.Section?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.TaxEntries.Add(entry);

            await _db.SaveChangesAsync();

            return MapToDto(entry);
        }

        // ================================================================
        // GET ALL
        // ================================================================

        public async Task<List<TaxEntryDto>> GetAllAsync(
            Guid userId,
            string financialYear)
        {
            var entries = await _db.TaxEntries
                .AsNoTracking()
                .Where(e =>
                    e.UserId == userId &&
                    e.FinancialYear == financialYear)
                .OrderBy(e => e.EntryType)
                .ThenBy(e => e.Category)
                .ToListAsync();

            return entries
                .Select(MapToDto)
                .ToList();
        }

        // ================================================================
        // UPDATE
        // ================================================================

        public async Task<TaxEntryDto> UpdateAsync(
            Guid userId,
            Guid entryId,
            UpdateTaxEntryDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var entry = await _db.TaxEntries
                .FirstOrDefaultAsync(e =>
                    e.TaxEntryId == entryId &&
                    e.UserId == userId);

            if (entry == null)
                throw new KeyNotFoundException(
                    "Tax entry not found.");

            if (dto.Category != null)
                entry.Category = dto.Category.Trim();

            if (dto.Description != null)
                entry.Description = dto.Description.Trim();

            if (dto.Amount.HasValue)
            {
                if (dto.Amount.Value < 0)
                    throw new ArgumentException(
                        "Amount cannot be negative.");

                entry.Amount = dto.Amount.Value;
            }

            if (dto.EntryType != null)
            {
                var entryType =
                    dto.EntryType.Trim().ToLowerInvariant();

                var validTypes = new[]
                {
                    "income",
                    "deduction",
                    "capital_gain"
                };

                if (!validTypes.Contains(entryType))
                    throw new ArgumentException(
                        "EntryType must be income, deduction, or capital_gain.");

                entry.EntryType = entryType;
            }

            if (dto.Section != null)
                entry.Section = dto.Section.Trim();

            entry.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return MapToDto(entry);
        }

        // ================================================================
        // DELETE
        // ================================================================

        public async Task<bool> DeleteAsync(
            Guid userId,
            Guid entryId)
        {
            var entry = await _db.TaxEntries
                .FirstOrDefaultAsync(e =>
                    e.TaxEntryId == entryId &&
                    e.UserId == userId);

            if (entry == null)
                return false;

            _db.TaxEntries.Remove(entry);

            await _db.SaveChangesAsync();

            return true;
        }

        // ================================================================
        // MAIN TAX CALCULATION
        // ================================================================

        public async Task<TaxComputationDto> ComputeTaxAsync(
            Guid userId,
            string financialYear)
        {
            if (string.IsNullOrWhiteSpace(financialYear))
                throw new ArgumentException(
                    "Financial year is required.");

            var entries = await _db.TaxEntries
                .AsNoTracking()
                .Where(e =>
                    e.UserId == userId &&
                    e.FinancialYear == financialYear)
                .ToListAsync();

            var entryDtos = entries
                .Select(MapToDto)
                .ToList();

            // ============================================================
            // INCOME
            // ============================================================

            var incomeEntries = entries
                .Where(e =>
                    string.Equals(
                        e.EntryType,
                        "income",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            var grossIncome = incomeEntries.Sum(e =>
                Math.Max(0m, e.Amount));

            // ============================================================
            // CAPITAL GAINS
            // ============================================================

            var capitalGainEntries = entries
                .Where(e =>
                    string.Equals(
                        e.EntryType,
                        "capital_gain",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            var stcg = capitalGainEntries
                .Where(IsShortTermCapitalGain)
                .Sum(e => Math.Max(0m, e.Amount));

            var ltcg = capitalGainEntries
                .Where(IsLongTermCapitalGain)
                .Sum(e => Math.Max(0m, e.Amount));

            var totalCapitalGains =
                stcg + ltcg;

            // ============================================================
            // DEDUCTIONS
            // ============================================================

            var deductionEntries = entries
                .Where(e =>
                    string.Equals(
                        e.EntryType,
                        "deduction",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            var totalEnteredDeductions =
                deductionEntries.Sum(e =>
                    Math.Max(0m, e.Amount));

            // ============================================================
            // NEW REGIME
            // ============================================================

            var newRegime =
                ComputeNewRegime(
                    grossIncome,
                    stcg,
                    ltcg);

            // ============================================================
            // OLD REGIME
            // ============================================================

            var oldRegime =
                ComputeOldRegime(
                    grossIncome,
                    stcg,
                    ltcg,
                    deductionEntries);

            // ============================================================
            // RECOMMENDATION
            // ============================================================

            var recommendedRegime =
                newRegime.TotalTax <= oldRegime.TotalTax
                    ? "new"
                    : "old";

            var taxSaved =
                Math.Abs(
                    oldRegime.TotalTax -
                    newRegime.TotalTax);

            return new TaxComputationDto
            {
                FinancialYear = financialYear,

                GrossIncome = grossIncome,

                CapitalGains = totalCapitalGains,

                ShortTermCapitalGains = stcg,

                LongTermCapitalGains = ltcg,

                TotalDeductions =
                    totalEnteredDeductions,

                Entries = entryDtos,

                NewRegime = newRegime,

                OldRegime = oldRegime,

                RecommendedRegime =
                    recommendedRegime,

                TaxSaved =
                    Math.Round(
                        taxSaved,
                        0)
            };
        }

        // ================================================================
        // NEW REGIME
        // FY 2025-26 / AY 2026-27
        // ================================================================

        private TaxRegimeResult ComputeNewRegime(
            decimal grossIncome,
            decimal stcg,
            decimal ltcg)
        {
            const decimal standardDeduction =
                75000m;

            var taxableIncome =
                Math.Max(
                    0m,
                    grossIncome -
                    standardDeduction);

            var slabTax =
                ComputeNewRegimeSlabs(
                    taxableIncome,
                    out var slabs);

            var rebate87A =
                CalculateNewRegime87A(
                    taxableIncome,
                    slabTax);

            var normalIncomeTax =
                Math.Max(
                    0m,
                    slabTax -
                    rebate87A);

            var stcgTax =
                CalculateStcgTax(stcg);

            var ltcgTax =
                CalculateLtcgTax(ltcg);

            var taxBeforeSurcharge =
                normalIncomeTax +
                stcgTax +
                ltcgTax;

            var totalIncomeForSurcharge =
                taxableIncome +
                stcg +
                ltcg;

            var surcharge =
                CalculateSurchargeNewRegime(
                    normalIncomeTax,
                    stcgTax,
                    ltcgTax,
                    totalIncomeForSurcharge);

            var cess =
                Math.Round(
                    (taxBeforeSurcharge +
                     surcharge) * 0.04m,
                    0);

            var totalTax =
                Math.Round(
                    taxBeforeSurcharge +
                    surcharge +
                    cess,
                    0);

            return new TaxRegimeResult
            {
                RegimeName =
                    "New Regime (FY 2025-26)",

                StandardDeduction =
                    standardDeduction,

                TotalDeductions =
                    standardDeduction,

                TaxableIncome =
                    Math.Round(
                        taxableIncome,
                        0),

                IncomeTax =
                    Math.Round(
                        normalIncomeTax,
                        0),

                StcgTax =
                    Math.Round(
                        stcgTax,
                        0),

                LtcgTax =
                    Math.Round(
                        ltcgTax,
                        0),

                Surcharge =
                    Math.Round(
                        surcharge,
                        0),

                HealthEducationCess =
                    Math.Round(
                        cess,
                        0),

                Rebate87A =
                    Math.Round(
                        rebate87A,
                        0),

                TotalTax =
                    Math.Round(
                        totalTax,
                        0),

                SlabBreakdown =
                    slabs
            };
        }

        // ================================================================
        // NEW REGIME SLABS
        // ================================================================

        private decimal ComputeNewRegimeSlabs(
            decimal taxableIncome,
            out List<TaxSlabDetailDto> slabs)
        {
            slabs =
                new List<TaxSlabDetailDto>();

            decimal tax = 0m;
            decimal remaining = taxableIncome;

            var brackets =
                new (decimal Limit,
                     decimal Rate,
                     string Label)[]
                {
                    (
                        400000m,
                        0.00m,
                        "Up to ₹4,00,000"
                    ),

                    (
                        400000m,
                        0.05m,
                        "₹4,00,001 – ₹8,00,000"
                    ),

                    (
                        400000m,
                        0.10m,
                        "₹8,00,001 – ₹12,00,000"
                    ),

                    (
                        400000m,
                        0.15m,
                        "₹12,00,001 – ₹16,00,000"
                    ),

                    (
                        400000m,
                        0.20m,
                        "₹16,00,001 – ₹20,00,000"
                    ),

                    (
                        400000m,
                        0.25m,
                        "₹20,00,001 – ₹24,00,000"
                    ),

                    (
                        decimal.MaxValue,
                        0.30m,
                        "Above ₹24,00,000"
                    )
                };

            foreach (var bracket in brackets)
            {
                if (remaining <= 0)
                    break;

                var taxableAmount =
                    Math.Min(
                        remaining,
                        bracket.Limit);

                var slabTax =
                    Math.Round(
                        taxableAmount *
                        bracket.Rate,
                        0);

                slabs.Add(
                    new TaxSlabDetailDto
                    {
                        Slab =
                            bracket.Label,

                        Rate =
                            bracket.Rate * 100,

                        TaxableAmount =
                            taxableAmount,

                        Tax =
                            slabTax
                    });

                tax += slabTax;

                remaining -= taxableAmount;
            }

            return Math.Round(
                tax,
                0);
        }

        // ================================================================
        // NEW REGIME 87A
        // ================================================================

        private decimal CalculateNewRegime87A(
            decimal taxableIncome,
            decimal slabTax)
        {
            if (slabTax <= 0)
                return 0m;

            // Full rebate where taxable income
            // does not exceed ₹12 lakh.
            if (taxableIncome <= 1200000m)
            {
                return Math.Min(
                    slabTax,
                    60000m);
            }

            /*
             * Marginal relief around ₹12 lakh.
             *
             * Maximum tax payable is limited to the
             * income exceeding ₹12 lakh.
             */

            var excessIncome =
                taxableIncome -
                1200000m;

            if (slabTax > excessIncome)
            {
                return Math.Max(
                    0m,
                    slabTax -
                    excessIncome);
            }

            return 0m;
        }

        // ================================================================
        // OLD REGIME
        // ================================================================

        private TaxRegimeResult ComputeOldRegime(
            decimal grossIncome,
            decimal stcg,
            decimal ltcg,
            List<TaxEntry> deductions)
        {
            const decimal standardDeduction =
                50000m;

            var eligibleDeductions =
                CalculateOldRegimeEligibleDeductions(
                    deductions);

            var totalDeductions =
                standardDeduction +
                eligibleDeductions;

            var taxableIncome =
                Math.Max(
                    0m,
                    grossIncome -
                    totalDeductions);

            var slabTax =
                ComputeOldRegimeSlabs(
                    taxableIncome,
                    out var slabs);

            var rebate87A = 0m;

            if (taxableIncome <= 500000m)
            {
                rebate87A =
                    Math.Min(
                        slabTax,
                        12500m);
            }

            var normalIncomeTax =
                Math.Max(
                    0m,
                    slabTax -
                    rebate87A);

            var stcgTax =
                CalculateStcgTax(stcg);

            var ltcgTax =
                CalculateLtcgTax(ltcg);

            var taxBeforeSurcharge =
                normalIncomeTax +
                stcgTax +
                ltcgTax;

            var totalIncomeForSurcharge =
                taxableIncome +
                stcg +
                ltcg;

            var surcharge =
                CalculateSurchargeOldRegime(
                    normalIncomeTax,
                    stcgTax,
                    ltcgTax,
                    totalIncomeForSurcharge);

            var cess =
                Math.Round(
                    (taxBeforeSurcharge +
                     surcharge) * 0.04m,
                    0);

            var totalTax =
                Math.Round(
                    taxBeforeSurcharge +
                    surcharge +
                    cess,
                    0);

            return new TaxRegimeResult
            {
                RegimeName =
                    "Old Regime (FY 2025-26)",

                StandardDeduction =
                    standardDeduction,

                TotalDeductions =
                    totalDeductions,

                TaxableIncome =
                    Math.Round(
                        taxableIncome,
                        0),

                IncomeTax =
                    Math.Round(
                        normalIncomeTax,
                        0),

                StcgTax =
                    Math.Round(
                        stcgTax,
                        0),

                LtcgTax =
                    Math.Round(
                        ltcgTax,
                        0),

                Surcharge =
                    Math.Round(
                        surcharge,
                        0),

                HealthEducationCess =
                    Math.Round(
                        cess,
                        0),

                Rebate87A =
                    Math.Round(
                        rebate87A,
                        0),

                TotalTax =
                    Math.Round(
                        totalTax,
                        0),

                SlabBreakdown =
                    slabs
            };
        }

        // ================================================================
        // OLD REGIME SLABS
        // ================================================================

        private decimal ComputeOldRegimeSlabs(
            decimal taxableIncome,
            out List<TaxSlabDetailDto> slabs)
        {
            slabs =
                new List<TaxSlabDetailDto>();

            decimal tax = 0m;
            decimal remaining = taxableIncome;

            var brackets =
                new (decimal Limit,
                     decimal Rate,
                     string Label)[]
                {
                    (
                        250000m,
                        0.00m,
                        "Up to ₹2,50,000"
                    ),

                    (
                        250000m,
                        0.05m,
                        "₹2,50,001 – ₹5,00,000"
                    ),

                    (
                        500000m,
                        0.20m,
                        "₹5,00,001 – ₹10,00,000"
                    ),

                    (
                        decimal.MaxValue,
                        0.30m,
                        "Above ₹10,00,000"
                    )
                };

            foreach (var bracket in brackets)
            {
                if (remaining <= 0)
                    break;

                var taxableAmount =
                    Math.Min(
                        remaining,
                        bracket.Limit);

                var slabTax =
                    Math.Round(
                        taxableAmount *
                        bracket.Rate,
                        0);

                slabs.Add(
                    new TaxSlabDetailDto
                    {
                        Slab =
                            bracket.Label,

                        Rate =
                            bracket.Rate * 100,

                        TaxableAmount =
                            taxableAmount,

                        Tax =
                            slabTax
                    });

                tax += slabTax;

                remaining -= taxableAmount;
            }

            return Math.Round(
                tax,
                0);
        }

        // ================================================================
        // OLD REGIME DEDUCTIONS
        // ================================================================

        private decimal CalculateOldRegimeEligibleDeductions(
            List<TaxEntry> deductions)
        {
            decimal total = 0m;

            // ------------------------------------------------------------
            // 80C + 80CCC + 80CCD(1)
            //
            // Combined maximum = ₹1,50,000
            // ------------------------------------------------------------

            decimal section80CBucket = 0m;

            // ------------------------------------------------------------
            // 80CCD(1B)
            //
            // Additional NPS deduction = maximum ₹50,000
            // ------------------------------------------------------------

            decimal section80CCD1B = 0m;

            // ------------------------------------------------------------
            // Other deductions
            // ------------------------------------------------------------

            decimal section80D = 0m;
            decimal section80TTA = 0m;
            decimal section80TTB = 0m;
            decimal section80E = 0m;
            decimal section80G = 0m;
            decimal section24B = 0m;

            decimal otherEligibleDeductions = 0m;

            foreach (var deduction in deductions)
            {
                if (deduction.Amount <= 0)
                    continue;

                var section =
                    NormalizeSection(
                        deduction.Section);

                switch (section)
                {
                    // ====================================================
                    // 80C / 80CCC / 80CCD(1)
                    // Combined bucket
                    // ====================================================

                    case "80C":

                    case "80CCC":

                    case "80CCD(1)":

                    case "80CCD":
                        section80CBucket +=
                            deduction.Amount;
                        break;

                    // ====================================================
                    // 80CCD(1B)
                    // Additional NPS
                    // ====================================================

                    case "80CCD(1B)":
                        section80CCD1B +=
                            deduction.Amount;
                        break;

                    // ====================================================
                    // 80D
                    // ====================================================

                    case "80D":
                        section80D +=
                            deduction.Amount;
                        break;

                    // ====================================================
                    // 80E
                    // ====================================================

                    case "80E":
                        section80E +=
                            deduction.Amount;
                        break;

                    // ====================================================
                    // 80G
                    // ====================================================

                    case "80G":
                        section80G +=
                            deduction.Amount;
                        break;

                    // ====================================================
                    // 80TTA
                    // ====================================================

                    case "80TTA":
                        section80TTA +=
                            deduction.Amount;
                        break;

                    // ====================================================
                    // 80TTB
                    // ====================================================

                    case "80TTB":
                        section80TTB +=
                            deduction.Amount;
                        break;

                    // ====================================================
                    // HOME LOAN INTEREST
                    // ====================================================

                    case "24B":
                    case "24(B)":
                        section24B +=
                            deduction.Amount;
                        break;

                    // ====================================================
                    // OTHER SECTIONS
                    // ====================================================

                    case "80EE":
                    case "80EEA":
                    case "80EEB":
                    case "80U":
                    case "80DD":
                    case "80DDB":
                    case "80DD(1)":
                    case "80DD(2)":
                    case "80GGC":
                    case "80GG":

                        otherEligibleDeductions +=
                            deduction.Amount;

                        break;

                    // ====================================================
                    // NORMAL EXPENSES / BILLS
                    // ====================================================

                    default:
                        /*
                         * Examples:
                         *
                         * Bills
                         * Shopping
                         * Grocery
                         * Mobile Bill
                         * Electricity Bill
                         *
                         * These are NOT automatically tax deductions.
                         */

                        break;
                }
            }

            // ============================================================
            // APPLY LIMITS
            // ============================================================

            var allowed80CBucket =
                Math.Min(
                    section80CBucket,
                    150000m);

            var allowed80CCD1B =
                Math.Min(
                    section80CCD1B,
                    50000m);

            var allowed80D =
                Math.Min(
                    section80D,
                    25000m);

            var allowed80TTA =
                Math.Min(
                    section80TTA,
                    10000m);

            var allowed80TTB =
                Math.Min(
                    section80TTB,
                    50000m);

            // ============================================================
            // TOTAL
            // ============================================================

            total =
                allowed80CBucket +
                allowed80CCD1B +
                allowed80D +
                allowed80TTA +
                allowed80TTB +
                section80E +
                section80G +
                section24B +
                otherEligibleDeductions;

            return Math.Max(
                0m,
                total);
        }

        // ================================================================
        // STCG
        // ================================================================

        private decimal CalculateStcgTax(
            decimal stcg)
        {
            if (stcg <= 0)
                return 0m;

            /*
             * Assumes the supplied STCG qualifies
             * for Section 111A special rate.
             */

            return Math.Round(
                stcg * 0.20m,
                0);
        }

        // ================================================================
        // LTCG
        // ================================================================

        private decimal CalculateLtcgTax(
            decimal ltcg)
        {
            if (ltcg <= 0)
                return 0m;

            /*
             * Assumes the supplied LTCG qualifies
             * for Section 112A.
             *
             * ₹1,25,000 exemption.
             */

            var taxableLtcg =
                Math.Max(
                    0m,
                    ltcg - 125000m);

            return Math.Round(
                taxableLtcg * 0.125m,
                0);
        }

        // ================================================================
        // NEW REGIME SURCHARGE
        // ================================================================

        private decimal CalculateSurchargeNewRegime(
            decimal normalTax,
            decimal stcgTax,
            decimal ltcgTax,
            decimal totalIncome)
        {
            if (totalIncome <= 5000000m)
                return 0m;

            var rate =
                GetNewRegimeSurchargeRate(
                    totalIncome);

            var normalSurcharge =
                normalTax * rate;

            var capitalGainSurcharge =
                (stcgTax + ltcgTax) *
                Math.Min(
                    rate,
                    0.15m);

            var surcharge =
                normalSurcharge +
                capitalGainSurcharge;

            return Math.Max(
                0m,
                Math.Round(
                    surcharge,
                    0));
        }

        private decimal GetNewRegimeSurchargeRate(
            decimal totalIncome)
        {
            if (totalIncome <= 5000000m)
                return 0m;

            if (totalIncome <= 10000000m)
                return 0.10m;

            if (totalIncome <= 20000000m)
                return 0.15m;

            return 0.25m;
        }

        // ================================================================
        // OLD REGIME SURCHARGE
        // ================================================================

        private decimal CalculateSurchargeOldRegime(
            decimal normalTax,
            decimal stcgTax,
            decimal ltcgTax,
            decimal totalIncome)
        {
            if (totalIncome <= 5000000m)
                return 0m;

            var rate =
                GetOldRegimeSurchargeRate(
                    totalIncome);

            var normalSurcharge =
                normalTax * rate;

            var capitalGainSurcharge =
                (stcgTax + ltcgTax) *
                Math.Min(
                    rate,
                    0.15m);

            var surcharge =
                normalSurcharge +
                capitalGainSurcharge;

            return Math.Max(
                0m,
                Math.Round(
                    surcharge,
                    0));
        }

        private decimal GetOldRegimeSurchargeRate(
            decimal totalIncome)
        {
            if (totalIncome <= 5000000m)
                return 0m;

            if (totalIncome <= 10000000m)
                return 0.10m;

            if (totalIncome <= 20000000m)
                return 0.15m;

            if (totalIncome <= 50000000m)
                return 0.25m;

            return 0.37m;
        }

        // ================================================================
        // CAPITAL GAIN CLASSIFICATION
        // ================================================================

        private bool IsShortTermCapitalGain(
            TaxEntry entry)
        {
            var category =
                entry.Category ?? string.Empty;

            return
                category.Contains(
                    "STCG",
                    StringComparison.OrdinalIgnoreCase)
                ||
                category.Contains(
                    "Short",
                    StringComparison.OrdinalIgnoreCase);
        }

        private bool IsLongTermCapitalGain(
            TaxEntry entry)
        {
            var category =
                entry.Category ?? string.Empty;

            return
                category.Contains(
                    "LTCG",
                    StringComparison.OrdinalIgnoreCase)
                ||
                category.Contains(
                    "Long",
                    StringComparison.OrdinalIgnoreCase);
        }

        // ================================================================
        // NORMALIZE SECTION
        // ================================================================

        private string NormalizeSection(
            string? section)
        {
            if (string.IsNullOrWhiteSpace(section))
                return string.Empty;

            return section
                .Trim()
                .Replace(" ", "")
                .ToUpperInvariant();
        }

        // ================================================================
        // PDF REPORT
        // ================================================================

        public async Task<byte[]> GenerateReportPdfAsync(
            Guid userId,
            string financialYear)
        {
            var computation =
                await ComputeTaxAsync(
                    userId,
                    financialYear);

            var sb =
                new StringBuilder();

            sb.AppendLine(
                "═══════════════════════════════════════════════════════════════");

            sb.AppendLine(
                $"         INCOME TAX COMPUTATION REPORT — FY {financialYear}");

            sb.AppendLine(
                $"         Assessment Year: {GetAssessmentYear(financialYear)}");

            sb.AppendLine(
                $"         Generated: {DateTime.Now:dd MMM yyyy, hh:mm tt}");

            sb.AppendLine(
                "═══════════════════════════════════════════════════════════════");

            sb.AppendLine();

            // ============================================================
            // INCOME
            // ============================================================

            sb.AppendLine(
                "━━━ INCOME SUMMARY ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            foreach (var entry in computation.Entries
                         .Where(e =>
                             string.Equals(
                                 e.EntryType,
                                 "income",
                                 StringComparison.OrdinalIgnoreCase)))
            {
                sb.AppendLine(
                    $"  {entry.Category,-35} ₹{entry.Amount,15:N0}");
            }

            sb.AppendLine(
                $"  {"Gross Income",-35} ₹{computation.GrossIncome,15:N0}");

            sb.AppendLine();

            // ============================================================
            // CAPITAL GAINS
            // ============================================================

            if (computation.CapitalGains > 0)
            {
                sb.AppendLine(
                    "━━━ CAPITAL GAINS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                foreach (var entry in computation.Entries
                             .Where(e =>
                                 string.Equals(
                                     e.EntryType,
                                     "capital_gain",
                                     StringComparison.OrdinalIgnoreCase)))
                {
                    sb.AppendLine(
                        $"  {entry.Category,-35} ₹{entry.Amount,15:N0}");
                }

                sb.AppendLine(
                    $"  {"Total Capital Gains",-35} ₹{computation.CapitalGains,15:N0}");

                sb.AppendLine(
                    $"  {"STCG",-35} ₹{computation.ShortTermCapitalGains,15:N0}");

                sb.AppendLine(
                    $"  {"LTCG",-35} ₹{computation.LongTermCapitalGains,15:N0}");

                sb.AppendLine();
            }

            // ============================================================
            // DEDUCTIONS
            // ============================================================

            var deductionEntries =
                computation.Entries
                    .Where(e =>
                        string.Equals(
                            e.EntryType,
                            "deduction",
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (deductionEntries.Any())
            {
                sb.AppendLine(
                    "━━━ ENTERED DEDUCTIONS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                foreach (var entry in deductionEntries)
                {
                    var section =
                        string.IsNullOrWhiteSpace(
                            entry.Section)
                            ? ""
                            : $" (Sec {entry.Section})";

                    sb.AppendLine(
                        $"  {entry.Category + section,-35} ₹{entry.Amount,15:N0}");
                }

                sb.AppendLine(
                    $"  {"Total Entered Deductions",-35} ₹{computation.TotalDeductions,15:N0}");

                sb.AppendLine();

                sb.AppendLine(
                    "  Note: Ordinary bills and personal expenses are not");
                sb.AppendLine(
                    "  automatically treated as income-tax deductions.");

                sb.AppendLine();
            }

            // ============================================================
            // NEW REGIME
            // ============================================================

            AppendRegimeDetails(
                sb,
                computation.NewRegime);

            // ============================================================
            // OLD REGIME
            // ============================================================

            AppendRegimeDetails(
                sb,
                computation.OldRegime);

            // ============================================================
            // RECOMMENDATION
            // ============================================================

            sb.AppendLine(
                "═══════════════════════════════════════════════════════════════");

            sb.AppendLine(
                $"  RECOMMENDED REGIME: {computation.RecommendedRegime.ToUpper()} REGIME");

            sb.AppendLine(
                $"  TAX SAVED: ₹{computation.TaxSaved:N0}");

            sb.AppendLine(
                "═══════════════════════════════════════════════════════════════");

            sb.AppendLine();

            sb.AppendLine(
                "  Disclaimer: This is an estimate based on the data provided.");

            sb.AppendLine(
                "  Actual tax may vary based on taxpayer status, age,");
            sb.AppendLine(
                "  residency, income type, capital-gain section and");
            sb.AppendLine(
                "  eligibility for deductions/rebates.");

            sb.AppendLine(
                "  Consult a qualified CA for final tax filing.");

            return Encoding.UTF8.GetBytes(
                sb.ToString());
        }

        // ================================================================
        // PDF REGIME DETAILS
        // ================================================================

        private void AppendRegimeDetails(
            StringBuilder sb,
            TaxRegimeResult regime)
        {
            sb.AppendLine(
                $"━━━ {regime.RegimeName.ToUpper()} ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            sb.AppendLine(
                $"  Standard Deduction                 ₹{regime.StandardDeduction,15:N0}");

            sb.AppendLine(
                $"  Total Deductions                   ₹{regime.TotalDeductions,15:N0}");

            sb.AppendLine(
                $"  Taxable Income                     ₹{regime.TaxableIncome,15:N0}");

            sb.AppendLine();

            sb.AppendLine(
                "  Slab Breakdown:");

            foreach (var slab in regime.SlabBreakdown)
            {
                sb.AppendLine(
                    $"    {slab.Slab,-30} " +
                    $"{slab.Rate,5:N0}%   " +
                    $"₹{slab.TaxableAmount,12:N0} → " +
                    $"₹{slab.Tax,10:N0}");
            }

            sb.AppendLine();

            var incomeTaxBeforeRebate =
                regime.IncomeTax +
                regime.Rebate87A;

            sb.AppendLine(
                $"  Income Tax (before rebate)         ₹{incomeTaxBeforeRebate,15:N0}");

            if (regime.Rebate87A > 0)
            {
                sb.AppendLine(
                    $"  Less: Rebate u/s 87A             (₹{regime.Rebate87A,14:N0})");
            }

            sb.AppendLine(
                $"  Income Tax (after rebate)          ₹{regime.IncomeTax,15:N0}");

            sb.AppendLine(
                $"  STCG Tax                           ₹{regime.StcgTax,15:N0}");

            sb.AppendLine(
                $"  LTCG Tax                           ₹{regime.LtcgTax,15:N0}");

            sb.AppendLine(
                $"  Surcharge                          ₹{regime.Surcharge,15:N0}");

            sb.AppendLine(
                $"  Health & Education Cess (4%)       ₹{regime.HealthEducationCess,15:N0}");

            sb.AppendLine(
                "  ─────────────────────────────────────────────────────────");

            sb.AppendLine(
                $"  TOTAL TAX                          ₹{regime.TotalTax,15:N0}");

            sb.AppendLine();
        }

        // ================================================================
        // ASSESSMENT YEAR
        // ================================================================

        private string GetAssessmentYear(
            string financialYear)
        {
            if (string.IsNullOrWhiteSpace(
                    financialYear))
            {
                return "N/A";
            }

            var parts =
                financialYear
                    .Trim()
                    .Split(
                        '-',
                        StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                return "N/A";

            if (!int.TryParse(
                    parts[0],
                    out var startYear))
            {
                return "N/A";
            }

            var assessmentStart =
                startYear + 1;

            var assessmentEnd =
                (startYear + 2) % 100;

            return
                $"AY {assessmentStart}-{assessmentEnd:D2}";
        }

        // ================================================================
        // DTO MAPPING
        // ================================================================

        private static TaxEntryDto MapToDto(
            TaxEntry e)
        {
            return new TaxEntryDto
            {
                TaxEntryId =
                    e.TaxEntryId,

                FinancialYear =
                    e.FinancialYear,

                Category =
                    e.Category,

                Description =
                    e.Description,

                Amount =
                    e.Amount,

                EntryType =
                    e.EntryType,

                Section =
                    e.Section,

                CreatedAt =
                    e.CreatedAt,

                UpdatedAt =
                    e.UpdatedAt
            };
        }
    }
}

