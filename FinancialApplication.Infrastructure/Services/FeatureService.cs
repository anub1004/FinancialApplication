using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Subscription;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Infrastructure.Services
{
    /// <summary>
    /// Implements IFeatureService — admin CRUD operations for Features
    /// with full FeatureAudit trail logging (old/new JSON snapshots).
    /// </summary>
    public class FeatureService : IFeatureService
    {
        private readonly AppDbContext _context;
        private readonly IFeatureAccessResolver _featureAccessResolver;

        public FeatureService(AppDbContext context, IFeatureAccessResolver featureAccessResolver)
        {
            _context = context;
            _featureAccessResolver = featureAccessResolver;
        }

        // ─────────────────────────────────────────────────────────────────────
        // READ
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<FeatureDto>> GetAllFeaturesAsync()
        {
            var features = await _context.Features
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.DisplayName)
                .ToListAsync();

            return features.Select(MapToDto).ToList();
        }

        public async Task<FeatureDto?> GetFeatureByIdAsync(Guid id)
        {
            var feature = await _context.Features.FindAsync(id);
            return feature == null ? null : MapToDto(feature);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<FeatureDto> CreateFeatureAsync(CreateFeatureRequest request)
        {
            // Validate unique FeatureKey (case-insensitive)
            var exists = await _context.Features
                .AnyAsync(f => f.FeatureKey.ToLower() == request.FeatureKey.ToLower());
            if (exists)
                throw new InvalidOperationException($"A feature with key '{request.FeatureKey}' already exists.");

            var feature = new Feature
            {
                FeatureKey  = request.FeatureKey.ToLower(),
                DisplayName = request.DisplayName,
                Description = request.Description,
                Category    = request.Category,
                IsActive    = request.IsActive,
                SortOrder   = request.SortOrder,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            };

            _context.Features.Add(feature);

            // Audit: Created (no OldValues)
            _context.FeatureAudits.Add(new FeatureAudit
            {
                FeatureId   = feature.Id,
                Action      = "Created",
                OldValues   = null,
                NewValues   = SerializeFeature(feature),
                PerformedBy = Guid.Empty, // replaced with caller identity in Phase 6
                CreatedAt   = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return MapToDto(feature);
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<FeatureDto> UpdateFeatureAsync(Guid id, UpdateFeatureRequest request)
        {
            var feature = await _context.Features.FindAsync(id);
            if (feature == null)
                throw new KeyNotFoundException($"Feature with ID {id} not found.");

            var oldSnapshot = SerializeFeature(feature);

            feature.DisplayName = request.DisplayName;
            feature.Description = request.Description;
            feature.Category    = request.Category;
            feature.IsActive    = request.IsActive;
            feature.SortOrder   = request.SortOrder;
            feature.UpdatedAt   = DateTime.UtcNow;

            // Audit: Updated (old → new snapshot)
            _context.FeatureAudits.Add(new FeatureAudit
            {
                FeatureId   = feature.Id,
                Action      = "Updated",
                OldValues   = oldSnapshot,
                NewValues   = SerializeFeature(feature),
                PerformedBy = Guid.Empty,
                CreatedAt   = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return MapToDto(feature);
        }

        // ─────────────────────────────────────────────────────────────────────
        // SOFT DELETE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<bool> DeleteFeatureAsync(Guid id)
        {
            var feature = await _context.Features.FindAsync(id);
            if (feature == null)
                throw new KeyNotFoundException($"Feature with ID {id} not found.");

            var oldSnapshot = SerializeFeature(feature);

            // Soft delete: mark inactive instead of removing the row
            feature.IsActive  = false;
            feature.UpdatedAt = DateTime.UtcNow;

            _context.FeatureAudits.Add(new FeatureAudit
            {
                FeatureId   = feature.Id,
                Action      = "Disabled",
                OldValues   = oldSnapshot,
                NewValues   = SerializeFeature(feature),
                PerformedBy = Guid.Empty,
                CreatedAt   = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // TOGGLE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<bool> ToggleFeatureAsync(Guid id)
        {
            var feature = await _context.Features.FindAsync(id);
            if (feature == null)
                throw new KeyNotFoundException($"Feature with ID {id} not found.");

            var oldSnapshot = SerializeFeature(feature);
            var action = feature.IsActive ? "Disabled" : "Enabled";

            feature.IsActive  = !feature.IsActive;
            feature.UpdatedAt = DateTime.UtcNow;

            _context.FeatureAudits.Add(new FeatureAudit
            {
                FeatureId   = feature.Id,
                Action      = action,
                OldValues   = oldSnapshot,
                NewValues   = SerializeFeature(feature),
                PerformedBy = Guid.Empty,
                CreatedAt   = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Global feature toggle affects all users — invalidate every cached feature set
            _featureAccessResolver.InvalidateAllCaches();

            return feature.IsActive;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private static FeatureDto MapToDto(Feature f) => new FeatureDto
        {
            Id          = f.Id,
            FeatureKey  = f.FeatureKey,
            DisplayName = f.DisplayName,
            Description = f.Description,
            Category    = f.Category,
            IsActive    = f.IsActive,
            SortOrder   = f.SortOrder,
            CreatedAt   = f.CreatedAt,
            UpdatedAt   = f.UpdatedAt
        };

        private static string SerializeFeature(Feature f) =>
            JsonSerializer.Serialize(new
            {
                f.FeatureKey,
                f.DisplayName,
                f.Description,
                f.Category,
                f.IsActive,
                f.SortOrder
            });
    }
}
