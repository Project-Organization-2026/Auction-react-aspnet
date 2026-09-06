using Auction.BLL.DTOs.Lots;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Options;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Auction.BLL.DTOs.Users;
using Auction.BLL.DTOs.Categories;

namespace Auction.BLL.Services;

public class LotsService
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;

    public LotsService(
        IRepositoryWrapper repositoryWrapper,
        IMapper mapper)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
    }

    public async Task<IEnumerable<LotDto>> GetAllAsync()
    {
        IEnumerable<Lot> lots = await _repositoryWrapper.LotsRepository.GetAllAsync(new QueryOptions<Lot>
        {
            Include = q => q.Include(l => l.Seller)
                .Include(l => l.Winner)
                .Include(l => l.Category)
                .Include(l => l.Images)
                .Include(l => l.Bids),
            AsNoTracking = true,
            AsSplitQuery = true,
            OrderByASC = q => q.Id
        });

        return _mapper.Map<IEnumerable<LotDto>>(lots);

    }

    public async Task<LotDto> CreateLotAsync(CreateLotDto createLotDto)
    {
        try
        {
            Lot lot = _mapper.Map<Lot>(createLotDto);

            var seller = await _repositoryWrapper.UsersRepository.GetFirstOrDefaultAsync(new QueryOptions<User>
            {
                Filter = u => u.Id == createLotDto.SellerId,
                AsNoTracking = true,
            });

            if (seller == null)
            {
                throw new ArgumentException($"Seller with ID {createLotDto.SellerId} not found.");
            }

            var category = await _repositoryWrapper.CategoriesRepository.GetFirstOrDefaultAsync(new QueryOptions<Category>
            {
                Filter = c => c.Id == createLotDto.CategoryId,
                AsNoTracking = true,
            });

            if (category == null)
            {
                throw new ArgumentException($"Category with ID {createLotDto.CategoryId} not found.");
            }
            lot.SellerId = createLotDto.SellerId;
            lot.CategoryId = createLotDto.CategoryId;
            lot.CurrentPrice = createLotDto.StartingPrice;
            lot.CreatedAt = DateTime.UtcNow;
            lot.StartTime = DateTime.UtcNow;

            await _repositoryWrapper.LotsRepository.CreateAsync(lot);

            if (await _repositoryWrapper.SaveChangesAsync() != 0)
            {
                var lotDto = _mapper.Map<LotDto>(lot);
                lotDto.Seller = _mapper.Map<UserSummaryDto>(seller);
                lotDto.Category = _mapper.Map<CategoryDto>(category);
                return lotDto;
            }

            throw new InvalidOperationException("Failed to save changes.");
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Failed to save changes in the database.");
        }
    }

    public async Task<LotDto> UpdateLotAsync(UpdateLotDto updateLotDto, int id)
    {
        try
        {
            var lot = await _repositoryWrapper.LotsRepository.GetFirstOrDefaultAsync(new QueryOptions<Lot>
            {
                Filter = l => l.Id == id,
                AsNoTracking = false,
            });

            if (lot == null)
            {
                throw new ArgumentException($"Lot with ID {id} not found.");
            }

            _mapper.Map(updateLotDto, lot);

            if (await _repositoryWrapper.SaveChangesAsync() != 0)
            {
                var updatedLot = await _repositoryWrapper.LotsRepository.GetFirstOrDefaultAsync(new QueryOptions<Lot>
                {
                    Filter = l => l.Id == id,
                    Include = q => q.Include(l => l.Seller)
                        .Include(l => l.Winner)
                        .Include(l => l.Category)
                        .Include(l => l.Images)
                        .Include(l => l.Bids),
                    AsNoTracking = true,
                    AsSplitQuery = true,
                });
                return _mapper.Map<LotDto>(updatedLot);
            }
            throw new InvalidOperationException("Failed to save changes.");
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Failed to save changes in the database.");
        }
    }

    public async Task<bool> DeleteLotAsync(int id)
    {
        try
        {
            var lot = await _repositoryWrapper.LotsRepository.GetFirstOrDefaultAsync(new QueryOptions<Lot>
            {
                Filter = l => l.Id == id,
                AsNoTracking = false,
            });

            if (lot == null)
            {
                throw new ArgumentException($"Lot with ID {id} not found.");
            }

            _repositoryWrapper.LotsRepository.Delete(lot);
            return await _repositoryWrapper.SaveChangesAsync() != 0;
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Failed to save changes in the database.");
        }
    }
}
