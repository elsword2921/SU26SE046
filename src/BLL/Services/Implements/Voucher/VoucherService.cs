using System.Data;
using BLL.DTOs;
using BLL.Services.Interfaces.Voucher;
using DAL;
using DAL.Models;
using DAL.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Implements.Voucher;

public class VoucherService : IVoucherService
{
    private readonly AppDbContext _context;

    public VoucherService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> CreateVoucherAsync(Guid managerId, CreateVoucherDto dto)
    {
        ValidateVoucherDates(dto.StartDate, dto.ExpireDate);
        if (dto.RequiredPoints <= 0)
            throw new InvalidOperationException("Required points must be greater than zero.");
        if (dto.Value < 0)
            throw new InvalidOperationException("Voucher value cannot be negative.");
        var now = DateTime.UtcNow;
        var voucher = new DAL.Models.Voucher
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            PartnerName = dto.PartnerName.Trim(),
            VoucherUrl = dto.VoucherUrl,
            ImageUrl = dto.ImageUrl,
            Description = dto.Description,
            TermsAndConditions = dto.TermsAndConditions,
            Value = dto.Value,
            RequiredPoints = dto.RequiredPoints,
            StartDate = dto.StartDate,
            ExpireDate = dto.ExpireDate,
            Status = VoucherStatus.Active,
            CreateAt = now,
            CreatedBy = managerId,
            IsActive = true
        };
        _context.Vouchers.Add(voucher);
        await _context.SaveChangesAsync();
        return voucher.Id;
    }

    public async Task UpdateVoucherAsync(Guid managerId, Guid voucherId, UpdateVoucherDto dto)
    {
        var voucher = await _context.Vouchers
            .FirstOrDefaultAsync(x => x.Id == voucherId && x.IsActive != false);
        if (voucher == null)
            throw new KeyNotFoundException("Voucher not found.");
        ValidateVoucherDates(dto.StartDate, dto.ExpireDate);
        if (dto.RequiredPoints <= 0)
            throw new InvalidOperationException("Required points must be greater than zero.");
        if (dto.Value < 0)
            throw new InvalidOperationException("Voucher value cannot be negative.");
        voucher.Name = dto.Name.Trim();
        voucher.PartnerName = dto.PartnerName.Trim();
        voucher.VoucherUrl = dto.VoucherUrl;
        voucher.ImageUrl = dto.ImageUrl;
        voucher.Description = dto.Description;
        voucher.TermsAndConditions = dto.TermsAndConditions;
        voucher.Value = dto.Value;
        voucher.RequiredPoints = dto.RequiredPoints;
        voucher.StartDate = dto.StartDate;
        voucher.ExpireDate = dto.ExpireDate;
        voucher.UpdateAt = DateTime.UtcNow;
        voucher.UpdatedBy = managerId;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateVoucherStatusAsync(Guid managerId, Guid voucherId, UpdateVoucherStatusDto dto)
    {
        var voucher = await _context.Vouchers
            .FirstOrDefaultAsync(x => x.Id == voucherId && x.IsActive != false);
        if (voucher == null)
            throw new KeyNotFoundException("Voucher not found.");
        voucher.Status = dto.Status;
        voucher.UpdateAt = DateTime.UtcNow;
        voucher.UpdatedBy = managerId;
        await _context.SaveChangesAsync();
    }

    public async Task AddVoucherCodesAsync(Guid managerId, Guid voucherId, AddVoucherCodesDto dto)
    {
        var voucher = await _context.Vouchers
            .FirstOrDefaultAsync(x => x.Id == voucherId && x.IsActive != false);
        if (voucher == null)
            throw new KeyNotFoundException("Voucher not found.");
        if (dto.Codes == null || dto.Codes.Count == 0)
            throw new InvalidOperationException("At least one voucher code is required.");
        var duplicatedInput = dto.Codes
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .GroupBy(x => x.Code.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicatedInput != null)
            throw new InvalidOperationException($"Duplicated voucher code: {duplicatedInput.Key}");
        var codes = dto.Codes
            .Select(x => x.Code.Trim()).ToList();
        if (codes.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Voucher code cannot be empty.");
        var existingCodes = await _context.VoucherCodes
            .Where(x => codes.Contains(x.Code)).Select(x => x.Code).ToListAsync();
        if (existingCodes.Count > 0)
            throw new InvalidOperationException($"Voucher code already exists: {existingCodes[0]}");
        var now = DateTime.UtcNow;
        foreach (var item in dto.Codes)
        {
            if (item.ExpireDate <= now)
                throw new InvalidOperationException($"Voucher code {item.Code} is already expired.");
            if (item.ExpireDate > voucher.ExpireDate)
                throw new InvalidOperationException($"Voucher code {item.Code} expires after its voucher.");
            _context.VoucherCodes.Add(new VoucherCode
            {
                Id = Guid.NewGuid(),
                VoucherId = voucherId,
                Code = item.Code.Trim(),
                ExpireDate = item.ExpireDate,
                Status = VoucherCodeStatus.Available,
                CreateAt = now,
                CreatedBy = managerId,
                IsActive = true
            });
        }
        await _context.SaveChangesAsync();
    }

    public async Task<List<VoucherCodeDto>> GetVoucherCodesAsync(Guid voucherId)
    {
        var exists = await _context.Vouchers.AnyAsync(x => x.Id == voucherId && x.IsActive != false);
        if (!exists)
            throw new KeyNotFoundException("Voucher not found.");
        return await _context.VoucherCodes
            .AsNoTracking()
            .Where(x =>
                x.VoucherId == voucherId &&
                x.IsActive != false)
            .OrderBy(x => x.ExpireDate)
            .Select(x => new VoucherCodeDto
            {
                Id = x.Id,
                Code = x.Code,
                ExpireDate = x.ExpireDate,
                Status = x.Status,
                RedeemedByUserId = x.RedeemedByUserId,
                RedeemedAt = x.RedeemedAt
            })
            .ToListAsync();
    }

    public async Task<List<VoucherDto>> GetAvailableVouchersAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Vouchers
            .AsNoTracking()
            .Where(v =>
                v.IsActive != false &&
                v.Status == VoucherStatus.Active &&
                v.StartDate <= now &&
                v.ExpireDate > now)
            .Select(v => new VoucherDto
            {
                Id = v.Id,
                Name = v.Name,
                PartnerName = v.PartnerName,
                VoucherUrl = v.VoucherUrl,
                ImageUrl = v.ImageUrl,
                Description = v.Description,
                TermsAndConditions = v.TermsAndConditions,
                Value = v.Value,
                RequiredPoints = v.RequiredPoints,
                StartDate = v.StartDate,
                ExpireDate = v.ExpireDate,
                Status = v.Status,
                AvailableQuantity = v.VoucherCodes.Count(c =>
                    c.IsActive != false &&
                    c.Status == VoucherCodeStatus.Available &&
                    c.ExpireDate > now)
            })
            .Where(v => v.AvailableQuantity > 0)
            .OrderBy(v => v.RequiredPoints)
            .ToListAsync();
    }

    public async Task<VoucherDto?> GetVoucherAsync(Guid voucherId)
    {
        var now = DateTime.UtcNow;
        return await _context.Vouchers
            .AsNoTracking()
            .Where(v =>
                v.Id == voucherId &&
                v.IsActive != false)
            .Select(v => new VoucherDto
            {
                Id = v.Id,
                Name = v.Name,
                PartnerName = v.PartnerName,
                VoucherUrl = v.VoucherUrl,
                ImageUrl = v.ImageUrl,
                Description = v.Description,
                TermsAndConditions = v.TermsAndConditions,
                Value = v.Value,
                RequiredPoints = v.RequiredPoints,
                StartDate = v.StartDate,
                ExpireDate = v.ExpireDate,
                Status = v.Status,
                AvailableQuantity = v.VoucherCodes.Count(c =>
                    c.IsActive != false &&
                    c.Status == VoucherCodeStatus.Available &&
                    c.ExpireDate > now)
            })
            .FirstOrDefaultAsync();
    }

    public async Task<RedeemVoucherResultDto> RedeemVoucherAsync(Guid userId, Guid voucherId)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var now = DateTime.UtcNow;
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive != false);
            if (user == null)
                throw new KeyNotFoundException("User not found.");
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(x => x.Id == voucherId && x.IsActive != false);
            if (voucher == null)
                throw new KeyNotFoundException("Voucher not found.");
            if (voucher.Status != VoucherStatus.Active)
                throw new InvalidOperationException("Voucher is currently inactive.");
            if (voucher.StartDate > now)
                throw new InvalidOperationException("Voucher is not available yet.");
            if (voucher.ExpireDate <= now)
                throw new InvalidOperationException("Voucher has expired.");
            if (user.DonationPoint < voucher.RequiredPoints)
                throw new InvalidOperationException("Insufficient donation points.");
            var voucherCode = await _context.VoucherCodes
                .Where(x =>
                    x.VoucherId == voucherId &&
                    x.IsActive != false &&
                    x.Status == VoucherCodeStatus.Available &&
                    x.ExpireDate > now)
                .OrderBy(x => x.ExpireDate)
                .ThenBy(x => x.CreateAt)
                .FirstOrDefaultAsync();
            if (voucherCode == null)
                throw new InvalidOperationException("Voucher is out of stock.");
            user.DonationPoint -= voucher.RequiredPoints;
            user.UpdateAt = now;
            voucherCode.Status = VoucherCodeStatus.Redeemed;
            voucherCode.RedeemedByUserId = userId;
            voucherCode.RedeemedAt = now;
            voucherCode.UpdateAt = now;
            voucherCode.UpdatedBy = userId;
            var redemption = new VoucherRedemption
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VoucherId = voucherId,
                VoucherCodeId = voucherCode.Id,
                PointsSpent = voucher.RequiredPoints,
                RedeemedAt = now,
                CreateAt = now,
                CreatedBy = userId,
                IsActive = true
            };
            _context.VoucherRedemptions.Add(redemption);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new RedeemVoucherResultDto
            {
                VoucherId = voucher.Id,
                VoucherName = voucher.Name,
                PartnerName = voucher.PartnerName,
                Code = voucherCode.Code,
                PointsSpent = redemption.PointsSpent,
                RemainingPoints = user.DonationPoint,
                ExpireDate = voucherCode.ExpireDate
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<MyVoucherDto>> GetMyVouchersAsync(Guid userId)
    {
        return await _context.VoucherRedemptions
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.IsActive != false)
            .OrderByDescending(x => x.RedeemedAt)
            .Select(x => new MyVoucherDto
            {
                VoucherCodeId = x.VoucherCodeId,
                VoucherId = x.VoucherId,
                VoucherName = x.Voucher.Name,
                PartnerName = x.Voucher.PartnerName,
                Code = x.VoucherCode.Code,
                RedeemedAt = x.RedeemedAt,
                ExpireDate = x.VoucherCode.ExpireDate
            })
            .ToListAsync();
    }

    public async Task<List<VoucherRedemptionDto>> GetMyRedemptionsAsync(Guid userId)
    {
        return await _context.VoucherRedemptions
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.IsActive != false)
            .OrderByDescending(x => x.RedeemedAt)
            .Select(x => new VoucherRedemptionDto
            {
                Id = x.Id,
                VoucherId = x.VoucherId,
                VoucherName = x.Voucher.Name,
                PointsSpent = x.PointsSpent,
                RedeemedAt = x.RedeemedAt
            })
            .ToListAsync();
    }

    public async Task<int> GetDonationPointAsync(
        Guid userId)
    {
        var point = await _context.Users
            .AsNoTracking()
            .Where(x =>
                x.Id == userId &&
                x.IsActive != false)
            .Select(x => (int?)x.DonationPoint)
            .FirstOrDefaultAsync();
        if (point == null)
            throw new KeyNotFoundException("User not found.");
        return point.Value;
    }

    private static void ValidateVoucherDates(DateTime startDate,DateTime expireDate)
    {
        if (expireDate <= startDate)
            throw new InvalidOperationException("Expire date must be after start date.");
    }
}