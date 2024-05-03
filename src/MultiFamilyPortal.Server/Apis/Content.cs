using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MultiFamilyPortal.Data;

namespace MultiFamilyPortal.Apis;

public static class Content
{
    public static WebApplication MapContent(this WebApplication app)
    {
        var contenttGroup = app.MapGroup("/api/content");
        contenttGroup.MapGet("/{contentId}", GetContent);
        
        return app;
    }


    public static async ValueTask<IResult> GetContent(IMFPContext _context, ISiteInfo _siteInfo, string contentId)
    {
        var content = await _context.CustomContent.FirstOrDefaultAsync(x => x.Id == contentId);

        if (content is null)
            return Results.NotFound();

        content.HtmlContent = Regex.Replace(content.HtmlContent, "{Address}", _siteInfo.Address);
        content.HtmlContent = Regex.Replace(content.HtmlContent, "{City}", _siteInfo.City);
        content.HtmlContent = Regex.Replace(content.HtmlContent, "{State}", _siteInfo.State);
        content.HtmlContent = Regex.Replace(content.HtmlContent, "{PostalCode}", _siteInfo.PostalCode);
        content.HtmlContent = Regex.Replace(content.HtmlContent, "{PublicEmail}", _siteInfo.PublicEmail);
        content.HtmlContent = Regex.Replace(content.HtmlContent, "{LegalBusinessName}", _siteInfo.LegalBusinessName);

        return Results.Ok(content);
    }
}
