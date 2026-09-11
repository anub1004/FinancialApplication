using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Tax;

namespace FinancialApplication.Application.Interfaces
{
    public interface ITaxReportService
    {
        Task<TaxEntryDto> CreateAsync(Guid userId, CreateTaxEntryDto dto);
        Task<List<TaxEntryDto>> GetAllAsync(Guid userId, string financialYear);
        Task<TaxEntryDto> UpdateAsync(Guid userId, Guid entryId, UpdateTaxEntryDto dto);
        Task<bool> DeleteAsync(Guid userId, Guid entryId);
        Task<TaxComputationDto> ComputeTaxAsync(Guid userId, string financialYear);
        Task<byte[]> GenerateReportPdfAsync(Guid userId, string financialYear);
    }
}
