using AvantiPoint.FileStorage;
using Microsoft.EntityFrameworkCore;
using MultiFamilyPortal.Data;

namespace MultiFamilyPortal.Apis;

public static class Files
{
    public static WebApplication MapFiles(this WebApplication app)
    {
        var FilesGroup = app.MapGroup("/api/files");
        app.MapGet("/property/{propertyId:guid}/file/{fileId:guid}", GetFile);
        
        return app;
    }


    public static async ValueTask<IResult> GetFile(IStorageService storage, IMFPContext _dbContext, Guid propertyId, Guid fileId)
    {
        var file = await _dbContext.UnderwritingProspectFiles.FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.Id == fileId);

        if (file is null)
            return Results.NotFound();

        var path = Path.Combine("underwriting", $"{propertyId}", $"{fileId}{Path.GetExtension(file.Name)}");
        using var fileStream = await storage.GetAsync(path);
        if (fileStream is null || fileStream == Stream.Null || fileStream.Length == 0)
            return Results.NotFound();

        using var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        var data = memoryStream.ToArray();
        var typeInfo = FileTypeLookup.GetFileTypeInfo(file.Name);
        return Results.File(data, typeInfo.MimeType, file.Name);
    }
}
