using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace anotterwebpage_WebApi.Repositories;

public class CollabRepository : ICollabRepository
{
    private readonly ApplicationDbContext _context;

    public CollabRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Collab> CreateAsync(Collab collaboration)
    {
        _context.Collabs.Add(collaboration);

        await _context.SaveChangesAsync();

        return collaboration;
    }
    
    public async Task<List<Collab>> GetAllAsync()
    {
        return await _context.Collabs
            .ToListAsync();
    }
    
    public async Task<Collab?> GetByIdAsync(int id)
    {
        return await _context.Collabs
            .FirstOrDefaultAsync(c => c.Id == id);
    }
    
    public async Task<Collab> UpdateAsync(Collab collab)
    {
        _context.Collabs.Update(collab);
        await _context.SaveChangesAsync();

        return collab;
    }
    
    public async Task DeleteAsync(Collab collab)
    {
        _context.Collabs.Remove(collab);
        await _context.SaveChangesAsync();
    }
}