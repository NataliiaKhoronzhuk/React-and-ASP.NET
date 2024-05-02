using MultiFamilyPortal.Services;
using AvantiPoint.FileStorage;
using AvantiPoint.MultiFamilyPortal.Themes;

namespace MultiFamilyPortal.Apis;

public static class Branding
{
    public static WebApplication MapBranding(this WebApplication app)
    {
        var brandingGroup = app.MapGroup("/theme/branding");
        brandingGroup.MapGet("/logo", GetLogo);
        brandingGroup.MapGet("/logo-side", GetLogoSide);
        brandingGroup.MapGet("/logo-dark", GetLogoDark);
        brandingGroup.MapGet("/logo-dark-side", GetLogoDarkSide);
        brandingGroup.MapGet("/resource", GetResource);

        return app;
    }

    private static ValueTask<IResult> GetLogo(IBrandService brandService, IWebHostEnvironment env, IStorageService storageService) =>
        Get(brandService, env, storageService, "logo");

    private static ValueTask<IResult> GetLogoSide(IBrandService brandService, IWebHostEnvironment env, IStorageService storageService) =>
        Get(brandService, env, storageService, "logo-side");

    private static ValueTask<IResult> GetLogoDark(IBrandService brandService, IWebHostEnvironment env, IStorageService storageService) =>
        Get(brandService, env, storageService, "logo-dark");

    private static ValueTask<IResult> GetLogoDarkSide(IBrandService brandService, IWebHostEnvironment env, IStorageService storageService) =>
        Get(brandService, env, storageService, "logo-dark-side");

    private static async ValueTask<IResult> GetResource(string file, IThemeFactory themeFactory)
    {
        var theme = themeFactory.GetFrontendTheme();
        var resource = theme.Resources.FirstOrDefault(x => x.Name.Equals(file, StringComparison.InvariantCultureIgnoreCase));

        if (resource is null)
            return Results.NotFound();

        return await Get(resource.Name, resource.Path, true);
    }

    private static async ValueTask<IResult> Get(IBrandService brand, IWebHostEnvironment env, IStorageService storage, string name, string defaultFile = null, bool redirect = false)
    {
        var Jpg = $"{name}.jpg";
        var Png = $"{name}.png";
        var Svg = $"{name}.svg";

        if (string.IsNullOrEmpty(defaultFile))
            defaultFile = Path.Combine(env.WebRootPath, "default-resources", "logo");

        var savedPng = Path.Combine(defaultFile, Png);
        var savedSvg = Path.Combine(defaultFile, Svg);
        var savedJpg = Path.Combine(defaultFile, Jpg);

        var jpgInfo = FileTypeLookup.GetFileTypeInfo(Jpg);
        var pngInfo = FileTypeLookup.GetFileTypeInfo(Png);
        var svgInfo = FileTypeLookup.GetFileTypeInfo(Svg);

        var _brand = await brand.GetBrandImage(name);
        if (_brand.Stream != Stream.Null)
            return Results.File(_brand.Stream, _brand.MimeType, _brand.FileName);

        else if (redirect)
            return Results.Redirect(defaultFile);

        else if (await storage.ExistsAsync(savedPng))
            return Results.File(savedPng, pngInfo.MimeType);

        else if (await storage.ExistsAsync(savedSvg))
            return Results.File(savedSvg, svgInfo.MimeType);

        
        else if (await storage.ExistsAsync(savedJpg))
            return Results.File(savedJpg, jpgInfo.MimeType);

        else
            return Results.NotFound();
    }
}
