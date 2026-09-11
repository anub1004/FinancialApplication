using FinancialApplication.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Application.Interfaces
{
    public interface ISettingService            
    {
        Task<SettingDto?> GetSettingsByUserIdAsync(Guid id);
        Task<SettingDto> UpdateSettingAsync(Guid id, SettingDto setting);
    }
}
