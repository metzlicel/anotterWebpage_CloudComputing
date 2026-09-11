using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;

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
}